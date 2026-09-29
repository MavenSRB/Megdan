using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.8b. Mortimer's repeated modal - "Choose one", "Choose up to two times" and "Choose
    /// up to three times" - and Master of Puppets' raising of three to five.
    ///
    /// <para><b>The passes.</b> Wuk's Treetop Ride, Rowenna's Grand Melee and Brynn's Decoy settled
    /// the reading (BR-Q4): each pass is one p.10 "up to" (<see cref="EffectNodeKind.Optional"/>,
    /// <see cref="EffectFailurePolicy.UpTo"/>, chooser Actor) over a one-of
    /// <see cref="EffectNodeKind.Choose"/> of the face's bullets, so the same bullet may be chosen
    /// again and every pass reads the board the last one left. "Choose one" is the bare modal with no
    /// "up to" around it.</para>
    ///
    /// <para><b>Master of Puppets.</b> "Whenever you would choose up to three times, you may choose up
    /// to five times instead." Only a face printing "up to three times" composes the two extra
    /// passes, behind a <see cref="EffectNodeKind.Conditional"/> on
    /// <see cref="SyntheticRosterCapabilities.UltimateUnlockedV1"/> - p.16's own question, whether
    /// his Ultimate card sits in the Ultimate area. "You may" is each extra pass's own "up to": he
    /// may stop at three.</para>
    ///
    /// <para><b>Why the gate has an explicit else.</b> RULING 53 lets a false printed "If" with no
    /// otherwise stand as a legal do-nothing play, and the offer walk reads that from the shape: a
    /// single-child Conditional that is false keeps the whole action offered. Master of Puppets'
    /// gate is not printed on the face and must not do that - with the Ultimate locked, a face
    /// whose three passes can do nothing is greyed like any other. So the gate's else-child is a
    /// read-only leaf (the same p.16 question, which changes nothing and which the walk judges as
    /// doing nothing), and the face is judged by its printed passes alone.</para>
    /// </summary>
    public static class MortimerRepeatedChoice
    {
        /// <summary>The count Master of Puppets raises "up to three times" to.</summary>
        public const int MasterOfPuppetsPasses = 5;

        /// <summary>The most passes a face prints without the Ultimate.</summary>
        public const int PrintedMostPasses = 3;

        public static string ModalNode(string prefix, int pass) => $"{prefix}-{pass}-modal";

        public static string UpToNode(string prefix, int pass) => $"{prefix}-{pass}-up-to";

        public static string PuppetsGateNode(string prefix) => $"{prefix}-puppets-gate";

        /// <summary>
        /// The steps a face's own Sequence runs, in order, with every node they need added to
        /// <paramref name="nodes"/>. <paramref name="pass"/> names one pass's bullets (at least two)
        /// and returns their nodes; it is called once per pass with the 1-based pass number, so
        /// every node id it mints must include that number.
        /// </summary>
        public static IReadOnlyList<EffectNodeId> Compose(
            string prefix,
            int printedPasses,
            Func<int, (IReadOnlyList<EffectNodeId> Options, IEnumerable<EffectNodeDefinition> Nodes)> pass,
            List<EffectNodeDefinition> nodes)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("A prefix is required.", nameof(prefix));
            if (printedPasses < 1 || printedPasses > PrintedMostPasses)
                throw new ArgumentOutOfRangeException(nameof(printedPasses), "Mortimer prints one, two or three.");
            if (pass == null) throw new ArgumentNullException(nameof(pass));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            if (printedPasses == 1)
            {
                AddPass(prefix, 1, wrapped: false, pass, nodes);
                return new[] { new EffectNodeId(ModalNode(prefix, 1)) };
            }

            var steps = new List<EffectNodeId>();
            for (var number = 1; number <= printedPasses; number++)
            {
                AddPass(prefix, number, wrapped: true, pass, nodes);
                steps.Add(new EffectNodeId(UpToNode(prefix, number)));
            }

            if (printedPasses == PrintedMostPasses)
            {
                var extra = new List<EffectNodeId>();
                for (var number = printedPasses + 1; number <= MasterOfPuppetsPasses; number++)
                {
                    AddPass(prefix, number, wrapped: true, pass, nodes);
                    extra.Add(new EffectNodeId(UpToNode(prefix, number)));
                }

                var gate = new EffectNodeId(PuppetsGateNode(prefix));
                var unlockedPasses = new EffectNodeId($"{prefix}-puppets-passes");
                var locked = new EffectNodeId($"{prefix}-puppets-locked");
                nodes.Add(new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.UltimateUnlockedV1,
                    childNodeIds: new[] { unlockedPasses, locked },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    unlockedPasses, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: extra));
                nodes.Add(new EffectNodeDefinition(
                    locked, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.UltimateUnlockedV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }));
                steps.Add(gate);
            }

            return steps;
        }

        private static void AddPass(
            string prefix,
            int number,
            bool wrapped,
            Func<int, (IReadOnlyList<EffectNodeId> Options, IEnumerable<EffectNodeDefinition> Nodes)> pass,
            List<EffectNodeDefinition> nodes)
        {
            var (options, bulletNodes) = pass(number);
            var modal = new EffectNodeId(ModalNode(prefix, number));
            if (wrapped)
            {
                nodes.Add(new EffectNodeDefinition(
                    new EffectNodeId(UpToNode(prefix, number)), EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                    childNodeIds: new[] { modal }, chooser: EffectChooserRole.Actor));
            }

            nodes.Add(new EffectNodeDefinition(
                modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                childNodeIds: options.ToArray(),
                chooser: EffectChooserRole.Actor,
                minimumSelections: 1,
                maximumSelections: 1));
            nodes.AddRange(bulletNodes);
        }
    }
}
