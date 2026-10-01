using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    public sealed class ValueContributionProjection
    {
        public ValueContributionProjection(ValueContribution contribution, string sourceLabel)
        {
            Contribution = contribution;
            SourceLabel = string.IsNullOrWhiteSpace(sourceLabel)
                ? throw new ArgumentException("A projected contribution requires a readable source label.", nameof(sourceLabel))
                : sourceLabel;
        }

        public ValueContribution Contribution { get; }
        public ValueContributionKind Kind => Contribution.Kind;
        public int Amount => Contribution.Amount;
        public string SourceLabel { get; }
        public MinionId SourceMinionId => Contribution.SourceMinionId;
    }

    public sealed class DefenseChoicePreviewProjection
    {
        public DefenseChoicePreviewProjection(
            CardInstanceId? cardInstanceId,
            string choiceLabel,
            IEnumerable<ValueContributionProjection> defenseContributions,
            int? defenseTotal,
            DefenseBlockResult blockResult,
            AttackResolutionOutcome outcome,
            bool suppressesMinionContributions = false)
        {
            // Milestone 8.11a relaxed this by exactly one case. Declining still never has a Total - it
            // contributes nothing and there is nothing to total. A card usually has one, but rulebook
            // p.14's Block has none at all: it settles the attack by its condition and skips the
            // comparison, so previewing it as a card worth zero would be previewing a card it is not.
            // The Total is absent there, and `BlockResult` is what the preview says instead.
            if (!cardInstanceId.HasValue && defenseTotal.HasValue)
            {
                throw new ArgumentException("Only a Defense-card preview has a Defense Total.");
            }

            if (cardInstanceId.HasValue && !defenseTotal.HasValue && blockResult == DefenseBlockResult.None)
            {
                throw new ArgumentException(
                    "A Defense-card preview without a Defense Total is a Block, and states its verdict.");
            }

            CardInstanceId = cardInstanceId;
            ChoiceLabel = string.IsNullOrWhiteSpace(choiceLabel)
                ? throw new ArgumentException("A Defense preview requires a readable choice label.", nameof(choiceLabel))
                : choiceLabel;
            var contributions = (defenseContributions ??
                throw new ArgumentNullException(nameof(defenseContributions))).ToArray();
            if (defenseTotal.HasValue && defenseTotal.Value != contributions.Sum(value => value.Amount))
            {
                throw new ArgumentException("A Defense preview total must equal its complete contribution list.");
            }

            DefenseContributions = Array.AsReadOnly(contributions);
            DefenseTotal = defenseTotal;
            BlockResult = blockResult;
            Outcome = outcome;
            SuppressesMinionContributions = suppressesMinionContributions;
        }

        /// <summary>
        /// Milestone 8.20a. Whether this offer's own printed text takes the nearby minions out of the
        /// Defense Total - Arien's three Duelist cards' "Ignore all minion defense modifiers", which
        /// <c>MinionDefenseContributionPolicy.SuppressAll</c> declares on the action.
        ///
        /// <para>It is stated rather than inferred because a numeric Defense with no minion
        /// contributions has two possible meanings - a card that suppressed them and a preview that
        /// dropped them - and only the card can tell them apart. Defaulting to <c>false</c> is the
        /// safe half: a suppressing offer whose builder forgets this fails
        /// <see cref="PendingDefensePreviewProjection"/>'s comparison loudly, which is what happened
        /// before this existed.</para>
        /// </summary>
        public bool SuppressesMinionContributions { get; }

        public CardInstanceId? CardInstanceId { get; }
        public bool IsDecline => !CardInstanceId.HasValue;
        public string ChoiceLabel { get; }
        public IReadOnlyList<ValueContributionProjection> DefenseContributions { get; }
        public int? DefenseTotal { get; }
        public DefenseBlockResult BlockResult { get; }
        public AttackResolutionOutcome Outcome { get; }
    }

    public sealed class PendingDefensePreviewProjection
    {
        public PendingDefensePreviewProjection(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            IEnumerable<ValueContributionProjection> attackContributions,
            int attackTotal,
            IEnumerable<DefenseChoicePreviewProjection> choices,
            IEnumerable<ValueContributionProjection> minionContributions)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("A Defense preview requires current resolution and choice identities.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            var projectedAttack = (attackContributions ??
                throw new ArgumentNullException(nameof(attackContributions))).ToArray();
            var projectedChoices = (choices ?? throw new ArgumentNullException(nameof(choices))).ToArray();
            var projectedMinions = (minionContributions ??
                throw new ArgumentNullException(nameof(minionContributions))).ToArray();
            if (projectedAttack.Sum(value => value.Amount) != attackTotal)
            {
                throw new ArgumentException("A Defense preview Attack Total must equal its complete contribution list.");
            }

            if (projectedChoices.Length == 0 || projectedChoices.Count(value => value.IsDecline) > 1 ||
                projectedChoices.Where(value => value.CardInstanceId.HasValue)
                    .Select(value => value.CardInstanceId.Value).Distinct().Count() !=
                projectedChoices.Count(value => value.CardInstanceId.HasValue))
            {
                throw new ArgumentException("A Defense preview requires unique legal choices.");
            }

            if (projectedMinions.Any(value => value.Kind != ValueContributionKind.MinionDefenseModifier ||
                    !value.SourceMinionId.IsDefined) ||
                projectedMinions.Select(value => value.SourceMinionId).Distinct().Count() != projectedMinions.Length)
            {
                throw new ArgumentException("A Defense preview requires unique typed minion contributions.");
            }

            foreach (var choice in projectedChoices)
            {
                var expected = AttackOutcomeComparison.Create(
                    attackTotal,
                    choice.DefenseTotal,
                    choice.BlockResult);
                var choiceMinions = choice.DefenseContributions
                    .Where(value => value.Kind == ValueContributionKind.MinionDefenseModifier)
                    .Select(value => value.Contribution);
                // Rulebook p.14 gives a Defense card two mutually exclusive shapes. Only an
                // ordinary numeric Defense calculates a Total and composes the shared minion
                // modifiers. A Block skips that comparison entirely, so its preview deliberately
                // carries neither a Total nor contributions even when nearby minions would modify a
                // numeric card. Decline has the same empty contribution shape for a different reason:
                // no Defense action was performed. Treating every non-decline card as numeric made a
                // valid Block beside a contributing minion throw while projecting the accepted
                // Attack-target command, leaving the live board looking as though its click failed.
                //
                // Milestone 8.20a adds p.14's third shape, and for the same reason the second was
                // added: a numeric Defense whose own text reads "Ignore all minion defense modifiers"
                // composes none of them, so measuring it against the shared set made every Attack on
                // a hero holding one throw while projecting the accepted Attack-target command - the
                // identical symptom, one card shape later. The card says which of the two it is;
                // nothing here guesses from an empty list.
                var usesNumericDefense = !choice.IsDecline &&
                    choice.BlockResult == DefenseBlockResult.None;
                var composesMinionModifiers = usesNumericDefense &&
                    !choice.SuppressesMinionContributions;
                var hasExpectedMinionSemantics = composesMinionModifiers
                    ? choiceMinions.SequenceEqual(
                        projectedMinions.Select(value => value.Contribution))
                    : !choiceMinions.Any();
                if (choice.Outcome != expected.Outcome ||
                    !hasExpectedMinionSemantics)
                {
                    throw new ArgumentException("A Defense choice preview must match shared comparison and minion semantics.");
                }
            }

            AttackContributions = Array.AsReadOnly(projectedAttack);
            AttackTotal = attackTotal;
            Choices = Array.AsReadOnly(projectedChoices);
            MinionContributions = Array.AsReadOnly(projectedMinions);
        }

        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public IReadOnlyList<ValueContributionProjection> AttackContributions { get; }
        public int AttackTotal { get; }
        public IReadOnlyList<DefenseChoicePreviewProjection> Choices { get; }
        public IReadOnlyList<ValueContributionProjection> MinionContributions { get; }
    }

    /// <summary>
    /// The minion Defense modifiers that would apply if one currently legal hero Attack target were
    /// selected, whether the target comes from the ordinary Attack choice or a typed Attack-intent
    /// effect choice such as Charge. The rules query computes these before presentation sees them;
    /// hovering merely selects one already-projected candidate.
    /// </summary>
    public sealed class AttackTargetDefensePreviewProjection
    {
        public AttackTargetDefensePreviewProjection(
            HeroId heroId,
            IEnumerable<ValueContributionProjection> minionContributions)
            : this(heroId, HeroOccupancy.FirstBody, minionContributions)
        {
        }

        /// <summary>
        /// Milestone 9.21a (RZ-Q3). The preview for one BODY of the hero: an Attack targets a body, and
        /// p.14's minion modifiers are measured from that body's space.
        /// </summary>
        public AttackTargetDefensePreviewProjection(
            HeroId heroId,
            int heroBody,
            IEnumerable<ValueContributionProjection> minionContributions)
        {
            if (heroBody < HeroOccupancy.FirstBody)
            {
                throw new ArgumentOutOfRangeException(nameof(heroBody), heroBody, "A hero body ordinal starts at 1.");
            }

            if (!heroId.IsDefined)
            {
                throw new ArgumentException("An Attack-target Defense preview requires a hero.", nameof(heroId));
            }

            var contributions = (minionContributions ??
                throw new ArgumentNullException(nameof(minionContributions))).ToArray();
            if (contributions.Any(value =>
                    value.Kind != ValueContributionKind.MinionDefenseModifier ||
                    !value.SourceMinionId.IsDefined ||
                    (value.Amount != -1 && value.Amount != 1)) ||
                contributions.Select(value => value.SourceMinionId).Distinct().Count() !=
                    contributions.Length)
            {
                throw new ArgumentException(
                    "An Attack-target Defense preview requires unique signed minion modifiers.",
                    nameof(minionContributions));
            }

            HeroId = heroId;
            HeroBody = heroBody;
            MinionContributions = Array.AsReadOnly(contributions);
        }

        public HeroId HeroId { get; }

        /// <summary>Milestone 9.21a. Which body of the hero this preview is measured from.</summary>
        public int HeroBody { get; }
        public IReadOnlyList<ValueContributionProjection> MinionContributions { get; }

        /// <summary>
        /// Milestone 9.21a. The (hero, body) pairs a projection previews, in order: every legal hero
        /// body of an ordinary Attack choice, then every hero a typed Attack-intent effect choice may
        /// select, as the body standing on the candidate's space. The session builds its previews from
        /// this list and the projection checks them against it, so the two cannot disagree.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<HeroId, int>> ExpectedKeys(
            PendingAttackTargetChoice attackChoice,
            GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice effectChoice,
            IEnumerable<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview> effectChoicePreviews,
            IEnumerable<HeroOccupancy> heroes)
        {
            var bodies = (heroes ?? Enumerable.Empty<HeroOccupancy>()).ToArray();
            var keys = new List<KeyValuePair<HeroId, int>>();
            if (attackChoice != null)
            {
                keys.AddRange(attackChoice.LegalTargets
                    .Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => new KeyValuePair<HeroId, int>(target.HeroId, target.HeroBody)));
            }

            if (effectChoice != null)
            {
                keys.AddRange((effectChoicePreviews ??
                        Enumerable.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>())
                    .Where(preview =>
                        preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Attack &&
                        preview.Occupant.Kind == BoardObjectKind.Hero &&
                        effectChoice.SelectInvocations.Any(invocation =>
                            invocation.Candidate.Equals(
                                EffectChoiceCandidate.Value_(preview.CandidateValue))))
                    .Select(preview => new KeyValuePair<HeroId, int>(
                        preview.Occupant.HeroId,
                        bodies
                            .Where(body => body.HeroId.Equals(preview.Occupant.HeroId) &&
                                body.Coordinate.Equals(preview.Coordinate))
                            .Select(body => body.Body)
                            .DefaultIfEmpty(HeroOccupancy.FirstBody)
                            .First())));
            }

            return Array.AsReadOnly(keys.Distinct().ToArray());
        }
    }

    public sealed class FastTravelOptionProjection
    {
        public FastTravelOptionProjection(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            IEnumerable<HexCoordinate> legalDestinations)
        {
            if (replacementOffer.InvocationContext != ActionInvocationContext.Replacement ||
                !replacedMovementActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Fast Travel projection requires replacement and replaced-action identities.");
            }

            ReplacementOffer = replacementOffer;
            ReplacedMovementActionDefinitionId = replacedMovementActionDefinitionId;
            LegalDestinations = Array.AsReadOnly((legalDestinations ??
                throw new ArgumentNullException(nameof(legalDestinations))).ToArray());
            if (LegalDestinations.Count == 0 || LegalDestinations.Distinct().Count() != LegalDestinations.Count)
            {
                throw new ArgumentException("Fast Travel projection requires unique legal destinations.", nameof(legalDestinations));
            }
        }

        public ActionOffer ReplacementOffer { get; }
        public ActionDefinitionId ReplacedMovementActionDefinitionId { get; }
        public IReadOnlyList<HexCoordinate> LegalDestinations { get; }
    }

    /// <summary>
    /// Milestone 6.3a: one currently available replaceable Attack, the tokens the general Clear
    /// could take instead, and the exact command for every subset a player may confirm.
    ///
    /// Milestone 6.3 implemented Clear and deliberately shipped no player-facing selection, so the
    /// configured HUD had no way to submit the `PerformClear` command that already existed. This is
    /// what that surface reads. Nothing here is new legality: the offer is
    /// <see cref="GeneralRuleActionOfferQuery.GetAvailableClearOffer"/>'s and the candidates are
    /// <see cref="ClearActionQuery.FindLegalTokenIds"/>'s, which are the same two answers the
    /// command path asks on its way in.
    ///
    /// The invocations are enumerated here rather than composed by presentation, because a payload a
    /// view builds is a payload a view can build wrongly. Adjacency bounds the candidate set to the
    /// six neighbouring spaces, so the enumeration is at most 64 commands and is refused outright
    /// above that - a bound this type asserts rather than assumes, since an unbounded candidate set
    /// would silently make the enumeration exponential.
    /// </summary>
    public sealed class ClearOptionProjection
    {
        /// <summary>The six neighbours of the actor's space, which is every space a Clear can reach.</summary>
        public const int MaximumCandidates = 6;

        public ClearOptionProjection(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<PlacedToken> adjacentTokens)
        {
            if (replacementOffer.InvocationContext != ActionInvocationContext.Replacement ||
                !replacedAttackActionDefinitionId.IsDefined)
            {
                throw new ArgumentException(
                    "Clear projection requires replacement and replaced-action identities.");
            }

            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId;
            AdjacentTokens = Array.AsReadOnly((adjacentTokens ??
                throw new ArgumentNullException(nameof(adjacentTokens))).ToArray());
            // Milestone 8.8b: the offer's availability is decided here, from the token list
            // `ClearActionQuery.FindLegalTokenIds` produced - so this and the refusal the turn
            // authority makes at the start are one query asked twice rather than two counts. p.13's
            // "any number" includes zero, so an offer with a candidate stays available and the empty
            // subset stays confirmable; what is stamped unavailable is a Clear that could remove
            // nothing whatever the hero chose.
            ReplacementOffer = replacementOffer.IsAvailable && AdjacentTokens.Count == 0
                ? replacementOffer.WithAvailability(false)
                : replacementOffer;
            if (AdjacentTokens.Any(token => token == null) ||
                AdjacentTokens.Select(token => token.Id).Distinct().Count() != AdjacentTokens.Count)
            {
                throw new ArgumentException(
                    "Clear projection requires distinct placed tokens.", nameof(adjacentTokens));
            }

            if (AdjacentTokens.Count > MaximumCandidates)
            {
                throw new ArgumentException(
                    "A Clear can only reach the actor's six adjacent spaces.", nameof(adjacentTokens));
            }

            // Milestone 8.8b: an offer the authority would refuse carries no command at all, which is
            // the same rule a greyed action offer follows. Without it this option would still project
            // the empty subset - a `PerformClear` the rules now reject - and "the projection offers only
            // what the rules accept" would stop being true, which is exactly what
            // `DeterministicMatchLoop` caught when the refusal landed without this.
            SubsetInvocations = Array.AsReadOnly(ReplacementOffer.IsAvailable
                ? EnumerateSubsets(
                    replacedAttackActionDefinitionId,
                    AdjacentTokens.Select(token => token.Id).ToArray())
                : Array.Empty<DirectorCommand>());
        }

        public ActionOffer ReplacementOffer { get; }
        public ActionDefinitionId ReplacedAttackActionDefinitionId { get; }

        /// <summary>
        /// The tokens this Clear may take, in the authority's own order. Each is the board's own
        /// instance, so a caller reads the coordinate and the trusted definition off the piece rather
        /// than looking either up.
        /// </summary>
        public IReadOnlyList<PlacedToken> AdjacentTokens { get; }

        /// <summary>
        /// One canonical <c>PerformClear</c> per confirmable subset, the empty one first. p.13 permits
        /// any number, and milestone 6.3 already accepts none, so confirming nothing is an invocation
        /// like any other rather than a case a surface has to special-case.
        ///
        /// <para>Empty when <see cref="ReplacementOffer"/> is unavailable - milestone 8.8b's Clear with
        /// no adjacent token. The option is still projected, because the control keeps its label and its
        /// place, but it carries no command, exactly as a greyed action offer does.</para>
        /// </summary>
        public IReadOnlyList<DirectorCommand> SubsetInvocations { get; }

        /// <summary>
        /// The one invocation that clears exactly these tokens, or <c>null</c> when the set is not a
        /// subset this option offers. Order and duplication do not matter: a subset is a set, and the
        /// command orders its own ids.
        /// </summary>
        public DirectorCommand FindInvocation(IEnumerable<TokenInstanceId> selectedTokenIds)
        {
            if (selectedTokenIds == null)
            {
                throw new ArgumentNullException(nameof(selectedTokenIds));
            }

            var selected = new HashSet<TokenInstanceId>(selectedTokenIds);
            return SubsetInvocations.SingleOrDefault(invocation =>
                invocation.TokenInstanceIds.Count == selected.Count &&
                invocation.TokenInstanceIds.All(selected.Contains));
        }

        private static DirectorCommand[] EnumerateSubsets(
            ActionDefinitionId replacedAttackActionDefinitionId,
            IReadOnlyList<TokenInstanceId> candidates)
        {
            var invocations = new DirectorCommand[1 << candidates.Count];
            for (var mask = 0; mask < invocations.Length; mask++)
            {
                var subset = new List<TokenInstanceId>();
                for (var index = 0; index < candidates.Count; index++)
                {
                    if ((mask & (1 << index)) != 0)
                    {
                        subset.Add(candidates[index]);
                    }
                }

                invocations[mask] = DirectorCommand.PerformClear(
                    replacedAttackActionDefinitionId, subset);
            }

            return invocations;
        }
    }

    /// <summary>
    /// One hero's rulebook p.15/p.16 progression as it currently stands: the Level, the coins, and the
    /// items, with each item reduced to the icon it raises and the dashboard slot it sits under.
    ///
    /// Milestone 7.8a. Before it, nothing about progression reached a reader at all - a hero's Level,
    /// coins and items were authoritative, persisted, and invisible - so the player-facing overview had
    /// nothing to draw and the leveling prompt had no coins to report. This is that missing projection,
    /// and it is a projection rather than a calculation: <see cref="ItemBonuses"/> counts the items a
    /// hero holds per icon and stops there. p.16's "+1 to the matching icon on each of your Action
    /// cards" is <c>ItemBonusQuery</c>'s, which composes it into a card's total from the printed value
    /// that card actually has; a reader that multiplied these counts into a card would be answering that
    /// question a second time and would get the answer wrong for every card with no matching icon.
    /// </summary>
    public sealed class HeroProgressionProjection
    {
        public HeroProgressionProjection(
            HeroId heroId,
            int level,
            int coins,
            bool leveledThisRound,
            IEnumerable<KeyValuePair<PrintedValueChannel, int>> itemBonuses)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException(
                    "A projected progression names the hero it belongs to.", nameof(heroId));
            }

            var bonuses = (itemBonuses ?? throw new ArgumentNullException(nameof(itemBonuses))).ToArray();
            if (bonuses.Any(entry => entry.Key == PrintedValueChannel.None || entry.Value <= 0) ||
                bonuses.Select(entry => entry.Key).Distinct().Count() != bonuses.Length)
            {
                throw new ArgumentException(
                    "Item bonuses name each raised icon once, with a positive count.",
                    nameof(itemBonuses));
            }

            HeroId = heroId;
            Level = level;
            Coins = coins;
            LeveledThisRound = leveledThisRound;
            ItemBonuses = Array.AsReadOnly(bonuses);
        }

        public HeroId HeroId { get; }

        /// <summary>p.15's Hero Level, between 1 and 8.</summary>
        public int Level { get; }

        /// <summary>What this hero currently has to spend, from the one ledger p.13, p.15 and p.8 share.</summary>
        public int Coins { get; }

        /// <summary>Whether this hero bought a level this round, which is what p.8's Pity Coin reads.</summary>
        public bool LeveledThisRound { get; }

        /// <summary>
        /// How many items this hero holds for each icon they raise, in icon order. An icon with no item
        /// is absent rather than present with a zero, so a reader draws the strip it is given.
        /// </summary>
        public IReadOnlyList<KeyValuePair<PrintedValueChannel, int>> ItemBonuses { get; }
    }

    public readonly struct MatchCommitmentProjection
    {
        public MatchCommitmentProjection(
            HeroId heroId,
            TurnCardCommitmentStatus status,
            bool canPass,
            CardInstanceId? revealedCardInstanceId)
        {
            HeroId = heroId;
            Status = status;
            CanPass = canPass;
            RevealedCardInstanceId = revealedCardInstanceId;
        }

        public HeroId HeroId { get; }
        public TurnCardCommitmentStatus Status { get; }
        public bool CanPass { get; }
        public CardInstanceId? RevealedCardInstanceId { get; }
    }

    /// <summary>
    /// Stable semantic identity of one genuine pending player-decision family.
    ///
    /// <para>This is deliberately narrower than presentation's player-journey vocabulary and is not
    /// a universal choice kind. A family identifies which established typed pending object is asking;
    /// its exact answer payloads remain the distinct <see cref="DirectorCommand"/> instances carried
    /// by <see cref="PendingDecisionContract"/>.</para>
    /// </summary>
    public readonly struct PendingDecisionFamilyId : IEquatable<PendingDecisionFamilyId>
    {
        public PendingDecisionFamilyId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException(
                    "A pending-decision family ID must not be blank.", nameof(value))
                : value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PendingDecisionFamilyId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is PendingDecisionFamilyId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>The bounded production identities for the twelve configured pending-decision families.</summary>
    public static class PendingDecisionFamilies
    {
        public static readonly PendingDecisionFamilyId Initiative =
            new PendingDecisionFamilyId("initiative");
        public static readonly PendingDecisionFamilyId Movement =
            new PendingDecisionFamilyId("movement");
        public static readonly PendingDecisionFamilyId AttackTarget =
            new PendingDecisionFamilyId("attack-target");
        public static readonly PendingDecisionFamilyId Defense =
            new PendingDecisionFamilyId("defense");
        public static readonly PendingDecisionFamilyId Effect =
            new PendingDecisionFamilyId("effect");
        public static readonly PendingDecisionFamilyId Correction =
            new PendingDecisionFamilyId("correction");
        public static readonly PendingDecisionFamilyId MinionBattle =
            new PendingDecisionFamilyId("minion-battle");
        public static readonly PendingDecisionFamilyId HeroRespawn =
            new PendingDecisionFamilyId("hero-respawn");
        public static readonly PendingDecisionFamilyId SystemTransition =
            new PendingDecisionFamilyId("system-transition");
        public static readonly PendingDecisionFamilyId RemovalPrevention =
            new PendingDecisionFamilyId("removal-prevention");

        /// <summary>
        /// Milestone 8.35g. A finished turn waiting for its own player to hand it on. It has one
        /// command and no candidates, which is what a consent is.
        /// </summary>
        public static readonly PendingDecisionFamilyId TurnRelease =
            new PendingDecisionFamilyId("turn-release");

        /// <summary>
        /// Milestone 9.21b. "Which of you acts" (RZ-Q4): a hero standing on several spaces picks the
        /// body that performs the action his card is about to choose - one answer per standing body.
        /// </summary>
        public static readonly PendingDecisionFamilyId ActingBody =
            new PendingDecisionFamilyId("acting-body");
    }

    /// <summary>
    /// One immutable application answer contract for the genuine pending player decision standing in
    /// a <see cref="MatchSessionProjection"/>.
    ///
    /// <para>It carries only semantic family identity and the ordered exact commands already implied
    /// by the existing typed pending object. A command is an answer token, not pre-authorization:
    /// <see cref="MatchSession.Execute"/> remains the sole legality and result authority and revalidates
    /// every submission.</para>
    /// </summary>
    public sealed class PendingDecisionContract
    {
        internal PendingDecisionContract(
            PendingDecisionFamilyId familyId,
            IEnumerable<DirectorCommand> commands)
        {
            if (!familyId.IsDefined)
            {
                throw new ArgumentException(
                    "A pending-decision contract requires a stable family ID.", nameof(familyId));
            }

            var materialized = (commands ?? throw new ArgumentNullException(nameof(commands))).ToArray();
            if (materialized.Length == 0 || materialized.Any(command => command == null))
            {
                throw new ArgumentException(
                    "A standing pending decision requires at least one exact command and no nulls.",
                    nameof(commands));
            }

            if (materialized.Distinct().Count() != materialized.Length)
            {
                throw new ArgumentException(
                    "A pending-decision contract cannot publish a duplicate command.", nameof(commands));
            }

            FamilyId = familyId;
            Commands = Array.AsReadOnly(materialized);
        }

        public PendingDecisionFamilyId FamilyId { get; }
        public IReadOnlyList<DirectorCommand> Commands { get; }

        internal static PendingDecisionContract Create(
            PendingInitiativeChoice initiative,
            PendingMovementChoice movement,
            PendingAttackTargetChoice attackTarget,
            PendingDefenseResponseChoice defense,
            PendingEffectChoice effect,
            GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection correction,
            PendingMinionBattleChoice minionBattle,
            PendingHeroRespawnChoice heroRespawn,
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep systemTransition,
            RemovalPreventionPrompt removalPrevention,
            GuardsDigital.Core.Cards.PendingTurnRelease turnRelease,
            PendingActingBodyChoice actingBody = null)
        {
            var standing = new List<PendingDecisionContract>();
            if (initiative != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.Initiative,
                    initiative.CandidateCardInstanceIds.Select(cardId =>
                        DirectorCommand.ChooseTiedActor(initiative.OwningSide, cardId))));
            }

            if (movement != null)
            {
                var commands = movement.LegalDestinations
                    .Select(destination => DirectorCommand.ChooseMovementDestination(
                        movement.ResolutionId, movement.ChoiceId, destination.Destination))
                    .ToList();
                if (movement.CanStop)
                {
                    commands.Add(DirectorCommand.StopMovement(
                        movement.ResolutionId, movement.ChoiceId));
                }

                standing.Add(new PendingDecisionContract(PendingDecisionFamilies.Movement, commands));
            }

            if (attackTarget != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.AttackTarget,
                    // Milestone 9.21a. One answer per legal BODY; a hero with one legal body is answered
                    // by its id alone, exactly as before, and a body is named only where it must be.
                    attackTarget.LegalTargets
                        .Where(target => target.Kind == AttackTargetKind.Hero)
                        .Select(target =>
                            attackTarget.LegalTargets.Count(other =>
                                    other.Kind == AttackTargetKind.Hero && other.HeroId.Equals(target.HeroId)) > 1
                                ? DirectorCommand.ChooseAttackTarget(
                                    attackTarget.ResolutionId, attackTarget.ChoiceId,
                                    target.HeroId, target.HeroBody)
                                : DirectorCommand.ChooseAttackTarget(
                                    attackTarget.ResolutionId, attackTarget.ChoiceId, target.HeroId))
                        .Concat(attackTarget.LegalTargetMinionIds.Select(minionId =>
                            DirectorCommand.ChooseAttackTarget(
                                attackTarget.ResolutionId, attackTarget.ChoiceId, minionId)))));
            }

            if (defense != null)
            {
                var commands = defense.LegalDefenseOffers
                    .Select(offer => DirectorCommand.ChooseDefenseCard(
                        defense.ResolutionId, defense.ChoiceId, offer.SourceCardInstanceId))
                    .ToList();
                if (defense.CanDecline)
                {
                    commands.Add(DirectorCommand.DeclineDefense(
                        defense.ResolutionId, defense.ChoiceId));
                }

                standing.Add(new PendingDecisionContract(PendingDecisionFamilies.Defense, commands));
            }

            if (effect != null)
            {
                var commands = effect.SelectInvocations
                    .Select(DirectorCommand.AnswerEffectChoice)
                    .Concat(effect.DeselectInvocations.Select(DirectorCommand.AnswerEffectChoice))
                    .ToList();
                if (effect.ConfirmInvocation != null)
                {
                    commands.Add(DirectorCommand.AnswerEffectChoice(effect.ConfirmInvocation));
                }

                if (effect.DeclineInvocation != null)
                {
                    commands.Add(DirectorCommand.AnswerEffectChoice(effect.DeclineInvocation));
                }

                if (effect.StopInvocation != null)
                {
                    commands.Add(DirectorCommand.AnswerEffectChoice(effect.StopInvocation));
                }

                standing.Add(new PendingDecisionContract(PendingDecisionFamilies.Effect, commands));
            }

            if (correction != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.Correction,
                    correction.SelectInvocations.Select(invocation =>
                    {
                        switch (invocation.Kind)
                        {
                            case GuardsDigital.Core.Rules.Displacement
                                .DisplacedMinionCorrectionCommandKind.SelectSubject:
                                return DirectorCommand.ChooseCorrectionSubject(invocation);
                            case GuardsDigital.Core.Rules.Displacement
                                .DisplacedMinionCorrectionCommandKind.SelectDestination:
                                return DirectorCommand.ChooseCorrectionDestination(invocation);
                            default:
                                throw new InvalidOperationException(
                                    "A correction projected an unsupported command kind.");
                        }
                    })));
            }

            if (minionBattle != null)
            {
                var commands = minionBattle.SelectInvocations
                    .Select(DirectorCommand.SelectMinionBattleRemoval)
                    .Concat(minionBattle.DeselectInvocations.Select(
                        DirectorCommand.DeselectMinionBattleRemoval))
                    .ToList();
                if (minionBattle.CanConfirm)
                {
                    commands.Add(DirectorCommand.ConfirmMinionBattleRemovals(
                        minionBattle.ConfirmInvocation));
                }

                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.MinionBattle, commands));
            }

            if (heroRespawn != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.HeroRespawn,
                    heroRespawn.RespawnInvocations.Select(DirectorCommand.RespawnHero)
                        .Concat(new[]
                        {
                            DirectorCommand.ResolveCardWithoutRespawn(heroRespawn.DeclineInvocation)
                        })));
            }

            if (systemTransition != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.SystemTransition,
                    systemTransition.Invocations.Select(DirectorCommand.AnswerSystemTransitionStep)));
            }

            if (removalPrevention != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.RemovalPrevention,
                    removalPrevention.Payments.Select(payment =>
                            DirectorCommand.AnswerRemovalPrevention(payment.Invocation))
                        .Concat(new[]
                        {
                            DirectorCommand.AnswerRemovalPrevention(
                                removalPrevention.DeclineInvocation)
                        })));
            }

            if (turnRelease != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.TurnRelease,
                    new[] { DirectorCommand.ReleaseTurn() }));
            }

            if (actingBody != null)
            {
                standing.Add(new PendingDecisionContract(
                    PendingDecisionFamilies.ActingBody,
                    actingBody.Invocations.Select(DirectorCommand.ChooseActingBody)));
            }

            // Milestone 8.35u AMENDED this guard rather than removing it, and the distinction is the
            // whole of what it still protects. What was never legal, and still is not, is two
            // different FAMILIES standing at once - two exclusive questions with two resume locations,
            // which is the class of defect this has been catching since it was written. What became
            // legal is one family holding several open ITEMS, because rulebook p.15 asks every hero
            // that bought a level at the same time. The count below is of families; the items live
            // inside the one contract, and the seat surface is what decides which of them each player
            // may answer.
            if (standing.Count > 1)
            {
                throw new InvalidOperationException(
                    "One immutable match projection cannot publish multiple pending player decision " +
                    "families. A single family may stand on several items at once - rulebook p.15's " +
                    "simultaneous leveling does - but two families never may.");
            }

            return standing.SingleOrDefault();
        }
    }

    public sealed class MatchSessionProjection : IMatchReadModel
    {
        private readonly Lazy<string> diagnosticSnapshot;
        private readonly Lazy<IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry>> matchLog;
        private readonly AuthoritativeSnapshotCapture authoritativeCapture;

        /// <summary>
        /// Milestone 8.35w. How many of the most recent completed rounds the read model
        /// carries, in the sense <see cref="GuardsDigital.Core.Diagnostics.MatchLogProjection.RetainedEntries"/>
        /// already carries the log: recent history a player reviews, not an archive replayed from
        /// the opening move.
        /// </summary>
        /// <remarks>
        /// <para><b>Why this exists at all.</b> The completed-round history was the one
        /// match-length-linear term in a seat projection, and the seat projection is what one
        /// session frame has to carry. At a measured 1,183 bytes per completed round it walked the
        /// frame into <see cref="PrivateSessionProtocol.MaxPayloadBytes"/> and the host then threw
        /// out of <see cref="PrivateSessionFrameCodec"/> on its own honest traffic - measured on the
        /// seeded six-seat walk at <b>34 completed rounds</b>, inside the length ordinary matches
        /// reach, and terminally: the send sequence has already advanced when the throw lands, so
        /// every seat reads a gap, every resync is served down the same throwing path, and the
        /// match stalls for the whole table with nothing on screen. The bound it violated is a
        /// defensive read-side cap on what an untrusted peer can make a host allocate, so raising it
        /// would weaken that guarantee for every message kind from every peer and would still leave
        /// the term unbounded - the same freeze, at a higher round number. A growth term with no
        /// limit is not fixed by a larger limit; it is fixed by a limit on the growth.</para>
        /// <para><b>Why the READ MODEL and not the codec.</b> Capping inside the codec would leave a
        /// host's own read model and its clients' disagreeing about the same match, and would break
        /// the encode/decode round-trip equality convergence is asserted with. Windowing the read
        /// model instead means every implementation of
        /// <see cref="IMatchReadModel.CompletedRounds"/> - this one, the seat-scoped view derived
        /// from it, and the networked client that decodes it - reports the same window, so
        /// byte-for-byte convergence still holds and no surface can show a history another surface
        /// does not have.</para>
        /// <para><b>What is NOT windowed.</b> <c>RoundResolutionAuthority</c> keeps every completed
        /// round, so the rules, the restore state and the authoritative snapshot are untouched by
        /// this and the snapshot schema does not move. This is a read-model bound, not a loss of
        /// history.</para>
        /// <para><b>Why four.</b> Measured, not chosen. Over 4,200-command seeded walks on the
        /// official six-player configuration the worst projection-and-log frame decomposes into
        /// <b>35,997 bytes</b> of content that does NOT grow with match length plus <b>1,108 bytes
        /// per retained round</b>, so this window saturates the frame at <b>40,431 bytes</b>:
        /// 61.7% of the 65,536-byte ceiling, and 77.2% of the 52,395-byte deliberate-resize guard
        /// the frame fixture holds. That leaves 11,964 bytes under the guard - room for the rest of
        /// the frame to grow by a third, against the 2,835 bytes it actually grew across 8.33 to
        /// 8.35r. Unwindowed, the same walk crossed the ceiling at 34 completed rounds and reached
        /// 83,793 bytes by round 54.</para>
        /// <para><b>And why four rather than any other affordable number.</b> Four is the measured
        /// reach of the log window this projection already carries: at 256 entries the log fills
        /// exactly at the fourth completed round (16,664 bytes there, flat afterwards). Matching it
        /// means the two history surfaces a seat reviews cover the same span, rather than one of
        /// them silently deciding what "recent" means for both.</para>
        /// </remarks>
        public const int RetainedCompletedRounds = 4;

        /// <summary>
        /// The retained window itself: the LAST <see cref="RetainedCompletedRounds"/> completed
        /// rounds, in their own order.
        /// </summary>
        /// <remarks>
        /// A projection is rebuilt on every accepted command, so this deliberately never
        /// materialises the authority's whole list to throw most of it away - it indexes the tail
        /// where the source is already a list, which is what every caller in the match loop passes.
        /// That makes a refresh cost the window rather than the match, which is the second thing
        /// wrong with an unbounded history and the reason this is not a filter at the codec.
        /// </remarks>
        private static IReadOnlyList<CompletedRoundRecord> RetainRecentCompletedRounds(
            IEnumerable<CompletedRoundRecord> completedRounds)
        {
            if (completedRounds is IReadOnlyList<CompletedRoundRecord> list)
            {
                var retained = Math.Min(list.Count, RetainedCompletedRounds);
                var window = new CompletedRoundRecord[retained];
                for (var index = 0; index < retained; index++)
                {
                    window[index] = list[list.Count - retained + index];
                }

                return Array.AsReadOnly(window);
            }

            var all = completedRounds.ToArray();
            return Array.AsReadOnly(all.Length <= RetainedCompletedRounds
                ? all
                : all.Skip(all.Length - RetainedCompletedRounds).ToArray());
        }

        public MatchSessionProjection(
            long version,
            QuickGameSetupState setup,
            BoardMapDefinition selectedMap,
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> cards,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<ObstacleOccupancy> obstacles,
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            TieBreakerState tieBreakerState,
            IEnumerable<RoundTurnSlot> turnSlots,
            IEnumerable<CompletedRoundRecord> completedRounds,
            EndOfRoundStage? currentEndOfRoundStage,
            IEnumerable<MatchCommitmentProjection> commitments,
            SelectedInitiativeActor selectedActor,
            PendingInitiativeChoice pendingInitiativeChoice,
            bool isTurnStarted,
            bool isTurnEnded,
            PendingCardActionResolution pendingMovementResolution,
            PendingMovementChoice pendingMovementChoice,
            PendingAttackResolution pendingAttackResolution,
            PendingAttackTargetChoice pendingAttackTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            IEnumerable<ActionOffer> availableTurnActions,
            IEnumerable<DirectorCommand> acceptedCommands,
            IEnumerable<RoundResolutionEvent> roundEvents,
            IReadOnlyList<RuleEventLogEntry> diagnosticEvents,
            AuthoritativeSnapshotCapture authoritativeSnapshot,
            bool isHistoryAvailable,
            IEnumerable<MinionOccupancy> minions,
            IEnumerable<PlacedToken> placedTokens,
            ActionOffer? availableHoldOffer,
            IEnumerable<FastTravelOptionProjection> availableFastTravelOptions = null,
            PendingDefensePreviewProjection pendingDefensePreview = null,
            PendingMinionBattleChoice pendingMinionBattleChoice = null,
            PendingHeroRespawnChoice pendingHeroRespawnChoice = null,
            PendingLanePushRequest pendingLanePushRequest = null,
            GuardsDigital.Core.Rules.EffectResolution.PendingResolution pendingSkillResolution = null,
            PendingEffectChoice pendingEffectChoice = null,
            GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
                pendingDisplacedMinionCorrection = null,
            IEnumerable<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
                pendingEffectChoicePreview = null,
            IEnumerable<ClearOptionProjection> availableClearOptions = null,
            GuardsDigital.Core.Rules.CardResolution.PendingImmediateLanePushRequest
                pendingImmediateLanePush = null,
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
                pendingSystemTransitionStep = null,
            IEnumerable<KeyValuePair<TeamId, int>> teamLifeCounters = null,
            GuardsDigital.Core.Rules.Victory.MatchOutcome outcome = default,
            IEnumerable<HeroProgressionProjection> heroProgression = null,
            IEnumerable<GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription>
                pendingSystemTransitionDescriptions = null,
            IEnumerable<CardInstanceId> activeEffectSourceCardInstanceIds = null,
            GuardsDigital.Core.Rules.CardResolution.RemovalPreventionPrompt
                pendingRemovalPrevention = null,
            IEnumerable<MarkerAttachment> heldMarkers = null,
            IEnumerable<AttackTargetDefensePreviewProjection>
                pendingAttackTargetDefensePreviews = null,
            GuardsDigital.Core.Cards.PendingTurnRelease pendingTurnRelease = null,
            int? remainingWaveCounters = null,
            IEnumerable<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry> scoreboard = null,
            PendingActingBodyChoice pendingActingBodyChoice = null,
            IEnumerable<HeroActingBody> actingBodies = null)
        {
            Version = version;
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            SelectedMap = selectedMap ?? throw new ArgumentNullException(nameof(selectedMap));
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            Definitions = Array.AsReadOnly((definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            Cards = Array.AsReadOnly((cards ?? throw new ArgumentNullException(nameof(cards))).Select(CloneCard).ToArray());
            Heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes))).ToArray());
            Obstacles = Array.AsReadOnly((obstacles ?? throw new ArgumentNullException(nameof(obstacles))).ToArray());
            Minions = Array.AsReadOnly((minions ?? throw new ArgumentNullException(nameof(minions))).ToArray());
            PlacedTokens = Array.AsReadOnly((placedTokens ??
                throw new ArgumentNullException(nameof(placedTokens))).ToArray());
            HeldMarkers = Array.AsReadOnly((heldMarkers ??
                Enumerable.Empty<MarkerAttachment>()).ToArray());
            if (HeldMarkers.Any(marker => marker == null) ||
                HeldMarkers.Select(marker => marker.Id).Distinct().Count() != HeldMarkers.Count)
            {
                throw new ArgumentException(
                    "Held markers must be non-null and distinct by instance ID.", nameof(heldMarkers));
            }

            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            TieBreakerState = tieBreakerState;
            TurnSlots = Array.AsReadOnly((turnSlots ?? throw new ArgumentNullException(nameof(turnSlots))).ToArray());
            CompletedRounds = RetainRecentCompletedRounds(
                completedRounds ?? throw new ArgumentNullException(nameof(completedRounds)));
            CurrentEndOfRoundStage = currentEndOfRoundStage;
            Commitments = Array.AsReadOnly((commitments ?? throw new ArgumentNullException(nameof(commitments))).ToArray());
            SelectedActor = selectedActor;
            PendingInitiativeChoice = pendingInitiativeChoice;
            IsTurnStarted = isTurnStarted;
            IsTurnEnded = isTurnEnded;
            PendingMovementResolution = pendingMovementResolution;
            PendingMovementChoice = pendingMovementChoice;
            PendingAttackResolution = pendingAttackResolution;
            PendingAttackTargetChoice = pendingAttackTargetChoice;
            PendingDefenseChoice = pendingDefenseChoice;
            PendingSkillResolution = pendingSkillResolution;
            PendingEffectChoice = pendingEffectChoice;
            PendingRemovalPrevention = pendingRemovalPrevention;
            AvailableTurnActions = Array.AsReadOnly((availableTurnActions ?? throw new ArgumentNullException(nameof(availableTurnActions))).ToArray());
            // These three are the growing sequences a refresh hands over: the accepted-command
            // history and the whole accumulated rule-event log grow for the match, and the current
            // round's events grow until the round boundary. Copying any of them on every refresh is
            // a per-command cost that grows with session length - the surviving half of finding
            // `F613-08` for the log, and `RISK-PERF-001`'s measured growth term for the other two,
            // closed at milestone 7.9b. Each arrives as a snapshot that cannot change, and copying
            // one of those defends against nothing. The copy is kept for every other input, because
            // this constructor is public and a projection that shares a caller's mutable list is not
            // immutable.
            AcceptedCommands = ImmutableOrCopied(acceptedCommands, nameof(acceptedCommands));
            RoundEvents = ImmutableOrCopied(roundEvents, nameof(roundEvents));
            DiagnosticEvents = ImmutableOrCopied(diagnosticEvents, nameof(diagnosticEvents));
            authoritativeCapture = authoritativeSnapshot ?? throw new ArgumentNullException(nameof(authoritativeSnapshot));
            IsHistoryAvailable = isHistoryAvailable;
            diagnosticSnapshot = new Lazy<string>(
                () => RuleEventLogSnapshotEnvelope.Serialize(AuthoritativeSnapshot, DiagnosticEvents),
                LazyThreadSafetyMode.None);
            matchLog = new Lazy<IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry>>(
                () => GuardsDigital.Core.Diagnostics.MatchLogProjection.Derive(DiagnosticEvents),
                LazyThreadSafetyMode.None);
            AvailableFastTravelOptions = Array.AsReadOnly((availableFastTravelOptions ??
                Enumerable.Empty<FastTravelOptionProjection>()).ToArray());
            PendingDefensePreview = pendingDefensePreview;
            PendingEffectChoicePreview = Array.AsReadOnly((pendingEffectChoicePreview ??
                Enumerable.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>())
                .ToArray());
            var attackTargetDefensePreviews = (pendingAttackTargetDefensePreviews ??
                Enumerable.Empty<AttackTargetDefensePreviewProjection>()).ToArray();
            // Milestone 9.21a: keyed by (hero, body), from the one derivation the session builds from.
            var expectedAttackTargetPreviewKeys = AttackTargetDefensePreviewProjection.ExpectedKeys(
                PendingAttackTargetChoice,
                PendingEffectChoice,
                PendingEffectChoicePreview,
                Heroes);
            if (attackTargetDefensePreviews.Any(value => value == null) ||
                !attackTargetDefensePreviews
                    .Select(value => new KeyValuePair<HeroId, int>(value.HeroId, value.HeroBody))
                    .SequenceEqual(expectedAttackTargetPreviewKeys))
            {
                throw new ArgumentException(
                    "Attack-target Defense previews must match the ordered legal hero targets " +
                    "from ordinary Attack and typed Attack-intent effect choices.",
                    nameof(pendingAttackTargetDefensePreviews));
            }
            PendingAttackTargetDefensePreviews = Array.AsReadOnly(attackTargetDefensePreviews);
            PendingMinionBattleChoice = pendingMinionBattleChoice;
            PendingHeroRespawnChoice = pendingHeroRespawnChoice;
            PendingActingBodyChoice = pendingActingBodyChoice;
            ActingBodies = Array.AsReadOnly((actingBodies ?? Enumerable.Empty<HeroActingBody>())
                .OrderBy(value => value.HeroId.Value, StringComparer.Ordinal)
                .ToArray());
            PendingLanePushRequest = pendingLanePushRequest;
            PendingImmediateLanePush = pendingImmediateLanePush;
            PendingSystemTransitionStep = pendingSystemTransitionStep;
            PendingDisplacedMinionCorrection = pendingDisplacedMinionCorrection;
            AvailableHoldOffer = availableHoldOffer;
            AvailableClearOptions = Array.AsReadOnly((availableClearOptions ??
                Enumerable.Empty<ClearOptionProjection>()).ToArray());

            // Milestone 7.4. Rulebook p.13's Life counters as they now stand, rather than the count the
            // setup states. Before this milestone the two were always the same number and the HUD read
            // the setup's; a Defeat now spends one, so a reader that kept reading the setup would show a
            // pile that never empties. An empty list is the honest absence for a composition with no
            // round authority behind it rather than a substituted full pile.
            TeamLifeCounters = Array.AsReadOnly((teamLifeCounters ??
                Enumerable.Empty<KeyValuePair<TeamId, int>>()).ToArray());

            // Milestone 8.35z60. p.8's live Wave-counter pool, mirroring TeamLifeCounters'
            // own reasoning just above: a composition built with no round authority behind it has
            // never pushed anything off the pool, so the setup's own starting total is the honest
            // reading rather than a substituted zero.
            RemainingWaveCounters = remainingWaveCounters ?? Setup.WaveCount;
            Outcome = outcome;
            HeroProgression = Array.AsReadOnly((heroProgression ??
                Enumerable.Empty<HeroProgressionProjection>()).ToArray());
            if (HeroProgression.Select(entry => entry.HeroId).Distinct().Count() != HeroProgression.Count)
            {
                throw new ArgumentException(
                    "One hero carries at most one projected progression.", nameof(heroProgression));
            }

            // Milestone 8.53. The victory screen's board, on the terms the progression beside it is
            // published: taken from the authority that accumulates it rather than counted here, and
            // empty for a composition with no round authority behind it, which is the same honest
            // absence TeamLifeCounters reports.
            Scoreboard = Array.AsReadOnly((scoreboard ??
                Enumerable.Empty<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry>()).ToArray());
            if (Scoreboard.Any(entry => entry == null) ||
                Scoreboard.Select(entry => entry.HeroId).Distinct().Count() != Scoreboard.Count)
            {
                throw new ArgumentException(
                    "One hero carries at most one scoreboard line.", nameof(scoreboard));
            }

            ActiveEffectSourceCardInstanceIds = Array.AsReadOnly((activeEffectSourceCardInstanceIds ??
                Enumerable.Empty<CardInstanceId>()).ToArray());
            if (ActiveEffectSourceCardInstanceIds.Distinct().Count() !=
                ActiveEffectSourceCardInstanceIds.Count)
            {
                throw new ArgumentException(
                    "A source card appears once however many of its Active effects are live.",
                    nameof(activeEffectSourceCardInstanceIds));
            }

            // Milestone 8.35u. One description per open item, because rulebook p.15 now asks every
            // hero that bought a level at once and each of them is asking a different question. The
            // list is in the rules' own order and the first of it is what the Director's own single
            // surface reads, which is the serialization that page always had at one controller.
            PendingSystemTransitionDescriptions = Array.AsReadOnly(
                (pendingSystemTransitionDescriptions
                    ?? Array.Empty<GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription>())
                .ToArray());
            if (PendingSystemTransitionDescriptions.Any(description =>
                    !ReferenceEquals(description.PendingStep, pendingSystemTransitionStep)))
            {
                // A description of some other pause would be a second answer to what the match is
                // waiting on, and the surface built from it would offer options the authority refuses.
                throw new ArgumentException(
                    "A projected transition description describes the pause this projection carries.",
                    nameof(pendingSystemTransitionDescriptions));
            }

            if (PendingSystemTransitionDescriptions.Select(description => description.ItemId)
                    .Distinct().Count() != PendingSystemTransitionDescriptions.Count)
            {
                throw new ArgumentException(
                    "A projection describes each open item of its pause at most once.",
                    nameof(pendingSystemTransitionDescriptions));
            }

            // Milestone 8.35g. The release yields to EVERY other standing decision, and this is the
            // only place that can arbitrate it: the turn authority withholds it for the three cases
            // it can see - a game ended mid-turn, p.8's mid-turn Lane Push, a stopped triggered
            // effect - but it cannot see the round-level questions, nor a decision the finished
            // resolution raised on ANOTHER hero's behalf. Brogan's Shield discard standing after an
            // enemy Ultimate's Defeat is exactly that: neither the discard nor the completed
            // resolution is wrong, and publishing both is what the single-decision invariant below
            // refuses. So the release waits until it is the only thing left to ask, which is also
            // what its own contract says it is - the last question of a turn, never a rival to one.
            PendingTurnRelease =
                PendingInitiativeChoice != null ||
                PendingMovementChoice != null ||
                PendingAttackTargetChoice != null ||
                PendingDefenseChoice != null ||
                PendingEffectChoice != null ||
                PendingDisplacedMinionCorrection != null ||
                PendingMinionBattleChoice != null ||
                PendingHeroRespawnChoice != null ||
                PendingActingBodyChoice != null ||
                PendingSystemTransitionStep != null ||
                PendingRemovalPrevention != null
                    ? null
                    : pendingTurnRelease;

            DecisionContract = PendingDecisionContract.Create(
                PendingInitiativeChoice,
                PendingMovementChoice,
                PendingAttackTargetChoice,
                PendingDefenseChoice,
                PendingEffectChoice,
                PendingDisplacedMinionCorrection,
                PendingMinionBattleChoice,
                PendingHeroRespawnChoice,
                PendingSystemTransitionStep,
                PendingRemovalPrevention,
                PendingTurnRelease,
                PendingActingBodyChoice);

            if (outcome.HasEnded != (phase == RoundResolutionPhase.GameEnded))
            {
                throw new ArgumentException(
                    "A projected match outcome agrees with the projected phase about whether the " +
                    "match has ended.",
                    nameof(outcome));
            }
            if (AvailableClearOptions.Select(option => option.ReplacedAttackActionDefinitionId)
                    .Distinct().Count() != AvailableClearOptions.Count)
            {
                throw new ArgumentException(
                    "One replaceable Attack carries at most one Clear option.",
                    nameof(availableClearOptions));
            }
        }

        public long Version { get; }
        public bool IsInputEnabled => true;

        /// <summary>
        /// Milestone 8.35f. Always false: the seat-scoped undo control belongs to a seat, and the
        /// Director's own Undo is its drawer's rather than this flag's.
        /// </summary>
        public bool IsSeatUndoAvailable => false;

        /// <summary>
        /// Milestone 8.35r. Always <see cref="MatchUndoMarker.None"/>, for the reason
        /// <see cref="IsSeatUndoAvailable"/> is always false: the marker is about the SEAT control,
        /// and the Director's own Undo belongs to no hero.
        /// </summary>
        public MatchUndoMarker UndoMarker => MatchUndoMarker.None;

        /// <summary>
        /// Milestone 8.35r. The Director's full-trust view of the standing decision's owner, read
        /// off whichever authoritative pending object the contract names.
        /// </summary>
        /// <remarks>
        /// This restates <c>SeatProjectionFactory.OwnerOfStandingDecision</c> rather than answering
        /// the question a second way: the seat composition already reads its own owner through that
        /// method, so a Director and a seat that disagreed about who is awaited would be a
        /// contradiction between two readings of one authority. There is one reading.
        /// </remarks>
        public PendingDecisionOwner DecisionOwner => SeatProjectionFactory.OwnerOfStandingDecision(this);

        /// <summary>
        /// Milestone 8.35r. The seat-public match log, derived from the rule-event log this
        /// projection already holds.
        /// </summary>
        /// <remarks>
        /// Deferred like <see cref="DiagnosticSnapshot"/> and for the same reason: nothing on the
        /// accepted-command path reads it, and a reader pays the derivation at most once per
        /// projection. It is computed from <see cref="DiagnosticEvents"/> - this projection's own
        /// immutable copy - so a caller holding an older projection reads the log as it was then,
        /// which is what makes a rewind observable as a truncation rather than as a mutation.
        /// </remarks>
        public IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog => matchLog.Value;

        /// <summary>
        /// Milestone 9.30b. The countable shape of every hero's hidden cards - hand, deck and
        /// spellbook - naming none of them. Table-public, so every seat is handed this same list
        /// (<see cref="SeatProjectionFactory"/>) and a network seat reads it off the wire.
        /// </summary>
        public IReadOnlyList<SeatHandSummary> HandSummaries =>
            handSummaries ?? (handSummaries = SeatHandSummary.Derive(Participants, Cards, Definitions));

        private IReadOnlyList<SeatHandSummary> handSummaries;

        public QuickGameSetupState Setup { get; }
        public BoardMapDefinition SelectedMap { get; }
        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<ObstacleOccupancy> Obstacles { get; }
        public IReadOnlyList<MinionOccupancy> Minions { get; }

        /// <summary>
        /// Milestone 6.2d: the tokens standing on the board, as the board authority holds them.
        ///
        /// It supersedes milestone 6.2's decision that presentation receives no token projection. That
        /// decision was correct while nothing consumed one; 6.6a printed a Replace and a token-only
        /// Remove onto a configured card, and 6.2b had already assigned this to the first presentation
        /// milestone that needed a token view. What is handed over is the authoritative instance, not a
        /// summary of it: the stable identity, the coordinate, and the trusted <c>TokenDefinition</c>
        /// object the catalog supplied. That last part is what keeps presentation free of any component
        /// catalog reference - a display name is read off the instance it belongs to, and nothing here
        /// is derived, so no rule can come to depend on a reading only this projection performs.
        /// </summary>
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }

        /// <summary>
        /// Milestone 8.23a: the markers currently attached to heroes, as the register holds them.
        ///
        /// <para>The marker twin of <see cref="PlacedTokens"/> above, and handed over on exactly the
        /// same terms: the authoritative <see cref="MarkerAttachment"/> instance, carrying the stable
        /// identity, the holder, and the trusted <c>MarkerDefinition</c> object the catalog supplied.
        /// A surface therefore reads the supply it came from, its printed strength and the channels it
        /// changes off the definition that states them, and needs no component catalog of its own.</para>
        ///
        /// <para><b>There is no coordinate here and there must never be one.</b> Rulebook p.18 keeps
        /// markers off the board - "Unlike tokens, markers are usually not placed on the board" - which
        /// <see cref="MarkerAttachment"/> makes structural by having no field one could occupy a space
        /// with. The holder is the whole of a marker's position.</para>
        ///
        /// <para>The list is the register's own, so the attachment's lifetime is the reading's: it
        /// appears the moment a marker is given and is gone the moment p.8's end of round or p.13's
        /// holder Defeat returns it. Nothing downstream keeps a count, which is what lets restore,
        /// replay and retained-prefix Undo redraw it with no serialized field of their own - every one
        /// of those paths already reconstructs the register.</para>
        /// </summary>
        public IReadOnlyList<MarkerAttachment> HeldMarkers { get; }

        /// <summary>
        /// The markers this hero is currently holding, in the register's own order.
        ///
        /// <para>Milestone 8.23a. The holder is the affected object - a marker property modifies the
        /// hero who <em>has</em> it, where a token property modifies its controller - so this is the
        /// question every surface drawing a marker asks, stated once here rather than as a filter each
        /// caller writes.</para>
        /// </summary>
        public IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
            HeldMarkers.Where(marker => marker.HolderHeroId.Equals(holderHeroId));

        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public TieBreakerState TieBreakerState { get; }
        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }
        public EndOfRoundStage? CurrentEndOfRoundStage { get; }
        public IReadOnlyList<MatchCommitmentProjection> Commitments { get; }
        public SelectedInitiativeActor SelectedActor { get; }
        public PendingInitiativeChoice PendingInitiativeChoice { get; }
        public bool IsTurnStarted { get; }
        public bool IsTurnEnded { get; }
        public PendingCardActionResolution PendingMovementResolution { get; }
        public bool HasPendingMovementResolution => PendingMovementResolution != null;
        public PendingMovementChoice PendingMovementChoice { get; }
        public PendingAttackResolution PendingAttackResolution { get; }
        public bool HasPendingAttackResolution => PendingAttackResolution != null;
        public PendingAttackTargetChoice PendingAttackTargetChoice { get; }
        public PendingDefenseResponseChoice PendingDefenseChoice { get; }

        /// <summary>
        /// The one genuine pending player decision standing in this projection, with the exact ordered
        /// commands its real surfaces may submit, or <c>null</c> when no such decision stands. Companion
        /// resolution envelopes and non-pending offers remain outside this algebra.
        /// </summary>
        public PendingDecisionContract DecisionContract { get; }

        /// <summary>
        /// The Skill effect currently resolving, and the decision it is waiting on. Presentation reads
        /// the projected invocations off the choice; it never derives Skill legality itself.
        /// </summary>
        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution { get; }
        public bool HasPendingSkillResolution => PendingSkillResolution != null;

        /// <summary>
        /// Milestone 8.58. The resolving card, read off whichever card-action frame is running.
        ///
        /// <para>Three frames can carry a printed text that asks a question, and a decision window is
        /// opened by any of them: a Skill's own body, an Attack's before/after text (rulebook p.14's
        /// riders) and a Movement's. The order is the order the authority itself resolves them in, and
        /// only one stands at a time.</para>
        ///
        /// <para><b>And a fourth that publishes no frame at all.</b> p.14's own before/after card text
        /// runs on the attack-defense backend's effect session, which the projection has never
        /// published - measured on 2026-09-19 with Wasp's Shock, whose "After the attack" discard
        /// chooser stands with all three frames null and a resolution id reading
        /// <c>card-text-red-wasp-shock-primary-attack-AfterTheAttack</c>. Rather than parse that id,
        /// the card in play answers it: while an effect choice stands, the acting hero's own selected
        /// card IS the card whose printed text raised it - the same reading the tray makes to draw
        /// that card at full strength, from the same public facts, on a host and on a seat alike.</para>
        /// </summary>
        public CardInstanceId ResolvingSourceCardInstanceId =>
            PendingSkillResolution?.SourceCardInstanceId ??
            PendingAttackResolution?.SourceCardInstanceId ??
            PendingMovementResolution?.SourceCardInstanceId ??
            (PendingEffectChoice != null && SelectedActor.IsDefined
                ? SelectedActor.CardInstanceId
                : default);

        public PendingEffectChoice PendingEffectChoice { get; }

        /// <summary>
        /// Milestone 8.21. The standing removal-prevention decision, or <c>null</c> - rulebook p.13's
        /// removal stage stopped while the hero whose source is offering decides whether to pay for it.
        ///
        /// <para>Milestone 8.19c1 built that decision as rules only and recorded that no configured
        /// card could open it; Brogan's Shield, Bolster and Fortify are the first that can. It has to
        /// be here rather than reachable only through the session, because <c>HasPendingResolution</c>
        /// refuses every other turn command while it stands: a client confined to this projection would
        /// otherwise see a board that had stopped accepting input with nothing on it to explain
        /// why.</para>
        ///
        /// <para>Presentation reads the projected payments and the decline off the prompt and submits
        /// one of them; it never asks which cards could pay, and an option the prompt did not project
        /// cannot be submitted at all.</para>
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.RemovalPreventionPrompt
            PendingRemovalPrevention { get; }

        /// <summary>
        /// Milestone 6.5c: what each space-valued candidate of that choice means on the board right
        /// now - the object standing there, and where a consuming Push would leave it. Empty unless
        /// such a choice is pending. It is the effect-frame counterpart of
        /// <see cref="PendingDefensePreview"/>: an authority-computed reading of a decision the player
        /// has not made yet, so presentation can show consequences without deriving any rule.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
            PendingEffectChoicePreview { get; }

        /// <summary>
        /// Milestone 6.4a's paused after-action correction, or <c>null</c>. Presentation reads the
        /// projected invocations off the choice; it never derives paths, distances, Battle-Zone
        /// membership, occupancy, or Tie-Breaker ordering itself.
        /// </summary>
        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection { get; }

        public IReadOnlyList<ActionOffer> AvailableTurnActions { get; }
        public IReadOnlyList<DirectorCommand> AcceptedCommands { get; }
        public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }
        public IReadOnlyList<RuleEventLogEntry> DiagnosticEvents { get; }

        /// <summary>
        /// The authoritative payload describing this projection's state, encoded on first read and
        /// then held.
        /// </summary>
        /// <remarks>
        /// Milestone 7.9b. Read from the capture taken when this projection was built - an
        /// immutable value, exactly like the copies <see cref="DiagnosticSnapshot"/> defers over -
        /// never from the live authorities, so a caller holding an older projection still reads the
        /// state as it was then. Deferring the encode is what took the payload assembly off the
        /// accepted-command path: nothing on that path reads this, and a reader that does pays it
        /// at most once per projection, which is the same count the eager encode paid.
        /// </remarks>
        public string AuthoritativeSnapshot => authoritativeCapture.Payload;

        /// <summary>
        /// The authoritative snapshot plus the ordered rule-event log, encoded on first read and
        /// then held.
        /// </summary>
        /// <remarks>
        /// Computed from <see cref="AuthoritativeSnapshot"/> and <see cref="DiagnosticEvents"/>,
        /// both of which this projection already holds as immutable copies. That matters: a lazy
        /// value reading the live authorities would hand a caller holding an older projection the
        /// state as it is now, and fixtures compare exactly that way. Deferring it is what stopped
        /// every command re-encoding the whole accumulated log - finding `F613-08`'s growth with
        /// session length - because nothing on the command path reads this.
        /// </remarks>
        public string DiagnosticSnapshot => diagnosticSnapshot.Value;

        /// <summary>
        /// Whether this session can still record accepted commands. False once a snapshot has been
        /// loaded, which permanently disables history.
        /// </summary>
        /// <remarks>
        /// Distinct from an empty <see cref="AcceptedCommands"/>. A fresh session also has nothing
        /// to undo, but its history is available and the next accepted command will appear there;
        /// after a snapshot load no command ever will. Finding `F613-10` is that the two were
        /// indistinguishable to everything downstream, including the Undo control.
        /// </remarks>
        public bool IsHistoryAvailable { get; }
        public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions { get; }

        /// <summary>
        /// Milestone 6.3a: the replaceable Attacks the current actor could take a Clear instead of,
        /// each carrying the tokens it would offer and the command for every confirmable subset.
        ///
        /// Empty whenever the current card prints no available replaceable Attack, which is what makes
        /// the absence of a Clear control a projected fact rather than a presentation decision. An
        /// option with no adjacent token is still an option: p.13 permits clearing none, so a player
        /// may take the replacement and resolve the card either way.
        /// </summary>
        public IReadOnlyList<ClearOptionProjection> AvailableClearOptions { get; }

        /// <summary>
        /// Milestone 8.7. p.12's Hold, offered or absent, decided once by the authority.
        ///
        /// <para>The presentation used to ask the offer query itself, which meant the control beside a
        /// turn and the command that ends it were two derivations of one answer - the shape the 7.9a
        /// review found as F-PRES-01, a live control drawn beside a refusal. It moved here when the
        /// offer began consulting the rules in effect: a restriction naming Hold is authoritative
        /// state, and a projection is where authoritative state is published.</para>
        /// </summary>
        public ActionOffer? AvailableHoldOffer { get; }
        public PendingDefensePreviewProjection PendingDefensePreview { get; }
        public IReadOnlyList<AttackTargetDefensePreviewProjection>
            PendingAttackTargetDefensePreviews { get; }
        public PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        public PendingHeroRespawnChoice PendingHeroRespawnChoice { get; }

        /// <summary>
        /// Milestone 9.21b. "Which of you acts" (RZ-Q4), standing before the action choice of a hero
        /// on several spaces. A seat carries it only when it owns the decision; over the wire it is
        /// re-derived from the <see cref="DecisionContract"/>, whose answers name the hero, the card and
        /// each body, and from <see cref="Heroes"/>, which hold the bodies' spaces - no row of its own.
        /// </summary>
        public PendingActingBodyChoice PendingActingBodyChoice { get; }

        /// <summary>
        /// Milestone 9.21i. The settled acting body of each many-bodied hero, read from the board
        /// authority that settles it (RZ-Q4); what the board's acting cue is drawn from.
        /// </summary>
        public IReadOnlyList<HeroActingBody> ActingBodies { get; }

        /// <summary>
        /// Milestone 8.35g. The finished turn waiting for its own player's release, or <c>null</c>
        /// when the match is not holding one.
        /// </summary>
        public GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease { get; }

        /// <summary>
        /// Milestone 7.4: every team's remaining p.13 Life counters, ordered by team id. Empty in a
        /// composition that has no round authority to read them from.
        /// </summary>
        public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters { get; }

        /// <summary>
        /// Milestone 8.35z60: rulebook p.8's ONE SHARED Wave-counter pool as it now stands,
        /// read straight off the round authority's <c>LanePushTransitionAuthority.RemainingWaveCounters</c>
        /// rather than <see cref="Setup"/>'s starting total. See <see cref="IMatchReadModel.RemainingWaveCounters"/>.
        /// </summary>
        public int RemainingWaveCounters { get; }

        /// <summary>
        /// Milestone 7.7: who won this match and which of the rulebook's three victory sentences ended
        /// it, or <see cref="GuardsDigital.Core.Rules.Victory.MatchOutcome.Unfinished"/> while it is
        /// still being played.
        ///
        /// Read from the round authority, which derives it, rather than accumulated here from the event
        /// stream: a restored match has no event stream to accumulate from and must project the same
        /// answer, which is the durability rule's "projection reconstruction from restored and replayed
        /// authority" applied to the one fact a finished match is read for.
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome { get; }

        /// <summary>
        /// Milestone 7.8a: every hero's Level, coins and items, ordered by hero id. Empty for a
        /// composition with no round authority behind it, which is the same honest absence
        /// <see cref="TeamLifeCounters"/> reports.
        /// </summary>
        public IReadOnlyList<HeroProgressionProjection> HeroProgression { get; }

        /// <summary>
        /// Milestone 8.53: the match scoreboard the victory screen prints, ordered by hero id and
        /// taken whole from <c>RoundResolutionAuthority.Scoreboard</c>. Empty for a composition with
        /// no round authority behind it, the same honest absence
        /// <see cref="HeroProgression"/> reports.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry> Scoreboard { get; }

        /// <summary>
        /// Milestone 8.5b: every card instance that is the source of at least one Active-effect
        /// instance currently in <see cref="GuardsDigital.Core.Timing.ActiveEffectPhase.Active"/>,
        /// ordered by instance id.
        ///
        /// <para>It is a derived reading of the authoritative lifecycle and nothing else. A dormant
        /// instance is not here because it is not in effect yet, and an ended one is not because it is
        /// over; a card whose definition <i>could</i> create an effect is not here either, because the
        /// question is about live instances rather than about content. One card appears once however
        /// many of its instances are live, which is what makes it answerable as a card-level fact.</para>
        ///
        /// <para><b>Why the application answers this and not presentation.</b> A view that wanted to
        /// know which cards are "in effect" could only get there by walking aggregate state, by
        /// inferring from a definition, or by replaying the lifecycle events it happened to see - and
        /// the third would give a different answer after a restore, a replay, or a retained-prefix
        /// Undo than during live play. Deriving it here means every one of those paths produces the
        /// same list from the same lifecycle, with nothing new on the wire.</para>
        /// </summary>
        public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds { get; }

        /// <summary>Director mode answers team choices directly and therefore has no agreement view.</summary>
        public TeamAgreementProjection TeamAgreement => null;

        /// <summary>Whether this card instance currently sources at least one live Active effect.</summary>
        public bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
            ActiveEffectSourceCardInstanceIds.Contains(cardInstanceId);

        /// <summary>
        /// Milestone 7.8a: what <see cref="PendingSystemTransitionStep"/> is actually asking, described
        /// by the rules that paused it, or <c>null</c> when nothing is paused or those rules describe
        /// nothing.
        ///
        /// It carries the pause it describes rather than a copy of its identities, so a reader answering
        /// from a description is answering the step the projection carries and cannot drift onto another
        /// one. Every option it names is one of that step's own, in that step's order.
        /// </summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
            PendingSystemTransitionDescription => PendingSystemTransitionDescriptions.Count == 0
                ? null
                : PendingSystemTransitionDescriptions[0];

        /// <summary>
        /// Milestone 8.35u. Every open item of the paused step, described - one per hero when
        /// rulebook p.15 is levelling them all at once, and exactly one for every other pause.
        ///
        /// <para>A seat is projected the one that is its own; this whole list reaches only the
        /// Director's own full-trust read model, because an option list names cards in the hand of
        /// the hero it belongs to.</para>
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription>
            PendingSystemTransitionDescriptions { get; }

        public PendingLanePushRequest PendingLanePushRequest { get; }

        /// <summary>
        /// Milestone 6.10a: where a multi-step system transition is stopped, or <c>null</c>. This is
        /// authoritative state reaching a reader, not a presentation surface - the milestone's non-goals
        /// forbid production presentation, and nothing draws this. It is projected because a paused
        /// transition is a state a caller has to be able to see to answer, exactly as the Minion-Battle
        /// choice beside it is.
        /// </summary>
        public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            PendingSystemTransitionStep { get; }

        /// <summary>
        /// Milestone 6.9: rulebook p.8's mid-turn Lane-Push request, or <c>null</c>. Projected beside its
        /// end-of-round sibling so a reader can tell a turn stopped at the immediate checkpoint from a
        /// turn stopped for any other reason. Nothing draws it - this milestone adds no control, because
        /// there is no decision for a player to make until milestones 6.10 and 6.11 resolve the push.
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.PendingImmediateLanePushRequest
            PendingImmediateLanePush { get; }

        public bool IsTurnActive =>
            Phase == RoundResolutionPhase.ResolvingTurn && IsTurnStarted && !IsTurnEnded;

        public MatchCommitmentProjection GetCommitment(HeroId heroId)
        {
            return Commitments.Single(value => value.HeroId.Equals(heroId));
        }

        /// <summary>
        /// A read-only copy of one authoritative card, carrying every fact about that card the
        /// authority holds.
        /// </summary>
        /// <remarks>
        /// <para><b>Milestone 8.71b fixed a shipped defect here, and it was 8.71a's.</b> This clone
        /// dropped <see cref="CardInstance.InitiativeOverride"/>, so a printed Initiative rewritten
        /// by Hanu the Trickster's Hurry up! reached the authority, the turn order and the snapshot
        /// - and reached NOTHING that reads the projection. Three readers were silently answering
        /// about a board that did not exist: <c>NetworkSeatProjection.WriteCards</c> wrote a
        /// rewritten-Initiative block that was always empty, so no client ever learned the number
        /// its own rail was about to be ordered by; <c>HeroOverviewPresentation</c> and
        /// <c>KineticCuePresenter</c> read the un-rewritten printed icon through the same query
        /// that would have returned the rewritten one; and <c>MatchHost.OpposingFingerprint</c>'s
        /// own 8.71a term - the interaction wall's whole answer to a Hurry up! on an enemy -
        /// compared a field that was never set, so the wall it was added to close stayed open.</para>
        /// <para>It is fixed by carrying the fact rather than by teaching each reader to fetch it,
        /// because the defect is that a COPY of a card was not a copy of that card. Every card fact
        /// this type ever gains belongs on this line for the same reason.</para>
        /// </remarks>
        private static CardInstance CloneCard(CardInstance card)
        {
            return new CardInstance(
                card.Id, card.DefinitionReference, card.OwnerId, card.State,
                card.InitiativeOverride);
        }

        /// <summary>
        /// The input itself when it is a snapshot that cannot change, and an immutable copy of it
        /// otherwise. Only the append-only logs' snapshots skip the copy; anything else keeps it,
        /// because sharing a caller's mutable list would make this projection mutable with it.
        /// </summary>
        private static IReadOnlyList<TItem> ImmutableOrCopied<TItem>(
            IEnumerable<TItem> items, string name)
        {
            if (items == null) throw new ArgumentNullException(name);
            return items is IImmutableEventSnapshot && items is IReadOnlyList<TItem> snapshot
                ? snapshot
                : Array.AsReadOnly(items.ToArray());
        }
    }
}
