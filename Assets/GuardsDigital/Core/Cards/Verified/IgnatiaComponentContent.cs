using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's engine seam - milestone 8.75 - the one mechanism no dealt hero carries: the
    /// Tie Breaker coin selecting which of a card's two printed texts applies.
    ///
    /// <para><b>The shape.</b> Every one of her action cards is composed as an
    /// <see cref="EffectNodeKind.Conditional"/> whose predicate is <c>ignatia-coin-sun@1</c>: its
    /// then-branch is the sun (☀) text, its else-branch the moon (☾) text. The predicate reads the
    /// live revealed coin face off <see cref="EffectCapabilityContext.TieBreakerRevealedSide"/>, which
    /// milestone 8.75 threads through the resolver, so no card holds coin geometry and the branch is
    /// chosen exactly where every other Conditional's is. Equilibrium's round-long free choice and the
    /// Ultimate's flip-and-repeat are later 8.75 slices that layer onto this one predicate.</para>
    ///
    /// <para><b>Deferred, not registered.</b> The only content that names this capability is Ignatia's,
    /// and she is not dealt by this build (milestone 7.9's gate). The registered-capability list is
    /// serialized into every snapshot, so registering one before its hero is dealt moves pinned bytes;
    /// <see cref="RegisteredHandlers"/> is therefore empty and <see cref="DeferredHandlers"/> carries
    /// the real one, which the dealing block moves across.</para>
    /// </summary>
    public static class IgnatiaComponentContent
    {
        public static EffectCapabilityId CoinSun { get; } = new EffectCapabilityId("ignatia-coin-sun");

        /// <summary><c>ignatia-coin-sun@1</c>: whether the revealed Tie Breaker face is her sun symbol.</summary>
        public static EffectCapabilityReference CoinSunV1 => new EffectCapabilityReference(CoinSun, 1);

        /// <summary>The boolean a <see cref="EffectNodeKind.Conditional"/> branches on: sun then, moon else.</summary>
        public const string SunOutput = "sun";

        /// <summary>
        /// The coin face that bears Ignatia's SUN (☀) symbol. USER-SETTLED 2026-09-22: "sun is red
        /// team". The Tie Breaker coin the engine models only as a <see cref="TeamSide"/>
        /// (<see cref="GuardsDigital.Core.Setup.TieBreakerState.RevealedSide"/>), and her sun/moon are
        /// its two physical faces.
        ///
        /// <para><b>Milestone 8.75b moved the constant, and only the constant.</b> It was declared here
        /// at 8.75; the HUD has to know the same thing to say which of a card's two texts is live, and
        /// presentation may not reference <see cref="GuardsDigital.Core.Cards.Verified"/> at all
        /// (<c>VerifiedContentIsolationConformanceTests</c>). Two copies of one mapping could disagree
        /// silently about which face is which, on the hero whose entire identity is that question, so
        /// it is stated once in non-Verified Core - <see cref="CoinSelectedTextQuery.SunSide"/> - and
        /// read from there here. The user's ruling is unchanged and the value is unchanged.</para>
        /// </summary>
        public static TeamSide SunFace => CoinSelectedTextQuery.SunSide;

        /// <summary>The other face bears the moon (☾). USER-SETTLED 2026-09-22: "moon is blue team".</summary>
        public static TeamSide MoonFace => CoinSelectedTextQuery.MoonSide;

        /// <summary>
        /// Whether a revealed coin face is Ignatia's sun. Pure and total over the three
        /// <see cref="TeamSide"/> values so both the handler and its tests read the one decision. An
        /// unrevealed coin (<see cref="TeamSide.None"/>) is not her sun - her cards are never resolved
        /// with the coin unrevealed in a real match, and the handler refuses that case rather than
        /// guessing a face.
        /// </summary>
        public static bool RevealedFaceIsSun(TeamSide revealedSide) => revealedSide == SunFace;

        /// <summary>Whether a revealed face is one of the two the coin can show.</summary>
        public static bool IsRevealed(TeamSide revealedSide) =>
            revealedSide == SunFace || revealedSide == MoonFace;

        // -----------------------------------------------------------------------------------------
        // Equilibrium (Silver): "This round: each time you perform or repeat a primary action you may
        // apply either ☀ or ☾ card text, regardless of the Tie Breaker coin." It creates a round-scoped
        // marker on the acting hero; while that marker stands, the coin predicate below stops reading
        // the coin and asks the actor which face to apply.
        // -----------------------------------------------------------------------------------------

        public static EffectCapabilityId Equilibrium { get; } =
            new EffectCapabilityId("ignatia-equilibrium");

        public static EffectCapabilityReference EquilibriumV1 =>
            new EffectCapabilityReference(Equilibrium, 1);

        /// <summary>The typed kind the coin predicate looks for to know the round grants free choice.</summary>
        public const string FreeCoinChoiceKind = "IgnatiaFreeCoinChoice";

        private static readonly ActiveEffectCancellationCondition[] EquilibriumCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated
        };

        /// <summary>
        /// The round-scoped marker Equilibrium creates on the acting hero. It carries no rule
        /// modification of its own - it is a flag the coin predicate reads - so it stands from the
        /// moment Equilibrium resolves until the round ends.
        /// </summary>
        public static ActiveEffectDefinition FreeCoinChoiceDefinition { get; } =
            new ActiveEffectDefinition(
                "ignatia-free-coin-choice-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.RoundEnd,
                EquilibriumCancellations,
                false,
                FreeCoinChoiceKind,
                "ignatia-free-coin-choice",
                Array.Empty<TimingCheckpoint>());

        /// <summary>
        /// Empty until the dealing block. The gate keeps every Ignatia declaration out of the
        /// registered-capability list that snapshots pin.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>The real handlers, carried here until the dealing block registers them.</summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new CoinSunHandler(),
                new ActiveEffectCreationHandler(EquilibriumV1, FreeCoinChoiceDefinition),
                new FlipTieBreakerHandler()
            };

        // -----------------------------------------------------------------------------------------
        // Chaos Incarnate (Purple Ultimate): "The first time each turn after you perform a primary
        // action you may flip the Tie Breaker coin; if you do, you may perform that action again,
        // choosing different targets." The flip is the one thing on that card no contract in the
        // build could already do - milestone 8.75a.
        // -----------------------------------------------------------------------------------------

        public static EffectCapabilityId FlipTieBreaker { get; } =
            new EffectCapabilityId("ignatia-flip-tie-breaker");

        /// <summary><c>ignatia-flip-tie-breaker@1</c>: turn the match's Tie Breaker coin over.</summary>
        public static EffectCapabilityReference FlipTieBreakerV1 =>
            new EffectCapabilityReference(FlipTieBreaker, 1);

        /// <summary>
        /// Asks the match to turn its Tie Breaker coin over, and does nothing else.
        ///
        /// <para><b>It asks rather than acts, and the split is not this hero's choice.</b>
        /// <see cref="EffectCapabilityContext"/> is "immutable, and carries no Unity object, delegate,
        /// or continuation" in as many words - a handler decides what happens here and the frame owns
        /// everything else - so the coin, which is authoritative round state
        /// (<c>InitiativeResolutionAuthority</c>'s), cannot be reached from inside one. The handler
        /// publishes <see cref="TieBreakerFlipQuery.FlippedOutput"/>, the resolver stops on it exactly
        /// as it stops on an applied Push, and the authority holding the coin turns it over before the
        /// next node runs. That ordering is the whole point: the next node is the repeat, and what her
        /// repeat may target is decided by the face the coin is NOW showing.</para>
        ///
        /// <para><b>It is never impossible.</b> Rulebook p.5 gives the coin two faces and a match
        /// always has one showing, so "flip the coin" is a step that can always be taken - which is
        /// why this publishes rather than refusing, and why the `may` above it is the only question
        /// the sentence asks.</para>
        /// </summary>
        private sealed class FlipTieBreakerHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => FlipTieBreakerV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[]
                    {
                        new EffectOutputBinding(
                            GuardsDigital.Core.Rules.EffectResolution.TieBreakerFlipQuery
                                .FlippedOutput,
                            EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                context == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(
                            GuardsDigital.Core.Rules.EffectResolution.TieBreakerFlipQuery
                                .FlippedOutput,
                            EffectValue.Boolean(true))
                    });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        /// <summary>Whether the acting hero is under Equilibrium's round-long free-choice marker.</summary>
        private static bool ActorMayChooseTheFace(EffectCapabilityContext context) =>
            context.Timing != null && context.ActorId.IsDefined &&
            context.Timing.Effects.Any(effect =>
                string.Equals(effect.Definition.EffectKind, FreeCoinChoiceKind, StringComparison.Ordinal) &&
                effect.AffectedHeroId.Equals(context.ActorId));

        /// <summary>
        /// A pure predicate over the match's own Tie Breaker state: it mutates nothing, asks nobody, and
        /// publishes one boolean - whether the live coin face is her sun.
        /// </summary>
        private sealed class CoinSunHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => CoinSunV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[] { new EffectOutputBinding(SunOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Equilibrium's round: the actor picks the face, so the coin is not read at all.
                //
                // Milestone 8.75b DECLARED what the two integers mean. They are the ordinals of
                // PrintedTextFace - Sun is 1 and Moon is 2 - offered under
                // EffectChoiceValueVocabulary.PrintedTextFace, so the seat drawing this pause reads
                // "APPLY THE SUN TEXT" and "APPLY THE MOON TEXT" rather than the buttons "1" and "0".
                // The moon candidate moved off 0 for the reason CardColor.None sits there too: a
                // vocabulary's members are the values a chooser may pick, and an offered candidate that
                // is the default integer is the trap every optional Integer in this codebase carries.
                if (ActorMayChooseTheFace(context))
                {
                    return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        new[]
                        {
                            EffectValue.Integer((int)PrintedTextFace.Sun),
                            EffectValue.Integer((int)PrintedTextFace.Moon)
                        },
                        1,
                        1,
                        valueVocabulary: EffectChoiceValueVocabulary.PrintedTextFace));
                }

                if (!IsRevealed(context.TieBreakerRevealedSide))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        SunOutput,
                        EffectValue.Boolean(RevealedFaceIsSun(context.TieBreakerRevealedSide)))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Integer ||
                    (selected[0].IntegerValue != (int)PrintedTextFace.Sun &&
                        selected[0].IntegerValue != (int)PrintedTextFace.Moon))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        SunOutput,
                        EffectValue.Boolean(selected[0].IntegerValue == (int)PrintedTextFace.Sun))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                if (context == null)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                if (ActorMayChooseTheFace(context))
                {
                    return EffectExecutionResultKind.NeedsChoice;
                }

                return IsRevealed(context.TieBreakerRevealedSide)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }
        }
    }
}
