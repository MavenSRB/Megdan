using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114c. Wuk's Blue retrieval faces.
    ///
    /// <para><b>"Remove a Tree token in radius."</b> is the first sentence of all three and a
    /// MANDATORY step (p.10): the printed-Radius object selection narrowed to Wuk's Trees (8.88's
    /// <c>tokenKind</c> filter on the object arm, with p.16's Radius items), then
    /// <c>synthetic-remove@2</c>. With no Tree in reach the face can do nothing and is not offered,
    /// and nothing after the removal happens without it.</para>
    ///
    /// <para><b>"You may retrieve a discarded card."</b> is Rock Solid's composition: an optional
    /// wrapper over <c>retrieve-card@1</c> naming Wuk as owner and chooser. <b>"A friendly hero in
    /// radius may retrieve a discarded card."</b> is Silverarrow's Blessing composition: the printed-
    /// Radius hero selection on Wuk's side (p.17: a friendly hero is never Wuk himself), then the
    /// retrieval with that hero as owner and chooser, its "may" theirs. Tree of Plenty takes one
    /// bullet; Abundance's "Choose one or both" takes one or both, and the two bullets name
    /// different heroes by construction.</para>
    ///
    /// <para>Claim and Assert Dominance are composed by this block's second half. Nothing here is
    /// registered until 8.114f deals him.</para>
    /// </summary>
    public static class WukBlueFaces
    {
        public static CardDefinitionId GiftsOfNature { get; } = WukCardContent.Id("gifts-of-nature");
        public static CardDefinitionId TreeOfPlenty { get; } = WukCardContent.Id("tree-of-plenty");
        public static CardDefinitionId ClaimDominance { get; } = WukCardContent.Id("claim-dominance");
        public static CardDefinitionId Abundance { get; } = WukCardContent.Id("abundance");
        public static CardDefinitionId AssertDominance { get; } = WukCardContent.Id("assert-dominance");

        public const string GiftsText =
            "Remove a Tree token in radius. You may retrieve a discarded card.";

        public const string PlentyText =
            "Remove a Tree token in radius. Choose one - • You may retrieve a discarded card. • A " +
            "friendly hero in radius may retrieve a discarded card.";

        public const string AbundanceText =
            "Remove a Tree token in radius. Choose one or both - • You may retrieve a discarded card. " +
            "• A friendly hero in radius may retrieve a discarded card.";

        public const string TreeNode = "blue-tree";
        public const string ModalNode = "blue-modal";
        public const string SelfBulletNode = "blue-self-bullet";
        public const string FriendBulletNode = "blue-friend-bullet";
        public const string FriendSelectNode = "blue-friend-select";

        /// <summary>The three retrieval faces, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> RetrievalDefinitions => LazyRetrieval.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyRetrieval =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Face(GiftsOfNature, "Gifts of Nature", CardTier.I, 8, 5, 3, PrintedValueChannel.None,
                    new[] { TreeOfPlenty, ClaimDominance }, 0, GiftsText),
                Face(TreeOfPlenty, "Tree of Plenty", CardTier.II, 9, 6, 4, PrintedValueChannel.Attack,
                    new[] { Abundance, AssertDominance }, 1, PlentyText),
                Face(Abundance, "Abundance", CardTier.III, 9, 6, 4, PrintedValueChannel.Range,
                    Array.Empty<CardDefinitionId>(), 2, AbundanceText)
            }));

        /// <param name="bullets">0 for Gifts' single sentence, else the modal's most selections.</param>
        private static CardDefinition Face(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense,
            int radius, PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, int bullets,
            string text) =>
            new CardDefinition(
                card, WukCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        WukCardContent.PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        Skill(card, bullets)),
                    WukCardContent.Movement(2),
                    WukCardContent.Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition Skill(CardDefinitionId card, int bullets)
        {
            var root = new EffectNodeId("blue-root");
            var tree = new EffectNodeId(TreeNode);
            var remove = new EffectNodeId("blue-remove");
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    tree, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                            PrintedIconObjectKind.TokensOnly),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.TokenKindParameterName,
                            EffectValue.TokenKind(WukComponentContent.TreeId)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(remove))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.RemoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("subject", tree, "chosen", EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            RemoveEligiblePieceKind.Token)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                        new EffectOutputBinding(RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                    })
            };

            var selfBullet = new EffectNodeId(SelfBulletNode);
            nodes.AddRange(SelfRetrieval(selfBullet));
            EffectNodeId afterRemoval;
            if (bullets == 0)
            {
                afterRemoval = selfBullet;
            }
            else
            {
                var modal = new EffectNodeId(ModalNode);
                var friendBullet = new EffectNodeId(FriendBulletNode);
                nodes.AddRange(FriendRetrieval(friendBullet));
                nodes.Add(new EffectNodeDefinition(
                    modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { selfBullet, friendBullet },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: bullets));
                afterRemoval = modal;
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { tree, remove, afterRemoval }));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), WukCardContent.ContentRevision, root, nodes);
        }

        /// <summary>"You may retrieve a discarded card." - Rock Solid's optional retrieval.</summary>
        private static IEnumerable<EffectNodeDefinition> SelfRetrieval(EffectNodeId bullet)
        {
            var retrieve = new EffectNodeId("blue-self-retrieve");
            yield return new EffectNodeDefinition(
                bullet, EffectNodeKind.Optional, EffectFailurePolicy.May,
                childNodeIds: new[] { retrieve }, chooser: EffectChooserRole.Actor);
            yield return new EffectNodeDefinition(
                retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.RetrieveCardV1,
                inputs: new[]
                {
                    EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                    EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                    EffectInputBinding.FromClosedVocabulary("kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                },
                outputs: new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) });
        }

        /// <summary>
        /// "A friendly hero in radius may retrieve a discarded card." - Silverarrow's Blessing: the
        /// hero is chosen, then they retrieve from their own pile and choose the card.
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> FriendRetrieval(EffectNodeId bullet)
        {
            var select = new EffectNodeId(FriendSelectNode);
            var retrieve = new EffectNodeId("blue-friend-retrieve");
            yield return new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { select, retrieve });
            yield return new EffectNodeDefinition(
                select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                        SourceTeamRelation.AlliedWithSource),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(retrieve))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                });
            yield return new EffectNodeDefinition(
                retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                SyntheticRosterCapabilities.RetrieveCardV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "owner", select, SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        "chooser", select, SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    EffectInputBinding.FromClosedVocabulary("kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("retrieved", EffectValueKind.CardInstance),
                    new EffectOutputBinding(SyntheticRosterCapabilities.RetrievedByOutputName, EffectValueKind.Hero)
                });
        }
    }
}
