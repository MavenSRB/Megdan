using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30h. The spellbook as the user approved it on 2026-10-01 (mockups A-D in
    /// <c>tmp/9-30-gydion-approved-mockups/</c>): the spell choice draws spell FACES in the decision
    /// window, a removed spell greyed and captioned REMOVED and a counting spell captioned with its
    /// values; Find Familiar's removal is a 0-3 pick of the spells still in the book, then CONFIRM; the
    /// SPELLBOOK drawer shows the owner's seat its book as faces and every other seat tier backs with
    /// counts, the removed pile faceup for all; the items strip's "SPELLBOOK n | m" chip opens it; a
    /// spell is a landscape card; the art is catalogued; and the log never names a spell back in the
    /// book to a seat that may not read it.
    ///
    /// <para>The spell choice is posed through <see cref="MatchFeaturePresenterTests.DelegatingReadModel"/>
    /// over a REAL pause: Gydion's real spells cast by a staged casting card on 9.30e's round fixture
    /// (nothing of his is registered until 9.30i), with the contract the session would publish for it.</para>
    /// </summary>
    public sealed class SpellbookPresentationTests
    {
        private static readonly HeroId Caster = GydionCountingSpellTests.GydionRound.Caster;
        private static readonly HeroId Owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        // -------------------------------------------------------------------------------------------
        // Approved mockup A: the spell choice
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSpellChoice_DrawsTheListedSpellsAsFaces_ARemovedOneGreyedAndCaptionedRemoved_ACountingOneCaptionedWithItsValues()
        {
            var round = MockupARound();
            var model = DecisionWindowPresenter.CreateViewModel(SpellChoice(round, ownerSeat: true), Caster);

            Assert.That(model, Is.Not.Null, "the caster owns the standing cast choice");
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded));
            Assert.That(model.Eyebrow, Is.EqualTo("SPELL CHOICE"), "mockup A's eyebrow");
            Assert.That(model.Question, Is.EqualTo("Choose and cast one spell in the spellbook."));
            Assert.That(model.ContextCards.Select(card => card.Card.CardInstanceId),
                Is.EqualTo(new[] { GydionCountingSpellTests.GydionRound.Played }),
                "the played card stands on the left");

            var spells = model.Controls.Where(control => control.Spell != null).ToArray();
            Assert.That(spells.Select(control => control.Label), Is.EqualTo(new[]
            {
                "FIND FAMILIAR",
                "FIREBALL - REMOVED",
                "SUNBURST - 5 OUT: ATTACK 5, RANGE 5"
            }), "in the card's printed order, as mockup A draws them. " + Describe(model));
            Assert.That(model.Controls, Has.Count.EqualTo(3), "and no worded bullet beside the faces");
            Assert.That(spells.Select(control => control.Spell.DefinitionReference.DefinitionId), Is.EqualTo(new[]
            {
                GydionCardContent.FindFamiliar, GydionCardContent.Fireball, GydionCardContent.Sunburst
            }), "each option is the face of the spell its bullet names");

            Assert.That(spells[0].IsAnswerable && spells[2].IsAnswerable, Is.True,
                "an in-book spell carries the contract's own command");
            Assert.That(spells[1].IsAnswerable, Is.False, "a spell not in the book answers nothing");
            Assert.That(spells[1].Spell.IsGreyed, Is.True, "and is its face greyed");
            Assert.That(spells[0].Spell.IsGreyed || spells[2].Spell.IsGreyed, Is.False);
        }

        /// <summary>
        /// The coordinator's 9.30h correction: a counting spell's caption is the EFFECTIVE value the cast
        /// will use, Gydion's items included - read on the rules side, never added by presentation. On
        /// b1546d48 this read "ATTACK 5, RANGE 5" and "RADIUS 5", the printed value and the count alone.
        /// </summary>
        [Test]
        public void TheSpellChoice_CaptionsACountingSpellWithTheValueTheCastWillUse_HisItemsIncluded()
        {
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.ChooseAndCastCard(
                    GydionCardContent.Sunburst, GydionCardContent.DimensionDoor, GydionCardContent.MidasTouch),
                removed: new[]
                {
                    GydionCardContent.MagicMissile, GydionCardContent.Shield, GydionCardContent.Fireball,
                    GydionCardContent.Suggestion, GydionCardContent.FindFamiliar
                },
                items: new[] { PrintedValueChannel.Attack, PrintedValueChannel.Range, PrintedValueChannel.Radius });
            round.StartCasting();

            foreach (var ownerSeat in new[] { true, false })
            {
                var model = DecisionWindowPresenter.CreateViewModel(SpellChoice(round, ownerSeat), Caster);
                Assert.That(model.Controls.Where(control => control.Spell != null).Select(control => control.Label),
                    Is.EqualTo(new[]
                    {
                        "SUNBURST - 5 OUT: ATTACK 6, RANGE 6",
                        "DIMENSION DOOR - 5 OUT: RADIUS 6",
                        "MIDAS' TOUCH - 5 OUT: 2 COINS"
                    }),
                    (ownerSeat ? "his seat" : "a seat that cannot read the book") +
                    ": the printed 0+ icon, his matching item and the count; coins take no item. " +
                    Describe(model));
            }
        }

        [Test]
        public void TheSpellChoice_OnASeatThatCannotReadTheBook_DrawsTheSameFacesAndCaptions_AndNamesNoBookCard()
        {
            // Hanu's delegated seat (RZ-Q14, 9.30c): offered the in-book NAMES and nothing else.
            var round = MockupARound();
            var projection = SpellChoice(round, ownerSeat: false);
            Assert.That(projection.Cards.Any(card => card.State.Location == CardLocation.Spellbook), Is.False);
            var model = DecisionWindowPresenter.CreateViewModel(projection, Caster);

            Assert.That(model.Controls.Where(control => control.Spell != null).Select(control => control.Label),
                Is.EqualTo(new[] { "FIND FAMILIAR", "FIREBALL - REMOVED", "SUNBURST - 5 OUT: ATTACK 5, RANGE 5" }),
                "the faces are definitions and the count is the public removed pile");
            var book = round.Cards.Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => card.Id.Value).ToArray();
            var drawn = string.Join("|", model.Controls.Select(control =>
                control.Label + " " + control.Spell?.AssistiveLabel + " " + control.Spell?.DefinitionReference));
            foreach (var id in book)
            {
                Assert.That(drawn, Does.Not.Contain(id), "no facedown spell's identity is drawn: " + id);
            }
        }

        [Test]
        public void ACastingCardsHover_ShowsItsTypedSpellsInPrintedOrder_WithRemovedSpellsFaded()
        {
            var round = MockupARound();
            var projection = SpellChoice(round, ownerSeat: true);
            var played = round.Card(GydionCountingSpellTests.GydionRound.Played);
            var card = CurrentRoundCards.TrayCard(
                projection, played, TrayCardGroup.Discarded, 0, false, false);

            Assert.That(card.CastableSpells.Select(spell => spell.DefinitionReference.DefinitionId),
                Is.EqualTo(new[]
                {
                    GydionCardContent.FindFamiliar,
                    GydionCardContent.Fireball,
                    GydionCardContent.Sunburst
                }), "typed cast arms preserve the card's printed order");
            Assert.That(card.CastableSpells.Select(spell => spell.IsGreyed),
                Is.EqualTo(new[] { false, true, false }),
                "Fireball is in the public removed pile; the two in-book spells remain lit");
            Assert.That(card.CastableSpells[1].Caption, Is.EqualTo("FIREBALL - REMOVED"));

            var preview = new VisualElement();
            DirectorHandsUnityView.PopulatePeekPreview(
                preview, card, new VisualElement(), dimUnavailable: false);
            var shelf = Named(preview, "gd-castable-spells").Single();
            Assert.That(shelf.style.width.value.value,
                Is.EqualTo(CastableSpellHoverView.WidthFor(card.CastableSpells)));
            Assert.That(shelf.style.height.value.value,
                Is.EqualTo(CastableSpellHoverView.HeightFor(card.CastableSpells)));
            Assert.That(shelf.style.left.value.value,
                Is.EqualTo((CardFootprint.Width - shelf.style.width.value.value) / 2f),
                "the one-face-wide stack is centred over its casting card");
            Assert.That(shelf.style.top.value.value,
                Is.EqualTo(-shelf.style.height.value.value - CastableSpellHoverView.Gap),
                "the stack clears the casting card above it");
            var column = Named(shelf, "gd-castable-spells-row").Single();
            Assert.That(column.style.flexDirection.value, Is.EqualTo(FlexDirection.Column));
            var hosts = Named(column, "gd-castable-spell").ToArray();
            Assert.That(hosts, Has.Length.EqualTo(3));
            Assert.That(hosts.Take(2).All(host => host.style.marginBottom.value.value > 0f), Is.True);
            Assert.That(hosts.Last().style.marginBottom.value.value, Is.Zero);
            var faces = Named(shelf, "gd-spell-face").ToArray();
            Assert.That(faces, Has.Length.EqualTo(3));
            Assert.That(faces.All(face => face.style.width.value.value > 200f), Is.True,
                "the hover uses readable spell faces rather than thumbnails");
            Assert.That(Named(shelf, "gd-spell-greyed").Count(), Is.EqualTo(1));
            Assert.That(Named(shelf, "gd-castable-spell-caption"), Is.Empty,
                "availability is the face's dimmed treatment; the printed card needs no duplicate label");
            Assert.That(Named(shelf, "gd-castable-spells-label"), Is.Empty);
        }

        /// <summary>
        /// Milestone 9.30j, found by Gydion's gate in Play Mode: the window's own submit path completes
        /// a choose-several on its last pick (6.5c's <c>EffectChoiceCompletion</c>), so Find Familiar's
        /// third face confirmed the removal behind the player and the "3 of 3 chosen ... CONFIRM" state
        /// this window draws (approved mockup, the user: "he picks up to three (0 allowed) and
        /// confirms") was never reached. A spell multi-pick is reviewed as a whole, as 8.93f's rune
        /// slots are: the third pick leaves the choice standing for CONFIRM, or for un-choosing one.
        /// </summary>
        [Test]
        public void FindFamiliarsThirdPick_LeavesTheThreeChosenFacesStandingUntilConfirm()
        {
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.CastingCard(GydionCardContent.FindFamiliar),
                removed: new[] { GydionCardContent.Wish });
            round.StartCasting();
            round.PickSpace(new HexCoordinate(1, 0));
            EffectChoiceCommand last = null;
            foreach (var spell in new[] { GydionCardContent.Sunburst, GydionCardContent.Shield, GydionCardContent.Fireball })
            {
                var arm = GydionCardContent.FindFamiliarRemovalArm(spell);
                last = round.Choice.SelectInvocations.Single(invocation => invocation.Candidate.OptionNodeId.Equals(arm));
                round.SelectArm(arm);
            }

            var filled = SpellChoice(round, ownerSeat: true);
            Assert.That(filled.PendingEffectChoice.SelectedCandidates.Count, Is.EqualTo(3), "three faces chosen");
            Assert.That(EffectChoiceCompletion.FindImmediateConfirm(filled, last), Is.Null,
                "the third pick does not confirm the removal on the player's behalf");
            var model = DecisionWindowPresenter.CreateViewModel(filled, Caster);
            Assert.That(model.Count, Is.EqualTo("3 of 3 chosen"));
            Assert.That(model.Controls.Count(control => control.IsChosen && control.Role == DecisionWindowControlRole.Deselect),
                Is.EqualTo(3), "each chosen face can still be un-chosen");
            Assert.That(model.Controls.Single(control => control.Role == DecisionWindowControlRole.Confirm).IsAnswerable,
                Is.True, "and CONFIRM is the player's own press");
        }

        /// <summary>
        /// Milestone 9.30j, found by Gydion's gate in Play Mode: The Archwizard's "Add all spell cards"
        /// or "Cast Wish instead" mixes a worded option with a spell. The worded option keeps its
        /// declared words; Wish is drawn as its face, and - because this choice is not a cast among
        /// spells - captioned with its own declared words, "CAST WISH INSTEAD", rather than the bare
        /// spell name a "Choose and cast one" face carries (approved mockup A).
        /// </summary>
        [Test]
        public void ThePrepareSpellsReplacement_DrawsTheRefillWorded_AndWishsFaceCaptionedCastWishInstead()
        {
            var model = DecisionWindowPresenter.CreateViewModel(ThePrepareSpellsReplacement(out var gydion), gydion);
            var refill = model.Controls.Single(control => control.Spell == null && control.IsAnswerable);
            Assert.That(refill.Label, Is.EqualTo("ADD ALL SPELL CARDS TO THE SPELLBOOK"), "the refill in its declared words");
            var wish = model.Controls.Single(control => control.Spell != null);
            Assert.That(wish.Spell.DefinitionReference.DefinitionId, Is.EqualTo(GydionCardContent.Wish), "Wish drawn as its face");
            Assert.That(wish.Spell.Caption, Is.EqualTo("CAST WISH INSTEAD"));
            Assert.That(wish.Label, Is.EqualTo("CAST WISH INSTEAD"), "the face's caption is the replacement's declared words");
        }

        /// <summary>
        /// Milestone 9.30j. The same window must not read "SPELL CHOICE" over "Choose and cast one spell
        /// in the spellbook.": it is The Archwizard's replacement of a refill, not a cast among spells.
        /// Its words come from the printed cards: the eyebrow is the unlocked Ultimate whose standing rule
        /// gates the choice (the Choose sits under Prepare Spells' "ultimate unlocked" branch), and the
        /// question is the two options' own declared words in printed order. A cast among spells is
        /// unchanged.
        /// </summary>
        [Test]
        public void ThePrepareSpellsReplacement_IsHeadedByTheArchwizard_AndAsksItsTwoPrintedOptions()
        {
            var model = DecisionWindowPresenter.CreateViewModel(ThePrepareSpellsReplacement(out var gydion), gydion);
            Assert.That(model.Eyebrow, Is.EqualTo("THE ARCHWIZARD"), "the Ultimate's printed name");
            Assert.That(model.Question, Is.EqualTo("Add all spell cards to the spellbook, or cast Wish instead."),
                "Prepare Spells' and The Archwizard's own words");

            var cast = DecisionWindowPresenter.CreateViewModel(SpellChoice(MockupARound(), true), Caster);
            Assert.That(cast.Eyebrow, Is.EqualTo(SpellbookPresentation.SpellChoiceEyebrow), "a cast among spells is unchanged");
            Assert.That(cast.Question, Is.EqualTo(SpellbookPresentation.CastQuestion));
        }

        /// <summary>
        /// Gydion's Tier III staged session, his Prepare Spells started: The Archwizard's add-or-Wish
        /// choice standing (GY-Q7: a full book still "would add cards").
        /// </summary>
        private static IMatchReadModel ThePrepareSpellsReplacement(out HeroId gydion)
        {
            var session = GydionDealingTests.StagedSession(tierThree: true, out var wasp, out gydion);
            var projection = session.Projection;
            var hero = gydion;
            var prepare = projection.Cards.Single(card => card.OwnerId.Equals(hero) &&
                card.DefinitionReference.DefinitionId.Equals(GydionCardContent.PrepareSpells));
            var hers = projection.Cards
                .Where(card => card.OwnerId.Equals(wasp) && card.State.Location == CardLocation.Hand)
                .OrderBy(card => projection.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
            Assert.That(session.Execute(GuardsDigital.Core.Replay.DirectorCommand.CommitCard(wasp, hers.Id)).Accepted, Is.True);
            Assert.That(session.Execute(GuardsDigital.Core.Replay.DirectorCommand.CommitCard(gydion, prepare.Id)).Accepted, Is.True);
            Assert.That(session.Execute(GuardsDigital.Core.Replay.DirectorCommand.StartAction(
                prepare.Id, GydionCardContent.PrimarySkill)).Accepted, Is.True);
            Assert.That(session.Projection.PendingEffectChoice?.NodeId,
                Is.EqualTo(GydionCardContent.PrepareSpellsReplacementChoice), "The Archwizard asks (GY-Q7)");
            return session.Projection;
        }

        [Test]
        public void FindFamiliarsRemoval_DrawsOnlyTheSpellsStillInTheBook_PicksUpToThree_ThenConfirm()
        {
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.CastingCard(GydionCardContent.FindFamiliar),
                removed: new[] { GydionCardContent.Wish });
            round.StartCasting();
            round.PickSpace(new HexCoordinate(1, 0));

            var model = DecisionWindowPresenter.CreateViewModel(SpellChoice(round, ownerSeat: true), Caster);
            var spells = model.Controls.Where(control => control.Spell != null).ToArray();
            Assert.That(model.Eyebrow, Is.EqualTo("SPELL CHOICE"));
            Assert.That(model.Question, Does.StartWith("Remove up to 3 other spells"));
            Assert.That(spells, Has.Length.EqualTo(20),
                "the other spells still in his book (the user: 'the spells still in his book'); Wish is removed. " +
                Describe(model));
            Assert.That(spells.Select(control => control.Spell.DefinitionReference.DefinitionId),
                Has.No.Member(GydionCardContent.Wish).And.No.Member(GydionCardContent.FindFamiliar));
            Assert.That(spells.All(control => control.IsAnswerable && !control.Spell.IsGreyed), Is.True);
            Assert.That(spells.Select(control => control.Label), Has.Member("SUNBURST"),
                "a removal casts nothing, so a counting spell is its name alone");
            Assert.That(model.Count, Is.EqualTo("0 of 3 chosen"));
            var confirm = model.Controls.Single(control => control.Role == DecisionWindowControlRole.Confirm);
            Assert.That(confirm.Label, Is.EqualTo("CONFIRM"), "pick 0-3, then CONFIRM (the user, 2026-10-01)");
            Assert.That(confirm.IsAnswerable, Is.True, "removing none is an answer");

            foreach (var spell in new[] { GydionCardContent.Sunburst, GydionCardContent.Shield, GydionCardContent.Fireball })
            {
                round.SelectArm(GydionCardContent.FindFamiliarRemovalArm(spell));
            }

            model = DecisionWindowPresenter.CreateViewModel(SpellChoice(round, ownerSeat: true), Caster);
            spells = model.Controls.Where(control => control.Spell != null).ToArray();
            Assert.That(model.Count, Is.EqualTo("3 of 3 chosen"));
            Assert.That(spells, Has.Length.EqualTo(20), "every face keeps its place once three are chosen");
            Assert.That(spells.Count(control => control.IsChosen && control.Role == DecisionWindowControlRole.Deselect),
                Is.EqualTo(3), "the three chosen wear the selection and un-choose on a press");
            Assert.That(spells.Count(control => !control.IsAnswerable && control.Spell.IsGreyed), Is.EqualTo(17),
                "the rest stand inert, greyed, in place");
            Assert.That(model.Controls.Single(control => control.Role == DecisionWindowControlRole.Confirm).Label,
                Is.EqualTo("CONFIRM"));
        }

        // -------------------------------------------------------------------------------------------
        // Approved mockups B, C and D: the drawer and the chip
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSpellbookDrawer_ShowsItsOwnersSeatTheBookAsFaces_AndEveryOtherSeatOnlyBacksWithCounts()
        {
            var host = new MatchHost(SpellbookZoneTests.CreateSession(true));
            var bookNames = host.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => host.Projection.Definitions.Single(value => value.Reference.Equals(card.DefinitionReference)))
                .ToArray();
            Assert.That(bookNames, Has.Length.EqualTo(2));

            var owner = Drawer(host, Owner, out var ownerView);
            Assert.That(TextOf(Named(owner, "gd-spellbook-in-book-label").Single()), Is.EqualTo("IN THE BOOK - 2"));
            Assert.That(TextOf(Named(owner, "gd-spellbook-removed-label").Single()), Is.EqualTo("REMOVED, FACEUP - 1"));
            Assert.That(Named(Named(owner, "gd-spellbook-in-book").Single(), "gd-spell-face").Count(), Is.EqualTo(2),
                "the owner's seat reads its book as faces (mockup B)");
            Assert.That(Named(owner, "gd-spell-back").Any(), Is.False, "and draws no back of its own book");
            Assert.That(TextOf(Named(owner, "gd-spellbook-subtitle").Single()),
                Does.StartWith("Spells currently in the book."));

            var other = Drawer(host, Other, out var otherView);
            var inBook = Named(other, "gd-spellbook-in-book").Single();
            Assert.That(Named(inBook, "gd-spell-face").Any(), Is.False, "no other seat sees a book face (mockup C)");
            var stacks = Named(inBook, "gd-spellbook-back-stack").ToArray();
            Assert.That(stacks.Select(stack => (CardTier)stack.userData), Is.EqualTo(new[] { CardTier.I, CardTier.IV }),
                "the book as the table sees it: the basic spell's Tier I back and the Tier IV back");
            Assert.That(stacks.Select(stack => TextOf(Named(stack, "gd-spellbook-back-count").Single())),
                Is.EqualTo(new[] { "x1", "x1" }));
            Assert.That(Named(Named(other, "gd-spellbook-removed").Single(), "gd-spell-face").Count(), Is.EqualTo(1),
                "and every seat reads the removed pile faceup");
            Assert.That(TextOf(Named(other, "gd-spellbook-title").Single()), Does.EndWith("'S SPELLBOOK"));

            // Privacy comes from the projection: nothing the other seat's HUD drew names a book spell.
            foreach (var element in Descendants(otherView.Root).Concat(Descendants(otherView.HoverPreviewRoot)))
            {
                foreach (var spell in bookNames)
                {
                    Assert.That(element.userData, Is.Not.EqualTo(spell.Reference),
                        "an element carries a facedown spell's reference: " + element.name);
                    Assert.That(element.tooltip ?? string.Empty, Does.Not.Contain(spell.DisplayName),
                        "an element names a facedown spell: " + element.name);
                    Assert.That((element as TextElement)?.text ?? string.Empty, Does.Not.Contain(spell.DisplayName),
                        "a label names a facedown spell: " + element.name);
                }
            }

            otherView.CloseSpellbook();
            Assert.That(otherView.IsSpellbookOpen, Is.False);
            Assert.That(otherView.SpellbookDrawer.parent.style.display.value, Is.EqualTo(DisplayStyle.None),
                "CLOSE takes the drawer away and changes nothing else");
            Assert.That(ownerView.OpenSpellbook(Other), Is.False, "a hero who owns no book has no drawer");
        }

        [Test]
        public void TheItemsStrip_DrawsTheSpellbookChip_InBookAndRemoved_OnlyForAHeroWithABook_AndItOpensTheDrawer()
        {
            var host = new MatchHost(SpellbookZoneTests.CreateSession(true));
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(new SeatMatchClient(host.ClientForSeat(host.Seats.FindByHero(Other).Id)), view)
                .Refresh();

            var chips = Named(view.Root, "gd-spellbook-chip").ToArray();
            Assert.That(chips, Has.Length.EqualTo(1), "only the hero who owns a book draws the chip (mockup D)");
            var chip = (Button)chips[0];
            Assert.That(chip.text, Is.EqualTo("SPELLBOOK 2 | 1"), "in the book | removed");
            Assert.That(chip.parent.name, Is.EqualTo("gd-item-chips"));
            Assert.That(chip.parent.parent.name, Is.EqualTo("gd-bonus-strip"), "it lives in the items strip");
            Assert.That(chip.tooltip, Does.Contain("2 in the book").And.Contain("1 removed"));

            Assert.That(view.OpenSpellbook(Owner), Is.True, "the chip opens its hero's drawer");
            Assert.That(view.OpenSpellbookHeroId, Is.EqualTo(Owner));
            Assert.That(view.SpellbookDrawer.parent.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void DrawerSpellsStayReadableInPlaceWithoutHoverPreviews()
        {
            var host = new MatchHost(SpellbookZoneTests.CreateSession(true));
            var drawer = Drawer(host, Owner, out var view);
            foreach (var spellHost in Named(drawer, "gd-spellbook-spell"))
            {
                using (var enter = PointerEnterEvent.GetPooled())
                { enter.target = spellHost; spellHost.SendEvent(enter); }
                Assert.That(Named(view.HoverPreviewRoot, "gd-spell-face"), Is.Empty);
                var face = Named(spellHost, "gd-spell-face").Single();
                Assert.That(face.style.width.value.value,
                    Is.EqualTo(SpellCardFootprint.Width * SpellCardFootprint.DrawerScale).Within(0.01f));
            }
        }

        // -------------------------------------------------------------------------------------------
        // The landscape face and its art
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ASpellFace_IsLandscape372By260_AtTheActionCardsOwnWidth()
        {
            Assert.That(SpellCardFootprint.Width, Is.EqualTo(CardFootprint.StandardWidth),
                "the action card's width - one card size family, two printed shapes");
            Assert.That(SpellCardFootprint.Height / SpellCardFootprint.Width,
                Is.EqualTo(260f / 372f).Within(0.0001f), "the verified 372x260 landscape scan");
            Assert.That(SpellCardFootprint.Height, Is.LessThan(SpellCardFootprint.Width));

            using (ConfiguredSpellArtwork.UseTextureLoader(_ => null))
            {
                var face = SpellCardFaceView.Build(
                    new SpellCardViewModel(GydionCardContent.Definition(GydionCardContent.Sunburst)));
                Assert.That(face.style.width.value.value, Is.EqualTo(SpellCardFootprint.Width));
                Assert.That(face.style.height.value.value, Is.EqualTo(SpellCardFootprint.Height));
                Assert.That(face.style.minHeight.value.value, Is.EqualTo(face.style.maxHeight.value.value),
                    "pinned at both ends, as every card face is");
            }
        }

        [Test]
        public void ASpellFace_DrawsItsPreparedFront_OrAGeneratedFaceWithoutTheArt_AndABackTheSame()
        {
            var sunburst = new SpellCardViewModel(GydionCardContent.Definition(GydionCardContent.Sunburst));
            var texture = new Texture2D(372, 260);
            var asked = new List<string>();
            using (ConfiguredSpellArtwork.UseTextureLoader(path =>
            {
                asked.Add(path);
                return texture;
            }))
            {
                var face = SpellCardFaceView.Build(sunburst);
                Assert.That(Named(face, "gd-spell-artwork").Count(), Is.EqualTo(1));
                Assert.That(Named(face, "gd-spell-generated").Any(), Is.False);
                Assert.That(Named(SpellCardFaceView.BuildBack(CardTier.II), "gd-spell-back-artwork").Count(), Is.EqualTo(1));
            }

            Assert.That(asked, Is.EqualTo(new[] { "Private/Spells/gydion-sunburst", "Private/Spells/Backs/II" }),
                "the prepared resource is addressed by the definition and the back, never by a name");

            using (ConfiguredSpellArtwork.UseTextureLoader(_ => null))
            {
                var face = SpellCardFaceView.Build(sunburst);
                Assert.That(Named(face, "gd-spell-generated").Count(), Is.EqualTo(1),
                    "a clean checkout still draws the spell, from its typed regions");
                Assert.That(TextOf(Named(face, "gd-spell-name").Single()), Is.EqualTo("Sunburst"));
                Assert.That(Named(SpellCardFaceView.BuildBack(CardTier.IV), "gd-spell-back-generated").Count(),
                    Is.EqualTo(1));
            }

            UnityEngine.Object.DestroyImmediate(texture);
        }

        [Test]
        public void TheArtCatalog_NamesEverySpellsFrontOnce_AndTheFourBacks_EachTheBackItsSpellsShow()
        {
            var catalog = ConfiguredSpellArtwork.Catalog;
            Assert.That(catalog.Fronts.Select(entry => entry.Reference),
                Is.EquivalentTo(GydionCardContent.SpellDefinitions.Select(definition => definition.Reference)),
                "one row per spell definition, at its current revision");
            Assert.That(catalog.Fronts.Select(entry => entry.ResourcePath), Is.EquivalentTo(
                GydionCardContent.SpellDefinitions.Select(definition => "Private/Spells/" + definition.Id.Value)));
            Assert.That(catalog.Backs.Select(entry => entry.Back),
                Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.III, CardTier.IV }));

            // Each back is the back of a spell that shows it: the stem the back is cut from is a front
            // of that Tier (a basic spell shows Tier I).
            foreach (var back in catalog.Backs)
            {
                var stem = back.SourcePath.Replace("__back.png", string.Empty);
                var front = catalog.Fronts.Single(entry => entry.SourcePath == stem + "__front.png");
                var definition = GydionCardContent.SpellDefinitions.Single(value => value.Reference.Equals(front.Reference));
                Assert.That(SpellbookBackCount.BackOf(definition.Progression), Is.EqualTo(back.Back),
                    back.SourcePath + " is the back " + definition.DisplayName + " shows");
            }

            var json = Resources.Load<TextAsset>(ConfiguredCardArtworkCatalog.ResourcePath).text;
            var withoutWish = json.Replace(
                ",\n    { \"definitionId\": \"gydion-wish\", \"contentRevision\": \"" + GydionCardContent.ContentRevision +
                "\", \"sourcePath\": \"cards/other-cards/spells-tt22__7711655__front.png\" }", string.Empty);
            Assert.That(withoutWish, Is.Not.EqualTo(json), "the fixture edit removed Wish's row");
            Assert.That(() => SpellArtworkCatalog.ParseAndValidate(withoutWish, ConfiguredSpellArtwork.ConfiguredReferences),
                Throws.InvalidOperationException.With.Message.Contains("omits spell"));
            Assert.That(() => SpellArtworkCatalog.ParseAndValidate(
                    json.Replace("{ \"back\": 4,", "{ \"back\": 3,"), ConfiguredSpellArtwork.ConfiguredReferences),
                Throws.InvalidOperationException.With.Message.Contains("appears twice"));
            Assert.That(ConfiguredCardArtworkCatalog.Catalog.Entries.Count, Is.EqualTo(578),
                "NebKher adds eighteen action-card rows; the spell arrays beside them remain separate");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 9.30h1: the printed ratio, and a size that reads, on every surface
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The user on the 9.30h captures: "scaling is a bit off for those spells". Every spell face -
        /// the spell choice, Find Familiar's pick, the drawer's faces, its tier backs and its pointer-rest
        /// reveal - is drawn at the printed 372:260 whatever size its scan was imported at. The 256x256
        /// scan is what Unity's default import made of a 372x260 PNG (a non-power-of-two size scaled to
        /// the nearest power of two); on 0b7de861 every face fitted it into its box and was drawn
        /// square, 1.000 against the printed 1.431.
        /// </summary>
        [Test]
        public void EverySpellFace_IsDrawnAtThePrinted372By260_InTheChoiceThePickAndTheDrawer_WhateverSizeItsScanWasImportedAt()
        {
            var square = new Texture2D(256, 256);
            try
            {
                using (ConfiguredSpellArtwork.UseTextureLoader(_ => square))
                {
                    foreach (var surface in SpellSurfaces())
                    {
                        Assert.That(surface.Faces, Is.Not.Empty, surface.Name + " draws spell faces");
                        foreach (var face in surface.Faces)
                        {
                            AssertDrawnAtThePrintedRatio(face, surface.Name);
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(square);
            }
        }

        /// <summary>
        /// The user: "they might be too small". The spell choice and every face in the drawer are the
        /// action card's own width (about 300 by 210); Find Familiar's pick is two thirds of that (about
        /// 200 by 140) in rows of at most seven; nothing is narrower than 170. On 0b7de861 the drawer
        /// drew 186-wide faces and the pick 180-wide ones in rows as long as the window.
        /// </summary>
        [Test]
        public void EverySpellFace_IsLargeEnoughToRead_TheChoiceAndTheDrawerAtTheActionCardsWidth_ThePickAtTwoThirdsInRowsOfSeven()
        {
            using (ConfiguredSpellArtwork.UseTextureLoader(_ => null))
            {
                foreach (var surface in SpellSurfaces())
                {
                    foreach (var face in surface.Faces)
                    {
                        var width = face.style.width.value.value;
                        Assert.That(width, Is.EqualTo(surface.Width).Within(0.5f),
                            surface.Name + " draws its faces " + surface.Width.ToString("F0") + " wide");
                        Assert.That(width, Is.GreaterThanOrEqualTo(170f), surface.Name + ": never below 170 wide");
                    }
                }

                var pick = Window(FindFamiliarRound());
                var controls = Named(pick.Root, "gd-decision-window-controls").Single();
                Assert.That(controls.style.maxWidth.keyword, Is.EqualTo(StyleKeyword.Undefined),
                    "the pick's row is bounded, so its faces wrap however wide the window is");
                // One option's whole slot as the window declares it: the face with its outline, then the gap.
                var option = Named(pick.Root, "gd-spell-option").First();
                var slot = option.style.width.value.value + option.style.marginRight.value.value;
                Assert.That(Mathf.FloorToInt(controls.style.maxWidth.value.value / slot), Is.EqualTo(7),
                    "rows of five to seven (the user), never the strip of eight");

                var choice = Window(DecisionWindowPresenter.CreateViewModel(SpellChoice(MockupARound(), true), Caster));
                Assert.That(Named(choice.Root, "gd-decision-window-controls").Single().style.maxWidth.keyword,
                    Is.EqualTo(StyleKeyword.Null), "a full-size choice keeps the window's own width");
            }
        }

        /// <summary>
        /// The other half of the squeeze: the importer kept a prepared spell at its own size only from
        /// 9.30h1. In a checkout holding the private corpus, every prepared front and back loads at
        /// 372x260; on 0b7de861 they loaded at 256x256.
        /// </summary>
        [Test]
        public void WhenPrivateCorpusIsPresent_EveryPreparedSpellScanLoadsAtItsPrinted372By260()
        {
            var corpus = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                "..", "Docs", "Guards of Atlantis II - Tabletopia Cards"));
            // A clean checkout deliberately has no private art; the generated face is proved above.
            if (!System.IO.Directory.Exists(corpus)) return;
            var catalog = ConfiguredSpellArtwork.Catalog;
            foreach (var path in catalog.Fronts.Select(entry => entry.ResourcePath)
                .Concat(catalog.Backs.Select(entry => entry.ResourcePath)))
            {
                var texture = Resources.Load<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path + " is prepared (Tools/Prepare-SpellArtwork.ps1)");
                Assert.That(texture.width + "x" + texture.height, Is.EqualTo("372x260"),
                    path + " is imported at its own size, not scaled to a power of two");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The log
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheLog_NamesASpellWhileItIsFaceup_AndNeverNamesASpellBackInTheBook_ToASeatThatCannotReadIt()
        {
            var host = new MatchHost(SpellbookZoneTests.CreateSession(true));
            var removed = host.Projection.Cards.Single(card => card.State.Location == CardLocation.RemovedFromSpellbook);
            var inBook = host.Projection.Cards.First(card => card.State.Location == CardLocation.Spellbook);
            string NameOf(CardInstance card) => host.Projection.Definitions
                .Single(value => value.Reference.Equals(card.DefinitionReference)).DisplayName.ToUpperInvariant();
            var entries = new[]
            {
                new MatchLogEntry(1, MatchLogEntryKind.SpellCast, 1, 1, subjectHeroId: Owner,
                    subjectCardInstanceId: removed.Id),
                // A spell cast earlier and since returned to the book by a refill: facedown again.
                new MatchLogEntry(2, MatchLogEntryKind.SpellCast, 1, 1, subjectHeroId: Owner,
                    subjectCardInstanceId: inBook.Id),
                new MatchLogEntry(3, MatchLogEntryKind.SpellRemovedFaceup, 1, 1, subjectHeroId: Owner,
                    subjectCardInstanceId: removed.Id),
                new MatchLogEntry(4, MatchLogEntryKind.SpellbookRefilled, 1, 1, subjectHeroId: Owner, amount: 2)
            };

            var other = MatchLogFormatter.ComposeOldestFirst(
                new LogReadModel(host.ProjectionForSeat(host.Seats.FindByHero(Other).Id), entries));
            Assert.That(other.Select(line => line.Text).ToArray(), Has.Length.EqualTo(4));
            Assert.That(other[0].Text, Does.Contain("CAST " + NameOf(removed)), "a faceup spell is named to every seat");
            Assert.That(other[1].Text, Does.Contain("CAST A SPELL"));
            Assert.That(other[1].Text, Does.Not.Contain(NameOf(inBook)),
                "a spell back in the book is never named to a seat that cannot read the book");
            Assert.That(other[2].Text, Does.Contain("REMOVED " + NameOf(removed) + " FROM THE SPELLBOOK"));
            Assert.That(other[3].Text, Does.Contain("RETURNED 2 SPELLS TO THE SPELLBOOK"));

            var owner = MatchLogFormatter.ComposeOldestFirst(
                new LogReadModel(host.ProjectionForSeat(host.Seats.FindByHero(Owner).Id), entries));
            Assert.That(owner[1].Text, Does.Contain("CAST " + NameOf(inBook)), "while its owner still reads it");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Mockup A's pause, shape for shape: a castable spell captioned with its name, a removed one
        /// greyed REMOVED and a counting one captioned with its values - Find Familiar, Fireball,
        /// Sunburst, with Magic Missile, Shield, Fireball, Suggestion and Dimension Door removed, so
        /// Sunburst counts five. Find Familiar stands in for the mockup's Burning Hands because Burning
        /// Hands' text is owed to 9.30d and its cast is refused until then (BorrowRefusal), which would
        /// grey it; this case is about the window, not about which spells are composed yet.
        /// </summary>
        private static GydionCountingSpellTests.GydionRound MockupARound()
        {
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.ChooseAndCastCard(
                    GydionCardContent.FindFamiliar, GydionCardContent.Fireball, GydionCardContent.Sunburst),
                removed: new[]
                {
                    GydionCardContent.MagicMissile, GydionCardContent.Shield, GydionCardContent.Fireball,
                    GydionCardContent.Suggestion, GydionCardContent.DimensionDoor
                });
            round.StartCasting();
            Assert.That(round.Choice, Is.Not.Null, "the cast choice stands");
            return round;
        }

        /// <summary>
        /// The round's standing effect choice as the seat that owns it reads it - or, with
        /// <paramref name="ownerSeat"/> false, as a seat that cannot read the book (no facedown spell).
        /// </summary>
        private static IMatchReadModel SpellChoice(GydionCountingSpellTests.GydionRound round, bool ownerSeat) =>
            new SpellChoiceReadModel(SpellbookZoneTests.CreateSession(false).Projection, round, ownerSeat);

        private sealed class SpellChoiceReadModel : MatchFeaturePresenterTests.DelegatingReadModel
        {
            private readonly GydionCountingSpellTests.GydionRound round;
            private readonly IReadOnlyList<CardInstance> cards;

            public SpellChoiceReadModel(IMatchReadModel inner, GydionCountingSpellTests.GydionRound round, bool ownerSeat)
                : base(inner)
            {
                this.round = round;
                cards = round.Cards
                    .Where(card => ownerSeat || card.State.Location != CardLocation.Spellbook)
                    .ToArray();
            }

            public override IReadOnlyList<CardInstance> Cards => cards;
            public override IReadOnlyList<CardDefinition> Definitions => round.Definitions;
            public override PendingEffectChoice PendingEffectChoice => round.Choice;
            public override IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview =>
                Array.Empty<EffectChoiceCandidatePreview>();
            public override PendingDecisionContract DecisionContract => PendingDecisionContract.Create(
                null, null, null, null, round.Choice, null, null, null, null, null, null);
            public override PendingDecisionOwner DecisionOwner =>
                PendingDecisionOwner.ForHero(PendingDecisionFamilies.Effect, Caster);
            public override CardInstanceId ResolvingSourceCardInstanceId => GydionCountingSpellTests.GydionRound.Played;
            public override PendingResolution PendingSkillResolution => null;
            public override GuardsDigital.Core.Cards.SelectedInitiativeActor SelectedActor => round.Turn.SelectedActor;

            /// <summary>The round's own items, projected as the session projects them: a count per icon.</summary>
            public override IReadOnlyList<HeroProgressionProjection> HeroProgression => new[]
            {
                new HeroProgressionProjection(Caster, 1, 0, false,
                    round.Round.HeroItems.GetItems(Caster)
                        .GroupBy(item => item.Icon)
                        .OrderBy(group => group.Key)
                        .Select(group => new KeyValuePair<PrintedValueChannel, int>(group.Key, group.Count())))
            };
            public override bool IsInputEnabled => true;
            public override GuardsDigital.Core.Cards.RoundResolutionPhase Phase =>
                GuardsDigital.Core.Cards.RoundResolutionPhase.ResolvingTurn;
        }

        private sealed class LogReadModel : MatchFeaturePresenterTests.DelegatingReadModel
        {
            private readonly IReadOnlyList<MatchLogEntry> log;

            public LogReadModel(IMatchReadModel inner, IReadOnlyList<MatchLogEntry> log) : base(inner) => this.log = log;

            public override IReadOnlyList<MatchLogEntry> MatchLog => log;
        }

        /// <summary>One seat's rails, rendered from its own projection, with <paramref name="hero"/>'s book open.</summary>
        private static VisualElement Drawer(MatchHost host, HeroId seatHero, out HeroOverviewsUnityView view)
        {
            view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(
                new SeatMatchClient(host.ClientForSeat(host.Seats.FindByHero(seatHero).Id)), view).Refresh();
            Assert.That(view.OpenSpellbook(Owner), Is.True, "the book's owner has a drawer on every seat");
            return view.SpellbookDrawer;
        }

        /// <summary>Find Familiar cast with Wish already removed: the twenty other spells still in the book.</summary>
        private static IMatchReadModel FindFamiliarRound()
        {
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.CastingCard(GydionCardContent.FindFamiliar),
                removed: new[] { GydionCardContent.Wish });
            round.StartCasting();
            round.PickSpace(new HexCoordinate(1, 0));
            return SpellChoice(round, ownerSeat: true);
        }

        private static DecisionWindowUnityView Window(IMatchReadModel projection) =>
            Window(DecisionWindowPresenter.CreateViewModel(projection, Caster));

        private static DecisionWindowUnityView Window(DecisionWindowViewModel model)
        {
            Assert.That(model, Is.Not.Null, "the caster owns the standing choice");
            var view = new DecisionWindowUnityView();
            view.Render(model);
            return view;
        }

        /// <summary>
        /// The drawer with Gydion's own spells on the Red plaque - seventeen in the book, five removed, as
        /// mockups B and C draw it - on his seat (faces) or another (tier backs), so every face and back
        /// is a catalogued one that draws its prepared scan.
        /// </summary>
        private static HeroOverviewsUnityView GydionDrawer(bool ownerSeat)
        {
            var host = new MatchHost(SpellbookZoneTests.CreateSession(true));
            Drawer(host, ownerSeat ? Owner : Other, out var view);
            var removedIds = new[]
            {
                GydionCardContent.MagicMissile, GydionCardContent.Shield, GydionCardContent.Fireball,
                GydionCardContent.Suggestion, GydionCardContent.FindFamiliar
            };
            var book = GydionCardContent.SpellDefinitions.Where(spell => !removedIds.Contains(spell.Id)).ToArray();
            var spellbook = new SpellbookViewModel(
                Owner,
                "Gydion",
                book.Length,
                ownerSeat ? book.Select(spell => new SpellCardViewModel(spell)).ToArray() : new SpellCardViewModel[0],
                ownerSeat
                    ? new SpellBackStackViewModel[0]
                    : book.GroupBy(spell => SpellbookBackCount.BackOf(spell.Progression))
                        .OrderBy(group => group.Key)
                        .Select(group => new SpellBackStackViewModel(group.Key, group.Count()))
                        .ToArray(),
                GydionCardContent.SpellDefinitions.Where(spell => removedIds.Contains(spell.Id))
                    .Select(spell => new SpellCardViewModel(spell)).ToArray());
            var source = view.Model.Heroes.Single(hero => hero.HeroId.Equals(Owner));
            var replacement = new HeroOverviewViewModel(
                source.HeroId, source.TeamSide, source.ContentId, source.HeroDisplayName, source.Level,
                source.Coins, source.ItemBonuses, source.TurnSlots, source.DiscardCards, source.IsViewed,
                source.CurrentCard, source.UltimateCard, source.HeldMarkers, source.IsAwaited, source.Worth,
                source.CurrentTurnMarker, spellbook);
            view.Render(new HeroOverviewsViewModel(
                view.Model.Version,
                view.Model.Heroes.Select(hero => hero.HeroId.Equals(Owner) ? replacement : hero)));
            Assert.That(view.OpenSpellbook(Owner), Is.True);
            return view;
        }

        private sealed class SpellSurface
        {
            public SpellSurface(string name, float width, IEnumerable<VisualElement> faces)
            {
                Name = name;
                Width = width;
                Faces = faces.ToArray();
            }

            public string Name { get; }

            /// <summary>The width every face on this surface is drawn at.</summary>
            public float Width { get; }

            public IReadOnlyList<VisualElement> Faces { get; }
        }

        /// <summary>
        /// Every surface that draws a spell, built under whatever art loader the caller has in place:
        /// the spell choice and the drawer at the action card's own width, the pick at two thirds.
        /// </summary>
        private static IReadOnlyList<SpellSurface> SpellSurfaces()
        {
            var full = CardFootprint.StandardWidth;
            var choice = Window(SpellChoice(MockupARound(), ownerSeat: true));
            var pick = Window(FindFamiliarRound());
            var own = GydionDrawer(ownerSeat: true);
            var other = GydionDrawer(ownerSeat: false);
            var surfaces = new List<SpellSurface>
            {
                new SpellSurface("the spell choice (mockup A)", full, Named(choice.Root, "gd-spell-face")),
                new SpellSurface("Find Familiar's pick", full * 2f / 3f, Named(pick.Root, "gd-spell-face")),
                new SpellSurface("the drawer on his own seat (mockup B)", full,
                    Named(own.SpellbookDrawer, "gd-spell-face")),
                new SpellSurface("the drawer's tier backs on another seat (mockup C)", full,
                    Named(other.SpellbookDrawer, "gd-spell-back")),
                new SpellSurface("the drawer's removed pile on another seat (mockup C)", full,
                    Named(other.SpellbookDrawer, "gd-spell-face"))
            };
            Assert.That(surfaces[0].Faces.Count, Is.EqualTo(3), "mockup A's three spells");
            Assert.That(surfaces[1].Faces.Count, Is.EqualTo(20), "the twenty other spells still in the book");
            Assert.That(surfaces[2].Faces.Count, Is.EqualTo(22), "seventeen in the book and five removed");
            return surfaces;
        }

        /// <summary>
        /// The ratio a face is DRAWN at: its pinned box when the scan is stretched or cropped into it,
        /// the scan's own when it is fitted (letterboxed) inside it.
        /// </summary>
        private static void AssertDrawnAtThePrintedRatio(VisualElement face, string where)
        {
            const float printed = 372f / 260f;
            var boxWidth = face.style.width.value.value;
            var boxHeight = face.style.height.value.value;
            Assert.That(boxWidth / boxHeight, Is.EqualTo(printed).Within(printed * 0.01f), where + ": the face's box");
            var art = Descendants(face).OfType<Image>().Single();
            Assert.That(art.style.width.value, Is.EqualTo(Length.Percent(100f)), where + ": the scan fills its box");
            Assert.That(art.style.height.value, Is.EqualTo(Length.Percent(100f)), where + ": the scan fills its box");
            var drawn = art.scaleMode == ScaleMode.ScaleToFit
                ? (float)art.image.width / art.image.height
                : boxWidth / boxHeight;
            Assert.That(drawn, Is.EqualTo(printed).Within(printed * 0.01f),
                where + ": a spell face is drawn at " + drawn.ToString("F3") + " (" + art.scaleMode + " of a " +
                art.image.width + "x" + art.image.height + " scan), not the printed 372:260 = " +
                printed.ToString("F3"));
        }

        private static string TextOf(VisualElement element) => ((TextElement)element).text;

        private static IEnumerable<VisualElement> Named(VisualElement root, string name) =>
            Descendants(root).Where(element => element.name == name);

        private static IEnumerable<VisualElement> Descendants(VisualElement element)
        {
            yield return element;
            foreach (var child in element.Children())
            {
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private static string Describe(DecisionWindowViewModel model) =>
            model == null
                ? "(no window)"
                : model.Title + " | " + string.Join(" / ", model.Controls.Select(control =>
                    control.Label + (control.Spell == null ? string.Empty : "[spell]") +
                    (control.IsAnswerable ? string.Empty : "[inert]")));
    }
}
