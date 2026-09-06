using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The six Active effects Silverarrow the Pathfinder's faces create - milestone
    /// 8.35z30.
    ///
    /// <para><b>Three of the six are a reuse and that is the milestone's largest single finding.</b>
    /// Grasping Roots, Entangling Vines and Grappling Branches print "<b>This turn:</b> Enemy heroes in
    /// radius cannot fast travel, or move more than 1 space with a movement action" - which is Arien's
    /// Deluge word for word, down to the punctuation. The capability-gap forecast rated the movement
    /// cap the highest-risk new work in her set, on the reading that no upper bound existed anywhere in
    /// the modification vocabulary; milestone 8.20 had already built it as
    /// <see cref="RuleModificationKind.ActivityLimit"/> for that very sentence. Nothing new was needed
    /// for three of her nineteen faces, and the three definitions below differ from each other only in
    /// the disk they are measured over.</para>
    ///
    /// <para><b>The other three are each a first.</b> Trailblazer is the first face to grant a
    /// permission its own words scope to an activity (RULING 47), and the first to draw a disk around
    /// its hero that admits heroes and its hero (
    /// <see cref="ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius"/>). Warning Shot and
    /// Treetop Sentinel are the roster's first "<b>End of turn:</b>" faces, which is
    /// <see cref="ActiveEffectDurationKind.EndOfTurn"/>.</para>
    ///
    /// <para>Each definition is Silverarrow's own rather than a reference to Arien's, for the reason
    /// his file states about Wasp's: a hero's content owns its own declarations, and two cards that
    /// happen to print one sentence today are two cards.</para>
    /// </summary>
    public static class SilverarrowActiveEffectContent
    {
        /// <summary>
        /// p.17's two ways an Active effect ends other than by its own duration - the same pair every
        /// verified hero declares, stated here because a hero's content owns its own declarations.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId GraspingRootsInRadius { get; } =
            new EffectCapabilityId("silverarrow-grasping-roots-in-radius");

        public static EffectCapabilityId EntanglingVinesInRadius { get; } =
            new EffectCapabilityId("silverarrow-entangling-vines-in-radius");

        public static EffectCapabilityId GrapplingBranchesInRadius { get; } =
            new EffectCapabilityId("silverarrow-grappling-branches-in-radius");

        public static EffectCapabilityId TrailblazerInRadius { get; } =
            new EffectCapabilityId("silverarrow-trailblazer-in-radius");

        public static EffectCapabilityId WarningShotAtEndOfTurn { get; } =
            new EffectCapabilityId("silverarrow-warning-shot-at-end-of-turn");

        public static EffectCapabilityId TreetopSentinelAtEndOfTurn { get; } =
            new EffectCapabilityId("silverarrow-treetop-sentinel-at-end-of-turn");

        public static EffectCapabilityReference GraspingRootsInRadiusV1 =>
            new EffectCapabilityReference(GraspingRootsInRadius, 1);

        public static EffectCapabilityReference EntanglingVinesInRadiusV1 =>
            new EffectCapabilityReference(EntanglingVinesInRadius, 1);

        public static EffectCapabilityReference GrapplingBranchesInRadiusV1 =>
            new EffectCapabilityReference(GrapplingBranchesInRadius, 1);

        public static EffectCapabilityReference TrailblazerInRadiusV1 =>
            new EffectCapabilityReference(TrailblazerInRadius, 1);

        public static EffectCapabilityReference WarningShotAtEndOfTurnV1 =>
            new EffectCapabilityReference(WarningShotAtEndOfTurn, 1);

        public static EffectCapabilityReference TreetopSentinelAtEndOfTurnV1 =>
            new EffectCapabilityReference(TreetopSentinelAtEndOfTurn, 1);

        /// <summary>
        /// "<b>This turn:</b> Enemy heroes in radius cannot fast travel, or move more than 1 space with
        /// a movement action." - Grasping Roots at Radius 2.
        ///
        /// <para>Read Arien's Deluge beside it: the sentence, the scope, the two declarations, the
        /// creation and expiry moments and p.11's start-of-movement reading are all his, and the only
        /// thing this card changes is the printed number. The cap is composed into the Movement Total
        /// once, when the movement action starts, because p.11 says so outright - "Effects which limit
        /// your movement distance ... are only checked at the start of the movement action" - so a hero
        /// who walks out of the disk mid-move keeps the shorter allowance they set off with.</para>
        ///
        /// <para><b>These three inherit a queued roster-wide corrective rather than introducing one.</b>
        /// RULING 15's turn-start sampling of restriction effects is not built anywhere today, and
        /// Arien's three faces print the identical sentence over the identical lifecycle. Whatever
        /// corrects his corrects these, and it is not a Silverarrow item.</para>
        /// </summary>
        public static ActiveEffectDefinition GraspingRootsDefinition { get; } =
            SlowedFootingDefinition(
                "silverarrow-grasping-roots-effect", "SilverarrowGraspingRoots",
                "silverarrow-grasping-roots", 2);

        /// <summary>Entangling Vines - the same sentence at Radius 3.</summary>
        public static ActiveEffectDefinition EntanglingVinesDefinition { get; } =
            SlowedFootingDefinition(
                "silverarrow-entangling-vines-effect", "SilverarrowEntanglingVines",
                "silverarrow-entangling-vines", 3);

        /// <summary>Grappling Branches - the same sentence at Radius 4, the widest of the three.</summary>
        public static ActiveEffectDefinition GrapplingBranchesDefinition { get; } =
            SlowedFootingDefinition(
                "silverarrow-grappling-branches-effect", "SilverarrowGrapplingBranches",
                "silverarrow-grappling-branches", 4);

        /// <summary>
        /// "<b>This round:</b> You and friendly heroes in radius may ignore obstacles while performing
        /// movement actions." - Trailblazer's Basic Skill, and the second half of its text.
        ///
        /// <para><b>Three printed clauses and each one is a declaration.</b> "You and friendly heroes in
        /// radius" is <see cref="ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius"/> paired
        /// with <see cref="SourceTeamRelation.AlliedWithSource"/> - the disk, the allies inside it, and
        /// the hero it is drawn around, who is admitted unconditionally for the reason Brogan's Bulwark
        /// admits him. "may ignore obstacles" is p.18's Move-Through permission, which the rulebook
        /// defines once and this card grants rather than describes. And "while performing movement
        /// actions" is the scope, which is the clause that had nowhere to go until this
        /// milestone.</para>
        ///
        /// <para><b>That third clause is RULING 47 and it is load-bearing.</b> The user settled that a
        /// Move written in card text is not a movement action, in both directions: a restriction on
        /// movement actions does not stop one, and a buff on movement actions does not reach one. So a
        /// hero standing in this disk crosses an obstacle while taking their Movement action and is
        /// stopped by that same obstacle a moment later when a card text moves them - which is exactly
        /// what the community answer in the designer channel's own Silverarrow thread says about this
        /// card ("Move is not the same as movement action"). Xargatha's Metamorphosis prints "You may
        /// move through obstacles" with no such clause and is therefore granted unscoped; the two
        /// permissions are one grant with and without a printed qualification, and
        /// <see cref="RuleModificationSet.TryGetPermission"/> is where the difference is read.</para>
        ///
        /// <para><b>Membership is live, per SL-07.</b> Nothing is recorded when the Skill resolves, so
        /// an ally who walks into the disk on a later turn of the same round has the permission then
        /// and loses it on walking out. That is p.17's own reading of a radius and the behaviour Wasp's
        /// Static Barrier overlay has had since milestone 8.7; what is new is only the window, because
        /// <b>This round:</b> is longer than anything Wasp's set declares.</para>
        /// </summary>
        public static ActiveEffectDefinition TrailblazerDefinition { get; } =
            new ActiveEffectDefinition(
                "silverarrow-trailblazer-effect",
                ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                Cancellations,
                false,
                "SilverarrowTrailblazer",
                "silverarrow-trailblazer",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForPermission(
                        RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement)
                },
                null,
                DeclaredRadius.PrintedIcon(4),
                SourceTeamRelation.AlliedWithSource);

        /// <summary>
        /// "<b>End of turn:</b> An enemy hero in radius discards a card, if able." - Warning Shot.
        ///
        /// <para><b>The keyword is the whole of what this definition declares.</b> It carries no
        /// modification at all: nothing about the board changes while it stands, and its only content
        /// is that a piece of work happens at the end of the turn. That is what makes it a new duration
        /// rather than a use of p.17's four - see
        /// <see cref="ActiveEffectDurationKind.EndOfTurn"/> - and it is why the scope is the controller,
        /// as Xargatha's Constrict's is: the sentence measures its disk from her, and the trigger asks
        /// the board where she is when the moment arrives.</para>
        ///
        /// <para><b>SL-06's two open questions, both answered by the shape rather than by a rule
        /// written somewhere.</b> Whose turn: the effect is created by her Skill and expires at that
        /// same turn's end, so it can only ever fire on hers. When the radius is measured: the
        /// triggered effect selects at fire time, reading her printed Radius against wherever she is
        /// standing then - so a hero who walked into the disk after the Skill resolved is a candidate
        /// and one who walked out is not. Nothing is recorded when the card resolves, which is the
        /// difference between the two readings the face leaves open.</para>
        /// </summary>
        public static ActiveEffectDefinition WarningShotDefinition { get; } =
            EndOfTurnDefinition(
                "silverarrow-warning-shot-effect",
                "SilverarrowWarningShot",
                "silverarrow-warning-shot",
                SilverarrowEffectContent.WarningShotTriggeredEffect.Reference);

        /// <summary>
        /// "<b>End of turn:</b> An enemy hero in radius discards a card, or is defeated." - Treetop
        /// Sentinel, which is Warning Shot's sentence with ledger question 13's escalation in place of
        /// its "if able".
        /// </summary>
        public static ActiveEffectDefinition TreetopSentinelDefinition { get; } =
            EndOfTurnDefinition(
                "silverarrow-treetop-sentinel-effect",
                "SilverarrowTreetopSentinel",
                "silverarrow-treetop-sentinel",
                SilverarrowEffectContent.TreetopSentinelTriggeredEffect.Reference);

        /// <summary>
        /// "cannot fast travel, or move more than 1 space with a movement action", stated once because
        /// three of her faces print one sentence and a second copy could drift from the first. It is
        /// the same pair of declarations Arien's Slippery Ground and Deluge make, and for the same
        /// printed reason: the first clause takes p.11's replacement away entirely, and the second
        /// leaves the Movement standing and caps it.
        /// </summary>
        private static IEnumerable<RuleModification> SlowedFooting => new[]
        {
            RuleModification.ForRestriction(RuleActivity.FastTravel),
            RuleModification.ForActivityLimit(RuleActivity.Movement, 1)
        };

        private static ActiveEffectDefinition SlowedFootingDefinition(
            string definitionId, string effectKind, string uniquenessKey, int printedRadius) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                effectKind,
                uniquenessKey,
                Array.Empty<TimingCheckpoint>(),
                SlowedFooting,
                null,
                DeclaredRadius.PrintedIcon(printedRadius),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// The shape both "End of turn:" faces share: created and activated by the Skill that prints
        /// it, triggering at the turn's own end, and expiring at that same moment so it fires exactly
        /// once. No radius is declared here - the effect affects nobody while it stands, and the disk
        /// its work measures is read off the printed icon by the triggered effect, where p.16's item
        /// can raise it.
        /// </summary>
        private static ActiveEffectDefinition EndOfTurnDefinition(
            string definitionId,
            string effectKind,
            string uniquenessKey,
            EffectDefinitionReference triggeredEffect) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                Cancellations,
                false,
                effectKind,
                uniquenessKey,
                new[] { TimingCheckpoint.TurnEnded },
                triggeredEffect: triggeredEffect);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(GraspingRootsInRadiusV1, GraspingRootsDefinition),
            new ActiveEffectCreationHandler(EntanglingVinesInRadiusV1, EntanglingVinesDefinition),
            new ActiveEffectCreationHandler(GrapplingBranchesInRadiusV1, GrapplingBranchesDefinition),
            new ActiveEffectCreationHandler(TrailblazerInRadiusV1, TrailblazerDefinition),
            new ActiveEffectCreationHandler(WarningShotAtEndOfTurnV1, WarningShotDefinition),
            new ActiveEffectCreationHandler(TreetopSentinelAtEndOfTurnV1, TreetopSentinelDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            GraspingRootsDefinition,
            EntanglingVinesDefinition,
            GrapplingBranchesDefinition,
            TrailblazerDefinition,
            WarningShotDefinition,
            TreetopSentinelDefinition
        });
    }
}
