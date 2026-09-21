using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's one selection over a PAIR of friendly units in his printed Radius - "Swap
    /// two friendly units in radius", which Monkey Trick, Monkey Twist and Monkey Business print word
    /// for word. Milestone 8.71f.
    ///
    /// <para><b>Why this is a contract of his rather than Sabina's.</b>
    /// <c>choose-target-pair@1</c> is the shipped pair selection and its grammar is exactly this
    /// sentence's - one question naming two pieces at once, canonicalised, feeding p.19's Swap - so
    /// reuse-first points straight at it. It is still the wrong contract here, and the measurement is
    /// two words of the printed sentence:</para>
    /// <list type="number">
    /// <item><description><b>"units".</b> <c>ChooseTargetPairHandler.Candidates</c> derives its
    /// candidates with <see cref="RosterUnitKind.Minion"/> written into the call - not a parameter, a
    /// literal - because Sabina's Listen Up, Roger Roger and Ready and Waiting are the only three faces
    /// that ever asked it and all three print "two MINIONS". Hanu prints "two friendly UNITS", which
    /// p.19 makes heroes and minions alike, so on that contract Monkey Trick could never name a
    /// teammate's hero at all.</description></item>
    /// <item><description><b>"in radius".</b> That handler takes its reach as a
    /// <c>distance</c> INTEGER the definition writes down, and Sabina's three faces bind their printed
    /// Radius as a literal per face. Hanu's Greens print Radius 1, 2, 2 and 3 AND his set hands out a
    /// Radius item (Pile On's unchosen twin), which p.16 raises on every face printing the icon - so a
    /// literal would measure his disk with a number the card stops printing the moment the item is
    /// earned. The shipped way to compose the icon with its item is
    /// <see cref="RadiusValueQuery"/>, read by the handler itself exactly as
    /// <c>synthetic-radius-hero-target@1</c> reads it.</description></item>
    /// </list>
    ///
    /// <para><b>Why the icon is not bridged through <c>synthetic-card-radius@1</c> instead.</b> That
    /// publisher exists and Garrus's Regroup binds it into a presence count, so a pair whose
    /// <c>distance</c> came from it would need no new contract - but its handler is not an
    /// <c>IEffectAnticipatedOutcomeCapabilityHandler</c>, and
    /// <c>EffectOptionFeasibilityQuery</c> answers a sequence's first APPLIED non-publisher node with
    /// <c>Verdict.Performable</c> and walks no further. Every Green would then have been offered on a
    /// board holding no legal pair at all - the exact offer-and-refuse shape 8.35l and 8.70 exist to
    /// stop. Teaching that shared publisher to anticipate would move the greying of four other heroes'
    /// faces, which is a widening this block has no measurement for. HA-Q13f records the reading.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Hanu
    /// content until 8.71h deals him - milestone 7.9's gate - so
    /// <see cref="GreenDeferredHandlers"/> is the whole list until then, <see cref="GreenRegisteredHandlers"/>
    /// is empty, and <c>HanuGreensTests</c> asserts the absence from both sides. It is a new handler
    /// TYPE, so the dealing block owes it a showcase consumer on the same gate's terms Brynn's three
    /// were given one.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>
        /// "Swap two friendly units in radius" - the two units that swap, as ONE question over the
        /// pair the sentence names.
        ///
        /// <para>It selects and never performs: p.19's exchange is <c>synthetic-swap@2</c>'s, which
        /// every two-space swap in this build already reaches, and this node hands it the two spaces
        /// the chooser picked.</para>
        /// </summary>
        public static EffectCapabilityId FriendlyUnitPair { get; } =
            new EffectCapabilityId("hanu-friendly-unit-pair");

        public static EffectCapabilityReference FriendlyUnitPairV1 =>
            new EffectCapabilityReference(FriendlyUnitPair, 1);

        /// <summary>
        /// Where the first named unit is standing - the value <c>synthetic-swap@2</c> reads, because
        /// p.19's exchange names two spaces and one piece per space makes a space an unambiguous
        /// identity for every kind at once. The spellings are the shipped pair's own, so a consumer
        /// written against either reads the same names.
        /// </summary>
        public const string FirstSpaceOutputName = "firstSpace";

        public const string SecondSpaceOutputName = "secondSpace";

        public const string FirstHeroOutputName = "firstHero";

        public const string FirstMinionOutputName = "firstMinion";

        public const string SecondHeroOutputName = "secondHero";

        public const string SecondMinionOutputName = "secondMinion";

        /// <summary>
        /// The one selection this block declares, DEFERRED until 8.71h deals him: milestone 7.9's gate
        /// keeps a contract out of production registration until dealt content can reach it.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> GreenDeferredHandlers() =>
            new IEffectCapabilityHandler[] { new FriendlyUnitPairHandler() };

        /// <summary>
        /// Nothing, and the member exists so the dealing block has one place to move the selection to
        /// and one pair of lists a fixture can assert from both sides.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> GreenRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The pair, whole: every unit on Hanu's own team standing inside the performing card's
        /// printed Radius, himself excluded, that p.18 and p.19 let him exchange.
        /// </summary>
        private sealed class FriendlyUnitPairHandler
            : IEffectCapabilityHandler, IEffectImmunityOverrideCapabilityHandler
        {
            /// <summary>
            /// p.19's Swap is the activity an override may be declared for, which is the list
            /// <c>choose-target-pair@1</c> and <c>synthetic-swap@2</c> both publish. None of his five
            /// Greens declares one; the support is stated because the question this node asks is a Swap
            /// question and a node that could not carry an override would answer it differently from
            /// the exchange behind it.
            /// </summary>
            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities { get; } =
                Array.AsReadOnly(new[] { RuleActivity.Swap });

            public EffectCapabilityReference Reference => FriendlyUnitPairV1;

            /// <summary>
            /// NO parameters, which is the sentence rather than a shortcut: all three faces printing
            /// this clause print the identical words, the reach is the performing card's own Radius
            /// icon, the side is "friendly" and the kind is "units". A parameter here would be a knob
            /// no printed face of his turns - and an unbound optional one is precisely how a narrowing
            /// goes silently missing.
            /// </summary>
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[]
                    {
                        new EffectOutputBinding(FirstSpaceOutputName, EffectValueKind.Coordinate),
                        new EffectOutputBinding(SecondSpaceOutputName, EffectValueKind.Coordinate),
                        new EffectOutputBinding(FirstHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(FirstMinionOutputName, EffectValueKind.Minion),
                        new EffectOutputBinding(SecondHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(SecondMinionOutputName, EffectValueKind.Minion)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Two, always. "Swap two friendly units" and p.19's exchange moves both, so a board
            /// holding one admissible friendly unit is not a smaller version of this question but no
            /// question at all - which is what greys the primary rather than offering an answer that
            /// cannot be completed (8.35l, 8.70).
            /// </summary>
            private const int Slots = 2;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Subjects(context);
                return candidates.Count < Slots
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), Slots, Slots));
            }

            /// <summary>
            /// The answer is measured against a freshly derived set rather than against the list the
            /// question was opened with - which is what makes a restore, a replay and a rewind ask the
            /// same question, and a forged answer unanswerable.
            ///
            /// <para>It is canonicalised before it is published. The card names an UNORDERED pair:
            /// swapping A with B and B with A are one act, so the two spaces the exchange receives may
            /// not depend on which of the two the player pressed first (HA-Q10, Sabina's Listen Up
            /// reading).</para>
            /// </summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                var subjects = Subjects(context);
                if (chosen.Length != Slots || chosen.Distinct().Count() != Slots ||
                    chosen.Any(value => !value.IsDefined || !subjects.Contains(value)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var canonical = Canonical(chosen);
                var facts = new List<EffectOutcomeFact>();
                Publish(context, facts, canonical[0],
                    FirstHeroOutputName, FirstMinionOutputName, FirstSpaceOutputName);
                Publish(context, facts, canonical[1],
                    SecondHeroOutputName, SecondMinionOutputName, SecondSpaceOutputName);
                return facts.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Subjects(context).Count < Slots
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Every unit the sentence admits, in the canonical order the presence query derives.
            ///
            /// <para><b>"friendly" is p.17's word and it excludes him.</b> "Another unit on your team.
            /// It never includes you" - so the disk is read on his own side and his own piece is taken
            /// out of it, which is also p.10's rule that an action affects the acting hero only where
            /// the text says so (Misa's Living Tornado reading, HA-Q10).</para>
            ///
            /// <para><b>"units" is heroes and minions and never a token.</b> p.19's units are the two
            /// kinds, so the exclusion is the noun rather than a filter; the consuming Swap declares
            /// <see cref="SwapEligiblePieceKind.Unit"/> and would refuse a token anyway, which is the
            /// second statement of the same word.</para>
            ///
            /// <para><b>"in radius" is p.9's printed icon with p.16's matching item composed onto
            /// it</b>, read through the same query every other printed-icon consumer in this build
            /// reads, so a card's selection and a card's Swap cannot come to measure different disks. A
            /// card printing no Radius has no disk at all and reaches nobody.</para>
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> Subjects(
                EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components?.Board == null || context.Participants == null ||
                    !context.CardState.IsAvailable ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications);
                if (radius.Total <= 0)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                return RosterPresenceQuery.FindUnitsWithin(
                        actorAt,
                        radius.Total,
                        RosterUnitKind.Unit,
                        SourceTeamRelation.AlliedWithSource,
                        context.ActorId,
                        components.Board,
                        context.Participants,
                        context.Modifications,
                        BoardObjectReference.ForHero(context.ActorId))
                    .Where(candidate => Admits(context, candidate))
                    .ToArray();
            }

            /// <summary>
            /// Whether p.18's immunity and p.19's restrictions let this actor exchange this piece -
            /// asked here so that a unit the Swap would refuse is never offered, which is milestone
            /// 8.35l's rule.
            ///
            /// <para><b>It asks exactly what the consuming Swap asks, in the same two activities.</b>
            /// <c>synthetic-swap@2</c> tests the enclosing action's activity AND p.19's Swap about BOTH
            /// of its objects, so a selection asking only one of them would offer a unit immune to the
            /// other and then have the exchange refuse it. Asking it per candidate is the whole
            /// question for a pair, because the two members are judged against the same actor and
            /// nothing about either answer depends on the other: any two admitted units are a pair the
            /// exchange takes.</para>
            /// </summary>
            private static bool Admits(
                EffectCapabilityContext context, BoardObjectReference candidate)
            {
                var piece = candidate.AsRuleObject();
                if (!piece.IsDefined)
                {
                    return false;
                }

                var actor = RuleObject.ForHero(context.ActorId);
                if (IsPrevented(context, piece, actor, context.EnclosingActivity) ||
                    IsPrevented(context, piece, actor, RuleActivity.Swap))
                {
                    return false;
                }

                return !RestrictionQuery.TryGetRestriction(
                    piece, RuleActivity.Swap, actor, context.Participants, context.Modifications,
                    out _);
            }

            private static bool IsPrevented(
                EffectCapabilityContext context,
                RuleObject piece,
                RuleObject actor,
                RuleActivity activity) =>
                activity != RuleActivity.None &&
                TargetQuery.IsPrevented(
                    piece,
                    activity,
                    actor,
                    context.Components.Board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    RuleActivity.Swap);

            /// <summary>
            /// The pair in one order rather than in the order it was pressed - heroes before minions
            /// and each group in ordinal order, which is the presence query's own derivation and
            /// therefore the order the offer was made in.
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> Canonical(
                IReadOnlyList<BoardObjectReference> chosen) =>
                chosen
                    .OrderBy(value => value.Kind)
                    .ThenBy(
                        value => value.HeroId.Value ?? value.MinionId.Value, StringComparer.Ordinal)
                    .ToArray();

            /// <summary>
            /// One member's identity and space, under the names a consuming node declared. A node that
            /// declares an output never sees one it did not ask for, and an outcome recorded under a
            /// name the definition does not carry is a snapshot this build refuses to read back.
            /// </summary>
            private static void Publish(
                EffectCapabilityContext context,
                ICollection<EffectOutcomeFact> facts,
                BoardObjectReference unit,
                string heroOutput,
                string minionOutput,
                string spaceOutput)
            {
                if (unit.Kind == BoardObjectKind.Hero && context.Publishes(heroOutput))
                {
                    facts.Add(new EffectOutcomeFact(heroOutput, EffectValue.Hero(unit.HeroId)));
                }

                if (unit.Kind == BoardObjectKind.Minion && context.Publishes(minionOutput))
                {
                    facts.Add(new EffectOutcomeFact(minionOutput, EffectValue.Minion(unit.MinionId)));
                }

                if (context.Publishes(spaceOutput) && context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(unit, out var at))
                {
                    facts.Add(new EffectOutcomeFact(spaceOutput, EffectValue.Space(at)));
                }
            }

            private static EffectValue AsValue(BoardObjectReference candidate) =>
                candidate.Kind == BoardObjectKind.Hero
                    ? EffectValue.Hero(candidate.HeroId)
                    : EffectValue.Minion(candidate.MinionId);

            private static BoardObjectReference FromValue(EffectValue value)
            {
                if (value.Kind == EffectValueKind.Hero)
                {
                    return BoardObjectReference.ForHero(value.HeroId);
                }

                return value.Kind == EffectValueKind.Minion
                    ? BoardObjectReference.ForMinion(value.MinionId)
                    : default;
            }
        }
    }
}
