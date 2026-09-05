using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    /// <summary>
    /// Milestone 8.21. Which of rulebook p.15's stages the second Blue seat is dealt at.
    ///
    /// <para>It exists because the two faces that seat is there to prove are not in Brogan's starting
    /// hand: p.6 leaves every Tier II and Tier III card in the deck, and War Drummer and Master Skald
    /// are one of each. A fixture therefore names the stage that holds the card it is about, exactly as
    /// the staged progressed configurations do.</para>
    /// </summary>
    public enum BroganAllyStage
    {
        /// <summary>p.6's own five, which is what a match deals before any Level Up.</summary>
        StartingHand = 0,
        TierTwo = 1,
        TierThree = 2
    }

    /// <summary>
    /// A composed match: who is playing, what each seat holds, and the content that match runs on.
    ///
    /// <para><b>Milestone 8.25b named this type after what it composes.</b> It was
    /// `PlaceholderOneVsOneTestConfiguration` from milestone 2.1 until then, and by 8.25 every word of
    /// that name was false: <see cref="Create"/> composes rulebook p.5's six official seats, it is the
    /// configuration a player loads rather than a test one, and nothing about it stands in for
    /// something later. Only the name had stayed behind, which is why the rename is its own
    /// milestone and changes no behaviour.</para>
    ///
    /// <para>The staged boards keep saying they are staged: <see cref="CreateVerifiedMirrorForInternalTest"/>,
    /// the per-hero <c>CreateStaged…ForInternalTest</c> arms and the <c>CreateFor…Fixture</c> arms are
    /// named for the fixture that asks for them, and <see cref="StagedBoardParticipants"/> states the
    /// seats they deal.</para>
    /// </summary>
    public sealed class MatchConfiguration
    {
        private MatchConfiguration(
            MatchParticipants participants,
            ConfiguredHand redHand,
            ConfiguredHand blueHand,
            TieBreakerState tieBreakerState,
            SetupRandomnessSeed setupSeed,
            bool validateInitialCardState = true,
            ConfiguredHand allyHand = null)
            : this(
                participants,
                allyHand == null
                    ? new[] { redHand, blueHand }
                    : new[] { redHand, blueHand, allyHand },
                tieBreakerState,
                setupSeed,
                validateInitialCardState)
        {
        }

        /// <summary>
        /// Milestone 8.25. One hand per seat, in the order the configuration deals them.
        ///
        /// <para>Every configuration before the official six-player one dealt two seats, or three on the
        /// single board that needed a friendly hero, and named them by side. That naming stops being a
        /// key at three heroes a side: `RedHand` is not a fact about a team, it is a fact about one
        /// seat. The list is the fact now, and the three named properties below are readings of it kept
        /// so the fixtures whose subject really is a one-versus-one board keep saying what they mean.
        /// </para>
        /// </summary>
        private MatchConfiguration(
            MatchParticipants participants,
            IEnumerable<ConfiguredHand> hands,
            TieBreakerState tieBreakerState,
            SetupRandomnessSeed setupSeed,
            bool validateInitialCardState)
        {
            SetupSeed = setupSeed;

            // Milestone 8.3. The content catalogs a match composed from this configuration runs on,
            // declared once here and handed to every consumer. This is the configured match's content
            // declaration - the place that knows which capabilities and components its cards need - and
            // it is the only production type besides MatchContentCatalogs itself that may ask the build
            // what it registers. Everything downstream takes what it is given.
            ContentCatalogs = MatchContentCatalogs.ForThisBuild();
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            var handArray = hands?.ToArray() ?? throw new ArgumentNullException(nameof(hands));
            if (handArray.Length == 0 || handArray.Any(hand => hand == null))
            {
                throw new ArgumentException(
                    "A configuration requires one non-null hand per dealt seat.", nameof(hands));
            }

            Hands = Array.AsReadOnly(handArray);
            if (!tieBreakerState.IsDefined)
            {
                throw new ArgumentException(
                    "A configuration requires authoritative Tie Breaker setup state.",
                    nameof(tieBreakerState));
            }

            TieBreakerState = tieBreakerState;

            // Milestone 7.5. Every card each hand supplies, hand and deck alike: p.15's Upgrade a Card
            // step searches the deck, so a match composed from the dealt five alone would have had a
            // mandatory level nobody could complete.
            var cardInstances = handArray
                .SelectMany(hand => hand.AllInstances)
                .ToArray();
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
            var cardDefinitions = handArray
                .SelectMany(hand => hand.Definitions)
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

        /// <summary>
        /// Milestone 8.25. One hand per dealt seat, in the order the configuration dealt them - two for
        /// every staged board, six for rulebook p.5's official setup.
        /// </summary>
        public IReadOnlyList<ConfiguredHand> Hands { get; }

        /// <summary>The first Red seat's hand. On a one-hero-per-team board it is the Red seat's.</summary>
        public ConfiguredHand RedHand => HandOn(TeamSide.Red);

        /// <summary>The first Blue seat's hand. On a one-hero-per-team board it is the Blue seat's.</summary>
        public ConfiguredHand BlueHand => HandOn(TeamSide.Blue);

        /// <summary>
        /// Milestone 8.21. The second Blue seat's hand on the one staged board that deals one, and
        /// <c>null</c> everywhere else - including the official six-player setup, where three Blue
        /// seats are the configuration rather than an extra one.
        /// </summary>
        public ConfiguredHand AllyHand => Hands.FirstOrDefault(hand =>
            string.Equals(
                OwnerOf(hand).Value,
                StagedBoardParticipants.BlueAllyHeroIdValue,
                StringComparison.Ordinal));

        private static HeroId OwnerOf(ConfiguredHand hand) => hand.AllInstances[0].OwnerId;

        private ConfiguredHand HandOn(TeamSide side) => Hands.First(hand =>
            !OwnerOf(hand).Value.Equals(
                StagedBoardParticipants.BlueAllyHeroIdValue, StringComparison.Ordinal) &&
            Participants.GetTeamForHero(OwnerOf(hand)).Side == side);

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
        /// The configured match: rulebook p.5's official six-player line-up - Wasp, Xargatha and
        /// Tigerclaw on Red against Arien, Brogan and Sabina on Blue - each dealt p.6's starting five
        /// with the remaining thirteen in their own deck. This is what the scene composes and what a
        /// player plays.
        ///
        /// <para><b>Milestone 8.25 is where this stopped being one hero per team.</b> Two seats were
        /// what the build could deal: one verified printed set at 8.4, dealt twice, then two different
        /// ones at 8.20. All six of p.6 setup step 7's dashboards exist now, so the match a player
        /// loads is the match the rulebook describes. `DEV-001` and `DEV-002` retire with it.</para>
        ///
        /// <para>The staged boards did not disappear: <see cref="CreateVerifiedMirrorForInternalTest"/>,
        /// <see cref="CreateStagedArienForInternalTest"/> and the rest still deal one seat per team, and
        /// every fixture whose subject is one printed set asks for the board it needs by name. What
        /// changed is what the default is.</para>
        ///
        /// <para>Milestone 8.4 is where this stopped being synthetic. Until then it dealt the
        /// mechanics-showcase cards, whose values and text were invented to exercise machinery; it now
        /// deals <see cref="WaspCardContent"/> and its five siblings, whose every value and word is
        /// transcribed from the printed faces. The showcase cards did not disappear -
        /// <see cref="SyntheticShowcaseHand"/> still fills the fixture configurations below, which say
        /// so in their names - but nothing synthetic stands in for hero content on the path a match
        /// takes any more.</para>
        ///
        /// <para>The six seats deal <b>108 instances of 108 definitions</b> between them - six printed
        /// sets, one each, and no two seats sharing a set the way the mirror's two did. An instance id
        /// is prefixed by the seat rather than by the team side, because a side no longer identifies
        /// one seat, and each definition id already carries its own hero's content-set prefix: a card
        /// id therefore still says both which set it belongs to and which seat holds it.</para>
        /// </summary>
        public static MatchConfiguration Create() => Create(Setup.MatchFormat.Default);

        /// <summary>
        /// The official match for one of rulebook pp.5-6's four printed setups, with p.6's own coin
        /// flip. <see cref="Setup.MatchFormat.Default"/> is milestone 8.25's six-player Quick Game,
        /// so <see cref="Create()"/> composes exactly the match it always has.
        /// </summary>
        public static MatchConfiguration Create(Setup.MatchFormat format)
        {
            // Milestone 8.28b. The draw happens once, here, and is recorded before anything derives
            // from it, so the coin flip below can be carried out again from the record and land on the
            // same side. It is still a real flip: the seed came from platform entropy a moment ago.
            var setupSeed = SetupRandomnessAuthority.DrawSeed();

            return CreateOfficial(
                format, TieBreakerSetupAuthority.CreateNormalSetupState(setupSeed), setupSeed);
        }

        /// <summary>
        /// Milestone 8.25. The official six-player match with p.6's coin flip replaced by an explicitly
        /// selected side, so a fixture can play it without a draw deciding who acts first. `DEV-005`,
        /// exactly as the staged internal-test configurations are.
        /// </summary>
        public static MatchConfiguration CreateOfficialSixPlayerForInternalTest(
            TeamSide initialTieBreakerSide) =>
            CreateOfficialForInternalTest(Setup.MatchFormat.ThreeVersusThreeQuick, initialTieBreakerSide);

        /// <summary>
        /// The official match for any printed format with p.6's coin flip replaced by an explicitly
        /// selected side, so a fixture can play it without a draw deciding who acts first. `DEV-005`.
        /// </summary>
        public static MatchConfiguration CreateOfficialForInternalTest(
            Setup.MatchFormat format, TeamSide initialTieBreakerSide) =>
            CreateOfficial(
                format,
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SetupRandomnessSeed.Unrecorded);

        /// <summary>
        /// The six official seats and the printed set each one holds, in p.6 setup step 7's order.
        ///
        /// <para>The hand a seat is dealt follows the hero that seat holds, which
        /// <see cref="OfficialSixPlayerParticipants"/> states; nothing here decides it a second time.
        /// The instance-id prefix is the seat rather than its team side, because three seats a side
        /// makes a side ambiguous - <c>red-01-wasp-magnetic-dagger</c> names the seat, the printed set
        /// and the card in that order.</para>
        /// </summary>
        private static MatchConfiguration CreateOfficial(
            Setup.MatchFormat format, TieBreakerState tieBreakerState, SetupRandomnessSeed setupSeed)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));

            var participants = format.PlayerCount == 4
                ? OfficialFourPlayerParticipants.Create()
                : OfficialSixPlayerParticipants.Create();
            var seats = format.PlayerCount == 4
                ? OfficialFourPlayerParticipants.Seats
                : OfficialSixPlayerParticipants.Seats;

            // The seat's instance-id prefix, which milestone 8.25 made the seat rather than the team
            // side: three seats a side made a side ambiguous. It is derived from the seat's position
            // within its own team so that a 2v2 and a 3v3 name the same chair the same way -
            // `red-01` is Wasp's seat in both, and only the absent third row differs.
            var redOrdinal = 0;
            var blueOrdinal = 0;
            var hands = new List<ConfiguredHand>();
            foreach (var seat in seats)
            {
                var prefix = seat.Side == TeamSide.Red
                    ? "red-0" + (++redOrdinal).ToString(CultureInfo.InvariantCulture)
                    : "blue-0" + (++blueOrdinal).ToString(CultureInfo.InvariantCulture);
                hands.Add(CreateVerifiedHand(seat.HeroId, prefix));
            }

            return new MatchConfiguration(
                participants, hands.ToArray(), tieBreakerState, setupSeed, true);
        }

        /// <summary>
        /// The verified printed set a hero deals, by hero id. One place names the six, so a
        /// configuration that deals four of them cannot deal a different Wasp than one dealing six.
        /// </summary>
        private static ConfiguredHand CreateVerifiedHand(string heroIdValue, string instancePrefix)
        {
            var heroId = new HeroId(heroIdValue);
            switch (heroIdValue)
            {
                case OfficialSixPlayerParticipants.WaspHeroIdValue:
                    return WaspCardContent.CreateHand(heroId, instancePrefix);
                case OfficialSixPlayerParticipants.XargathaHeroIdValue:
                    return Cards.Verified.XargathaCardContent.CreateHand(heroId, instancePrefix);
                case OfficialSixPlayerParticipants.TigerclawHeroIdValue:
                    return Cards.Verified.TigerclawCardContent.CreateHand(heroId, instancePrefix);
                case OfficialSixPlayerParticipants.ArienHeroIdValue:
                    return Cards.Verified.ArienCardContent.CreateHand(heroId, instancePrefix);
                case OfficialSixPlayerParticipants.BroganHeroIdValue:
                    return Cards.Verified.BroganCardContent.CreateHand(heroId, instancePrefix);
                case OfficialSixPlayerParticipants.SabinaHeroIdValue:
                    return Cards.Verified.SabinaCardContent.CreateHand(heroId, instancePrefix);
                default:
                    throw new ArgumentException(
                        "No verified printed set is registered for hero '" + heroIdValue + "'.",
                        nameof(heroIdValue));
            }
        }

        /// <summary>
        /// Milestone 8.20. The staged Wasp-versus-Arien match with p.6's coin flip replaced by an
        /// explicitly selected side, so a fixture can play it without a draw deciding who acts first.
        /// `DEV-005`, exactly as <see cref="CreateVerifiedMirrorForInternalTest"/> is for the mirror.
        /// </summary>
        public static MatchConfiguration CreateStagedArienForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedArienHand(TeamSide.Blue));
        }

        /// <summary>
        /// Milestone 8.21. The staged Wasp-versus-Brogan match, on the same `DEV-005` terms the staged
        /// Arien match is on.
        ///
        /// <para>Brogan takes the Blue seat rather than joining it, and that is deliberate: milestone
        /// 8.25 owns the official six-player configuration and every hero spawn it assigns, so until
        /// then this build stages one hero per team and a third dealt hero is a fixture's choice of
        /// which two play rather than a wider match. <see cref="Create"/> - the scene a player loads -
        /// is untouched and still deals Wasp against Arien.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedBroganForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedBroganHand(TeamSide.Blue));
        }

        /// <summary>
        /// Milestone 8.21. Wasp on Red against Arien <em>and</em> Brogan on Blue - the one staged
        /// configuration in this build that deals two heroes to a team.
        ///
        /// <para><b>Why it exists.</b> Two of Brogan's faces name a friendly hero: War Drummer and
        /// Master Skald print "A friendly hero in range gains N coins", and rulebook p.10 keeps the
        /// acting hero out of what an action affects unless the text says so. In every other
        /// configuration this build has, Brogan is the only hero on his side, so that selection has no
        /// candidate at all and the Skill stops at its first step - which would leave two of nineteen
        /// faces unreachable through the ordinary player pipeline rather than merely untested.</para>
        ///
        /// <para><b>What it is not.</b> Not the six-player setup, which milestone 8.25 still owns
        /// along with all six official spawn assignments: Red fields one hero, the board is the same
        /// official map, and Brogan takes the second Blue hero spawn the map has always declared. Not
        /// what a player loads either - <see cref="Create"/> is untouched. `DEV-004`/`DEV-005` terms,
        /// as every staged configuration here is on.</para>
        ///
        /// <para>Arien holds the Blue seat beside him rather than a second Wasp, because he is the
        /// hero this build already deals there and a fixture that invented a third printed set would
        /// be proving something no match has.</para>
        /// </summary>
        /// <param name="stage">
        /// Which of rulebook p.15's stages Brogan's seat is dealt at. It is a parameter because the two
        /// cards this configuration exists for are not in his starting hand: War Drummer is a Tier II
        /// card and Master Skald a Tier III one, so p.6 leaves both in the deck and only a progressed
        /// hand holds either. The branch is the second sibling of each colour, which is the half of
        /// p.15's choice those two cards are on.
        /// </param>
        public static MatchConfiguration CreateStagedBroganWithAllyForInternalTest(
            TeamSide initialTieBreakerSide,
            BroganAllyStage stage = BroganAllyStage.StartingHand)
        {
            var broganId = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            return new MatchConfiguration(
                StagedBoardParticipants.CreateWithBlueAlly(),
                CreateVerifiedWaspHand(TeamSide.Red)(
                    new HeroId(StagedBoardParticipants.RedHeroIdValue)),
                CreateVerifiedArienHand(TeamSide.Blue)(
                    new HeroId(StagedBoardParticipants.BlueHeroIdValue)),
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SetupRandomnessSeed.Unrecorded,
                allyHand: CreateAllyBroganHand(broganId, stage));
        }

        private static ConfiguredHand CreateAllyBroganHand(HeroId ownerId, BroganAllyStage stage)
        {
            switch (stage)
            {
                case BroganAllyStage.TierTwo:
                    return GuardsDigital.Core.Cards.Verified.BroganProgressedHands.CreateTierTwoHand(
                        ownerId, "blue-ally",
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.SecondSibling);
                case BroganAllyStage.TierThree:
                    return GuardsDigital.Core.Cards.Verified.BroganProgressedHands.CreateTierThreeHand(
                        ownerId, "blue-ally",
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.SecondSibling);
                default:
                    return GuardsDigital.Core.Cards.Verified.BroganCardContent.CreateHand(
                        ownerId, "blue-ally");
            }
        }

        /// <summary>
        /// Milestone 8.21. The staged Wasp-versus-Brogan match at rulebook p.15's Tier II stage, with
        /// Brogan holding one Tier II card of each colour on the branch the caller names - the same
        /// pairing rule <see cref="CreateStagedArienTierTwoForInternalTest"/> records.
        /// </summary>
        public static MatchConfiguration CreateStagedBroganTierTwoForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.BroganUpgradeBranch branch)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: false),
                CreateProgressedBroganHand(tierThree: false, branch));
        }

        /// <summary>
        /// Milestone 8.21. The staged Wasp-versus-Brogan match at rulebook p.15's Tier III stage with
        /// p.16's Ultimate unlocked on both seats: High Voltage faceup in Red's Ultimate area and One
        /// Man Army in Brogan's.
        /// </summary>
        public static MatchConfiguration CreateStagedBroganTierThreeForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.BroganUpgradeBranch branch)
        {
            var configuration = Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: true),
                CreateProgressedBroganHand(tierThree: true, branch));

            foreach (var ultimate in configuration.CardInstances.Where(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.WaspCardContent.HighVoltage) ||
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.BroganCardContent.OneManArmy)))
            {
                if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
                {
                    throw new InvalidOperationException(
                        "The staged Tier III match could not unlock a seat's dealt Ultimate.");
                }
            }

            return configuration;
        }

        /// <summary>
        /// Milestone 8.23. The staged Wasp-versus-Tigerclaw match, on the same `DEV-005` terms the
        /// staged Arien, Brogan and Sabina matches are on.
        ///
        /// <para>Tigerclaw takes the Blue seat though p.5 puts him on Red, for the reason
        /// <see cref="CreateStagedBroganForInternalTest"/> records: milestone 8.25 owns the official
        /// six-player configuration and every hero spawn it assigns. <see cref="Create"/> - the scene a
        /// player loads - is untouched and still deals Wasp against Arien.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedTigerclawForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedTigerclawHand(TeamSide.Blue));
        }

        /// <summary>
        /// Milestone 8.23. The staged Wasp-versus-Tigerclaw match at rulebook p.15's Tier II stage,
        /// with Tigerclaw holding one Tier II card of each colour on the branch the caller names - the
        /// same pairing rule <see cref="CreateStagedArienTierTwoForInternalTest"/> records.
        ///
        /// <para>The branch matters more here than for any earlier hero: his Blue pair is one Ranged
        /// Block against one non-Ranged Block, so which sibling a staged hand took decides which
        /// attacks it can stop at all.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedTigerclawTierTwoForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.TigerclawUpgradeBranch branch)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: false),
                CreateProgressedTigerclawHand(tierThree: false, branch));
        }

        /// <summary>
        /// Milestone 8.23. The staged Wasp-versus-Tigerclaw match at rulebook p.15's Tier III stage
        /// with p.16's Ultimate unlocked on both seats: High Voltage faceup in Red's Ultimate area and
        /// Cloak and Daggers in Tigerclaw's.
        /// </summary>
        public static MatchConfiguration CreateStagedTigerclawTierThreeForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.TigerclawUpgradeBranch branch)
        {
            var configuration = Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: true),
                CreateProgressedTigerclawHand(tierThree: true, branch));

            foreach (var ultimate in configuration.CardInstances.Where(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.WaspCardContent.HighVoltage) ||
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.TigerclawCardContent.CloakAndDaggers)))
            {
                if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
                {
                    throw new InvalidOperationException(
                        "The staged Tier III match could not unlock a seat's dealt Ultimate.");
                }
            }

            return configuration;
        }

        /// <summary>
        /// Milestone 8.24. The staged Wasp-versus-Xargatha match, on the same `DEV-005` terms the
        /// staged Arien, Brogan, Sabina and Tigerclaw matches are on.
        ///
        /// <para>Xargatha takes the Blue seat though p.5 puts her on Red, for the reason
        /// <see cref="CreateStagedBroganForInternalTest"/> records: milestone 8.25 owns the official
        /// six-player configuration and every hero spawn it assigns. <see cref="Create"/> - the scene a
        /// player loads - is untouched and still deals Wasp against Arien.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedXargathaForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedXargathaHand(TeamSide.Blue));
        }

        /// <summary>
        /// Milestone 8.24. The staged Wasp-versus-Xargatha match at rulebook p.15's Tier II stage, with
        /// Xargatha holding one Tier II card of each colour on the branch the caller names - the same
        /// pairing rule <see cref="CreateStagedArienTierTwoForInternalTest"/> records.
        ///
        /// <para>Her branches are the widest apart in the roster: the first takes the petrification
        /// line and a second Movement primary, the second takes a Ranged Attack, a retrieval and an
        /// end-of-round Defeat. Which sibling a staged hand took therefore decides which of her
        /// mechanics exist in that match at all.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedXargathaTierTwoForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.XargathaUpgradeBranch branch)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: false),
                CreateProgressedXargathaHand(tierThree: false, branch));
        }

        /// <summary>
        /// Milestone 8.24. The staged Wasp-versus-Xargatha match at rulebook p.15's Tier III stage with
        /// p.16's Ultimate unlocked on both seats: High Voltage faceup in Red's Ultimate area and
        /// Metamorphosis in Xargatha's.
        ///
        /// <para>Hers is the first unlocked Ultimate in this build that asks nothing and waits for
        /// nothing: it is simply true from the moment it is faceup, so a Tier III Xargatha's Movement
        /// and Initiative are already scaling with whoever is standing beside her before any card is
        /// played.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedXargathaTierThreeForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.XargathaUpgradeBranch branch)
        {
            var configuration = Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: true),
                CreateProgressedXargathaHand(tierThree: true, branch));

            foreach (var ultimate in configuration.CardInstances.Where(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.WaspCardContent.HighVoltage) ||
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.XargathaCardContent.Metamorphosis)))
            {
                if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
                {
                    throw new InvalidOperationException(
                        "The staged Tier III match could not unlock a seat's dealt Ultimate.");
                }
            }

            return configuration;
        }

        /// <summary>
        /// Milestone 8.22. The staged Wasp-versus-Sabina match, on the same `DEV-005` terms the staged
        /// Arien and Brogan matches are on.
        ///
        /// <para>Sabina takes the Blue seat rather than joining it, for the reason
        /// <see cref="CreateStagedBroganForInternalTest"/> records: milestone 8.25 owns the official
        /// six-player configuration and every hero spawn it assigns. <see cref="Create"/> - the scene a
        /// player loads - is untouched and still deals Wasp against Arien.</para>
        ///
        /// <para><b>She needs no ally seat, and that is worth saying because Brogan did.</b> Two of his
        /// faces name a friendly <em>hero</em> and a one-hero team gave that selection no candidate at
        /// all. Sabina's kit commands minions instead - "a friendly minion in radius", "two minions in
        /// radius", "your friendly minion" - and p.8's waves put minions on the board in every
        /// configuration this build has, so every one of her faces has something to act on without a
        /// second Blue hero.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedSabinaForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateVerifiedWaspHand(TeamSide.Red),
                CreateVerifiedSabinaHand(TeamSide.Blue));
        }

        /// <summary>
        /// Milestone 8.22. The staged Wasp-versus-Sabina match at rulebook p.15's Tier II stage, with
        /// Sabina holding one Tier II card of each colour on the branch the caller names - the same
        /// pairing rule <see cref="CreateStagedArienTierTwoForInternalTest"/> records.
        /// </summary>
        public static MatchConfiguration CreateStagedSabinaTierTwoForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.SabinaUpgradeBranch branch)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: false),
                CreateProgressedSabinaHand(tierThree: false, branch));
        }

        /// <summary>
        /// Milestone 8.22. The staged Wasp-versus-Sabina match at rulebook p.15's Tier III stage with
        /// p.16's Ultimate unlocked on both seats: High Voltage faceup in Red's Ultimate area and Big
        /// Sodding Gun in Sabina's.
        /// </summary>
        public static MatchConfiguration CreateStagedSabinaTierThreeForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.SabinaUpgradeBranch branch)
        {
            var configuration = Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: true),
                CreateProgressedSabinaHand(tierThree: true, branch));

            foreach (var ultimate in configuration.CardInstances.Where(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.WaspCardContent.HighVoltage) ||
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.SabinaCardContent.BigSoddingGun)))
            {
                if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
                {
                    throw new InvalidOperationException(
                        "The staged Tier III match could not unlock a seat's dealt Ultimate.");
                }
            }

            return configuration;
        }

        /// <summary>
        /// Milestone 8.20. The staged match at rulebook p.15's Tier II stage: Wasp holds her Red seat's
        /// three Tier II cards and Arien holds one Tier II card of each colour, on the branch the
        /// caller names.
        ///
        /// <para>The branch is a parameter because a single seat can only hold three of a Tier's six
        /// cards - p.15 offers a pair and the player takes one - so reaching Arien's whole printed set
        /// takes two stagings where the mirror reached Wasp's in one. `DEV-004` terms throughout, for
        /// the reason <see cref="Verified.ArienProgressedHands"/> records.</para>
        /// </summary>
        public static MatchConfiguration CreateStagedArienTierTwoForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.ArienUpgradeBranch branch)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: false),
                CreateProgressedArienHand(tierThree: false, branch));
        }

        /// <summary>
        /// Milestone 8.20. The staged match at rulebook p.15's Tier III stage with p.16's Ultimate
        /// unlocked on both seats: High Voltage faceup in Red's Ultimate area and Living Tsunami in
        /// Arien's.
        /// </summary>
        public static MatchConfiguration CreateStagedArienTierThreeForInternalTest(
            TeamSide initialTieBreakerSide, Cards.Verified.ArienUpgradeBranch branch)
        {
            var configuration = Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                CreateProgressedWaspHand(TeamSide.Red, tierThree: true),
                CreateProgressedArienHand(tierThree: true, branch));

            // Both hands deal their Ultimate to the deck, exactly as the initial-setup validator
            // requires, and each is unlocked here through the one p.16 transition allowed to put a
            // card in the Ultimate area.
            foreach (var ultimate in configuration.CardInstances.Where(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.WaspCardContent.HighVoltage) ||
                card.DefinitionReference.DefinitionId.Equals(
                    GuardsDigital.Core.Cards.Verified.ArienCardContent.LivingTsunami)))
            {
                if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate))
                {
                    throw new InvalidOperationException(
                        "The staged Tier III match could not unlock a seat's dealt Ultimate.");
                }
            }

            return configuration;
        }

        /// <summary>
        /// The configured Wasp match with p.5's coin flip replaced by an explicitly selected side, so
        /// a fixture can play it without a draw deciding who acts first. `DEV-005`, exactly as
        /// <see cref="CreateForInternalTest"/> uses it for the showcase hands.
        /// </summary>
        public static MatchConfiguration CreateVerifiedMirrorForInternalTest(
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
        public static MatchConfiguration CreateVerifiedTierTwoMirrorForInternalTest(
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
        public static MatchConfiguration CreateVerifiedTierThreeMirrorForInternalTest(
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
        public static MatchConfiguration CreateForShowcaseFixture()
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
        public static MatchConfiguration CreateForVerifiedDefenseFixture(
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
        /// Milestone 8.20. One seat's verified Arien hand, prefixed by the seat's team side for the
        /// reason Wasp's is. The two prefixes cannot collide even on a board that dealt both sets to
        /// one side, because each definition id already carries its own hero's content-set name.
        /// </summary>
        private static Func<HeroId, ConfiguredHand> CreateVerifiedArienHand(TeamSide side) =>
            ownerId => GuardsDigital.Core.Cards.Verified.ArienCardContent.CreateHand(
                ownerId, side == TeamSide.Red ? "red" : "blue");

        private static Func<HeroId, ConfiguredHand> CreateProgressedArienHand(
            bool tierThree, Cards.Verified.ArienUpgradeBranch branch) =>
            ownerId => tierThree
                ? GuardsDigital.Core.Cards.Verified.ArienProgressedHands.CreateTierThreeHand(
                    ownerId, "blue", branch)
                : GuardsDigital.Core.Cards.Verified.ArienProgressedHands.CreateTierTwoHand(
                    ownerId, "blue", branch);

        /// <summary>
        /// Milestone 8.21. One seat's verified Brogan hand, prefixed by the seat's team side for the
        /// reason the two earlier sets are.
        /// </summary>
        private static Func<HeroId, ConfiguredHand> CreateVerifiedBroganHand(TeamSide side) =>
            ownerId => GuardsDigital.Core.Cards.Verified.BroganCardContent.CreateHand(
                ownerId, side == TeamSide.Red ? "red" : "blue");

        private static Func<HeroId, ConfiguredHand> CreateProgressedBroganHand(
            bool tierThree, Cards.Verified.BroganUpgradeBranch branch) =>
            ownerId => tierThree
                ? GuardsDigital.Core.Cards.Verified.BroganProgressedHands.CreateTierThreeHand(
                    ownerId, "blue", branch)
                : GuardsDigital.Core.Cards.Verified.BroganProgressedHands.CreateTierTwoHand(
                    ownerId, "blue", branch);

        /// <summary>
        /// Milestone 8.22. One seat's verified Sabina hand, prefixed by the seat's team side for the
        /// reason the three earlier sets are.
        /// </summary>
        private static Func<HeroId, ConfiguredHand> CreateVerifiedTigerclawHand(TeamSide side) =>
            ownerId => GuardsDigital.Core.Cards.Verified.TigerclawCardContent.CreateHand(
                ownerId, side == TeamSide.Red ? "red" : "blue");

        private static Func<HeroId, ConfiguredHand> CreateProgressedTigerclawHand(
            bool tierThree, Cards.Verified.TigerclawUpgradeBranch branch) =>
            ownerId => tierThree
                ? GuardsDigital.Core.Cards.Verified.TigerclawProgressedHands.CreateTierThreeHand(
                    ownerId, "blue", branch)
                : GuardsDigital.Core.Cards.Verified.TigerclawProgressedHands.CreateTierTwoHand(
                    ownerId, "blue", branch);

        /// <summary>
        /// Milestone 8.24. One seat's verified Xargatha hand, prefixed by the seat's team side for the
        /// reason the four earlier sets are.
        /// </summary>
        private static Func<HeroId, ConfiguredHand> CreateVerifiedXargathaHand(TeamSide side) =>
            ownerId => GuardsDigital.Core.Cards.Verified.XargathaCardContent.CreateHand(
                ownerId, side == TeamSide.Red ? "red" : "blue");

        private static Func<HeroId, ConfiguredHand> CreateProgressedXargathaHand(
            bool tierThree, Cards.Verified.XargathaUpgradeBranch branch) =>
            ownerId => tierThree
                ? GuardsDigital.Core.Cards.Verified.XargathaProgressedHands.CreateTierThreeHand(
                    ownerId, "blue", branch)
                : GuardsDigital.Core.Cards.Verified.XargathaProgressedHands.CreateTierTwoHand(
                    ownerId, "blue", branch);

        private static Func<HeroId, ConfiguredHand> CreateVerifiedSabinaHand(TeamSide side) =>
            ownerId => GuardsDigital.Core.Cards.Verified.SabinaCardContent.CreateHand(
                ownerId, side == TeamSide.Red ? "red" : "blue");

        private static Func<HeroId, ConfiguredHand> CreateProgressedSabinaHand(
            bool tierThree, Cards.Verified.SabinaUpgradeBranch branch) =>
            ownerId => tierThree
                ? GuardsDigital.Core.Cards.Verified.SabinaProgressedHands.CreateTierThreeHand(
                    ownerId, "blue", branch)
                : GuardsDigital.Core.Cards.Verified.SabinaProgressedHands.CreateTierTwoHand(
                    ownerId, "blue", branch);

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
        public static MatchConfiguration CreateForAttackTextFixture(
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
        public static MatchConfiguration CreateForContinuationFixture(
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
        public static MatchConfiguration CreateForMinionBattleRuleFixture(
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
        public static MatchConfiguration CreateForPausingSourceFixture(
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
        public static MatchConfiguration CreateForAfterPushFixture(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRedWithAfterPushUltimate,
                SyntheticShowcaseHand.CreateBlue);
        }

        public static MatchConfiguration CreateForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                SyntheticShowcaseHand.CreateRed,
                SyntheticShowcaseHand.CreateBlue);
        }

        public MatchConfigurationRestoreState CaptureRestoreState() =>
            new MatchConfigurationRestoreState(
                Participants,
                CardDefinitions,
                CardInstances,
                TieBreakerState,
                SetupSeed);

        public static MatchConfiguration Restore(
            MatchConfigurationRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var participants = state.Participants;
            var definitionArray = state.Definitions;
            var instanceArray = state.Instances;

            // Milestone 8.25. A restore rebuilds one hand per configured hero, in the participant
            // graph's own order, and asks nothing about sides at all.
            //
            // Milestone 8.21 had already stopped identifying a side by its one hero - the staged board
            // that deals a friendly hero named the extra seat rather than ordering it - and the official
            // six-player setup finishes the move: with three seats a side there is no "the Red hero" to
            // name. Which hand belongs to which hero was never a fact about a side; it is card ownership,
            // which every restored instance already carries.
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

            // The hands are rebuilt from the flat definition and instance lists a snapshot restores,
            // using only relationships the content itself states; every seat is rebuilt the same way and
            // from the same lists, so a sixth is another owner rather than another rule.
            return new MatchConfiguration(
                participants,
                participants.Heroes.Select(hero => BuildHand(hero.Id)).ToArray(),
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
        public static MatchConfiguration CreateForSkillFixture(
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
        public static MatchConfiguration CreateForRepeatFixture(
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
        public static MatchConfiguration CreateForCardStateFixture(
            TeamSide initialTieBreakerSide)
        {
            var redHeroId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

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
        public static MatchConfiguration CreateForRadiusFixture(
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
        public static MatchConfiguration CreateForOrbitFixture(
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
        public static MatchConfiguration CreateForKinesisFixture(
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
        private static MatchConfiguration Create(
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
        private static MatchConfiguration Create(
            TieBreakerState tieBreakerState,
            SetupRandomnessSeed setupSeed,
            Func<HeroId, ConfiguredHand> redHand,
            Func<HeroId, ConfiguredHand> blueHand)
        {
            var participants = StagedBoardParticipants.Create();
            var redHeroId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

            return new MatchConfiguration(
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
