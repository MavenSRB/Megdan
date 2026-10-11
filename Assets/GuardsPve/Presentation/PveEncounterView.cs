using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// enemies' steps, shows each standing enemy's four-turn plan beside its token with its cards on
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
        private PveCardIndex cards;
        private UIDocument document;
        private VisualElement root;
        private VisualElement panel;
        private Label status;
        private Label promptBanner;
        private Label message;
        private VisualElement actions;
        private Label cardDetail;
        private readonly Dictionary<HeroId, Label> strips = new Dictionary<HeroId, Label>();
        private readonly Dictionary<HeroId, VisualElement> enemyRows = new Dictionary<HeroId, VisualElement>();
        private readonly Dictionary<HeroId, Sprite> enemyArt = new Dictionary<HeroId, Sprite>();
        private HeroId hoveredEnemy;
        private HeroId inspectedEnemy;
        private VisualElement inspectionAnchor;
        private ScrollView enemyRail;
        private HeroOverviewsUnityView sharedEnemyView;
        private HeroOverviewsViewModel lastRailModel;
        private float nextStepAt;
        private float presentationWaitStartedAt = -1f;
        private HeroId promptedEnemy;
        private string lastActionText = string.Empty;
        private long renderedVersion = -1;
        private string renderedPrompt;
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
            view.cards = new PveCardIndex(encounterSession.Encounter.Catalog);
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
            rail.style.top = 110;
            rail.style.bottom = 330;
            rail.style.width = 264;
            rail.style.backgroundColor = new Color(0.08f, 0.05f, 0.05f, 0.94f);
            root.Add(rail);
            var heading = Text(16, FontStyle.Bold, Color.white);
            heading.text = "ENEMIES";
            rail.Add(heading);
            enemyRail = rail;
            sharedEnemyView = new HeroOverviewsUnityView(() => session.Projection)
            {
                TurnSlotsOverride = hero => PlannedSlots(hero)
            };
            sharedEnemyView.HeroHighlighted += hero => runtime.BoardView.SetHoveredHero(hero);
            root.Add(sharedEnemyView.Root);
            foreach (var enemy in session.Encounter.Enemies)
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

            cardDetail = Text(13, FontStyle.Normal, Color.white);
            cardDetail.name = "pve-card-detail";
            cardDetail.style.position = Position.Absolute;
            cardDetail.style.backgroundColor = new Color(0.12f, 0.08f, 0.04f, 0.95f);
            cardDetail.style.paddingLeft = 8;
            cardDetail.style.paddingRight = 8;
            cardDetail.style.paddingTop = 6;
            cardDetail.style.paddingBottom = 6;
            cardDetail.style.whiteSpace = WhiteSpace.Normal;
            cardDetail.style.maxWidth = 360;
            cardDetail.style.display = DisplayStyle.None;
            cardDetail.pickingMode = PickingMode.Ignore;
            root.Add(cardDetail);
        }

        private void Update()
        {
            if (session == null) return;
            if (session.Status == PveEncounterStatus.Ongoing && Time.unscaledTime >= nextStepAt &&
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
            if (session == null) return;
            PlaceStrips();
            RefreshEnemyRail();
            AdaptHudRails();
        }

        private void InspectEnemy(PveEnemyRecord enemy, VisualElement anchor)
        {
            inspectedEnemy = enemy.HeroId;
            inspectionAnchor = anchor;
            hoveredEnemy = enemy.HeroId;
            runtime.BoardView.SetHoveredHero(enemy.HeroId);
            ShowCards(enemy, anchor);
        }

        private void ClearInspection()
        {
            inspectedEnemy = default;
            inspectionAnchor = null;
            hoveredEnemy = default;
            runtime.BoardView.SetHoveredHero(default);
            cardDetail.style.display = DisplayStyle.None;
        }

        private void OnBoardHeroHighlighted(HeroId hero)
        {
            hoveredEnemy = hero;
            sharedEnemyView?.SetHighlightedHero(hero);
        }

        private IReadOnlyList<HeroTurnSlotViewModel> PlannedSlots(HeroOverviewViewModel hero)
        {
            var enemy = session.Encounter.FindEnemy(hero.HeroId);
            if (enemy == null) return hero.TurnSlots;
            var match = session.Projection;
            var plan = PvePlans.PlanFor(session.Encounter, enemy, Math.Max(match.RoundIndex, enemy.Spawn.Round));
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
                    isResolving: match.IsTurnActive && match.SelectedActor.ActorId.Equals(enemy.HeroId) && turn == match.TurnIndex);
                slots.Add(new HeroTurnSlotViewModel(turn, face.Color, true, face));
            }
            return slots;
        }

        private void RefreshEnemyRail()
        {
            var match = session.Projection;
            var sourceModel = runtime.HeroOverviewsView.Model;
            if (sourceModel == null) return;
            if (!ReferenceEquals(sourceModel, lastRailModel))
            {
                lastRailModel = sourceModel;
                sharedEnemyView.Render(new HeroOverviewsViewModel(sourceModel.Version,
                    sourceModel.Heroes.Where(hero => session.Encounter.FindEnemy(hero.HeroId) != null)));
                var column = sharedEnemyView.Root.Q("gd-hero-rail-red") ?? enemyRail.Q("gd-hero-rail-red");
                enemyRail.Add(column);
                column.style.position = Position.Relative;
                column.style.left = 0;
                column.style.top = 0;
                column.style.width = Length.Percent(100);
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
                    var reading = Text(12, FontStyle.Normal, Color.white);
                    reading.name = "pve-enemy-reading";
                    row.Add(reading);
                    if (enemyArt.TryGetValue(hero, out var sprite))
                        row.Q("gd-hero-icon").style.backgroundImage = new StyleBackground(sprite);
                    // Enemies have neither items nor a coin reward; keep their identity, slots and discard UI.
                    row.Q("gd-bonus-strip").style.display = DisplayStyle.None;
                    var worth = row.Q("gd-hero-worth");
                    if (worth != null) worth.style.display = DisplayStyle.None;
                    enemyRows[hero] = row;
                }
            }
            sharedEnemyView.Root.style.width = root.resolvedStyle.width;
            sharedEnemyView.Root.style.height = root.resolvedStyle.height;
            sharedEnemyView.Root.BringToFront();
            foreach (var enemy in session.Encounter.Enemies)
            {
                var row = enemyRows[enemy.HeroId];
                var standing = match.Heroes.Any(value => value.HeroId.Equals(enemy.HeroId));
                var defeated = match.Scoreboard.Any(value => value.HeroId.Equals(enemy.HeroId) && value.Deaths > 0);
                var acting = match.IsTurnActive && match.SelectedActor.ActorId.Equals(enemy.HeroId);
                var state = standing ? (acting ? "Acting" : "On board") : defeated ? "Defeated" : "Spawn round " + enemy.Spawn.Round;
                var defenses = match.Cards.Count(card => card.OwnerId.Equals(enemy.HeroId) &&
                    card.State.Location == CardLocation.Hand && cards.Purple(card.DefinitionReference.DefinitionId) != null);
                row.Q<Label>("pve-enemy-reading").text = state + " | Level " + enemy.Type.Level +
                    " | " + defenses + " defense";
                row.style.opacity = defeated && !standing ? 0.6f : 1f;
                row.style.backgroundColor = acting || hoveredEnemy.Equals(enemy.HeroId) || promptedEnemy.Equals(enemy.HeroId)
                    ? new Color(0.45f, 0.24f, 0.08f, 1f) : new Color(0.17f, 0.09f, 0.08f, 1f);
                if (enemyArt.TryGetValue(enemy.HeroId, out var art))
                {
                    foreach (var view in runtime.BoardView.HeroViews.Where(value => value != null && value.Occupancy.HeroId.Equals(enemy.HeroId)))
                        view.SetTokenFaceOverride(art.texture);
                }
            }
            if (inspectedEnemy.IsDefined && inspectionAnchor != null)
                ShowCards(session.Encounter.FindEnemy(inspectedEnemy), inspectionAnchor);
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.BoardView.HeroHighlighted -= OnBoardHeroHighlighted;
            if (document != null && document.panelSettings != null) Destroy(document.panelSettings);
        }

        private void RenderPanel()
        {
            var match = session.Projection;
            var prompt = session.CurrentPrompt;
            var promptText = prompt?.Text;
            var lost = session.Status == PveEncounterStatus.Lost;
            status.text = lost
                ? "ENCOUNTER LOST - every creep is defeated"
                : $"PvE  Round {match.RoundIndex}  Turn {match.TurnIndex}  -  seed {session.Encounter.Setup.Seed}";
            status.style.color = lost ? new Color(1f, 0.4f, 0.35f) : Color.white;
            promptedEnemy = prompt?.Enemy ?? default;
            promptBanner.style.display = prompt == null || lost ? DisplayStyle.None : DisplayStyle.Flex;
            promptBanner.text = prompt == null ? string.Empty : PromptBannerText(match, prompt);
            if (renderedVersion == match.Version && renderedPrompt == promptText && !lost)
            {
                message.text = MessageFor(match, prompt);
                return;
            }

            renderedVersion = match.Version;
            renderedPrompt = promptText;
            message.text = MessageFor(match, prompt);
            actions.Clear();
            if (lost)
            {
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

            var pending = match.PendingSystemTransitionStep;
            if (pending != null && MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out _))
            {
                foreach (var item in pending.OpenItems.Where(value => value.ChoosingSide == TeamSide.Blue))
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
                foreach (var hero in session.Encounter.Heroes)
                {
                    var commitment = match.Commitments.FirstOrDefault(value => value.HeroId.Equals(hero));
                    if (!commitment.HeroId.IsDefined || commitment.Status != TurnCardCommitmentStatus.AwaitingResponse) continue;
                    var level = match.HeroProgression.FirstOrDefault(value => value.HeroId.Equals(hero))?.Level ?? 1;
                    var command = DirectorCommand.BeginSystemTransition(MatchModeRules.LevelGrantTransitionId(hero));
                    if (session.Refusal(command) != null) continue;
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
            if (session.Status == PveEncounterStatus.Lost) return "Undo or restart from the Director drawer, or use the buttons below.";
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

            var next = session.Peek();
            var acting = next.Kind == PveStepKind.Command ? "Enemies acting... " : "Your move. ";
            return acting + lastActionText;
        }

        private void Submit(DirectorCommand command)
        {
            var result = session.Execute(command);
            lastActionText = result.Accepted ? string.Empty : "Refused: " + result.Detail;
            renderedVersion = -1;
            runtime.RefreshAll();
        }

        private void PlaceStrips()
        {
            var match = session.Projection;
            var camera = Camera.main;
            var views = runtime.BoardView.HeroViews.Where(view => view != null).ToArray();
            foreach (var enemy in session.Encounter.Enemies)
            {
                if (!strips.TryGetValue(enemy.HeroId, out var strip))
                {
                    strip = Text(12, FontStyle.Bold, Color.white);
                    strip.name = "pve-plan-" + enemy.HeroId.Value;
                    strip.style.position = Position.Absolute;
                    strip.style.backgroundColor = enemy.Spawn.Round > 1
                        ? new Color(0.36f, 0.2f, 0.06f, 0.9f)
                        : new Color(0.85f, 0.42f, 0.08f, 0.9f);
                    strip.style.paddingLeft = 4;
                    strip.style.paddingRight = 4;
                    var captured = enemy;
                    strip.RegisterCallback<PointerEnterEvent>(_ => InspectEnemy(captured, strip));
                    strip.RegisterCallback<PointerLeaveEvent>(_ => ClearInspection());
                    root.Add(strip);
                    strips[enemy.HeroId] = strip;
                }

                var standing = match.Heroes.Any(value => value.HeroId.Equals(enemy.HeroId));
                var view = views.FirstOrDefault(value => value.Occupancy.HeroId.Equals(enemy.HeroId) && value.gameObject.activeInHierarchy);
                if (!standing || view == null || camera == null || root.panel == null)
                {
                    strip.style.display = DisplayStyle.None;
                    continue;
                }

                var screen = camera.WorldToScreenPoint(view.transform.position);
                if (screen.z < 0)
                {
                    strip.style.display = DisplayStyle.None;
                    continue;
                }

                var point = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
                strip.style.display = DisplayStyle.Flex;
                // W-026. The enemy a standing prompt decides for is marked on the board as well.
                var edge = promptedEnemy.Equals(enemy.HeroId) ? 2f : 0f;
                var edgeColour = new Color(1f, 0.95f, 0.3f, 1f);
                strip.style.borderTopWidth = edge;
                strip.style.borderBottomWidth = edge;
                strip.style.borderLeftWidth = edge;
                strip.style.borderRightWidth = edge;
                strip.style.borderTopColor = edgeColour;
                strip.style.borderBottomColor = edgeColour;
                strip.style.borderLeftColor = edgeColour;
                strip.style.borderRightColor = edgeColour;
                strip.style.left = point.x - 60;
                strip.style.top = point.y - 52;
                strip.text = PlanText(match, enemy);
            }
        }

        private string PlanText(IMatchReadModel match, PveEnemyRecord enemy)
        {
            var plan = PvePlans.PlanFor(session.Encounter, enemy, Math.Max(match.RoundIndex, enemy.Spawn.Round));
            var slots = plan.Select((key, index) =>
            {
                var card = enemy.Type.FindOrange(key);
                var text = card == null ? "?" : $"{card.Initiative}{ActionGlyph(card)}";
                return match.RoundIndex >= enemy.Spawn.Round && index + 1 == match.TurnIndex ? "[" + text + "]" : text;
            });
            return enemy.Ordinal.ToString(CultureInfo.InvariantCulture) + ": " + string.Join(" ", slots);
        }

        private static string ActionGlyph(PveOrangeCard card)
        {
            if (card.IsIdle) return "z";
            var attack = card.Options.Any(option => option.Kind == PveOptionKind.Attack);
            var move = card.Options.Any(option => option.Kind == PveOptionKind.Move);
            return attack && move ? "AM" : attack ? "A" : "M";
        }

        private void ShowCards(PveEnemyRecord enemy, VisualElement anchor)
        {
            var lines = new List<string> { enemy.DisplayName + " (level " + enemy.Type.Level + ")" };
            foreach (var card in enemy.Type.OrangeCards)
            {
                var copies = card.Copies > 1 ? " x" + card.Copies : string.Empty;
                if (card.IsIdle)
                {
                    lines.Add($"{card.Key}{copies} {card.Name}: Init {card.Initiative}, idle");
                    continue;
                }

                var values = new List<string> { "Init " + card.Initiative };
                if (card.AttackValue.HasValue)
                    values.Add("Atk " + card.AttackValue + (card.AttackRange > 0 ? " range " + card.AttackRange : " adjacent"));
                if (card.MoveValue.HasValue) values.Add("Move " + card.MoveValue);
                var options = string.Join(", then ", card.Options.Select(option =>
                    (option.Kind == PveOptionKind.Attack ? "attack " : "move to ") + option.Target.ToString().ToLowerInvariant()));
                lines.Add($"{card.Key}{copies} {card.Name}: {string.Join(", ", values)} - {options}");
            }

            foreach (var card in enemy.Type.PurpleCards)
            {
                lines.Add($"{card.Key} {card.Name}: Defense {card.Defense} (purple)");
            }

            cardDetail.text = string.Join("\n", lines);
            var bounds = anchor.worldBound;
            cardDetail.style.left = Mathf.Clamp(bounds.xMax + 8, 0, Mathf.Max(0, root.resolvedStyle.width - 360));
            cardDetail.style.top = Mathf.Clamp(bounds.yMin, 0, Mathf.Max(0, root.resolvedStyle.height - 300));
            cardDetail.style.display = DisplayStyle.Flex;
            cardDetail.BringToFront();
        }

        /// <summary>
        /// The existing HUD lays its rails out for three heroes a side: the enemies' rail is hidden
        /// (their plans are shown beside their tokens) and a Blue rail of more than three heroes is
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
            var heroes = session.Encounter.Heroes.Count;
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
            var enemy = session.Encounter.FindEnemy(hero);
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
