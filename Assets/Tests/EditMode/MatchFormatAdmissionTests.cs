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
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(9),
                "A version 8 peer reads the format string as the content digest and then finds the " +
                "key proof missing, so the old version is refused from the bounded header instead.");
        }

        [Test]
        public void TheSnapshotSchema_DidNotMoveBecauseAFormatIsValuesAndNotShape()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(36),
                "The Wave total, each team's Life counters and the participant list have all been " +
                "counted values in the payload for many versions, so a 2v2 payload is a v36 payload " +
                "of different numbers rather than of a different shape.");
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
