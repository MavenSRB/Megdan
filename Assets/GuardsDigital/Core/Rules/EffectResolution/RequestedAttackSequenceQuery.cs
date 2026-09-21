using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.35z27. The plan a completed modal frame named: which bullets the actor
    /// chose, in the order the frame ran them, and whether the card printed "(on different targets)".
    ///
    /// <para>Read exactly the way milestone 8.19a reads a bound Attack target and milestone 8.10 reads
    /// a requested Defeat - as ordinary applied node outcomes, confirmed against the trusted
    /// definition before they are believed. That transport is what makes the plan survive the pause
    /// the modal itself opens, a snapshot restore taken while it stands, and a replay.</para>
    ///
    /// <para><b>The order is the ACTOR's, and this remark is milestone 8.71c's correction of its own
    /// earlier text.</b> Milestone 8.35z27 wrote here that "the order is the definition's printed
    /// order, not the actor's", and that the actor's freedom to order the bullets was "a rule this
    /// build does not yet give any modal". Milestone 8.35z29 then gave it to every modal, for Misa's
    /// Power Overwhelming and for p.10's own general sentence - "Unless the card text says otherwise
    /// you can choose the bullet points in any order" -
    /// by pushing a <see cref="EffectNodeKind.Choose"/>'s selected options in reverse of
    /// <c>PendingEffectChoice.SelectedCandidates</c>, which is appended to as each Select command
    /// arrives and therefore already IS the chooser's order. The outcomes arrive in the order the
    /// options execute, so the plan this query builds is the order the actor named. The paragraph
    /// above was never re-read after that change; HA-Q6 measured it against Hanu's "Choose one, or
    /// both, in any order", found the engine already correct and the comment wrong, and
    /// <c>HanuRedsTests</c> holds the reading with a test rather than with a sentence.</para>
    /// </summary>
    public static class RequestedAttackSequenceQuery
    {
        /// <summary>
        /// The plan the frame's completed outcomes named, or <c>null</c> when they named none - which
        /// is every frame in this build outside a modal Attack.
        /// </summary>
        public static AttackSequencePlan Requested(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return null;
            }

            var bullets = new List<AttackSequenceBullet>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var requireDistinctTargets = false;
            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out _))
                {
                    continue;
                }

                if (!TryReadBullet(outcome, outcome.NodeId, out var bullet, out var distinct))
                {
                    continue;
                }

                // A frame that ran the same bullet twice is a frame that answered one modal twice;
                // p.10 gives a modal action each bullet once, so the first record stands and the
                // second is not a second sequence.
                if (!seen.Add(bullet.BulletId))
                {
                    continue;
                }

                bullets.Add(bullet);
                requireDistinctTargets |= distinct;
            }

            return bullets.Count == 0
                ? null
                : new AttackSequencePlan(bullets, requireDistinctTargets);
        }

        /// <summary>
        /// Milestone 8.35z27. Every bullet a modal Attack's card text DECLARES, read
        /// statically from the definition rather than from a run.
        ///
        /// <para>It is a different question from <see cref="Requested"/>, which reports what an actor
        /// actually chose. This one is what the offer-feasibility gate needs: before the action starts
        /// there is no frame and no answer, and the question "can this Attack reach anything at all"
        /// is answered by asking each printed sentence of the board as it stands. Every bullet's
        /// sentence is literal content on its node - the three enums of a printed target policy - so
        /// no execution is required to read it, which is exactly why the sentence travels as a policy
        /// rather than as a unit.</para>
        /// </summary>
        public static IReadOnlyList<AttackSequenceBullet> Declared(EffectDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var declared = new List<AttackSequenceBullet>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in definition.Nodes)
            {
                if (node.Kind != EffectNodeKind.Capability ||
                    !node.Capability.Equals(SyntheticSequenceCapabilities.AttackSequenceV1) ||
                    !TryReadDeclaredBullet(node, out var bullet) ||
                    !seen.Add(bullet.BulletId))
                {
                    continue;
                }

                declared.Add(bullet);
            }

            return declared;
        }

        private static bool TryReadDeclaredBullet(
            EffectNodeDefinition node, out AttackSequenceBullet bullet)
        {
            bullet = default;
            if (!TryReadLiteralInteger(
                    node, SyntheticSequenceCapabilities.TargetableUnitsParameter, out var units) ||
                !TryReadLiteralInteger(
                    node, SyntheticSequenceCapabilities.ReachParameter, out var reach) ||
                !TryReadLiteralInteger(
                    node, SyntheticSequenceCapabilities.LineParameter, out var line) ||
                !Enum.IsDefined(typeof(AttackTargetableUnits), units) ||
                !Enum.IsDefined(typeof(AttackTargetReach), reach) ||
                !Enum.IsDefined(typeof(AttackTargetLine), line))
            {
                return false;
            }

            bullet = new AttackSequenceBullet(
                SyntheticSequenceCapabilities.BulletIdFor(node.Id),
                new PrintedAttackTargetPolicy(
                    (AttackTargetableUnits)units,
                    (AttackTargetReach)reach,
                    (AttackTargetLine)line),
                DeclaredNarrowing(node),
                !node.Inputs.Any(input => input.ParameterName == SyntheticSequenceCapabilities.AppliesAfterAttackTextParameter &&
                    input.SourceKind == EffectBindingSourceKind.Literal && input.Literal.Kind == EffectValueKind.Boolean &&
                    !input.Literal.BooleanValue));
            return true;
        }

        /// <summary>
        /// Milestone 8.35z45. The predicate this bullet's node declares, read as a literal
        /// exactly as its three policy enums are.
        ///
        /// <para>A node that binds nothing here prints no clause, which is every bullet composed before
        /// Middlefinger of Death. An out-of-range literal is read as no clause rather than refused: this
        /// query answers the offer-feasibility gate, and a bullet whose declaration is unreadable is
        /// already refused by the capability's own <c>Anticipate</c> when the modal runs. Widening the
        /// offer is the safe direction of the two - it offers an action the answer may then decline,
        /// where narrowing it would grey a card for a reason nothing printed.</para>
        /// </summary>
        private static AttackSequenceSubjectNarrowing DeclaredNarrowing(EffectNodeDefinition node) =>
            TryReadLiteralInteger(
                node, SyntheticSequenceCapabilities.SubjectNarrowingParameter, out var narrowing) &&
            Enum.IsDefined(typeof(AttackSequenceSubjectNarrowing), narrowing)
                ? (AttackSequenceSubjectNarrowing)narrowing
                : AttackSequenceSubjectNarrowing.None;

        private static bool TryReadLiteralInteger(
            EffectNodeDefinition node, string parameterName, out int value)
        {
            value = 0;
            foreach (var input in node.Inputs)
            {
                if (!string.Equals(input.ParameterName, parameterName, StringComparison.Ordinal) ||
                    input.SourceKind != EffectBindingSourceKind.Literal ||
                    input.Literal.Kind != EffectValueKind.Integer)
                {
                    continue;
                }

                value = input.Literal.IntegerValue;
                return true;
            }

            return false;
        }

        private static bool TryReadBullet(
            EffectNodeOutcome outcome,
            EffectNodeId nodeId,
            out AttackSequenceBullet bullet,
            out bool distinctTargets)
        {
            bullet = default;
            distinctTargets = false;

            var units = -1;
            var reach = -1;
            var line = -1;
            var sawDistinct = false;

            // Milestone 8.35z45. Absent unless the bullet prints the clause, so a plan
            // named by a frame that ran before this field existed reads exactly as it did.
            var narrowing = AttackSequenceSubjectNarrowing.None;
            var appliesAfterAttackText = true;
            foreach (var fact in outcome.Outputs)
            {
                if (string.Equals(
                        fact.OutputName, SyntheticSequenceCapabilities.TargetableUnitsOutput,
                        StringComparison.Ordinal) &&
                    fact.Value.Kind == EffectValueKind.Integer)
                {
                    units = fact.Value.IntegerValue;
                }
                else if (string.Equals(
                        fact.OutputName, SyntheticSequenceCapabilities.ReachOutput,
                        StringComparison.Ordinal) &&
                    fact.Value.Kind == EffectValueKind.Integer)
                {
                    reach = fact.Value.IntegerValue;
                }
                else if (string.Equals(
                        fact.OutputName, SyntheticSequenceCapabilities.LineOutput,
                        StringComparison.Ordinal) &&
                    fact.Value.Kind == EffectValueKind.Integer)
                {
                    line = fact.Value.IntegerValue;
                }
                else if (string.Equals(
                        fact.OutputName, SyntheticSequenceCapabilities.DistinctTargetsOutput,
                        StringComparison.Ordinal) &&
                    fact.Value.Kind == EffectValueKind.Boolean)
                {
                    sawDistinct = true;
                    distinctTargets = fact.Value.BooleanValue;
                }
                else if (string.Equals(
                        fact.OutputName, SyntheticSequenceCapabilities.SubjectNarrowingOutput,
                        StringComparison.Ordinal) &&
                    fact.Value.Kind == EffectValueKind.Integer &&
                    Enum.IsDefined(
                        typeof(AttackSequenceSubjectNarrowing), fact.Value.IntegerValue))
                {
                    narrowing = (AttackSequenceSubjectNarrowing)fact.Value.IntegerValue;
                }
                else if (fact.OutputName == SyntheticSequenceCapabilities.AppliesAfterAttackTextOutput &&
                    fact.Value.Kind == EffectValueKind.Boolean)
                    appliesAfterAttackText = fact.Value.BooleanValue;
            }

            if (!sawDistinct ||
                !Enum.IsDefined(typeof(AttackTargetableUnits), units) ||
                !Enum.IsDefined(typeof(AttackTargetReach), reach) ||
                !Enum.IsDefined(typeof(AttackTargetLine), line))
            {
                distinctTargets = false;
                return false;
            }

            bullet = new AttackSequenceBullet(
                SyntheticSequenceCapabilities.BulletIdFor(nodeId),
                new PrintedAttackTargetPolicy(
                    (AttackTargetableUnits)units,
                    (AttackTargetReach)reach,
                    (AttackTargetLine)line),
                narrowing, appliesAfterAttackText);
            return true;
        }
    }
}
