using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Tali's token system - the first tokens a verified hero has, and the batch's largest single
    /// piece. Milestone 8.35z32.
    ///
    /// <para>Four claims are measured here, each of which is a printed sentence or a user ruling:
    /// <b>one supply of three shared across roles</b> (RULING 44), the <b>Ice initiative aura</b> with
    /// no floor (RULING 25), the <b>totem save</b> as a mandatory removal prevention, and <b>token
    /// immunity</b> (RULING 26) - the first immunity in the game addressed to something p.19 says is
    /// not a unit.</para>
    ///
    /// <para>The board is a six-space line, which is enough for every adjacency these rules turn on and
    /// small enough that a failure names the space rather than a search.</para>
    /// </summary>
    public sealed class TaliTokenSystemTests
    {
        private static readonly MapId Map = new MapId("synthetic-tali-token-map");

        private static readonly HeroId Tali = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        // A radius-2 disc, so one space can be adjacent to Tali, an enemy hero AND a friendly minion
        // at once - which is what these rules all turn on and which a line board cannot express.
        private static readonly HexCoordinate TokenAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate TaliAt = new HexCoordinate(1, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(0, 1);
        private static readonly HexCoordinate MinionAt = new HexCoordinate(-1, 0);

        /// <summary>Distance 2 from the token space and 3 from the minion: adjacent to neither.</summary>
        private static readonly HexCoordinate FarSpace = new HexCoordinate(2, 0);

        // -------------------------------------------------------------------------------------
        // RULING 44: one physical supply of three, shared across roles.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AllThreeRolesDeclareOnePileOfThree()
        {
            foreach (var definition in TaliComponentContent.Tokens())
            {
                Assert.That(definition.SupplyCount, Is.EqualTo(3),
                    "RULING 44: \"She has 3 tokens... They are the same token.\"");
                Assert.That(definition.SupplyGroupId,
                    Is.EqualTo(TaliComponentContent.SupplyGroupId),
                    "One physical pile, named the same way by every role that draws from it.");
                Assert.That(definition.RemovalCondition,
                    Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup),
                    "p.19's baseline; neither Green face says otherwise.");
            }

            Assert.That(TaliComponentContent.Tokens().Count(), Is.EqualTo(3),
                "Ice, and one totem definition per printed Green totem face.");
        }

        [Test]
        public void PlacingAnIceTokenLeavesTwoForATotem_BecauseTheSupplyIsOnePile()
        {
            var board = CreateBoard();
            var catalog = CreateCatalog();

            Assert.That(
                catalog.CountAvailableTokens(
                    TaliComponentContent.VeneratedTotemDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(3),
                "Nothing is placed, so the whole pile is available to every role.");

            Place(board, TaliComponentContent.IceDefinition, TokenAt, "ice-1");

            Assert.That(
                catalog.CountAvailableTokens(
                    TaliComponentContent.VeneratedTotemDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(2),
                "An Ice token on the board is a token out of the ONE pile, so the totem role sees " +
                "two left - which per-definition supplies would have reported as three and put nine " +
                "tokens in a box that holds three.");
            Assert.That(
                catalog.CountAvailableTokens(
                    TaliComponentContent.IceDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(2),
                "Both roles read the same pile, so both report the same number.");
        }

        /// <summary>
        /// Milestone 9.39, ruling W10 (the user, 2026-10-03): "Place up to 3 Ice tokens in radius" is
        /// one decision about how many. One placed and the second "up to" declined asks no third -
        /// before it the third was still opened - and the Ice token already placed stands.
        /// </summary>
        [Test]
        public void PackIce_OneOfThreeThenADecline_AsksNoThird_AndTheFirstStands()
        {
            var board = TaliBoard.Create(tier: 3, branch: TaliUpgradeBranch.FirstSibling);
            board.StartPrimary(TaliCardContent.PackIce);
            Assert.That(board.Session.Projection.PendingEffectChoice?.NodeId.Value, Is.EqualTo("place-optional-0"));
            board.Confirm();
            Assert.That(board.Session.Projection.PendingEffectChoice?.NodeId.Value, Is.EqualTo("place-token-0"));
            board.SelectFirst();
            board.ConfirmIfStillOwed();
            Assert.That(board.Session.Projection.PlacedTokens, Has.Count.EqualTo(1));
            Assert.That(board.Session.Projection.PendingEffectChoice?.NodeId.Value, Is.EqualTo("place-optional-1"),
                "the second copy is the next question.");

            board.Decline();

            Assert.That(board.Session.Projection.PendingEffectChoice, Is.Null,
                "W10: the decline closes the count; place-optional-2 is never asked.");
            Assert.That(board.Session.Projection.PlacedTokens, Has.Count.EqualTo(1), "the first Ice token stands.");
        }

        [Test]
        public void TheShortSupplyStepReachesAcrossRoles()
        {
            var board = CreateBoard();
            var catalog = CreateCatalog();

            Place(board, TaliComponentContent.IceDefinition, TokenAt, "ice-1");
            Place(board, TaliComponentContent.IceDefinition, new HexCoordinate(1, 1), "ice-2");
            Place(board, TaliComponentContent.VeneratedTotemDefinition, FarSpace, "totem-1");

            Assert.That(
                catalog.CountAvailableTokens(
                    TaliComponentContent.IceDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(0),
                "Three placed out of three; a fourth placement must take one back first (p.18).");

            var reclaimable = catalog
                .ReclaimableForPlacement(
                    TaliComponentContent.IceDefinition.Reference, board.PlacedTokens)
                .ToArray();

            Assert.That(reclaimable.Length, Is.EqualTo(3),
                "RULING 44's overflow corollary: with one shared type, p.18's \"tokens of the same " +
                "type\" reaches her own placed tokens whatever role they currently hold.");
            Assert.That(
                reclaimable.Any(token => TaliComponentContent.IsATotem(token)),
                Is.True,
                "An Ice placement short of supply may pull back a standing TOTEM - the ruling names " +
                "this case in as many words.");
        }

        [Test]
        public void ACatalogRefusesASharedSupplyWhoseMembersDisagreeAboutItsSize()
        {
            var honest = TaliComponentContent.IceDefinition;
            var liar = new TokenDefinition(
                new TokenDefinitionId("tali-ice-liar"),
                1,
                "Ice",
                honest.SupplyCount + 1,
                Array.Empty<TokenPropertyDefinition>(),
                TokenRemovalCondition.EndOfRoundCleanup,
                TaliComponentContent.SupplyGroupId);

            Assert.That(
                () => new ComponentDefinitionCatalog(
                    new[] { honest, liar }, Array.Empty<MarkerDefinition>()),
                Throws.ArgumentException,
                "One pile has one size; two numbers for it is exactly the drift a derived count " +
                "exists to make impossible.");
        }

        // -------------------------------------------------------------------------------------
        // RULING 25: the Ice aura, and no floor.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AnIceTokenGivesTheEnemyHeroBesideItMinusOneInitiative()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.IceDefinition, TokenAt, "ice-1");

            var applied = InitiativeModificationsOn(board, Enemy);

            Assert.That(applied.Length, Is.EqualTo(1),
                "\"Enemy heroes have -1 Initiative for every Ice token they are adjacent to.\"");
            Assert.That(applied[0].Modification.Amount, Is.EqualTo(-1));
            Assert.That(applied[0].Modification.Channel, Is.EqualTo(PrintedValueChannel.Initiative));
        }

        [Test]
        public void TheAuraDoesNotReachTaliHerself_BecauseTheSentenceSaysEnemyHeroes()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.IceDefinition, TokenAt, "ice-1");

            Assert.That(InitiativeModificationsOn(board, Tali), Is.Empty,
                "Tali stands adjacent to the token too; the face narrows the aura to enemy heroes " +
                "and the affected-object scope is what carries that.");
        }

        [Test]
        public void AHeroAdjacentToThreeIceTokensIsAtMinusThree_WithNoFloorAndNothingCounting()
        {
            // Three Ice tokens on three of the enemy hero's own free neighbours, which is the board
            // position the printed sentence is about.
            var board = CreateBoard();
            var neighbours = EnemyAt.GetNeighbors()
                .Where(space => board.IsEmptyPlayableSpace(space))
                .Take(3)
                .ToArray();
            Assert.That(neighbours.Length, Is.EqualTo(3), "The fixture must be able to stage three.");
            for (var index = 0; index < neighbours.Length; index++)
            {
                Place(board, TaliComponentContent.IceDefinition, neighbours[index], $"ice-{index}");
            }

            var applied = InitiativeModificationsOn(board, Enemy);

            Assert.That(applied.Length, Is.EqualTo(3),
                "Each token contributes its own -1 to the hero beside it, so \"for every Ice token " +
                "they are adjacent to\" is what per-token sources already say - nothing counts, and " +
                "a counted query would have made each of the three contribute -3.");
            Assert.That(applied.Sum(entry => entry.Modification.Amount), Is.EqualTo(-3));
            Assert.That(applied.All(entry => entry.Modification.Amount == -1), Is.True,
                "RULING 25 puts no floor on the arithmetic; nothing here clamps, and the total is " +
                "free to take a Green card's printed 3 or 4 to zero and below.");
        }

        // -------------------------------------------------------------------------------------
        // The totem save, and RULING 26's immunity.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AncestralTotemSavesOnlyMeleeMinions_AndVeneratedSavesAnyFriendlyMinion()
        {
            Assert.That(
                SaveScopeOf(TaliComponentContent.AncestralTotemDefinition),
                Is.EqualTo(TokenPropertyAffectedObject.FriendlyMeleeMinionsAdjacentToTheToken),
                "Ancestral Totem prints one extra word - \"a friendly MELEE minion\".");
            Assert.That(
                SaveScopeOf(TaliComponentContent.VeneratedTotemDefinition),
                Is.EqualTo(TokenPropertyAffectedObject.FriendlyMinionsAdjacentToTheToken),
                "Venerated Totem drops it, which is a strict widening and is transcribed as one.");
        }

        [Test]
        public void OnlyVeneratedTotemIsImmune_BecauseOnlyItPrintsTheSentence()
        {
            Assert.That(
                TaliComponentContent.AncestralTotemDefinition.Properties.Any(property =>
                    property.Modification != null &&
                    property.Modification.Kind == RuleModificationKind.Immunity),
                Is.False,
                "A silence is an absence, not a gap to fill.");

            var immunity = TaliComponentContent.VeneratedTotemDefinition.Properties.Single(property =>
                property.Modification != null &&
                property.Modification.Kind == RuleModificationKind.Immunity);

            Assert.That(immunity.AffectedObject,
                Is.EqualTo(TokenPropertyAffectedObject.TheTokenItself),
                "\"Totem tokens are immune\" is addressed to the token, which is why the rules-object " +
                "vocabulary needed its first token member.");
            Assert.That(immunity.Modification.Activities, Is.EquivalentTo(new[] { RuleActivity.All }),
                "The sentence names no activity, so it is p.18's unqualified immunity - and RULING 26 " +
                "reads that as blocking player actions including p.13's Clear.");
        }

        [Test]
        public void AVeneratedTotemOnTheBoardIsAnImmuneRulesObject()
        {
            var board = CreateBoard();
            var token = Place(
                board, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-1");

            var set = Build(board);
            var immunities = set.For(RuleObject.ForToken(token.Id))
                .Where(applied => applied.Modification.Kind == RuleModificationKind.Immunity)
                .ToArray();

            Assert.That(immunities.Length, Is.EqualTo(1),
                "The token is the affected object of its own immunity, which is only expressible " +
                "because RuleObjectKind gained its token member.");
        }

        [Test]
        public void AnAncestralTotemOnTheBoardIsNotImmune()
        {
            var board = CreateBoard();
            var token = Place(
                board, TaliComponentContent.AncestralTotemDefinition, TokenAt, "totem-1");

            Assert.That(
                Build(board).For(RuleObject.ForToken(token.Id))
                    .Any(applied => applied.Modification.Kind == RuleModificationKind.Immunity),
                Is.False,
                "The Tier II face prints no immunity sentence, so an opponent's Clear still answers it.");
        }

        /// <summary>
        /// RULING 26's operative half, asked of the query that actually decides it: an opponent's
        /// p.13 Clear is <b>refused</b> against a Venerated totem and <b>allowed</b> against an
        /// Ancestral one.
        ///
        /// <para>This is the claim the whole token-immunity widening exists to make, and it is asked
        /// through <see cref="ImmunityQuery.IsImmuneTo"/> rather than by inspecting the modification
        /// set, because that query is what every player action consults. It also proves the query is
        /// safe for a target p.19 says is not a unit: the non-immune arm walks past the declared
        /// immunity into the standing-Heavy check, which guards on
        /// <c>IntrinsicKindQuery.IsMinionMiniature</c> and answers <c>false</c> for a token rather
        /// than reaching for a minion identity it does not have.</para>
        ///
        /// <para>The pair is one test on purpose: "immune" and "not immune" measured on two boards
        /// that differ only in which printed Green face placed the token is the difference the two
        /// cards are for.</para>
        /// </summary>
        [Test]
        public void AnEnemysClearIsRefusedAgainstAVeneratedTotemAndAllowedAgainstAnAncestralOne()
        {
            var participants = StagedBoardParticipants.Create();

            var immuneBoard = CreateBoard();
            var immuneTotem = Place(
                immuneBoard, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-immune");
            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForToken(immuneTotem.Id),
                    RuleActivity.Clear,
                    RuleObject.ForHero(Enemy),
                    immuneBoard,
                    participants,
                    Build(immuneBoard)),
                Is.True,
                "\"Totem tokens are immune\" names no activity, so p.18's unqualified immunity " +
                "reaches the Clear every hero has - which is what RULING 26 says it costs the " +
                "opponent, and what makes the Tier III card worth its Tier.");

            var plainBoard = CreateBoard();
            var plainTotem = Place(
                plainBoard, TaliComponentContent.AncestralTotemDefinition, TokenAt, "totem-plain");
            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForToken(plainTotem.Id),
                    RuleActivity.Clear,
                    RuleObject.ForHero(Enemy),
                    plainBoard,
                    participants,
                    Build(plainBoard)),
                Is.False,
                "The Tier II face prints no immunity sentence, so p.13's Clear still answers it - " +
                "and the query reaches this answer without stumbling over a target that is not a unit.");
        }

        /// <summary>
        /// RULING 26's other half, and the half that makes the card answerable: <b>system-level
        /// removals still work</b>. An immune totem is still taken by p.8's end-of-round cleanup.
        ///
        /// <para>This is a measurement rather than a reading. The boundary the ruling draws - player
        /// actions including p.13's Clear are refused, p.8's cleanup and its lane-push spawn clearing
        /// are not - is a fact about which code paths ask <c>ImmunityQuery</c>, and a rule that lives
        /// in the absence of a call is exactly the kind that decays silently. Asserting the immunity
        /// exists <em>and</em> that the cleanup takes the token anyway pins both sides of it, so a
        /// future widening that made cleanup consult immunity would fail here rather than quietly
        /// leave a permanent obstacle on the board.</para>
        /// </summary>
        [Test]
        public void AnImmuneTotemIsStillTakenByTheEndOfRoundCleanup()
        {
            var board = CreateBoard();
            var token = Place(
                board, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-1");

            Assert.That(
                Build(board).For(RuleObject.ForToken(token.Id))
                    .Any(applied => applied.Modification.Kind == RuleModificationKind.Immunity),
                Is.True,
                "The precondition: this really is the immune face.");

            Assert.That(
                ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens)
                    .Select(removed => removed.Id),
                Has.Member(token.Id),
                "p.8's \"Remove all tokens from the board\" is not an action anybody performs, asks " +
                "no immunity question, and is the counterplay RULING 26 leaves standing - without " +
                "which Venerated Totem would put a permanent obstacle on the board.");
        }

        [Test]
        public void TheSaveIsMandatory_SoItIsReadByAutomaticRatherThanOfferedAsADecision()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-1");
            var minion = RuleObject.ForMinion(new MinionId(MinionIdValue));
            var set = Build(board);

            var offered = RemovalPreventionQuery.Offered(
                minion, set, Array.Empty<CardDefinition>(), Array.Empty<CardInstance>());
            Assert.That(offered, Is.Empty,
                "A mandatory prevention is not a decision: offering it would be a question with one " +
                "legal answer, and the payable-cards test would then drop it for having no CARDS to " +
                "pay with, which would let a printed sentence silently never fire.");

            var automatic = RemovalPreventionQuery.Automatic(minion, set);
            Assert.That(automatic.Count, Is.EqualTo(1),
                "\"If a friendly minion adjacent to a totem would be defeated, remove the totem " +
                "instead\" carries none of p.10's three softeners, so it is mandatory.");
            Assert.That(
                automatic[0].Modification.RemovalPreventionCost.Kind,
                Is.EqualTo(RemovalPreventionCostKind.RemoveTheSourceToken),
                "The price is the totem, not a card from anybody's hand.");
        }

        /// <summary>
        /// Milestone 8.37. The wiring this milestone owed: a mandatory prevention now
        /// resolves the removal directly, through the exact type
        /// <c>AttackDefenseCardActionStrategyBackend.ResolveRemovalStage</c> asks -
        /// <see cref="RemovalStageDecision.IsAutomaticallyPrevented"/> - rather than only being
        /// readable off <see cref="RemovalPreventionQuery.Automatic"/> in isolation.
        /// </summary>
        [Test]
        public void TheMandatoryTotemSaveIsReadByRemovalStageDecisionItself_NotOnlyByTheBareQuery()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-1");
            var minion = BoardObjectReference.ForMinion(new MinionId(MinionIdValue));
            var stage = new RemovalStageDecision(
                RuleActivity.Attack,
                RuleObject.ForHero(Tali),
                board,
                board,
                StagedBoardParticipants.Create(),
                Build(board),
                LanePushCauseKind.Defeat);

            Assert.That(stage.IsAutomaticallyPrevented(minion), Is.True,
                "The exact call ResolveRemovalStage now makes before ever offering a card-priced " +
                "pause must see the totem's mandatory save.");
            Assert.That(stage.OfferedPreventions(
                    minion, Array.Empty<CardDefinition>(), Array.Empty<CardInstance>()),
                Is.Empty,
                "The mandatory family is never mixed into the offered, card-priced one - Offered's own " +
                "IsPaidWithACard filter already keeps them apart, so DescribeRemovalPreventionOptions " +
                "needed no change of its own.");
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(1),
                "Reading the available prevention must not spend the source token.");
            Assert.That(stage.ApplyAutomaticPrevention(minion), Is.True,
                "A mandatory save is applied without asking anyone.");
            Assert.That(board.PlacedTokens, Is.Empty,
                "The Totem that saved the minion is removed as the printed cost.");
            Assert.That(board.Minions.Any(occupancy =>
                occupancy.MinionId.Equals(new MinionId(MinionIdValue))), Is.True,
                "The saved minion remains on the board.");
        }

        /// <summary>
        /// The negative control the wiring test above needs: a subject nothing mandatory protects is
        /// not automatically prevented, so the new check cannot be silently always-true.
        /// </summary>
        [Test]
        public void ANonAdjacentMinionIsNotAutomaticallyPrevented()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.VeneratedTotemDefinition, FarSpace, "totem-1");
            var minion = BoardObjectReference.ForMinion(new MinionId(MinionIdValue));
            var stage = new RemovalStageDecision(
                RuleActivity.Attack,
                RuleObject.ForHero(Tali),
                board,
                board,
                StagedBoardParticipants.Create(),
                Build(board),
                LanePushCauseKind.Defeat);

            Assert.That(stage.IsAutomaticallyPrevented(minion), Is.False);
            Assert.That(stage.ApplyAutomaticPrevention(minion), Is.False);
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(1),
                "An inapplicable Totem cannot be consumed.");
        }

        [Test]
        public void TwoAdjacentTotemsConsumeOnlyTheFirstCanonicalSource()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.AncestralTotemDefinition,
                new HexCoordinate(-1, 1), "totem-z");
            Place(board, TaliComponentContent.AncestralTotemDefinition,
                TokenAt, "totem-a");
            var stage = new RemovalStageDecision(
                RuleActivity.Attack,
                RuleObject.ForHero(Tali),
                board, board, StagedBoardParticipants.Create(), Build(board),
                LanePushCauseKind.Defeat);

            Assert.That(stage.ApplyAutomaticPrevention(
                BoardObjectReference.ForMinion(new MinionId(MinionIdValue))), Is.True);
            Assert.That(board.PlacedTokens.Select(token => token.Id.Value),
                Is.EquivalentTo(new[] { "totem-z" }),
                "One Defeat costs one Totem, chosen by stable source order.");
        }

        [Test]
        public void AnAncestralTotemDoesNotSaveARangedMinion()
        {
            var board = CreateBoard(MinionType(StandardMinionType.Ranged));
            Place(board, TaliComponentContent.AncestralTotemDefinition, TokenAt, "totem-1");

            Assert.That(
                RemovalPreventionQuery.Automatic(
                    RuleObject.ForMinion(new MinionId(MinionIdValue)), Build(board)),
                Is.Empty,
                "Ancestral Totem's printed \"melee\" is the whole difference between the two faces.");
        }

        [Test]
        public void AVeneratedTotemDoesSaveTheSameRangedMinion()
        {
            var board = CreateBoard(MinionType(StandardMinionType.Ranged));
            Place(board, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-1");

            Assert.That(
                RemovalPreventionQuery.Automatic(
                    RuleObject.ForMinion(new MinionId(MinionIdValue)), Build(board)).Count,
                Is.EqualTo(1),
                "The pair differ in exactly the one printed word, and the pair of tests is the proof.");
        }

        [Test]
        public void ATotemDoesNotSaveAMinionItIsNotAdjacentTo()
        {
            var board = CreateBoard();
            Place(board, TaliComponentContent.VeneratedTotemDefinition, FarSpace, "totem-1");

            Assert.That(
                RemovalPreventionQuery.Automatic(
                    RuleObject.ForMinion(new MinionId(MinionIdValue)), Build(board)),
                Is.Empty,
                "The adjacency is measured from the TOKEN's space, which is where p.19 puts a placed " +
                "token's own lifetime.");
        }

        [Test]
        public void ATotemDoesNotSaveAnEnemyMinion()
        {
            var board = CreateBoard(side: TeamSide.Blue);
            Place(board, TaliComponentContent.VeneratedTotemDefinition, TokenAt, "totem-1");

            Assert.That(
                RemovalPreventionQuery.Automatic(
                    RuleObject.ForMinion(new MinionId(MinionIdValue)), Build(board)),
                Is.Empty,
                "p.17's Friendly is \"another unit on your team\", read from the token's controller.");
        }

        // -------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------

        private const string MinionIdValue = "tali-token-minion-01";

        private static StandardMinionType MinionType(StandardMinionType type) => type;

        private static AppliedRuleModification[] InitiativeModificationsOn(
            AdjacentMoveAuthority board, HeroId heroId) =>
            Build(board).For(RuleObject.ForHero(heroId))
                .Where(applied =>
                    applied.Modification.Kind == RuleModificationKind.ValueModifier &&
                    applied.Modification.Channel == PrintedValueChannel.Initiative)
                .ToArray();

        private static RuleModificationSet Build(AdjacentMoveAuthority board) =>
            RuleModificationQuery.Build(
                null,
                board,
                StagedBoardParticipants.Create(),
                TieBreakerState.Unrevealed,
                UnlockedUltimateSet.None,
                RuleObject.None,
                null,
                null);

        private static TokenPropertyAffectedObject SaveScopeOf(TokenDefinition definition) =>
            definition.Properties
                .Single(property =>
                    property.Modification != null &&
                    property.Modification.Kind == RuleModificationKind.RemovalPrevention)
                .AffectedObject;

        private static PlacedToken Place(
            AdjacentMoveAuthority board,
            TokenDefinition definition,
            HexCoordinate coordinate,
            string instanceId)
        {
            var token = new PlacedToken(
                new TokenInstanceId(instanceId),
                definition,
                Tali,
                Tali,
                new CardInstanceId("tali-token-source-card"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        private static ComponentDefinitionCatalog CreateCatalog() =>
            new ComponentDefinitionCatalog(
                TaliComponentContent.Tokens(), Array.Empty<MarkerDefinition>());

        /// <summary>
        /// A radius-2 disc with Tali, an enemy hero and a friendly minion each standing on one of the
        /// token space's six neighbours - so a single placement at <see cref="TokenAt"/> is adjacent to
        /// all three and the three affected-object scopes can be told apart on one board.
        /// </summary>
        private static AdjacentMoveAuthority CreateBoard(
            StandardMinionType minionType = StandardMinionType.Melee,
            TeamSide side = TeamSide.Red) =>
            new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, DiscSpaces()),
                new[]
                {
                    new HeroOccupancy(Tali, TaliAt),
                    new HeroOccupancy(Enemy, EnemyAt)
                },
                Array.Empty<ObstacleOccupancy>(),
                new[]
                {
                    new QuickGameMinionSetupState(
                        new MinionId(MinionIdValue),
                        new TeamId(side == TeamSide.Red
                            ? StagedBoardParticipants.RedTeamIdValue
                            : StagedBoardParticipants.BlueTeamIdValue),
                        side,
                        minionType,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-tali-token")),
                        new ConfiguredSpaceReference(
                            Map, new SpaceId($"q{MinionAt.Q}_r{MinionAt.R}")),
                        MinionAt)
                });

        /// <summary>Every hex within distance 2 of the token space.</summary>
        private static HexCoordinate[] DiscSpaces()
        {
            var spaces = new System.Collections.Generic.List<HexCoordinate>();
            for (var q = -2; q <= 2; q++)
            {
                for (var r = -2; r <= 2; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(TokenAt, coordinate) <= 2)
                    {
                        spaces.Add(coordinate);
                    }
                }
            }

            return spaces.ToArray();
        }
    }
}
