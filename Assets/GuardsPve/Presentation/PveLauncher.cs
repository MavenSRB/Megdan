using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>
    /// The PvE executable's first scene: load and validate the content beside the executable, let the
    /// player choose heroes and a seed (PVE-D02/D13), compose the encounter and hand it to the
    /// existing match scene, which builds its views, HUD and runtime as it does for every match.
    /// </summary>
    public sealed class PveLauncher : MonoBehaviour
    {
        public const string MatchSceneName = "Milestone1_2_HexGrid";
        public const string SmokeFlag = "-guardsPveSmokeTest";

        private readonly List<string> chosen = new List<string>();
        private PveEnemyCatalog catalog;
        private PveEncounterDefinition encounter;
        private GuardsDigital.Core.Board.BoardMapDefinition map;
        private VisualElement root;
        private Label countLabel;
        private Label problemLabel;
        private TextField seedField;
        private readonly Dictionary<string, Button> heroButtons = new Dictionary<string, Button>();

        /// <summary>The encounter the last Start composed; read by tests and the smoke.</summary>
        public static PveComposedEncounter LastComposed { get; private set; }

        /// <summary>The overlay attached to the last composed encounter.</summary>
        public static PveEncounterView LastView { get; private set; }

        private PveCardEditor editor;

        private void Start()
        {
            string loadProblem = null;
            try
            {
                PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out catalog, out encounter);
                map = OfficialBoardMapCatalog.Load().Load(encounter.MapReference);
                var problems = PveContentValidator.Validate(catalog, encounter, map);
                if (problems.Count > 0) loadProblem = string.Join("\n", problems);
            }
            catch (Exception exception)
            {
                loadProblem = exception.Message;
            }

            if (Environment.GetCommandLineArgs().Contains(SmokeFlag))
            {
                PveSmokeRunner.Run(this, loadProblem);
                return;
            }

            BuildScreen(loadProblem);
        }

        /// <summary>
        /// Compose the encounter and load the match scene. Returns the problems that prevented it,
        /// or none when the scene is loading.
        /// </summary>
        public IReadOnlyList<string> StartEncounter(
            IEnumerable<string> heroIdValues, ulong seed, Action<ConfiguredMatchSceneRuntime> composed = null)
        {
            if (catalog == null || encounter == null || map == null)
                return new[] { "PvE content did not load." };
            PveComposedEncounter composedEncounter;
            try
            {
                composedEncounter = PveEncounterComposer.Compose(
                    new PveEncounterSetup(heroIdValues, seed), encounter, catalog, map);
            }
            catch (PveSetupException exception)
            {
                return exception.Problems;
            }

            LastComposed = composedEncounter;
            MatchSceneCompositionRoot.NextComposedExternalMatch = new ExternalMatchComposition(
                composedEncounter.InitialState,
                runtime =>
                {
                    LastView = PveEncounterView.Attach(runtime);
                    composed?.Invoke(runtime);
                },
                (host, director) => new GuardsDigital.Pve.Play.PveEncounterSession(composedEncounter, host, director),
                // W-026 (PVE-D02). The player directs Blue; the HUD never follows or offers an enemy.
                GuardsDigital.Core.Participants.TeamSide.Blue);
            SceneManager.LoadScene(MatchSceneName, LoadSceneMode.Single);
            return Array.Empty<string>();
        }

        private void BuildScreen(string loadProblem)
        {
            var document = gameObject.AddComponent<UIDocument>();
            var authored = Resources.Load<PanelSettings>(ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath);
            var settings = authored != null ? Instantiate(authored) : ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Guards PvE Launcher Panel";
            settings.hideFlags = HideFlags.HideAndDontSave;
            MatchHudPanelScaling.Apply(settings);
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

            root.Clear();
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            var column = new VisualElement();
            column.style.width = Length.Percent(80);
            column.style.maxWidth = 1100;
            root.Add(column);

            column.Add(Text("GUARDS PvE - TEST ENCOUNTER", 40, FontStyle.Bold, Color.white));
            column.Add(Text("Defend the three creeps. Enemy content is placeholder test data.", 20,
                FontStyle.Normal, new Color(0.75f, 0.78f, 0.82f)));

            problemLabel = Text(string.Empty, 18, FontStyle.Bold, new Color(1f, 0.45f, 0.4f));
            problemLabel.style.whiteSpace = WhiteSpace.Normal;
            if (loadProblem != null)
            {
                problemLabel.text = "Content problem:\n" + loadProblem;
                column.Add(problemLabel);
                return;
            }

            countLabel = Text(string.Empty, 22, FontStyle.Bold, Color.white);
            countLabel.style.marginTop = 24;
            column.Add(countLabel);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginTop = 8;
            column.Add(grid);
            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                var value = hero.HeroIdValue;
                var button = new Button(() => Toggle(value)) { text = hero.DisplayName };
                button.name = "pve-hero-" + value;
                button.style.width = 250;
                button.style.height = 44;
                button.style.fontSize = 16;
                button.style.marginRight = 6;
                button.style.marginBottom = 6;
                heroButtons[value] = button;
                grid.Add(button);
            }

            var seedRow = new VisualElement();
            seedRow.style.flexDirection = FlexDirection.Row;
            seedRow.style.alignItems = Align.Center;
            seedRow.style.marginTop = 20;
            column.Add(seedRow);
            seedRow.Add(Text("Seed", 20, FontStyle.Bold, Color.white));
            seedField = new TextField { value = PveSeed.Draw().ToString(CultureInfo.InvariantCulture) };
            seedField.name = "pve-seed";
            seedField.style.width = 320;
            seedField.style.fontSize = 18;
            seedField.style.marginLeft = 12;
            seedRow.Add(seedField);
            var reroll = new Button(() => seedField.value = PveSeed.Draw().ToString(CultureInfo.InvariantCulture))
            {
                text = "New seed"
            };
            reroll.style.fontSize = 16;
            seedRow.Add(reroll);

            var editButton = new Button(ToggleEditor) { text = "EDIT ENEMY CARDS", name = "pve-edit-cards" };
            editButton.style.marginTop = 16;
            editButton.style.width = 320;
            editButton.style.fontSize = 16;
            column.Add(editButton);
            editorHost = new VisualElement { name = "pve-card-editor-host" };
            column.Add(editorHost);

            var start = new Button(OnStartClicked) { text = "START ENCOUNTER" };
            start.name = "pve-start";
            start.style.marginTop = 24;
            start.style.height = 56;
            start.style.width = 320;
            start.style.fontSize = 22;
            start.style.unityFontStyleAndWeight = FontStyle.Bold;
            column.Add(start);
            column.Add(problemLabel);
            Refresh();
        }

        private VisualElement editorHost;

        /// <summary>Shows or hides the enemy card editor (user request 2026-10-10).</summary>
        private void ToggleEditor()
        {
            if (editor != null)
            {
                editorHost.Clear();
                editor = null;
                return;
            }

            editor = new PveCardEditor(
                () =>
                {
                    PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var fresh, out _);
                    return fresh;
                },
                saved =>
                {
                    catalog = saved;
                    var problems = PveContentValidator.Validate(catalog, encounter, map);
                    problemLabel.text = problems.Count == 0 ? string.Empty : string.Join("\n", problems);
                });
            editorHost.Add(editor.Build());
        }

        private void Toggle(string heroIdValue)
        {
            if (!chosen.Remove(heroIdValue))
            {
                if (chosen.Count >= encounter.HeroPlacement.Count) return;
                chosen.Add(heroIdValue);
            }

            Refresh();
        }

        private void Refresh()
        {
            countLabel.text = $"Heroes chosen: {chosen.Count} / {encounter.HeroPlacement.Count}";
            foreach (var pair in heroButtons)
            {
                var order = chosen.IndexOf(pair.Key);
                pair.Value.style.backgroundColor = order >= 0
                    ? new Color(0.18f, 0.5f, 1f)
                    : new Color(0.22f, 0.24f, 0.28f);
                pair.Value.style.color = Color.white;
            }
        }

        private void OnStartClicked()
        {
            if (!ulong.TryParse(seedField.value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
            {
                problemLabel.text = "The seed must be a whole number.";
                return;
            }

            var problems = StartEncounter(chosen, seed);
            problemLabel.text = problems.Count == 0 ? string.Empty : string.Join("\n", problems);
        }

        private static Label Text(string text, int size, FontStyle style, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = style;
            label.style.color = color;
            label.style.marginBottom = 6;
            return label;
        }
    }
}
