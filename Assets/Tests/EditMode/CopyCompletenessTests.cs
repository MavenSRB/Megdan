using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.11. Every hand-written copy, projection and wire form carries every field.
    /// </summary>
    /// <remarks>
    /// <para>Hand-written copies have already dropped fields in shipped code:
    /// <c>MatchSessionProjection.CloneCard</c> lost Hanu's <c>InitiativeOverride</c>, so the wire
    /// block that carried it was always empty (8.71b). Each check here builds its input with every
    /// public property populated (<see cref="CompletenessPopulator"/>) or drives a real match to
    /// it, passes it through the copy or the encode/decode round trip, and compares the two public
    /// property by public property (<see cref="CompletenessProbe"/>). A property added later is
    /// therefore compared the day it is added: it fails until it is carried, or until it is
    /// named on that check's allow-list with the reason it is deliberately left behind.</para>
    /// </remarks>
    public sealed class CopyCompletenessTests
    {
        // ---------------------------------------------------------------------------------------
        // The projection's own copy.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// <c>MatchSessionProjection.CloneCard</c> copies every fact a card instance holds. No
        /// allow-list: a copy of a card that is not a copy of that card is the 8.71b defect.
        /// </summary>
        [Test]
        public void CloneCard_CarriesEveryCardFact()
        {
            var clone = typeof(MatchSessionProjection).GetMethod(
                "CloneCard", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(clone, Is.Not.Null, "The projection's card copy moved; point this check at it.");

            var probe = new CompletenessProbe();
            var populator = new CompletenessPopulator(100);
            var cards = Enumerable.Range(1, 4)
                .Select(seed => new CompletenessPopulator(seed * 100).Create<CardInstance>())
                // p.6's two starting states, so the derived "is it still where setup put it" reads
                // are compared holding true as well as false.
                .Concat(new[]
                {
                    CardInstance.CreateInHand(populator.Create<CardInstanceId>(),
                        populator.Create<CardDefinitionReference>(), populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>()),
                    CardInstance.CreateInDeck(populator.Create<CardInstanceId>(),
                        populator.Create<CardDefinitionReference>(), populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>()),
                    // Milestone 9.30b: the spellbook's starting state.
                    CardInstance.CreateInSpellbook(populator.Create<CardInstanceId>(),
                        populator.Create<CardDefinitionReference>(), populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>())
                });
            foreach (var card in cards)
            {
                var copy = (CardInstance)clone.Invoke(null, new object[] { card });
                Assert.That(copy, Is.Not.SameAs(card), "The projection hands out a copy, not the authority's card.");
                probe.Compare(card, copy, "CloneCard(" + card.Id + ")");
            }

            probe.AssertNoDifferences("MatchSessionProjection.CloneCard");
            probe.AssertExercised("MatchSessionProjection.CloneCard");
        }

        // ---------------------------------------------------------------------------------------
        // The command codec.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Every <see cref="DirectorCommand"/> factory, with every argument populated, survives
        /// <see cref="NetworkCommandCodec"/> property by property - including the nested effect,
        /// removal-prevention, respawn, Minion Battle, correction and system-transition answers.
        /// </summary>
        [Test]
        public void NetworkCommandCodec_CarriesEveryFieldOfEveryFactory()
        {
            var probe = new CompletenessProbe();
            var factories = typeof(DirectorCommand)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.ReturnType == typeof(DirectorCommand))
                .OrderBy(method => method.Name + "/" + method.GetParameters().Length, StringComparer.Ordinal)
                .ToArray();
            Assert.That(factories, Is.Not.Empty);

            var unbuilt = new List<string>();
            foreach (var factory in factories)
            {
                var name = factory.Name + "(" + string.Join(", ",
                    factory.GetParameters().Select(parameter => parameter.ParameterType.Name)) + ")";
                var built = 0;
                for (var variant = 0; variant < 1024 && built < 192; variant++)
                {
                    var populator = CommandPopulator(1000 + variant * 37);
                    populator.OptionalParametersAtDefault = variant % 2 == 1;
                    DirectorCommand command;
                    try
                    {
                        command = (DirectorCommand)factory.Invoke(null, factory.GetParameters()
                            .Select(parameter => populator.Create(parameter.ParameterType)).ToArray());
                    }
                    catch (TargetInvocationException exception) when (
                        exception.InnerException is ArgumentException)
                    {
                        continue;
                    }

                    built++;
                    var payload = NetworkCommandCodec.Encode(command);
                    Assert.That(NetworkCommandCodec.TryDecode(payload, out var decoded), Is.True,
                        name + " did not decode: " + command);
                    probe.Compare(command, decoded, name);
                }

                if (built == 0)
                {
                    unbuilt.Add(name);
                }
            }

            Assert.That(unbuilt, Is.Empty,
                "No populated argument set was accepted by these factories, so their fields were " +
                "never round-tripped: " + string.Join(", ", unbuilt));
            probe.AssertNoDifferences("NetworkCommandCodec");
            probe.AssertExercised("NetworkCommandCodec");
        }

        private static CompletenessPopulator CommandPopulator(int seed) =>
            new CompletenessPopulator(seed)
                .With(WireEffectValue)
                // The only unit a Minion Battle answer names is a candidate's: a hero or a minion,
                // never a token (MinionBattleCandidate has exactly those two constructors).
                .With(populator => populator.Create<GuardsDigital.Core.Cards.MinionBattleCandidate>().Unit);

        /// <summary>
        /// Any effect value a payload may carry: every <see cref="GuardsDigital.Core.Effects.EffectValue"/> factory except
        /// <see cref="GuardsDigital.Core.Effects.EffectValue.TokenKind"/>, which is a definition-time address that "the
        /// snapshot writer and the network codec refuse" (8.91) and never a candidate or answer.
        /// Drawn from the factories by reflection, so a new value kind is round-tripped the day it
        /// is added unless it is refused the same way.
        /// </summary>
        internal static GuardsDigital.Core.Effects.EffectValue WireEffectValue(CompletenessPopulator populator)
        {
            var factories = typeof(GuardsDigital.Core.Effects.EffectValue)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.ReturnType == typeof(GuardsDigital.Core.Effects.EffectValue) &&
                    method.Name != nameof(GuardsDigital.Core.Effects.EffectValue.TokenKind) &&
                    method.GetParameters().Length == 1)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ToArray();
            var factory = factories[populator.NextChoice(factories.Length)];
            return (GuardsDigital.Core.Effects.EffectValue)factory.Invoke(
                null, new[] { populator.Create(factory.GetParameters()[0].ParameterType) });
        }

        // ---------------------------------------------------------------------------------------
        // The message codecs: every XFrame / TryDecodeX pair, found by reflection.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Every submission, result, projection, agreement, Undo and resync message
        /// <see cref="NetworkMessageCodec"/> frames survives its own decoder field by field.
        /// </summary>
        [Test]
        public void NetworkMessageCodec_CarriesEveryFieldOfEveryMessage()
        {
            AssertEveryFramedMessageRoundTrips(typeof(NetworkMessageCodec), expectedPairs: 8,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    // Milestone 9.29: an agreement statement names a unit only as a Minion Battle pick,
                    // and p.8 removes heroes and minions, never tokens.
                    { "RuleObject.TokenInstanceId", "a Minion Battle pick never names a token" },
                    { "TokenInstanceId.IsDefined", "a Minion Battle pick never names a token" },
                    { "TokenInstanceId.Value", "a Minion Battle pick never names a token" },
                });
        }

        /// <summary>
        /// Every lobby message <see cref="PrivateSessionFrameCodec"/> frames survives its own
        /// decoder field by field.
        /// </summary>
        [Test]
        public void PrivateSessionFrameCodec_CarriesEveryFieldOfEveryMessage()
        {
            AssertEveryFramedMessageRoundTrips(typeof(PrivateSessionFrameCodec), expectedPairs: 10);
        }

        private static void AssertEveryFramedMessageRoundTrips(
            Type codec, int expectedPairs, IReadOnlyDictionary<string, string> unexercised = null)
        {
            var encoders = codec.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name.EndsWith("Frame", StringComparison.Ordinal) &&
                    method.ReturnType == typeof(byte[]) && method.GetParameters().Length == 1)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(encoders.Length, Is.EqualTo(expectedPairs),
                codec.Name + " gained or lost a message encoder; this check pairs every one with " +
                "its decoder, so update the expected count once the new pair is covered here.");

            var probe = new CompletenessProbe();
            foreach (var encoder in encoders)
            {
                var stem = encoder.Name.Substring(0, encoder.Name.Length - "Frame".Length);
                var messageType = encoder.GetParameters()[0].ParameterType;
                var decoder = codec.GetMethod("TryDecode" + stem, BindingFlags.Public | BindingFlags.Static);
                Assert.That(decoder, Is.Not.Null, encoder.Name + " has no TryDecode" + stem + ".");

                for (var seed = 1; seed <= 3; seed++)
                {
                    var message = MessagePopulator(seed * 1000).Create(messageType);
                    var frame = (byte[])encoder.Invoke(null, new[] { message });
                    var payload = frame.Skip(PrivateSessionProtocol.FrameHeaderBytes).ToArray();
                    var arguments = new object[] { payload, null };
                    Assert.That((bool)decoder.Invoke(null, arguments), Is.True,
                        "TryDecode" + stem + " refused its own encoder's frame.");
                    probe.Compare(message, arguments[1], codec.Name + "." + stem);
                }
            }

            probe.AssertNoDifferences(codec.Name);
            if (unexercised == null)
                probe.AssertExercised(codec.Name);
            else
                probe.AssertExercised(codec.Name, unexercised);
        }

        private static CompletenessPopulator MessagePopulator(int seed) =>
            new CompletenessPopulator(seed)
                // Milestone 9.29. An agreement statement is drawn as each of its shapes in turn across the
                // three seeds - a preference, a hero pick and a minion take-back - so every arm the
                // statement codec writes is round-tripped rather than whichever factory a draw lands on.
                .With(populator => AgreementStatement(populator, seed / 1000 % 3))
                // A log row is made the way the host makes one, from an entry, so its enum columns
                // hold members the decoder admits.
                .With(populator => NetworkMatchLogCodec.ToRow(populator.Create<MatchLogEntry>()))
                // A grant's seat counts are bounded by the table (claimed <= total <= MaxSeats);
                // every other argument is drawn.
                .With(populator => new SeatGrantedMessage(
                    populator.Create<SeatRosterEntry>(),
                    PrivateSessionProtocol.MaxSeats - 1,
                    PrivateSessionProtocol.MaxSeats,
                    populator.NextInt(),
                    populator.NextString("capability-")));

        private static TeamAgreementCommand AgreementStatement(CompletenessPopulator populator, int shape)
        {
            var proposal = populator.Create<TeamAgreementProposalId>();
            switch (shape)
            {
                case 0:
                    return TeamAgreementCommand.Prefer(proposal, populator.Create<CardInstanceId>());
                case 1:
                    return TeamAgreementCommand.Pick(
                        proposal, RuleObject.ForHero(populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>()));
                default:
                    return TeamAgreementCommand.Unpick(
                        proposal, RuleObject.ForMinion(populator.Create<GuardsDigital.Core.Setup.MinionId>()));
            }
        }

        // ---------------------------------------------------------------------------------------
        // The match-log codec.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// A seat-public match-log line survives <see cref="NetworkMatchLogCodec"/>'s whole path -
        /// entry to row, row to bytes, bytes to row, row to entry - field by field.
        /// </summary>
        [Test]
        public void NetworkMatchLogCodec_CarriesEveryFieldOfAnEntry()
        {
            var probe = new CompletenessProbe();
            for (var seed = 1; seed <= 6; seed++)
            {
                var entry = new CompletenessPopulator(seed * 100).Create<MatchLogEntry>();
                probe.Compare(entry, RoundTripLogEntry(entry), "MatchLog(" + entry.Kind + ")");
            }

            probe.AssertNoDifferences("NetworkMatchLogCodec");
            probe.AssertExercised("NetworkMatchLogCodec");
        }

        // ---------------------------------------------------------------------------------------
        // SeatProjectionFactory's per-seat records against the projection they are cut from.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Every public member of <see cref="MatchSessionProjection"/> reaches
        /// <see cref="SeatProjection"/> under the same name, or is named here as deliberately
        /// withheld with its reason; and on the seat that owns the standing decision (or acts, when
        /// none stands) every carried member equals the source property by property, at sampled
        /// states of the official walk, except where the seat's copy is a named narrowing.
        /// </summary>
        [Test]
        public void SeatProjection_CarriesEveryProjectionMember_OrNamesWhyNot()
        {
            var members = typeof(MatchSessionProjection)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            var seatMembers = new HashSet<string>(
                CompletenessProbe.ReadableProperties(typeof(SeatProjection)).Select(property => property.Name),
                StringComparer.Ordinal);

            var undeclared = members
                .Where(name => !seatMembers.Contains(name) && !DirectorOnly.ContainsKey(name))
                .ToArray();
            Assert.That(undeclared, Is.Empty,
                "These projection members reach no seat and are not declared Director-only; carry " +
                "them on SeatProjection or name why a seat may not see them: " + string.Join(", ", undeclared));
            var staleDeclarations = DirectorOnly.Keys
                .Where(name => !members.Contains(name) || seatMembers.Contains(name))
                .ToArray();
            Assert.That(staleDeclarations, Is.Empty,
                "These Director-only declarations name a member that no longer exists or now reaches " +
                "the seat: " + string.Join(", ", staleDeclarations));

            var carried = members.Where(seatMembers.Contains).ToArray();
            var probe = new CompletenessProbe();
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var compared = 0;
            var withheldSeen = new HashSet<string>(StringComparer.Ordinal);
            void CompareFullSeat()
            {
                var source = host.Projection;
                var seat = FullSeat(host, source);
                if (seat == null)
                {
                    return;
                }

                var view = host.ProjectionForSeat(seat.Id);
                foreach (var name in carried)
                {
                    if (SeatNarrowed.ContainsKey(name))
                    {
                        withheldSeen.Add(name);
                        continue;
                    }

                    // Offers reach the actor only during its active turn and while nothing stands
                    // (8.30b); the source keeps listing them outside that window, where a seat is
                    // not offered them.
                    if ((source.DecisionContract != null || !source.IsTurnActive) &&
                        OfferMembers.Contains(name))
                    {
                        continue;
                    }

                    probe.Compare(
                        typeof(MatchSessionProjection).GetProperty(name).GetValue(source),
                        typeof(SeatProjection).GetProperty(name).GetValue(view),
                        "v" + source.Version + " seat " + seat.Id + "." + name);
                }

                compared++;
            }

            CompareFullSeat();
            var step = 0;
            OwnerScopedProjectionTests.DriveThroughSeats(host, OfficialBudget, _ =>
            {
                if (++step % 5 == 0)
                {
                    CompareFullSeat();
                }

                return false;
            }, OfficialSeed);

            Assert.That(compared, Is.GreaterThan(50), "The walk stopped short of a meaningful sample.");
            Assert.That(withheldSeen, Is.EquivalentTo(SeatNarrowed.Keys),
                "A narrowed-member declaration names a member the seat no longer carries.");
            probe.AssertNoDifferences("SeatProjection against its source");
        }

        /// <summary>
        /// The seat whose projection should hold everything the source holds: the one owner of the
        /// standing decision, or the acting seat when none stands; <c>null</c> when a decision
        /// stands on more than one seat (each then sees its own narrowed part).
        /// </summary>
        private static MatchSeat FullSeat(IMatchHost host, MatchSessionProjection source)
        {
            var owner = SeatProjectionFactory.OwnerOfStandingDecision(source);
            var owners = host.Seats.Seats.Where(seat => owner.IsOwnedBy(seat)).ToArray();
            if (source.DecisionContract != null)
            {
                return owners.Length == 1 &&
                    (!source.IsTurnActive || !source.SelectedActor.IsDefined ||
                        owners[0].Owns(SeatProjectionFactory.DecidingActor(source)))
                    ? owners[0]
                    : null;
            }

            return source.IsTurnActive && source.SelectedActor.IsDefined
                ? host.Seats.Seats.FirstOrDefault(seat => seat.Owns(SeatProjectionFactory.DecidingActor(source)))
                : host.Seats.Seats.First();
        }

        private static readonly HashSet<string> OfferMembers = new HashSet<string>(StringComparer.Ordinal)
        {
            "AvailableTurnActions", "AvailableFastTravelOptions", "AvailableClearOptions", "AvailableHoldOffer"
        };

        /// <summary>Projection members a seat never receives, each with its reason.</summary>
        private static readonly IReadOnlyDictionary<string, string> DirectorOnly =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "AcceptedCommands", "Director-only authority (8.30a): the accepted-command history" },
                { "AuthoritativeSnapshot", "Director-only authority (8.30a): the snapshot" },
                { "DiagnosticSnapshot", "Director-only authority (8.30a): derived from the snapshot and the diagnostic log" },
                { "DiagnosticEvents", "Director-only authority (8.30a): the whole rule-event log" },
                { "IsHistoryAvailable", "Director-only authority (8.30a): the unbounded Undo" },
                { "RoundEvents", "Director-only authority (8.30a): the rule events of the round" },
                { "IsTurnStarted", "derived: a seat reads IsTurnActive, the one predicate built from it" },
                { "IsTurnEnded", "derived: a seat reads IsTurnActive, the one predicate built from it" },
                { "PendingLanePushRequest", "Director-only: an automatic step with no decision for any seat (6.9)" },
                { "PendingImmediateLanePush", "Director-only: an automatic step with no decision for any seat (6.9)" },
                { "PendingSystemTransitionDescriptions", "owner-scoped: every hero's description; a seat carries its own as PendingSystemTransitionDescription (8.35u)" },
                { "PendingRevealRetrieval", "owner-scoped: the answering seat receives these commands through its narrowed DecisionContract, not as a separate list" },
            };

        /// <summary>
        /// Members a seat carries as a deliberate narrowing of the source's value; each narrowing
        /// copy is compared on its own below.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> SeatNarrowed =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Cards", "owner-scoped (F-ER2-07): the seat's own cards plus the public ones" },
                { "PlacedTokens", "FacedownTokenMask (8.61d); compared by FacedownTokenMask_CarriesEveryTokenFactButItsFace" },
                { "AvailableClearOptions", "FacedownTokenMask (8.61d); compared by FacedownTokenMask_CarriesEveryTokenFactButItsFace" },
                { "PendingSkillResolution", "SeatPendingSurface.NarrowResolution (8.40f); compared by NarrowResolution_CopiesEveryFrameFactButWhatItWithholds" },
                { "PendingSystemTransitionStep", "SeatPendingSurface.NarrowSystemTransition (8.35u); compared by the narrowing check" },
                { "PendingSystemTransitionDescription", "SeatPendingSurface.NarrowSystemTransitionDescription (8.35u); compared by the narrowing check" },
                { "DecisionContract", "SeatProjectionFactory.NarrowContract: the seat's own answers only" },
                { "IsSeatUndoAvailable", "supplied per seat by the host (8.35f); the Director's projection holds a constant false" },
                { "UndoMarker", "supplied per seat by the host (8.35r); the Director's projection holds a constant None" },
            };

        // ---------------------------------------------------------------------------------------
        // The narrowing copies a seat's projection is built from.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// <c>FacedownTokenMask</c> (8.61d) rebuilds a facedown token for a seat that does not
        /// control it, and a Clear option around such tokens: everything but the face survives.
        /// </summary>
        [Test]
        public void FacedownTokenMask_CarriesEveryTokenFactButItsFace()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var facedown = catalogs.Components.RegisteredTokens
                .Select(reference => catalogs.Components.LoadToken(reference))
                .Where(definition => definition.HidesItsKind)
                .ToArray();
            Assert.That(facedown, Is.Not.Empty, "No dealt token hides its kind; point this check at one that does.");

            var omissions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClearOptionProjection.SubsetInvocations", "derived: enumerated by the option's own constructor from the token ids AdjacentTokens carries" },
                { "PlacedToken.Definition", "the masked face itself (8.61d): the one fact the mask exists to withhold" },
                { "PlacedToken.PropertySources", "derived from the definition, so it follows the face" },
            };
            var probe = new CompletenessProbe(omissions);
            var populator = new CompletenessPopulator(9113);
            var tokens = facedown.Select(definition => new GuardsDigital.Core.Components.PlacedToken(
                populator.Create<GuardsDigital.Core.Components.TokenInstanceId>(),
                definition,
                populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>(),
                populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>(),
                populator.Create<CardInstanceId>(),
                populator.Create<GuardsDigital.Core.Board.HexCoordinate>())).ToArray();

            var masked = FacedownTokenMask.Mask(tokens, null);
            Assert.That(masked.Select(token => token.Definition),
                Is.EqualTo(facedown.Select(definition => definition.FacedownFace)),
                "The mask shows every non-controlling seat the facedown face.");
            probe.Compare(tokens, masked, "FacedownTokenMask.Mask");

            var option = new ClearOptionProjection(
                new ActionOffer(
                    populator.Create<CardInstanceId>(),
                    populator.Create<CardDefinitionReference>(),
                    populator.Create<ActionDefinitionId>(),
                    populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    ActionInvocationContext.Replacement,
                    true,
                    populator.Create<AttackRangeKind>(),
                    populator.Create<ActionCardStateTransition>()),
                populator.Create<ActionDefinitionId>(),
                tokens);
            var maskedOptions = FacedownTokenMask.MaskClearOptions(new[] { option }, null);
            Assert.That(maskedOptions[0], Is.Not.SameAs(option), "The option was not rebuilt, so nothing was checked.");
            probe.Compare(option, maskedOptions[0], "FacedownTokenMask.MaskClearOptions");

            const string UnstatedReason =
                "an available offer carries no unavailability reason by construction, and a Clear " +
                "with tokens is available";
            probe.AssertNoDifferences("FacedownTokenMask");
            probe.AssertEveryOmissionReached("FacedownTokenMask");
            probe.AssertExercised("FacedownTokenMask", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ActionOffer.Unavailability", UnstatedReason },
                { "ActionUnavailability.BlockedActivity", UnstatedReason },
                { "ActionUnavailability.BlockingActiveEffectId", UnstatedReason },
                { "ActionUnavailability.BlockingCardInstanceId", UnstatedReason },
                { "ActionUnavailability.BlockingHeroId", UnstatedReason },
                { "ActionUnavailability.IsStated", UnstatedReason },
                { "ActionUnavailability.Kind", UnstatedReason },
                { "ActiveEffectId.IsDefined", UnstatedReason },
                { "ActiveEffectId.Value", UnstatedReason },
            });
        }

        /// <summary>
        /// <c>SeatPendingSurface.NarrowSystemTransition</c> and <c>NarrowSystemTransitionDescription</c>
        /// (8.35u) rebuild p.15's concurrent level-up pause for one seat: everything survives but
        /// the other heroes' open items and the facts derived from them.
        /// </summary>
        [Test]
        public void SystemTransitionNarrowing_CopiesEveryFactButOtherHeroesItems()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seats = host.Seats.Seats.Take(2).ToArray();
            var populator = new CompletenessPopulator(9114)
                // rulebook p.16's four item slots, numbered from 1
                .With(p => new GuardsDigital.Core.Units.Heroes.HeroDashboardSlotPosition(1 + p.NextChoice(4)));
            var items = seats.Select(seat => new GuardsDigital.Core.Rules.Transitions.SystemTransitionOpenItem(
                new GuardsDigital.Core.Rules.Progression.HeroLevelUpItem(
                    seat.HeroId,
                    new GuardsDigital.Core.Rules.Progression.HeroLevel(2),
                    GuardsDigital.Core.Rules.Progression.HeroLevelUpItemKind.Upgrade).ToItemId(),
                seat.TeamSide,
                populator.Create<GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId[]>())).ToArray();
            var step = new GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep(
                populator.Create<GuardsDigital.Core.Rules.Transitions.SystemTransitionInstanceId>(),
                populator.Create<GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId>(),
                populator.Create<GuardsDigital.Core.Rules.Transitions.SystemTransitionStepId>(),
                items,
                populator.Create<GuardsDigital.Core.Rules.Transitions.SystemTransitionStepId[]>(),
                populator.Create<GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId[]>());
            Assert.That(step.IsConcurrent, Is.True);

            var surface = typeof(SeatPendingSurface);
            var narrowStep = surface.GetMethod("NarrowSystemTransition", BindingFlags.NonPublic | BindingFlags.Static);
            var narrowDescription = surface.GetMethod(
                "NarrowSystemTransitionDescription", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(narrowStep, Is.Not.Null);
            Assert.That(narrowDescription, Is.Not.Null);

            var stepOmissions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "PendingSystemTransitionStep.OpenItems", "narrowed to this seat's own item (8.35u); the item itself is compared whole below" },
                { "PendingSystemTransitionStep.IsConcurrent", "derived from OpenItems" },
                { "PendingSystemTransitionStep.ItemId", "derived from OpenItems (and refuses on a concurrent pause)" },
                { "PendingSystemTransitionStep.ChoosingSide", "derived from OpenItems (and refuses on a concurrent pause)" },
                { "PendingSystemTransitionStep.Options", "derived from OpenItems (and refuses on a concurrent pause)" },
                { "PendingSystemTransitionStep.AwaitedSide", "derived from OpenItems" },
                { "PendingSystemTransitionStep.AllOptions", "derived from OpenItems" },
                { "PendingSystemTransitionStep.Invocations", "derived from OpenItems: the other heroes' answers are what narrowing withholds" },
                { "SystemTransitionStepDescription.PendingStep", "the narrowed pause, compared on its own above" },
            };
            var probe = new CompletenessProbe(stepOmissions);

            // The Director's projection carrying the pause and one description per hero, built by
            // the projection's own constructor so its item/description agreement rule still holds.
            var descriptions = items.Select((item, index) =>
                new GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription(
                    GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade,
                    step,
                    seats[index].HeroId,
                    item.Options.Select(optionId =>
                        GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionDescription.Upgrade(
                            optionId,
                            populator.Create<CardInstanceId>(),
                            populator.Create<CardDefinitionReference>(),
                            populator.Create<CardInstanceId>(),
                            populator.Create<CardDefinitionReference>(),
                            populator.Create<CardInstanceId>(),
                            populator.Create<CardDefinitionReference>(),
                            populator.Create<GuardsDigital.Core.Cards.PrintedValueChannel>(),
                            populator.Create<GuardsDigital.Core.Units.Heroes.HeroDashboardSlotPosition>())),
                    2,
                    populator.NextInt(),
                    populator.NextInt(),
                    item.ItemId)).ToArray();
            var source = Rebuild(host.Projection, new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["pendingSystemTransitionStep"] = step,
                ["pendingSystemTransitionDescriptions"] = descriptions,
            });

            for (var index = 0; index < seats.Length; index++)
            {
                var narrowed = (GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep)
                    narrowStep.Invoke(null, new object[] { step, seats[index] });
                Assert.That(narrowed, Is.Not.SameAs(step));
                probe.Compare(step, narrowed, "NarrowSystemTransition(" + seats[index].Id + ")");
                probe.Compare(items[index], narrowed.OpenItems.Single(),
                    "NarrowSystemTransition(" + seats[index].Id + ").OpenItems[0]");

                var description = narrowDescription.Invoke(null, new object[] { source, narrowed });
                Assert.That(description, Is.Not.Null.And.Not.SameAs(descriptions[index]));
                probe.Compare(descriptions[index], description,
                    "NarrowSystemTransitionDescription(" + seats[index].Id + ")");
            }

            probe.AssertNoDifferences("System-transition narrowing");
            probe.AssertEveryOmissionReached("System-transition narrowing");
            probe.AssertExercised("System-transition narrowing");
        }

        /// <summary>
        /// <c>SeatPendingSurface.NarrowResolution</c> (8.40f) rebuilds a running effect frame for a
        /// seat - through <c>PendingResolution.With</c> and fresh outcome and choice records: every
        /// fact survives but the pause a non-owner is not handed and the card facts it may not name.
        /// </summary>
        [Test]
        public void NarrowResolution_CopiesEveryFrameFactButWhatItWithholds()
        {
            var populator = new CompletenessPopulator(9115)
                .With(WireEffectValue)
                .With(RunningFrame);
            var withheldPause = new CompletenessProbe(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "PendingResolution.PendingChoice", "the pause travels only to the seat that owns it (8.40f rule 1)" },
                { "PendingResolution.IsAwaitingChoice", "derived from PendingChoice" },
            });
            var unnamedCards = new CompletenessProbe(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "EffectNodeOutcome.Outputs", "card facts this seat may not name are dropped (8.40f rule 2)" },
                { "EffectChoiceRecord.Selected", "card facts this seat may not name are dropped (8.40f rule 2)" },
            });

            for (var seed = 0; seed < 48; seed++)
            {
                var frame = populator.Create<GuardsDigital.Core.Rules.EffectResolution.PendingResolution>();
                Assert.That(frame.PendingChoice, Is.Not.Null);

                // Rule 1 alone: a non-owner, with every card nameable, loses the pause and nothing else.
                var everyCard = new HashSet<CardInstanceId>(
                    frame.CompletedOutcomes.SelectMany(outcome => outcome.Outputs)
                        .Select(output => output.Value.CardInstanceId)
                        .Concat(frame.AcceptedChoices.SelectMany(choice => choice.Selected)
                            .Select(candidate => candidate.Value.CardInstanceId)));
                var withoutPause = SeatPendingSurface.NarrowResolution(frame, false, everyCard);
                Assert.That(withoutPause, Is.Not.SameAs(frame));
                Assert.That(withoutPause.PendingChoice, Is.Null);
                withheldPause.Compare(frame, withoutPause, "NarrowResolution(non-owner)#" + seed);

                // Rule 2 alone: the owner, naming no card, keeps the pause and loses only card facts.
                var withoutCards = SeatPendingSurface.NarrowResolution(frame, true, new HashSet<CardInstanceId>());
                unnamedCards.Compare(frame, withoutCards, "NarrowResolution(owner, no cards)#" + seed);
            }

            withheldPause.AssertNoDifferences("NarrowResolution, withheld pause");
            withheldPause.AssertEveryOmissionReached("NarrowResolution, withheld pause");
            withheldPause.AssertExercised("NarrowResolution, withheld pause",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    // Milestone 9.21c. A frame's repeat captures its bound piece at hero grain, because
                    // the snapshot persists the identity and not a body; a frame never holds one.
                    { "BoardObjectReference.HeroBody", "a frame's bound repeat target is captured at hero grain (9.21c)" }
                });
            unnamedCards.AssertNoDifferences("NarrowResolution, unnamed cards");
            unnamedCards.AssertEveryOmissionReached("NarrowResolution, unnamed cards");
        }

        /// <summary>
        /// A running effect frame standing on a pause, its parts drawn by the populator and tied
        /// together where the frame's own invariants require it: the pause belongs to the frame,
        /// a repeated instance runs under the repeated context, and the pause's candidates are
        /// distinct with its selection among them.
        /// </summary>
        private static GuardsDigital.Core.Rules.EffectResolution.PendingResolution RunningFrame(
            CompletenessPopulator populator)
        {
            var resolutionId = populator.Create<GuardsDigital.Core.Rules.EffectResolution.EffectResolutionId>();
            var candidates = new[]
            {
                GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidate.Value_(
                    GuardsDigital.Core.Effects.EffectValue.Card(populator.Create<CardInstanceId>())),
                GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidate.Value_(
                    GuardsDigital.Core.Effects.EffectValue.Hero(populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>())),
            };
            var pause = new GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice(
                resolutionId,
                populator.Create<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceId>(),
                populator.Create<GuardsDigital.Core.Effects.EffectNodeId>(),
                populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>(),
                GuardsDigital.Core.Rules.EffectResolution.EffectChoiceKind.SelectTarget,
                candidates,
                0,
                2,
                new[] { candidates[0] },
                Array.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidate>(),
                false,
                true,
                GuardsDigital.Core.Rules.EffectResolution.EffectChoiceValueVocabulary.None);
            var repeated = populator.NextChoice(2) == 0;
            return new GuardsDigital.Core.Rules.EffectResolution.PendingResolution(
                resolutionId,
                populator.Create<CardInstanceId>(),
                populator.Create<CardDefinitionReference>(),
                populator.Create<ActionDefinitionId>(),
                populator.Create<GuardsDigital.Core.Effects.EffectDefinitionReference>(),
                populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>(),
                populator.Create<GuardsDigital.Core.Units.Heroes.HeroId>(),
                repeated ? ActionInvocationContext.Repeated : ActionInvocationContext.PrintedPrimary,
                populator.Create<GuardsDigital.Core.Rules.EffectResolution.EffectContinuationFrame[]>(),
                populator.Create<GuardsDigital.Core.Rules.EffectResolution.EffectNodeOutcome[]>(),
                populator.Create<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceRecord[]>(),
                pause,
                repeated ? populator.Create<GuardsDigital.Core.Effects.RepeatContinuation>() : default);
        }

        // ---------------------------------------------------------------------------------------
        // The seat projection over the wire: seat record, detail record, log and the client model.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Every <see cref="IMatchReadModel"/> member of every seat's projection, at every step of
        /// seeded walks over every dealt hero and the pending-family fixtures, survives the whole
        /// wire path - <see cref="NetworkSeatProjectionCodec"/> (with
        /// <c>NetworkProjectionDetailCodec</c> inside it), the projection frame with its match-log
        /// rows (<see cref="NetworkMessageCodec"/>, <see cref="NetworkMatchLogCodec"/>) and the
        /// client's <see cref="NetworkMatchReadModel"/> - property by property, down to the nested
        /// records.
        /// </summary>
        /// <remarks>
        /// The input here is played rather than populated, because a seat projection's invariants
        /// are the rules' own. What a walk never populates is therefore listed by name in
        /// <see cref="WireUnexercised"/>, so a property added later that no walk reaches fails until
        /// it is reached or declared - the same growth guarantee the populated checks give.
        /// </remarks>
        [Test]
        public void SeatProjection_SurvivesTheWire_EveryMemberOfEverySeat()
        {
            var probe = new CompletenessProbe(WireOmissions);
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var envelope = new NetworkEnvelope("completeness", 1, 1, 1);
            var checkedViews = 0;
            foreach (var walk in Walks())
            {
                void CheckEverySeat()
                {
                    var first = true;
                    foreach (var seat in walk.Host.Seats.Seats)
                    {
                        var view = walk.Host.ProjectionForSeat(seat.Id);
                        var frame = NetworkMessageCodec.SeatProjectionFrame(new NetworkSeatProjectionMessage(
                            envelope,
                            NetworkSeatProjectionCodec.Encode(view),
                            view.MatchLog.Select(NetworkMatchLogCodec.ToRow).ToArray()));
                        Assert.That(NetworkMessageCodec.TryDecodeSeatProjection(
                            frame.Skip(PrivateSessionProtocol.FrameHeaderBytes).ToArray(), out var message),
                            Is.True, walk.Label + ": the projection frame did not decode.");
                        Assert.That(NetworkSeatProjectionCodec.TryDecode(message.ProjectionPayload, out var record),
                            Is.True, walk.Label + ": the seat record did not decode.");
                        var network = new NetworkMatchReadModel(walk.Host.Projection, catalogs, record, true);
                        network.ReplaceMatchLog(message.MatchLog.Select(NetworkMatchLogCodec.ToEntry).ToArray());
                        // The match log is one table-public list, identical for every seat of a state
                        // (8.35r), so it is compared on the first seat only - it is most of the cost.
                        probe.CompareThrough(typeof(IMatchReadModel), view, network,
                            walk.Label + " v" + view.Version + " seat " + seat.Id,
                            first ? null : new[] { nameof(IMatchReadModel.MatchLog) });
                        first = false;
                        checkedViews++;
                    }
                }

                CheckEverySeat();
                var shapes = new Dictionary<string, int>(StringComparer.Ordinal);
                var step = 0;
                OwnerScopedProjectionTests.DriveThroughSeats(walk.Host, walk.Budget, _ =>
                {
                    // The first ChecksPerShape states of every SHAPE, and every SampleEvery-th state
                    // besides: a dropped field is a property of the state's shape, not of how many
                    // commands led to it, and comparing all six seats at every step would cost the
                    // harness several times what it finds.
                    var shape = ShapeOf(walk.Host.Projection);
                    shapes.TryGetValue(shape, out var seen);
                    shapes[shape] = seen + 1;
                    if (seen < ChecksPerShape || ++step % SampleEvery == 0)
                    {
                        CheckEverySeat();
                    }

                    return false;
                }, walk.Seed);
                CheckEverySeat();
            }

            checkedViews += CheckOverlaidState(probe, catalogs, envelope);

            TestContext.WriteLine("Seat views compared across the wire: " + checkedViews +
                "; property comparisons: " + probe.Comparisons);
            probe.AssertNoDifferences("Seat projection over the wire");
            probe.AssertEveryOmissionReached("Seat projection over the wire");
            probe.AssertExercised("Seat projection over the wire", WireUnexercised);
        }

        /// <summary>
        /// The facts a seeded walk rarely or never holds - a rewritten printed Initiative (Hanu's
        /// Hurry up!, the 8.71b field), a held marker, a named Undo, a scoreboard
        /// with every column counted, a Pass offered on a commitment, and a greyed action that
        /// names its blocker - laid over a real mid-turn projection through the projection's own
        /// public constructor, then composed for every seat by the host's own
        /// <c>SeatProjectionFactory</c> and sent over the same wire path.
        /// </summary>
        /// <remarks>
        /// Every other constructor argument is the walked projection's own value, read back by
        /// name, so the overlay changes only what it names and the constructor's own invariants
        /// still hold for everything else.
        /// </remarks>
        private static int CheckOverlaidState(
            CompletenessProbe probe, MatchContentCatalogs catalogs, NetworkEnvelope envelope)
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 200, _ =>
                host.Projection.IsTurnActive && host.Projection.DecisionContract == null &&
                host.Projection.AvailableTurnActions.Count > 0, 2);
            var source = host.Projection;
            Assert.That(source.IsTurnActive && source.AvailableTurnActions.Count > 0, Is.True,
                "The overlay base walk no longer reaches an offered turn.");
            Assert.That(source.HeroDefenseModifiers, Is.Not.Empty,
                "The overlay base walk needs a standing hero's Defense modifier row.");

            var populator = new CompletenessPopulator(9110);
            var heroes = source.Participants.Heroes.Select(hero => hero.Id).ToArray();
            // A marker held below a turn slot, so every column of the row is stated: its definition
            // must be a printed retained marker (MarkerAttachment's own rule).
            var marker = catalogs.Components.RegisteredMarkers
                .Select(reference => catalogs.Components.LoadMarker(reference))
                .First(definition => definition.ReturnCondition ==
                    GuardsDigital.Core.Components.MarkerReturnCondition.RetainedAtRoundEndAndDefeat);
            var overlaid = Rebuild(source, new Dictionary<string, object>(StringComparer.Ordinal)
            {
                // Milestone 9.32h: one gold card lies facedown in its owner's discard pile (Bushido's p.10
                // swap, owed by 9.32g), so every seat's hand summary counts it by its known colour.
                ["cards"] = source.Cards.Select(card => new CardInstance(
                    card.Id, card.DefinitionReference, card.OwnerId,
                    card.Id.Equals(FacedownGoldOf(source))
                        ? new CardStateFacts(CardLocation.DiscardPile, false, CardResolutionStatus.None,
                            CardFaceOrientation.FaceDown)
                        : card.State,
                    new PrintedInitiativeOverride(populator.NextInt(), source.Cards[0].Id))).ToArray(),
                ["availableTurnActions"] = source.AvailableTurnActions.Select(offer =>
                    offer.WithAvailability(false, ActionUnavailability.BlockedBy(
                        populator.Create<GuardsDigital.Core.Rules.Modifications.RuleActivity>(),
                        heroes[1],
                        source.Cards[1].Id,
                        populator.Create<GuardsDigital.Core.Timing.ActiveEffectId>()))).ToArray(),
                ["heldMarkers"] = heroes.Select((hero, index) => new GuardsDigital.Core.Components.MarkerAttachment(
                    new GuardsDigital.Core.Components.MarkerInstanceId("marker-" + index),
                    marker,
                    heroes[(index + 1) % heroes.Length],
                    hero,
                    source.Cards[index].Id,
                    populator.Create<ActionDefinitionId>(),
                    index % RoundResolutionAuthority.TurnsPerRound + 1,
                    true)).ToArray(),
                ["commitments"] = source.Commitments.Select(commitment => new MatchCommitmentProjection(
                    commitment.HeroId, commitment.Status, true,
                    commitment.RevealedCardInstanceId, true)).ToArray(),
                ["scoreboard"] = heroes.Select(hero => new GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry(
                    hero, populator.NextInt(), populator.NextInt(), populator.NextInt(),
                    populator.NextInt(), populator.NextInt(), populator.NextInt())).ToArray(),
                // Milestone 9.30g. Every team's count toward a printed team victory (Gydion's Wish),
                // which no walk reaches before Gydion is dealt (9.30i).
                // One reached and one short, so every fact of a row is stated.
                ["teamVictoryTallies"] = source.Participants.Teams.Select((team, index) =>
                    new GuardsDigital.Core.Rules.Victory.TeamVictoryTally(
                        team.Id, index == 0 ? 3 : 1, 3)).ToArray(),
                // The seeded walks do not encounter a continuous Defense source. Populate its
                // typed identity on a real standing hero row so the wire's optional field is
                // compared with a non-default value instead of silently remaining untested.
                ["heroDefenseModifiers"] = source.HeroDefenseModifiers.Select((modifier, index) =>
                    index == 0
                        ? new HeroDefenseModifierProjection(
                            modifier.HeroId, modifier.HeroBody,
                            modifier.Contributions.Concat(new[]
                            {
                                new ValueContributionProjection(
                                    ValueContribution.FromContinuousSource(
                                        "completeness:continuous-defense",
                                        PrintedValueChannel.Defense, 1),
                                    "Continuous Defense source")
                            }))
                        : modifier).ToArray(),
            });

            var checkedViews = 0;
            foreach (var seat in host.Seats.Seats)
            {
                var view = SeatProjectionFactory.ForSeat(
                    overlaid,
                    seat,
                    host.ProjectionForSeat(seat.Id).TeamAgreement,
                    isSeatUndoAvailable: true,
                    undoMarker: MatchUndoMarker.Of(heroes[3], populator.NextInt()));
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(view), out var record), Is.True);
                var network = new NetworkMatchReadModel(overlaid, catalogs, record, true);
                network.ReplaceMatchLog(view.MatchLog);
                probe.CompareThrough(typeof(IMatchReadModel), view, network,
                    "overlaid v" + view.Version + " seat " + seat.Id,
                    new[] { nameof(IMatchReadModel.MatchLog) });
                checkedViews++;
            }

            return checkedViews;
        }

        [Test]
        public void RevealRetrievalDecision_OffersOnlyOwningSeatAndSurvivesWire()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var source = host.Projection;
            var owner = host.Seats.Seats[0];
            var cards = source.Cards.Where(card => card.OwnerId.Equals(owner.HeroId))
                .Take(2).Select(card => card.Id).ToArray();
            Assert.That(cards, Has.Length.EqualTo(2));
            var commands = cards.Select(cardId =>
                DirectorCommand.RetrieveOneAfterReveal(owner.HeroId, cardId)).ToArray();
            var overlaid = Rebuild(source, new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["pendingRevealRetrieval"] = commands
            });
            Assert.That(overlaid.DecisionContract.FamilyId,
                Is.EqualTo(PendingDecisionFamilies.RevealRetrieval));

            foreach (var seat in host.Seats.Seats)
            {
                var view = SeatProjectionFactory.ForSeat(overlaid, seat,
                    host.ProjectionForSeat(seat.Id).TeamAgreement);
                var isOwner = seat.HeroId.Equals(owner.HeroId);
                Assert.That(view.DecisionContract != null, Is.EqualTo(isOwner));
                Assert.That(view.AuthorizedCommands, isOwner ? Is.EqualTo(commands) : Is.Empty);
                Assert.That(view.DecisionOwner.HeroIds, Is.EqualTo(new[] { owner.HeroId }));
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(view), out var record), Is.True);
                var client = new NetworkMatchReadModel(overlaid,
                    MatchContentCatalogs.ForThisBuild(), record, true);
                Assert.That(client.AuthorizedCommands, isOwner ? Is.EqualTo(commands) : Is.Empty);
                Assert.That(client.DecisionContract != null, Is.EqualTo(isOwner));
            }
        }

        /// <summary>Milestone 9.32h. The first gold card in a hand, the overlay's facedown discard.</summary>
        private static CardInstanceId FacedownGoldOf(MatchSessionProjection source) =>
            source.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    source.Definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Progression.Color == CardColor.Gold)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .Select(card => card.Id)
                .First();

        /// <summary>
        /// The projection's public constructor, called with <paramref name="source"/>'s own value
        /// for every argument except those <paramref name="overrides"/> names.
        /// </summary>
        private static MatchSessionProjection Rebuild(
            MatchSessionProjection source, IReadOnlyDictionary<string, object> overrides)
        {
            var type = typeof(MatchSessionProjection);
            var constructor = type.GetConstructors()
                .OrderByDescending(candidate => candidate.GetParameters().Length)
                .First();
            var used = new HashSet<string>(StringComparer.Ordinal);
            var arguments = constructor.GetParameters().Select(parameter =>
            {
                if (overrides.TryGetValue(parameter.Name, out var replaced))
                {
                    used.Add(parameter.Name);
                    return replaced;
                }

                if (parameter.Name == "authoritativeSnapshot")
                {
                    return type.GetField("authoritativeCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(source);
                }

                var property = type.GetProperty(
                    char.ToUpperInvariant(parameter.Name[0]) + parameter.Name.Substring(1));
                Assert.That(property, Is.Not.Null,
                    "The projection constructor's '" + parameter.Name + "' has no same-named member to read back.");
                return property.GetValue(source);
            }).ToArray();
            Assert.That(used, Is.EquivalentTo(overrides.Keys), "An overlay named no constructor argument.");
            return (MatchSessionProjection)constructor.Invoke(arguments);
        }

        /// <summary>
        /// Milestone 9.11, finding F2 (wire /25). A level-up description reaches a network seat
        /// naming the item it describes, as the host's view of that seat does.
        /// </summary>
        /// <remarks>
        /// The description row did not carry <c>SystemTransitionStepDescription.ItemId</c>, so a
        /// client rebuilt every p.15 upgrade question with no item while the host's named
        /// <c>hero:level:upgrade</c>. Deriving it on the client from the prompt kind would be a
        /// second reading of the host's answer; the row carries it instead.
        /// </remarks>
        [Test]
        public void NetworkClient_CarriesTheItemALevelUpDescriptionNames()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            SeatProjection found = null;
            OwnerScopedProjectionTests.DriveThroughSeats(host, 400, _ =>
            {
                found = host.Seats.Seats
                    .Select(seat => host.ProjectionForSeat(seat.Id))
                    .FirstOrDefault(view => view.PendingSystemTransitionDescription?.ItemId.IsDefined == true);
                return found != null;
            }, 2);
            Assert.That(found, Is.Not.Null, "The seeded walk no longer reaches a level-up description.");

            var network = ThroughTheWire(host, found);
            Assert.That(network.PendingSystemTransitionDescription, Is.Not.Null);
            Assert.That(network.PendingSystemTransitionDescription.ItemId,
                Is.EqualTo(found.PendingSystemTransitionDescription.ItemId),
                "The description names its item on the wire; a client reads it rather than guessing it.");
        }

        /// <summary>
        /// Milestone 9.11, finding F3 (wire /25). A card-text choice reaches a network seat with the
        /// node's own "declinable regardless of minimum" answer, not the Decline offer it helps
        /// derive.
        /// </summary>
        /// <remarks>
        /// The row carried only whether a Decline is offered, and a client passed that as the node's
        /// flag - so a choice whose Decline stands because its minimum is zero read "declinable
        /// regardless of minimum" on every client while the host held false.
        /// </remarks>
        [Test]
        public void NetworkClient_CarriesTheChoicesOwnDeclineFlag()
        {
            SeatProjection found = null;
            IMatchHost foundHost = null;
            foreach (var walk in Walks())
            {
                OwnerScopedProjectionTests.DriveThroughSeats(walk.Host, walk.Budget, _ =>
                {
                    var choice = walk.Host.Projection.PendingEffectChoice;
                    if (choice == null || choice.DeclinableRegardlessOfMinimum ||
                        choice.DeclineInvocation == null)
                    {
                        return false;
                    }

                    found = walk.Host.Seats.Seats
                        .Select(seat => walk.Host.ProjectionForSeat(seat.Id))
                        .FirstOrDefault(view => view.PendingEffectChoice != null);
                    return found != null;
                }, walk.Seed);
                if (found != null)
                {
                    foundHost = walk.Host;
                    break;
                }
            }

            Assert.That(found, Is.Not.Null,
                "No walk reaches a choice whose Decline stands for a reason other than the node's flag.");
            var network = ThroughTheWire(foundHost, found);
            Assert.That(network.PendingEffectChoice, Is.Not.Null);
            Assert.That(network.PendingEffectChoice.DeclinableRegardlessOfMinimum,
                Is.EqualTo(found.PendingEffectChoice.DeclinableRegardlessOfMinimum),
                "The node's own flag crosses; the Decline offer is a consequence of it, not a copy.");
            Assert.That(network.PendingEffectChoice.DeclineInvocation != null, Is.True,
                "and the Decline offer is unchanged.");
        }

        private static NetworkMatchReadModel ThroughTheWire(IMatchHost host, SeatProjection view)
        {
            Assert.That(NetworkSeatProjectionCodec.TryDecode(
                NetworkSeatProjectionCodec.Encode(view), out var record), Is.True);
            return new NetworkMatchReadModel(host.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
        }

        /// <summary>
        /// Milestone 9.11, finding F1. A network seat standing inside a Movement frame names the
        /// card that is resolving, exactly as the host's own view of that seat does.
        /// </summary>
        /// <remarks>
        /// <para>8.58 published <see cref="IMatchReadModel.ResolvingSourceCardInstanceId"/> so a
        /// card-text question can name its card on a client, and 8.80 made the client read the
        /// Skill and Attack slots from the rows the wire delivers. The Movement slot was left on
        /// <see cref="NetworkMatchReadModel.PendingMovementResolution"/>, which is <c>null</c> by
        /// construction (8.40f), so it answered "no card" whenever the Movement frame was the one
        /// standing - although its row has carried the card since 8.32. The wire completeness walk
        /// found it on the official seed-2 walk: Tigerclaw's Blink Strike and Light-Fingered.</para>
        /// </remarks>
        [Test]
        public void NetworkClient_NamesTheResolvingCard_WhileAMovementFrameStands()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var catalogs = MatchContentCatalogs.ForThisBuild();
            SeatProjection found = null;
            OwnerScopedProjectionTests.DriveThroughSeats(host, 200, _ =>
            {
                found = host.Seats.Seats
                    .Select(seat => host.ProjectionForSeat(seat.Id))
                    .FirstOrDefault(view => view.HasPendingMovementResolution &&
                        !view.HasPendingSkillResolution && !view.HasPendingAttackResolution);
                return found != null;
            }, 2);
            Assert.That(found, Is.Not.Null, "The seeded walk no longer stops inside a Movement frame.");

            Assert.That(NetworkSeatProjectionCodec.TryDecode(
                NetworkSeatProjectionCodec.Encode(found), out var record), Is.True);
            var network = new NetworkMatchReadModel(host.Projection, catalogs, record, true);

            Assert.That(found.ResolvingSourceCardInstanceId.IsDefined, Is.True);
            Assert.That(network.ResolvingSourceCardInstanceId,
                Is.EqualTo(found.ResolvingSourceCardInstanceId),
                "The Movement frame's card crosses on its own row; a client standing inside that " +
                "frame names it, as the host's view of the same seat does.");
        }

        /// <summary>
        /// What the network client deliberately does not rebuild, each with its reason.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> WireOmissions =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    "IMatchReadModel.PendingMovementResolution",
                    "owner-scoped frame (8.40f): the client materialises no resolution frame; its " +
                    "presence crosses as HasPendingMovementResolution"
                },
                {
                    "IMatchReadModel.PendingAttackResolution",
                    "owner-scoped frame (8.40f): the client materialises no resolution frame; its " +
                    "presence crosses as HasPendingAttackResolution and its card as " +
                    "ResolvingSourceCardInstanceId (8.80)"
                },
                {
                    "IMatchReadModel.PendingSkillResolution",
                    "owner-scoped frame (8.40f): completed outcomes and accepted choices can name " +
                    "another seat's cards; presence crosses as HasPendingSkillResolution and the " +
                    "resolving card as ResolvingSourceCardInstanceId (8.58)"
                },
            };

        /// <summary>
        /// Properties the walks compare but never see holding anything but their default, so a
        /// drop there would go unnoticed. Each is a named limitation of this check, not a pass.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> WireUnexercised =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Covered instead by a populated check in this fixture.
                { "MatchLogEntry.BlockingCardInstanceId", Covered("NetworkMatchLogCodec_CarriesEveryFieldOfAnEntry") },
                { "MatchLogEntry.BlockingHeroId", Covered("NetworkMatchLogCodec_CarriesEveryFieldOfAnEntry") },
                { "MatchLogEntry.TargetHeroId", Covered("NetworkMatchLogCodec_CarriesEveryFieldOfAnEntry") },
                { "DirectorCommand.SystemTransitionDefinitionId", Covered("NetworkCommandCodec_CarriesEveryFieldOfEveryFactory") },
                { "DirectorCommand.SecondCardInstanceId", Covered("NetworkCommandCodec_CarriesEveryFieldOfEveryFactory") },
                { "MinionBattleChoiceCommand.HeroId", Covered("NetworkCommandCodec_CarriesEveryFieldOfEveryFactory") },
                // Never non-default on a SEAT by construction.
                { "BoardObjectReference.HeroBody", "9.21c: a projection names board objects at hero grain - a candidate's own space says which body of a many-bodied hero it is - so a preview's occupant is normalized before it is projected" },
                { "BoardObjectReference.TokenInstanceId", NotReached("a token-valued board object reference in a projected choice; token interactions are covered by focused Illusion and token presentation tests") },
                { "PendingSystemTransitionStep.IsConcurrent", "a seat's pause is narrowed to its own one item (8.35u), so it is never concurrent on a seat" },
                { "MarkerAttachment.PropertySources", "derived from the trusted marker definition, which no dealt marker gives a property" },
                { "PlacedToken.PropertySources", "derived from the trusted token definition; no token the walks place prints a property" },
                // Milestone 9.29: a seat answers the Minion Battle through its own picks, and the authority's
                // selection stays empty until every pick matches - then the host fills and confirms it inside
                // one submission - so no seat projection ever carries a non-empty shared selection.
                { "PendingMinionBattleChoice.CanConfirm", NinePointTwoNine },
                { "PendingMinionBattleChoice.DeselectInvocations", NinePointTwoNine },
                { "PendingMinionBattleChoice.LegalDeselectMinionIds", NinePointTwoNine },
                { "PendingMinionBattleChoice.LegalDeselectUnits", NinePointTwoNine },
                { "PendingMinionBattleChoice.SelectedMinionIds", NinePointTwoNine },
                { "PendingMinionBattleChoice.SelectedUnits", NinePointTwoNine },
                // Not reached by the walks: a drop here would go unseen until a walk or fixture reaches it.
                { "DefenseChoicePreviewProjection.BlockResult", NotReached("a Defense preview whose choice blocks") },
                { "DefenseChoicePreviewProjection.SuppressesMinionContributions", NotReached("a Defense card suppressing minion support") },
                { "EffectChoiceCandidatePreview.ResultingCoordinate", NotReached("an effect-choice preview with a resulting space") },
                { "EffectValue.BooleanValue", NotReached("a true boolean effect value in a projected choice") },
                { "EffectValue.NodeId", NotReached("a node value in a projected effect choice") },
                { "EndOfRoundStageOutcome.Kind", NotReached("a non-default end-of-round stage outcome in the retained window") },
                { "HeroRespawnDestination.IsFallback", NotReached("a respawn with only fallback destinations") },
                { "MatchOutcome.HasSoloWinner", NotReached("a solo victory") },
                { "MatchOutcome.WinningHeroId", NotReached("a solo victory") },
                { "MinionBattleCandidate.HeroId", NotReached("a hero counted as a Minion Battle candidate") },
                { "PendingDisplacedMinionCorrection.SubjectCandidates", NotReached("a correction at its subject stage") },
                { "PendingEffectChoice.StopInvocation", NotReached("a mode selection whose every option is refused") },
                { "PendingSystemTransitionStep.CompletedStepIds", NotReached("a transition paused after a completed step") },
                { "RemovalPreventionCost.AdmittedColors", NotReached("a removal-prevention price naming colours") },
                { "RuleObject.HeroId", NotReached("a hero unit in a Minion Battle selection") },
                { "RuleObject.TokenInstanceId", NotReached("a token unit in a projected selection") },
                { "SystemTransitionOptionDescription.Destination", NotReached("a Lane Push respawn placement") },
                { "SystemTransitionOptionDescription.DestinationSpaceId", NotReached("a Lane Push respawn placement") },
                { "SystemTransitionStepDescription.RemainingCoins", NotReached("a level-up with coins left over") },
                { "TeamAgreementParticipantProjection.PreferredCardInstanceId", NotReached("a team agreement with a statement made") },
                { "TurnCardAssignment.PassedWithEmptyHand", NotReached("a pass with an empty hand") },
                // Milestone 9.32h removed the five 9.32a and 9.32g declarations here (a facedown slot card,
                // its withheld row and known colour, a facedown discard's colour, a log row's card moves):
                // the "facedown gold fixture" walk and the overlay now reach every one.
                // A card move never departs facedown: no swap names a facedown card (p.10, 9.32a).
                { "CardMoveFact.DepartsFacedown", NotReached("a card moved from a facedown state") },
                // Milestone 9.32h. Thirty dealt heroes rotate the six-hero line-ups so that none of their
                // seeded walks reaches a Clear with an adjacent token any more (9.30i's twenty-nine did);
                // the declarations 9.30i removed come back, the command fields covered by the codec's
                // own factory walk as before.
                { "DirectorCommand.ActionDefinitionId", Covered("NetworkCommandCodec_CarriesEveryFieldOfEveryFactory") },
                { "DirectorCommand.TokenInstanceIds", Covered("NetworkCommandCodec_CarriesEveryFieldOfEveryFactory") },
                { "ClearOptionProjection.AdjacentTokens", NotReached("a Clear with an adjacent token") },
                { "ClearOptionProjection.SubsetInvocations", NotReached("a Clear with an adjacent token") },
                { "ValueContribution.HasSourceActiveEffectId", NotReached("a contribution sourced from an Active effect") },
                { "ValueContribution.SourceActiveEffectId", NotReached("a contribution sourced from an Active effect") },
            };

        private const string NinePointTwoNine =
            "9.29: on a seat-served host the shared selection stays empty until every seat's picks match";

        private static string Covered(string check) => "exercised with every field populated by " + check;

        private static string NotReached(string state) =>
            "LIMITATION: no walk, fixture or overlay here reaches " + state;

        internal sealed class Walk
        {
            public Walk(string label, IMatchHost host, int budget, int seed)
            {
                Label = label;
                Host = host;
                Budget = budget;
                Seed = seed;
            }

            public string Label { get; }
            public IMatchHost Host { get; }
            public int Budget { get; }
            public int Seed { get; }
        }

        /// <summary>
        /// The seeded official walk, one walk per line-up of six covering every dealt hero, and the
        /// pending-family fixtures a random walk rarely stops inside.
        /// </summary>
        internal static IEnumerable<Walk> Walks()
        {
            yield return new Walk("official", MatchHost.Compose(
                OwnerScopedProjectionTests.CreateOfficialInitialState()), OfficialBudget, OfficialSeed);

            var dealt = GuardsDigital.Core.Setup.QuickGameSetupConfiguration.DealtHeroes.ToArray();
            var lineups = (dealt.Length + 5) / 6;
            for (var lineup = 0; lineup < lineups; lineup++)
            {
                var picked = Enumerable.Range(0, 6)
                    .Select(offset => dealt[(lineup * 6 + offset) % dealt.Length])
                    .ToArray();
                yield return new Walk(
                    "line-up " + string.Join(",", picked.Select(hero => hero.ContentId.Value)),
                    OwnerScopedProjectionTests.ComposeForLineup(
                        new GuardsDigital.Core.Setup.HeroLineup(picked.Take(3), picked.Skip(3))),
                    LineupBudget, lineup + 1);
            }

            yield return new Walk("defense fixture",
                new MatchHost(ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()), 0, 1);
            yield return new Walk("respawn fixture",
                new MatchHost(HeroRespawnTests.CreatePendingRespawnSession()), 0, 1);
            yield return new Walk("removal-prevention fixture",
                new MatchHost(ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause()), 0, 1);
            // Milestone 9.21a. A hero standing on two spaces (Razzle's many bodies, staged): the board's
            // hero rows carry a second body, the Attack choice offers each body with its own Defense
            // preview and names the body in every answer, and the Defense window stands on the second.
            yield return new Walk("two-body attack-target fixture",
                new MatchHost(HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _, out _, out _)), 0, 1);
            yield return new Walk("two-body defense fixture",
                new MatchHost(HeroBodiesBoard.CreateSessionAtTheDefenseOfBodyTwo()), 0, 1);
            // Milestone 9.21b. "Which of you acts" standing on the same board: the decision contract
            // carries one ChooseActingBody answer per body, each naming its body.
            yield return new Walk("two-body acting-body pick fixture",
                new MatchHost(HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out _)), 0, 1);
            // Milestone 9.30b. A hero owning a spellbook with one spell removed faceup: every seat's
            // hand summaries carry a non-zero book count, and the removed spell crosses as a public
            // card in its new location while the book's facedown spells reach their owner alone.
            yield return new Walk("spellbook fixture",
                new MatchHost(SpellbookZoneTests.CreateSession(true)), 0, 1);
            // Milestone 9.32h (owed by 9.32a and 9.32g). Takahide dealt: Float like a Butterfly swapped
            // for a deck gold (a swap row with card moves in the log), then Bushido turned the resolved
            // gold facedown in its slot - the withheld row and its known colour on every other seat.
            yield return new Walk("facedown gold fixture",
                new MatchHost(TakahideDealingTests.CreateFacedownGoldSession()), 0, 1);
        }

        /// <summary>
        /// The official walk's seed and budget: an offline scan of seeds 1-30 at 1,200 steps
        /// (this driver, base 43294cd9) put seed 19's first Attack-target choice at step 223, its
        /// Minion Battle at 235 and its ending at 779 - the three shapes most seeds never reach.
        /// </summary>
        private const int OfficialSeed = 19;
        private const int OfficialBudget = 800;
        private const int LineupBudget = 200;
        private const int SampleEvery = 10;
        private const int ChecksPerShape = 6;

        /// <summary>What a state is, for sampling: which decision stands, in which phase, whose turn.</summary>
        private static string ShapeOf(MatchSessionProjection projection) =>
            (projection.DecisionContract?.FamilyId.Value ?? "-") + "|" + projection.Phase + "|" +
            (projection.PendingDefensePreview != null) + "|" + projection.Outcome.HasEnded;

        internal static MatchLogEntry RoundTripLogEntry(MatchLogEntry entry)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                {
                    NetworkMatchLogCodec.Write(writer, NetworkMatchLogCodec.ToRow(entry));
                }

                stream.Position = 0;
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                {
                    Assert.That(NetworkMatchLogCodec.TryRead(reader, out var row), Is.True,
                        "The log codec refused its own row: " + entry);
                    Assert.That(stream.Position, Is.EqualTo(stream.Length));
                    return NetworkMatchLogCodec.ToEntry(row);
                }
            }
        }
    }
}
