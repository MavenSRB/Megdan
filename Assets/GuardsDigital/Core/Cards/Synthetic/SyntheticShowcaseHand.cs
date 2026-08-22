using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>
    /// The synthetic mechanics-showcase hands: ten cards whose printed actions the Phase 6 showcase
    /// milestones fixed and milestone 7.9 gated, plus the fixture Skill cards a named factory adds.
    ///
    /// <para><b>What this used to be.</b> Until milestone 8.4 this was <c>PlaceholderMovementHand</c>,
    /// and the temporary-structure register described it exactly: a synthetic five-card hand standing
    /// in for verified hero content, with milestone 8.4 named as the owner of its retirement. The
    /// standing-in is what retired. The configured session deals
    /// <see cref="GuardsDigital.Core.Cards.Verified.WaspCardContent"/> now, and these cards kept only
    /// the job they were always good at: exercising machinery that verified content has no card for
    /// yet, in fixtures that say so in their names. Nothing here is authority for any rule.</para>
    ///
    /// <para>The hand shape itself moved to <see cref="ConfiguredHand"/>, which both this content and
    /// the verified content fill, because a hand has the same shape whoever printed the cards.</para>
    /// </summary>
    public static class SyntheticShowcaseHand
    {
        /// <summary>
        /// Milestone 7.1's colour and Tier assignment for the two configured hands, and why each hand
        /// looks the way it does.
        ///
        /// The shape is the rulebook's own starting hand. Page 6 deals "the three Tier I cards and the
        /// two basic cards with no Tier", page 15 says the hand holds "exactly 5 cards, 1 of each
        /// color", and each hand below is exactly that: five distinct colours, the Gold and Silver pair
        /// Basic and the other three Tier I.
        ///
        /// The assignment is then chosen so that no derivation could reproduce it, which is what
        /// acceptance check two asks for. Card colour is not team identity: the Red team holds the
        /// Red-coloured Red Bombard and the Blue-coloured Red Bulwark, and the Blue team holds the
        /// Red-coloured Blue Sentinel and the Blue-coloured Blue Feint, so no reading of
        /// <c>TeamSide</c> produces either column. It is not the display name: every card in a hand
        /// shares that hand's word, and the five colours in each hand are distinct, so a name-derived
        /// rule would have to give all five the same answer. It is not the hand slot: the Basic pair
        /// sits at positions one and three of the Red hand and three and five of the Blue one. And it
        /// is not the printed action icon, which p.7 ties to colour for official cards - Red Lancer
        /// prints a Ranged Attack and is Gold, Red Bulwark prints a Skill and is Blue, Blue Hammer
        /// prints an Attack and is Green. That deliberate mismatch is the point: these are synthetic
        /// cards whose primary actions the showcase milestones fixed, milestone 7.9 gates that matrix
        /// unchanged, and nothing in the implementation may read a colour off an icon.
        ///
        /// Every one of the ten therefore advances to <c>phase-7-1-rev-1</c>. No printed value, action,
        /// effect node, binding, capability or Initiative moves; the definitions gain progression
        /// content and nothing else, and the six converted Skill cards' effect definitions inherit the
        /// new string only through the revision coupling this file has always had.
        /// </summary>
        private static readonly SyntheticCardProfile[] RedProfiles =
        {
            SyntheticCardProfile.WithPushPlaceSkill(
                "red-needle", "Red Needle", 13, 2, 1, CardProgressionProfile.BasicCard(CardColor.Silver)),
            SyntheticCardProfile.WithModificationSkill(
                "red-bulwark", "Red Bulwark", 11, 2, 3, TierOne("red-bulwark", CardColor.Blue)),
            new SyntheticCardProfile(
                "red-lancer", "Red Lancer", 9, AttackRangeKind.Ranged, 2, 3, 3, 1,
                CardProgressionProfile.BasicCard(CardColor.Gold)),
            new SyntheticCardProfile(
                "red-pursuit", "Red Pursuit", 7, AttackRangeKind.NonRanged, 4, 0, 3, 2,
                TierOne("red-pursuit", CardColor.Green)),
            SyntheticCardProfile.WithDirectMoveSpawnSkill(
                "red-bombard", "Red Bombard", 5, 2, 2, TierOne("red-bombard", CardColor.Red))
        };

        private static readonly SyntheticCardProfile[] BlueProfiles =
        {
            SyntheticCardProfile.WithImmunityTimingSkill(
                "blue-feint", "Blue Feint", 12, 3, 2, TierOne("blue-feint", CardColor.Blue)),
            SyntheticCardProfile.WithExchangeSkill(
                "blue-sentinel", "Blue Sentinel", 11, 2, 4, TierOne("blue-sentinel", CardColor.Red)),
            new SyntheticCardProfile(
                "blue-scout-shot", "Blue Scout Shot", 8, AttackRangeKind.Ranged, 2, 2, 4, 1,
                CardProgressionProfile.BasicCard(CardColor.Silver)),
            new SyntheticCardProfile(
                "blue-hammer", "Blue Hammer", 6, AttackRangeKind.NonRanged, 5, 0, 2, 1,
                TierOne("blue-hammer", CardColor.Green)),
            SyntheticCardProfile.WithPushRepeatSkill(
                "blue-longbow", "Blue Longbow", 4, 3, 2, CardProgressionProfile.BasicCard(CardColor.Gold))
        };

        /// <summary>
        /// The Tier I profile for a configured card, naming the two Tier II cards of its own colour
        /// that <see cref="SyntheticUpgradeTree"/> supplies for it.
        ///
        /// The two ids are written here rather than derived from the colour, because that is the
        /// distinction acceptance check five draws: colour constrains which upgrades are legal, and the
        /// relationship itself is content the card states. <see cref="CardProgressionValidator"/> then
        /// checks the stated pair against the colour and Tier rules over the whole set.
        /// </summary>
        private static CardProgressionProfile TierOne(string cardId, CardColor color) =>
            CardProgressionProfile.NonBasicCard(
                color,
                CardTier.I,
                // p.16 prints an Item icon on "every Tier II and Tier III Action card", so a Tier I card
                // states the absence of one rather than being allowed to omit the argument.
                PrintedValueChannel.None,
                SyntheticUpgradeTree.DefinitionId(cardId, CardTier.II, 'a'),
                SyntheticUpgradeTree.DefinitionId(cardId, CardTier.II, 'b'));

        /// <summary>
        /// Milestone 6.2a's Skill cards. Higher Initiative acts first, so Red's sits above every
        /// ordinary Red card and Blue's below every ordinary Blue card: the Red hero reliably wins the
        /// turn and can invoke the Skill without the fixture having to steer Initiative.
        /// </summary>
        private static readonly SyntheticSkillProfile RedSkillProfile =
            new SyntheticSkillProfile("red-focus", "Red Focus", 14);

        private static readonly SyntheticSkillProfile BlueSkillProfile =
            new SyntheticSkillProfile("blue-focus", "Blue Focus", 3);

        /// <summary>
        /// Milestone 6.2's Skill cards. They sit outside their team's ordinary Initiative band for the
        /// same reason the 6.2a pair does, so a fixture can reach the Skill without steering Initiative.
        ///
        /// They are separate cards rather than a second action on the 6.2a Skill card. A secondary Skill
        /// action does not exist in the rulebook - p.7 lists Skill only among the primary icons, and
        /// names Hold, Fast Travel and Clear as the secondary ones - so adding one would have invented
        /// content to save a card.
        ///
        /// Fixture-only again since the configured scene went back to five cards per hero. Milestone
        /// 6.2d had dealt the Blue one into the scene as its sixth card, purely so a player could put a
        /// token on the board; Blue Sentinel and Blue Feint now carry those three capabilities between
        /// them, and Red's copy was dropped rather than rehoused, so no Red profile moved. Both cards
        /// stay here because the eight-card Skill-fixture hand still deals them, and several fixtures
        /// exist to reach one operation family through them in isolation.
        /// </summary>
        private static readonly SyntheticSkillProfile RedComponentSkillProfile =
            new SyntheticSkillProfile("red-cache", "Red Cache", 15);

        private static readonly SyntheticSkillProfile BlueComponentSkillProfile =
            new SyntheticSkillProfile("blue-cache", "Blue Cache", 2);

        /// <summary>
        /// Milestone 6.4's Skill cards, sitting outside their team's ordinary Initiative band for the
        /// same reason both earlier pairs do. Red's is the highest Initiative in the fixture hand and
        /// Blue's the lowest, so the Red hero reliably acts first and a displacement fixture never has
        /// to steer Initiative to reach the Skill.
        ///
        /// Milestone 6.5 widened them from the displacement Skill into the fixture hand's board-object
        /// Skill: the same card now also carries p.18's Place and Spawn. A fifth pair of cards would
        /// have needed a fifth free Initiative outside the ordinary band, and Blue's band already
        /// reaches down to 1 with no positive value left below it, so a new pair could only have been
        /// bought by renumbering cards several pinned fixtures name. Separability is unaffected: the
        /// operations are branches of one `Choose`, so a fixture still reaches exactly the one it wants.
        /// </summary>
        private static readonly SyntheticSkillProfile RedDisplacementSkillProfile =
            new SyntheticSkillProfile("red-shove", "Red Shove", 16);

        private static readonly SyntheticSkillProfile BlueDisplacementSkillProfile =
            new SyntheticSkillProfile("blue-shove", "Blue Shove", 1);

        /// <summary>
        /// Milestone 6.7's Skill cards: a displacement `Choose one` followed by a `may` Repeat.
        ///
        /// A pair of their own rather than a seventh branch on the Shove card, because a Repeat's
        /// subject is the whole enclosing action - every fixture that reaches Shove today would gain a
        /// repeat prompt it never asked about, and several of them count the facts their action emits.
        /// They are dealt only by the repeat configuration, so the Shove hand and every digest measured
        /// against it are untouched.
        ///
        /// Red's 17 is the highest Initiative in any fixture hand, so the Red hero reliably acts first;
        /// Blue's 5 is the one value free inside Blue's ordinary band, which is where a Blue card has to
        /// sit now that the band reaches down to 1 and nothing positive is left below it.
        /// </summary>
        private static readonly SyntheticSkillProfile RedRepeatSkillProfile =
            new SyntheticSkillProfile("red-volley", "Red Volley", 17);

        private static readonly SyntheticSkillProfile BlueRepeatSkillProfile =
            new SyntheticSkillProfile("blue-volley", "Blue Volley", 5);

        /// <summary>
        /// Milestone 7.1a's Skill cards: require the other hero to discard one card of their choosing.
        ///
        /// A pair of their own, for the reason every fixture pair before them is separate: this is the
        /// first Skill whose chooser is not the actor, so every fixture that plays an existing Skill
        /// would gain a prompt belonging to someone else, and several of them count the facts their
        /// action emits and assert whose turn continues. They are dealt only by the card-state
        /// configuration, so the Shove, component and Volley hands and every digest measured against
        /// them are untouched.
        ///
        /// Red's 18 is the highest Initiative in any fixture hand, so the Red hero reliably acts first
        /// and a discard fixture never has to steer Initiative to reach the Skill. Blue reuses 5, the
        /// one value free inside Blue's ordinary band; it collides with nothing, because the Volley
        /// hand that also uses it is a different configuration and the two are never dealt together.
        /// </summary>
        private static readonly SyntheticSkillProfile RedCardStateSkillProfile =
            new SyntheticSkillProfile("red-demand", "Red Demand", 18);

        private static readonly SyntheticSkillProfile BlueCardStateSkillProfile =
            new SyntheticSkillProfile("blue-demand", "Blue Demand", 5);

        /// <summary>
        /// Milestone 8.10's escalation, on content that is nobody's card: the named hero discards, and
        /// is defeated when they had nothing to give.
        ///
        /// It exists so that the capability reaching rulebook p.13's Defeat list is proved here rather
        /// than arriving with the verified card that binds it, which is the deferral milestone 7.9's
        /// gate refuses. Electroblast is that card; this is the seam it names.
        /// </summary>
        private static readonly SyntheticSkillProfile RedEscalatingSkillProfile =
            new SyntheticSkillProfile("red-ultimatum", "Red Ultimatum", 19);

        private static readonly SyntheticSkillProfile BlueEscalatingSkillProfile =
            new SyntheticSkillProfile("blue-ultimatum", "Blue Ultimatum", 4);

        /// <summary>
        /// Milestone 7.2b's Skill cards: the first cards in this build that print rulebook p.9's Radius
        /// icon.
        ///
        /// A pair of their own, for the reason every fixture pair before them is separate, and for one
        /// more that is this milestone's own: a printed Radius is the first printed value a Skill
        /// carries at all, so adding it to an existing fixture Skill would change what every fixture
        /// that plays that card measures. The ordinary ten-card hands are untouched, which keeps
        /// milestone 7.9's card matrix and every pinned digest measuring exactly what they measured
        /// before.
        ///
        /// Red's 17 sits above every ordinary Red card and below 7.1a's 18, so a Radius fixture reaches
        /// its Skill without steering Initiative and without colliding with the card-state pair, which
        /// is a different configuration and is never dealt beside it. Blue's 5 is the same free value
        /// inside Blue's ordinary band that the other fixture pairs reuse for the same reason.
        /// </summary>
        private static readonly SyntheticSkillProfile RedRadiusSkillProfile =
            new SyntheticSkillProfile("red-shockwave", "Red Shockwave", 17);

        private static readonly SyntheticSkillProfile BlueRadiusSkillProfile =
            new SyntheticSkillProfile("blue-shockwave", "Blue Shockwave", 5);

        /// <summary>
        /// The printed Radius those two cards carry. p.9's own example values are 1 and 3 - "if your
        /// card has a Range/Radius value of 1, your primary action can only affect adjacent targets.
        /// With a Range/Radius value of 3, your primary action can affect targets up to 3 spaces away" -
        /// and 2 is chosen deliberately between them: it is large enough that a matching item's fourth
        /// ring is distinguishable from the printed disk, and small enough that the boundary case fits
        /// inside a focused fixture's board.
        /// </summary>
        internal const int RadiusSkillPrintedRadius = 2;

        /// <summary>
        /// Milestone 8.6's two fixture Skills, which prove this milestone's four new declarations on
        /// synthetic content before any verified card composes them - the discipline every general seam
        /// in this project has followed since 6.4.
        ///
        /// Two cards rather than one because p.9 puts one Range-or-Radius icon on a card and these read
        /// different ones. The orbit card carries the equidistant destination policy and the bounded
        /// repeat with its same-target binding; the kinesis card carries the printed-Range selection
        /// with p.19's straight-line complement and the Place anchored at the acting hero. Each is dealt
        /// by its own configuration, so the ordinary five and every pinned digest measure what they
        /// measured before.
        ///
        /// Red's 19 and 20 continue above 7.1a's 18, so neither steers Initiative inside a Red hand;
        /// Blue's 5 is the same free value inside Blue's ordinary band that the other fixture pairs
        /// reuse, and no configuration deals two of them together.
        /// </summary>
        private static readonly SyntheticSkillProfile RedOrbitSkillProfile =
            new SyntheticSkillProfile("red-orbit", "Red Orbit", 19);

        private static readonly SyntheticSkillProfile BlueOrbitSkillProfile =
            new SyntheticSkillProfile("blue-orbit", "Blue Orbit", 5);

        private static readonly SyntheticSkillProfile RedKinesisSkillProfile =
            new SyntheticSkillProfile("red-kinesis", "Red Kinesis", 20);

        private static readonly SyntheticSkillProfile BlueKinesisSkillProfile =
            new SyntheticSkillProfile("blue-kinesis", "Blue Kinesis", 5);

        /// <summary>
        /// The printed Range the kinesis cards carry - p.9's own larger example value, "with a
        /// Range/Radius value of 3, your primary action can affect targets up to 3 spaces away", which
        /// is large enough for a straight-line complement to have something to exclude and something to
        /// keep in the same disk.
        /// </summary>
        internal const int KinesisSkillPrintedRange = 3;

        /// <summary>
        /// How many times the orbit fixture's repeat may ask: the first declared maximum above one in
        /// this build, which is what makes a third instance observable at all.
        /// </summary>
        internal const int OrbitSkillMaximumRepeats = 2;

        /// <summary>
        /// Milestone 7.3's Ultimate cards, one per hand - the eighteenth definition each hand's deck
        /// owes rulebook p.6.
        ///
        /// Unlike every fixture pair above, these are supplied by every hand rather than by a
        /// configuration of their own, and that is p.6's arrangement rather than a convenience: setup
        /// deals five cards and returns "the remaining Tier II, Tier III, and Tier IV cards to your
        /// deck", so a hero's content is not complete without one. Milestone 7.1 supplied the Tier II
        /// and Tier III cards and recorded the gap in those words - seventeen definitions per hand,
        /// "p.6's eighteen-card deck less the Ultimate this milestone does not add" - and this is the
        /// milestone that adds it.
        ///
        /// Neither is dealt, for the reason none of milestone 7.1's twenty-four is: p.15 and p.16 find
        /// these cards in the player's own deck, and milestone 7.5 is the first milestone that moves
        /// one. So no hand grows, no card matrix changes, and the only thing this content moves is the
        /// definition table every payload carries.
        ///
        /// The two declare the same ability, which is deliberate. The synthetic content gives the two
        /// teams matched cards throughout, and the alternative here would be inventing a second power
        /// for one side out of a permission vocabulary whose only member is p.18's - so the pair states
        /// one sourced rule twice rather than one sourced rule and one invented one. That the ability is
        /// card content rather than something every hero simply has is proved from the other side, by a
        /// card that declares none being refused an Ultimate's classification outright.
        /// </summary>
        private static readonly SyntheticUltimateProfile RedUltimateProfile =
            new SyntheticUltimateProfile("red-ultimate", "Red Ultimate");

        private static readonly SyntheticUltimateProfile BlueUltimateProfile =
            new SyntheticUltimateProfile("blue-ultimate", "Blue Ultimate");

        public static ConfiguredHand Create(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, RedUltimateProfile, ownerId.Value);
        }

        public static ConfiguredHand CreateRed(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, RedUltimateProfile, null);
        }

        public static ConfiguredHand CreateBlue(HeroId ownerId)
        {
            return Create(ownerId, BlueProfiles, BlueUltimateProfile, null);
        }

        /// <summary>
        /// The five ordinary cards plus one Skill-bearing card, for milestone 6.2a's end-to-end fixture.
        ///
        /// The Skill lives on its own profile rather than being added to the five. Those five pin an
        /// action matrix several fixtures assert against - exactly one printed-primary offer and one
        /// printed-secondary offer of each kind per card - and threading a fourth action through them
        /// would break that matrix and move every pinned digest for a content reason. Keeping the Skill
        /// separate leaves the ordinary cards byte-identical, so the only thing that moves a digest in
        /// this milestone is the schema change itself.
        /// </summary>
        public static ConfiguredHand CreateRedWithSkill(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null, RedSkillProfile, RedComponentSkillProfile,
                RedDisplacementSkillProfile);
        }

        public static ConfiguredHand CreateBlueWithSkill(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null, BlueSkillProfile, BlueComponentSkillProfile,
                BlueDisplacementSkillProfile);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 6.7's Repeat card alone.
        ///
        /// Kept to one extra card for the reason every earlier fixture hand is: the ordinary five pin an
        /// action matrix and a digest that must not move, and a repeat fixture needs exactly one action
        /// that can displace something and then ask to happen again.
        /// </summary>
        public static ConfiguredHand CreateRedWithRepeatSkill(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null, repeatSkillProfile: RedRepeatSkillProfile);
        }

        public static ConfiguredHand CreateBlueWithRepeatSkill(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null, repeatSkillProfile: BlueRepeatSkillProfile);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 7.1a's card-state Skill alone.
        ///
        /// It takes the hero who must discard rather than deriving one, because there is nothing to
        /// derive it from: a hand is built for its owner and knows nothing about who is across the
        /// board, and "the opposing hero" is not a fact a 3v3 match has a single answer to. The
        /// configuration that deals these hands knows both heroes and names them, which is exactly
        /// where a card's own text would name its subject.
        /// </summary>
        public static ConfiguredHand CreateRedWithCardStateSkill(
            HeroId ownerId, HeroId discardingHeroId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null,
                cardStateSkillProfile: RedCardStateSkillProfile,
                cardStateDiscardingHeroId: discardingHeroId);
        }

        public static ConfiguredHand CreateBlueWithCardStateSkill(
            HeroId ownerId, HeroId discardingHeroId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null,
                cardStateSkillProfile: BlueCardStateSkillProfile,
                cardStateDiscardingHeroId: discardingHeroId);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 8.10's escalating Skill alone: the named hero
        /// discards a card, or is defeated if they had none to give.
        /// </summary>
        public static ConfiguredHand CreateRedWithEscalatingSkill(
            HeroId ownerId, HeroId discardingHeroId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null,
                escalatingSkillProfile: RedEscalatingSkillProfile,
                cardStateDiscardingHeroId: discardingHeroId);
        }

        public static ConfiguredHand CreateBlueWithEscalatingSkill(
            HeroId ownerId, HeroId discardingHeroId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null,
                escalatingSkillProfile: BlueEscalatingSkillProfile,
                cardStateDiscardingHeroId: discardingHeroId);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 7.2b's printed-Radius Skill alone.
        ///
        /// Its own configuration for the reason the four fixture hands before it have one, and because
        /// this card is the first to print a Radius icon: the value is content, so a fixture that
        /// measures the disk must be the only thing that can change it.
        /// </summary>
        public static ConfiguredHand CreateRedWithRadiusSkill(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null, radiusSkillProfile: RedRadiusSkillProfile);
        }

        public static ConfiguredHand CreateBlueWithRadiusSkill(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null, radiusSkillProfile: BlueRadiusSkillProfile);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 8.6's orbit Skill alone: a printed-Radius selection,
        /// a one-space direct Move that may end neither closer to nor farther from the actor, and a
        /// repeat that may ask twice and binds the same target each time.
        /// </summary>
        public static ConfiguredHand CreateRedWithOrbitSkill(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null, orbitSkillProfile: RedOrbitSkillProfile);
        }

        public static ConfiguredHand CreateBlueWithOrbitSkill(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null, orbitSkillProfile: BlueOrbitSkillProfile);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 8.6's kinesis Skill alone: a printed-Range selection
        /// that excludes everything in a straight line from the actor, feeding a Place that takes a unit
        /// or a token from the board into an empty space adjacent to the actor.
        /// </summary>
        public static ConfiguredHand CreateRedWithKinesisSkill(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null,
                kinesisSkillProfile: RedKinesisSkillProfile);
        }

        public static ConfiguredHand CreateBlueWithKinesisSkill(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null,
                kinesisSkillProfile: BlueKinesisSkillProfile);
        }


        /// <summary>
        /// Milestone 8.5's fixture cards: printed Attacks and a printed Defense that carry card text.
        ///
        /// <para>Their own hands, for the reason every fixture hand before them has one - the five
        /// ordinary cards are what the approved card matrix and every pinned digest measure, so a card
        /// that opens a new seam must not join them. These are the first non-Skill actions in this
        /// project to carry an <c>EffectRoot</c>, which is the seam the 7.9a review recorded as
        /// F-CARD-01, and each one names exactly one of rulebook p.14's three card-text moments so a
        /// fixture can watch the order the page prints.</para>
        ///
        /// <para>The text itself is deliberately the smallest thing that can be observed: one
        /// unconditional <c>NoteActor</c> node, which applies and publishes the acting hero. What is
        /// under test is <i>when</i> a card's text is applied and by whom, not what any particular
        /// sentence does; the cards that do something land at their own Wasp slices.</para>
        /// </summary>
        private static readonly SyntheticAttackTextProfile RedBeforeTextProfile =
            new SyntheticAttackTextProfile(
                "red-foretell", "Red Foretell", 6, 4, 2, 2, CardTextTiming.BeforeTheAttack, false, false);

        private static readonly SyntheticAttackTextProfile RedAfterTextProfile =
            new SyntheticAttackTextProfile(
                "red-aftermath", "Red Aftermath", 4, 4, 2, 2, CardTextTiming.AfterTheAttack, false, true);

        private static readonly SyntheticAttackTextProfile BlueDefenseTextProfile =
            new SyntheticAttackTextProfile(
                "blue-rejoinder", "Blue Rejoinder", 3, 3, 3, 2, CardTextTiming.None, true, false);

        /// <summary>
        /// Milestone 8.10. An After-the-attack text whose whole content is the question Thunder
        /// Boomerang asks before its repeat: did this Attack take a hero?
        /// </summary>
        private static readonly SyntheticAttackTextProfile RedConditionalTextProfile =
            new SyntheticAttackTextProfile(
                "red-reckoning", "Red Reckoning", 7, 4, 2, 2, CardTextTiming.AfterTheAttack,
                false, false, readsTheAttacksTarget: true);

        /// <summary>
        /// The five ordinary cards plus milestone 8.5's two Attack-text cards: one applying its text at
        /// p.14's step 1 and one at step 7, the second also narrowed to p.13's heroes alone.
        /// </summary>
        public static ConfiguredHand CreateRedWithAttackText(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null,
                attackTextProfiles: new[] { RedBeforeTextProfile, RedAfterTextProfile });
        }

        /// <summary>
        /// The five ordinary cards plus milestone 8.10's Attack-text card alone: an After-the-attack
        /// text whose content is the question about what the Attack took.
        ///
        /// Its own hand rather than a third card in the one above, for the reason every fixture hand
        /// here has one: milestone 8.5's cases assert over the hand that milestone dealt, and a card
        /// appended to it would make those assertions read past something they were not written about.
        /// </summary>
        public static ConfiguredHand CreateRedWithConditionalAttackText(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, RedUltimateProfile, null,
                attackTextProfiles: new[] { RedConditionalTextProfile });
        }

        /// <summary>
        /// The five ordinary cards plus one whose <i>secondary Defense</i> carries text, which is
        /// p.14's step 4 - the only card-text moment that belongs to the defender.
        /// </summary>
        public static ConfiguredHand CreateBlueWithAttackText(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, BlueUltimateProfile, null,
                attackTextProfiles: new[] { BlueDefenseTextProfile });
        }
        private static ConfiguredHand Create(
            HeroId ownerId,
            IReadOnlyList<SyntheticCardProfile> profiles,
            SyntheticUltimateProfile ultimateProfile,
            string instancePrefix,
            SyntheticSkillProfile skillProfile = null,
            SyntheticSkillProfile componentSkillProfile = null,
            SyntheticSkillProfile displacementSkillProfile = null,
            SyntheticSkillProfile repeatSkillProfile = null,
            SyntheticSkillProfile cardStateSkillProfile = null,
            HeroId cardStateDiscardingHeroId = default,
            SyntheticSkillProfile escalatingSkillProfile = null,
            SyntheticSkillProfile radiusSkillProfile = null,
            SyntheticSkillProfile orbitSkillProfile = null,
            SyntheticSkillProfile kinesisSkillProfile = null,
            IReadOnlyList<SyntheticAttackTextProfile> attackTextProfiles = null)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("A placeholder hand requires a defined owner.", nameof(ownerId));
            }

            var definitions = new List<CardDefinition>();
            var instances = new List<CardInstance>();
            var deckInstances = new List<CardInstance>();
            foreach (var profile in profiles)
            {
                AddCard(definitions, instances, CreateDefinition(profile), profile.Id, instancePrefix, ownerId);

                // Milestone 7.1 supplied the cards this one upgrades into as definitions with no
                // instance, because nothing could take one yet. Milestone 7.5 deals them, into the deck
                // p.15 sends the player to: "Find two Action cards in your deck that have the same color
                // as the chosen card but are one Tier higher." p.6 put them there at setup - "return the
                // remaining Tier II, Tier III, and Tier IV cards to your deck" - so this is the page's
                // own placement rather than a convenience for the level purchase.
                foreach (var target in SyntheticUpgradeTree.DealtDefinitions(profile))
                {
                    AddDeckCard(
                        definitions, deckInstances, target.Value, target.Key, instancePrefix, ownerId);
                }
            }

            // Milestone 7.3 supplied the Tier IV card and milestone 7.5 deals it, from the same p.6
            // sentence and into the same deck. p.16 takes it from there and nowhere else.
            if (ultimateProfile != null)
            {
                AddDeckCard(
                    definitions,
                    deckInstances,
                    SyntheticUltimateCard.Definition(ultimateProfile),
                    ultimateProfile.Id,
                    instancePrefix,
                    ownerId);
            }

            if (skillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateSkillDefinition(skillProfile),
                    skillProfile.Id, instancePrefix, ownerId);
            }

            if (componentSkillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateComponentSkillDefinition(componentSkillProfile),
                    componentSkillProfile.Id, instancePrefix, ownerId);
            }

            if (displacementSkillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateDisplacementSkillDefinition(displacementSkillProfile),
                    displacementSkillProfile.Id, instancePrefix, ownerId);
            }

            if (repeatSkillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateRepeatSkillDefinition(repeatSkillProfile),
                    repeatSkillProfile.Id, instancePrefix, ownerId);
            }

            if (cardStateSkillProfile != null)
            {
                if (!cardStateDiscardingHeroId.IsDefined)
                {
                    throw new ArgumentException(
                        "A card-state Skill names the hero who must discard.",
                        nameof(cardStateDiscardingHeroId));
                }

                AddCard(
                    definitions, instances,
                    CreateCardStateSkillDefinition(cardStateSkillProfile, cardStateDiscardingHeroId),
                    cardStateSkillProfile.Id, instancePrefix, ownerId);
            }

            if (escalatingSkillProfile != null)
            {
                if (!cardStateDiscardingHeroId.IsDefined)
                {
                    throw new ArgumentException(
                        "An escalating Skill names the hero who must discard or be defeated.",
                        nameof(cardStateDiscardingHeroId));
                }

                AddCard(
                    definitions, instances,
                    CreateEscalatingSkillDefinition(escalatingSkillProfile, cardStateDiscardingHeroId),
                    escalatingSkillProfile.Id, instancePrefix, ownerId);
            }

            if (radiusSkillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateRadiusSkillDefinition(radiusSkillProfile),
                    radiusSkillProfile.Id, instancePrefix, ownerId);
            }

            if (orbitSkillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateOrbitSkillDefinition(orbitSkillProfile),
                    orbitSkillProfile.Id, instancePrefix, ownerId);
            }

            if (kinesisSkillProfile != null)
            {
                AddCard(
                    definitions, instances, CreateKinesisSkillDefinition(kinesisSkillProfile),
                    kinesisSkillProfile.Id, instancePrefix, ownerId);
            }

            // Milestone 8.5. Dealt like every other fixture card, so a match can play one, and appended
            // after the ordinary five so no existing hand position moves.
            foreach (var attackText in attackTextProfiles ?? Array.Empty<SyntheticAttackTextProfile>())
            {
                AddCard(
                    definitions, instances, CreateAttackTextDefinition(attackText),
                    attackText.Id, instancePrefix, ownerId);
            }

            return new ConfiguredHand(
                definitions.AsReadOnly(),
                instances.AsReadOnly(),
                deckInstances.AsReadOnly());
        }

        /// <summary>
        /// Milestone 7.5. One card this hand leaves in the deck, in the shape <see cref="AddCard"/>
        /// gives a dealt one and differing in the one thing that differs: where p.6 puts it.
        ///
        /// Face-up, which is the same orientation <c>CardLifecycleAuthority.TryReturnToDeck</c> gives a
        /// card p.15 returns there. A hero searches their own deck by that page's instruction, so the
        /// colour and Tier of a deck card are readable to the rule that looks for them; a face-down deck
        /// card would have its printed characteristics suppressed and could never be found.
        /// </summary>
        private static void AddDeckCard(
            ICollection<CardDefinition> definitions,
            ICollection<CardInstance> deckInstances,
            CardDefinition definition,
            string cardId,
            string instancePrefix,
            HeroId ownerId)
        {
            definitions.Add(definition);
            deckInstances.Add(CardInstance.CreateInDeck(
                new CardInstanceId(
                    string.IsNullOrWhiteSpace(instancePrefix)
                        ? $"{cardId}-card"
                        : $"{instancePrefix}-{cardId}-card"),
                definition.Reference,
                ownerId));
        }

        private static void AddCard(
            ICollection<CardDefinition> definitions,
            ICollection<CardInstance> instances,
            CardDefinition definition,
            string profileId,
            string instancePrefix,
            HeroId ownerId)
        {
            definitions.Add(definition);
            instances.Add(CardInstance.CreateInHand(
                new CardInstanceId(
                    string.IsNullOrWhiteSpace(instancePrefix)
                        ? $"{profileId}-card"
                        : $"{instancePrefix}-{profileId}-card"),
                definition.Reference,
                ownerId));
        }

        /// <summary>
        /// A card whose only printed action is milestone 6.2a's synthetic Skill: an ordered Sequence
        /// that marks the actor with an Active effect, then offers a `may` step whose child asks the
        /// actor to choose a hero.
        ///
        /// Deliberately board-neutral and small. It exercises the execution frame - Sequence ordering,
        /// an optional step that opens a real decision, a capability-requested choice, typed output
        /// binding, and a created Active effect crossing the snapshot boundary - and implies no verified
        /// card content. It carries its own content revision, so the ordinary cards are untouched.
        /// </summary>
        private static CardDefinition CreateSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("skill-root");
            var mark = new EffectNodeId("skill-mark");
            var optional = new EffectNodeId("skill-may-choose");
            var choose = new EffectNodeId("skill-choose-hero");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-6-2a-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { mark, optional }),
                    new EffectNodeDefinition(
                        mark, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.MarkActorV1,
                        outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(
                        optional, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { choose },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        choose, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.ChooseHeroV1,
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-6-2a-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                // Milestone 7.1: a fixture Skill card states no colour, Tier or classification. The
                // absence is explicit because the constructor refuses a definition without a
                // profile, so every colour, Tier and upgrade query excludes these by
                // classification rather than by a colour that happened to be left unset.
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 6.2's Skill: one chooser-owned selection among the three component operations this
        /// milestone implements - place a token, give a marker, or return one this card gave.
        ///
        /// A <c>Choose</c> root rather than three cards, because the milestone needs all three paths
        /// reachable and a `Choose` is the frame structure that offers alternatives. The place branch is
        /// the rulebook's p.18 order made explicit: an `if able` reclamation step, then the placement,
        /// as two nodes so each carries at most one decision.
        ///
        /// Board-neutral in the sense that matters: it names no hero, no card, and no space. Which
        /// spaces are offered comes from the capability, not from this table.
        /// </summary>
        private static CardDefinition CreateComponentSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("component-root");
            var placeBranch = new EffectNodeId("component-place-branch");
            var reclaim = new EffectNodeId("component-reclaim-token");
            var place = new EffectNodeId("component-place-token");
            var give = new EffectNodeId("component-give-marker");
            var returnMarker = new EffectNodeId("component-return-marker");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-6-2-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { placeBranch, give, returnMarker },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        placeBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { reclaim, place }),
                    // `if able` is the rulebook's own word for the short-supply step, and it is the
                    // policy that lets an empty board skip the step instead of stopping the action.
                    new EffectNodeDefinition(
                        reclaim, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticComponentCapabilities.ReclaimTokenV1,
                        outputs: new[] { new EffectOutputBinding("reclaimed", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticComponentCapabilities.PlaceTokenV1,
                        outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        give, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticComponentCapabilities.GiveMarkerV1,
                        outputs: new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(
                        returnMarker, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticComponentCapabilities.ReturnMarkerV1,
                        outputs: new[] { new EffectOutputBinding("returned", EffectValueKind.Hero) })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-6-2-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                // Milestone 7.1: a fixture Skill card states no colour, Tier or classification. The
                // absence is explicit because the constructor refuses a definition without a
                // profile, so every colour, Tier and upgrade query excludes these by
                // classification rather than by a colour that happened to be left unset.
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-component-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 6.4's Skill: one chooser-owned selection between the two displacement operations
        /// this milestone implements - a direct Move of the acting hero, and an Object Push of every
        /// object adjacent to it.
        ///
        /// A third Skill card rather than a branch on 6.2's, for the reason recorded there: the five
        /// ordinary cards pin the card matrix and every default digest, and each milestone's Skill card
        /// stays separable so a fixture can reach one operation family without the others.
        ///
        /// The two branches carry different failure policies on purpose, because that is where p.11 and
        /// p.18 put the difference. The Move branch is `up to`, so its distance is optional and every
        /// shorter destination is legal. The Push branch is mandatory at a fixed distance of 2, so it
        /// opens no distance decision at all and every adjacent object moves as far as it can - which is
        /// the shape that shows a blocked target still resolving to a legal short or zero-space push.
        ///
        /// Since milestone 6.8k the Move branch is a sequence: a target-selection node offers the actor
        /// itself and every adjacent object, and the direct Move binds the published space as its
        /// subject. That is the only legal way to fill the widened coordinate parameter - a role cannot
        /// supply a coordinate - and it must be a Sequence, because node-output guarantees propagate
        /// through nothing else. Selecting the actor's own space is the original self-move; selecting an
        /// adjacent minion or token is the subject breadth 6.4a's correction needs. No `towards` policy
        /// is set; a `towards` bound needs verified card text to read it from.
        ///
        /// Milestone 6.5 appends two more branches to the same `Choose`: a Place of the acting hero
        /// into a different empty space within radius 2, and a Spawn of one friendly supply miniature
        /// onto its own Spawn point. The Place branch is a Sequence, because p.18's Place names the
        /// piece it moves and a coordinate can only reach a capability through a prior node's published
        /// output - here the actor's own space, which is a fact rather than a decision, so no prompt is
        /// invented for it. They are appended rather than inserted so the existing branch positions the
        /// 6.4 and 6.8k fixtures name are untouched.
        ///
        /// Milestone 6.6 appends three more, again at the end and for the same reason: p.19's Swap of
        /// the acting hero with one adjacent object, its Replace of one adjacent Hazard token with a
        /// Ward from the supply, and its Remove of one adjacent piece back to its supply. All three are
        /// Sequences behind a target selection, because each names the piece it works on by space and a
        /// role cannot supply a coordinate. The Swap's selection excludes the actor - p.19 requires two
        /// objects - while the other two include it harmlessly, since neither admits a hero.
        ///
        /// Milestone 6.8l adds no branch and moves none. Each of the four selections now names the node
        /// its chosen object is for, so a branch is judged by what that step could do rather than by
        /// whether anything at all stands in range. Three of them are answerable straight away; the
        /// Swap's is not, because its Swap also reads the actor's own space from a node that runs after
        /// the selection, and a consumer missing an input a later step publishes is left alone. That is
        /// the conservative case rather than a gap in the coverage.
        ///
        /// Milestone 6.6a adds no branch either. The Swap and the Remove now declare which kind of
        /// piece they may take, and this card declares p.19's own unrestricted breadth on both, which
        /// is what it has always done - the operations simply say so now instead of assuming it. Blue
        /// Sentinel is the card that declares something narrower.
        ///
        /// Board-neutral in the same sense as the others: it names no hero, no card, and no space.
        /// </summary>
        private static CardDefinition CreateDisplacementSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("displacement-root");
            var moveBranch = new EffectNodeId("displacement-move-branch");
            var moveSequence = new EffectNodeId("displacement-move-sequence");
            var selectSubject = new EffectNodeId("displacement-select-subject");
            var move = new EffectNodeId("displacement-direct-move");
            var push = new EffectNodeId("displacement-object-push");
            var placeBranch = new EffectNodeId("placement-place-branch");
            var actorSpace = new EffectNodeId("placement-actor-space");
            var place = new EffectNodeId("placement-place");
            var spawn = new EffectNodeId("placement-spawn");
            var swapBranch = new EffectNodeId("exchange-swap-branch");
            var swapSelect = new EffectNodeId("exchange-swap-select");
            var swapActorSpace = new EffectNodeId("exchange-swap-actor-space");
            var swap = new EffectNodeId("exchange-swap");
            var replaceBranch = new EffectNodeId("exchange-replace-branch");
            var replaceSelect = new EffectNodeId("exchange-replace-select");
            var replace = new EffectNodeId("exchange-replace");
            var removeBranch = new EffectNodeId("exchange-remove-branch");
            var removeSelect = new EffectNodeId("exchange-remove-select");
            var remove = new EffectNodeId("exchange-remove");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-6-4c-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[]
                        {
                            moveBranch, push, placeBranch, spawn, swapBranch, replaceBranch, removeBranch
                        },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    // `up to` is the rulebook's own wording for an optional distance (p.11), and it is
                    // the policy that admits every destination at or below the bound rather than only
                    // the exact one.
                    new EffectNodeDefinition(
                        moveBranch, EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                        childNodeIds: new[] { moveSequence },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        moveSequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectSubject, move }),
                    new EffectNodeDefinition(
                        selectSubject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(move))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectSubject, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.DirectMoveDestinationPolicy.Unrestricted))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.ObjectPushV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            // The card names one number, so the least it names is the same one.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            // p.18's own breadth, restated by the fixture hand rather than narrowed:
                            // this card says every adjacent object, and a token standing beside the
                            // actor is one. It is the conservative case the batch's coverage rests on.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.UnitOrToken)),
                            // The same breadth over the other two things milestone 8.8 lets a card
                            // narrow, and this card narrows neither: it names no side of the board,
                            // and it names no maximum, which is p.18's plain batch over every object
                            // standing beside the pusher.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                                EffectValue.Integer((int)Rules.Modifications.SourceTeamRelation.AnyTeam)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MaximumTargetsParameterName,
                                EffectValue.Integer(0))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        placeBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("radius", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Placement.PlaceEligiblePieceKind.GamePiece)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Placement.PlaceDestinationAnchor.PlacedPiece))
                        },
                        outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        spawn, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.SpawnV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("spawned", EffectValueKind.Minion),
                            new EffectOutputBinding("at", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        swapBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { swapSelect, swapActorSpace, swap }),
                    new EffectNodeDefinition(
                        swapSelect, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            // p.19 requires two objects, so the actor is not among its own candidates.
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(swap))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        swapActorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "first", swapActorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                "second", swapSelect, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.BoardObjects.SwapEligiblePieceKind.GamePiece))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        replaceBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { replaceSelect, replace }),
                    new EffectNodeDefinition(
                        replaceSelect, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(replace))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        replace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.ReplaceTokenV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", replaceSelect, "chosen", EffectValueKind.Coordinate)
                        },
                        outputs: new[] { new EffectOutputBinding("replaced", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        removeBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { removeSelect, remove }),
                    new EffectNodeDefinition(
                        removeSelect, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(remove))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.RemoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", removeSelect, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.BoardObjects.RemoveEligiblePieceKind.GamePiece))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                            new EffectOutputBinding("lanePush", EffectValueKind.Boolean)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-6-4c-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                // Milestone 7.1: a fixture Skill card states no colour, Tier or classification. The
                // absence is explicit because the constructor refuses a definition without a
                // profile, so every colour, Tier and upgrade query excludes these by
                // classification rather than by a colour that happened to be left unset.
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-displacement-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// A card whose only printed action is milestone 6.7's bounded Repeat: `Choose one` between
        /// naming an adjacent object and moving it one space, or pushing every adjacent object two
        /// spaces - and then `you may Repeat this action`.
        ///
        /// Every leaf is a capability the displacement Skill already uses at a different distance. That
        /// is deliberate: 6.7's non-goals bar a new leaf effect written only to exercise Repeat, and
        /// what a repeat has to prove is that the second instance is a real separate action, not that
        /// some new thing happens inside it.
        ///
        /// The two branches exist because the acceptance text asks two different questions of the
        /// repeated instance. The choice-bearing branch is where a later instance can legally name a
        /// different subject and a different destination than the first did; the fixed push is where
        /// the correction between the instances is deterministic, so a fixture can pause it, save it,
        /// and answer it without steering anything. The Repeat sits after the `Choose` rather than
        /// inside a branch, because p.10's `may` here governs the action, not the mode.
        ///
        /// Board-neutral like every other fixture Skill: it names no hero, no card, and no space.
        /// </summary>
        private static CardDefinition CreateRepeatSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("repeat-root");
            var choice = new EffectNodeId("repeat-choice");
            var moveSequence = new EffectNodeId("repeat-move-sequence");
            var selectSubject = new EffectNodeId("repeat-select-subject");
            var move = new EffectNodeId("repeat-direct-move");
            var push = new EffectNodeId("repeat-object-push");
            var repeat = new EffectNodeId("repeat-again");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-6-4c-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { choice, repeat }),
                    new EffectNodeDefinition(
                        choice, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { moveSequence, push },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        moveSequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectSubject, move }),
                    new EffectNodeDefinition(
                        selectSubject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(move))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectSubject, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.DirectMoveDestinationPolicy.Unrestricted))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.ObjectPushV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            // The card names one number, so the least it names is the same one.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            // p.18's own breadth, restated by the fixture hand rather than narrowed:
                            // this card says every adjacent object, and a token standing beside the
                            // actor is one. It is the conservative case the batch's coverage rests on.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.UnitOrToken)),
                            // The same breadth over the other two things milestone 8.8 lets a card
                            // narrow, and this card narrows neither: it names no side of the board,
                            // and it names no maximum, which is p.18's plain batch over every object
                            // standing beside the pusher.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                                EffectValue.Integer((int)Rules.Modifications.SourceTeamRelation.AnyTeam)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MaximumTargetsParameterName,
                                EffectValue.Integer(0))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor)
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-6-4c-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                // Milestone 7.1: a fixture Skill card states no colour, Tier or classification. The
                // absence is explicit because the constructor refuses a definition without a
                // profile, so every colour, Tier and upgrade query excludes these by
                // classification rather than by a colour that happened to be left unset.
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-repeat-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 7.1a's fixture Skill: the named hero discards one card of their own choosing.
        ///
        /// One capability node under a Sequence root, deliberately - the milestone is about a
        /// capability reaching card state, not about control flow, and a card that did anything else
        /// as well would make every assertion about the discard read past something unrelated.
        ///
        /// The node is Mandatory and the `if able` behaviour still comes from the capability rather
        /// than from the policy. A hero with no card to give reports a declined step, which the
        /// resolver continues past whatever the policy says, so the action ends without failing; the
        /// policy is what would matter if the capability reported that the discard was impossible,
        /// and it never does.
        /// </summary>
        private static CardDefinition CreateCardStateSkillDefinition(
            SyntheticSkillProfile profile,
            HeroId discardingHeroId)
        {
            var root = new EffectNodeId("demand-root");
            var discard = new EffectNodeId("demand-discard");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-7-1a-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { discard }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticCardStateCapabilities.HeroParameter,
                                EffectValue.Hero(discardingHeroId))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-7-1a-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-card-state-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 8.10's fixture Skill: the named hero discards one card of their own choosing, or
        /// is defeated if they had none to give.
        ///
        /// <para>It is here so the seam reaching rulebook p.13's Defeat list from card text exists on
        /// content that is nobody's card, before the verified card that binds it - which is what
        /// milestone 7.9's deferral gate requires and what Electroblast then names. Nothing about the
        /// escalation is Wasp's: the shape is p.10's mandatory step with two ways to complete, read
        /// from <see cref="EffectNodeKind.Conditional"/>'s then-and-else.</para>
        ///
        /// <para>The discard is revision 2, which reports whether it happened rather than declining in
        /// silence; the conditional reads that report; and the defeat node declares the request the
        /// card action performs the page from. No node here performs p.13 itself.</para>
        /// </summary>
        private static CardDefinition CreateEscalatingSkillDefinition(
            SyntheticSkillProfile profile,
            HeroId discardingHeroId)
        {
            var root = new EffectNodeId("ultimatum-root");
            var discard = new EffectNodeId("ultimatum-discard");
            var escalation = new EffectNodeId("ultimatum-escalation");
            var defeat = new EffectNodeId("ultimatum-defeat");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-8-10-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { discard, escalation }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticCardStateCapabilities.HeroParameter,
                                EffectValue.Hero(discardingHeroId))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { defeat },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag",
                                discard,
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatHeroV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticConsequenceCapabilities.HeroParameter,
                                EffectValue.Hero(discardingHeroId))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                                EffectValueKind.Hero)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-8-10-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-escalating-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 7.2b's card: a Skill that prints rulebook p.9's Radius icon, chooses one unit
        /// inside that Radius, and pushes it one space.
        ///
        /// Every part of it already existed except the bound. The selection is the same chooser-owned
        /// target selection every other card names an object through, and the push is p.18's own named
        /// Push at distance 1 - what this card adds is that the selection's reach is the printed icon
        /// rather than a number written into the effect. That is the distinction the milestone exists
        /// to make representable: p.16 raises a printed icon and never "a number in the card text", so
        /// an item can widen this card's reach while the push distance of 1 beside it stays 1 whatever
        /// the hero owns.
        ///
        /// The Push is what makes it a card rather than a probe: choosing alone mutates nothing, and a
        /// consumer that does something is what proves the printed value reached the board.
        /// </summary>
        private static CardDefinition CreateRadiusSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("shockwave-root");
            var selectTarget = new EffectNodeId("shockwave-select");
            var push = new EffectNodeId("shockwave-push");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-7-2b-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectTarget, push }),
                    new EffectNodeDefinition(
                        selectTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusTargetV1,
                        inputs: new[]
                        {
                            // p.19 keeps self and another distinct, and this card affects another unit.
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                "activity",
                                EffectValue.Integer((int)Rules.Modifications.RuleActivity.ObjectPush)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(push))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.PushTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", selectTarget, "chosen", EffectValueKind.Coordinate),
                            // Card text, not a printed icon: p.16 never raises this one, whatever
                            // items the hero owns.
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.Unit))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-7-2b-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-radius-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Radius, RadiusSkillPrintedRadius)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 8.6's orbit fixture: `Move a unit or a token in radius 1 space, without moving it
        /// away from you or closer to you. You may repeat this action up to two times on the same
        /// target.`
        ///
        /// Everything before the repeat is already registered - the printed-Radius selection is 7.2b's
        /// and the direct Move is 6.4's, widened at 6.8k - so what this card adds is the declared
        /// destination policy and, for the first time in this build, a repeat maximum above one bound to
        /// an earlier selection. None of it is official content; the sentence is deliberately the same
        /// shape Wasp's three telekinetic Move skills print, because a fixture that proved a different
        /// shape would prove nothing about them.
        /// </summary>
        private static CardDefinition CreateOrbitSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("orbit-root");
            var selectTarget = new EffectNodeId("orbit-select");
            var move = new EffectNodeId("orbit-move");
            var repeat = new EffectNodeId("orbit-repeat");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-8-6-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectTarget, move, repeat }),
                    new EffectNodeDefinition(
                        selectTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            // Card-text direct Move has no activity of its own and reads its enclosing
                            // action's, which is this Skill.
                            EffectInputBinding.FromLiteral(
                                "activity",
                                EffectValue.Integer((int)Rules.Modifications.RuleActivity.Skill)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(move))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.DirectMoveDestinationPolicy
                                        .EquidistantFromActor))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: OrbitSkillMaximumRepeats,
                        repeatTargetBinding: RepeatTargetBinding.SameTarget,
                        boundSelectionNodeId: selectTarget)
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-8-6-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-orbit-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Radius, RadiusSkillPrintedRadius)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Milestone 8.6's kinesis fixture: `Place a unit or a token in range, which is not in a
        /// straight line, into a space adjacent to you.`
        ///
        /// It is the first card in this build to print a Range icon its own text reads, and the first
        /// to ask p.19's straight-line question at all. The Place beneath is milestone 6.5's, under the
        /// two declarations milestone 8.6 added to it: the piece kind the card names, and the space its
        /// destinations are measured from. Not official content; the sentence mirrors Wasp's
        /// Telekinesis for the reason the orbit fixture mirrors her Move skills.
        /// </summary>
        private static CardDefinition CreateKinesisSkillDefinition(SyntheticSkillProfile profile)
        {
            var root = new EffectNodeId("kinesis-root");
            var selectTarget = new EffectNodeId("kinesis-select");
            var place = new EffectNodeId("kinesis-place");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-8-6-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectTarget, place }),
                    new EffectNodeDefinition(
                        selectTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                "activity",
                                EffectValue.Integer((int)Rules.Modifications.RuleActivity.Skill)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeStraightLineParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(place))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", selectTarget, "chosen", EffectValueKind.Coordinate),
                            // `adjacent to you` is card text, which p.16 never raises.
                            EffectInputBinding.FromLiteral("radius", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Placement.PlaceEligiblePieceKind.UnitOrToken)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Placement.PlaceDestinationAnchor.ActingHero))
                        },
                        outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-8-6-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-kinesis-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Range, KinesisSkillPrintedRange)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        private sealed class SyntheticSkillProfile
        {
            public SyntheticSkillProfile(string id, string displayName, int initiative)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
        }


        /// <summary>
        /// Milestone 8.5. One fixture card whose printed Attack, or whose printed secondary Defense,
        /// carries card text applied at one of rulebook p.14's numbered moments.
        /// </summary>
        private sealed class SyntheticAttackTextProfile
        {
            public SyntheticAttackTextProfile(
                string id,
                string displayName,
                int initiative,
                int attackValue,
                int defenseValue,
                int movementValue,
                CardTextTiming primaryTiming,
                bool defenseCarriesText,
                bool narrowedToHeroes,
                bool readsTheAttacksTarget = false)
            {
                ReadsTheAttacksTarget = readsTheAttacksTarget;
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                AttackValue = attackValue;
                DefenseValue = defenseValue;
                MovementValue = movementValue;
                PrimaryTiming = primaryTiming;
                DefenseCarriesText = defenseCarriesText;
                NarrowedToHeroes = narrowedToHeroes;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int AttackValue { get; }
            public int DefenseValue { get; }
            public int MovementValue { get; }
            public CardTextTiming PrimaryTiming { get; }

            /// <summary>Milestone 8.10: whether this card.s text asks what the Attack took.</summary>
            public bool ReadsTheAttacksTarget { get; }
            public bool DefenseCarriesText { get; }
            public bool NarrowedToHeroes { get; }
        }

        /// <summary>
        /// The smallest card text that can be watched: one unconditional node that applies and publishes
        /// the acting hero. It opens no choice, which is what
        /// <see cref="EffectDefinitionValidator.ValidateCardText"/> requires of text applied inside an
        /// Attack, and it is enough to say exactly when the text ran and whose it was.
        /// </summary>
        private static EffectDefinition CardTextEffect(string id) =>
            new EffectDefinition(
                new EffectDefinitionId($"placeholder-{id}-text"),
                "phase-8-5-rev-1",
                new EffectNodeId("note"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("note"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1,
                        outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) })
                });

        /// <summary>
        /// Milestone 8.10's card text: the same note, applied only where the Attack this text is
        /// printed inside took a hero.
        ///
        /// It exists so the predicate over the enclosing Attack's target is proved on content that is
        /// nobody's card, before Thunder Boomerang names it - which is what milestone 7.9's deferral
        /// gate requires. What is under test is the predicate, so its then-branch is the same
        /// unconditional note every other fixture text applies.
        /// </summary>
        private static EffectDefinition ConditionalCardTextEffect(string id) =>
            new EffectDefinition(
                new EffectDefinitionId($"placeholder-{id}-text"),
                "phase-8-10-rev-1",
                new EffectNodeId("took-a-hero"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("took-a-hero"), EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.AttackTookAHeroV1,
                        childNodeIds: new[] { new EffectNodeId("note") }),
                    new EffectNodeDefinition(
                        new EffectNodeId("note"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1,
                        outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) })
                });

        private static CardDefinition CreateAttackTextDefinition(SyntheticAttackTextProfile profile)
        {
            var actions = new List<ActionDefinition>
            {
                new ActionDefinition(
                    new ActionDefinitionId("primary-attack-text"),
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.AttackValue) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    profile.PrimaryTiming == CardTextTiming.None
                        ? null
                        : profile.ReadsTheAttacksTarget
                            ? ConditionalCardTextEffect(profile.Id)
                            : CardTextEffect(profile.Id),
                    default,
                    PrintedDefenseResolution.None,
                    profile.PrimaryTiming,
                    profile.NarrowedToHeroes
                        ? PrintedAttackTargetPolicy
                            .ForClassification(AttackRangeKind.NonRanged).NarrowedToHeroes()
                        : (PrintedAttackTargetPolicy?)null),
                CreateMovementAction(profile.MovementValue)
            };

            actions.Add(new ActionDefinition(
                new ActionDefinitionId("secondary-defense"),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, profile.DefenseValue) },
                new ActionDefinitionPolicy(
                    profile.DefenseCarriesText
                        ? CardTextApplicationPolicy.Applies
                        : CardTextApplicationPolicy.Suppressed,
                    false,
                    false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                profile.DefenseCarriesText ? CardTextEffect($"{profile.Id}-defense") : null,
                default,
                PrintedDefenseResolution.None,
                profile.DefenseCarriesText ? CardTextTiming.DefenseAction : CardTextTiming.None));

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-8-5-rev-1",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                actions);
        }
        private static CardDefinition CreateDefinition(SyntheticCardProfile profile)
        {
            var actions = new[]
            {
                CreatePrintedPrimaryAction(profile),
                CreateMovementAction(profile.MovementValue),
                CreateDefenseAction(profile.DefenseValue)
            };

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                profile.ContentRevision,
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                profile.Progression,
                actions);
        }

        /// <summary>
        /// The card's one printed Primary action, selected by the profile's declared discriminator
        /// rather than by its id or display name. A branch on the card's name would be exactly the
        /// card-name rule branch the showcase milestones forbid, and it would also make every future
        /// conversion edit this method; a discriminator leaves the unconverted profiles alone.
        /// </summary>
        private static ActionDefinition CreatePrintedPrimaryAction(SyntheticCardProfile profile)
        {
            switch (profile.PrintedPrimary)
            {
                case SyntheticPrintedPrimaryKind.Attack:
                    return CreateAttackAction(profile.AttackRange, profile.AttackValue, profile.RangeValue);
                case SyntheticPrintedPrimaryKind.ModificationSkill:
                    return CreateModificationSkillAction(profile);
                case SyntheticPrintedPrimaryKind.ImmunityTimingSkill:
                    return CreateImmunityTimingSkillAction(profile);
                case SyntheticPrintedPrimaryKind.PushPlaceSkill:
                    return CreatePushPlaceSkillAction(profile);
                case SyntheticPrintedPrimaryKind.DirectMoveSpawnSkill:
                    return CreateDirectMoveSpawnSkillAction(profile);
                case SyntheticPrintedPrimaryKind.ExchangeSkill:
                    return CreateExchangeSkillAction(profile);
                case SyntheticPrintedPrimaryKind.PushRepeatSkill:
                    return CreatePushRepeatSkillAction(profile);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile),
                        profile.PrintedPrimary,
                        "This synthetic profile declares no printed Primary action.");
            }
        }

        private static ActionDefinition CreateMovementAction(int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId("secondary-movement"),
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));
        }

        private static ActionDefinition CreateAttackAction(
            AttackRangeKind attackRange,
            int attackValue,
            int rangeValue)
        {
            var values = attackRange == AttackRangeKind.Ranged
                ? new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, attackValue),
                    new PrintedValueDefinition(PrintedValueChannel.Range, rangeValue)
                }
                : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, attackValue) };
            return new ActionDefinition(
                new ActionDefinitionId(
                    attackRange == AttackRangeKind.Ranged
                        ? "primary-ranged-attack"
                        : "primary-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                values,
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                attackRange,
                default);
        }

        /// <summary>
        /// Milestone 6.8a's conversion: one three-mode Skill replacing this card's printed Primary
        /// Attack. Each mode is a direct child of a `Choose` and names one capability milestone 6.8
        /// already registered, so the card supplies no rule of its own - it is the stable source, and
        /// the affected object is whichever hero the action's authority reports as the actor.
        ///
        /// The three modes are deliberately one family each: a value modifier, an action restriction,
        /// and a narrow permission, under `This Turn`, `This Round` and `Next Turn` respectively. What
        /// each mode does, when it activates, when it expires, and what cancels it is definition data
        /// on the Active-effect definitions this table only points at, never anything this card states.
        ///
        /// Board-neutral: it names no hero, no card and no space, and it carries no printed value,
        /// because a Skill has no printed icon channel of its own. None of it is official content.
        /// </summary>
        private static ActionDefinition CreateModificationSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("bulwark-root");
            var defenseBonus = new EffectNodeId("bulwark-defense-bonus");
            var fastTravelRestriction = new EffectNodeId("bulwark-fast-travel-restriction");
            var moveThroughPermission = new EffectNodeId("bulwark-move-through-permission");

            // Every mode publishes the affected hero under the exact name and kind
            // ModificationEffectHandler declares, which the definition validator checks by both.
            EffectNodeDefinition Mode(EffectNodeId id, EffectCapabilityReference capability) =>
                new EffectNodeDefinition(
                    id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    capability,
                    outputs: new[] { new EffectOutputBinding("affected", EffectValueKind.Hero) });

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                profile.ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { defenseBonus, fastTravelRestriction, moveThroughPermission },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Mode(defenseBonus, SyntheticModificationCapabilities.GrantDefenseBonusV1),
                    Mode(fastTravelRestriction, SyntheticModificationCapabilities.RestrictFastTravelV1),
                    Mode(moveThroughPermission, SyntheticModificationCapabilities.PermitMoveThroughObstaclesV1)
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-modification-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

        /// <summary>
        /// Milestone 6.8b's conversion: one two-mode Skill replacing this card's printed Primary
        /// Attack. Like 6.8a's, each mode is a direct child of a `Choose` and names one capability
        /// milestone 6.8 already registered, so the card supplies no rule of its own - it is the stable
        /// source, and the affected object is whichever hero the action's authority reports as the
        /// actor.
        ///
        /// The two families are deliberately the two 6.8a left out, and they are the two whose timing
        /// questions are hardest: Push immunity under `Next Turn, After Playing Cards`, which p.17
        /// applies "once next turn, after all players have played and revealed their cards" rather than
        /// at the start of the turn, and a `Counts As` minion declaration for `This Round`, which p.17
        /// makes a maximal replacement of the object's rules and qualities. Neither mode is a value
        /// modifier, an action restriction, or a narrow permission; those stay with Red Bulwark.
        ///
        /// Board-neutral: it names no hero, no card and no space, and it carries no printed value,
        /// because a Skill has no printed icon channel of its own. None of it is official content.
        ///
        /// The last three modes arrived together when the configured scene went back to five cards per
        /// hero and the Blue Cache card that had carried them stopped being dealt. They keep that
        /// card's own order - place, give, return - and are appended, so 6.8b's two keep their indices.
        ///
        /// Two of the three belong here on their own merits: a marker attaches to a hero, which is what
        /// the two modes above already do. The placement is here for a harder reason. Its family is
        /// Blue Sentinel's, whose Swap, Replace and Remove are p.19's operations on that same piece,
        /// and putting it there is what a reader would expect. Implementation refuted that: p.8 removes
        /// every placed token at the end of a round, and a card is played at most once in a round, so a
        /// self-supplying Sentinel could never have a Hazard standing when it came back to hand. Here
        /// it sits one Initiative above Sentinel on the same team, so one round holds both the play
        /// that places the token and the play that acts on it - milestone 6.2d's shape without 6.2d's
        /// sixth card.
        ///
        /// Give and Return stay on one card because p.18's return is scoped to the source that gave -
        /// <c>ReturnMarkerHandler</c> finds an attachment by <c>SourceCardInstanceId</c> - so splitting
        /// them across two cards would leave the Return with nothing it could ever find. It keeps the
        /// `if able` policy the Cache card gave it, so a hand that has given no marker yet reads the
        /// mode greyed rather than spending the action on a stop.
        /// </summary>
        private static ActionDefinition CreateImmunityTimingSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("feint-root");
            var pushImmunity = new EffectNodeId("feint-push-immunity");
            var countsAsMinion = new EffectNodeId("feint-counts-as-minion");
            var placeBranch = new EffectNodeId("feint-place-branch");
            var reclaimToken = new EffectNodeId("feint-reclaim-token");
            var placeToken = new EffectNodeId("feint-place-token");
            var giveMarker = new EffectNodeId("feint-give-marker");
            var returnMarker = new EffectNodeId("feint-return-marker");

            EffectNodeDefinition Mode(EffectNodeId id, EffectCapabilityReference capability) =>
                new EffectNodeDefinition(
                    id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    capability,
                    outputs: new[] { new EffectOutputBinding("affected", EffectValueKind.Hero) });

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                profile.ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[]
                        {
                            pushImmunity, countsAsMinion, placeBranch, giveMarker, returnMarker
                        },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Mode(pushImmunity, SyntheticModificationCapabilities.GrantPushImmunityV1),
                    Mode(countsAsMinion, SyntheticModificationCapabilities.CountAsMinionV1),
                    // The place branch is p.18's order made explicit, exactly as the Cache card had
                    // it: an `if able` short-supply reclamation, then the placement, as two nodes so
                    // each carries at most one decision. That is why it is the one branch here with a
                    // shape of its own; the four beside it are single capabilities.
                    new EffectNodeDefinition(
                        placeBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { reclaimToken, placeToken }),
                    new EffectNodeDefinition(
                        reclaimToken, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticComponentCapabilities.ReclaimTokenV1,
                        outputs: new[] { new EffectOutputBinding("reclaimed", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        placeToken, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticComponentCapabilities.PlaceTokenV1,
                        outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) }),
                    // Neither marker mode can use `Mode`: the two capabilities publish `holder` and
                    // `returned` rather than `affected`, and the definition validator checks an
                    // output by both its name and its kind.
                    new EffectNodeDefinition(
                        giveMarker, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticComponentCapabilities.GiveMarkerV1,
                        outputs: new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(
                        returnMarker, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticComponentCapabilities.ReturnMarkerV1,
                        outputs: new[] { new EffectOutputBinding("returned", EffectValueKind.Hero) })
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-immunity-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

        /// <summary>
        /// Milestone 6.5a's conversion: the exact modal Skill `Choose one: choose another adjacent unit
        /// and Push it 3 spaces; or Place your hero in a different empty space within Radius 4`,
        /// replacing this card's printed Primary Attack.
        ///
        /// Each branch is a Sequence rather than a single node, because both operations name a board
        /// object and a coordinate can only reach a capability through a prior node's published output.
        /// The two branches differ in what that prior node is, and that difference is the card text's:
        /// `choose another adjacent unit` is a decision, so the Push branch opens milestone 6.8k's
        /// target selection bounded to Radius 1 with the actor excluded, while `your hero` is not a
        /// decision, so the Place branch reads the actor's own space as a fact and invents no prompt.
        ///
        /// Every value here is declaration rather than behaviour: distance 3 under a mandatory policy,
        /// so the Push opens no distance choice and travels as far as it can including zero; obstacles
        /// not ignored, so p.18's ordinary transit and endpoint rules apply; Radius 4 inclusive, which
        /// the Place capability measures as rulebook distance rather than a route. None of it is
        /// official content, and the card names no hero, no card and no space.
        /// </summary>
        private static ActionDefinition CreatePushPlaceSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("needle-root");
            var pushBranch = new EffectNodeId("needle-push-branch");
            var selectTarget = new EffectNodeId("needle-select-target");
            var push = new EffectNodeId("needle-push");
            var placeBranch = new EffectNodeId("needle-place-branch");
            var actorSpace = new EffectNodeId("needle-actor-space");
            var cardRadius = new EffectNodeId("needle-card-radius");
            var place = new EffectNodeId("needle-place");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                profile.ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { pushBranch, placeBranch },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        pushBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectTarget, push }),
                    new EffectNodeDefinition(
                        selectTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                "activity", EffectValue.Integer((int)Rules.Modifications.RuleActivity.ObjectPush)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(push))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.PushTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(3)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            // `unit` rather than p.18's own unit-or-token breadth, which is why an
                            // adjacent token is never among this branch's candidates.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.Unit))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        placeBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, cardRadius, place }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    // Milestone 7.2c. The bound is the card's own printed Radius icon, published as a
                    // fact and bound below, rather than the literal 4 this branch carried since 6.5a.
                    // p.9 is explicit that a card text mentioning Radius "refers to this value", so a
                    // card that says "within Radius" and writes its own number down states a bound the
                    // page does not give it - and leaves p.16's Item Bonus no icon to raise.
                    new EffectNodeDefinition(
                        cardRadius, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticValueCapabilities.CardRadiusV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                "radius",
                                cardRadius,
                                SyntheticValueCapabilities.RadiusOutput,
                                EffectValueKind.Integer),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Placement.PlaceEligiblePieceKind.GamePiece)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Placement.PlaceDestinationAnchor.PlacedPiece))
                        },
                        outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) })
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-push-place-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                // Milestone 7.2c. The Radius this card's text has named since 6.5a, printed where p.9
                // puts it: "some primary actions have a Range or Radius icon and a value on the right
                // side of the card". The value is the 4 the text already stated, so no rule changes -
                // what changes is that the number is now the card's printed icon, which p.16's items
                // raise and which the Place branch reads instead of carrying its own copy.
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, PushPlaceRadius) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

        /// <summary>
        /// Red Needle's printed Radius, and the exact value milestone 6.5a's card text named.
        /// </summary>
        internal const int PushPlaceRadius = 4;

        /// <summary>
        /// Milestone 6.5b's conversion: the modal Skill `Choose one: directly Move another adjacent
        /// unit up to 2 spaces; or Spawn one unit from your supply onto its own Spawn point`, replacing
        /// this card's printed Primary Attack.
        ///
        /// The two branches differ in shape for the same reason 6.5a's do, and the difference is the
        /// card text's rather than this table's. `another adjacent unit` is a decision, so the Move
        /// branch is a Sequence opening milestone 6.8k's target selection at Radius 1 with the actor
        /// excluded, and the direct Move binds the published space as its subject - the only legal way
        /// to fill that coordinate parameter, since a role resolves without board access and the
        /// definition validator refuses a role that tries. Which miniature Spawns is the Spawn
        /// capability's own one decision and its destination is declared rather than chosen, so that
        /// branch is a single node with nothing to sequence.
        ///
        /// The selection uses revision 1 of the target capability, not 6.5a's revision 2. Revision 2
        /// exists so an offer can name the activity its consuming operation will ask immunity for, and
        /// there card text Push asks `RuleActivity.ObjectPush`. Card-text direct Move has no activity
        /// of its own - p.11 says it is expressly not a Movement action - so the capability asks the
        /// enclosing action's activity, and revision 1 is bound to exactly that. Naming Skill through
        /// revision 2 would state the same thing twice.
        ///
        /// Every value here is declaration rather than behaviour: Radius 1 with the actor excluded,
        /// distance 2 under `up to` so every shorter destination is legal, and no `towards` bound,
        /// which would need verified card text to read it from. None of it is official content, and
        /// the card names no hero, no card and no space.
        /// </summary>
        private static ActionDefinition CreateDirectMoveSpawnSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("bombard-root");
            var moveBranch = new EffectNodeId("bombard-move-branch");
            var selectTarget = new EffectNodeId("bombard-select-target");
            var move = new EffectNodeId("bombard-direct-move");
            var spawn = new EffectNodeId("bombard-spawn");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                profile.ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { moveBranch, spawn },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        moveBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectTarget, move }),
                    new EffectNodeDefinition(
                        selectTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(move))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    // `up to` is p.11's own wording for an optional distance, and it is the policy the
                    // capability reads to admit every destination at or below the bound rather than
                    // only the exact one.
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.DirectMoveDestinationPolicy.Unrestricted))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        spawn, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.SpawnV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("spawned", EffectValueKind.Minion),
                            new EffectOutputBinding("at", EffectValueKind.Coordinate)
                        })
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-direct-move-spawn-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

        /// <summary>
        /// Milestone 6.6a's conversion: the modal Skill `Choose one: Swap your hero with another
        /// adjacent unit; or Replace an adjacent Hazard token with a Ward from your supply; or Remove
        /// an adjacent token to its supply`, replacing this card's printed Primary Attack.
        ///
        /// All three branches are Sequences behind milestone 6.8k's target selection, because each
        /// operation names the piece it works on by space and a role cannot supply a coordinate. What
        /// differs between them is what each names, and that is the card text's difference rather than
        /// this table's: p.19 lets a Swap exchange any two objects, lets a Remove take any game piece,
        /// and this card says unit and token. Each branch declares its own narrowing on the operation
        /// that performs it, so milestone 6.8l offers only the pieces that mode can act on - the Swap
        /// never names the adjacent Hazard, and the Remove never names the adjacent minion.
        ///
        /// The Swap names the acting hero through the declaration whose first object is the actor,
        /// rather than through the actor's-space bridge the fixture hand's board-object Skill uses.
        /// `your hero` is not a decision, and it is not a step either: a bridge node would have to run
        /// before the selection for the consumer to be readable, and a walk judging an option has run
        /// nothing yet, so the branch would be offered whenever anything at all stood beside the actor
        /// and choosing a piece the Swap cannot take would spend the action on a stop. The fixture
        /// hand's card keeps the bridge and its unnarrowed selection, which is the conservative case
        /// milestone 6.8l's coverage rests on.
        ///
        /// The Replace names no substitute and no supply: which token comes in, where the outgoing one
        /// goes, that nothing transfers, and that observers see one replacement are all the capability's
        /// own declaration, and a card that restated them would be stating a rule of its own. None of
        /// it is official content, and the card names no hero, no card and no space.
        ///
        /// When the configured scene went back to five cards per hero and the Blue Cache card stopped
        /// being dealt, its placement deliberately did not come here, though this is the card whose
        /// family it belongs to - a token is a board object, and these three branches are the
        /// board-object operations p.19 gives that same piece. Implementation refuted it. Rulebook p.8
        /// removes every placed token at the end of a round and a card is played at most once in a
        /// round, so a Hazard this card placed would always be gone before this card returned to hand,
        /// and the Replace and the Remove would name a piece that could never be standing. The
        /// placement went to Blue Feint instead, one Initiative above this card, so both are playable
        /// in the same round and the Hazard is still there when this card is reached - which is the
        /// shape milestone 6.2d had with the Cache card, kept without the sixth card.
        /// </summary>
        private static ActionDefinition CreateExchangeSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("sentinel-root");
            var swapBranch = new EffectNodeId("sentinel-swap-branch");
            var swapSelect = new EffectNodeId("sentinel-swap-select");
            var swap = new EffectNodeId("sentinel-swap");
            var replaceBranch = new EffectNodeId("sentinel-replace-branch");
            var replaceSelect = new EffectNodeId("sentinel-replace-select");
            var replace = new EffectNodeId("sentinel-replace");
            var removeBranch = new EffectNodeId("sentinel-remove-branch");
            var removeSelect = new EffectNodeId("sentinel-remove-select");
            var remove = new EffectNodeId("sentinel-remove");

            // `another adjacent` is Radius 1 with the acting hero excluded, in all three branches.
            EffectNodeDefinition Selection(EffectNodeId id, EffectNodeId consumer) =>
                new EffectNodeDefinition(
                    id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.ChooseTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(consumer))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                profile.ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { swapBranch, replaceBranch, removeBranch },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        swapBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { swapSelect, swap }),
                    Selection(swapSelect, swap),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapWithActorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", swapSelect, "chosen", EffectValueKind.Coordinate),
                            // `unit` rather than p.19's general object, which is why an adjacent
                            // token is never among this branch's candidates.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.BoardObjects.SwapEligiblePieceKind.Unit))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        replaceBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { replaceSelect, replace }),
                    Selection(replaceSelect, replace),
                    new EffectNodeDefinition(
                        replace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.ReplaceTokenV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", replaceSelect, "chosen", EffectValueKind.Coordinate)
                        },
                        outputs: new[] { new EffectOutputBinding("replaced", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        removeBranch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { removeSelect, remove }),
                    Selection(removeSelect, remove),
                    new EffectNodeDefinition(
                        remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.RemoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", removeSelect, "chosen", EffectValueKind.Coordinate),
                            // `token` rather than p.19's general game piece. A turn-time Remove of
                            // the last minion of a side out of the Battle Zone is one of p.8's two
                            // recognized causes, and nothing can emit that push until milestone 6.9,
                            // so no configured card may reach it.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.BoardObjects.RemoveEligiblePieceKind.Token))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                            new EffectOutputBinding("lanePush", EffectValueKind.Boolean)
                        })
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-exchange-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

        /// <summary>
        /// Milestone 6.7a's conversion: the Skill `Choose another adjacent unit and Push it 1 space;
        /// you may Repeat this action once`, replacing this card's printed Primary Attack.
        ///
        /// The root is a plain Sequence rather than a `Choose`, because this card text names one thing
        /// to do rather than alternatives, and the Repeat is its last child, which is where p.10 puts
        /// it: after you perform this action, you may perform the entire action again. The `may` is the
        /// node's failure policy rather than an `Optional` wrapper around it, because there is no step
        /// to wrap - the Repeat performs nothing itself and the decision is the node's own.
        ///
        /// Both leaves before it are milestone 6.5a's push branch at a different distance, which is the
        /// point of scheduling this card last: everything it does is already registered and already has
        /// a production consumer, so what the conversion adds is the repeat and nothing else. `another
        /// adjacent unit` is Radius 1 with the acting hero excluded, and the selection names the Push as
        /// the node it selects for, so milestone 6.8l offers only the objects that Push can actually
        /// take. The activity is `ObjectPush` because this is card-text Push, which p.18 gives an
        /// activity of its own - unlike card-text direct Move, which has none and reads its enclosing
        /// action's.
        ///
        /// Distance 1 under a mandatory policy is declaration rather than behaviour, and it is the
        /// distance that makes the second instance say something: a push of 1 directly away leaves the
        /// subject two spaces off, so a repeated instance has to find a different unit or find none,
        /// while a blocked push leaves it adjacent and legally nameable again. None of it is official
        /// content, and the card names no hero, no card and no space.
        /// </summary>
        private static ActionDefinition CreatePushRepeatSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("longbow-root");
            var selectTarget = new EffectNodeId("longbow-select-target");
            var push = new EffectNodeId("longbow-push");
            var repeat = new EffectNodeId("longbow-repeat");

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                profile.ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectTarget, push, repeat }),
                    new EffectNodeDefinition(
                        selectTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                "activity", EffectValue.Integer((int)Rules.Modifications.RuleActivity.ObjectPush)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(push))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.PushTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            // `unit` rather than p.18's own unit-or-token breadth, which is why an
                            // adjacent token is never among this branch's candidates.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.Unit))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        }),
                    // `once` is the node kind's own bound rather than a count: a Repeat refuses to be
                    // mandatory, and the instance it starts runs under a repeated context in which this
                    // node is ineligible and silently skipped.
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor)
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-push-repeat-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

        private static ActionDefinition CreateDefenseAction(int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId("defense-reaction"),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
        }

        /// <summary>
        /// Which printed Primary action a synthetic profile carries. The showcase milestones convert
        /// six of the ten cards one at a time, so this is the seam each conversion moves through.
        /// </summary>
        private enum SyntheticPrintedPrimaryKind
        {
            Attack = 0,
            ModificationSkill = 1,
            ImmunityTimingSkill = 2,
            PushPlaceSkill = 3,
            DirectMoveSpawnSkill = 4,
            ExchangeSkill = 5,
            PushRepeatSkill = 6
        }

        private sealed class SyntheticCardProfile
        {
            /// <summary>
            /// The revision the five cards were printed at since milestone 5.1a. A converted card
            /// advances past it alone, so the unconverted profiles stay byte-identical and only the
            /// converted card's definition moves a pinned digest.
            /// </summary>
            /// <summary>
            /// Milestone 7.1's revision, and the first one all ten configured cards have shared since
            /// 5.1a: every one of them gains colour and Tier content in the same milestone, so every
            /// one of them moves, and there is nothing left for the per-card strings to distinguish.
            /// It is also what the generated upgrade-target definitions carried when 7.1 created them;
            /// milestone 7.2 moved those alone to <see cref="SyntheticUpgradeTree.ItemIconRevision"/>,
            /// because only they gained content in it.
            /// </summary>
            public const string ProgressionRevision = "phase-7-1-rev-1";

            public SyntheticCardProfile(
                string id,
                string displayName,
                int initiative,
                AttackRangeKind attackRange,
                int attackValue,
                int rangeValue,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression)
                : this(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.Attack, ProgressionRevision,
                    attackRange, attackValue, rangeValue, movementValue, defenseValue, progression)
            {
            }

            private SyntheticCardProfile(
                string id,
                string displayName,
                int initiative,
                SyntheticPrintedPrimaryKind printedPrimary,
                string contentRevision,
                AttackRangeKind attackRange,
                int attackValue,
                int rangeValue,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                PrintedPrimary = printedPrimary;
                ContentRevision = contentRevision;
                AttackRange = attackRange;
                AttackValue = attackValue;
                RangeValue = rangeValue;
                MovementValue = movementValue;
                DefenseValue = defenseValue;
                Progression = progression;
            }

            /// <summary>
            /// Milestone 6.8a's converted card: it keeps its identity, Initiative, printed Secondary
            /// Movement and Defense reaction, and states no Attack values at all, because its printed
            /// Primary Attack is replaced rather than suppressed.
            /// </summary>
            public static SyntheticCardProfile WithModificationSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.ModificationSkill,
                    ProgressionRevision, AttackRangeKind.None, 0, 0, movementValue, defenseValue,
                    progression);

            /// <summary>
            /// Milestone 6.8b's converted card, on the same terms: identity, Initiative, printed
            /// Secondary Movement and Defense reaction all stay, and it states no Attack values at all,
            /// because its printed Primary Attack is replaced rather than suppressed.
            ///
            /// Revision 2 is the two marker modes the retired Blue Cache card handed over. Only this
            /// profile and Blue Sentinel's advance; the eight others, both Red hands included, stay
            /// byte-identical, so the digests that move are the two Blue definitions and the instances
            /// referring to them rather than the whole table.
            /// </summary>
            public static SyntheticCardProfile WithImmunityTimingSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.ImmunityTimingSkill,
                    ProgressionRevision, AttackRangeKind.None, 0, 0, movementValue, defenseValue,
                    progression);

            /// <summary>
            /// Milestone 6.5a's converted card, on the same terms as the two before it, and the one
            /// configured card whose revision milestone 7.2c moves.
            ///
            /// It advances alone rather than through <see cref="ProgressionRevision"/>, which the ten
            /// configured cards otherwise share. Moving the shared constant would declare a
            /// compatibility cause for nine cards that did not change, which is the reasoning milestone
            /// 7.2 recorded when it left all ten alone and moved only the generated upgrade targets.
            /// </summary>
            public const string PushPlaceRadiusRevision = "phase-7-2c-rev-1";

            public static SyntheticCardProfile WithPushPlaceSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.PushPlaceSkill,
                    PushPlaceRadiusRevision, AttackRangeKind.None, 0, 0, movementValue, defenseValue,
                    progression);

            /// <summary>Milestone 6.5b's converted card, on the same terms as the three before it.</summary>
            public static SyntheticCardProfile WithDirectMoveSpawnSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.DirectMoveSpawnSkill,
                    ProgressionRevision, AttackRangeKind.None, 0, 0, movementValue, defenseValue,
                    progression);

            /// <summary>
            /// Milestone 6.6a's converted card, on the same terms as the four before it. It is the
            /// first Blue conversion since 6.8b, and with it the hands' one cross-hero Initiative tie
            /// - Red Bulwark and this card, both at 11 - is a tie between two Skills, so a tied turn
            /// can no longer perform an Attack.
            /// </summary>
            public static SyntheticCardProfile WithExchangeSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.ExchangeSkill,
                    ProgressionRevision, AttackRangeKind.None, 0, 0, movementValue, defenseValue,
                    progression);

            /// <summary>
            /// Milestone 6.7a's converted card, the last of the showcase six and on the same terms as
            /// the five before it. It takes both hands to the schedule's floor of exactly one printed
            /// non-Ranged and one printed Ranged Attack each - Blue Hammer and Blue Scout Shot on this
            /// side - and it takes the lowest Initiative in either hand off the printed-Attack list, so
            /// no card below Initiative 5 prints an Attack any more. Its Defense reaction, Secondary
            /// Movement and Initiative are untouched, which is why a fixture that commits it to lose
            /// the turn or to react on Defense is unaffected by the conversion.
            /// </summary>
            public static SyntheticCardProfile WithPushRepeatSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue,
                CardProgressionProfile progression) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.PushRepeatSkill,
                    ProgressionRevision, AttackRangeKind.None, 0, 0, movementValue, defenseValue,
                    progression);

            public string Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public SyntheticPrintedPrimaryKind PrintedPrimary { get; }
            public string ContentRevision { get; }
            public AttackRangeKind AttackRange { get; }
            public int AttackValue { get; }
            public int RangeValue { get; }
            public int MovementValue { get; }
            public int DefenseValue { get; }

            /// <summary>Milestone 7.1's printed colour, Tier, classification and upgrade candidates.</summary>
            public CardProgressionProfile Progression { get; }
        }

        /// <summary>
        /// One hand's Ultimate, as the two things that vary between two heroes' Ultimates: which card
        /// it is, and what it is called. Everything else p.16 fixes for all of them.
        /// </summary>
        private sealed class SyntheticUltimateProfile
        {
            public SyntheticUltimateProfile(string id, string displayName)
            {
                Id = id;
                DisplayName = displayName;
            }

            public string Id { get; }

            public string DisplayName { get; }
        }

        /// <summary>
        /// Milestone 7.3's synthetic Ultimate: rulebook p.16's "special Tier IV card", built from a
        /// hand's own profile rather than written out twice.
        ///
        /// What it prints is exactly what p.16 shows and nothing more. There is no Initiative, because
        /// the printed card carries no value column and is never played into a turn. There are no
        /// action definitions, because the card is not an action - it is placed beside the dashboard and
        /// its ability "is always in effect". There is no Item icon, because that belongs to "every
        /// Tier II and Tier III Action card". And there is no colour, because the printed swatch is none
        /// of p.15's five and no rule reads it.
        ///
        /// What it does print is the permanent ability, and that ability is p.18's Move Through grant
        /// rather than something invented for the occasion. The permission vocabulary has exactly one
        /// member, and it has one deliberately: <c>RulePermissionKind</c> records that a permission
        /// "names a grant, not an activity" and is not widened ahead of the verified text that requires
        /// it. A synthetic Ultimate that granted a second, unsourced permission - or a numeric bonus
        /// chosen out of the air - would be the rebalancing milestone 7.1's generated cards go out of
        /// their way to avoid. Granting a rule the rulebook already defines, permanently and to its own
        /// hero, is the smallest content that makes p.16's sentence observable.
        /// </summary>
        private static class SyntheticUltimateCard
        {
            /// <summary>
            /// The revision these two definitions carry, and nothing else does. They are new content
            /// rather than changed content, so no existing card's revision moves with them.
            /// </summary>
            public const string UltimateRevision = "phase-7-3-rev-1";

            public static CardDefinitionId DefinitionId(SyntheticUltimateProfile profile) =>
                new CardDefinitionId($"placeholder-{profile.Id}");

            public static CardDefinition Definition(SyntheticUltimateProfile profile) =>
                new CardDefinition(
                    DefinitionId(profile),
                    UltimateRevision,
                    $"TEST - {profile.DisplayName}",
                    default,
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.UltimateCard(),
                    Array.Empty<ActionDefinition>(),
                    new UltimateRuleDefinition(
                        "permanent-move-through",
                        "SyntheticPermission",
                        GuardsDigital.Core.Rules.Modifications.RuleModification.ForPermission(
                            GuardsDigital.Core.Rules.Modifications.RulePermissionKind
                                .MoveThroughObstacles)));
        }

        /// <summary>
        /// Milestone 7.1's synthetic upgrade content: the four definitions a configured Tier I card can
        /// eventually become, generated from that card rather than written out four times.
        ///
        /// The shape is p.15's. Each Tier I card names two Tier II cards of its own colour; both of
        /// those name the same two Tier III cards, because the page sends the player back to the same
        /// deck with the same instruction ("two Action cards in your deck that have the same color as
        /// the chosen card but are one Tier higher") and the other Tier II card has already been set
        /// aside as an item by then. A Tier III card names none: the only card above it is the
        /// Ultimate, which p.16 unlocks instead of upgrading into, and which milestone 7.3 owns. Three
        /// colours of five cards each plus the Basic pair is seventeen definitions per hand, which is
        /// p.6's eighteen-card deck less the Ultimate this milestone does not add.
        ///
        /// What they print is deliberately minimal and deliberately says so: one printed Primary
        /// Movement carrying the source card's Movement value, and the source card's Defense reaction.
        /// They are not conversions of the card they upgrade from, they invent no power curve - copying
        /// the source values rather than raising them is what keeps this from rebalancing anything -
        /// and no Skill effect is duplicated into them. Milestone 7.5 is the first that moves one into a
        /// hand, and it is the milestone that will have a reason to print anything more.
        ///
        /// The Movement Primary is also evidence rather than convenience. p.7 ties an official card's
        /// primary icon to its colour; these print Movement whatever their colour, so a Red or Blue
        /// upgrade card prints the green icon, and no query in this build can be reading colour off an
        /// action kind.
        /// </summary>
        private static class SyntheticUpgradeTree
        {
            /// <summary>
            /// The stable id of one generated upgrade target. Public shape rather than a private
            /// convention because the Tier I profiles name their candidates through it, so the id a
            /// card states and the id a definition is built with cannot drift apart.
            /// </summary>
            public static CardDefinitionId DefinitionId(string cardId, CardTier tier, char branch) =>
                new CardDefinitionId($"placeholder-{cardId}-t{(int)tier}{branch}");

            /// <summary>
            /// Milestone 7.2's revision, carried by these twenty-four definitions and by nothing else.
            ///
            /// They are the only definitions the milestone changes: p.16 prints an Item icon on every
            /// Tier II and Tier III card, and every Tier II and Tier III card in this build is one of
            /// these. The ten configured cards are Tier I or Basic, print no Item icon, and so keep
            /// <see cref="SyntheticCardProfile.ProgressionRevision"/> - moving them would have declared a
            /// content change that did not happen and cost a compatibility cause for nothing.
            /// </summary>
            public const string ItemIconRevision = "phase-7-2-rev-1";

            /// <summary>
            /// Which Item icon each generated upgrade target prints, stated per branch rather than
            /// derived from anything.
            ///
            /// The four are distinct because that is what makes p.15's upgrade a decision with a
            /// consequence rather than a formality, which is the shape p.16 illustrates with its own
            /// worked example: the same upgrade offers a card whose item gives +1 Attack and one whose
            /// item gives +1 Initiative, and taking one sets the other aside. Milestone 7.5 is the first
            /// that makes a hero choose between them.
            ///
            /// Attack is deliberately among them even though these cards print no Attack icon, and that
            /// is evidence rather than an oversight: p.16's Item icon says what the item gives, not what
            /// the card it came from does, and an item whose slot an Action card does not print "has no
            /// effect on that particular Action card". Content that could not express that case would
            /// leave the page's own worked counter-example provable only in a hand-built fixture.
            ///
            /// Range and Radius are the two of the six that no generated card carries. Nothing rules on
            /// that - <see cref="ItemBonusQuery"/> answers for all six channels alike and is proved for
            /// all six - and four branches cannot carry six icons without repeating one, which would
            /// have made the upgrade choice smaller than p.16 draws it.
            /// </summary>
            private static PrintedValueChannel ItemIcon(CardTier tier, char branch)
            {
                if (tier == CardTier.II)
                {
                    return branch == 'a' ? PrintedValueChannel.Attack : PrintedValueChannel.Initiative;
                }

                return branch == 'a' ? PrintedValueChannel.Movement : PrintedValueChannel.Defense;
            }

            /// <summary>The card id one generated upgrade target is dealt under, matching its definition id.</summary>
            private static string CardId(string cardId, CardTier tier, char branch) =>
                $"{cardId}-t{(int)tier}{branch}";

            /// <summary>
            /// The four upgrade targets one Tier I card leads to, each paired with the card id its deck
            /// instance is dealt under (milestone 7.5). One statement produces both, so a definition and
            /// the card that carries it cannot be named differently.
            /// </summary>
            public static IEnumerable<KeyValuePair<string, CardDefinition>> DealtDefinitions(
                SyntheticCardProfile profile)
            {
                var progression = profile.Progression;
                if (!progression.IsNonBasic || progression.Tier != CardTier.I)
                {
                    return Enumerable.Empty<KeyValuePair<string, CardDefinition>>();
                }

                var tierThree = new[]
                {
                    DefinitionId(profile.Id, CardTier.III, 'a'),
                    DefinitionId(profile.Id, CardTier.III, 'b')
                };

                return new[]
                {
                    Dealt(profile, CardTier.II, 'a', tierThree),
                    Dealt(profile, CardTier.II, 'b', tierThree),
                    Dealt(profile, CardTier.III, 'a', Array.Empty<CardDefinitionId>()),
                    Dealt(profile, CardTier.III, 'b', Array.Empty<CardDefinitionId>())
                };
            }

            private static KeyValuePair<string, CardDefinition> Dealt(
                SyntheticCardProfile profile,
                CardTier tier,
                char branch,
                IReadOnlyList<CardDefinitionId> nextTierCandidateIds) =>
                new KeyValuePair<string, CardDefinition>(
                    CardId(profile.Id, tier, branch),
                    Create(profile, tier, branch, nextTierCandidateIds));

            private static CardDefinition Create(
                SyntheticCardProfile profile,
                CardTier tier,
                char branch,
                IReadOnlyList<CardDefinitionId> nextTierCandidateIds)
            {
                var numeral = tier == CardTier.II ? "II" : "III";
                return new CardDefinition(
                    DefinitionId(profile.Id, tier, branch),
                    ItemIconRevision,
                    $"TEST - {profile.DisplayName} {numeral}-{char.ToUpperInvariant(branch)}",
                    new InitiativeValue(profile.Initiative),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NonBasicCard(
                        profile.Progression.Color, tier, ItemIcon(tier, branch), nextTierCandidateIds),
                    new[]
                    {
                        CreatePrimaryMovementAction(profile.MovementValue),
                        CreateDefenseAction(profile.DefenseValue)
                    });
            }

            /// <summary>
            /// A printed Primary Movement, which p.7 lists among the five primary icons. It differs
            /// from <see cref="CreateMovementAction"/> only in origin, and that difference is the whole
            /// content of these cards: card text applies when performing a Primary action, so the
            /// policy says Applies where the printed Secondary one says Suppressed.
            /// </summary>
            private static ActionDefinition CreatePrimaryMovementAction(int value) =>
                new ActionDefinition(
                    new ActionDefinitionId("primary-movement"),
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true));
        }
    }
}
