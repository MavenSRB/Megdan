using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public static partial class AuthoritativeSnapshotSerializer
    {
        private static void WriteCardAction(BinaryWriter writer, CardActionResolutionRestoreState state)
        {
            if (state == null)
            {
                writer.Write(NoCardActionAuthorityKind);
                WriteDefeatRewards(writer, DefeatRewardRestoreStateHolder.Absent());
                return;
            }

            var (continuationKind, pendingMovement, pendingAttack, pendingSkill, defeatRewards) = state;
            writer.Write((int)continuationKind);
            WriteDefeatRewards(writer, defeatRewards);
            if (continuationKind == CardActionContinuationKind.Movement)
            {
                WriteMovementResolution(writer, pendingMovement);
            }

            if (continuationKind == CardActionContinuationKind.Attack)
            {
                WriteAttackResolution(writer, pendingAttack);
            }

            if (continuationKind == CardActionContinuationKind.Skill)
            {
                WriteSkillResolution(writer, pendingSkill);
            }
        }

        private static CardActionResolutionAuthority ReadCardAction(
            BinaryReader reader,
            AdjacentMoveAuthority board,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            EffectCapabilityCatalog capabilities,
            MarkerAttachmentRegister markers)
        {
            var kind = reader.ReadInt32();
            var defeatRewards = ReadDefeatRewards(reader);
            if (kind == NoCardActionAuthorityKind) return null;

            CardActionResolutionRestoreState state;
            switch ((CardActionContinuationKind)kind)
            {
                case CardActionContinuationKind.None:
                    state = CardActionResolutionRestoreState.WithoutPendingAction(defeatRewards);
                    break;
                case CardActionContinuationKind.Movement:
                    state = CardActionResolutionRestoreState.WithPendingMovement(
                        ReadMovementResolution(reader), defeatRewards);
                    break;
                case CardActionContinuationKind.Attack:
                    state = CardActionResolutionRestoreState.WithPendingAttack(
                        ReadAttackResolution(reader), defeatRewards);
                    break;
                case CardActionContinuationKind.Skill:
                    state = CardActionResolutionRestoreState.WithPendingSkill(
                        ReadSkillResolution(
                            reader, definitions, capabilities, participants,
                            CardActionResolutionAuthority.CreateRestoredComponents(board, markers)),
                        defeatRewards);
                    break;
                default:
                    throw Malformed("Unknown pending card-action kind.");
            }

            // The snapshot never stores the pending choices; they are derived during
            // restore, so no verified choice is supplied here.
            return CardActionResolutionAuthority.Restore(
                board, state, null, null, null, definitions, cards, participants, markers);
        }

        /// <summary>
        /// Milestone 6.2a. A pending Skill writes identity, position, and results - never the semantics
        /// of what it is doing.
        ///
        /// The effect definition appears only as an ID plus content revision, and the pending choice's
        /// candidate list is deliberately absent: restore re-derives candidates by asking the trusted
        /// handler and then checks the payload's selection against them. A tampered candidate is
        /// therefore not merely rejected, it is unrepresentable. This is the pattern
        /// <see cref="ReadCardAction"/> already used for movement and attack choices.
        /// </summary>
        private static void WriteSkillResolution(BinaryWriter writer, PendingResolution pending)
        {
            WriteCommonResolution(writer, new ActionResolutionId(pending.ResolutionId.Value),
                pending.SourceCardInstanceId, pending.SourceDefinitionReference,
                pending.ActionDefinitionId, pending.ActorId, pending.InvocationContext,
                new ActionResolutionStepId("skill"));
            writer.Write(pending.ControllerId.Value);
            writer.Write(pending.EffectDefinitionReference.DefinitionId.Value);
            writer.Write(pending.EffectDefinitionReference.ContentRevision);

            writer.Write(pending.Continuation.Count);
            foreach (var frame in pending.Continuation)
            {
                writer.Write(frame.NodeId.Value);
                writer.Write(frame.ChildIndex);
            }

            writer.Write(pending.CompletedOutcomes.Count);
            foreach (var outcome in pending.CompletedOutcomes)
            {
                writer.Write(outcome.NodeId.Value);
                writer.Write((int)outcome.ResultKind);
                writer.Write(outcome.Outputs.Count);
                foreach (var output in outcome.Outputs)
                {
                    writer.Write(output.OutputName);
                    WriteEffectValue(writer, output.Value);
                }
            }

            writer.Write(pending.AcceptedChoices.Count);
            foreach (var record in pending.AcceptedChoices)
            {
                writer.Write(record.NodeId.Value);
                writer.Write(record.ChoiceId.Value);
                writer.Write((int)record.ResolvedBy);
                writer.Write(record.Selected.Count);
                foreach (var candidate in record.Selected) WriteEffectCandidate(writer, candidate);
            }

            writer.Write(pending.PendingChoice != null);
            if (pending.PendingChoice == null) return;

            var choice = pending.PendingChoice;
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.NodeId.Value);
            writer.Write(choice.ChooserId.Value);
            writer.Write((int)choice.Kind);
            writer.Write(choice.MinimumSelections);
            writer.Write(choice.MaximumSelections);
            writer.Write(choice.SelectedCandidates.Count);
            foreach (var candidate in choice.SelectedCandidates) WriteEffectCandidate(writer, candidate);
        }

        /// <summary>
        /// Rebinds a pending Skill to the trusted match definitions before any of its state is
        /// accepted. The payload names an effect definition; the semantics come from the configured
        /// card, and a payload that names a definition or node this build does not have is refused
        /// rather than reconstructed.
        /// </summary>
        private static PendingResolution ReadSkillResolution(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            EffectCapabilityCatalog capabilities,
            MatchParticipants participants,
            MatchComponents components)
        {
            var common = ReadCommonResolution(reader);
            var controllerId = new HeroId(reader.ReadString());
            var effectDefinitionId = new EffectDefinitionId(reader.ReadString());
            var effectContentRevision = reader.ReadString();
            var effectReference = new EffectDefinitionReference(effectDefinitionId, effectContentRevision);

            var trusted = FindTrustedEffectRoot(definitions, common, effectReference);
            if (trusted == null)
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.EffectDefinitionMissing,
                    $"Snapshot names effect definition '{effectReference}', which the configured match definitions do not provide for action '{common.ActionId}'.");
            }

            var continuation = ReadMany(reader, "effect continuation frames", () =>
            {
                var nodeId = new EffectNodeId(reader.ReadString());
                var childIndex = reader.ReadInt32();
                if (!trusted.TryGetNode(nodeId, out _))
                {
                    throw Malformed($"Snapshot effect continuation names unknown node '{nodeId}'.");
                }

                return new EffectContinuationFrame(nodeId, childIndex);
            });

            var outcomes = ReadMany(reader, "effect node outcomes", () =>
            {
                var nodeId = new EffectNodeId(reader.ReadString());
                var resultKind = SnapshotEnumReader.Read<EffectExecutionResultKind>(
                    reader, "effect node result kind");
                if (!trusted.TryGetNode(nodeId, out var node))
                {
                    throw Malformed($"Snapshot effect outcome names unknown node '{nodeId}'.");
                }

                var outputs = ReadMany(reader, "effect node outputs", () =>
                {
                    var outputName = reader.ReadString();
                    var value = ReadEffectValue(reader);
                    var declared = node.Outputs.FirstOrDefault(candidate =>
                        string.Equals(candidate.OutputName, outputName, StringComparison.Ordinal));
                    if (string.IsNullOrEmpty(declared.OutputName) || declared.ValueKind != value.Kind)
                    {
                        throw Malformed(
                            $"Snapshot effect outcome declares output '{outputName}' that node '{nodeId}' does not publish at that type.");
                    }

                    return new EffectOutcomeFact(outputName, value);
                });

                return new EffectNodeOutcome(nodeId, resultKind, outputs);
            });

            var accepted = ReadMany(reader, "accepted effect choices", () =>
            {
                var nodeId = new EffectNodeId(reader.ReadString());
                var choiceId = new EffectChoiceId(reader.ReadString());
                var resolvedBy = SnapshotEnumReader.Read<EffectChoiceCommandKind>(
                    reader, "accepted effect choice resolution");
                var selected = ReadMany(reader, "accepted effect choice selection", () => ReadEffectCandidate(reader));
                return new EffectChoiceRecord(nodeId, choiceId, resolvedBy, selected);
            });

            PendingEffectChoice pendingChoice = null;
            if (reader.ReadBoolean())
            {
                var choiceId = new EffectChoiceId(reader.ReadString());
                var nodeId = new EffectNodeId(reader.ReadString());
                var chooserId = new HeroId(reader.ReadString());
                var choiceKind = SnapshotEnumReader.Read<EffectChoiceKind>(reader, "pending effect choice kind");
                var minimum = reader.ReadInt32();
                var maximum = reader.ReadInt32();
                var selected = ReadMany(reader, "pending effect choice selection", () => ReadEffectCandidate(reader));
                if (!trusted.TryGetNode(nodeId, out var node))
                {
                    throw Malformed($"Snapshot pending effect choice names unknown node '{nodeId}'.");
                }

                // Candidates are never written. A control node's options come from the trusted
                // definition; a capability's come from asking the trusted handler again. Either way a
                // payload can only ever narrow an authoritative candidate set, never invent one.
                // A confirm-or-decline choice offers nothing to pick between, so there is no candidate
                // set to re-derive and no capability to ask - the control node itself opened it.
                var candidates =
                    choiceKind == EffectChoiceKind.ConfirmOrDecline
                        ? Array.Empty<EffectChoiceCandidate>()
                        : choiceKind == EffectChoiceKind.SelectOption
                            ? node.ChildNodeIds.Select(EffectChoiceCandidate.Option).ToArray()
                            : DeriveCapabilityCandidates(
                                node, common, controllerId, capabilities, participants, components);
                if (selected.Any(candidate => !candidates.Contains(candidate)))
                {
                    throw Malformed(
                        $"Snapshot pending effect choice selects a candidate node '{nodeId}' does not offer.");
                }

                pendingChoice = new PendingEffectChoice(
                    new EffectResolutionId(common.ResolutionId.Value), choiceId, nodeId, chooserId,
                    choiceKind, candidates, minimum, maximum, selected);
            }

            return new PendingResolution(
                new EffectResolutionId(common.ResolutionId.Value),
                common.CardId,
                common.DefinitionReference,
                common.ActionId,
                trusted.Reference,
                common.ActorId,
                controllerId,
                common.InvocationContext,
                continuation,
                outcomes,
                accepted,
                pendingChoice);
        }

        /// <summary>
        /// Re-asks the registered handler what it offered. Handlers are pure and stateless, so replaying
        /// the request reproduces the authoritative candidate set without the payload describing it.
        /// </summary>
        /// <remarks>
        /// Milestone 6.2 made the state this request runs against load-bearing. A capability whose
        /// candidates come from the board - which empty spaces may take a token, which same-type tokens
        /// may be taken back - must be re-asked against the state this same payload just restored, not
        /// against an empty context. The board and the marker register are both already read by the time
        /// a pending Skill is decoded, which is what makes that possible; before this milestone every
        /// capability derived its candidates from identities alone and the difference could not show.
        /// </remarks>
        private static EffectChoiceCandidate[] DeriveCapabilityCandidates(
            EffectNodeDefinition node,
            CommonResolution common,
            HeroId controllerId,
            EffectCapabilityCatalog capabilities,
            MatchParticipants participants,
            MatchComponents components)
        {
            var handler = capabilities.Load(node.Capability);
            var offered = handler.Execute(new EffectCapabilityContext(
                node.Id,
                node.FailurePolicy,
                common.ActorId,
                controllerId,
                common.CardId,
                common.ActionId,
                null,
                1,
                1,
                participants,
                null,
                components));
            if (offered.Choice == null)
            {
                throw Malformed(
                    $"Snapshot pending effect choice names node '{node.Id}', whose capability no longer requests a choice.");
            }

            return offered.Choice.Candidates.Select(EffectChoiceCandidate.Value_).ToArray();
        }

        private static EffectDefinition FindTrustedEffectRoot(
            IReadOnlyList<CardDefinition> definitions,
            CommonResolution common,
            EffectDefinitionReference reference)
        {
            foreach (var definition in definitions)
            {
                if (!definition.Reference.Equals(common.DefinitionReference)) continue;
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.Id.Equals(common.ActionId) &&
                        action.EffectRoot != null &&
                        action.EffectRoot.Reference.Equals(reference))
                    {
                        return action.EffectRoot;
                    }
                }
            }

            return null;
        }

        private static EffectChoiceCandidate ReadEffectCandidate(BinaryReader reader)
        {
            var kind = SnapshotEnumReader.Read<EffectChoiceCandidateKind>(reader, "effect choice candidate kind");
            return kind == EffectChoiceCandidateKind.Option
                ? EffectChoiceCandidate.Option(new EffectNodeId(reader.ReadString()))
                : EffectChoiceCandidate.Value_(ReadEffectValue(reader));
        }

        private static EffectValue ReadEffectValue(BinaryReader reader)
        {
            var kind = SnapshotEnumReader.Read<EffectValueKind>(reader, "effect value kind");
            switch (kind)
            {
                case EffectValueKind.Boolean:
                    return EffectValue.Boolean(reader.ReadBoolean());
                case EffectValueKind.Integer:
                    return EffectValue.Integer(reader.ReadInt32());
                case EffectValueKind.Hero:
                    return EffectValue.Hero(new HeroId(reader.ReadString()));
                case EffectValueKind.Minion:
                    return EffectValue.Minion(new MinionId(reader.ReadString()));
                case EffectValueKind.CardInstance:
                    return EffectValue.Card(new CardInstanceId(reader.ReadString()));
                case EffectValueKind.Coordinate:
                    return EffectValue.Space(ReadCoordinate(reader));
                default:
                    throw Malformed("Snapshot effect value has no defined kind.");
            }
        }

        private static void WriteEffectCandidate(BinaryWriter writer, EffectChoiceCandidate candidate)
        {
            writer.Write((int)candidate.Kind);
            if (candidate.Kind == EffectChoiceCandidateKind.Option)
            {
                writer.Write(candidate.OptionNodeId.Value);
                return;
            }

            WriteEffectValue(writer, candidate.Value);
        }

        private static void WriteEffectValue(BinaryWriter writer, EffectValue value)
        {
            writer.Write((int)value.Kind);
            switch (value.Kind)
            {
                case EffectValueKind.Boolean:
                    writer.Write(value.BooleanValue);
                    break;
                case EffectValueKind.Integer:
                    writer.Write(value.IntegerValue);
                    break;
                case EffectValueKind.Hero:
                    writer.Write(value.HeroId.Value);
                    break;
                case EffectValueKind.Minion:
                    writer.Write(value.MinionId.Value);
                    break;
                case EffectValueKind.CardInstance:
                    writer.Write(value.CardInstanceId.Value);
                    break;
                case EffectValueKind.Coordinate:
                    WriteCoordinate(writer, value.Coordinate);
                    break;
            }
        }

        private static void WriteMovementResolution(BinaryWriter writer, PendingCardActionResolution pending)
        {
            WriteCommonResolution(writer, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.SourceDefinitionReference, pending.ActionDefinitionId, pending.ActorId,
                pending.InvocationContext, pending.CurrentStepId);
            var state = pending.MovementSessionState;
            writer.Write(state.MovementAllowance);
            writer.Write(state.RemainingAllowance);
            writer.Write(state.IsComplete);
            WriteCoordinates(writer, state.Route);
        }

        private static PendingCardActionResolution ReadMovementResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var allowance = reader.ReadInt32();
            var remaining = reader.ReadInt32();
            var complete = reader.ReadBoolean();
            var route = ReadCoordinates(reader, "movement route");
            var events = new List<AdjacentMoveEvent>();
            for (var index = 0; index < route.Length - 1; index++)
                events.Add(new AdjacentMoveEvent(common.ActorId, route[index], route[index + 1]));
            return new PendingCardActionResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId,
                new MovementSessionState(common.ActorId, allowance, remaining, complete, route, events));
        }

        private static void WriteAttackResolution(BinaryWriter writer, PendingAttackResolution pending)
        {
            WriteCommonResolution(writer, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.SourceDefinitionReference, pending.ActionDefinitionId, pending.ActorId,
                pending.InvocationContext, pending.CurrentStepId);
            writer.Write((int)pending.Phase);
            writer.Write((int)pending.Target.Kind);
            if (pending.Target.Kind == AttackTargetKind.Hero) writer.Write(pending.TargetHeroId.Value);
            if (pending.Target.Kind == AttackTargetKind.Minion) writer.Write(pending.TargetMinionId.Value);
            WritePrintedValue(writer, pending.AttackValue);
            writer.Write(pending.DefenseOffer.HasValue);
            if (pending.DefenseOffer.HasValue) WriteActionOffer(writer, pending.DefenseOffer.Value);
            WritePrintedValue(writer, pending.DefenseValue);
        }

        private static PendingAttackResolution ReadAttackResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var phase = SnapshotEnumReader.Read<AttackResolutionPhase>(reader, "attack resolution phase");
            var targetKind = SnapshotEnumReader.Read<AttackTargetKind>(reader, "attack target kind");
            // A switch mirroring WriteAttackResolution rather than a ternary chain. The chain's final
            // `default` silently produced an empty target and, because it also skipped the ReadString
            // that a Hero or Minion target consumes, left every field after it misaligned - surfacing,
            // if at all, as the unrelated trailing-data error. Read<T> already rejects an undefined
            // value; this makes a future member added to AttackTargetKind fail loudly here too,
            // matching the pending card-action kind switch above.
            AttackTarget target;
            switch (targetKind)
            {
                case AttackTargetKind.Hero:
                    target = AttackTarget.Hero(new HeroId(reader.ReadString()));
                    break;
                case AttackTargetKind.Minion:
                    target = AttackTarget.Minion(new MinionId(reader.ReadString()));
                    break;
                case AttackTargetKind.None:
                    target = default;
                    break;
                default:
                    throw Malformed($"Unhandled attack target kind '{targetKind}'.");
            }
            var attackValue = ReadPrintedValue(reader);
            var defenseOffer = reader.ReadBoolean() ? (ActionOffer?)ReadActionOffer(reader) : null;
            var defenseValue = ReadPrintedValue(reader);
            return new PendingAttackResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId, phase, target,
                attackValue, defenseOffer, defenseValue);
        }

        private static void WriteDefeatRewards(BinaryWriter writer, DefeatRewardRestoreStateHolder holder)
        {
            var (isPresent, rewards) = holder;
            writer.Write(isPresent);
            if (!isPresent) return;
            var (coinBalances, awardedOutcomeIds) = rewards;
            writer.Write(coinBalances.Count);
            foreach (var balance in coinBalances)
            {
                writer.Write(balance.Key.Value);
                writer.Write(balance.Value);
            }
            writer.Write(awardedOutcomeIds.Count);
            foreach (var outcomeId in awardedOutcomeIds) writer.Write(outcomeId.Value);
        }

        private static DefeatRewardRestoreStateHolder ReadDefeatRewards(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return DefeatRewardRestoreStateHolder.Absent();
            var balances = ReadMany(reader, "hero coin balances", () =>
                new KeyValuePair<HeroId, int>(new HeroId(reader.ReadString()), reader.ReadInt32()));
            var outcomes = ReadMany(reader, "awarded Defeat outcomes", () =>
                new DefeatOutcomeId(reader.ReadString()));
            return DefeatRewardRestoreStateHolder.Present(
                new DefeatRewardRestoreState(balances, outcomes));
        }

    }
}
