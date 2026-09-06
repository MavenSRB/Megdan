using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Dodger the Warlock's nineteen printed faces as definition content - milestone 8.35z28, and the
    /// seventh <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built. She is
    /// the demo set's last hero under the user's 2026-09-05 roster-expansion amendment.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces by the read-only inventory worker whose
    /// verified draft is <c>tmp/8-35d-8-35e-multiplayer-corrections/worker-hero-dodger/INVENTORY.md</c>.
    /// Her governed <c>Docs/AI/Rules/HERO_DODGER.md</c> shard is created at her own ceremony, from that
    /// file; the order, the names and the column meanings match it, so a reviewer can audit one against
    /// the other without holding anything in their head. Nothing is derived, for the reason
    /// <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>One board fact drives nine of her nineteen faces.</b> Both Gold cards, the Silver basic,
    /// all three Blue Defense primaries and three of the five Green Skills condition on empty spawn
    /// points in the Battle Zone. No shipped hero has a single derived board quantity carrying this much
    /// of a deck, and the quantity itself is
    /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/> - one derivation
    /// reached three ways, because a Defense value is a number, a ritual is a step, and a Silver basic's
    /// clause is a narrowing on who may be picked.</para>
    ///
    /// <para><b>Two user rulings are settled content here and are not re-derived.</b> RULING 4
    /// (2026-09-06) answers D-01: "empty spawn points" means <em>minion</em> spawn points only, matching
    /// printed p.14's own count of Vampiric Shield. The DESIGNER RULING of the same session - captured
    /// from the official "rules-questions-goa2" Discord, 2026-07-25, on Aegis of Doom - answers the
    /// anchoring: Dodger herself need not stand in the Battle Zone, only the counted spawn points must.
    /// Both live in the query above rather than in this file.</para>
    ///
    /// <para><b>What is different from the six sets already built, and therefore what this file is the
    /// first to state.</b> Every Attack she owns is Ranged - all five Red cards and both Gold cards
    /// print <c>Attack - Ranged</c>, a first in the verified roster and what her Sniper role is made of,
    /// which also means every one of her attacks is answerable by a "Block a ranged attack" card. Three
    /// faces print a <em>conditional</em> Defense value - <c>3+</c>, <c>4+</c> and <c>4+</c> - which
    /// printed p.14 illustrates on Vampiric Shield itself and names outright: "Primary Defense Value 4
    /// (The '+' sign signifies that the value can be increased by the card text.)" So the printed value
    /// is the plain number and the <c>+</c> is the card's own text, declared here as a
    /// <see cref="RosterValueContributionDefinition"/> exactly as <see cref="TigerclawCardContent"/>'s
    /// printed <c>5+</c> is - and she is the first hero to print one on a Defense.</para>
    ///
    /// <para><b>What this phase composes and what it parks, and why.</b> Eleven faces compose. Rusty
    /// Razor is p.20's Handicap and carries the standing deferral the six earlier sets' Handicaps carry.
    /// The remaining seven are parked on questions the user has not yet answered, each naming its own
    /// question id through <see cref="PrintedTextComposition.DeferredUntil"/>: Middlefinger of Death
    /// (D-06, what one Attack action with two targets is), Weakness and Enfeeblement (D-08, which layer
    /// a "-N Attack" reduces; Enfeeblement also D-09, what "cannot repeat actions" forbids), Darkest
    /// Ritual (D-04, whether its two clauses are independent, and D-05), Necromancy and Necromastery
    /// (D-07, which minion is respawned and who chooses), and Tide of Darkness (D-02 and D-03, how far
    /// the Ultimate reaches). A parked face parks whole: <see cref="PrintedTextComposition"/> refuses to
    /// coexist with an effect root, which is the same statement <see cref="TigerclawCardContent"/> makes
    /// about Nimble Strike - "a deferred face composes no step-1 text for the binding to be about. Its
    /// target binding ships with its composition, exactly as its nodes do." Their printed values and
    /// words ship here as inventory all the same; what is deferred is the composition, not the
    /// card.</para>
    ///
    /// <para><b>What her set does not print, which is content too.</b> She prints no p.14 Block, no
    /// non-Ranged Attack, and no Movement primary. No face of hers names, places, gives or targets a
    /// token or a marker, so this milestone adds no component content at all - ledger question 14
    /// already rules that a hero's tokens are defined by that hero's card faces, and hers name none.
    /// Death Trap prints no Movement icon at all, which is a real constraint on play because it sits in
    /// her opening hand.</para>
    ///
    /// <para><b>Transcription notes that are printed facts, not typos of this file.</b> Darkest Ritual's
    /// reminder reads <c>wtih</c>, not <c>with</c> - verified at 9x magnification - and that line ends
    /// without a full stop where Weakness's reminder ends with one inside its parentheses. Darkest
    /// Ritual also prints a capital <c>If</c> immediately after its semicolon. Both Gold cards print
    /// <c>battlezone</c> as one word where all nine other spawn-point faces print <c>battle zone</c> as
    /// two (D-12). Under the ledger's authority hierarchy the face wins; none of these is corrected
    /// here.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class DodgerCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="XargathaCardContent.ContentRevision"/> has: it moves when Dodger's content does.
        ///
        /// <para>Moved to <c>rev-2</c> at milestone 8.35z28, which composed six faces the user's
        /// 2026-09-06 rulings unparked. <b>This bump owes no
        /// <c>ConfiguredCardArtworkCatalog.json</c> rows</b>, and that is measured rather than assumed:
        /// milestone 8.35l's trap is that a content-revision bump owes the artwork catalog, but that
        /// catalog's <c>Configured</c> list names the six dealt heroes explicitly and Dodger is in
        /// none of it, because no phase has dealt her. Her artwork rows and her identity pin arrive
        /// together at her dealing milestone, which is the same milestone that registers her contracts
        /// - three debts with one cause, and none of them separable.</para>
        /// </summary>
        public const string ContentRevision = "dodger-8-35x-rev-2";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "dodger";

        /// <summary>
        /// The named trigger a face parked on an unanswered decision-list question carries, spelled once
        /// so a reader can grep the question ids and so two faces gated on one question cannot drift
        /// apart in how they say so.
        ///
        /// <para>It is <see cref="PrintedTextComposition.DeferredUntil"/> rather than
        /// <see cref="PrintedTextComposition.AwaitedAt"/> because that shape is the one whose statement
        /// is true: "the condition that would schedule it". These compositions are not owed by a
        /// milestone that could simply be run - they are owed by an answer, and inventing a milestone id
        /// for work nobody can yet schedule would be exactly the free-floating "later" that type exists
        /// to refuse.</para>
        /// </summary>
        private static string AwaitingRuling(string questionId) =>
            $"the user's ruling on question {questionId}";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId DreadRazor { get; } = Id("dread-razor");
        public static CardDefinitionId RustyRazor { get; } = Id("rusty-razor");
        public static CardDefinitionId DeathTrap { get; } = Id("death-trap");
        public static CardDefinitionId LittlefingerOfDeath { get; } = Id("littlefinger-of-death");
        public static CardDefinitionId FingerOfDeath { get; } = Id("finger-of-death");
        public static CardDefinitionId BurningSkull { get; } = Id("burning-skull");
        public static CardDefinitionId MiddlefingerOfDeath { get; } = Id("middlefinger-of-death");
        public static CardDefinitionId BlazingSkull { get; } = Id("blazing-skull");
        public static CardDefinitionId ShieldOfDecay { get; } = Id("shield-of-decay");
        public static CardDefinitionId VampiricShield { get; } = Id("vampiric-shield");
        public static CardDefinitionId Weakness { get; } = Id("weakness");
        public static CardDefinitionId AegisOfDoom { get; } = Id("aegis-of-doom");
        public static CardDefinitionId Enfeeblement { get; } = Id("enfeeblement");
        public static CardDefinitionId DarkRitual { get; } = Id("dark-ritual");
        public static CardDefinitionId DarkerRitual { get; } = Id("darker-ritual");
        public static CardDefinitionId Necromancy { get; } = Id("necromancy");
        public static CardDefinitionId DarkestRitual { get; } = Id("darkest-ritual");
        public static CardDefinitionId Necromastery { get; } = Id("necromastery");
        public static CardDefinitionId TideOfDarkness { get; } = Id("tide-of-darkness");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the six earlier sets' are. Dodger needs
        // four: she prints no non-Ranged Attack, so there is no melee Attack identity here, and she
        // prints no Movement primary, so there is no primary Movement one either.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        /// <summary>
        /// p.14's "Primary Defense Value", which her three Blue Defense cards print and which that page
        /// illustrates on one of them. Its identity is separate from the secondary Defense below because
        /// the two are different actions on different cards - no face of hers carries both - and only
        /// this one applies card text.
        /// </summary>
        public static ActionDefinitionId PrimaryDefense { get; } =
            new ActionDefinitionId("primary-defense");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Dread Razor.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            DreadRazor, DeathTrap, LittlefingerOfDeath, ShieldOfDecay, DarkRitual
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            FingerOfDeath, BurningSkull, MiddlefingerOfDeath, BlazingSkull,
            VampiricShield, Weakness, AegisOfDoom, Enfeeblement,
            DarkerRitual, Necromancy, DarkestRitual, Necromastery,
            TideOfDarkness
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            RustyRazor
        });

        /// <summary>
        /// All nineteen faces, in the order the inventory's printed-value table lists them.
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions { get; } = Array.AsReadOnly(
            Profiles().Select(Build).ToArray());

        /// <summary>
        /// The eighteen definitions a match deals from - everything but the boxed Handicap card.
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.AsReadOnly(
            AllDefinitions
                .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                .ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Dodger prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Dodger's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix " +
                    "that distinguishes one seat's cards from another's.",
                    nameof(instanceIdPrefix));
            }

            return Array.AsReadOnly(DealtDefinitions
                .Select(definition =>
                {
                    var instanceId = new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}");
                    return StartingHand.Contains(definition.Id)
                        ? CardInstance.CreateInHand(instanceId, definition.Reference, ownerId)
                        : CardInstance.CreateInDeck(instanceId, definition.Reference, ownerId);
                })
                .ToArray());
        }

        /// <summary>
        /// One seat's whole setup as a hand the configured match can be composed from: p.6's five in
        /// hand, the thirteen its deck keeps, and the eighteen definitions both draw on.
        /// </summary>
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(
                DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }

        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        // -----------------------------------------------------------------------------------------
        // The printed faces. One row per card, in the inventory's table order, with the same columns:
        // Initiative, the two secondary icons (0 where the face prints none), the primary action and
        // its value, the p.9 Range or Radius icon, the progression content, the transcribed text, and
        // the composition each face either carries or explicitly owes.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<DodgerCardProfile> Profiles() => new[]
        {
            // Her Gold basic, and the first verified basic Attack whose reach is bought by a board
            // condition rather than printed outright. p.12's prose reads like a contradiction until the
            // gate is noticed - "the only Range 2 attack Dodger has is on her Red card", while this card
            // also prints Range 2 - and the two agree because this card's ranged bullet is gated and the
            // Red card's is not, so it is not an attack she simply has.
            new DodgerCardProfile(
                DreadRazor, "Dread Razor", 12, 1, 1,
                DodgerPrimaryKind.RangedAttack, 2, PrintedValueChannel.Range, 2,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Target a unit adjacent to you. • If you are adjacent to an empty spawn " +
                "point in the battlezone, target a unit in range.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike and Xargatha's
            // Desperate Cleave are, by the same 2026-08-21 user decision: p.6 returns it to the box and
            // the six-player pitch never brings it back. What it says that Dread Razor does not is on
            // the row rather than in the deferral - both of its bullets name a hero where the ordinary
            // Gold card names any unit - and the narrowing ships because it is transcription, while the
            // card-text target binding does not, because that is composition.
            new DodgerCardProfile(
                RustyRazor, "Rusty Razor", 11, 1, 1,
                DodgerPrimaryKind.RangedAttack, 1, PrintedValueChannel.Range, 2,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Target a hero adjacent to you. • If you are adjacent to an empty spawn " +
                "point in the battlezone, target a hero in range.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly),

            // Her Silver basic and her only basic Skill, so any future card keying off "a basic skill"
            // reads this action and no other. Its secondary block was verified four independent ways
            // under the 2026-09-06 user quality directive: Sec Def 1, and NO secondary Movement. The
            // absence is recorded as an absence - p.9 makes a secondary available only where its icon is
            // printed - and it is a real constraint on play, because this card sits in her opening hand.
            new DodgerCardProfile(
                DeathTrap, "Death Trap", 7, 1, 0,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "An enemy hero in radius who is adjacent to an empty spawn point in the battle zone " +
                "discards a card, if able.",
                PrintedTextComposition.None),

            // The Red line. Five Ranged Attacks: three modal ones whose second bullet reads the target
            // hero's own discard pile, and two whose rider shoves the minions standing beside her away.
            new DodgerCardProfile(
                LittlefingerOfDeath, "Littlefinger of Death", 9, 5, 4,
                DodgerPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    FingerOfDeath, BurningSkull),
                "Choose one — • Target a unit adjacent to you. • Target a hero in range who has one or " +
                "more cards in the discard.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            new DodgerCardProfile(
                FingerOfDeath, "Finger of Death", 9, 6, 4,
                DodgerPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    MiddlefingerOfDeath, BlazingSkull),
                "Choose one — • Target a unit adjacent to you. • Target a hero in range who has one or " +
                "more cards in the discard.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            new DodgerCardProfile(
                BurningSkull, "Burning Skull", 9, 5, 4,
                DodgerPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    MiddlefingerOfDeath, BlazingSkull),
                "Target a unit in range. After the attack: Move up to 1 minion adjacent to you 1 space " +
                "to a space not adjacent to you.",
                PrintedTextComposition.None),

            // STILL PARKED, but no longer on a question. The user's 2026-09-06 ruling on D-06 answered
            // it: "choose one or both" resolves the complete p.14 order against target one and then
            // completely against target two, each defending separately. What this face now waits on is
            // a seam the engine does not have.
            //
            // The only shape that produces a second complete p.14 sequence today is
            // EffectNodeKind.RepeatAction, and composing this card on it would be wrong in a way a
            // green harness would never show. p.10 keeps Modal Actions and Repeat as two different
            // rules, and the user's ruling on D-09 scopes Enfeeblement's "cannot repeat actions" to the
            // Repeat keyword alone - so a Middlefinger built as a repeat would have its second bullet
            // silently forbidden by Enfeeblement, which is a rule neither card prints. That is a
            // measured interaction rather than a worry: this same milestone composed the restriction
            // and its two gates, so the defect would exist the moment both faces were composed.
            //
            // The face's printed values, its words and its Tier III item icon ship here, exactly as
            // they did in phase one; the composition and its card-text target binding wait together
            // for a modal second-sequence seam that is not a repeat.
            new DodgerCardProfile(
                MiddlefingerOfDeath, "Middlefinger of Death", 10, 6, 4,
                DodgerPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Choose one or both (on different targets) — • Target a unit adjacent to you. • Target " +
                "a hero in range who has one or more cards in the discard.",
                PrintedTextComposition.DeferredUntil(
                    "the modal second-sequence seam - one Attack action driving two complete p.14 " +
                    "orders without being p.10's Repeat")),

            new DodgerCardProfile(
                BlazingSkull, "Blazing Skull", 10, 5, 4,
                DodgerPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Target a unit in range. After the attack: Move each of up to 2 minions adjacent to " +
                "you 1 space to a space not adjacent to you.",
                PrintedTextComposition.None),

            // The Blue Defense line, and the printed `3+` / `4+` / `4+` that printed p.14 uses Vampiric
            // Shield to explain. All three are plain Defense primaries, so their text reaches the table
            // only when the card is played for its primary Defense - which is exactly what that page's
            // worked example demonstrates and contrasts against a Defense used as a secondary.
            new DodgerCardProfile(
                ShieldOfDecay, "Shield of Decay", 10, 0, 3,
                DodgerPrimaryKind.Defense, 3, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    VampiricShield, Weakness),
                "+2 [Defense] Defense if there are 2 or more empty spawn points in radius in the " +
                "battle zone.",
                PrintedTextComposition.None,
                raisedDefense: 2),

            new DodgerCardProfile(
                VampiricShield, "Vampiric Shield", 10, 0, 3,
                DodgerPrimaryKind.Defense, 4, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                    AegisOfDoom, Enfeeblement),
                "+2 [Defense] Defense if there are 2 or more empty spawn points in radius in the " +
                "battle zone.",
                PrintedTextComposition.None,
                raisedDefense: 2),

            // PARKED on D-08: the face prints the Attack icon and the word "Attack", which names the
            // channel but not the layer p.14 applies it at - the printed Attack value on the enemy's
            // card, or the Attack Total the page builds from it. Its reminder settles one thing either
            // way and is recorded rather than composed: there is no floor at zero.
            new DodgerCardProfile(
                Weakness, "Weakness", 10, 4, 3,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    AegisOfDoom, Enfeeblement),
                "This turn: Enemy heroes in radius have -4 [Attack] Attack. ( They can still attack, " +
                "even with a negative attack value. )",
                PrintedTextComposition.None),

            new DodgerCardProfile(
                AegisOfDoom, "Aegis of Doom", 11, 0, 3,
                DodgerPrimaryKind.Defense, 4, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Initiative),
                "+4 [Defense] Defense if there are 2 or more empty spawn points in radius in the " +
                "battle zone.",
                PrintedTextComposition.None,
                raisedDefense: 4),

            // PARKED on D-08 and D-09. It carries Weakness's open question about which layer the
            // reduction reaches, and adds its own: what "cannot repeat actions" forbids - p.10's Repeat
            // keyword alone, or anything that re-performs an action - and when that is checked, which is
            // a live question because an Active effect's radius follows its hero.
            new DodgerCardProfile(
                Enfeeblement, "Enfeeblement", 11, 4, 3,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "This turn: Enemy heroes in radius have -6 [Attack] Attack and cannot repeat actions.",
                PrintedTextComposition.None),

            // The Green rituals. She is the first hero who is paid for the board emptying out: these
            // farm coins from removed minions' vacant spawn points, while her Necromancy pair refills
            // the very points the rest of her deck wants empty - an internal tension that is content.
            new DodgerCardProfile(
                DarkRitual, "Dark Ritual", 3, 2, 2,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    DarkerRitual, Necromancy),
                "If there are 2 or more empty spawn points in radius in the battle zone, gain 1 coin.",
                PrintedTextComposition.None),

            new DodgerCardProfile(
                DarkerRitual, "Darker Ritual", 3, 2, 2,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Defense,
                    DarkestRitual, Necromastery),
                "If there are 2 or more empty spawn points in radius in the battle zone, gain 2 coins.",
                PrintedTextComposition.None),

            // p.19's Respawn keys availability to minion type and colour and requires that something
            // died first. The user's 2026-09-06 ruling on D-07 settles the two open halves: the chosen
            // Spawn point's type fixes what comes back (p.8's matching-type precedent), and Heavies are
            // excluded - not by an added rule but as a consequence of p.13's immunity plus the Lane
            // Push, which is why the implementation states p.19's count as printed and proves the
            // exclusion instead of writing it. The face prints no Range or Radius icon, which is why
            // its row states none and its reach is the sentence's own "adjacent to you".
            new DodgerCardProfile(
                Necromancy, "Necromancy", 3, 2, 2,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    DarkestRitual, Necromastery),
                "Respawn a friendly minion in an empty friendly spawn point adjacent to you in the " +
                "battle zone.",
                PrintedTextComposition.None),

            // Two clauses under one semicolon, and the user's 2026-09-06 ruling on D-04 makes them
            // INDEPENDENT: the capital `If` after the semicolon starts a new sentence, so the item is
            // gained on having the Ultimate whatever the spawn-point count says. p.16 makes that
            // consequential rather than academic - "Once you unlock your Ultimate, you cannot gain any
            // additional levels, but can still gain coins" - so after the Ultimate this clause is the
            // only route to a new item in the game, and D-05 rules it unbounded and permanent.
            new DodgerCardProfile(
                DarkestRitual, "Darkest Ritual", 2, 3, 2,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Defense),
                "If there are 2 or more empty spawn points in radius in the battle zone, gain 2 coins; " +
                "If you have Ultimate, gain +1 [Attack] Attack item. ( Use any spare card from the box " +
                "wtih the corresponding icon )",
                PrintedTextComposition.None),

            new DodgerCardProfile(
                Necromastery, "Necromastery", 2, 3, 2,
                DodgerPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Range),
                "Respawn a friendly minion in an empty friendly spawn point in radius in the battle " +
                "zone.",
                PrintedTextComposition.None),

            // p.16's permanent ability, and the first verified Ultimate that is a global overlay rather
            // than a triggered or standing ability about its own hero: it rewrites what every space on
            // the board counts as. The user's 2026-09-06 rulings settle how far it reaches - D-02, that
            // a Defense action is squarely inside "an action" even though it is performed inside
            // another hero's Attack, and D-03, that a Spawn point it grants satisfies the word "empty"
            // wherever the space itself is, which is what turns all nine of its beneficiaries on. It
            // prints no Initiative, no icons and no actions at all, which is what makes the row's zeros
            // a statement rather than an omission.
            new DodgerCardProfile(
                TideOfDarkness, "Tide of Darkness", 0, 0, 0,
                DodgerPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "While you are performing an action, all spaces count as if they were in the battle " +
                "zone and had a friendly minion spawn point.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(DodgerCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == DodgerPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Id.Equals(TideOfDarkness)
                    // p.16's permanent ability, composed at milestone 8.35z28 from the user's
                    // 2026-09-06 rulings on D-02 and D-03. One printed sentence, one declaration: the
                    // rule is named in full by
                    // RuleModificationKind.AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone,
                    // because the sentence carries no number, no channel and no named type for a
                    // property to hold - and because adding a public property to RuleModification would
                    // move the identity digest of every shipped hero whose Ultimate reaches it.
                    ? UltimateRuleDefinition.PermanentAbility(
                        "dodger-tide-of-darkness",
                        "DodgerTideOfDarkness",
                        new[]
                        {
                            RuleModification
                                .ForAllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone()
                        })
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(DodgerCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the six earlier sets express it. Her
            // dashes are Death Trap's Movement column, the three Blue Defense primaries' Defense column
            // - where the value is the primary's - and the Ultimate's whole row.
            if (profile.SecondaryMovement > 0)
            {
                actions.Add(new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Movement, profile.SecondaryMovement)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)));
            }

            if (profile.SecondaryDefense > 0)
            {
                actions.Add(new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Defense, profile.SecondaryDefense)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            }

            return actions;
        }

        private static ActionDefinition BuildPrimary(DodgerCardProfile profile)
        {
            switch (profile.Primary)
            {
                case DodgerPrimaryKind.None:
                    return null;

                case DodgerPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        DodgerEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        DodgerEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged),
                        null,
                        MinionDefenseContributionPolicy.IncludeAll);

                case DodgerPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        DodgerEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case DodgerPrimaryKind.Defense:
                    // p.14's "Primary Defense Value", carried by all three of her Blue Defense cards and
                    // illustrated on one of them. It costs and resolves exactly as every other Defense
                    // does - discarded from hand, in response to an attack, compared against the Attack
                    // Total - and what is new is the printed Radius icon beside the number and the `+`
                    // the card's own text supplies.
                    //
                    // The `+` is declared rather than composed, for the reason TigerclawCardContent
                    // records about its own printed `5+`: it is not a step. Nobody chooses anything,
                    // nothing happens on the board, and the clause is read when p.14 computes the
                    // Defense Total. An effect node describing it would be describing the same rule in a
                    // second vocabulary, which is why these three cards carry no effect root at all -
                    // their whole sentence is that declaration.
                    return new ActionDefinition(
                        PrimaryDefense,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        null,
                        profile.TextComposition,
                        PrintedDefenseResolution.Value,
                        CardTextTiming.None,
                        null,
                        RaisedDefense(profile),
                        MinionDefenseContributionPolicy.IncludeAll);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Dodger primary action kind.");
            }
        }

        /// <summary>
        /// p.14's printed <c>+</c> on a Defense value, as the typed declaration that makes it true - or
        /// nothing on the sixteen faces that print a plain number or none at all.
        ///
        /// <para>All three faces print the same condition - "2 or more empty spawn points in radius in
        /// the battle zone" - and differ only in the amount, which is why the threshold is named by the
        /// query and the amount by the row. The disk is the card's own printed Radius icon, so p.16's
        /// Radius item widens the question these cards ask; the "2 or more" is a number the sentence
        /// writes down, and p.16 never raises one of those.</para>
        /// </summary>
        private static IEnumerable<RosterValueContributionDefinition> RaisedDefense(
            DodgerCardProfile profile) =>
            profile.RaisedDefense == 0
                ? Enumerable.Empty<RosterValueContributionDefinition>()
                : new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Defense,
                        RosterValueQueryKind
                            .TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius,
                        profile.RaisedDefense)
                };

        /// <summary>
        /// What one printed Attack of Dodger's may target, or <c>null</c> where the face narrows nothing
        /// and p.13's own reading for the subtype stands.
        ///
        /// <para>One of her seven Attacks states something p.13 does not: Rusty Razor prints "a hero" in
        /// both of its bullets, which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/>
        /// names. The rest print "a unit" for the target the action itself takes.</para>
        ///
        /// <para>Three of them name their own target instead - Dread Razor, Littlefinger of Death and
        /// Finger of Death, whose modal bullets <em>are</em> the targeting sentence - so p.14 does not
        /// ask the ordinary target question first. That is <c>AttackTargetBinding.ByCardText</c>, which
        /// milestone 8.21 minted for Brogan's two modal throws and which these three reach for the
        /// identical printed reason. Middlefinger of Death prints the same shape and is excluded,
        /// because a deferred face composes no step-1 text for the binding to be about; its target
        /// binding ships with its composition, exactly as its nodes do.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            DodgerCardProfile profile, AttackRangeKind attackRange)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            if (profile.TargetableUnits != AttackTargetableUnits.HeroesAndMinions)
            {
                classification = new PrintedAttackTargetPolicy(
                    profile.TargetableUnits, classification.Reach, classification.Line);
            }

            if (profile.TargetBoundByCardText && !profile.TextComposition.IsAwaited)
            {
                return classification.BoundByCardText();
            }

            return profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions
                ? (PrintedAttackTargetPolicy?)null
                : classification;
        }

        /// <summary>The p.9 Range or Radius icon a face prints, or nothing where it prints neither.</summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(DodgerCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum DodgerPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,

            /// <summary>
            /// Every Attack she owns. There is no non-Ranged member here because there is no
            /// non-Ranged Attack in her set - a first in the verified roster.
            /// </summary>
            RangedAttack = 1,

            Skill = 2,

            /// <summary>p.14's Defense printed as the card's primary action, carrying its own value.</summary>
            Defense = 3
        }

        private sealed class DodgerCardProfile
        {
            public DodgerCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                DodgerPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                bool targetBoundByCardText = false,
                int raisedDefense = 0)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Primary = primary;
                PrimaryValue = primaryValue;
                IconChannel = iconChannel;
                IconValue = iconValue;
                Progression = progression;
                PrintedText = printedText;
                TextComposition = textComposition;
                TargetableUnits = targetableUnits;
                TargetBoundByCardText = targetBoundByCardText;
                RaisedDefense = raisedDefense;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public DodgerPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>Whether this face's own bullets name the target p.14 would otherwise ask for.</summary>
            public bool TargetBoundByCardText { get; }

            /// <summary>What this face's printed `+` adds to its Defense, or zero on the sixteen without one.</summary>
            public int RaisedDefense { get; }
        }
    }
}
