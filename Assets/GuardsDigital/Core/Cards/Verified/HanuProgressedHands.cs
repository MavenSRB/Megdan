using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.71h. Which of rulebook p.15's two siblings a staged Hanu seat took at each upgrade,
    /// so a fixture can name the half of his surface it wants without driving nine level-ups.
    ///
    /// <para>His three ladders split the same way on every colour, and the split falls along the lines
    /// his six composition blocks were drawn on, so each branch reaches whole families rather than a
    /// scattering:</para>
    /// <list type="bullet">
    /// <item><description><b>FirstSibling</b> - Even the Odds, There and Back Again and Monkey Twist at
    /// Tier II; Trusted Sidekick, Safe Travels and Monkey Business at Tier III. This is the branch that
    /// carries the HERO bullet of the Red modal ("a hero in range, adjacent to your friendly hero"),
    /// the ONE-OR-BOTH modal at its full 1..2 with Trusted Sidekick's disjoint bullets, the two
    /// journeys whose swap back this build fires at <c>TimingCheckpoint.TurnEnded</c> with Safe
    /// Travels' trailing move printed AFTER the end-of-turn sentence (HA-Q8), and the friendly PAIR
    /// swap with Monkey Business's own trailing move.</description></item>
    /// <item><description><b>SecondSibling</b> - Outnumber, This Way! and Hear Nothing at Tier II; Pile
    /// On, That Way! and See Nothing at Tier III. This is the only branch that reaches the MINION
    /// bullet (<c>AttackTargetableUnits.MinionsOnly</c>, and Pile On's reading that one minion may take
    /// both bullets - RULING 19), either CO-MOVE and therefore the cross-seat distance a friendly hero
    /// chooses on their own seat, and the plain enemy-hero swap with See Nothing's trailing move from
    /// the exchanged space.</description></item>
    /// </list>
    ///
    /// <para>Both basics stay in hand at every stage, exactly as p.6 leaves them: his Gold Fight and
    /// Flight and his Silver Hurry up! are never upgraded away - and that matters for him more than for
    /// any hero before him, because Hurry up! is the face his Ultimate rides. The Ultimate Trick is
    /// reachable on BOTH branches, since p.16 unlocks it from the deck rather than upgrading into
    /// it.</para>
    ///
    /// <para>These are internal staged hands rather than a substitute for the real p.15 progression
    /// transaction - the same statement every other hero's file of this shape makes.</para>
    /// </summary>
    public enum HanuUpgradeBranch
    {
        FirstSibling = 0,
        SecondSibling = 1
    }

    /// <summary>
    /// Milestone 8.71h. One seat's Hanu hand at rulebook p.15's Tier II or Tier III stage, built from
    /// his own dealt definitions so a staged match holds real printed cards rather than a fixture's
    /// copy of them.
    /// </summary>
    public static class HanuProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(
            HeroId owner, string prefix, HanuUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    HanuCardContent.EvenTheOdds,
                    HanuCardContent.ThereAndBackAgain,
                    HanuCardContent.MonkeyTwist
                },
                new[]
                {
                    HanuCardContent.Outnumber,
                    HanuCardContent.ThisWay,
                    HanuCardContent.HearNothing
                });

        public static ConfiguredHand CreateTierThreeHand(
            HeroId owner, string prefix, HanuUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    HanuCardContent.TrustedSidekick,
                    HanuCardContent.SafeTravels,
                    HanuCardContent.MonkeyBusiness
                },
                new[]
                {
                    HanuCardContent.PileOn,
                    HanuCardContent.ThatWay,
                    HanuCardContent.SeeNothing
                });

        private static ConfiguredHand Create(
            HeroId owner,
            string prefix,
            HanuUpgradeBranch branch,
            CardDefinitionId[] first,
            CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(HanuUpgradeBranch), branch))
            {
                throw new ArgumentOutOfRangeException(nameof(branch));
            }

            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    "Staged cards require a defined owner and a per-seat instance identity.");
            }

            var inHand = new HashSet<CardDefinitionId>(
                branch == HanuUpgradeBranch.FirstSibling ? first : second)
            {
                HanuCardContent.FightAndFlight,
                HanuCardContent.HurryUp
            };

            var cards = HanuCardContent.DealtDefinitions
                .Select(definition => inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference,
                        owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference,
                        owner))
                .ToArray();

            return new ConfiguredHand(
                HanuCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
