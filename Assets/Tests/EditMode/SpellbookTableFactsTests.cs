using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30h. The two table facts the spellbook's presentation reads and the engine owed it:
    /// the facedown book counted by the BACK each spell shows, on every seat and across the wire
    /// (lesson 25: "the counts and the tier backs reach every seat"), and the seat-public log rows a
    /// cast, a faceup removal and a refill write - naming only spells that are faceup as the row is
    /// written. Unity-free: the dotnet harness runs every case here.
    ///
    /// <para>The regressions read the new members BY NAME (a property's name, a log kind's name) so
    /// they compile on the base and fail there with a message, rather than failing to compile.</para>
    /// </summary>
    public sealed class SpellbookTableFactsTests
    {
        private static readonly HeroId Owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        // -------------------------------------------------------------------------------------------
        // The book by its backs
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EverySeat_CountsTheBookByTheBackItsSpellsShow_AcrossTheWire_AndNamesNoFacedownSpell()
        {
            // The synthetic book: a basic Gold spell, a Tier I Red spell and a Tier IV Purple one, all
            // facedown. A basic spell shows the Tier I back (the corpus's backs are byte-identical), so
            // the table sees two Tier I backs and one Tier IV back.
            var host = new MatchHost(SpellbookZoneTests.CreateSession(false));
            var bookIds = host.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => card.Id.Value)
                .ToArray();
            Assert.That(bookIds, Has.Length.EqualTo(3));
            var catalogs = MatchContentCatalogs.ForThisBuild();

            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                var summary = view.HandSummaries.Single(value => value.HeroId.Equals(Owner));
                Assert.That(Backs(summary), Is.EqualTo("Ix2|IVx1"),
                    seat.Id + ": every seat counts the book by the back each spell shows.");
                Assert.That(Backs(view.HandSummaries.Single(value => value.HeroId.Equals(Other))),
                    Is.EqualTo(string.Empty), "a hero with no book shows no back");

                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(view), out var record), Is.True, seat.Id.ToString());
                var network = new NetworkMatchReadModel(host.Projection, catalogs, record, true);
                Assert.That(Backs(network.HandSummaries.Single(value => value.HeroId.Equals(Owner))),
                    Is.EqualTo("Ix2|IVx1"), seat.Id + ": and the wire carries the backs to a network seat.");
            }

            var other = host.ProjectionForSeat(host.Seats.FindByHero(Other).Id);
            Assert.That(other.Cards.Select(card => card.Id.Value).Intersect(bookIds), Is.Empty,
                "A back is all a non-owner seat gets of a facedown spell.");
            var payload = Encoding.UTF8.GetString(NetworkSeatProjectionCodec.Encode(other));
            foreach (var id in bookIds)
            {
                Assert.That(payload, Does.Not.Contain(id), "The wire names no facedown spell: " + id);
            }

            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(33),
                "/32 carries the backs; a /31 reader would take the back count for the next row's id.");
        }

        // -------------------------------------------------------------------------------------------
        // The log
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ACast_WritesOneSeatPublicRow_NamingTheCasterAndTheSpell_FaceupAsItIsWritten()
        {
            var cast = new SpellCastTests.Cast(SyntheticSpellbook.CantripId);
            cast.StartCasting();
            cast.ChooseArm(SyntheticSpellbook.CastWardArm);
            var ward = cast.Spell(SyntheticSpellbook.WardSpellId);
            Assert.That(ward.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));

            var rows = Rows(Derive(cast.Events), "SpellCast");
            Assert.That(rows, Has.Length.EqualTo(1),
                "one seat-public row says the spell was cast; the log holds: " + Describe(Derive(cast.Events)));
            Assert.That(rows[0].SubjectHeroId, Is.EqualTo(SpellCastTests.Cast.Caster), "the caster's row");
            Assert.That(rows[0].SubjectCardInstanceId, Is.EqualTo(ward.Id),
                "naming the spell, which lies faceup in the open removed pile");
            Assert.That(GameplayNarrationLog.Describe(rows[0]), Does.Contain(" cast " + ward.Id.Value));
        }

        [Test]
        public void ARefill_WritesOneRowCountingTheReturnedSpells_AndNamingNone()
        {
            var cast = new SpellCastTests.Cast(
                SyntheticSpellbook.PrepareSpellsId,
                removed: new[] { SyntheticSpellbook.AttackSpellId, SyntheticSpellbook.WardSpellId });
            cast.StartCasting();
            Assert.That(cast.Spell(SyntheticSpellbook.WardSpellId).State.Location, Is.EqualTo(CardLocation.Spellbook));

            var log = Derive(cast.Events);
            var rows = Rows(log, "SpellbookRefilled");
            Assert.That(rows, Has.Length.EqualTo(1), "one row says the book was refilled; the log holds: " + Describe(log));
            Assert.That(rows[0].SubjectHeroId, Is.EqualTo(SpellCastTests.Cast.Caster));
            Assert.That(rows[0].Amount, Is.EqualTo(2), "how many returned");
            Assert.That(rows[0].SubjectCardInstanceId.IsDefined, Is.False,
                "and no card: every returned spell is facedown again");
            var spells = cast.Cards.Where(card => SpellbookQuery.IsInASpellbook(card.State.Location))
                .Select(card => card.Id.Value).ToArray();
            var raw = new RuleEventLogProjection();
            raw.Append(cast.Events);
            var refillLines = raw.Entries.Where(entry => entry.EventType.EndsWith("CardTextRefilledSpellbook"))
                .Select(entry => entry.StableReferences + " " + entry.Detail).ToArray();
            Assert.That(refillLines, Has.Length.EqualTo(1));
            foreach (var id in spells)
            {
                Assert.That(refillLines[0], Does.Not.Contain(id), "the refill's fact names no spell");
            }

            Assert.That(GameplayNarrationLog.Describe(rows[0]), Does.Contain("returned 2 spells to their spellbook"));
        }

        [Test]
        public void AnEmptyRefill_WritesNoRefillRow_GyQ7()
        {
            var cast = new SpellCastTests.Cast(SyntheticSpellbook.PrepareSpellsId);
            cast.StartCasting();
            Assert.That(Rows(Derive(cast.Events), "SpellbookRefilled"), Is.Empty,
                "an empty refill (always playable, GY-Q7) returned nothing, and says nothing");
        }

        [Test]
        public void FindFamiliarsRemovals_WriteOneRowPerSpellRemoved_NamingTheOwnerAndTheSpell()
        {
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.CastingCard(GydionCardContent.FindFamiliar));
            round.StartCasting();
            round.PickSpace(new HexCoordinate(1, 0));
            round.SelectArm(GydionCardContent.FindFamiliarRemovalArm(GydionCardContent.Shield));
            round.SelectArm(GydionCardContent.FindFamiliarRemovalArm(GydionCardContent.Fireball));
            var events = round.Confirm();

            var log = Derive(events);
            var rows = Rows(log, "SpellRemovedFaceup");
            Assert.That(rows, Has.Length.EqualTo(2), "one row per spell removed; the log holds: " + Describe(log));
            Assert.That(rows.Select(row => row.SubjectHeroId), Is.All.EqualTo(GydionCountingSpellTests.GydionRound.Caster));
            Assert.That(rows.Select(row => row.SubjectCardInstanceId), Is.EquivalentTo(new[]
            {
                round.Spell(GydionCardContent.Shield).Id, round.Spell(GydionCardContent.Fireball).Id
            }), "naming each spell, faceup in the removed pile as the row is written");
            Assert.That(Rows(log, "SpellCast"), Is.Empty, "a removal is not a cast");
            Assert.That(GameplayNarrationLog.Describe(rows[0]), Does.Contain("from their spellbook faceup"));
        }

        [Test]
        public void TheNarration_HasWordsForEveryNewRow()
        {
            foreach (var name in new[] { "SpellCast", "SpellRemovedFaceup", "SpellbookRefilled" })
            {
                Assert.That(Enum.GetNames(typeof(MatchLogEntryKind)), Has.Member(name),
                    "the seat-public log names a spellbook change: " + name);
                var entry = new MatchLogEntry(
                    1, (MatchLogEntryKind)Enum.Parse(typeof(MatchLogEntryKind), name), 1, 1,
                    subjectHeroId: Owner, amount: 3);
                Assert.That(GameplayNarrationLog.Describe(entry), Does.Not.Contain("unnarrated"), name);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>The summary's backs as "Ix2|IVx1", read by name so the case compiles on the base.</summary>
        private static string Backs(SeatHandSummary summary)
        {
            var property = summary.GetType().GetProperty("SpellbookBacks");
            if (property == null)
            {
                return null;
            }

            return string.Join("|", ((System.Collections.IEnumerable)property.GetValue(summary))
                .Cast<object>().Select(back => back.ToString()));
        }

        private static IReadOnlyList<MatchLogEntry> Derive(IEnumerable<RoundResolutionEvent> events)
        {
            var log = new RuleEventLogProjection();
            log.Append(events);
            return MatchLogProjection.Derive(log.Entries, int.MaxValue);
        }

        private static MatchLogEntry[] Rows(IEnumerable<MatchLogEntry> log, string kind) =>
            log.Where(entry => entry.Kind.ToString() == kind).ToArray();

        private static string Describe(IEnumerable<MatchLogEntry> log) =>
            string.Join(", ", log.Select(entry => entry.Kind.ToString()));
    }
}
