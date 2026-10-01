using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30c: casting a spell from a spellbook, and refilling the book (Prepare Spells). A
    /// cast names its spell by definition, removes it faceup "as the spell is cast", and the caster
    /// performs its printed primary through the borrow path with the spell's values and the caster's
    /// items (GY-Q3). "Choose and cast one" greys the bullet whose spell is not in the book. A refill
    /// returns every removed spell and ends each returned spell's Active effects (GY-05 designer).
    /// Proven on <see cref="SyntheticSpellbook"/>'s acting spells on a staged round; no Gydion face is
    /// registered.
    /// </summary>
    public sealed class SpellCastTests
    {
        private static readonly CardDefinitionId Attack = SyntheticSpellbook.AttackSpellId;
        private static readonly CardDefinitionId Movement = SyntheticSpellbook.MovementSpellId;
        private static readonly CardDefinitionId Ward = SyntheticSpellbook.WardSpellId;

        // -------------------------------------------------------------------------------------------
        // Casting
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ACast_RemovesTheSpellFaceup_AndTheCasterPerformsItsPrimaryWithHisItems()
        {
            var cast = new Cast(SyntheticSpellbook.CantripId, item: PrintedValueChannel.Movement);
            Assert.That(cast.IsOffered, Is.True, "every spell is in the book");

            cast.StartCasting();
            Assert.That(cast.Choice?.NodeId.Value, Is.EqualTo("choose-and-cast-one"));
            Assert.That(cast.Choice.ChooserId, Is.EqualTo(Cast.Caster));
            cast.ChooseArm(SyntheticSpellbook.CastMovementArm);

            var spell = cast.Spell(Movement);
            Assert.That(spell.State, Is.EqualTo(new CardStateFacts(
                    CardLocation.RemovedFromSpellbook, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)),
                "\"As the spell is cast, remove it from the spellbook faceup.\"");
            Assert.That(cast.Spell(Attack).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(cast.Spell(Ward).State.Location, Is.EqualTo(CardLocation.Spellbook));

            var move = cast.Turn.PendingMovementChoice;
            Assert.That(move, Is.Not.Null, "a cast Movement spell is a Movement action (GY-Q3)");
            Assert.That(move.MovingHeroId, Is.EqualTo(Cast.Caster), "performed by the hero casting the spell");
            Assert.That(move.RemainingAllowance, Is.EqualTo(SyntheticSpellbook.MovementSpellValue + 1),
                "the spell's printed Movement and the caster's Movement item");
            Assert.That(cast.Turn.PendingMovementResolution.SourceCardInstanceId, Is.EqualTo(spell.Id),
                "the Movement is performed off the spell card");

            var destination = move.LegalDestinations.First(value => value.Destination.Equals(new HexCoordinate(0, -2))).Destination;
            cast.MoveTo(destination);
            cast.StopMovingIfAsked();
            Assert.That(cast.Board.TryGetHeroCoordinate(Cast.Caster, out var at) && at.Equals(destination), Is.True);
            Assert.That(cast.Card(Cast.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved),
                "the played card resolves after the spell's action");
            Assert.That(spell.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                "the borrowed spell does not move again: the cast moved it, the borrow never does");
            Assert.That(cast.BasicActionsOff(spell.Id), Is.Empty,
                "the Red Tier I spell is non-basic, so its action is not a basic action (its own classification)");
        }

        [Test]
        public void ACastAttack_IsDefendedThroughPage14_AsAnOrdinaryAttackOffTheSpell()
        {
            var cast = new Cast(SyntheticSpellbook.CantripId, item: PrintedValueChannel.Attack);
            Assert.That(cast.PlayedOfferKind, Is.EqualTo(ActionKind.Skill), "the played card is a Skill action (GY-Q3)");

            cast.StartCasting();
            cast.ChooseArm(SyntheticSpellbook.CastAttackArm);
            var spell = cast.Spell(Attack);
            Assert.That(spell.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));

            var target = cast.Turn.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, "a cast Attack opens p.14's target step");
            Assert.That(target.ChooserId, Is.EqualTo(Cast.Caster));
            Assert.That(target.LegalTargetHeroIds, Is.EqualTo(new[] { Cast.Enemy }));
            cast.TargetEnemy();

            var defense = cast.Turn.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, "the defender defends a cast Attack as any Attack (GY-Q3)");
            Assert.That(defense.ChooserId, Is.EqualTo(Cast.Enemy));
            Assert.That(defense.AttackerId, Is.EqualTo(Cast.Caster));
            Assert.That(defense.SourceCardInstanceId, Is.EqualTo(spell.Id), "the Defense window names the cast spell");
            Assert.That(defense.SourceDefinitionReference, Is.EqualTo(spell.DefinitionReference));
            Assert.That(PublicCardZoneQuery.IsPubliclyVisible(spell), Is.True,
                "and every seat can read it: the removed pile is an open zone (9.30b)");
            Assert.That(defense.AttackTotal, Is.EqualTo(SyntheticSpellbook.AttackSpellValue + 1),
                "the spell's printed Attack and the caster's Attack item");
            Assert.That(defense.LegalDefenseOffers.Select(offer => offer.SourceCardInstanceId),
                Has.Member(Cast.EnemyWall));

            var atDefense = cast.Snapshot();
            var restored = cast.Restore(atDefense);
            Assert.That(restored.Turn.PendingDefenseChoice?.SourceCardInstanceId, Is.EqualTo(spell.Id),
                "the cast Attack's Defense window survives a restore");
            restored.DefendWithTheWall();
            cast.DefendWithTheWall();
            Assert.That(restored.Snapshot(), Is.EqualTo(cast.Snapshot()),
                "and the restored borrow finishes in the same authoritative bytes");
            Assert.That(cast.Card(Cast.EnemyWall).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(cast.Board.TryGetHeroCoordinate(Cast.Enemy, out _), Is.True, "the Wall blocks it");
            Assert.That(cast.Card(Cast.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));

            Assert.That(cast.BasicActionsOff(spell.Id).Select(action => action.Kind), Is.EqualTo(new[] { ActionKind.Attack }),
                "the Gold spell is basic, so a cast of it is a basic Attack performed (its own classification)");
            Assert.That(PlayedAttackCardThisTurnQuery.HasPlayed(Cast.Caster, 1, 1, cast.Round.TurnSlots,
                    cast.Definitions, cast.Cards), Is.False,
                "\"played an Attack card\" reads the played card, a Skill (GY-Q3)");
        }

        /// <summary>
        /// GY-Q12, the user's answer (2026-10-01, "Yes, castable"): a cast is playable whenever its spell
        /// is in the book, even if the spell's action then does nothing, because the faceup removal
        /// changes the game - Sunburst, Dimension Door and Midas' Touch count removed spells. W7's grey
        /// (a borrowed primary that can do nothing) therefore does not apply to a cast.
        /// </summary>
        [Test]
        public void ACastWhoseSpellCannotAct_IsStillPlayable_AndRemovesTheSpellFaceupPerformingNothing_GyQ12()
        {
            var evocation = new Cast(SyntheticSpellbook.EvocationId, enemyBeside: false);
            Assert.That(evocation.IsOffered, Is.True,
                "GY-Q12 (user): \"Cast the Attack spell\" with nobody in reach is offered - casting removes the spell");
            Assert.That(evocation.Board.TryGetHeroCoordinate(Cast.Enemy, out var enemyBefore), Is.True);
            Assert.That(evocation.Board.TryGetHeroCoordinate(Cast.Caster, out var casterBefore), Is.True);

            evocation.StartCasting();
            Assert.That(evocation.Spell(Attack).State, Is.EqualTo(new CardStateFacts(
                    CardLocation.RemovedFromSpellbook, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)),
                "the spell is cast: removed from the book faceup");
            Assert.That(evocation.Turn.PendingAttackTargetChoice, Is.Null, "and its Attack reaches nobody");
            Assert.That(evocation.Turn.PendingDefenseChoice, Is.Null);
            Assert.That(evocation.Turn.PendingEffectChoice, Is.Null);
            Assert.That(evocation.Board.TryGetHeroCoordinate(Cast.Enemy, out var enemyAfter) &&
                enemyAfter.Equals(enemyBefore), Is.True, "nobody is touched");
            Assert.That(evocation.Board.TryGetHeroCoordinate(Cast.Caster, out var casterAfter) &&
                casterAfter.Equals(casterBefore), Is.True);
            Assert.That(evocation.Spell(Movement).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(evocation.Spell(Ward).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(evocation.Card(Cast.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));

            var onlyTheAttackLeft = new Cast(SyntheticSpellbook.CantripId, enemyBeside: false,
                removed: new[] { Movement, Ward });
            Assert.That(onlyTheAttackLeft.IsOffered, Is.True,
                "a \"Choose and cast one\" whose only in-book spell cannot act is offered too");

            var cantrip = new Cast(SyntheticSpellbook.CantripId, enemyBeside: false);
            cantrip.StartCasting();
            Assert.That(cantrip.Choice.UnavailableCandidates, Is.Empty,
                "every in-book spell's bullet is available, the one that cannot act included");
            cantrip.ChooseArm(SyntheticSpellbook.CastAttackArm);
            Assert.That(cantrip.Spell(Attack).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(cantrip.Turn.PendingAttackTargetChoice, Is.Null);
            Assert.That(cantrip.Card(Cast.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void ARemovedSpellsBullet_IsGreyed_AndItsAnswerIsRefused()
        {
            var cast = new Cast(SyntheticSpellbook.CantripId, removed: new[] { Attack });
            Assert.That(cast.IsOffered, Is.True);
            cast.StartCasting();

            var choice = cast.Choice;
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId), Is.EqualTo(new[]
            {
                SyntheticSpellbook.CastAttackArm, SyntheticSpellbook.CastMovementArm, SyntheticSpellbook.CastWardArm
            }), "every printed bullet is shown");
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { SyntheticSpellbook.CastAttackArm }), "the removed spell's bullet is greyed");
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { SyntheticSpellbook.CastMovementArm, SyntheticSpellbook.CastWardArm }));

            var before = cast.Snapshot();
            var forged = new EffectChoiceCommand(choice.SelectInvocations[0].ResolutionId,
                choice.SelectInvocations[0].ChoiceId, choice.SelectInvocations[0].Kind,
                EffectChoiceCandidate.Option(SyntheticSpellbook.CastAttackArm));
            Assert.That(cast.Round.TryAnswerEffectChoice(forged).Accepted, Is.False,
                "an answer naming the greyed bullet is refused");
            Assert.That(cast.Snapshot(), Is.EqualTo(before), "and changes nothing");

            var evocation = new Cast(SyntheticSpellbook.EvocationId, removed: new[] { Attack });
            Assert.That(evocation.IsOffered, Is.False, "\"Cast the Attack spell\" with that spell removed is greyed");
        }

        [Test]
        public void ACardWhoseEverySpellIsRemoved_IsGreyed()
        {
            var cast = new Cast(SyntheticSpellbook.CantripId, removed: new[] { Attack, Movement, Ward });
            Assert.That(cast.IsOffered, Is.False, "every bullet is greyed, so the whole card is");
            Assert.That(cast.Round.TryPerformHold(Cast.Played).Accepted, Is.True, "and p.12's Hold stays");

            var oneLeft = new Cast(SyntheticSpellbook.CantripId, removed: new[] { Attack, Movement });
            Assert.That(oneLeft.IsOffered, Is.True, "one spell in the book is enough");
        }

        // -------------------------------------------------------------------------------------------
        // Refilling
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ARefill_ReturnsEveryRemovedSpell_AndEndsTheReturnedSpellsActiveEffect()
        {
            var cast = new Cast(SyntheticSpellbook.CantripId, removed: new[] { Movement },
                next: SyntheticSpellbook.PrepareSpellsId);
            cast.StartCasting();
            cast.ChooseArm(SyntheticSpellbook.CastWardArm);

            var ward = cast.Spell(Ward);
            var effect = cast.Round.Timing.Effects.Single(value => value.Definition.Equals(SyntheticSpellbook.WardDefinition));
            Assert.That(effect.SourceCardInstanceId, Is.EqualTo(ward.Id),
                "a cast's Active effect is stamped with the SPELL, the borrowed card, not the played card");
            Assert.That(effect.ControllerHeroId, Is.EqualTo(Cast.Caster));
            Assert.That(effect.EndedAt, Is.Null);
            Assert.That(ward.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));

            cast.NextTurn();
            Assert.That(cast.Round.Timing.Effects.Single(value => value.Id.Equals(effect.Id)).EndedAt, Is.Null,
                "\"This round\": the Ward still stands on the next turn");
            Assert.That(cast.IsOfferedOn(Cast.NextCard), Is.True);
            cast.StartCastingOff(Cast.NextCard);

            foreach (var spell in new[] { Attack, Movement, Ward })
            {
                Assert.That(cast.Spell(spell).State, Is.EqualTo(CardStateFacts.InitialInSpellbook()),
                    spell + ": \"Add all spell cards to the spellbook\" - facedown, as setup put it");
            }

            var ended = cast.Round.Timing.Effects.Single(value => value.Id.Equals(effect.Id));
            Assert.That(ended.EndedAt, Is.EqualTo(TimingCheckpoint.SourceCardStateChanged),
                "\"Card changes state - becomes facedown\" (GY-05 designer): the Ward ends with its spell's return");
            Assert.That(cast.Card(Cast.NextCard).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>
        /// GY-Q7, the user's answer (2026-10-01, "Always playable"), against the recommendation that
        /// greyed it: Prepare Spells is offered with nothing removed, and resolves returning nothing.
        /// </summary>
        [Test]
        public void AnEmptyRefill_IsAlwaysPlayable_AndResolvesChangingNoSpell_GyQ7()
        {
            var empty = new Cast(SyntheticSpellbook.PrepareSpellsId);
            Assert.That(empty.IsOffered, Is.True,
                "GY-Q7 (user): Prepare Spells is always playable, even with nothing removed");

            empty.StartCasting();
            foreach (var spell in new[] { Attack, Movement, Ward })
            {
                Assert.That(empty.Spell(spell).State, Is.EqualTo(CardStateFacts.InitialInSpellbook()),
                    spell + " stays in the book, untouched");
            }

            Assert.That(empty.Round.Timing.Effects, Is.Empty, "nothing is created or cancelled");
            Assert.That(empty.Turn.PendingEffectChoice, Is.Null, "nothing is asked");
            Assert.That(empty.Card(Cast.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved),
                "and the card resolves");
        }

        // -------------------------------------------------------------------------------------------
        // Pauses
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ACastChoice_RestoresMidChoice_AndItsAnswerCrossesTheWire()
        {
            var live = new Cast(SyntheticSpellbook.CantripId, removed: new[] { Ward });
            live.StartCasting();
            var pending = live.Choice;
            var bytes = live.Snapshot();

            var restored = live.Restore(bytes);
            Assert.That(restored.Snapshot(), Is.EqualTo(bytes), "byte-identical round trip");
            var resumed = restored.Choice;
            Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId), "the same question stands");
            Assert.That(resumed.Candidates, Is.EqualTo(pending.Candidates));
            Assert.That(resumed.UnavailableCandidates, Is.EqualTo(pending.UnavailableCandidates),
                "and the greying is re-derived from the restored book");

            var answer = pending.SelectInvocations.Single(invocation =>
                invocation.Candidate.OptionNodeId.Equals(SyntheticSpellbook.CastAttackArm));
            Assert.That(NetworkCommandCodec.TryDecode(
                    NetworkCommandCodec.Encode(DirectorCommand.AnswerEffectChoice(answer)), out var decoded),
                Is.True);
            Assert.That(decoded, Is.EqualTo(DirectorCommand.AnswerEffectChoice(answer)),
                "the answer names a bullet, a shape the wire already carries: no wire move");

            live.ChooseArm(SyntheticSpellbook.CastAttackArm);
            restored.ChooseArm(SyntheticSpellbook.CastAttackArm);
            Assert.That(restored.Snapshot(), Is.EqualTo(live.Snapshot()),
                "the restored pause replays to the same authoritative bytes");
            Assert.That(restored.Spell(Attack).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(53));
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(33));
        }

        // -------------------------------------------------------------------------------------------
        // Offer against outcome (lesson 18)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryOfferedCast_DoesWhatItsBulletSays_OnEveryStagedBook()
        {
            var books = new[]
            {
                Array.Empty<CardDefinitionId>(),
                new[] { Attack },
                new[] { Movement, Ward },
                new[] { Attack, Ward },
                new[] { Attack, Movement, Ward }
            };

            foreach (var removed in books)
            {
                foreach (var beside in new[] { true, false })
                {
                    var position = "removed [" + string.Join(", ", removed.Select(id => id.Value)) + "], enemy " +
                        (beside ? "beside" : "far");
                    bool InBook(CardDefinitionId spell) => !removed.Contains(spell);

                    var cantrip = new Cast(SyntheticSpellbook.CantripId, beside, removed);
                    Assert.That(cantrip.IsOffered, Is.EqualTo(new[] { Attack, Movement, Ward }.Any(InBook)),
                        position + ": the Cantrip is offered exactly when a spell it names is in the book (GY-Q12)");
                    if (cantrip.IsOffered)
                    {
                        cantrip.StartCasting();
                        var pause = cantrip.Snapshot();
                        foreach (var (arm, spell) in Arms)
                        {
                            var greyed = cantrip.Choice.UnavailableCandidates.Any(candidate =>
                                candidate.OptionNodeId.Equals(arm));
                            Assert.That(greyed, Is.EqualTo(!InBook(spell)),
                                position + ": " + arm + " is greyed exactly when its spell is removed");
                            if (greyed)
                            {
                                continue;
                            }

                            var taken = cantrip.Restore(pause);
                            taken.ChooseArm(arm);
                            Assert.That(taken.Snapshot(), Is.Not.EqualTo(pause), position + ": " + arm + " changes the game");
                            Assert.That(taken.Spell(spell).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                                position + ": " + arm + " casts its own spell");
                            Assert.That(new[] { Attack, Movement, Ward }.Where(other => !other.Equals(spell))
                                    .All(other => taken.Spell(other).State.Location ==
                                        (InBook(other) ? CardLocation.Spellbook : CardLocation.RemovedFromSpellbook)),
                                Is.True, position + ": " + arm + " casts nothing else");
                            AssertThePrimaryStarted(taken, spell, beside, position);
                        }
                    }

                    var evocation = new Cast(SyntheticSpellbook.EvocationId, beside, removed);
                    Assert.That(evocation.IsOffered, Is.EqualTo(InBook(Attack)),
                        position + ": \"Cast the Attack spell\" is offered exactly when that spell is in the book (GY-Q12)");
                    if (evocation.IsOffered)
                    {
                        evocation.StartCasting();
                        Assert.That(evocation.Spell(Attack).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
                        AssertThePrimaryStarted(evocation, Attack, beside, position);
                    }

                    if (removed.Length > 0)
                    {
                        var prepare = new Cast(SyntheticSpellbook.PrepareSpellsId, beside, removed);
                        Assert.That(prepare.IsOffered, Is.True, position + ": a refill with spells to return is offered");
                        prepare.StartCasting();
                        Assert.That(new[] { Attack, Movement, Ward }.Select(spell => prepare.Spell(spell).State.Location),
                            Is.All.EqualTo(CardLocation.Spellbook), position + ": and returns every one");
                    }
                }
            }
        }

        private static void AssertThePrimaryStarted(Cast cast, CardDefinitionId spell, bool enemyBeside, string position)
        {
            if (spell.Equals(Attack))
            {
                Assert.That(cast.Turn.PendingAttackTargetChoice != null, Is.EqualTo(enemyBeside),
                    position + ": the Attack asks for its target exactly when one is in reach");
            }
            else if (spell.Equals(Movement))
            {
                Assert.That(cast.Turn.PendingMovementChoice?.RemainingAllowance,
                    Is.EqualTo(SyntheticSpellbook.MovementSpellValue), position + ": the Movement starts");
            }
            else
            {
                Assert.That(cast.Round.Timing.Effects.Count(effect =>
                        effect.SourceCardInstanceId.Equals(cast.Spell(Ward).Id)), Is.EqualTo(1),
                    position + ": the Ward stands");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Delegation (HA-Q3, RZ-Q14)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void UnderTheUltimateTrick_TheDecidingSeatIsOfferedTheInBookSpellsByName_AndNoBookCard()
        {
            var cast = new Cast(SyntheticSpellbook.CantripId, removed: new[] { Attack }, delegated: true);
            Assert.That(DelegatedActionQuery.DecidingHeroFor(cast.Definitions, cast.Cards, cast.Board.Occupancies,
                Cast.Caster), Is.EqualTo(Cast.Enemy), "the enemy's Ultimate decides the caster's action");

            cast.StartCasting();
            var choice = cast.Choice;
            Assert.That(SeatProjectionFactory.DelegatedDecisionFamilies, Has.Member(PendingDecisionFamilies.Effect),
                "the cast's choice is an effect choice, which the delegation hands to the deciding seat whole (8.71b)");
            Assert.That(choice.Candidates.Select(candidate => candidate.Kind), Is.All.EqualTo(EffectChoiceCandidateKind.Option),
                "the deciding seat chooses a bullet - a spell by name - never a card");

            var labels = SyntheticSpellbook.DecisionLabels().ToDictionary(label => label.NodeId, label => label.Label.Chosen);
            var offered = choice.SelectInvocations.Select(invocation => labels[invocation.Candidate.OptionNodeId]).ToArray();
            Assert.That(offered, Is.EqualTo(new[]
            {
                SyntheticSpellbook.CastLabel(Movement), SyntheticSpellbook.CastLabel(Ward)
            }), "exactly the spells still in the book are offered, named by their own printed names");

            var bookIds = cast.Cards.Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => card.Id.Value).ToArray();
            Assert.That(bookIds, Has.Length.EqualTo(2));
            var shown = string.Join("|", choice.Candidates.Concat(choice.UnavailableCandidates)
                .Select(candidate => candidate.ToString()));
            foreach (var id in bookIds)
            {
                Assert.That(shown, Does.Not.Contain(id), "no facedown spell's identity is in the choice");
            }

            cast.ChooseArm(SyntheticSpellbook.CastWardArm);
            Assert.That(cast.Spell(Ward).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                "the deciding seat's answer casts the caster's spell");
        }

        // -------------------------------------------------------------------------------------------
        // Labels
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryCastBullet_HasALabelDerivedFromItsSpell()
        {
            var catalogs = Cast.CreateCatalogs();
            var cantrip = SyntheticSpellbook.CastingCardDefinitions.Single(card => card.Id.Equals(SyntheticSpellbook.CantripId));
            Assert.That(DecisionLabelRequirementQuery.For(cantrip, catalogs).Select(row => row.NodeId),
                Is.EquivalentTo(SyntheticSpellbook.DecisionLabels().Select(label => label.NodeId)),
                "the controls the Cantrip raises are exactly the labelled bullets");
            Assert.That(SyntheticSpellbook.DecisionLabels().Select(label => label.Label.Chosen), Is.EqualTo(new[]
            {
                "Cast TEST - Attack Spell", "Cast TEST - Movement Spell", "Cast TEST - Ward Spell"
            }));
            foreach (var single in new[] { SyntheticSpellbook.EvocationId, SyntheticSpellbook.PrepareSpellsId })
            {
                Assert.That(DecisionLabelRequirementQuery.For(
                        SyntheticSpellbook.CastingCardDefinitions.Single(card => card.Id.Equals(single)), catalogs),
                    Is.Empty, single + " asks nothing");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The location audit 9.30b left open
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheGuessDomain_NeverCountsASpell()
        {
            var owner = Cast.Caster;
            var blue = new CardDefinition(new CardDefinitionId("audit-blue"), "rev-1", "TEST - Blue", new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver));
            var cards = SyntheticSpellbook.CreateSpellbookInstances(owner, "audit")
                .Concat(new[] { CardInstance.CreateInHand(new CardInstanceId("audit-hand"), blue.Reference, owner) })
                .ToArray();
            var definitions = SyntheticSpellbook.Definitions.Concat(new[] { blue }).ToArray();
            var context = new EffectCapabilityContext(
                new EffectNodeId("audit"), EffectFailurePolicy.Mandatory, Cast.Enemy, Cast.Enemy,
                new CardInstanceId("audit-source"),
                new CardDefinitionReference(new CardDefinitionId("audit-source"), "rev-1"),
                new ActionDefinitionId("audit-action"), new Dictionary<string, EffectValue>(), 1, 1,
                StagedBoardParticipants.Create(), null, GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                EffectCardState.Of(definitions, cards), ActionKind.Skill);

            Assert.That(SyntheticBorrowedActionCapabilities.GuessableColors(context, owner),
                Is.EqualTo(new[] { CardColor.Silver }),
                "a spell is never in a hand, so the Gold and Red spells lying facedown in the book make " +
                "neither colour guessable - only the Silver card in hand does");
        }

        [Test]
        public void AStagedHand_NamingASpell_IsRefusedAsNotDealt()
        {
            var configuration = MatchConfiguration.CreateForSpellbookFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(), configuration.Participants, map,
                configuration.TieBreakerState);
            var exception = Assert.Throws<SandboxStagingException>(() => SandboxStagingAuthority.Stage(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(Cast.Caster, hand: new[] { SyntheticSpellbook.BasicSpellId })
                }), map, setup, configuration));
            Assert.That(exception.Reason, Is.EqualTo(SandboxStagingRejectionReason.CardNotOwnedByHero),
                "a staged hand chooses among the cards a hero was dealt, and a spell is never dealt: " +
                exception.Message);
            Assert.That(configuration.CardInstances.Single(card =>
                    card.DefinitionReference.DefinitionId.Equals(SyntheticSpellbook.BasicSpellId)).State,
                Is.EqualTo(CardStateFacts.InitialInSpellbook()), "and the spell stays in the book");
        }

        // -------------------------------------------------------------------------------------------
        // The staged round
        // -------------------------------------------------------------------------------------------

        private static IEnumerable<(EffectNodeId Arm, CardDefinitionId Spell)> Arms => new[]
        {
            (SyntheticSpellbook.CastAttackArm, Attack),
            (SyntheticSpellbook.CastMovementArm, Movement),
            (SyntheticSpellbook.CastWardArm, Ward)
        };

        /// <summary>
        /// One round on a small disk: the caster (Red) owns the three acting spells in his book, with
        /// <c>removed</c> staged faceup through the lifecycle's own transition, and has committed the
        /// played casting card; the enemy (Blue) stands beside him or far away, has committed a card he
        /// can only Hold, and holds a Wall to defend with. Optionally the enemy owns an unlocked
        /// Ultimate declaring The Ultimate Trick's rule and has rewritten the caster's card (HA-Q3).
        /// </summary>
        internal sealed class Cast
        {
            internal static readonly HeroId Caster = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly CardInstanceId Played = new CardInstanceId("caster-played");
            internal static readonly CardInstanceId NextCard = new CardInstanceId("caster-next");
            internal static readonly CardInstanceId EnemyPlayed = new CardInstanceId("enemy-played");
            internal static readonly CardInstanceId EnemyNext = new CardInstanceId("enemy-next");
            internal static readonly CardInstanceId EnemyWall = new CardInstanceId("enemy-wall");
            private static readonly CardInstanceId EnemyUltimate = new CardInstanceId("enemy-ultimate");
            private static readonly HexCoordinate CasterAt = new HexCoordinate(0, 0);
            private static readonly HexCoordinate Beside = new HexCoordinate(1, 0);
            private static readonly HexCoordinate FarAway = new HexCoordinate(4, 0);

            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();
            private readonly MatchContentCatalogs catalogs;

            internal Cast(
                CardDefinitionId played,
                bool enemyBeside = true,
                IEnumerable<CardDefinitionId> removed = null,
                CardDefinitionId next = default,
                PrintedValueChannel item = PrintedValueChannel.None,
                bool delegated = false)
            {
                catalogs = CreateCatalogs();
                Definitions = SyntheticSpellbook.CastingSpellDefinitions
                    .Concat(SyntheticSpellbook.CastingCardDefinitions)
                    .Concat(new[] { EnemyHold, EnemyWallDefinition, DelegatingUltimate })
                    .ToArray();

                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Played, DefinitionOf(played).Reference, Caster),
                    CardInstance.CreateInHand(EnemyPlayed, EnemyHold.Reference, Enemy),
                    CardInstance.CreateInHand(EnemyNext, EnemyHold.Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, EnemyWallDefinition.Reference, Enemy)
                };
                if (next.IsDefined)
                {
                    cards.Add(CardInstance.CreateInHand(NextCard, DefinitionOf(next).Reference, Caster));
                }

                var spells = SyntheticSpellbook.CreateCastingSpellbookInstances(Caster, "caster");
                foreach (var spell in removed ?? Enumerable.Empty<CardDefinitionId>())
                {
                    Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(
                        spells.Single(card => card.DefinitionReference.DefinitionId.Equals(spell))), Is.True,
                        "the staged removal is 9.30b's own transition");
                }

                cards.AddRange(spells);
                if (delegated)
                {
                    var ultimate = CardInstance.CreateInDeck(EnemyUltimate, DelegatingUltimate.Reference, Enemy);
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                    cards.Add(ultimate);
                }

                Cards = cards.ToArray();
                var spaces = (from q in Enumerable.Range(-4, 9)
                              from r in Enumerable.Range(-4, 9)
                              where Math.Abs(q + r) <= 4
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("spell-cast"), 1, spaces),
                    new[]
                    {
                        new HeroOccupancy(Caster, CasterAt),
                        new HeroOccupancy(Enemy, enemyBeside ? Beside : FarAway)
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, StagedBoardParticipants.Create(), new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                if (item != PrintedValueChannel.None)
                {
                    Assert.That(Round.HeroItems.TryGrantItemFromTheBox(Caster, item).IsPlaced, Is.True);
                }

                Accept(Round.TryCommitCard(Caster, Played));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                if (delegated)
                {
                    Assert.That(CardLifecycleAuthority.TrySetPrintedInitiativeOverride(Card(Played), 11, EnemyPlayed),
                        Is.True, "Hurry up!'s rewrite, by 8.71a's own transition");
                }

                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            private Cast(Cast template, string snapshot)
            {
                catalogs = template.catalogs;
                Definitions = template.Definitions;
                var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot, Definitions,
                    new[] { template.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
            }

            internal static MatchContentCatalogs CreateCatalogs()
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(SyntheticSpellbook.CastingHandlers())),
                    defaults.Components,
                    defaults.TriggeredEffects);
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;

            /// <summary>Milestone 9.30h: every event this round has accepted, for the match log they derive.</summary>
            internal IReadOnlyList<RoundResolutionEvent> Events => events;

            internal Cast Restore(string snapshot) => new Cast(this, snapshot);

            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal CardInstance Spell(CardDefinitionId spell) =>
                Cards.Single(card => card.OwnerId.Equals(Caster) && card.DefinitionReference.DefinitionId.Equals(spell));

            internal bool IsOffered => IsOfferedOn(Played);

            internal bool IsOfferedOn(CardInstanceId card) => OfferOn(card).IsAvailable;

            internal ActionKind PlayedOfferKind
            {
                get
                {
                    var offer = OfferOn(Played);
                    return Definitions.Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind;
                }
            }

            internal void StartCasting() => StartCastingOff(Played);

            internal void StartCastingOff(CardInstanceId card) =>
                Accept(Round.TryStartSkill(new ActionResolutionId("cast-" + card.Value), OfferOn(card)));

            internal void ChooseArm(EffectNodeId arm)
            {
                Assert.That(Choice, Is.Not.Null, "a choice stands");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(arm))));
            }

            internal void TargetEnemy()
            {
                var target = Turn.PendingAttackTargetChoice;
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
            }

            internal void DefendWithTheWall()
            {
                var defense = Turn.PendingDefenseChoice;
                Accept(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, EnemyWall)));
            }

            internal void MoveTo(HexCoordinate space)
            {
                var move = Turn.PendingMovementChoice;
                Accept(Round.TryAnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                    move.ResolutionId, move.ChoiceId, space)));
            }

            internal void StopMovingIfAsked()
            {
                var move = Turn.PendingMovementChoice;
                if (move != null)
                {
                    Accept(Round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(move.ResolutionId, move.ChoiceId)));
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

            internal IReadOnlyList<PerformedBasicAction> BasicActionsOff(CardInstanceId card) =>
                events.Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                        value.TurnEvent.Kind == TurnResolutionEventKind.BasicActionPerformed &&
                        value.TurnEvent.BasicAction.SourceCardInstanceId.Equals(card))
                    .Select(value => value.TurnEvent.BasicAction)
                    .ToArray();

            private void ReleaseIfAsked()
            {
                if (Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            private ActionOffer OfferOn(CardInstanceId card) =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(card) &&
                    offer.ActionDefinitionId.Equals(SyntheticSpellbook.CastingPrimaryId));

            private CardDefinition DefinitionOf(CardDefinitionId id) => Definitions.Single(value => value.Id.Equals(id));

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }

            private static readonly CardDefinition EnemyHold = new CardDefinition(
                new CardDefinitionId("spell-cast-enemy-hold"), "rev-1", "TEST - Hold",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"),
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            private static readonly CardDefinition EnemyWallDefinition = new CardDefinition(
                new CardDefinitionId("spell-cast-enemy-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });

            /// <summary>An Ultimate declaring The Ultimate Trick's rule kind, as HanuUltimateTrickTests stages one.</summary>
            private static readonly CardDefinition DelegatingUltimate = new CardDefinition(
                new CardDefinitionId("spell-cast-delegating-ultimate"), "rev-1", "TEST - Delegating Ultimate",
                default, CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.UltimateCard(),
                Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.PermanentDeclaration(
                    "spell-cast-delegating-ultimate", DelegatedActionQuery.DelegatingRuleKind),
                CardSourceContent.NotSourced);
        }
    }
}
