using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71a. Hanu the Trickster's Hurry up! - "Set the printed [Initiative] Initiative
    /// value of an unresolved card of a hero in range to 11, until it is resolved or otherwise
    /// changes state. ( This may change the initiative order and tie breakers )."
    ///
    /// <para><b>What the cases are about.</b> Four separable things, and each is measured where it
    /// actually lives. WHO may be named is the selection's, and every case there is a board that
    /// changes exactly one word of the printed clause - which team, how far, whether a hero holds
    /// p.10's unresolved card, whether p.18 lets them be targeted at all. WHAT the rewrite does is
    /// the printed-value query's: the icon reads eleven and everything that composes onto a printed
    /// icon still composes. WHEN it ends is the card lifecycle's, one case per exit. And whether it
    /// SURVIVES is the snapshot's.</para>
    ///
    /// <para><b>Why the capability cases sit at the effect FRAME rather than on a configured match.</b>
    /// Nothing of Hanu's is registered - milestone 7.9's gate holds his content out of
    /// <see cref="MatchContentCatalogs"/> until 8.71h deals him - so no composed match can put this
    /// card in a hand and offer its Skill. <c>BrynnTrackingSwapTests</c> and
    /// <c>MinSmokeBombTests</c> record the same boundary for the same reason. The turn-order and
    /// snapshot cases do NOT have that boundary, because neither needs his card: the rewrite is a
    /// fact about a card instance, and the internal-test configuration has card instances.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class HanuHurryUpTests
    {
        private static readonly HeroId Hanu = new HeroId("hanu-trickster");
        private static readonly HeroId Ally = new HeroId("hanu-blue-ally");
        private static readonly HeroId Enemy = new HeroId("hanu-red-enemy");
        private static readonly HeroId FarEnemy = new HeroId("hanu-red-far-enemy");

        private static readonly TeamId BlueTeam = new TeamId("hanu-blue-team");
        private static readonly TeamId RedTeam = new TeamId("hanu-red-team");
        private static readonly MapId Map = new MapId("hanu-hurry-up-map");

        private static readonly HexCoordinate HanuAt = new HexCoordinate(0, 0);

        /// <summary>Two spaces out, and his own side - the clause names no team.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(2, 0);

        /// <summary>Three spaces out, and the other side.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(3, 0);

        /// <summary>Five spaces out: outside the printed Range 4 and inside it with one p.16 item.</summary>
        private static readonly HexCoordinate FarEnemyAt = new HexCoordinate(5, 0);

        private const string TestRevision = "hanu-hurry-up-test-revision";

        // -------------------------------------------------------------------------------------------
        // The printed face
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The face, as his shard prints it: Silver basic, Initiative 12, Defense 1, NO Movement icon
        /// at all, a Basic Skill primary carrying a Range 4 icon, and no item.
        /// </summary>
        [Test]
        public void TheFace_IsTheSilverBasicHisShardPrints()
        {
            var face = HurryUpFace();

            Assert.That(face.DisplayName, Is.EqualTo("Hurry up!"));
            Assert.That(face.Initiative.Value, Is.EqualTo(12));
            Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(face.Progression.IsBasic, Is.True,
                "Silver is one of p.6's two basic cards, so it climbs no ladder and hands out no " +
                "item.");

            var primary = face.ActionDefinitions.Single(
                action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill),
                "The printed band reads BASIC SKILL - RANGED, and p.12 makes the card text the " +
                "action.");
            Assert.That(primary.Id, Is.EqualTo(HanuCardContent.PrimaryBasicSkill));
            Assert.That(
                primary.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range),
                "The Ranged band's icon is a RANGE, read by the card's own text rather than by " +
                "p.13's targeting - Bain's Get over here! is the shipped precedent.");
            Assert.That(primary.BaseValues.Single().Amount, Is.EqualTo(4));

            Assert.That(
                face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                    .BaseValues.Single().Amount,
                Is.EqualTo(1));
            Assert.That(
                face.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement), Is.False,
                "HA-06: the Movement icon is ABSENT on this face, never zero, so there is no " +
                "Movement action to declare with a zero in it.");
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(HanuCardContent.HurryUpText));
        }

        /// <summary>
        /// The body is the two-node mandatory sequence the sentence is, and every clause the
        /// selection carries is written into the definition rather than left to a default.
        /// </summary>
        [Test]
        public void TheBody_IsOneMandatorySequenceOfASelectionAndARewrite()
        {
            var body = HurryUpBody();
            var root = body.Nodes.Single(node => node.Id.Value == HanuEffectContent.HurryUpRootNode);

            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Sequence));
            Assert.That(root.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory));
            Assert.That(
                root.ChildNodeIds.Select(child => child.Value).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuEffectContent.HurryUpTargetNode, HanuEffectContent.HurryUpRewriteNode
                }),
                "Select, then rewrite - and both mandatory, so a board with nobody to name is " +
                "p.10's impossible mandatory step rather than a card that does nothing.");

            var target = Node(body, HanuEffectContent.HurryUpTargetNode);
            Assert.That(
                target.Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.RangeHeroTargetV1),
                "\"a hero in range\" is the shipped printed-Range hero selection word for word.");
            Assert.That(
                Literal(target, SyntheticTargetingCapabilities.TargetRelationParameterName)
                    .IntegerValue,
                Is.EqualTo((int)SourceTeamRelation.AnyTeam),
                "The card names no side, so neither does the definition (HA-Q1).");
            Assert.That(
                Literal(target, SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName)
                    .BooleanValue,
                Is.True,
                "\"an unresolved card of a hero in range\" is bound, not left unsaid.");
            Assert.That(
                Literal(target, SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName)
                    .BooleanValue,
                Is.False,
                "Required on the Range arm and false here: this face is no Block rider and names " +
                "no attacker.");
            Assert.That(
                target.Inputs.Any(input =>
                    input.ParameterName ==
                    SyntheticTargetingCapabilities.IncludeSelfParameterName),
                Is.False,
                "Hanu is left out by saying nothing, which is p.10's own rule: an action affects " +
                "you only when the text says so.");

            var rewrite = Node(body, HanuEffectContent.HurryUpRewriteNode);
            Assert.That(
                rewrite.Capability,
                Is.EqualTo(HanuComponentContent.HurryUpInitiativeV1));
            Assert.That(rewrite.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory));
            Assert.That(
                Literal(rewrite, HanuComponentContent.HurryUpPrintedInitiativeParameterName)
                    .IntegerValue,
                Is.EqualTo(11),
                "The card's own printed eleven, declared at the call site rather than written into " +
                "the handler.");
            Assert.That(
                rewrite.Outputs.Select(output => output.OutputName).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuEffectContent.HurryUpTargetOutputName,
                    HanuEffectContent.HurryUpRewrittenCardOutputName
                }),
                "The hook milestone 8.71b rides: the hero the rewrite HAPPENED to, and the card it " +
                "happened on.");
        }

        // -------------------------------------------------------------------------------------------
        // Who may be named
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE OFFER. One board and every word of "a hero in range" holding "an unresolved
        /// card" doing its own work: the friendly hero two spaces out and the enemy hero three spaces
        /// out are both named, because the clause names no side; the enemy five spaces out is not,
        /// because the printed Range 4 does not reach him; and Hanu is not, although he is holding an
        /// unresolved card of his own.
        /// </summary>
        [Test]
        public void TheOffer_IsEveryHeroInRangeOnEitherTeam_AndNeverHanuHimself()
        {
            var world = World.Staged();

            Assert.That(Named(world.RunToPause()),
                Is.EqualTo(new[] { Ally.Value, Enemy.Value }),
                "Both sides, in ordinal order, and nobody else.");
        }

        /// <summary>
        /// He is not a candidate even though he holds exactly the card the clause describes, which is
        /// what makes the exclusion p.10's rule rather than an accident of the board.
        /// </summary>
        [Test]
        public void HanuIsNeverACandidate_ThoughHisOwnCardIsUnresolved()
        {
            var world = World.Staged();

            Assert.That(
                HeroUnresolvedCardQuery.Holds(world.Instances, Hanu), Is.True,
                "The fixture must actually leave his own card unresolved, or this case proves " +
                "nothing.");
            Assert.That(Named(world.RunToPause()), Has.No.Member(Hanu.Value),
                "p.10: an action affects you only when the text says so, and this text does not.");
        }

        /// <summary>
        /// p.16's Range item widens the disk and nothing else. Same board, same hero, and the only
        /// difference is one item under the matching icon.
        /// </summary>
        [Test]
        public void ARangeItem_WidensTheOffer()
        {
            Assert.That(HexDistanceQuery.Calculate(HanuAt, FarEnemyAt), Is.EqualTo(5));

            Assert.That(Named(World.Staged().RunToPause()), Has.No.Member(FarEnemy.Value),
                "Five spaces is outside the printed Range 4.");
            Assert.That(
                Named(World.Staged(rangeItems: 1).RunToPause()),
                Is.EqualTo(new[] { Ally.Value, Enemy.Value, FarEnemy.Value }),
                "and one p.16 item raises the printed icon by one, which reaches him.");
        }

        /// <summary>
        /// A hero in range whose card is NOT unresolved is not offered - and the three readings p.10
        /// separates are all measured on one board: a hero who has resolved their card, a hero whose
        /// card is still face down, and a hero who has played nothing at all.
        /// </summary>
        [Test]
        public void AHeroWithNoUnresolvedCard_IsNeverOffered()
        {
            Assert.That(
                Named(World.Staged(allyState: CardShape.Resolved).RunToPause()),
                Is.EqualTo(new[] { Enemy.Value }),
                "A resolved card is not an unresolved one - p.10's own word.");
            Assert.That(
                Named(World.Staged(allyState: CardShape.FaceDown).RunToPause()),
                Is.EqualTo(new[] { Enemy.Value }),
                "A facedown card's printed characteristics are suppressed, so there is no printed " +
                "Initiative there to set.");
            Assert.That(
                Named(World.Staged(allyState: CardShape.InHand).RunToPause()),
                Is.EqualTo(new[] { Enemy.Value }),
                "A hero who has played nothing holds no card the clause is about.");
        }

        /// <summary>
        /// The narrowing is an OPTIONAL BOOLEAN on the printed-Range hero arm, which is the shape 8.57c
        /// settled. Since milestone 8.88 every arm that can offer a hero declares it too, optional, and
        /// the minion arm refuses it by name - a minion holds no card.
        /// </summary>
        [Test]
        public void TheNarrowing_IsAnOptionalBooleanOnEveryArmOfferingAHero()
        {
            var handlers = SyntheticTargetingCapabilities.Handlers().ToArray();

            Assert.That(
                Declares(
                    handlers, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                    SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName),
                Is.True,
                "Hurry up! prints a Range 4 icon, so the Range hero arm is the one that declares " +
                "it.");
            var range = Parameter(
                handlers, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName);
            Assert.That(range.ValueKind, Is.EqualTo(EffectValueKind.Boolean),
                "A hero has an unresolved card or has not - there is no threshold to name, and an " +
                "unbound optional Integer would read as zero.");
            Assert.That(range.IsRequired, Is.False,
                "so every hero selection composed before Hanu keeps its exact candidate set " +
                "without naming it.");

            foreach (var reference in new[]
                     {
                         SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                         SyntheticTargetingCapabilities.RangeUnitTargetV1,
                         SyntheticTargetingCapabilities.RangeMinionTargetV1,
                         SyntheticTargetingCapabilities.RangeTargetV1
                     })
            {
                // Milestone 8.88 reversed the rule this loop used to pin: every arm that can offer a
                // hero now declares the clause, optional, and the minion arm refuses it by name
                // because a minion holds no card.
                var refusal = SyntheticTargetingCapabilities.PrintedIconFilterRefusal(
                    reference, SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName);
                Assert.That(
                    Declares(
                        handlers, reference,
                        SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName),
                    Is.EqualTo(refusal == null),
                    reference + " declares the clause exactly when it does not refuse it by name.");
            }
        }

        /// <summary>
        /// An IMMUNE hero in range is not offered. p.19 makes this a target, so p.18 narrows the
        /// offer exactly as it narrows every other printed-icon selection - and the clause is
        /// measured by changing nothing but the immunity.
        /// </summary>
        [Test]
        public void AnImmuneHeroInRange_IsNeverOffered()
        {
            Assert.That(Named(World.Staged().RunToPause()), Has.Member(Enemy.Value));
            Assert.That(
                Named(World.Staged(immuneHero: Enemy).RunToPause()),
                Is.EqualTo(new[] { Ally.Value }),
                "\"you cannot target that unit\" - and the narrowing is at the offer, so he is " +
                "never offered rather than offered and then refused.");
        }

        /// <summary>
        /// A board on which no hero in range holds an unresolved card is p.10's impossible mandatory
        /// step: the selection refuses rather than offering an empty question, which is what greys
        /// the primary at the offer.
        /// </summary>
        [Test]
        public void ABoardWithNobodyToName_IsAnImpossibleMandatoryStep()
        {
            var world = World.Staged(
                allyState: CardShape.Resolved, enemyState: CardShape.Resolved);

            var result = world.Run();
            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses: the step that could have asked cannot be taken.");
            Assert.That(
                world.Instances.Any(card => card.InitiativeOverride.IsDefined), Is.False,
                "p.10 stops the ACTION, so nothing of the sentence happens - no card is rewritten " +
                "and no question was ever put.");
        }

        // -------------------------------------------------------------------------------------------
        // What the rewrite does
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE REWRITE. Naming the enemy sets HIS card's printed Initiative to eleven, leaves
        /// every other card alone, and publishes the two facts 8.71b rides.
        /// </summary>
        [Test]
        public void NamingAHero_SetsThatHerosOwnCardToEleven_AndPublishesTheHook()
        {
            var world = World.Staged();
            var choice = world.RunToPause();

            world.AnswerToCompletion(choice, Pick(choice, Enemy));

            Assert.That(world.CardOf(Enemy).InitiativeOverride.IsDefined, Is.True);
            Assert.That(world.CardOf(Enemy).InitiativeOverride.Value, Is.EqualTo(11));
            Assert.That(
                world.CardOf(Enemy).InitiativeOverride.SourceCardInstanceId,
                Is.EqualTo(World.SourceCardId),
                "The rewrite names the card whose text performed it, which is what lets a seat and " +
                "a restore say which sentence is in effect.");
            Assert.That(world.CardOf(Ally).InitiativeOverride.IsDefined, Is.False,
                "One card, and the one the answer named.");
            Assert.That(world.CardOf(Hanu).InitiativeOverride.IsDefined, Is.False);
        }

        /// <summary>
        /// THE HOOK MILESTONE 8.71b RIDES, asked of the node that publishes it. The Ultimate Trick
        /// prints "You choose the next action, and how it is performed for a hero you target with the
        /// 'Hurry Up!'", so what it needs is the hero a rewrite actually happened to - which is why
        /// this is published by the rewrite and not by the selection above it.
        /// </summary>
        [Test]
        public void TheRewriteNode_PublishesTheTargetedHeroAndTheRewrittenCard()
        {
            var world = World.Staged();

            var result = world.InvokeRewrite(Enemy);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                result.Outcomes.Select(fact => fact.OutputName).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuComponentContent.TargetedHeroOutputName,
                    HanuComponentContent.RewrittenCardOutputName
                }));
            Assert.That(
                Published(result, HanuEffectContent.HurryUpTargetOutputName).HeroId,
                Is.EqualTo(Enemy),
                "8.71b binds this output of this node to learn which hero Hurry up! targeted.");
            Assert.That(
                Published(result, HanuEffectContent.HurryUpRewrittenCardOutputName).CardInstanceId,
                Is.EqualTo(world.CardOf(Enemy).Id),
                "and the card beside it, for a consumer that has to watch the CARD, because that is " +
                "what the rewrite lapses with.");
        }

        /// <summary>
        /// The rewrite node refuses a hero holding nothing the sentence is about, rather than
        /// rewriting something else - the live re-derivation milestone 6.8l's rule requires, because
        /// what was true when the offer opened is not what the answer applies to.
        /// </summary>
        [Test]
        public void TheRewriteNode_RefusesAHeroWhoseCardIsNoLongerUnresolved()
        {
            var world = World.Staged();
            Assert.That(
                CardLifecycleAuthority.TryResolveAfterAction(world.CardOf(Enemy)), Is.True);

            Assert.That(
                world.InvokeRewrite(Enemy).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(world.CardOf(Enemy).InitiativeOverride.IsDefined, Is.False);
            Assert.That(
                world.Instances.Any(card => card.InitiativeOverride.IsDefined), Is.False,
                "and no other card was rewritten instead.");
        }

        /// <summary>
        /// The rewrite replaces the printed ICON and nothing else: what comes back is the one
        /// <see cref="ValueContributionKind.PrintedIcon"/> contribution an unrewritten card comes
        /// back with, naming the same card, with a different number in it.
        /// </summary>
        [Test]
        public void TheRewrittenValue_IsThePrintedIconAndNotASecondContribution()
        {
            var world = World.Staged();
            var card = world.CardOf(Enemy);

            var before = PrintedValueQuery.CalculatePrintedInitiative(card, world.Definitions);
            Assert.That(before.Total, Is.EqualTo(12));
            Assert.That(before.Contributions.Count, Is.EqualTo(1));

            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(card, 11, World.SourceCardId),
                Is.True);

            var after = PrintedValueQuery.CalculatePrintedInitiative(card, world.Definitions);
            Assert.That(after.Total, Is.EqualTo(11));
            Assert.That(after.Contributions.Count, Is.EqualTo(1),
                "\"Set the PRINTED value\" - so the rewritten number IS the printed contribution " +
                "rather than an addend beside it, and everything that composes onto a printed icon " +
                "still composes onto this one.");
            Assert.That(
                after.Contributions.Single().Kind, Is.EqualTo(ValueContributionKind.PrintedIcon));
            Assert.That(
                after.Contributions.Single().SourceCardInstanceId, Is.EqualTo(card.Id));
        }

        /// <summary>
        /// A second Hurry up! on the same card sets eleven again rather than stacking, which is what
        /// "set" means (HA-Q1) - measured by rewriting to a different number first, so the case can
        /// tell a replacement from an addition.
        /// </summary>
        [Test]
        public void ASecondRewriteOfTheSameCard_SetsRatherThanStacks()
        {
            var world = World.Staged();
            var card = world.CardOf(Enemy);

            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(card, 4, World.SourceCardId),
                Is.True);
            Assert.That(
                PrintedValueQuery.CalculatePrintedInitiative(card, world.Definitions).Total,
                Is.EqualTo(4));

            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    card, 11, World.SecondSourceCardId),
                Is.True);
            Assert.That(
                PrintedValueQuery.CalculatePrintedInitiative(card, world.Definitions).Total,
                Is.EqualTo(11),
                "Eleven, not fifteen: the sentence sets a value and nothing here adds one.");
            Assert.That(
                card.InitiativeOverride.SourceCardInstanceId, Is.EqualTo(World.SecondSourceCardId),
                "and the newest sentence is the one in effect.");
        }

        /// <summary>
        /// A card the sentence cannot be about is refused rather than quietly rewritten: the
        /// transition admits exactly p.10's unresolved shape.
        /// </summary>
        [Test]
        public void TheTransition_AdmitsOnlyAnUnresolvedPlayedCard()
        {
            var world = World.Staged(
                allyState: CardShape.Resolved, enemyState: CardShape.FaceDown);

            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    world.CardOf(Ally), 11, World.SourceCardId),
                Is.False,
                "A resolved card is not an unresolved one.");
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    world.CardOf(Enemy), 11, World.SourceCardId),
                Is.False,
                "A facedown card's printed characteristics are suppressed.");
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    world.HandCardOf(FarEnemy), 11, World.SourceCardId),
                Is.False,
                "A card in hand has not been played at all.");
        }

        // -------------------------------------------------------------------------------------------
        // The turn order
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE POINT OF THE CARD. A rewritten card is ordered at eleven: below a twelve, above a ten,
        /// and it is the same board and the same three cards each time.
        /// </summary>
        [Test]
        public void TheRewrittenCardIsOrderedAtEleven_BelowATwelveAndAboveATen()
        {
            var order = new Order();
            order.Play(Hanu, TeamSide.Blue, 12);
            order.Play(Enemy, TeamSide.Red, 14);
            order.Play(Ally, TeamSide.Blue, 10);

            Assert.That(order.NextActor(), Is.EqualTo(Enemy),
                "Fourteen acts first while nothing has been rewritten.");

            order.Reset();
            order.Rewrite(Enemy, 11);
            Assert.That(order.NextActor(), Is.EqualTo(Hanu),
                "His fourteen now reads eleven, so the twelve is highest.");

            order.Reset();
            order.Rewrite(Hanu, 11);
            order.Rewrite(Enemy, 11);
            Assert.That(order.Totals()[Ally], Is.EqualTo(10),
                "and a card nobody rewrote still reads the number it prints.");
        }

        /// <summary>
        /// p.16's Initiative item and a hero-scoped modifier still compose ON TOP of the rewritten
        /// icon, because the rewritten icon IS a printed icon. Same card, same eleven, and the total
        /// the round is ordered by moves with each addend.
        /// </summary>
        [Test]
        public void ItemsAndHeroModifiers_ComposeOnTopOfTheRewrittenIcon()
        {
            var plain = new Order();
            plain.Play(Enemy, TeamSide.Red, 14);
            plain.Rewrite(Enemy, 11);
            Assert.That(plain.Totals()[Enemy], Is.EqualTo(11));

            var withItem = new Order(initiativeItemsFor: Enemy);
            withItem.Play(Enemy, TeamSide.Red, 14);
            withItem.Rewrite(Enemy, 11);
            Assert.That(withItem.Totals()[Enemy], Is.EqualTo(12),
                "p.16's item raises the printed icon, and the rewritten number is the printed icon.");

            var withModifier = new Order(heroModifierFor: Enemy);
            withModifier.Play(Enemy, TeamSide.Red, 14);
            withModifier.Rewrite(Enemy, 11);
            Assert.That(withModifier.Totals()[Enemy], Is.EqualTo(13),
                "and a hero-scoped Initiative modifier - Xargatha's Metamorphosis, Tigerclaw's " +
                "poison marker - adds on top for the same reason.");
        }

        /// <summary>
        /// Two cards rewritten to eleven on opposite sides is p.7's cross-team tie, broken by the Tie
        /// Breaker coin exactly as a printed tie is - which is the parenthesis the card prints being
        /// true rather than special-cased.
        /// </summary>
        [Test]
        public void ATieAtElevenIsBrokenByTheCoin()
        {
            var red = new Order(TeamSide.Red);
            red.Play(Hanu, TeamSide.Blue, 12);
            red.Play(Enemy, TeamSide.Red, 14);
            red.Rewrite(Hanu, 11);
            red.Rewrite(Enemy, 11);
            Assert.That(red.NextActor(), Is.EqualTo(Enemy),
                "The revealed side acts first, which is p.7's own rule.");

            var blue = new Order(TeamSide.Blue);
            blue.Play(Hanu, TeamSide.Blue, 12);
            blue.Play(Enemy, TeamSide.Red, 14);
            blue.Rewrite(Hanu, 11);
            blue.Rewrite(Enemy, 11);
            Assert.That(blue.NextActor(), Is.EqualTo(Hanu),
                "and the same board with the coin the other way up picks the other hero, so the " +
                "coin is what decided it.");
        }

        /// <summary>
        /// A rewrite written BETWEEN two selections reorders the rest of the round, which is p.7's
        /// "After the player who acted first has resolved their card, check the Initiative again"
        /// doing the work the card's parenthesis promises.
        /// </summary>
        [Test]
        public void ARewriteBetweenTwoSelections_ReordersTheRestOfTheRound()
        {
            var order = new Order();
            order.Play(Hanu, TeamSide.Blue, 13);
            order.Play(Enemy, TeamSide.Red, 12);
            order.Play(Ally, TeamSide.Blue, 10);

            Assert.That(order.NextActor(), Is.EqualTo(Hanu));
            order.Rewrite(Enemy, 9);
            order.Finish(Hanu);

            Assert.That(order.NextActor(), Is.EqualTo(Ally),
                "The enemy's twelve now reads nine, so the ten goes before him - and nothing had " +
                "to be recomputed for that, because the order is asked afresh at every selection.");
        }

        // -------------------------------------------------------------------------------------------
        // When it ends
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "until it is resolved or otherwise changes state" - one case per exit p.10 knows, and all
        /// of them reach the same single clearing rather than a list this build has to maintain.
        /// </summary>
        [Test]
        public void TheRewriteLapsesAtEveryStateChangeOfItsOwnCard()
        {
            AssertLapses("resolved", CardLifecycleAuthority.TryResolveAfterAction);
            AssertLapses("retrieved during its own action",
                CardLifecycleAuthority.TryRetrieveResolvingCard);
            AssertLapses("resolved, then discarded", card =>
                CardLifecycleAuthority.TryResolveAfterAction(card) &&
                CardLifecycleAuthority.TryDiscardResolvedCard(card));
            AssertLapses("resolved, then retrieved at the end of the round", card =>
                CardLifecycleAuthority.TryResolveAfterAction(card) &&
                CardLifecycleAuthority.TryRetrieveEligibleCard(card));
        }

        /// <summary>
        /// And the complement: a card that is NOT its own card keeps its rewrite while every other
        /// card in the match changes state around it, which is what makes the lapse a fact about the
        /// card rather than about the round.
        /// </summary>
        [Test]
        public void ARewriteSurvivesEveryOtherCardsStateChange()
        {
            var world = World.Staged();
            var enemyCard = world.CardOf(Enemy);
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    enemyCard, 11, World.SourceCardId),
                Is.True);

            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(world.CardOf(Hanu)), Is.True);
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(world.CardOf(Ally)), Is.True);
            Assert.That(
                CardLifecycleAuthority.TryDiscardResolvedCard(world.CardOf(Ally)), Is.True);

            Assert.That(enemyCard.InitiativeOverride.IsDefined, Is.True,
                "It follows the CARD (HA-Q1), so nothing another card does reaches it.");
            Assert.That(
                PrintedValueQuery.CalculatePrintedInitiative(enemyCard, world.Definitions).Total,
                Is.EqualTo(11));
        }

        /// <summary>
        /// A transition that changes nothing is not a state change, so it does not end the rewrite -
        /// p.10's own word is "changes".
        /// </summary>
        [Test]
        public void ATransitionThatChangesNothing_DoesNotEndTheRewrite()
        {
            var world = World.Staged();
            var card = world.CardOf(Enemy);
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(card, 11, World.SourceCardId),
                Is.True);

            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.False,
                "It is already face up, so there is no reveal to perform.");
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.False,
                "It is not in hand.");
            Assert.That(card.InitiativeOverride.IsDefined, Is.True,
                "and a refused transition has changed no state, so nothing has lapsed.");
        }

        // -------------------------------------------------------------------------------------------
        // Through a snapshot
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The rewrite survives capture and restore, and the restored match re-serializes
        /// byte-identically - which is the replay half stated the way every other persisted fact in
        /// this build states it.
        /// </summary>
        [Test]
        public void TheRewriteSurvivesCaptureRestoreAndReplay()
        {
            var fixture = ConfiguredRound();
            var rewritten = PlayedCards(fixture)[0];
            var untouched = PlayedCards(fixture)[1];
            var printed = PrintedValueQuery
                .CalculatePrintedInitiative(untouched, fixture.Configuration.CardDefinitions).Total;

            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    rewritten, 11, untouched.Id),
                Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var restored = Restore(fixture, payload);

            var restoredCard = restored.Cards.Single(
                card => card.Id.Equals(rewritten.Id));
            Assert.That(restoredCard.InitiativeOverride.IsDefined, Is.True);
            Assert.That(restoredCard.InitiativeOverride.Value, Is.EqualTo(11));
            Assert.That(
                restoredCard.InitiativeOverride.SourceCardInstanceId, Is.EqualTo(untouched.Id),
                "The sentence in effect is named in the payload, because nothing else remembers it.");
            Assert.That(
                restored.Cards.Single(card => card.Id.Equals(untouched.Id))
                    .InitiativeOverride.IsDefined,
                Is.False,
                "and a card no sentence touched comes back untouched - the section is SPARSE.");
            Assert.That(
                PrintedValueQuery
                    .CalculatePrintedInitiative(
                        restored.Cards.Single(card => card.Id.Equals(untouched.Id)),
                        fixture.Configuration.CardDefinitions)
                    .Total,
                Is.EqualTo(printed));

            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "A replay of the restored match writes the same bytes, which is what says the " +
                "rewrite is state rather than a reading.");
        }

        /// <summary>
        /// A payload carrying NO rewrite costs exactly four bytes more than one written before this
        /// milestone - the sparse section's own count - which is the measurement the schema bump's
        /// decision record is argued from.
        /// </summary>
        [Test]
        public void ThePayloadWithoutARewrite_CostsOneCountAndNothingPerCard()
        {
            var fixture = ConfiguredRound();
            var plain = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);

            var rewritten = PlayedCards(fixture)[0];
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    rewritten, 11, rewritten.Id),
                Is.True);
            var withOne = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);

            var plainBytes = Convert.FromBase64String(plain).Length;
            var withOneBytes = Convert.FromBase64String(withOne).Length;
            Assert.That(withOneBytes - plainBytes,
                Is.EqualTo(
                    1 + rewritten.Id.Value.Length + 4 + 1 + rewritten.Id.Value.Length),
                "One entry costs its two bounded strings and one Int32 and nothing else. The " +
                "difference between the two payloads is the whole of what a rewrite adds, and the " +
                "four bytes every payload now carries are the count that is zero in the first.");
            Assert.That(
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(51),
                "v44 is this section. See SnapshotCompatibilityCorpusTests for the +4 measured on " +
                "all sixteen captures and the reject-rather-than-migrate decision.");
        }

        /// <summary>
        /// A payload that rewrites a card it does not leave in p.10's unresolved shape is refused
        /// rather than half-read, which is the fail-closed half every other bounded reader in this
        /// serializer already has.
        /// </summary>
        [Test]
        public void APayloadRewritingACardItDoesNotContain_IsRefused()
        {
            var fixture = ConfiguredRound();
            var rewritten = PlayedCards(fixture)[0];
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    rewritten, 11, PlayedCards(fixture)[1].Id),
                Is.True);
            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);

            // The sparse entry, byte for byte: the card's bounded id, the Int32 eleven, and the
            // bounded id of the card whose text wrote it. Nothing else in the payload can be that
            // sequence, so the position is FOUND rather than assumed - and the byte changed is
            // inside the entry's own card id, which is what makes the corruption the one this
            // reader's refusal is about.
            var corrupted = Convert.FromBase64String(payload);
            var entry = Entry(rewritten.Id.Value, 11, PlayedCards(fixture)[1].Id.Value);
            var at = LastIndexOf(corrupted, entry);
            Assert.That(at, Is.GreaterThanOrEqualTo(0),
                "The fixture must actually find the rewrite entry in the payload.");
            corrupted[at + 1] = (byte)'z';

            Assert.That(
                () => Restore(fixture, Convert.ToBase64String(corrupted)),
                Throws.TypeOf<SnapshotLoadException>(),
                "A rewrite naming a card the payload does not contain describes a match this " +
                "build cannot accept.");
        }

        // -------------------------------------------------------------------------------------------
        // The seat's rewind
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.42e's interaction wall reaches a rewritten Initiative on an OPPONENT's card,
        /// and before this block it did not: the fingerprint compared where an opponent's cards
        /// were, and a rewrite moves no card. This is the state it had no term for.
        /// </summary>
        [Test]
        public void ARewriteOnAnOpponentsCard_IsAnInteractionTheFingerprintCanSee()
        {
            var world = World.Staged();
            var card = world.CardOf(Enemy);

            var before = CardTerm(card);
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(card, 11, World.SourceCardId),
                Is.True);

            Assert.That(card.State.Location, Is.EqualTo(CardLocation.TurnArea),
                "The card has not moved, which is exactly why the wall could not see this before: " +
                "every other term of the fingerprint - heroes, minions, card locations, markers, " +
                "tokens, life counters - compares equal across a Hurry up!.");
            Assert.That(CardTerm(card), Is.Not.EqualTo(before),
                "and the term the host now composes does differ, which is what raises the floor.");

            var friendly = World.Staged();
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                    friendly.CardOf(Ally), 11, World.SourceCardId),
                Is.True,
                "A rewrite on a FRIENDLY hero's card is the same transition; the wall is about " +
                "whose card it is, which is the host's own opponents-only filter and not this " +
                "term's business.");
        }

        // -------------------------------------------------------------------------------------------
        // The gate, the labels and the claims
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate, asserted from BOTH sides - and from milestone 8.71h it is asserted
        /// the other way round: the one contract this block declares is in the REGISTERED list, the
        /// deferred list is empty, and the catalog a configured match loads can start it.
        ///
        /// <para>The gate itself has not changed. It withholds a contract until dealt content can
        /// reach it, and 8.71h is the milestone where that became true: it deals him, registers his
        /// six contracts and gives each new handler TYPE a showcase consumer in ONE change, which is
        /// Silverarrow's 8.35z46 law.</para>
        /// </summary>
        [Test]
        public void TheOneNewContract_IsRegisteredSinceTheDealing()
        {
            Assert.That(
                HanuComponentContent.HurryUpRegisteredHandlers()
                    .Select(handler => handler.Reference).ToArray(),
                Is.EqualTo(new[] { HanuComponentContent.HurryUpInitiativeV1 }));
            Assert.That(HanuComponentContent.HurryUpDeferredHandlers(), Is.Empty);

            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities
                    .IsRegistered(HanuComponentContent.HurryUpInitiativeV1),
                Is.True,
                "Hurry up! is a face a configured match deals since 8.71h, so the rewrite its text " +
                "performs is one the build can start - and `HanuDealingTests` drives it end to end.");
        }

        /// <summary>
        /// The one face this block composes is one of his nineteen, under the set's one real revision
        /// since milestone 8.71g replaced this block's placeholder "hanu-8-71a-prep".
        /// </summary>
        [Test]
        public void TheComposedSet_IsOneFaceUnderTheSetsRevision()
        {
            // MERGE NOTE (8.71 wave): the integrated aggregate holds every landed block's faces, so
            // this block's one face is asserted on its own list and by containment in the aggregate.
            Assert.That(HanuCardContent.HurryUpDefinitions().Count(), Is.EqualTo(1));
            Assert.That(HanuCardContent.AllDefinitions.Select(face => face.Id.Value),
                Does.Contain("hanu-hurry-up"));
            Assert.That(HanuCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "8.71g stated his set; 8.71h is still the block that registers it.");
            Assert.That(
                HanuCardContent.HurryUpDefinitions().Select(face => face.Reference.ContentRevision)
                    .Distinct().ToArray(),
                Is.EqualTo(new[] { HanuCardContent.ContentRevision }),
                "one string across his whole set, which `HanuPrintedSetTests` asserts from both ends.");
        }

        /// <summary>
        /// The decision labels this block owes are DERIVED from the composed face rather than
        /// declared by guess, which is the measurement 8.69f's ten-of-nineteen miss cost.
        /// </summary>
        [Test]
        public void TheDecisionLabels_AreDerivedAndThisFaceRaisesNone()
        {
            // MERGE NOTE (8.71 wave): derived over THIS block's face, not the integrated aggregate.
            var required = DecisionLabelRequirementQuery
                .For(HanuCardContent.HurryUpDefinitions().ToArray())
                .Select(requirement => requirement.NodeId.Value)
                .ToArray();

            Assert.That(required, Is.Empty,
                "Both steps are mandatory and the one prompt is a selection whose candidates name " +
                "themselves, so the face raises no confirm-or-decline and no modal arm.");
            Assert.That(HanuEffectContent.HurryUpDecisionLabels(), Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static void AssertLapses(string what, Func<CardInstance, bool> exit)
        {
            var world = World.Staged();
            var card = world.CardOf(Enemy);
            Assert.That(
                CardLifecycleAuthority.TrySetPrintedInitiativeOverride(card, 11, World.SourceCardId),
                Is.True,
                what);
            Assert.That(
                PrintedValueQuery.CalculatePrintedInitiative(card, world.Definitions).Total,
                Is.EqualTo(11),
                what);

            Assert.That(exit(card), Is.True, what + ": the exit must actually happen.");
            Assert.That(card.InitiativeOverride.IsDefined, Is.False, what);
            Assert.That(
                PrintedValueQuery.CalculatePrintedInitiative(card, world.Definitions).Total,
                Is.EqualTo(12),
                what + ": and the card reads the number it prints again.");
        }

        private static EffectCapabilityParameter Parameter(
            IEnumerable<IEffectCapabilityHandler> handlers,
            EffectCapabilityReference reference,
            string parameterName) =>
            handlers
                .Single(handler => handler.Reference.Equals(reference))
                .Schema.Parameters
                .Single(parameter => parameter.Name == parameterName);

        /// <summary>
        /// Whether that arm declares that parameter at all. It is asked as a PRESENCE rather than
        /// as a null, because a capability parameter is a readonly struct - so the absent value is
        /// an all-default parameter with a null name, which no null check would catch.
        /// </summary>
        private static bool Declares(
            IEnumerable<IEffectCapabilityHandler> handlers,
            EffectCapabilityReference reference,
            string parameterName) =>
            handlers
                .Single(handler => handler.Reference.Equals(reference))
                .Schema.Parameters
                .Any(parameter => parameter.Name == parameterName);

        /// <summary>The term milestone 8.42e's fingerprint composes for one opposing card.</summary>
        private static string CardTerm(CardInstance card) =>
            "c:" + card.Id.Value + "@" + card.State.Location +
            (card.InitiativeOverride.IsDefined ? "!" + card.InitiativeOverride.Value : string.Empty);

        /// <summary>
        /// One rewrite entry as <c>WriteCards</c> writes it: two bounded strings with a
        /// <see cref="System.IO.BinaryWriter"/> length prefix around one little-endian Int32. Every
        /// id in this build is short enough for a one-byte prefix, which the assertion states.
        /// </summary>
        private static byte[] Entry(string cardId, int value, string sourceCardId)
        {
            var card = System.Text.Encoding.UTF8.GetBytes(cardId);
            var source = System.Text.Encoding.UTF8.GetBytes(sourceCardId);
            Assert.That(card.Length, Is.LessThan(128));
            Assert.That(source.Length, Is.LessThan(128));
            var bytes = new List<byte> { (byte)card.Length };
            bytes.AddRange(card);
            bytes.AddRange(BitConverter.GetBytes(value));
            bytes.Add((byte)source.Length);
            bytes.AddRange(source);
            return bytes.ToArray();
        }

        private static int LastIndexOf(byte[] haystack, byte[] needle)
        {
            for (var start = haystack.Length - needle.Length; start >= 0; start--)
            {
                var found = true;
                for (var index = 0; index < needle.Length && found; index++)
                {
                    found = haystack[start + index] == needle[index];
                }

                if (found)
                {
                    return start;
                }
            }

            return -1;
        }

        private static CardDefinition HurryUpFace() =>
            HanuCardContent.AllDefinitions.Single(
                definition => definition.Id.Equals(HanuCardContent.HurryUp));

        private static EffectDefinition HurryUpBody() =>
            HurryUpFace().ActionDefinitions
                .Single(action => action.Kind == ActionKind.Skill)
                .EffectRoot;

        private static EffectNodeDefinition Node(EffectDefinition body, string nodeId) =>
            body.Nodes.Single(node => node.Id.Value == nodeId);

        private static EffectValue Literal(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal;

        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates.Select(candidate => candidate.Value.HeroId.Value).ToArray();

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HeroId hero) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero));

        private static EffectValue Published(EffectExecutionResult result, string outputName) =>
            result.Outcomes
                .Single(fact => fact.OutputName == outputName)
                .Value;

        private static MatchParticipants Participants()
        {
            var hanuPlayer = new PlayerId("hanu-player");
            var allyPlayer = new PlayerId("hanu-ally-player");
            var enemyPlayer = new PlayerId("hanu-enemy-player");
            var farEnemyPlayer = new PlayerId("hanu-far-enemy-player");

            return new MatchParticipants(
                new[]
                {
                    new Team(BlueTeam, TeamSide.Blue),
                    new Team(RedTeam, TeamSide.Red)
                },
                new[]
                {
                    new Player(hanuPlayer, BlueTeam),
                    new Player(allyPlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(farEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Hanu, hanuPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(FarEnemy, farEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>One face with a chosen printed Initiative, for a card the clause is about.</summary>
        private static CardDefinition Target(string id, int initiative) => new CardDefinition(
            new CardDefinitionId(id),
            TestRevision,
            id,
            new InitiativeValue(initiative),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-movement"),
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            },
            null,
            CardSourceContent.NotSourced);

        /// <summary>Which of p.10's shapes a staged hero's card is left in.</summary>
        private enum CardShape
        {
            /// <summary>Played, face up, unresolved - the one the clause is about.</summary>
            Unresolved = 0,
            Resolved = 1,
            FaceDown = 2,
            InHand = 3
        }

        private static AuthoritativeSnapshotSerializerFixture ConfiguredRound()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var red = configuration.RedHand.Instances[0].OwnerId;
            var blue = configuration.BlueHand.Instances[0].OwnerId;
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(0, 1)
                },
                new[]
                {
                    new HeroOccupancy(red, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue, new HexCoordinate(1, 0))
                },
                Array.Empty<ObstacleOccupancy>());
            var cardActions = new CardActionResolutionAuthority(
                MatchContentCatalogs.ForThisBuild(), board);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForCardActionsOnly(cardActions));

            var redCard = configuration.RedHand.Instances[3];
            var blueCard = configuration.BlueHand.Instances[3];
            Assert.That(round.TryCommitCard(redCard.OwnerId, redCard.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(blueCard.OwnerId, blueCard.Id).Accepted, Is.True);

            return new AuthoritativeSnapshotSerializerFixture(configuration, board, round);
        }

        /// <summary>The two committed cards, face up and unresolved, in a stable order.</summary>
        private static CardInstance[] PlayedCards(AuthoritativeSnapshotSerializerFixture fixture) =>
            fixture.Configuration.CardInstances
                .Where(CardLifecycleAuthority.IsResolvingCardInTurnArea)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();

        private static RestoredAuthoritativeMatch Restore(
            AuthoritativeSnapshotSerializerFixture fixture, string payload) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());

        private sealed class AuthoritativeSnapshotSerializerFixture
        {
            public AuthoritativeSnapshotSerializerFixture(
                MatchConfiguration configuration,
                AdjacentMoveAuthority board,
                RoundResolutionAuthority round)
            {
                Configuration = configuration;
                Board = board;
                Round = round;
            }

            public MatchConfiguration Configuration { get; }

            public AdjacentMoveAuthority Board { get; }

            public RoundResolutionAuthority Round { get; }
        }

        /// <summary>
        /// One round's worth of played cards and the Initiative selection asked over them - p.7's own
        /// question, asked the way the round asks it.
        /// </summary>
        private sealed class Order
        {
            private readonly MatchParticipants participants;
            private readonly List<CardDefinition> definitions = new List<CardDefinition>();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly Dictionary<HeroId, int> printed = new Dictionary<HeroId, int>();
            private readonly Dictionary<HeroId, int> rewrites = new Dictionary<HeroId, int>();
            private readonly HeroItemState items;
            private readonly RuleModificationSet modifications;
            private readonly TieBreakerState tieBreaker;
            private InitiativeResolutionAuthority authority;

            internal Order(
                TeamSide revealed = TeamSide.Blue,
                HeroId initiativeItemsFor = default,
                HeroId heroModifierFor = default)
            {
                participants = Participants();
                tieBreaker = new TieBreakerState(revealed);
                items = initiativeItemsFor.IsDefined
                    ? HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                    {
                        new HeroItem(
                            initiativeItemsFor,
                            new CardInstanceId("hanu-initiative-item"),
                            HeroDashboard.RulebookLayout.Slots[2].Position,
                            PrintedValueChannel.Initiative,
                            1)
                    }))
                    : new HeroItemState(participants);
                modifications = heroModifierFor.IsDefined
                    ? new RuleModificationSet(
                        new[]
                        {
                            AppliedRuleModification.FromActiveEffect(
                                RuleModification.ForValue(PrintedValueChannel.Initiative, 2),
                                RuleObject.ForHero(heroModifierFor),
                                new ActiveEffectId("hanu-initiative-modifier"),
                                heroModifierFor,
                                TeamSide.Red,
                                1)
                        },
                        Array.Empty<AppliedObstacleOverlay>(),
                        tieBreaker)
                    : RuleModificationSet.Empty;
                authority = new InitiativeResolutionAuthority(tieBreaker);
            }

            internal void Play(HeroId hero, TeamSide side, int initiative)
            {
                var definition = Target("hanu-order-" + hero.Value, initiative);
                definitions.Add(definition);
                var card = CardInstance.CreateInHand(
                    new CardInstanceId("card-" + hero.Value), definition.Reference, hero);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                instances.Add(card);
                printed[hero] = initiative;
            }

            /// <summary>One Hurry up!, performed on the named hero's card.</summary>
            internal void Rewrite(HeroId hero, int value)
            {
                var card = instances.Single(instance => instance.OwnerId.Equals(hero));
                Assert.That(
                    CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                        card, value, new CardInstanceId("hanu-order-source")),
                    Is.True);
                rewrites[hero] = value;
            }

            /// <summary>p.7's check, asked fresh - which is what a mid-round rewrite is read by.</summary>
            internal HeroId NextActor()
            {
                var result = authority.TrySelectNextActor(
                    definitions, instances, participants, items, modifications);
                Assert.That(result.Accepted, Is.True);
                Assert.That(authority.PendingChoice, Is.Null,
                    "These boards are built so that one card is highest on its own side.");
                return authority.SelectedActor.ActorId;
            }

            /// <summary>The acting hero resolves their card, which is what re-opens the question.</summary>
            internal void Finish(HeroId hero)
            {
                var card = instances.Single(instance => instance.OwnerId.Equals(hero));
                Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
                Assert.That(
                    authority.TryAcknowledgeSelectedCardResolved(card.Id, instances).Accepted,
                    Is.True);
            }

            /// <summary>Every unresolved card's total, as p.7 orders by.</summary>
            internal Dictionary<HeroId, int> Totals() =>
                instances
                    .Where(CardLifecycleAuthority.IsResolvingCardInTurnArea)
                    .ToDictionary(
                        card => card.OwnerId,
                        card =>
                        {
                            var value = PrintedValueQuery.CalculatePrintedInitiative(
                                card, definitions);
                            value = ItemBonusQuery.Apply(
                                value, items, card.OwnerId, modifications);
                            var declared = modifications.ValueContributions(
                                RuleObject.ForHero(card.OwnerId),
                                PrintedValueChannel.Initiative);
                            return declared.Count == 0
                                ? value.Total
                                : new ValueResult(
                                        PrintedValueChannel.Initiative,
                                        value.Contributions.Concat(declared))
                                    .Total;
                        });

            /// <summary>
            /// The same cards, played again with the same printed numbers and no rewrites - so two
            /// readings of one board differ in exactly the thing the case is about.
            /// </summary>
            internal void Reset()
            {
                var replay = printed.ToArray();
                var sides = instances.ToDictionary(
                    card => card.OwnerId,
                    card => participants.GetTeamForHero(card.OwnerId).Side);
                definitions.Clear();
                instances.Clear();
                printed.Clear();
                rewrites.Clear();
                authority = new InitiativeResolutionAuthority(tieBreaker);
                foreach (var entry in replay)
                {
                    Play(entry.Key, sides[entry.Key], entry.Value);
                }
            }
        }

        /// <summary>
        /// One staged board and the frame walking Hurry up!'s body over it.
        ///
        /// <para>Nothing here is a configured match: see the fixture's own remarks for why, and for
        /// who owns the part this cannot reach.</para>
        /// </summary>
        private sealed class World
        {
            internal static readonly CardInstanceId SourceCardId =
                new CardInstanceId("hanu-hurry-up-source");

            internal static readonly CardInstanceId SecondSourceCardId =
                new CardInstanceId("hanu-hurry-up-second-source");

            private readonly MatchParticipants participants;
            private readonly MatchComponents components;
            private readonly RuleModificationSet modifications;
            private readonly HeroItemState items;
            private readonly List<CardInstance> instances;
            private readonly List<CardDefinition> definitions;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private World(
                MatchParticipants participants,
                MatchComponents components,
                RuleModificationSet modifications,
                HeroItemState items,
                List<CardDefinition> definitions,
                List<CardInstance> instances)
            {
                this.participants = participants;
                this.components = components;
                this.modifications = modifications;
                this.items = items;
                this.definitions = definitions;
                this.instances = instances;
                context = BuildContext();
            }

            internal IReadOnlyList<CardInstance> Instances => instances;

            internal IReadOnlyList<CardDefinition> Definitions => definitions;

            /// <summary>
            /// Hanu at the origin with his own Hurry up! played, a friendly hero two spaces out, an
            /// enemy three spaces out and a second enemy five spaces out - and one card each, in
            /// whichever of p.10's shapes the case is about.
            /// </summary>
            internal static World Staged(
                int rangeItems = 0,
                CardShape allyState = CardShape.Unresolved,
                CardShape enemyState = CardShape.Unresolved,
                HeroId immuneHero = default)
            {
                var participants = Participants();
                var board = Board();
                var definitions = new List<CardDefinition> { HurryUpFace() };
                var instances = new List<CardInstance>();

                Stage(definitions, instances, Hanu, 12, CardShape.Unresolved, HurryUpFace());
                Stage(definitions, instances, Ally, 12, allyState);
                Stage(definitions, instances, Enemy, 12, enemyState);
                Stage(definitions, instances, FarEnemy, 12, CardShape.Unresolved);

                return new World(
                    participants,
                    new MatchComponents(
                        SyntheticComponentDefinitions.CreateCatalog(),
                        board,
                        new MarkerAttachmentRegister(),
                        board,
                        board,
                        board,
                        board),
                    immuneHero.IsDefined
                        ? ImmunityFor(participants, immuneHero)
                        : RuleModificationSet.Empty,
                    RangeItems(participants, rangeItems),
                    definitions,
                    instances);
            }

            private static void Stage(
                List<CardDefinition> definitions,
                List<CardInstance> instances,
                HeroId hero,
                int initiative,
                CardShape shape,
                CardDefinition face = null)
            {
                var definition = face ?? Target("hanu-staged-" + hero.Value, initiative);
                if (face == null)
                {
                    definitions.Add(definition);
                }

                var card = CardInstance.CreateInHand(
                    new CardInstanceId(
                        face == null ? "card-" + hero.Value : SourceCardId.Value),
                    definition.Reference,
                    hero);
                switch (shape)
                {
                    case CardShape.InHand:
                        break;
                    case CardShape.FaceDown:
                        Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                        break;
                    case CardShape.Resolved:
                        Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
                        break;
                    default:
                        Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                        break;
                }

                instances.Add(card);

                // A second card, left in hand, so a case about a hero who has played nothing has a
                // card of theirs to name.
                var spare = Target("hanu-spare-" + hero.Value, initiative);
                definitions.Add(spare);
                instances.Add(CardInstance.CreateInHand(
                    new CardInstanceId("spare-" + hero.Value), spare.Reference, hero));
            }

            internal CardInstance CardOf(HeroId hero) =>
                instances.Single(card =>
                    card.OwnerId.Equals(hero) && !card.Id.Value.StartsWith("spare-"));

            internal CardInstance HandCardOf(HeroId hero) =>
                instances.Single(card => card.Id.Value == "spare-" + hero.Value);

            private static AdjacentMoveAuthority Board()
            {
                var zone = new ZoneId("hanu-hurry-up-zone");
                var spaces = new List<BoardMapSpace>();
                for (var q = -8; q <= 8; q++)
                {
                    for (var r = -8; r <= 8; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, HanuAt) > 8)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            new SpaceId($"q{coordinate.Q}_r{coordinate.R}"),
                            coordinate,
                            BoardMapSpaceKind.Playable,
                            zone));
                    }
                }

                return new AdjacentMoveAuthority(
                    new BoardMapDefinition(
                        Map, 1, "Hanu Hurry Up Map",
                        spaces,
                        new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                        Array.Empty<BoardMapSpawnPoint>(),
                        Array.Empty<ZoneId>()),
                    new[]
                    {
                        new HeroOccupancy(Hanu, HanuAt),
                        new HeroOccupancy(Ally, AllyAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(FarEnemy, FarEnemyAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
            }

            private static HeroItemState RangeItems(MatchParticipants participants, int count) =>
                count <= 0
                    ? new HeroItemState(participants)
                    : HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                    {
                        new HeroItem(
                            Hanu,
                            new CardInstanceId("hanu-range-item"),
                            HeroDashboard.RulebookLayout.Slots[3].Position,
                            PrintedValueChannel.Range,
                            count)
                    }));

            /// <summary>
            /// A live set carrying one whole immunity, built the way a match builds one - from an
            /// Active effect in its active phase - rather than hand-assembled.
            /// </summary>
            private static RuleModificationSet ImmunityFor(
                MatchParticipants participants, HeroId heroId)
            {
                var definition = new ActiveEffectDefinition(
                    "hanu-skill-immunity-effect",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.CardsPlayedAndRevealed,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                    true,
                    "HanuSkillImmunity",
                    "hanu-skill-immunity",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForImmunity(RuleActivity.Skill) });

                var timing = new ActiveEffectLifecycle();
                timing.Create(
                    new ActiveEffectId("hanu-immunity-" + heroId.Value),
                    definition,
                    heroId,
                    TeamSide.Red,
                    default,
                    new CardInstanceId("hanu-immunity-source"),
                    new ActionDefinitionId("hanu-immunity-action"),
                    1,
                    1);
                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                    new TieBreakerState(TeamSide.Blue));

                var set = RuleModificationQuery.Build(
                    timing,
                    null,
                    participants,
                    new TieBreakerState(TeamSide.Blue),
                    UnlockedUltimateSet.None, RuleObject.None, null, null);
                Assert.That(
                    set.IsImmuneTo(
                        RuleObject.ForHero(heroId), RuleActivity.Skill, RuleObject.ForHero(Hanu)),
                    Is.True,
                    "The fixture must actually hold the immunity, or the case above proves nothing.");
                return set;
            }

            private EffectResolutionContext BuildContext() =>
                new EffectResolutionContext(
                    HurryUpBody(),
                    SyntheticEffectCapabilities.CreateCatalog(
                        HanuComponentContent.HurryUpRegisteredHandlers()),
                    participants,
                    null,
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Skill,
                    components: components,
                    heroItems: items);

            /// <summary>
            /// The rewrite node alone, asked directly, with the hero the selection would have named
            /// bound as its input - which is how the facts it PUBLISHES can be read at all, since a
            /// completed resolution keeps no frame to read them off.
            /// </summary>
            internal EffectExecutionResult InvokeRewrite(HeroId hero)
            {
                var node = Node(HurryUpBody(), HanuEffectContent.HurryUpRewriteNode);
                var handler = HanuComponentContent.HurryUpRegisteredHandlers()
                    .Single(value => value.Reference.Equals(
                        HanuComponentContent.HurryUpInitiativeV1));
                return handler.Execute(new EffectCapabilityContext(
                    node.Id,
                    node.FailurePolicy,
                    Hanu,
                    Hanu,
                    SourceCardId,
                    HurryUpFace().Reference,
                    HanuCardContent.PrimaryBasicSkill,
                    new Dictionary<string, EffectValue>
                    {
                        {
                            HanuComponentContent.HurryUpHeroParameterName, EffectValue.Hero(hero)
                        },
                        {
                            HanuComponentContent.HurryUpPrintedInitiativeParameterName,
                            EffectValue.Integer(HanuEffectContent.HurryUpPrintedInitiative)
                        }
                    },
                    1,
                    1,
                    participants,
                    null,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Skill,
                    components,
                    items,
                    declaredOutputs: node.Outputs));
            }

            internal EffectResolutionStepResult Run()
            {
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("hanu-hurry-up-resolution"),
                    SourceCardId,
                    HurryUpFace().Reference,
                    HanuCardContent.PrimaryBasicSkill,
                    Hanu,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    context,
                    RepeatContinuation.FirstInstance);

                Assert.That(result.Accepted, Is.True, "The walk must start.");
                frame = result.Frame;
                return result;
            }

            internal PendingEffectChoice RunToPause()
            {
                var result = Run();
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult AnswerToCompletion(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                frame = result.Frame;
                return result;
            }
        }
    }
}
