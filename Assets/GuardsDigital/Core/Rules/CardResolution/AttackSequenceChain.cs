using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.35z45. The predicate one bullet of a modal Attack prints about the
    /// candidate itself, over and above which units its sentence reaches and how far.
    ///
    /// <para><b>Why a closed vocabulary and not the capability reference the seam was sketched as.</b>
    /// <see cref="AttackSequenceBullet"/>'s own documentation sized this extension point as "an optional
    /// <c>EffectCapabilityReference</c> here, resolved through the existing capability catalog". Taking
    /// it that way was measured against the serializer's standing rule and refused. The bullet is
    /// PERSISTED, and every writer in
    /// <see cref="GuardsDigital.Core.Snapshots.AuthoritativeSnapshotSerializer"/> says the same thing
    /// about what may cross: identity and content the payload NAMES, never semantics it CARRIES. A
    /// capability id plus a version is a name the reader would have to resolve to a handler and then
    /// trust to filter a target set - which is a payload choosing which code runs. A closed enum is
    /// content the payload names: it is read back through
    /// <c>SnapshotEnumReader</c> exactly as the bullet's three policy enums are, a value outside the
    /// declared range is a rejected snapshot rather than a reconstructed one, and the meaning stays in
    /// this build.</para>
    ///
    /// <para><b>The rule lives in the member's name.</b> That is the same discipline
    /// <see cref="GuardsDigital.Core.Rules.Modifications.RuleModificationKind"/> follows and the reason
    /// the Dodger phase-2 milestone's Ultimate passed the identity oracle on its first run: adding an
    /// enum member is not a content-revision event, adding a public property to a type a shipped hero's
    /// definition graph reaches is. This narrowing therefore sits on the bullet - a type derived from
    /// node outcomes that no card definition graph reaches - rather than on
    /// <see cref="GuardsDigital.Core.Cards.PrintedAttackTargetPolicy"/>, which every action definition in
    /// the roster does reach and which could not be widened without moving all seven shipped heroes.
    /// </para>
    ///
    /// <para><b>It is evaluated per candidate at the bullet's own sequence start</b>, never at the
    /// modal. Ruling 19 binds each sequence's target against the board as that sequence finds it, and a
    /// pile read one whole rulebook p.14 order early would be a different pile: the first sequence's own
    /// Defense window puts cards into exactly the discard the second bullet reads.</para>
    /// </summary>
    public enum AttackSequenceSubjectNarrowing
    {
        /// <summary>
        /// The bullet's printed sentence says nothing about the candidate beyond which units it reaches
        /// and how far - which is every bullet in the build before Middlefinger of Death.
        /// </summary>
        None = 0,

        /// <summary>
        /// "a hero in range <b>who has one or more cards in the discard</b>" - the second bullet of
        /// Dodger the Warlock's Middlefinger of Death.
        ///
        /// <para>It is a NARROWING and not a gate, which is the grammar milestone 8.21 settled for
        /// Brogan's "who has played an attack card this turn" and milestone 8.22 for Sabina's "adjacent
        /// to your friendly minion": "who has" qualifies the hero being picked, so a hero with an empty
        /// pile is not offered - rather than being offered and then refused, and rather than the whole
        /// bullet being switched off by a board question. Dodger's own Littlefinger of Death and Finger
        /// of Death print the identical clause on the identical bullet and already read it that way
        /// through <c>hasCardInDiscard</c> on their printed-Range hero selection; this member is the
        /// same sentence reaching the modal-Attack contract, and both consult
        /// <see cref="GuardsDigital.Core.Cards.HeroDiscardPileQuery"/> so the two cannot come to
        /// disagree.</para>
        /// </summary>
        HasOneOrMoreCardsInTheDiscard = 1,

        /// <summary>
        /// "a hero in range <b>with an empty discard</b>" - the exact negation of
        /// <see cref="HasOneOrMoreCardsInTheDiscard"/>, and the first member this vocabulary needed a
        /// second entry for rather than a boolean flip of the first: a closed enum names each printed
        /// reading rather than pairing one member with an inverted bit, so a reader that recognises only
        /// the earlier value is not silently handed the opposite candidate set. Whisper the Outcast's
        /// Swift Justice, milestone 8.35z53.
        ///
        /// <para>Bullet 1's own printed sentence - "Target a hero in range with an empty discard" - is
        /// the candidate predicate; the step-7 rider that follows it ("that hero performs a movement
        /// action on the card they defended with") is a separate reading of the same completed
        /// sequence and is not this member's concern. Consults the same
        /// <see cref="GuardsDigital.Core.Cards.HeroDiscardPileQuery"/> as its complement, so "empty" and
        /// "one or more" cannot come to disagree about the same pile.</para>
        /// </summary>
        HasAnEmptyDiscard = 2,

        /// <summary>
        /// "Target a hero in range <b>with a [bounty] bounty marker</b>" - the first bullet of Bain the
        /// Bounty Hunter's Hunter-Seeker, milestone 8.40c.
        ///
        /// <para><b>The first member of this vocabulary about a COMPONENT rather than about a pile</b>,
        /// and the reason it is a member at all is the one this type was minted for: a persisted bullet
        /// cannot name the marker, because the only thing it could name is a component id, and a payload
        /// that named content the reader has to resolve is the sketch the remarks above record as
        /// refused. So the member says whose marker instead - the ACTOR's, which is who "a bounty
        /// marker" belongs to on every face that prints the clause - and
        /// <see cref="GuardsDigital.Core.Components.HeldMarkerQuery.HoldersOfAMarkerDeclaredBy"/> turns
        /// that back into a definition. The same method answers <c>choose-target@2</c>'s
        /// <c>holdsTheActorsMarker</c> narrowing and Bain's marker-presence predicate, so one printed
        /// sentence has one reading in all three places - the discipline the two members above keep
        /// with <c>HeroDiscardPileQuery</c>.</para>
        ///
        /// <para>Its value is 3 and the three members above keep theirs, so no payload written before
        /// this milestone decodes as a different predicate.</para>
        /// </summary>
        HoldsTheActorsMarker = 3,

        /// <summary>
        /// "Target a hero in range <b>who is adjacent to 3 or more obstacles</b>" - the second bullet
        /// of Brynn the Seeker's Gold basic, Familiar Ground, milestone 8.69a. His boxed Handicap,
        /// Home Turf, prints the identical bullet.
        ///
        /// <para><b>The first member of this vocabulary about the BOARD rather than about the candidate
        /// itself.</b> The three above read a pile or a component the candidate owns; this one reads
        /// the six spaces around wherever the candidate happens to be standing, which is a fact that
        /// changes when anybody moves. That makes the member's own remark about timing load-bearing
        /// rather than decorative: it is evaluated per candidate at the bullet's own sequence start,
        /// and a modal Attack whose first bullet pushed somebody has changed the answer for its
        /// second.</para>
        ///
        /// <para><b>The number is in the name for this type's own stated reason</b> - the rule lives in
        /// the member - and because the predicate is a threshold rather than a count:
        /// <see cref="GuardsDigital.Core.Rules.Modifications.ObstacleAdjacencyQuery"/> is the one owner
        /// of it, so this narrowing, the +2 on the Attack total, the radius-hero selection and the
        /// presence predicate cannot come to disagree about one printed phrase - the discipline the two
        /// discard members keep with <c>HeroDiscardPileQuery</c> and the marker member keeps with
        /// <c>HeldMarkerQuery</c>. Asking it reaches p.17's counts-as overlay as well, which is how his
        /// Ultimate widens this bullet without the bullet naming an Ultimate.</para>
        ///
        /// <para>Heroes only, because "a hero" is the noun the bullet prints; a minion in range is
        /// dropped rather than admitted, on the same terms the discard members drop one.</para>
        ///
        /// <para>Its value is 4 and the four members above keep theirs, so no payload written before
        /// this milestone decodes as a different predicate.</para>
        /// </summary>
        IsAdjacentToThreeOrMoreObstacles = 4,

        /// <summary>
        /// "Target a hero (minion) in range, <b>adjacent to your friendly hero</b>" - the second bullet
        /// of Hanu the Trickster's four graded Reds, milestone 8.71c. Helping Hand and Even the Odds
        /// print it of a hero, Outnumber and Pile On of a minion.
        ///
        /// <para><b>The first member of this vocabulary that says nothing about WHICH KIND of unit it
        /// is about</b>, and that is the reading rather than an economy. "Adjacent to your friendly
        /// hero" is a fact about the candidate's neighbourhood; the NOUN the bullet targets is already
        /// carried by <see cref="GuardsDigital.Core.Cards.AttackTargetableUnits"/> on the bullet's own
        /// printed policy - <c>HeroesOnly</c> on the two hero bullets, <c>MinionsOnly</c> on the two
        /// minion ones. So one member serves both nouns, and the four members above it - each of which
        /// drops a minion because a pile, a marker and "a hero in range" are hero clauses - keep doing
        /// exactly what their own sentences say.</para>
        ///
        /// <para><b>"Your friendly hero" is rulebook p.17's Friendly read literally</b>: "another unit
        /// on your team. It never includes you". So the neighbour that satisfies this is a hero on the
        /// ACTOR's team other than the actor, which is HA-Q5 - a candidate standing beside Hanu alone
        /// satisfies the bullet that prints "adjacent to you" and not this one. It is asked of
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.RosterPresenceQuery.FindUnitsWithin"/>,
        /// which is this build's one owner of "units within a disk standing in a team relation to the
        /// source", so the team question here and the team question every allied selection asks cannot
        /// come to disagree.</para>
        ///
        /// <para><b>Like member 4 it reads the board</b>, so its answer changes when anybody moves and
        /// this type's own timing remark is load-bearing: it is evaluated per candidate at the bullet's
        /// own sequence start, and a "choose one, or both" whose first bullet defeated the teammate's
        /// neighbour has changed the answer for its second.</para>
        ///
        /// <para>Its value is 5 and the five members above keep theirs, so no payload written before
        /// this milestone decodes as a different predicate.</para>
        /// </summary>
        AdjacentToAFriendlyHeroOfTheActor = 5,

        /// <summary>
        /// "Target a hero in range, <b>adjacent to your friendly hero and not adjacent to you</b>" -
        /// the second bullet of Hanu's Trusted Sidekick, milestone 8.71c.
        ///
        /// <para>It is <see cref="AdjacentToAFriendlyHeroOfTheActor"/> with the printed second half,
        /// and a SEPARATE member rather than a flag beside it for the reason
        /// <see cref="HasAnEmptyDiscard"/> is separate from its own complement: a closed enum names each
        /// printed reading whole, so a reader that recognises only the shorter clause is never handed
        /// the wider candidate set. Trusted Sidekick is the only face in the build that prints the
        /// exclusion, and it prints it because "or both" would otherwise let its two bullets name one
        /// unit - the bullets are disjoint by their own words (HA-Q6), which is why the face needs no
        /// "(on different targets)" to keep them apart.</para>
        ///
        /// <para>"Not adjacent to you" is measured from the ACTOR's own space, which is who "you" is on
        /// every face that prints the clause - the same reading
        /// <c>SyntheticRosterCapabilities.ExcludeAdjacentParameter</c> takes of the identical words
        /// (milestone 8.39h), and p.19's innermost ring removed from under the printed Range rather
        /// than a second distance of its own.</para>
        ///
        /// <para>Its value is 6 and the six members above keep theirs.</para>
        /// </summary>
        AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor = 6,

        /// <summary>
        /// "Target a unit in range <b>adjacent to a Tree token</b>" - the second bullet of Wuk the
        /// Grove Keeper's Nature's Protector, Nature's Guardian and Nature's Champion, milestone 8.114b.
        ///
        /// <para><b>Ownership names the kind</b>, the reading milestone 8.88 recorded for every token
        /// clause of an undealt hero: this vocabulary is a closed enum and carries no token definition,
        /// and Wuk owns exactly one kind of token, so "a token the actor owns" and "a Tree token" select
        /// the same pieces for him (his shard records the equivalence). Like member 5 it says nothing
        /// about which kind of unit it is about - the noun is the bullet's own
        /// <see cref="GuardsDigital.Core.Cards.AttackTargetableUnits"/>.</para>
        ///
        /// <para>Standing beside a token is not something done to it, so no immunity is asked of the
        /// token (p.19), and it reads the board per candidate at the bullet's own sequence start, so a
        /// Tree removed by an earlier bullet has changed the answer for a later one.</para>
        ///
        /// <para>Its value is 7 and the seven members above keep theirs.</para>
        /// </summary>
        AdjacentToATokenTheActorOwns = 7
    }

    /// <summary>
    /// Milestone 8.35z27. One bullet of a modal Attack's "choose one, or both", and the
    /// targeting sentence printed beside it.
    ///
    /// <para>The sentence travels as a <see cref="PrintedAttackTargetPolicy"/> rather than as a
    /// resolved unit, because rulebook p.14 is run once per chosen bullet and each run binds its own
    /// target against the board <em>as it then stands</em>. A bullet that carried a unit would have
    /// chosen it at the modal, which is one board earlier than the sequence it belongs to.</para>
    ///
    /// <para><b>The extension point this contract left open, TAKEN at milestone
    /// 8.35z45.</b> A printed policy states which units, how far, and whether p.19's
    /// straight line is excluded. It cannot state a PREDICATE about the candidate, and Middlefinger of
    /// Death's second bullet is "Target a hero in range who has one or more cards in the discard". That
    /// predicate now travels as <see cref="SubjectNarrowing"/>, a closed printed vocabulary rather than
    /// the capability reference the seam was sketched as - see
    /// <see cref="AttackSequenceSubjectNarrowing"/> for why the sketch was refused. The contract's own
    /// prediction held exactly: adding it touched this type alone, so it moved no shared declaration
    /// struct and no shipped hero's content identity.</para>
    ///
    /// <para>The id is the effect node the card declares this bullet as. There is deliberately no
    /// printed ordinal beside it: the node already IS the bullet's identity, the order the bullets
    /// run in is the order their outcomes arrive in, and a literal ordinal would be a second place
    /// the same fact could be wrong - as well as an integer capability parameter that is neither an
    /// ordinary quantity nor an enum-backed vocabulary, which content load refuses.</para>
    /// </summary>
    public readonly struct AttackSequenceBullet : IEquatable<AttackSequenceBullet>
    {
        public AttackSequenceBullet(
            string bulletId,
            PrintedAttackTargetPolicy targetPolicy,
            AttackSequenceSubjectNarrowing subjectNarrowing = AttackSequenceSubjectNarrowing.None,
            bool appliesAfterAttackText = true)
        {
            if (string.IsNullOrWhiteSpace(bulletId))
            {
                throw new ArgumentException(
                    "A modal Attack bullet is named by the card it is printed on.", nameof(bulletId));
            }

            if (!Enum.IsDefined(typeof(AttackSequenceSubjectNarrowing), subjectNarrowing))
            {
                throw new ArgumentException(
                    "A modal Attack bullet states the predicate it prints about its candidate, or " +
                    "states that it prints none.",
                    nameof(subjectNarrowing));
            }

            BulletId = bulletId;
            TargetPolicy = targetPolicy;
            SubjectNarrowing = subjectNarrowing;
            AppliesAfterAttackText = appliesAfterAttackText;
        }

        /// <summary>The bullet's stable content identity, so a paused chain names what it is owed.</summary>
        public string BulletId { get; }

        /// <summary>What this bullet's own sentence lets its sequence target.</summary>
        public PrintedAttackTargetPolicy TargetPolicy { get; }

        /// <summary>
        /// Milestone 8.35z45. The predicate this bullet prints about the candidate itself,
        /// applied to each candidate when this bullet's own sequence derives its target set.
        ///
        /// <para>It is deliberately NOT part of <see cref="TargetPolicy"/>. The policy is a printed
        /// Attack's own declaration and every action definition in the roster carries one; the
        /// narrowing belongs to one bullet of one modal, and putting it on the shared struct would have
        /// re-identified all seven shipped heroes to say something six of them do not print.</para>
        /// </summary>
        public AttackSequenceSubjectNarrowing SubjectNarrowing { get; }

        /// <summary>Whether this bullet includes the action's after-Attack text.</summary>
        public bool AppliesAfterAttackText { get; }

        /// <summary>Whether this bullet prints a predicate about the candidate at all.</summary>
        public bool NarrowsItsSubject => SubjectNarrowing != AttackSequenceSubjectNarrowing.None;

        public bool IsDefined => !string.IsNullOrWhiteSpace(BulletId);

        public bool Equals(AttackSequenceBullet other) =>
            string.Equals(BulletId, other.BulletId, StringComparison.Ordinal) &&
            TargetPolicy.Equals(other.TargetPolicy) &&
            SubjectNarrowing == other.SubjectNarrowing &&
            AppliesAfterAttackText == other.AppliesAfterAttackText;

        public override bool Equals(object obj) =>
            obj is AttackSequenceBullet other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (((((StringComparer.Ordinal.GetHashCode(BulletId ?? string.Empty) * 397) ^
                    TargetPolicy.GetHashCode()) * 397) ^ (int)SubjectNarrowing) * 397) ^ AppliesAfterAttackText.GetHashCode();
            }
        }

        public override string ToString() => NarrowsItsSubject
            ? $"{BulletId} ({TargetPolicy}, {SubjectNarrowing})"
            : $"{BulletId} ({TargetPolicy})";
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
