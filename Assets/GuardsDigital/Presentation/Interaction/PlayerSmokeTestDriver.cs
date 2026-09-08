using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.UIElements;

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

        /// <summary>
        /// Two, because the second is the one that proves something new. Milestone 7.6 established that
        /// an ordinary end of round only stops on p.15's Upgrade a Card from the second round onward, so
        /// a one-round path would never meet the pause this gate exists to answer.
        /// </summary>
        private const int RoundsToPlay = 2;

        /// <summary>
        /// Generous for two rounds of the configured match and small enough that a player which stops
        /// making progress fails inside the gate's timeout instead of hanging.
        /// </summary>
        private const int CommandBudget = 400;

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

        private IEnumerator Start()
        {
            var exitCode = RunScriptedInteraction();
            if (exitCode == 0 && System.Environment.GetCommandLineArgs().Contains("-guardsArtworkGallery"))
            {
                UnityEngine.Application.runInBackground = true;
                try
                {
                    compositionRoot.Hud.RootElement.Add(BuildArtworkInspection());
                    Debug.Log($"{MarkerPrefix} ARTWORK_GALLERY_READY fronts=10 catalog={ConfiguredCardArtworkCatalog.Catalog.Entries.Count}");
                }
                catch (System.Exception exception) { exitCode = Fail(exception.Message); }
                // Opt-in, bounded inspection window; the ordinary smoke and normal player never wait here.
                if (exitCode == 0) yield return new WaitForSecondsRealtime(60f);
            }
            Debug.Log($"{MarkerPrefix} EXIT {exitCode}");
            UnityEngine.Application.Quit(exitCode);
        }

        /// <summary>The shared Editor/player artwork probe. Never attached by the normal game.</summary>
        public static VisualElement BuildArtworkInspection()
        {
            foreach (var entry in ConfiguredCardArtworkCatalog.Catalog.Entries)
                if (!ConfiguredCardArtwork.TryLoadTexture(entry.Reference, out var texture, out _) ||
                    texture.width != 372 || texture.height != 520)
                    throw new System.InvalidOperationException("Private artwork missing or malformed: " + entry.ResourcePath);

            var gallery = new VisualElement { name = "gd-private-artwork-verification" };
            gallery.style.position = Position.Absolute;
            gallery.style.left = 400f;
            gallery.style.top = 120f;
            gallery.style.flexDirection = FlexDirection.Row;
            gallery.style.backgroundColor = MatchHudTheme.TopBarBackground;
            var heroes = new[]
            {
                (Name: "Dodger", Cards: DodgerCardContent.DealtDefinitions),
                (Name: "Misa", Cards: MisaCardContent.DealtDefinitions),
                (Name: "Silverarrow", Cards: SilverarrowCardContent.DealtDefinitions),
                (Name: "Ursafar", Cards: UrsafarCardContent.DealtDefinitions),
                (Name: "Whisper", Cards: WhisperCardContent.DealtDefinitions)
            };
            foreach (var hero in heroes)
            {
                var column = new VisualElement();
                column.style.width = CardFootprint.Width + 20f;
                column.style.flexShrink = 0f;
                column.Add(new Label(hero.Name) { style = { color = Color.white, fontSize = 20f } });
                gallery.Add(column);
                foreach (var tier in new[] { CardTier.None, CardTier.II })
                {
                    var definition = hero.Cards.First(card => card.Progression.Tier == tier);
                    var card = CurrentRoundCards.DeckCard(
                        new CardInstanceId("artwork-check-" + definition.Id.Value), definition);
                    var face = DirectorHandsUnityView.BuildStandaloneCard(card);
                    face.userData = definition.Reference;
                    column.Add(face);
                }
            }
            return gallery;
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

            // Milestone 7.9. One accepted command proves the stack survived the build; it does not
            // prove the built player can be played. Milestone 7.6 recorded exactly why that matters -
            // p.8's Pity Coin leaves every hero able to afford the first level, so from the second round
            // on an ordinary end of round stops on p.15's Upgrade a Card, and "a built player that
            // reaches an end of round it cannot answer completes nothing". So the smoke path now plays
            // whole rounds and answers every pause it meets on the way.
            var rounds = PlayCompleteRounds(session, RoundsToPlay);
            if (rounds != 0)
            {
                return rounds;
            }

            Debug.Log(PassMarker);
            return 0;
        }

        /// <summary>
        /// Plays whole rounds of the composed match, answering whichever decision the authoritative
        /// projection is waiting on, and reports each round and each answered pause as its own marker.
        /// </summary>
        /// <remarks>
        /// Deterministic without a seed: every decision is answered with its own first projected option,
        /// so one build always produces one log. That is what a smoke gate wants and what the seeded
        /// generator in the Edit Mode suite deliberately is not - correctness of the loop is proved
        /// there, across many seeds, on a tier that can assert. What can only be proved here is that the
        /// same loop runs at all inside a built player, where a stripped type or an unresolved resource
        /// is a possibility for the first time.
        ///
        /// It never derives legality. Each branch reads the pending decision's own projected invocations
        /// or the offers the projection published, exactly as the HUD does, so a smoke path cannot pass
        /// by doing something the rules would have refused a player.
        /// </remarks>
        private int PlayCompleteRounds(IMatchSession session, int rounds)
        {
            var target = session.Projection.RoundIndex + rounds;
            var pauses = 0;
            var battles = 0;

            for (var command = 0; command < CommandBudget; command++)
            {
                var projection = session.Projection;
                if (projection.Phase == RoundResolutionPhase.GameEnded)
                {
                    Debug.Log($"{MarkerPrefix} MATCH_ENDED cause={projection.Outcome.Cause}");
                    return 0;
                }

                if (projection.RoundIndex >= target)
                {
                    Debug.Log($"{MarkerPrefix} ROUNDS_PLAYED rounds={rounds} " +
                        $"roundIndex={projection.RoundIndex} pausesAnswered={pauses} " +
                        $"minionBattles={battles} version={projection.Version}");
                    return 0;
                }

                var next = NextCommand(projection, ref pauses, ref battles);
                if (next == null)
                {
                    return Fail(
                        $"the projection offered no legal command in phase {projection.Phase} " +
                        $"at round {projection.RoundIndex}, turn {projection.TurnIndex}");
                }

                var accepted = session.Execute(next);
                if (!accepted.Accepted)
                {
                    return Fail(
                        $"a projected {next.Kind} was refused by the rules: {accepted.Detail}");
                }
            }

            return Fail($"the smoke path ran out of its {CommandBudget}-command budget before " +
                $"completing {rounds} rounds");
        }

        /// <summary>
        /// The first option of whichever decision is pending, or the plainest way to advance when none
        /// is. Returns null only when nothing at all is offered, which the caller reports as a failure.
        /// </summary>
        private static DirectorCommand NextCommand(
            MatchSessionProjection projection, ref int pauses, ref int battles)
        {
            var decision = projection.DecisionContract;
            if (decision != null)
            {
                if (decision.FamilyId.Equals(PendingDecisionFamilies.SystemTransition))
                {
                    // Milestone 8.35u made p.15's leveling simultaneous: a step may now be waiting on
                    // several heroes' choices at once, and PendingSystemTransitionStep's single-item
                    // ItemId/ChoosingSide/Options accessors throw rather than guess which one "the"
                    // pause means. OpenItems/AllOptions never throw and are exactly what a caller that
                    // only wants to log "what is on the table" needs; the command itself still comes
                    // from decision.Commands below, which names its own item on every entry and answers
                    // one open item per call - so a concurrent pause is walked one hero at a time here,
                    // same as everywhere else that is not the specific chooser's own screen.
                    var step = projection.PendingSystemTransitionStep;
                    pauses++;
                    Debug.Log($"{MarkerPrefix} PAUSE_ANSWERED transition={step.DefinitionId.Value} " +
                        $"step={step.StepId.Value} items={step.OpenItems.Count} " +
                        $"options={step.AllOptions.Count}");
                }

                if (decision.FamilyId.Equals(PendingDecisionFamilies.MinionBattle))
                {
                    var confirm = decision.Commands.FirstOrDefault(command =>
                        command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals);
                    if (confirm != null)
                    {
                        battles++;
                        Debug.Log($"{MarkerPrefix} MINION_BATTLE removals=" +
                            $"{projection.PendingMinionBattleChoice.SelectedMinionIds.Count}");
                        return confirm;
                    }

                    return decision.Commands.FirstOrDefault(command =>
                        command.Kind == DirectorCommandKind.SelectMinionBattleRemoval);
                }

                if (decision.FamilyId.Equals(PendingDecisionFamilies.Movement))
                {
                    return decision.Commands.FirstOrDefault(command =>
                            command.Kind == DirectorCommandKind.StopMovement) ??
                        decision.Commands.FirstOrDefault();
                }

                if (decision.FamilyId.Equals(PendingDecisionFamilies.Effect))
                {
                    return decision.Commands.FirstOrDefault(command =>
                            command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            command.EffectChoice.Kind == EffectChoiceCommandKind.Stop) ??
                        decision.Commands.FirstOrDefault(command =>
                            command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            command.EffectChoice.Kind == EffectChoiceCommandKind.Select) ??
                        decision.Commands.FirstOrDefault(command =>
                            command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm) ??
                        decision.Commands.FirstOrDefault(command =>
                            command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
                }

                return decision.Commands.FirstOrDefault(command =>
                    command.Kind != DirectorCommandKind.DeselectMinionBattleRemoval &&
                    !(command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        command.EffectChoice.Kind == EffectChoiceCommandKind.Deselect));
            }

            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                return DirectorCommand.AdvanceEndOfRound();
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                foreach (var commitment in projection.Commitments)
                {
                    if (commitment.Status != TurnCardCommitmentStatus.AwaitingResponse)
                    {
                        continue;
                    }

                    var card = projection.Cards.FirstOrDefault(value =>
                        value.OwnerId.Equals(commitment.HeroId) &&
                        value.State.Location == CardLocation.Hand);
                    return card != null
                        ? DirectorCommand.CommitCard(commitment.HeroId, card.Id)
                        : commitment.CanPass
                            ? DirectorCommand.Pass(commitment.HeroId)
                            : null;
                }

                return null;
            }

            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined)
            {
                return null;
            }

            // A Hold rather than an action: p.7 derives it from the selected unresolved card, it is
            // legal on every turn this path reaches, and spending the turn on it is what keeps the
            // smoke path short enough to run inside a build's start-up.
            // Milestone 8.7: read from the projection rather than re-derived here, so the smoke path
            // takes the same Hold the authority offers.
            var hold = projection.AvailableHoldOffer;
            return hold.HasValue && hold.Value.IsAvailable
                ? DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId)
                : null;
        }

        private static int Fail(string reason)
        {
            Debug.LogError($"{FailMarker} {reason}");
            return 1;
        }
    }
}
