using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z45. Middlefinger of Death - the last unparked face of Dodger the
    /// Warlock's set, and the per-candidate predicate the multi-sequence attack contract left an
    /// extension point for.
    ///
    /// <para><b>The printed face.</b> "Choose one or both (on different targets) — • Target a unit
    /// adjacent to you. • Target a hero in range who has one or more cards in the discard." Three
    /// separate clauses have to be true at once for it to be composed correctly, and each is a
    /// different kind of statement: the modal cardinality ("one or both") is p.10's, the exclusion
    /// ("on different targets") is Ruling 19's rider printed on the card, and the second bullet's
    /// qualifier ("who has one or more cards in the discard") is a predicate about the candidate that
    /// nothing in the engine could say until this milestone.</para>
    ///
    /// <para><b>Why the qualifier could not simply be added to the target policy.</b>
    /// <see cref="PrintedAttackTargetPolicy"/> is declared by every Attack action in the roster, so
    /// widening IT would have re-identified all seven shipped heroes to carry a field six of them do
    /// not print. <see cref="AttackSequenceBullet"/> is reached by no card definition graph, which is
    /// what the multi-sequence contract predicted when it sized this extension point - and the
    /// prediction held: no other hero's content revision moved.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class MiddlefingerOfDeathTests
    {
        // -------------------------------------------------------------------------------------------
        // What the face declares
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The two printed bullets, read out of the composed definition by the same query the
        /// offer-feasibility gate uses.
        ///
        /// <para>The bullets are asserted as a literal table rather than derived, for the reason every
        /// verified set's printed-value table is literal: a case that read its expectations out of the
        /// definitions would pass whatever the definitions happened to say.</para>
        /// </summary>
        [Test]
        public void TheFace_DeclaresTwoBullets_AndOnlyTheSecondNamesThePile()
        {
            var declared = RequestedAttackSequenceQuery.Declared(AttackText());

            Assert.That(declared, Has.Count.EqualTo(2),
                "\"Choose one or both\" prints exactly two bullets, and each is one attack-sequence " +
                "leaf under the modal.");

            Assert.That(declared[0].BulletId, Is.EqualTo("middlefinger-adjacent"));
            Assert.That(declared[0].TargetPolicy.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                "\"Target a UNIT adjacent to you\" - p.19's units, which are heroes and minions both.");
            Assert.That(declared[0].TargetPolicy.Reach,
                Is.EqualTo(AttackTargetReach.AdjacentOnly));
            Assert.That(declared[0].TargetPolicy.Line, Is.EqualTo(AttackTargetLine.AnyLine));
            Assert.That(declared[0].SubjectNarrowing,
                Is.EqualTo(AttackSequenceSubjectNarrowing.None),
                "The first bullet qualifies nobody, and says so by declaring nothing rather than by " +
                "naming a member called None - the rule that keeps `excludeAdjacent` off the arms " +
                "whose faces do not print it.");
            Assert.That(declared[0].NarrowsItsSubject, Is.False);

            Assert.That(declared[1].BulletId, Is.EqualTo("middlefinger-discard"));
            Assert.That(declared[1].TargetPolicy.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesOnly),
                "\"Target a HERO in range\" - and heroes only is the sentence's own word rather than " +
                "an inference from the pile clause, though the two agree: a minion has no hand and no " +
                "discard pile for \"who has\" to be about.");
            Assert.That(declared[1].TargetPolicy.Reach,
                Is.EqualTo(AttackTargetReach.PrintedRange),
                "\"in range\" is p.13's ceiling read off the card's printed Range icon.");
            Assert.That(declared[1].TargetPolicy.Line, Is.EqualTo(AttackTargetLine.AnyLine));
            Assert.That(declared[1].SubjectNarrowing,
                Is.EqualTo(AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard),
                "\"who has one or more cards in the discard\" is THE clause this milestone exists " +
                "for. Before it, a bullet carried a printed target policy - which units, how far, " +
                "which line - and could not state a predicate about the candidate at all, which is " +
                "why this face stayed parked after Ruling 5 had answered everything else about it.");
            Assert.That(declared[1].NarrowsItsSubject, Is.True);
        }

        /// <summary>
        /// The modal is p.10's "one or both": a chooser-owned Choose taking between one and two of its
        /// options.
        /// </summary>
        [Test]
        public void TheModal_TakesOneOrBothRatherThanExactlyOne()
        {
            var text = AttackText();
            var modal = text.Nodes.Single(node => node.Id.Equals(text.RootNodeId));

            Assert.That(modal.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(modal.MinimumSelections, Is.EqualTo(1),
                "A modal Attack that chose nothing would be an Attack that never targeted anything, " +
                "which p.10's mandatory step refuses.");
            Assert.That(modal.MaximumSelections, Is.EqualTo(2),
                "\"or both\" is the whole difference between this face and her Littlefinger and " +
                "Finger of Death, which print \"Choose one\" and take exactly one. That difference is " +
                "also why those two compose against a single bound target and this one composes " +
                "against the multi-sequence contract.");
            Assert.That(modal.ChildNodeIds, Has.Count.EqualTo(2));
        }

        /// <summary>
        /// "(on different targets)" is printed on the card rather than on one bullet, so BOTH bullets
        /// declare it.
        ///
        /// <para>That is not decoration. The plan reads the flag off whichever bullets an actor
        /// actually chose, so a card that declared it on one bullet only would apply its own printed
        /// constraint or not depending on which single bullet a one-bullet answer had taken.</para>
        /// </summary>
        [Test]
        public void TheDifferentTargetsClause_IsDeclaredOnBothBullets_BecauseTheCardPrintsIt()
        {
            var text = AttackText();

            foreach (var bulletId in new[] { "middlefinger-adjacent", "middlefinger-discard" })
            {
                var node = text.Nodes.Single(value => value.Id.Value == bulletId);
                var distinct = node.Inputs.Single(input =>
                    input.ParameterName ==
                    SyntheticSequenceCapabilities.DistinctTargetsParameter);

                Assert.That(distinct.SourceKind, Is.EqualTo(EffectBindingSourceKind.Literal),
                    bulletId + ": the clause is printed on the card, so it is a literal rather than " +
                    "something read off the board.");
                Assert.That(distinct.Literal.BooleanValue, Is.True,
                    bulletId + ": Middlefinger of Death prints \"(on different targets)\", which " +
                    "Ruling 19 makes the card's own statement - Whisper's Grim Reaper prints no such " +
                    "parenthetical and may point both bullets at one unit.");
            }
        }

        /// <summary>
        /// The pile clause is declared on the second bullet and NOWHERE else - not on the first bullet
        /// as a stated absence, and not on the action.
        /// </summary>
        [Test]
        public void ThePileClause_IsDeclaredOnTheOneBulletThatPrintsIt()
        {
            var text = AttackText();
            var adjacent = text.Nodes.Single(value => value.Id.Value == "middlefinger-adjacent");
            var discard = text.Nodes.Single(value => value.Id.Value == "middlefinger-discard");

            Assert.That(
                adjacent.Inputs.Any(input =>
                    input.ParameterName ==
                    SyntheticSequenceCapabilities.SubjectNarrowingParameter),
                Is.False,
                "\"Target a unit adjacent to you\" prints no qualifier, so its node declares none. An " +
                "unbound optional parameter IS the absence of the clause, which is what lets every " +
                "bullet composed before this face stay byte-identical.");
            Assert.That(
                adjacent.Outputs.Any(output =>
                    output.OutputName == SyntheticSequenceCapabilities.SubjectNarrowingOutput),
                Is.False,
                "and it publishes no narrowing fact either, so its outcome carries exactly the four " +
                "facts every attack-sequence bullet in the build has always carried.");

            var narrowing = discard.Inputs.Single(input =>
                input.ParameterName == SyntheticSequenceCapabilities.SubjectNarrowingParameter);
            Assert.That(narrowing.SourceKind, Is.EqualTo(EffectBindingSourceKind.Literal));
            Assert.That(narrowing.Literal.IntegerValue,
                Is.EqualTo((int)AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard));
            Assert.That(
                discard.Outputs.Any(output =>
                    output.OutputName == SyntheticSequenceCapabilities.SubjectNarrowingOutput),
                Is.True,
                "and it publishes the fact, because the plan the frame names is what carries the " +
                "clause into the sequence that will apply it.");
        }

        /// <summary>
        /// The narrowing travels as a CLOSED VOCABULARY rather than as the capability reference this
        /// extension point was originally sized as, and the capability's declared schema is what says
        /// so.
        ///
        /// <para>The distinction is the milestone's central design decision and it is asserted rather
        /// than left in a comment. Content load keeps a closed list of ordinary quantities; a parameter
        /// named from that list may not declare an enum domain, and one outside it must. Declaring the
        /// domain is also what makes a tampered snapshot a REFUSED payload rather than a reconstructed
        /// one, because the reader validates the value against the same vocabulary.</para>
        /// </summary>
        [Test]
        public void TheNarrowing_IsAnEnumBackedVocabularyAndIsOptional()
        {
            var handler = SyntheticSequenceCapabilities.Handlers().Single();
            var parameter = handler.Schema.Parameters.Single(value =>
                value.Name == SyntheticSequenceCapabilities.SubjectNarrowingParameter);

            Assert.That(parameter.ValueKind, Is.EqualTo(EffectValueKind.Integer));
            Assert.That(parameter.IsRequired, Is.False,
                "Optional, and that is what keeps every bullet composed before this face unchanged: " +
                "Whisper's Grim Reaper declares nothing here and its four bullets read exactly as " +
                "they did when the field did not exist.");
            Assert.That(parameter.IntegerDomain.IsDefined, Is.True,
                "A parameter outside content load's closed list of ordinary quantities is REQUIRED to " +
                "declare its enum domain - the same rule that made the reach parameter " +
                "`targetReach` rather than `reach`.");
            Assert.That(
                parameter.IntegerDomain.LegalValues.OrderBy(value => value).ToArray(),
                Is.EqualTo(new[]
                {
                    (int)AttackSequenceSubjectNarrowing.None,
                    (int)AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard,
                    (int)AttackSequenceSubjectNarrowing.HasAnEmptyDiscard
                }.OrderBy(value => value).ToArray()),
                "Three members as of milestone 8.35z53. The second printed clause - " +
                "Whisper's Swift Justice prints the COMPLEMENT, \"a hero in range with an EMPTY " +
                "discard\" - arrived as exactly the member and nothing more this comment predicted, " +
                "which is the whole reason this is an enum rather than a capability reference: the " +
                "next one costs no schema move. Because `EffectIntegerDomain.All<T>()` reads the enum " +
                "TYPE, the parameter's own domain admitted the new member automatically; this " +
                "assertion is the one place that has to be told by hand, and being told is the point " +
                "- a domain that silently grew would be a domain nobody was watching.");
        }

        // -------------------------------------------------------------------------------------------
        // What the clause DOES, on a board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE MILESTONE'S HEADLINE MEASUREMENT. On one board, at one instant, the two bullets are
        /// offered different candidate sets - and the only thing that differs between them is the
        /// printed pile clause.
        ///
        /// <para>The enemy hero is standing beside Dodger and inside her printed Range, so every other
        /// rule that could remove a candidate is satisfied and is PROVEN satisfied by the first bullet
        /// offering that very hero. Their discard is empty, and the second bullet offers nobody at all.
        /// That is the whole of what "who has one or more cards in the discard" means, measured as a
        /// difference rather than asserted as a property - and it is the reading milestone 8.21 settled
        /// for Brogan and 8.22 for Sabina: "who has" NARROWS who may be picked, so a hero who does not
        /// qualify is never offered rather than being offered and then refused.</para>
        ///
        /// <para><b>An Attack with nothing to take stops, and the card is spent.</b> That is p.10's
        /// mandatory-step rule and not a special case of this clause. The half worth stating is which
        /// tier of the two-tier modal rule applies: the BULLET is still offered at the modal, because
        /// whether a bullet's sequence will find a subject is a question about a board that does not
        /// exist yet - the FIRST sequence's own Defense window puts cards into exactly the pile the
        /// second bullet reads, so a bullet greyed on the present tense would be refused on a reading
        /// the rules have not reached. What IS judged up front is the ACTION: an Attack whose every
        /// declared bullet reaches nobody is greyed before it is offered, which is where this
        /// milestone's narrowing joins the offer-feasibility gate.</para>
        /// </summary>
        [Test]
        public void TheSecondBullet_OffersNobodyWithAnEmptyDiscard_WhereTheFirstOffersThatSameHero()
        {
            var withFirstBullet = StartMiddlefinger();
            ChooseOnlyTheBullet(withFirstBullet, 1);

            var firstChoice = withFirstBullet.Projection.PendingAttackTargetChoice;
            Assert.That(firstChoice, Is.Not.Null,
                "\"Target a unit adjacent to you\" - and the enemy hero is standing beside her.");
            Assert.That(firstChoice.LegalTargetHeroIds, Is.Not.Empty);
            Assert.That(
                withFirstBullet.Projection.Cards.Any(card =>
                    card.State.Location == CardLocation.DiscardPile),
                Is.False,
                "and NOBODY has discarded anything yet, which is what makes the contrast below about " +
                "the pile and about nothing else.");

            var withSecondBullet = StartMiddlefinger();
            ChooseOnlyTheBullet(withSecondBullet, 2);

            Assert.That(withSecondBullet.Projection.PendingAttackTargetChoice, Is.Null,
                "\"Target a hero in range WHO HAS one or more cards in the discard\". The same hero " +
                "the first bullet just offered is still adjacent and still inside the printed Range, " +
                "so reach, p.18's immunities and the target policy all admit them exactly as they did " +
                "a moment ago. Their discard is empty, so this bullet admits nobody - which is the " +
                "clause, isolated to a single degree of freedom.");
            Assert.That(withSecondBullet.Projection.HasPendingAttackResolution, Is.False,
                "and the Attack stops rather than standing open on an empty list, which is p.10's " +
                "mandatory step and not this clause's doing.");
        }

        /// <summary>
        /// "One or both" runs a complete rulebook p.14 per chosen bullet, and all of it is ONE action.
        /// </summary>
        [Test]
        public void ChoosingBoth_RunsACompletePageFourteenPerBullet()
        {
            var session = StartMiddlefinger();
            ChooseBothBullets(session);

            var first = session.Projection.PendingAttackTargetChoice;
            Assert.That(first, Is.Not.Null,
                "The modal is answered and the FIRST chosen bullet opens its own targeting.");
            Assert.That(first.ResolutionId.Value, Does.EndWith(":seq-1"),
                "Each sequence runs under an identity derived from the action's, because rulebook " +
                "p.13's Defeat outcome pays out once per identity and two sequences may each defeat.");

            RunOneSequence(session);

            Assert.That(CompletedSequences(session), Has.Length.EqualTo(2),
                "Both chosen bullets reached an outcome of their own. The second found no subject - " +
                "the card prints \"(on different targets)\" and the only hero in range is the one the " +
                "first sequence took - and a sequence that finds nobody is still a p.14 that ran.");
        }

        /// <summary>
        /// Ruling 19's rider, from the side the card is on: Middlefinger PRINTS "(on different
        /// targets)", so the unit the first sequence bound is gone from the second's set.
        /// </summary>
        [Test]
        public void TheDifferentTargetsClause_TakesTheFirstSequencesUnitOutOfTheSeconds()
        {
            var session = StartMiddlefinger();
            ChooseBothBullets(session);
            RunOneSequence(session);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "The second sequence has no candidate. On this board that is over-determined and " +
                "deliberately so: the card's own \"(on different targets)\" excludes the unit the " +
                "first sequence bound, and Middlefinger's printed Attack of 6 defeated that hero " +
                "outright. Both readings say the same thing here, which is why the pile clause is " +
                "measured in the case above instead - on a board where nothing has been bound at all.");
            Assert.That(session.Projection.HasPendingAttackResolution, Is.False,
                "and the whole action is over, rather than one sequence of it.");
        }

        // -------------------------------------------------------------------------------------------
        // The new field crosses: restore and undo, each ISOLATED from the other
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A capture taken mid-chain round-trips byte-identically THROUGH the new field, and the
        /// restored session goes on to owe exactly the sequences the payload said it owed.
        ///
        /// <para><b>Why this is the case that proves the schema move was needed.</b> The plan standing
        /// here names two bullets, and the second one carries
        /// <see cref="AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard"/>. A build that
        /// wrote the plan without that field would restore a second bullet that admits every enemy hero
        /// in range, empty pile or not - so the payload has to carry it, and carrying it is what moved
        /// the version.</para>
        ///
        /// <para>Restore is exercised here on its own, and undo on its own below. Milestone 8.35z34's
        /// two-question pause defect was found only by isolating them: a composed drive can pass while
        /// one of the two paths is broken, because the other one repairs the state on the way past.</para>
        /// </summary>
        [Test]
        public void APauseMidChain_RestoresByteIdenticallyAndStillOwesItsSecondSequence()
        {
            var session = StartMiddlefinger();
            ChooseBothBullets(session);

            var standing = session.Projection.PendingAttackTargetChoice;
            Assert.That(standing, Is.Not.Null);

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateEngagedStagedDodgerTierThreeSession();
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "Schema v40 carries the whole plan - the action that opened it, both chosen bullets " +
                "with each one's printed targeting sentence AND the predicate each prints about its " +
                "candidate, and the units earlier sequences bound - so a mid-chain capture " +
                "round-trips to the same bytes.");

            var restoredChoice = restored.Projection.PendingAttackTargetChoice;
            Assert.That(restoredChoice, Is.Not.Null);
            Assert.That(restoredChoice.ResolutionId, Is.EqualTo(standing.ResolutionId));
            Assert.That(
                restoredChoice.LegalTargetHeroIds.OrderBy(value => value.Value).ToArray(),
                Is.EqualTo(standing.LegalTargetHeroIds.OrderBy(value => value.Value).ToArray()));

            // Driven ON THE RESTORED SESSION rather than the original: a plan that came back short a
            // bullet, or short a bullet's clause, would still answer the assertions above and only
            // diverge once the chain is asked to continue.
            RunOneSequence(restored);
            Assert.That(CompletedSequences(restored), Has.Length.EqualTo(2),
                "The restored action owed a second sequence and ran it.");
        }

        /// <summary>
        /// The same plan captured one command later, inside the first sequence's own Defense window -
        /// the state where p.14 is half-way through a sequence AND the chain still owes another.
        /// </summary>
        [Test]
        public void APauseInsideASequencesDefenseWindow_RestoresByteIdentically()
        {
            var session = StartMiddlefinger();
            ChooseBothBullets(session);
            var choice = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(choice.LegalTargetHeroIds[0]));

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "each sequence has a Defense window of its own, which is half of what makes it a " +
                "complete p.14 rather than a rider on the first");

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateEngagedStagedDodgerTierThreeSession();
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload));
            Assert.That(restored.Projection.PendingDefenseChoice, Is.Not.Null);
        }

        /// <summary>
        /// Undo, exercised on its own rather than after a restore. A modal Attack rewound from its
        /// first sequence's targeting goes back to the modal that opened it - so the plan the chain was
        /// carrying is genuinely discarded rather than left standing beside a rewound board.
        /// </summary>
        [Test]
        public void APauseMidChain_IsUndoneBackToTheModalThatOpenedIt()
        {
            var session = StartMiddlefinger();
            ChooseBothBullets(session);

            var before = session.CaptureSnapshot();
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Not.Null);

            var undo = session.Undo();
            Assert.That(undo.Accepted, Is.True, undo.Detail);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "the sequence that was choosing a target is gone");
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "and what stands in its place is the modal itself, still asking which bullets - which " +
                "is the command the rewind landed on rather than a chain left half-built.");
            Assert.That(session.CaptureSnapshot(), Is.Not.EqualTo(before),
                "and the rewind really moved the state, so this case is not passing on a no-op.");
        }

        // -------------------------------------------------------------------------------------------
        // Enfeeblement, which is the reason this face was parked for two milestones
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Enfeeblement's "cannot repeat actions" leaves a choose-both Attack WHOLE, which is the
        /// defect the Dodger phase-two worker predicted and the reason the multi-sequence contract
        /// exists at all.
        ///
        /// <para>Both cards are Dodger's, so this is not a cross-hero hypothetical: had Middlefinger
        /// been composed on <c>EffectNodeKind.RepeatAction</c> - the only shape that produced a second
        /// complete p.14 before milestone 8.35z27 - then her own Enfeeblement would silently have
        /// forbidden her own card's second bullet, a rule neither face prints. PLAN Ruling 14 scopes
        /// "cannot repeat actions" to p.10's Repeat KEYWORD alone.</para>
        ///
        /// <para>The case asserts both halves, and the first half is the anti-vacuity one: the
        /// restriction has to be REAL for its absence to mean anything.</para>
        /// </summary>
        [Test]
        public void Enfeeblement_LeavesAChooseBothAttackWhole()
        {
            Assert.That(
                DodgerActiveEffectContent.EnfeeblementDefinition.Modifications.Any(modification =>
                    modification.Kind == RuleModificationKind.Restriction &&
                    modification.Activities.Contains(RuleActivity.Repeat)),
                Is.True,
                "Enfeeblement really does declare p.10's Repeat restriction. Without this the case " +
                "below would pass against a card that restricts nothing.");

            var text = AttackText();
            Assert.That(
                text.Nodes.Any(node => node.Kind == EffectNodeKind.RepeatAction), Is.False,
                "and Middlefinger of Death composes no repeat node, so there is nothing on this face " +
                "for that restriction to reach. Its second sequence is a second p.14 order, which the " +
                "multi-sequence contract runs WITHOUT labelling it a repeat - the implementation is " +
                "shared with p.14 wholesale and the LABEL deliberately is not.");

            var session = StartMiddlefinger();
            ChooseBothBullets(session);
            RunOneSequence(session);

            Assert.That(CompletedSequences(session), Has.Length.EqualTo(2),
                "and both sequences ran.");
            Assert.That(
                session.Projection.RoundEvents.Any(value =>
                    value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.BasicActionPerformed &&
                    value.TurnEvent.BasicAction.WasRepeatedInstance),
                Is.False,
                "with nothing in the turn recorded as a repeated instance, which is the flag every " +
                "repeat reader in this build keys on.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Dodger at Tier III on the first upgrade branch with Middlefinger of Death committed and its
        /// Attack started, standing at rulebook p.14's step 1 with the modal asking which bullets.
        /// </summary>
        private static MatchSession StartMiddlefinger()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedStagedDodgerTierThreeSession();
            var middlefinger = session.Projection.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    DodgerCardContent.MiddlefingerOfDeath) &&
                card.State.Location == CardLocation.Hand);

            // The enemy commits its LOWEST Initiative, so Dodger's Initiative of 10 leaves her acting
            // first - these cases are about her turn and would otherwise be about Wasp's.
            var enemy = session.Projection.Cards
                .Where(card =>
                    card.State.Location == CardLocation.Hand &&
                    !card.OwnerId.Equals(middlefinger.OwnerId))
                .OrderBy(card => InitiativeOf(session, card))
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();

            Accept(session, DirectorCommand.CommitCard(
                middlefinger.OwnerId, middlefinger.Id));
            Accept(session, DirectorCommand.CommitCard(enemy.OwnerId, enemy.Id));
            Assert.That(
                session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(middlefinger.Id),
                "The fixture's Initiative values have to leave Middlefinger of Death acting, or every " +
                "case here would be asserting about the other hero's turn.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary &&
                offer.AttackRange != AttackRangeKind.None);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "A modal Attack declares `attack-target@2`: what its step-1 text names is a plan, so " +
                "the action starts straight into p.14's step 1 with nothing to choose yet.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "and the modal is what stands there instead.");
            return session;
        }

        private static void ChooseBothBullets(MatchSession session)
        {
            var modal = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(modal.SelectInvocations[0]));
            var afterFirst = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(afterFirst.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        /// <summary>Answers the modal with one bullet: 1 for the adjacent unit, 2 for the pile clause.</summary>
        private static void ChooseOnlyTheBullet(MatchSession session, int ordinal)
        {
            var modal = session.Projection.PendingEffectChoice;
            Assert.That(modal.SelectInvocations, Has.Count.EqualTo(2));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                modal.SelectInvocations[ordinal - 1]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        /// <summary>Runs the whole of the standing sequence's p.14 against its first legal hero.</summary>
        private static void RunOneSequence(MatchSession session)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(choice.LegalTargetHeroIds[0]));

            var defense = session.Projection.PendingDefenseChoice;
            if (defense == null)
            {
                return;
            }

            Accept(
                session,
                defense.LegalDefenseOffers.Count > 0
                    ? DirectorCommand.ChooseDefenseCard(
                        defense.LegalDefenseOffers[0].SourceCardInstanceId)
                    : DirectorCommand.DeclineDefense());
        }

        /// <summary>Every p.14 order this action ran to an outcome.</summary>
        private static CardActionResolutionEvent[] CompletedSequences(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value =>
                    value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.AttackActionProgressed &&
                    value.TurnEvent.CardActionEvent.Kind ==
                        CardActionResolutionEventKind.AttackResolutionCompleted)
                .Select(value => value.TurnEvent.CardActionEvent)
                .ToArray();

        private static int InitiativeOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Initiative.Value;

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True,
                $"{command.Kind} was refused: {result.RejectionReason}");
        }

        private static EffectDefinition AttackText()
        {
            var definition = DodgerCardContent.AllDefinitions.Single(value =>
                value.Id.Equals(DodgerCardContent.MiddlefingerOfDeath));
            var primary = definition.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Attack &&
                action.Id.Equals(DodgerCardContent.PrimaryRangedAttack));

            Assert.That(primary.EffectRoot, Is.Not.Null,
                "Middlefinger of Death composes a body at milestone 8.35z45. If this is " +
                "null the face has been re-parked, and every case below is about a card that no " +
                "longer exists.");
            Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack),
                "Its bullets ARE the targeting sentence, so they are applied at rulebook p.14's step " +
                "1 - before an Attack Total is announced against a target that does not exist yet.");
            return primary.EffectRoot;
        }
    }
}
