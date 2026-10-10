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
        public const string LauncherSceneName = "PveLauncher";

        private ConfiguredMatchSceneRuntime runtime;
        private PveEncounterSession session;
        private PveCardIndex cards;
        private UIDocument document;
        private VisualElement root;
        private VisualElement panel;
        private Label status;
        private Label message;
        private VisualElement actions;
        private Label cardDetail;
        private readonly Dictionary<HeroId, Label> strips = new Dictionary<HeroId, Label>();
        private float nextStepAt;
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
            if (session.Status == PveEncounterStatus.Ongoing && Time.unscaledTime >= nextStepAt)
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

        private void LateUpdate()
        {
            if (session == null) return;
            PlaceStrips();
            AdaptHudRails();
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
                    actions.Add(ActionButton(AnswerLabel(match, command), () => Submit(command)));
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
                    strip.RegisterCallback<PointerEnterEvent>(_ => ShowCards(captured, strip));
                    strip.RegisterCallback<PointerLeaveEvent>(_ => cardDetail.style.display = DisplayStyle.None);
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
                strip.style.left = point.x - 60;
                strip.style.top = point.y - 52;
                strip.text = PlanText(match, enemy);
            }
        }

        private string PlanText(IMatchReadModel match, PveEnemyRecord enemy)
        {
            var plan = PvePlans.PlanFor(session.Encounter, enemy, match.RoundIndex);
            var slots = plan.Select((key, index) =>
            {
                var card = enemy.Type.FindOrange(key);
                var text = card == null ? "?" : $"{card.Initiative}{ActionGlyph(card)}";
                return index + 1 == match.TurnIndex ? "[" + text + "]" : text;
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
            cardDetail.style.left = anchor.resolvedStyle.left;
            cardDetail.style.top = anchor.resolvedStyle.top + 22;
            cardDetail.style.display = DisplayStyle.Flex;
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

        private string AnswerLabel(IMatchReadModel match, DirectorCommand command)
        {
            switch (command.Kind)
            {
                case DirectorCommandKind.ChooseAttackTarget:
                    return command.HeroId.IsDefined ? "Target " + HeroName(match, command.HeroId) : "Target creep " + command.MinionId.Value;
                case DirectorCommandKind.ChooseMovementDestination:
                    return "Move to " + command.Coordinate.Q + "," + command.Coordinate.R;
                case DirectorCommandKind.AnswerSystemTransitionStep:
                    return "Enter at " + SpaceOf(command.SystemTransitionChoice.OptionId);
                case DirectorCommandKind.ChooseTiedActor:
                    var owner = match.Cards.FirstOrDefault(value => value.Id.Equals(command.CardInstanceId))?.OwnerId ?? default;
                    return HeroName(match, owner) + " first";
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
