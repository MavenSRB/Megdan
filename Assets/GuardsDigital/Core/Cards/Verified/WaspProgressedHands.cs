using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Wasp's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.15.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// fourteen coin-earning rounds a real progression needs are outside any fixture's budget, which
    /// is the same reason milestone 8.14 composed its post-progression session from transitioned
    /// instances rather than playing there. What survives into a composed session is card
    /// <em>location</em> - the unlock is the Ultimate's own position, and p.15's returns are why the
    /// lower-Tier cards sit in the deck - so these hands state exactly the locations the page's own
    /// procedure produces and nothing else. Items and Levels do not carry: the session derives fresh
    /// progression state, exactly as it did for 8.14's ordinary-match case.</para>
    ///
    /// <para>The three upgraded cards per seat are one of each colour, per p.15's own closing count
    /// ("exactly 5 cards, 1 of each color"), and the two seats hold complementary siblings so that
    /// between them every Tier II or Tier III card is in some hand: a mirror fixture that dealt both
    /// seats the same three would leave the other three unreachable in play.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.5's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class WaspProgressedHands
    {
        /// <summary>
        /// The Tier II stage, Red seat: Electrocute, Kinetic Repulse and Deflect Projectiles beside
        /// the two basics. The Blue seat holds the three siblings, so the ranged attacker Charged
        /// Boomerang faces the seat holding the ranged Block.
        /// </summary>
        private static readonly CardDefinitionId[] TierTwoRed =
        {
            WaspCardContent.Electrocute, WaspCardContent.KineticRepulse,
            WaspCardContent.DeflectProjectiles
        };

        private static readonly CardDefinitionId[] TierTwoBlue =
        {
            WaspCardContent.ChargedBoomerang, WaspCardContent.ControlGravity,
            WaspCardContent.Telekinesis
        };

        /// <summary>
        /// The Tier III stage, Red seat: Electroblast, Kinetic Blast and Reflect Projectiles, with
        /// the Ultimate unlocked. The Blue seat holds Thunder Boomerang, Center of Mass and Mass
        /// Telekinesis, so the ranged attacker again faces the seat holding the ranged Block.
        /// </summary>
        private static readonly CardDefinitionId[] TierThreeRed =
        {
            WaspCardContent.Electroblast, WaspCardContent.KineticBlast,
            WaspCardContent.ReflectProjectiles
        };

        private static readonly CardDefinitionId[] TierThreeBlue =
        {
            WaspCardContent.ThunderBoomerang, WaspCardContent.CenterOfMass,
            WaspCardContent.MassTelekinesis
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, Participants.TeamSide side) =>
            Create(
                ownerId,
                instanceIdPrefix,
                side == Participants.TeamSide.Red ? TierTwoRed : TierTwoBlue);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// High Voltage is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area, which keeps this staging on the same single writer the
        /// rules use.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, Participants.TeamSide side) =>
            Create(
                ownerId,
                instanceIdPrefix,
                side == Participants.TeamSide.Red ? TierThreeRed : TierThreeBlue);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Wasp's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                WaspCardContent.MagneticDagger,
                WaspCardContent.StaticBarrier
            };
            var cards = WaspCardContent.DealtDefinitions
                .Select(definition =>
                {
                    var instanceId = new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}");
                    return inHand.Contains(definition.Id)
                        ? CardInstance.CreateInHand(instanceId, definition.Reference, ownerId)
                        : CardInstance.CreateInDeck(instanceId, definition.Reference, ownerId);
                })
                .ToArray();
            return new ConfiguredHand(
                WaspCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
