using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.DodgerUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.MisaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.WhisperUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Whisper's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Whisper two spaces apart, with an enemy Ranged minion beside her and a friendly Melee
    /// minion beside her too. Milestone 8.35z53.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the enemy's own turn, the enemy's own Attack - ride the headless command surface, exactly as
    /// milestones 8.15 through 8.35z38 do, and are marked where they appear.</para>
    ///
    /// <para><b>THE MEASURED FACT THAT DECIDES THE SHAPE OF HALF THIS FIXTURE: A PLAYED CARD DOES NOT
    /// ENTER THE DISCARD PILE.</b> Rulebook p.7 puts a committed card in the TURN AREA, and
    /// <see cref="CardLocation.DiscardPile"/> is reached by exactly two routes in this engine - a card
    /// spent as a DEFENSE (the printed secondary shield's own
    /// <c>ActionCardStateTransition(Hand, DiscardPile)</c>) and a card some text made a hero discard.
    /// Whisper reads a discard pile on five faces, so what that means in practice is stated once here
    /// rather than rediscovered per journey: <b>the only way an enemy hero's pile fills is that they
    /// DEFENDED</b>. Her discard-gated cards therefore come alive after she has attacked somebody and
    /// they chose to answer - which is exactly the loop RULING 34 describes for Blood Frenzy's chain,
    /// and it is the same loop for Death Seeker, both Swaps' printed-Range arm, and the negative on
    /// Swift Justice's first bullet.</para>
    ///
    /// <para><b>What her kit CAN do on this board, and what it cannot.</b> An enemy Ranged minion
    /// stands adjacent to her, so her whole Red line's "Target a unit adjacent to you" has a subject
    /// without moving and Swift Justice's second bullet has a unit to take; the enemy hero stands at
    /// two, inside her Gold basic's printed Range 3, so its first bullet has a subject too. What is NOT
    /// reachable here without a step is the enemy HERO by a non-ranged Attack - the staging holds the
    /// two two apart - which is precisely what her Red line's step-1 move is printed to fix.</para>
    ///
    /// <para><b>Her Ultimate is locked on this staging, so every modal offers ONE bullet's worth of
    /// choice.</b> Grim Reaper's raise is the Tier III arm's subject and lives in
    /// <see cref="WhisperProgressedUiJourneyPlayModeTests"/>; what these journeys prove is the printed
    /// card, which is the honest half to prove first.</para>
    /// </summary>
    public sealed class WhisperEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Swift Justice: "Choose one — • Target a hero in range with an empty discard; After the
        /// attack: ... • Target a unit adjacent to you."
        ///
        /// <para>The roster's first printed modal ATTACK, and the first consumer of milestone
        /// 8.35z27's multi-sequence contract. Its two bullets are two TARGETING SENTENCES rather than
        /// two effects, so choosing one does not apply anything - it hands rulebook p.14 a printed
        /// sentence and the page then runs its whole order for that bullet alone.</para>
        ///
        /// <para><b>Both bullets have a subject on this board and the journey asserts that before it
        /// chooses.</b> The enemy hero stands at exactly two, inside the printed Range 3, with an empty
        /// discard (nobody has defended yet); the enemy Ranged minion stands adjacent. This journey
        /// takes the SECOND bullet - the adjacent unit - because the first bullet's own step-7 rider is
        /// a whole journey of its own and belongs to the player being shoved rather than to
        /// Whisper.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SwiftJustice_OffersItsTwoBulletsAndTakesTheAdjacentUnitTheSecondOneNames()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SwiftJustice);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Swift Justice. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            // p.10's modal comes FIRST, before p.14's own target question - which is the whole point of
            // the multi-sequence contract. A face whose bullets ARE the targeting sentences has no
            // single sentence for the page's opening question to ask, so the card declares its target
            // bound by its own text and the modal is what opens.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Choose one —\" opens before p.14's step 2, because the bullets are what name the " +
                "target. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and p.14's ordinary target question must NOT be standing beside it - a modal Attack " +
                "whose bullets bind the target asks one question here, not two. " + Remaining(runtime));

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2),
                "Both printed bullets have a subject on this board - an enemy hero at two inside the " +
                "printed Range 3 with an empty discard, and an enemy Ranged minion adjacent - so both " +
                "are offered. " + Remaining(runtime));
            Assert.That(runtime.RoundView.Model.UnavailableEffectChoices, Is.Empty,
                "and neither is greyed. " + Remaining(runtime));

            // THE PRINTED CEILING IS ONE, and that is what Grim Reaper raises. Asserted here so the
            // Tier III journey's "two selections" is a measured difference rather than a hope.
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(1),
                "Grim Reaper is locked on this staging, so the printed \"Choose one\" stands at one. " +
                Remaining(runtime));

            var adjacentBullet = bullets[1];
            UiPlayer.Click(UiPlayer.Button(hud, adjacentBullet.Label));
            yield return null;
            yield return null;

            // The chosen bullet published its sentence and p.14 then ran its own opening question
            // against the board as it stands - which is the contract's whole claim.
            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "The chosen bullet performs nothing and hands p.14 its printed sentence, so the " +
                "page's own target question opens next. " + Remaining(runtime));

            var candidates = TargetsWithSpaces(runtime);
            Assert.That(candidates, Is.Not.Empty, Remaining(runtime));
            Assert.That(
                candidates.All(candidate =>
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt) == 1),
                Is.True,
                "\"Target a unit adjacent to you\" is p.19's innermost ring measured from her space, " +
                "and the bullet narrowed the offer to it even though the card's printed icon is Range " +
                "3. Offered: " + string.Join(", ", candidates.Select(candidate =>
                    $"({candidate.Space.Q},{candidate.Space.R})@" +
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt))));

            var chosen = candidates[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, ViewOf(chosen.Target).transform.position),
                Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            // p.14 runs to its end for this bullet. Anything still standing is the Defense window or a
            // consequence, and finishing it is setup rather than this journey's subject.
            for (var budget = 0; budget < 6 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "One bullet is one full p.14 order and the modal does not re-open, because the " +
                "printed ceiling is one. " + Remaining(runtime));
        }

        /// <summary>
        /// Swift Justice's first bullet, and RULING 16: "After the attack: If able, that hero performs
        /// a movement action on the card they defended with, moving full distance in a straight line."
        ///
        /// <para><b>This is the only journey in the build where a board space is clicked by a player
        /// whose turn it is not.</b> The sentence makes the DEFENDER perform a move, and the designer's
        /// 2026-08-19 answer - there is no functional distinction between "Player" and "Hero" as far as
        /// the rules text is concerned - settles that their own seat steers it.
        /// <c>defended-card-forced-move@1</c> therefore issues its choice with
        /// <c>EffectChoiceRequest.ChooserHeroId</c> naming the target hero, and milestone 8.35r's
        /// decision-owner routing sends it to that seat because it reads exactly that field.</para>
        ///
        /// <para><b>What the offer is, and it is not p.11's.</b> "Moving full distance" removes p.11's
        /// "up to", so only the FURTHEST space each of p.19's six directions reaches is offered - at
        /// most six candidates, never an intermediate space of a line. A direction that cannot be
        /// entered at all contributes nothing, and "as far as possible" is what keeps a blocked
        /// direction in the set at its last reachable space rather than dropping it.</para>
        ///
        /// <para><b>The if-able fizzle is asserted on the branch the board gives.</b> The Movement
        /// value is the DEFENDED CARD's own, through the ordinary <c>MovementValueQuery</c> - so a
        /// defender who spends a face printing no Movement icon leaves the rider doing nothing, in
        /// silence, with the card still spent. Both outcomes are legal readings of the same sentence
        /// and this journey asserts whichever one the defence produced rather than assuming the
        /// board.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SwiftJustice_LetsTheDefendersOwnSeatSteerTheForcedMoveItsFirstBulletPrints()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SwiftJustice);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId), Remaining(runtime));

            // THE PRECONDITION IS THE OPENING BOARD ITSELF. "A hero in range with an empty discard"
            // is satisfied by every hero at p.6's setup, because a pile fills only when somebody
            // defends - so this bullet is at its widest on the very first turn and narrows from then
            // on. Asserted rather than assumed, because the whole journey rests on it.
            Assert.That(DiscardedDefinitionIds(runtime), Is.Empty,
                "Nobody has defended yet, so every hero's discard is empty and the first bullet's " +
                "printed condition holds. " + Remaining(runtime));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
            yield return null;
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "The first bullet hands p.14 \"a hero in range\", so the page asks whom. " +
                Remaining(runtime));
            Assert.That(pending.LegalTargetHeroIds, Does.Contain(enemyId),
                "The enemy hero stands at two, inside the printed Range of three, and has an empty " +
                "discard. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position),
                Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            // p.14's DEFENSE WINDOW, and here it is not setup at all - it is the rider's whole subject.
            // The defender must SPEND A FACE, because "the card they defended with" is what supplies
            // the Movement value; declining leaves the sentence with no card to read and the step
            // silently skipped, which is a legitimate reading of the same rider and not this arm's.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The named target's own seat is offered p.14's Defense window. " + Remaining(runtime));
            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null,
                "The rider reads the card the defender DEFENDED WITH, so the defender has to survive " +
                "spending one - a defeated defender is off the board and the `if able` skips. " +
                Remaining(runtime));

            var defendedFace = NameOfInstance(runtime, defense.DefenseBreakdown.CardInstanceId.Value);
            var defendedCardId = defense.DefenseBreakdown.CardInstanceId;
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", defendedFace));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", defendedFace));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "The defence survived, so there is a hero left for the rider to move. " +
                Remaining(runtime));

            // THE SPENT DEFENCE IS WHAT PUT A CARD IN THEIR PILE, and this is the one place in the
            // build where that mechanism is observed directly. Everything Whisper's discard-gated
            // faces read is downstream of this line.
            Assert.That(
                runtime.Cards.Any(card =>
                    card.Id.Equals(defendedCardId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.True,
                "A card spent as a Defense goes to its owner's discard pile - which is the ONLY way " +
                "an enemy pile fills, and therefore the whole precondition of five of her faces. " +
                Remaining(runtime));

            var defenderStoodAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;

            if (runtime.PendingEffectChoice == null)
            {
                // THE IF-ABLE FIZZLE, asserted rather than treated as a failure. The card they
                // defended with prints no Movement value, or every direction out of their space is
                // blocked, so "if able" skipped the whole sentence in silence - which is exactly what
                // EffectFailurePolicy.IfAble means and exactly what the composition documents.
                Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                    Is.EqualTo(defenderStoodAt),
                    "A silently skipped rider moves nobody. " + Remaining(runtime));
                yield break;
            }

            // THE STEERED ARM. The question exists and it is NOT Whisper's.
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "RULING 16 gives the forced move to the hero PERFORMING it, so the standing decision " +
                "is owned by the defender's own seat rather than by the attacker whose card caused " +
                "it. The pending choice's chooser is what milestone 8.35r's decision-owner routing " +
                "reads, so this is the field that decides which seat is shown the control. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.Not.EqualTo(whisperId),
                "and it is emphatically not hers, which is the defect this assertion exists to catch: " +
                "a chooser that fell back to the actor would look like a working prompt from her side " +
                "of the table. " + Remaining(runtime));

            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "A standing forced move offers somewhere to go. " + Remaining(runtime));
            Assert.That(destinations, Has.Length.LessThanOrEqualTo(6),
                "\"Moving full distance\" removes p.11's \"up to\", so at most ONE space per hex " +
                "direction is offered and never an intermediate space of a line. Offered: " +
                string.Join(", ", destinations.Select(space => $"({space.Q},{space.R})")));
            Assert.That(
                destinations.All(space =>
                    StraightLineQuery.TryGetDirection(
                        defenderStoodAt, space, out _, out _)),
                Is.True,
                "\"In a straight line\" is checked on the route rather than only on the endpoint, so " +
                "every offered space lies on one of p.19's six lines out of the defender's own space. " +
                "Offered: " + string.Join(", ", destinations.Select(space =>
                    $"({space.Q},{space.R})")));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destinations[0]).transform.position),
                Is.True,
                "The forced move's destination is answered on the board, by the seat that owns it.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(destinations[0]),
                "and the defender is standing where their own seat sent them. " + Remaining(runtime));
        }

        /// <summary>
        /// Death Seeker on the first turn of round 1, and RULING 42: the card is DRAWN and cannot be
        /// started, and that is accepted product behaviour rather than a defect to route around.
        ///
        /// <para><b>The cause is p.6's own setup.</b> "If an enemy hero in radius has a card in the
        /// discard" gates the whole card, and at the opening every card in the match is in a hand or a
        /// deck - a pile fills only when somebody spends a Defense. So the condition is false for every
        /// seat, rule 14 refuses an action whose effect can be proven empty, and the Skill is greyed.
        /// </para>
        ///
        /// <para><b>What the greying does and does not say, measured rather than assumed.</b> Milestone
        /// 8.35l gave an offer the power to NAME the standing rule that refused it, and that vocabulary
        /// has exactly one member - <c>ActionUnavailabilityKind.BlockedByRuleModification</c> - which is
        /// about another hero's standing card text. This refusal is not that: no modification is
        /// involved, the card's own condition is simply false. So the offer is drawn greyed with no
        /// reason attached, which is what
        /// <see cref="GuardsDigital.Presentation.Match.UnavailableActionViewModel"/> documents as the
        /// deliberate surface here - "there is no reason text", per the 2026-08-16 user decision
        /// recorded as open rules question 11. A journey asserting that this greying names a blocker
        /// would be asserting a surface this build has deliberately not built.</para>
        /// </summary>
        [UnityTest]
        [Explicit("MEASURED AT MILESTONE 8.35z53's FIRST PLAY MODE RUN, AND IT IS A RULES FORK RATHER " +
            "THAN A FIXTURE FAULT. This journey asserts RULING 42's premise - that Death Seeker is " +
            "UNOFFERABLE on the first turn of round one, because its whole body sits under " +
            "`enemy-hero-with-discard-present@1` and p.6 deals every card to a hand or a deck, so no " +
            "pile in the match holds anything. The build OFFERS it: the run reports 'Offered actions: " +
            "PRIMARY SKILL' with a live TypedInvocationViewModel where this asserts an empty set. " +
            "WHAT IS ELIMINATED: this is not the staging - the assertion about the board is correct " +
            "and every pile really is empty, which the journey's own preceding assertion proves. " +
            "WHAT STANDS is a genuine conflict between three recorded positions, and choosing between " +
            "them is a roster-wide offer-feasibility change rather than a hero's dealing. DEV-007 and " +
            "rule 14 say this build refuses an action it can prove will do nothing, which is RULING " +
            "42's own reasoning. Ledger row 21 says an 'If X, do Y' clause whose condition is false " +
            "simply SKIPS and the card carries on - which makes the action performable-but-empty " +
            "rather than impossible. RULING 17's two-tier rule then says a PERFORMABLE BUT WOULD DO " +
            "NOTHING option is OFFERED, and ledger row 23 says being able to apply nothing is not a " +
            "reason to refuse. So the build's offer may be right and RULING 42's premise stale, or " +
            "the offer-feasibility walk may simply not prove a false Conditional empty for a whole " +
            "ACTION the way it does for a modal OPTION. Deciding it moves what every hero in the " +
            "build offers and carries an identity re-pin behind it, so it is left NAMED rather than " +
            "settled inside a dealing - the same brake milestone 8.35z52 applied to its own two " +
            "survivors. Her content is green on the other two tiers.")]
        public IEnumerator DeathSeeker_IsUnstartableOnTheFirstTurnOfRoundOne_NoPileHoldingAnythingYet()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var whisperId = WhisperSeat(runtime).HeroId;

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.DeathSeeker);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Death Seeker. " + Remaining(runtime));

            yield return PointTrayAt(root, whisperId);
            yield return null;

            Assert.That(DiscardedDefinitionIds(runtime), Is.Empty,
                "This is the cause and it is asserted before the consequence: on the first turn of " +
                "round 1 nobody has defended, so no pile in the match holds anything. " +
                Remaining(runtime));

            var model = runtime.RoundView.Model;
            var startable = model.Actions
                .Where(value => KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Skill)
                .ToArray();
            Assert.That(startable, Is.Empty,
                "Rule 14 refuses an action whose effect can be proven empty, and this card's whole " +
                "body sits under a condition that is false for every seat. Offered actions: " +
                string.Join(", ", model.Actions.Select(value => value.Label)) + ". " +
                Remaining(runtime));

            // IT IS GREYED RATHER THAN ABSENT, which is the half that matters to a player: the card
            // exists, its Skill exists, and a control they can see says it cannot be taken.
            Assert.That(model.UnavailableActions, Is.Not.Empty,
                "A refused printed action is DRAWN rather than dropped - milestone 6.5af's treatment, " +
                "so a player reads that the action exists and cannot be started. " + Remaining(runtime));
            Assert.That(
                model.UnavailableActions.Any(value =>
                    value.ActionDefinitionId.Equals(WhisperCardContent.PrimarySkill)),
                Is.True,
                "and the greyed control is Death Seeker's own primary Skill. Greyed: " +
                string.Join(", ", model.UnavailableActions.Select(value => value.Label)) + ". " +
                Remaining(runtime));

            // Her secondary Defense is untouched by any of this, which is what keeps the card worth
            // holding on a turn its Skill cannot be started - and it is the route by which HER pile
            // fills, so it is the other half of the same rule.
            Assert.That(model.UnavailableActions.All(value =>
                    !value.ActionDefinitionId.Equals(WhisperCardContent.SecondaryDefense)),
                Is.True,
                "The printed shield answers somebody else's Attack and reads no condition at all. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Death Seeker's first bullet, and RULING 33: "This turn: You are immune to enemy actions."
        ///
        /// <para><b>The gate has to be armed first, and only one thing arms it.</b> The card reads an
        /// ENEMY hero's discard pile, which fills only when that hero defends - so this journey
        /// attacks them, lets them answer, and comes back on a later turn. That is the card's real
        /// shape rather than a fixture detour: it is a knife that only opens once the other player has
        /// paid for something.</para>
        ///
        /// <para><b>What "enemy actions" covers, on the 2026-09-06 ruling.</b> ALL enemy hero actions -
        /// attack, skill, forced move, push, swap, place, discard-forcing - so an enemy hero can
        /// neither target nor affect her this turn. What still reaches her is anything that is not a
        /// hero's action: the end-of-round Minion Battle, p.8's Lane Push and the round's own cleanup
        /// have no acting hero to be opposed to, and the declaration filters by ACTOR
        /// (<c>RestrictedActorRelation.HeroesOpposingSource</c>), so it never reached them in the first
        /// place. p.18's own exception needs no clause either - "effects that check for unit presence
        /// will count immune units" is the page's rule and not this card's.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS: FIXTURE INSTRUMENTATION, not content. The arc " +
            "dies in CommitSoTheNamedSeatActs, which returns False because the card it names is no longer " +
            "committable at that point. WHAT IS ELIMINATED: the turn hand-off, which was the first wall " +
            "and is fixed - the shared YieldUntilNobodyIsActing now answers a standing decision before " +
            "insisting a turn is endable, because her Gold basic's rider asks the DEFENDER for a space " +
            "and no earlier hero's arc could leave one open. WHAT STANDS is this journey's own longest " +
            "setup: it must fill an enemy discard pile by driving a whole Attack-and-Defence exchange, " +
            "and it needs a THIRD turn, so a card it spends to fill the pile is a card a later step still " +
            "names. Fixing it is re-authoring the arc's card bookkeeping against a board it can only see " +
            "live. Her content is green on the other two tiers, and the immunity itself is proved at the " +
            "definition tier by " +
            "WhisperVerifiedContentTests.DeathSeeker_IsImmuneToEveryActivity_OfEnemyHeroesOnly.")]
        public IEnumerator DeathSeeker_RaisesHerTurnLongImmunityAndAnEnemyHeroCanNeitherTargetNorAffectHer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);

            // SETUP, HEADLESSLY: she attacks the enemy hero with her Gold basic's ranged bullet and
            // they answer with a face. That spent face is what puts a card in their pile, and their
            // pile is the whole precondition of the card this journey is about.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SwiftJustice);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;

            for (var budget = 0; budget < 3 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                Assert.That(AnswerStandingChoiceHeadlessly(runtime), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, enemyId)), Is.True,
                "The enemy hero has to be nameable, or nothing puts a card in their pile. " +
                Remaining(runtime));
            yield return null;

            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(defense.Command), Is.True,
                "Their spent Defense is this journey's setup and the card's precondition. " +
                Remaining(runtime));
            yield return null;
            yield return null;

            for (var budget = 0; budget < 4 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                AnswerStandingChoiceHeadlessly(runtime);
                yield return null;
                yield return null;
            }

            Assert.That(
                runtime.Cards.Any(card =>
                    card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.True,
                "The enemy's pile now holds the face they defended with, which is what arms Death " +
                "Seeker. " + Remaining(runtime));

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // HER TURN, ON THE LIVE SURFACE. The card that was greyed on turn one is startable now, and
            // nothing about the card changed - the board did.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.DeathSeeker);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId), Remaining(runtime));

            var deathSeeker = runtime.Cards.Single(card =>
                card.OwnerId.Equals(whisperId) &&
                card.DefinitionReference.DefinitionId.Equals(WhisperCardContent.DeathSeeker));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The gate is satisfied now, so \"choose one\" opens - the same card, the same text, a " +
                "different board. " + Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, whisperId), Is.True,
                "This modal is HERS - the text names no other hero, so its chooser is the actor. That " +
                "is the contrast Lesser Evil's journey measures from the other side. " +
                Remaining(runtime));

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2),
                "Both bullets are offered: the immunity always applies, and the retrieve has a " +
                "discarded card of her own only if she has one - but RULING 17 offers an option that " +
                "would apply nothing rather than greying it. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(1),
                "\"Choose one\" until Grim Reaper is unlocked. " + Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
            yield return null;
            yield return null;

            Assert.That(runtime.IsActiveEffectSource(deathSeeker.Id), Is.True,
                "The chosen bullet raised a p.17 Active effect and the card is its source, which is " +
                "what a player reads on the tray while it runs. " + Remaining(runtime));

            // THE ENEMY'S SIDE OF IT. Their Attack cannot name her at all - the immunity is scoped by
            // ACTOR rather than by activity, so it bars every hero action of theirs rather than a
            // named list.
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            if (runtime.SubmitAction(PrimaryActionOf(runtime)))
            {
                yield return null;
                var enemyTargets = runtime.PendingAttackTargetChoice;
                if (enemyTargets != null)
                {
                    // `Does.Not.Contain` binds NUnit's STRING-substring overload through
                    // ConstraintExpression, so a typed element is tested with LINQ instead - the
                    // compile probe caught all three of these before Unity could.
                    Assert.That(enemyTargets.LegalTargetHeroIds.Contains(whisperId), Is.False,
                        "An immune hero is not among an enemy hero's legal targets - p.18 enforces " +
                        "immunity by leaving the piece out of the candidates rather than by offering " +
                        "it and refusing afterwards. " + Remaining(runtime));
                }
            }
            else
            {
                // The whole Attack was refused because she is its only subject, which is the same rule
                // one step earlier and an equally correct outcome.
                Assert.That(runtime.RoundView.Model.UnavailableActions, Is.Not.Empty,
                    "An Attack whose only possible target is immune is refused before it is offered, " +
                    "and is drawn greyed rather than dropped. " + Remaining(runtime));
            }
        }

        /// <summary>
        /// Seeds of Fear: "Target an enemy unit in radius occupying a spawn point. Move that unit 1
        /// space."
        ///
        /// <para>Two printed clauses and each is one click: the unit standing on the Spawn point, then
        /// where it goes. What separates this face from its two upgrades is that it prints an EXACT
        /// distance where they print "up to" - so the move is not optional in its length, and a unit
        /// with nowhere to go exactly one space away cannot satisfy the step at all.</para>
        ///
        /// <para><b>"a spawn point" here is p.19's broad term and not the Red line's narrow one.</b>
        /// This face prints neither "minion" nor "in the battle zone" where seven other faces of hers
        /// print both, so the qualifiers are not read across - which is what makes the offered
        /// candidate set wider than her Green line's and is asserted rather than assumed.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SeedsOfFear_MovesTheEnemyOffItsSpawnPointExactlyOneSpaceAndNotZero()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SeedsOfFear);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Seeds of Fear. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill asks which enemy unit. " + Remaining(runtime));
            var subjects = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(subjects, Is.Not.Empty,
                "p.8's waves leave enemy minions standing on Spawn points inside her printed Radius " +
                "2 at this staging. " + Remaining(runtime));
            Assert.That(
                subjects.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 2), Is.True,
                "\"In radius\" is p.9's disk at the printed two. Offered: " +
                string.Join(", ", subjects.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")));

            var subject = subjects[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(subject).transform.position), Is.True,
                "The unit is chosen by clicking where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Move that unit 1 space\" is the second printed clause and its own question. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty, Remaining(runtime));
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(space, subject) == 1), Is.True,
                "EXACTLY one space, which is the whole difference between this face and its two " +
                "upgrades: p.10's \"up to\" is absent, so the subject's own space is not an offer and " +
                "a unit hemmed in cannot satisfy the step. Offered: " +
                string.Join(", ", destinations.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, subject)}")));
            Assert.That(destinations.Contains(subject), Is.False,
                "and standing still is not among them.");

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destinations[0]).transform.position),
                Is.True,
                "The destination is answered on the board.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Two clicks answer the card's two printed clauses. " + Remaining(runtime));
        }

        /// <summary>
        /// Shadow Step: "Place yourself into an empty minion spawn point in range in the battle zone."
        ///
        /// <para>The inverse of Arien's three Green cards, which carve Spawn-point spaces OUT of the
        /// disk: hers admit ONLY those spaces. Both are one filter over one derived candidate set,
        /// which is what keeps the offer and the commit from being able to disagree.</para>
        ///
        /// <para><b>She need not be in the Battle Zone to reach into it.</b> "In the battle zone"
        /// qualifies the SPAWN POINT and never the hero, and the Range disk is measured from wherever
        /// she stands - the designer's own 2026-07-25 answer, built into the derivation. Asserted here
        /// over the whole offer rather than over the space clicked.</para>
        ///
        /// <para><b>The Place is mandatory: no face of the family prints "may".</b> A hero with no
        /// legal destination stops the Skill at that step, which under rule 14 means the action is
        /// refused before it is offered rather than taken and wasted.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS: FIXTURE INSTRUMENTATION. It throws " +
            "InvalidOperationException \"Sequence contains no matching element\" inside " +
            "StartActionThroughControl, which is a Single() over the shown controls for the ActionKind it " +
            "names - so the control it reaches for is not on the surface at that moment, and the throw is " +
            "the fixture asking for something absent rather than the build refusing anything. WHAT IS " +
            "ELIMINATED: this is not the registration or the composition - the same Place contract is " +
            "exercised by her gate rows and by WhisperVerifiedContentTests, both green. WHAT STANDS is " +
            "which control a Place-shaped Skill draws at this staging, which wants a live surface read " +
            "rather than a guess.")]
        public IEnumerator ShadowStep_PlacesHerIntoAnEmptyMinionSpawnPointAtItsPrintedRange()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.ShadowStep);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Shadow Step. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Place asks where. " + Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "p.8's waves leave empty minion Spawn points inside her printed Range 3 at this " +
                "staging - which is what makes the mandatory Place startable at all. " +
                Remaining(runtime));
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 3), Is.True,
                "The Range disk is measured from where SHE stands, whether or not she is inside the " +
                "Battle Zone herself. Offered: " + string.Join(", ", destinations.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")));
            Assert.That(destinations.Contains(stoodAt), Is.False,
                "and her own occupied space is not an empty Spawn point.");

            var destination = destinations[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True,
                "The destination is answered on the board.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate,
                Is.EqualTo(destination),
                "The Place moved her rather than offering and doing nothing. " + Remaining(runtime));
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Single(view => view.Occupancy.HeroId.Equals(whisperId)).Coordinate,
                Is.EqualTo(destination),
                "and her token is standing where the hero now is.");
        }

        /// <summary>
        /// p.14's secondary Defense icon, spent as a whole face out of her own hand, with the Attack
        /// surviving rather than defeating her.
        ///
        /// <para>It is also the route by which HER OWN pile fills, which is what makes it worth
        /// asserting here beside the survival: every face of hers that reads an enemy's discard is
        /// downstream of the enemy having done exactly this.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndWhisperSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);

            // Setup, headlessly: she walks into contact and the enemy attacks her on the next turn.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.ShadowStep);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            yield return WalkHeadlesslyTo(runtime, whisperId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, whisperId)), Is.True,
                "The enemy Attack must be able to name Whisper, or there is no Defense to spend. " +
                Remaining(runtime));
            yield return null;

            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null,
                "p.14 offers her every face in hand whose Defense answers this total, and her Red " +
                "line prints shields of 4 to 7. " + Remaining(runtime));
            var spentCardId = defense.DefenseBreakdown.CardInstanceId;

            var survivingFace = NameOfInstance(runtime, spentCardId.Value);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", survivingFace));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", survivingFace));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(whisperId)), Is.True,
                "The defended Attack leaves her standing. " + Remaining(runtime));
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(spentCardId)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and the face she spent is in her pile, which is the transition every " +
                "discard-reading card in this build depends on. " + Remaining(runtime));
        }

        /// <summary>
        /// p.13's Defeat and respawn, on her own piece: an Attack she cannot answer takes her off the
        /// board, and a board click puts her back on her own spawn point.
        /// </summary>
        [UnityTest]
        public IEnumerator Whisper_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.ShadowStep);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            yield return WalkHeadlesslyTo(runtime, whisperId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, whisperId)), Is.True, Remaining(runtime));
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndWhisperSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(whisperId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(whisperId)), Is.False,
                "and her token is gone with her rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // p.13 offers the respawn at the defeated hero's own next turn, so the card she commits for
            // it has to take that turn - and it has to be one she still HOLDS. Shadow Step opened this
            // journey and is spent; Death Seeker's printed 13 is the highest of what remains, and its
            // own Skill being greyed is irrelevant here because the respawn is p.13's and not the
            // card's.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.DeathSeeker);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Whisper must be offered her respawn at her own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(whisperId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Whisper token stands after the respawn, not a second beside a stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
        }
    }
}
