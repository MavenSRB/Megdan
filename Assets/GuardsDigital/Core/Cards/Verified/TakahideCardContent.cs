using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Takahide the Warlord's printed content (the thirtieth hero; [his shard](Docs/AI/Rules/HERO_TAKAHIDE.md)).
    /// Milestone 9.32d composes his three gold cards, Bushido and his five Red faces on 9.32a-9.32c's
    /// contracts; 9.32e composes the Blue and Green faces and 9.32f the Ultimate. 9.32h deals him: his
    /// set is <c>TakahideCardContent.Set.cs</c>, the swap and discard-choice families are registered in
    /// the build's synthetic catalog and his two obstacle effects' creations in its content catalog.
    ///
    /// <para><b>Three gold cards.</b> Float like a Butterfly starts in the hand and Sting like a Bee
    /// and Strike like a Tiger in the deck, by their own reminder text; each swaps itself with a
    /// different gold card in the deck (p.10, TK-Q1, TK-Q2), and Bushido swaps "your gold card" with
    /// one, facedown when the old one was resolved or discarded.</para>
    /// </summary>
    public static partial class TakahideCardContent
    {
        /// <summary>The revision every Takahide definition carries.</summary>
        public const string ContentRevision = "takahide-9-32h-rev-1";

        public const string ContentSetId = "takahide";

        public static ActionDefinitionId PrimaryAttack => new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimarySkill => new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId PrimaryMovement => new ActionDefinitionId("primary-movement");
        public static ActionDefinitionId SecondaryMovement => new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense => new ActionDefinitionId("secondary-defense");

        // -------------------------------------------------------------------------------------------
        // Every face's identity, so the upgrade lines can name faces composed in later blocks.
        // -------------------------------------------------------------------------------------------

        // Expression-bodied, so a partial file's static initialiser can read them whatever order the
        // compiler runs the partial files' initialisers in (9.32e).
        public static CardDefinitionId StingLikeABee => Id("sting-like-a-bee");
        public static CardDefinitionId FloatLikeAButterfly => Id("float-like-a-butterfly");
        public static CardDefinitionId StrikeLikeATiger => Id("strike-like-a-tiger");
        public static CardDefinitionId Bushido => Id("bushido");

        public static CardDefinitionId SetAnExample => Id("set-an-example");
        public static CardDefinitionId LeadFromTheFront => Id("lead-from-the-front");
        public static CardDefinitionId SpinningBlade => Id("spinning-blade");
        public static CardDefinitionId HoldMySake => Id("hold-my-sake");
        public static CardDefinitionId BladeHelix => Id("blade-helix");

        public static CardDefinitionId ProvenWarrior => Id("proven-warrior");
        public static CardDefinitionId ChosenChampion => Id("chosen-champion");
        public static CardDefinitionId CalculatedRisk => Id("calculated-risk");
        public static CardDefinitionId TheRightHand => Id("the-right-hand");
        public static CardDefinitionId TacticalGambit => Id("tactical-gambit");

        public static CardDefinitionId ComeToAid => Id("come-to-aid");
        public static CardDefinitionId BringTheRelief => Id("bring-the-relief");
        public static CardDefinitionId PledgeOfAllegiance => Id("pledge-of-allegiance");
        public static CardDefinitionId CommitReserves => Id("commit-reserves");
        public static CardDefinitionId LoyalRetainer => Id("loyal-retainer");

        public static CardDefinitionId ReadyForWar => Id("ready-for-war");

        /// <summary>Milestone 9.32d's faces: the three golds, Bushido and the five Reds.</summary>
        public static IReadOnlyList<CardDefinition> BasicAndRedDefinitions => LazyBasicAndRed.Value;

        /// <summary>
        /// Milestone 9.32h. The two creations his content owns - Spinning Blade's and Blade Helix's
        /// enemy-obstacle effects - joined to <c>MatchContentCatalogs</c>. The swap and discard-choice
        /// families his faces name are the synthetic catalog's (<c>SyntheticEffectCapabilities</c>).
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new ActiveEffectCreationHandler(GrantSpinningBladeV1, SpinningBladeObstacles);
            yield return new ActiveEffectCreationHandler(GrantBladeHelixV1, BladeHelixObstacles);
        }

        // -------------------------------------------------------------------------------------------
        // The basic faces
        // -------------------------------------------------------------------------------------------

        public const string StingLikeABeeText =
            "Target a unit at maximum range. After the attack: Swap this card with a different gold " +
            "card in your deck. (This card starts the game in your deck).";

        public const string FloatLikeAButterflyText =
            "Swap this card with a different gold card in your deck. (This card starts the game in " +
            "your hand.)";

        public const string StrikeLikeATigerText =
            "Target a unit adjacent to you. After the attack: Swap this card with a different gold " +
            "card in your deck. (This card starts the game in your deck).";

        public const string BushidoText =
            "Swap your gold card with a different gold card in your deck; if you swap a resolved or " +
            "discarded gold card this way, the new card is facedown.";

        /// <summary>Gold, Basic: Initiative 7, Defense 1, Movement 1, Basic Attack - Ranged 5, Range 3.</summary>
        public static CardDefinition StingLikeABeeDefinition { get; } = new CardDefinition(
            StingLikeABee, ContentRevision, "Sting like a Bee", new InitiativeValue(7),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 5),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 3)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged, default,
                    GoldSwap(StingLikeABee),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
                    // "Target a unit at maximum range": exactly the printed Range with Range items
                    // (Silverarrow's RULING 1).
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged).AtExactlyThePrintedRange()),
                Movement(1),
                Defense(1)
            },
            null, CardSourceContent.Transcribed(StingLikeABeeText));

        /// <summary>
        /// Gold, Basic: Initiative 8, Defense 8, Basic Movement 5. The swap is text after the Movement
        /// (TK-Q1): it runs when the walk ends, a move of 0 included, and not when Fast Travel replaces
        /// the Movement or the card is discarded to defend.
        /// </summary>
        public static CardDefinition FloatLikeAButterflyDefinition { get; } = new CardDefinition(
            FloatLikeAButterfly, ContentRevision, "Float like a Butterfly", new InitiativeValue(8),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 5) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    GoldSwap(FloatLikeAButterfly),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheMovement),
                Defense(8)
            },
            null, CardSourceContent.Transcribed(FloatLikeAButterflyText));

        /// <summary>Gold, Basic: Initiative 9, Defense 3, Movement 3, Basic Attack 7.</summary>
        public static CardDefinition StrikeLikeATigerDefinition { get; } = new CardDefinition(
            StrikeLikeATiger, ContentRevision, "Strike like a Tiger", new InitiativeValue(9),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 7) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    GoldSwap(StrikeLikeATiger),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                Movement(3),
                Defense(3)
            },
            null, CardSourceContent.Transcribed(StrikeLikeATigerText));

        /// <summary>Silver, Basic: Initiative 5, Defense 6, Basic Skill.</summary>
        public static CardDefinition BushidoDefinition { get; } = new CardDefinition(
            Bushido, ContentRevision, "Bushido", new InitiativeValue(5),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    BushidoSwap()),
                Defense(6)
            },
            null, CardSourceContent.Transcribed(BushidoText));

        /// <summary>
        /// "Swap this card with a different gold card in your deck." No "may", so it is performed
        /// whenever a gold card is in the deck and silently does nothing when none is (as after Ready
        /// for War): <c>IfAble</c>. Takahide chooses which deck gold (p.10 Making Choices).
        /// </summary>
        private static EffectDefinition GoldSwap(CardDefinitionId card)
        {
            var root = new EffectNodeId(card.Value + "-swap-with-deck-gold");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDeckSwapCapabilities.SwapThisCardWithDeckCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDeckSwapCapabilities.ColorParameterName, CardColor.Gold)
                        },
                        outputs: SwapOutputs())
                });
        }

        /// <summary>
        /// "Swap your gold card with a different gold card in your deck; if you swap a resolved or
        /// discarded gold card this way, the new card is facedown." The Skill's one step, mandatory: it
        /// is offered whenever his gold card and a deck gold exist, which is every turn Bushido can be
        /// played (Ready for War moves Bushido to the deck).
        /// </summary>
        private static EffectDefinition BushidoSwap()
        {
            var root = new EffectNodeId("bushido-swap-your-gold-card");
            return new EffectDefinition(
                new EffectDefinitionId(Bushido.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDeckSwapCapabilities.SwapOwnedColorCardWithDeckCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDeckSwapCapabilities.ColorParameterName, CardColor.Gold),
                            EffectInputBinding.FromLiteral(
                                SyntheticDeckSwapCapabilities.FacedownWhenRestingParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: SwapOutputs())
                });
        }

        private static EffectOutputBinding[] SwapOutputs() =>
            new[]
            {
                new EffectOutputBinding(SyntheticDeckSwapCapabilities.SwappedOutOutputName, EffectValueKind.CardInstance),
                new EffectOutputBinding(SyntheticDeckSwapCapabilities.SwappedInOutputName, EffectValueKind.CardInstance)
            };

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyBasicAndRed =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                StingLikeABeeDefinition,
                FloatLikeAButterflyDefinition,
                StrikeLikeATigerDefinition,
                BushidoDefinition,
                SetAnExampleDefinition,
                LeadFromTheFrontDefinition,
                SpinningBladeDefinition,
                HoldMySakeDefinition,
                BladeHelixDefinition
            }));

        internal static ActionDefinition Movement(int value) => new ActionDefinition(
            SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        internal static ActionDefinition Defense(int value) => new ActionDefinition(
            SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        internal static CardDefinitionId Id(string card) =>
            new CardDefinitionId(ContentSetId + "-" + card);
    }
}
