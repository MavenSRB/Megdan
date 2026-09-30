using System;
using System.Collections.Generic;
using System.IO;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.32. The bounded command DTO: one discriminated encoding per
    /// <see cref="DirectorCommandKind"/>, carrying exactly the fields that kind's own factory
    /// takes, and rebuilt through those factories so every guard they enforce locally is enforced
    /// on wire input too.
    /// </summary>
    /// <remarks>
    /// <para>The decision record's rule is that the gameplay payload "is one discriminated,
    /// bounded command DTO mapped by the host to an offered typed command; it is never an
    /// arbitrary serialized <c>DirectorCommand</c> object graph". This codec is the first half of
    /// that sentence: a fixed field list per kind, every enum validated where it is read, every
    /// identity checked non-empty before a Core type is constructed, and trailing bytes refused.
    /// The mapping half lives in <see cref="PrivateSessionHost"/>, which submits the seat's own
    /// offered instance when the decoded command equals one - so a decoded object never reaches
    /// the authority except by being refused there.</para>
    /// <para>Encoding is total over the command vocabulary and deterministic, so two equal
    /// commands encode identically; decoding is the exact inverse for every command a projection
    /// can publish, which the codec fixture proves by round-tripping live offered surfaces.</para>
    /// </remarks>
    public static class NetworkCommandCodec
    {
        /// <summary>The most token identities one Clear subset may name on the wire.</summary>
        public const int MaxTokenSelections = 64;

        public static byte[] Encode(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8))
            {
                Write(writer, command);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static bool TryDecode(byte[] payload, out DirectorCommand command)
        {
            command = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8))
                {
                    if (!TryRead(reader, out command))
                    {
                        return false;
                    }

                    if (stream.Position != stream.Length)
                    {
                        command = null;
                        return false;
                    }

                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                command = null;
                return false;
            }
        }

        internal static void Write(BinaryWriter writer, DirectorCommand command)
        {
            writer.Write((int)command.Kind);
            switch (command.Kind)
            {
                case DirectorCommandKind.CommitCard:
                    writer.Write(command.HeroId.Value);
                    writer.Write(command.CardInstanceId.Value);
                    return;
                case DirectorCommandKind.Pass:
                    writer.Write(command.HeroId.Value);
                    return;
                case DirectorCommandKind.ChooseTiedActor:
                    writer.Write((int)command.TeamSide);
                    writer.Write(command.CardInstanceId.Value);
                    return;
                case DirectorCommandKind.PerformHold:
                    writer.Write(command.CardInstanceId.Value);
                    return;
                case DirectorCommandKind.StartAction:
                    WriteOptionalId(writer, command.CardInstanceId.IsDefined, command.CardInstanceId.Value);
                    writer.Write(command.ActionDefinitionId.Value);
                    return;
                case DirectorCommandKind.ChooseMovementDestination:
                    WriteCoordinate(writer, command.Coordinate);
                    WriteResolutionPair(writer, command);
                    return;
                case DirectorCommandKind.StopMovement:
                    WriteResolutionPair(writer, command);
                    return;
                case DirectorCommandKind.ChooseAttackTarget:
                    WriteOptionalId(writer, command.HeroId.IsDefined, command.HeroId.Value);
                    WriteOptionalId(writer, command.MinionId.IsDefined, command.MinionId.Value);
                    WriteResolutionPair(writer, command);
                    // Milestone 9.21a, protocol /26: the body an answer names, 0 for the hero alone.
                    writer.Write(command.HeroBody);
                    return;
                case DirectorCommandKind.ChooseDefenseCard:
                    writer.Write(command.CardInstanceId.Value);
                    WriteResolutionPair(writer, command);
                    return;
                case DirectorCommandKind.DeclineDefense:
                    WriteResolutionPair(writer, command);
                    return;
                case DirectorCommandKind.AdvanceEndOfRound:
                    return;
                // Milestone 8.35g. The release carries no payload: the only turn it can release
                // is the one standing in the pause, and the host knows which that is.
                case DirectorCommandKind.ReleaseTurn:
                    return;
                case DirectorCommandKind.PerformFastTravel:
                    writer.Write(command.ActionDefinitionId.Value);
                    WriteCoordinate(writer, command.Coordinate);
                    return;
                case DirectorCommandKind.SelectMinionBattleRemoval:
                case DirectorCommandKind.DeselectMinionBattleRemoval:
                    writer.Write((int)command.TeamSide);
                    writer.Write(command.EndOfRoundResolutionId.Value);
                    writer.Write(command.EndOfRoundChoiceId.Value);
                    WriteOptionalId(writer, command.HeroId.IsDefined, command.HeroId.Value);
                    WriteOptionalId(writer, command.MinionId.IsDefined, command.MinionId.Value);
                    return;
                case DirectorCommandKind.ConfirmMinionBattleRemovals:
                    writer.Write((int)command.TeamSide);
                    writer.Write(command.EndOfRoundResolutionId.Value);
                    writer.Write(command.EndOfRoundChoiceId.Value);
                    return;
                case DirectorCommandKind.AnswerEffectChoice:
                    WriteEffectChoice(writer, command.EffectChoice);
                    return;
                case DirectorCommandKind.PerformClear:
                    writer.Write(command.ActionDefinitionId.Value);
                    writer.Write(command.TokenInstanceIds.Count);
                    foreach (var tokenId in command.TokenInstanceIds)
                    {
                        writer.Write(tokenId.Value);
                    }

                    return;
                case DirectorCommandKind.ChooseCorrectionSubject:
                    writer.Write((int)command.TeamSide);
                    writer.Write(command.ResolutionId.Value);
                    writer.Write(command.ChoiceId.Value);
                    writer.Write(command.MinionId.Value);
                    return;
                case DirectorCommandKind.ChooseCorrectionDestination:
                    writer.Write((int)command.TeamSide);
                    writer.Write(command.ResolutionId.Value);
                    writer.Write(command.ChoiceId.Value);
                    WriteOptionalId(writer, command.MinionId.IsDefined, command.MinionId.Value);
                    WriteCoordinate(writer, command.Coordinate);
                    return;
                case DirectorCommandKind.RespawnHero:
                    writer.Write(command.HeroId.Value);
                    writer.Write(command.CardInstanceId.Value);
                    WriteCoordinate(writer, command.Coordinate);
                    return;
                case DirectorCommandKind.ResolveCardWithoutRespawn:
                    writer.Write(command.HeroId.Value);
                    writer.Write(command.CardInstanceId.Value);
                    return;
                // Milestone 9.21b, protocol /27: "which of you acts" names the hero, the card and the body.
                case DirectorCommandKind.ChooseActingBody:
                    writer.Write(command.HeroId.Value);
                    writer.Write(command.CardInstanceId.Value);
                    writer.Write(command.HeroBody);
                    return;
                case DirectorCommandKind.BeginSystemTransition:
                    writer.Write(command.SystemTransitionDefinitionId.Value);
                    return;
                case DirectorCommandKind.AnswerSystemTransitionStep:
                    WriteSystemTransitionChoice(writer, command.SystemTransitionChoice);
                    return;
                case DirectorCommandKind.AnswerRemovalPrevention:
                    WriteRemovalPrevention(writer, command.RemovalPrevention);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(command), command.Kind,
                        "A command kind with no wire encoding reached the codec. Every member of " +
                        nameof(DirectorCommandKind) + " has a case here.");
            }
        }

        internal static bool TryRead(BinaryReader reader, out DirectorCommand command)
        {
            command = null;
            try
            {
                var kindValue = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(DirectorCommandKind), kindValue))
                {
                    return false;
                }

                switch ((DirectorCommandKind)kindValue)
                {
                    case DirectorCommandKind.CommitCard:
                    {
                        if (!TryReadId(reader, out var hero) || !TryReadId(reader, out var card))
                        {
                            return false;
                        }

                        command = DirectorCommand.CommitCard(new HeroId(hero), new CardInstanceId(card));
                        return true;
                    }

                    case DirectorCommandKind.Pass:
                    {
                        if (!TryReadId(reader, out var hero))
                        {
                            return false;
                        }

                        command = DirectorCommand.Pass(new HeroId(hero));
                        return true;
                    }

                    case DirectorCommandKind.ChooseTiedActor:
                    {
                        if (!TryReadTeamSide(reader, out var side) || !TryReadId(reader, out var card))
                        {
                            return false;
                        }

                        command = DirectorCommand.ChooseTiedActor(side, new CardInstanceId(card));
                        return true;
                    }

                    case DirectorCommandKind.PerformHold:
                    {
                        if (!TryReadId(reader, out var card))
                        {
                            return false;
                        }

                        command = DirectorCommand.PerformHold(new CardInstanceId(card));
                        return true;
                    }

                    case DirectorCommandKind.StartAction:
                    {
                        if (!TryReadOptionalId(reader, out var card) || !TryReadId(reader, out var action))
                        {
                            return false;
                        }

                        command = DirectorCommand.StartAction(
                            card == null ? default : new CardInstanceId(card),
                            new ActionDefinitionId(action));
                        return true;
                    }

                    case DirectorCommandKind.ChooseMovementDestination:
                    {
                        var destination = ReadCoordinate(reader);
                        if (!TryReadResolutionPair(reader, out var resolution, out var choice))
                        {
                            return false;
                        }

                        command = resolution == null
                            ? DirectorCommand.ChooseMovementDestination(destination)
                            : DirectorCommand.ChooseMovementDestination(
                                new ActionResolutionId(resolution),
                                new ActionResolutionStepId(choice),
                                destination);
                        return true;
                    }

                    case DirectorCommandKind.StopMovement:
                    {
                        if (!TryReadResolutionPair(reader, out var resolution, out var choice))
                        {
                            return false;
                        }

                        command = resolution == null
                            ? DirectorCommand.StopMovement()
                            : DirectorCommand.StopMovement(
                                new ActionResolutionId(resolution),
                                new ActionResolutionStepId(choice));
                        return true;
                    }

                    case DirectorCommandKind.ChooseAttackTarget:
                    {
                        if (!TryReadOptionalId(reader, out var hero) ||
                            !TryReadOptionalId(reader, out var minion) ||
                            !TryReadResolutionPair(reader, out var resolution, out var choice) ||
                            (hero == null) == (minion == null))
                        {
                            return false;
                        }

                        var heroBody = reader.ReadInt32();
                        if (heroBody < 0 || (heroBody > 0 && (hero == null || resolution == null)))
                        {
                            return false;
                        }

                        if (hero != null && heroBody > 0)
                        {
                            command = DirectorCommand.ChooseAttackTarget(
                                new ActionResolutionId(resolution),
                                new ActionResolutionStepId(choice),
                                new HeroId(hero),
                                heroBody);
                        }
                        else if (hero != null)
                        {
                            command = resolution == null
                                ? DirectorCommand.ChooseAttackTarget(new HeroId(hero))
                                : DirectorCommand.ChooseAttackTarget(
                                    new ActionResolutionId(resolution),
                                    new ActionResolutionStepId(choice),
                                    new HeroId(hero));
                        }
                        else
                        {
                            command = resolution == null
                                ? DirectorCommand.ChooseAttackTarget(new MinionId(minion))
                                : DirectorCommand.ChooseAttackTarget(
                                    new ActionResolutionId(resolution),
                                    new ActionResolutionStepId(choice),
                                    new MinionId(minion));
                        }

                        return true;
                    }

                    case DirectorCommandKind.ChooseDefenseCard:
                    {
                        if (!TryReadId(reader, out var card) ||
                            !TryReadResolutionPair(reader, out var resolution, out var choice))
                        {
                            return false;
                        }

                        command = resolution == null
                            ? DirectorCommand.ChooseDefenseCard(new CardInstanceId(card))
                            : DirectorCommand.ChooseDefenseCard(
                                new ActionResolutionId(resolution),
                                new ActionResolutionStepId(choice),
                                new CardInstanceId(card));
                        return true;
                    }

                    case DirectorCommandKind.DeclineDefense:
                    {
                        if (!TryReadResolutionPair(reader, out var resolution, out var choice))
                        {
                            return false;
                        }

                        command = resolution == null
                            ? DirectorCommand.DeclineDefense()
                            : DirectorCommand.DeclineDefense(
                                new ActionResolutionId(resolution),
                                new ActionResolutionStepId(choice));
                        return true;
                    }

                    case DirectorCommandKind.AdvanceEndOfRound:
                        command = DirectorCommand.AdvanceEndOfRound();
                        return true;

                    case DirectorCommandKind.ReleaseTurn:
                        command = DirectorCommand.ReleaseTurn();
                        return true;

                    case DirectorCommandKind.PerformFastTravel:
                    {
                        if (!TryReadId(reader, out var action))
                        {
                            return false;
                        }

                        command = DirectorCommand.PerformFastTravel(
                            new ActionDefinitionId(action), ReadCoordinate(reader));
                        return true;
                    }

                    case DirectorCommandKind.SelectMinionBattleRemoval:
                    case DirectorCommandKind.DeselectMinionBattleRemoval:
                    {
                        if (!TryReadTeamSide(reader, out var side) ||
                            !TryReadId(reader, out var resolution) ||
                            !TryReadId(reader, out var choice) ||
                            !TryReadOptionalId(reader, out var hero) ||
                            !TryReadOptionalId(reader, out var minion) ||
                            (hero == null) == (minion == null))
                        {
                            return false;
                        }

                        var unit = hero != null
                            ? Core.Rules.Modifications.RuleObject.ForHero(new HeroId(hero))
                            : Core.Rules.Modifications.RuleObject.ForMinion(new MinionId(minion));
                        var select = (DirectorCommandKind)kindValue == DirectorCommandKind.SelectMinionBattleRemoval;
                        var invocation = new MinionBattleChoiceCommand(
                            select ? MinionBattleChoiceCommandKind.Select : MinionBattleChoiceCommandKind.Deselect,
                            new EndOfRoundResolutionId(resolution),
                            new EndOfRoundChoiceId(choice),
                            side,
                            unit);
                        command = select
                            ? DirectorCommand.SelectMinionBattleRemoval(invocation)
                            : DirectorCommand.DeselectMinionBattleRemoval(invocation);
                        return true;
                    }

                    case DirectorCommandKind.ConfirmMinionBattleRemovals:
                    {
                        if (!TryReadTeamSide(reader, out var side) ||
                            !TryReadId(reader, out var resolution) ||
                            !TryReadId(reader, out var choice))
                        {
                            return false;
                        }

                        command = DirectorCommand.ConfirmMinionBattleRemovals(new MinionBattleChoiceCommand(
                            MinionBattleChoiceCommandKind.Confirm,
                            new EndOfRoundResolutionId(resolution),
                            new EndOfRoundChoiceId(choice),
                            side));
                        return true;
                    }

                    case DirectorCommandKind.AnswerEffectChoice:
                    {
                        if (!TryReadEffectChoice(reader, out var invocation))
                        {
                            return false;
                        }

                        command = DirectorCommand.AnswerEffectChoice(invocation);
                        return true;
                    }

                    case DirectorCommandKind.PerformClear:
                    {
                        if (!TryReadId(reader, out var action))
                        {
                            return false;
                        }

                        var count = reader.ReadInt32();
                        if (count < 0 || count > MaxTokenSelections)
                        {
                            return false;
                        }

                        var tokens = new List<TokenInstanceId>(count);
                        for (var index = 0; index < count; index++)
                        {
                            if (!TryReadId(reader, out var token))
                            {
                                return false;
                            }

                            tokens.Add(new TokenInstanceId(token));
                        }

                        command = DirectorCommand.PerformClear(new ActionDefinitionId(action), tokens);
                        return true;
                    }

                    case DirectorCommandKind.ChooseCorrectionSubject:
                    {
                        if (!TryReadTeamSide(reader, out var side) ||
                            !TryReadId(reader, out var resolution) ||
                            !TryReadId(reader, out var choice) ||
                            !TryReadId(reader, out var minion))
                        {
                            return false;
                        }

                        command = DirectorCommand.ChooseCorrectionSubject(new DisplacedMinionCorrectionCommand(
                            DisplacedMinionCorrectionCommandKind.SelectSubject,
                            new ActionResolutionId(resolution),
                            new ActionResolutionStepId(choice),
                            side,
                            new MinionId(minion),
                            default));
                        return true;
                    }

                    case DirectorCommandKind.ChooseCorrectionDestination:
                    {
                        if (!TryReadTeamSide(reader, out var side) ||
                            !TryReadId(reader, out var resolution) ||
                            !TryReadId(reader, out var choice) ||
                            !TryReadOptionalId(reader, out var minion))
                        {
                            return false;
                        }

                        command = DirectorCommand.ChooseCorrectionDestination(new DisplacedMinionCorrectionCommand(
                            DisplacedMinionCorrectionCommandKind.SelectDestination,
                            new ActionResolutionId(resolution),
                            new ActionResolutionStepId(choice),
                            side,
                            minion == null ? default : new MinionId(minion),
                            ReadCoordinate(reader)));
                        return true;
                    }

                    case DirectorCommandKind.RespawnHero:
                    {
                        if (!TryReadId(reader, out var hero) || !TryReadId(reader, out var card))
                        {
                            return false;
                        }

                        command = DirectorCommand.RespawnHero(new HeroRespawnCommand(
                            HeroRespawnCommandKind.Respawn,
                            new HeroId(hero),
                            new CardInstanceId(card),
                            ReadCoordinate(reader)));
                        return true;
                    }

                    case DirectorCommandKind.ResolveCardWithoutRespawn:
                    {
                        if (!TryReadId(reader, out var hero) || !TryReadId(reader, out var card))
                        {
                            return false;
                        }

                        command = DirectorCommand.ResolveCardWithoutRespawn(new HeroRespawnCommand(
                            HeroRespawnCommandKind.ResolveWithoutAction,
                            new HeroId(hero),
                            new CardInstanceId(card)));
                        return true;
                    }

                    case DirectorCommandKind.ChooseActingBody:
                    {
                        if (!TryReadId(reader, out var hero) || !TryReadId(reader, out var card))
                        {
                            return false;
                        }

                        command = DirectorCommand.ChooseActingBody(new ActingBodyCommand(
                            new HeroId(hero),
                            new CardInstanceId(card),
                            reader.ReadInt32()));
                        return true;
                    }

                    case DirectorCommandKind.BeginSystemTransition:
                    {
                        if (!TryReadId(reader, out var definition))
                        {
                            return false;
                        }

                        command = DirectorCommand.BeginSystemTransition(
                            new SystemTransitionDefinitionId(definition));
                        return true;
                    }

                    case DirectorCommandKind.AnswerSystemTransitionStep:
                    {
                        if (!TryReadSystemTransitionChoice(reader, out var invocation))
                        {
                            return false;
                        }

                        command = DirectorCommand.AnswerSystemTransitionStep(invocation);
                        return true;
                    }

                    case DirectorCommandKind.AnswerRemovalPrevention:
                    {
                        if (!TryReadRemovalPrevention(reader, out var invocation))
                        {
                            return false;
                        }

                        command = DirectorCommand.AnswerRemovalPrevention(invocation);
                        return true;
                    }

                    default:
                        return false;
                }
            }
            catch (ArgumentException)
            {
                // A Core factory guard refused the field combination. The guard is the validation;
                // wire input that fails it is malformed, not exceptional.
                command = null;
                return false;
            }
        }

        private static void WriteEffectChoice(BinaryWriter writer, EffectChoiceCommand invocation)
        {
            writer.Write(invocation.ResolutionId.Value);
            writer.Write(invocation.ChoiceId.Value);
            writer.Write((int)invocation.Kind);
            writer.Write((int)invocation.Candidate.Kind);
            switch (invocation.Candidate.Kind)
            {
                case EffectChoiceCandidateKind.Option:
                    writer.Write(invocation.Candidate.OptionNodeId.Value);
                    break;
                case EffectChoiceCandidateKind.Value:
                    WriteEffectValue(writer, invocation.Candidate.Value);
                    break;
            }

            WriteOptionalId(writer, invocation.AnsweringHeroId.IsDefined, invocation.AnsweringHeroId.Value);
        }

        private static bool TryReadEffectChoice(BinaryReader reader, out EffectChoiceCommand invocation)
        {
            invocation = null;
            if (!TryReadId(reader, out var resolution) || !TryReadId(reader, out var choice))
            {
                return false;
            }

            var kindValue = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(EffectChoiceCommandKind), kindValue) ||
                (EffectChoiceCommandKind)kindValue == EffectChoiceCommandKind.None)
            {
                return false;
            }

            var candidateKindValue = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(EffectChoiceCandidateKind), candidateKindValue))
            {
                return false;
            }

            var candidate = default(EffectChoiceCandidate);
            switch ((EffectChoiceCandidateKind)candidateKindValue)
            {
                case EffectChoiceCandidateKind.Option:
                {
                    if (!TryReadId(reader, out var node))
                    {
                        return false;
                    }

                    candidate = EffectChoiceCandidate.Option(new EffectNodeId(node));
                    break;
                }

                case EffectChoiceCandidateKind.Value:
                {
                    if (!TryReadEffectValue(reader, out var value))
                    {
                        return false;
                    }

                    candidate = EffectChoiceCandidate.Value_(value);
                    break;
                }
            }

            if (!TryReadOptionalId(reader, out var answeringHero))
            {
                return false;
            }

            invocation = new EffectChoiceCommand(
                new EffectResolutionId(resolution),
                new EffectChoiceId(choice),
                (EffectChoiceCommandKind)kindValue,
                candidate,
                answeringHero == null ? default : new HeroId(answeringHero));
            return true;
        }

        private static void WriteEffectValue(BinaryWriter writer, EffectValue value)
        {
            writer.Write((int)value.Kind);
            switch (value.Kind)
            {
                case EffectValueKind.Boolean:
                    writer.Write(value.BooleanValue);
                    return;
                case EffectValueKind.Integer:
                    writer.Write(value.IntegerValue);
                    return;
                case EffectValueKind.Hero:
                    writer.Write(value.HeroId.Value);
                    return;
                case EffectValueKind.Minion:
                    writer.Write(value.MinionId.Value);
                    return;
                case EffectValueKind.CardInstance:
                    writer.Write(value.CardInstanceId.Value);
                    return;
                case EffectValueKind.Coordinate:
                    WriteCoordinate(writer, value.Coordinate);
                    return;
                case EffectValueKind.Node:
                    writer.Write(value.NodeId.Value);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(value), value.Kind, "An effect value kind with no wire encoding.");
            }
        }

        private static bool TryReadEffectValue(BinaryReader reader, out EffectValue value)
        {
            value = default;
            var kindValue = reader.ReadInt32();
            switch (kindValue)
            {
                case (int)EffectValueKind.Boolean:
                    value = EffectValue.Boolean(reader.ReadBoolean());
                    return true;
                case (int)EffectValueKind.Integer:
                    value = EffectValue.Integer(reader.ReadInt32());
                    return true;
                case (int)EffectValueKind.Hero:
                {
                    if (!TryReadId(reader, out var hero))
                    {
                        return false;
                    }

                    value = EffectValue.Hero(new HeroId(hero));
                    return true;
                }

                case (int)EffectValueKind.Minion:
                {
                    if (!TryReadId(reader, out var minion))
                    {
                        return false;
                    }

                    value = EffectValue.Minion(new MinionId(minion));
                    return true;
                }

                case (int)EffectValueKind.CardInstance:
                {
                    if (!TryReadId(reader, out var card))
                    {
                        return false;
                    }

                    value = EffectValue.Card(new CardInstanceId(card));
                    return true;
                }

                case (int)EffectValueKind.Coordinate:
                    value = EffectValue.Space(ReadCoordinate(reader));
                    return true;
                case (int)EffectValueKind.Node:
                {
                    if (!TryReadId(reader, out var node))
                    {
                        return false;
                    }

                    value = EffectValue.Node(new EffectNodeId(node));
                    return true;
                }

                default:
                    return false;
            }
        }

        private static void WriteSystemTransitionChoice(
            BinaryWriter writer, SystemTransitionChoiceCommand invocation)
        {
            writer.Write(invocation.InstanceId.Value);
            writer.Write(invocation.StepId.Value);
            WriteOptionalId(writer, invocation.ItemId.IsDefined, invocation.ItemId.Value);
            writer.Write((int)invocation.ChoosingSide);
            writer.Write(invocation.OptionId.Value);
        }

        private static bool TryReadSystemTransitionChoice(
            BinaryReader reader, out SystemTransitionChoiceCommand invocation)
        {
            invocation = default;
            if (!TryReadId(reader, out var instance) ||
                !TryReadId(reader, out var step) ||
                !TryReadOptionalId(reader, out var item))
            {
                return false;
            }

            if (!TryReadTeamSide(reader, out var side) || !TryReadId(reader, out var option))
            {
                return false;
            }

            invocation = new SystemTransitionChoiceCommand(
                new SystemTransitionInstanceId(instance),
                new SystemTransitionStepId(step),
                item == null ? default : new SystemTransitionItemId(item),
                side,
                new SystemTransitionOptionId(option));
            return true;
        }

        private static void WriteRemovalPrevention(BinaryWriter writer, RemovalPreventionCommand invocation)
        {
            writer.Write(invocation.RequestId);
            writer.Write(invocation.DecidingHeroId.Value);
            writer.Write(invocation.Accepted);
            if (invocation.Accepted)
            {
                writer.Write(invocation.SourceId);
                writer.Write(invocation.PaymentCardInstanceId.Value);
            }
        }

        private static bool TryReadRemovalPrevention(
            BinaryReader reader, out RemovalPreventionCommand invocation)
        {
            invocation = null;
            if (!TryReadId(reader, out var request) || !TryReadId(reader, out var hero))
            {
                return false;
            }

            if (!reader.ReadBoolean())
            {
                invocation = RemovalPreventionCommand.Decline(request, new HeroId(hero));
                return true;
            }

            if (!TryReadId(reader, out var source) || !TryReadId(reader, out var payment))
            {
                return false;
            }

            invocation = RemovalPreventionCommand.Accept(
                request, new HeroId(hero), source, new CardInstanceId(payment));
            return true;
        }

        /// <summary>
        /// A resolution/choice pair travels together: both named, or - for the pre-envelope
        /// command shapes - both absent. A mixed pair is refused.
        /// </summary>
        private static void WriteResolutionPair(BinaryWriter writer, DirectorCommand command)
        {
            WriteOptionalId(writer, command.ResolutionId.IsDefined, command.ResolutionId.Value);
            WriteOptionalId(writer, command.ChoiceId.IsDefined, command.ChoiceId.Value);
        }

        private static bool TryReadResolutionPair(
            BinaryReader reader, out string resolution, out string choice)
        {
            choice = null;
            if (!TryReadOptionalId(reader, out resolution) || !TryReadOptionalId(reader, out choice))
            {
                return false;
            }

            return (resolution == null) == (choice == null);
        }

        private static void WriteCoordinate(BinaryWriter writer, HexCoordinate coordinate)
        {
            writer.Write(coordinate.Q);
            writer.Write(coordinate.R);
        }

        private static HexCoordinate ReadCoordinate(BinaryReader reader)
        {
            var q = reader.ReadInt32();
            var r = reader.ReadInt32();
            return new HexCoordinate(q, r);
        }

        private static void WriteOptionalId(BinaryWriter writer, bool isDefined, string value)
        {
            writer.Write(isDefined);
            if (isDefined)
            {
                writer.Write(value);
            }
        }

        private static bool TryReadOptionalId(BinaryReader reader, out string value)
        {
            value = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            value = reader.ReadString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryReadId(BinaryReader reader, out string value)
        {
            value = reader.ReadString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryReadTeamSide(BinaryReader reader, out TeamSide side)
        {
            var value = reader.ReadInt32();
            switch (value)
            {
                case (int)TeamSide.Red:
                    side = TeamSide.Red;
                    return true;
                case (int)TeamSide.Blue:
                    side = TeamSide.Blue;
                    return true;
                default:
                    side = TeamSide.None;
                    return false;
            }
        }
    }
}
