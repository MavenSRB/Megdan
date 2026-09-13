using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40a. Rulebook p.13's Life cost charged to the hero who WEARS a declared marker -
    /// Bain's Dead or Alive and Side Quest, "a hero with a [bounty] bounty marker spends 1 additional
    /// [life] life counter when defeated".
    ///
    /// <para><b>The family existed and the read did not, which is the whole milestone.</b>
    /// <c>RuleModificationKind.AdditionalDefeatLifeCost</c> was minted at 8.35z31 for Ursafar's Tear and
    /// its own remarks say it "is declared about the <em>attacker</em>"; p.13's spend site asked it
    /// about the actor and nobody else. <c>RuleModificationQuery.FromHeldMarkers</c> has made every
    /// marker property affect <c>RuleObject.ForHero(marker.HolderHeroId)</c> since 8.23, so the
    /// declaration was already landing on the right object and was simply never consulted. These cases
    /// prove the set the spend site is handed already carried it, that p.13 now reads it, and that
    /// Tear's attacker-side meaning did not move.</para>
    ///
    /// <para><b>The board is featureless on purpose.</b> A radius-3 disk with two heroes on it and
    /// nothing else, on <c>DefeatMarkerReturnTests</c>' terms: on the official map a case could pass
    /// because terrain or a zone boundary removed a candidate the clause had nothing to do with.</para>
    ///
    /// <para><b>The modification set is DERIVED rather than assembled wherever a case can afford it.</b>
    /// Every whole-defeat case below passes the set <c>CardActionResolutionAuthority.CurrentModifications</c>
    /// produces from the live marker register, which is exactly the set a configured match hands the
    /// spend site - the fidelity rule this project pays for when a probe and the shipped path diverge.
    /// </para>
    /// </summary>
    public sealed class BainMarkerDefeatCostTests
    {
        // -------------------------------------------------------------------------------------------
        // (a) The declaration reaches the set the spend site is handed
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The probe this milestone ordered, asserted rather than reasoned about: a bounty marker held
        /// by a hero puts its declaration into the derived set, scoped to the HOLDER, and leaves the
        /// attacker-scoped question answering exactly what it answered before.
        /// </summary>
        [Test]
        public void AHeldBountyMarker_DeclaresItsCostAgainstTheHolderInTheDerivedSet()
        {
            var fixture = new BountyDefeatFixture();
            fixture.GiveBountyTo(fixture.DefenderId);

            var set = fixture.DerivedModifications();

            Assert.That(
                set.Modifications.Single(modification =>
                        modification.Modification.Kind ==
                        RuleModificationKind.AdditionalDefeatLifeCostWhenDefeated)
                    .AffectedObject,
                Is.EqualTo(RuleObject.ForHero(fixture.DefenderId)),
                "FromHeldMarkers affects the marker's HOLDER, so the printed sentence's subject is " +
                "already the object the declaration lands on.");
            Assert.That(
                set.AdditionalDefeatLifeCost(RuleObject.ForHero(fixture.DefenderId)), Is.Zero,
                "The ATTACKER-side family is Tear's, and the marker declares nothing of it - which " +
                "is why p.13's attacker-only read could not see the bounty at all.");
            Assert.That(
                set.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.AttackerId), RuleObject.ForHero(fixture.DefenderId)),
                Is.EqualTo(1),
                "The two-subject read is what milestone 8.40a adds, and it finds the declaration the " +
                "one-subject read cannot.");
        }

        /// <summary>
        /// THE COLLISION THE PROBE FOUND, PINNED SO IT CANNOT COME BACK. Reading the older family about
        /// the defeated hero as well - which is what this block predicted before it measured anything -
        /// makes a bounty-wearing ATTACKER charge their victim an extra counter, because
        /// <c>FromHeldMarkers</c> affects the holder whoever the holder is. No printed face says that,
        /// and the split into two families is what makes it impossible rather than merely unintended.
        /// </summary>
        [Test]
        public void TheTwoFamilies_AreDisjoint_SoAWearersOwnVictimIsNotCharged()
        {
            var fixture = new BountyDefeatFixture();
            fixture.GiveBountyTo(fixture.AttackerId);

            var set = fixture.DerivedModifications();

            Assert.That(set.AdditionalDefeatLifeCost(RuleObject.ForHero(fixture.AttackerId)), Is.Zero,
                "A bounty on the hero doing the defeating declares nothing about whom he defeats.");
            Assert.That(
                set.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.AttackerId), RuleObject.ForHero(fixture.DefenderId)),
                Is.Zero);
            Assert.That(
                set.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.DefenderId), RuleObject.ForHero(fixture.AttackerId)),
                Is.EqualTo(1),
                "The same board with the roles swapped charges one, which is the printed sentence: " +
                "the wearer pays when the wearer is defeated.");
        }

        [Test]
        public void WithNoMarkerOut_TheDerivedSetCarriesNoAdditionalCostAtAll()
        {
            var fixture = new BountyDefeatFixture();

            var set = fixture.DerivedModifications();

            Assert.That(
                set.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.AttackerId), RuleObject.ForHero(fixture.DefenderId)),
                Is.Zero);
        }

        // -------------------------------------------------------------------------------------------
        // The charge: Tier + 1 for a holder, Tier for everybody else, on one board at the same Tier
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AHeroWearingTheMarker_SpendsTierPlusOne_AndAHeroWearingNoneSpendsTier()
        {
            var bare = new BountyDefeatFixture();
            var tier = bare.LowestNonBasicTier(bare.DefenderId);
            var bareDefeat = bare.DefeatTheDefender();

            var wearing = new BountyDefeatFixture();
            Assert.That(wearing.LowestNonBasicTier(wearing.DefenderId), Is.EqualTo(tier),
                "The same board and the same printed set, so the only thing that differs between the " +
                "two runs is the marker.");
            wearing.GiveBountyTo(wearing.DefenderId);
            var wearingDefeat = wearing.DefeatTheDefender();

            Assert.That(bareDefeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)),
                "A hero wearing none spends exactly what p.13 prints.");
            Assert.That(wearingDefeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier) + 1),
                "\"a hero with a bounty marker spends 1 additional life counter when defeated\".");
            Assert.That(wearing.RemainingLife(wearing.DefenderTeamId),
                Is.EqualTo(wearing.StartingLifeCounters - wearingDefeat.LifeSpend.Spent),
                "One combined spend, which is the 2026-09-06 ruling's arithmetic: computed once, paid " +
                "once.");
            Assert.That(wearing.RemainingLife(wearing.AttackerTeamId),
                Is.EqualTo(wearing.StartingLifeCounters),
                "p.13 spends the DEFEATED hero's team's counters, marker or no marker.");
        }

        /// <summary>
        /// The bounty on a hero who is not the one being defeated changes nothing, which is the
        /// negative that says the read is about the defeated holder rather than about the board - and
        /// the whole-defeat form of the collision the two families exist to prevent.
        /// </summary>
        [Test]
        public void AMarkerWornByTheATTACKER_ChargesNothingForTheHeroHeDefeats()
        {
            var fixture = new BountyDefeatFixture();
            fixture.GiveBountyTo(fixture.AttackerId);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)),
                "The printed sentence charges the WEARER when the WEARER is defeated; a wearer who " +
                "does the defeating charges their victim nothing.");
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(1),
                "and p.13 returns the DEFEATED hero's markers, so the attacker keeps his.");
        }

        [Test]
        public void AMarkerReturnedBeforeTheDefeat_ChargesNothing()
        {
            var fixture = new BountyDefeatFixture();
            var attachment = fixture.GiveBountyTo(fixture.DefenderId);
            fixture.Markers.Return(attachment.Id);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefender();

            Assert.That(fixture.Markers.Attachments, Is.Empty);
            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)),
                "The rule is printed about the holder in the present tense, so a marker that is back " +
                "in the supply declares nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The order: p.13's own marker return does not cancel the charge for the defeat that returned it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.13 prices the Defeat and then returns the defeated hero's markers, in that order, so the
        /// bounty a Defeat removes still charges for the Defeat that removed it.
        ///
        /// <para>The ordering is structural rather than positional and this case asserts BOTH halves:
        /// the set is derived at the command boundary before the page's list begins, and inside the
        /// list the cost is computed at the top while the return is spliced in at the page's third
        /// line - which the event stream shows directly, the return standing between the removal and
        /// the counters it did not reduce.</para>
        /// </summary>
        [Test]
        public void ThePagesOwnMarkerReturn_DoesNotCancelTheChargeForTheDefeatThatReturnedIt()
        {
            var fixture = new BountyDefeatFixture();
            var attachment = fixture.GiveBountyTo(fixture.DefenderId);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefender();

            Assert.That(fixture.LastEventKinds,
                Has.Member(CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat),
                "p.13: \"Any markers they had are returned.\"");
            Assert.That(fixture.Markers.Attachments, Is.Empty);
            Assert.That(
                fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition),
                Is.EqualTo(BainComponentContent.BountySupplyCount),
                "Returned means back into the supply, which is where 8.40b's give will find it.");
            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier) + 1),
                "The charge is read before the return, so the marker this Defeat took off the " +
                "dashboard still paid for it.");
            Assert.That(
                fixture.IndexOfEvent(CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat),
                Is.LessThan(fixture.IndexOfEvent(CardActionResolutionEventKind.TeamLifeSpent)),
                "The return is reported at the page's third line and the counters at its fifth, so " +
                "the stream shows a return that preceded a spend it did not reduce.");
        }

        // -------------------------------------------------------------------------------------------
        // Two declarations charge two, and Ursafar's Tear is byte-identical in behaviour
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The acceptance the family's own remarks demand: "two declarations charge two", asserted with
        /// Ursafar's attacker-side declaration and Bain's holder-side marker standing together on one
        /// board at one Tier, rather than reasoned about from the counting rule.
        /// </summary>
        [Test]
        public void TearAndTheBountyTogether_ChargeTwo()
        {
            var fixture = new BountyDefeatFixture();
            fixture.GiveBountyTo(fixture.DefenderId);
            fixture.StandTearOn(fixture.AttackerId);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var set = fixture.DerivedModifications();
            Assert.That(set.AdditionalDefeatLifeCost(RuleObject.ForHero(fixture.AttackerId)),
                Is.EqualTo(1), "Tear's, unchanged and still about the attacker.");
            Assert.That(
                set.Modifications.Count(modification =>
                    modification.Modification.Kind ==
                        RuleModificationKind.AdditionalDefeatLifeCostWhenDefeated &&
                    modification.AffectedObject.Equals(RuleObject.ForHero(fixture.DefenderId))),
                Is.EqualTo(1), "The marker's, about its holder, in the other family.");
            Assert.That(
                set.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.AttackerId), RuleObject.ForHero(fixture.DefenderId)),
                Is.EqualTo(2), "Two families, two declarations, one number.");

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier) + 2),
                "A hero wearing Bain's marker who is defeated by Ursafar while Tear stands charges " +
                "TWO, by construction and without either card saying so.");
        }

        /// <summary>
        /// Tear alone, on the same board, charging exactly what it charged before this milestone - and
        /// the specific regression the holder-side read could have introduced: an attacker-side
        /// declaration must not be counted a second time because the defeated hero is now also asked
        /// about.
        /// </summary>
        [Test]
        public void TearAlone_ChargesOne_AndTheHolderSideReadDoesNotDoubleCountIt()
        {
            var fixture = new BountyDefeatFixture();
            fixture.StandTearOn(fixture.AttackerId);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var set = fixture.DerivedModifications();
            Assert.That(
                set.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.AttackerId), RuleObject.ForHero(fixture.DefenderId)),
                Is.EqualTo(1),
                "One declaration, counted once, whichever of the two subjects it is declared about.");

            var defeat = fixture.DefeatTheDefender();

            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier) + 1),
                "Tear's behaviour is byte-identical: Tier + 1, computed once and spent once.");
        }

        /// <summary>
        /// The degenerate subject, which <c>synthetic-defeat-hero@1</c> makes reachable in principle -
        /// it takes a hero parameter and attaches no cause, so the hero it names could be the actor.
        /// One declaration standing on a hero who is both subjects is charged once, because the two
        /// families are disjoint and each modification can match at most one of them.
        /// </summary>
        [Test]
        public void OneDeclaration_OnAHeroWhoIsBothTheActorAndTheDefeated_IsCountedOnce()
        {
            var hero = new HeroId("self-defeating-hero");
            var self = RuleObject.ForHero(hero);

            Assert.That(
                DeclaredAbout(hero, RuleModification.ForAdditionalDefeatLifeCost())
                    .AdditionalDefeatLifeCost(self, self),
                Is.EqualTo(1),
                "Tear's family, counted once even though the same object answers both subjects.");
            Assert.That(
                DeclaredAbout(hero, RuleModification.ForAdditionalDefeatLifeCostWhenDefeated())
                    .AdditionalDefeatLifeCost(self, self),
                Is.EqualTo(1),
                "and the marker's family, likewise.");
            Assert.That(
                DeclaredAbout(
                        hero,
                        RuleModification.ForAdditionalDefeatLifeCost(),
                        RuleModification.ForAdditionalDefeatLifeCostWhenDefeated())
                    .AdditionalDefeatLifeCost(self, self),
                Is.EqualTo(2),
                "Two declarations charge two, which is the counting rule the family exists to keep.");
            Assert.That(
                DeclaredAbout(
                        hero,
                        RuleModification.ForAdditionalDefeatLifeCostWhenDefeated(),
                        RuleModification.ForAdditionalDefeatLifeCostWhenDefeated())
                    .AdditionalDefeatLifeCost(self, self),
                Is.EqualTo(2),
                "and so do two of one family, so a second source is never silently swallowed.");
        }

        [Test]
        public void AnUndefinedSubject_ContributesNothingRatherThanMatchingEverything()
        {
            var hero = new HeroId("declared-hero");
            var attackerSide = DeclaredAbout(hero, RuleModification.ForAdditionalDefeatLifeCost());
            var defeatedSide =
                DeclaredAbout(hero, RuleModification.ForAdditionalDefeatLifeCostWhenDefeated());

            Assert.That(
                attackerSide.AdditionalDefeatLifeCost(RuleObject.ForHero(hero), RuleObject.None),
                Is.EqualTo(1));
            Assert.That(
                attackerSide.AdditionalDefeatLifeCost(RuleObject.None, RuleObject.ForHero(hero)),
                Is.Zero,
                "An attacker-side declaration is not found by asking about the defeated hero, which " +
                "is the split stated as an assertion.");
            Assert.That(
                defeatedSide.AdditionalDefeatLifeCost(RuleObject.None, RuleObject.ForHero(hero)),
                Is.EqualTo(1));
            Assert.That(
                defeatedSide.AdditionalDefeatLifeCost(RuleObject.ForHero(hero), RuleObject.None),
                Is.Zero);
            Assert.That(
                defeatedSide.AdditionalDefeatLifeCost(RuleObject.None, RuleObject.None), Is.Zero,
                "The single-subject overload's rule, restated for two.");
        }

        // -------------------------------------------------------------------------------------------
        // BA-Q4: every doorway to p.13's spend, including a Defeat no Attack caused
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// BA-Q4's reading, proved rather than argued: the cost is charged wherever p.13's spend is
        /// computed, and there is exactly ONE such place. <c>ResolveHeroDefeat</c> is the only caller of
        /// <c>HeroDefeatRewardSchedule.LifeCounters</c> and the only caller of
        /// <c>TeamLifeState.TrySpend</c> in the whole production tree, and it is reached by two
        /// entrances - p.14's step 6, and <c>ResolveEffectInvokedHeroDefeat</c>, which is what milestone
        /// 8.22's request transport carries every card-text Defeat through.
        ///
        /// <para>This case drives the second entrance directly, with a Skill source: no Attack, no
        /// attack total, no Defense card, p.18's immunity question asked about a Skill. The internal
        /// seam is reached by reflection because Core publishes no InternalsVisibleTo for the test
        /// assembly.</para>
        /// </summary>
        [Test]
        public void ADefeatNoAttackCaused_ChargesTheHoldersCostAtTheSameOneSpendSite()
        {
            var fixture = new BountyDefeatFixture();
            fixture.GiveBountyTo(fixture.DefenderId);
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefenderThroughASkill();

            Assert.That(defeat, Is.Not.Null,
                "A composition that cannot price a Defeat returns null and changes nothing; this one " +
                "binds Levels and counters.");
            Assert.That(defeat.HasLifeSpend, Is.True);
            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier) + 1),
                "The printed sentence says \"when defeated\" and attaches no cause, so a Defeat a " +
                "Skill asked for charges exactly what p.14's step 6 charges.");
            Assert.That(fixture.Markers.Attachments, Is.Empty,
                "And the same page returns the marker on the same list.");
        }

        [Test]
        public void ADefeatNoAttackCaused_ChargesNothingExtraWhenNoMarkerIsWorn()
        {
            var fixture = new BountyDefeatFixture();
            var tier = fixture.LowestNonBasicTier(fixture.DefenderId);

            var defeat = fixture.DefeatTheDefenderThroughASkill();

            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)));
        }

        // -------------------------------------------------------------------------------------------
        // The property is read by DEFINITION, not by "any marker"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A hero wearing Tigerclaw's Poison is not wearing a bounty. Both definitions are on the board
        /// at once so the case cannot pass by there being nothing else to confuse - and the assertion
        /// is on the derived set as well as on the spend, because the set is where a reading of "any
        /// held marker" would show up.
        /// </summary>
        [Test]
        public void AMarkerOfADifferentDefinition_DeclaresNoDefeatCost()
        {
            var poisonOnly = new BountyDefeatFixture();
            poisonOnly.GivePoisonTo(poisonOnly.DefenderId);
            var tier = poisonOnly.LowestNonBasicTier(poisonOnly.DefenderId);

            var poisonSet = poisonOnly.DerivedModifications();
            Assert.That(
                poisonSet.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(poisonOnly.AttackerId),
                    RuleObject.ForHero(poisonOnly.DefenderId)),
                Is.Zero,
                "Poison's three declarations are value modifiers; none is of this family.");
            Assert.That(
                poisonSet.ValueContributions(
                        RuleObject.ForHero(poisonOnly.DefenderId), PrintedValueChannel.Defense)
                    .Sum(contribution => contribution.Amount),
                Is.EqualTo(-1),
                "and the Poison marker is genuinely standing, so the negative above is not an empty " +
                "board answering it.");
            Assert.That(poisonOnly.DefeatTheDefender().LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)));

            var both = new BountyDefeatFixture();
            both.GivePoisonTo(both.DefenderId);
            both.GiveBountyTo(both.DefenderId);
            Assert.That(both.Markers.Attachments, Has.Count.EqualTo(2));

            var bothSet = both.DerivedModifications();
            Assert.That(
                bothSet.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(both.AttackerId), RuleObject.ForHero(both.DefenderId)),
                Is.EqualTo(1),
                "One bounty, one charge: the second attachment on the same holder adds nothing to " +
                "this family.");
            Assert.That(both.DefeatTheDefender().LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier) + 1));
        }

        // -------------------------------------------------------------------------------------------
        // The marker definition itself, and the deferral of BOTH its halves
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheBountyMarker_IsTheRecordedReadingOfItsPrintedSentence()
        {
            var definition = BainComponentContent.BountyDefinition;

            Assert.That(definition.SupplyCount, Is.EqualTo(1),
                "BA-D1: his Ultimate says \"THE bounty marker\" and all five Green faces ask about " +
                "\"A hero\" in the singular, so the supply is one.");
            Assert.That(BainComponentContent.BountySupplyCount, Is.EqualTo(definition.SupplyCount));
            Assert.That(definition.ReturnCondition,
                Is.EqualTo(MarkerReturnCondition.GeneralCleanup),
                "BA-02: p.8's end-of-round return and p.13's defeated-holder return are one condition " +
                "in this vocabulary, and no face of his prints an earlier one.");
            Assert.That(definition.Revision, Is.EqualTo(1));

            var property = definition.Properties.Single();
            Assert.That(property.Modification.Kind,
                Is.EqualTo(RuleModificationKind.AdditionalDefeatLifeCostWhenDefeated),
                "The marker's sentence names the DEFEATED holder, which is the twelfth family; " +
                "Ursafar's Tear keeps the eleventh, which names the attacker.");
            Assert.That(
                UrsafarActiveEffectContent.AdditionalDefeatLifeCostDefinition.Modifications.Single()
                    .Kind,
                Is.EqualTo(RuleModificationKind.AdditionalDefeatLifeCost),
                "asserted from Tear's own content, so a later edit that moved it fails here.");
            Assert.That(property.Modification.Channel, Is.EqualTo(PrintedValueChannel.None));
            Assert.That(property.Modification.Amount, Is.Zero,
                "The family carries no amount: one printed face says \"1 additional\", so the family " +
                "says one and two declarations charge two. It also modifies no printed value channel " +
                "at all, which is the whole difference between it and Tigerclaw's.");
            Assert.That(property.ApplicabilityKind, Is.EqualTo("while-held"),
                "The sentence is present-tense about the holder, which is what makes it a marker " +
                "property rather than a rule the giving card keeps.");
        }

        /// <summary>
        /// The paired deferral assertion, which exists so that milestone 8.40j cannot register one half
        /// and forget the other: his giving capability and his marker DEFINITION are two lists a
        /// snapshot pins, and both are still outside the build's registered sets.
        /// </summary>
        [Test]
        public void NeitherHalfOfBainsMarker_IsRegisteredByThisBuild()
        {
            Assert.That(BainComponentContent.RegisteredHandlers(), Is.Empty,
                "He is not dealt, and the registered capability list is serialized into every " +
                "snapshot.");

            // Milestone 8.40b adds his two marker MOVEMENTS beside the give, and this assertion is
            // widened rather than duplicated: the list it pins is the list that dealing has to move.
            var deferred = BainComponentContent.DeferredHandlers().ToArray();
            Assert.That(deferred.Select(handler => handler.Reference),
                Is.EqualTo(new[]
                {
                    BainComponentContent.GiveBountyV1,
                    BainComponentContent.TransferBountyV1,
                    BainComponentContent.ReturnBountyV1,
                    // Milestone 8.40c's marker-presence predicate, deferred beside the give it reads.
                    // Its own deferral - and the showcase consumer it owes where the give owes none -
                    // is asserted in BainMarkerPredicateTests.
                    BainComponentContent.BountyHeldV1
                }));
            Assert.That(
                deferred.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.GiveBountyV1)),
                Is.InstanceOf<GuardsDigital.Core.Effects.Synthetic.SyntheticComponentCapabilities
                    .MarkerGivingHandler>(),
                "The handler TYPE is Tigerclaw's, already registered and already showcased, so this " +
                "instance owes no showcase consumer at the dealing.");
            Assert.That(BainComponentContent.Handlers().Select(handler => handler.Reference),
                Is.EqualTo(deferred.Select(handler => handler.Reference)));

            var capabilities = MatchContentCatalogs.ForThisBuild().Capabilities;
            Assert.That(capabilities.IsRegistered(BainComponentContent.GiveBountyV1), Is.False,
                "HALF ONE: the capability. Registering it before a dealt card can start it moves " +
                "pinned bytes and fails milestone 7.9's reachability gate.");
            Assert.That(capabilities.IsRegistered(BainComponentContent.TransferBountyV1), Is.False);
            Assert.That(capabilities.IsRegistered(BainComponentContent.ReturnBountyV1), Is.False);

            var components = SyntheticComponentDefinitions.CreateCatalog();
            Assert.That(components.IsRegistered(BainComponentContent.BountyDefinition.Reference),
                Is.False,
                "HALF TWO: the marker definition. A match registers one list of what it can put on a " +
                "dashboard and the snapshot pins that list.");
            Assert.That(components.RegisteredMarkers,
                Has.No.Member(BainComponentContent.BountyDefinition.Reference));
            Assert.That(
                components.IsRegistered(TigerclawComponentContent.PoisonOneDefinition.Reference),
                Is.True,
                "and Tigerclaw's is registered, so the two assertions above are about deferral " +
                "rather than about a catalog nobody fills.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static RuleModificationSet DeclaredAbout(
            HeroId hero, params RuleModification[] declarations) =>
            new RuleModificationSet(
                declarations.Select((declaration, index) =>
                    AppliedRuleModification.FromActiveEffect(
                        declaration,
                        RuleObject.ForHero(hero),
                        new ActiveEffectId("declaration-" + index),
                        hero,
                        TeamSide.Red,
                        index)),
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>
        /// The staged two-hero Attack <c>DefeatMarkerReturnTests</c> drives, with the marker register
        /// live, the Life counters bound, and the modification set derived from the authority itself
        /// rather than stubbed empty.
        /// </summary>
        private sealed class BountyDefeatFixture
        {
            private const int LifeCountersPerTeam = 8;

            private readonly MatchConfiguration configuration;
            private readonly CardActionResolutionAuthority authority;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly TeamLifeState teamLives;
            private readonly ActionOffer attackOffer;
            private IReadOnlyList<ResolutionStep> lastSteps = Array.Empty<ResolutionStep>();

            public BountyDefeatFixture()
            {
                configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                AttackerId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                DefenderId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(AttackerId, new HexCoordinate(0, 0)),
                        new HeroOccupancy(DefenderId, new HexCoordinate(1, 0))
                    });
                teamLives = new TeamLifeState(configuration.Participants, LifeCountersPerTeam);
                authority = new CardActionResolutionAuthority(
                    MatchContentCatalogs.ForThisBuild(),
                    Board,
                    Markers,
                    new HeroProgressionState(configuration.Participants),
                    teamLives);
                attackOffer = ActionOfferQuery.GetAvailableOffers(
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        AttackerId,
                        ActionOfferWindow.NormalPlay)
                    .Single(candidate =>
                        candidate.SourceCardInstanceId.Value == "red-pursuit-card" &&
                        candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            }

            public HeroId AttackerId { get; }
            public HeroId DefenderId { get; }
            public AdjacentMoveAuthority Board { get; }
            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();
            public int StartingLifeCounters => LifeCountersPerTeam;

            public TeamId AttackerTeamId => configuration.Participants.GetTeamForHero(AttackerId).Id;
            public TeamId DefenderTeamId => configuration.Participants.GetTeamForHero(DefenderId).Id;

            public int RemainingLife(TeamId teamId) => teamLives.GetCounters(teamId);

            public IReadOnlyList<CardActionResolutionEventKind> LastEventKinds =>
                lastSteps
                    .Where(step => step.Kind == ResolutionStepKind.Fact)
                    .Select(step => step.Fact.Kind)
                    .ToArray();

            public int IndexOfEvent(CardActionResolutionEventKind kind)
            {
                for (var index = 0; index < lastSteps.Count; index++)
                {
                    if (lastSteps[index].Kind == ResolutionStepKind.Fact &&
                        lastSteps[index].Fact.Kind == kind)
                    {
                        return index;
                    }
                }

                return -1;
            }

            public CardTier LowestNonBasicTier(HeroId heroId)
            {
                Assert.That(CardCharacteristicQuery.TryGetLowestNonBasicTier(
                        new CardCharacteristicCriteria(ownerId: heroId),
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        out var tier), Is.True);
                return tier;
            }

            public MarkerAttachment GiveBountyTo(HeroId holder) =>
                Attach(BainComponentContent.BountyDefinition, "bounty", holder);

            public MarkerAttachment GivePoisonTo(HeroId holder) =>
                Attach(TigerclawComponentContent.PoisonOneDefinition, "poison", holder);

            /// <summary>
            /// Ursafar's own declaration, taken from his content and stood up through the production
            /// lifecycle rather than hand-assembled into a set - which is what makes "two declarations
            /// charge two" an assertion about the shipped card.
            /// </summary>
            public void StandTearOn(HeroId controller)
            {
                timing.Create(
                    new ActiveEffectId("tear-cost-" + controller.Value),
                    UrsafarActiveEffectContent.AdditionalDefeatLifeCostDefinition,
                    controller,
                    configuration.Participants.GetTeamForHero(controller).Side,
                    controller,
                    new CardInstanceId("ursafar-tear"),
                    new ActionDefinitionId("primary-attack"),
                    1,
                    1);
                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.ActionStarted, 1, 1),
                    configuration.TieBreakerState);
                Assert.That(
                    timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active),
                    "Tear's declaration has to be STANDING when p.13's cost is computed.");
            }

            /// <summary>
            /// The set a configured match hands the spend site: built by the authority itself, from the
            /// live marker register and the live Active-effect lifecycle.
            /// </summary>
            public RuleModificationSet DerivedModifications() =>
                authority.CurrentModifications(
                    timing,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    UnlockedUltimateSet.None,
                    RuleObject.ForHero(AttackerId));

            /// <summary>p.14's step 6, driven through the dispatcher against the weakest Defense.</summary>
            public HeroDefeatResolution DefeatTheDefender()
            {
                var modifications = DerivedModifications();
                var start = authority.StartAttackResolution(
                    new ActionResolutionId("bounty-defeat"),
                    attackOffer,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    modifications);
                var opened = authority.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        start.PendingTargetChoice.ResolutionId,
                        start.PendingTargetChoice.ChoiceId,
                        DefenderId),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    modifications);
                var weakest = opened.PendingDefenseChoice.LegalDefenseOffers
                    .OrderBy(offer => PrintedValueQuery.CalculatePrintedValue(
                        offer, configuration.CardDefinitions, PrintedValueChannel.Defense).Total)
                    .First();
                authority.AnswerDefenseResponse(
                    DefenseResponseChoiceCommand.SelectCards(
                        opened.PendingDefenseChoice.ResolutionId,
                        opened.PendingDefenseChoice.ChoiceId,
                        weakest.SourceCardInstanceId),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    modifications);
                var outcome = authority.ResolveAttackOutcome(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    modifications);
                lastSteps = outcome.Steps;
                Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
                Assert.That(outcome.HeroDefeat, Is.Not.Null);
                Assert.That(outcome.HeroDefeat.HasLifeSpend, Is.True);
                return outcome.HeroDefeat;
            }

            /// <summary>
            /// The other entrance to the same list: milestone 8.22's effect-invoked Defeat, run about a
            /// SKILL. Reached by reflection because the backend and its seam are internal to Core and
            /// Core publishes no InternalsVisibleTo for this assembly - the same route the Whisper
            /// fixtures take to <c>CaptureRestoreState</c>.
            /// </summary>
            public HeroDefeatResolution DefeatTheDefenderThroughASkill()
            {
                var backendField = typeof(CardActionResolutionAuthority).GetField(
                    "attackDefenseBackend", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(backendField, Is.Not.Null,
                    "CardActionResolutionAuthority still holds one Attack/Defense backend.");
                var backend = backendField.GetValue(authority);
                var seam = backend.GetType().GetMethod(
                    "ResolveEffectInvokedHeroDefeat", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(seam, Is.Not.Null,
                    "Milestone 8.22's effect-invoked entrance to p.13 is still one method.");

                var source = EffectInvokedDefeatSource.FromSkill(
                    new ActionResolutionId("bounty-skill-defeat"),
                    new CardInstanceId("bounty-skill-source"),
                    new ActionDefinitionId("primary-skill"),
                    AttackerId);
                Assert.That(source.Activity, Is.EqualTo(RuleActivity.Skill),
                    "No Attack caused this Defeat, and p.18's immunity question is asked about a Skill.");
                Assert.That(source.Outcome, Is.EqualTo(AttackResolutionOutcome.None));

                var steps = new List<ResolutionStep>();
                var resolved = (HeroDefeatResolution)seam.Invoke(backend, new object[]
                {
                    source,
                    DefenderId,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    DerivedModifications(),
                    steps
                });
                lastSteps = steps;
                return resolved;
            }

            private MarkerAttachment Attach(
                MarkerDefinition definition, string instancePrefix, HeroId holder)
            {
                var attachment = new MarkerAttachment(
                    new MarkerInstanceId($"{instancePrefix}-{holder.Value}"),
                    definition,
                    AttackerId,
                    holder,
                    new CardInstanceId($"{instancePrefix}-source"),
                    new ActionDefinitionId("primary-attack"));
                Markers.Give(attachment);
                return attachment;
            }

            private static HexCoordinate[] FeaturelessDisk(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates.ToArray();
            }
        }
    }
}
