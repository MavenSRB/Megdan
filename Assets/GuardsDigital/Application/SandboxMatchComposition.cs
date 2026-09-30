using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Composes a staged situation into an ordinary match. Milestone 8.35z17.
    ///
    /// <para><b>The load-a-staged-description entry point the amendment asks for.</b> The staged
    /// situation becomes the session's own <see cref="MatchSessionInitialState"/>, which is the
    /// whole design: from here on there is nothing sandbox-shaped left. The session, the host, the
    /// director client, Undo, restart, the event log, snapshots and deterministic replay are the
    /// ones every dealt match already uses, and none of them knows a staged board from a dealt one.
    /// A restart returns to the STAGED board rather than to a dealt one, which is what a tester
    /// setting up a situation and playing it repeatedly actually wants, and it falls out of putting
    /// the staging in the initial state rather than beside it.</para>
    ///
    /// <para><b>Director-only, by construction rather than by rule.</b> The amendment requires the
    /// staging surface never to be offered to seats and never to enter the networked protocol.
    /// Nothing here is a command, a message kind or a submission: staging happens strictly before an
    /// authority exists, so there is no moment at which a seat could reach it and nothing for a
    /// codec to carry. <see cref="ComposeDirectorClient"/> returns the Director's own unscoped
    /// client - the same <see cref="LocalDirectorMatchClient"/> the Director's controls already use
    /// - and this type deliberately offers no seat-scoped arm at all.</para>
    /// </summary>
    public static class SandboxMatchComposition
    {
        /// <summary>
        /// The staged initial state: the description applied to the dealt setup, and the hands
        /// staged into <paramref name="configuration"/> in place.
        /// </summary>
        /// <remarks>
        /// The configuration is mutated rather than copied because that is how a staged hand
        /// carries: <see cref="InitialScenarioDescription"/> clones each card instance with its
        /// state, so the scenario built below records exactly the hand positions staging produced,
        /// and every rebuild reproduces them. A caller that needs the dealt configuration afterwards
        /// composes a fresh one.
        /// </remarks>
        public static MatchSessionInitialState Compose(
            SandboxStagingDescription description,
            BoardMapDefinition map,
            QuickGameSetupState setup,
            MatchConfiguration configuration,
            IEnumerable<BoardMapDefinition> availableMaps = null,
            IEnumerable<ObstacleOccupancy> initialObstacles = null,
            IEnumerable<HeroOccupancy> additionalHeroBodies = null,
            IEnumerable<HeroBodySupply> heroBodySupplies = null)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var staged = SandboxStagingAuthority.Stage(description, map, setup, configuration);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                staged.Setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))
                    // Milestone 9.21i. A Director fixture's further bodies of a staged hero (body 2 and
                    // up), beside the supplies that declare him many-bodied - 9.21a's staging seam.
                    .Concat(additionalHeroBodies ?? Array.Empty<HeroOccupancy>()),
                initialObstacles ?? Array.Empty<ObstacleOccupancy>(),
                heroBodySupplies);
            return new MatchSessionInitialState(
                scenario,
                staged.Setup,
                availableMaps ?? new[] { map },
                configuration,
                null,
                staged.Progression);
        }

        /// <summary>
        /// The staged match behind the Director's own client, which is the surface the sandbox
        /// plays through. It is <see cref="MatchHost.Compose"/> and
        /// <see cref="LocalDirectorMatchClient"/> exactly as the ordinary scene composes them, so
        /// every seat the match has is answered by the Director and no seat client is bound.
        /// </summary>
        public static LocalDirectorMatchClient ComposeDirectorClient(
            SandboxStagingDescription description,
            BoardMapDefinition map,
            QuickGameSetupState setup,
            MatchConfiguration configuration,
            IEnumerable<BoardMapDefinition> availableMaps = null) =>
            new LocalDirectorMatchClient(
                MatchHost.Compose(
                    Compose(description, map, setup, configuration, availableMaps)));
    }
}
