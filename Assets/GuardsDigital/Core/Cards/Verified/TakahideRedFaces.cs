using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.32d. Takahide's five Red faces, each a melee Attack ("Target a unit adjacent to
    /// you") whose after-attack text is one of two shapes:
    ///
    /// <list type="bullet">
    /// <item>Set an Example, Lead from the Front and Hold my Saké: "A friendly hero in radius may swap
    /// their unresolved card with a card in their hand[, or in their discard]." Takahide chooses the
    /// friendly hero among those the swap can act on (6.8l; a teammate who has already resolved has no
    /// unresolved card); that hero's seat decides whether and with which card (9.32b). With no such
    /// teammate the text does nothing and the Attack still resolves (TK-Q4: the Attack is what is
    /// offered).</item>
    /// <item>Spinning Blade and Blade Helix: "This turn: Empty spaces adjacent to you [in radius] count
    /// as obstacles for the enemy units" (9.32c, TK-Q3). "Adjacent to you" is a disk of one the text
    /// writes; "in radius" is the printed icon with Radius items (R1) - Blade Helix prints 1.</item>
    /// </list>
    /// </summary>
    public static partial class TakahideCardContent
    {
        public const string SetAnExampleText =
            "Target a unit adjacent to you. After the attack: A friendly hero in radius may swap their " +
            "unresolved card with a card in their hand.";

        public const string LeadFromTheFrontText = SetAnExampleText;

        public const string HoldMySakeText =
            "Target a unit adjacent to you. After the attack: A friendly hero in radius may swap their " +
            "unresolved card with a card in their hand, or in their discard.";

        public const string SpinningBladeText =
            "Target a unit adjacent to you. After the attack: This turn: Empty spaces adjacent to you " +
            "count as obstacles for the enemy units.";

        public const string BladeHelixText =
            "Target a unit adjacent to you. After the attack: This turn: Empty spaces in radius count " +
            "as obstacles for the enemy units.";

        public static EffectCapabilityReference GrantSpinningBladeV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("takahide-spinning-blade-obstacles"), 1);

        public static EffectCapabilityReference GrantBladeHelixV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("takahide-blade-helix-obstacles"), 1);

        /// <summary>"This turn: Empty spaces adjacent to you count as obstacles for the enemy units."</summary>
        public static ActiveEffectDefinition SpinningBladeObstacles { get; } =
            EnemyObstacles("takahide-spinning-blade-obstacles", DeclaredRadius.WrittenInText(1));

        /// <summary>"This turn: Empty spaces in radius count as obstacles for the enemy units." Radius 1 printed.</summary>
        public static ActiveEffectDefinition BladeHelixObstacles { get; } =
            EnemyObstacles("takahide-blade-helix-obstacles", DeclaredRadius.PrintedIcon(1));

        /// <summary>Red, Tier I: Initiative 11, Defense 1, Movement 1, Attack 2, Radius 3.</summary>
        public static CardDefinition SetAnExampleDefinition { get; } = RedAttack(
            SetAnExample, "Set an Example", CardTier.I, 11, defense: 1, attack: 2, radius: 3,
            PrintedValueChannel.None, new[] { LeadFromTheFront, SpinningBlade },
            FriendSwap(SetAnExample, fromDiscard: false), SetAnExampleText);

        /// <summary>Red, Tier II: Initiative 11, Defense 2, Movement 1, Attack 3, Radius 4; Initiative item.</summary>
        public static CardDefinition LeadFromTheFrontDefinition { get; } = RedAttack(
            LeadFromTheFront, "Lead from the Front", CardTier.II, 11, defense: 2, attack: 3, radius: 4,
            PrintedValueChannel.Initiative, new[] { HoldMySake, BladeHelix },
            FriendSwap(LeadFromTheFront, fromDiscard: false), LeadFromTheFrontText);

        /// <summary>Red, Tier II: Initiative 11, Defense 2, Movement 1, Attack 3, Radius 4; Defense item.</summary>
        public static CardDefinition SpinningBladeDefinition { get; } = RedAttack(
            SpinningBlade, "Spinning Blade", CardTier.II, 11, defense: 2, attack: 3, radius: 4,
            PrintedValueChannel.Defense, new[] { HoldMySake, BladeHelix },
            Obstacles(SpinningBlade, GrantSpinningBladeV1), SpinningBladeText);

        /// <summary>Red, Tier III: Initiative 12, Defense 2, Movement 1, Attack 3, Radius 4; Movement item.</summary>
        public static CardDefinition HoldMySakeDefinition { get; } = RedAttack(
            HoldMySake, "Hold my Saké", CardTier.III, 12, defense: 2, attack: 3, radius: 4,
            PrintedValueChannel.Movement, Array.Empty<CardDefinitionId>(),
            FriendSwap(HoldMySake, fromDiscard: true), HoldMySakeText);

        /// <summary>Red, Tier III: Initiative 12, Defense 2, Movement 1, Attack 3, Radius 1; Range item.</summary>
        public static CardDefinition BladeHelixDefinition { get; } = RedAttack(
            BladeHelix, "Blade Helix", CardTier.III, 12, defense: 2, attack: 3, radius: 1,
            PrintedValueChannel.Range, Array.Empty<CardDefinitionId>(),
            Obstacles(BladeHelix, GrantBladeHelixV1), BladeHelixText);

        private static CardDefinition RedAttack(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense, int attack,
            int radius, PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next,
            EffectDefinition afterTheAttack, string text) =>
            new CardDefinition(
                card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, radius)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default,
                        afterTheAttack,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                    Movement(1),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        /// <summary>
        /// "A friendly hero in radius may swap their unresolved card with a card in their hand[, or in
        /// their discard]." Takahide picks the hero (p.10 Making Choices) from those the swap can act on;
        /// the hero's <c>May</c> and card are asked of their seat. With nobody to pick, the sentence does
        /// nothing - it is after-attack text, and the Attack has already been performed (9.32b's shape).
        /// </summary>
        private static EffectDefinition FriendSwap(CardDefinitionId card, bool fromDiscard)
        {
            var root = new EffectNodeId(card.Value + "-after-the-attack");
            var friend = new EffectNodeId(card.Value + "-friendly-hero-in-radius");
            var may = new EffectNodeId(card.Value + "-may-swap");
            var swap = new EffectNodeId(card.Value + "-swap-unresolved-card");
            EffectInputBinding ChosenHero(string parameter) =>
                EffectInputBinding.FromNodeOutput(
                    parameter, friend, SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero);
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { friend, may }),
                    new EffectNodeDefinition(
                        friend, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            // "A friendly hero" never includes him (p.10 Picking Targets).
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { swap },
                        inputs: new[] { ChosenHero(EffectNodeDefinition.ChooserParameterName) },
                        chooser: EffectChooserRole.BoundHero),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticUnresolvedSwapCapabilities.SwapUnresolvedCardV1,
                        inputs: new[]
                        {
                            ChosenHero(SyntheticUnresolvedSwapCapabilities.OwnerParameterName),
                            EffectInputBinding.FromLiteral(
                                SyntheticUnresolvedSwapCapabilities.FromDiscardParameterName,
                                EffectValue.Boolean(fromDiscard))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticUnresolvedSwapCapabilities.SwappedOutOutputName, EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticUnresolvedSwapCapabilities.SwappedInOutputName, EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>The obstacle effect's one creation step, after the attack.</summary>
        private static EffectDefinition Obstacles(CardDefinitionId card, EffectCapabilityReference grant)
        {
            var root = new EffectNodeId(card.Value + "-this-turn");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, grant,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "This turn" (p.17): created after the attack and ended before the turn ends; cancelled when
        /// Takahide is defeated (p.13) or the cards are retrieved. Its disk is drawn around his current
        /// space and bars the enemy's units (TK-Q3).
        /// </summary>
        private static ActiveEffectDefinition EnemyObstacles(string id, DeclaredRadius radius) =>
            new ActiveEffectDefinition(
                id,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false,
                "TakahideEnemyObstacles",
                id,
                Array.Empty<TimingCheckpoint>(),
                null,
                new[] { ConditionalObstacleOverlay.DeclareForUnits(SourceTeamRelation.OpposingSource) },
                radius);
    }
}
