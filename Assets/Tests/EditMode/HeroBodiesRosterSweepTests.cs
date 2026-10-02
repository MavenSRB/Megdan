using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21c. Every other hero reads a many-bodied hero (Razzle) by RZ-Q2: for anybody else
    /// he is ONE hero on several spaces. A selection may pick any body as the space it reads; a step
    /// that counts or names heroes counts him once; a hero-level result happens once; presence checks
    /// and obstacles see every body.
    ///
    /// <para><b>The sweep</b> (<see cref="EveryCapabilityTheDealtRosterComposes_IsClassifiedByHowItReadsAManyBodiedHero"/>)
    /// is content-derived in the style of <see cref="RosterMoveLeashSweepTests"/>: it enumerates every
    /// capability the dealt roster's faces compose and requires each to be classified by how it reads a
    /// hero standing on several spaces, so a capability a later hero adds cannot slip past the reading
    /// unasked. The staged cases below it prove one representative per distinct shape on a board where
    /// the Blue hero declares Razzle's supply of four bodies (RZ-Q1) through the internal-test seam, as
    /// <see cref="HeroBodiesTests"/> stages him; no Razzle content is dealt.</para>
    /// </summary>
    public sealed class HeroBodiesRosterSweepTests
    {
        // -------------------------------------------------------------------------------------------
        // The sweep
        // -------------------------------------------------------------------------------------------

        /// <summary>How a capability reads a hero standing on several spaces (RZ-Q2).</summary>
        public enum Reading
        {
            /// <summary>Reads no hero or unit on the board: cards, values, conditions, its own tokens.</summary>
            NoHeroOrUnit,

            /// <summary>Reads the actor's own space - the acting body (RZ-Q4, 9.21b).</summary>
            ActingBody,

            /// <summary>Reads the enclosing Attack's target at the body the Attack took (RZ-Q3).</summary>
            AttackedBody,

            /// <summary>
            /// A selection reading the picked unit's space: each body in reach is its own answer, as the
            /// space it stands on, and he is counted once (two of his bodies are never two units).
            /// Where the node reads no space he is offered once, as the hero.
            /// </summary>
            BodyAsSpace,

            /// <summary>A selection naming heroes: he is offered once, as the hero.</summary>
            HeroOnce,

            /// <summary>A presence, count or obstacle read: every body is seen, he is counted once.</summary>
            PresenceEveryBody,

            /// <summary>A result applied to the hero as a whole - discard, coins, marker, Defeat, a
            /// modification addressed to the hero - once.</summary>
            HeroLevelResult,

            /// <summary>An operation on a space (push, move, swap, place, remove): it acts on the body
            /// standing on that space.</summary>
            SpaceNamesBody,

            /// <summary>
            /// Open for 9.21d-9.21k: the hero is named once and his space is read at a body the reading
            /// does not let anybody choose - see the reason beside each entry.
            /// </summary>
            OpenBodyNotChosen
        }

        /// <summary>
        /// The reading of every handler the dealt roster composes, keyed by its declaring type and name.
        /// A face composes a capability; the handler is what reads the board, and every capability of
        /// one handler reads a many-bodied hero the same way.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, Reading> Readings = new Dictionary<string, Reading>
        {
            // Active effects: modifications addressed to the hero (RuleObject.ForHero) cover every body;
            // a radius they draw around another hero reads every body of a hero inside it and counts him
            // once (9.21a); a radius anchored on a many-bodied CONTROLLER is 9.21a's named refusal.
            ["ActiveEffectCreationHandler"] = Reading.HeroLevelResult,

            ["BrynnComponentContent+AttackTargetWasAdjacentToObstaclesHandler"] = Reading.AttackedBody,
            ["BrynnComponentContent+EnemyHeroInRadiusAdjacentToObstaclesHandler"] = Reading.PresenceEveryBody,
            // Brynn's swap subjects name the hero once and read the first of his bodies in radius
            // adjacent to three obstacles, not the one the player would pick.
            ["BrynnComponentContent+TrackingSwapSubjectHandler"] = Reading.OpenBodyNotChosen,
            ["HanuComponentContent+FriendlyUnitPairHandler"] = Reading.BodyAsSpace,
            ["HanuComponentContent+HurryUpInitiativeHandler"] = Reading.HeroLevelResult,
            // The journey partner is the hero an Active effect records; his space is re-read at his
            // acting (default) body because the effect records the hero, not the body.
            ["HanuComponentContent+JourneyPartnerHandler"] = Reading.OpenBodyNotChosen,
            // A card-text Attack target is bound as the hero, so the Attack takes body 1.
            ["IgnatiaAttackTargetingContent+IgnatiaRangeAttackTargetHandler"] = Reading.OpenBodyNotChosen,
            ["IgnatiaComponentContent+FlipTieBreakerHandler"] = Reading.NoHeroOrUnit,
            ["IgnatiaDiscardTargetingContent+EnemyHeroAdjacentToMagmaOrMinionHandler"] = Reading.HeroOnce,
            ["IgnatiaDiscardTargetingContent+EnemyMinionAdjacentToEnemyHeroHandler"] = Reading.PresenceEveryBody,
            ["IgnatiaMagmaContent+PlaceMagmaWakeHandler"] = Reading.PresenceEveryBody,
            ["MrakRockPlacementContent+PlaceAlongLineHandler"] = Reading.PresenceEveryBody,
            ["MrakRockPlacementContent+PlaceFarthestAdjacentHandler"] = Reading.PresenceEveryBody,
            ["MrakRockPlacementContent+PlaceInSpaceHandler"] = Reading.PresenceEveryBody,
            ["MrakRockPlacementContent+ReclaimHandler"] = Reading.NoHeroOrUnit,
            // As Ignatia's: a text-bound Attack target is body 1.
            ["SnorriAttackTargetContent+RuneTargetHandler"] = Reading.OpenBodyNotChosen,
            ["SnorriBlueSupportCapabilities+ChooseOwnedItemHandler"] = Reading.NoHeroOrUnit,
            ["SnorriBlueSupportCapabilities+ChooseResolvedCardHandler"] = Reading.NoHeroOrUnit,
            ["SnorriBlueSupportCapabilities+RetrieveAllDiscardedHandler"] = Reading.NoHeroOrUnit,
            ["SnorriBlueSupportCapabilities+SwapItemWithCardHandler"] = Reading.NoHeroOrUnit,
            ["SnorriBlueSupportCapabilities+SwapResolvedWithHandHandler"] = Reading.NoHeroOrUnit,
            ["SnorriComponentContent+InscribeRunesHandler"] = Reading.NoHeroOrUnit,
            ["SnorriGreenBlockContent+ConditionHandler"] = Reading.NoHeroOrUnit,
            ["SnorriGreenEffectContent+SelectRuneHandler"] = Reading.NoHeroOrUnit,
            ["SnorriUltimateContent+ChooseRuneHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticAnchoredTargetCapabilities+AnchoredTargetHandler"] = Reading.HeroOnce,
            ["SyntheticAttackTargetSurvivalCapabilities+AttackTargetSurvivedHandler"] = Reading.PresenceEveryBody,
            ["SyntheticBorrowedActionCapabilities+BorrowPrimaryActionHandler"] = Reading.HeroLevelResult,
            ["SyntheticBorrowedActionCapabilities+FriendlyHeroInPlayHandler"] = Reading.HeroOnce,
            ["SyntheticBorrowedActionCapabilities+NameColorHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticBorrowedCardTextCapabilities+BorrowAfterAttackTextHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticCardStateCapabilities+DiscardChosenCardHandler"] = Reading.HeroLevelResult,
            ["SyntheticComponentCapabilities+MarkerGivingHandler"] = Reading.HeroLevelResult,
            ["SyntheticComponentCapabilities+MarkerTransferHandler"] = Reading.HeroLevelResult,
            ["SyntheticComponentCapabilities+ReturnMarkerHandler"] = Reading.HeroLevelResult,
            ["SyntheticConditionCapabilities+SourceCardInDiscardHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticConsequenceCapabilities+AttackTookAHeroHandler"] = Reading.HeroLevelResult,
            ["SyntheticConsequenceCapabilities+DefeatHeroHandler"] = Reading.HeroLevelResult,
            ["SyntheticConsequenceCapabilities+DefeatMinionHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticContinuationCapabilities+AroundPrimaryActionHandler"] = Reading.NoHeroOrUnit,
            // Binds the Attack's target as the hero; a many-bodied hero bound so is body 1.
            ["SyntheticContinuationCapabilities+SetAttackTargetHandler"] = Reading.OpenBodyNotChosen,
            ["SyntheticCoupledMoveCapabilities+CoupledMoveHandler"] = Reading.SpaceNamesBody,
            // Milestone 9.32h. Takahide's three: each reads cards (a hero's deck, unresolved card or
            // discard pile named by an earlier selection), no hero or unit on the board.
            ["SyntheticDeckSwapCapabilities+SwapWithDeckCardHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticDiscardedCardChoiceCapabilities+ChooseDiscardedCardHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticUnresolvedSwapCapabilities+SwapUnresolvedCardHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticDisplacementCapabilities+BatchDirectMoveHandler"] = Reading.SpaceNamesBody,
            ["SyntheticDisplacementCapabilities+ChooseDirectionHandler"] = Reading.ActingBody,
            ["SyntheticDisplacementCapabilities+DirectMoveHandler"] = Reading.SpaceNamesBody,
            ["SyntheticDisplacementCapabilities+ObjectPushHandler"] = Reading.SpaceNamesBody,
            ["SyntheticDisplacementCapabilities+PushTargetHandler"] = Reading.SpaceNamesBody,
            ["SyntheticEffectCapabilities+NoteActorHandler"] = Reading.HeroLevelResult,
            ["SyntheticExchangeCapabilities+RemoveHandler"] = Reading.SpaceNamesBody,
            ["SyntheticExchangeCapabilities+SwapHandler"] = Reading.SpaceNamesBody,
            ["SyntheticExchangeCapabilities+SwapWithActorHandler"] = Reading.SpaceNamesBody,
            // Milestone 9.21j. Razzle's capability nodes, each read from the acting body (RZ-Q4): the
            // spawn's radius, the removal's "other", "another one of you" and the retreat's line
            // through the acting body; their staged two-body cases are 9.21d-9.21f's own fixtures.
            // His two gates - "more than one of you" and "other you in radius" - are Conditional
            // predicates rather than capability nodes, so this walk does not reach them (9.21e and
            // 9.21g prove them from the acting body). His two repeats by a different body are
            // RepeatAction nodes: the repeated instance is performed with the other body settled as
            // the acting body (9.21h), the ActingBody reading too.
            ["SyntheticHeroBodyCapabilities+RemoveActorBodiesFromTheBoard"] = Reading.ActingBody,
            ["SyntheticHeroBodyCapabilities+SpawnActorBodyInRadiusHandler"] = Reading.ActingBody,
            ["SyntheticHeroBodyMoveCapabilities+ChooseOtherActorBody"] = Reading.ActingBody,
            ["SyntheticHeroBodyMoveCapabilities+ContinuedLineEnd"] = Reading.ActingBody,
            ["SyntheticHiddenChoiceCapabilities+HiddenCardChoiceHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticHiddenChoiceCapabilities+RevealChosenCardHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticNamedBorrowCapabilities+Handler"] = Reading.NoHeroOrUnit,
            ["SyntheticObstacleCapabilities+ObstacleSpaceHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticObstacleCapabilities+PlaceAdjacentObstacleHandler"] = Reading.PresenceEveryBody,
            ["SyntheticObstacleCapabilities+RemoveObstacleHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticOwnedTokenCapabilities+OwnedTokenFactHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticPlacementCapabilities+ActorSpaceHandler"] = Reading.ActingBody,
            ["SyntheticPlacementCapabilities+PlaceHandler"] = Reading.SpaceNamesBody,
            ["SyntheticPlacementCapabilities+RespawnFriendlyMinionHandler"] = Reading.PresenceEveryBody,
            ["SyntheticResolvedDiscardCapabilities+DiscardResolvedCardHandler"] = Reading.HeroLevelResult,
            // Milestone 9.30i. Gydion's spellbook reads cards, never the board: a spell named in a
            // book, its cast and removal, the refill and the count of removed spells. A cast spell's
            // primary is then performed by the caster through the borrow path, whose own nodes are
            // classified above. Wish's team count and claim, and Energy Drain's regained Life, are
            // results applied to the caster's team as a whole.
            ["SyntheticSpellbookCapabilities+CastSpellHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticSpellbookCapabilities+CountOtherRemovedSpellsHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticSpellbookCapabilities+RefillSpellbookHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticSpellbookCapabilities+RemoveSpellFaceupHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticSpellbookCapabilities+SpellInBook"] = Reading.NoHeroOrUnit,
            ["SyntheticTeamLifeCapabilities+RegainSpentLifeHandler"] = Reading.HeroLevelResult,
            ["SyntheticTeamVictoryCapabilities+AdvanceHandler"] = Reading.HeroLevelResult,
            ["SyntheticTeamVictoryCapabilities+ClaimHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+ActorMovedThisActionHandler"] = Reading.ActingBody,
            ["SyntheticRosterCapabilities+AdjacentTerrainPresenceHandler"] = Reading.ActingBody,
            ["SyntheticRosterCapabilities+BehindTargetLineHandler"] = Reading.HeroOnce,
            ["SyntheticRosterCapabilities+CardTextFastTravelHandler"] = Reading.PresenceEveryBody,
            ["SyntheticRosterCapabilities+ChargeLineHandler"] = Reading.ActingBody,
            ["SyntheticRosterCapabilities+ChooseSwapSubjectHandler"] = Reading.BodyAsSpace,
            ["SyntheticRosterCapabilities+ChooseTargetHandler"] = Reading.BodyAsSpace,
            ["SyntheticRosterCapabilities+ChooseTargetPairHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticRosterCapabilities+ChooseTargetSetHandler"] = Reading.BodyAsSpace,
            ["SyntheticRosterCapabilities+ChooseTargetTrioHandler"] = Reading.BodyAsSpace,
            ["SyntheticRosterCapabilities+ClaimSoloVictoryHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+DefendedCardForcedMoveHandler"] = Reading.AttackedBody,
            ["SyntheticRosterCapabilities+DiscardOwnedCardHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+EmptyMinionSpawnPointCountHandler"] = Reading.PresenceEveryBody,
            ["SyntheticRosterCapabilities+EnclosingTargetDistanceHandler"] = Reading.AttackedBody,
            ["SyntheticRosterCapabilities+FormerTargetSpaceHandler"] = Reading.AttackedBody,
            ["SyntheticRosterCapabilities+GainItemFromTheBoxHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+GainResourceHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+HeroDefeatedThisRoundHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+LineDestinationHandler"] = Reading.ActingBody,
            ["SyntheticRosterCapabilities+OwnedCardPresentHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticRosterCapabilities+PreConsequenceAdjacencyHandler"] = Reading.AttackedBody,
            ["SyntheticRosterCapabilities+PresenceCountHandler"] = Reading.PresenceEveryBody,
            ["SyntheticRosterCapabilities+RetrieveCardHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+SpacesAdjacentHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticRosterCapabilities+SubjectAdjacentTerrainPresenceHandler"] = Reading.SpaceNamesBody,
            ["SyntheticRosterCapabilities+TakeDeckCardAsItemHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+TargetIsAdjacentHandler"] = Reading.AttackedBody,
            ["SyntheticRosterCapabilities+ThroughTargetLineHandler"] = Reading.BodyAsSpace,
            ["SyntheticRosterCapabilities+TransferResourceHandler"] = Reading.HeroLevelResult,
            ["SyntheticRosterCapabilities+TraversedUnitHandler"] = Reading.BodyAsSpace,
            ["SyntheticRosterCapabilities+UltimateUnlockedHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticRosterCapabilities+UnitPairHandler"] = Reading.BodyAsSpace,
            // p.13's own Attack targets per body (9.21a), a different target at hero grain.
            ["SyntheticSequenceCapabilities+AttackSequenceHandler"] = Reading.AttackedBody,
            ["SyntheticTargetingCapabilities+ChooseTargetHandler"] = Reading.BodyAsSpace,
            ["SyntheticTargetingCapabilities+HeroAdjacentToMinionHandler"] = Reading.HeroOnce,
            ["SyntheticTargetingCapabilities+PrintedIconTargetHandler"] = Reading.BodyAsSpace,
            ["SyntheticTargetingCapabilities+RangeObstacleTargetHandler"] = Reading.BodyAsSpace,
            ["SyntheticTokenCapabilities+PlaceHeroTokenHandler"] = Reading.PresenceEveryBody,
            ["SyntheticTokenCapabilities+ReclaimHeroTokenHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticTokenCapabilities+ReplaceMinionWithTokenHandler"] = Reading.NoHeroOrUnit,
            ["SyntheticValueCapabilities+CardRadiusHandler"] = Reading.NoHeroOrUnit,
            ["TrinketsComponentContent+ActorInTurretRadiusHandler"] = Reading.ActingBody,
            ["WukDominanceContent+ExcludeAdjacentEnemyMinionsHandler"] = Reading.ActingBody,
            ["WukTrampleContent+ChooseTrampledMinionsHandler"] = Reading.NoHeroOrUnit,
            ["WukTrampleContent+TrampledEnemyHeroesHandler"] = Reading.HeroOnce,
            ["WukTreeSwapContent+ChooseFriendlyUnitAndTreeHandler"] = Reading.BodyAsSpace
        };

        [Test]
        public void EveryCapabilityTheDealtRosterComposes_IsClassifiedByHowItReadsAManyBodiedHero()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var composed = PrintedWordingCorpus.All
                .SelectMany(face => face.Nodes.Where(node => node.Kind == EffectNodeKind.Capability))
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();
            Assert.That(composed.Length, Is.GreaterThan(200), "The dealt roster composes its capabilities.");

            // Milestone 9.30i. Gydion's spells, which 9.30d put in the corpus before his dealing, are
            // dealt with him, so every capability the corpus names is registered again.
            var lookup = catalogs.Capabilities;
            var handlers = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var capability in composed)
            {
                Assert.That(lookup.TryLoad(capability, out var handler), Is.True,
                    capability + " is registered.");
                var key = HandlerKey(handler);
                if (!handlers.TryGetValue(key, out var ids))
                {
                    handlers[key] = ids = new List<string>();
                }

                ids.Add(capability.ToString());
            }

            var unclassified = handlers.Keys.Where(key => !Readings.ContainsKey(key)).ToArray();
            Assert.That(unclassified, Is.Empty,
                "Every handler a dealt face composes states how it reads a hero on several spaces " +
                "(RZ-Q2). Classify these, proving a new shape with a staged two-body case: " +
                string.Join(", ", unclassified.Select(key => key + " [" + string.Join(" ", handlers[key]) + "]")));
            var stale = Readings.Keys.Where(key => !handlers.ContainsKey(key)).ToArray();
            Assert.That(stale, Is.Empty, "No dealt face composes these any more; remove their rows.");

            foreach (var group in handlers.GroupBy(entry => Readings[entry.Key]).OrderBy(group => group.Key))
            {
                TestContext.WriteLine(
                    $"{group.Key}: {group.Sum(entry => entry.Value.Count)} capabilities over " +
                    $"{group.Count()} handlers - {string.Join(", ", group.Select(entry => entry.Key))}");
            }
        }

        private static string HandlerKey(IEffectCapabilityHandler handler)
        {
            var type = handler.GetType();
            return type.DeclaringType == null ? type.Name : type.DeclaringType.Name + "+" + type.Name;
        }

        // -------------------------------------------------------------------------------------------
        // Staged two-body cases, one per distinct shape
        // -------------------------------------------------------------------------------------------

        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        /// <summary>
        /// HeroOnce, Arien's Dangerous Current under W3: "Up to 1 enemy hero in any of the N spaces in
        /// a straight line directly behind the target discards a card, or is defeated." Both of his
        /// bodies stand in the two spaces behind the target; he is named once, and naming him is one
        /// hero-level result. Before 9.21c the offer listed him twice and the choice refused to open.
        /// </summary>
        [Test]
        public void DangerousCurrent_NamesHimOnce_ThoughTwoOfHisBodiesStandInTheFile()
        {
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                new HeroOccupancy(Ally, new HexCoordinate(1, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(2, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(3, 0)));
            var handler = Handler(SyntheticRosterCapabilities.BehindTargetLineV1);
            var context = Context(board, Red,
                new Dictionary<string, EffectValue> { ["length"] = EffectValue.Integer(2) },
                attack: new EnclosingAttackFacts(
                    Red, Ally, default, AttackRangeKind.NonRanged, new HexCoordinate(1, 0)));

            var result = handler.Execute(context);
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates, Is.EqualTo(new[] { EffectValue.Hero(Blue) }),
                "One hero on two spaces of the file is one enemy hero (RZ-Q2).");
            Assert.That(result.Choice.MaximumSelections, Is.EqualTo(1));

            var named = handler.Resume(context, new[] { EffectValue.Hero(Blue) });
            Assert.That(named.Outcomes.Single(fact => fact.OutputName == SyntheticRosterCapabilities.ChosenHeroOutput)
                .Value, Is.EqualTo(EffectValue.Hero(Blue)), "The discard is asked of the hero once.");
        }

        /// <summary>
        /// HeroOnce and BodyAsSpace, the printed-icon family (Wasp's Shock: "An enemy hero in radius").
        /// A node that names the hero offers him once; a node that also reads his space offers each
        /// body in reach as its space, publishes the picked body's space, and refuses the bare hero.
        /// </summary>
        [Test]
        public void APrintedIconHeroSelection_OffersHimOnce_OrEachBodyAsItsSpace_WhereItReadsTheSpace()
        {
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(2, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(-2, 1)),
                new HeroOccupancy(Blue, 3, new HexCoordinate(6, -6)));
            var handler = Handler(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var inputs = IconInputs(SourceTeamRelation.OpposingSource);

            var naming = Context(board, Red, inputs, declared: new[]
            {
                new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
            });
            Assert.That(handler.Execute(naming).Choice.Candidates, Is.EqualTo(new[] { EffectValue.Hero(Blue) }),
                "Two bodies in radius (and one out of it) are one hero to a selection that only names him.");

            var reading = Context(board, Red, inputs, declared: new[]
            {
                new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName, EffectValueKind.Coordinate)
            });
            var offered = handler.Execute(reading).Choice.Candidates;
            Assert.That(offered, Is.EquivalentTo(new[]
            {
                EffectValue.Space(new HexCoordinate(2, 0)), EffectValue.Space(new HexCoordinate(-2, 1))
            }), "A selection that reads the space may pick either body in reach as the space it reads.");

            var picked = handler.Resume(reading, new[] { EffectValue.Space(new HexCoordinate(-2, 1)) });
            Assert.That(picked.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(picked, SyntheticTargetingCapabilities.ChosenHeroOutputName), Is.EqualTo(EffectValue.Hero(Blue)));
            Assert.That(Output(picked, SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName),
                Is.EqualTo(EffectValue.Space(new HexCoordinate(-2, 1))), "The picked body's space, not body 1's.");
            Assert.That(handler.Resume(reading, new[] { EffectValue.Hero(Blue) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The bare hero names no body where two are offered, so it is refused, never aimed at one.");
            Assert.That(handler.Resume(reading, new[] { EffectValue.Space(new HexCoordinate(6, -6)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory), "The body out of radius is not offered.");
        }

        /// <summary>
        /// HeroOnce and BodyAsSpace from an ALLY (RZ-Q5's allies' side follows RZ-Q2). "A friendly hero"
        /// in play is one hero; an ally's "friendly hero in radius" that reads the space picks a body,
        /// and the Swap that follows exchanges the ally with that body and leaves his other body where
        /// it stood.
        /// </summary>
        [Test]
        public void AnAllysFriendlyHeroSelection_OffersHimOnce_AndActsOnTheBodyItPicks()
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var board = Board(
                new HeroOccupancy(Ally, new HexCoordinate(0, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(1, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(0, 2)),
                new HeroOccupancy(Red, new HexCoordinate(-3, 0)));

            var inPlay = Handler(SyntheticBorrowedActionCapabilities.FriendlyHeroInPlayV1);
            var friendly = inPlay.Execute(Context(board, Ally, new Dictionary<string, EffectValue>(), participants: participants));
            Assert.That(friendly.Choice.Candidates.Count(value => value.Equals(EffectValue.Hero(Blue))), Is.EqualTo(1),
                "A friendly hero in play is offered once, however many spaces he stands on.");

            var handler = Handler(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var reading = Context(board, Ally, IconInputs(SourceTeamRelation.AlliedWithSource), participants: participants,
                declared: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName, EffectValueKind.Coordinate)
                });
            Assert.That(handler.Execute(reading).Choice.Candidates, Is.EquivalentTo(new[]
            {
                EffectValue.Space(new HexCoordinate(1, 0)), EffectValue.Space(new HexCoordinate(0, 2))
            }));
            var picked = handler.Resume(reading, new[] { EffectValue.Space(new HexCoordinate(0, 2)) });
            var space = Output(picked, SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName).Coordinate;

            Assert.That(board.TryGetObjectAt(space, out var body), Is.True);
            Assert.That(body.HeroBody, Is.EqualTo(2));
            Assert.That(board.CommitValidatedSwap(BoardObjectReference.ForHero(Ally), body).Accepted, Is.True);
            Assert.That(Body(board, Blue, 2), Is.EqualTo(new HexCoordinate(0, 0)), "The picked body swapped.");
            Assert.That(Body(board, Blue, 1), Is.EqualTo(new HexCoordinate(1, 0)), "His other body stayed.");
            Assert.That(board.TryGetHeroCoordinate(Ally, out var allyAt) && allyAt.Equals(new HexCoordinate(0, 2)), Is.True);
        }

        /// <summary>
        /// SpaceNamesBody: a hostile Push and a Swap of a body chosen as its space move THAT body. The
        /// board names the body standing on a space, reads it back there, and commits it there; a
        /// reference naming only the hero keeps answering the acting (default) body as before.
        /// </summary>
        [Test]
        public void AHostilePushAndASwap_OfABodyChosenAsItsSpace_MoveThatBody()
        {
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(-2, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(1, 0)));

            Assert.That(board.TryGetObjectAt(new HexCoordinate(1, 0), out var bodyTwo), Is.True);
            Assert.That(bodyTwo, Is.EqualTo(BoardObjectReference.ForHero(Blue)), "Still the same hero (hero grain).");
            Assert.That(bodyTwo.HeroBody, Is.EqualTo(2), "The space names the body standing on it.");
            Assert.That(board.TryGetObjectCoordinate(bodyTwo, out var read) && read.Equals(new HexCoordinate(1, 0)), Is.True);
            Assert.That(board.TryGetObjectCoordinate(BoardObjectReference.ForHero(Blue), out var hero) &&
                hero.Equals(new HexCoordinate(-2, 0)), Is.True, "The hero alone is still his default body.");

            // p.18's Push from Red's space, two spaces straight away: body 2 travels, body 1 does not.
            var push = board.ResolvePush(new HexCoordinate(0, 0), new HexCoordinate(1, 0), 2,
                bodyTwo.AsRuleObject(), false, RuleModificationSet.Empty);
            Assert.That(board.CommitValidatedDisplacement(bodyTwo, push.Endpoint).Accepted, Is.True);
            Assert.That(Body(board, Blue, 2), Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(Body(board, Blue, 1), Is.EqualTo(new HexCoordinate(-2, 0)));

            // The roster's "choose-target" that reads the space offers each body in reach as its space
            // and publishes the one picked, which is the space the Push above consumed.
            var roster = Handler(SyntheticRosterCapabilities.ChooseTargetV2);
            var reading = Context(board, Red, new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.OriginParameter] = EffectValue.Integer((int)RosterQueryOrigin.Actor),
                    [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(3),
                    [SyntheticRosterCapabilities.UnitKindParameter] = EffectValue.Integer((int)RosterUnitKind.Unit),
                    [SyntheticRosterCapabilities.RelationParameter] = EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                    ["activity"] = EffectValue.Integer((int)RuleActivity.ObjectPush)
                },
                declared: new[]
                {
                    new EffectOutputBinding(SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                });
            Assert.That(roster.Execute(reading).Choice.Candidates, Is.EquivalentTo(new[]
            {
                EffectValue.Space(new HexCoordinate(-2, 0)), EffectValue.Space(new HexCoordinate(3, 0))
            }));
            var chosen = roster.Resume(reading, new[] { EffectValue.Space(new HexCoordinate(3, 0)) });
            Assert.That(Output(chosen, SyntheticRosterCapabilities.ChosenSpaceOutput),
                Is.EqualTo(EffectValue.Space(new HexCoordinate(3, 0))));
            Assert.That(Output(chosen, SyntheticRosterCapabilities.ChosenHeroOutput), Is.EqualTo(EffectValue.Hero(Blue)));
            Assert.That(roster.Resume(reading, new[] { EffectValue.Hero(Blue) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        /// <summary>
        /// "Up to 2 enemy units" counts him once: his two bodies are offered as their spaces, but the
        /// pair of them is refused - two of his bodies are one unit - while one body with another
        /// unit, or one body alone, is taken.
        /// </summary>
        [Test]
        public void UpToTwoUnits_CountsHimOnce_SoTwoOfHisBodiesAreNotTwoUnits()
        {
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(1, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(-1, 0)));
            var handler = Handler(SyntheticRosterCapabilities.ChooseTargetSetV1);
            var context = Context(board, Red, new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.OriginParameter] = EffectValue.Integer((int)RosterQueryOrigin.Actor),
                    [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(1),
                    [SyntheticRosterCapabilities.UnitKindParameter] = EffectValue.Integer((int)RosterUnitKind.Unit),
                    [SyntheticRosterCapabilities.RelationParameter] = EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                    ["activity"] = EffectValue.Integer((int)RuleActivity.ObjectPush),
                    ["minimum"] = EffectValue.Integer(0),
                    ["maximum"] = EffectValue.Integer(2)
                },
                declared: new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                    new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                });

            var choice = handler.Execute(context).Choice;
            Assert.That(choice.Candidates, Is.EquivalentTo(new[]
            {
                EffectValue.Space(new HexCoordinate(1, 0)), EffectValue.Space(new HexCoordinate(-1, 0))
            }));
            Assert.That(choice.MaximumSelections, Is.EqualTo(1), "One unit stands adjacent, on two spaces.");
            Assert.That(handler.Resume(context, choice.Candidates.ToArray()).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory), "Two of his bodies are one unit (RZ-Q2).");
            var one = handler.Resume(context, new[] { EffectValue.Space(new HexCoordinate(-1, 0)) });
            Assert.That(one.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Output(one, "firstSpace"), Is.EqualTo(EffectValue.Space(new HexCoordinate(-1, 0))));
        }

        /// <summary>
        /// HeroOnce, "each enemy hero adjacent to that token discards" (Min's Death Grenade under W3):
        /// two of his bodies beside the grenade are one hero, so he discards once.
        /// </summary>
        [Test]
        public void EachEnemyHeroBesideTheGrenade_NamesHimOnce()
        {
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(-3, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(1, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(-1, 0)));
            var handler = Handler(MinEffectContent.GrenadeBlastTargetV1);
            var context = Context(board, Red,
                new Dictionary<string, EffectValue> { ["anchor"] = EffectValue.Space(new HexCoordinate(0, 0)) });
            Assert.That(handler.Execute(context).Choice.Candidates, Is.EqualTo(new[] { EffectValue.Hero(Blue) }));
        }

        /// <summary>
        /// PresenceEveryBody, p.11's Fast Travel: "no enemy heroes or minions" in the Zone. Any one of
        /// his bodies in the destination Zone blocks an enemy's Fast Travel there, body 1 or not.
        /// </summary>
        [Test]
        public void FastTravel_IsBlockedByAnyOfHisBodiesInTheZone()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants, map, configuration.TieBreakerState);
            var red = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blue = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            var zoneOf = map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable && space.ZoneId.HasValue)
                .ToDictionary(space => space.Coordinate, space => space.ZoneId.Value);
            var origin = zoneOf[red.Coordinate];
            var neighbour = map.Zones.Select(zone => zone.Id)
                .First(zone => !zone.Equals(origin) &&
                    map.GetZoneRelationship(origin, zone) == ZoneRelationship.Adjacent &&
                    !zone.Equals(zoneOf[blue.Coordinate]));
            var inNeighbour = zoneOf.Where(entry => entry.Value.Equals(neighbour)).Select(entry => entry.Key)
                .OrderBy(at => at.Q).ThenBy(at => at.R).ToArray();

            AdjacentMoveAuthority Staged(bool bodyTwoThere) => new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(red.HeroId, red.Coordinate),
                    new HeroOccupancy(blue.HeroId, 1, blue.Coordinate),
                    new HeroOccupancy(blue.HeroId, 2, bodyTwoThere ? inNeighbour[0] : blue.Coordinate.GetNeighbors()
                        .First(at => zoneOf.ContainsKey(at) && zoneOf[at].Equals(zoneOf[blue.Coordinate]) &&
                            !at.Equals(red.Coordinate)))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>(),
                new[] { new HeroBodySupply(blue.HeroId, 4) });

            CardActionResolutionRejectionReason Travel(AdjacentMoveAuthority board) =>
                FastTravelActionQuery.EvaluateDestinationForActor(
                    red.HeroId, inNeighbour[1], board, board, configuration.Participants,
                    Array.Empty<Core.Units.Minions.MinionOccupancy>(), FastTravelZoneScope.AdjacentZoneOnly);

            Assert.That(Travel(Staged(false)), Is.EqualTo(CardActionResolutionRejectionReason.None),
                "Fixture precondition: with no body in the Zone the Fast Travel is open.");
            Assert.That(Travel(Staged(true)),
                Is.EqualTo(CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInDestinationZone),
                "His second body alone in the Zone is an enemy hero there (a presence check sees every body).");
        }

        /// <summary>
        /// HeroLevelResult: an immunity is addressed to the hero, so it shields every body from a
        /// targeting question, while presence still sees them all (p.18).
        /// </summary>
        [Test]
        public void AnImmunityOnTheHero_ShieldsEveryBody_AndPresenceStillSeesThem()
        {
            var participants = StagedBoardParticipants.Create();
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(1, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(-1, 0)));
            var immune = new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForImmunity(RuleActivity.Skill), RuleObject.ForHero(Blue),
                        new ActiveEffectId("rz-immunity"), Blue, TeamSide.Blue, 1)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

            var open = TargetQuery.FindTargetsWithin(new HexCoordinate(0, 0), 1, false, RuleObject.ForHero(Red),
                RuleActivity.Skill, board, participants, RuleModificationSet.Empty);
            Assert.That(open.Select(target => target.HeroBody), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(TargetQuery.FindTargetsWithin(new HexCoordinate(0, 0), 1, false, RuleObject.ForHero(Red),
                RuleActivity.Skill, board, participants, immune), Is.Empty, "Neither body may be targeted.");
            Assert.That(UnitPresenceQuery.FindUnitsWithin(new HexCoordinate(0, 0), 1, EffectiveUnitKind.Hero, board, immune)
                .Count(unit => unit.Equals(RuleObject.ForHero(Blue))), Is.EqualTo(1),
                "Presence counts the immune hero, once.");
        }

        /// <summary>
        /// AttackedBody: a reading of "the target" names the body the Attack took (RZ-Q3) - Brynn's
        /// "target adjacent to three obstacles" reads that body and not any body of the hero (9.21a's
        /// open item), and the enclosing Attack's target reference addresses it.
        /// </summary>
        [Test]
        public void TheTargetOfCardText_IsTheBodyTheAttackTook()
        {
            // Body 2 stands beside three obstacles (Red and his bodies 3 and 4); body 1 beside none.
            var board = Board(
                new HeroOccupancy(Red, new HexCoordinate(2, 0)),
                new HeroOccupancy(Blue, 1, new HexCoordinate(-4, 0)),
                new HeroOccupancy(Blue, 2, new HexCoordinate(3, 0)),
                new HeroOccupancy(Blue, 3, new HexCoordinate(4, 0)),
                new HeroOccupancy(Blue, 4, new HexCoordinate(3, 1)));
            var tookBodyOne = new EnclosingAttackFacts(Red, Blue, default, AttackRangeKind.Ranged,
                new HexCoordinate(-4, 0), heroTargetBody: 1);
            var tookBodyTwo = new EnclosingAttackFacts(Red, Blue, default, AttackRangeKind.NonRanged,
                new HexCoordinate(3, 0), heroTargetBody: 2);

            Assert.That(tookBodyTwo.HeroTargetReference.HeroBody, Is.EqualTo(2));
            Assert.That(board.TryGetObjectCoordinate(tookBodyTwo.HeroTargetReference, out var at) &&
                at.Equals(new HexCoordinate(3, 0)), Is.True, "The target is read at the attacked body.");

            Assert.That(ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                board, Blue, RuleModificationSet.Empty, tookBodyTwo.HeroTargetBody), Is.True);
            Assert.That(ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, Blue, RuleModificationSet.Empty, tookBodyOne.HeroTargetBody), Is.False,
                "An Attack that took body 1 reads body 1's space, not whichever body qualifies.");
            Assert.That(ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, Blue, RuleModificationSet.Empty), Is.True,
                "A read naming no body (a selection's candidate hero) still sees every body (9.21a).");
        }

        /// <summary>
        /// Milestone 9.21h (RZ-Q13, the user's "any body"). The radius anchored on a hero standing on
        /// several spaces ("in radius" of "you", an Active effect the many-bodied hero controls) is read
        /// from every body: a space is in radius of him if it is in radius of at least one. Until 9.21h
        /// the reading was 9.21a's named refusal, and this case - Arien given four bodies (the soak's
        /// line-up 0), reaching his own aura at the ninth command - proved it refused atomically by
        /// name. The same forty commands now all resolve, the aura standing while he stands on four
        /// spaces.
        /// </summary>
        [Test]
        public void AFaceReachingAnAuraAnchoredOnAManyBodiedHero_ReadsEveryBody_AndResolves()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var picked = OfferOutcomeTests.RotatedLineups()[0];
            var session = new MatchSession(RandomLineupSoakTests.ComposeManyBodied(map, picked, 0));
            var random = new MatchLoopRandom(1);
            var auraWithSeveralBodies = false;
            for (var step = 0; step < 40; step++)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var chosen = legal[random.Next(legal.Count)];
                var result = session.Execute(chosen);
                Assert.That(result.Accepted, Is.True,
                    $"step {step}: {result.Detail} (RZ-Q13 settles the reading; nothing is refused).");
                var projection = session.Projection;
                var manyBodied = projection.Heroes.GroupBy(row => row.HeroId)
                    .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
                auraWithSeveralBodies |= projection.ActiveEffectSourceCardInstanceIds.Any(card =>
                    manyBodied.Contains(projection.Cards.Single(value => value.Id.Equals(card)).OwnerId));
            }

            Assert.That(auraWithSeveralBodies, Is.True,
                "Fixture precondition: the many-bodied hero's own Active effect stood while several of his " +
                "bodies did.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static AdjacentMoveAuthority Board(params HeroOccupancy[] heroes)
        {
            var coordinates = Enumerable.Range(-6, 13)
                .SelectMany(q => Enumerable.Range(-6, 13).Select(r => new HexCoordinate(q, r)))
                .ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("hero-bodies-sweep"), 1, coordinates),
                heroes,
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>(),
                new[] { new HeroBodySupply(Blue, 4) });
        }

        private static HexCoordinate Body(AdjacentMoveAuthority board, HeroId hero, int body)
        {
            Assert.That(board.TryGetHeroBodyCoordinate(hero, body, out var at), Is.True);
            return at;
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference)
        {
            Assert.That(MatchContentCatalogs.ForThisBuild().Capabilities.TryLoad(reference, out var handler), Is.True);
            return handler;
        }

        private static EffectValue Output(EffectExecutionResult result, string name) =>
            result.Outcomes.Single(fact => fact.OutputName == name).Value;

        private static Dictionary<string, EffectValue> IconInputs(SourceTeamRelation relation) =>
            new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Skill),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] = EffectValue.Integer((int)relation)
            };

        private static readonly ActionDefinition ProbeAction = new ActionDefinition(
            new ActionDefinitionId("rz-sweep-skill"),
            ActionKind.Skill,
            ActionDefinitionOrigin.PrintedPrimary,
            new[]
            {
                new PrintedValueDefinition(PrintedValueChannel.Radius, 3)
            },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
            AttackRangeKind.None,
            default,
            new EffectDefinition(
                new EffectDefinitionId("rz-sweep-effect"),
                "rev-1",
                new EffectNodeId("rz-sweep-root"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("rz-sweep-root"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.NoteActorV1)
                }));

        private static readonly CardDefinition ProbeCard = new CardDefinition(
            new CardDefinitionId("rz-sweep-probe"),
            "rev-1",
            "TEST - many-bodied sweep probe",
            new InitiativeValue(9),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[] { ProbeAction });

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            HeroId actor,
            IReadOnlyDictionary<string, EffectValue> inputs,
            IReadOnlyList<EffectOutputBinding> declared = null,
            EnclosingAttackFacts attack = default,
            MatchParticipants participants = null)
        {
            participants = participants ?? StagedBoardParticipants.Create();
            var card = new CardInstance(
                new CardInstanceId("rz-sweep-card"), ProbeCard.Reference, actor,
                new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Unresolved,
                    CardFaceOrientation.FaceUp));
            return new EffectCapabilityContext(
                new EffectNodeId("rz-sweep-node"),
                EffectFailurePolicy.Mandatory,
                actor,
                actor,
                card.Id,
                card.DefinitionReference,
                ProbeAction.Id,
                inputs,
                1,
                1,
                participants,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { ProbeCard }, new[] { card }),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                new HeroItemState(participants),
                enclosingAttack: attack,
                declaredOutputs: declared);
        }
    }
}
