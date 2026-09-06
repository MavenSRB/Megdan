using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z15. Rulebook p.10's repeat is an offer, so a repeat with no legal subject is
    /// not offered - measured over every repeat the dealt roster prints, not over the one card a
    /// player reported.
    ///
    /// <para><b>The report.</b> Xargatha resolved Cleave - "Target a unit adjacent to you. After the
    /// attack: May repeat once on a different enemy hero" - against the only enemy hero adjacent to
    /// her. He defended. p.10's confirm-or-decline then opened for a repeat whose subject set was
    /// empty, because the one enemy hero in reach was the one the repeat is forbidden to take.</para>
    ///
    /// <para><b>Why this is a class and not a card.</b> Milestone 8.25e made the same offer honest on
    /// the after-action-ability path and <c>Rules/RULE_SYSTEMS.md</c> row 48 recorded the ruling for
    /// every path: "whether the second instance can start is asked of the board as it then stands,
    /// under the target binding the asking node declares, rather than of the action that ended". Card
    /// text printed inside rulebook p.14 never asked it. Four composed faces reach a
    /// <see cref="EffectNodeKind.RepeatAction"/> node that way, and <see cref="EveryRepeatTheRosterPrints"/>
    /// is the table that keeps a fifth from arriving unjudged.</para>
    ///
    /// <para><b>Suppression rather than a greyed reason, and the vocabulary decides it.</b> Milestone
    /// 8.35s's grey-with-reason grammar attaches a cause to an <see cref="ActionOffer"/> a player can
    /// hover before choosing. A repeat node publishes no offer: its only two states are a pause that
    /// stands and a pause that does not, and the node's own rule has said since milestone 8.23 that an
    /// ineligible repeat is skipped in silence because "asking, and then doing nothing when the player
    /// confirms" is the one outcome a `may` must never produce. Drawing a greyed ACCEPT the player
    /// cannot answer would be that outcome with a label on it.</para>
    /// </summary>
    [TestFixture]
    public class RepeatOfferFeasibilityTests
    {
        // -----------------------------------------------------------------------------------------
        // The class, derived from the content rather than listed by hand
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Every <see cref="EffectNodeKind.RepeatAction"/> node the dealt roster's action definitions
        /// print, named by the kind of action each one repeats.
        ///
        /// <para><b>The kind is what decides which rule judges the offer, and nothing else is.</b> A
        /// node repeating an <see cref="ActionKind.Attack"/> is card text printed inside rulebook
        /// p.14 - for an Attack the action's effect root <em>is</em> its card-text body, which is why
        /// there is no separate column for that - and starting a second Attack instance needs a legal
        /// target, so this is the family the milestone judges and every row of it is driven below. A
        /// node repeating a <see cref="ActionKind.Skill"/> repeats the effect it lives in; starting one
        /// needs nothing from the board, so p.10 stops such an instance at whatever mandatory step it
        /// cannot take - the page's own Mandatory and Optional Steps rather than an offer that should
        /// not have been made. Those rows are untouched, and named here so that stays deliberate.</para>
        ///
        /// <para><b>What this enumeration deliberately does not reach.</b> Tigerclaw's Cloak and
        /// Daggers, whose repeat is a permanent after-action ability rather than an action definition's
        /// own body, so no walk over <see cref="CardDefinition.ActionDefinitions"/> finds it. Milestone
        /// 8.25e judges that one through the same
        /// <c>AttackDefenseCardActionStrategyBackend.CanStartRepeatedAttack</c> this milestone now asks
        /// from the card-text side, and <c>AfterActionRepeatContinuityTests</c> is its sweep.</para>
        ///
        /// <para>Derived from <see cref="MatchConfiguration.CreateOfficialSixPlayerForInternalTest"/>
        /// so a seventh hero's repeat arrives as a new row rather than as a new unjudged prompt.</para>
        /// </summary>
        [Test]
        public void EveryRepeatTheRosterPrints_IsAccountedForByFamily()
        {
            Assert.That(
                RepeatRowsForProbe().ToArray(),
                Is.EqualTo(new[]
                {
                    // Repeats a Skill: its own frame bounds it, and this milestone changes nothing.
                    "arien-ebb-and-flow|primary-skill|Skill|Unbound",

                    // Repeats an Attack: the family this milestone judges.
                    "arien-violent-torrent|primary-attack|Attack|DifferentTarget",

                    // Unbound since the phantom-declaration removal. All three printed faces read
                    // "May repeat once / up to two times" and stop, so nothing in the text asks for a
                    // different minion; the binding they used to declare described no printed clause
                    // and derived no exclusion (only Attacks derive one). Free choice - the same
                    // minion again included - is what these cards say.
                    "sabina-marching-orders|primary-skill|Skill|Unbound",
                    "sabina-path-to-victory|primary-skill|Skill|Unbound",
                    "sabina-troop-movement|primary-skill|Skill|Unbound",
                    "wasp-center-of-mass|primary-skill|Skill|SameTarget",
                    "wasp-control-gravity|primary-skill|Skill|SameTarget",
                    "wasp-lift-up|primary-skill|Skill|SameTarget",
                    "wasp-mass-telekinesis|primary-skill|Skill|Unbound",

                    "wasp-thunder-boomerang|primary-ranged-attack|Attack|DifferentTarget",
                    "xargatha-cleave|primary-attack|Attack|DifferentEnemyHero",
                    "xargatha-rapid-thrusts|primary-ranged-attack|Attack|DifferentEnemyHero"
                }),
                "A repeat this table does not name is a repeat nothing has decided the offer rule " +
                "for. Add the row, and if it repeats an Attack, drive its empty-subject case before " +
                "shipping the content.");
        }

        /// <summary>
        /// The same class read from the other side: every dealt card whose printed transcription uses
        /// rulebook p.10's repeat vocabulary, reconciled against the nodes above.
        ///
        /// <para><b>Why both directions are needed.</b> The table above enumerates composed nodes, so
        /// it is blind to a face that prints "may repeat" and composes nothing - the exact shape of a
        /// silent content gap, and one no offer rule could ever be wrong about because there would be
        /// no offer. Reading the printed text instead reconciles against it, and the one dealt card
        /// carrying no node is accounted for by name rather than by tolerance.</para>
        ///
        /// <para><b>Measured, and the measurement corrected a guess.</b> Xargatha's Desperate Cleave
        /// prints Cleave's repeat clause word for word and composes nothing, so it looked like a
        /// second exception - but it is p.20's Handicap card, which rulebook p.6 returns to the box,
        /// so it is not in the dealt configuration this walk reads at all. It is named in
        /// <c>XargathaEffectContent.AttackTextFor</c>'s own comment instead, and a milestone that
        /// deals it inherits this gate for free: it would compose through the same
        /// <c>CreateEnemyHeroRepeat</c> Cleave and Rapid Thrusts already use.</para>
        /// </summary>
        [Test]
        public void EveryCardPrintingARepeat_EitherComposesOneOrIsNamedHere()
        {
            var configuration =
                MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var printsARepeat = configuration.CardDefinitions
                .Where(definition =>
                    definition.SourceContent != null &&
                    definition.SourceContent.PrintedText != null &&
                    definition.SourceContent.PrintedText.IndexOf(
                        "repeat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray();

            var composesARepeat = new HashSet<string>(
                RepeatRowsForProbe().Select(row => row.Split('|')[0]));

            Assert.That(
                printsARepeat.Where(id => !composesARepeat.Contains(id)).ToArray(),
                Is.EqualTo(new[]
                {
                    // A permanent after-action ability rather than an action definition's own body,
                    // so no walk over ActionDefinitions reaches it. Milestone 8.25e judges its offer
                    // through the same query the card-text family now asks.
                    "tigerclaw-cloak-and-daggers"
                }),
                "A card printing p.10's repeat vocabulary but composing no repeat node is either a " +
                "content gap or an ability reached some other way. The one dealt card in that " +
                "position is the latter, and a second arrival has to say which it is here.");
        }

        /// <summary>
        /// The four Attack repeats above, each driven empty in its own case below. Stated as its own
        /// assertion so the coverage claim cannot quietly shrink: a fifth Attack repeat added to the
        /// content fails here until somebody drives it.
        /// </summary>
        [Test]
        public void EveryAttackRepeat_HasAnEmptySubjectCaseInThisBuild()
        {
            Assert.That(
                RepeatRowsForProbe().Where(row => row.Contains("|Attack|")).ToArray(),
                Is.EqualTo(new[]
                {
                    // ViolentTorrentsRepeat_WithNoOtherUnitInReach_IsNotOfferedAtAll
                    "arien-violent-torrent|primary-attack|Attack|DifferentTarget",
                    // ConfiguredAttackCardTests
                    //   .ThunderBoomerang_OffersNoRepeatWhenItTookTheOnlyUnitInReach
                    "wasp-thunder-boomerang|primary-ranged-attack|Attack|DifferentTarget",
                    // CleavesRepeat_WithNoOtherEnemyHeroAdjacent_IsNotOfferedAtAll
                    "xargatha-cleave|primary-attack|Attack|DifferentEnemyHero",
                    // RapidThrustsRepeat_WithNoOtherEnemyHeroInReach_IsNotOfferedAtAll
                    "xargatha-rapid-thrusts|primary-ranged-attack|Attack|DifferentEnemyHero"
                }),
                "Each row here is named by the case that drives its empty-subject board.");
        }

        // -----------------------------------------------------------------------------------------
        // The empty-subject case, driven once per card-text repeat the roster prints
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// THE REPORTED DEFECT, in the shape the player met it: Xargatha adjacent to exactly one enemy
        /// hero, that hero is Cleave's target, and he successfully defends.
        ///
        /// <para><b>The defended half is deliberate and must not become the fix.</b> p.10 says "After
        /// you perform this action, you may perform the entire action again" and conditions nothing on
        /// the outcome, so an Attack that was defended was still performed and still offers its
        /// repeat. What is refused here is the empty subject set and nothing else -
        /// <see cref="CleavesRepeat_WithAnotherEnemyHeroInReach_IsStillOffered"/> is the same defended
        /// Attack with somewhere for the repeat to go, and it still opens.</para>
        /// </summary>
        [Test]
        public void CleavesRepeat_WithNoOtherEnemyHeroAdjacent_IsNotOfferedAtAll()
        {
            var fixture = XargathaBoard.Create(
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", StagedHeroBoard.FourthAdjacent));

            fixture.PlayAttackAt(
                XargathaCardContent.Cleave, fixture.Enemy, defendSuccessfully: true);

            Assert.That(fixture.Choice, Is.Null,
                "\"May repeat once on a different enemy hero\" and the only enemy hero in reach is " +
                "the one this instance just attacked, so the repeat has no subject and p.10's " +
                "confirm-or-decline is not opened. Standing instead: " +
                (fixture.Choice == null ? "nothing" : fixture.Choice.NodeId.Value));
            Assert.That(fixture.Session.Projection.PendingAttackTargetChoice, Is.Null,
                "and no second instance opened a target question either.");
            Assert.That(fixture.RuleEventTypes(), Has.Member("Turn.CardResolved"),
                "The action reached its own natural end rather than stopping on the offer: the " +
                "card is resolved, which is what a repeat that is not owed leaves behind.");
        }

        /// <summary>
        /// The half that must not move. The same card, the same defended Attack, and a second enemy
        /// hero standing beside her - so the repeat has a subject and is offered exactly as before.
        /// </summary>
        [Test]
        public void CleavesRepeat_WithAnotherEnemyHeroInReach_IsStillOffered()
        {
            var fixture = XargathaBoard.Create(
                minionPlacements: StagedHeroBoard.At(
                    "red-melee-01", StagedHeroBoard.FirstAdjacent),
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", StagedHeroBoard.FourthAdjacent));

            fixture.PlayAttackAt(
                XargathaCardContent.Cleave, fixture.MinionAt(StagedHeroBoard.FirstAdjacent));

            Assert.That(fixture.Choice, Is.Not.Null,
                "The printed instance took a minion and the enemy hero is still standing beside " +
                "her, so \"on a different enemy hero\" names somebody and the offer stands - which " +
                "is also her parenthesis in words: the repeat survives a minion first target.");
            Assert.That(fixture.Choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
        }

        /// <summary>
        /// Cleave's sibling on the same clause. Rapid Thrusts is Ranged and scales its Range with the
        /// enemies around her, so its printed set is wider than Cleave's and its repeat's set is
        /// exactly as narrow: one enemy hero on the board, taken, leaves nobody.
        /// </summary>
        [Test]
        public void RapidThrustsRepeat_WithNoOtherEnemyHeroInReach_IsNotOfferedAtAll()
        {
            var fixture = XargathaBoard.Create(
                tier: 3,
                branch: XargathaUpgradeBranch.SecondSibling,
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", StagedHeroBoard.FourthAdjacent));

            fixture.PlayAttackAt(
                XargathaCardContent.RapidThrusts, fixture.Enemy, defendSuccessfully: true);

            Assert.That(fixture.Choice, Is.Null,
                "The same printed clause on a Ranged face: the one enemy hero in range is the one " +
                "the repeat may not take.");
        }

        // Wasp's Thunder Boomerang - the other binding, "on a different target" - is driven in
        // `ConfiguredAttackCardTests.ThunderBoomerang_OffersNoRepeatWhenItTookTheOnlyUnitInReach`,
        // beside the two cases that already hold its repeat's other answers and the private helper
        // that stages its board.

        /// <summary>
        /// Arien's Violent Torrent, and it is the row that made this a measurement rather than a
        /// deduction.
        ///
        /// <para>Its repeat is the build's only one printed at rulebook p.14's "Before the attack:",
        /// which reads like a moment that cannot judge itself - the exclusion every binding names is
        /// the Attack's own target. Driven, the node turns out to be reached with the target already
        /// chosen: on a board holding one adjacent minion it opened p.10's confirm, accepted the
        /// confirmation, and started nothing at all. So the family is judged on whether the target is
        /// known and not on which moment the text prints at, and this card is the second member of the
        /// class rather than an exception to it.</para>
        /// </summary>
        [Test]
        public void ViolentTorrentsRepeat_WithNoOtherUnitInReach_IsNotOfferedAtAll()
        {
            var board = ArienBoard.Create(
                tier: 3,
                minionPlacements: StagedHeroBoard.At(
                    "red-melee-01", StagedHeroBoard.FirstAdjacent),
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01"));

            board.StartPrimary(ArienCardContent.ViolentTorrent);

            // p.15 has taken him to Tier III, so p.16's Ultimate is out of the deck and Living
            // Tsunami's before-action trigger intercepts the start. Declined, exactly as
            // AttackLifecycleBoundaryTests declines it, because it is a fact about this staging.
            Assert.That(board.Choice, Is.Not.Null, "Tier III opens the Ultimate's trigger first.");
            board.Decline();

            var target = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, "Violent Torrent opens p.14's target question.");
            board.Accept(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId,
                board.MinionAt(StagedHeroBoard.FirstAdjacent)));

            Assert.That(board.Choice, Is.Null,
                "\"May repeat once on a different unit\" and there is no different unit, so the " +
                "offer is not made. Before this milestone it was made, confirmed, and nothing " +
                "happened. Standing instead: " +
                (board.Choice == null ? "nothing" : board.Choice.NodeId.Value));
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        private static readonly HexCoordinate FarFromTheEngagement = new HexCoordinate(3, -1);

        private static readonly string[] AllMinions =
        {
            "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
            "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };

        private static IReadOnlyCollection<string> EveryMinionExcept(params string[] kept) =>
            AllMinions.Where(minion => !kept.Contains(minion)).ToArray();

        /// <summary>
        /// Every repeat node the roster's action definitions print, as
        /// <c>card|action|kind|binding</c>, ordered and distinct.
        ///
        /// <para><b>Distinct is load-bearing.</b> An Attack's card-text body is reached both as
        /// <see cref="ActionDefinition.EffectRoot"/> and through
        /// <see cref="ActionDefinition.CardTextAt"/>, because for an Attack they are the same
        /// definition - so a walk that names the property it arrived by reports every Attack repeat
        /// twice and invites a reader to believe there are two of them. The node is the row.</para>
        /// </summary>
        internal static IEnumerable<string> RepeatRowsForProbe()
        {
            var configuration =
                MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            return configuration.CardDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .SelectMany(action => Bodies(action)
                        .Where(body => body != null)
                        .SelectMany(body => body.Nodes)
                        .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                        .Select(node =>
                            $"{definition.Id.Value}|{action.Id.Value}|{action.Kind}|" +
                            $"{node.RepeatTargetBinding}")))
                .Distinct()
                .OrderBy(row => row)
                .ToArray();
        }

        private static IEnumerable<EffectDefinition> Bodies(ActionDefinition action)
        {
            yield return action.EffectRoot;
            yield return action.CardTextAt(action.CardTextTiming);
            yield return action.CardTextAt(action.SecondCardTextTiming);
        }
    }
}
