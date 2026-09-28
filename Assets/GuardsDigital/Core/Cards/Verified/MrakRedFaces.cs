using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.112c. Mrak's five Red faces. Every one prints the roster's first secondary Attack
    /// (8.112a) on its rail.
    ///
    /// <para><b>The Seismic line</b> - Seismic Slam, Seismic Assault, Epicentre: "An enemy hero in
    /// radius adjacent to terrain, or to a rock token, discards a card, or is defeated." 8.88's
    /// printed-Radius hero selection with both adjacency kinds, then p.10's two-armed step
    /// (`discard-chosen-card@2` and its inability flag), Epicentre adding "Repeat once on a different
    /// target" - p.10's optional repeat, excluding the hero the first instance named.</para>
    ///
    /// <para><b>Stone Carapace and Rock Solid</b> - a Movement 4 primary whose text, p.11's default,
    /// comes into effect before the movement: "This round: If you would discard a card from your hand,
    /// you may discard this card instead; you may discard this card to perform its defense action, as
    /// if it was in your hand." One This round effect grants both clauses as permissions naming this
    /// card (user rulings MR-Q4, MR-Q5, MR-Q6), and p.17 ends it the moment the card changes state.
    /// Rock Solid first prints "You may retrieve a discarded card."</para>
    /// </summary>
    public static class MrakRedFaces
    {
        public static CardDefinitionId SeismicSlam { get; } = Id("seismic-slam");
        public static CardDefinitionId SeismicAssault { get; } = Id("seismic-assault");
        public static CardDefinitionId StoneCarapace { get; } = Id("stone-carapace");
        public static CardDefinitionId Epicentre { get; } = Id("epicentre");
        public static CardDefinitionId RockSolid { get; } = Id("rock-solid");

        public static ActionDefinitionId PrimaryMovement { get; } = new ActionDefinitionId("primary-movement");

        public const string SeismicText =
            "An enemy hero in radius adjacent to terrain, or to a rock token, discards a card, or is " +
            "defeated.";

        public const string CarapaceClause =
            "This round: If you would discard a card from your hand, you may discard this card " +
            "instead; you may discard this card to perform its defense action, as if it was in your hand.";

        public static IReadOnlyList<CardDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            Seismic(SeismicSlam, "Seismic Slam", CardTier.I, 7, 7, 6, 3, PrintedValueChannel.None,
                new[] { SeismicAssault, StoneCarapace }, false,
                SeismicText + " (The secondary Attack action is non-ranged)"),
            Seismic(SeismicAssault, "Seismic Assault", CardTier.II, 7, 8, 7, 4,
                PrintedValueChannel.Initiative, new[] { Epicentre, RockSolid }, false, SeismicText),
            Carapace(StoneCarapace, "Stone Carapace", CardTier.II, 7, PrintedValueChannel.Defense,
                new[] { Epicentre, RockSolid }, false),
            Seismic(Epicentre, "Epicentre", CardTier.III, 8, 8, 7, 4, PrintedValueChannel.Range,
                Array.Empty<CardDefinitionId>(), true,
                SeismicText + " Repeat once on a different target."),
            Carapace(RockSolid, "Rock Solid", CardTier.III, 8, PrintedValueChannel.Defense,
                Array.Empty<CardDefinitionId>(), true)
        });

        // -------------------------------------------------------------------------------------------
        // The This round grant
        // -------------------------------------------------------------------------------------------

        public static EffectCapabilityReference CarapaceGrantFor(CardDefinitionId card) =>
            new EffectCapabilityReference(new EffectCapabilityId(card.Value + "-substitution"), 1);

        public static ActiveEffectDefinition CarapaceGrantDefinitionFor(CardDefinitionId card) =>
            new ActiveEffectDefinition(
                card.Value + "-substitution-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved,
                    ActiveEffectCancellationCondition.SourceCardStateChanged
                },
                false,
                "MrakCarapace",
                card.Value + "-substitution",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForPermission(RulePermissionKind.DiscardSourceCardInsteadOfHandCard),
                    RuleModification.ForPermission(RulePermissionKind.DefendWithSourceCardAsIfInHand)
                });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActiveEffectCreationHandler(
                    CarapaceGrantFor(StoneCarapace), CarapaceGrantDefinitionFor(StoneCarapace)),
                new ActiveEffectCreationHandler(
                    CarapaceGrantFor(RockSolid), CarapaceGrantDefinitionFor(RockSolid))
            };

        public static IEnumerable<ActiveEffectDefinition> ActiveEffectDefinitions() =>
            new[]
            {
                CarapaceGrantDefinitionFor(StoneCarapace),
                CarapaceGrantDefinitionFor(RockSolid)
            };

        // -------------------------------------------------------------------------------------------
        // Faces
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Seismic(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense,
            int secondaryAttack, int radius, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> next, bool repeats, string text) =>
            new CardDefinition(
                card, MrakCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        MrakCardContent.PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        SeismicBody(card, repeats)),
                    Movement(3),
                    MrakCardContent.SecondaryAttack(secondaryAttack),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static CardDefinition Carapace(
            CardDefinitionId card, string title, CardTier tier, int initiative,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, bool retrieves) =>
            new CardDefinition(
                card, MrakCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        CarapaceBody(card, retrieves),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheMovement),
                    MrakCardContent.SecondaryAttack(7),
                    Defense(6)
                },
                null, CardSourceContent.Transcribed(
                    retrieves ? "You may retrieve a discarded card. " + CarapaceClause : CarapaceClause));

        private static EffectDefinition SeismicBody(CardDefinitionId card, bool repeats)
        {
            var root = new EffectNodeId("seismic-root");
            var select = new EffectNodeId("seismic-select");
            var discard = new EffectNodeId("seismic-discard");
            var unable = new EffectNodeId("seismic-if-unable");
            var defeat = new EffectNodeId("seismic-defeat");
            var repeat = new EffectNodeId("seismic-repeat");
            var hero = new[]
            {
                EffectInputBinding.FromNodeOutput(
                    SyntheticCardStateCapabilities.HeroParameter, select,
                    SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
            };
            var children = new List<EffectNodeId> { select, discard, unable };
            if (repeats) children.Add(repeat);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.AdjacentToTerrainParameterName,
                            EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName,
                            EffectValue.TokenKind(MrakComponentContent.RockId))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2, inputs: hero,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.UnableToDiscardOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    unable, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard, SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }),
                new EffectNodeDefinition(
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, select,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    })
            };
            if (repeats)
            {
                // Milestone 9.1, ruling W1: "Repeat once on a different target" prints no "may".
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    maximumRepeats: 1,
                    repeatTargetBinding: RepeatTargetBinding.DifferentSelectionSubject,
                    boundSelectionNodeId: select));
            }

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), MrakCardContent.ContentRevision,
                root, nodes);
        }

        private static EffectDefinition CarapaceBody(CardDefinitionId card, bool retrieves)
        {
            var root = new EffectNodeId("carapace-root");
            var mayRetrieve = new EffectNodeId("carapace-may-retrieve");
            var retrieve = new EffectNodeId("carapace-retrieve");
            var grant = new EffectNodeId("carapace-grant");
            var children = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            if (retrieves)
            {
                children.Add(mayRetrieve);
                nodes.Add(new EffectNodeDefinition(
                    mayRetrieve, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { retrieve }, chooser: EffectChooserRole.Actor));
                nodes.Add(new EffectNodeDefinition(
                    retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.RetrieveCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                    },
                    outputs: new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) }));
            }

            children.Add(grant);
            nodes.Add(new EffectNodeDefinition(
                grant, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                CarapaceGrantFor(card),
                outputs: new[]
                {
                    new EffectOutputBinding(ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                }));
            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: children));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-movement-text"), MrakCardContent.ContentRevision,
                root, nodes);
        }

        private static ActionDefinition Movement(int value) => new ActionDefinition(
            MrakCardContent.SecondaryMovement, ActionKind.Movement,
            ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        private static ActionDefinition Defense(int value) => new ActionDefinition(
            MrakCardContent.SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        private static CardDefinitionId Id(string card) =>
            new CardDefinitionId(MrakCardContent.ContentSetId + "-" + card);
    }
}
