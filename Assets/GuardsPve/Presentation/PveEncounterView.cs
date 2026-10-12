using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Play;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>
    /// The PvE encounter on top of the existing match scene (PVE-D05/D10/D14/D16/D04): paces the
    /// enemies' steps, shows each enemy's four-turn plan in a compact rail with its cards on
    /// hover, offers the round-start placement and granted levels, asks the player to settle enemy
    /// ties and reports the loss. Every command goes through the encounter session.
    /// </summary>
    public sealed class PveEncounterView : MonoBehaviour
    {
        /// <summary>Seconds between enemy steps; the launch smoke shortens it.</summary>
        public float StepSeconds { get; set; } = 0.45f;

        /// <summary>
        /// W-026. Whether an enemy step waits for the board's animations and attack spotlight to
        /// settle - for at most <see cref="MaxPresentationWaitSeconds"/> - so a step is never drawn on
        /// top of the one before it. The launch smoke, which draws nothing, turns it off.
        /// </summary>
        public bool WaitsForPresentation { get; set; } = true;

        public const float MaxPresentationWaitSeconds = 3f;
        public const string LauncherSceneName = "PveLauncher";

        private ConfiguredMatchSceneRuntime runtime;
        private PveEncounterSession session;
        private PveComposedEncounter encounter;
        private bool coop;
        private Action<DirectorCommand> sharedSubmit;
        private Func<DirectorCommand, bool> playerSubmit;
        private IMatchReadModel ReadModel => coop ? runtime.ReadModel : session.Projection;
        private PveCardIndex cards;
        private UIDocument document;
        private VisualElement root;
        private VisualElement panel;
        private Label status;
        private Label promptBanner;
        private Label message;
        private VisualElement actions;
        private readonly Dictionary<HeroId, VisualElement> enemyRows = new Dictionary<HeroId, VisualElement>();
        private readonly Dictionary<HeroId, Sprite> enemyArt = new Dictionary<HeroId, Sprite>();
        private HeroId hoveredEnemy;
        private ScrollView enemyRail;
        private const float EnemyRailTop = 110f;
        private const float EnemyRailBottomClearance = 330f;
        private const float EnemyRailHeadingHeight = 24f;
        private const float EnemyRowHeight = 64f;
        private const float EnemyRowGap = 5f;
        private HeroOverviewsUnityView sharedEnemyView;
        private HeroOverviewsViewModel lastRailModel;
        private float nextStepAt;
        private float presentationWaitStartedAt = -1f;
        private HeroId promptedEnemy;
        private string lastActionText = string.Empty;
        private long renderedVersion = -1;
        private string renderedPrompt;
        private SeatId renderedSeat;
        private ConfiguredMatchHudRenderer hudRenderer;

        public PveEncounterSession Session => session;

        /// <summary>Attaches the view to a freshly composed runtime whose session is the encounter's.</summary>
        public static PveEncounterView Attach(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            var encounterSession = runtime.Session as PveEncounterSession
                ?? throw new InvalidOperationException("The composed runtime is not a PvE encounter.");
            var view = new GameObject("PvE Encounter").AddComponent<PveEncounterView>();
            view.runtime = runtime;
            view.session = encounterSession;
            view.encounter = encounterSession.Encounter;
            view.cards = new PveCardIndex(encounterSession.Encounter.Catalog);
            view.Build();
            return view;
        }

        public static PveEncounterView AttachCoop(ConfiguredMatchSceneRuntime runtime, PveComposedEncounter encounter,
            PveEncounterSession authority, Action<DirectorCommand> sharedSubmit, Func<DirectorCommand, bool> playerSubmit)
        {
            var view = new GameObject("PvE Co-op Encounter").AddComponent<PveEncounterView>();
            view.runtime = runtime;
            view.encounter = encounter;
            view.session = authority;
            view.coop = true;
            view.sharedSubmit = sharedSubmit;
            view.playerSubmit = playerSubmit;
            view.cards = new PveCardIndex(encounter.Catalog);
            view.Build();
            return view;
        }

        private void Build()
        {
            document = gameObject.AddComponent<UIDocument>();
            var authored = Resources.Load<PanelSettings>(ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath);
            var settings = authored != null ? Instantiate(authored) : ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Guards PvE Overlay Panel";
            settings.hideFlags = HideFlags.HideAndDontSave;
            MatchHudPanelScaling.Apply(settings);
            settings.clearColor = false;
            settings.sortingOrder = 300f;
            var theme = Resources.Load<ThemeStyleSheet>(ConfiguredMatchHudRenderer.RuntimeThemeResourcePath);
            if (theme != null) settings.themeStyleSheet = theme;
            document.panelSettings = settings;
            root = document.rootVisualElement;
            if (root == null)
            {
                document.enabled = false;
                document.enabled = true;
                root = document.rootVisualElement;
            }

            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            var rail = new ScrollView(ScrollViewMode.Vertical) { name = "pve-enemy-rail" };
            rail.style.position = Position.Absolute;
            rail.style.left = 12;
            rail.style.top = EnemyRailTop;
            rail.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            rail.style.width = 264;
            rail.style.backgroundColor = new Color(0.08f, 0.05f, 0.05f, 0.94f);
            root.Add(rail);
            var heading = Text(16, FontStyle.Bold, Color.white);
            heading.text = "ENEMIES";
            heading.style.height = EnemyRailHeadingHeight;
            heading.style.flexShrink = 0;
            heading.style.marginTop = heading.style.marginBottom = 0;
            rail.Add(heading);
            enemyRail = rail;
            sharedEnemyView = new HeroOverviewsUnityView(() => ReadModel)
            {
                TurnSlotsOverride = hero => PlannedSlots(hero)
            };
            sharedEnemyView.HeroHighlighted += hero => runtime.BoardView.SetHoveredHero(hero);
            root.Add(sharedEnemyView.Root);
            foreach (var enemy in encounter.Enemies)
                if (enemy.Type.TokenType.HasValue)
                    enemyArt[enemy.HeroId] = StandardMinionArtCatalog.Load(TeamSide.Red, enemy.Type.TokenType.Value);
            runtime.BoardView.HeroHighlighted += OnBoardHeroHighlighted;

            panel = new VisualElement { name = "pve-panel" };
            panel.style.position = Position.Absolute;
            panel.style.left = 290;
            panel.style.top = 110;
            panel.style.width = 420;
            panel.style.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 0.88f);
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            root.Add(panel);

            status = Text(15, FontStyle.Bold, Color.white);
            status.name = "pve-status";
            panel.Add(status);
            // W-026. An enemy's tie is the player's to settle (PVE-D10/D12/D20); this says so in so
            // many words, so it never reads as a decision of the player's own hero.
            promptBanner = Text(14, FontStyle.Bold, Color.black);
            promptBanner.name = "pve-prompt-banner";
            promptBanner.style.backgroundColor = new Color(1f, 0.62f, 0.15f, 1f);
            promptBanner.style.paddingLeft = 6;
            promptBanner.style.paddingRight = 6;
            promptBanner.style.paddingTop = 3;
            promptBanner.style.paddingBottom = 3;
            promptBanner.style.marginTop = 4;
            promptBanner.style.whiteSpace = WhiteSpace.Normal;
            promptBanner.style.display = DisplayStyle.None;
            panel.Add(promptBanner);
            message = Text(13, FontStyle.Normal, new Color(1f, 0.85f, 0.4f));
            message.name = "pve-message";
            message.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(message);
            actions = new VisualElement { name = "pve-actions" };
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexWrap = Wrap.Wrap;
            panel.Add(actions);
        }

        private void Update()
        {
            if (runtime == null) return;
            if (!coop && session.Status == PveEncounterStatus.Ongoing && Time.unscaledTime >= nextStepAt &&
                PresentationSettled())
            {
                var result = session.Step(out var step);
                if (step.Kind == PveStepKind.Command)
                {
                    nextStepAt = Time.unscaledTime + StepSeconds;
                    if (result != null && result.Accepted)
                    {
                        lastActionText = step.Note ?? step.Command.Kind.ToString();
                        runtime.RefreshAll();
                    }
                    else
                    {
                        lastActionText = "Enemy step refused: " + result?.Detail;
                        nextStepAt = Time.unscaledTime + 2f;
                    }
                }
            }

            RenderPanel();
        }

        /// <summary>
        /// W-026. Whether the board has finished drawing the last step, or has been waited on long
        /// enough. When the next step is not the enemies' own there is nothing to wait for.
        /// </summary>
        private bool PresentationSettled()
        {
            if (!WaitsForPresentation || session.Peek().Kind != PveStepKind.Command || runtime.IsClockInputReady)
            {
                presentationWaitStartedAt = -1f;
                return true;
            }

            if (presentationWaitStartedAt < 0f) presentationWaitStartedAt = Time.unscaledTime;
            if (Time.unscaledTime - presentationWaitStartedAt < MaxPresentationWaitSeconds) return false;
            presentationWaitStartedAt = -1f;
            return true;
        }

        private void LateUpdate()
        {
            if (runtime == null) return;
            RefreshEnemyRail();
            AdaptHudRails();
        }

        private void OnBoardHeroHighlighted(HeroId hero)
        {
            hoveredEnemy = hero;
            sharedEnemyView?.SetHighlightedHero(hero);
        }

        private IReadOnlyList<HeroTurnSlotViewModel> PlannedSlots(HeroOverviewViewModel hero)
        {
            var enemy = encounter.FindEnemy(hero.HeroId);
            if (enemy == null) return hero.TurnSlots;
            var match = ReadModel;
            var plan = PvePlans.PlanFor(encounter, enemy, Math.Max(match.RoundIndex, enemy.Spawn.Round));
            var copies = new Dictionary<string, int>();
            var slots = new List<HeroTurnSlotViewModel>();
            for (var turn = 1; turn <= plan.Count; turn++)
            {
                var key = plan[turn - 1];
                copies.TryGetValue(key, out var copy);
                copies[key] = ++copy;
                var orange = enemy.Type.FindOrange(key);
                var definitionId = PveCardFactory.OrangeId(enemy.Type, orange, copy);
                var definition = match.Definitions.First(value => value.Id.Equals(definitionId));
                var instance = match.Cards.FirstOrDefault(value => value.OwnerId.Equals(enemy.HeroId) &&
                    value.DefinitionReference.DefinitionId.Equals(definitionId));
                // Past slots use the actual resolved card (including a PVE-D21 replacement).
                var actual = hero.TurnSlots.FirstOrDefault(value => value.TurnIndex == turn);
                if (turn < match.TurnIndex && actual != null && actual.IsOccupied)
                {
                    slots.Add(actual);
                    continue;
                }
                var face = new TrayCardViewModel(instance?.Id ?? new CardInstanceId(enemy.HeroId.Value + "-plan-" + turn),
                    definition.Reference, definition.DisplayName, definition.Progression.Color,
                    CurrentRoundCards.ReadFace(definition), TrayCardGroup.Available, 0, false, false,
                    isActiveEffectSource: instance != null && match.StandingActiveEffectSourceCardInstanceIds.Contains(instance.Id),
                    isResolving: match.IsTurnActive && match.SelectedActor.ActorId.Equals(enemy.HeroId) && turn == match.TurnIndex);
                slots.Add(new HeroTurnSlotViewModel(turn, face.Color, true, face));
            }
            return slots;
        }

        private void RefreshEnemyRail()
        {
            var match = ReadModel;
            var sourceModel = runtime.HeroOverviewsView.Model;
            if (sourceModel == null) return;
            if (!ReferenceEquals(sourceModel, lastRailModel))
            {
                lastRailModel = sourceModel;
                sharedEnemyView.Render(new HeroOverviewsViewModel(sourceModel.Version,
                    sourceModel.Heroes.Where(hero => encounter.FindEnemy(hero.HeroId) != null &&
                        match.Heroes.Any(standing => standing.HeroId.Equals(hero.HeroId)))));
                var column = sharedEnemyView.Root.Q("gd-hero-rail-red") ?? enemyRail.Q("gd-hero-rail-red");
                enemyRail.Add(column);
                column.style.position = Position.Relative;
                column.style.left = 0;
                column.style.top = 0;
                column.style.width = Length.Percent(100);
                column.style.flexShrink = 0;
                enemyRows.Clear();
                foreach (var row in column.Children())
                {
                    var hero = (HeroId)row.userData;
                    row.name = "pve-enemy-" + hero.Value;
                    var model = sourceModel.Heroes.First(value => value.HeroId.Equals(hero));
                    var planned = PlannedSlots(model);
                    var miniatures = row.Query<VisualElement>("gd-turn-slot").ToList();
                    for (var index = 0; index < miniatures.Count; index++)
                    {
                        // PvE's typed orange plan cards have no tabletop progression colour.
                        // Decorate the shared miniature without assigning a rules-facing colour.
                        var miniature = miniatures[index];
                        if (cards.Orange(planned[index].PlayedCard.DefinitionReference.DefinitionId) != null)
                            miniature.style.backgroundColor = new Color(0.9f, 0.44f, 0.09f);
                        var initiative = Text(11, FontStyle.Bold, Color.black);
                        initiative.name = "pve-plan-initiative";
                        initiative.text = planned[index].PlayedCard?.Face.Initiative?.AmountLabel ?? "-";
                        initiative.pickingMode = PickingMode.Ignore;
                        initiative.style.unityTextAlign = TextAnchor.MiddleCenter;
                        miniature.Add(initiative);
                    }
                    if (enemyArt.TryGetValue(hero, out var sprite))
                        row.Q("gd-hero-icon").style.backgroundImage = new StyleBackground(sprite);
                    CompactEnemyRow(row, hero);
                    enemyRows[hero] = row;
                }
            }
            // Fit the live rows, retaining the existing hand clearance only as an overflow cap.
            var availableHeight = Mathf.Max(0f, root.resolvedStyle.height - EnemyRailTop - EnemyRailBottomClearance);
            enemyRail.style.height = Mathf.Min(EnemyRailHeadingHeight + enemyRows.Count * (EnemyRowHeight + EnemyRowGap), availableHeight);
            enemyRail.style.display = enemyRows.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            sharedEnemyView.Root.style.width = root.resolvedStyle.width;
            sharedEnemyView.Root.style.height = root.resolvedStyle.height;
            sharedEnemyView.Root.BringToFront();
            foreach (var enemy in encounter.Enemies)
            {
                if (!enemyRows.TryGetValue(enemy.HeroId, out var row)) continue;
                var standing = match.Heroes.Any(value => value.HeroId.Equals(enemy.HeroId));
                var defeated = match.Scoreboard.Any(value => value.HeroId.Equals(enemy.HeroId) && value.Deaths > 0);
                var acting = match.IsTurnActive && match.SelectedActor.ActorId.Equals(enemy.HeroId);
                var state = standing ? (acting ? "Acting" : "On board") : defeated ? "Defeated" : "Spawn round " + enemy.Spawn.Round;
                row.tooltip = enemy.DisplayName + " — " + state;
                row.style.opacity = defeated && !standing ? 0.6f : 1f;
                row.style.backgroundColor = acting || hoveredEnemy.Equals(enemy.HeroId) || promptedEnemy.Equals(enemy.HeroId)
                    ? new Color(0.45f, 0.24f, 0.08f, 1f) : new Color(0.17f, 0.09f, 0.08f, 1f);
                if (enemyArt.TryGetValue(enemy.HeroId, out var art))
                {
                    foreach (var view in runtime.BoardView.HeroViews.Where(value => value != null && value.Occupancy.HeroId.Equals(enemy.HeroId)))
                        view.SetTokenFaceOverride(art.texture);
                }
            }
        }

        private void CompactEnemyRow(VisualElement row, HeroId hero)
        {
            // Keep the shared token, turn miniatures, hover anchors and row identity handlers.
            // Hero progression, current-card plaque and discard regions do not belong in this rail.
            var icon = row.Q("gd-hero-icon");
            var turns = row.Q("gd-turn-slots");
            icon.RemoveFromHierarchy();
            turns.RemoveFromHierarchy();
            row.Clear();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = EnemyRowHeight;
            row.style.minHeight = EnemyRowHeight;
            row.style.flexShrink = 0;
            row.style.paddingTop = row.style.paddingBottom = 4;
            row.style.paddingLeft = row.style.paddingRight = 3;
            row.style.marginBottom = EnemyRowGap;
            icon.style.width = icon.style.height = 36;
            icon.style.minWidth = 36;
            icon.style.flexShrink = 0;
            row.Add(icon);
            turns.style.flexGrow = 1;
            turns.style.minWidth = 0;
            turns.style.marginTop = 0;
            turns.style.marginLeft = 3;
            turns.style.paddingLeft = turns.style.paddingRight = 0;
            foreach (var active in turns.Query<Label>("gd-turn-slot-active").ToList())
                active.style.display = DisplayStyle.None;
            foreach (var outline in turns.Query<VisualElement>("gd-turn-slot-outline").ToList())
                outline.style.paddingLeft = outline.style.paddingRight = 0;
            row.Add(turns);

            var defenses = ReadModel.Cards.Where(card => card.OwnerId.Equals(hero) &&
                cards.Purple(card.DefinitionReference.DefinitionId) != null).OrderBy(card => card.Id.Value).ToArray();
            if (defenses.Length == 0) return;
            var defenseSlots = new VisualElement { name = "pve-defense-slots" };
            defenseSlots.style.flexDirection = FlexDirection.Row;
            defenseSlots.style.marginLeft = 4;
            defenseSlots.style.flexShrink = 0;
            row.Add(defenseSlots);
            foreach (var card in defenses)
            {
                var definition = ReadModel.Definitions.First(value => value.Reference.Equals(card.DefinitionReference));
                var available = card.State.Location == CardLocation.Hand;
                var face = new TrayCardViewModel(card.Id, definition.Reference, definition.DisplayName,
                    definition.Progression.Color, CurrentRoundCards.ReadFace(definition),
                    available ? TrayCardGroup.Available : TrayCardGroup.Discarded, 0, false, false);
                var miniature = sharedEnemyView.BuildPublicCardMiniature("pve-defense-card", face);
                // Purple is PvE presentation, not a tabletop progression colour.
                miniature.style.backgroundColor = new Color(.48f, .22f, .68f);
                miniature.style.width = 20;
                miniature.style.marginLeft = 2;
                miniature.style.opacity = available ? 1f : .35f;
                miniature.userData = card.Id;
                var value = Text(11, FontStyle.Bold, Color.white);
                value.name = "pve-defense-value";
                value.text = cards.Purple(card.DefinitionReference.DefinitionId).Defense.ToString();
                value.style.unityTextAlign = TextAnchor.MiddleCenter;
                value.pickingMode = PickingMode.Ignore;
                miniature.Add(value);
                defenseSlots.Add(miniature);
            }
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.BoardView.HeroHighlighted -= OnBoardHeroHighlighted;
            if (document != null && document.panelSettings != null) Destroy(document.panelSettings);
        }

        private void RenderPanel()
        {
            var match = ReadModel;
            var prompt = session?.CurrentPrompt;
            var promptText = prompt?.Text;
            var lost = PveEnemyController.IsLost(ReadModel);
            status.text = lost
                ? "ENCOUNTER LOST - every creep is defeated"
                : $"PvE  Round {match.RoundIndex}  Turn {match.TurnIndex}  -  seed {encounter.Setup.Seed}";
            status.style.color = lost ? new Color(1f, 0.4f, 0.35f) : Color.white;
            promptedEnemy = prompt?.Enemy ?? default;
            promptBanner.style.display = prompt == null || lost ? DisplayStyle.None : DisplayStyle.Flex;
            promptBanner.text = prompt == null ? string.Empty : PromptBannerText(match, prompt);
            var currentSeat = runtime.Seat?.Id ?? default;
            if (renderedVersion == match.Version && renderedPrompt == promptText && renderedSeat.Equals(currentSeat) && !lost)
            {
                message.text = MessageFor(match, prompt);
                return;
            }

            renderedVersion = match.Version;
            renderedPrompt = promptText;
            renderedSeat = currentSeat;
            message.text = MessageFor(match, prompt);
            actions.Clear();
            if (lost)
            {
                if (coop) return;
                actions.Add(ActionButton("Restart (same seed)", () => { session.Restart(); runtime.RefreshAll(); }));
                actions.Add(ActionButton("New setup", () => SceneManager.LoadScene(LauncherSceneName, LoadSceneMode.Single)));
                return;
            }

            if (prompt != null)
            {
                foreach (var answer in prompt.Answers)
                {
                    var command = answer;
                    actions.Add(ActionButton(AnswerLabel(match, prompt, command), () => Submit(command)));
                }

                return;
            }

            if (coop && session != null && session.Projection.DecisionContract != null &&
                PveCoopMatch.IsSharedChoice(session.Projection))
            {
                foreach (var choice in session.Projection.DecisionContract.Commands)
                {
                    var command = choice;
                    var owner = match.Cards.FirstOrDefault(card => card.Id.Equals(command.CardInstanceId))?.OwnerId ?? default;
                    var label = command.Kind == DirectorCommandKind.ChooseTiedActor ? HeroName(match, owner) + " acts first" : command.Kind.ToString();
                    actions.Add(ActionButton(label, () => Submit(command)));
                }
                return;
            }
            var pending = match.PendingSystemTransitionStep;
            if (pending != null && MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out _))
            {
                foreach (var item in pending.OpenItems.Where(value => value.ChoosingSide == TeamSide.Blue &&
                    (!coop || (ModeRoundStartPlacementRules.TryReadItem(value.ItemId, out var owner) && runtime.Seat.Owns(owner)))))
                {
                    foreach (var option in item.Options)
                    {
                        var command = DirectorCommand.AnswerSystemTransitionStep(new SystemTransitionChoiceCommand(
                            pending.InstanceId, pending.StepId, item.ItemId, TeamSide.Blue, option));
                        actions.Add(ActionButton("Place at " + SpaceOf(option), () => Submit(command)));
                    }
                }

                return;
            }

            if (pending == null && match.Phase == RoundResolutionPhase.AwaitingCommitments && match.TurnIndex == 1)
            {
                foreach (var hero in encounter.Heroes)
                {
                    if (coop && !runtime.Seat.Owns(hero)) continue;
                    var commitment = match.Commitments.FirstOrDefault(value => value.HeroId.Equals(hero));
                    if (!commitment.HeroId.IsDefined || commitment.Status != TurnCardCommitmentStatus.AwaitingResponse) continue;
                    var level = match.HeroProgression.FirstOrDefault(value => value.HeroId.Equals(hero))?.Level ?? 1;
                    var command = DirectorCommand.BeginSystemTransition(MatchModeRules.LevelGrantTransitionId(hero));
                    if (!coop && session.Refusal(command) != null) continue;
                    actions.Add(ActionButton($"+1 level {HeroName(match, hero)} (Lv {level})", () => Submit(command)));
                }
            }
        }

        private string PromptBannerText(IMatchReadModel match, PvePrompt prompt) =>
            prompt.Enemy.IsDefined
                ? "ENEMY DECISION - you choose for " + HeroName(match, prompt.Enemy).ToUpperInvariant()
                : "ENEMY DECISION - you choose for the enemies";

        private string MessageFor(IMatchReadModel match, PvePrompt prompt)
        {
            if (PveEnemyController.IsLost(ReadModel)) return "Undo or restart from the Director drawer, or use the buttons below.";
            if (prompt != null) return prompt.Text;
            var pending = match.PendingSystemTransitionStep;
            if (pending != null && MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out _))
            {
                var item = pending.OpenItems.FirstOrDefault(value => value.ChoosingSide == TeamSide.Blue);
                if (item != null && ModeRoundStartPlacementRules.TryReadItem(item.ItemId, out var hero))
                {
                    return "Place " + HeroName(match, hero) + " on a placement space.";
                }
            }

            if (coop) return "Your hero's choices are yours. The host resolves shared and enemy decisions.";
            var next = session.Peek();
            var acting = next.Kind == PveStepKind.Command ? "Enemies acting... " : "Your move. ";
            return acting + lastActionText;
        }

        private void Submit(DirectorCommand command)
        {
            if (coop)
            {
                if (session != null && (session.CurrentPrompt != null ||
                    PveCoopMatch.IsSharedChoice(session.Projection)))
                    sharedSubmit(command);
                else lastActionText = playerSubmit(command) ? string.Empty : "This choice is not currently available to your hero.";
                renderedVersion = -1;
                return;
            }
            var result = session.Execute(command);
            lastActionText = result.Accepted ? string.Empty : "Refused: " + result.Detail;
            renderedVersion = -1;
            runtime.RefreshAll();
        }


        /// <summary>
        /// The existing HUD lays its rails out for three heroes a side: the enemies' rail is hidden
        /// (replaced by the compact public enemy rail) and a Blue rail of more than three heroes is
        /// scaled to fit.
        /// </summary>
        private void AdaptHudRails()
        {
            if (hudRenderer == null) hudRenderer = FindAnyObjectByType<ConfiguredMatchHudRenderer>();
            if (hudRenderer == null) return;
            var hudDocument = hudRenderer.GetComponent<UIDocument>();
            var hudRoot = hudDocument == null ? null : hudDocument.rootVisualElement;
            if (hudRoot == null) return;
            var red = hudRoot.Q("gd-hero-rail-red");
            if (red != null) red.style.display = DisplayStyle.None;
            var blue = hudRoot.Q("gd-hero-rail-blue");
            var heroes = encounter.Heroes.Count;
            if (blue != null && heroes > 3)
            {
                var scale = 3f / heroes;
                blue.style.scale = new Scale(new Vector3(scale, scale, 1f));
                blue.style.transformOrigin = new TransformOrigin(Length.Percent(100), Length.Percent(0), 0);
            }
        }

        private string AnswerLabel(IMatchReadModel match, PvePrompt prompt, DirectorCommand command)
        {
            var enemy = prompt.Enemy.IsDefined ? HeroName(match, prompt.Enemy) + " " : string.Empty;
            switch (command.Kind)
            {
                case DirectorCommandKind.ChooseAttackTarget:
                    return enemy + "attacks " + (command.HeroId.IsDefined
                        ? HeroName(match, command.HeroId)
                        : "creep " + command.MinionId.Value);
                case DirectorCommandKind.ChooseMovementDestination:
                    return enemy + "moves to " + command.Coordinate.Q + "," + command.Coordinate.R;
                case DirectorCommandKind.AnswerSystemTransitionStep:
                    return enemy + "enters at " + SpaceOf(command.SystemTransitionChoice.OptionId);
                case DirectorCommandKind.ChooseTiedActor:
                    var owner = match.Cards.FirstOrDefault(value => value.Id.Equals(command.CardInstanceId))?.OwnerId ?? default;
                    return HeroName(match, owner) + " acts first";
                default:
                    return command.Kind.ToString();
            }
        }

        private string HeroName(IMatchReadModel match, HeroId hero)
        {
            var enemy = encounter.FindEnemy(hero);
            if (enemy != null) return enemy.DisplayName;
            var setup = match.Setup.Heroes.FirstOrDefault(value => value.HeroId.Equals(hero));
            return setup?.DisplayName ?? hero.Value;
        }

        private static string SpaceOf(SystemTransitionOptionId option) =>
            ModeRoundStartPlacementRules.TryReadOption(option, out var space) ? space.Q + "," + space.R : option.Value;

        private static Button ActionButton(string text, Action click)
        {
            var button = new Button(click) { text = text };
            button.style.fontSize = 13;
            button.style.marginRight = 4;
            button.style.marginTop = 4;
            return button;
        }

        private static Label Text(int size, FontStyle style, Color color)
        {
            var label = new Label();
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = style;
            label.style.color = color;
            return label;
        }
    }
}
