using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// Milestone 8.38c1. A standing promise to pay its controller when an enemy minion is defeated
    /// inside its radius - Swift's Mark for Death, "<b>Next turn:</b> the first time an enemy minion in
    /// radius is defeated, gain 1 coin", and Hunting Season, which says the same of the first two
    /// times.
    ///
    /// <para><b>The bounty is spent by the DEFEAT, not by the removal, and rulebook p.13 is explicit
    /// about the difference.</b> Its Bolster example prevents a minion's removal and the attacking hero
    /// still collects the coins, so a Defeat whose removal is stopped is a complete Defeat. "Is
    /// defeated" therefore reads the Defeat, which is why this is asked beside
    /// <c>coinLedger.Award</c> - the moment p.13 pays for the Defeat itself - and not at the removal
    /// stage that runs afterwards and may decide nothing happened.</para>
    ///
    /// <para><b>The printed count is instance multiplicity rather than a counter.</b> A face promising
    /// two payments stands two of these effects and each is spent once, so "how many payments remain"
    /// is "how many are still live" - a fact the lifecycle already records, restores and undoes. A
    /// remaining-uses field on the instance would have been a second place for the same fact and a
    /// schema revision to carry it. <see cref="ActiveEffectLifecycle.CancelOne"/> is what spends one.</para>
    ///
    /// <para><b>Whose radius, measured when.</b> The disk is the controller's, centred where the
    /// controller is standing at the moment of the Defeat, drawn at the SOURCE CARD's live printed
    /// Radius so that p.16's Radius item raises the promise exactly as it raises the Skill that made
    /// it. That is the only reading the printed words admit - "an enemy minion <em>in radius</em>" is a
    /// fact about the board when the minion dies - and it is why a bounty cannot be settled at the end
    /// of the turn, when its controller may be standing somewhere else.</para>
    ///
    /// <para><b>Nobody has to be the one who killed it.</b> The face says "is defeated", not "you
    /// defeat", so the query never looks at the acting hero. In practice the actor is usually the
    /// controller - a hero acts on their own turn - but narrowing to that would be reading a sentence
    /// the card does not print.</para>
    /// </summary>
    public static class MinionDefeatBountyQuery
    {
        /// <summary>
        /// The <see cref="ActiveEffectDefinition.EffectKind"/> a bounty declares. It is the whole
        /// coupling between the content that creates one and the Defeat that spends it: no card id, no
        /// hero id and no capability name appears on either side.
        /// </summary>
        public const string EffectKind = "MinionDefeatBounty";

        /// <summary>The grant kind a spent bounty pays under - p.16's ordinary card-effect coin.</summary>
        public const CoinGrantKind GrantKind = CoinGrantKind.CardEffect;

        /// <summary>
        /// Spends at most ONE bounty for one defeated minion and returns what it paid, or an empty list
        /// when nothing applies.
        ///
        /// <para><b>One, because one minion died.</b> Two of Hunting Season's effects standing over the
        /// same space are two promises about two future minions, not two payments for one - "the first
        /// two <em>times</em>" counts occurrences. The oldest live effect is the one spent, which is
        /// arbitrary between two identical promises and deterministic, which is what a replay needs.</para>
        ///
        /// <para>Every argument may be absent, and absence means no bounty rather than a fault: most of
        /// this build's compositions hold no lifecycle, no ledger or no items, and a Defeat in one of
        /// them must resolve exactly as it did before this milestone existed.</para>
        /// </summary>
        public static IReadOnlyList<CoinGrantOperation> Spend(
            ActiveEffectLifecycle timing,
            HeroCoinLedger coinLedger,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            HeroItemState items,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MinionOccupancy defeatedMinion,
            int roundIndex,
            int turnIndex)
        {
            if (timing == null || coinLedger == null || board == null || participants == null ||
                defeatedMinion == null || roundIndex < 1 || turnIndex < 1)
            {
                return Array.Empty<CoinGrantOperation>();
            }

            var spendable = timing.Effects
                .Where(effect =>
                    effect != null &&
                    effect.Phase == ActiveEffectPhase.Active &&
                    string.Equals(effect.Definition.EffectKind, EffectKind, StringComparison.Ordinal) &&
                    Covers(
                        effect, board, participants, items, definitions, instances, defeatedMinion,
                        // Milestone 8.61h. p.16's item bonus is what Min's "This round: Double your
                        // item bonuses" scales, and this promise's disk is a printed Radius icon like
                        // any other - so the multiplier is read here too. It comes off the lifecycle
                        // rather than off a modification set because this query is composed with no
                        // set at all; `ItemBonusMultiplierQuery` owns both readings so the promise and
                        // the Skill that made it cannot measure different disks.
                        ItemBonusMultiplierQuery.BonusPerItem(effect.ControllerHeroId, timing)))
                .OrderBy(effect => effect.CreationSequence)
                .FirstOrDefault();
            if (spendable == null)
            {
                return Array.Empty<CoinGrantOperation>();
            }

            // The cancellation comes first so that a ledger that refuses the grant - an overflowing
            // balance is the only way it can - cannot leave a promise standing that has already been
            // treated as paid. Refusing costs the bounty, which is the conservative direction: the
            // alternative pays a coin the ledger did not record.
            timing.CancelOne(spendable.Id, roundIndex, turnIndex);
            if (!coinLedger.TryGrant(spendable.ControllerHeroId, 1, GrantKind, out var operation))
            {
                return Array.Empty<CoinGrantOperation>();
            }

            return Array.AsReadOnly(new[] { operation });
        }

        /// <summary>
        /// Whether one standing bounty reaches this minion: the minion opposes its controller, and it
        /// stands inside the controller's live disk.
        /// </summary>
        private static bool Covers(
            ActiveEffectInstance effect,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            HeroItemState items,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MinionOccupancy defeatedMinion,
            int bonusPerItem)
        {
            if (!effect.ControllerHeroId.IsDefined ||
                !board.TryGetSoleHeroCoordinate(
                    effect.ControllerHeroId, "the anchor of a Defeat-bounty promise", out var controllerAt))
            {
                // p.16 ends an Ultimate's ability while its hero is off the board and p.17 says the
                // same of an Active effect's controller; a promise whose maker is not standing
                // anywhere reaches nothing.
                return false;
            }

            if (participants.GetTeamForHero(effect.ControllerHeroId).Side == defeatedMinion.TeamSide)
            {
                // "an ENEMY minion", read from the controller's side rather than from the actor's -
                // the promise is theirs, so the enmity is theirs too.
                return false;
            }

            // The disk is the SOURCE CARD's printed Radius icon, read live through the same query
            // every printed total goes through, so p.16's Radius item raises the promise exactly as it
            // raises the Skill that made it.
            //
            // It is read from the card rather than restated on the effect because an Active effect may
            // only declare a radius when its own SCOPE is measured from one, and this effect's scope is
            // its controller: the disk is a measurement the promise takes, not the set of units the
            // effect affects. Saying it twice would also have been a second place for one number.
            var sourceCard = instances?.FirstOrDefault(card =>
                card != null && card.Id.Equals(effect.SourceCardInstanceId));
            if (sourceCard == null)
            {
                return false;
            }

            var radius = RadiusValueQuery.Calculate(
                effect.SourceCardInstanceId,
                sourceCard.DefinitionReference,
                effect.SourceActionDefinitionId,
                definitions,
                effect.ControllerHeroId,
                items,
                bonusPerItem).Total;
            return radius > 0 &&
                HexDistanceQuery.Calculate(controllerAt, defeatedMinion.Coordinate) <= radius;
        }
    }
}
