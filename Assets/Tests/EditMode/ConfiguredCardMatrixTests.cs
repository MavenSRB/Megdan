using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.9 acceptance checks two and three: the exact configured primary-action matrix, the
    /// three general replacements that must stay derived, and the two showcase cards whose relationship
    /// no fixture had ever proved from the dealt hand.
    /// </summary>
    /// <remarks>
    /// The printed value columns are not repeated here.
    /// <see cref="CardDefinitionInstanceTests.SyntheticHands_MatchApprovedFiveCardValueAndActionMatrix"/>
    /// owns them and pins every Attack, Range, Movement and Defense number of all ten cards; what that
    /// fixture does not pin is *which* mode family each card's primary action is, because it compares
    /// <see cref="ActionKind"/> and six of the ten are Skills. This one pins the identity, which is what
    /// the gate's matrix actually names: Red Needle's Push/Place, Red Bulwark's modifier/restriction/
    /// permission, Red Bombard's direct Move/Spawn, Blue Feint's immunity/effective-type/component,
    /// Blue Sentinel's Swap/Replace/Remove, Blue Longbow's bounded Repeat, and the four printed Attacks.
    ///
    /// The two scenarios at the bottom are the check the showcase schedule has owed since milestone
    /// 6.2d. Blue Feint carries the token placement rather than Blue Sentinel, whose board-object family
    /// it belongs to, because p.8 removes every placed token at the end of a round and a card is played
    /// at most once in a round - a Sentinel supplying its own Hazard could never have one standing when
    /// it came back. The claim is therefore that Feint at Initiative 12 and Sentinel at 11 are two cards
    /// of one hand and one round holds both plays. <see cref="ConfiguredExchangeSkillTests"/> proved
    /// Sentinel's three branches with a Hazard supplied by the fixture-only Cache card; these prove them
    /// with the Hazard the dealt hand actually supplies.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </remarks>
    public sealed class ConfiguredCardMatrixTests
    {
        private static readonly ActionDefinitionId FeintSkill = new ActionDefinitionId("primary-immunity-skill");
        private static readonly ActionDefinitionId SentinelSkill = new ActionDefinitionId("primary-exchange-skill");

        private static readonly EffectNodeId FeintPlaceBranch = new EffectNodeId("feint-place-branch");
        private static readonly EffectNodeId FeintGiveMarker = new EffectNodeId("feint-give-marker");
        private static readonly EffectNodeId FeintReturnMarker = new EffectNodeId("feint-return-marker");
        private static readonly EffectNodeId SentinelReplaceBranch = new EffectNodeId("sentinel-replace-branch");
        private static readonly EffectNodeId SentinelRemoveBranch = new EffectNodeId("sentinel-remove-branch");

        // -----------------------------------------------------------------------------------------
        // The matrix
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheTenConfiguredCards_CarryTheExactApprovedPrimaryActionMatrix()
        {
            var red = SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero"));
            var blue = SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"));

            AssertPrimaryActions(red, new[]
            {
                Row("placeholder-red-needle", "TEST - Red Needle", 13, ActionKind.Skill, "primary-push-place-skill"),
                Row("placeholder-red-bulwark", "TEST - Red Bulwark", 11, ActionKind.Skill, "primary-modification-skill"),
                Row("placeholder-red-lancer", "TEST - Red Lancer", 9, ActionKind.Attack, null),
                Row("placeholder-red-pursuit", "TEST - Red Pursuit", 7, ActionKind.Attack, null),
                Row("placeholder-red-bombard", "TEST - Red Bombard", 5, ActionKind.Skill, "primary-direct-move-spawn-skill")
            });

            AssertPrimaryActions(blue, new[]
            {
                Row("placeholder-blue-feint", "TEST - Blue Feint", 12, ActionKind.Skill, "primary-immunity-skill"),
                Row("placeholder-blue-sentinel", "TEST - Blue Sentinel", 11, ActionKind.Skill, "primary-exchange-skill"),
                Row("placeholder-blue-scout-shot", "TEST - Blue Scout Shot", 8, ActionKind.Attack, null),
                Row("placeholder-blue-hammer", "TEST - Blue Hammer", 6, ActionKind.Attack, null),
                Row("placeholder-blue-longbow", "TEST - Blue Longbow", 4, ActionKind.Skill, "primary-push-repeat-skill")
            });
        }

        [Test]
        public void EachTeam_KeepsAtLeastOnePrintedRangedAndOnePrintedNonRangedAttack()
        {
            foreach (var hand in new[]
            {
                SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero")),
                SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"))
            })
            {
                var attacks = hand.DealtDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                        action.Kind == ActionKind.Attack)
                    .ToArray();

                Assert.That(attacks.Count(action => action.AttackRange == AttackRangeKind.Ranged),
                    Is.GreaterThanOrEqualTo(1), "Each team keeps a printed Ranged Attack.");
                Assert.That(attacks.Count(action => action.AttackRange == AttackRangeKind.NonRanged),
                    Is.GreaterThanOrEqualTo(1), "Each team keeps a printed non-Ranged Attack.");
            }
        }

        [Test]
        public void HoldFastTravelAndClear_AreDerivedAndNeverPrintedOnAShowcaseCard()
        {
            var printed = new[]
                {
                    SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero")),
                    SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"))
                }
                .SelectMany(hand => hand.DealtDefinitions)
                .SelectMany(definition => definition.ActionDefinitions)
                .ToArray();

            // p.7 lists Hold, Fast Travel and Clear among the secondary actions every hero has, not
            // among the icons a card prints. A showcase Skill that printed one would be invented text.
            Assert.That(printed.Any(action => action.Kind == ActionKind.Hold), Is.False);
            Assert.That(printed.Any(action => action.Kind == ActionKind.FastTravel), Is.False);
            Assert.That(printed.Any(action => action.Kind == ActionKind.Clear), Is.False);
            Assert.That(
                printed.Any(action => action.Origin == ActionDefinitionOrigin.Replacement),
                Is.False,
                "A replacement is derived from an eligible source action at the offer query, never " +
                "printed onto a card.");

            // And in a live turn the general rules still derive them. A Hold comes from the selected
            // unresolved card - which is why it cannot be asked of a hand that is not in a turn - and
            // Fast Travel replaces that card's own printed Secondary Movement rather than an invented
            // action of its own.
            // The ordinary configured starting position rather than this file's adjacent one, because
            // p.7's Fast Travel is refused while an enemy stands in the origin Zone - which is exactly
            // the position the scenarios below deliberately build.
            var live = ConfiguredMatchFixtures.CreateConfiguredSession();
            CommitOneCardEachBy(live, "red-lancer", "blue-feint");
            var actor = live.Projection.SelectedActor;

            var hold = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                actor.CardInstanceId, actor.ActorId,
                live.Projection.Definitions, live.Projection.Cards, RuleModificationSet.Empty);
            Assert.That(hold.HasValue, Is.True);
            Assert.That(hold.Value.IsAvailable, Is.True);
            Assert.That(hold.Value.InvocationContext,
                Is.EqualTo(ActionInvocationContext.GeneralRuleSecondary),
                "A Hold is a general-rule secondary action, not something the card printed.");
            Assert.That(hold.Value.SourceCardInstanceId, Is.EqualTo(actor.CardInstanceId),
                "It is derived from the selected card, so it names that card as its source.");

            var actingCard = live.Projection.Cards.Single(value => value.Id.Equals(actor.CardInstanceId));
            var actingDefinition = live.Projection.Definitions.Single(value =>
                value.Reference.Equals(actingCard.DefinitionReference));
            var printedMovement = actingDefinition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedSecondary &&
                action.Kind == ActionKind.Movement);
            Assert.That(live.Projection.AvailableFastTravelOptions, Is.Not.Empty);
            Assert.That(
                live.Projection.AvailableFastTravelOptions
                    .Select(option => option.ReplacedMovementActionDefinitionId),
                Is.All.EqualTo(printedMovement.Id),
                "Fast Travel replaces an eligible printed Movement offer and nothing else.");
            Assert.That(
                live.Projection.AvailableFastTravelOptions
                    .Select(option => option.ReplacementOffer.InvocationContext),
                Is.All.EqualTo(ActionInvocationContext.Replacement));
        }

        [Test]
        public void EveryConfiguredDefinition_IsVisiblyTestOnlyAndClaimsNoVerifiedContent()
        {
            foreach (var hand in new[]
            {
                SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero")),
                SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"))
            })
            {
                Assert.That(hand.Definitions, Has.All.Property("ContentAuthority")
                    .EqualTo(CardContentAuthority.SyntheticTestFixture),
                    "No configured definition may claim verified authority.");
                Assert.That(hand.Definitions, Has.All.Property("DisplayName").StartsWith("TEST - "),
                    "A synthetic definition says so in the one string a player would ever read.");
                Assert.That(
                    hand.Definitions.Any(definition =>
                        definition.Id.Value.IndexOf("wasp", StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.False,
                    "No configured definition names the hero whose inventory milestone 8.1 owns.");
            }
        }

        [Test]
        public void TheFixtureOnlySkillCards_StayOutOfEveryNormalHandAndKeepADistinctIsolationRole()
        {
            // Milestone 7.9's disposition of the extra Focus and Cache cards, which the gate requires
            // either retired or documented. They are retained, and the role each keeps is one no
            // configured showcase card can take over:
            //
            //   Focus (`primary-skill`, milestone 6.2a) is the only *board-neutral* Skill in the
            //   project. Its effect is a Sequence that marks the actor and then offers a `may` step
            //   opening a real decision, and it touches no space, no object and no component at all.
            //   That is what lets the execution-frame fixtures - ordering, an optional step, a
            //   capability-requested choice, a typed output binding, an Active effect crossing the
            //   snapshot boundary - assert on the frame without a board operation's outcome mixed in.
            //   All six configured Skills touch the board, so none of them can stand in for it.
            //
            //   Cache (`primary-component-skill`, milestone 6.2) reaches the same three component
            //   capabilities Blue Feint now carries, but reaches them alone: it prints one Skill whose
            //   only modes are place, give and return, on a hand with no board-object siblings and an
            //   Initiative outside its team's ordinary band. Blue Feint offers those three behind two
            //   timing modes and a five-way Choose, on a card whose whole point is that Blue Sentinel
            //   acts on what it places. A component fixture that had only Feint would be asserting on
            //   the showcase relationship every time it wanted one token on the board - and two corpus
            //   payloads, `placed-token-v21` and `attached-marker-v21`, are captured through Cache for
            //   exactly that reason.
            //
            // What the gate actually forbids is either of them enlarging a normal hand or sitting
            // there unexplained. The first half is what this asserts; the second is written above.
            var fixtureOnly = new[] { "placeholder-red-focus", "placeholder-blue-focus",
                "placeholder-red-cache", "placeholder-blue-cache" };

            foreach (var hand in new[]
            {
                SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero")),
                SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"))
            })
            {
                Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(5));
                Assert.That(
                    hand.AllInstances.Any(instance =>
                        fixtureOnly.Any(id => instance.Id.Value.StartsWith(
                            id.Replace("placeholder-", string.Empty), StringComparison.Ordinal))),
                    Is.False,
                    "Neither fixture-only card is dealt to, or held in the deck of, a normal hand.");
            }

            // And they exist where the fixtures that need them look. The Skill-fixture hand is the only
            // one that deals them, which is what keeps them out of the configured scene entirely.
            var skillHand = SyntheticShowcaseHand.CreateBlueWithSkill(new HeroId("blue-synthetic-hero"));
            Assert.That(
                skillHand.DealtDefinitions.Select(definition => definition.Id.Value),
                Has.Some.EqualTo("placeholder-blue-focus"));
            Assert.That(
                skillHand.DealtDefinitions.Select(definition => definition.Id.Value),
                Has.Some.EqualTo("placeholder-blue-cache"));

            // Neither states a colour or a Tier, so every progression query excludes them by
            // classification rather than by a colour that happened to be left unset.
            foreach (var definition in skillHand.DealtDefinitions.Where(value =>
                value.Id.Value == "placeholder-blue-focus" || value.Id.Value == "placeholder-blue-cache"))
            {
                Assert.That(definition.Progression, Is.EqualTo(CardProgressionProfile.NotStated),
                    $"{definition.Id.Value} must state no progression content of its own.");
            }
        }

        [Test]
        public void NoRegisteredCapability_IsDeferredToAVerifiedHeroMilestone()
        {
            // Milestone 7.9 acceptance check six, asked mechanically rather than by reading the roadmap:
            // "no source-reviewed general keyword or reusable core capability is deferred for first
            // implementation to a verified hero milestone". A capability that no card can reach is
            // exactly such a deferral - it exists as a handler, it registers, it serializes, and the
            // first thing that would ever run it is a hero this project has not written yet.
            //
            // The registration list is the one a snapshot records to say which contract set it was
            // written against, so this compares the same list the wire does. What it demands of each
            // entry is a card - configured or fixture - whose printed effect names it, because a card
            // is the only thing that starts an effect in this build.
            //
            // Milestone 8.9 splits that demand in two, because the rule the check quotes has two halves
            // and one registration set finally distinguishes them. A *capability* must be on some card
            // this build can deal - which, since milestone 8.4, includes Wasp's printed set as well as
            // the showcase hands, and leaving hers out would have made the rule ask a question about
            // half the build's cards. A *handler implementation* - the reusable core capability the
            // 7.9 check actually names - must additionally be reachable from a showcase card, so a
            // general contract still cannot arrive for the first time inside a verified hero's slice.
            // Wasp's two Active effects pass the second half because they run on the handler milestone
            // 6.8 proved on Red Bulwark; a new handler introduced by her content would not.
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;
            var registered = catalog.Registered;
            Assert.That(registered, Is.Not.Empty, "The catalog must register something to check.");

            var owner = new HeroId("capability-coverage-hero");
            var opponent = new HeroId("capability-coverage-opponent");
            var showcaseReachable = new[]
                {
                    SyntheticShowcaseHand.CreateRed(owner),
                    SyntheticShowcaseHand.CreateBlue(owner),
                    SyntheticShowcaseHand.CreateRedWithSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithRepeatSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithRepeatSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithCardStateSkill(owner, opponent),
                    SyntheticShowcaseHand.CreateBlueWithCardStateSkill(owner, opponent),
                    SyntheticShowcaseHand.CreateRedWithEscalatingSkill(owner, opponent),
                    SyntheticShowcaseHand.CreateBlueWithEscalatingSkill(owner, opponent),
                    SyntheticShowcaseHand.CreateRedWithConditionalAttackText(owner),
                    SyntheticShowcaseHand.CreateBlueWithConditionalDefenseText(owner),
                    SyntheticShowcaseHand.CreateRedWithRadiusSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithRadiusSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithOrbitSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithOrbitSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithKinesisSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithKinesisSkill(owner),
                    // Milestone 8.19a's four roster continuation shapes, which is where
                    // `set-attack-target@1` and `around-primary-action@1` become reachable from
                    // showcase content rather than waiting for a hero's card.
                    SyntheticShowcaseHand.CreateRedWithContinuationText(owner),
                    SyntheticShowcaseHand.CreateBlueWithContinuationText(owner),
                    // Milestone 8.19c's five persistent source shapes, dealt to both hands so each
                    // contract is reachable from two hero-shaped consumers rather than from one.
                    SyntheticShowcaseHand.CreateRedWithPersistentSourceSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithPersistentSourceSkill(owner),
                    // Milestone 8.19c1's two pausing source shapes, dealt to both hands for the same
                    // reason. The milestone's third pause is an Ultimate declaration rather than a
                    // capability, so it has nothing to register and nothing to reach.
                    SyntheticShowcaseHand.CreateRedWithPausingSourceSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithPausingSourceSkill(owner),
                    // Milestone 8.20's three roster selection contracts. Two of them - `choose-target@2`
                    // and `behind-target-line@1` - were implemented at 8.19b and reach the build's
                    // catalog only now, because this is the milestone at which a printed card first
                    // names one; `spaces-adjacent@1` is this milestone's own. All three are proved on
                    // showcase content here so that none of them arrives for the first time inside a
                    // verified hero's slice, which is what the second half of this gate is about.
                    SyntheticShowcaseHand.CreateRedWithRosterSelectionSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithRosterSelectionSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithLineBehindText(owner),
                    // Milestone 8.21's seven. Five were implemented at 8.19b and reach the build's
                    // catalog only now, because this is the milestone at which a printed card first
                    // names one; `discard-owned-card@1` and `owned-card-present@1` are this
                    // milestone's own. All seven are proved on showcase content here so that none of
                    // them arrives for the first time inside a verified hero's slice.
                    SyntheticShowcaseHand.CreateRedWithRosterPaymentSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithRosterPaymentSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithChargeAndVacatedSpaceText(owner),
                    // Milestone 8.22's five. Four were implemented at 8.19b and reach the build's
                    // catalog only now, because this is the milestone at which a printed card first
                    // names one; `target-is-adjacent@1` is this milestone's own. The command Skill
                    // carries the three a Skill can ask - the minion count against a threshold, the
                    // unordered pair, the bounded set - and the Attack-text card carries the two
                    // adjacency questions, which need an enclosing Attack to be about.
                    SyntheticShowcaseHand.CreateRedWithRosterCommandSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithRosterCommandSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithAdjacencyRiderText(owner),
                    // Milestone 8.23's four. Three were implemented at 8.19b and reach the build's
                    // catalog only now, because this is the milestone at which a printed card first
                    // names one; `actor-moved-this-action` is this milestone's own, minted because
                    // Combat Reflexes is the first face to ask at one of p.14's moments about what
                    // happened at another. The thieving Skill carries the two a Skill can ask - the
                    // terrain presence and the coin transfer - and the two-moment Attack card carries
                    // the two that need an enclosing Attack, on the first fixture action to print card
                    // text at both of p.14's attacker moments.
                    SyntheticShowcaseHand.CreateRedWithRosterTheftSkill(owner),
                    SyntheticShowcaseHand.CreateBlueWithRosterTheftSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithTwoMomentAttackText(owner),
                    // Milestone 8.35z27's one: `attack-sequence@1`, the leaf a modal
                    // Attack's "choose one, or both" publishes its chosen bullets through. It reaches
                    // the catalog on showcase content and on nothing else, because all three of its
                    // real consumers are unlanded - Middlefinger of Death is on a hero branch, and
                    // Grim Reaper and Spirit Bear are future heroes - which is exactly the case this
                    // gate's synthetic half exists for.
                    SyntheticShowcaseHand.CreateRedWithModalSequenceAttackText(owner),
                    // Milestone 8.35z34's two, and the reason there are two rather than four: the
                    // showcase half of this gate is asked of a handler TYPE, and Dodger's dealing
                    // registers four types across six capability ids. The Gravedigger reaches the
                    // empty-Spawn-point count and rulebook p.19's Respawn; the Reliquary reaches the
                    // Ultimate predicate and p.16's card-text item grant. Her printed-Radius arms of
                    // the first two run on the same two types and are admitted by the concat below,
                    // which is exactly the division this gate draws.
                    SyntheticShowcaseHand.CreateRedWithSpawnPointSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithBoxItemSkill(owner),
                    // Milestone 8.35z38's three, on exactly the division the two above draw: Misa's
                    // dealing registers SIX capability ids across FOUR handler types, and the showcase
                    // half of this gate is asked of the type. The Windrunner reaches the straight-line
                    // destination and its traversal selection; the Weathervane reaches the unit-pair
                    // selection; the Stormarcher's Attack text reaches both tenses of the
                    // enclosing-target range question, which need an enclosing Attack to be about and
                    // therefore cannot live on a Skill. Her second declaration of each shared type -
                    // the equal-distance pair, the present tense - runs on the same type and is
                    // admitted by the concat below.
                    SyntheticShowcaseHand.CreateRedWithLineTravelSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithUnitPairSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithRangeTenseRiderText(owner),
                    // Milestone 8.35z46's one, on the same division the pairs above draw:
                    // Silverarrow's dealing registers ONE capability id across ONE handler type, and
                    // the showcase half of this gate is asked of the type. The Pathfinder reaches
                    // `fast-travel@1`. `choose-line-destination@1`, her other new-to-her contract, is
                    // already registered and already proved above by Misa's Windrunner - it is not a
                    // second registration, so it owes no second showcase card.
                    SyntheticShowcaseHand.CreateRedWithFastTravelSkill(owner),
                    // Milestone 8.35z50's three, the largest set any dealing has needed, on the same
                    // division every pair above draws: Ursafar's dealing registers three new handler
                    // TYPES, and the showcase half of this gate is asked of the type. The Berserker
                    // reaches `hero-condition@1` - the enraged predicate twelve of his faces read - and
                    // the Scavenger reaches `borrow-primary-action@1`, the contract Angry Roar and his
                    // two borrowing Greens start. The Tracker's Attack text reaches
                    // `attack-target-survived@1`, which needs an enclosing Attack to be about and
                    // therefore cannot live on a Skill. His fifteen Active-effect creations run on
                    // ActiveEffectCreationHandler and are admitted by the concat below, which is
                    // exactly the division this gate draws.
                    SyntheticShowcaseHand.CreateRedWithHeroConditionSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithBorrowedActionSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithTargetSurvivedRiderText(owner),
                    // Milestone 8.35z53's four handler types on THREE cards, which is the same
                    // grouping 8.35z34 used to reach four ids on two: the Executioner carries the
                    // discard predicate and the minion trio because Whisper's own faces put them in
                    // one sentence, the Displacer carries the union-selection her two Swaps name, and
                    // the Magistrate's second card-text body carries the defender-steered forced move,
                    // which needs p.14's Defense window to have happened before it is about anything.
                    // `synthetic-radius-unit-target@1` needs no card here - it is a seventh declaration
                    // over PrintedIconTargetHandler - and her one Active-effect creation runs on
                    // ActiveEffectCreationHandler, so both are admitted by the concat below.
                    SyntheticShowcaseHand.CreateRedWithDiscardGatedTrioSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithSwapSubjectSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithDefendedForcedMoveText(owner),
                    // Milestone 8.37's EIGHT handler types on FIVE cards, the largest set any dealing
                    // has needed and grouped on the same rule: two contracts share a card when they
                    // share a printed SENTENCE, because a fixture that split them would prove a shape
                    // no face has. The Spiritbinder names a friendly hero and has THAT hero name a
                    // colour; the Galecaller chooses a direction and batch-moves along it; the
                    // Cairnwright places a hero token and answers p.18's short supply beside it; the
                    // Revenant reads its own discard state; and the Frostblade's second card-text body
                    // reads the colour the defender spent, which needs p.14's Defense window to have
                    // happened before it is about anything.
                    SyntheticShowcaseHand.CreateRedWithColorNamingSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithDirectionBatchSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithHeroTokenSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithSourceDiscardSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithDefendedColorRiderText(owner),
                    // Milestone 8.39i's TWO handler types on TWO cards, and the grouping rule is why
                    // there are two rather than one: a card carries two contracts only when one
                    // printed SENTENCE carries both, and Cutter's obstacle grapple and her solo
                    // victory are her Silver basic and her Blue Tier III. The Grapnel is the only
                    // showcase Skill in the file that PRINTS a value - the obstacle selection reads
                    // the acting card's own Range through `RangeValueQuery`, so a card printing none
                    // could name the contract and never offer a candidate - and the Privateer's
                    // threshold is deliberately unreachable, because this is the first showcase card
                    // whose handler can END A MATCH and proving a contract must not do that.
                    SyntheticShowcaseHand.CreateRedWithObstacleGrappleSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithSoloVictorySkill(owner),
                    // Milestone 8.40j's FOUR handler types on TWO cards, and the grouping rule is why
                    // there are two rather than four: two contracts share a card exactly when one
                    // printed SENTENCE carries both. Close Call prints "If a hero in play has a bounty
                    // marker, block the attack and that hero gives the marker to you", so the Bondsman
                    // carries the holder predicate and the transfer over the fixture's own Focus
                    // marker - a definition-bound handler can only be showcased on a marker that is
                    // nobody's, which is the shape `synthetic-give-marker@1` set at milestone 6.2. And
                    // his three guessing Blues print "An enemy hero in radius ... chooses one of those
                    // cards; Guess the card's color, then reveal the card", so the Cardsharp carries
                    // the hidden pick and the reveal.
                    SyntheticShowcaseHand.CreateRedWithMarkerBoardSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithHiddenGuessSkill(owner),
                    // Milestone 8.61j's TWO handler types on TWO cards, and the grouping rule is
                    // why there are two rather than one: a card carries two contracts only when one
                    // printed SENTENCE carries both, and Min's rider borrow is her Gold basic while
                    // her deck-card item is the second bullet of her Green Tier III. The Understudy
                    // names the card whose "After the attack" text a borrow would apply - the
                    // SPLICE that applies it is the resolution authority's rather than a node's, so
                    // nothing here consumes the output - and the Archivist asks for a Tier no
                    // fixture deck holds, so proving the contract cannot take a card out of a
                    // fixture's own deck as a side effect.
                    SyntheticShowcaseHand.CreateRedWithBorrowedCardTextSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithDeckItemSkill(owner),
                    // Milestone 8.69g's THREE handler types on THREE cards, and the grouping rule is
                    // why there are three rather than one: a card carries two contracts only when one
                    // printed SENTENCE carries both, and Brynn's three questions live on three
                    // different faces - the guides' second sentence, the split Reds' gate and the
                    // tracking swaps' subject. Two are Skills printing a Radius 3, because both
                    // contracts measure their reach off the performing card's own printed icon; the
                    // third is an "After the attack" rider on an Attack, because the fact it publishes
                    // is a fact about the Attack that is running and a Skill would have nothing to ask
                    // it about.
                    SyntheticShowcaseHand.CreateRedWithObstacleRadiusSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithTrackingSwapSubjectSkill(owner),
                    SyntheticShowcaseHand.CreateRedWithObstacleTargetingRiderText(owner),
                    SyntheticResolvedRepriseHand.Create(owner),
                    SyntheticCoupledEscortHand.Create(owner)
                }
                .SelectMany(hand => hand.Definitions)
                .SelectMany(NamedCapabilities)
                .Distinct()
                .ToArray();
            var reachable = showcaseReachable
                .Concat(WaspCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.20. Arien's printed set is the second verified hand this build can
                // deal, so the same half of the rule that admitted Wasp's admits his: a capability on
                // a card a match deals is not a deferral. The other half - that no *handler* arrives
                // for the first time inside a hero slice - still binds, and his five Active effects
                // and two printed-icon twins all run on handlers the showcase already reaches.
                .Concat(ArienCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.21. Brogan's printed set is the third, and it is admitted on the same
                // half of the rule: a capability named by a card a configured match deals is not a
                // deferral, whichever staged configuration deals it. The other half still binds - no
                // *handler* may arrive for the first time inside a hero slice - and his two new ones
                // are proved on showcase content above before this concat admits the cards that name
                // them.
                .Concat(BroganCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.22. Sabina's printed set is the fourth, admitted on the same half of the
                // rule: a capability named by a card a configured match deals is not a deferral. The
                // other half still binds - no *handler* may arrive for the first time inside a hero
                // slice - and her one new one is proved on showcase content above before this concat
                // admits the cards that name it.
                .Concat(SabinaCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.23. Tigerclaw's printed set is the fifth, admitted on the same half of
                // the rule: a capability named by a card a configured match deals is not a deferral.
                // The other half still binds - no *handler* may arrive for the first time inside a
                // hero slice - and the check below is what holds it, on his four new contracts and on
                // the printed-Radius Place twin beside them.
                .Concat(TigerclawCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.24. Xargatha's printed set is the sixth and the last of the 8.17 roster,
                // admitted on the same half of the rule: a capability named by a card a configured
                // match deals is not a deferral. The other half still binds - no *handler* may arrive
                // for the first time inside a hero slice - and her five new contracts all run on
                // ActiveEffectCreationHandler, the shape the showcase has reached since milestone 6.8.
                // Her two end-of-round sources reach their effects through the triggered-effect concat
                // below rather than through a card node, which is what that concat is there for.
                .Concat(XargathaCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.35z34. Dodger's printed set is the SEVENTH, and the first from outside
                // the 8.17 roster - admitted on the same half of the rule: a capability named by a card
                // a configured match deals is not a deferral. The other half still binds, and this is
                // the first hero for whom it did real work: four of her six new contracts are new
                // HANDLER TYPES, so her dealing had to add showcase consumers for them before this
                // concat could admit the cards that name them. Her Active-effect pair is the exception
                // and says why - it runs on ActiveEffectCreationHandler, which the showcase has reached
                // since milestone 6.8.
                .Concat(DodgerCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.35z38. Misa's printed set is the EIGHTH, admitted on the same half of the
                // rule: a capability named by a card a configured match deals is not a deferral. The
                // other half did real work again - four of her six new roster contracts are new HANDLER
                // TYPES, proved on the three showcase cards above before this concat admits the faces
                // that name them. Her two Place declarations and her six Active-effect creations are
                // the exceptions and say why: PlaceHandler and ActiveEffectCreationHandler are types
                // the showcase has reached since milestones 6.6 and 6.8.
                //
                // Her three Green step-moves reach their effects through the triggered-effect concat
                // below rather than through a card node, which is what that concat is there for.
                .Concat(MisaCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.35z46. Silverarrow's printed set is the NINTH, admitted on the same half
                // of the rule: a capability named by a card a configured match deals is not a
                // deferral. The other half did real work once more - one of her two new roster
                // contracts is a new HANDLER TYPE, proved on the showcase card above before this
                // concat admits the faces that name it. Her other new contract,
                // `choose-line-destination@1`, is the exception and says why: it registered already at
                // Misa's dealing, and her three straight-line faces reach the same registered contract
                // Misa's five Blue Skills do. Her six Active-effect creations owe no showcase card
                // either - they run on ActiveEffectCreationHandler, the type the showcase has reached
                // since milestone 6.8.
                //
                // Her three "End of turn:" and permanent-ability effects reach theirs through the
                // triggered-effect concat below rather than through a card node, which is what that
                // concat is there for.
                .Concat(SilverarrowCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.35z50. Ursafar's printed set is the TENTH, admitted on the same half of
                // the rule: a capability named by a card a configured match deals is not a deferral.
                // The other half did the most work it has done for any dealing so far - THREE of his
                // contracts are new HANDLER TYPES (`hero-condition@1`, `borrow-primary-action@1` and
                // `attack-target-survived@1`), each proved on its own showcase card above before this
                // concat admits the faces that name it. His fifteen Active-effect creations owe no
                // showcase card - they run on ActiveEffectCreationHandler, the type the showcase has
                // reached since milestone 6.8 - and he contributes no triggered definition at all,
                // because Unbound Fury prints a permanent ability and declares none.
                .Concat(UrsafarCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.35z53. Whisper's nineteen faces, on the same terms. FOUR of her roster
                // contracts are new HANDLER TYPES - `choose-target-trio@1`, `defended-card-forced-move@1`,
                // `enemy-hero-with-discard-present@1` and `choose-swap-subject@1` - each proved on its
                // own showcase card above before this concat admits the faces that name it.
                // `synthetic-radius-unit-target@1` owes no showcase card: it is a seventh declaration
                // over PrintedIconTargetHandler, the type p.9's two icons have shared since 8.19b. Her
                // one Active-effect creation (Death Seeker's turn-long immunity) owes none either - it
                // runs on ActiveEffectCreationHandler, the type the showcase has reached since 6.8.
                .Concat(WhisperCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.37. Tali's nineteen faces, the twelfth and last set, on the same terms.
                // SIX of her contracts are new HANDLER TYPES - `synthetic-source-card-in-discard@1`,
                // `friendly-hero-in-play@1`, `name-color@1`, `synthetic-choose-direction@1`,
                // `synthetic-batch-direct-move@1` and `defended-card-color@1` - each proved on its own
                // showcase card above before this concat admits the faces that name it. Her six token
                // handlers owe no showcase card of their own: they run on SyntheticTokenCapabilities'
                // place and reclaim handlers, the types milestone 6.2's token placement proved and the
                // showcase has reached ever since. She declares no Active effect at all.
                .Concat(TaliCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.38e. Swift's eighteen dealt faces, the thirteenth set and the first
                // after the package arc, on the same terms. ONE of her contracts is a new HANDLER TYPE
                // - `subject-adjacent-terrain-presence@1`, the enemy-scoped reading her three Green
                // faces qualify their target by - and it is proved on the roster-theft showcase card
                // above, beside the actor-scoped declaration it is deliberately NOT a parameter of,
                // before this concat admits the faces that name it. Her two straight-line Places and
                // her Reload!'s borrow owe no showcase card of their own: they run on PlaceHandler and
                // BorrowPrimaryActionHandler, types this build already registers, and milestone 7.9's
                // gate asks its second half of the handler TYPE.
                .Concat(SwiftCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.39i. Cutter's eighteen dealt faces, the fourteenth set, on the same
                // terms. TWO of her contracts are new HANDLER TYPES -
                // `synthetic-range-obstacle-target@1`, the obstacle her Silver basic grapples to, and
                // `claim-solo-victory@1`, the ending her Blue Tier III claims - each proved on its own
                // showcase card above before this concat admits the faces that name them. The other
                // nineteen contracts her set names were all registered before this milestone, which is
                // what thirteen lettered build blocks bought. She declares no Active effect at all.
                .Concat(CutterCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.40j. Bain's eighteen dealt faces, the fifteenth set, on the same terms.
                // FOUR of his contracts are new HANDLER TYPES - `bain-transfer-bounty-marker@1`
                // (8.40b), `bain-bounty-marker-held@1` (8.40c) and 8.40f's two hidden-information
                // contracts - each proved on one of the two showcase cards above before this concat
                // admits the faces that name them. His give and his return owe none: those run on
                // `MarkerGivingHandler` and `ReturnMarkerHandler`, types this build has registered and
                // showcased since milestones 6.2 and 8.23. His five Active-effect creations owe none
                // either - `ActiveEffectCreationHandler` is the type the showcase has reached since
                // milestone 6.8 - and his Ultimate's effect and Another One!'s End-of-turn repeat
                // reach theirs through the triggered-effect concat below rather than through a card
                // node, which is what that concat is there for.
                .Concat(BainCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                .Concat(GarrusCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                .Concat(WidgetCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.57g, the eighteenth dealing. Her two beside-a-named-hero Places are
                // reachable from this concat and from nowhere else: Throw the Gauntlet names the
                // printed-Range one and her three Green Movements the printed-Radius twin, which is
                // the whole of what registering her at 8.57g bought. She owes no showcase consumer -
                // both are further declarations over `PlaceHandler`, a type this build has registered
                // since milestone 6.6, and the gate's second half is asked of a handler TYPE.
                .Concat(RowennaCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.61j, the nineteenth dealing and the widest a dealing has been: her
                // SEVENTEEN own contracts plus two shared ones are reachable from this concat and
                // from nowhere else. Two of the nineteen are new handler TYPES -
                // `borrow-after-attack-text@1` and `take-deck-card-as-item@1` - each proved on its
                // own showcase card above before this concat admits the faces that name them. The
                // other seventeen owe no showcase consumer: her token operations run on
                // `SyntheticTokenCapabilities` and `SyntheticOwnedTokenCapabilities`, types
                // registered and showcased since milestone 8.37; her two anchored hero selections
                // run on Widget's `SyntheticAnchoredTargetCapabilities`; and her four Active-effect
                // creations run on `ActiveEffectCreationHandler`, the type Arien's five persistent
                // texts proved at milestone 6.8. Her seven triggered bodies - four mine-trap
                // consequences, two end-of-turn grenades and Flurry's repeat - reach their
                // capabilities through the triggered-effect concat below rather than through a card
                // node, which is what that concat is there for.
                .Concat(MinCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                // Milestone 8.69g, the twentieth dealing and the narrowest a dealing has been since
                // Rowenna's: his THREE own contracts are reachable from this concat and from nowhere
                // else. ALL THREE are new handler TYPES, each proved on its own showcase card above
                // before this concat admits the ten faces that name them. Nothing else of his owes a
                // showcase consumer, because everything else his eighteen faces name is the roster's
                // own vocabulary - the modal Attack, `choose-target@2`, the three printed-Radius
                // selections, `synthetic-direct-move@3`, `synthetic-swap-actor@1`, both discards, the
                // Defeat and `retrieve-card@1`. He declares no token, no marker and no triggered
                // effect, so there is no triggered-effect contribution of his to the concat below.
                .Concat(BrynnCardContent.DealtDefinitions.SelectMany(NamedCapabilities))
                .Concat(MatchContentCatalogs.ForThisBuild().TriggeredEffects.Definitions
                    .SelectMany(definition => definition.Nodes)
                    .Where(node => node.Capability.IsDefined)
                    .Select(node => node.Capability))
                .Distinct()
                .ToArray();

            var deferred = registered
                .Where(capability => !reachable.Any(value => value.Equals(capability)))
                .Select(capability => capability.CapabilityId.Value)
                .Where(id => !FrameShapeStubs.Contains(id, StringComparer.Ordinal))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(deferred, Is.Empty,
                "Every registered capability must already be reachable from a printed card. These are " +
                $"not: {string.Join(", ", deferred)}. A capability first reached by verified hero " +
                "content is a deferral this gate exists to refuse.");

            var showcaseHandlerTypes = new HashSet<Type>(showcaseReachable
                .Select(capability => catalog.Load(capability).GetType()));
            var heroOnlyHandlerTypes = registered
                .Where(capability => !showcaseReachable.Any(value => value.Equals(capability)))
                .Select(capability => catalog.Load(capability).GetType())
                .Where(type => !showcaseHandlerTypes.Contains(type))
                .Select(type => type.Name)
                .Where(name => !FrameShapeStubHandlers.Contains(name, StringComparer.Ordinal))
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(heroOnlyHandlerTypes, Is.EqualTo(new[]
                {
                    "DefeatMinionHandler",
                    "HeroAdjacentToMinionHandler"
                }),
                "Milestone 8.13's approved reusable delta is exactly one staged minion-Defeat request " +
                "and one pre-removal adjacency selector. Any further hero-only implementation is an " +
                "unapproved deferral from the synthetic gate.");

            // The exclusion list is not allowed to be a place things quietly go. Every entry must still
            // be registered, so a stub that is deleted or renamed fails here rather than sitting in a
            // list nothing reads.
            foreach (var stub in FrameShapeStubs)
            {
                Assert.That(
                    registered.Any(capability => string.Equals(
                        capability.CapabilityId.Value, stub, StringComparison.Ordinal)),
                    Is.True,
                    $"{stub} is declared as a frame-shape stub but is no longer registered.");
            }
        }

        /// <summary>
        /// The registered capabilities that are deliberately on no card, and why each is not a deferral.
        /// </summary>
        /// <remarks>
        /// These five exist to give the effect-resolution fixtures a node with a known shape - one that
        /// records that it ran, one that reports a condition, one that is impossible, one that declines,
        /// one that accepts a hero and binds it. They implement no rule and reference no rulebook page,
        /// so there is nothing about them a verified hero milestone could be waiting for; putting any of
        /// them on a card would be inventing content to satisfy a test. Every capability that does
        /// implement something - the discard, the Radius-bounded selection, every board operation, every
        /// component operation, every modification - is on a card, which is what the case above proves.
        /// </remarks>
        private static readonly string[] FrameShapeStubs =
        {
            "synthetic-accept-hero",
            "synthetic-declined",
            "synthetic-impossible",
            "synthetic-is-flagged",
            "synthetic-note-actor"
        };

        /// <summary>
        /// The handler implementations behind the five stubs above, excluded from the second half of
        /// the rule for the same reason and no other: a handler reachable from no card at all cannot be
        /// shown reachable from a showcase card either, and the list above already says why these five
        /// are on no card.
        /// </summary>
        private static readonly string[] FrameShapeStubHandlers =
        {
            "AcceptHeroHandler",
            "DeclinedHandler",
            "ImpossibleHandler",
            "IsFlaggedHandler",
            "NoteActorHandler"
        };

        /// <summary>Every capability one card definition's printed effects name.</summary>
        /// <summary>
        /// Every capability a printed card names, from both bodies of card text an action may declare.
        ///
        /// <para>Milestone 8.23 added the second: Combat Reflexes and Leaping Strike print text at two
        /// of rulebook p.14's moments, and a reader that walked only the first would report the second's
        /// contracts as unreachable - which is exactly what this gate would then refuse for the wrong
        /// reason.</para>
        /// </summary>
        private static IEnumerable<EffectCapabilityReference> NamedCapabilities(CardDefinition definition) =>
            definition.ActionDefinitions
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(root => root != null)
                .SelectMany(root => root.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability);

        // -----------------------------------------------------------------------------------------
        // Blue Feint's component modes, from the dealt hand
        // -----------------------------------------------------------------------------------------

        [Test]
        public void BlueFeint_OffersItsFiveModesFromTheDealtFiveCardHand()
        {
            var session = StartFeintSkill(out _);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId.Value),
                Is.EqualTo(new[]
                {
                    "feint-push-immunity", "feint-counts-as-minion", "feint-place-branch",
                    "feint-give-marker", "feint-return-marker"
                }),
                "Milestone 6.8b's two timing modes and the three the retired Cache card handed over, " +
                "in the definition's own order, offered from the card the configured scene deals.");
            Assert.That(choice.ChooserId, Is.EqualTo(session.Projection.SelectedActor.ActorId));

            // Milestone 8.35z35 (Ruling 17). This mode used to be projected greyed here, and the
            // reason recorded was that p.18 scopes a return to the source that gave and this hand has
            // given no marker. The scoping is unchanged and still true; what changed is what it makes
            // the mode. The node declares `if able`, which is p.10 saying an impossible step is simply
            // not taken - so nothing about this branch stops the resolution, and a branch that stops
            // nothing is empty rather than blocked. Ruling 17 offers an empty option, because taking it
            // is a legal answer: the return does not happen, the node completes, and the card is not
            // spent on a stop. Greying it was the offer surface disagreeing with the branch's own
            // declared policy.
            // `Has.No.Member` rather than `Does.Not.Contain`: the sequence's element is the value type
            // EffectNodeId, and under Unity's compiler `Does.Not.Contain` binds to NUnit's string
            // overload and fails the Edit Mode BUILD while the single-assembly dotnet harness compiles
            // it happily. Fourth recorded instance of that trap; the collection form is this one.
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Has.No.Member(FeintReturnMarker),
                "`if able` means the step is skipped, not that the mode cannot be taken.");
            Assert.That(
                choice.SelectInvocations.Any(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(FeintReturnMarker)),
                Is.True,
                "So the mode is offered AND selectable, and choosing it returns no marker - which is " +
                "exactly what the card text says happens when there is none to return.");
        }

        [Test]
        public void BlueFeintsPlacement_SuppliesTheHazardBlueSentinelsReplaceTakesInTheSameRound()
        {
            var session = PlaceAHazardWithBlueFeint(out var tokenAt, out var roundIndex);

            FinishTheTurn(session);
            CommitOneCardEachBy(session, "red-pursuit", "blue-sentinel");
            StartSkill(session, SentinelSkill);
            ChooseBranch(session, SentinelReplaceBranch);
            SelectSpace(session, tokenAt);

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(roundIndex),
                "One round holds both the play that places the token and the play that acts on it, " +
                "which is the whole reason the placement is on Feint rather than on Sentinel.");
            var standing = session.Projection.PlacedTokens.Single(token => token.Coordinate.Equals(tokenAt));
            Assert.That(standing.Definition.Reference, Is.EqualTo(SyntheticComponentDefinitions.WardV1),
                "p.19's Replace substitutes a different object, so the Hazard Feint placed is gone and " +
                "a Ward stands in its space.");
        }

        [Test]
        public void BlueFeintsPlacement_SuppliesTheHazardBlueSentinelsRemoveTakesInTheSameRound()
        {
            var session = PlaceAHazardWithBlueFeint(out var tokenAt, out var roundIndex);

            FinishTheTurn(session);
            CommitOneCardEachBy(session, "red-pursuit", "blue-sentinel");
            StartSkill(session, SentinelSkill);
            ChooseBranch(session, SentinelRemoveBranch);
            SelectSpace(session, tokenAt);

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(roundIndex));
            Assert.That(
                session.Projection.PlacedTokens.Any(token => token.Coordinate.Equals(tokenAt)),
                Is.False,
                "A standalone Remove returns the token Feint placed to its supply inside the same round.");
        }

        // -----------------------------------------------------------------------------------------
        // The fourth typed contribution source
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ADefenderStandingOnItsOwnHazard_ReadsTheContinuousSourceInItsDefensePreview()
        {
            // Milestone 7.9's own defect, found by playing rather than by reading. The Hazard token has
            // granted its controller Defense +1 since milestone 6.8, and rulebook p.19 keeps a token's
            // properties outside the Active-effect rules, so the contribution it makes is the fourth
            // typed source - neither a card, nor a minion, nor an Active effect. The Defense preview
            // read the first three and threw `InvalidOperationException` on the fourth, so attacking a
            // hero who controlled a placed token made the match unplayable. No earlier fixture had ever
            // attacked one; `DeterministicMatchLoop` did it on its first seeded run.
            var session = PlaceAHazardWithBlueFeint(out _, out _);

            // Milestone 8.35g. Feint's resolution finished, so its own player holds the turn until
            // they release it; Red's offers below belong to the hero that release hands on to.
            Accept(session, DirectorCommand.ReleaseTurn());

            // Red's Lancer is committed in the same turn and acts after Feint's Initiative 12, so the
            // Attack lands on the hero that now controls the token.
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable && DefinitionOf(session, offer).ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));

            var preview = session.Projection.PendingDefensePreview;
            Assert.That(preview, Is.Not.Null,
                "Building this preview is what used to throw; reaching it at all is the fix.");
            var contributions = preview.Choices
                .Where(choice => !choice.IsDecline)
                .SelectMany(choice => choice.DefenseContributions)
                .ToArray();
            var continuous = contributions
                .Where(contribution => contribution.Kind == ValueContributionKind.ContinuousSourceModifier)
                .ToArray();

            Assert.That(continuous, Is.Not.Empty,
                "The placed Hazard's property reaches its controller's Defense Total.");
            Assert.That(continuous.Select(contribution => contribution.Amount), Is.All.EqualTo(1));
            Assert.That(continuous, Has.All.Property("SourceLabel").StartsWith("Continuous source "),
                "The fourth source gets a readable label of its own rather than an exception.");
            Assert.That(
                continuous.Any(contribution => string.IsNullOrWhiteSpace(contribution.SourceLabel)),
                Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // Scenario
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The configured match with the two heroes standing beside each other, played one turn: Blue
        /// commits Feint at Initiative 12 and Red its Ranged Lancer at 9, so Blue acts first and places
        /// a Hazard, and the turn is left standing on Red's action.
        /// </summary>
        private static MatchSession PlaceAHazardWithBlueFeint(
            out HexCoordinate tokenAt, out int roundIndex)
        {
            var session = CreateAdjacentConfiguredSession();
            CommitOneCardEachBy(session, "red-lancer", "blue-feint");
            StartSkill(session, FeintSkill);
            ChooseBranch(session, FeintPlaceBranch);

            // The place branch is p.18's order made explicit: an `if able` reclamation the supply makes
            // unavailable while nothing is placed, then the placement itself.
            var choice = session.Projection.PendingEffectChoice;
            tokenAt = choice.Candidates[0].Value.Coordinate;
            SelectSpace(session, tokenAt);

            var placed = session.Projection.PlacedTokens.Single();
            Assert.That(placed.Definition.Reference, Is.EqualTo(SyntheticComponentDefinitions.HazardV4));
            Assert.That(placed.Coordinate, Is.EqualTo(tokenAt));
            roundIndex = session.Projection.RoundIndex;
            return session;
        }

        private static MatchSession StartFeintSkill(out HexCoordinate blueAt)
        {
            var session = CreateAdjacentConfiguredSession();
            blueAt = session.Projection.Heroes
                .Single(hero => session.Projection.Participants
                    .GetTeamForHero(hero.HeroId).Side == TeamSide.Blue)
                .Coordinate;
            CommitOneCardEachBy(session, "red-lancer", "blue-feint");
            StartSkill(session, FeintSkill);
            return session;
        }

        /// <summary>
        /// The configured 1v1 match on the official map with the Blue hero moved beside the Red one, so
        /// one hero's Attack can reach the other and a placed token stands beside both.
        /// </summary>
        private static MatchSession CreateAdjacentConfiguredSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;

            var heroes = setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, besideSpaceId), beside)
                    : hero)
                .ToArray();
            var tuned = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes,
                setup.Minions);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                tuned.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, tuned, new[] { map }));
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        private sealed class PrimaryActionRow
        {
            public PrimaryActionRow(
                string definitionId, string displayName, int initiative, ActionKind kind, string actionId)
            {
                DefinitionId = definitionId;
                DisplayName = displayName;
                Initiative = initiative;
                Kind = kind;
                ActionId = actionId;
            }

            public string DefinitionId { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public ActionKind Kind { get; }

            /// <summary>The exact Skill this card carries, or null for the four printed Attacks.</summary>
            public string ActionId { get; }
        }

        private static PrimaryActionRow Row(
            string definitionId, string displayName, int initiative, ActionKind kind, string actionId) =>
            new PrimaryActionRow(definitionId, displayName, initiative, kind, actionId);

        private static void AssertPrimaryActions(ConfiguredHand hand, PrimaryActionRow[] rows)
        {
            Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(rows.Length),
                "p.15 holds a hand at exactly five cards, one of each colour.");
            Assert.That(hand.Instances, Has.Count.EqualTo(rows.Length));

            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                var definition = hand.DealtDefinitions[index];
                var instance = hand.Instances[index];

                Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId(row.DefinitionId)),
                    "A showcase conversion advances a revision, never a card's stable identity.");
                Assert.That(definition.DisplayName, Is.EqualTo(row.DisplayName));
                Assert.That(definition.Initiative.Value, Is.EqualTo(row.Initiative));
                Assert.That(instance.DefinitionReference, Is.EqualTo(definition.Reference),
                    "The hand slot holds the instance of the definition the matrix names for it.");
                Assert.That(instance.OwnerId, Is.EqualTo(hand.Instances[0].OwnerId),
                    "All five cards of a hand belong to the one hero it was dealt to.");

                var primary = definition.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                Assert.That(primary.Kind, Is.EqualTo(row.Kind));
                if (row.ActionId != null)
                {
                    Assert.That(primary.Id, Is.EqualTo(new ActionDefinitionId(row.ActionId)),
                        $"{row.DisplayName} carries the exact showcase mode family the matrix names.");
                }
                else
                {
                    Assert.That(primary.AttackRange, Is.Not.EqualTo(AttackRangeKind.None),
                        "A printed Attack declares whether it is Ranged.");
                }

                Assert.That(
                    definition.ActionDefinitions.Count(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedSecondary &&
                        action.Kind == ActionKind.Movement),
                    Is.EqualTo(1),
                    "Every showcase card keeps its printed Secondary Movement.");
                Assert.That(
                    definition.ActionDefinitions.Count(action =>
                        action.Origin == ActionDefinitionOrigin.Reaction &&
                        action.Kind == ActionKind.Defense),
                    Is.EqualTo(1),
                    "Every showcase card keeps its Defense reaction.");
            }

            Assert.That(
                rows.Where(row => row.ActionId != null).Select(row => row.ActionId).Distinct().Count(),
                Is.EqualTo(rows.Count(row => row.ActionId != null)),
                "No two cards of one hand carry the same Skill.");
        }

        private static void CommitOneCardEachBy(
            MatchSession session, string redFragment, string blueFragment)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var pair in new[]
            {
                new { Side = TeamSide.Red, Fragment = redFragment },
                new { Side = TeamSide.Blue, Fragment = blueFragment }
            })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == pair.Side &&
                    candidate.Id.Value.StartsWith(pair.Fragment, StringComparison.Ordinal) &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "The committed Initiatives differ, so no tie has to be steered.");
        }

        /// <summary>
        /// Holds out the rest of the turn the scenario left standing, so the next commitment can be
        /// made. The Hazard placed in that turn stays on the board across it: p.8 removes placed tokens
        /// at the end of the *round*, which is the boundary this deliberately does not cross.
        /// </summary>
        private static void FinishTheTurn(MatchSession session)
        {
            var turnIndex = session.Projection.TurnIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                session.Projection.TurnIndex == turnIndex)
            {
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, so this loop presses the release exactly as that player does.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments),
                "A round of the configured match holds more than one turn, and this scenario needs the " +
                "second one.");
        }


        /// <summary>
        /// Milestone 8.5, closing the 7.9a review's F-CARD-01.
        ///
        /// Until now <c>ActionDefinition</c> refused an <c>EffectRoot</c> on anything but a Skill, so a
        /// printed Attack's "After the attack:" rider had nowhere to live and the whole of rulebook
        /// p.14's card-text order was unreachable. This plays one attack with text on both sides of it
        /// and reads the ordered facts back: the attacker's text at the page's step 1, the defender's
        /// at step 4, and nothing else moved.
        ///
        /// The order is asserted against the surrounding facts rather than by index, because what p.14
        /// fixes is the position relative to the attack's own steps: step 1 is "the very beginning",
        /// before the Attack Total is announced, and step 4 sits between step 3's discard and step 5's
        /// Defense total.
        /// </summary>
        [Test]
        public void APrintedAttackWhoseTextApplies_RunsItsEffectRootInsideItsAttackTimingOrder()
        {
            var session = ConfiguredMatchFixtures.CreateAttackTextSession();
            var attacker = CommitAttackTextTurn(session, "red-foretell-card", "blue-longbow-card");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.ActionDefinitionId.Value == "primary-attack-text");
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));

            var afterTargeting = AttackTextLog(session);
            Assert.That(afterTargeting, Does.Contain("Action.CardTextApplied:BeforeTheAttack:note:Applied"),
                "p.14 step 1: the attacker applies its 'Before the attack' text.");
            Assert.That(
                afterTargeting.IndexOf("Action.CardTextApplied:BeforeTheAttack:note:Applied"),
                Is.LessThan(afterTargeting.IndexOf("Action.AttackTotalCalculated")),
                "and p.13 puts it 'at the very beginning', which is before the total is announced.");

            var defense = session.Projection.PendingDefenseChoice;
            var rejoinder = defense.LegalDefenseOffers.Single(offer =>
                offer.SourceCardInstanceId.Value == "blue-rejoinder-card");
            Accept(session, DirectorCommand.ChooseDefenseCard(
                defense.ResolutionId, defense.ChoiceId, rejoinder.SourceCardInstanceId));

            var log = AttackTextLog(session);
            Assert.That(log, Does.Contain("Action.CardTextApplied:DefenseAction:note:Applied"),
                "p.14 step 4: the defender applies its own Defense action text.");
            Assert.That(
                log.IndexOf("Action.DefenseCardDiscarded"),
                Is.LessThan(log.IndexOf("Action.CardTextApplied:DefenseAction:note:Applied")),
                "after step 3's discard,");
            Assert.That(
                log.IndexOf("Action.CardTextApplied:DefenseAction:note:Applied"),
                Is.LessThan(log.IndexOf("Action.DefenseTotalCalculated")),
                "and before step 5's Defense total.");
            Assert.That(attacker.IsDefined, Is.True);
        }

        /// <summary>
        /// p.14 step 7 on the other half of the same seam: "Apply 'After the attack:' text on the
        /// Attack card, if any" - after step 6 has checked the attack and removed the defending hero if
        /// it succeeded. p.13 says the same in words, "at the very end, after the target either
        /// defends, or is defeated and removed".
        /// </summary>
        [Test]
        public void AnAfterTheAttackRider_IsAppliedAfterTheAttackWasCheckedAndTheTargetRemoved()
        {
            var session = ConfiguredMatchFixtures.CreateAttackTextSession();
            CommitAttackTextTurn(session, "red-aftermath-card", "blue-longbow-card");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.ActionDefinitionId.Value == "primary-attack-text");
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));
            var defense = session.Projection.PendingDefenseChoice;
            Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));

            var log = AttackTextLog(session);
            Assert.That(log, Does.Contain("Action.CardTextApplied:AfterTheAttack:note:Applied"));
            Assert.That(
                log.IndexOf("Action.AttackOutcomeDetermined"),
                Is.LessThan(log.IndexOf("Action.CardTextApplied:AfterTheAttack:note:Applied")),
                "Step 6 checks the attack before step 7 applies the rider,");
            Assert.That(
                log.IndexOf("Action.CardTextApplied:AfterTheAttack:note:Applied"),
                Is.LessThan(log.IndexOf("Action.AttackResolutionCompleted")),
                "and the rider is still inside the Attack, which p.14 ends at step 9.");
            Assert.That(log, Has.None.EqualTo("Action.CardTextApplied:BeforeTheAttack:note:Applied"),
                "One card states one moment; this card prints no 'Before the attack' text.");
        }

        /// <summary>
        /// p.13's other half of the same declaration: "You may attack enemy heroes and minions", which
        /// a card may narrow in its own words. Wasp's Crackling Dagger prints "Target a hero adjacent to
        /// you", and this is that shape - the same Attack, the same adjacency, one fewer legal target.
        /// </summary>
        [Test]
        public void APrintedAttackNarrowedToHeroes_OffersNoAdjacentMinion()
        {
            var session = ConfiguredMatchFixtures.CreateAttackTextSessionWithAnAdjacentMinion(
                out var adjacentMinion);
            CommitAttackTextTurn(session, "red-aftermath-card", "blue-longbow-card");

            var narrowed = session.Projection.AvailableTurnActions.Single(offer =>
                offer.ActionDefinitionId.Value == "primary-attack-text");
            Accept(session, DirectorCommand.StartAction(narrowed.ActionDefinitionId));
            var choice = session.Projection.PendingAttackTargetChoice;

            Assert.That(choice.LegalTargetMinionIds, Has.None.EqualTo(adjacentMinion),
                "The card narrows p.13's default to heroes, so the minion beside the attacker is not " +
                "one of its targets.");
            Assert.That(choice.LegalTargetHeroIds, Is.Not.Empty,
                "and the hero beside it still is, or the narrowing has nothing to be measured against.");

            // The same position, asked of a card that narrows nothing: the minion is a target there,
            // which is what makes the absence above the card's own doing rather than the board's.
            var control = ConfiguredMatchFixtures.CreateAttackTextSessionWithAnAdjacentMinion(out var same);
            CommitAttackTextTurn(control, "red-pursuit-card", "blue-longbow-card");
            var ordinary = control.Projection.AvailableTurnActions.Single(offer =>
                DefinitionOf(control, offer).ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(control, DirectorCommand.StartAction(ordinary.ActionDefinitionId));

            Assert.That(control.Projection.PendingAttackTargetChoice.LegalTargetMinionIds,
                Has.Member(same));
        }

        /// <summary>
        /// Commits the named Red and Blue cards and settles any Initiative tie so the Red card acts,
        /// which is what puts the card-text Attack in front of the fixture.
        /// </summary>
        private static HeroId CommitAttackTextTurn(
            MatchSession session, string redCardId, string blueCardId)
        {
            var red = session.Projection.Cards.Single(card =>
                card.Id.Value == redCardId && card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards.Single(card =>
                card.Id.Value == blueCardId && card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide, red.Id));
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The Red card must be the one acting, or this fixture is watching the wrong turn.");
            return red.OwnerId;
        }

        /// <summary>
        /// The ordered rule-event log as this fixture reads it: the event type, plus the step id for a
        /// card-text fact, which is where the p.14 moment and the node are recorded.
        /// </summary>
        private static List<string> AttackTextLog(MatchSession session) =>
            session.Projection.DiagnosticEvents
                .Select(entry => entry.EventType == "Action.CardTextApplied"
                    ? entry.EventType + ":" + StepOf(entry)
                    : entry.EventType)
                .ToList();

        private static string StepOf(GuardsDigital.Core.Diagnostics.RuleEventLogEntry entry)
        {
            var marker = "step=";
            var start = entry.Detail.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
            {
                return entry.Detail;
            }

            var value = entry.Detail.Substring(start + marker.Length);
            var end = value.IndexOf(';');
            return end < 0 ? value : value.Substring(0, end);
        }
        private static void StartSkill(MatchSession session, ActionDefinitionId skill)
        {
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(skill));
            Assert.That(offer.IsAvailable, Is.True,
                "The card whose turn this is must be the one the scenario committed for it.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        private static void ChooseBranch(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(branch)));
        }

        private static void SelectSpace(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, choice.SelectInvocations.Single(
                candidate => candidate.Candidate.Value.Coordinate.Equals(coordinate)));
        }

        private static CardDefinition DefinitionOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(offer.SourceDefinitionReference));

        private static void Accept(MatchSession session, DirectorCommand command) =>
            ConfiguredMatchFixtures.Accept(session, command);
    }
}
