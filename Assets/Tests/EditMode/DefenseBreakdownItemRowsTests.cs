using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.74. The Defense breakdown reads every item-card bonus on one side as ONE row.
    ///
    /// <para>The user, on a Defense preview that named "Outnumber" as a defence source: "It should not
    /// say card name, it shuld just be items and then the number they provide", and "if there are 2
    /// cards that provide defense its just defense +2. Same goes for attack if total is being modified
    /// by items." These cases build the projection the authority would publish and read the rows the
    /// window would draw, on both sides, so the collapse is held where it is decided
    /// (<see cref="RoundActionPresenter.CreateDefenseBreakdown"/>) rather than where it is painted.</para>
    /// </summary>
    public sealed class DefenseBreakdownItemRowsTests
    {
        private static readonly ActionResolutionId Resolution = new ActionResolutionId("8-74-resolution");
        private static readonly ActionResolutionStepId Choice = new ActionResolutionStepId("8-74-choice");

        [Test]
        public void TwoAttackItemsCollapseIntoOneItemsRowCarryingTheirSum()
        {
            var preview = new PendingDefensePreviewProjection(
                Resolution, Choice,
                new[]
                {
                    Card(ValueContributionKind.PrintedIcon, "attacker-card", PrintedValueChannel.Attack, 3, "Printed Attack"),
                    Card(ValueContributionKind.ItemModifier, "even-the-odds", PrintedValueChannel.Attack, 1, "Even the Odds"),
                    Card(ValueContributionKind.ItemModifier, "outnumber", PrintedValueChannel.Attack, 1, "Outnumber")
                },
                5,
                new[] { Decline() },
                Enumerable.Empty<ValueContributionProjection>());

            var model = RoundActionPresenter.CreateDefenseBreakdown(1, preview, preview.Choices.Single());

            Assert.That(model.AttackRows.Select(row => (row.Amount, row.SourceLabel)), Is.EqualTo(new[]
            {
                (3, "Printed Attack"),
                (2, RoundActionPresenter.CollapsedItemsLabel)
            }), "the two item cards read as one Items row worth their sum, where the first item stood.");
            Assert.That(model.AttackRows.Sum(row => row.Amount), Is.EqualTo(preview.AttackTotal),
                "collapsing changes no total.");
            Assert.That(model.Text, Does.Contain("+2 — Items"));
            Assert.That(model.Text, Does.Not.Contain("Outnumber"), "no item card is named by itself.");
        }

        [Test]
        public void DefenseItemsCollapseAroundANamedMinionRowWhichKeepsItsOwnLine()
        {
            var card = Card(ValueContributionKind.PrintedIcon, "defender-card", PrintedValueChannel.Defense, 2, "Printed Defense");
            var firstItem = Card(ValueContributionKind.ItemModifier, "shield", PrintedValueChannel.Defense, 1, "Steady Advance");
            var minion = new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.MinionDefenseModifier, new MinionId("blue-melee-01"),
                    PrintedValueChannel.Defense, 1),
                "blue-melee-01");
            var secondItem = Card(ValueContributionKind.ItemModifier, "helm", PrintedValueChannel.Defense, 1, "Parry");
            var choice = new DefenseChoicePreviewProjection(
                new CardInstanceId("defender-card"), "Shield Wall",
                new[] { card, firstItem, minion, secondItem }, 5,
                DefenseBlockResult.None, AttackResolutionOutcome.SuccessfullyDefended);
            var preview = new PendingDefensePreviewProjection(
                Resolution, Choice,
                new[] { Card(ValueContributionKind.PrintedIcon, "attacker-card", PrintedValueChannel.Attack, 4, "Printed Attack") },
                4,
                new[] { choice },
                new[] { minion });

            var model = RoundActionPresenter.CreateDefenseBreakdown(2, preview, choice);

            Assert.That(model.DefenseRows.Select(row => (row.Amount, row.SourceLabel)), Is.EqualTo(new[]
            {
                (2, "Printed Defense"),
                (2, RoundActionPresenter.CollapsedItemsLabel),
                (1, "blue-melee-01")
            }), "the items merge where the first stood; the minion keeps its own named row after them.");
            Assert.That(model.AttackRows.Select(row => row.SourceLabel), Is.EqualTo(new[] { "Printed Attack" }),
                "a side with no item is unchanged.");
            Assert.That(model.DefenseRows.Sum(row => row.Amount), Is.EqualTo(choice.DefenseTotal));
        }

        [Test]
        public void ASideWithoutItemsKeepsEveryNamedRowAndADeclineDrawsNoDefenseRows()
        {
            var preview = new PendingDefensePreviewProjection(
                Resolution, Choice,
                new[]
                {
                    Card(ValueContributionKind.PrintedIcon, "attacker-card", PrintedValueChannel.Attack, 3, "Printed Attack"),
                    Card(ValueContributionKind.CardTextModifier, "attacker-card", PrintedValueChannel.Attack, 1, "Card text")
                },
                4,
                new[] { Decline() },
                Enumerable.Empty<ValueContributionProjection>());

            var model = RoundActionPresenter.CreateDefenseBreakdown(3, preview, preview.Choices.Single());

            Assert.That(model.AttackRows.Select(row => row.SourceLabel),
                Is.EqualTo(new[] { "Printed Attack", "Card text" }),
                "only ItemModifier contributions collapse; every other typed source keeps its own row.");
            Assert.That(model.DefenseRows, Is.Empty, "declining contributes nothing and lists nothing.");
            Assert.That(model.Text, Does.Not.Contain(RoundActionPresenter.CollapsedItemsLabel));
        }

        // An item contribution names only its item card; a printed icon or card text names the action
        // it belongs to as well - `ValueContribution` refuses the other pairing on each kind.
        private static ValueContributionProjection Card(
            ValueContributionKind kind, string cardId, PrintedValueChannel channel, int amount, string label) =>
            new ValueContributionProjection(
                kind == ValueContributionKind.ItemModifier
                    ? new ValueContribution(kind, new CardInstanceId(cardId), channel, amount)
                    : new ValueContribution(
                        kind, new CardInstanceId(cardId), new ActionDefinitionId("primary"), channel, amount),
                label);

        private static DefenseChoicePreviewProjection Decline() =>
            new DefenseChoicePreviewProjection(
                null, "Decline", Enumerable.Empty<ValueContributionProjection>(), null,
                DefenseBlockResult.None, AttackResolutionOutcome.AttackSuccessful);
    }
}
