using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75d. The repeat's offer excludes what its printed clause excludes, on an Attack
    /// whose target is named by CARD TEXT.
    ///
    /// <para><b>The gap, as milestone 8.75c recorded it (ID-Q10c).</b> Loosely-Aimed Firebolts prints
    /// "May repeat once on a different hero" and Imminent Eruption "May repeat once on a minion".
    /// Rulebook p.13's own target question applies both halves of such a clause - the kind it admits
    /// and the unit the asking instance took - but every Attack of Ignatia's reaches p.14's step 1
    /// with NOTHING bound (<c>AttackTargetBinding.ByCardText</c>) and names a unit from its own
    /// printed subject instead. The repeated instance re-ran that subject whole, and the subject has
    /// never heard of the clause that produced it: the offer still admitted the hero the first
    /// instance attacked, and the kind narrowing did not reach it at all. That is the
    /// offer-admits-more-than-the-answer-can-perform class of milestones 8.29d, 8.29e, 8.35z5 and
    /// 8.35z15.</para>
    ///
    /// <para><b>Where the narrowing now travels, and why nothing else moved.</b> The clause is read
    /// off the trusted definition into <see cref="EnclosingAttackFacts.RepeatedSubject"/> at the one
    /// place those facts are built, and consulted at the one place every selection's offer AND answer
    /// are already narrowed together - <see cref="EffectSelectionEligibilityQuery"/>. So one reading
    /// serves the live pause, the answer, the feasibility walk and the snapshot restore, and a
    /// selection that does not name this Attack's target is untouched.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaRepeatOfferTests
    {
        private static readonly HexCoordinate AdjacentInLine = new HexCoordinate(1, 0);
        private static readonly HexCoordinate AdjacentBehind = new HexCoordinate(0, -1);
        private static readonly HexCoordinate AdjacentAbove = new HexCoordinate(0, 1);
        private static readonly HexCoordinate TwoInLine = new HexCoordinate(2, 0);
        private static readonly HexCoordinate ThreeInLine = new HexCoordinate(3, 0);
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);
        private static readonly HexCoordinate OffTheFile = new HexCoordinate(1, 6);

        // -------------------------------------------------------------------------------------------
        // "May repeat once on a different hero" - Loosely-Aimed Firebolts' moon arm
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The NOUN half: "on a different HERO" narrows the repeated instance to p.19's heroes, so the
        /// minion the same printed sentence offered the first instance is not offered the second.
        ///
        /// <para>Her moon line is "Target a unit in range in a straight line" - a UNIT, which is why
        /// the minion three spaces along the file is a legal first subject and why the narrowing has
        /// something to take away.</para>
        /// </summary>
        [Test]
        public void LooselyAimedsRepeat_OffersHeroesOnly_WhereItsPrintedInstanceOfferedUnits()
        {
            var printed = new IgnatiaFrame(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, StraightFile());
            Assert.That(
                printed.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value,
                    IgnatiaBoard.SecondEnemy.Value,
                    IgnatiaBoard.EnemyMinion.Value
                }),
                "Arrange: the printed instance's ☾ sentence names UNITS in a straight line, so both " +
                "enemy heroes and the enemy minion on the file qualify.");

            var repeated = Repeat(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, StraightFile(),
                AttackTarget.Minion(IgnatiaBoard.EnemyMinion));
            Assert.That(
                repeated.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.SecondEnemy.Value
                }),
                "\"on a different HERO\" - the repeated instance may take a hero and nothing else, " +
                "so the minion the identical printed sentence offered a moment ago is gone.");
        }

        /// <summary>
        /// The DIFFERENT half: the hero the asking instance took is out of the repeat's set, which is
        /// the exclusion rulebook p.13's own question already makes for an Attack that asks it.
        /// </summary>
        [Test]
        public void LooselyAimedsRepeat_ExcludesTheHeroTheAskingInstanceTook()
        {
            var repeated = Repeat(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, StraightFile(),
                AttackTarget.Hero(IgnatiaBoard.Enemy));
            Assert.That(
                repeated.Candidates(),
                Is.EqualTo(new[] { IgnatiaBoard.SecondEnemy.Value }),
                "\"on a DIFFERENT hero\": the hero this instance attacked is excluded, the minion is " +
                "the wrong kind, and the second enemy hero is what is left.");
        }

        /// <summary>
        /// The answer agrees with the offer, which is what makes this a rule rather than a
        /// presentation: the excluded hero is refused when it arrives as an answer, by the same method
        /// that left it out of the offer.
        /// </summary>
        [Test]
        public void LooselyAimedsRepeat_RefusesTheExcludedHeroAsAnAnswerToo()
        {
            var repeated = Repeat(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, StraightFile(),
                AttackTarget.Hero(IgnatiaBoard.Enemy));
            var selection = IgnatiaBoard.PrimaryText(IgnatiaCardContent.LooselyAimedFirebolts)
                .Nodes.Single(node => node.Id.Value == "ignatia-attack-moon-select");

            Assert.That(
                EffectSelectionEligibilityQuery.AreStillEligible(
                    selection,
                    new[] { EffectValue.Hero(IgnatiaBoard.Enemy) },
                    repeated.Step.Frame,
                    Array.Empty<EffectNodeOutcome>(),
                    repeated.Context),
                Is.False,
                "A piece the producing clause left out of the offer cannot be submitted to it.");

            Assert.That(
                EffectSelectionEligibilityQuery.AreStillEligible(
                    selection,
                    new[] { EffectValue.Minion(IgnatiaBoard.EnemyMinion) },
                    repeated.Step.Frame,
                    Array.Empty<EffectNodeOutcome>(),
                    repeated.Context),
                Is.False,
                "and neither can a unit of the kind the clause does not name.");

            Assert.That(
                EffectSelectionEligibilityQuery.AreStillEligible(
                    selection,
                    new[] { EffectValue.Hero(IgnatiaBoard.SecondEnemy) },
                    repeated.Step.Frame,
                    Array.Empty<EffectNodeOutcome>(),
                    repeated.Context),
                Is.True,
                "while the hero the clause does name is answerable, so this narrows and never bars.");
        }

        /// <summary>
        /// A board the clause empties: her printed sentence still names somebody, the clause admits
        /// nobody, and the step reports itself impossible rather than opening a question with nothing
        /// in it.
        ///
        /// <para>This is what p.10's offer rests on. The repeat is a `may`, and milestone 8.35z15's
        /// rule is that it is not offered where the instance it would start cannot begin - so the same
        /// verdict is asked of <see cref="EffectOptionFeasibilityQuery"/>, which is the query the
        /// card-action authority now puts to a planned repeat of a card-text-bound Attack.</para>
        /// </summary>
        [Test]
        public void LooselyAimedsRepeat_IsImpossibleWhereTheClauseAdmitsNobody()
        {
            var board = OneHeroOnTheFile();
            var printed = new IgnatiaFrame(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, board);
            Assert.That(
                printed.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }),
                "Arrange: the printed sentence names two units on the file, so nothing here is about " +
                "an empty board.");

            var repeated = Repeat(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, OneHeroOnTheFile(),
                AttackTarget.Hero(IgnatiaBoard.Enemy));
            Assert.That(repeated.Choice, Is.Null,
                "The one hero on the file is the one this instance took and the other unit is a " +
                "minion, so \"a different hero\" has nobody at all.");
            Assert.That(
                repeated.Outcome("ignatia-attack-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and a mandatory step with nothing to name stops the action rather than pausing.");

            Assert.That(
                CanBindATarget(
                    IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon,
                    OneHeroOnTheFile(), AttackTarget.Hero(IgnatiaBoard.Enemy)),
                Is.False,
                "so the walk that decides whether p.10's repeat is offered at all reports the same " +
                "thing, before the offer is made.");

            Assert.That(
                CanBindATarget(
                    IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon,
                    StraightFile(), AttackTarget.Hero(IgnatiaBoard.Enemy)),
                Is.True,
                "and on the board with a second hero on the file it reports the opposite, which is " +
                "what keeps this a narrowing rather than a suppression.");
        }

        // -------------------------------------------------------------------------------------------
        // "May repeat once on a minion" - Imminent Eruption's sun arm
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The twin at the other end of p.19's kinds. Her sun line is "Target a unit adjacent to you",
        /// so the printed instance may take the enemy hero in her ring - and "on a MINION" then admits
        /// the minions in that ring and nobody else.
        /// </summary>
        [Test]
        public void ImminentEruptionsRepeat_OffersMinionsOnly_AndNotTheHeroItsOwnInstanceTook()
        {
            var printed = new IgnatiaFrame(
                IgnatiaCardContent.ImminentEruption, IgnatiaBoard.Sun, TheRing());
            Assert.That(
                printed.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value,
                    IgnatiaBoard.EnemyMinion.Value,
                    IgnatiaBoard.SecondEnemyMinion.Value
                }),
                "Arrange: ☀ \"a unit adjacent to you\" is p.19's ring of six and both kinds on it " +
                "qualify.");

            var repeated = Repeat(
                IgnatiaCardContent.ImminentEruption, IgnatiaBoard.Sun, TheRing(),
                AttackTarget.Hero(IgnatiaBoard.Enemy));
            Assert.That(
                repeated.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.EnemyMinion.Value, IgnatiaBoard.SecondEnemyMinion.Value
                }),
                "\"on a MINION\" - the hero the first instance attacked is excluded twice over, by " +
                "the noun and by the word \"different\" the ruling reads into it.");
        }

        /// <summary>
        /// The same clause excluding a minion: RULING 38's reading of the phrase is that the repeated
        /// minion is a DIFFERENT unit, so a first instance that took a minion leaves the other one.
        /// </summary>
        [Test]
        public void ImminentEruptionsRepeat_ExcludesTheMinionTheAskingInstanceTook()
        {
            var repeated = Repeat(
                IgnatiaCardContent.ImminentEruption, IgnatiaBoard.Sun, TheRing(),
                AttackTarget.Minion(IgnatiaBoard.EnemyMinion));
            Assert.That(
                repeated.Candidates(),
                Is.EqualTo(new[] { IgnatiaBoard.SecondEnemyMinion.Value }),
                "One minion in the ring is the one this instance took; the other is what is left, " +
                "and the hero is the wrong kind.");
        }

        // -------------------------------------------------------------------------------------------
        // The class, measured over the corpus rather than over the two cards that reported it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every Attack in the twenty-two-hero corpus that declares a repeat clause, beside the unit
        /// kind a repeated instance of it may take - read through the production derivation
        /// (<see cref="CardActionResolutionAuthority.EnclosingAttackOf"/>) rather than off the node.
        ///
        /// <para><b>This is the table that proves the fix moved one card and no other.</b> Every row
        /// but Imminent Eruption's reports exactly what it reported before milestone 8.75d. Hers did
        /// not: the single-clause fallback asked "does this card declare
        /// <see cref="RepeatTargetBinding.DifferentEnemyHero"/>" rather than "which binding does it
        /// declare", because when milestone 8.24 wrote it that was the only member narrowing a kind.
        /// Silverarrow's Rain of Arrows - the only other card carrying
        /// <see cref="RepeatTargetBinding.DifferentEnemyMinion"/> - prints a CHAIN and is answered by
        /// <c>CardTextRepeatChain</c>, so it never reached the fallback and the hole stayed shut until
        /// a card printed that clause on its own.</para>
        ///
        /// <para>A new row here is a new repeat clause somebody composed; a CHANGED row is this
        /// milestone's rule moving a card it was not measured against.</para>
        /// </summary>
        [Test]
        public void EveryRepeatInTheCorpus_NarrowsTheKindItsOwnClauseNames()
        {
            Assert.That(
                RepeatRows(),
                Is.EqualTo(new[]
                {
                    "arien|arien-violent-torrent|DifferentTarget|None",
                    "brynn|brynn-split-attack|DifferentUnitAdjacentToActor|None",
                    "brynn|brynn-split-throw|DifferentTarget|None",
                    "ignatia|ignatia-imminent-eruption|DifferentEnemyMinion|Minion",
                    "ignatia|ignatia-loosely-aimed-firebolts|DifferentEnemyHero|Hero",
                    "rowenna|rowenna-paragon-of-grace|DifferentTarget|None",
                    "rowenna|rowenna-paragon-of-valor|DifferentTarget|None",
                    "silverarrow|silverarrow-rain-of-arrows|DifferentEnemyHero|Hero",
                    "wasp|wasp-thunder-boomerang|DifferentTarget|None",
                    "whisper|whisper-blood-frenzy|DifferentTarget|None",
                    "whisper|whisper-blood-fury|DifferentTarget|None",
                    "xargatha|xargatha-cleave|DifferentEnemyHero|Hero",
                    "xargatha|xargatha-rapid-thrusts|DifferentEnemyHero|Hero"
                }),
                "A changed row means this milestone's reading moved a card it was not measured " +
                "against; a new row means a repeat clause arrived after it.");
        }

        /// <summary>
        /// The other half of the same class: which of those Attacks bind their target by CARD TEXT,
        /// and therefore reach the narrowing added at this milestone rather than rulebook p.13's own
        /// target question.
        ///
        /// <para><b>MEASURED over all twenty-two heroes' composed content, and it is exactly two.</b>
        /// Every other repeat-declaring Attack in the corpus asks p.13's question and has been correct
        /// since milestone 8.24. The two here are Ignatia's, which is why ID-Q10c was hers to close
        /// and why no earlier hero's repeat journey moves.</para>
        ///
        /// <para><b>And the guard the seam needs.</b> The card-text narrowing carries the KIND a
        /// clause admits and the units it excludes; what it does not carry is
        /// <see cref="RepeatTargetBinding.DifferentUnitAdjacentToActor"/>'s REACH narrowing, because
        /// the one card that declares it - Brynn's Split Attack - binds its target through p.13's own
        /// question and never reaches a card-text subject. A face that declared it AND bound by card
        /// text would be a shape nothing narrows, so it arrives here as a red row rather than as a
        /// silently unnarrowed offer.</para>
        /// </summary>
        [Test]
        public void OnlyIgnatiasTwoFaces_BothBindTheirTargetByCardTextAndDeclareARepeat()
        {
            var byCardText = CardTextBoundRepeatRows();

            Assert.That(
                byCardText,
                Is.EqualTo(new[]
                {
                    "ignatia|ignatia-imminent-eruption|DifferentEnemyMinion",
                    "ignatia|ignatia-loosely-aimed-firebolts|DifferentEnemyHero"
                }),
                "A new row is an Attack whose printed subject now has to obey a repeat clause. Drive " +
                "its narrowing before shipping the content.");

            Assert.That(
                byCardText.Any(row =>
                    row.EndsWith(
                        RepeatTargetBinding.DifferentUnitAdjacentToActor.ToString(),
                        StringComparison.Ordinal)),
                Is.False,
                "The card-text seam carries the KIND a clause admits and the units it excludes, and " +
                "not the reach narrowing Split Attack prints - which no card-text-bound Attack " +
                "declares. One that did would be unnarrowed, so this is where it stops.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One of her faces walked as the REPEATED instance its own clause produced, with the unit the
        /// asking instance took excluded - the frame the card-action authority builds for p.14's step 1
        /// of a repeat, assembled through the production derivation rather than by hand.
        /// </summary>
        private static IgnatiaFrame Repeat(
            CardDefinitionId cardId,
            TeamSide coin,
            AdjacentMoveAuthority board,
            AttackTarget taken) =>
            new IgnatiaFrame(
                cardId, coin, board,
                enclosingAttack: RepeatedAttackFacts(cardId, taken));

        /// <summary>
        /// What card text applied inside a repeated instance of <paramref name="cardId"/> may ask
        /// about the Attack around it, built by the one method a live match builds it with.
        /// </summary>
        private static EnclosingAttackFacts RepeatedAttackFacts(
            CardDefinitionId cardId, AttackTarget taken)
        {
            var face = IgnatiaBoard.Face(cardId);
            var attack = new PendingAttackResolution(
                // The identity a card-text repeat runs under - the asking instance's, with the
                // chain's own suffix, which is how a repeated instance knows its position.
                new ActionResolutionId("ignatia-" + cardId.Value + ":repeat"),
                IgnatiaBoard.FaceInstance,
                face.Reference,
                IgnatiaBoard.Primary(cardId).Id,
                IgnatiaBoard.Ignatia,
                ActionInvocationContext.Repeated,
                new ActionResolutionStepId("before-attack-text"),
                AttackResolutionPhase.ApplyBeforeAttackText,
                target: default(AttackTarget),
                attackValue: null,
                excludedTarget: taken);

            return CardActionResolutionAuthority.EnclosingAttackOf(
                attack, IgnatiaCardContent.AllDefinitions);
        }

        /// <summary>
        /// Whether the printed subject of a repeated instance could name anybody - the exact walk the
        /// card-action authority puts to a planned repeat before p.10's offer is made.
        /// </summary>
        private static bool CanBindATarget(
            CardDefinitionId cardId,
            TeamSide coin,
            AdjacentMoveAuthority board,
            AttackTarget taken)
        {
            var frame = new IgnatiaFrame(
                cardId, coin, board,
                startImmediately: false,
                enclosingAttack: RepeatedAttackFacts(cardId, taken));
            return EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding(
                EffectResolutionSession.BuildStartFrame(
                    new EffectResolutionId("probe-" + cardId.Value),
                    IgnatiaBoard.FaceInstance,
                    IgnatiaBoard.Face(cardId).Reference,
                    IgnatiaBoard.Primary(cardId).Id,
                    IgnatiaBoard.Ignatia,
                    IgnatiaBoard.Ignatia,
                    ActionInvocationContext.PrintedPrimary,
                    frame.Context),
                frame.Context);
        }

        /// <summary>Two enemy heroes and an enemy minion on one of her six files, in that order.</summary>
        private static AdjacentMoveAuthority StraightFile() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, AdjacentInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, TwoInLine)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, ThreeInLine)
                });

        /// <summary>The same file with ONE enemy hero on it, which is what empties the clause.</summary>
        private static AdjacentMoveAuthority OneHeroOnTheFile() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, AdjacentInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, OffTheFile)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, ThreeInLine)
                });

        /// <summary>One enemy hero and two enemy minions in p.19's ring of six around her.</summary>
        private static AdjacentMoveAuthority TheRing() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, AdjacentInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, OffTheFile)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, AdjacentBehind),
                    IgnatiaBoard.RedMinion(IgnatiaBoard.SecondEnemyMinion, AdjacentAbove)
                });

        private static string[] RepeatRows() => Corpus()
            .SelectMany(entry => RepeatDeclaringAttacks(entry.Card)
                .Select(pair => $"{entry.Owner}|{entry.Card.Id.Value}|{pair.Binding}|" +
                    $"{AdmittedKindOf(entry.Card, pair.Action)}"))
            .Distinct()
            .OrderBy(row => row, StringComparer.Ordinal)
            .ToArray();

        private static string[] CardTextBoundRepeatRows() => Corpus()
            .SelectMany(entry => RepeatDeclaringAttacks(entry.Card)
                .Where(pair => pair.Action.AttackTargetPolicy.HasValue &&
                    pair.Action.AttackTargetPolicy.Value.TargetIsBoundByCardText)
                .Select(pair => $"{entry.Owner}|{entry.Card.Id.Value}|{pair.Binding}"))
            .Distinct()
            .OrderBy(row => row, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// The kind a repeated instance of this action may take, read through the production
        /// derivation the live card text consults.
        /// </summary>
        private static BoardObjectKind AdmittedKindOf(CardDefinition card, ActionDefinition action)
        {
            var attack = new PendingAttackResolution(
                new ActionResolutionId(card.Id.Value + ":repeat"),
                new CardInstanceId("repeat-row-instance"),
                card.Reference,
                action.Id,
                new HeroId("repeat-row-actor"),
                ActionInvocationContext.Repeated,
                new ActionResolutionStepId("before-attack-text"),
                AttackResolutionPhase.ApplyBeforeAttackText,
                target: default(AttackTarget),
                attackValue: null);

            return CardActionResolutionAuthority
                .EnclosingAttackOf(attack, new[] { card })
                .RepeatedSubject.AdmittedKind;
        }

        private static IEnumerable<(ActionDefinition Action, RepeatTargetBinding Binding)>
            RepeatDeclaringAttacks(CardDefinition card) =>
            (card.ActionDefinitions ?? Array.Empty<ActionDefinition>())
                .Where(action => action.Kind == ActionKind.Attack)
                .SelectMany(action => Bodies(action)
                    .Where(body => body != null)
                    .SelectMany(body => body.Nodes)
                    .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                    .Select(node => (Action: action, node.RepeatTargetBinding))
                    .Take(1));

        private static IEnumerable<EffectDefinition> Bodies(ActionDefinition action)
        {
            yield return action.EffectRoot;
            yield return action.CardTextAt(action.CardTextTiming);
            yield return action.CardTextAt(action.SecondCardTextTiming);
        }

        /// <summary>
        /// Every hero's composed content, dealt or not - the same enumeration
        /// <c>AttackTargetBindingConditionalChooseOfferSweepTests</c> keeps, and kept for the same
        /// reason: a hero whose line is missing is a corpus nothing measures.
        /// </summary>
        private static IEnumerable<(string Owner, CardDefinition Card)> Corpus()
        {
            foreach (var card in ArienCardContent.AllDefinitions) yield return ("arien", card);
            foreach (var card in BroganCardContent.AllDefinitions) yield return ("brogan", card);
            foreach (var card in SabinaCardContent.AllDefinitions) yield return ("sabina", card);
            foreach (var card in TigerclawCardContent.AllDefinitions) yield return ("tigerclaw", card);
            foreach (var card in WaspCardContent.AllDefinitions) yield return ("wasp", card);
            foreach (var card in XargathaCardContent.AllDefinitions) yield return ("xargatha", card);
            foreach (var card in DodgerCardContent.AllDefinitions) yield return ("dodger", card);
            foreach (var card in MisaCardContent.AllDefinitions) yield return ("misa", card);
            foreach (var card in SilverarrowCardContent.AllDefinitions) yield return ("silverarrow", card);
            foreach (var card in UrsafarCardContent.AllDefinitions) yield return ("ursafar", card);
            foreach (var card in TaliCardContent.AllDefinitions) yield return ("tali", card);
            foreach (var card in WhisperCardContent.AllDefinitions) yield return ("whisper", card);
            foreach (var card in SwiftCardContent.AllDefinitions) yield return ("swift", card);
            foreach (var card in CutterCardContent.AllDefinitions) yield return ("cutter", card);
            foreach (var card in BainCardContent.AllDefinitions) yield return ("bain", card);
            foreach (var card in GarrusCardContent.AllDefinitions) yield return ("garrus", card);
            foreach (var card in WidgetCardContent.AllDefinitions) yield return ("widget", card);
            foreach (var card in RowennaCardContent.AllDefinitions) yield return ("rowenna", card);
            foreach (var card in MinCardContent.AllDefinitions) yield return ("min", card);
            foreach (var card in BrynnCardContent.AllDefinitions) yield return ("brynn", card);
            foreach (var card in HanuCardContent.AllDefinitions) yield return ("hanu", card);
            foreach (var card in IgnatiaCardContent.AllDefinitions) yield return ("ignatia", card);
        }
    }
}
