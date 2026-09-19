using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's five Red faces - the Stances - as "After the attack:" bodies. Milestone
    /// 8.61c.
    ///
    /// <para><b>What the five print.</b> Every one of them opens "Target a unit adjacent to you",
    /// which is rulebook p.13's own reach for a non-Ranged Attack and therefore composes nothing at
    /// all; what this file owns is the rider each of them prints after it.
    /// <list type="bullet">
    /// <item><description><b>Crane Stance</b> (Red I, A 3): "Push an enemy unit adjacent to you up to
    /// 3 spaces."</description></item>
    /// <item><description><b>Tiger Stance</b> (Red II, A 4): "You may move 1 space to a space adjacent
    /// to the target. Push an enemy unit adjacent to you up to 3 spaces."</description></item>
    /// <item><description><b>Viper Stance</b> (Red II, A 5, r 4): "You may swap with a [Smoke bomb]
    /// Smoke bomb in radius."</description></item>
    /// <item><description><b>Dragon Stance</b> (Red III, A 4): "You may move 1 or 2 spaces to a space
    /// adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces."</description></item>
    /// <item><description><b>Cobra Stance</b> (Red III, A 5, r 4): "You may swap with a [Smoke bomb]
    /// Smoke bomb in radius; if you do, you may place the Smoke bomb into a space in
    /// radius."</description></item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Three clauses, composed once each.</b> The push is one shape printed on three faces at
    /// one number; the move is one shape printed on two faces at two distances; the swap and its
    /// re-place are milestone 8.61a's own sub-graphs, offered there precisely so this block would
    /// compose the faces rather than the piece. Nothing here is a per-card body with a shared
    /// sentence copied into it.</para>
    ///
    /// <para><b>Every rider is self-contained and reachable through the card's Attack action</b>, which
    /// is what milestone 8.61g's borrow needs: Fast as Lightning and Chop Down "Apply the 'After the
    /// attack' text of your resolved or discarded red card", so each of these bodies has to be
    /// runnable inside an Attack frame that is not its own card's. Nothing below reads the performing
    /// card except through the two channels that already answer for a borrow - the enclosing Attack's
    /// facts, and p.9's printed Radius icon - so a borrowed rider asks the same questions of a
    /// different card without this file knowing it happened.</para>
    ///
    /// <para><b>Nothing here is registered.</b> See <see cref="MinComponentContent"/>: no
    /// configured match deals Min until milestone 8.61j, and this block adds no capability handler of
    /// its own at all - the whole of its engine delta is
    /// <see cref="DirectMoveDestinationPolicy.AdjacentToTheNamedSpace"/>, a declared narrowing on a
    /// capability this build already registers.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>p.18's push distance her three pushing Reds print, all three at the same number.</summary>
        public const int StancePushDistance = 3;

        /// <summary>
        /// Which of rulebook p.14's moments a Stance's composed text is applied at. All five print
        /// "After the attack:", which is step 7 - after the target has defended or been removed.
        /// Named by the keyword the face prints rather than parsed out of the transcription, which is
        /// <c>RowennaEffectContent.AttackTextTimingFor</c>'s shape.
        /// </summary>
        public static CardTextTiming StanceTextTimingFor(CardDefinitionId cardId) =>
            IsAStance(cardId) ? CardTextTiming.AfterTheAttack : CardTextTiming.None;

        /// <summary>Whether this is one of her five Red faces.</summary>
        public static bool IsAStance(CardDefinitionId cardId) =>
            cardId.Equals(MinCardContent.CraneStance) ||
            cardId.Equals(MinCardContent.TigerStance) ||
            cardId.Equals(MinCardContent.ViperStance) ||
            cardId.Equals(MinCardContent.DragonStance) ||
            cardId.Equals(MinCardContent.CobraStance);

        /// <summary>
        /// The "After the attack:" body of one Stance, or <c>null</c> for a face this block does not
        /// own - the shape every hero's effect content in this project uses, so a card builder asks
        /// one question rather than five.
        /// </summary>
        public static EffectDefinition StanceTextFor(CardDefinitionId cardId, string revision)
        {
            if (cardId.Equals(MinCardContent.CraneStance))
            {
                return CraneStance(cardId, revision);
            }

            if (cardId.Equals(MinCardContent.TigerStance))
            {
                return TigerStance(cardId, revision);
            }

            if (cardId.Equals(MinCardContent.ViperStance))
            {
                return ViperStance(cardId, revision);
            }

            if (cardId.Equals(MinCardContent.DragonStance))
            {
                return DragonStance(cardId, revision);
            }

            if (cardId.Equals(MinCardContent.CobraStance))
            {
                return CobraStance(cardId, revision);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // The five bodies
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Crane Stance, whole: "After the attack: Push an enemy unit adjacent to you up to 3 spaces."
        /// The plainest of the five - one sentence, and the one the other two pushing faces end with.
        /// </summary>
        public static EffectDefinition CraneStance(CardDefinitionId card, string revision) =>
            new EffectDefinition(
                StanceEffectId(card), revision, Id(CranePrefix),
                PushAnAdjacentEnemyUnit(CranePrefix).ToArray());

        /// <summary>
        /// Tiger Stance: "You may move 1 space to a space adjacent to the target. Push an enemy unit
        /// adjacent to you up to 3 spaces."
        ///
        /// <para><b>The move is first because the sentence is first, and that is load-bearing.</b>
        /// p.18 measures a push directly away from the PUSHING unit, so where Min is standing when the
        /// push runs decides both who is adjacent to her and which way they go - and this card moves
        /// her before it pushes. Written the other way round it would be a different card.</para>
        /// </summary>
        public static EffectDefinition TigerStance(CardDefinitionId card, string revision) =>
            StanceWithAMove(card, revision, TigerPrefix, distance: 1, printsTwoDistances: false);

        /// <summary>
        /// Dragon Stance: "You may move 1 or 2 spaces to a space adjacent to the target. Push an enemy
        /// unit adjacent to you up to 3 spaces." Tiger's sentence with one word changed, so it is the
        /// same composition at a different number - see <see cref="MoveBesideTheTarget"/> for why that
        /// one word still costs a node.
        /// </summary>
        public static EffectDefinition DragonStance(CardDefinitionId card, string revision) =>
            StanceWithAMove(card, revision, DragonPrefix, distance: 2, printsTwoDistances: true);

        /// <summary>
        /// Viper Stance, whole: "After the attack: You may swap with a [Smoke bomb] Smoke bomb in
        /// radius."
        ///
        /// <para><b>The swap is milestone 8.61a's sub-graph and the "you may" is this card's.</b> That
        /// milestone composed the exchange as a mandatory pair - the bomb's bounded position, then
        /// p.19's Swap - deliberately without an optionality of its own, because Poof!, Vanish and Ruse
        /// print the same exchange with NO "you may" on it (8.61b). So the printed word arrives here,
        /// as the <see cref="EffectNodeKind.Optional"/> wrapper p.10 makes of it, and a bomb standing
        /// outside the printed Radius leaves the branch with nothing to do - which is a question the
        /// resolution never asks rather than an offer it withdraws.</para>
        /// </summary>
        public static EffectDefinition ViperStance(CardDefinitionId card, string revision) =>
            new EffectDefinition(
                StanceEffectId(card), revision, Id(ViperPrefix),
                new[] { May(ViperPrefix, ViperPrefix + "-exchange") }
                    .Concat(SwapWithSmokeBombInRadius(ViperPrefix + "-exchange"))
                    .ToArray());

        /// <summary>
        /// Cobra Stance, whole: "After the attack: You may swap with a [Smoke bomb] Smoke bomb in
        /// radius; if you do, you may place the Smoke bomb into a space in radius."
        ///
        /// <para><b>"If you do" is the nesting and not a flag, which is the one reading this face
        /// forced.</b> Wasp's Deflect Projectiles reads its own "if you do" by asking the enclosing
        /// Attack's printed subtype a second time, because the Block's condition is a fact about
        /// something else; here the antecedent is a step THIS text performs, and p.19's Swap publishes
        /// two coordinates and no Boolean for a <see cref="EffectNodeKind.Conditional"/> to branch on.
        /// So the second clause is the second child of the arm the first clause's "you may" governs:
        /// reaching it at all means the swap was confirmed and applied, and declining the first
        /// question skips both - which is exactly what the semicolon says. A published flag would have
        /// been a second statement of the structure rather than a second fact.</para>
        ///
        /// <para><b>The re-place is measured from her NEW space, and that is the sentence too.</b>
        /// p.19's Swap has already exchanged the two positions by the time the second clause runs, so
        /// "in radius" is read from where Min now stands - the bomb's former space - and the piece it
        /// picks up is the one she just traded with. 8.61a's re-place reads the bomb's position
        /// UNBOUNDED for exactly this reason: the radius in this clause bounds the destination, not
        /// the piece.</para>
        /// </summary>
        public static EffectDefinition CobraStance(CardDefinitionId card, string revision) =>
            new EffectDefinition(
                StanceEffectId(card), revision, Id(CobraPrefix),
                new[]
                {
                    May(CobraPrefix, CobraPrefix + "-arm"),
                    Sequence(
                        CobraPrefix + "-arm",
                        CobraPrefix + "-exchange",
                        CobraPrefix + "-replace-may"),
                    May(CobraPrefix + "-replace-may", CobraPrefix + "-replace")
                }
                .Concat(SwapWithSmokeBombInRadius(CobraPrefix + "-exchange"))
                .Concat(PlaceSmokeBombInRadius(CobraPrefix + "-replace"))
                .ToArray());

        // -----------------------------------------------------------------------------------------
        // The clauses
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The two faces that print a move before their push, composed once. The whole of their
        /// difference is how many distances the sentence names.
        /// </summary>
        private static EffectDefinition StanceWithAMove(
            CardDefinitionId card,
            string revision,
            string prefix,
            int distance,
            bool printsTwoDistances) =>
            new EffectDefinition(
                StanceEffectId(card), revision, Id(prefix),
                new[] { Sequence(prefix, prefix + "-move", prefix + "-push") }
                    .Concat(MoveBesideTheTarget(prefix + "-move", distance, printsTwoDistances))
                    .Concat(PushAnAdjacentEnemyUnit(prefix + "-push"))
                    .ToArray());

        /// <summary>
        /// "Push an enemy unit adjacent to you up to 3 spaces" - Crane, Tiger and Dragon Stance.
        ///
        /// <para><b>The subject is chosen and the push is p.18's named one</b>, which is Cutter's Walk
        /// the Plank shape and for her reason: p.18's BATCH push takes every object standing beside the
        /// pusher and narrows them by side and by count, and this sentence names ONE unit. So a
        /// radius-1 <c>choose-target@2</c> names it and <c>synthetic-push-target@3</c> pushes the space
        /// that selection published. The reach is the literal 1 the sentence writes rather than a
        /// printed icon: Crane, Tiger and Dragon print no Range and no Radius at all, which is
        /// milestone 8.57c's correction - "adjacent to you" is a word, and p.16's items raise icons.
        /// </para>
        ///
        /// <para><b>The step is mandatory and the DISTANCE is what "up to" makes optional.</b> p.10
        /// names exactly three non-mandatory wordings - "you may", "up to" and "if able" - and this
        /// sentence carries the second of them over the spaces alone, so the selection above is
        /// mandatory and the push itself carries <see cref="EffectFailurePolicy.UpTo"/> with a floor of
        /// nought: Min may push the unit she names anywhere from nowhere to three spaces, which is
        /// p.18's own "this can result in pushing it 0 spaces". With no enemy unit standing beside her
        /// there is nobody to name and the mandatory selection is p.10's impossible step, which stops
        /// the text where it stands - and on all three faces this clause is the last thing printed, so
        /// nothing is lost by stopping there.</para>
        ///
        /// <para><b>"Enemy unit" is p.19's word for both kinds</b> - a hero or a minion on the
        /// opposing side - and the immunity question is asked as <see cref="RuleActivity.ObjectPush"/>
        /// rather than as the enclosing Attack's activity, because what a named subject is about to
        /// have done to it is a push. A unit immune to being pushed is therefore not offered rather
        /// than offered and refused.</para>
        /// </summary>
        private static IReadOnlyList<EffectNodeDefinition> PushAnAdjacentEnemyUnit(string prefix)
        {
            var root = Require(prefix);
            return new[]
            {
                Sequence(root, root + "-subject", root + "-push"),
                new EffectNodeDefinition(
                    Id(root + "-subject"),
                    EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Unit),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.ObjectPush)
                    },
                    outputs: new[]
                    {
                        Output(SyntheticRosterCapabilities.ChosenSpaceOutput,
                            EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    Id(root + "-push"),
                    EffectNodeKind.Capability,
                    EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.PushTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "target", Id(root + "-subject"),
                            SyntheticRosterCapabilities.ChosenSpaceOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral(
                            "distance", EffectValue.Integer(StancePushDistance)),

                        // "up to 3 spaces" names one end of the range and p.10 supplies the other, so
                        // the printed floor is nought rather than a second number the card prints.
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                            EffectValue.Integer(0)),

                        // None of the three faces says anything about obstacles, and p.18 makes
                        // crossing a property of the pushing effect rather than a default.
                        EffectInputBinding.FromLiteral(
                            "ignoresObstacles", EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            PushEligiblePieceKind.Unit)
                    },
                    outputs: new[] { Output("distance", EffectValueKind.Integer) })
            };
        }

        /// <summary>
        /// "You may move 1 space [1 or 2 spaces] to a space adjacent to the target" - Tiger and Dragon
        /// Stance.
        ///
        /// <para><b>It is a p.11 card-text move and never a Place or a Fast Travel.</b> The page says
        /// so in as many words - card-text movement "follows the same rules and restrictions as a
        /// Movement action, except: it cannot be used to Fast Travel" - so Min walks: obstacles stop
        /// her, occupied spaces are not destinations, and <c>ignoresObstacles</c> is not declared
        /// because nothing on either face mentions crossing anything. The clause narrows only where she
        /// may END UP, which is
        /// <see cref="DirectMoveDestinationPolicy.AdjacentToTheNamedSpace"/> - minted at this milestone
        /// because a Place qualifier of the same words existed and a Move policy did not.</para>
        ///
        /// <para><b>A removed target is p.10's stop and it is asked before anything else.</b> "The
        /// target" is the unit the enclosing Attack took, and p.14 step 6 can have taken it off the
        /// board before step 7 runs this - so the clause has no referent and the step simply does not
        /// happen, while the push that follows is a separate sentence and still does.
        /// <c>attack-target-survived@1</c> publishes the answer and a
        /// <see cref="EffectNodeKind.Conditional"/> over <c>synthetic-is-flagged@1</c> gates the move
        /// on it, which is Cutter's own zone-crossing shape and milestone 8.40c's rule about a
        /// Conditional whose consequence is about the thing the question is about. Without the gate the
        /// space the target VACATED would still be published - <c>attacked-target-space@1</c> captures
        /// it at validation precisely so a rider can name it after a defeat - and Min would be offered
        /// a ring around a corpse.</para>
        ///
        /// <para><b>The space is the one the Attack bound, and for a surviving target that is where it
        /// is standing.</b> An Attack does not move its defender, and no shipped Defense text relocates
        /// one; the only printed clause in this project that will is Min's own Poof!, Vanish and Ruse
        /// (8.61b), and no match fields two Mins. So reading the captured space rather than re-deriving
        /// the target's live position asks the same question with one fact instead of two - and it is
        /// the same fact Brogan's Onslaught and Sabina's Point Blank Shot already read at this step.
        /// </para>
        ///
        /// <para><b>Why the two faces are two shapes.</b> Tiger names ONE distance, so its move is a
        /// single <see cref="EffectFailurePolicy.May"/> node - Tigerclaw's five "you may move 1 space"
        /// faces exactly - where p.11 leaves the number exact and the step optional. Dragon names TWO,
        /// and <see cref="AdmittedDistances"/> makes the distance a decision only under
        /// <see cref="EffectFailurePolicy.UpTo"/>, which is not a wording that can also be declined. So
        /// Dragon's "you may" becomes the <see cref="EffectNodeKind.Optional"/> wrapper p.10 makes of
        /// it and "1 or 2" becomes an <c>up to</c> with the printed floor of one that milestone 8.8
        /// added for Kinetic Blast's "3 or 4 spaces" - the two halves of the sentence carried by the
        /// two declarations that state them, rather than one node meaning both.</para>
        ///
        /// <para><b>Boxed in is a decline and not a stop.</b> A move with no legal destination reports
        /// itself impossible, and a node that is not mandatory turns that into p.10's skip - so the
        /// push still follows, on both faces and by construction.</para>
        /// </summary>
        private static IReadOnlyList<EffectNodeDefinition> MoveBesideTheTarget(
            string prefix, int distance, bool printsTwoDistances)
        {
            var root = Require(prefix);
            var move = new EffectNodeDefinition(
                Id(root + "-step"),
                EffectNodeKind.Capability,
                printsTwoDistances ? EffectFailurePolicy.UpTo : EffectFailurePolicy.May,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                    {
                        From("mover", root + "-actor-space", SpaceOutput),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.AdjacentToTheNamedSpace),
                        From(
                            SyntheticDisplacementCapabilities.BesideSpaceParameterName,
                            root + "-target-space", SpaceOutput)
                    }
                    .Concat(printsTwoDistances
                        ? new[]
                        {
                            // Milestone 8.8's printed floor: "1 or 2 spaces" writes both ends of one
                            // range, so the declaration writes both too.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(1))
                        }
                        : Array.Empty<EffectInputBinding>()),
                outputs: new[] { Output("moved", EffectValueKind.Coordinate) });

            var arm = printsTwoDistances
                ? new[]
                {
                    Sequence(
                        root + "-arm", root + "-target-space", root + "-actor-space",
                        root + "-may"),
                    May(root + "-may", root + "-step")
                }
                : new[]
                {
                    Sequence(
                        root + "-arm", root + "-target-space", root + "-actor-space",
                        root + "-step")
                };

            return new[]
                {
                    Sequence(root, root + "-survived", root + "-gate"),
                    new EffectNodeDefinition(
                        Id(root + "-survived"),
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticAttackTargetSurvivalCapabilities.AttackTargetSurvivedV1,
                        outputs: new[]
                        {
                            Output(
                                SyntheticAttackTargetSurvivalCapabilities.TargetSurvivedOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        Id(root + "-gate"),
                        EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { Id(root + "-arm") },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", Id(root + "-survived"),
                                SyntheticAttackTargetSurvivalCapabilities.TargetSurvivedOutput,
                                EffectValueKind.Boolean)
                        },

                        // A predicate that publishes its answer must have the node declare it, or a
                        // restore re-deriving the recorded outcome finds an output the node does not
                        // admit and refuses the payload - milestone 8.11's finding on Thunder
                        // Boomerang's own conditional.
                        outputs: new[] { Output("value", EffectValueKind.Boolean) }),
                    new EffectNodeDefinition(
                        Id(root + "-target-space"),
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[] { Output(SpaceOutput, EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        Id(root + "-actor-space"),
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { Output(SpaceOutput, EffectValueKind.Coordinate) })
                }
                .Concat(arm)
                .Concat(new[] { move })
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.46a's words for every control her Stances raise - what pressing it does, and
        /// what declining does.
        ///
        /// <para><b>Four controls on three faces, and Tiger raises none.</b> The requirement is the
        /// resolver's own rule rather than a list: a node opens p.10's confirm-or-decline when it is an
        /// <see cref="EffectNodeKind.Optional"/> that is not `if able`, which here is Viper's and
        /// Cobra's "you may swap", Cobra's "you may place" and Dragon's "you may move". Tiger's move is
        /// a CAPABILITY node carrying `may` - a step that opens its own destination prompt and may be
        /// declined there - so it raises no separate control and owes no words, exactly as
        /// Silverarrow's whole set does.</para>
        ///
        /// <para>Declared in this block's own partial and wired into <c>DecisionLabelCatalog</c> by the
        /// milestone that deals her, for the reason every contribution in that file joins at a dealing:
        /// what a table is FOR is a window over a card a match deals.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> StanceDecisionLabels()
        {
            yield return DecisionLabelDeclaration.DecisionStep(
                MinCardContent.ViperStance, ViperPrefix,
                "Swap with your smoke bomb", "Stay where you are");
            yield return DecisionLabelDeclaration.DecisionStep(
                MinCardContent.CobraStance, CobraPrefix,
                "Swap with your smoke bomb", "Stay where you are");
            yield return DecisionLabelDeclaration.DecisionStep(
                MinCardContent.CobraStance, CobraPrefix + "-replace-may",
                "Throw the smoke bomb again", "Leave the smoke bomb");
            yield return DecisionLabelDeclaration.DecisionStep(
                MinCardContent.DragonStance, DragonPrefix + "-move-may",
                "Step around the target", "Hold your ground");
        }

        // -----------------------------------------------------------------------------------------
        // Shared spellings
        // -----------------------------------------------------------------------------------------

        private const string CranePrefix = "crane";
        private const string TigerPrefix = "tiger";
        private const string ViperPrefix = "viper";
        private const string DragonPrefix = "dragon";
        private const string CobraPrefix = "cobra";

        /// <summary>
        /// The identity of one Stance's "After the attack:" body. It carries the card's own id, so the
        /// five definitions are five rather than one shape referenced five times - which is what lets a
        /// paused resolution name the face it is inside.
        /// </summary>
        private static EffectDefinitionId StanceEffectId(CardDefinitionId card) =>
            new EffectDefinitionId(card.Value + "-attack-text");

        /// <summary>
        /// p.10's "you may": one child, the actor's decision, and declining costs nothing that
        /// follows. The chooser is <see cref="EffectChooserRole.Actor"/> because every sentence here
        /// says "you".
        /// </summary>
        private static EffectNodeDefinition May(string id, string child) =>
            new EffectNodeDefinition(
                Id(id),
                EffectNodeKind.Optional,
                EffectFailurePolicy.May,
                childNodeIds: new[] { Id(child) },
                chooser: EffectChooserRole.Actor);
    }
}
