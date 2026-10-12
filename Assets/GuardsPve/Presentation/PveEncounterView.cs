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
    /// PVE-D39: paces enemy steps and draws their compact public cards. Placement and endings
    /// use the shared scene surfaces; only Blue-owned shared choices require this host control.
    /// </summary>
    public sealed class PveEncounterView : MonoBehaviour
    {
        private const float SpentEnemyCardOpacity = .35f;

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
        private IMatchReadModel ReadModel => coop ? runtime.ReadModel : session.Projection;
        private PveCardIndex cards;
        private UIDocument document;
        private VisualElement root;
        private readonly Dictionary<HeroId, VisualElement> enemyRows = new Dictionary<HeroId, VisualElement>();
        private readonly Dictionary<HeroId, Sprite> enemyArt = new Dictionary<HeroId, Sprite>();
        private HeroId hoveredEnemy;
        private ScrollView enemyRail;
        // Reserve the shared tiebreaker banner even while Blue holds it.
        private static float EnemyRailTop => ConfiguredMatchHudRenderer
            .CalculateTieBreakerRailBannerRect(0f, 0f, TeamSide.Red).yMax + 4f;
        private const float EnemyRailBottomClearance = 330f;
        private const float EnemyRowHeight = 64f;
        private const float EnemyRowGap = 5f;
        private HeroOverviewsUnityView sharedEnemyView;
        private HeroOverviewsViewModel lastRailModel;
        private float nextStepAt;
        private float presentationWaitStartedAt = -1f;
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
            PveEncounterSession authority, Func<DirectorCommand, bool> sharedSubmit)
        {
            var view = new GameObject("PvE Co-op Encounter").AddComponent<PveEncounterView>();
            view.runtime = runtime;
            view.encounter = encounter;
            view.session = authority;
            view.coop = true;
            view.cards = new PveCardIndex(encounter.Catalog);
            if (authority != null)
                runtime.DecisionWindowPresenter.UseTiedInitiativeAuthority(sharedSubmit);
            view.Build();
            return view;
        }

        private void Build()
        {
            PveEnemyArtwork.Configure(encounter.Catalog);
            runtime.VictoryScreenPresenter.ModeEnding = projection => PveEnemyController.IsLost(projection)
                ? VictoryScreenPresenter.DeriveModeEnding(projection, TeamSide.Blue, "ALL FRIENDLY CREEPS WERE DEFEATED") : null;
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

            enemyRail = rail;
            sharedEnemyView = new HeroOverviewsUnityView(() => ReadModel)
            {
                TurnSlotsOverride = hero => PlannedSlots(hero)
            };
            sharedEnemyView.HeroHighlighted += hero => runtime.BoardView.SetHoveredHero(hero);
            root.Add(sharedEnemyView.Root);
            runtime.ModeCardPipsOverride = EnemyCardPips;
            runtime.ModeCardPipInspection = CanInspectEnemyCardPip;
            foreach (var enemy in encounter.Enemies)
                if (enemy.Type.TokenType.HasValue)
                    enemyArt[enemy.HeroId] = StandardMinionArtCatalog.Load(TeamSide.Red, enemy.Type.TokenType.Value);
            runtime.BoardView.HeroHighlighted += OnBoardHeroHighlighted;

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
                        runtime.RefreshAll();
                    }
                    else
                    {
                        Debug.LogWarning("Enemy step refused: " + result?.Detail);
                        nextStepAt = Time.unscaledTime + 2f;
                    }
                }
            }
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
            root.style.display = runtime.VictoryScreenView.Model == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (runtime.VictoryScreenView.Model != null) return;
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
                // Resolved slots use the actual card immediately (including a PVE-D21 replacement),
                // even while the rest of the current turn is still resolving.
                var actual = hero.TurnSlots.FirstOrDefault(value => value.TurnIndex == turn);
                if (actual != null && actual.IsOccupied)
                {
                    slots.Add(actual);
                    continue;
                }
                var face = new TrayCardViewModel(instance?.Id ?? new CardInstanceId(enemy.HeroId.Value + "-plan-" + turn),
                    definition.Reference, definition.DisplayName, PveEnemyArtwork.DisplayColor(enemy.Type, orange),
                    CurrentRoundCards.ReadFace(definition), TrayCardGroup.Available, 0, false, false,
                    isActiveEffectSource: instance != null && match.StandingActiveEffectSourceCardInstanceIds.Contains(instance.Id),
                    isResolving: match.IsTurnActive && match.SelectedActor.ActorId.Equals(enemy.HeroId) && turn == match.TurnIndex);
                slots.Add(new HeroTurnSlotViewModel(turn, face.Color, true, face));
            }
            return slots;
        }

        private IReadOnlyList<HeroCardPip> EnemyCardPips(HeroId hero)
        {
            if (encounter.FindEnemy(hero) == null) return null;
            var overview = runtime.HeroOverviewsView?.Model?.Heroes
                .FirstOrDefault(value => value.HeroId.Equals(hero));
            if (overview == null) return Array.Empty<HeroCardPip>();

            var pips = PlannedSlots(overview)
                .Where(slot => slot.IsOccupied && slot.PlayedCard != null &&
                    slot.PlayedCard.Group != TrayCardGroup.Played)
                .Select(slot => new HeroCardPip(
                    slot.PlayedCard.Color,
                    slot.PlayedCard.DefinitionReference))
                .ToList();
            pips.AddRange(ReadModel.Cards
                .Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand &&
                    cards.Purple(card.DefinitionReference.DefinitionId) != null)
                .OrderBy(card => card.Id.Value)
                .Select(card => new HeroCardPip(
                    PveEnemyArtwork.DisplayColor(cards.Purple(card.DefinitionReference.DefinitionId)),
                    card.DefinitionReference)));
            return pips;
        }

        private bool CanInspectEnemyCardPip(HeroId hero, CardDefinitionReference reference) =>
            EnemyCardPips(hero)?.Any(pip => pip.DefinitionReference.Equals(reference)) == true;

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
                    var enemy = encounter.FindEnemy(hero);
                    row.name = "pve-enemy-" + hero.Value;
                    var model = sourceModel.Heroes.First(value => value.HeroId.Equals(hero));
                    var planned = PlannedSlots(model);
                    var miniatures = row.Query<VisualElement>("gd-turn-slot").ToList();
                    for (var index = 0; index < miniatures.Count; index++)
                    {
                        var miniature = miniatures[index];
                        var plannedCard = cards.Orange(planned[index].PlayedCard.DefinitionReference.DefinitionId);
                        if (plannedCard != null)
                            miniature.style.backgroundColor = MatchHudTheme.Face(
                                PveEnemyArtwork.DisplayColor(enemy.Type, plannedCard));
                        miniature.style.opacity = planned[index].PlayedCard.Group == TrayCardGroup.Played
                            ? SpentEnemyCardOpacity
                            : 1f;
                        var initiative = Text(13, FontStyle.Bold, Color.black);
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
            enemyRail.style.height = Mathf.Min(enemyRows.Count * (EnemyRowHeight + EnemyRowGap), availableHeight);
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
                row.style.backgroundColor = acting || hoveredEnemy.Equals(enemy.HeroId)
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
                    PveEnemyArtwork.DisplayColor(cards.Purple(card.DefinitionReference.DefinitionId)),
                    CurrentRoundCards.ReadFace(definition),
                    available ? TrayCardGroup.Available : TrayCardGroup.Discarded, 0, false, false);
                var miniature = sharedEnemyView.BuildPublicCardMiniature("pve-defense-card", face);
                miniature.style.backgroundColor = MatchHudTheme.Face(face.Color);
                miniature.style.width = 20;
                miniature.style.marginLeft = 2;
                miniature.style.opacity = available ? 1f : SpentEnemyCardOpacity;
                miniature.userData = card.Id;
                var value = Text(13, FontStyle.Bold, Color.white);
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
            if (runtime != null)
            {
                runtime.BoardView.HeroHighlighted -= OnBoardHeroHighlighted;
                runtime.ModeCardPipsOverride = null;
                runtime.ModeCardPipInspection = null;
            }
            if (document != null && document.panelSettings != null) Destroy(document.panelSettings);
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
