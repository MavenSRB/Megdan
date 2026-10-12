using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Transitions
{
    /// <summary>
    /// Which decision a paused system-transition step is asking for.
    ///
    /// It names the rule that stopped rather than the step identity that recorded it, because the step
    /// identity is a string the rules own and a reader outside them cannot be asked to parse. Milestone
    /// 7.8a: a view exists for this decision now, and it must not learn to read
    /// <c>HeroLevelUpItem</c>'s or <c>LanePushRespawnItem</c>'s encodings to draw it.
    /// </summary>
    public enum SystemTransitionPromptKind
    {
        None = 0,

        /// <summary>Rulebook p.15's Upgrade a Card, for one level one hero has already paid for.</summary>
        LevelUpUpgrade = 1,

        /// <summary>Rulebook p.8's sidebar Place, for a minion whose Spawn point is blocked.</summary>
        LanePushRespawnPlace = 2,

        /// <summary>An original mode's owner-chosen initial hero placement.</summary>
        HeroPlacement = 3
    }

    /// <summary>
    /// One option of a paused step, described by what taking it would do.
    ///
    /// Every field is authoritative content read from current state - card instances, their definition
    /// references, a printed icon, a space - and none of it is a display string. That division is the one
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview"/> already draws:
    /// the rules state the facts, and presentation composes the words. It is also what lets the milestone
    /// 7.8a acceptance rule hold that no label is produced by parsing an option identity - a reader never
    /// sees one.
    ///
    /// The two factories are the two shapes of decision this build's transitions actually stop on. A
    /// third would add a third factory rather than widen either of these, so an option can never carry
    /// half of one kind and half of another.
    /// </summary>
    public sealed class SystemTransitionOptionDescription
    {
        private SystemTransitionOptionDescription(
            SystemTransitionOptionId optionId,
            SystemTransitionPromptKind kind,
            CardInstanceId upgradedCardInstanceId,
            CardDefinitionReference upgradedCard,
            CardInstanceId takenCardInstanceId,
            CardDefinitionReference takenCard,
            CardInstanceId setAsideCardInstanceId,
            CardDefinitionReference setAsideCard,
            PrintedValueChannel itemIcon,
            HeroDashboardSlotPosition itemSlot,
            HexCoordinate destination,
            SpaceId destinationSpaceId)
        {
            if (!optionId.IsDefined)
            {
                throw new ArgumentException(
                    "A described option names the option the step actually offered.", nameof(optionId));
            }

            OptionId = optionId;
            Kind = kind;
            UpgradedCardInstanceId = upgradedCardInstanceId;
            UpgradedCard = upgradedCard;
            TakenCardInstanceId = takenCardInstanceId;
            TakenCard = takenCard;
            SetAsideCardInstanceId = setAsideCardInstanceId;
            SetAsideCard = setAsideCard;
            ItemIcon = itemIcon;
            ItemSlot = itemSlot;
            Destination = destination;
            DestinationSpaceId = destinationSpaceId;
        }

        public SystemTransitionOptionId OptionId { get; }

        public SystemTransitionPromptKind Kind { get; }

        /// <summary>p.15's "Choose a non-basic Action card in your hand": the card returned to the deck.</summary>
        public CardInstanceId UpgradedCardInstanceId { get; }

        public CardDefinitionReference UpgradedCard { get; }

        /// <summary>p.15's "Take one of those cards into your hand".</summary>
        public CardInstanceId TakenCardInstanceId { get; }

        public CardDefinitionReference TakenCard { get; }

        /// <summary>
        /// p.15's "set the other one aside", which p.16 then turns into an item. Undefined when the deck
        /// holds no second candidate, which is an answer the page's own instruction produces rather than
        /// a failure - and then <see cref="ItemIcon"/> is <see cref="PrintedValueChannel.None"/> too.
        /// </summary>
        public CardInstanceId SetAsideCardInstanceId { get; }

        public CardDefinitionReference SetAsideCard { get; }

        /// <summary>
        /// p.16's "the slot with a matching icon", read from the set-aside card's own printed Item icon
        /// and checked against this hero's dashboard. <see cref="PrintedValueChannel.None"/> when there
        /// is no card to set aside, or when no slot of that dashboard accepts its icon.
        /// </summary>
        public PrintedValueChannel ItemIcon { get; }

        /// <summary>The dashboard slot that icon matches, undefined whenever <see cref="ItemIcon"/> is none.</summary>
        public HeroDashboardSlotPosition ItemSlot { get; }

        /// <summary>p.8's "nearest empty space of their choice in the Battle Zone".</summary>
        public HexCoordinate Destination { get; }

        public SpaceId DestinationSpaceId { get; }

        public static SystemTransitionOptionDescription Upgrade(
            SystemTransitionOptionId optionId,
            CardInstanceId upgradedCardInstanceId,
            CardDefinitionReference upgradedCard,
            CardInstanceId takenCardInstanceId,
            CardDefinitionReference takenCard,
            CardInstanceId setAsideCardInstanceId,
            CardDefinitionReference setAsideCard,
            PrintedValueChannel itemIcon,
            HeroDashboardSlotPosition itemSlot)
        {
            if (!upgradedCardInstanceId.IsDefined || !takenCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "An upgrade names the card returned to the deck and the card taken in its place.");
            }

            if (!setAsideCardInstanceId.IsDefined && itemIcon != PrintedValueChannel.None)
            {
                throw new ArgumentException(
                    "An upgrade with no card set aside gains no item, so it names no icon.",
                    nameof(itemIcon));
            }

            return new SystemTransitionOptionDescription(
                optionId,
                SystemTransitionPromptKind.LevelUpUpgrade,
                upgradedCardInstanceId,
                upgradedCard,
                takenCardInstanceId,
                takenCard,
                setAsideCardInstanceId,
                setAsideCard,
                itemIcon,
                itemSlot,
                default,
                default);
        }

        public static SystemTransitionOptionDescription Place(
            SystemTransitionOptionId optionId,
            HexCoordinate destination,
            SpaceId destinationSpaceId) =>
            new SystemTransitionOptionDescription(
                optionId,
                SystemTransitionPromptKind.LanePushRespawnPlace,
                default,
                default,
                default,
                default,
                default,
                default,
                PrintedValueChannel.None,
                default,
                destination,
                destinationSpaceId);

        public static SystemTransitionOptionDescription PlaceHero(
            SystemTransitionOptionId optionId, HexCoordinate destination, SpaceId destinationSpaceId) =>
            new SystemTransitionOptionDescription(optionId, SystemTransitionPromptKind.HeroPlacement,
                default, default, default, default, default, default, PrintedValueChannel.None,
                default, destination, destinationSpaceId);
    }

    /// <summary>
    /// What the paused step is, whose decision it is, and what each of its options would do.
    ///
    /// Milestone 7.8a. Before it, a paused transition reached a reader as
    /// <see cref="PendingSystemTransitionStep"/> alone - identities and option identities, with no way to
    /// say what any of them meant - and the configured scene answered it through a director command that
    /// named an option without being able to describe it. This is the missing half: the same pause,
    /// described from the state that produced it.
    ///
    /// The option order is the pause's own. A description reorders nothing, adds nothing, and omits
    /// nothing: <see cref="Options"/> is required to name exactly the pending step's options, once each,
    /// so a surface built from it cannot offer a choice the authority would refuse or hide one it would
    /// accept.
    /// </summary>
    public sealed class SystemTransitionStepDescription
    {
        public SystemTransitionStepDescription(
            SystemTransitionPromptKind kind,
            PendingSystemTransitionStep pendingStep,
            HeroId heroId,
            IEnumerable<SystemTransitionOptionDescription> options,
            int level = 0,
            int paidCoins = 0,
            int remainingCoins = 0,
            SystemTransitionItemId itemId = default)
        {
            if (kind == SystemTransitionPromptKind.None)
            {
                throw new ArgumentException(
                    "A described pause names the decision it is asking for.", nameof(kind));
            }

            PendingStep = pendingStep ?? throw new ArgumentNullException(nameof(pendingStep));
            var described = (options ?? throw new ArgumentNullException(nameof(options))).ToArray();
            if (described.Any(option => option == null) ||
                described.Any(option => option.Kind != kind))
            {
                throw new ArgumentException(
                    "Every described option belongs to the decision the pause is asking for.",
                    nameof(options));
            }

            // Milestone 8.35u. A pause may now hold one open item per hero, so a description says WHICH
            // one it describes and is checked against that item's own options. Omitting the item is
            // still legal and still means "the pause's one item" - it reaches PendingStep.Options,
            // which throws rather than guessing when several stand.
            var describedItem = itemId.IsDefined
                ? pendingStep.OpenItems.FirstOrDefault(open => open.ItemId.Equals(itemId))
                : null;
            if (itemId.IsDefined && describedItem == null)
            {
                throw new ArgumentException(
                    $"A description names item '{itemId}', which this pause is not waiting on.",
                    nameof(itemId));
            }

            var offered = describedItem?.Options ?? pendingStep.Options;
            if (described.Length != offered.Count ||
                described.Select(option => option.OptionId).Distinct().Count() != described.Length ||
                described.Any(option => !offered.Contains(option.OptionId)))
            {
                throw new ArgumentException(
                    "A description names exactly the options the paused step offered, once each.",
                    nameof(options));
            }

            ItemId = itemId;

            // Milestone 8.35u. The described item's own chooser, carried rather than read back off
            // the pause: a pause may now hold one open item per hero, so `PendingStep.ChoosingSide`
            // has no single answer and throws. A description is always about ONE question, so it
            // always has one, and a surface that draws "whose question is this" reads it here.
            ChoosingSide = describedItem?.ChoosingSide ?? pendingStep.OpenItems[0].ChoosingSide;
            Kind = kind;
            HeroId = heroId;
            Level = level;
            PaidCoins = paidCoins;
            RemainingCoins = remainingCoins;
            Options = Array.AsReadOnly(described);
        }

        public SystemTransitionPromptKind Kind { get; }

        /// <summary>The pause this describes, carrying the chooser and the exact typed invocations.</summary>
        public PendingSystemTransitionStep PendingStep { get; }

        /// <summary>
        /// Milestone 8.35u. Which open item of that pause this describes, or the named absence when the
        /// pause has one and the caller did not have to say.
        /// </summary>
        public SystemTransitionItemId ItemId { get; }

        /// <summary>
        /// Milestone 8.35u. The side that answers the ONE question this describes - never the pause's,
        /// which may be waiting on both sides at once.
        /// </summary>
        public TeamSide ChoosingSide { get; }

        /// <summary>
        /// The hero whose decision this is, for a pause that belongs to one. Undefined for p.8's Place,
        /// which belongs to a team and a minion rather than to a hero.
        /// </summary>
        public HeroId HeroId { get; }

        /// <summary>p.15's Level this purchase reached, or zero when the pause is not a purchase.</summary>
        public int Level { get; }

        /// <summary>What p.15's table charged for that level, or zero.</summary>
        public int PaidCoins { get; }

        /// <summary>What the hero has left after paying it, or zero.</summary>
        public int RemainingCoins { get; }

        public IReadOnlyList<SystemTransitionOptionDescription> Options { get; }
    }

    /// <summary>
    /// Step rules that can say what their own pause means.
    ///
    /// Optional by design. <see cref="ISystemTransitionStepRules"/> is what a transition needs in order
    /// to run; this is what a reader needs in order to draw one, and a build whose transitions never
    /// pause on a player owes nothing here. <see cref="SystemTransitionAuthority.DescribePendingStep"/>
    /// returns null rather than throwing when the running rules do not implement it, because "this build
    /// cannot describe that pause" is a composition fact rather than a defect.
    /// </summary>
    public interface IDescribingSystemTransitionStepRules
    {
        /// <summary>
        /// Describes the paused step from current authoritative state, or returns null when these rules
        /// did not produce it. Reading changes nothing.
        /// </summary>
        SystemTransitionStepDescription Describe(PendingSystemTransitionStep pendingStep);
    }

    /// <summary>
    /// Milestone 8.35u. Step rules whose pause can stand on several choosers at once, and which can
    /// therefore describe one named item of it rather than only "the" pause.
    ///
    /// Optional for the same reason the interface above it is, and separate from it for a stronger one:
    /// implementing it is what a step DECLARES when it opts into simultaneity. Milestone 6.11's blocked
    /// respawn implements only the single door and stays one Place at a time, which is correct - a
    /// placement changes the board the next placement reads, so those items are genuinely sequential.
    /// Rulebook p.15's leveling implements this one, because a hero's cards, coins and Level are that
    /// hero's own and nothing one hero chooses changes what another is offered.
    /// </summary>
    public interface IDescribingConcurrentSystemTransitionStepRules : IDescribingSystemTransitionStepRules
    {
        /// <summary>
        /// Describes one named open item of the paused step, or returns null when these rules did not
        /// produce it or the pause is not waiting on that item. Reading changes nothing.
        /// </summary>
        SystemTransitionStepDescription DescribeOpenItem(
            PendingSystemTransitionStep pendingStep, SystemTransitionItemId itemId);
    }
}
