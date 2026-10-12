using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Pve.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>
    /// The setup screen's enemy card editor (user request 2026-10-10, PVE-D15): every number on every
    /// enemy card - Initiative, Movement, Attack, Range and Defense - can be changed and saved to the
    /// content JSON beside the executable, so different values can be tried without editing files.
    /// Option order, targets, plans and copies stay as the JSON states them.
    /// </summary>
    public sealed class PveCardEditor
    {
        private readonly Func<PveEnemyCatalog> load;
        private readonly Action<PveEnemyCatalog> saved;
        private readonly Dictionary<string, IntegerField> fields = new Dictionary<string, IntegerField>();
        private PveEnemyCatalog catalog;
        private Label problems;

        public PveCardEditor(Func<PveEnemyCatalog> load, Action<PveEnemyCatalog> saved)
        {
            this.load = load ?? throw new ArgumentNullException(nameof(load));
            this.saved = saved;
        }

        public VisualElement Root { get; private set; }

        public VisualElement Build()
        {
            catalog = load();
            fields.Clear();
            Root = new ScrollView { name = "pve-card-editor" };
            Root.style.maxHeight = 520;
            Root.style.marginTop = 12;
            Root.style.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            Root.style.paddingLeft = 8;
            Root.style.paddingTop = 6;
            Root.style.paddingBottom = 6;

            foreach (var type in catalog.Types)
            {
                Root.Add(Heading(type.DisplayName + " (" + type.Id + ")"));
                foreach (var card in type.OrangeCards)
                {
                    var row = Row(card.Key + " " + card.Name + (card.IsIdle ? " (idle)" : string.Empty));
                    row.Add(Field(type, card.Key, "init", card.Initiative));
                    if (!card.IsIdle && card.Skill == PveSkillKind.None)
                    {
                        row.Add(Field(type, card.Key, "move", card.MoveValue ?? 0));
                        row.Add(Field(type, card.Key, "attack", card.AttackValue ?? 0));
                        row.Add(Field(type, card.Key, "range", card.AttackRange));
                    }

                    Root.Add(row);
                }

                foreach (var card in type.PurpleCards)
                {
                    var row = Row(card.Key + " " + card.Name + " (purple)");
                    row.Add(Field(type, card.Key, "init", card.Initiative));
                    row.Add(Field(type, card.Key, "defense", card.Defense));
                    Root.Add(row);
                }
            }

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 8;
            buttons.Add(new Button(Save) { text = "SAVE ENEMY CARDS", name = "pve-card-editor-save" });
            buttons.Add(new Button(Revert) { text = "Revert" });
            Root.Add(buttons);
            problems = new Label { name = "pve-card-editor-problems" };
            problems.style.whiteSpace = WhiteSpace.Normal;
            problems.style.color = new Color(1f, 0.6f, 0.5f);
            Root.Add(problems);
            Root.Add(new Label("0 means none: a card with Move 0 does not move, with Attack 0 does not attack. " +
                "Range 0 attacks an adjacent unit.") { style = { color = new Color(0.7f, 0.72f, 0.76f), whiteSpace = WhiteSpace.Normal } });
            return Root;
        }

        /// <summary>The catalog with the edited numbers, keeping every option, target, plan and copy.</summary>
        public PveEnemyCatalog Edited() => new PveEnemyCatalog(catalog.ContentStatus, catalog.Types.Select(type =>
            new PveEnemyType(
                type.Id,
                type.DisplayName,
                type.Level,
                type.OrangeCards.Select(card =>
                {
                    if (card.IsIdle || card.Skill != PveSkillKind.None)
                    {
                        return new PveOrangeCard(card.Key, card.Name, Value(type, card.Key, "init", card.Initiative),
                            card.Copies, null, 0, null, card.Options, card.IsIdle, card.Skill);
                    }

                    var attack = Value(type, card.Key, "attack", card.AttackValue ?? 0);
                    var move = Value(type, card.Key, "move", card.MoveValue ?? 0);
                    var options = card.Options
                        .Where(option => (option.Kind != PveOptionKind.Attack || attack > 0) &&
                            (option.Kind != PveOptionKind.Move || move > 0))
                        .ToArray();
                    return new PveOrangeCard(card.Key, card.Name, Value(type, card.Key, "init", card.Initiative), card.Copies,
                        attack > 0 ? attack : (int?)null, Value(type, card.Key, "range", card.AttackRange),
                        move > 0 ? move : (int?)null, options);
                }),
                type.PurpleCards.Select(card => new PvePurpleCard(card.Key, card.Name,
                    Value(type, card.Key, "init", card.Initiative), Value(type, card.Key, "defense", card.Defense))),
                type.Plans, type.TokenType, type.MinionCategory)));

        private void Save()
        {
            var edited = Edited();
            var errors = PveContentValidator.ValidateCatalog(edited);
            if (errors.Count > 0)
            {
                problems.text = string.Join("\n", errors);
                return;
            }

            try
            {
                File.WriteAllText(Path.Combine(PveDataPaths.Directory, PveDataPaths.EnemiesFileName),
                    PveContentLoader.SaveCatalog(edited));
            }
            catch (Exception exception)
            {
                problems.text = "Could not save: " + exception.Message;
                return;
            }

            catalog = edited;
            problems.text = "Saved.";
            saved?.Invoke(edited);
        }

        private void Revert()
        {
            catalog = load();
            foreach (var type in catalog.Types)
            {
                foreach (var card in type.OrangeCards)
                {
                    Set(type, card.Key, "init", card.Initiative);
                    Set(type, card.Key, "move", card.MoveValue ?? 0);
                    Set(type, card.Key, "attack", card.AttackValue ?? 0);
                    Set(type, card.Key, "range", card.AttackRange);
                }

                foreach (var card in type.PurpleCards)
                {
                    Set(type, card.Key, "init", card.Initiative);
                    Set(type, card.Key, "defense", card.Defense);
                }
            }

            problems.text = "Reverted to the saved file.";
        }

        private IntegerField Field(PveEnemyType type, string key, string name, int value)
        {
            var field = new IntegerField(name) { value = value, name = "pve-edit-" + type.Id + "-" + key + "-" + name };
            field.style.width = 120;
            field.labelElement.style.minWidth = 46;
            fields[Key(type, key, name)] = field;
            return field;
        }

        private int Value(PveEnemyType type, string key, string name, int fallback) =>
            fields.TryGetValue(Key(type, key, name), out var field) ? field.value : fallback;

        private void Set(PveEnemyType type, string key, string name, int value)
        {
            if (fields.TryGetValue(Key(type, key, name), out var field)) field.SetValueWithoutNotify(value);
        }

        private static string Key(PveEnemyType type, string key, string name) => type.Id + "/" + key + "/" + name;

        private static Label Heading(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            label.style.marginTop = 6;
            return label;
        }

        private static VisualElement Row(string title)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            var label = new Label(title);
            label.style.width = 190;
            label.style.color = new Color(0.9f, 0.9f, 0.92f);
            row.Add(label);
            return row;
        }
    }
}
