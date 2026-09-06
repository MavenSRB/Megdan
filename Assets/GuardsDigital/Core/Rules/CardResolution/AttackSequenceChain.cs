using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.35z27. One bullet of a modal Attack's "choose one, or both", and the
    /// targeting sentence printed beside it.
    ///
    /// <para>The sentence travels as a <see cref="PrintedAttackTargetPolicy"/> rather than as a
    /// resolved unit, because rulebook p.14 is run once per chosen bullet and each run binds its own
    /// target against the board <em>as it then stands</em>. A bullet that carried a unit would have
    /// chosen it at the modal, which is one board earlier than the sequence it belongs to.</para>
    ///
    /// <para><b>The one extension point this contract leaves open.</b> A printed policy states which
    /// units, how far, and whether p.19's straight line is excluded. It cannot state a PREDICATE
    /// about the candidate, and one printed bullet in the unlanded roster needs one: Middlefinger of
    /// Death's second bullet is "Target a hero in range who has one or more cards in the discard".
    /// The seam for it is an optional <c>EffectCapabilityReference</c> here, resolved through the
    /// existing capability catalog as a per-candidate predicate where the target set is derived. It
    /// is deliberately not built yet - nothing in this build prints such a bullet, and inventing the
    /// shape before a printed face names it is the deferral milestone 7.9's gate refuses. Adding it
    /// later touches this type alone, so it moves no shared declaration struct and no hero's content
    /// identity.</para>
    ///
    /// <para>The id is the effect node the card declares this bullet as. There is deliberately no
    /// printed ordinal beside it: the node already IS the bullet's identity, the order the bullets
    /// run in is the order their outcomes arrive in, and a literal ordinal would be a second place
    /// the same fact could be wrong - as well as an integer capability parameter that is neither an
    /// ordinary quantity nor an enum-backed vocabulary, which content load refuses.</para>
    /// </summary>
    public readonly struct AttackSequenceBullet : IEquatable<AttackSequenceBullet>
    {
        public AttackSequenceBullet(string bulletId, PrintedAttackTargetPolicy targetPolicy)
        {
            if (string.IsNullOrWhiteSpace(bulletId))
            {
                throw new ArgumentException(
                    "A modal Attack bullet is named by the card it is printed on.", nameof(bulletId));
            }

            BulletId = bulletId;
            TargetPolicy = targetPolicy;
        }

        /// <summary>The bullet's stable content identity, so a paused chain names what it is owed.</summary>
        public string BulletId { get; }

        /// <summary>What this bullet's own sentence lets its sequence target.</summary>
        public PrintedAttackTargetPolicy TargetPolicy { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(BulletId);

        public bool Equals(AttackSequenceBullet other) =>
            string.Equals(BulletId, other.BulletId, StringComparison.Ordinal) &&
            TargetPolicy.Equals(other.TargetPolicy);

        public override bool Equals(object obj) =>
            obj is AttackSequenceBullet other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(BulletId ?? string.Empty) * 397) ^
                    TargetPolicy.GetHashCode();
            }
        }

        public override string ToString() => $"{BulletId} ({TargetPolicy})";
    }

    /// <summary>
    /// Milestone 8.35z27. What one modal Attack action is going to do: the bullets its
    /// actor chose, in the order the actor chose them, and whether the card forbids pointing two of
    /// them at one unit.
    ///
    /// <para><b>Ruling 5 and Ruling 19.</b> "Choose one, or both" runs the whole of rulebook p.14 once
    /// per chosen bullet - own targeting, own Defense window, own Attack Total with its own per-target
    /// minion modifiers, own removal - and the whole of it is ONE action. p.10's Modal Actions passage
    /// says so in the rulebook's own words: "Treat this as a single action. Unless the card text says
    /// otherwise you can choose the bullet points in any order."</para>
    ///
    /// <para><b>Why this is not p.10's Repeat.</b> A repeat is a SEPARATE instance of an action, and
    /// every rule keyed on that label reads it as one: a restriction that forbids repeating actions
    /// stops it, p.8's mid-turn Lane Push resolves between instances, and "you performed an Attack
    /// action" is raised once per instance. None of those sentences is printed on a modal Attack, so
    /// composing one as a repeat would hand a card a rule its text does not carry - which is exactly
    /// what the Dodger phase-2 worker measured on Middlefinger of Death against Enfeeblement's
    /// "cannot repeat actions" (PLAN Ruling 14: p.10's Repeat keyword ONLY). The implementation is
    /// shared with p.14 wholesale; the LABEL is not shared, and this type is the label.</para>
    ///
    /// <para><b>The different-targets constraint is the card's, not the contract's.</b> Ruling 19's
    /// rider: with no printed "(on different targets)" the same unit may be both targets provided it
    /// still satisfies each bullet's own sentence when that bullet resolves; Middlefinger and Spirit
    /// Bear print the constraint and their targets must differ.</para>
    /// </summary>
    public sealed class AttackSequencePlan
    {
        public AttackSequencePlan(
            IEnumerable<AttackSequenceBullet> bullets, bool requireDistinctTargets)
        {
            var chosen = (bullets ?? throw new ArgumentNullException(nameof(bullets))).ToArray();
            if (chosen.Length == 0)
            {
                throw new ArgumentException(
                    "A modal Attack runs at least the one bullet its actor chose.", nameof(bullets));
            }

            if (chosen.Any(bullet => !bullet.IsDefined))
            {
                throw new ArgumentException(
                    "Every bullet in a modal Attack's plan is named.", nameof(bullets));
            }

            if (chosen.Select(bullet => bullet.BulletId)
                .Distinct(StringComparer.Ordinal).Count() != chosen.Length)
            {
                throw new ArgumentException(
                    "p.10 lets a modal action take each bullet once, so a plan names each of them " +
                    "at most once.",
                    nameof(bullets));
            }

            Bullets = new ReadOnlyCollection<AttackSequenceBullet>(chosen);
            RequireDistinctTargets = requireDistinctTargets;
        }

        /// <summary>The chosen bullets in the actor's chosen order; p.10 lets the actor pick it.</summary>
        public IReadOnlyList<AttackSequenceBullet> Bullets { get; }

        /// <summary>Whether the card printed "(on different targets)".</summary>
        public bool RequireDistinctTargets { get; }
    }

    /// <summary>
    /// Milestone 8.35z27. A modal Attack action part-way through its plan: which sequences
    /// have started, and which units the earlier ones bound.
    ///
    /// <para>It is identity and nothing else, which is what lets a snapshot carry it, an Undo rewind
    /// through it and a per-seat projection describe it. What a sequence <em>does</em> is p.14's, run
    /// by the ordinary Attack resolver with this bullet's policy handed to its target set.</para>
    ///
    /// <para><b>Why the taken targets are carried rather than re-derived.</b> Nothing on the board
    /// says which unit an earlier sequence attacked once that sequence is over - a defended target is
    /// still standing and looks exactly like a unit nobody has taken. Ruling 19's "(on different
    /// targets)" needs the answer, so the chain records it. Milestone 8.10's single
    /// <c>ExcludedTarget</c> would have covered a two-bullet card and nothing wider, and the roster
    /// prints "or both" rather than "or all", so the list is the honest shape rather than the
    /// convenient one.</para>
    /// </summary>
    public sealed class PendingAttackSequenceChain
    {
        public PendingAttackSequenceChain(
            PendingAttackResolution actionIdentity,
            AttackSequencePlan plan,
            int startedSequences,
            IEnumerable<AttackTarget> targetsTaken)
        {
            ActionIdentity = actionIdentity ?? throw new ArgumentNullException(nameof(actionIdentity));
            if (!actionIdentity.ResolutionId.IsDefined)
            {
                throw new ArgumentException(
                    "A modal Attack chain is identified by the action that opened it.",
                    nameof(actionIdentity));
            }

            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (startedSequences < 0 || startedSequences > plan.Bullets.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startedSequences), startedSequences,
                    "A modal Attack cannot have started more sequences than its plan names.");
            }

            StartedSequences = startedSequences;
            TargetsTaken = new ReadOnlyCollection<AttackTarget>(
                (targetsTaken ?? Array.Empty<AttackTarget>()).ToArray());
        }

        /// <summary>
        /// The action every one of this chain's sequences belongs to: the resolution the modal opened,
        /// standing at rulebook p.14's step 1 with no target of its own.
        ///
        /// <para>It is carried rather than rebuilt because it genuinely outlives every sequence: p.14's
        /// minion arm can end a sequence's command with a Defeat standing at its removal stage, which
        /// leaves the action with a live plan and no pending resolution at all - and the next sequence
        /// still has to know whose card, whose action and whose hero it is. It carries only identity,
        /// which is why the snapshot writes it with the same writer every other pending Attack uses.</para>
        /// </summary>
        public PendingAttackResolution ActionIdentity { get; }

        /// <summary>The identity of the action the modal opened; every sequence derives its own from it.</summary>
        public ActionResolutionId BaseResolutionId => ActionIdentity.ResolutionId;

        public AttackSequencePlan Plan { get; }

        /// <summary>How many of the plan's sequences have been started, the running one included.</summary>
        public int StartedSequences { get; }

        /// <summary>The units the sequences already resolved bound, in the order they bound them.</summary>
        public IReadOnlyList<AttackTarget> TargetsTaken { get; }

        /// <summary>Whether p.14 is still owed for a bullet this action chose.</summary>
        public bool HasNextSequence => StartedSequences < Plan.Bullets.Count;

        /// <summary>The bullet the sequence now running belongs to.</summary>
        public AttackSequenceBullet RunningBullet => StartedSequences <= 0
            ? default
            : Plan.Bullets[StartedSequences - 1];

        /// <summary>The bullet the next sequence will run.</summary>
        public AttackSequenceBullet NextBullet => HasNextSequence
            ? Plan.Bullets[StartedSequences]
            : default;

        /// <summary>
        /// The identity the next sequence runs under.
        ///
        /// <para>It is derived rather than counted, for the reason milestone 8.25d gave the card-text
        /// repeat one: rulebook p.13's Defeat outcome is identified as
        /// <c>&lt;resolution&gt;:hero-defeat</c> and <see cref="HeroCoinLedger"/> refuses to pay one
        /// twice, so two sequences of a choose-both Attack that shared an identity could not both
        /// defeat a hero. Deriving it from the base and the position also means a replay of the same
        /// accepted commands mints the same identities.</para>
        /// </summary>
        public ActionResolutionId NextSequenceResolutionId =>
            new ActionResolutionId($"{BaseResolutionId.Value}:seq-{StartedSequences + 1}");

        /// <summary>Whether this resolution is one of this chain's sequences.</summary>
        public bool Owns(ActionResolutionId resolutionId) =>
            resolutionId.IsDefined && resolutionId.Value.StartsWith(
                BaseResolutionId.Value + ":seq-", StringComparison.Ordinal);

        /// <summary>
        /// The units the sequence now choosing may not take - which is every unit an earlier sequence
        /// of this same action bound. Empty unless the card printed "(on different targets)": Ruling
        /// 19's rider is that an unconstrained card may point both bullets at one unit, so the
        /// exclusion has to be the card's statement rather than the contract's habit.
        /// </summary>
        public IReadOnlyList<AttackTarget> ExcludedTargets =>
            Plan.RequireDistinctTargets
                ? TargetsTaken
                : (IReadOnlyList<AttackTarget>)Array.Empty<AttackTarget>();

        /// <summary>The same chain with its next sequence started.</summary>
        public PendingAttackSequenceChain Starting()
        {
            if (!HasNextSequence)
            {
                throw new InvalidOperationException(
                    "A modal Attack whose plan is spent has no sequence left to start.");
            }

            return new PendingAttackSequenceChain(
                ActionIdentity, Plan, StartedSequences + 1, TargetsTaken);
        }

        /// <summary>The same chain, remembering the unit the running sequence just bound.</summary>
        public PendingAttackSequenceChain Binding(AttackTarget target)
        {
            if (!target.IsDefined)
            {
                return this;
            }

            return new PendingAttackSequenceChain(
                ActionIdentity, Plan, StartedSequences,
                TargetsTaken.Concat(new[] { target }));
        }
    }
}
