using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Participants
{
    public sealed class PlaceholderOneVsOneTestConfiguration
    {
        private PlaceholderOneVsOneTestConfiguration(
            MatchParticipants participants,
            PlaceholderMovementHand redHand,
            PlaceholderMovementHand blueHand,
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

            var cardDefinitions = redHand.Definitions.Concat(blueHand.Definitions).ToArray();
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

        public PlaceholderMovementHand RedHand { get; }

        public PlaceholderMovementHand BlueHand { get; }

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
        /// The ordinary five cards per hero, and what the configured scene deals.
        ///
        /// It had a named sibling of its own between milestones 6.2d and the five-card scene, because
        /// the scene was dealt a sixth card - milestone 6.2's Cache - so a player could put a token on
        /// the board at all. That sibling is gone rather than renamed: Blue Feint absorbed the Cache
        /// card's three modes, so the scene's hand and the hand every pinned digest, corpus fixture and
        /// card-matrix assertion measures are the same five again, and there is nothing left for a
        /// second factory to say.
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
                PlaceholderMovementHand.CreateRed,
                PlaceholderMovementHand.CreateBlue);
        }

        public static PlaceholderOneVsOneTestConfiguration CreateForInternalTest(
            TeamSide initialTieBreakerSide)
        {
            return Create(
                TieBreakerSetupAuthority.CreateInternalTestSetupState(initialTieBreakerSide),
                PlaceholderMovementHand.CreateRed,
                PlaceholderMovementHand.CreateBlue);
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
            PlaceholderMovementHand BuildHand(HeroId ownerId, CardDefinitionId ultimateId)
            {
                var ownedInstances = instanceArray.Where(instance => instance.OwnerId.Equals(ownerId)).ToArray();
                var ownedReferences = new HashSet<CardDefinitionReference>(
                    ownedInstances.Select(instance => instance.DefinitionReference));
                var upgradeTargets = UpgradeTargetsOf(ownerId);

                // Milestone 7.5. The dealt/deck split is a setup fact rather than a live one - every
                // card moves during a match - so it is recovered from the same content relationship
                // that routes the definitions two lines above: a card whose definition this hero's own
                // upgrade closure reaches, or its Ultimate, is one p.6 left in the deck, and every other
                // owned card is one p.6 dealt. Where each card is right now is on the card itself.
                bool IsDeckCard(CardInstance instance) =>
                    upgradeTargets.Contains(instance.DefinitionReference.DefinitionId) ||
                    instance.DefinitionReference.DefinitionId.Equals(ultimateId);

                return PlaceholderMovementHand.Restore(new PlaceholderMovementHandRestoreState(
                    definitionArray.Where(definition =>
                        ownedReferences.Contains(definition.Reference) ||
                        upgradeTargets.Contains(definition.Id) ||
                        definition.Id.Equals(ultimateId)),
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

            // Milestone 7.3. The Ultimate each hand supplies is named rather than derived, because
            // p.16 attaches it to the hero rather than to any card and so leaves no relationship for the
            // upgrade closure above to follow. Naming it here is the same thing the Create factories do
            // when they build these hands, and the sides are already re-identified from the restored
            // teams two statements above, so nothing new is being decided.
            return new PlaceholderOneVsOneTestConfiguration(
                participants,
                BuildHand(redHero, PlaceholderMovementHand.RedUltimateDefinitionId),
                BuildHand(blueHero, PlaceholderMovementHand.BlueUltimateDefinitionId),
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
                PlaceholderMovementHand.CreateRedWithSkill,
                PlaceholderMovementHand.CreateBlueWithSkill);
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
                PlaceholderMovementHand.CreateRedWithRepeatSkill,
                PlaceholderMovementHand.CreateBlueWithRepeatSkill);
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
                owner => PlaceholderMovementHand.CreateRedWithCardStateSkill(owner, blueHeroId),
                owner => PlaceholderMovementHand.CreateBlueWithCardStateSkill(owner, redHeroId));
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
                PlaceholderMovementHand.CreateRedWithRadiusSkill,
                PlaceholderMovementHand.CreateBlueWithRadiusSkill);
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
            Func<HeroId, PlaceholderMovementHand> redHand,
            Func<HeroId, PlaceholderMovementHand> blueHand)
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
            Func<HeroId, PlaceholderMovementHand> redHand,
            Func<HeroId, PlaceholderMovementHand> blueHand)
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

                    var rejections = GuardsDigital.Core.Effects.EffectDefinitionValidator.Validate(
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
