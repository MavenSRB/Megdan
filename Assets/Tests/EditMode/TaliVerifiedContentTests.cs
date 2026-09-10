using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Tali the Spiritcaller's nineteen printed faces, audited against the inventory pass that
    /// transcribed them. Milestone 8.35z32.
    ///
    /// <para>Every assertion here is a printed fact, and the shard's table is the thing it is auditing:
    /// values, colours, Tiers, icons, upgrade pairs and the p.6 deck shape. Nothing in this file
    /// exercises an effect - the token system has its own fixture beside this one - because a card
    /// definition being right is a separate claim from its text working.</para>
    /// </summary>
    public sealed class TaliVerifiedContentTests
    {
        private static readonly HeroId Owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        [Test]
        public void TheSetIsNineteenFaces_EighteenOfWhichAMatchDeals()
        {
            Assert.That(TaliCardContent.AllDefinitions.Count, Is.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(TaliCardContent.DealtDefinitions.Count, Is.EqualTo(18),
                "p.6 returns the Handicap card to the box at setup.");
            Assert.That(
                TaliCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(TaliCardContent.IceShard));
            Assert.That(
                TaliCardContent.AllDefinitions.Select(definition => definition.Id).Distinct().Count(),
                Is.EqualTo(19),
                "Every face has its own identity; the upgrade pairs reference them by name.");
        }

        [Test]
        public void TheStartingHandIsThreeTierOnesAndTheTwoBasics()
        {
            Assert.That(TaliCardContent.StartingHand, Is.EquivalentTo(new[]
            {
                TaliCardContent.IceBlast,
                TaliCardContent.CommuneWithSpirits,
                TaliCardContent.WinterDagger,
                TaliCardContent.ColdSnap,
                TaliCardContent.GlacialBarrier
            }), "p.6 step 11: the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(TaliCardContent.Deck.Count, Is.EqualTo(13),
                "Six Tier II, six Tier III and the Ultimate.");
            Assert.That(
                TaliCardContent.StartingHand.Intersect(TaliCardContent.Deck),
                Is.Empty,
                "A card is dealt to the hand or left in the deck, never both.");
        }

        [TestCase("ice-blast", 11, 1, 1, PrintedValueChannel.Radius, 4)]
        [TestCase("ice-shard", 10, 1, 1, PrintedValueChannel.Radius, 3)]
        [TestCase("commune-with-spirits", 13, 2, 0, PrintedValueChannel.None, 0)]
        [TestCase("winter-dagger", 8, 6, 4, PrintedValueChannel.None, 0)]
        [TestCase("winter-spear", 9, 6, 4, PrintedValueChannel.None, 0)]
        [TestCase("spirit-wolf", 9, 3, 4, PrintedValueChannel.Range, 2)]
        [TestCase("winter-scepter", 9, 7, 4, PrintedValueChannel.None, 0)]
        [TestCase("spirit-bear", 9, 4, 4, PrintedValueChannel.Range, 2)]
        [TestCase("cold-snap", 9, 5, 3, PrintedValueChannel.Radius, 2)]
        [TestCase("snowstorm", 10, 5, 3, PrintedValueChannel.Radius, 3)]
        [TestCase("guardian-spirit", 10, 5, 3, PrintedValueChannel.Radius, 4)]
        [TestCase("blizzard", 10, 6, 3, PrintedValueChannel.Radius, 3)]
        [TestCase("warrior-spirit", 10, 6, 3, PrintedValueChannel.Radius, 4)]
        [TestCase("glacial-barrier", 4, 3, 2, PrintedValueChannel.Radius, 3)]
        [TestCase("wall-of-frost", 3, 3, 2, PrintedValueChannel.Radius, 3)]
        [TestCase("ancestral-totem", 3, 3, 2, PrintedValueChannel.Range, 2)]
        [TestCase("pack-ice", 3, 4, 2, PrintedValueChannel.Radius, 3)]
        [TestCase("venerated-totem", 3, 4, 2, PrintedValueChannel.Range, 2)]
        public void EachFacePrintsTheValuesTheInventoryRead(
            string cardId,
            int initiative,
            int secondaryDefense,
            int secondaryMovement,
            PrintedValueChannel iconChannel,
            int iconValue)
        {
            var definition = TaliCardContent.Definition(new CardDefinitionId("tali-" + cardId));

            Assert.That(definition.Initiative.Value, Is.EqualTo(initiative), "Printed Initiative.");

            var defense = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Defense);
            Assert.That(defense, Is.Not.Null, "Every face but the Ultimate prints a Defense secondary.");
            Assert.That(
                defense.BaseValues.Single(value => value.Channel == PrintedValueChannel.Defense).Amount,
                Is.EqualTo(secondaryDefense));

            var movement = definition.ActionDefinitions.FirstOrDefault(action =>
                action.Kind == ActionKind.Movement &&
                action.Origin == ActionDefinitionOrigin.PrintedSecondary);
            if (secondaryMovement == 0)
            {
                Assert.That(movement, Is.Null,
                    "A dash in the shard's Sec Mov column is an absent action, not a zero value - " +
                    "and Commune with Spirits is the set's one instance, the one-icon ribbon whose " +
                    "lone glyph is a SHIELD read by shape rather than by slot.");
            }
            else
            {
                Assert.That(
                    movement.BaseValues
                        .Single(value => value.Channel == PrintedValueChannel.Movement).Amount,
                    Is.EqualTo(secondaryMovement));
            }

            var primary = definition.ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            if (iconChannel == PrintedValueChannel.None)
            {
                Assert.That(
                    primary.BaseValues.Any(value =>
                        value.Channel == PrintedValueChannel.Range ||
                        value.Channel == PrintedValueChannel.Radius),
                    Is.False,
                    "A face printing neither reach icon declares neither.");
            }
            else
            {
                Assert.That(
                    primary.BaseValues.Single(value => value.Channel == iconChannel).Amount,
                    Is.EqualTo(iconValue),
                    "p.9's Range/Radius icon, told apart from the other by the card's own wording.");
            }
        }

        [TestCase("ice-blast", 3)]
        [TestCase("ice-shard", 2)]
        [TestCase("winter-dagger", 2)]
        [TestCase("winter-spear", 3)]
        [TestCase("spirit-wolf", 3)]
        [TestCase("winter-scepter", 4)]
        [TestCase("spirit-bear", 3)]
        public void EachAttackPrintsItsBaseValue_WithTheRulebooksPlusCueLeftToTheCardText(
            string cardId, int attack)
        {
            var primary = TaliCardContent.Definition(new CardDefinitionId("tali-" + cardId))
                .ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(
                primary.BaseValues.Single(value => value.Channel == PrintedValueChannel.Attack).Amount,
                Is.EqualTo(attack),
                "p.13 and p.14 both say the '+' sign signifies a value the card text can increase, so " +
                "the printed value is the base and the +3 is card text - Tali is the first hero in " +
                "this project to print the cue on an Attack.");
        }

        [Test]
        public void HerOnlyBasicAttackIsIceBlast_AndHerOnlyBasicSkillIsCommune()
        {
            var basics = TaliCardContent.AllDefinitions
                .Where(definition =>
                    definition.Progression.Classification == CardClassification.Basic)
                .ToArray();

            Assert.That(basics.Select(definition => definition.Id), Is.EquivalentTo(new[]
            {
                TaliCardContent.IceBlast, TaliCardContent.IceShard, TaliCardContent.CommuneWithSpirits
            }), "p.9: the Gold and Silver cards are basic and all other cards are non-basic.");

            Assert.That(
                TaliCardContent.Definition(TaliCardContent.IceBlast).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind,
                Is.EqualTo(ActionKind.Attack),
                "Reign of Winter's trigger reads 'your basic attack', and this is the only one a " +
                "dealt deck has.");
            Assert.That(
                TaliCardContent.Definition(TaliCardContent.CommuneWithSpirits).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind,
                Is.EqualTo(ActionKind.Skill));
        }

        [Test]
        public void TheFourRangedFacesAreExactlyTheFourPrintingARangeIcon()
        {
            var printRange = TaliCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.BaseValues.Any(value => value.Channel == PrintedValueChannel.Range)))
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(printRange, Is.EquivalentTo(new[]
            {
                TaliCardContent.SpiritWolf, TaliCardContent.SpiritBear,
                TaliCardContent.AncestralTotem, TaliCardContent.VeneratedTotem
            }), "p.9: Ranged primaries carry a range icon; every Radius icon belongs to a non-Ranged " +
                "action.");

            // The classification is an Attack's, so only the two Ranged Attacks carry it. Her two
            // Ranged Skills are Ranged by their printed icon and their own wording, which is what p.9
            // gives a Skill - there is no Skill range classification to declare.
            foreach (var cardId in new[] { TaliCardContent.SpiritWolf, TaliCardContent.SpiritBear })
            {
                Assert.That(
                    TaliCardContent.Definition(cardId).ActionDefinitions
                        .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .AttackRange,
                    Is.EqualTo(AttackRangeKind.Ranged));
            }
        }

        [TestCase("winter-dagger", "winter-spear", "spirit-wolf")]
        [TestCase("winter-spear", "winter-scepter", "spirit-bear")]
        [TestCase("spirit-wolf", "winter-scepter", "spirit-bear")]
        [TestCase("cold-snap", "snowstorm", "guardian-spirit")]
        [TestCase("snowstorm", "blizzard", "warrior-spirit")]
        [TestCase("guardian-spirit", "blizzard", "warrior-spirit")]
        [TestCase("glacial-barrier", "wall-of-frost", "ancestral-totem")]
        [TestCase("wall-of-frost", "pack-ice", "venerated-totem")]
        [TestCase("ancestral-totem", "pack-ice", "venerated-totem")]
        public void EachUpgradeOffersThePairItsColourAndTierName(
            string fromId, string firstCandidate, string secondCandidate)
        {
            Assert.That(
                TaliCardContent.Definition(new CardDefinitionId("tali-" + fromId))
                    .Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[]
                {
                    new CardDefinitionId("tali-" + firstCandidate),
                    new CardDefinitionId("tali-" + secondCandidate)
                }),
                "p.15 upgrades by colour and Tier, so each Level Up offers the pair one Tier up in " +
                "that colour.");
        }

        [Test]
        public void TheUltimateIsTierFour_PrintsNoAction_AndCarriesItsRadius()
        {
            var ultimate = TaliCardContent.Definition(TaliCardContent.ReignOfWinter);

            Assert.That(ultimate.Progression.Tier, Is.EqualTo(CardTier.IV));
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "p.16's Ultimate provides a permanent ability rather than an action - which is also " +
                "why RULING 21's Purple naming is legal and inert.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);
            Assert.That(
                ultimate.UltimateRule.PrintedValues
                    .Single(value => value.Channel == PrintedValueChannel.Radius).Amount,
                Is.EqualTo(4),
                "Ledger question 12's reading needs this printed icon for a +1 Radius item to make it 5.");
        }

        [Test]
        public void EveryFaceIsTranscribed_AndNoFaceInventsText()
        {
            foreach (var definition in TaliCardContent.AllDefinitions)
            {
                Assert.That(definition.SourceContent.PrintedText, Is.Not.Null.And.Not.Empty,
                    $"'{definition.Id}' ships the words its face prints.");
                Assert.That(definition.ContentAuthority,
                    Is.EqualTo(CardContentAuthority.VerifiedOfficial));
                Assert.That(definition.ContentRevision, Is.EqualTo(TaliCardContent.ContentRevision));
            }
        }

        [Test]
        public void EightFacesReadTheirOwnDiscardState_WhichIsWhatMakesCommuneTheHero()
        {
            foreach (var cardId in TaliCardContent.ReadsItsOwnDiscardState)
            {
                var text = TaliCardContent.Definition(cardId).SourceContent.PrintedText;
                Assert.That(
                    text.Contains("in the discard") || text.Contains("in your discard"),
                    Is.True,
                    $"'{cardId}' is listed as reading its own discard state, so its text must say so.");
            }

            Assert.That(TaliCardContent.ReadsItsOwnDiscardState.Count, Is.EqualTo(8));
        }

        /// <summary>
        /// Faces whose whole printed debt beyond targeting is a dynamic value contribution
        /// (<see cref="RosterValueContributionDefinition"/>) rather than an effect node - Sabina's
        /// identically-shaped Quickdraw family is the precedent, and her own verified-content test
        /// asserts the same exception by name rather than folding it into a general rule.
        /// </summary>
        private static readonly CardDefinitionId[] ComposedWithNoEffectRoot =
        {
            TaliCardContent.WinterDagger, TaliCardContent.WinterSpear, TaliCardContent.WinterScepter
        };

        /// <summary>
        /// The composed-versus-deferred boundary, asserted in both directions so it cannot drift.
        ///
        /// <para>A face marked composed must actually have an effect definition OR a dynamic value
        /// contribution (see <see cref="ComposedWithNoEffectRoot"/>), and a face listed as owed must be
        /// marked deferred with the reason the effect content gives. The failure this catches is the
        /// quiet one: a definition claiming its text is composed while the effect content returns null
        /// for it and declares no value contribution either, which would ship a card that silently does
        /// nothing.</para>
        /// </summary>
        [Test]
        public void EveryFaceIsEitherComposedOrExplicitlyDeferred_WithNoDisagreementBetweenTheTwo()
        {
            foreach (var definition in TaliCardContent.AllDefinitions)
            {
                var primary = definition.ActionDefinitions
                    .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (primary == null)
                {
                    // The Ultimate carries its debt on the rule rather than on an action. Milestone
                    // 8.37 composes Reign of Winter's rule, so it too must now agree
                    // with CompositionOwed rather than being asserted always-awaited.
                    var ultimateOwed = TaliEffectContent.CompositionOwed.ContainsKey(definition.Id);
                    Assert.That(definition.UltimateRule.TextComposition.IsAwaited, Is.EqualTo(ultimateOwed),
                        $"'{definition.Id}' Ultimate rule must be marked deferred exactly when the " +
                        "effect content lists it as owed.");
                    continue;
                }

                var owed = TaliEffectContent.CompositionOwed.ContainsKey(definition.Id);
                Assert.That(primary.TextComposition.IsAwaited, Is.EqualTo(owed),
                    $"'{definition.Id}' must be marked deferred exactly when the effect content lists " +
                    "it as owed.");

                if (owed)
                {
                    Assert.That(
                        primary.TextComposition.NamedTrigger,
                        Is.EqualTo(TaliEffectContent.CompositionOwed[definition.Id]),
                        "The deferral names the piece the effect content says it waits on.");
                }
                else if (ComposedWithNoEffectRoot.Contains(definition.Id))
                {
                    Assert.That(primary.EffectRoot, Is.Null,
                        $"'{definition.Id}' composes no card text: its +3 is a value contribution, " +
                        "never a step.");
                    Assert.That(primary.RosterValueContributions, Is.Not.Empty,
                        $"'{definition.Id}' claims its text is composed, so its dynamic value must be " +
                        "declared.");
                }
                else
                {
                    Assert.That(primary.EffectRoot, Is.Not.Null,
                        $"'{definition.Id}' claims its text is composed, so it must have one.");
                }
            }
        }

        [Test]
        public void TheTaliDealPrepCompositionsAreExactlyTheseSeventeenFaces()
        {
            var composed = TaliCardContent.AllDefinitions
                .Where(definition => !TaliEffectContent.CompositionOwed.ContainsKey(definition.Id))
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(composed, Is.EquivalentTo(new[]
            {
                // Phase one's Green line.
                TaliCardContent.GlacialBarrier, TaliCardContent.WallOfFrost, TaliCardContent.PackIce,
                TaliCardContent.AncestralTotem, TaliCardContent.VeneratedTotem,

                // Milestone 8.37's twelve.
                TaliCardContent.IceBlast, TaliCardContent.CommuneWithSpirits,
                TaliCardContent.WinterDagger, TaliCardContent.WinterSpear, TaliCardContent.WinterScepter,
                TaliCardContent.SpiritWolf, TaliCardContent.SpiritBear,
                TaliCardContent.ColdSnap, TaliCardContent.Snowstorm,
                TaliCardContent.GuardianSpirit, TaliCardContent.WarriorSpirit,
                TaliCardContent.ReignOfWinter
            }), "Seventeen of her nineteen faces are composed; only p.20's parked Handicap card and " +
                "Blizzard's second, independent sentence remain deferred, each with a named, still-true " +
                "reason in CompositionOwed.");
        }

        /// <summary>
        /// RULING 15's turn-start membership sampling is about "This turn: enemy heroes in radius
        /// [suffer X]" effects, and the charter asked for this to be confirmed rather than assumed:
        /// <b>Tali's set prints no such restriction anywhere</b>, so nothing of Arien's corrective
        /// transfers to her.
        ///
        /// <para>Her Ice penalty is a <em>token aura</em> and not an active-effect restriction - a
        /// standing board-object modifier whose adjacency is re-measured every time the live
        /// modification set is derived, which is what p.7's mid-turn Initiative re-check needs in order
        /// to see it at all. There is no membership to sample.</para>
        /// </summary>
        [Test]
        public void HerOnlyActiveEffectKeywordIsBlizzardsEndOfTurn_SoRulingFifteenReachesNothingOfHers()
        {
            foreach (var definition in TaliCardContent.AllDefinitions)
            {
                var text = definition.SourceContent.PrintedText;
                Assert.That(text, Does.Not.Contain("This turn:"),
                    $"'{definition.Id}' would be the first Tali face to print one.");
                Assert.That(text, Does.Not.Contain("This round:"),
                    $"'{definition.Id}' would be the first Tali face to print one.");
                Assert.That(text, Does.Not.Contain("Next turn:"),
                    $"'{definition.Id}' would be the first Tali face to print one.");
            }

            var endOfTurn = TaliCardContent.AllDefinitions
                .Where(definition => definition.SourceContent.PrintedText.Contains("End of turn:"))
                .Select(definition => definition.Id)
                .ToArray();
            Assert.That(endOfTurn, Is.EquivalentTo(new[] { TaliCardContent.Blizzard }),
                "Blizzard's discard-gated repeat is her one Active-effect keyword, and it is a p.17 " +
                "one-shot deferred application rather than a duration.");
        }

        /// <summary>
        /// RULING 21's Purple arm, cross-checked against the contract that has to honour it rather
        /// than asserted about Tali alone.
        ///
        /// <para>The ruling is that all six p.9 colours are nameable and Purple is <b>inert</b>: the
        /// naming step completes and the perform step finds nothing, because an Ultimate has no
        /// performable primary action. Ursafar's borrow contract answers that from its own side with
        /// <c>BorrowRefusal.NoPrintedPrimaryAction</c>, and this asserts the two meet - which is the
        /// only place in either worker's output where they can be checked against each other.</para>
        /// </summary>
        [Test]
        public void NamingPurpleIsLegalAndInert_BecauseTheBorrowContractRefusesAnUltimate()
        {
            Assert.That(
                BorrowedPrimaryActionDeclaration.Refuses(
                    TaliCardContent.Definition(TaliCardContent.ReignOfWinter)),
                Is.EqualTo(BorrowRefusal.NoPrintedPrimaryAction),
                "RULING 21: a Purple naming completes and the perform step finds nothing. The " +
                "contract already answers that from its own side, so the inert half of the ruling " +
                "costs no new code.");
        }

        /// <summary>
        /// <b>THE ENGINE GAP RULING 21 OPENED IS NOW CLOSED, pinned here so a reader finds the closure
        /// rather than the debt.</b>
        ///
        /// <para>This test used to fail the moment <see cref="CardColor.Purple"/> was added, by design -
        /// "whoever adds it reads this and the reasoning above". Milestone 8.37 is that
        /// milestone: <see cref="CardColor"/> now has six printed colours plus <see cref="CardColor.None"/>,
        /// and the assertion below is the flipped form the original comment named as its own successor -
        /// every one of p.9's six colours, Purple included, builds a legal borrow pool.</para>
        /// </summary>
        [Test]
        public void CardColourNowHasSixPrintedColours_AndAllSixNameCommuneAPool()
        {
            Assert.That(
                System.Enum.GetValues(typeof(CardColor)).Length,
                Is.EqualTo(7),
                "Six printed colours plus None. RULING 21's sixth printed colour - Purple - is " +
                "wired at milestone 8.37.");

            foreach (var color in new[]
            {
                CardColor.Gold, CardColor.Silver, CardColor.Red, CardColor.Green, CardColor.Blue,
                CardColor.Purple
            })
            {
                Assert.That(
                    BorrowedPrimaryActionDeclaration.FromDiscardedCards(color).RequiredColor,
                    Is.EqualTo(color),
                    "Arty (8/15): \"Naming the color is not [optional]. The action you are performing " +
                    "is referring to the color, not the card you may or may not have chosen to " +
                    "discard.\" So every nameable colour must build a legal pool whether or not she " +
                    "holds one - all six now do, RULING 21's Purple included.");
            }

            Assert.That(
                () => CardProgressionProfile.NonBasicCard(
                    CardColor.Purple, CardTier.I, PrintedValueChannel.None),
                Throws.ArgumentException,
                "Purple is a colour the naming step may offer, never a colour a hand card may declare - " +
                "see CardColor's own remarks.");
        }

        [Test]
        public void ASeatsDealtCardsAreTheEighteen_FiveInHandAndThirteenInTheDeck()
        {
            var hand = TaliCardContent.CreateHand(Owner, "tali-seat");

            Assert.That(hand.Definitions.Count, Is.EqualTo(18));
            Assert.That(hand.Instances.Count(), Is.EqualTo(5));
            Assert.That(hand.DeckInstances.Count(), Is.EqualTo(13));
            Assert.That(
                hand.Instances.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(TaliCardContent.StartingHand));
        }
    }
}
