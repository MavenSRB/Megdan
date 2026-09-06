using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z31. What Ursafar the Savage's nineteen faces are, and what each one
    /// composes.
    ///
    /// <para>This file audits the definitions against the transcription. What a player is offered and
    /// what the board looks like afterwards belongs to a later milestone that deals him; nothing here
    /// needs a board, which is the point - his whole set turns on one question about state, and that
    /// question is answerable at a desk.</para>
    /// </summary>
    public sealed class UrsafarVerifiedContentTests
    {
        /// <summary>
        /// The catalog his content actually runs against: the build's generic capabilities plus the
        /// three deferred sets this milestone owns. It is assembled here rather than taken from
        /// <c>MatchContentCatalogs.ForThisBuild()</c> precisely because those three are deliberately not
        /// registered yet - the registered list is serialized, and a hero this build does not deal must
        /// not move a pinned snapshot.
        /// </summary>
        private static EffectCapabilityCatalog Catalog() =>
            SyntheticEffectCapabilities.CreateCatalog(
                UrsafarActiveEffectContent.DeferredHandlers()
                    .Concat(SyntheticConditionCapabilities.DeferredHandlers())
                    .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers()));

        // -----------------------------------------------------------------------------------------
        // The printed set.
        // -----------------------------------------------------------------------------------------

        [Test]
        public void NineteenFaces_EighteenDealt_AndOneReturnedToTheBox()
        {
            Assert.That(UrsafarCardContent.AllDefinitions, Has.Count.EqualTo(19));
            Assert.That(UrsafarCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(UrsafarCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { UrsafarCardContent.JawsThatBite }));
            Assert.That(UrsafarCardContent.StartingHand, Has.Count.EqualTo(5));
            Assert.That(UrsafarCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                UrsafarCardContent.StartingHand
                    .Concat(UrsafarCardContent.Deck)
                    .Concat(UrsafarCardContent.ReturnedToBoxAtSetup)
                    .Distinct()
                    .Count(),
                Is.EqualTo(19),
                "p.6 partitions the printed set into the starting hand, the deck and the boxed " +
                "Handicap card, with nothing in two places and nothing left out.");
        }

        [Test]
        public void EveryFace_IsVerifiedOfficial_CarriesItsTranscription_AndOneRevision()
        {
            Assert.That(
                UrsafarCardContent.AllDefinitions.Select(definition => definition.ContentAuthority)
                    .Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
            Assert.That(
                UrsafarCardContent.AllDefinitions.Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { UrsafarCardContent.ContentRevision }));
            Assert.That(
                UrsafarCardContent.AllDefinitions.Where(definition =>
                    string.IsNullOrWhiteSpace(definition.SourceContent?.PrintedText)),
                Is.Empty,
                "The extraction is sole authority for a pack hero, so every face ships its words.");
        }

        /// <summary>
        /// The value table, audited row by row against <c>HERO_URSAFAR.md</c>'s. A dash in the shard's
        /// secondary columns is a zero here and means the icon is absent, which is why the assertion is
        /// about the action existing rather than about its value.
        /// </summary>
        [TestCase("claws-that-catch", 12, 2, 1, ActionKind.Attack, 4)]
        [TestCase("jaws-that-bite", 11, 1, 1, ActionKind.Attack, 3)]
        [TestCase("angry-roar", 7, 4, 0, ActionKind.Skill, 0)]
        [TestCase("prey-drive", 8, 7, 3, ActionKind.Attack, 5)]
        [TestCase("prey-abundance", 9, 7, 3, ActionKind.Attack, 6)]
        [TestCase("rip", 9, 6, 3, ActionKind.Attack, 5)]
        [TestCase("feeding-frenzy", 9, 8, 3, ActionKind.Attack, 6)]
        [TestCase("tear", 9, 7, 3, ActionKind.Attack, 5)]
        [TestCase("prowling-brute", 9, 5, 0, ActionKind.Movement, 1)]
        [TestCase("rampaging-beast", 10, 6, 0, ActionKind.Movement, 1)]
        [TestCase("cold-ire", 10, 6, 0, ActionKind.Movement, 1)]
        [TestCase("unstoppable-force", 10, 6, 0, ActionKind.Movement, 1)]
        [TestCase("eyes-of-flame", 10, 6, 0, ActionKind.Movement, 1)]
        [TestCase("sniff-out", 4, 2, 2, ActionKind.Skill, 0)]
        [TestCase("eyes-on-the-prey", 3, 3, 2, ActionKind.Skill, 0)]
        [TestCase("instinctive-reaction", 3, 3, 2, ActionKind.Skill, 0)]
        [TestCase("apex-predator", 3, 3, 2, ActionKind.Skill, 0)]
        [TestCase("evolutionary-response", 3, 3, 2, ActionKind.Skill, 0)]
        public void PrintedValues_MatchTheTranscription(
            string cardId,
            int initiative,
            int secondaryDefense,
            int secondaryMovement,
            ActionKind primaryKind,
            int primaryValue)
        {
            var definition = UrsafarCardContent.Definition(
                new CardDefinitionId("ursafar-" + cardId));
            Assert.That(definition.Initiative.Value, Is.EqualTo(initiative));

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(primaryKind));
            if (primaryValue > 0)
            {
                var channel = primaryKind == ActionKind.Attack
                    ? PrintedValueChannel.Attack
                    : PrintedValueChannel.Movement;
                Assert.That(
                    primary.BaseValues.Single(value => value.Channel == channel).Amount,
                    Is.EqualTo(primaryValue));
            }

            AssertSecondary(definition, ActionKind.Defense, PrintedValueChannel.Defense, secondaryDefense);
            AssertSecondary(
                definition, ActionKind.Movement, PrintedValueChannel.Movement, secondaryMovement);
        }

        private static void AssertSecondary(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel, int amount)
        {
            var secondary = definition.ActionDefinitions.SingleOrDefault(action =>
                action.Kind == kind && action.Origin != ActionDefinitionOrigin.PrintedPrimary);
            if (amount == 0)
            {
                Assert.That(secondary, Is.Null,
                    $"{definition.Id} prints no secondary {kind} icon, so it offers no such action.");
                return;
            }

            Assert.That(secondary, Is.Not.Null);
            Assert.That(
                secondary.BaseValues.Single(value => value.Channel == channel).Amount,
                Is.EqualTo(amount));
        }

        [Test]
        public void TheFiveBlueFaces_ArePrimaryMovementCards_TheRosterFirst()
        {
            var blue = new[]
            {
                UrsafarCardContent.ProwlingBrute, UrsafarCardContent.RampagingBeast,
                UrsafarCardContent.ColdIre, UrsafarCardContent.UnstoppableForce,
                UrsafarCardContent.EyesOfFlame
            };

            foreach (var cardId in blue)
            {
                var definition = UrsafarCardContent.Definition(cardId);
                var primary = definition.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                Assert.That(primary.Kind, Is.EqualTo(ActionKind.Movement), cardId.Value);

                // p.9 promotes the icon into the primary slot, so no Blue face prints a secondary one.
                Assert.That(
                    definition.ActionDefinitions.Count(action =>
                        action.Kind == ActionKind.Movement),
                    Is.EqualTo(1),
                    $"{cardId} prints one Movement icon and it is the primary's.");
            }
        }

        /// <summary>
        /// Which side of the walk each Blue face's text lands on. Three say "after movement" and take
        /// the milestone's new moment; two say nothing and take p.11's default.
        /// </summary>
        [TestCase("prowling-brute", CardTextTiming.AfterTheMovement)]
        [TestCase("rampaging-beast", CardTextTiming.AfterTheMovement)]
        [TestCase("unstoppable-force", CardTextTiming.AfterTheMovement)]
        [TestCase("cold-ire", CardTextTiming.BeforeTheMovement)]
        [TestCase("eyes-of-flame", CardTextTiming.BeforeTheMovement)]
        public void BlueCardText_LandsOnTheSideTheFacePrints(string cardId, CardTextTiming expected)
        {
            var definition = UrsafarCardContent.Definition(
                new CardDefinitionId("ursafar-" + cardId));
            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(primary.CardTextTiming, Is.EqualTo(expected));
            Assert.That(primary.EffectRoot, Is.Not.Null);
            Assert.That(
                CardTextTimingQuery.OwningActionKind(primary.CardTextTiming),
                Is.EqualTo(ActionKind.Movement));
        }

        [Test]
        public void OnlyTheHandicapCardIsDeferred_AndEveryOtherFaceIsComposedOrPrintsNoStep()
        {
            var awaited = UrsafarCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.TextComposition.IsAwaited))
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(awaited, Is.EqualTo(new[] { UrsafarCardContent.JawsThatBite }),
                "p.20's Handicap card is the standing deferral; nothing else of his is owed.");
        }

        // -----------------------------------------------------------------------------------------
        // The enraged state.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Thirteen faces print the keyword line, not fourteen. The inventory draft's prose says
        /// fourteen; its own transcription table lists thirteen, and 19 faces less five Green and one
        /// Ultimate is thirteen. This pins the corrected figure so the shard cannot inherit the slip.
        /// </summary>
        [Test]
        public void ThirteenFacesGrantEnraged_AndTwelveOfThemComposeIt()
        {
            Assert.That(UrsafarCardContent.GrantsEnraged, Has.Count.EqualTo(13));
            Assert.That(UrsafarActiveEffectContent.ComposedGrants, Has.Count.EqualTo(12));
            Assert.That(
                UrsafarCardContent.GrantsEnraged
                    .Except(UrsafarActiveEffectContent.ComposedGrants),
                Is.EqualTo(new[] { UrsafarCardContent.JawsThatBite }));

            // The five Green faces read the state and never grant it; the Ultimate replaces it.
            var nonGranting = UrsafarCardContent.AllDefinitions
                .Select(definition => definition.Id)
                .Except(UrsafarCardContent.GrantsEnraged)
                .ToArray();
            Assert.That(nonGranting, Has.Length.EqualTo(6));
            Assert.That(nonGranting, Contains.Item(UrsafarCardContent.UnboundFury));
        }

        [Test]
        public void EveryGrant_IsARoundLongControllerEffect_DormantUntilItsOwnActionEnds()
        {
            foreach (var cardId in UrsafarActiveEffectContent.ComposedGrants)
            {
                var definition = UrsafarActiveEffectContent.DefinitionFor(cardId);
                Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisRound));
                Assert.That(
                    definition.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
                Assert.That(definition.ExpirationCheckpoint, Is.EqualTo(TimingCheckpoint.RoundEnd));
                Assert.That(definition.Modifications, Is.Empty,
                    "Being enraged changes no rule; it exists to be read.");

                // The build-up rule, structurally: created when the action starts, active only when it
                // ends, so the card that grants it cannot read it.
                Assert.That(definition.CreationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionStarted));
                Assert.That(definition.ActivationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionEnded));
                Assert.That(
                    definition.EffectKind,
                    Is.EqualTo(HeroConditionQuery.EffectKind(HeroConditionKind.Enraged)));
            }
        }

        [Test]
        public void EveryGrant_CarriesItsOwnUniquenessKey_SoTwelveCanCoexist()
        {
            var keys = UrsafarActiveEffectContent.ComposedGrants
                .Select(cardId => UrsafarActiveEffectContent.DefinitionFor(cardId).UniquenessKey)
                .ToArray();

            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length),
                "p.10 allows one instance of an active effect per CARD, and the designer's own answer " +
                "is that several enrage effects coexist. A shared key would suppress all but the " +
                "first as a duplicate.");
        }

        /// <summary>
        /// The rule the whole hero turns on, driven through the real lifecycle rather than asserted
        /// about a definition: a card's grant is invisible to that same card's own condition.
        /// </summary>
        [Test]
        public void AGrantIsDormantForTheWholeOfItsOwnAction_AndActiveAfterwards()
        {
            var hero = new HeroId("ursafar-1");
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(
                new ActiveEffectId("grant"),
                UrsafarActiveEffectContent.DefinitionFor(UrsafarCardContent.ProwlingBrute),
                hero,
                TeamSide.Red,
                hero,
                new CardInstanceId("ursafar-1-prowling-brute"),
                new ActionDefinitionId("primary-movement"),
                1,
                1);

            Assert.That(
                HeroConditionQuery.IsInCondition(HeroConditionKind.Enraged, hero, lifecycle),
                Is.False,
                "Its own action is still running, so the card cannot satisfy its own 'If enraged'.");

            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                HeroConditionQuery.IsInCondition(HeroConditionKind.Enraged, hero, lifecycle),
                Is.True,
                "The next card of the round reads it, which is what makes enrage a build-up.");
        }

        // -----------------------------------------------------------------------------------------
        // The compositions.
        // -----------------------------------------------------------------------------------------

        [Test]
        public void EveryComposedRoot_IsAValidEffectDefinition()
        {
            var catalog = Catalog();
            var checkedRoots = 0;

            foreach (var definition in UrsafarCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    foreach (var root in new[] { action.EffectRoot, action.SecondCardTextRoot })
                    {
                        if (root == null)
                        {
                            continue;
                        }

                        checkedRoots++;
                        Assert.That(
                            EffectDefinitionValidator.Validate(root, catalog),
                            Is.Empty,
                            $"{definition.Id} / {action.Id} / {root.Id}");
                    }
                }
            }

            Assert.That(checkedRoots, Is.EqualTo(19),
                "Seventeen faces compose text - every one but p.20's Handicap card and the Ultimate, " +
                "which prints a permanent ability rather than an action - and two of them, Claws That " +
                "Catch and Tear, compose a root at each of p.14's two attacker moments.");
        }

        [Test]
        public void AngryRoar_BorrowsFromTheActivePool_AndStillGrantsWhenTheConditionIsFalse()
        {
            var primary = UrsafarCardContent.Definition(UrsafarCardContent.AngryRoar)
                .ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var root = primary.EffectRoot;

            var borrow = root.Nodes.Single(node =>
                node.Capability.Equals(
                    SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1));
            Assert.That(
                (BorrowedActionPool)borrow.Inputs
                    .Single(input => input.ParameterName ==
                        SyntheticBorrowedActionCapabilities.PoolParameter)
                    .Literal.IntegerValue,
                Is.EqualTo(BorrowedActionPool.OwnedActiveCardsWithAnActiveEffect));

            // The grant is a sibling of the condition, not its child: a false condition skips its own
            // clause and the card continues, which is what the general conditional ruling says.
            var conditional = root.Nodes.Single(node =>
                node.Kind == EffectNodeKind.Conditional);
            var grant = root.Nodes.Single(node =>
                node.Capability.Equals(
                    UrsafarActiveEffectContent.GrantFor(UrsafarCardContent.AngryRoar)));
            Assert.That(conditional.ChildNodeIds.Contains(grant.Id), Is.False);

            var rootNode = root.Nodes.Single(node => node.Id.Equals(root.RootNodeId));
            Assert.That(rootNode.ChildNodeIds.Contains(grant.Id), Is.True);
        }

        [Test]
        public void BothGoldFaces_PutTheirGrantAfterTheAttack_SoAFizzledAttackNeverEnrages()
        {
            var primary = UrsafarCardContent.Definition(UrsafarCardContent.ClawsThatCatch)
                .ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack));
            Assert.That(primary.SecondCardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(
                primary.SecondCardTextRoot.Nodes.Single().Capability,
                Is.EqualTo(UrsafarActiveEffectContent.GrantFor(UrsafarCardContent.ClawsThatCatch)),
                "The designer's own answer: the attack is mandatory and the card fizzles before you " +
                "enrage if there is no target. Step 7 is never reached when step 1 stops the card.");
        }

        [Test]
        public void EvolutionaryResponse_MayTakeBothBullets_WhereInstinctiveReactionTakesOne()
        {
            Assert.That(ChooseBounds(UrsafarCardContent.InstinctiveReaction), Is.EqualTo((1, 1)));
            Assert.That(ChooseBounds(UrsafarCardContent.EvolutionaryResponse), Is.EqualTo((1, 2)));
        }

        private static (int, int) ChooseBounds(CardDefinitionId cardId)
        {
            var node = UrsafarCardContent.Definition(cardId)
                .ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot.Nodes
                .Single(candidate => candidate.Kind == EffectNodeKind.Choose);
            return (node.MinimumSelections, node.MaximumSelections);
        }

        [Test]
        public void UnboundFury_IsAStandingDeclaration_ReadByBothOfItsClauses()
        {
            var rule = UrsafarCardContent.Definition(UrsafarCardContent.UnboundFury).UltimateRule;

            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.Modifications, Is.Empty,
                "Neither clause changes a value, a permission or an object type.");
            Assert.That(rule.TriggeredEffect.HasValue, Is.False);
            Assert.That(rule.RuleKind, Is.EqualTo(UrsafarEffectContent.UnboundFuryRuleKind));

            // Both readings match on the same rule kind, and this is what stops them drifting apart.
            Assert.That(
                HeroConditionQuery.StandingRuleKind(HeroConditionKind.Enraged),
                Is.EqualTo(rule.RuleKind));
            Assert.That(
                BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActiveRuleKind,
                Is.EqualTo(rule.RuleKind));
        }

        /// <summary>
        /// The shortfall, asserted by name. Three Red faces print "and the target was not removed" and
        /// nothing in this build can answer it; their removal clause is not composed. This test fails
        /// the day the fact arrives without the clause being composed with it, which is the only way to
        /// keep a named gap from becoming a forgotten one.
        /// </summary>
        [Test]
        public void TheThreeRemovalReds_ComposeTheirGrantAndNotTheirRemoval()
        {
            Assert.That(UrsafarEffectContent.TargetSurvivedFactIsOwed, Has.Count.EqualTo(3));

            foreach (var cardId in UrsafarEffectContent.TargetSurvivedFactIsOwed)
            {
                var primary = UrsafarCardContent.Definition(cardId).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
                Assert.That(primary.EffectRoot.Nodes, Has.Count.EqualTo(1), cardId.Value);
                Assert.That(
                    primary.EffectRoot.Nodes.Single().Capability,
                    Is.EqualTo(UrsafarActiveEffectContent.GrantFor(cardId)));
                Assert.That(
                    UrsafarCardContent.Definition(cardId).SourceContent.PrintedText,
                    Does.Contain("the target was not removed"),
                    "The clause is printed and transcribed; only its composition is owed.");
            }
        }

        [Test]
        public void NothingOfHisIsRegisteredYet_BecauseThisBuildDoesNotDealHim()
        {
            Assert.That(UrsafarActiveEffectContent.RegisteredHandlers(), Is.Empty);
            Assert.That(SyntheticConditionCapabilities.RegisteredHandlers(), Is.Empty);
            Assert.That(SyntheticBorrowedActionCapabilities.RegisteredHandlers(), Is.Empty);

            var build = MatchContentCatalogs.ForThisBuild().Capabilities;
            Assert.That(
                build.IsRegistered(SyntheticConditionCapabilities.HeroConditionV1), Is.False);
            Assert.That(
                build.IsRegistered(SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1),
                Is.False);
            Assert.That(
                build.IsRegistered(
                    UrsafarActiveEffectContent.GrantFor(UrsafarCardContent.AngryRoar)),
                Is.False,
                "The registered list is serialized; registering before he is dealt would move pinned " +
                "snapshot bytes and read at the far end as a serialization defect.");
        }

        [Test]
        public void DeferredHandlers_AreFifteen_AndAllRegisterCleanlyTogether()
        {
            var handlers = UrsafarActiveEffectContent.DeferredHandlers().ToArray();
            Assert.That(handlers, Has.Length.EqualTo(15),
                "Twelve enrage grants, two Movement raises, and Tear's defeat-cost declaration.");
            Assert.That(
                handlers.Select(handler => handler.Reference).Distinct().Count(),
                Is.EqualTo(handlers.Length));
            Assert.That(() => Catalog(), Throws.Nothing);
        }
    }
}
