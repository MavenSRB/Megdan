using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40c. The three readings of "a hero has a [bounty] bounty marker" Bain the Bounty
    /// Hunter's set prints - the predicate his five Green faces open with, the selection narrowing his
    /// Hand Crossbow's first bullet prints, and the p.14 attack-sequence narrowing Hunter-Seeker's does -
    /// proved as vocabulary before any of his content exists.
    ///
    /// <para><b>NOTHING IN THE CATALOG READ MARKER STATE AS A QUESTION, AND THE PROBE MEASURED IT
    /// RATHER THAN ASSUMING IT.</b> Across the 171 capabilities this build registers, the only
    /// parameter or output named after a marker at all is the <c>holder</c> the three marker-GIVING
    /// handlers take and publish. The narrowings that exist are board, geometry and card-state only.
    /// The first case below is that sweep, kept as a guard rather than thrown away.</para>
    ///
    /// <para><b>ONE CLAUSE, THREE DECLARATIONS THAT CARRY DIFFERENT THINGS, ONE QUERY.</b> A capability
    /// handler can be handed a <see cref="MarkerDefinition"/> at construction, which is how the give
    /// already keeps a payload from naming a different marker. A <c>choose-target@2</c> declaration
    /// cannot: <see cref="EffectValueKind"/> names booleans, quantities, heroes, minions, cards, spaces
    /// and nodes, and none of them is a marker. A persisted <c>AttackSequenceSubjectNarrowing</c> can
    /// carry no content at all. So the two narrowings say whose marker instead - the ACTOR's, which is
    /// who "a bounty marker" belongs to on every face that prints the clause - and
    /// <see cref="HeldMarkerQuery"/> turns that back into a definition. All three read
    /// <see cref="HeldMarkerQuery.HoldersOf"/> in the end, which is why the agreement case below is an
    /// assertion about one board rather than an argument about three files.</para>
    ///
    /// <para><b>The board is featureless on purpose</b>, on <c>BainMarkerDefeatCostTests</c>' and
    /// <c>ContinuedLineBoard</c>'s terms: a radius-3 disk, three heroes standing on it and nothing else,
    /// so the only thing that can remove a candidate is the clause under test. Two of the three heroes
    /// are enemies of the actor and both are adjacent to him, which is what makes "admits exactly the
    /// holders" a set difference rather than a property.</para>
    ///
    /// <para>No Bain content here. These cases bind the parameter and the enum member from probe nodes
    /// and probe bullets; the milestone that composes his faces is 8.40i.</para>
    /// </summary>
    [TestFixture]
    public sealed class BainMarkerPredicateTests
    {
        // -------------------------------------------------------------------------------------------
        // (a) What the catalog reads today
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The probe milestone 8.40c ordered, kept as a guard and RE-MEASURED at milestone 8.40j: the
        /// registered catalog read marker state nowhere but inside a give until his dealing, and the
        /// list below is exactly what the dealing added to it - nothing else in the build grew a
        /// marker-named parameter or output in the same change.
        /// </summary>
        [Test]
        public void TheRegisteredCatalog_ReadsMarkerStateOnlyWhereBainsDealingPutIt()
        {
            var capabilities = MatchContentCatalogs.ForThisBuild().Capabilities;
            var readers = capabilities.Registered
                .Select(capabilities.Load)
                .Where(handler => handler.Schema.Parameters.Select(parameter => parameter.Name)
                    .Concat(handler.Schema.Outputs.Select(output => output.OutputName))
                    .Any(name =>
                        name.IndexOf("marker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("holder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("attach", StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(handler => handler.Reference.ToString())
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(readers, Is.EqualTo(new[]
                {
                    "bain-bounty-marker-held@1",
                    "bain-give-bounty-marker@1",
                    "bain-transfer-bounty-marker@1",
                    "choose-target@2",
                    "synthetic-give-marker@1",
                    "synthetic-marker-held@1",
                    "synthetic-transfer-marker@1",
                    "tigerclaw-give-poison-marker-1@1",
                    "tigerclaw-give-poison-marker-2@1"
                }),
                "Measured, not remembered. Before milestone 8.40c the only marker-named parameter or " +
                "output in the whole registered catalog was the hero a GIVE takes and publishes - " +
                "three handlers, and nothing anywhere that asked whether a marker is WORN, which is " +
                "the gap 8.40c filled. `choose-target@2` carries that milestone's own seventh " +
                "narrowing and it is default-off: the corpus case below asserts that no declaration " +
                "written before it binds it. Milestone 8.40j added the other four by DEALING him - " +
                "his own predicate and transfer, and the two showcase instances over the fixture's " +
                "Focus marker that milestone 7.9's gate asks of a new handler TYPE. His return " +
                "appears in neither list, which is a fact about its schema rather than an omission: " +
                "it names no hero at all.");
            Assert.That(capabilities.IsRegistered(BainComponentContent.BountyHeldV1), Is.True,
                "and the predicate built at 8.40c is registered now that he is dealt, which is what " +
                "makes the sweep above the dealt build's own reading rather than a stale one.");
        }

        // -------------------------------------------------------------------------------------------
        // The predicate
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ThePredicate_AnswersTrueAndPublishesTheHolder()
        {
            var fixture = new MarkerPredicateFixture();
            fixture.GiveBountyTo(fixture.EnemyA);

            var result = fixture.AskThePredicate();

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                result.Outcomes.Single(outcome => outcome.OutputName ==
                    SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName)
                    .Value.BooleanValue,
                Is.True,
                "\"If a hero in play has a bounty marker\" - one does.");
            Assert.That(
                result.Outcomes.Single(outcome => outcome.OutputName ==
                    SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName)
                    .Value.HeroId,
                Is.EqualTo(fixture.EnemyA),
                "and WHICH hero, because Close Call's consequence is about THAT hero - a predicate " +
                "answering only true or false would leave 8.40b's transfer with nothing to bind.");
        }

        [Test]
        public void ThePredicate_AnswersFalseAndPublishesNothingWhenNobodyWearsIt()
        {
            var fixture = new MarkerPredicateFixture();

            var result = fixture.AskThePredicate();

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                result.Outcomes.Single(outcome => outcome.OutputName ==
                    SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName)
                    .Value.BooleanValue,
                Is.False);
            Assert.That(
                result.Outcomes.Select(outcome => outcome.OutputName),
                Has.No.Member(SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName),
                "No hero is published rather than an undefined one, so a later node binding the " +
                "holder has nothing to bind rather than something meaningless.");
        }

        /// <summary>
        /// A hero wearing Tigerclaw's Poison is not wearing a bounty, asserted on a board carrying both
        /// so the case cannot pass by there being nothing else to confuse.
        /// </summary>
        [Test]
        public void ThePredicate_IsUnaffectedByAMarkerOfADifferentDefinition()
        {
            var poisonOnly = new MarkerPredicateFixture();
            poisonOnly.GivePoisonTo(poisonOnly.EnemyB, owner: poisonOnly.EnemyA);

            Assert.That(poisonOnly.Markers.Attachments, Has.Count.EqualTo(1),
                "The Poison marker is genuinely standing, so the negative below is not an empty " +
                "board answering it.");
            Assert.That(poisonOnly.PredicateHolders(), Is.Empty,
                "Poison is a different definition and the question is asked about one definition.");

            var both = new MarkerPredicateFixture();
            both.GivePoisonTo(both.EnemyB, owner: both.EnemyA);
            both.GiveBountyTo(both.EnemyA);

            Assert.That(both.Markers.Attachments, Has.Count.EqualTo(2));
            Assert.That(both.PredicateHolders(), Is.EqualTo(new[] { both.EnemyA }),
                "and with both on the board the answer names the bounty's holder alone.");
        }

        /// <summary>
        /// BA-D5: "a hero in play" is any hero on the board, Bain included. A marker Close Call has just
        /// pulled onto him still satisfies Vantage Point's condition, which is what "(The marker's
        /// effect is applied to you)" tells the reader to expect.
        /// </summary>
        [Test]
        public void ThePredicate_CountsAMarkerTheActorHimselfWears()
        {
            var fixture = new MarkerPredicateFixture();
            fixture.GiveBountyTo(fixture.Actor);

            Assert.That(fixture.PredicateHolders(), Is.EqualTo(new[] { fixture.Actor }),
                "BA-D5. The Green faces ask about the board rather than about a side.");
        }

        /// <summary>
        /// The holder is published only where the declaring node asked for it - <c>chosenSpace</c>'s
        /// rule, and what keeps Perfect Getaway (which prints no consequence at all) from recording a
        /// fact its own snapshot would then have to carry.
        /// </summary>
        [Test]
        public void ThePredicate_PublishesTheHolderOnlyWhereTheNodeAsksForIt()
        {
            var fixture = new MarkerPredicateFixture();
            fixture.GiveBountyTo(fixture.EnemyA);

            var asked = fixture.AskThePredicate();
            var notAsked = fixture.AskThePredicate(publishHolder: false);

            Assert.That(asked.Outcomes.Select(outcome => outcome.OutputName),
                Has.Member(SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName));
            Assert.That(notAsked.Outcomes.Select(outcome => outcome.OutputName),
                Is.EqualTo(new[]
                {
                    SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName
                }),
                "A node that asks only the question records only the answer.");
        }

        [Test]
        public void ThePredicate_ReportsImpossibleWithNoRegisterRatherThanAnsweringFalse()
        {
            var fixture = new MarkerPredicateFixture();

            var result = fixture.AskThePredicate(withComponents: false);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A silent no would look exactly like a board with no marker out, which is the " +
                "failure EffectCardState.Absent exists to refuse.");
            Assert.That(fixture.ThePredicateHandler.Anticipate(fixture.PredicateContext(
                    publishHolder: true, withComponents: false)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and the probe says the same thing the execution does.");
        }

        /// <summary>
        /// PROBE FINDING (a), ASSERTED: how the two halves of this predicate are consumed, and the one
        /// shape that does NOT work.
        ///
        /// <para>The Boolean half is read by a <see cref="EffectNodeKind.Conditional"/> naming the
        /// capability directly - <c>EffectDefinitionValidator</c> admits any capability that declares a
        /// Boolean output, and the session branches on any true Boolean outcome. That is Perfect
        /// Getaway's whole shape.</para>
        ///
        /// <para><b>The Hero half cannot be bound from inside that Conditional's own branch</b>, and
        /// this is the finding worth carrying into 8.40b and 8.40g rather than discovering there. The
        /// validator's guaranteed-predecessor rule is explicit that only a Sequence contributes: a
        /// Conditional's branch inherits what its PARENT already guaranteed and never the Conditional
        /// itself. So a face whose consequence is about "that hero" composes the predicate as an
        /// ordinary Capability node standing first in a Sequence, and branches with
        /// <c>synthetic-is-flagged@1</c> - which is exactly the shape that capability was minted for.
        /// Both shapes are asserted here, and the refused one is asserted as refused.</para>
        /// </summary>
        [Test]
        public void ThePredicate_IsAConditionalPredicateAndItsHolderBindsIntoALaterNode()
        {
            var catalog = CatalogWithBainsMarkerHandlers();

            Assert.That(
                EffectDefinitionValidator.Validate(PerfectGetawayShape(), catalog), Is.Empty,
                "A Conditional may name the predicate directly, because it declares a Boolean output " +
                "to branch on.");
            Assert.That(
                EffectDefinitionValidator.Validate(CloseCallShape(), catalog), Is.Empty,
                "and a face whose consequence is about THAT hero runs the predicate first and binds " +
                "Out(n).holder inside a synthetic-is-flagged@1 branch, which is what 8.40b's transfer " +
                "and 8.40g's Block will do.");

            var refused = EffectDefinitionValidator.Validate(
                BindingTheConditionalsOwnOutputInsideItsBranch(), catalog);
            Assert.That(refused.Select(rejection => rejection.Reason),
                Has.Member(EffectDefinitionRejectionReason.NodeOutputNotGuaranteed),
                "THE SHAPE THAT DOES NOT WORK, pinned: a Conditional's own branch does not guarantee " +
                "the Conditional, so a branch child cannot bind the predicate node's Hero output. A " +
                "composition that tried would fail content load rather than silently bind nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The seventh narrowing on choose-target@2
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSeventhNarrowing_AdmitsExactlyTheHoldersAndNothingElse()
        {
            var fixture = new MarkerPredicateFixture();

            Assert.That(fixture.SelectionCandidates(narrowToHolders: false),
                Is.EqualTo(new[] { fixture.EnemyB, fixture.EnemyA }),
                "The premise: unnarrowed, both enemy heroes stand beside him and both are offered.");
            Assert.That(fixture.SelectionCandidates(narrowToHolders: true), Is.Empty,
                "Narrowed with no marker out, the clause admits nobody rather than falling back to " +
                "the unnarrowed set.");

            fixture.GiveBountyTo(fixture.EnemyA);
            fixture.GivePoisonTo(fixture.EnemyB, owner: fixture.EnemyA);

            Assert.That(fixture.SelectionCandidates(narrowToHolders: true),
                Is.EqualTo(new[] { fixture.EnemyA }),
                "\"Target a hero in range with a bounty marker\": the bounty's holder and not the " +
                "hero wearing a marker of a different definition.");
            Assert.That(fixture.SelectionCandidates(narrowToHolders: false),
                Is.EqualTo(new[] { fixture.EnemyB, fixture.EnemyA }),
                "and the unnarrowed clause on the very same board still offers both, so the " +
                "difference above is the parameter and nothing else.");
        }

        [Test]
        public void TheSeventhNarrowing_IsOptionalAndDefaultsOff()
        {
            var parameter = ChooseTargetHandler().Schema.Parameters.Single(candidate =>
                candidate.Name == SyntheticRosterCapabilities.HoldsTheActorsMarkerParameter);

            Assert.That(parameter.ValueKind, Is.EqualTo(EffectValueKind.Boolean));
            Assert.That(parameter.IsRequired, Is.False,
                "Optional and default-off, which is what keeps every declaration written before this " +
                "milestone at its exact candidate set.");
            Assert.That(
                ChooseTargetHandler().Schema.Parameters
                    .Where(candidate => !candidate.IsRequired)
                    .Select(candidate => candidate.Name)
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "activity",
                    SyntheticRosterCapabilities.AdmittedMinionClassParameter,
                    SyntheticRosterCapabilities.ExcludeTargetParameter,
                    SyntheticRosterCapabilities.PlayedAttackThisTurnParameter,
                    SyntheticRosterCapabilities.AdjacentToFriendlyMinionParameter,
                    SyntheticRosterCapabilities.AdjacentToAnotherEnemyUnitParameter,
                    SyntheticRosterCapabilities.ExcludeAdjacentParameter,
                    SyntheticRosterCapabilities.ContinuedLineFromParameter,
                    SyntheticRosterCapabilities.HoldsTheActorsMarkerParameter,

                    // Milestone 8.57c. Rowenna's two Blue Defeats, "an enemy hero adjacent to you
                    // with no cards in hand" - appended rather than inserted, which is exactly what
                    // this assertion exists to notice.
                    SyntheticRosterCapabilities.RequiresEmptyHandParameter
                }),
                "The six printed narrowings this milestone found, in their own order, with the " +
                "seventh appended - measured rather than remembered, so a later insertion that " +
                "reordered them is told about here. 8.57c appended the eighth.");
        }

        /// <summary>
        /// The claim every optional narrowing in this catalog owes, asserted over the DECLARATIONS the
        /// corpus actually contains rather than over a remembered list of faces: no declaration written
        /// before this milestone binds the new parameter, so none of them can have moved.
        /// </summary>
        [Test]
        public void EveryDeclarationWrittenBeforeThisMilestone_BindsNoMarkerNarrowing()
        {
            var nodes = WholeCorpus()
                .SelectMany(entry => entry.Definition.Nodes)
                .Where(node => node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetV2))
                .ToArray();

            Assert.That(nodes.Length, Is.EqualTo(75),
                "RE-MEASURED at 8.75a: the 8.69 wave's 71 plus Ignatia the Mad's four, which are " +
                "her four SUN attack lines that name an adjacent unit - Chaos Bolt ('a minion " +
                "adjacent to you'), Unreliable Jolt ('a hero adjacent to you'), Crack of Doom and " +
                "Imminent Eruption ('a unit adjacent to you'). Her other thirteen composed faces " +
                "reach their subject through the four card-text ranges minted in " +
                "`IgnatiaAttackTargetingContent`, the two discard subjects in " +
                "`IgnatiaDiscardTargetingContent` or a printed Radius, and none of them binds this " +
                "capability. Her composition landed at the 8.75 inventory commit, which ran only " +
                "`--filter Ignatia` and never this suite, so the number was already out of date on " +
                "the branch this block was cut from; it is re-measured here rather than left red. " +
                "RE-MEASURED at the 8.69 wave merge: 8.69e's 69 plus 8.69c's two - Mountain Guide's " +
                "and Expedition Leader's first sentences (a friendly unit adjacent to you), none on " +
                "Decoy, whose bullets read a printed Radius icon. " +
                "RE-MEASURED at 8.69e: 8.61c's 66 plus Brynn's three trap faces, whose first bullet " +
                "is 'An enemy hero adjacent to you' - one each on Bear Trap, Log Trap and Deadfall " +
                "Trap, and none on True Grit or Die Hard, which name no unit at all. His second " +
                "bullet is the printed-Radius selection and is counted nowhere here. " +
                "RE-MEASURED at 8.61c: 8.57d's 63 plus Min's three 'Push an enemy unit adjacent to " +
                "you' subject selections - one each on Crane, Tiger and Dragon Stance, and none on " +
                "Viper or Cobra, whose riders name no unit at all. (8.57d's own count was 8.57c's 59 " +
                "plus Rowenna's four 'that enemy hero' selections: one each on Close Quarters and " +
                "Melee, and TWO on Grand Melee, whose printed 'up to two times' is two sibling " +
                "performances and therefore two namings.) A different count means a face was added " +
                "or removed - read which, then re-measure rather than editing this number to match.");
            Assert.That(
                nodes.SelectMany(node => node.Inputs).Select(input => input.ParameterName).Distinct()
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[]
                {
                    "activity",
                    SyntheticRosterCapabilities.AdjacentToAnotherEnemyUnitParameter,
                    SyntheticRosterCapabilities.AdjacentToFriendlyMinionParameter,
                    SyntheticRosterCapabilities.ContinuedLineFromParameter,
                    SyntheticRosterCapabilities.ExcludeAdjacentParameter,
                    SyntheticRosterCapabilities.ExcludeTargetParameter,
                    SyntheticRosterCapabilities.OriginParameter,
                    SyntheticRosterCapabilities.PlayedAttackThisTurnParameter,
                    SyntheticRosterCapabilities.DistanceParameter,
                    SyntheticRosterCapabilities.RelationParameter,
                    SyntheticRosterCapabilities.UnitKindParameter,

                    // Milestone 8.57c. Rowenna's two Blue Defeats bind the empty-hand narrowing, so
                    // the corpus now contains it - and 8.40c's marker narrowing is still bound by
                    // nobody, which is what this case is about.
                    SyntheticRosterCapabilities.RequiresEmptyHandParameter
                }.OrderBy(name => name, StringComparer.Ordinal).ToArray()),
                "and the parameter this milestone added is bound by none of them, which is the whole " +
                "of what \"every declaration keeps its exact candidate set\" means for a filter that " +
                "does nothing unless it is switched on.");
            Assert.That(
                nodes.SelectMany(node => node.Inputs).Select(input => input.ParameterName),
                Has.No.Member(SyntheticRosterCapabilities.HoldsTheActorsMarkerParameter),
                "8.40c's own parameter, stated as its own assertion now that the list above has " +
                "grown a later milestone's entry: the claim is about the MARKER narrowing.");
        }

        /// <summary>
        /// One query, read twice: the set the offer opens with and the set the answer is checked
        /// against are the same set, so a hero the unnarrowed clause would have offered cannot be
        /// submitted.
        /// </summary>
        [Test]
        public void TheNarrowedOfferAndItsAnswer_ReadTheOneQuery()
        {
            var fixture = new MarkerPredicateFixture();
            fixture.GiveBountyTo(fixture.EnemyA);
            var handler = ChooseTargetHandler();
            var context = fixture.SelectionContext(narrowToHolders: true);

            Assert.That(handler.Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                handler.Resume(context, new[] { EffectValue.Hero(fixture.EnemyA) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The holder is the answer the offer named.");
            Assert.That(
                handler.Resume(context, new[] { EffectValue.Hero(fixture.EnemyB) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and a hero the unnarrowed clause WOULD have offered is refused by the same query " +
                "that left them out of the offer, rather than by a second rule written beside it.");
        }

        // -------------------------------------------------------------------------------------------
        // The fourth AttackSequenceSubjectNarrowing member
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheFourthMember_NarrowsABulletsSubjectsToHolders()
        {
            var fixture = new MarkerPredicateFixture();

            Assert.That(
                fixture.BulletSubjects(AttackSequenceSubjectNarrowing.HoldsTheActorsMarker), Is.Empty,
                "With no marker out the bullet admits nobody, and p.10's mandatory step is what " +
                "stops the Attack - not this clause pretending it was never printed.");

            fixture.GiveBountyTo(fixture.EnemyA);
            fixture.GivePoisonTo(fixture.EnemyB, owner: fixture.EnemyA);

            Assert.That(
                fixture.BulletSubjects(AttackSequenceSubjectNarrowing.HoldsTheActorsMarker),
                Is.EqualTo(new[] { fixture.EnemyA }),
                "\"Target a hero in range with a bounty marker\" on Hunter-Seeker's first bullet.");
            Assert.That(
                fixture.BulletSubjects(AttackSequenceSubjectNarrowing.None),
                Is.EqualTo(new[] { fixture.EnemyB, fixture.EnemyA }),
                "and the same board through a bullet that prints no qualifier still reaches both, so " +
                "reach, p.18's immunities and the printed policy are all proven to admit the hero " +
                "the clause removed.");
        }

        [Test]
        public void ABulletDeclaringNone_IsUnaffectedByAMarkerOnTheBoard()
        {
            var fixture = new MarkerPredicateFixture();
            var before = fixture.BulletSubjects(AttackSequenceSubjectNarrowing.None);

            fixture.GiveBountyTo(fixture.EnemyA);

            Assert.That(fixture.BulletSubjects(AttackSequenceSubjectNarrowing.None), Is.EqualTo(before),
                "A bullet that prints no qualifier reads no component, which is what an unnarrowed " +
                "bullet meant before this milestone and still means.");
        }

        /// <summary>
        /// The two existing members keep their VALUES, so no payload written earlier decodes as a
        /// different predicate, and keep their BEHAVIOUR, asserted by putting a marker on the board and
        /// observing that the pile-reading members do not notice.
        /// </summary>
        [Test]
        public void TheTwoExistingMembers_KeepTheirValuesAndAnswerFromThePileNotTheMarker()
        {
            Assert.That((int)AttackSequenceSubjectNarrowing.None, Is.Zero);
            Assert.That((int)AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard,
                Is.EqualTo(1));
            Assert.That((int)AttackSequenceSubjectNarrowing.HasAnEmptyDiscard, Is.EqualTo(2));
            Assert.That((int)AttackSequenceSubjectNarrowing.HoldsTheActorsMarker, Is.EqualTo(3),
                "The enum grows by one at the end, with the existing members keeping their values.");

            var fixture = new MarkerPredicateFixture();
            var withCards = fixture.BulletSubjects(
                AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard);
            var withEmpty = fixture.BulletSubjects(AttackSequenceSubjectNarrowing.HasAnEmptyDiscard);

            fixture.GiveBountyTo(fixture.EnemyA);

            Assert.That(
                fixture.BulletSubjects(AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard),
                Is.EqualTo(withCards),
                "Dodger's clause reads a discard pile and a bounty is not one.");
            Assert.That(
                fixture.BulletSubjects(AttackSequenceSubjectNarrowing.HasAnEmptyDiscard),
                Is.EqualTo(withEmpty),
                "and so does Whisper's complement, so neither moved when the marker arrived.");
            Assert.That(withEmpty, Is.EqualTo(new[] { fixture.EnemyB, fixture.EnemyA }),
                "The board's own premise: nobody has discarded anything, so the empty-pile member " +
                "admits both and the one above it admits neither - which is what makes the two " +
                "assertions above measurements rather than tautologies.");
            Assert.That(withCards, Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // The one question the block answers rather than meets
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The predicate, the selection narrowing and the bullet narrowing, asked about ONE board at one
        /// instant. Two readings of one clause is exactly the divergence a shared label must never hide,
        /// so this is asserted rather than argued from the fact that all three call one method.
        /// </summary>
        [Test]
        public void ThePredicateAndBothNarrowings_AgreeOnOneBoard()
        {
            var fixture = new MarkerPredicateFixture();
            fixture.GiveBountyTo(fixture.EnemyA);
            fixture.GivePoisonTo(fixture.EnemyB, owner: fixture.EnemyA);

            var predicate = fixture.PredicateHolders();
            var selection = fixture.SelectionCandidates(narrowToHolders: true);
            var bullet = fixture.BulletSubjects(AttackSequenceSubjectNarrowing.HoldsTheActorsMarker);

            Assert.That(predicate, Is.EqualTo(new[] { fixture.EnemyA }));
            Assert.That(selection, Is.EqualTo(predicate),
                "The selection admits exactly the hero the predicate names.");
            Assert.That(bullet, Is.EqualTo(predicate),
                "and so does the p.14 bullet - one clause, three declarations, one answer.");

            var bare = new MarkerPredicateFixture();
            Assert.That(bare.PredicateHolders(), Is.Empty);
            Assert.That(bare.SelectionCandidates(narrowToHolders: true), Is.Empty);
            Assert.That(
                bare.BulletSubjects(AttackSequenceSubjectNarrowing.HoldsTheActorsMarker), Is.Empty,
                "and they agree about the empty board too, which is the half a shared label could " +
                "still have got differently.");
        }

        // -------------------------------------------------------------------------------------------
        // The registration
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The predicate cleared milestone 7.9's gate beside the give, and the two owed the dealing
        /// different things: the give's handler TYPE is Tigerclaw's, already registered and already
        /// showcased, while this one's was new at 8.40c and took a showcase consumer at 8.40j.
        /// </summary>
        [Test]
        public void ThePredicate_IsRegisteredBesideTheGiveAndTookAShowcaseConsumer()
        {
            Assert.That(BainComponentContent.DeferredHandlers(), Is.Empty);

            var registered = BainComponentContent.RegisteredHandlers().ToArray();
            Assert.That(registered.Select(handler => handler.Reference),
                Is.EqualTo(new[]
                {
                    BainComponentContent.GiveBountyV1, BainComponentContent.TransferBountyV1,
                    BainComponentContent.ReturnBountyV1, BainComponentContent.BountyHeldV1
                }));
            Assert.That(
                registered.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.BountyHeldV1)),
                Is.InstanceOf<SyntheticComponentCapabilities.MarkerHolderHandler>(),
                "A NEW handler type: nothing in the catalog read marker state as a question before " +
                "milestone 8.40c, which is why this instance took a showcase consumer at the dealing " +
                "where the give did not.");

            var capabilities = MatchContentCatalogs.ForThisBuild().Capabilities;
            Assert.That(capabilities.IsRegistered(BainComponentContent.BountyHeldV1), Is.True,
                "A dealt card starts it, which is what milestone 7.9's reachability gate asks before " +
                "a capability may be registered.");
            Assert.That(capabilities.IsRegistered(BainComponentContent.GiveBountyV1), Is.True,
                "and the give it was written beside moved in the same change, so 8.40j moved one list.");
            Assert.That(
                capabilities.IsRegistered(SyntheticComponentCapabilities.MarkerHeldV1), Is.True,
                "and the showcase consumer this TYPE owed is registered over the fixture's own Focus " +
                "marker, so his predicate is not the first place the type ever runs.");
        }

        // -------------------------------------------------------------------------------------------
        // Shapes the validator is asked about
        // -------------------------------------------------------------------------------------------

        private static readonly EffectNodeId Root = new EffectNodeId("bain-probe-root");
        private static readonly EffectNodeId Predicate = new EffectNodeId("bain-probe-predicate");
        private static readonly EffectNodeId Branch = new EffectNodeId("bain-probe-branch");
        private static readonly EffectNodeId Consequence = new EffectNodeId("bain-probe-consequence");

        private static EffectCapabilityCatalog CatalogWithBainsMarkerHandlers()
        {
            var registered = SyntheticEffectCapabilities.CreateCatalog();
            return new EffectCapabilityCatalog(
                registered.Registered.Select(registered.Load)
                    .Concat(BainComponentContent.Handlers()));
        }

        /// <summary>"If a hero in play has a bounty marker, block the attack" - the condition alone.</summary>
        private static EffectDefinition PerfectGetawayShape() =>
            new EffectDefinition(
                new EffectDefinitionId("bain-probe-perfect-getaway"),
                "probe-rev-1",
                Root,
                new[]
                {
                    new EffectNodeDefinition(
                        Root,
                        EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        BainComponentContent.BountyHeldV1,
                        childNodeIds: new[] { Consequence },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean)
                        }),
                    GiveBinding(Consequence, bindHolderFrom: default)
                });

        /// <summary>
        /// "If a hero in play has a bounty marker, ... that hero gives the marker to you" - the predicate
        /// first, the branch second, and the consequence binding the hero the predicate named.
        /// </summary>
        private static EffectDefinition CloseCallShape() =>
            new EffectDefinition(
                new EffectDefinitionId("bain-probe-close-call"),
                "probe-rev-1",
                Root,
                new[]
                {
                    new EffectNodeDefinition(
                        Root,
                        EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Predicate, Branch }),
                    PredicateNode(),
                    new EffectNodeDefinition(
                        Branch,
                        EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { Consequence },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag",
                                Predicate,
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    GiveBinding(Consequence, bindHolderFrom: Predicate)
                });

        /// <summary>The shape the guaranteed-predecessor rule refuses, kept so the finding is a case.</summary>
        private static EffectDefinition BindingTheConditionalsOwnOutputInsideItsBranch() =>
            new EffectDefinition(
                new EffectDefinitionId("bain-probe-refused"),
                "probe-rev-1",
                Predicate,
                new[]
                {
                    new EffectNodeDefinition(
                        Predicate,
                        EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        BainComponentContent.BountyHeldV1,
                        childNodeIds: new[] { Consequence },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean),
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName,
                                EffectValueKind.Hero)
                        }),
                    GiveBinding(Consequence, bindHolderFrom: Predicate)
                });

        private static EffectNodeDefinition PredicateNode() =>
            new EffectNodeDefinition(
                Predicate,
                EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                BainComponentContent.BountyHeldV1,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                        EffectValueKind.Boolean),
                    new EffectOutputBinding(
                        SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName,
                        EffectValueKind.Hero)
                });

        private static EffectNodeDefinition GiveBinding(
            EffectNodeId id, EffectNodeId bindHolderFrom) =>
            new EffectNodeDefinition(
                id,
                EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                BainComponentContent.GiveBountyV1,
                inputs: bindHolderFrom.IsDefined
                    ? new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticComponentCapabilities.MarkerGivingHandler.HolderParameterName,
                            bindHolderFrom,
                            SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName,
                            EffectValueKind.Hero)
                    }
                    : null,
                outputs: new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) });

        // -------------------------------------------------------------------------------------------
        // The corpus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed effect definition the build's verified content classes contain, found by
        /// reflection rather than by a list of heroes - the shape milestone 8.38d gave the sweep guard
        /// in <c>AttackTargetBindingConditionalChooseOfferSweepTests</c>, so a sixteenth hero cannot be
        /// added without this sweep seeing him.
        /// </summary>
        private static IEnumerable<(string Owner, EffectDefinition Definition)> WholeCorpus()
        {
            // Milestone 8.61g. The reflection is shared rather than copied, and it now admits a hero
            // whose set is composed by a parallel wave and therefore has no aggregate yet - see
            // VerifiedContentCorpus for the whole reading.
            foreach (var type in VerifiedContentCorpus.ContentClasses())
            {
                var cards = VerifiedContentCorpus.AllDefinitionsOf(type);

                foreach (var card in cards)
                {
                    foreach (var action in card.ActionDefinitions)
                    {
                        if (action.EffectRoot != null)
                        {
                            yield return (type.Name, action.EffectRoot);
                        }

                        if (action.SecondCardTextRoot != null)
                        {
                            yield return (type.Name, action.SecondCardTextRoot);
                        }
                    }
                }
            }
        }

        private static IEffectCapabilityHandler ChooseTargetHandler() =>
            SyntheticRosterCapabilities.RegisteredHandlers()
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .Single(handler =>
                    handler.Reference.Equals(SyntheticRosterCapabilities.ChooseTargetV2));

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One featureless radius-3 disk, three heroes - the actor and two enemies, both adjacent to him
        /// - and one live marker register, read by all three declarations this milestone touches.
        /// </summary>
        private sealed class MarkerPredicateFixture
        {
            private static readonly ActionDefinitionId ProbeAction =
                new ActionDefinitionId("bain-probe-action");
            private static readonly CardInstanceId ProbeInstance =
                new CardInstanceId("bain-probe-instance");

            private readonly MatchConfiguration configuration;
            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly CardActionResolutionAuthority authority;
            private readonly ActionOffer attackOffer;
            private int minted;

            public MarkerPredicateFixture()
            {
                configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                participants = StagedBoardParticipants.CreateWithBlueAlly();
                Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                EnemyA = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                EnemyB = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
                board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(EnemyA, new HexCoordinate(1, 0)),
                        new HeroOccupancy(EnemyB, new HexCoordinate(0, 1))
                    });
                authority = new CardActionResolutionAuthority(
                    MatchContentCatalogs.ForThisBuild(),
                    board,
                    Markers,
                    new HeroProgressionState(participants),
                    new TeamLifeState(participants, 8));
                attackOffer = ActionOfferQuery.GetAvailableOffers(
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        Actor,
                        ActionOfferWindow.NormalPlay)
                    .Single(candidate =>
                        candidate.SourceCardInstanceId.Value == "red-pursuit-card" &&
                        candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            }

            public HeroId Actor { get; }
            public HeroId EnemyA { get; }
            public HeroId EnemyB { get; }
            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();

            public IEffectCapabilityHandler ThePredicateHandler =>
                BainComponentContent.Handlers().Single(handler =>
                    handler.Reference.Equals(BainComponentContent.BountyHeldV1));

            public MarkerAttachment GiveBountyTo(HeroId holder) =>
                Attach(BainComponentContent.BountyDefinition, holder, Actor);

            /// <summary>
            /// Tigerclaw's Poison, owned by a hero other than the actor - which is what a marker of a
            /// different definition actually looks like on a board, because the owner of an attachment
            /// is the hero whose card put it out.
            /// </summary>
            public MarkerAttachment GivePoisonTo(HeroId holder, HeroId owner) =>
                Attach(TigerclawComponentContent.PoisonOneDefinition, holder, owner);

            public EffectExecutionResult AskThePredicate(
                bool publishHolder = true, bool withComponents = true) =>
                ThePredicateHandler.Execute(PredicateContext(publishHolder, withComponents));

            public IReadOnlyList<HeroId> PredicateHolders()
            {
                var result = AskThePredicate();
                Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
                return result.Outcomes
                    .Where(outcome => outcome.OutputName ==
                        SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName)
                    .Select(outcome => outcome.Value.HeroId)
                    .ToArray();
            }

            public EffectCapabilityContext PredicateContext(
                bool publishHolder, bool withComponents)
            {
                var outputs = new List<EffectOutputBinding>
                {
                    new EffectOutputBinding(
                        SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                        EffectValueKind.Boolean)
                };
                if (publishHolder)
                {
                    outputs.Add(new EffectOutputBinding(
                        SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName,
                        EffectValueKind.Hero));
                }

                return Context(
                    new Dictionary<string, EffectValue>(), outputs, withComponents);
            }

            /// <summary>
            /// "A hero in radius", with and without the printed marker clause: a radius-1 disk of enemy
            /// heroes, which on this board is both of them.
            /// </summary>
            public IReadOnlyList<HeroId> SelectionCandidates(bool narrowToHolders)
            {
                var opened = ChooseTargetHandler().Execute(SelectionContext(narrowToHolders));
                return opened.Kind != EffectExecutionResultKind.NeedsChoice
                    ? Array.Empty<HeroId>()
                    : opened.Choice.Candidates.Select(candidate => candidate.HeroId).ToArray();
            }

            public EffectCapabilityContext SelectionContext(bool narrowToHolders)
            {
                var inputs = new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.OriginParameter] =
                        EffectValue.Integer((int)RosterQueryOrigin.Actor),
                    [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(1),
                    [SyntheticRosterCapabilities.UnitKindParameter] =
                        EffectValue.Integer((int)RosterUnitKind.Hero),
                    [SyntheticRosterCapabilities.RelationParameter] =
                        EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
                };
                if (narrowToHolders)
                {
                    inputs[SyntheticRosterCapabilities.HoldsTheActorsMarkerParameter] =
                        EffectValue.Boolean(true);
                }

                return Context(
                    inputs,
                    new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    withComponents: true);
            }

            /// <summary>
            /// The heroes one p.14 sequence would admit for a bullet printing
            /// <paramref name="narrowing"/>, read at the one site the backend evaluates it: reached by
            /// reflection because the backend is internal to Core and Core publishes no
            /// InternalsVisibleTo for this assembly, exactly as <c>BainMarkerDefeatCostTests</c> reaches
            /// milestone 8.22's effect-invoked Defeat.
            /// </summary>
            public IReadOnlyList<HeroId> BulletSubjects(AttackSequenceSubjectNarrowing narrowing)
            {
                var backendField = typeof(CardActionResolutionAuthority).GetField(
                    "attackDefenseBackend", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(backendField, Is.Not.Null,
                    "CardActionResolutionAuthority still holds one Attack/Defense backend.");
                var backend = backendField.GetValue(authority);
                var find = backend.GetType().GetMethod(
                    "FindLegalAttackTargets", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(find, Is.Not.Null,
                    "and the bullet's predicate is still evaluated where its target set is derived.");

                var admitted = (IEnumerable<AttackTarget>)find.Invoke(backend, new object[]
                {
                    Actor,
                    attackOffer,
                    configuration.CardDefinitions,
                    participants,
                    RuleModificationSet.Empty,
                    new AttackSequenceBullet(
                        "bain-probe-bullet",
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged),
                        narrowing),
                    configuration.CardInstances
                });

                return admitted
                    .Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId)
                    .ToArray();
            }

            private MarkerAttachment Attach(
                MarkerDefinition definition, HeroId holder, HeroId owner)
            {
                var attachment = new MarkerAttachment(
                    new MarkerInstanceId(
                        $"{definition.Reference.DefinitionId.Value}-{++minted}"),
                    definition,
                    owner,
                    holder,
                    ProbeInstance,
                    ProbeAction);
                Markers.Give(attachment);
                return attachment;
            }

            private EffectCapabilityContext Context(
                IReadOnlyDictionary<string, EffectValue> inputs,
                IReadOnlyList<EffectOutputBinding> declaredOutputs,
                bool withComponents) =>
                new EffectCapabilityContext(
                    new EffectNodeId("bain-probe-node"),
                    EffectFailurePolicy.Mandatory,
                    Actor,
                    Actor,
                    ProbeInstance,
                    new CardDefinitionReference(
                        new CardDefinitionId("bain-probe-card"), "probe-rev-1"),
                    ProbeAction,
                    inputs,
                    1,
                    1,
                    participants,
                    null,
                    RuleModificationSet.Empty,
                    EffectCardState.Absent,
                    ActionKind.Skill,
                    withComponents
                        ? new MatchComponents(
                            GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions
                                .CreateCatalog(),
                            board,
                            Markers,
                            board,
                            board,
                            board,
                            board)
                        : null,
                    declaredOutputs: declaredOutputs);

            private static HexCoordinate[] FeaturelessDisk(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates.ToArray();
            }
        }
    }
}
