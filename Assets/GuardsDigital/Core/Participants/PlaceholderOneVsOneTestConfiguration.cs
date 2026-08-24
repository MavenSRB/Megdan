using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    public sealed class PlaceholderOneVsOneTestConfiguration
    {
        private PlaceholderOneVsOneTestConfiguration(
            MatchParticipants participants,
            ConfiguredHand redHand,
            ConfiguredHand blueHand,
            TieBreakerState tieBreakerState,
            SetupRandomnessSeed setupSeed,
            bool validateInitialCardState = true)
        {
            SetupSeed = setupSeed;

            // Milestone 8.3. The content catalogs a match composed from this configuration runs on,
            // declared once here and handed to every consumer. This is the configured match's content
            // declaration - the place that knows which capabilities and components its cards need - and
            // it is the only production type besides MatchContentCatalogs itself that may ask the build
            // what it registers. Everything downstream takes what it is given.
            ContentCatalogs = MatchContentCatalogs.ForThisBuild();
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            RedHand = redHand ?? throw new ArgumentNullException(nameof(redHand));
            BlueHand = blueHand ?? throw new ArgumentNullException(nameof(blueHand));
            if (!tieBreakerState.IsDefined)
            {
                throw new ArgumentException(
                    "The test configuration requires authoritative Tie Breaker setup state.",
                    nameof(tieBreakerState));
            }

            TieBreakerState = tieBreakerState;

            // Milestone 7.5. Every card each hand supplies, hand and deck alike: p.15's Upgrade a Card
            // step searches the deck, so a match composed from the dealt five alone would have had a
            // mandatory level nobody could complete.
            var cardInstances = redHand.AllInstances.Concat(blueHand.AllInstances).ToArray();
            foreach (var instance in cardInstances)
            {
                participants.GetHero(instance.OwnerId);
            }

            // Milestone 8.4. Two hands may supply the same definition, and in the mirrored Wasp
            // configuration they always do: a mirror deals one printed set to both seats, so the
            // eighteen definitions are one set with thirty-six instances rather than two sets of
            // eighteen. Distinct by reference rather than by id on purpose - the same id at two
            // content revisions is a real conflict and stays one, and CardInitialSetupValidator below
            // is what refuses it.
            var cardDefinitions = redHand.Definitions
                .Concat(blueHand.Definitions)
                .GroupBy(definition => definition.Reference)
                .Select(group => group.First())
                .ToArray();
            if (validateInitialCardState)
            {
                CardInitialSetupValidator.Validate(cardDefinitions, cardInstances);

                // Milestone 7.2a. The cards and the heroes are both well formed by here; what this adds
                // is the claim they make together - that every Tier II or Tier III card a hero's own
                // upgrade tree leads to prints an icon one of that hero's four p.16 slots accepts. It is
                // asked here rather than inside the card validator because it is the one place a
                // configuration knows both its heroes and their cards.
                GuardsDigital.Core.Rules.Progression.HeroItemSlotSetupValidator.Validate(
                    participants, cardDefinitions, cardInstances);

                // Milestone 7.9d, finding F-CARD-04 - the 6.2b review's F62B-05, unclosed since. The
                // effect-definition validator has always been complete and has never had a production
                // caller: its only invocations were its own fixture and the configured Skill tests, so
                // the guarantees its own summary describes - children resolve, the graph is acyclic and
                // reachable, every capability is registered at the exact revision named, every input
                // matches its handler's declared type, and no node reads an output from a branch that
                // may not have run - were enforced nowhere on a path a match takes.
                //
                // That is inert while every definition is compile-time C# a fixture already validates,
                // and wrong the moment content stops being written that way. Unvalidated, each of those
                // rules fails as a mid-resolution fault instead: the card is spent, the turn is open,
                // and the engine throws where a rule should have refused. Asked here because this is
                // where a configuration first holds both its definitions and the catalog the match will
                // compose them against.
                ValidateEffectDefinitions(cardDefinitions, ContentCatalogs);
            }
            CardDefinitions = Array.AsReadOnly(cardDefinitions);
            CardInstances = Array.AsReadOnly(cardInstances);
        }

        public MatchParticipants Participants { get; }

        /// <summary>
        /// Milestone 8.3. The trusted capability and component content a match composed from this
        /// configuration runs on. Every consumer - the card-action dispatcher, the snapshot restore,
        /// the effect-definition validation below - takes this object rather than asking the build for
        /// a catalog of its own.
        /// </summary>
        public MatchContentCatalogs ContentCatalogs { get; }

        public ConfiguredHand RedHand { get; }

        public ConfiguredHand BlueHand { get; }

        public TieBreakerState TieBreakerState { get; }

        /// <summary>
        /// The recorded setup draw <see cref="TieBreakerState"/> was derived from, or
        /// <see cref="SetupRandomnessSeed.Unrecorded"/> when the side was explicitly selected under
        /// `DEV-005`. Milestone 8.28b.
        /// </summary>
        public SetupRandomnessSeed SetupSeed { get; }

        public IReadOnlyList<CardDefinition> CardDefinitions { get; }

        public IReadOnlyList<CardInstance> CardInstances { get; }

        /// <summary>
        /// The configured match: two mirrored Wasps, each dealt rulebook p.5's starting five with the
        /// remaining thirteen in their deck. This is what the scene composes and what a player plays.
        ///
        /// <para>Milestone 8.4 is where this stopped being synthetic. Until now it dealt the
        /// mechanics-showcase cards, whose values and text were invented to exercise machinery; it now
        /// deals <see cref="WaspCardContent"/>, whose every value and word is transcribed from the
        /// printed faces. The showcase cards did not disappear - <see cref="SyntheticShowcaseHand"/>
        /// still fills the fixture configurations below, which say so in their names - but nothing
        /// synthetic stands in for hero content on the path a match takes any more.</para>
        ///
        /// <para>Both seats deal the same eighteen definitions, because a mirror is one printed set
        /// dealt twice. Only the instance ids differ, and they are prefixed by the seat's team side so
        /// that a card id says which Wasp holds it.</para>
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration Create()
        {
            // Milestone 8.28b. The draw happens once, here, and is recorded before anything derives
            // from it, so the coin flip below can be carried out again from the record and land on the
            // same side. It is still a real flip: the seed came from platform entropy a moment ago.
            var setupSeed = SetupRandomnessAuthority.DrawSeed();

            return Create(
                TieBreakerSetupAuthority.CreateNormalSetupState(setupSeed),
                setupSeed,
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedWaspHand(TeamSide.Blue));
        }

        /// <summary>
        /// The configured Wasp match with p.5's coin flip replaced by an explicitly selected side, so
        /// a fixture can play it without a draw deciding who acts first. `DEV-005`, exactly as
        /// <see cref="CreateForInternalTest"/> uses it for the showcase hands.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateVerifiedMirrorForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedWaspHand(TeamSide.Blue));
        }

        /// <summary>
        /// The verified mirror at rulebook p.15's Tier II stage: both seats hold the two basics plus
        /// one Tier II card of each colour, complementary between the seats so every Tier II card is
        /// in some hand, with the returned Tier I cards and everything else in the deck. Milestone
        /// 8.15, on `DEV-004` terms - a live match cannot be seeded to this position, for the reason
        /// <see cref="Verified.WaspProgressedHands"/> records.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateVerifiedTierTwoMirrorForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: false),
                CreateProgressedWaspHand(TeamSide.Blue, tierThree: false));
        }

        /// <summary>
        /// The verified mirror at rulebook p.15's Tier III stage with p.16's Ultimate unlocked on both
        /// seats: the two basics plus one Tier III card of each colour in each hand, complementary
        /// between the seats, High Voltage faceup in each seat's Ultimate area, and every lower Tier
        /// in the deck. Milestone 8.15, on the same `DEV-004` terms.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateVerifiedTierThreeMirrorForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            var configuration = Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: true),
                CreateProgressedWaspHand(TeamSide.Blue, tierThree: true));

            // The initial-setup validator rightly admits only hand and deck, so the hands deal High
            // Voltage to the deck and each seat's copy is unlocked here through the one p.16
            // transition allowed to put a card in the Ultimate area - the same order the fixture
            // behind milestone 8.14's ordinary-match case established.
            foreach (var ultimate in configuration.CardInstances.Where(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.WaspCardContent.HighVoltage)))
            {
                if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
                {
                    throw new InvalidOperationException(
                        "The Tier III mirror could not unlock a seat's dealt High Voltage.");
                }
            }

            return configuration;
        }

        private static Func<HeroId, ConfiguredHand> CreateProgressedWaspHand(
            TeamSide side, bool tierThree) =>
            ownerId => tierThree
                ? GuardsDigital.Core.Cards.Verified.WaspProgressedHands.CreateTierThreeHand(
                    ownerId, side == TeamSide.Red ? "red" : "blue", side)
                : GuardsDigital.Core.Cards.Verified.WaspProgressedHands.CreateTierTwoHand(
                    ownerId, side == TeamSide.Red ? "red" : "blue", side);

        /// <summary>
        /// The mechanics-showcase hands with p.5's coin flip, which is what
        /// <see cref="Create"/> was before milestone 8.4 dealt verified content there.
        ///
        /// Every fixture that measures the showcase card matrix, its progression content or its
        /// synthetic Skills asks for it by this name now, so that what a test is holding is legible
        /// from the call rather than from what the default happens to be this milestone.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForShowcaseFixture()
        {
            var setupSeed = SetupRandomnessAuthority.DrawSeed();

            return Create(
                TieBreakerSetupAuthority.CreateNormalSetupState(setupSeed),
                setupSeed,
                SyntheticShowcaseHand.CreateRed,
                SyntheticShowcaseHand.CreateBlue);
        }

        /// <summary>
        /// A showcase attacker facing a verified defender: the Red seat holds the mechanics-showcase
        /// hand and the Blue seat holds Wasp's.
        ///
        /// <para>Milestone 8.4. p.14's Defense reaction can only be reached by being attacked, and no
        /// Wasp hero can attack yet - this milestone's own rule greys and refuses every printed primary
        /// whose card text has no composition, which is all nineteen faces until the 8.6-8.13 slices
        /// land. So the one configuration that can prove a verified secondary Defense answers a real
        /// attack from its printed value is one where the attacker's card text is already composed and
        /// the defender's card is Wasp's. Nothing synthetic decides anything about the defence: the
        /// Defense value, the discard it spends and the card it comes off are all verified content.</para>
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForVerifiedDefenseFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRed,
                CreateVerifiedWaspHand(TeamSide.Blue));
        }

        /// <summary>
        /// One seat's verified Wasp hand. The instance-id prefix is the seat's team side rather than
        /// its hero id: both are stable for this configuration, and the side is what a reader of a log
        /// or a snapshot can place without knowing which synthetic hero id belongs to whom.
        /// </summary>
        private static Func<HeroId, ConfiguredHand> CreateVerifiedWaspHand(TeamSide side) =>
            ownerId => WaspCardContent.CreateHand(
                ownerId, side == TeamSide.Red ? "red" : "blue");

        /// <summary>
        /// The ordinary configuration plus milestone 8.5's card-text cards: two printed Attacks in the
        /// Red hand applying their text at rulebook p.14's steps 1 and 7, and one card in the Blue hand
        /// whose printed secondary Defense applies its text at step 4.
        ///
        /// Separate from every configuration before it for the reason each of those is separate, and
        /// for one more: these are the first non-Skill actions in this project to carry an
        /// <c>EffectRoot</c> at all, so a fixture that watches p.14's order must be the only thing that
        /// can change what applies inside it.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForAttackTextFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithAttackText,
                SyntheticShowcaseHand.CreateBlueWithAttackText);
        }

        /// <summary>
        /// Milestone 8.19a. The ordinary configuration plus the four roster continuation shapes: two
        /// attacker-side cards in Red's hand and the defender-side rider and around-the-Movement card
        /// in Blue's.
        ///
        /// <para>Its own configuration for the reason the orbit and kinesis fixtures have theirs: these
        /// cards reach pause altitudes no other content in the build reaches, and a fixture asserting
        /// over the ordinary hand would read past them.</para>
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForContinuationFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithContinuationText,
                SyntheticShowcaseHand.CreateBlueWithContinuationText);
        }

        /// <summary>
        /// Milestone 8.19c. The ordinary configuration with one difference: Red's Ultimate declares
        /// the isolated Minion-Battle rule instead of milestone 7.3's permanent permission.
        ///
        /// Its own configuration for the reason the continuation fixture has one: the rule changes how
        /// p.8's battle counts and removes, and a fixture asserting over the ordinary hand would be
        /// asserting about a battle nothing had entered.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForMinionBattleRuleFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithMinionBattleUltimate,
                SyntheticShowcaseHand.CreateBlue);
        }

        /// <summary>
        /// Milestone 8.19c1. Both hands carry the two-mode pausing-source Skill, so either side can
        /// create the removal-prevention source or the pre-Minion-Battle one.
        ///
        /// Its own configuration for the reason the persistent-source and Minion-Battle fixtures have
        /// theirs: these two sources stop the game at moments no other content reaches, and a fixture
        /// asserting over the ordinary hand would be asserting about a pause nothing had opened.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForPausingSourceFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithPausingSourceSkill,
                SyntheticShowcaseHand.CreateBlueWithPausingSourceSkill);
        }

        /// <summary>
        /// Milestone 8.19c1. Red's Ultimate declares the after-Push trigger and Red's hand carries the
        /// Skill that performs rulebook p.18's Push, which is the pair the trigger needs.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForAfterPushFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithAfterPushUltimate,
                SyntheticShowcaseHand.CreateBlue);
        }

        public static PlaceholderOneVsOneTestConfiguration CreateForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRed,
                SyntheticShowcaseHand.CreateBlue);
        }

        public PlaceholderConfigurationRestoreState CaptureRestoreState() =>
            new PlaceholderConfigurationRestoreState(
                Participants,
                CardDefinitions,
                CardInstances,
                TieBreakerState,
                SetupSeed);

        public static PlaceholderOneVsOneTestConfiguration Restore(
            PlaceholderConfigurationRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var participants = state.Participants;
            var definitionArray = state.Definitions;
            var instanceArray = state.Instances;
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            ConfiguredHand BuildHand(HeroId ownerId)
            {
                var ownedInstances = instanceArray.Where(instance => instance.OwnerId.Equals(ownerId)).ToArray();
                var ownedReferences = new HashSet<CardDefinitionReference>(
                    ownedInstances.Select(instance => instance.DefinitionReference));
                var upgradeTargets = UpgradeTargetsOf(ownerId);
                var definitionsById = definitionArray
                    .GroupBy(definition => definition.Id)
                    .ToDictionary(group => group.Key, group => group.First());

                // Milestone 7.5. The dealt/deck split is a setup fact rather than a live one - every
                // card moves during a match - so it is recovered from the same content relationship
                // that routes the definitions two lines above: a card whose definition this hero's own
                // upgrade closure reaches, or its Ultimate, is one p.6 left in the deck, and every other
                // owned card is one p.6 dealt. Where each card is right now is on the card itself.
                //
                // Milestone 8.4 reads the Ultimate off the definition rather than from a named id.
                // Milestone 7.3 had to name it - p.16 attaches an Ultimate to the hero rather than to
                // any card, so the upgrade closure has no edge to follow - but milestone 7.5 then dealt
                // it an instance, which routes it to its owner like every other card, leaving only this
                // classification to answer. The verified mirror is what forced the change: both seats
                // deal one printed set, so one named id no longer tells the two hands apart, while
                // `IsUltimate` is stated by the content itself and tells each seat's own instance
                // exactly what p.6 says about it.
                bool IsDeckCard(CardInstance instance) =>
                    upgradeTargets.Contains(instance.DefinitionReference.DefinitionId) ||
                    (definitionsById.TryGetValue(instance.DefinitionReference.DefinitionId, out var owned) &&
                        owned.Progression.IsUltimate);

                return ConfiguredHand.Restore(new ConfiguredHandRestoreState(
                    definitionArray.Where(definition =>
                        ownedReferences.Contains(definition.Reference) ||
                        upgradeTargets.Contains(definition.Id)),
                    ownedInstances.Where(instance => !IsDeckCard(instance)),
                    ownedInstances.Where(IsDeckCard)));
            }

            // Milestone 7.1. A hand carries the cards its cards can become, and since the upgrade
            // targets are definitions with no instance yet, instance ownership alone cannot route them
            // back to the hand that supplied them. The closure of each owned card's stated next-Tier
            // candidates is what does, and it is the same relationship the content states rather than a
            // second rule about names or ids: a restored configuration therefore holds exactly the
            // definitions the live one did, which is what makes restore-and-continue an equivalence
            // rather than a subset.
            HashSet<CardDefinitionId> UpgradeTargetsOf(HeroId ownerId)
            {
                var definitionsById = new Dictionary<CardDefinitionId, CardDefinition>();
                foreach (var definition in definitionArray)
                {
                    if (!definitionsById.ContainsKey(definition.Id))
                    {
                        definitionsById.Add(definition.Id, definition);
                    }
                }

                var reached = new HashSet<CardDefinitionId>();
                var pending = new Queue<CardDefinitionId>(instanceArray
                    .Where(instance => instance.OwnerId.Equals(ownerId))
                    .Select(instance => instance.DefinitionReference.DefinitionId));
                while (pending.Count > 0)
                {
                    if (!definitionsById.TryGetValue(pending.Dequeue(), out var definition))
                    {
                        continue;
                    }

                    foreach (var candidateId in definition.Progression.NextTierCandidateIds)
                    {
                        if (reached.Add(candidateId))
                        {
                            pending.Enqueue(candidateId);
                        }
                    }
                }

                return reached;
            }

            // The two hands are rebuilt from the flat definition and instance lists a snapshot
            // restores, using only relationships the content itself states; the sides are already
            // re-identified from the restored teams two statements above, so nothing new is decided.
            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                BuildHand(redHero),
                BuildHand(blueHero),
                state.TieBreakerState,
                state.SetupSeed,
                false);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 6.2a's Skill-bearing card in each hand.
        ///
        /// Separate from the default deliberately: the default configuration is what every pinned
        /// digest and card-matrix fixture measures, so the Skill fixture must not change it.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForSkillFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithSkill,
                SyntheticShowcaseHand.CreateBlueWithSkill);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 6.7's Repeat-bearing card in each hand.
        ///
        /// Separate for the same reason the three before it are, and for one more: a Repeat's subject is
        /// the whole enclosing action, so adding it to an existing fixture Skill would give every
        /// fixture that plays that Skill a decision it never asked about.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForRepeatFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithRepeatSkill,
                SyntheticShowcaseHand.CreateBlueWithRepeatSkill);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 7.1a's card-state Skill in each hand.
        ///
        /// Separate for the reason the three before it are, and for one more: this Skill's chooser is
        /// the other hero, so dealing it into an existing fixture hand would give every fixture that
        /// plays a Skill a decision belonging to someone who is not acting.
        ///
        /// It is the only configuration that has to tell a hand about a hero other than its owner. A
        /// card that makes another hero discard has to name that hero, a hand is built for its owner
        /// alone, and "the opposing hero" is not something a 3v3 match can be asked for, so the
        /// configuration - which is the one place both heroes are known - names them.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForCardStateFixture(
            TeamSide initialTieBreakerSide)
        {
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                owner => SyntheticShowcaseHand.CreateRedWithCardStateSkill(owner, blueHeroId),
                owner => SyntheticShowcaseHand.CreateBlueWithCardStateSkill(owner, redHeroId));
        }

        /// <summary>
        /// The ordinary configuration plus milestone 7.2b's printed-Radius Skill in each hand.
        ///
        /// Separate for the reason the four before it are, and for one more: this pair is the first
        /// content in the build to print rulebook p.9's Radius icon, so a fixture that measures the
        /// disk that icon scopes must be the only configuration that can change it.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForRadiusFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithRadiusSkill,
                SyntheticShowcaseHand.CreateBlueWithRadiusSkill);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 8.6's orbit Skill in each hand.
        ///
        /// Separate for the reason the five before it are, and for one more: this card declares the
        /// first repeat maximum above one in the build and binds its repeated instances to one target,
        /// so a fixture that watches a chain of three instances must be the only configuration that can
        /// change what that chain does.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForOrbitFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithOrbitSkill,
                SyntheticShowcaseHand.CreateBlueWithOrbitSkill);
        }

        /// <summary>
        /// The ordinary configuration plus milestone 8.6's kinesis Skill in each hand - the first
        /// content in the build to print rulebook p.9's Range icon on a Skill and the first to ask
        /// p.19's straight-line question, so it is separate for exactly the reason the Radius pair is.
        /// </summary>
        public static PlaceholderOneVsOneTestConfiguration CreateForKinesisFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithKinesisSkill,
                SyntheticShowcaseHand.CreateBlueWithKinesisSkill);
        }

        /// <summary>
        /// Each named configuration says which hand it deals rather than setting a flag another one
        /// reads. Milestone 6.2d needed a third combination and a second boolean would have made the
        /// callers here describe themselves by what they are not. The five-card scene removed that
        /// combination again, and the shape stayed, because it is what lets the three fixture hands
        /// differ from the ordinary one without any of them naming a flag.
        /// </summary>
        private static PlaceholderOneVsOneTestConfiguration Create(
            TieBreakerState tieBreakerState,
            Func<HeroId, ConfiguredHand> redHand,
            Func<HeroId, ConfiguredHand> blueHand)
        {
            return Create(tieBreakerState, SetupRandomnessSeed.Unrecorded, redHand, blueHand);
        }

        /// <summary>
        /// Milestone 8.28b. The seed is stated rather than defaulted, so a configuration whose Tie
        /// Breaker was flipped and one whose side was selected cannot be confused for each other: the
        /// first records the draw it made, the second records <see cref="SetupRandomnessSeed.Unrecorded"/>
        /// because it made none.
        /// </summary>
        private static PlaceholderOneVsOneTestConfiguration Create(
            TieBreakerState tieBreakerState,
            SetupRandomnessSeed setupSeed,
            Func<HeroId, ConfiguredHand> redHand,
            Func<HeroId, ConfiguredHand> blueHand)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                redHand(redHeroId),
                blueHand(blueHeroId),
                tieBreakerState,
                setupSeed);
        }

        /// <summary>
        /// Every effect root every configured card carries, validated against the same catalog
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.CardActionResolutionAuthority"/> composes
        /// for the match, with the typed rejection list surfaced whole so a content author fixes one
        /// load failure rather than one rejection per attempt.
        /// </summary>
        private static void ValidateEffectDefinitions(
            IEnumerable<CardDefinition> definitions,
            MatchContentCatalogs catalogs)
        {
            var catalog = catalogs.Capabilities;
            foreach (var definition in definitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot == null)
                    {
                        continue;
                    }

                    // Milestone 8.5. Text applied at one of rulebook p.14's Attack moments is validated
                    // under the stricter rule; a Skill's own effect is the action and may pause freely.
                    // Milestone 8.10 made the moment part of the question, because only step 1 still
                    // has to finish inside the command that reached it.
                    var rejections = action.CardTextTiming != CardTextTiming.None
                        ? GuardsDigital.Core.Effects.EffectDefinitionValidator.ValidateCardText(
                            action.EffectRoot, catalog, action.CardTextTiming)
                        : GuardsDigital.Core.Effects.EffectDefinitionValidator.Validate(
                            action.EffectRoot, catalog);
                    if (rejections.Count > 0)
                    {
                        throw new ArgumentException(
                            $"Card definition '{definition.Id}' action '{action.Id}' carries an effect " +
                            "definition the match would fault on mid-resolution: " +
                            string.Join("; ", rejections.Select(rejection => rejection.ToString())),
                            nameof(definitions));
                    }
                }
            }
        }
    }
}
