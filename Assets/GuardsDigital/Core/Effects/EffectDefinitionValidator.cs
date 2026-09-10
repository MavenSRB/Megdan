using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;

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
        /// Milestone 8.6. A `on the same target` repeat names a selection whose target it could not
        /// inherit: one this definition does not declare, one that publishes no single space, one that
        /// has not certainly run by the time the repeat is reached, or a second such repeat in the same
        /// definition, which would leave one inherited target with two nodes claiming it.
        /// </summary>
        UnresolvedRepeatTargetBinding = 16,

        /// <summary>
        /// Milestone 8.19a. Text applied around a primary action does not have the shape that timing
        /// requires: a root sequence of exactly two nodes - the question that places the work and the
        /// work it places - whose first node is rulebook p.10's <c>Choose</c> over branches that each
        /// publish one side through `around-primary-action@1`.
        ///
        /// <para>The shape is declared rather than inferred because the definition is entered twice,
        /// once per side of the action, and entering a definition at a node inside it is only
        /// meaningful if the definition says which node means what. Milestone 8.5's opposite rule -
        /// that text inside an Attack could not pause at all - was retired here: rulebook p.14's step 1
        /// now holds a question, so a pausing shape is admitted at every one of the page's moments.</para>
        /// </summary>
        AroundActionShapeIsMalformed = 17,

        /// <summary>
        /// A node declares an immunity exception its capability does not consume, or an activity the
        /// capability never asks. Ignored override semantics are rejected at content load.
        /// </summary>
        UnsupportedImmunityOverride = 18,

        /// <summary>
        /// Milestone 8.25e7. An enum-backed integer parameter has no matching typed literal, carries
        /// a member outside the parameter's legal set, or was supplied by a different enum whose
        /// numeric value merely happens to match.
        /// </summary>
        IntegerClosedVocabularyMismatch = 19
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
        /// Everything <see cref="Validate"/> checks, plus the extra rule the moment this text is
        /// applied at imposes on its shape.
        ///
        /// <para><b>Rulebook p.14's three moments impose nothing any more.</b> Milestone 8.5 refused a
        /// pausing shape at all three, because an attack's remaining steps were produced in the command
        /// that began them and nothing could hold them while a question stood. Milestone 8.10 supplied
        /// the holding for steps 4 and 7, whose remainders are a function of the attack's own identity.
        /// Milestone 8.19a supplied it for step 1, whose remainder is the rest of the page: the Attack
        /// resolver now produces everything after that step against live state through one generator,
        /// so a question standing there is held by the same paused frame the other two use. Arien's
        /// Noble Blade and Brogan's Mad Dash both ask one, which is why the moment had to open.</para>
        ///
        /// <para><b>Text applied around a primary action imposes a shape instead of a prohibition.</b>
        /// That timing is not one of the page's moments at all - the actor chooses which side of the
        /// action the text runs on - so the definition is entered twice, once for the question and once
        /// for the work it places. Entering a definition at a node inside it is only meaningful if the
        /// definition says which node means what, so the shape is declared: a root sequence of exactly
        /// two nodes, whose first is p.10's <c>Choose</c> over branches that each publish one side.</para>
        /// </summary>
        public static IReadOnlyList<EffectDefinitionRejection> ValidateCardText(
            EffectDefinition definition,
            EffectCapabilityCatalog catalog,
            CardTextTiming moment)
        {
            var rejections = Validate(definition, catalog).ToList();
            if (definition == null || moment != CardTextTiming.AroundTheAction)
            {
                return Array.AsReadOnly(rejections.ToArray());
            }

            ValidateAroundActionShape(definition, rejections);
            return Array.AsReadOnly(rejections.ToArray());
        }

        /// <summary>
        /// The declared shape of text applied around a primary action, checked at content load so a
        /// malformed composition is a rejected card rather than a turn that cannot continue.
        /// </summary>
        private static void ValidateAroundActionShape(
            EffectDefinition definition,
            ICollection<EffectDefinitionRejection> rejections)
        {
            if (!AroundActionShape.IsWellFormed(definition))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.AroundActionShapeIsMalformed,
                    $"Effect definition '{definition.Id}' is applied around a primary action, so its " +
                    "root is a sequence of exactly two nodes: the question that places its work and " +
                    "the work it places."));
                return;
            }

            var orderingNodeId = AroundActionShape.OrderingNode(definition);
            if (!definition.TryGetNode(orderingNodeId, out var ordering) ||
                ordering.Kind != EffectNodeKind.Choose ||
                ordering.ChildNodeIds.Count == 0)
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.AroundActionShapeIsMalformed,
                    $"Node '{orderingNodeId}' places this text on one side of its action, so it is " +
                    "rulebook p.10's Choose over at least one branch."));
                return;
            }

            foreach (var childId in ordering.ChildNodeIds)
            {
                if (definition.TryGetNode(childId, out var child) &&
                    child.Kind == EffectNodeKind.Capability &&
                    child.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticContinuationCapabilities
                            .AroundPrimaryActionV1))
                {
                    continue;
                }

                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.AroundActionShapeIsMalformed,
                    $"Branch '{childId}' of an around-action ordering question publishes which side " +
                    "of the action this text runs on, so it invokes `around-primary-action@1`."));
            }
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

            if (node.ImmunityOverrides.Count > 0)
            {
                var consumer = handler as IEffectImmunityOverrideCapabilityHandler;
                foreach (var declaration in node.ImmunityOverrides)
                {
                    if (consumer == null ||
                        !consumer.SupportedImmunityOverrideActivities.Contains(declaration.Activity))
                    {
                        rejections.Add(new EffectDefinitionRejection(
                            EffectDefinitionRejectionReason.UnsupportedImmunityOverride,
                            $"Node '{node.Id}' declares immunity override '{declaration}', but " +
                            $"capability '{node.Capability}' does not consume that activity."));
                    }
                }
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
                else
                {
                    ValidateIntegerDomain(node, input, parameter, rejections);
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

        private static void ValidateIntegerDomain(
            EffectNodeDefinition node,
            EffectInputBinding input,
            EffectCapabilityParameter parameter,
            ICollection<EffectDefinitionRejection> rejections)
        {
            if (!parameter.IntegerDomain.IsDefined && !input.IntegerVocabulary.IsDefined)
            {
                return;
            }

            // Milestone 8.37. A node output may satisfy a closed vocabulary, and the check it gets is
            // the one that can actually be made at definition time: the VOCABULARY must match, and
            // there is no literal whose membership could be tested because the value does not exist
            // yet. Commune with Spirits is why - an ally names the colour her borrow narrows to, at
            // runtime - and the membership half is enforced by EffectResolutionSession when the
            // produced value arrives, so the closed vocabulary is still closed. Everything else stays
            // literal-only: a role binding or an undeclared node output against a closed parameter is
            // the same fault it has always been.
            if (parameter.IntegerDomain.IsDefined &&
                input.SourceKind == EffectBindingSourceKind.NodeOutput &&
                input.IntegerVocabulary.IsDefined &&
                parameter.IntegerDomain.HasSameVocabulary(input.IntegerVocabulary))
            {
                return;
            }

            if (!parameter.IntegerDomain.IsDefined ||
                input.SourceKind != EffectBindingSourceKind.Literal ||
                !input.IntegerVocabulary.IsDefined ||
                !parameter.IntegerDomain.HasSameVocabulary(input.IntegerVocabulary) ||
                !parameter.IntegerDomain.Contains(input.Literal.IntegerValue))
            {
                rejections.Add(new EffectDefinitionRejection(
                    EffectDefinitionRejectionReason.IntegerClosedVocabularyMismatch,
                    $"Node '{node.Id}' binds '{input.ParameterName}' with integer vocabulary " +
                    $"'{input.IntegerVocabulary}' and value {input.Literal.IntegerValue}, but the " +
                    $"capability declares '{parameter.IntegerDomain}'."));
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
                // Milestone 8.22. The frame resolves it wherever it has one - a defender's card text
                // inside an Attack, and an after-push trigger's pushed hero. Where it has none the
                // binding resolves to the undefined hero and the step reports itself impossible, which
                // is why this is a value kind the frame can supply rather than a fifth role it cannot.
                case EffectRoleKind.OpposingHero:
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
