using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>Why one effect frame is standing inside another one.</summary>
    public enum EffectInterruptKind
    {
        None = 0,

        /// <summary>
        /// `after-push-trigger@1`. One of rulebook p.18's Pushes applied to an object a trusted rule
        /// source watches, and that source's effect is running before the effect that pushed carries
        /// on - Sabina's Big Sodding Gun, "If you push an enemy hero, that hero discards a card, or is
        /// defeated".
        /// </summary>
        AfterPushTrigger = 1,

        /// <summary>
        /// Milestone 8.40h. A marker was GIVEN to a hero by a trusted rule source's controller, and
        /// that source's effect is running before the effect that gave it carries on - Bain's A
        /// Complicated Profession, "After you give a hero the bounty marker that hero discards a card,
        /// if able".
        ///
        /// <para>A second kind on the one shape rather than a second shape: everything
        /// <see cref="PendingEffectInterrupt"/> holds is already what this moment needs - the trigger's
        /// frame, the hero the moment named, and the identity of the resolution it interrupted. What
        /// differs is only which moment it was, which is exactly what this enum is for. Its
        /// <see cref="PendingEffectInterrupt.RemainingSubjects"/> is always empty: p.18 pushes a batch
        /// at the same time, and nothing in the rules gives several markers at once.</para>
        /// </summary>
        AfterMarkerGivenTrigger = 2,

        // Subject is the discarding owner. The exact card is the completed discard outcome
        // on InterruptedResolutionId, not a second independently persisted card identity.
        AfterResolvedCardDiscardTrigger = 3,

        /// <summary>
        /// Milestone 8.61e. A hero moved through a token whose own text says it goes off, the token has
        /// been removed and revealed, and what its removal costs that hero is running before whatever
        /// displaced them carries on - Min the Dragonmonk's mines, "After an enemy hero moves through a
        /// mine, remove it; for every blast removed, that hero discards a card, if able".
        ///
        /// <para>A fourth kind on the one shape, for the reason the second was: everything this type
        /// holds is already what this moment needs - the trigger's frame, the hero the moment named,
        /// and the identity of the resolution it interrupted. Its
        /// <see cref="PendingEffectInterrupt.RemainingSubjects"/> is always empty, and cannot be
        /// anything else: several blasts under ONE hero are several questions for the same hero, which
        /// is a count the content declares as one effect with that many nodes rather than a queue of
        /// subjects this type would refuse - it will not carry its own subject as still owed a
        /// run.</para>
        ///
        /// <para><b>What it interrupts may be nothing at all.</b> A push inside an Attack's card text
        /// carries the hero over a mine while that text is standing, and this holds that frame's place
        /// exactly as an after-push trigger does. A Movement ACTION is not an effect, so the trap opened
        /// on the way out of one interrupts a resolution that has already finished - and the answer path
        /// already says what to do with that: nothing is invented in its place, the trigger's own work
        /// is reported, and the action is left as the rest of the engine finds it.</para>
        /// </summary>
        AfterMoveThroughTrapTrigger = 4
    }

    /// <summary>
    /// Milestone 8.19c1. A trigger's effect frame standing inside the effect it interrupted.
    ///
    /// <para><b>Why it exists at all.</b> <see cref="PendingResolution"/> holds one effect definition
    /// and one node path, so it can say where a single effect stopped and nothing else. Every pause
    /// before this milestone was exactly that - one effect, standing on its own question. An after-push
    /// trigger is entered from <em>inside</em> another effect's node: the pushing effect has finished
    /// that node and has more to do, and the trigger may pause on a question of its own before it
    /// finishes. That is two positions to hold at once, and one frame has no representable place for
    /// the second - which is the schema boundary that made milestone 8.19c1 a milestone rather than the
    /// rest of 8.19c.</para>
    ///
    /// <para><b>Where the other frame is.</b> Deliberately not in here. The interrupted effect stays in
    /// whichever continuation already owned it - a Skill's backend, or rulebook p.14's card-text
    /// continuation - so a Push performed inside either of them reaches the same trigger and neither
    /// host needs a copy of the other's state. What this carries instead is
    /// <see cref="InterruptedResolutionId"/>, so a restore can check that the two frames it read belong
    /// together rather than trusting a payload that paired a trigger with somebody else's effect.</para>
    ///
    /// <para><b>What it carries.</b> The trigger's frame, why it is standing there, the object the Push
    /// applied to, and that identity. Nothing about either effect's semantics: the frame names an
    /// effect definition reference and a restore rebinds it to the trusted catalog, exactly as every
    /// other continuation in this engine does. The pushed object is here rather than re-derived because
    /// the board moves - by the time the trigger's question is answered, the hero it was asked about
    /// may have been pushed again, defeated, or removed, and "that hero" is the one the push named.</para>
    ///
    /// <para><b>It nests exactly once.</b> An interrupt is created only from a frame that is not itself
    /// one, so a trigger's own effect cannot stack a third position. That is a stated bound rather than
    /// an oversight: the roster's one printed shape is a trigger hanging off a push, no verified text
    /// pushes from inside a trigger, and a general stack would be a depth a payload could claim and no
    /// rule could justify.</para>
    /// </summary>
    public sealed class PendingEffectInterrupt
    {
        public PendingEffectInterrupt(
            PendingResolution frame,
            EffectInterruptKind kind,
            RuleObject subject,
            EffectResolutionId interruptedResolutionId,
            IEnumerable<RuleObject> remainingSubjects = null)
        {
            var remaining = (remainingSubjects ?? Enumerable.Empty<RuleObject>()).ToArray();
            if (remaining.Any(value => !value.IsDefined))
            {
                throw new ArgumentException(
                    "Every object still owed a run is a defined object.", nameof(remainingSubjects));
            }

            if (remaining.Any(value => value.Equals(subject)))
            {
                throw new ArgumentException(
                    "The object this interrupt is running for is not also still owed a run.",
                    nameof(remainingSubjects));
            }

            RemainingSubjects = new ReadOnlyCollection<RuleObject>(remaining);
            if (frame == null)
            {
                throw new ArgumentNullException(
                    nameof(frame), "An effect interrupt is the trigger frame that is standing.");
            }

            if (kind == EffectInterruptKind.None || !Enum.IsDefined(typeof(EffectInterruptKind), kind))
            {
                throw new ArgumentException(
                    "An effect interrupt states why it is standing inside another effect.", nameof(kind));
            }

            if (!subject.IsDefined)
            {
                throw new ArgumentException(
                    "An effect interrupt names the object the moment concerned.", nameof(subject));
            }

            if (!interruptedResolutionId.IsDefined)
            {
                throw new ArgumentException(
                    "An effect interrupt names the resolution it interrupted; without one nothing " +
                    "would be interrupted and the trigger would simply be an effect.",
                    nameof(interruptedResolutionId));
            }

            if (interruptedResolutionId.Equals(frame.ResolutionId))
            {
                throw new ArgumentException(
                    "A trigger does not interrupt itself.", nameof(interruptedResolutionId));
            }

            Frame = frame;
            Kind = kind;
            Subject = subject;
            InterruptedResolutionId = interruptedResolutionId;
        }

        /// <summary>The trigger's own frame, identical in kind to a paused Skill's.</summary>
        public PendingResolution Frame { get; }

        public EffectInterruptKind Kind { get; }

        /// <summary>The object the interrupting moment named - the pushed unit, for a Push.</summary>
        public RuleObject Subject { get; }

        /// <summary>
        /// The identity of the effect this trigger interrupted. The frame itself lives in that
        /// effect's own continuation; this is what pairs the two.
        /// </summary>
        public EffectResolutionId InterruptedResolutionId { get; }

        /// <summary>
        /// The other objects the same applied Push owes this source a run for, in the order the push
        /// committed them; empty whenever this is the last or only one.
        ///
        /// <para>Rulebook p.18 pushes a batch "at the same time", so one Push can displace several
        /// units a single source watches, and a trigger that stopped on a question would otherwise take
        /// the rest of the batch down with it. The queue is here rather than re-derived because by the
        /// time the question is answered the board has moved: the units this push displaced are a fact
        /// about the push, not about where anybody is standing afterwards.</para>
        /// </summary>
        public IReadOnlyList<RuleObject> RemainingSubjects { get; }

        /// <summary>The same interrupt carrying a trigger frame that has taken one more step.</summary>
        public PendingEffectInterrupt WithFrame(PendingResolution frame) =>
            new PendingEffectInterrupt(frame, Kind, Subject, InterruptedResolutionId, RemainingSubjects);
    }
}
