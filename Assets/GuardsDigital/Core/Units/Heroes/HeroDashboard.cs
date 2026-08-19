using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Units.Heroes
{
    /// <summary>
    /// Which of rulebook p.16's four item slots a position names, counted from the left of the
    /// dashboard as the page counts them.
    ///
    /// It is a typed position rather than an integer or an icon because milestone 7.2a's first
    /// acceptance check says a slot is never interchangeable with an icon. p.16 identifies both ways
    /// in the same paragraph - it magnifies "the first item slot of his dashboard which has the same
    /// icon" and it places an item "under the third slot of his dashboard" - and the fourth slot
    /// carries three icons, so the two identifications stop agreeing there. A position that was a
    /// <see cref="PrintedValueChannel"/> could not name that slot at all, and one that was a bare
    /// integer would be assignable from an item's order.
    /// </summary>
    public readonly struct HeroDashboardSlotPosition : IEquatable<HeroDashboardSlotPosition>
    {
        public HeroDashboardSlotPosition(int index)
        {
            if (index < 1 || index > HeroDashboard.SlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"A hero dashboard has {HeroDashboard.SlotCount} item slots, numbered from 1 " +
                    "(rulebook p.16).");
            }

            Index = index;
        }

        /// <summary>The 1-based slot number. Zero on a default value, which names no slot.</summary>
        public int Index { get; }

        public bool IsDefined => Index >= 1 && Index <= HeroDashboard.SlotCount;

        public bool Equals(HeroDashboardSlotPosition other) => Index == other.Index;

        public override bool Equals(object obj) =>
            obj is HeroDashboardSlotPosition other && Equals(other);

        public override int GetHashCode() => Index;

        public override string ToString() => IsDefined ? $"slot {Index}" : "no slot";
    }

    /// <summary>
    /// One item slot on a hero dashboard: where it sits, and which printed icons it accepts an item
    /// for.
    ///
    /// A slot accepts a set rather than a single icon because the fourth one does. p.16's dashboard
    /// strip carries six icons above four tucked-card positions, and the user supplied the grouping
    /// from the component on 2026-08-19: Attack, Defense, Initiative, and a fourth taking Range,
    /// Movement and Radius together. So an item under the fourth slot may be a +1 Range, a +1 Movement
    /// or a +1 Radius item, and the slot alone cannot say which - which is why
    /// <see cref="GuardsDigital.Core.Rules.Progression.HeroItem"/> records both the slot it occupies
    /// and the icon it supplies, and why the Item Bonus reads the icon.
    /// </summary>
    public sealed class HeroDashboardSlot : IEquatable<HeroDashboardSlot>
    {
        public HeroDashboardSlot(
            HeroDashboardSlotPosition position,
            params PrintedValueChannel[] acceptedIcons)
            : this(position, (IEnumerable<PrintedValueChannel>)acceptedIcons)
        {
        }

        public HeroDashboardSlot(
            HeroDashboardSlotPosition position,
            IEnumerable<PrintedValueChannel> acceptedIcons)
        {
            if (!position.IsDefined)
            {
                throw new ArgumentException(
                    "An item slot states which of the dashboard's positions it is.", nameof(position));
            }

            var icons = (acceptedIcons ?? throw new ArgumentNullException(nameof(acceptedIcons)))
                .ToArray();
            if (icons.Length == 0)
            {
                throw new ArgumentException(
                    "An item slot accepts at least one of rulebook p.16's icons.", nameof(acceptedIcons));
            }

            if (icons.Any(icon =>
                    icon == PrintedValueChannel.None ||
                    !Enum.IsDefined(typeof(PrintedValueChannel), icon)))
            {
                throw new ArgumentException(
                    "An item slot accepts named printed icons (rulebook p.16).", nameof(acceptedIcons));
            }

            if (icons.Distinct().Count() != icons.Length)
            {
                throw new ArgumentException(
                    "An item slot names each icon it accepts once.", nameof(acceptedIcons));
            }

            Position = position;
            AcceptedIcons = Array.AsReadOnly(icons);
        }

        public HeroDashboardSlotPosition Position { get; }

        /// <summary>The icons this slot accepts an item for, in the order the dashboard prints them.</summary>
        public IReadOnlyList<PrintedValueChannel> AcceptedIcons { get; }

        public bool Accepts(PrintedValueChannel icon) =>
            icon != PrintedValueChannel.None && AcceptedIcons.Contains(icon);

        public bool Equals(HeroDashboardSlot other) =>
            other != null &&
            Position.Equals(other.Position) &&
            AcceptedIcons.SequenceEqual(other.AcceptedIcons);

        public override bool Equals(object obj) => Equals(obj as HeroDashboardSlot);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                foreach (var icon in AcceptedIcons)
                {
                    hashCode = (hashCode * 397) ^ (int)icon;
                }

                return hashCode;
            }
        }

        public override string ToString() =>
            $"{Position} ({string.Join("/", AcceptedIcons.Select(icon => icon.ToString()))})";
    }

    /// <summary>
    /// The four item slots printed on the top of one hero's dashboard - rulebook p.16's "one of four
    /// slots on the top of your hero dashboard" - as authoritative hero content.
    ///
    /// Milestone 7.2 recorded which slot an item occupied without anything stating which slots a hero
    /// has, which left the page's correspondence an assumption: a placement could not refuse an item
    /// its owner had nowhere to put. This type is what the placement asks, and it is content rather
    /// than a constant so that a hero whose dashboard differs is expressible. It is a required
    /// argument of <see cref="GuardsDigital.Core.Participants.Hero"/> for the reason p.16's Item icon
    /// is a required argument of <c>CardProgressionProfile.NonBasicCard</c>: a hero whose slots were
    /// simply left out would be one that could never be told it has nowhere to put an item.
    ///
    /// It deliberately does not require the four slots to cover all six icons. A dashboard that
    /// accepts five of them is legal content, and refusing an item for the sixth is exactly the
    /// refusal this milestone adds; a type that made the coverage total would make that refusal
    /// unreachable and the check that proves it vacuous. What the content must not do is let a hero
    /// reach a card whose icon no slot accepts, and that is a question about a whole configuration
    /// rather than about one dashboard, which is why
    /// <see cref="GuardsDigital.Core.Rules.Progression.HeroItemSlotSetupValidator"/> asks it.
    /// </summary>
    public sealed class HeroDashboard : IEquatable<HeroDashboard>
    {
        /// <summary>p.16: an Item icon "matches the icon on one of four slots".</summary>
        public const int SlotCount = 4;

        public HeroDashboard(IEnumerable<HeroDashboardSlot> slots)
        {
            var declared = (slots ?? throw new ArgumentNullException(nameof(slots))).ToArray();
            if (declared.Any(slot => slot == null))
            {
                throw new ArgumentException("A dashboard's slots must not be null.", nameof(slots));
            }

            if (declared.Length != SlotCount)
            {
                throw new ArgumentException(
                    $"A hero dashboard declares exactly {SlotCount} item slots (rulebook p.16); this " +
                    $"one declares {declared.Length}.",
                    nameof(slots));
            }

            for (var index = 0; index < declared.Length; index++)
            {
                if (declared[index].Position.Index != index + 1)
                {
                    throw new ArgumentException(
                        $"A dashboard's item slots are its positions 1..{SlotCount} in order.",
                        nameof(slots));
                }
            }

            var accepted = declared.SelectMany(slot => slot.AcceptedIcons).ToArray();
            if (accepted.Distinct().Count() != accepted.Length)
            {
                throw new ArgumentException(
                    "No icon is accepted by two slots of one dashboard; p.16 matches an Item icon to " +
                    "the slot with that icon, which two slots would make ambiguous.",
                    nameof(slots));
            }

            Slots = Array.AsReadOnly(declared);
        }

        /// <summary>
        /// The layout every hero in this build declares.
        ///
        /// p.16 states the first and the third itself - the magnified Attack example is "the first item
        /// slot", and the Initiative example places its item "under the third slot" - and the user
        /// supplied the second and the fourth from the component on 2026-08-19, because the page text
        /// states neither and the page scan will not resolve the label strip further. It is one named
        /// layout rather than four literals repeated per hero, so that a build in which two heroes
        /// differ has to say so.
        /// </summary>
        public static HeroDashboard RulebookLayout { get; } = new HeroDashboard(new[]
        {
            new HeroDashboardSlot(new HeroDashboardSlotPosition(1), PrintedValueChannel.Attack),
            new HeroDashboardSlot(new HeroDashboardSlotPosition(2), PrintedValueChannel.Defense),
            new HeroDashboardSlot(new HeroDashboardSlotPosition(3), PrintedValueChannel.Initiative),
            new HeroDashboardSlot(
                new HeroDashboardSlotPosition(4),
                PrintedValueChannel.Range,
                PrintedValueChannel.Movement,
                PrintedValueChannel.Radius)
        });

        public IReadOnlyList<HeroDashboardSlot> Slots { get; }

        public HeroDashboardSlot GetSlot(HeroDashboardSlotPosition position)
        {
            if (!position.IsDefined)
            {
                throw new ArgumentException("A slot lookup states a position.", nameof(position));
            }

            return Slots[position.Index - 1];
        }

        /// <summary>
        /// p.16's match: the slot this dashboard accepts an item with that icon under, if it has one.
        /// False is an ordinary answer and the one a hero with no slot for the icon gives.
        /// </summary>
        public bool TryFindSlotAccepting(PrintedValueChannel icon, out HeroDashboardSlot slot)
        {
            slot = Slots.FirstOrDefault(candidate => candidate.Accepts(icon));
            return slot != null;
        }

        public bool Accepts(PrintedValueChannel icon) => TryFindSlotAccepting(icon, out _);

        public bool Equals(HeroDashboard other) =>
            other != null && Slots.SequenceEqual(other.Slots);

        public override bool Equals(object obj) => Equals(obj as HeroDashboard);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SlotCount;
                foreach (var slot in Slots)
                {
                    hashCode = (hashCode * 397) ^ slot.GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString() => string.Join(", ", Slots.Select(slot => slot.ToString()));
    }
}
