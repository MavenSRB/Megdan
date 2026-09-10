using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards
{
    public sealed class ActionDefinition
    {
        public ActionDefinition(
            ActionDefinitionId id,
            ActionKind kind,
            ActionDefinitionOrigin origin,
            IEnumerable<PrintedValueDefinition> baseValues,
            ActionDefinitionPolicy defaultPolicy)
            : this(
                id,
                kind,
                origin,
                baseValues,
                defaultPolicy,
                AttackRangeKind.None,
                default)
        {
        }

        public ActionDefinition(
            ActionDefinitionId id,
            ActionKind kind,
            ActionDefinitionOrigin origin,
            IEnumerable<PrintedValueDefinition> baseValues,
            ActionDefinitionPolicy defaultPolicy,
            AttackRangeKind attackRange,
            ActionCardStateTransition sourceCardStateTransition,
            EffectDefinition effectRoot = null,
            PrintedTextComposition textComposition = default,
            PrintedDefenseResolution defenseResolution = PrintedDefenseResolution.None,
            CardTextTiming cardTextTiming = CardTextTiming.None,
            PrintedAttackTargetPolicy? attackTargetPolicy = null,
            IEnumerable<RosterValueContributionDefinition> rosterValueContributions = null,
            MinionDefenseContributionPolicy minionDefenseContributions =
                MinionDefenseContributionPolicy.IncludeAll,
            DefenseOfferOriginRestriction defenseOfferRestriction = default,
            PrintedBlockCondition blockCondition = PrintedBlockCondition.None,
            EffectDefinition secondCardTextRoot = null,
            CardTextTiming secondCardTextTiming = CardTextTiming.None)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Action definition requires a defined ID.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(ActionKind), kind) || kind == ActionKind.None)
            {
                throw new ArgumentException("Action definition requires an action kind.", nameof(kind));
            }

            if (!Enum.IsDefined(typeof(ActionDefinitionOrigin), origin) || origin == ActionDefinitionOrigin.None)
            {
                throw new ArgumentException("Action definition requires an origin.", nameof(origin));
            }

            if (baseValues == null)
            {
                throw new ArgumentNullException(nameof(baseValues));
            }

            if (!Enum.IsDefined(typeof(AttackRangeKind), attackRange))
            {
                throw new ArgumentException("Action definition requires a valid Attack range classification.", nameof(attackRange));
            }

            if (!Enum.IsDefined(typeof(PrintedDefenseResolution), defenseResolution))
            {
                throw new ArgumentException(
                    "Action definition requires a valid printed Defense resolution.", nameof(defenseResolution));
            }

            if (!Enum.IsDefined(typeof(CardTextTiming), cardTextTiming))
            {
                throw new ArgumentException(
                    "Action definition requires a valid card-text timing.", nameof(cardTextTiming));
            }

            if (!Enum.IsDefined(typeof(PrintedBlockCondition), blockCondition))
            {
                throw new ArgumentException(
                    "Action definition requires a valid printed Block condition.",
                    nameof(blockCondition));
            }

            ValidatePolicy(origin, defaultPolicy);

            Id = id;
            Kind = kind;
            Origin = origin;
            var materializedBaseValues = baseValues.ToArray();
            BaseValues = Array.AsReadOnly(materializedBaseValues);
            DefaultPolicy = defaultPolicy;
            AttackRange = attackRange;
            SourceCardStateTransition = sourceCardStateTransition;
            EffectRoot = effectRoot;
            TextComposition = textComposition;
            CardTextTiming = cardTextTiming;
            RosterValueContributions = Array.AsReadOnly(
                (rosterValueContributions ??
                    Enumerable.Empty<RosterValueContributionDefinition>()).ToArray());
            MinionDefenseContributions = minionDefenseContributions;
            DefenseOfferRestriction = defenseOfferRestriction;
            BlockCondition = blockCondition;
            SecondCardTextRoot = secondCardTextRoot;
            SecondCardTextTiming = secondCardTextTiming;

            // Milestone 8.5. p.13 states an Attack's reach as a rule about the card - adjacent for a
            // non-Ranged one "even if the card has a radius icon", the printed Range for a Ranged one -
            // so every Attack has a target policy and one that narrows nothing is the page's own
            // default rather than an absence. Nothing but an Attack has one, because no other action
            // kind targets an enemy unit.
            AttackTargetPolicy = kind == ActionKind.Attack &&
                (attackRange == AttackRangeKind.Ranged || attackRange == AttackRangeKind.NonRanged)
                ? attackTargetPolicy ?? PrintedAttackTargetPolicy.ForClassification(attackRange)
                : (PrintedAttackTargetPolicy?)null;

            if (attackTargetPolicy.HasValue && !AttackTargetPolicy.HasValue)
            {
                throw new ArgumentException(
                    "Only a classified Attack action definition declares what it may target.",
                    nameof(AttackTargetPolicy));
            }

            // Milestone 8.3. A Defense action that names no resolution resolves the way every Defense
            // action in this build has since milestone 2.3 - by comparing its printed number - so the
            // default is normalised here rather than left as a third state every reader has to
            // interpret. p.14's Block is opt-in, which is what keeps a definition that simply forgot
            // its Defense value from arriving as a Block.
            DefenseResolution = kind == ActionKind.Defense && defenseResolution == PrintedDefenseResolution.None
                ? PrintedDefenseResolution.Value
                : defenseResolution;

            if (Kind != ActionKind.Defense && DefenseResolution != PrintedDefenseResolution.None)
            {
                throw new ArgumentException(
                    "Only a Defense action definition states how a Defense resolves.",
                    nameof(defenseResolution));
            }

            // Milestone 8.23. p.14 resolves a Block by "the Block condition" and never says what one
            // is, so the card has to. The requirement runs both ways: a Block that names no condition
            // would be resolved by whatever a reader assumed, and anything else naming one would be
            // stating a rule its printed face does not carry.
            if (DefenseResolution == PrintedDefenseResolution.Block &&
                BlockCondition == PrintedBlockCondition.None)
            {
                throw new ArgumentException(
                    "p.14's Block resolves by the condition its card prints, so a Block action " +
                    "definition names that condition.",
                    nameof(blockCondition));
            }

            if (DefenseResolution != PrintedDefenseResolution.Block &&
                BlockCondition != PrintedBlockCondition.None)
            {
                throw new ArgumentException(
                    "Only a printed Block states a Block condition.", nameof(blockCondition));
            }

            ValidateSecondCardText();

            if (BaseValues.Select(value => value.Channel).Distinct().Count() != BaseValues.Count)
            {
                throw new ArgumentException("Action definition base values must have unique channels.", nameof(baseValues));
            }

            if (!Enum.IsDefined(typeof(MinionDefenseContributionPolicy), MinionDefenseContributions))
            {
                throw new ArgumentException(
                    "An action definition requires a valid minion Defense contribution policy.",
                    nameof(minionDefenseContributions));
            }

            if (RosterValueContributions.Distinct().Count() != RosterValueContributions.Count)
            {
                throw new ArgumentException(
                    "An action definition cannot declare the same roster value contribution twice.",
                    nameof(rosterValueContributions));
            }

            if (RosterValueContributions.Any(value =>
                    (value.Channel != PrintedValueChannel.Attack &&
                     value.Channel != PrintedValueChannel.Range &&
                     value.Channel != PrintedValueChannel.Defense) ||
                    value.Query == RosterValueQueryKind.None ||
                    value.AmountPerMatch <= 0))
            {
                throw new ArgumentException(
                    "Roster value contributions must be fully defined typed declarations.",
                    nameof(rosterValueContributions));
            }

            // Milestone 8.35z28. The channel decides which action may carry the declaration, because the
            // two channels are read at two different moments by two different heroes: an Attack's own
            // Attack and Range totals are the attacker's, and a Defense total is the defender's answer
            // to them. Dodger's Shield of Decay, Vampiric Shield and Aegis of Doom are the first
            // verified faces to print a `+` on a Defense value, which printed p.14 illustrates on
            // Vampiric Shield itself and names in as many words - "Primary Defense Value 4 (The '+'
            // sign signifies that the value can be increased by the card text)."
            if (RosterValueContributions.Any(value =>
                    value.Channel == PrintedValueChannel.Defense) &&
                Kind != ActionKind.Defense)
            {
                throw new ArgumentException(
                    "A dynamic Defense contribution belongs to a Defense action.",
                    nameof(rosterValueContributions));
            }

            if (RosterValueContributions.Any(value =>
                    value.Channel != PrintedValueChannel.Defense) &&
                Kind != ActionKind.Attack)
            {
                throw new ArgumentException(
                    "Dynamic Attack/Range declarations belong to an Attack action.",
                    nameof(rosterValueContributions));
            }

            if (RosterValueContributions.Any(value =>
                    value.Channel == PrintedValueChannel.Range) &&
                AttackRange != AttackRangeKind.Ranged)
            {
                throw new ArgumentException(
                    "A dynamic Range contribution belongs to a Ranged Attack that prints Range.",
                    nameof(rosterValueContributions));
            }

            if (MinionDefenseContributions != MinionDefenseContributionPolicy.IncludeAll &&
                Kind != ActionKind.Defense)
            {
                throw new ArgumentException(
                    "Only a Defense action changes p.14's minion Defense contributions.",
                    nameof(minionDefenseContributions));
            }

            if (DefenseOfferRestriction.IsDefined && Kind != ActionKind.Attack)
            {
                throw new ArgumentException(
                    "Only an incoming Attack restricts which Defense origins may answer it.",
                    nameof(defenseOfferRestriction));
            }

            ValidateActionShape(baseValues);
        }

        public ActionDefinitionId Id { get; }

        public ActionKind Kind { get; }

        public ActionDefinitionOrigin Origin { get; }

        public IReadOnlyList<PrintedValueDefinition> BaseValues { get; }

        public ActionDefinitionPolicy DefaultPolicy { get; }

        public AttackRangeKind AttackRange { get; }

        public ActionCardStateTransition SourceCardStateTransition { get; }

        /// <summary>
        /// The effect this action executes, for kinds that resolve through the typed effect frame.
        /// Null for Movement, Attack, Defense, Hold, Fast Travel, and Clear, which have their own algorithms.
        /// </summary>
        public EffectDefinition EffectRoot { get; }

        /// <summary>
        /// Milestone 8.3. Whether this action's printed text still owes its typed composition, and to
        /// which milestone. <see cref="PrintedTextComposition.None"/> on everything that either carries
        /// its composition already or prints no text at all.
        /// </summary>
        public PrintedTextComposition TextComposition { get; }

        /// <summary>
        /// Milestone 8.3. How a Defense action resolves - p.14's numeric comparison or its Block -
        /// and <see cref="PrintedDefenseResolution.None"/> for every other kind.
        /// </summary>
        public PrintedDefenseResolution DefenseResolution { get; }

        /// <summary>
        /// Milestone 8.5. Which of p.14's numbered moments this action's <see cref="EffectRoot"/> is
        /// applied at, for a printed Attack or Defense whose root is card text rather than the action
        /// itself. <see cref="CardTextTiming.None"/> on a Skill, whose effect runs when the action does,
        /// and on everything carrying no root.
        /// </summary>
        public CardTextTiming CardTextTiming { get; }

        /// <summary>
        /// Milestone 8.5. What a printed Attack may target and how far it reaches, as rulebook p.13
        /// states it - present on every classified Attack and null on every other kind.
        /// </summary>
        public PrintedAttackTargetPolicy? AttackTargetPolicy { get; }

        /// <summary>
        /// Milestone 8.19b's trusted Attack/Range contribution declarations. Empty means the printed
        /// total has no roster-derived card-text addition.
        /// </summary>
        public IReadOnlyList<RosterValueContributionDefinition> RosterValueContributions { get; }

        /// <summary>
        /// Whether this Defense includes p.14's ordinary friendly and enemy minion modifiers.
        /// </summary>
        public MinionDefenseContributionPolicy MinionDefenseContributions { get; }

        /// <summary>
        /// The exact Defense-origin restriction this incoming Attack applies, or the typed absence.
        /// </summary>
        public DefenseOfferOriginRestriction DefenseOfferRestriction { get; }

        /// <summary>
        /// The condition p.14 resolves this Block by, and <see cref="PrintedBlockCondition.None"/> for
        /// every action that is not one. Milestone 8.23.
        /// </summary>
        public PrintedBlockCondition BlockCondition { get; }

        /// <summary>
        /// A second body of card text this action prints, applied at a different one of rulebook
        /// p.14's moments - or <c>null</c>, which is every action built before milestone 8.23.
        ///
        /// <para><b>Why a second root rather than one root with two entry points.</b>
        /// <see cref="CardTextTiming"/> recorded the reason this was left open: "One action declares
        /// one timing. No verified face in Wasp's set prints two attack-text keywords ... so a card
        /// printing both would widen this to a list at the slice that first transcribes one, rather
        /// than being guessed at here." Tigerclaw's Combat Reflexes and Leaping Strike are that slice.
        /// Each prints "Before the attack:" and "After the attack:" on the same face, and p.14 runs
        /// them six numbered steps apart with the defender's whole response in between - so they are
        /// two applications of card text and not one effect with a gap in it. Each one pauses,
        /// persists and resumes on its own, exactly as a single body does.</para>
        ///
        /// <para>Two is the whole of what verified content prints, and the pair is stated as two
        /// members rather than a list for that reason: p.14 gives an attacker exactly two moments -
        /// step 1 and step 7 - and step 4 belongs to the defender's own card. A face printing a third
        /// would have nowhere to put it.</para>
        /// </summary>
        public EffectDefinition SecondCardTextRoot { get; }

        /// <summary>Which of p.14's moments <see cref="SecondCardTextRoot"/> is applied at.</summary>
        public CardTextTiming SecondCardTextTiming { get; }

        /// <summary>
        /// The card text this action applies at one of p.14's moments, or <c>null</c> where it prints
        /// none for that moment. Milestone 8.23, and the one place the two declared bodies are chosen
        /// between - so nothing downstream has to know there can be two.
        /// </summary>
        public EffectDefinition CardTextAt(CardTextTiming timing)
        {
            if (timing == CardTextTiming.None)
            {
                return null;
            }

            if (CardTextTiming == timing)
            {
                return EffectRoot;
            }

            return SecondCardTextTiming == timing ? SecondCardTextRoot : null;
        }

        /// <summary>
        /// The printed body this action declares under <paramref name="reference"/>, or <c>null</c>
        /// where it declares none.
        ///
        /// <para>Milestone 8.23, and the second half of what <see cref="CardTextAt"/> exists for: a
        /// paused frame names the body it is resolving by reference rather than by moment, because a
        /// Skill's frame has no moment at all. An action carrying two bodies has to answer that
        /// question about both of them - a reading that consulted <see cref="EffectRoot"/> alone found
        /// nothing for a step-7 frame and the answer to its question was refused, which is how a
        /// player met it: Combat Reflexes' second move opened, and no answer to it was accepted.</para>
        /// </summary>
        public EffectDefinition EffectRootFor(EffectDefinitionReference reference)
        {
            if (EffectRoot != null && EffectRoot.Reference.Equals(reference))
            {
                return EffectRoot;
            }

            return SecondCardTextRoot != null && SecondCardTextRoot.Reference.Equals(reference)
                ? SecondCardTextRoot
                : null;
        }

        /// <summary>
        /// Milestone 8.23. What a second printed body of card text has to be true of.
        ///
        /// <para>It requires a first, because "second" is meaningless without one and an action whose
        /// only text sat here would be invisible to every reader that asks about
        /// <see cref="CardTextTiming"/>. It requires a different moment, because two bodies at the
        /// same moment are one body. Both moments must be the attacker's own two, because those are
        /// the only two p.14 gives one card - the third is the defender's Defense action text, printed
        /// on the defender's card. And only an Attack may carry one, because p.14's numbered order is
        /// the Attack's alone.</para>
        /// </summary>
        private void ValidateSecondCardText()
        {
            if (!Enum.IsDefined(typeof(CardTextTiming), SecondCardTextTiming))
            {
                throw new ArgumentException(
                    "Action definition requires a valid second card-text timing.",
                    nameof(SecondCardTextTiming));
            }

            if (SecondCardTextRoot == null && SecondCardTextTiming == CardTextTiming.None)
            {
                return;
            }

            if (SecondCardTextRoot == null || SecondCardTextTiming == CardTextTiming.None)
            {
                throw new ArgumentException(
                    "A second body of card text names both the text and the moment it is applied at.",
                    nameof(SecondCardTextRoot));
            }

            if (EffectRoot == null || CardTextTiming == CardTextTiming.None)
            {
                throw new ArgumentException(
                    "A second body of card text requires a first; an action whose only text were " +
                    "declared here would be invisible to every reader that asks about CardTextTiming.",
                    nameof(SecondCardTextRoot));
            }

            if (SecondCardTextTiming == CardTextTiming)
            {
                throw new ArgumentException(
                    "Two bodies of card text applied at the same moment are one body.",
                    nameof(SecondCardTextTiming));
            }

            if (Kind != ActionKind.Attack)
            {
                throw new ArgumentException(
                    "Only an Attack prints text at two moments; p.14's numbered order is the " +
                    "Attack's alone.",
                    nameof(SecondCardTextRoot));
            }

            if (!IsAttackerMoment(CardTextTiming) || !IsAttackerMoment(SecondCardTextTiming))
            {
                throw new ArgumentException(
                    "p.14 gives the attacker two moments - step 1 and step 7 - and its third belongs " +
                    "to the defender's own card.",
                    nameof(SecondCardTextTiming));
            }
        }

        private static bool IsAttackerMoment(CardTextTiming timing) =>
            timing == CardTextTiming.BeforeTheAttack || timing == CardTextTiming.AfterTheAttack;

        private void ValidateActionShape(IEnumerable<PrintedValueDefinition> baseValues)
        {
            if (Kind == ActionKind.Movement)
            {
                // Milestone 8.20 widens this by p.9's own sentence, exactly as milestone 8.3 widened
                // the non-Ranged Attack's shape: "Some primary actions have a Range or Radius icon and
                // a value on the right side of the card". Arien's Deluge prints Movement 3 as its
                // primary and a Radius 1 icon beside it, and the icon is the scope its "This turn"
                // text asks about rather than a second Movement number. A secondary Movement icon
                // prints no such thing, which is why the widening is bounded to the primary.
                var admitsAnIcon = Origin == ActionDefinitionOrigin.PrintedPrimary;
                var optionalChannels = admitsAnIcon
                    ? new[] { PrintedValueChannel.Radius, PrintedValueChannel.Range }
                    : Array.Empty<PrintedValueChannel>();
                if (BaseValues.Count < 1 ||
                    BaseValues.Count > 2 ||
                    BaseValues.All(value => value.Channel != PrintedValueChannel.Movement) ||
                    BaseValues.Any(value =>
                        value.Channel != PrintedValueChannel.Movement &&
                        !optionalChannels.Contains(value.Channel)))
                {
                    throw new ArgumentException(
                        admitsAnIcon
                            ? "A Movement action definition prints one Movement value and at most " +
                              "rulebook p.9's one Range or Radius icon."
                            : "Movement action definitions currently support exactly one Movement " +
                              "printed value.",
                        nameof(baseValues));
                }

                if (BaseValues.Any(value =>
                        value.Channel != PrintedValueChannel.Movement && value.Amount < 1))
                {
                    throw new ArgumentException(
                        "A printed Range or Radius icon states a positive value; p.9's smallest is 1, " +
                        "which reaches adjacent spaces only.",
                        nameof(baseValues));
                }
            }
            else if (Kind == ActionKind.Attack)
            {
                if (AttackRange == AttackRangeKind.None)
                {
                    throw new ArgumentException("Attack action definitions require a range classification.", nameof(AttackRange));
                }

                // Milestone 8.3 widens the non-Ranged shape by p.9's own sentence, "Some primary actions
                // have a Range or Radius icon and a value on the right side of the card". Four of
                // Wasp's Attacks print a Radius beside their Attack value - it is the scope their
                // After-the-attack text asks about, not a second combat number - so the shape is one
                // Attack value plus at most p.9's one icon. A Ranged Attack's icon is the Range that
                // classifies it, and no verified face prints both.
                var expectedChannels = AttackRange == AttackRangeKind.Ranged
                    ? new[] { PrintedValueChannel.Attack, PrintedValueChannel.Range }
                    : new[] { PrintedValueChannel.Attack };
                var optionalChannels = AttackRange == AttackRangeKind.Ranged
                    ? Array.Empty<PrintedValueChannel>()
                    : new[] { PrintedValueChannel.Radius };
                if (BaseValues.Count < expectedChannels.Length ||
                    BaseValues.Count > expectedChannels.Length + optionalChannels.Length ||
                    expectedChannels.Any(channel => BaseValues.All(value => value.Channel != channel)) ||
                    BaseValues.Any(value =>
                        !expectedChannels.Contains(value.Channel) &&
                        !optionalChannels.Contains(value.Channel)))
                {
                    throw new ArgumentException(
                        AttackRange == AttackRangeKind.Ranged
                            ? "Ranged Attack action definitions require exactly one Attack and one Range printed value."
                            : "Non-Ranged Attack action definitions require exactly one Attack printed " +
                              "value and at most rulebook p.9's Radius icon.",
                        nameof(baseValues));
                }

                if (AttackRange == AttackRangeKind.Ranged &&
                    BaseValues.Single(value => value.Channel == PrintedValueChannel.Range).Amount < 1)
                {
                    throw new ArgumentException(
                        "Ranged Attack action definitions require a positive printed Range value.",
                        nameof(baseValues));
                }

                if (BaseValues.Any(value =>
                        value.Channel == PrintedValueChannel.Radius && value.Amount < 1))
                {
                    throw new ArgumentException(
                        "A printed Radius icon states a positive value; p.9's smallest is 1, which " +
                        "reaches adjacent targets only.",
                        nameof(baseValues));
                }
            }
            else if (Kind == ActionKind.Defense && DefenseResolution == PrintedDefenseResolution.Block)
            {
                // Milestone 8.3, and p.14's other Defense shape. A Block card prints a shield with an
                // exclamation mark where every other card prints a number, so the two numeric rules
                // below cannot apply to it: there is no Defense value to require, and the card is not
                // the in-hand card a secondary Defense discards - p.14 prints the Block as the card's
                // own primary action, which milestone 8.11 resolves on milestone 5.8's reserved
                // DefenseBlockResult seam.
                if (Origin != ActionDefinitionOrigin.PrintedPrimary)
                {
                    throw new ArgumentException(
                        "p.14's Block is a card's printed primary action, not a secondary reaction.",
                        nameof(Origin));
                }

                if (BaseValues.Any(value => value.Channel != PrintedValueChannel.Range))
                {
                    throw new ArgumentException(
                        "A Block prints no Defense value; the only value icon p.9 admits beside a " +
                        "primary action is Range or Radius, and a Block card's is Range.",
                        nameof(baseValues));
                }

                if (BaseValues.Count > 0 && BaseValues.Single().Amount < 1)
                {
                    throw new ArgumentException(
                        "A printed Range icon states a positive value.", nameof(baseValues));
                }
            }
            else if (Kind == ActionKind.Defense)
            {
                // Milestone 8.35z28 widens this by p.9's own sentence, exactly as milestone 8.3 widened
                // the non-Ranged Attack's shape and milestone 8.20 the Movement primary's: "Some
                // primary actions have a Range or Radius icon and a value on the right side of the
                // card." Dodger's Shield of Decay, Vampiric Shield and Aegis of Doom each print a
                // Defense primary of 3, 4 and 4 and a Radius 2 icon beside it, and the icon is the
                // scope their own text asks about - "2 or more empty spawn points in radius" - rather
                // than a second combat number. A secondary Defense icon prints no such thing, which is
                // why the widening is bounded to the primary.
                var admitsAnIcon = Origin == ActionDefinitionOrigin.PrintedPrimary;
                var optionalChannels = admitsAnIcon
                    ? new[] { PrintedValueChannel.Radius, PrintedValueChannel.Range }
                    : Array.Empty<PrintedValueChannel>();

                // One Defense value, plus at most p.9's one icon - which is one and not both, exactly
                // as the Movement and Skill shapes above bound it.
                var maximumValues = admitsAnIcon ? 2 : 1;
                if (BaseValues.Count < 1 ||
                    BaseValues.Count > maximumValues ||
                    BaseValues.All(value => value.Channel != PrintedValueChannel.Defense) ||
                    BaseValues.Any(value =>
                        value.Channel != PrintedValueChannel.Defense &&
                        !optionalChannels.Contains(value.Channel)))
                {
                    throw new ArgumentException(
                        admitsAnIcon
                            ? "A Defense action definition prints one Defense value and at most " +
                              "rulebook p.9's one Range or Radius icon."
                            : "Defense action definitions currently support exactly one Defense " +
                              "printed value.",
                        nameof(baseValues));
                }

                if (BaseValues.Any(value =>
                        value.Channel != PrintedValueChannel.Defense && value.Amount < 1))
                {
                    throw new ArgumentException(
                        "A printed Range or Radius icon states a positive value; p.9's smallest is 1, " +
                        "which reaches adjacent spaces only.",
                        nameof(baseValues));
                }

                // Milestone 8.20 admits the second place a Defense value is printed. Every Defense in
                // this build until now was the small icon in a card's secondary strip, and Arien's
                // three Blue Duelist cards print a Defense of 5, 6 and 6 in the <em>primary</em> slot
                // with no secondary Defense icon at all - which is p.14's Block shape carrying a number
                // instead of the shield-and-exclamation-mark.
                //
                // What does not change is the cost or the moment: p.14 gives one rule for both -
                // "It can only be performed in response to an attack targeting you, by discarding
                // exactly one card from your hand" - so the discard below is still required of either
                // origin, and ActionOfferQuery keeps both out of the turn's own offers.
                if (Origin != ActionDefinitionOrigin.Reaction &&
                    Origin != ActionDefinitionOrigin.PrintedPrimary)
                {
                    throw new ArgumentException(
                        "A Defense action definition is a card's printed primary or its secondary " +
                        "reaction.",
                        nameof(Origin));
                }

                if (!SourceCardStateTransition.Equals(new ActionCardStateTransition(
                        CardLocation.Hand,
                        CardLocation.DiscardPile)))
                {
                    throw new ArgumentException(
                        "Defense action definitions must require an in-hand card and declare discard as their cost/state transition.",
                        nameof(SourceCardStateTransition));
                }
            }
            else if (Kind == ActionKind.FastTravel || Kind == ActionKind.Clear)
            {
                if (Origin != ActionDefinitionOrigin.Replacement || BaseValues.Count != 0)
                {
                    throw new ArgumentException(
                        $"{Kind} must be a value-free replacement action definition.",
                        nameof(baseValues));
                }
            }

            else if (Kind == ActionKind.Skill)
            {
                // Milestone 8.3 admits the one absence a Skill may state, and states it rather than
                // permitting a null. A Skill's whole content is its effect, so a Skill carrying neither
                // an effect root nor an explicit outstanding composition is a card that does nothing;
                // a verified Skill whose composition is owed to its own 8.5-8.13 slice says so through
                // PrintedTextComposition, and milestone 8.4 is what refuses to perform it meanwhile.
                if (EffectRoot == null && !TextComposition.IsAwaited)
                {
                    throw new ArgumentException(
                        "Skill action definitions require an effect root, or an explicit statement of " +
                        "the milestone that owes its composition.", nameof(EffectRoot));
                }

                if (Origin != ActionDefinitionOrigin.PrintedPrimary &&
                    Origin != ActionDefinitionOrigin.PrintedSecondary)
                {
                    throw new ArgumentException(
                        "Skill action definitions are printed on their card.", nameof(Origin));
                }

                // A Skill carries no printed combat value: what it does is its effect, and the effect
                // is data the resolver walks rather than a number an algorithm consumes. Milestone
                // 7.2b admits the one exception rulebook p.9 prints, and only that one: "Some primary
                // actions have a Range or Radius icon and a value on the right side of the card", and
                // "the value on the Range/Radius icon determines the maximum Range/Radius of your
                // action". A Radius is not a combat value the Skill's algorithm consumes - a Skill has
                // none - it is the scope its effect asks about, so it is printed content the effect
                // reads rather than a number written into the effect's text.
                //
                // Milestone 8.3 admits the other half of the same sentence. p.9 writes "a Range or
                // Radius icon", and Wasp's Telekinesis and Mass Telekinesis print the Range one on a
                // Skill, so restricting this to Radius alone was the synthetic content's shape rather
                // than the page's rule. The page prints one such icon, not two, which is why the count
                // is bounded here rather than left to the unique-channel rule above.
                if (BaseValues.Any(value =>
                        value.Channel != PrintedValueChannel.Radius &&
                        value.Channel != PrintedValueChannel.Range))
                {
                    throw new ArgumentException(
                        "A Skill action definition prints no value other than rulebook p.9's Range or Radius.",
                        nameof(baseValues));
                }

                if (BaseValues.Count > 1)
                {
                    throw new ArgumentException(
                        "Rulebook p.9 prints one Range or Radius icon beside a primary action, not both.",
                        nameof(baseValues));
                }

                if (BaseValues.Count > 0)
                {
                    // p.9 prints the icon for a primary action, and the same page makes the Ranged
                    // subtype the Range icon's own: "All other actions are non-ranged, including those
                    // with a Radius icon." A Skill declares no Attack range classification at all,
                    // which the whole-definition rule below already enforces, so a Radius-printing
                    // Skill is non-Ranged by construction.
                    if (Origin != ActionDefinitionOrigin.PrintedPrimary)
                    {
                        throw new ArgumentException(
                            "Rulebook p.9 prints a Radius icon for a primary action.", nameof(Origin));
                    }

                    if (BaseValues.Single().Amount < 1)
                    {
                        throw new ArgumentException(
                            "A printed Radius icon states a positive value; p.9's smallest is 1, which " +
                            "reaches adjacent targets only.",
                            nameof(baseValues));
                    }
                }
            }

            // Milestone 8.5 opens the seam the 7.9a review recorded as F-CARD-01. Until now a
            // non-Skill action could carry no effect root at all, so a printed Attack's "After the
            // attack:" rider had nowhere to live and Movement, Hold, Fast Travel and Clear were told
            // apart from it by the same absence. The two are separated here instead. A Skill's root is
            // the action - p.12: "When you perform a Skill action, simply apply the card text" - and an
            // Attack's or a Defense's root is card text applied at one of p.14's numbered moments,
            // which is why the second kind must name its moment and the first must not. Movement, Hold,
            // Fast Travel and Clear still carry none: each is an algorithm, and the two replacements
            // suppress their source card's text by their own definition.
            // Milestone 8.19a added the fourth kind. A Movement action carries an effect root only as
            // text applied around it - Xargatha's Charm moves an enemy minion before or after her own
            // Movement - so the root and the timing arrive together or not at all, which is what the
            // rule below already requires of an Attack's and a Defense's.
            // Milestone 8.20 adds the Movement primary's other moment. p.11 states the default -
            // "Unless it says otherwise, the card text comes into effect before movement" - so a card
            // that says nothing carries BeforeTheMovement and a card that says otherwise carries
            // 8.19a's AroundTheAction. Both are still text on a Movement, so both arrive with a timing
            // or not at all.
            // Milestone 8.35z31 adds the third. Ursafar's three swap faces print "after
            // movement" in so many words, which is the "otherwise" p.11's sentence allows, so the side
            // is the card's statement rather than the page's default or the actor's answer. It joins
            // the same list for the same reason: still text on a Movement, still arriving with a
            // timing or not at all.
            if (EffectRoot != null &&
                Kind != ActionKind.Skill && Kind != ActionKind.Attack && Kind != ActionKind.Defense &&
                !(Kind == ActionKind.Movement &&
                    (CardTextTiming == CardTextTiming.AroundTheAction ||
                        CardTextTiming == CardTextTiming.BeforeTheMovement ||
                        CardTextTiming == CardTextTiming.AfterTheMovement)))
            {
                throw new ArgumentException(
                    "Only a Skill, an Attack, a Defense, or a Movement action applying text before, " +
                    "after or around itself executes through the typed effect frame.",
                    nameof(EffectRoot));
            }

            if (CardTextTiming != CardTextTiming.None)
            {
                if (EffectRoot == null)
                {
                    throw new ArgumentException(
                        "A card-text timing states when this action's effect root is applied, so it " +
                        "cannot be stated without one.", nameof(CardTextTiming));
                }

                if (Kind != CardTextTimingQuery.OwningActionKind(CardTextTiming))
                {
                    throw new ArgumentException(
                        $"Rulebook p.14 applies {CardTextTiming} inside a " +
                        $"{CardTextTimingQuery.OwningActionKind(CardTextTiming)} action, not a {Kind} one.",
                        nameof(CardTextTiming));
                }

                // p.13: "When you perform a primary action, apply the card text." An action whose
                // source-card text is suppressed performs none, so it has no moment to apply one at -
                // which is exactly what keeps a replacement from inheriting the replaced card's rider.
                if (DefaultPolicy.CardTextApplication != CardTextApplicationPolicy.Applies)
                {
                    throw new ArgumentException(
                        "Only an action that applies its source card's text applies it at one of " +
                        "rulebook p.14's moments.", nameof(CardTextTiming));
                }
            }
            else if (EffectRoot != null && Kind != ActionKind.Skill)
            {
                throw new ArgumentException(
                    "An Attack or Defense action's effect root is card text, so it names the moment " +
                    "rulebook p.14 applies it at.", nameof(CardTextTiming));
            }

            // Milestone 8.19a. An Attack whose target its own text names has to have text to name it
            // with, and that text has to be the moment rulebook p.14 puts before the Attack Total: step
            // 1. Both refusals are the statement failing in the only two ways it can - no text at all,
            // and text at a moment the target is already needed by.
            if (AttackTargetPolicy.HasValue && AttackTargetPolicy.Value.TargetIsBoundByCardText &&
                CardTextTiming != CardTextTiming.BeforeTheAttack)
            {
                throw new ArgumentException(
                    "An Attack whose target is bound by its card text applies that text at rulebook " +
                    "p.14's step 1, before the page announces the Attack Total against it.",
                    nameof(AttackTargetPolicy));
            }

            if (AttackTargetPolicy.HasValue &&
                AttackRange == AttackRangeKind.NonRanged &&
                AttackTargetPolicy.Value.Reach != AttackTargetReach.AdjacentOnly)
            {
                throw new ArgumentException(
                    "Rulebook p.13: \"Non-Ranged Attacks can only target units adjacent to you (even " +
                    "if the card has a radius icon).\"", nameof(AttackTargetPolicy));
            }

            // Milestone 8.35z30 adds the band beside the ceiling: "at maximum range" reads
            // the same printed number this refusal is about, so an Attack declaring either reach
            // prints one.
            if (AttackTargetPolicy.HasValue &&
                (AttackTargetPolicy.Value.Reach == AttackTargetReach.PrintedRange ||
                    AttackTargetPolicy.Value.Reach == AttackTargetReach.ExactlyPrintedRange ||
                    AttackTargetPolicy.Value.Reach ==
                        AttackTargetReach.PrintedRangeBeyondAdjacent) &&
                BaseValues.All(value => value.Channel != PrintedValueChannel.Range))
            {
                throw new ArgumentException(
                    "Rulebook p.13 reaches \"based on the Range value on the card\", so an Attack " +
                    "declaring that reach prints one.", nameof(AttackTargetPolicy));
            }

            // Milestone 8.3. An outstanding composition is a statement about this action's printed card
            // text, so it is refused in both of the two places it could not be true: beside an effect
            // root, which is the composition it claims is missing, and on an action whose source-card
            // text is suppressed, which by p.9 performs no card text to owe. Without those two the
            // statement would decay into a free-floating annotation any action could carry.
            if (TextComposition.IsAwaited)
            {
                if (EffectRoot != null)
                {
                    throw new ArgumentException(
                        "An action carrying its effect root owes no composition.", nameof(TextComposition));
                }

                if (DefaultPolicy.CardTextApplication != CardTextApplicationPolicy.Applies)
                {
                    throw new ArgumentException(
                        "Only an action that performs its source card's text can owe that text a " +
                        "composition (rulebook p.9).",
                        nameof(TextComposition));
                }
            }

            if (Kind != ActionKind.Attack && AttackRange != AttackRangeKind.None)
            {
                throw new ArgumentException("Only Attack action definitions may declare an Attack range classification.", nameof(AttackRange));
            }
        }

        private static void ValidatePolicy(ActionDefinitionOrigin origin, ActionDefinitionPolicy defaultPolicy)
        {
            if (!defaultPolicy.IsDefined)
            {
                throw new ArgumentException("Action definitions require a defined default policy.", nameof(defaultPolicy));
            }

            if (origin == ActionDefinitionOrigin.PrintedPrimary &&
                defaultPolicy.CardTextApplication != CardTextApplicationPolicy.Applies)
            {
                throw new ArgumentException("Printed Primary actions must default to applying card text.", nameof(defaultPolicy));
            }

            if ((origin == ActionDefinitionOrigin.PrintedSecondary ||
                    origin == ActionDefinitionOrigin.GeneralRuleSecondary ||
                    origin == ActionDefinitionOrigin.Replacement) &&
                defaultPolicy.CardTextApplication != CardTextApplicationPolicy.Suppressed)
            {
                throw new ArgumentException(
                    "Printed Secondary, General Rule Secondary, and replacement actions must default to suppressing source-card text.",
                    nameof(defaultPolicy));
            }

            if ((origin == ActionDefinitionOrigin.Replacement ||
                    origin == ActionDefinitionOrigin.Reaction) &&
                defaultPolicy.CanBeReplaced)
            {
                throw new ArgumentException("Replacement and reaction actions cannot default to being replaced.", nameof(defaultPolicy));
            }
        }
    }
}
