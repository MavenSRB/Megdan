using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Drives one scripted interaction inside a built player so the player-build smoke gate can
    /// prove the artifact actually runs, not merely that it compiled and linked.
    /// </summary>
    /// <remarks>
    /// This exists because a build that succeeds can still ship a player that resolves no shader,
    /// loads no map resource, or strips a type it needs at runtime - none of which any Editor test
    /// can observe. Something inside the player has to reach a configured match and say so.
    ///
    /// It is inert unless the player is launched with <see cref="CommandLineFlag"/>, and
    /// <see cref="MatchSceneCompositionRoot"/> only attaches it when that flag is present, so a
    /// normally launched build never constructs it. Being attached through AddComponent by
    /// production code is also what keeps it reachable for the living-document validator's
    /// component-reachability check; it must not be authored into the saved scene instead.
    ///
    /// Every marker is written with an unmistakable prefix because the gate script reads the player
    /// log as its only channel - a player has no test runner and no NUnit XML.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerSmokeTestDriver : MonoBehaviour
    {
        public const string CommandLineFlag = "-guardsSmokeTest";
        public const string MarkerPrefix = "GUARDS_SMOKE:";
        public const string PassMarker = MarkerPrefix + " PASS";
        public const string FailMarker = MarkerPrefix + " FAIL";

        private MatchSceneCompositionRoot compositionRoot;

        public static bool IsRequested()
        {
            return System.Environment.GetCommandLineArgs()
                .Any(argument => string.Equals(argument, CommandLineFlag, System.StringComparison.Ordinal));
        }

        public void Configure(MatchSceneCompositionRoot root)
        {
            compositionRoot = root;
        }

        private void Start()
        {
            var exitCode = RunScriptedInteraction();
            Debug.Log($"{MarkerPrefix} EXIT {exitCode}");
            UnityEngine.Application.Quit(exitCode);
        }

        private int RunScriptedInteraction()
        {
            Debug.Log($"{MarkerPrefix} BEGIN");

            if (compositionRoot == null)
            {
                return Fail("the composition root was never handed to the smoke driver");
            }

            var scene = gameObject.scene;
            Debug.Log($"{MarkerPrefix} SCENE {scene.name}");

            var session = compositionRoot.Session;
            if (session == null)
            {
                return Fail("the composition root produced no match session, so the scene never composed");
            }

            var projection = session.Projection;
            Debug.Log($"{MarkerPrefix} COMPOSED heroes={projection.Heroes.Count} " +
                $"cards={projection.Cards.Count} version={projection.Version}");

            if (projection.Heroes.Count == 0)
            {
                return Fail("the configured match reached no heroes, so the map resource did not load");
            }

            // A real typed command through the real port. Committing a director card is the
            // cheapest command that changes authoritative state without needing a turn to be in
            // progress, and its acceptance proves the whole Core/Application/Presentation stack
            // survived the build rather than merely loading.
            var card = projection.Cards.FirstOrDefault(value => value.State.Location == CardLocation.Hand);
            if (card == null)
            {
                return Fail("no in-hand card was projected, so no command could be scripted");
            }

            var versionBefore = projection.Version;
            var result = session.Execute(DirectorCommand.CommitCard(card.OwnerId, card.Id));
            if (!result.Accepted)
            {
                return Fail($"the scripted CommitCard was rejected: {result.RejectionReason}");
            }

            var after = session.Projection;
            if (after.Version <= versionBefore)
            {
                return Fail($"the projection version did not advance past {versionBefore}");
            }

            var commitment = after.GetCommitment(card.OwnerId);
            if (commitment.Status == TurnCardCommitmentStatus.AwaitingResponse)
            {
                return Fail("the commitment status did not move off AwaitingResponse");
            }

            Debug.Log($"{MarkerPrefix} COMMAND_ACCEPTED kind=CommitCard " +
                $"version={after.Version} commitment={commitment.Status}");
            Debug.Log(PassMarker);
            return 0;
        }

        private static int Fail(string reason)
        {
            Debug.LogError($"{FailMarker} {reason}");
            return 1;
        }
    }
}
