using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Effects
{
    /// <summary>Why an effect definition was refused at content load.</summary>
    public enum EffectDefinitionRejectionReason
    {
        None = 0,
        UnresolvedChildNode = 1,
        UnreachableNode = 2,
        CyclicNodeGraph = 3,
        UnregisteredCapability = 4,
        UndeclaredParameter = 5,
        MissingRequiredParameter = 6,
        IncompatibleBindingType = 7,
        UnresolvedNodeOutput = 8,
        NodeOutputNotGuaranteed = 9,
        PredicateMustReturnBoolean = 10,
        RoleCannotSupplyValueKind = 11,

        /// <summary>
        /// Milestone 6.8l. A selection names a consumer this definition does not declare. One
        /// definition is one action's node table, so this is also how "a node in another action" is
        /// refused: there is no address that reaches out of it.
        /// </summary>
        UnresolvedSelectionConsumer = 12,

        /// <summary>
        /// Milestone 6.8l. A selection names a node that reads nothing it publishes, so the two are
        /// unrelated and filtering the candidates by that node's eligibility would be a guess.
        /// </summary>
        SelectionConsumerReadsNothing = 13,

        /// <summary>
        /// Milestone 8.3, closing the 7.9a review's F-CARD-06. A node is named as a child by more than
        /// one node, or the root is named as a child at all. The node table is a tree; only the acyclic
        /// part of that was ever checked, so a directed acyclic graph loaded cleanly into machinery
        /// that assumes each node has one parent.
        /// </summary>
        NodeReachableThroughTwoParents = 14,

        /// <summary>
        /// Milestone 8.5. A printed Attack's or Defense's card text contains a node that would open an
        /// authoritative choice. Rulebook p.14 applies that text inside an Attack whose remaining steps
        /// are produced in the same command that began them, and this build persists no state that
        /// could hold them while a question stands - so the composition is refused at content load
        /// rather than faulting halfway through an attack that has already been announced. A Skill's
        /// effect is the action itself and pauses freely; only text applied inside another action is
        /// bound by this. The slice that first transcribes a rider needing a question owns the nesting
        /// and the schema move it costs.
        /// </summary>
        CardTextCannotPauseInsideAnAttack = 15,

        /// <summary>
        /// Milestone 8.6. A `on the same target` repeat names a selection whose target it could not
        /// inherit: one this definition does not declare, one that publishes no single space, one that
        /// has not certainly run by the time the repeat is reached, or a second such repeat in the same
        /// definition, which would leave one inherited target with two nodes claiming it.
        /// </summary>
        UnresolvedRepeatTargetBinding = 16
    }

    public readonly struct EffectDefinitionRejection
    {
        public EffectDefinitionRejection(EffectDefinitionRejectionReason reason, string detail)
        {
            Reason = reason;
            Detail = detail;
        }

        public EffectDefinitionRejectionReason Reason { get; }
        public string Detail { get; }

        public override string ToString() => $"{Reason}: {Detail}";
    }

    /// <summary>
    /// Rejects an effect definition before a match starts rather than during one.
    ///
    /// Per-node shape is already enforced by <see cref="EffectNodeDefinition"/>'s own constructor. What
    /// needs the whole table lives here: that children resolve, that the graph is acyclic and fully
    /// reachable from the root, that every capability is registered at the exact revision named, that
    /// every input matches its handler's declared parameter type, and - the one non-obvious rule - that
    /// a node reading an earlier node's output is only reachable through paths where that node has
    /// certainly already run.
    ///
    /// That last rule is what makes typed result binding safe without a general expression language: a
    /// node cannot read "the target that was chosen" from a branch that may not have executed.
    /// </summary>
    public static class EffectDefinitionValidator
    {
        /// <summary>
        /// Everything <see cref="Validate"/> checks, plus the one extra rule text applied inside
        /// another action is bound by at the one of rulebook p.14's moments that cannot hold a
        /// question.
        ///
        /// <para>Milestone 8.5 refused a pausing shape at all three of the page's moments, because an
        /// attack's remaining steps were produced in the command that began them and nothing could hold
        /// them while a question stood. Milestone 8.10 supplied the holding for two of the three: what
        /// p.14 leaves after step 4 and after step 7 is a function of the attack's own identity, so a
        /// paused frame plus that identity is enough to finish the page, and Wasp's riders - which
        /// choose an enemy hero and then let that hero choose a card - resolve through it.</para>
        ///
        /// <para><b>Step 1 still cannot.</b> What follows the attacker's "Before the attack" text is
        /// not a function of identity: on the arm where p.13 simply defeats a targeted minion, it
        /// contains the facts of a Defeat the resolver has already computed and applied, and no
        /// persisted identity reproduces those. A composition that could ask a question there is
        /// therefore still refused here, at content load, rather than faulting halfway through an
        /// attack that has already been announced. A Skill is unaffected at every moment: its effect is
        /// the action, and pausing it is what the whole frame exists for.</para>
        ///
        /// <para>The rule is about the three node shapes that open a choice on their own: p.10's
        /// <c>Choose</c>, anything policied <c>May</c> (which opens the page's confirm-or-decline), and
        /// <c>RepeatAction</c>, whose eligible <c>may</c> does the same. A capability that requests one
        /// is caught at execution instead, because nothing a definition declares says whether a handler
        /// will ask.</para>
        /// </summary>
        public static IReadOnlyList<EffectDefinitionRejection> ValidateCardText(
            EffectDefinition definition,
            EffectCapabilityCatalog catalog,
            CardTextTiming moment)
        {
            var rejections = Validate(definition, catalog).ToList();
            if (definition == null || moment != CardTextTiming.BeforeTheAttack)
            {
                return Array.AsReadOnly(rejections.ToArray());
            }

            foreach (var node in definition.Nodes)
            {
                var pausingShape =
                    node.Kind == EffectNodeKind.Choose ? "rulebook p.10's Choose" :
                    node.Kind == EffectNodeKind.RepeatAction ? "a repeat the actor may decline" :
                    node.FailurePolicy == EffectFailurePolicy.May ? "a `may` step" :
                    null;
                if (pausingShape == null)
                {
                    continue;
                }

                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.CardTextCannotPauseInsideAnAttack,
                    $"Node '{node.Id}' is {pausingShape}, which asks the player a question. Card text " +
                    "applied at rulebook p.14's step 1 has to finish inside the command that reached " +
                    "it, because what the page does next is not a function of the attack's identity."));
            }

            return Array.AsReadOnly(rejections.ToArray());
        }

        public static IReadOnlyList<EffectDefinitionRejection> Validate(
            EffectDefinition definition,
            EffectCapabilityCatalog catalog)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var rejections = new List<EffectDefinitionRejection>();

            foreach (var node in definition.Nodes)
            {
                foreach (var childId in node.ChildNodeIds)
                {
                    if (!definition.TryGetNode(childId, out _))
                    {
                        rejections.Add(new EffectDefinitionRejection(
                            EffectDefinitionRejectionReason.UnresolvedChildNode,
                            $"Node '{node.Id}' names child '{childId}', which this definition does not declare."));
                    }
                }
            }

            if (rejections.Count > 0)
            {
                // Reachability and dominance both walk children; an unresolved child would make them lie.
                return rejections;
            }

            var reachable = Reachable(definition);
            foreach (var node in definition.Nodes.Where(candidate => !reachable.Contains(candidate.Id)))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnreachableNode,
                    $"Node '{node.Id}' is not reachable from root '{definition.RootNodeId}'."));
            }

            if (HasCycle(definition))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.CyclicNodeGraph,
                    $"Effect definition '{definition.Id}' contains a node cycle; repetition is milestone 6.7's Repeat, not a loop in the node table."));
                return rejections;
            }

            var beforeParentRules = rejections.Count;
            ValidateSingleParent(definition, rejections);
            if (rejections.Count > beforeParentRules)
            {
                // The dominance analysis below is only meaningful over a tree, so reporting its
                // conclusions about a graph that is not one would be reporting guesses.
                return rejections;
            }

            var guaranteedBefore = GuaranteedPredecessors(definition);

            foreach (var node in definition.Nodes)
            {
                ValidateCapability(definition, catalog, node, rejections);
                ValidateBindings(definition, node, guaranteedBefore, rejections);
            }

            ValidateRepeatTargetBinding(definition, guaranteedBefore, rejections);

            return rejections;
        }

        /// <summary>
        /// Milestone 8.6: that a `on the same target` repeat can actually inherit a target.
        ///
        /// Three things have to hold, and the analysis for all three already exists. The bound node
        /// must be declared here, because one definition is one action's node table and there is no
        /// address that reaches out of it. It must publish exactly one space, because that space is the
        /// whole of what the repeated instance inherits and a node publishing two would leave the
        /// engine choosing. And it must be a <i>guaranteed</i> predecessor of the repeat - the same
        /// dominance question a node binding an earlier node's output already asks - because a repeat
        /// that requested a bound target the run never produced would silently degrade into p.10's
        /// free choice, which is the opposite of what the card says.
        ///
        /// One further rule is about the definition rather than the node: at most one repeat in it may
        /// bind a target. A continuation carries one inherited space and no node identity, deliberately
        /// (<see cref="RepeatContinuation"/>), so two bindings would leave the repeated instance unable
        /// to say which selection the space it is holding belongs to.
        /// </summary>
        private static void ValidateRepeatTargetBinding(
            EffectDefinition definition,
            IReadOnlyDictionary<EffectNodeId, HashSet<EffectNodeId>> guaranteedBefore,
            List<EffectDefinitionRejection> rejections)
        {
            var binding = definition.Nodes
                .Where(node => node.RepeatTargetBinding == RepeatTargetBinding.SameTarget)
                .ToArray();
            if (binding.Length == 0)
            {
                return;
            }

            if (binding.Length > 1)
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding,
                    $"Effect definition '{definition.Id}' declares {binding.Length} repeats that bind a " +
                    "target; a repeated instance inherits one space and could not tell which selection " +
                    "it belongs to."));
                return;
            }

            var repeat = binding[0];
            if (!definition.TryGetNode(repeat.BoundSelectionNodeId, out var selection))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding,
                    $"Repeat '{repeat.Id}' binds the target of '{repeat.BoundSelectionNodeId}', which " +
                    "this definition does not declare."));
                return;
            }

            if (selection.Outputs.Count(output => output.ValueKind == EffectValueKind.Coordinate) != 1)
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding,
                    $"Repeat '{repeat.Id}' binds the target of '{selection.Id}', which does not publish " +
                    "exactly one space for the repeated instance to inherit."));
            }

            if (!guaranteedBefore.TryGetValue(repeat.Id, out var guaranteed) ||
                !guaranteed.Contains(selection.Id))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding,
                    $"Repeat '{repeat.Id}' binds the target of '{selection.Id}', which has not " +
                    "certainly run wherever the repeat is reached."));
            }
        }

        /// <summary>
        /// Milestone 8.3, closing the 7.9a review's F-CARD-06: the node table is a tree, and until now
        /// only the acyclic half of that was checked.
        ///
        /// A diamond - one node named as a child by two others - is acyclic and fully reachable, so it
        /// loaded cleanly, and then every part of the engine that assumes one parent read it wrong. The
        /// concrete failure is in <see cref="GuaranteedPredecessors"/> one method below: a node's
        /// guaranteed set is built by <b>unioning</b> what each parent guarantees, which is correct for
        /// a tree because there is one parent to union from. Give a node two, and it inherits the
        /// guarantees of the path that did <i>not</i> run - so a node under both arms of a Choose could
        /// bind an output produced only in the arm the chooser declined, and the resolver would fault
        /// mid-resolution on a definition this validator had passed. The correct answer over a graph is
        /// the intersection, but computing it would only license authoring shapes the resolver's
        /// single-visit execution and node-keyed continuations cannot represent either, so the table is
        /// held to the shape it is already read as.
        ///
        /// Sharing is expressed by declaring the node twice under two ids. That costs an author a
        /// duplicated declaration and buys every reader the guarantee that a node id names one position
        /// in one tree.
        /// </summary>
        private static void ValidateSingleParent(
            EffectDefinition definition,
            ICollection<EffectDefinitionRejection> rejections)
        {
            var parentsByChild = new Dictionary<EffectNodeId, List<EffectNodeId>>();
            foreach (var node in definition.Nodes)
            {
                foreach (var child in node.ChildNodeIds)
                {
                    if (!parentsByChild.TryGetValue(child, out var parents))
                    {
                        parents = new List<EffectNodeId>();
                        parentsByChild.Add(child, parents);
                    }

                    parents.Add(node.Id);
                }
            }

            foreach (var entry in parentsByChild
                .Where(candidate => candidate.Value.Count > 1)
                .OrderBy(candidate => candidate.Key.Value, StringComparer.Ordinal))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.NodeReachableThroughTwoParents,
                    $"Node '{entry.Key}' is named as a child by " +
                    string.Join(", ", entry.Value.Select(parent => $"'{parent}'")) +
                    "; a node table is a tree, so a shared node is declared twice under two ids."));
            }

            if (parentsByChild.TryGetValue(definition.RootNodeId, out var rootParents))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.NodeReachableThroughTwoParents,
                    $"Root node '{definition.RootNodeId}' is named as a child by " +
                    string.Join(", ", rootParents.Select(parent => $"'{parent}'")) +
                    "; the root is where execution starts and has no parent."));
            }
        }

        private static void ValidateCapability(
            EffectDefinition definition,
            EffectCapabilityCatalog catalog,
            EffectNodeDefinition node,
            ICollection<EffectDefinitionRejection> rejections)
        {
            if (!node.Capability.IsDefined)
            {
                return;
            }

            if (!catalog.TryLoad(node.Capability, out var handler))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnregisteredCapability,
                    $"Node '{node.Id}' names capability '{node.Capability}', which is not registered in this build."));
                return;
            }

            var declared = handler.Schema.Parameters.ToDictionary(
                parameter => parameter.Name, StringComparer.Ordinal);

            foreach (var input in node.Inputs)
            {
                if (!declared.TryGetValue(input.ParameterName, out var parameter))
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.UndeclaredParameter,
                        $"Node '{node.Id}' binds '{input.ParameterName}', which capability '{node.Capability}' does not declare."));
                    continue;
                }

                if (parameter.ValueKind != input.ValueKind)
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.IncompatibleBindingType,
                        $"Node '{node.Id}' binds '{input.ParameterName}' as {input.ValueKind}, but capability '{node.Capability}' declares {parameter.ValueKind}."));
                }
            }

            var bound = new HashSet<string>(
                node.Inputs.Select(input => input.ParameterName), StringComparer.Ordinal);
            foreach (var parameter in handler.Schema.Parameters.Where(candidate => candidate.IsRequired))
            {
                if (!bound.Contains(parameter.Name))
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.MissingRequiredParameter,
                        $"Node '{node.Id}' does not bind required parameter '{parameter.Name}' of capability '{node.Capability}'."));
                }
            }

            if (node.Kind == EffectNodeKind.Conditional &&
                !handler.Schema.Outputs.Any(output => output.ValueKind == EffectValueKind.Boolean))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.PredicateMustReturnBoolean,
                    $"Conditional node '{node.Id}' uses capability '{node.Capability}', which declares no Boolean output to branch on."));
            }

            foreach (var output in node.Outputs)
            {
                if (!handler.Schema.Outputs.Any(declaredOutput =>
                        string.Equals(declaredOutput.OutputName, output.OutputName, StringComparison.Ordinal) &&
                        declaredOutput.ValueKind == output.ValueKind))
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.UnresolvedNodeOutput,
                        $"Node '{node.Id}' publishes '{output}', which capability '{node.Capability}' does not declare."));
                }
            }
        }

        private static void ValidateBindings(
            EffectDefinition definition,
            EffectNodeDefinition node,
            IReadOnlyDictionary<EffectNodeId, HashSet<EffectNodeId>> guaranteedBefore,
            ICollection<EffectDefinitionRejection> rejections)
        {
            foreach (var input in node.Inputs
                .Where(candidate => candidate.SourceKind == EffectBindingSourceKind.Role))
            {
                // Milestone 6.8k. The resolver reads roles from the execution frame alone, with no
                // board access, so what a role can supply is fixed: a hero identity or the source
                // card. A binding declaring any other kind - a coordinate above all - would pass the
                // parameter type-match and then fault mid-resolution, which is exactly the failure
                // this validator exists to move to content load.
                var supplied = RoleSuppliedKind(input.Role);
                if (supplied != input.ValueKind)
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.RoleCannotSupplyValueKind,
                        $"Node '{node.Id}' binds '{input.ParameterName}' as {input.ValueKind} from role {input.Role}, " +
                        (supplied == EffectValueKind.None
                            ? "which the execution frame cannot supply at all."
                            : $"but the execution frame supplies {supplied} for that role.")));
                }
            }

            ValidateSelectionConsumer(definition, node, rejections);

            foreach (var input in node.Inputs
                .Where(candidate => candidate.SourceKind == EffectBindingSourceKind.NodeOutput))
            {
                if (!definition.TryGetNode(input.SourceNodeId, out var source))
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.UnresolvedNodeOutput,
                        $"Node '{node.Id}' reads output '{input.SourceOutputName}' from '{input.SourceNodeId}', which this definition does not declare."));
                    continue;
                }

                var declaredOutput = source.Outputs.FirstOrDefault(output =>
                    string.Equals(output.OutputName, input.SourceOutputName, StringComparison.Ordinal));
                if (string.IsNullOrEmpty(declaredOutput.OutputName))
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.UnresolvedNodeOutput,
                        $"Node '{node.Id}' reads output '{input.SourceOutputName}' from '{input.SourceNodeId}', which does not publish it."));
                    continue;
                }

                if (declaredOutput.ValueKind != input.ValueKind)
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.IncompatibleBindingType,
                        $"Node '{node.Id}' reads '{input.SourceOutputName}' as {input.ValueKind}, but '{input.SourceNodeId}' publishes {declaredOutput.ValueKind}."));
                    continue;
                }

                if (!guaranteedBefore.TryGetValue(node.Id, out var predecessors) ||
                    !predecessors.Contains(input.SourceNodeId))
                {
                    rejections.Add(new EffectDefinitionRejection(
                        EffectDefinitionRejectionReason.NodeOutputNotGuaranteed,
                        $"Node '{node.Id}' reads output '{input.SourceOutputName}' from '{input.SourceNodeId}', which is not guaranteed to have executed first."));
                }
            }
        }

        /// <summary>
        /// Milestone 6.8l. The two things a parameter type-match cannot state about the node a
        /// selection selects for: that this definition declares it, and that it reads something the
        /// selection publishes.
        ///
        /// Both are refused here rather than at the first pause for the reason every other binding
        /// rule is: a definition naming a node that is not there, or one that ignores the choice, does
        /// not describe an effect anybody can play, and finding that out mid-resolution would mean
        /// finding it out after a card was spent. The kind of the binding needs no rule - it is a
        /// declared parameter like any other, and the type-match above already checks it.
        /// </summary>
        private static void ValidateSelectionConsumer(
            EffectDefinition definition,
            EffectNodeDefinition node,
            ICollection<EffectDefinitionRejection> rejections)
        {
            if (!EffectSelectionDeclaration.TryReadConsumer(node, out var consumerNodeId))
            {
                return;
            }

            if (!definition.TryGetNode(consumerNodeId, out var consumer))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.UnresolvedSelectionConsumer,
                    $"Node '{node.Id}' selects for '{consumerNodeId}', which this definition does not declare."));
                return;
            }

            if (EffectSelectionDeclaration.ConsumedParameters(consumer, node.Id).Count == 0)
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.SelectionConsumerReadsNothing,
                    $"Node '{node.Id}' selects for '{consumerNodeId}', which binds no output of '{node.Id}'."));
            }
        }

        /// <summary>
        /// What the execution frame supplies for a role - the mirror of the resolver's own role switch,
        /// stated here so an impossible binding is refused before a match instead of faulting inside
        /// one. <see cref="EffectRoleKind.ChosenTarget"/> and <see cref="EffectRoleKind.ChosenSpace"/>
        /// are declared vocabulary no frame resolves; a selection reaches a node through a prior
        /// target-selection node's published output, never through a role.
        /// </summary>
        private static EffectValueKind RoleSuppliedKind(EffectRoleKind role)
        {
            switch (role)
            {
                case EffectRoleKind.Actor:
                case EffectRoleKind.Chooser:
                case EffectRoleKind.Controller:
                    return EffectValueKind.Hero;
                case EffectRoleKind.SourceCard:
                    return EffectValueKind.CardInstance;
                default:
                    return EffectValueKind.None;
            }
        }

        private static HashSet<EffectNodeId> Reachable(EffectDefinition definition)
        {
            var reachable = new HashSet<EffectNodeId>();
            var pending = new Stack<EffectNodeId>();
            pending.Push(definition.RootNodeId);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!reachable.Add(current))
                {
                    continue;
                }

                foreach (var child in definition.GetNode(current).ChildNodeIds)
                {
                    pending.Push(child);
                }
            }

            return reachable;
        }

        private static bool HasCycle(EffectDefinition definition)
        {
            var visiting = new HashSet<EffectNodeId>();
            var visited = new HashSet<EffectNodeId>();
            return definition.Nodes.Any(node => Visit(definition, node.Id, visiting, visited));
        }

        private static bool Visit(
            EffectDefinition definition,
            EffectNodeId nodeId,
            HashSet<EffectNodeId> visiting,
            HashSet<EffectNodeId> visited)
        {
            if (visited.Contains(nodeId))
            {
                return false;
            }

            if (!visiting.Add(nodeId))
            {
                return true;
            }

            foreach (var child in definition.GetNode(nodeId).ChildNodeIds)
            {
                if (Visit(definition, child, visiting, visited))
                {
                    return true;
                }
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
            return false;
        }

        /// <summary>
        /// For each node, the set of nodes that have certainly finished before it runs.
        ///
        /// Only a Sequence contributes: its earlier children, and everything guaranteed before those,
        /// are guaranteed before each later child. A Choose branch, a Conditional branch, and an
        /// Optional child are all skippable, so nothing inside one is guaranteed to any node outside it.
        /// A node is never guaranteed before itself.
        /// </summary>
        private static IReadOnlyDictionary<EffectNodeId, HashSet<EffectNodeId>> GuaranteedPredecessors(
            EffectDefinition definition)
        {
            var guaranteed = definition.Nodes.ToDictionary(
                node => node.Id, _ => new HashSet<EffectNodeId>());

            var order = TopologicalOrder(definition);
            foreach (var nodeId in order)
            {
                var node = definition.GetNode(nodeId);
                if (node.Kind != EffectNodeKind.Sequence)
                {
                    // A skippable child inherits only what its parent already guaranteed.
                    foreach (var child in node.ChildNodeIds)
                    {
                        guaranteed[child].UnionWith(guaranteed[nodeId]);
                    }

                    continue;
                }

                var completed = new HashSet<EffectNodeId>(guaranteed[nodeId]);
                foreach (var child in node.ChildNodeIds)
                {
                    guaranteed[child].UnionWith(completed);
                    completed.UnionWith(GuaranteedSubtree(definition, child));
                }
            }

            return guaranteed;
        }

        private static IReadOnlyList<EffectNodeId> TopologicalOrder(EffectDefinition definition)
        {
            var ordered = new List<EffectNodeId>();
            var seen = new HashSet<EffectNodeId>();
            void Walk(EffectNodeId nodeId)
            {
                if (!seen.Add(nodeId))
                {
                    return;
                }

                ordered.Add(nodeId);
                foreach (var child in definition.GetNode(nodeId).ChildNodeIds)
                {
                    Walk(child);
                }
            }

            Walk(definition.RootNodeId);
            return ordered;
        }

        /// <summary>
        /// The nodes that certainly run whenever <paramref name="nodeId"/> runs: itself, and - only
        /// through a Sequence - its children's guaranteed subtrees.
        ///
        /// An Optional child may be declined, a Choose option may not be picked, and a Conditional
        /// branch depends on a predicate, so nothing beneath those is guaranteed to anything outside
        /// them. Treating a subtree as guaranteed merely because it is nested is the bug this replaced:
        /// it let a node bind an output produced inside a `may` that the chooser can refuse.
        /// </summary>
        private static IEnumerable<EffectNodeId> GuaranteedSubtree(
            EffectDefinition definition,
            EffectNodeId nodeId)
        {
            yield return nodeId;

            var node = definition.GetNode(nodeId);
            if (node.Kind != EffectNodeKind.Sequence)
            {
                yield break;
            }

            foreach (var child in node.ChildNodeIds)
            {
                foreach (var guaranteed in GuaranteedSubtree(definition, child))
                {
                    yield return guaranteed;
                }
            }
        }
    }
}
