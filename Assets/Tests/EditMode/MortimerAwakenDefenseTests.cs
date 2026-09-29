using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8f. Awaken! is a Defense/Skill card - its face prints "Basic Defense / Skill" - and
    /// rulebook pp.13-14 read it: "Cards with this icon can be used as either a Skill card, or as a
    /// Defense card ... The card text is applied whether you play this card as a Skill card, or discard
    /// it to defend, as a Defense card." 9.8a had composed it as a Skill with a secondary Defense.
    /// </summary>
    public sealed class MortimerAwakenDefenseTests
    {
        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        [Test]
        public void Awaken_IsASkillAndADefenseAsPrintedPrimaries_WithNoSecondaryDefense()
        {
            var awaken = MortimerCardContent.AwakenDefinition;
            var primaries = awaken.ActionDefinitions
                .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).ToArray();

            Assert.That(primaries.Select(action => action.Kind),
                Is.EquivalentTo(new[] { ActionKind.Skill, ActionKind.Defense }));
            var defense = primaries.Single(action => action.Kind == ActionKind.Defense);
            Assert.That(defense.BaseValues.Single(value => value.Channel == PrintedValueChannel.Defense).Amount,
                Is.EqualTo(3), "Basic Defense 3.");
            Assert.That(defense.CardTextAt(CardTextTiming.DefenseAction), Is.Not.Null,
                "the card text applies when it is discarded to defend.");
            Assert.That(awaken.ActionDefinitions.Any(action =>
                action.Kind == ActionKind.Defense && action.Origin != ActionDefinitionOrigin.PrintedPrimary),
                Is.False, "no rail Defense badge: the face prints none.");
        }

        [Test]
        public void DiscardedToDefend_AwakensTextPlacesZombies()
        {
            var round = new MortimerRound(MortimerCardContent.AwakenDefinition, enemyAt: H(1, 0),
                enemyAttacksFirst: true);
            round.EnemyStrikesMortimer();

            Assert.That(round.DefenseOffers(), Has.Member(MortimerCardContent.AwakenDefinition.Reference));
            round.MortimerDefendsWith(MortimerCardContent.AwakenDefinition);

            Assert.That(round.PendingNode, Is.EqualTo("awaken-place-1"),
                "p.14's step 4: the defender's card text - where the first Zombie goes.");
            Assert.That(round.Chooser, Is.EqualTo(MortimerRound.Mortimer));
            round.PickSpaces(H(0, 1));
            round.Decline();
            round.Decline();
            round.Decline();

            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(0, 1) }));
        }
    }
}
