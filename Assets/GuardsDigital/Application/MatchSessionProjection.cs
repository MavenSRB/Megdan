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
        {
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
            MinionContributions = Array.AsReadOnly(contributions);
        }

        public HeroId HeroId { get; }
        public IReadOnlyList<ValueContributionProjection> MinionContributions { get; }
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

    /// <summary>The bounded production identities for the eleven configured pending-decision families.</summary>
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
            GuardsDigital.Core.Cards.PendingTurnRelease turnRelease)
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
                    attackTarget.LegalTargetHeroIds.Select(heroId =>
                            DirectorCommand.ChooseAttackTarget(
                                attackTarget.ResolutionId, attackTarget.ChoiceId, heroId))
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

            if (standing.Count > 1)
            {
                throw new InvalidOperationException(
                    "One immutable match projection cannot publish multiple pending player decisions.");
            }

            return standing.SingleOrDefault();
        }
    }

    public sealed class MatchSessionProjection : IMatchReadModel
    {
        private readonly Lazy<string> diagnosticSnapshot;
        private readonly AuthoritativeSnapshotCapture authoritativeCapture;

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
            GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
                pendingSystemTransitionDescription = null,
            IEnumerable<CardInstanceId> activeEffectSourceCardInstanceIds = null,
            GuardsDigital.Core.Rules.CardResolution.RemovalPreventionPrompt
                pendingRemovalPrevention = null,
            IEnumerable<MarkerAttachment> heldMarkers = null,
            IEnumerable<AttackTargetDefensePreviewProjection>
                pendingAttackTargetDefensePreviews = null,
            GuardsDigital.Core.Cards.PendingTurnRelease pendingTurnRelease = null)
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
            CompletedRounds = Array.AsReadOnly((completedRounds ?? throw new ArgumentNullException(nameof(completedRounds))).ToArray());
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
            AvailableFastTravelOptions = Array.AsReadOnly((availableFastTravelOptions ??
                Enumerable.Empty<FastTravelOptionProjection>()).ToArray());
            PendingDefensePreview = pendingDefensePreview;
            PendingEffectChoicePreview = Array.AsReadOnly((pendingEffectChoicePreview ??
                Enumerable.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>())
                .ToArray());
            var attackTargetDefensePreviews = (pendingAttackTargetDefensePreviews ??
                Enumerable.Empty<AttackTargetDefensePreviewProjection>()).ToArray();
            var expectedAttackTargetPreviewHeroIds = (PendingAttackTargetChoice == null
                    ? Enumerable.Empty<HeroId>()
                    : PendingAttackTargetChoice.LegalTargetHeroIds)
                .Concat(PendingEffectChoice == null
                    ? Enumerable.Empty<HeroId>()
                    : PendingEffectChoicePreview
                        .Where(preview =>
                            preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Attack &&
                            preview.Occupant.Kind == BoardObjectKind.Hero &&
                            PendingEffectChoice.SelectInvocations.Any(invocation =>
                                invocation.Candidate.Equals(
                                    EffectChoiceCandidate.Value_(preview.CandidateValue))))
                        .Select(preview => preview.Occupant.HeroId))
                .Distinct()
                .ToArray();
            if (attackTargetDefensePreviews.Any(value => value == null) ||
                attackTargetDefensePreviews.Select(value => value.HeroId).Distinct().Count() !=
                    attackTargetDefensePreviews.Length ||
                !attackTargetDefensePreviews.Select(value => value.HeroId).SequenceEqual(
                    expectedAttackTargetPreviewHeroIds))
            {
                throw new ArgumentException(
                    "Attack-target Defense previews must match the ordered legal hero targets " +
                    "from ordinary Attack and typed Attack-intent effect choices.",
                    nameof(pendingAttackTargetDefensePreviews));
            }
            PendingAttackTargetDefensePreviews = Array.AsReadOnly(attackTargetDefensePreviews);
            PendingMinionBattleChoice = pendingMinionBattleChoice;
            PendingHeroRespawnChoice = pendingHeroRespawnChoice;
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
            Outcome = outcome;
            HeroProgression = Array.AsReadOnly((heroProgression ??
                Enumerable.Empty<HeroProgressionProjection>()).ToArray());
            if (HeroProgression.Select(entry => entry.HeroId).Distinct().Count() != HeroProgression.Count)
            {
                throw new ArgumentException(
                    "One hero carries at most one projected progression.", nameof(heroProgression));
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

            PendingSystemTransitionDescription = pendingSystemTransitionDescription;
            if (pendingSystemTransitionDescription != null &&
                !ReferenceEquals(
                    pendingSystemTransitionDescription.PendingStep, pendingSystemTransitionStep))
            {
                // A description of some other pause would be a second answer to what the match is
                // waiting on, and the surface built from it would offer options the authority refuses.
                throw new ArgumentException(
                    "A projected transition description describes the pause this projection carries.",
                    nameof(pendingSystemTransitionDescription));
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
                PendingTurnRelease);

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
            PendingSystemTransitionDescription { get; }

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

        private static CardInstance CloneCard(CardInstance card)
        {
            return new CardInstance(card.Id, card.DefinitionReference, card.OwnerId, card.State);
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
