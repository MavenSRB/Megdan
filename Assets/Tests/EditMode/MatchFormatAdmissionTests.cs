using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The ELEVENTH amendment's compatibility half: two peers that composed different rulebook
    /// setups must be refused by name rather than admitted into one another's sessions.
    /// </summary>
    public sealed class MatchFormatAdmissionTests
    {
        private static PrivateSessionIdentity Identity(
            string matchFormatId, string digest = "digest-a", string build = "build-a") =>
            new PrivateSessionIdentity(
                PrivateSessionProtocol.FamilyName,
                PrivateSessionProtocol.Version,
                build,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion,
                "official-single-lane",
                1,
                matchFormatId,
                digest);

        [Test]
        public void TwoGameLengthsOfTheSameCount_AreRefusedByFormatWhereTheyUsedToBeAdmitted()
        {
            // This is the gap the element exists to close, and it is worth stating in its own case:
            // a Quick and a Long game of the same player count register byte-identical content, so
            // every other element of the tuple agrees and the pair was admitted before.
            var quick = Identity("3v3-quick");
            var length = Identity("3v3-long");

            Assert.That(quick.ContentDigest, Is.EqualTo(length.ContentDigest),
                "The game length changes no card, capability, token or marker.");
            Assert.That(quick.FirstMismatch(length),
                Is.EqualTo(SessionRejectionReason.MatchFormatMismatch));
            Assert.That(quick.Equals(length), Is.False);
        }

        [Test]
        public void APlayerCountMismatch_IsReportedAsTheFormatRatherThanAsTheContentDigest()
        {
            // A 2v2 and a 3v3 fail BOTH elements - four printed sets are not six - and the peer is
            // told the one it can act on. That is the whole reason the format is checked first.
            var twoVersusTwo = Identity("2v2-quick", "digest-four-sets");
            var threeVersusThree = Identity("3v3-quick", "digest-six-sets");

            Assert.That(twoVersusTwo.FirstMismatch(threeVersusThree),
                Is.EqualTo(SessionRejectionReason.MatchFormatMismatch));
        }

        [Test]
        public void AContentDigestMismatch_StillMeansWhatItSaysWhenTheFormatsAgree()
        {
            var host = Identity("3v3-quick", "digest-a");
            var client = Identity("3v3-quick", "digest-b");

            Assert.That(host.FirstMismatch(client),
                Is.EqualTo(SessionRejectionReason.ContentDigestMismatch),
                "Same format, different registered content: the older reason is untouched.");
        }

        [Test]
        public void EveryEarlierElement_StillOutranksTheFormat()
        {
            var host = Identity("3v3-quick");
            var otherBuild = Identity("2v2-long", "digest-z", "build-b");

            Assert.That(host.FirstMismatch(otherBuild),
                Is.EqualTo(SessionRejectionReason.BuildIdentityMismatch),
                "The build identity is checked before the format, and a peer is told the first " +
                "failing element and nothing about the ones after it.");
        }

        [Test]
        public void TheFormatElement_IsDerivedFromTheSetupTheCompositionActuallyHas()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);

            foreach (var format in MatchFormat.All)
            {
                var configuration = MatchConfiguration.CreateOfficialForInternalTest(
                    format, TeamSide.Red);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateOfficial(format),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);

                var identity = PrivateSessionIdentity.ForComposition(
                    map, configuration, setup, "derived-build");

                Assert.That(identity.MatchFormatId, Is.EqualTo(format.Id),
                    "The tuple names the setup that exists, read off the participants and the " +
                    "Wave and Life counts rather than declared beside them.");
                Assert.That(identity.CanonicalText, Does.Contain("format=" + format.Id));
            }
        }

        [Test]
        public void TheFourPrintedFormats_AreFourMutuallyIncompatibleSessions()
        {
            var identities = MatchFormat.All.Select(format => Identity(format.Id)).ToArray();

            for (var left = 0; left < identities.Length; left++)
            {
                for (var right = 0; right < identities.Length; right++)
                {
                    var expected = left == right
                        ? SessionRejectionReason.None
                        : SessionRejectionReason.MatchFormatMismatch;
                    Assert.That(identities[left].FirstMismatch(identities[right]), Is.EqualTo(expected),
                        "Format " + MatchFormat.All[left].Id + " against " +
                        MatchFormat.All[right].Id + ".");
                }
            }
        }

        [Test]
        public void AnIdentity_RefusesToExistWithoutAFormat()
        {
            Assert.That(() => Identity(null), Throws.ArgumentException);
            Assert.That(() => Identity("   "), Throws.ArgumentException);
        }

        [Test]
        public void TheHelloFrame_CarriesTheFormatAndRoundTripsIt()
        {
            var identity = Identity("2v2-long");
            var encoded = PrivateSessionFrameCodec.HelloFrame(
                new SessionHelloMessage(identity, "proof-value"));

            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(encoded, 0, encoded.Length), Is.True);
            Assert.That(assembler.TryDequeue(out var frame), Is.True);
            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.Hello));
            Assert.That(PrivateSessionFrameCodec.TryDecodeHello(frame.Payload, out var decoded), Is.True);
            Assert.That(decoded.Identity.MatchFormatId, Is.EqualTo("2v2-long"));
            Assert.That(decoded.Identity.FirstMismatch(identity),
                Is.EqualTo(SessionRejectionReason.None));
            Assert.That(decoded.KeyProof, Is.EqualTo("proof-value"));
        }

        [Test]
        public void TheProtocolVersion_MovedBecauseTheHelloPayloadGrewAField()
        {
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(27),
                "8.80 carries the ATTACKING CARD ON THE DEFENSE QUESTION: the pending Defense "
                + "row gained the attacking card's instance id, its definition id and that "
                + "definition's content revision, and the action definition id, all written "
                + "between the attacker id and the attack total - so a /20 peer reads the first "
                + "of those strings where it expects the total's four bytes and mis-decodes the "
                + "rest of the detail record. It rides the CHOICE rather than the Attack FRAME "
                + "because SeatProjection hands that frame to the ACTING seat alone, and the seat "
                + "being asked to defend is by construction not that seat - which is why the "
                + "attacker's card had been missing from the Defense window on every network "
                + "seat, the host's own loopback seat included, since 8.58 drew it. The three "
                + "facts are table-public: the card stands face up in p.4's turn area, already in "
                + "every seat's own card list. SCHEMA IS UNMOVED at v44 - the choice is derived "
                + "from the authoritative Attack state at projection time and never persisted. " +
                "8.35z73 carries COIN-AWARD ROWS: the seat-public match log now names each " +
                "authoritative reward recipient and amount. No bytes grew, but the closed enum " +
                "vocabulary widened, so an /18 peer refuses rather than going dark. " +
                "8.61e carries the SPRUNG-TRAP ROW: the seat-public match log gained " +
                "MoveThroughTrapSprung, which is Min the Dragonmonk's mines going off - an enemy " +
                "hero moves through a mine, it is removed, and for every blast removed that hero " +
                "discards a card if able. NO BYTES GREW: the row spends the subject hero, the " +
                "origin and the amount the layout has carried since 8.35r, and the bump is here for " +
                "the reason 8.40f's and 8.51's were - NetworkMatchLogCodec.TryRead refuses any kind " +
                "its own build does not declare, so a version 17 peer meeting this number refuses the " +
                "whole FRAME and that seat goes dark. The row has to exist because the reveal has no " +
                "other surface: until it goes off a mine is facedown and every seat but its " +
                "controller's is handed a face naming no kind, and the board shows only a space " +
                "emptying. SCHEMA IS UNMOVED at v43 - the row is re-derived from the retained " +
                "rule-event log, and the pause the trap opens is an ordinary effect interrupt whose " +
                "kind the card-action payload already writes as an int. " +
                "8.53 carries the MATCH SCOREBOARD: NetworkSeatProjectionRecord gained a repeated " +
                "row - hero id plus six counters, kills, deaths, attacks at heroes, times defended, " +
                "minions killed and coins earned - written between the per-hero progression rows and " +
                "the Active-effect source list, so a version 16 peer reads that row count as the " +
                "source count and mis-decodes the entire remainder of the frame. It is CARRIED " +
                "because it cannot be derived: every fact these columns count does cross the wire, " +
                "as a seat-public match-log row, but that log is a BOUNDED WINDOW of recent lines, " +
                "so a client counting its own totals would report fewer kills the longer a match ran " +
                "and would start a reconnected seat's board from the moment it came back. SCHEMA " +
                "MOVED WITH IT to v43 - only the second time these two have moved together - because " +
                "the board is accumulated state rather than a derived reading, so it has to be " +
                "persisted. " +
                "8.51 carries a HOLD: the seat-public match log gained CardHeld, rulebook p.12's Hold performed by the seat's own command, so a version 15 peer refuses the whole frame for the same Enum.IsDefined reason a version 14 peer did below. NO BYTES GREW - the row spends the subject hero and subject card the layout has carried since 8.35r - and the schema stays at v42, the row being re-derived from the retained rule-event log's Turn.HoldActionPerformed. It exists because the 2026-09-18 playtest's report that a card got resolved without input could be read off the wire only from the timing of two silent submissions. " +
                "8.40f carries a REVEAL that card text performed. The seat-public match log gained one member, CardRevealedByCardText, and NetworkMatchLog.TryRead validates every row's kind with Enum.IsDefined - so a protocol 14 peer meeting the new number does not mis-read the row, it refuses the whole FRAME and that seat goes dark, which is the one kind of change this ratchet exists for. THE ROW ITSELF GREW NO BYTES: MatchLogEntry has carried a subject hero and a subject card since 8.35r and this member spends exactly those two, so only the VOCABULARY widened. It is a new member rather than CardRevealed reused because every consumer of that one reads it as rulebook p.7's turn-card commitment - the flip cue borrows the hero's CURRENT CARD SLOT to play it at, the reveal batch is re-staged in printed-Initiative order because the commitment flow emits its reveals in roster order, and the channel's own one-for-one ledger asserts exactly as many such rows as Commitment.CardRevealed events occurred - and a card turned face up out of a HAND mid-resolution is in neither place, so reuse would have animated the wrong card and falsified a declared ledger. SCHEMA IS UNMOVED at v42: the reveal's memory is the DERIVED seat-public log, re-derived from the retained rule-event log on restore, replay and Undo, so nothing new is persisted and no corpus digest or pinned constant moves with it. " +
                "8.39g carries a SOLO ending. Every projected outcome before it named a winning TEAM and a losing team, because all three of the rulebook's victory sentences are about teams; Cutter's A Fistful of Coins awards the match to one HERO and leaves that hero's own team-mates among the losers, so the outcome row gained a hero field and the two team fields became the named absence for that shape. A protocol 13 peer reads the hero id where it expects the next record and mis-decodes the remainder, so it is refused from the bounded header. SCHEMA MOVED WITH IT to v42, which is the only time these two have moved together: a solo winner is the one victory fact this build cannot re-derive on restore. 8.37b carries a pending effect choice's declared VALUE VOCABULARY, so a joined seat draws six named colours or six named directions rather than the digits 0 through 5 - Tali's Commune with Spirits puts one of those decisions to a hero who is not the acting seat, so the seat that must answer it is by construction a remote one. A protocol 12 peer is refused before any payload is read. At 8.37b the schema was UNMOVED at v41: restore re-asks the handler, which re-declares it. " +
                "8.36b required authenticated keyed departures and rejected protocol 11. " +
                "Milestone 8.35z60's integration pass took it to 11 to carry rulebook p.8's shared " +
                "Wave-counter pool on the seat projection, so a remote seat's top strip reads the " +
                "host's live number instead of its own frozen starting total. A version 10 peer " +
                "would read that new Int32 as the hero-row count that follows it and mis-decode the " +
                "whole remainder of the frame, so it is refused from the bounded header. " +
                "A version 8 peer reads the format string as the content digest and then finds the " +
                "key proof missing, so the old version is refused from the bounded header instead. " +
                "Milestone 8.35z37 took it to 10 for the ChooseHero kind that carries a " +
                "seat's hero pick to the host: a version 9 peer has no case for kind 21 and would " +
                "refuse it as an unexpected message, which is a protocol surface change and is " +
                "taken as one. This case pins the CURRENT version rather than the one the format " +
                "element moved to, so a later move is accounted for here rather than passing " +
                "unread." +
                "Milestone 8.71a carries the REWRITTEN PRINTED INITIATIVE at /20: each "
                + "seat projection's card sections end in a sparse block naming the cards "
                + "Hanu's Hurry up! has set to 11, which a /19 peer would read as whatever "
                + "field follows the last card row. SCHEMA MOVES WITH IT to v44, because "
                + "the rewrite is authoritative state no live state remembers.");
        }

        [Test]
        public void TheSnapshotSchema_DidNotMoveBecauseAFormatIsValuesAndNotShape()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(50),
                "The Wave total, each team's Life counters and the participant list have all been " +
                "counted values in the payload for many versions, so a 2v2 payload is a v38 payload " +
                "of different numbers rather than of a different shape. The version itself has moved " +
                "since - milestone 8.35z21 added the decided-and-unstarted repeat instances, " +
                "milestone 8.35z27 added the modal Attack part-way through its plan, and milestone " +
                "8.35z45 added the predicate each of that plan's bullets prints about its " +
                "candidate (taking 40 rather than 39, because v39 is claimed), and milestone 8.53 " +
                "added the match scoreboard - all shape changes " +
                "belonging to those milestones and not to this format." +
                "Milestone 8.71a moved it again, to v44, for Hanu the Trickster's " +
                "rewritten printed Initiative - a sparse card-keyed list in the card " +
                "section, which is authoritative state no live state remembers.");
        }

        [Test]
        public void EveryFormatMismatch_HasWordsForAPlayerAndForTheHostLog()
        {
            Assert.That(
                PrivateLobbyLabels.Refusal(SessionRejectionReason.MatchFormatMismatch),
                Is.Not.Empty);
            Assert.That(
                PrivateLobbyLabels.Refusal(SessionRejectionReason.MatchFormatMismatch),
                Does.Contain("format"));
        }
    }
}
