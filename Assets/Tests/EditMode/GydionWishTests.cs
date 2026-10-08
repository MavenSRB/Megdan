using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30g: The Archwizard and Wish. Wish casts any spell still in the book (GY-Q9), is
    /// removed faceup as it is cast (GY-Q2), raises its caster's team's public count (GY-Q10), and the
    /// third Wish wins the game for that team once its whole action, its inner spell included, has
    /// resolved - unless the inner spell already ended the match. The Archwizard turns Prepare Spells'
    /// refill into "Add all spell cards to the spellbook" or "Cast Wish instead" while Wish is in the
    /// book, a full book included (GY-Q7). Proven on Gydion's own content on a staged round; nothing
    /// is registered, so the fixture's catalog carries the deferred handlers.
    /// </summary>
    public sealed class GydionWishTests
    {
        private static readonly HeroId Caster = WishRound.Caster;

        // -------------------------------------------------------------------------------------------
        // Wish
        // -------------------------------------------------------------------------------------------

        [Test]
        public void Wish_CastsAnySpellStillInTheBook_IncludingOneNoCardOfHisLists_GyQ9()
        {
            var round = new WishRound(removed: new[] { GydionCardContent.Sunburst });
            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);

            var choice = round.Choice;
            Assert.That(choice?.NodeId, Is.EqualTo(GydionCardContent.WishChoice),
                "\"Cast any spell in the Spellbook\" is asked");
            Assert.That(choice.ChooserId, Is.EqualTo(Caster));
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(GydionCardContent.WishCastableSpells.Select(GydionCardContent.WishCastArm)),
                "one option per other spell, of every tier (GY-Q9) - Wish itself is not one");
            Assert.That(choice.Candidates, Has.Count.EqualTo(21));
            Assert.That(choice.Candidates.Select(candidate => candidate.Kind),
                Is.All.EqualTo(EffectChoiceCandidateKind.Option), "spells are named, never offered as book cards");
            // Greyed exactly when the spell is removed - or, until 9.30d and 9.30f compose it, when its
            // printed text is still owed, which the cast refuses rather than performing short (9.30e).
            bool Owed(CardDefinitionId spell) =>
                GydionCardContent.Definition(spell).ActionDefinitions.Single().TextComposition.IsAwaited;
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(GydionCardContent.WishCastableSpells
                    .Where(spell => spell.Equals(GydionCardContent.Sunburst) || Owed(spell))
                    .Select(GydionCardContent.WishCastArm)),
                "a spell already removed is greyed; every in-book composed spell is castable (GY-Q12)");
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Does.Contain(GydionCardContent.WishCastArm(GydionCardContent.Sunburst)));
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.SupersetOf(new[] { GydionCardContent.MidasTouch, GydionCardContent.FindFamiliar, GydionCardContent.DimensionDoor }
                    .Select(GydionCardContent.WishCastArm)),
                "spells printed only on cards he does not hold are offered (GY-Q9)");
            Assert.That(choice.DeclineInvocation, Is.Null, "the cast is not a \"may\"");

            // Midas' Touch is printed only on Lesser and Greater Transmutation, neither of which he holds.
            var coinsBefore = round.Coins(Caster);
            round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.MidasTouch));
            Assert.That(round.Spell(GydionCardContent.MidasTouch).State, Is.EqualTo(RemovedFaceup),
                "the chosen spell is cast: removed from the book faceup");
            Assert.That(round.Coins(Caster) - coinsBefore, Is.EqualTo(1),
                "and performed by the caster: two others removed (Sunburst and Wish) gain 1 coin");
            Assert.That(round.Card(WishRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved),
                "the played card resolves after Wish and its inner spell");
            Assert.That(GydionCardContent.SpellIds.Where(spell =>
                    round.Spell(spell).State.Location == CardLocation.RemovedFromSpellbook),
                Is.EquivalentTo(new[] { GydionCardContent.Sunburst, GydionCardContent.Wish, GydionCardContent.MidasTouch }),
                "nothing else is cast and nothing is refilled");
        }

        [Test]
        public void Wish_IsRemovedFromTheBookFaceupAsItIsCast_GyQ2()
        {
            var round = new WishRound();
            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);

            Assert.That(round.Spell(GydionCardContent.Wish).State, Is.EqualTo(RemovedFaceup),
                "GY-Q2: Wish is removed faceup as it is cast, like every spell");
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.WishChoice));
            Assert.That(round.Choice.Candidates.Concat(round.Choice.UnavailableCandidates)
                    .Select(candidate => candidate.OptionNodeId),
                Has.No.Member(GydionCardContent.WishCastArm(GydionCardContent.Wish)),
                "and it is not in the book while it resolves, so it never casts itself");
            Assert.That(GydionCardContent.SpellIds.Where(spell => !spell.Equals(GydionCardContent.Wish))
                    .All(spell => round.Spell(spell).State.Location == CardLocation.Spellbook), Is.True,
                "casting Wish instead adds no spell to the book");
        }

        /// <summary>
        /// With every other spell removed Wish has nothing to cast; the cast is still a cast (GY-Q12),
        /// so it is removed, it counts, and nothing is asked.
        /// </summary>
        [Test]
        public void Wish_WithNothingElseInTheBook_CastsNothingAndStillCounts_GyQ12()
        {
            var round = new WishRound(removed: GydionCardContent.WishCastableSpells);
            round.StartCasting();
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.PrepareSpellsReplacementChoice),
                "a book holding only Wish still offers it instead of the refill");
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);

            Assert.That(round.Choice, Is.Null, "\"Cast any spell in the Spellbook\" finds none and asks nothing");
            Assert.That(round.Spell(GydionCardContent.Wish).State, Is.EqualTo(RemovedFaceup));
            Assert.That(round.Round.TeamVictoryTallies.Single().Count, Is.EqualTo(1), "and the Wish counts");
            Assert.That(round.Card(WishRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // -------------------------------------------------------------------------------------------
        // The count and the team victory (GY-Q10)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheWishCount_RisesWhenWishIsCast_AndSurvivesHisDefeatAndTheRound_GyQ10()
        {
            var round = new WishRound(enemyAt: new HexCoordinate(1, 0), enemyStrikes: true);
            Assert.That(round.Round.TeamVictoryTallies, Is.Empty, "no team has counted yet");

            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);
            Assert.That(round.Round.TeamVictoryTallies, Is.EqualTo(new[] { new TeamVictoryTally(round.CasterTeam, 1, 3) }),
                "the count rises when Wish is cast, before its inner spell, toward the printed three");
            round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.MidasTouch));
            Assert.That(round.Round.Outcome.HasEnded, Is.False, "one Wish wins nothing");
            Assert.That(round.Round.TeamVictoryTeamId.IsDefined, Is.False);

            round.EnemyDefeatsTheCaster();
            Assert.That(round.Board.Occupancies.Any(value => value.HeroId.Equals(Caster)), Is.False,
                "Gydion is defeated and off the board");
            Assert.That(round.Round.TeamVictoryTallies.Single().Count, Is.EqualTo(1), "the count survives his Defeat");

            round.FinishTheRound();
            Assert.That(round.Round.RoundIndex, Is.EqualTo(2));
            Assert.That(round.Round.TeamVictoryTallies.Single().Count, Is.EqualTo(1), "and the round's end");

            var restored = round.Restore(round.Snapshot());
            Assert.That(restored.Round.TeamVictoryTallies, Is.EqualTo(round.Round.TeamVictoryTallies),
                "and a restore: the count is persisted");
        }

        [Test]
        public void EachWishCast_AddsOneToTheCount()
        {
            var round = new WishRound(wishesAlreadyCast: 1);
            Assert.That(round.Round.TeamVictoryTallies.Single().Count, Is.EqualTo(1));
            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);
            Assert.That(round.Round.TeamVictoryTallies.Single(), Is.EqualTo(new TeamVictoryTally(round.CasterTeam, 2, 3)));
            round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.MidasTouch));
            Assert.That(round.Round.Outcome.HasEnded, Is.False, "two Wishes win nothing");
            Assert.That(round.Round.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded));
        }

        [Test]
        public void TheThirdWish_WinsForHisTeam_OnceItsInnerSpellHasResolved_GyQ10()
        {
            var round = new WishRound(wishesAlreadyCast: 2);
            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);
            Assert.That(round.Round.TeamVictoryTallies.Single().Count, Is.EqualTo(3), "the third Wish is cast");

            round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.FindFamiliar));
            Assert.That(round.OfferedSpaces(), Is.Not.Empty, "Find Familiar, the inner spell, asks where its token goes");
            Assert.That(round.Round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn),
                "the match has not ended while the inner spell is still resolving");
            Assert.That(round.Round.TeamVictoryTeamId.IsDefined, Is.False, "and nothing is claimed yet");

            round.PickSpace(new HexCoordinate(1, 0));
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.FindFamiliarRemovalChoice));
            Assert.That(round.Round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn),
                "nor while its second sentence is asked");
            round.Confirm();

            Assert.That(round.Familiars().ToArray(), Has.Length.EqualTo(1), "the inner spell resolved whole");
            Assert.That(round.Round.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "\"After you cast the Wish spell three times your team wins the game\"");
            Assert.That(round.Round.Outcome, Is.EqualTo(MatchOutcome.Won(
                MatchVictoryCause.WishesCast, round.CasterTeam, round.EnemyTeam)),
                "his TEAM wins, by the card's own cause");
            Assert.That(round.Round.Outcome.HasSoloWinner, Is.False);
        }

        [Test]
        public void AnEndingTheInnerSpellCauses_Stands_GyQ10()
        {
            // Sunburst: three spells staged removed and Wish removed by its cast make Attack 4 and
            // Range 4, at exactly which the enemy stands; his team has one Life left.
            var round = new WishRound(
                wishesAlreadyCast: 2,
                removed: new[] { GydionCardContent.MagicMissile, GydionCardContent.ShockingGrasp, GydionCardContent.ExpeditiousRetreat },
                enemyAt: new HexCoordinate(4, 0),
                lives: 1);
            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);
            round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.Sunburst));
            round.TargetEnemy();
            round.EnemyDeclinesTheDefense();

            Assert.That(round.Round.TeamLives.Counters.Single(entry => entry.Key.Equals(round.EnemyTeam)).Value,
                Is.EqualTo(0), "the inner spell spent the opposing team's last Life");
            Assert.That(round.Round.TeamVictoryTallies.Single().Count, Is.EqualTo(3), "on the third Wish");
            Assert.That(round.Round.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(round.Round.Outcome, Is.EqualTo(MatchOutcome.Won(
                MatchVictoryCause.LifeCountersExhausted, round.CasterTeam, round.EnemyTeam)),
                "the ending the inner spell caused stands (GY-Q10); the Wish claims nothing over it");
            Assert.That(round.Round.TeamVictoryTeamId.IsDefined, Is.False);
            var restored = round.Restore(round.Snapshot());
            Assert.That(restored.Round.Outcome, Is.EqualTo(round.Round.Outcome), "and the ended match restores");
        }

        [Test]
        public void ASnapshotMidWish_RestoresTheCountAndTheNestedCasts_AndFinishesInTheSameBytes()
        {
            var live = new WishRound(wishesAlreadyCast: 2);
            live.StartCasting();
            live.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);

            var atWish = live.Snapshot();
            var restoredAtWish = live.Restore(atWish);
            Assert.That(restoredAtWish.Snapshot(), Is.EqualTo(atWish), "byte-identical round trip at Wish's own choice");
            Assert.That(restoredAtWish.Round.TeamVictoryTallies, Is.EqualTo(live.Round.TeamVictoryTallies),
                "the count Wish raised as it was cast is restored");
            Assert.That(restoredAtWish.Choice?.NodeId, Is.EqualTo(GydionCardContent.WishChoice));
            Assert.That(restoredAtWish.Choice.UnavailableCandidates, Is.EqualTo(live.Choice.UnavailableCandidates));

            live.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.FindFamiliar));
            restoredAtWish.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.FindFamiliar));
            Assert.That(restoredAtWish.Snapshot(), Is.EqualTo(live.Snapshot()));

            // Inside the inner spell: Prepare Spells and Wish both stand suspended around Find Familiar.
            var inner = live.Snapshot();
            var restored = live.Restore(inner);
            Assert.That(restored.Snapshot(), Is.EqualTo(inner), "byte-identical round trip inside the inner spell");
            foreach (var target in new[] { live, restored })
            {
                target.PickSpace(new HexCoordinate(1, 0));
                target.Confirm();
            }

            Assert.That(restored.Snapshot(), Is.EqualTo(live.Snapshot()), "the restored Wish finishes in the same bytes");
            Assert.That(restored.Round.Outcome, Is.EqualTo(MatchOutcome.Won(
                MatchVictoryCause.WishesCast, live.CasterTeam, live.EnemyTeam)),
                "and wins once its inner spell has resolved, as the live one does");
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(59));
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(49));
        }

        /// <summary>
        /// A match a card's claim ended restores from its own snapshot. The reader's outcome check read
        /// neither recorded ending, so a payload ended by a claim was refused as an unexplained end -
        /// the solo claim's too, since 8.39g.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void AMatchACardClaimEnded_RestoresFromItsSnapshot(bool byTheThirdWish)
        {
            var round = new WishRound(wishesAlreadyCast: byTheThirdWish ? 2 : 0);
            round.StartCasting();
            round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);
            if (!byTheThirdWish)
            {
                Assert.That(round.Round.TryClaim(Caster), Is.True, "a solo claim, recorded");
            }

            round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.MidasTouch));
            Assert.That(round.Round.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(round.Round.Outcome.Cause, Is.EqualTo(
                byTheThirdWish ? MatchVictoryCause.WishesCast : MatchVictoryCause.CoinHoardClaimed));

            var snapshot = round.Snapshot();
            var restored = round.Restore(snapshot);
            Assert.That(restored.Round.Outcome, Is.EqualTo(round.Round.Outcome), "the ended match restores");
            Assert.That(restored.Snapshot(), Is.EqualTo(snapshot));
        }

        [Test]
        public void AClaimIsRefused_ShortOfTheThreshold_AndOnceAnotherClaimStands()
        {
            var round = new WishRound(wishesAlreadyCast: 2);
            Assert.That(round.Round.TryClaimTeamVictory(Caster), Is.False, "two of three claims nothing");
            Assert.That(round.Round.TryAdvanceTeamVictoryTally(Caster, 3), Is.True);
            Assert.That(round.Round.TryClaim(Caster), Is.True, "a solo claim taken first ...");
            Assert.That(round.Round.TryClaimTeamVictory(Caster), Is.False,
                "... leaves no match for the team claim: two endings cannot both be the ending");
            Assert.That(round.Round.TryClaimTeamVictory(new HeroId("nobody")), Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // The Archwizard (GY-Q7, lesson 26)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheArchwizard_OffersWishInsteadOfTheRefill_FromAFullBookToo_GyQ7()
        {
            foreach (var removed in new[]
                     {
                         Array.Empty<CardDefinitionId>(),
                         new[] { GydionCardContent.Fireball, GydionCardContent.Shield }
                     })
            {
                var round = new WishRound(removed: removed);
                var at = removed.Length == 0 ? "full book: " : "two removed: ";
                Assert.That(round.IsOffered, Is.True, at + "Prepare Spells is playable (GY-Q7)");
                round.StartCasting();
                var choice = round.Choice;
                Assert.That(choice?.NodeId, Is.EqualTo(GydionCardContent.PrepareSpellsReplacementChoice),
                    at + "\"you may cast the Wish spell in the spellbook instead\" is asked");
                Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId), Is.EqualTo(new[]
                {
                    GydionCardContent.PrepareSpellsAddAllArm, GydionCardContent.PrepareSpellsWishInsteadArm
                }));
                Assert.That(choice.UnavailableCandidates, Is.Empty, at + "both are available");
                Assert.That(GydionCardContent.SpellIds.Where(spell => round.Spell(spell).State.Location ==
                        CardLocation.RemovedFromSpellbook), Is.EquivalentTo(removed),
                    at + "nothing is added before he chooses");

                round.ChooseArm(GydionCardContent.PrepareSpellsAddAllArm);
                Assert.That(GydionCardContent.SpellIds.All(spell =>
                        round.Spell(spell).State.Equals(CardStateFacts.InitialInSpellbook())), Is.True,
                    at + "choosing the refill adds every spell back");
                Assert.That(round.Round.TeamVictoryTallies, Is.Empty, at + "and casts no Wish");
                Assert.That(round.Card(WishRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            }
        }

        [Test]
        public void TheArchwizard_OffersNothingWithWishRemoved_OrWhileItIsLocked()
        {
            var removedWish = new WishRound(removed: new[] { GydionCardContent.Wish, GydionCardContent.Fireball });
            removedWish.StartCasting();
            Assert.That(removedWish.Choice, Is.Null, "with Wish removed there is no \"instead\": nothing is asked");
            Assert.That(GydionCardContent.SpellIds.All(spell =>
                    removedWish.Spell(spell).State.Equals(CardStateFacts.InitialInSpellbook())), Is.True,
                "and the refill adds every spell back, Wish included (GY-Q2: the next refill is an ordinary one)");

            var locked = new WishRound(removed: new[] { GydionCardContent.Fireball }, ultimateUnlocked: false);
            locked.StartCasting();
            Assert.That(locked.Choice, Is.Null, "without the Ultimate unlocked nothing is asked");
            Assert.That(locked.Spell(GydionCardContent.Fireball).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(locked.Card(WishRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void CastingWishInstead_KeepsARunningShieldLikeEffect_AndTheRefillEndsIt_Lesson26()
        {
            foreach (var wishInstead in new[] { true, false })
            {
                var round = new WishRound(firstCastsTheWard: true);
                round.StartCasting();
                var ward = round.Ward;
                var effect = round.Round.Timing.Effects.Single(value =>
                    value.Definition.Equals(SyntheticSpellbook.WardDefinition));
                Assert.That(effect.SourceCardInstanceId, Is.EqualTo(ward.Id), "the Ward is stamped with its spell");
                Assert.That(ward.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));

                round.NextTurn();
                round.StartCastingOff(WishRound.NextCard);
                Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.PrepareSpellsReplacementChoice));
                if (wishInstead)
                {
                    round.ChooseArm(GydionCardContent.PrepareSpellsWishInsteadArm);
                    round.ChooseArm(GydionCardContent.WishCastArm(GydionCardContent.MidasTouch));
                    Assert.That(round.Round.Timing.Effects.Single(value => value.Id.Equals(effect.Id)).EndedAt,
                        Is.Null, "casting Wish instead returns no spell, so the Ward keeps running (lesson 26)");
                    Assert.That(ward.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
                }
                else
                {
                    round.ChooseArm(GydionCardContent.PrepareSpellsAddAllArm);
                    Assert.That(round.Round.Timing.Effects.Single(value => value.Id.Equals(effect.Id)).EndedAt,
                        Is.EqualTo(TimingCheckpoint.SourceCardStateChanged),
                        "the refill returns the Ward's spell, which ends it (GY-05 designer)");
                    Assert.That(ward.State, Is.EqualTo(CardStateFacts.InitialInSpellbook()));
                }

                Assert.That(round.Card(WishRound.NextCard).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            }
        }

        // -------------------------------------------------------------------------------------------
        // Content and labels
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheArchwizardAndPrepareSpells_PrintTheirFaces()
        {
            var ultimate = GydionCardContent.TheArchwizardDefinition;
            Assert.That(ultimate.DisplayName, Is.EqualTo("The Archwizard"));
            Assert.That(ultimate.Progression.IsUltimate, Is.True);
            Assert.That(ultimate.UltimateRule.RuleKind, Is.EqualTo(GydionCardContent.TheArchwizardRuleKind));
            Assert.That(ultimate.SourceContent.PrintedText, Is.EqualTo(GydionCardContent.TheArchwizardText));

            var prepare = GydionCardContent.PrepareSpellsDefinition;
            Assert.That(prepare.DisplayName, Is.EqualTo("Prepare Spells"));
            Assert.That(prepare.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(prepare.Progression.Classification, Is.EqualTo(CardClassification.Basic));
            Assert.That(prepare.Initiative, Is.EqualTo(new InitiativeValue(13)));
            Assert.That(prepare.ActionDefinitions.Select(action => (action.Kind, action.Origin)), Is.EqualTo(new[]
            {
                (ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary),
                (ActionKind.Defense, ActionDefinitionOrigin.Reaction)
            }));
            Assert.That(prepare.ActionDefinitions[1].BaseValues.Single(),
                Is.EqualTo(new PrintedValueDefinition(PrintedValueChannel.Defense, 2)));

            var wish = GydionCardContent.Definition(GydionCardContent.Wish).ActionDefinitions.Single();
            Assert.That(wish.TextComposition.IsAwaited, Is.False, "Wish is composed");
            Assert.That(BorrowedPrimaryActionDeclaration.Refuses(GydionCardContent.Definition(GydionCardContent.Wish)),
                Is.EqualTo(BorrowRefusal.None), "so it can be cast");
        }

        [Test]
        public void WishsOptionsAndTheReplacement_HaveLabelsDerivedFromTheContent()
        {
            var catalogs = WishRound.CreateCatalogs();
            var labels = GydionCardContent.ArchwizardDecisionLabels().ToArray();
            foreach (var card in new[]
                     {
                         GydionCardContent.Definition(GydionCardContent.Wish), GydionCardContent.PrepareSpellsDefinition
                     })
            {
                Assert.That(DecisionLabelRequirementQuery.For(card, catalogs).Select(row => row.NodeId),
                    Is.EquivalentTo(labels.Where(label => label.CardId.Equals(card.Id)).Select(label => label.NodeId)),
                    card.DisplayName + ": the controls it raises are exactly the labelled ones");
            }

            string Label(EffectNodeId node) => labels.Single(label => label.NodeId.Equals(node)).Label.Chosen;
            Assert.That(Label(GydionCardContent.WishCastArm(GydionCardContent.MidasTouch)), Is.EqualTo("Cast Midas' Touch"),
                "the printed name, read off the definition");
            Assert.That(Label(GydionCardContent.PrepareSpellsAddAllArm), Is.EqualTo("Add all spell cards to the spellbook"));
            Assert.That(Label(GydionCardContent.PrepareSpellsWishInsteadArm), Is.EqualTo("Cast Wish instead"));
        }

        [Test]
        public void TheTallyAndTheTeamEnding_CrossTheSeatProjectionAndTheWire()
        {
            var tally = new TeamVictoryTally(new TeamId("team-red"), 2, 3);
            Assert.That(tally.ToString(), Is.EqualTo("2/3"), "what the plaque prints");
            Assert.That(tally.IsReached, Is.False);
            Assert.That(new TeamVictoryTally(new TeamId("team-red"), 3, 3).IsReached, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new TeamVictoryTally(new TeamId("team-red"), 0, 3));

            var participants = StagedBoardParticipants.Create();
            var red = participants.GetPlayerForHero(Caster).TeamId;
            var blue = participants.Teams.Single(team => !team.Id.Equals(red)).Id;
            Assert.That(TeamVictoryTallyQuery.ForHero(participants, new[] { new TeamVictoryTally(red, 1, 3) }, Caster),
                Is.EqualTo(new TeamVictoryTally(red, 1, 3)));
            Assert.That(TeamVictoryTallyQuery.ForHero(participants, new[] { new TeamVictoryTally(blue, 1, 3) }, Caster),
                Is.Null, "another team's count is not his");

            var reading = MatchOutcomeQuery.Read(participants, LanePushGameEndingCauseNone, TeamSide.None,
                new TeamLifeState(participants, 2), true, default, red);
            Assert.That(reading.Outcome, Is.EqualTo(MatchOutcome.Won(MatchVictoryCause.WishesCast, red, blue)));
            Assert.That(MatchOutcomeQuery.Read(participants, LanePushGameEndingCauseNone, TeamSide.None,
                    new TeamLifeState(participants, 2), false, default, red).Fault,
                Is.EqualTo(MatchOutcomeFault.UnrecordedGameEnd), "the post-command check notices it");
            Assert.That(MatchOutcomeQuery.Read(participants, LanePushGameEndingCauseNone, TeamSide.None,
                    new TeamLifeState(participants, 2), true, Caster, red).Fault,
                Is.EqualTo(MatchOutcomeFault.ConflictingVictoryPaths), "two endings are refused, not preferred");
        }

        private static readonly GuardsDigital.Core.Rules.LanePush.LanePushGameEndingCause LanePushGameEndingCauseNone =
            GuardsDigital.Core.Rules.LanePush.LanePushGameEndingCause.None;

        private static readonly CardStateFacts RemovedFaceup = new CardStateFacts(
            CardLocation.RemovedFromSpellbook, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp);

        // -------------------------------------------------------------------------------------------
        // The staged round
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One round on a disk: the caster (Red) owns Gydion's twenty-two spells (and, when asked, the
        /// synthetic Ward spell 9.30c stages Shield's shape with), <c>removed</c> staged faceup by 9.30b's
        /// transition, The Archwizard unlocked by the lifecycle's own transition, and has committed
        /// Prepare Spells (or a "Cast the Ward" card with Prepare Spells next). <c>wishesAlreadyCast</c>
        /// is staged through the round's own count. The enemy (Blue) commits a Tier I card he can only
        /// Hold (or a strong Attack) and holds a Wall.
        /// </summary>
        internal sealed class WishRound
        {
            internal static readonly HeroId Caster = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly CardInstanceId Played = new CardInstanceId("wish-played");
            internal static readonly CardInstanceId NextCard = new CardInstanceId("wish-next");
            internal static readonly CardInstanceId EnemyPlayed = new CardInstanceId("wish-enemy-played");
            internal static readonly CardInstanceId EnemyNext = new CardInstanceId("wish-enemy-next");
            internal static readonly CardInstanceId EnemyWall = new CardInstanceId("wish-enemy-wall");
            private static readonly CardInstanceId Ultimate = new CardInstanceId("wish-the-archwizard");
            private static readonly CardInstanceId WardInstance = new CardInstanceId("wish-book-ward");
            private static readonly ActionDefinitionId CastingPrimary = GydionCardContent.PrimarySkill;

            private readonly MatchContentCatalogs catalogs;
            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();
            private readonly MatchParticipants participants = StagedBoardParticipants.Create();

            internal WishRound(
                IEnumerable<CardDefinitionId> removed = null,
                HexCoordinate? enemyAt = null,
                bool enemyStrikes = false,
                int wishesAlreadyCast = 0,
                int lives = 4,
                bool ultimateUnlocked = true,
                bool firstCastsTheWard = false)
            {
                catalogs = CreateCatalogs();
                Definitions = GydionCardContent.SpellDefinitions
                    .Concat(GydionCardContent.ArchwizardCardDefinitions)
                    .Concat(new[] { WardSpell, CastWardCard, EnemyHold, EnemyStrike, EnemyWallDefinition })
                    .ToArray();

                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Played,
                        (firstCastsTheWard ? CastWardCard : GydionCardContent.PrepareSpellsDefinition).Reference, Caster),
                    CardInstance.CreateInHand(EnemyPlayed, (enemyStrikes ? EnemyStrike : EnemyHold).Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, EnemyWallDefinition.Reference, Enemy)
                };
                if (firstCastsTheWard)
                {
                    cards.Add(CardInstance.CreateInHand(NextCard, GydionCardContent.PrepareSpellsDefinition.Reference, Caster));
                    cards.Add(CardInstance.CreateInHand(EnemyNext, EnemyHold.Reference, Enemy));
                    cards.Add(CardInstance.CreateInSpellbook(WardInstance, WardSpell.Reference, Caster));
                }

                var spells = GydionCardContent.SpellDefinitions
                    .Select(spell => CardInstance.CreateInSpellbook(
                        new CardInstanceId("wish-book-" + spell.Id.Value), spell.Reference, Caster))
                    .ToArray();
                foreach (var spell in removed ?? Enumerable.Empty<CardDefinitionId>())
                {
                    Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(
                            spells.Single(card => card.DefinitionReference.DefinitionId.Equals(spell))), Is.True,
                        "the staged removal is 9.30b's own transition");
                }

                cards.AddRange(spells);
                var ultimate = CardInstance.CreateInDeck(
                    Ultimate, GydionCardContent.TheArchwizardDefinition.Reference, Caster);
                if (ultimateUnlocked)
                {
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                        "the unlock is the lifecycle's own transition");
                }

                cards.Add(ultimate);
                Cards = cards.ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("gydion-wish"), 1, Disk(5)),
                    new[]
                    {
                        new HeroOccupancy(Caster, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, enemyAt ?? new HexCoordinate(5, 0))
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                cardActions = new CardActionResolutionAuthority(catalogs, Board, markers);
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(cardActions, Board, new ActiveEffectLifecycle(), markers,
                        null, null, null, new TeamLifeState(participants, lives), null, null));
                for (var wish = 0; wish < wishesAlreadyCast; wish++)
                {
                    Assert.That(Round.TryAdvanceTeamVictoryTally(Caster, GydionCardContent.WishesToWin), Is.True,
                        "the staged count is the round's own");
                }

                Accept(Round.TryCommitCard(Caster, Played));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            private WishRound(WishRound template, string snapshot)
            {
                catalogs = template.catalogs;
                Definitions = template.Definitions;
                var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot, Definitions,
                    new[] { template.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
                cardActions = restored.CardActions;
            }

            private readonly CardActionResolutionAuthority cardActions;

            internal static MatchContentCatalogs CreateCatalogs()
            {
                // Since Gydion's dealing (9.30i) the build registers his spells' contracts; the fixture
                // adds only the synthetic spells' naming steps and the Ward's Active effect (9.30c),
                // Shield's shape.
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(SyntheticSpellbook.CastingHandlers())),
                    defaults.Components,
                    defaults.TriggeredEffects);
            }

            internal static IReadOnlyList<HexCoordinate> Disk(int radius) =>
                (from q in Enumerable.Range(-radius, 2 * radius + 1)
                 from r in Enumerable.Range(-radius, 2 * radius + 1)
                 where Math.Abs(q + r) <= radius
                 select new HexCoordinate(q, r)).ToArray();

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn?.PendingEffectChoice;
            internal TeamId CasterTeam => participants.GetPlayerForHero(Caster).TeamId;
            internal TeamId EnemyTeam => participants.GetPlayerForHero(Enemy).TeamId;

            internal WishRound Restore(string snapshot) => new WishRound(this, snapshot);

            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal CardInstance Spell(CardDefinitionId spell) =>
                Cards.Single(card => card.OwnerId.Equals(Caster) && card.DefinitionReference.DefinitionId.Equals(spell));

            internal CardInstance Ward => Card(WardInstance);

            internal int Coins(HeroId hero) => cardActions.CoinLedger == null ? 0 : cardActions.CoinLedger.GetCoins(hero);

            internal IReadOnlyList<PlacedToken> Familiars() =>
                Board.PlacedTokens.Where(token => token.Definition.Id.Equals(GydionComponentContent.FamiliarId)).ToArray();

            internal bool IsOffered => OfferOn(Played).IsAvailable;

            internal void StartCasting() => StartCastingOff(Played);

            internal void StartCastingOff(CardInstanceId card) =>
                Accept(Round.TryStartSkill(new ActionResolutionId("cast-" + card.Value), OfferOn(card)));

            internal void ChooseArm(EffectNodeId arm)
            {
                Assert.That(Choice, Is.Not.Null, "a choice stands");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(arm))));
            }

            internal IReadOnlyList<HexCoordinate> OfferedSpaces()
            {
                Assert.That(Choice, Is.Not.Null, "a destination is asked");
                return Choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate)
                    .ToArray();
            }

            internal void PickSpace(HexCoordinate space)
            {
                Assert.That(Choice, Is.Not.Null, "a destination is asked");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    invocation.Candidate.Value.Coordinate.Equals(space))));
            }

            internal void Confirm()
            {
                Assert.That(Choice?.ConfirmInvocation, Is.Not.Null, "the choice can be confirmed");
                Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));
            }

            internal void TargetEnemy()
            {
                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "the cast Attack asks for its target");
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
            }

            internal void EnemyDeclinesTheDefense()
            {
                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null, "the enemy is asked to defend");
                Accept(Round.TryAnswerDefenseResponse(
                    DefenseResponseChoiceCommand.Decline(defense.ResolutionId, defense.ChoiceId)));
            }

            /// <summary>The caster's turn ends; the enemy's Attack 20 then defeats him undefended.</summary>
            internal void EnemyDefeatsTheCaster()
            {
                ReleaseIfAsked();
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
                var strike = Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(EnemyPlayed) && offer.ActionDefinitionId.Equals(EnemyStrikeId));
                Accept(Round.TryStartAttack(new ActionResolutionId("enemy-strike"), strike));
                var target = Turn.PendingAttackTargetChoice;
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Caster)));
                var defense = Turn.PendingDefenseChoice;
                if (defense != null)
                {
                    Accept(Round.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.Decline(defense.ResolutionId, defense.ChoiceId)));
                }
            }

            /// <summary>The caster's turn ends, the enemy Holds, and the round's next turn is committed.</summary>
            internal void NextTurn()
            {
                ReleaseIfAsked();
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
                Accept(Round.TryPerformHold(Turn.SelectedActor.CardInstanceId));
                ReleaseIfAsked();
                Accept(Round.TryCommitCard(Caster, NextCard));
                Accept(Round.TryCommitCard(Enemy, EnemyNext));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            /// <summary>
            /// Every later actor Holds or releases, every later turn is passed by a hero with nothing to
            /// commit, and the end of the round is advanced until the next round awaits commitments.
            /// </summary>
            internal void FinishTheRound()
            {
                var heroes = new[] { Caster, Enemy };
                var startedIn = Round.RoundIndex;
                for (var guard = 0; Round.RoundIndex == startedIn && guard < 128; guard++)
                {
                    switch (Round.Phase)
                    {
                        case RoundResolutionPhase.ResolvingTurn:
                            if (Turn.PendingTurnRelease != null)
                            {
                                Accept(Round.TryReleaseTurn());
                            }
                            else if (Turn.PendingHeroRespawnChoice != null)
                            {
                                Accept(Round.TryAnswerHeroRespawn(Turn.PendingHeroRespawnChoice.DeclineInvocation));
                            }
                            else
                            {
                                Accept(Round.TryPerformHold(Turn.SelectedActor.CardInstanceId));
                            }

                            break;
                        case RoundResolutionPhase.AwaitingCommitments:
                            foreach (var hero in heroes)
                            {
                                if (Round.Phase != RoundResolutionPhase.AwaitingCommitments) break;
                                if (Round.TryPass(hero).Accepted) continue;
                                foreach (var card in Cards.Where(card =>
                                             card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand))
                                {
                                    if (Round.TryCommitCard(hero, card.Id).Accepted) break;
                                }
                            }

                            break;
                        case RoundResolutionPhase.EndOfRound:
                            Accept(Round.TryAdvanceEndOfRound());
                            break;
                        default:
                            Assert.Fail("The round stopped at " + Round.Phase);
                            break;
                    }
                }

                Assert.That(Round.RoundIndex, Is.EqualTo(startedIn + 1), "the round finished");
            }

            private void ReleaseIfAsked()
            {
                if (Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            private ActionOffer OfferOn(CardInstanceId card) =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(card) && offer.ActionDefinitionId.Equals(CastingPrimary));

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }

            private static readonly ActionDefinitionId EnemyStrikeId = new ActionDefinitionId("primary-attack");

            /// <summary>The Ward of 9.30c - "This round: +1 Defense", ended when its spell changes state.</summary>
            private static CardDefinition WardSpell =>
                SyntheticSpellbook.CastingSpellDefinitions.Single(spell => spell.Id.Equals(SyntheticSpellbook.WardSpellId));

            private static readonly EffectNodeId CastWardArm = new EffectNodeId("cast-the-ward");

            private static readonly CardDefinition CastWardCard = new CardDefinition(
                new CardDefinitionId("wish-test-cast-ward"), "rev-1", "TEST - Cast the Ward", new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(CastingPrimary, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        new EffectDefinition(new EffectDefinitionId("wish-test-cast-ward-skill"), "rev-1", CastWardArm,
                            SyntheticSpellbookCapabilities.CastArm(CastWardArm, SyntheticSpellbook.WardSpellInBookV1)))
                });

            /// <summary>A Tier I card, so p.13 prices the enemy at one Life.</summary>
            private static readonly CardDefinition EnemyHold = new CardDefinition(
                new CardDefinitionId("wish-test-enemy-hold"), "rev-1", "TEST - Hold",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.I, PrintedValueChannel.None),
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            private static readonly CardDefinition EnemyStrike = new CardDefinition(
                new CardDefinitionId("wish-test-enemy-strike"), "rev-1", "TEST - Strike",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(EnemyStrikeId, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 20) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default)
                });

            private static readonly CardDefinition EnemyWallDefinition = new CardDefinition(
                new CardDefinitionId("wish-test-enemy-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });
        }
    }
}
