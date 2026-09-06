using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z31. THE BORROW CONTRACT: one card's printed primary action performed off
    /// another card.
    ///
    /// <para>Three of Ursafar's faces print the clause and Tali's Commune with Spirits prints it again,
    /// and the user's 2026-09-06 ruling made all four the same rule. This file is where that rule is
    /// pinned - what the pool admits, what the two printed qualifiers on Angry Roar each do, and what
    /// performing the borrowed action means. Every case is a function of state alone, which is the
    /// point: the pool changes shape the moment his Ultimate is unlocked, and a rule that could only be
    /// checked by playing him would be a rule nobody could prove.</para>
    /// </summary>
    public sealed class BorrowedPrimaryActionTests
    {
        private static readonly HeroId Ursafar = new HeroId("ursafar-1");
        private static readonly HeroId Ally = new HeroId("ally-1");

        private static EffectCapabilityCatalog Catalog() =>
            SyntheticEffectCapabilities.CreateCatalog(
                UrsafarActiveEffectContent.DeferredHandlers()
                    .Concat(SyntheticConditionCapabilities.DeferredHandlers())
                    .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers()));

        private static CardInstance CardFor(HeroId owner, CardDefinitionId definitionId) =>
            CardInstance.CreateInHand(
                new CardInstanceId($"{owner.Value}-{definitionId.Value}"),
                UrsafarCardContent.Definition(definitionId).Reference,
                owner);

        /// <summary>One of his cards, discarded - the pool his two borrowing Greens reach.</summary>
        private static CardInstance Discarded(HeroId owner, CardDefinitionId definitionId)
        {
            var card = CardFor(owner, definitionId);
            CardLifecycleAuthority.TryDiscardFromHand(card);
            return card;
        }

        // -----------------------------------------------------------------------------------------
        // The discarded pool - Instinctive Reaction, Evolutionary Response, and Tali's Commune.
        // -----------------------------------------------------------------------------------------

        [Test]
        public void DiscardedPool_AdmitsOnlyOwnedDiscardedCards()
        {
            var mine = Discarded(Ursafar, UrsafarCardContent.PreyDrive);
            var stillInHand = CardFor(Ursafar, UrsafarCardContent.Rip);
            var theirs = Discarded(Ally, UrsafarCardContent.SniffOut);

            var eligible = BorrowedPrimaryActionQuery.EligibleCards(
                BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None),
                Ursafar,
                UrsafarCardContent.AllDefinitions,
                new[] { mine, stillInHand, theirs },
                Array.Empty<ActiveEffectInstance>(),
                Catalog());

            Assert.That(eligible.Select(card => card.Id), Is.EqualTo(new[] { mine.Id }),
                "p.10: you can never reach another hero's cards, and a card in hand is not discarded.");
        }

        /// <summary>
        /// Tali's consumption surface. Her Commune with Spirits has an ally name one of p.9's six
        /// colours and then performs the primary action on a discarded card of that colour, so the
        /// declaration that narrows by colour is hers - and Ursafar's two Greens, which narrow nothing,
        /// pass <see cref="CardColor.None"/> through the same door.
        /// </summary>
        [Test]
        public void DiscardedPool_NarrowsByColour_WhichIsTaliesConsumptionSurface()
        {
            var red = Discarded(Ursafar, UrsafarCardContent.PreyDrive);
            var blue = Discarded(Ursafar, UrsafarCardContent.ProwlingBrute);
            var green = Discarded(Ursafar, UrsafarCardContent.SniffOut);
            var cards = new[] { red, blue, green };

            Assert.That(
                Eligible(BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.Blue), cards),
                Is.EqualTo(new[] { blue.Id }));
            Assert.That(
                Eligible(BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None), cards)
                    .Count,
                Is.EqualTo(3),
                "A declaration that narrows nothing says so with None rather than by omission.");
        }

        [Test]
        public void DiscardedPool_RefusesACardWhoseTextIsTranscribedButNotComposed()
        {
            var handicap = Discarded(Ursafar, UrsafarCardContent.JawsThatBite);

            Assert.That(
                Eligible(
                    BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None),
                    new[] { handicap }),
                Is.Empty,
                "A deferred face would perform a shortened version of itself.");
            Assert.That(
                BorrowedPrimaryActionDeclaration.Refuses(
                    UrsafarCardContent.Definition(UrsafarCardContent.JawsThatBite)),
                Is.EqualTo(BorrowRefusal.PrintedTextNotComposed));
        }

        [Test]
        public void TheUltimateCanNeverBeBorrowedFrom_BecauseItPrintsNoPrimaryAction()
        {
            Assert.That(
                BorrowedPrimaryActionDeclaration.Refuses(
                    UrsafarCardContent.Definition(UrsafarCardContent.UnboundFury)),
                Is.EqualTo(BorrowRefusal.NoPrintedPrimaryAction),
                "p.16's Ultimate is a permanent ability; 'the primary action on that card' names " +
                "nothing. The same reading the user gave for a Purple naming on Tali's Silver.");
        }

        [Test]
        public void NoPrintedPrimaryOfHisMovesItsOwnCard_WhichIsTheContractsInvariantMeasured()
        {
            foreach (var definition in UrsafarCardContent.AllDefinitions)
            {
                var refusal = BorrowedPrimaryActionDeclaration.Refuses(definition);
                Assert.That(refusal, Is.Not.EqualTo(BorrowRefusal.ActionMovesItsOwnCard),
                    $"{definition.Id}: a borrowed primary must leave its card where it stands.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The active pool - Angry Roar, and the two qualifiers the user's ruling gave meaning to.
        // -----------------------------------------------------------------------------------------

        [Test]
        public void PrintsAnActiveEffect_IsTrueOfEveryGrantingFace_AndFalseOfEveryGreenOne()
        {
            var catalog = Catalog();

            foreach (var cardId in UrsafarActiveEffectContent.ComposedGrants)
            {
                Assert.That(
                    BorrowedPrimaryActionQuery.PrintsAnActiveEffect(
                        UrsafarCardContent.Definition(cardId), catalog),
                    Is.True,
                    cardId.Value);
            }

            foreach (var cardId in new[]
                {
                    UrsafarCardContent.SniffOut, UrsafarCardContent.EyesOnThePrey,
                    UrsafarCardContent.ApexPredator, UrsafarCardContent.InstinctiveReaction,
                    UrsafarCardContent.EvolutionaryResponse
                })
            {
                Assert.That(
                    BorrowedPrimaryActionQuery.PrintsAnActiveEffect(
                        UrsafarCardContent.Definition(cardId), catalog),
                    Is.False,
                    $"{cardId} reads the state and never grants it, so its text prints no active " +
                    "effect and Angry Roar's second qualifier keeps it out of the pool.");
            }
        }

        [Test]
        public void ActivePool_BeforeTheUltimate_AdmitsOnlyCardsSourcingALiveActiveEffect()
        {
            var running = CardFor(Ursafar, UrsafarCardContent.PreyDrive);
            var quiet = CardFor(Ursafar, UrsafarCardContent.Rip);
            var effects = LiveEnrage(running, UrsafarCardContent.PreyDrive);

            var eligible = BorrowedPrimaryActionQuery.EligibleCards(
                BorrowedPrimaryActionDeclaration.FromActiveCardsWithAnActiveEffect(),
                Ursafar,
                UrsafarCardContent.AllDefinitions,
                new[] { running, quiet },
                effects,
                Catalog());

            Assert.That(eligible.Select(card => card.Id), Is.EqualTo(new[] { running.Id }),
                "p.17's sideways signal is exactly 'this card is the source of a live active effect'.");
        }

        /// <summary>
        /// The Ultimate's second clause, and the case the user's ruling names outright: after Unbound
        /// Fury every resolved card counts as active, so Angry Roar reaches one whose own effect has
        /// already expired - and the second printed qualifier is then the only thing still narrowing
        /// the pool, which is why the card is written with both.
        /// </summary>
        [Test]
        public void ActivePool_AfterTheUltimate_AdmitsResolvedCardsWhoseEffectHasExpired()
        {
            var expired = CardFor(Ursafar, UrsafarCardContent.PreyDrive);
            var greenResolved = CardFor(Ursafar, UrsafarCardContent.SniffOut);
            Resolve(expired);
            Resolve(greenResolved);

            var declaration = BorrowedPrimaryActionDeclaration.FromActiveCardsWithAnActiveEffect();
            var cards = new[] { expired, greenResolved };

            Assert.That(
                BorrowedPrimaryActionQuery.EligibleCards(
                    declaration, Ursafar, UrsafarCardContent.AllDefinitions, cards,
                    Array.Empty<ActiveEffectInstance>(), Catalog(),
                    resolvedCardsCountAsActive: false),
                Is.Empty,
                "Without the Ultimate a resolved card whose effect ended is not active.");

            var widened = BorrowedPrimaryActionQuery.EligibleCards(
                declaration, Ursafar, UrsafarCardContent.AllDefinitions, cards,
                Array.Empty<ActiveEffectInstance>(), Catalog(),
                resolvedCardsCountAsActive: true);

            Assert.That(widened.Select(card => card.Id), Is.EqualTo(new[] { expired.Id }),
                "The Green card is resolved and counts as active too, but its text prints no active " +
                "effect - so the second qualifier does the work the first no longer does.");
        }

        [Test]
        public void TheWideningIsReadOffTheUltimatesOwnRuleKind()
        {
            var sources = new[]
            {
                new ContinuousRuleSource(
                    "ultimate:card:rule",
                    ContinuousRuleSourceKind.Ultimate,
                    Ursafar,
                    UrsafarEffectContent.UnboundFuryRuleKind,
                    UltimateRuleDefinition.ApplicabilityKind)
            };

            Assert.That(
                BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActive(Ursafar, sources),
                Is.True);
            Assert.That(
                BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActive(Ally, sources),
                Is.False,
                "It is his Ultimate and it widens his pool alone.");
            Assert.That(
                BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActive(
                    Ursafar, Array.Empty<ContinuousRuleSource>()),
                Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // What performing a borrowed primary means.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The invariant the whole contract rests on: a borrowed instance is stamped
        /// <see cref="ActionInvocationContext.TextGranted"/> and never
        /// <see cref="ActionInvocationContext.Repeated"/>, which is what makes it invisible to every
        /// repeat reader in the engine without any of them being touched.
        /// </summary>
        [Test]
        public void ABorrowedInstanceIsTextGranted_AndThereforeNotARepeat()
        {
            Assert.That(
                BorrowedPrimaryActionDeclaration.InvocationContext,
                Is.EqualTo(ActionInvocationContext.TextGranted));
            Assert.That(
                BorrowedPrimaryActionDeclaration.InvocationContext,
                Is.Not.EqualTo(ActionInvocationContext.Repeated),
                "p.10 bars repeating a repeated action and treats each repeat instance as a separate " +
                "action. A borrow is neither, and the user's ruling says so outright.");
        }

        /// <summary>
        /// "The borrowed card's supertype carries" - the ruling's own words - and it is true by
        /// construction rather than by a rule written for it: the offer names the borrowed card's
        /// definition, so every reader that asks what kind of action was performed reads the borrowed
        /// card's printed identity.
        /// </summary>
        [Test]
        public void TheBorrowedCardsOwnSupertypeAndValuesAreWhatWouldBePerformed()
        {
            var gold = UrsafarCardContent.Definition(UrsafarCardContent.ClawsThatCatch);
            var green = UrsafarCardContent.Definition(UrsafarCardContent.SniffOut);

            Assert.That(gold.Progression.IsBasic, Is.True,
                "Borrowing Claws That Catch performs a BASIC attack, which feeds every trigger that " +
                "reads one.");
            Assert.That(green.Progression.IsBasic, Is.False);

            var goldPrimary = gold.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(
                goldPrimary.BaseValues.Single(value =>
                    value.Channel == PrintedValueChannel.Attack).Amount,
                Is.EqualTo(4),
                "Its own printed value, because the offer names its own definition.");
            Assert.That(goldPrimary.EffectRoot, Is.Not.Null,
                "Its own text, including - on any Ursafar card - its own enrage grant.");
        }

        [Test]
        public void ADeclarationIsValueEqual_SoTwoFacesPrintingTheSameClauseAreTheSameDeclaration()
        {
            Assert.That(
                BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None),
                Is.EqualTo(BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None)));
            Assert.That(
                BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.Blue),
                Is.Not.EqualTo(BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None)));
            Assert.That(
                BorrowedPrimaryActionDeclaration.FromActiveCardsWithAnActiveEffect().Pool,
                Is.EqualTo(BorrowedActionPool.OwnedActiveCardsWithAnActiveEffect));
        }

        // -----------------------------------------------------------------------------------------

        private static IReadOnlyList<CardInstanceId> Eligible(
            BorrowedPrimaryActionDeclaration declaration, IEnumerable<CardInstance> cards) =>
            BorrowedPrimaryActionQuery.EligibleCards(
                    declaration,
                    Ursafar,
                    UrsafarCardContent.AllDefinitions,
                    cards,
                    Array.Empty<ActiveEffectInstance>(),
                    Catalog())
                .Select(card => card.Id)
                .ToArray();

        private static void Resolve(CardInstance card)
        {
            CardLifecycleAuthority.TryPlayFacedown(card);
            CardLifecycleAuthority.TryReveal(card);
            CardLifecycleAuthority.TryResolveAfterAction(card);
        }

        private static IReadOnlyList<ActiveEffectInstance> LiveEnrage(
            CardInstance sourceCard, CardDefinitionId cardId)
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(
                new ActiveEffectId("enraged"),
                UrsafarActiveEffectContent.DefinitionFor(cardId),
                Ursafar,
                TeamSide.Red,
                Ursafar,
                sourceCard.Id,
                new ActionDefinitionId("primary-attack"),
                1,
                1);
            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                new TieBreakerState(TeamSide.Red));
            return lifecycle.Effects;
        }
    }
}
