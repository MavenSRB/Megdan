using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnapshotEnumReaderTests
    {
        /// <summary>
        /// Every enum the authoritative snapshot payload carries. The decode boundary is uniform
        /// only if it holds for all of them, so the matrix below is driven from this one list.
        /// Code-truth check 6 in the living-document validator is the other half: it proves every
        /// reader in the snapshot decoder goes through <c>SnapshotEnumReader</c>, so a value that
        /// reaches the payload cannot bypass the behaviour these cases pin.
        /// </summary>
        private static readonly Type[] PayloadEnums =
        {
            typeof(TeamSide),
            typeof(CardLocation),
            typeof(CardResolutionStatus),
            typeof(CardFaceOrientation),
            typeof(ActionInvocationContext),
            typeof(AttackRangeKind),
            typeof(PrintedValueChannel),
            typeof(ValueContributionKind),
            typeof(EndOfRoundStage),
            typeof(EndOfRoundStageOutcomeKind),
            typeof(RoundResolutionPhase),
            typeof(AttackResolutionPhase),
            typeof(AttackTargetKind),
            typeof(CardActionContinuationKind),
            typeof(TimingCheckpoint),
            // Milestone 6.2a removed ActiveEffectDurationKind, ActiveEffectAffectedScopeKind and
            // ActiveEffectCancellationCondition from this list because they left the wire entirely:
            // they are definition semantics a restore now takes from the registered capability
            // catalog, so no payload states them and there is nothing to decode.
            typeof(ActiveEffectPhase),
            typeof(ContinuousRuleSourceKind),
            typeof(StandardMinionType),
            typeof(EffectExecutionResultKind),
            typeof(EffectChoiceKind),
            typeof(EffectChoiceCommandKind),
            typeof(EffectValueKind),
            typeof(EffectChoiceCandidateKind)
        };

        [Test]
        public void Read_RejectsEveryUndefinedValueOfEveryPayloadEnum()
        {
            var failures = new List<string>();
            foreach (var payloadEnum in PayloadEnums)
            {
                foreach (var undefined in UndefinedValuesFor(payloadEnum))
                {
                    var label = $"{payloadEnum.Name}={undefined}";
                    var exception = Assert.Throws<SnapshotLoadException>(
                        () => Read(payloadEnum, undefined, "probe field"),
                        $"{label} was accepted, so an undefined value reaches the restored graph.");
                    if (exception.Reason != SnapshotLoadRejectionReason.UnrecognizedEnumValue)
                    {
                        failures.Add($"{label} rejected as {exception.Reason}");
                    }
                    else if (!exception.Message.Contains("probe field") ||
                             !exception.Message.Contains(payloadEnum.Name) ||
                             !exception.Message.Contains(undefined.ToString()))
                    {
                        failures.Add($"{label} message does not name the field, type, and value: {exception.Message}");
                    }
                }
            }

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void Read_AcceptsEveryDeclaredMemberOfEveryPayloadEnum()
        {
            var failures = new List<string>();
            foreach (var payloadEnum in PayloadEnums)
            {
                foreach (var declared in Enum.GetValues(payloadEnum))
                {
                    var ordinal = Convert.ToInt32(declared);
                    var restored = Read(payloadEnum, ordinal, "probe field");
                    if (!Equals(restored, declared))
                    {
                        failures.Add($"{payloadEnum.Name}.{declared} round-tripped as {restored}");
                    }
                }
            }

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void Read_ConsumesExactlyFourBytesSoTheStreamStaysAligned()
        {
            // The defect this milestone closes was not only acceptance. An undefined AttackTargetKind
            // fell through a ternary to `default` and skipped the ReadString that a Hero or Minion
            // target would have consumed, desynchronizing every field after it. A reader that leaves
            // the stream in the wrong place is a decode-boundary failure even when it throws.
            using (var stream = new MemoryStream(Payload(writer =>
            {
                writer.Write((int)TeamSide.Blue);
                writer.Write("sentinel");
            })))
            using (var reader = new BinaryReader(stream))
            {
                Assert.That(SnapshotEnumReader.Read<TeamSide>(reader, "team side"), Is.EqualTo(TeamSide.Blue));
                Assert.That(reader.ReadString(), Is.EqualTo("sentinel"));
            }
        }

        [Test]
        public void ReadNullable_ReadsThePresenceFlagAndRejectsAnUndefinedValueBehindIt()
        {
            using (var absent = new BinaryReader(new MemoryStream(Payload(writer =>
            {
                writer.Write(false);
                writer.Write("sentinel");
            }))))
            {
                Assert.That(SnapshotEnumReader.ReadNullable<TimingCheckpoint>(absent, "ended at"), Is.Null);
                Assert.That(absent.ReadString(), Is.EqualTo("sentinel"),
                    "An absent value must consume the flag and nothing else.");
            }

            using (var present = new BinaryReader(new MemoryStream(Payload(writer =>
            {
                writer.Write(true);
                writer.Write((int)TimingCheckpoint.RoundEnd);
            }))))
            {
                Assert.That(SnapshotEnumReader.ReadNullable<TimingCheckpoint>(present, "ended at"),
                    Is.EqualTo(TimingCheckpoint.RoundEnd));
            }

            using (var undefined = new BinaryReader(new MemoryStream(Payload(writer =>
            {
                writer.Write(true);
                writer.Write(int.MaxValue);
            }))))
            {
                var exception = Assert.Throws<SnapshotLoadException>(
                    () => SnapshotEnumReader.ReadNullable<TimingCheckpoint>(undefined, "ended at"));
                Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnrecognizedEnumValue));
            }
        }

        [Test]
        public void PayloadEnums_AreIntBackedAndNotFlagsSoTheDeclaredMemberCheckIsWellFounded()
        {
            // Read<T> compares against the declared members. That is the right question only for a
            // non-flags, int-backed enum: a flags enum's legal combinations are not themselves
            // declared members, and a non-int-backed enum would not survive the writer's
            // `writer.Write((int)value)` in the first place. SnapshotEnumReader throws on both, so
            // this states the assumption the whole boundary rests on.
            var offenders = PayloadEnums
                .Where(payloadEnum =>
                    Enum.GetUnderlyingType(payloadEnum) != typeof(int) ||
                    payloadEnum.IsDefined(typeof(FlagsAttribute), false))
                .Select(payloadEnum => payloadEnum.Name)
                .ToArray();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void PayloadEnums_AreListedOnceEach()
        {
            Assert.That(PayloadEnums, Is.Unique);
            Assert.That(PayloadEnums.All(payloadEnum => payloadEnum.IsEnum), Is.True);
        }

        /// <summary>
        /// Values that cannot be a declared member: below the lowest, above the highest, and the two
        /// extremes. `max + 1` is the one a newly added member would occupy, which is exactly the
        /// incompatible-build case this boundary exists for.
        /// </summary>
        private static IEnumerable<int> UndefinedValuesFor(Type payloadEnum)
        {
            var declared = Enum.GetValues(payloadEnum).Cast<object>().Select(Convert.ToInt32).ToArray();
            var candidates = new[] { -1, int.MinValue, int.MaxValue, declared.Max() + 1, declared.Min() - 1 };
            return candidates.Where(candidate => !declared.Contains(candidate)).Distinct();
        }

        private static object Read(Type payloadEnum, int raw, string fieldName)
        {
            var method = typeof(SnapshotEnumReader)
                .GetMethod(nameof(SnapshotEnumReader.Read))
                .MakeGenericMethod(payloadEnum);
            using (var reader = new BinaryReader(new MemoryStream(Payload(writer => writer.Write(raw)))))
            {
                try
                {
                    return method.Invoke(null, new object[] { reader, fieldName });
                }
                catch (System.Reflection.TargetInvocationException invocation)
                {
                    throw invocation.InnerException;
                }
            }
        }

        private static byte[] Payload(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                write(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
