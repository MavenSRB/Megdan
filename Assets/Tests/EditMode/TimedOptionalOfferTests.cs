using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.29e: what the configured roster offers at rulebook p.14's two attacker moments and
    /// at p.11's own moment before a Movement.
    ///
    /// <para>A player met the defect at Arien's Noble Blade: "Before the attack: You may move another
    /// unit that is adjacent to the target 1 space" opened p.10's confirm-or-decline with no such unit
    /// on the board, at the one moment every other control is withheld, and both answers left the same
    /// board. The <see cref="EffectNodeKind.Optional"/> node paused before anything asked what the
    /// branch behind it could do - which is the question milestone 6.5ac already asks of every option
    /// of a <c>Choose</c>.</para>
    ///
    /// <para>The matrix below is why this is closed once rather than per card. Every optional surface
    /// the six dealt heroes print at those moments is enumerated from the content itself and paired
    /// with the gate that decides whether it is offered, so a seventh hero's text arrives as a new row
    /// rather than as a new dead prompt - and the rows that were already honest are recorded as such
    /// instead of being re-closed.</para>
    /// </summary>
    public sealed class TimedOptionalOfferTests
    {
        /// <summary>
        /// p.14's step 1 and step 7, plus the two moments a Movement's own text is applied at. p.14's
        /// step 4 is the defender's own card and is not a moment the attacker's text reaches, so it is
        /// not one of these.
        /// </summary>
        private static readonly CardTextTiming[] TimedMoments =
        {
            CardTextTiming.BeforeTheAttack,
            CardTextTiming.AfterTheAttack,
            CardTextTiming.AroundTheAction,
            CardTextTiming.BeforeTheMovement
        };

        /// <summary>
        /// Every optional surface the six dealt heroes print at a timed moment, and what refuses it
        /// when it can do nothing. Read from the configured content, so a face that gains, loses or
        /// re-declares one moves a row here.
        ///
        /// <para>Four gates and no fifth. The four rows the offer gate closes are the two printed
        /// sentences milestone 8.29e was reported for, one of them on three faces; the rest were
        /// already honest, and saying so is the point of listing them - an <c>Optional</c> control
        /// node was the only shape whose offer never consulted its own branch.</para>
        ///
        /// <para><b>Milestone 8.83 added THREE rows and closed none of them, which is the shape a
        /// correction takes here.</b> Charm, Control and Dominate print "you may move an enemy minion
        /// in radius UP TO 2 spaces" and declared their move <c>Mandatory</c> at an exact 2, so it was
        /// not an optional surface at all and this matrix could not see it. It is
        /// <c>Capability/UpTo</c> now - p.11's optional distance - and it arrives already gated by the
        /// same capability <c>Anticipate</c> that has answered every other direct Move here since
        /// 8.29e. The `you may` beside it is the <c>green-may</c> row above and is untouched.</para>
        /// </summary>
        private static readonly string[] DeclaredTimedOptionalSurfaces =
        {
            "arien-noble-blade | BeforeTheAttack | noble-may | Optional/May | offer gate",
            "arien-noble-blade | BeforeTheAttack | noble-move | Capability/IfAble | attempted, never asked",
            "arien-rogue-wave | AfterTheAttack | wave-push | Capability/UpTo | the capability's own Anticipate",
            "arien-tidal-blast | AfterTheAttack | wave-push | Capability/UpTo | the capability's own Anticipate",
            "arien-violent-torrent | BeforeTheAttack | current-repeat | RepeatAction/May | repeat gate",
            "brogan-throwing-axe | BeforeTheAttack | throw-mode | Choose/Mandatory | option gate",
            "brogan-throwing-axe | BeforeTheAttack | throw-discard | Capability/May | the capability's own Anticipate",
            "brogan-throwing-spear | BeforeTheAttack | throw-mode | Choose/Mandatory | option gate",
            "brogan-throwing-spear | BeforeTheAttack | throw-discard | Capability/May | the capability's own Anticipate",
            "sabina-bullet-hell | AfterTheAttack | shootout-targets | Capability/UpTo | the capability's own Anticipate",
            "sabina-bullet-hell | AfterTheAttack | shootout-remove | Capability/IfAble | attempted, never asked",
            "sabina-bullet-hell | AfterTheAttack | shootout-remove-second | Capability/IfAble | attempted, never asked",
            "sabina-shootout | AfterTheAttack | shootout-targets | Capability/UpTo | the capability's own Anticipate",
            "sabina-shootout | AfterTheAttack | shootout-remove | Capability/IfAble | attempted, never asked",
            "tigerclaw-combat-reflexes | BeforeTheAttack | before-move | Capability/May | the capability's own Anticipate",
            "tigerclaw-combat-reflexes | AfterTheAttack | reflexes-move | Capability/May | the capability's own Anticipate",
            "tigerclaw-hit-and-run | AfterTheAttack | after-move | Capability/May | the capability's own Anticipate",
            "tigerclaw-leaping-strike | BeforeTheAttack | before-move | Capability/May | the capability's own Anticipate",
            "tigerclaw-leaping-strike | AfterTheAttack | after-move | Capability/May | the capability's own Anticipate",
            "wasp-electrocute | AfterTheAttack | radius-discard-discard | Capability/IfAble | attempted, never asked",
            "wasp-shock | AfterTheAttack | radius-discard-discard | Capability/IfAble | attempted, never asked",
            "wasp-thunder-boomerang | AfterTheAttack | thunder-repeat | RepeatAction/May | repeat gate",
            "xargatha-charm | AroundTheAction | green-order | Choose/Mandatory | option gate",
            "xargatha-charm | AroundTheAction | green-may | Optional/May | offer gate",
            "xargatha-charm | AroundTheAction | green-move | Capability/UpTo | the capability's own Anticipate",
            "xargatha-cleave | AfterTheAttack | repeat | RepeatAction/May | repeat gate",
            "xargatha-control | AroundTheAction | green-order | Choose/Mandatory | option gate",
            "xargatha-control | AroundTheAction | green-may | Optional/May | offer gate",
            "xargatha-control | AroundTheAction | green-move | Capability/UpTo | the capability's own Anticipate",
            "xargatha-dominate | AroundTheAction | green-order | Choose/Mandatory | option gate",
            "xargatha-dominate | AroundTheAction | green-may | Optional/May | offer gate",
            "xargatha-dominate | AroundTheAction | green-move | Capability/UpTo | the capability's own Anticipate",
            "xargatha-rapid-thrusts | AfterTheAttack | repeat | RepeatAction/May | repeat gate"
        };

        /// <summary>
        /// Milestone 8.29e's closing check over the configured roster. The matrix is derived from the
        /// dealt content rather than listed by hand, so a face whose timed text gains an optional
        /// surface fails here instead of shipping a prompt nobody judged.
        ///
        /// <para>A table alone would only say the surfaces were sorted, so the two Arien faces the
        /// defect was reported on are played here as well: the one whose gate this milestone added
        /// and the one whose capability already had it. The rest of the matrix is proved where it
        /// belongs - by the gate each row names, in that gate's own milestone.</para>
        /// </summary>
        [Test]
        public void ConfiguredTimedOptionalEffects_OfferOnlyPerformableBranches()
        {
            Assert.That(TimedOptionalSurfaces(), Is.EqualTo(DeclaredTimedOptionalSurfaces),
                "Every optional surface printed at a timed moment is closed by one of four gates, " +
                "and a new one is a decision rather than a default.");

            Assert.That(NobleBladeAloneWithItsTarget().Choice, Is.Null,
                "Noble Blade's step-1 `may` has no unit to move, so no offer stands - the row " +
                "above says the offer gate closes it, and this is that row happening.");
            Assert.That(RogueWaveWithNobodyBesideHim().Choice, Is.Null,
                "and Rogue Wave's step-7 `up to` has nobody to push, which its own capability " +
                "already answered before this milestone.");
        }

        /// <summary>
        /// The `Optional` control node is the only shape in the roster whose offer used to be made
        /// without judging its branch, and this is what says so: the four gates are exhaustive over
        /// the matrix, and the two printed sentences that reach the one milestone 8.29e added are
        /// named here rather than counted.
        /// </summary>
        [Test]
        public void TheOfferGate_ReachesExactlyTheOptionalControlNodesOfTheRoster()
        {
            Assert.That(
                TimedOptionalSurfaces().Where(row => row.EndsWith("| offer gate")).ToArray(),
                Is.EqualTo(new[]
                {
                    "arien-noble-blade | BeforeTheAttack | noble-may | Optional/May | offer gate",
                    "xargatha-charm | AroundTheAction | green-may | Optional/May | offer gate",
                    "xargatha-control | AroundTheAction | green-may | Optional/May | offer gate",
                    "xargatha-dominate | AroundTheAction | green-may | Optional/May | offer gate"
                }),
                "An `if able` Optional never asked, and every other surface is a capability, a " +
                "Choose or a Repeat whose own gate already read the state.");
        }

        // -------------------------------------------------------------------------------------
        // Arien, who is where the defect was met
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Noble Blade's step-1 offer, on the two boards that decide it. Nothing about the Attack
        /// changes: the target is legal on both, and it is the branch that is judged.
        /// </summary>
        [Test]
        public void AriensBeforeAttackMove_IsOfferedOnlyWhereAnotherAdjacentUnitStands()
        {
            var alone = NobleBladeAloneWithItsTarget();

            Assert.That(alone.Choice, Is.Null,
                "Nothing but the target itself stands beside the target, and Arien is never " +
                "'another unit', so p.10's confirm-or-decline has nothing to be about.");
            Assert.That(alone.NodeApplied("noble-move"), Is.False,
                "And the move it governs did not run: declining is what the skipped node records.");

            var accompanied = ArienBoard.Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent,
                    ["red-melee-02"] = StagedHeroBoard.TwoAway
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01", "red-melee-02"));
            accompanied.PlayAttackAt(
                ArienCardContent.NobleBlade, accompanied.MinionAt(StagedHeroBoard.FirstAdjacent));

            var offer = accompanied.Choice;
            Assert.That(offer, Is.Not.Null,
                "A unit adjacent to the target that can take a step is exactly what the sentence " +
                "offers to move, so the question is a real one.");
            Assert.That(offer.NodeId.Value, Is.EqualTo("noble-may"));
            Assert.That(offer.ConfirmInvocation, Is.Not.Null);
            Assert.That(offer.DeclineInvocation, Is.Not.Null,
                "The face prints `may`, so declining is still the player's to give.");

            accompanied.RoundTripThroughASnapshot();
            Assert.That(accompanied.Choice.NodeId.Value, Is.EqualTo("noble-may"),
                "A restored match agrees about whether the branch exists, because the offer is " +
                "read from the state rather than stored as a fact.");
            accompanied.Decline();
            Assert.That(accompanied.Choice, Is.Null);
        }

        /// <summary>
        /// Rogue Wave's step-7 push, which is the surface the same report named and which needed no
        /// correction: its `up to` capability already answers "nothing to push" before any prompt
        /// opens. The case is here because a matrix row that says so has to be provable.
        /// </summary>
        [Test]
        public void AriensAfterAttackPush_IsOfferedOnlyWhereAnEnemyUnitStandsBesideHim()
        {
            var alone = RogueWaveWithNobodyBesideHim();

            Assert.That(alone.Choice, Is.Null,
                "'an enemy unit adjacent to you' names nobody, and p.18's batch push reports that " +
                "before it offers a distance.");

            var beside = ArienBoard.Create(
                tier: 2,
                branch: ArienUpgradeBranch.SecondSibling,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.TwoAway,
                    ["red-melee-02"] = StagedHeroBoard.SecondAdjacent
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01", "red-melee-02"));
            beside.PlayAttackAt(
                ArienCardContent.RogueWave, beside.MinionAt(StagedHeroBoard.TwoAway));

            Assert.That(beside.Choice, Is.Not.Null,
                "One enemy minion standing beside him is what the sentence asks about.");
            Assert.That(beside.Choice.NodeId.Value, Is.EqualTo("wave-push"));
        }

        // -------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------

        /// <summary>A free playable space far from the engagement, for the seat a case needs out of it.</summary>
        private static readonly HexCoordinate FarFromTheEngagement = new HexCoordinate(2, 0);

        /// <summary>
        /// Noble Blade played at the one minion on the board, with the enemy hero out of the
        /// engagement - so the target has no neighbour but Arien, whom "another unit" excludes.
        /// </summary>
        private static StagedHeroBoard NobleBladeAloneWithItsTarget()
        {
            var board = ArienBoard.Create(
                minionPlacements: StagedHeroBoard.At(
                    "red-melee-01", StagedHeroBoard.FirstAdjacent),
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01"));
            board.PlayAttackAt(
                ArienCardContent.NobleBlade, board.MinionAt(StagedHeroBoard.FirstAdjacent));
            return board;
        }

        /// <summary>
        /// Rogue Wave fired at the one minion on the board, two spaces off - so p.18's push finds
        /// nothing standing beside Arien when step 7 arrives.
        /// </summary>
        private static StagedHeroBoard RogueWaveWithNobodyBesideHim()
        {
            var board = ArienBoard.Create(
                tier: 2,
                branch: ArienUpgradeBranch.SecondSibling,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.TwoAway),
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01"));
            board.PlayAttackAt(
                ArienCardContent.RogueWave, board.MinionAt(StagedHeroBoard.TwoAway));
            return board;
        }

        private static IReadOnlyList<string> TimedOptionalSurfaces()
        {
            var configuration =
                MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var rows = new List<string>();
            foreach (var card in configuration.CardDefinitions
                .OrderBy(definition => definition.Id.Value, System.StringComparer.Ordinal))
            {
                foreach (var action in card.ActionDefinitions)
                {
                    foreach (var moment in TimedMoments)
                    {
                        var text = action.CardTextAt(moment);
                        if (text == null)
                        {
                            continue;
                        }

                        foreach (var node in text.Nodes.Where(IsAnOfferSurface))
                        {
                            rows.Add(
                                $"{card.Id.Value} | {moment} | {node.Id.Value} | " +
                                $"{node.Kind}/{node.FailurePolicy} | {GateFor(node)}");
                        }
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// Whether this node can put something to the player that they might not have to take: an
        /// optional control, a mode, a repeat, or a step the card marks `may`, `up to` or `if able`.
        /// A mandatory capability is not one of these - it applies or it stops the action.
        /// </summary>
        private static bool IsAnOfferSurface(EffectNodeDefinition node)
        {
            switch (node.Kind)
            {
                case EffectNodeKind.Optional:
                case EffectNodeKind.Choose:
                case EffectNodeKind.RepeatAction:
                    return true;
                case EffectNodeKind.Capability:
                    return node.FailurePolicy != EffectFailurePolicy.Mandatory;
                default:
                    return false;
            }
        }

        /// <summary>What refuses this surface when it can do nothing, named by the milestone that owns it.</summary>
        private static string GateFor(EffectNodeDefinition node)
        {
            switch (node.Kind)
            {
                case EffectNodeKind.Optional:
                    return node.FailurePolicy == EffectFailurePolicy.IfAble
                        ? "attempted, never asked"
                        : "offer gate";
                case EffectNodeKind.Choose:
                    return "option gate";
                case EffectNodeKind.RepeatAction:
                    return "repeat gate";
                default:
                    return node.FailurePolicy == EffectFailurePolicy.IfAble
                        ? "attempted, never asked"
                        : "the capability's own Anticipate";
            }
        }

        private static IReadOnlyCollection<string> EveryMinionExcept(params string[] kept) =>
            AllMinions.Where(minion => !kept.Contains(minion)).ToArray();

        private static readonly string[] AllMinions =
        {
            "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
            "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };
    }
}
