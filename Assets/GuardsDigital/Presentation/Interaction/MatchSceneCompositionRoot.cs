using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Which cards the scene deals when it composes. Milestone 8.4.
    /// </summary>
    public enum SceneCardContent
    {
        /// <summary>
        /// The configured match: rulebook p.5's official six-player line-up - Wasp, Xargatha and
        /// Tigerclaw on Red against Arien, Brogan and Sabina on Blue - each dealt their own verified
        /// printed set. This is what a player, and the built player the smoke gate runs, always gets.
        ///
        /// <para>Milestone 8.25 made it six. It was two mirrored Wasps for as long as one verified set
        /// was all this build had, and two different heroes from milestone 8.20; both boards are still
        /// composed by the arms below, which the fixtures whose subject is one printed set ask for by
        /// name.</para>
        /// </summary>
        ConfiguredMatch = 0,

        /// <summary>
        /// The mechanics-showcase hands, for the Play Mode fixtures whose subject is what a real scene
        /// draws for a composed Skill: greyed controls, board cues, effect-choice destinations,
        /// generated token views. Verified content cannot stand in for them, because Wasp's own text
        /// lands at the 8.6-8.13 slices and until then every printed primary of hers is refused.
        /// </summary>
        MechanicsShowcaseFixture = 1,

        /// <summary>
        /// The verified starting hands with both Wasps restaged already engaged - each adjacent to one
        /// Blue melee Battle-Zone minion, two spaces from each other around it - for the milestone
        /// 8.15 player-pipeline journeys whose subject is a played card and not the approach march.
        /// Cards, values and rules are exactly <see cref="ConfiguredMatch"/>'s; only the two heroes'
        /// staged spaces differ, and the Tie Breaker side is fixed under `DEV-005` so a journey is
        /// deterministic.
        /// </summary>
        VerifiedEngagedMirror = 2,

        /// <summary>
        /// The engaged mirror at rulebook p.15's Tier II stage: complementary Tier II hands between
        /// the seats, returned Tier I cards in the deck. Milestone 8.15, on `DEV-004` terms - a live
        /// match cannot be seeded to this position, so the journeys that verify the upgraded cards
        /// in play start from the position p.15's own procedure produces.
        /// </summary>
        VerifiedTierTwoEngagedMirror = 3,

        /// <summary>
        /// The engaged mirror at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats. Milestone 8.15, on the same terms.
        /// </summary>
        VerifiedTierThreeEngagedMirror = 4,

        /// <summary>
        /// Milestone 8.20. The staged Wasp-versus-Arien hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedMirror"/> stages its two - same spaces, same
        /// minion, same fixed Tie Breaker side under `DEV-005` - so an Arien journey starts from a
        /// played card rather than from the approach march.
        /// </summary>
        VerifiedEngagedStagedArien = 5,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Arien holding the first sibling
        /// of each colour. `DEV-004` terms, for the reason the mirror's own progressed stages are on
        /// them.
        /// </summary>
        VerifiedTierTwoEngagedStagedArien = 6,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats - High Voltage in Wasp's area and Living Tsunami in Arien's.
        /// </summary>
        VerifiedTierThreeEngagedStagedArien = 7,

        /// <summary>
        /// The Tier II staging with Arien holding the <em>second</em> sibling of each colour - Rogue
        /// Wave, Slippery Ground and Arcane Whirlpool - which is the other half of p.15's choice and
        /// therefore the other half of his printed set.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedArien = 8,

        /// <summary>
        /// The Tier III staging with Arien holding the second sibling of each colour: Tidal Blast,
        /// Deluge and Ebb and Flow.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedArien = 9,

        /// <summary>
        /// Milestone 8.20. The verified Wasp mirror at p.6's own spawn positions - the scene the
        /// default arm composed before this milestone put Arien on the Blue seat.
        ///
        /// <para>It is named rather than gone because one fixture's subject genuinely is the mirror:
        /// the pointer-driven Attack that ends on p.14's Block beside numeric Defense choices needs a
        /// defender holding a Block, and Arien prints none - his three Duelist cards print p.14's other
        /// Defense shape instead. That scenario also drives an eight-turn approach from the spawns, so
        /// the engaged staging cannot stand in for it.</para>
        /// </summary>
        VerifiedMirror = 10,

        /// <summary>
        /// Milestone 8.21. The staged Wasp-versus-Brogan hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedArien"/> stages its two - same spaces,
        /// same minion, same fixed Tie Breaker side under `DEV-005`.
        ///
        /// <para>Brogan takes the Blue seat rather than joining it. Milestone 8.25 owns the official
        /// six-player configuration, so a third dealt hero is a fixture's choice of which two play
        /// rather than a wider match, and <see cref="ConfiguredMatch"/> still deals Wasp against
        /// Arien.</para>
        /// </summary>
        VerifiedEngagedStagedBrogan = 11,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Brogan holding the first sibling
        /// of each colour - Bullrush, Mighty Punch and Bolster. `DEV-004` terms.
        /// </summary>
        VerifiedTierTwoEngagedStagedBrogan = 12,

        /// <summary>
        /// The Tier II staging with Brogan holding the <em>second</em> sibling of each colour -
        /// Throwing Axe, Shield Bash and War Drummer - which is the other half of p.15's choice and
        /// therefore the other half of his printed set.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedBrogan = 13,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats - High Voltage in Wasp's area and One Man Army in Brogan's - and Brogan
        /// holding the first sibling of each colour.
        /// </summary>
        VerifiedTierThreeEngagedStagedBrogan = 14,

        /// <summary>
        /// The Tier III staging with Brogan holding the second sibling of each colour: Throwing Spear,
        /// Counterattack and Master Skald.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedBrogan = 15,

        /// <summary>
        /// Milestone 8.21. Wasp on Red against Arien <em>and</em> Brogan on Blue - the one composition
        /// in this build that deals two heroes to a team.
        ///
        /// <para>It exists for two of Brogan's nineteen faces. War Drummer and Master Skald print "A
        /// friendly hero in range gains N coins", and rulebook p.10 keeps the acting hero out of what
        /// an action affects unless the text says so; with one hero per team that selection has no
        /// candidate at all, so those two cards could not be driven through the player pipeline in any
        /// other composition this build has.</para>
        ///
        /// <para>The three heroes stand at their own spawn points rather than restaged into an
        /// engagement. The staging exists to put a hero next to an enemy minion, and neither of these
        /// cards names an enemy: what they need is an ally in range, and the second Blue spawn is two
        /// spaces from the first - well inside their printed Range 4.</para>
        ///
        /// <para>Still not the six-player setup, which milestone 8.25 owns: Red fields one hero and
        /// <see cref="ConfiguredMatch"/> is untouched.</para>
        /// </summary>
        VerifiedStagedBroganWithAlly = 16,

        /// <summary>
        /// The same three-hero staging with Brogan's own seat dealt at rulebook p.15's Tier II stage
        /// on the second-sibling branch, which is the only position War Drummer is in his hand from.
        ///
        /// <para>It is a separate arm rather than a parameter of <see cref="VerifiedStagedBroganWithAlly"/>
        /// because a scene request names one composition: p.6 leaves War Drummer in the deck, so the
        /// starting-hand arm above cannot reach the card the ally seat exists for.</para>
        /// </summary>
        VerifiedStagedBroganWithAllyAtTierTwo = 17,

        /// <summary>
        /// The three-hero staging at rulebook p.15's Tier III stage on the second-sibling branch,
        /// where Master Skald is in Brogan's hand - the Tier III half of the same pair.
        /// </summary>
        VerifiedStagedBroganWithAllyAtTierThree = 18,

        /// <summary>
        /// Milestone 8.22. The staged Wasp-versus-Sabina hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedBrogan"/> stages its two - same spaces,
        /// same minion, same fixed Tie Breaker side under `DEV-005`.
        ///
        /// <para>Sabina takes the Blue seat rather than joining it. Milestone 8.25 owns the official
        /// six-player configuration, so a fourth dealt hero is a fixture's choice of which two play
        /// rather than a wider match, and <see cref="ConfiguredMatch"/> still deals Wasp against
        /// Arien.</para>
        ///
        /// <para><b>No ally arm accompanies these, and the contrast with Brogan's is the reason worth
        /// recording.</b> His three-hero arms exist because two of his faces name a friendly hero and a
        /// one-hero team gave that selection no candidate. Sabina's Blue and Green kit commands minions
        /// instead, and the engaged staging already puts her beside one Blue melee Battle-Zone minion
        /// with eleven more on the board - so every face of hers has something to act on here.</para>
        /// </summary>
        VerifiedEngagedStagedSabina = 19,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Sabina holding the first sibling
        /// of each colour - Gunslinger, Roger Roger and Marching Orders. `DEV-004` terms.
        /// </summary>
        VerifiedTierTwoEngagedStagedSabina = 20,

        /// <summary>
        /// The Tier II staging with Sabina holding the <em>second</em> sibling of each colour -
        /// Shootout, Steady Advance and Close Support - which is the other half of p.15's choice and
        /// therefore the other half of her printed set.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedSabina = 21,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats - High Voltage in Wasp's area and Big Sodding Gun in Sabina's - and Sabina
        /// holding the first sibling of each colour: Dead Shot, Ready and Waiting and Path to Victory.
        /// </summary>
        VerifiedTierThreeEngagedStagedSabina = 22,

        /// <summary>
        /// The Tier III staging with Sabina holding the second sibling of each colour: Bullet Hell,
        /// Unwavering Resolve and Covering Fire.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedSabina = 23,

        /// <summary>
        /// Milestone 8.23. The staged Wasp-versus-Tigerclaw hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedSabina"/> stages its two.
        ///
        /// <para>Tigerclaw takes the Blue seat rather than joining it, for the reason Brogan's and
        /// Sabina's arms record: milestone 8.25 owns the official six-player configuration, and what
        /// these arms owe until then is a board a player's own input can reach him on. Nothing his
        /// printed set names needs a friendly hero - his thieving Greens take from an ENEMY hero, and
        /// his two Poison faces say "a hero in range", which a one-versus-one answers with the
        /// enemy.</para>
        /// </summary>
        VerifiedEngagedStagedTigerclaw = 24,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Tigerclaw holding the first
        /// sibling of each colour: Combat Reflexes, Sidestep and Pick Pocket.
        /// </summary>
        VerifiedTierTwoEngagedStagedTigerclaw = 25,

        /// <summary>
        /// The Tier II staging with Tigerclaw holding the <em>second</em> sibling of each colour -
        /// Backstab, Parry and Poisoned Dagger - which is the half of p.15's upgrade the row above
        /// does not deal.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedTigerclaw = 26,

        /// <summary>
        /// The engaged staging at p.15's Tier III stage, with p.16's Ultimate unlocked on both seats -
        /// High Voltage in Wasp's area and Cloak and Daggers in Tigerclaw's - and Tigerclaw holding
        /// the first sibling of each colour: Leaping Strike, Evade and Master Thief.
        /// </summary>
        VerifiedTierThreeEngagedStagedTigerclaw = 27,

        /// <summary>
        /// The Tier III staging with Tigerclaw holding the second sibling of each colour: Backstab
        /// with a Ballista, Riposte and Poisoned Dart.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedTigerclaw = 28,

        /// <summary>
        /// Milestone 8.24. The staged Wasp-versus-Xargatha hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedTigerclaw"/> stages its two.
        ///
        /// <para>Xargatha takes the Blue seat rather than joining it, for the reason the three arms
        /// before hers record: milestone 8.25 owns the official six-player configuration. She needs no
        /// ally arm at all, and her reason is the cleanest in the roster - every clause of hers that
        /// names another unit names an ENEMY one, so a friendly hero would widen no candidate set she
        /// has.</para>
        ///
        /// <para><b>What her arms do need is the staging's minions left standing.</b> Five of her
        /// faces are answered only by a minion and three of her values are counted from the ones
        /// beside her, so the engaged position's own neighbours are content here rather than
        /// scenery.</para>
        /// </summary>
        VerifiedEngagedStagedXargatha = 29,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Xargatha holding the first
        /// sibling of each colour: Deadly Swipe, Petrifying Stare and Control.
        /// </summary>
        VerifiedTierTwoEngagedStagedXargatha = 30,

        /// <summary>
        /// The Tier II staging with Xargatha holding the <em>second</em> sibling of each colour - Long
        /// Thrust, Fresh Converts and Constrict - which is the half of p.15's upgrade the row above
        /// does not deal. Her two branches are the widest apart in the roster: this one petrifies
        /// nothing and its Skills do nothing at all until p.8's pre-battle moment.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedXargatha = 31,

        /// <summary>
        /// The engaged staging at p.15's Tier III stage, with p.16's Ultimate unlocked on both seats -
        /// High Voltage in Wasp's area and Metamorphosis in Xargatha's - and Xargatha holding the
        /// first sibling of each colour: Lethal Spin, Turn Into Statues and Dominate.
        ///
        /// <para>Hers is the first unlocked Ultimate in this build that asks nothing and waits for
        /// nothing, so this staging starts with her Movement and Initiative already scaling with
        /// whoever is standing beside her.</para>
        /// </summary>
        VerifiedTierThreeEngagedStagedXargatha = 32,

        /// <summary>
        /// The Tier III staging with Xargatha holding the second sibling of each colour: Rapid
        /// Thrusts, Devoted Followers and Final Embrace.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedXargatha = 33,

        /// <summary>
        /// Milestone 8.25. The staged one-hero-per-team board - Wasp on Red, Arien on Blue, both at
        /// their own spawn points and neither restaged - which is what
        /// <see cref="ConfiguredMatch"/> composed between milestones 8.20 and 8.25.
        ///
        /// <para>It exists because the default stopped being that board and the fixtures written on it
        /// did not: the scene and HUD journeys that measure two rails, two pieces and one hero a side
        /// measure a real composition and go on measuring the same one, by name instead of by being the
        /// default. Nothing about the board changed - same map, same spawn points, same twelve
        /// minions.</para>
        ///
        /// <para>Its Tie Breaker side is selected rather than flipped, on the `DEV-005` terms every
        /// named staged arm here is on. The default's own coin is a real draw and stays one; a journey
        /// that reads which hero the match is waiting on does not need to know which way it landed, and
        /// one that does should not be asking a coin.</para>
        /// </summary>
        VerifiedStagedArien = 34,

        /// <summary>
        /// Milestone 8.35z34. The staged Wasp-versus-Dodger hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedXargatha"/> stages its two. She is the
        /// seventh hero this build deals and the first from outside the mapped pitch roster.
        ///
        /// <para>Dodger takes the Blue seat rather than joining it, for the reason the four arms
        /// before hers record: milestone 8.25 owns the official six-player configuration. She needs no
        /// ally arm - every clause of hers naming another hero names an enemy one.</para>
        ///
        /// <para><b>What her arms need that no earlier hero's did is the staging's minions left
        /// standing on their SPAWN POINTS.</b> Nine of her faces read empty minion Spawn points in the
        /// Battle Zone, so the engaged position's occupied points are what makes her conditions
        /// measurable at all - and a fixture that swept the board clear would silently satisfy every
        /// one of them rather than break.</para>
        /// </summary>
        VerifiedEngagedStagedDodger = 35,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Dodger holding the first sibling
        /// of each colour: Finger of Death, Vampiric Shield and Darker Ritual.
        /// </summary>
        VerifiedTierTwoEngagedStagedDodger = 36,

        /// <summary>
        /// The Tier II staging with Dodger holding the <em>second</em> sibling of each colour - Burning
        /// Skull, Weakness and Necromancy - which is the half of p.15's upgrade the row above does not
        /// deal. It is the only Tier II arm of hers that can bring a minion back: Necromancy is Darker
        /// Ritual's Green sibling, so the branch decides whether this match contains a Respawn at all.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedDodger = 37,

        /// <summary>
        /// The engaged staging at p.15's Tier III stage, with p.16's Ultimate unlocked on both seats -
        /// High Voltage in Wasp's area and Tide of Darkness in Dodger's - and Dodger holding the first
        /// sibling of each colour: Middlefinger of Death, Aegis of Doom and Darkest Ritual.
        ///
        /// <para>Hers is the widest-reaching unlocked Ultimate in this build. Like Xargatha's it asks
        /// nothing and waits for nothing, but what it is true about is the BOARD: while she performs
        /// any action, every space counts as being in the Battle Zone and as carrying a friendly minion
        /// Spawn point. This staging therefore starts with all nine of her spawn-point conditions
        /// already satisfied.</para>
        /// </summary>
        VerifiedTierThreeEngagedStagedDodger = 38,

        /// <summary>
        /// The Tier III staging with Dodger holding the second sibling of each colour: Blazing Skull,
        /// Enfeeblement and Necromastery. This is the one arm in which RULING 48 is reachable through a
        /// player's own input - her Ultimate is up, so Necromastery's Respawn takes any empty space in
        /// its printed Radius, and at a granted point it asks which minion type comes back.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedDodger = 39,

        /// <summary>
        /// Milestone 8.35z38. The staged Wasp-versus-Misa hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedDodger"/> stages its two. She is the
        /// eighth hero this build deals.
        ///
        /// <para>Misa takes the Blue seat rather than joining it, for the reason the five arms before
        /// hers record: milestone 8.25 owns the official six-player configuration. She needs no ally
        /// arm - no face of hers names a friendly hero at all.</para>
        ///
        /// <para><b>What her arms need is the engaged position's DISTANCES.</b> RULING 1 makes four of
        /// her faces ask for something at exactly the printed number, and the engaged staging is what
        /// puts two heroes close enough for a Blue line to cross one and far enough for a ring of four
        /// to have empty members. A fixture that stood them at spawn would leave half her set
        /// unreachable and say nothing about why.</para>
        /// </summary>
        VerifiedEngagedStagedMisa = 40,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Misa holding the first sibling of
        /// each colour: Matter of Honor, Gust of Wind and Discipline.
        ///
        /// <para>This is the branch that carries RULING 31's escalating free move and RULING 29's
        /// place-the-unit-you-moved-through rider - see
        /// <see cref="Cards.Verified.MisaProgressedHands"/>, which records the whole split.</para>
        /// </summary>
        VerifiedTierTwoEngagedStagedMisa = 41,

        /// <summary>
        /// The Tier II staging with Misa holding the <em>second</em> sibling of each colour - Power
        /// Shot, Dash and Slash and Living Tornado - which is the half of p.15's upgrade the row above
        /// does not deal. It is the only Tier II arm of hers that carries a ranged Attack or a Swap at
        /// all, so RULING 32's tense pair and RULING 1's maximum-radius pair are reachable here and
        /// nowhere else at this Tier.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedMisa = 42,

        /// <summary>
        /// The engaged staging at p.15's Tier III stage, with p.16's Ultimate unlocked on both seats -
        /// High Voltage in Wasp's area and Power Overwhelming in Misa's - and Misa holding the first
        /// sibling of each colour: Worthy Opponent, Crushing Squall and Mastery.
        ///
        /// <para>Hers is the only unlocked Ultimate in this build that changes how a DECISION is shaped
        /// rather than what a rule is true of. Its first clause raises the ceiling on her own modal
        /// from one option to two (RULING 28), so this staging is the only one in which Watch How I
        /// Soar's two bullets are reachable in one play; its second arms a discard on her own
        /// self-placements (RULING 30).</para>
        /// </summary>
        VerifiedTierThreeEngagedStagedMisa = 43,

        /// <summary>
        /// The Tier III staging with Misa holding the second sibling of each colour: Thunder Shot,
        /// Death from Above and Storm Spirit. This is the one arm in which RULING 32's present tense
        /// and RULING 1's equal-distance pair are both reachable through a player's own input, with her
        /// Ultimate up.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedMisa = 44,

        /// <summary>
        /// Milestone 8.35z46. The engaged staging with Silverarrow the Pathfinder on the
        /// Blue seat, the ninth hero this build deals.
        ///
        /// <para>She takes the Blue seat rather than joining it, for the reason the six arms before
        /// hers record: milestone 8.25 owns the official six-player configuration. She needs no ally
        /// arm - seventeen of her nineteen faces name an enemy or nobody, and the two that print "a
        /// hero in radius" already admit two classes on this board because RULING 8 includes
        /// herself.</para>
        ///
        /// <para><b>What her arms need that no earlier hero's did is DISTANCE, and the engaged staging
        /// is what supplies it.</b> RULING 1 makes six of her seven Attacks reach a BAND rather than a
        /// ceiling - a unit one space too near is not a legal target at all - so an unrestaged board
        /// fifteen spaces wide puts every one of them out of reach, and the engagement is what brings
        /// the two heroes to a distance her printed Ranges can name. The measured consequence is that
        /// her Attack journeys live here and nowhere else.</para>
        /// </summary>
        VerifiedEngagedStagedSilverarrow = 45,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Silverarrow holding the first
        /// sibling of each colour: Opportunity Shot, Entangling Vines and Divert Attention. This is the
        /// arm RULING 37's realised-distance self-move is reachable on.
        /// </summary>
        VerifiedTierTwoEngagedStagedSilverarrow = 46,

        /// <summary>
        /// The Tier II staging with Silverarrow holding the <em>second</em> sibling of each colour -
        /// Long Shot, Warning Shot and Fae Healing. It is the only Tier II arm carrying an "End of
        /// turn:" face or a retrieval, which is what makes SL-06 and RULING 8 reachable at all.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedSilverarrow = 47,

        /// <summary>
        /// The Tier III staging with Silverarrow holding the first sibling of each colour - Clear Shot,
        /// Grappling Branches and Lead Astray - and p.16's Ultimate unlocked on both seats. Clear
        /// Shot's Range 4 is the longest printed reach in the roster and its modal is where RULING 39
        /// is read.
        /// </summary>
        VerifiedTierThreeEngagedStagedSilverarrow = 48,

        /// <summary>
        /// The Tier III staging with Silverarrow holding the second sibling of each colour: Rain of
        /// Arrows, Treetop Sentinel and Nature's Blessing. This is the one arm on which RULING 38's
        /// mandatory chain is reachable through a player's own input - three Attack instances from one
        /// card, each narrowed to what its own printed clause admits.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedSilverarrow = 49,

        /// <summary>
        /// Milestone 8.35z50. The staged Wasp-versus-Ursafar hands with both heroes
        /// restaged already engaged, exactly as <see cref="VerifiedEngagedStagedMisa"/> stages its two.
        /// He is the TENTH hero this build deals.
        ///
        /// <para><b>These five members are numbered 50-54 rather than the 45-49 this hero's prep branch
        /// drafted.</b> That branch was cut before milestone 8.35z46 dealt Silverarrow, who holds 45-49
        /// on main; the Whisper dealing-prep branch independently drafted the same five numbers, so
        /// three separate claims to one range existed at once. The collision was found by reading the
        /// enum before the merge rather than by a red run, and it is recorded here because the same
        /// shape has now bitten twice - milestone 8.35z46 renumbered Misa's 40-44 for the identical
        /// reason. A prep branch cut before a dealing cannot know which members that dealing took.</para>
        ///
        /// <para>Ursafar takes the Blue seat rather than joining it, for the reason the arms before his
        /// record: milestone 8.25 owns the official six-player configuration. He needs no ally seat -
        /// no face of his names a friendly hero at all.</para>
        ///
        /// <para><b>What his arms need is a target to attack and enemy minions to remove</b> - both of
        /// which the engaged position and p.8's ordinary waves supply, exactly as they do for Sabina's
        /// and Wasp's own minion-reading faces.</para>
        /// </summary>
        VerifiedEngagedStagedUrsafar = 50,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Ursafar holding the first sibling
        /// of each colour: Prey Abundance, Rampaging Beast and Eyes on the Prey.
        ///
        /// <para>See <see cref="Cards.Verified.UrsafarProgressedHands"/>, which records the whole
        /// split: this branch carries the first-arm faces (no borrow, no Life-counter spend).</para>
        /// </summary>
        VerifiedTierTwoEngagedStagedUrsafar = 51,

        /// <summary>
        /// The Tier II staging with Ursafar holding the <em>second</em> sibling of each colour - Rip,
        /// Cold Ire and Instinctive Reaction - which is the half of p.15's upgrade the row above does
        /// not deal. It is the only Tier II arm of his that carries a borrow-bearing face at all, so
        /// THE UNIVERSAL BORROW RULE is reachable here and nowhere else at this Tier.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedUrsafar = 52,

        /// <summary>
        /// The engaged staging at p.15's Tier III stage, with p.16's Ultimate unlocked on both seats -
        /// High Voltage in Wasp's area and Unbound Fury in Ursafar's - and Ursafar holding the first
        /// sibling of each colour: Feeding Frenzy, Unstoppable Force and Apex Predator.
        ///
        /// <para>This is the one arm in which the two-minion arm of the target-survived removal is
        /// reachable through a player's own input, with his Ultimate up.</para>
        /// </summary>
        VerifiedTierThreeEngagedStagedUrsafar = 53,

        /// <summary>
        /// The Tier III staging with Ursafar holding the second sibling of each colour: Tear, Eyes of
        /// Flame and Evolutionary Response. This is the one arm in which RULING 35's Life-counter spend
        /// and the borrow's post-Ultimate widening (RULING 43) are both reachable through a player's
        /// own input, with his Ultimate up.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedUrsafar = 54,

        /// <summary>
        /// Milestone <c>8.35z53</c>. The staged Wasp-versus-Whisper hands with both
        /// heroes restaged already engaged, exactly as <see cref="VerifiedEngagedStagedMisa"/> stages
        /// its two. She is the ELEVENTH hero this build deals, and the first from the Devoted Hero pack.
        ///
        /// <para>Whisper takes the Blue seat rather than joining it, for the reason the arms before
        /// hers record: milestone 8.25 owns the official six-player configuration. She needs no ally
        /// arm - no face of hers names a friendly hero at all.</para>
        ///
        /// <para><b>What her arms need is CONTACT and the enemy's own spent cards.</b> Her whole Red
        /// line ends in "Target a unit adjacent to you", her Gold basic's second bullet asks for a unit
        /// adjacent to her, and Lesser Evil's and Greater Good's Defeats are measured from her space -
        /// so a fixture that stood the two heroes at spawn would leave most of her set with no subject
        /// at all. The engaged position supplies that. What NO staging can supply is a discard pile:
        /// five of her faces read one and p.6 deals every card to a hand or a deck, so those faces come
        /// alive on turn two and RULING 42 records that as printed rather than as a defect.</para>
        /// </summary>
        VerifiedEngagedStagedWhisper = 55,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Whisper holding the first sibling
        /// of each colour: Sanguine Path, Sprouts of Panic and Shadow Walk.
        ///
        /// <para>This is the branch that MOVES - the Spawn-point-counted step before the Attack, the
        /// two-space enemy displacement and the Range 4 self-placement. See
        /// <see cref="Cards.Verified.WhisperProgressedHands"/>, which records the whole split.</para>
        /// </summary>
        VerifiedTierTwoEngagedStagedWhisper = 56,

        /// <summary>
        /// The Tier II staging with Whisper holding the <em>second</em> sibling of each colour - Blood
        /// Fury, Lesser Evil and Cruel Twist - which is the half of p.15's upgrade the row above does
        /// not deal. It is the only Tier II arm of hers that carries a repeat chain, an opponent-chosen
        /// modal or a Swap at all, so RULING 34's chain and RULING 17's escape hatch are reachable here
        /// and nowhere else at this Tier.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedWhisper = 57,

        /// <summary>
        /// The engaged staging at p.15's Tier III stage, with p.16's Ultimate unlocked on both seats -
        /// High Voltage in Wasp's area and Grim Reaper in Whisper's - and Whisper holding the first
        /// sibling of each colour: Blood Pilgrimage, Blooming Nightmare and Creeping Shadow.
        ///
        /// <para>Grim Reaper raises the option ceiling on her two BASIC cards alone, so this staging is
        /// the only one in which Swift Justice offers both of its printed bullets and Death Seeker
        /// applies both of its. It reaches nothing this arm's three upgrades print, which is the
        /// scoping made observable: p.9 makes every action on a basic card a basic action, and none of
        /// these three is a basic card.</para>
        /// </summary>
        VerifiedTierThreeEngagedStagedWhisper = 58,

        /// <summary>
        /// The Tier III staging with Whisper holding the second sibling of each colour: Blood Frenzy,
        /// Greater Good and Sealed Fate. This is the one arm in which RULING 34's five-instance chain,
        /// RULING 17's opponent-chosen modal at its widest, and Grim Reaper's raised ceiling are all
        /// reachable through a player's own input at once.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedWhisper = 59,


        /// <summary>
        /// Milestone 8.37. The engaged staging with Tali the Spiritcaller on the Blue
        /// seat, the twelfth hero this build deals.
        ///
        /// <para>She takes the Blue seat rather than joining it, for the reason the seven arms before
        /// hers record: milestone 8.25 owns the official six-player configuration. She needs no ally
        /// arm for reachability - Commune with Spirits' mandatory naming already admits herself as a
        /// candidate friendly hero - but the ally-naming half of her full borrow flow is proven on
        /// <see cref="Participants.MatchConfiguration.CreateStagedTaliWithAllyForInternalTest"/>
        /// instead, which this enum does not need its own arm for: that configuration is reached
        /// directly rather than through the engaged one-versus-one staging every other arm here
        /// restages.</para>
        ///
        /// <para>Her Attacks are all adjacent-or-in-radius rather than banded, so the engagement serves
        /// her the way it serves every earlier hero: it brings the two heroes to a distance her printed
        /// Radius and Range disks can actually reach, rather than compensating for a RULING 1-style
        /// band the way Silverarrow's own arm does.</para>
        /// </summary>
        VerifiedEngagedStagedTali = 60,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Tali holding the first sibling of
        /// each colour: Winter Spear, Snowstorm and Wall of Frost - the Ice-heavy track.
        /// </summary>
        VerifiedTierTwoEngagedStagedTali = 61,

        /// <summary>
        /// The Tier II staging with Tali holding the second sibling of each colour: Spirit Wolf,
        /// Guardian Spirit and Ancestral Totem - the Totem/Spirit track, and the only Tier II arm
        /// carrying a retrieval or the modal self-referential gate.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedTali = 62,

        /// <summary>
        /// The Tier III staging with Tali holding the first sibling of each colour - Winter Scepter,
        /// Blizzard and Pack Ice - and p.16's Ultimate unlocked on both seats. Milestone 8.37a put
        /// Blizzard here in place of the second branch's Warrior Spirit, the composition having arrived.
        /// </summary>
        VerifiedTierThreeEngagedStagedTali = 63,

        /// <summary>
        /// The Tier III staging with Tali holding the second sibling of each colour - Spirit Bear,
        /// Warrior Spirit and Venerated Totem - and p.16's Ultimate unlocked on both seats. This is the
        /// only Tier III arm carrying the totem immunity (RULING 26) and the multi-sequence "choose one,
        /// or both" modal.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedTali = 64,

        /// <summary>
        /// Milestone 8.37. Wasp on Red against Tali <em>and</em> an ally (Arien) on Blue
        /// - the shape <see cref="VerifiedStagedBroganWithAlly"/> already proves, with the roles
        /// swapped: there the second Blue hero is what BROGAN's own faces needed a candidate for, here
        /// the second Blue hero is what COMMUNE WITH SPIRITS' mandatory naming step selects, so the
        /// journey can show an ally - not Tali herself - naming the colour.
        ///
        /// <para>Commune with Spirits is in her starting hand, so this is one arm rather than three:
        /// unlike War Drummer and Master Skald, no progressed stage is needed to reach it.</para>
        ///
        /// <para>Not restaged, for the identical reason Brogan's own ally arm is not: neither seat's
        /// selection reads distance, so the two Blue spawns already are wherever the naming step
        /// needs them.</para>
        /// </summary>
        VerifiedStagedTaliWithAlly = 65,

        /// <summary>
        /// Milestone 8.38e. The engaged staging with Swift the Sharpshooter on the Blue seat, holding
        /// rulebook p.6's five starting cards. The thirteenth hero, and the first staged after the
        /// package arc closed.
        /// </summary>
        VerifiedEngagedStagedSwift = 66,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Swift holding the first sibling
        /// of each colour: Prepared Shot, Assault Jump and Pin Down.
        /// </summary>
        VerifiedTierTwoEngagedStagedSwift = 67,

        /// <summary>
        /// The Tier II staging with Swift holding the second sibling of each colour: Shotgun, Delayed
        /// Jump and Mark for Death - the only Tier II arm that reaches an "End of turn:" face and the
        /// only one that stands a bounty.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedSwift = 68,

        /// <summary>
        /// The Tier III staging with Swift holding the first sibling of each colour - Killshot, Drop
        /// Trooper and Killing Ground - and Bullet Time unlocked beside her dashboard.
        /// </summary>
        VerifiedTierThreeEngagedStagedSwift = 69,

        /// <summary>
        /// The Tier III staging with Swift holding the second sibling of each colour - Super-Shotgun,
        /// Mobile Scout and Hunting Season - and Bullet Time unlocked. This is the only arm that
        /// reaches her fast travel, her escalating discard and her two-promise bounty at once.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedSwift = 70,

        /// <summary>
        /// Milestone 8.39i. The engaged staging with Cutter the Sky Pirate on the Blue seat, holding
        /// rulebook p.6's five starting cards. The fourteenth hero.
        /// </summary>
        VerifiedEngagedStagedCutter = 71,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Cutter holding the first sibling
        /// of each colour: Bold Thrust, Barrage and Ramming Speed.
        /// </summary>
        VerifiedTierTwoEngagedStagedCutter = 72,

        /// <summary>
        /// The Tier II staging with Cutter holding the second sibling of each colour: Evasive Shot,
        /// X Marks the Spot and Outmaneuver - the only Tier II arm that reaches her enemy-chosen modal
        /// and her minion swap.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedCutter = 73,

        /// <summary>
        /// The Tier III staging with Cutter holding the first sibling of each colour - Fearless Lunge,
        /// Broadside and Crashland - and Legend of the Skies unlocked beside her dashboard.
        /// </summary>
        VerifiedTierThreeEngagedStagedCutter = 74,

        /// <summary>
        /// The Tier III staging with Cutter holding the second sibling of each colour - Tumble Shot,
        /// A Fistful of Coins and Outsmart - and Legend of the Skies unlocked. This is the only arm
        /// that reaches the solo-victory claim at all.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedCutter = 75,

        /// <summary>
        /// Milestone 8.40j. The engaged staging with Bain the Bounty Hunter on the Blue seat, holding
        /// rulebook p.6's five starting cards. The fifteenth hero.
        /// </summary>
        VerifiedEngagedStagedBain = 76,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Bain holding the first sibling
        /// of each colour: Heavy Crossbow, Dead Man's Hand and Narrow Escape.
        /// </summary>
        VerifiedTierTwoEngagedStagedBain = 77,

        /// <summary>
        /// The Tier II staging with Bain holding the second sibling of each colour: Hand Crossbow,
        /// Drinking Buddies and Vantage Point - the only Tier II arm that reaches a marker modal, the
        /// shared retrieval and a Movement primary.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedBain = 78,

        /// <summary>
        /// The Tier III staging with Bain holding the first sibling of each colour - Arbalest, We're
        /// Not Done Yet! and Perfect Getaway - and A Complicated Profession unlocked beside his
        /// dashboard. This is the only arm that reaches the failure modal.
        /// </summary>
        VerifiedTierThreeEngagedStagedBain = 79,

        /// <summary>
        /// The Tier III staging with Bain holding the second sibling of each colour - Hunter-Seeker,
        /// Another One! and High Ground - and A Complicated Profession unlocked. This is the only arm
        /// that reaches the one-or-both modal and the End-of-turn repeat.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedBain = 80,
        VerifiedEngagedStagedGarrus = 81,
        VerifiedTierTwoEngagedStagedGarrus = 82,
        VerifiedTierTwoAlternateEngagedStagedGarrus = 83,
        VerifiedTierThreeEngagedStagedGarrus = 84,
        VerifiedTierThreeAlternateEngagedStagedGarrus = 85,
        VerifiedEngagedStagedWidget = 86,
        VerifiedTierTwoEngagedStagedWidget = 87,
        VerifiedTierTwoAlternateEngagedStagedWidget = 88,
        VerifiedTierThreeEngagedStagedWidget = 89,
        VerifiedTierThreeAlternateEngagedStagedWidget = 90,

        /// <summary>
        /// Milestone 8.57g. The engaged staging with Rowenna the Vanguard on the Blue seat, holding
        /// rulebook p.6's five starting cards. The EIGHTEENTH hero.
        /// </summary>
        VerifiedEngagedStagedRowenna = 91,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Rowenna holding the first
        /// sibling of each colour: Fair Share, Protect the Weak and Melee.
        /// </summary>
        VerifiedTierTwoEngagedStagedRowenna = 92,

        /// <summary>
        /// The Tier II staging with Rowenna holding the second sibling of each colour: Feat of
        /// Bravery, Accept Surrender and Opening Shots - the only Tier II arm that reaches a Defeat
        /// Blue and the both-parties empty-discard predicate.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedRowenna = 93,

        /// <summary>
        /// The Tier III staging with Rowenna holding the first sibling of each colour - Paragon of
        /// Grace, Defend the Innocent and Grand Melee - and Fabled Lance unlocked beside her
        /// dashboard. This is the only arm that reaches Grand Melee's "Choose up to two times" and
        /// the Range item composing with the Ultimate's printed Range 2 to reach exactly 3.
        /// </summary>
        VerifiedTierThreeEngagedStagedRowenna = 94,

        /// <summary>
        /// The Tier III staging with Rowenna holding the second sibling of each colour - Paragon of
        /// Valor, Glorious Triumph and Opening Volley - and Fabled Lance unlocked. This is the only
        /// arm that reaches the tripled assist coins and Opening Volley's repeat.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedRowenna = 95,

        /// <summary>
        /// Milestone 8.57g. The staged board that gives Rowenna an ALLY - the one configuration on
        /// which "a friendly hero in radius" has a candidate, because milestone 8.57a composed that
        /// selection with <c>includeSelf</c> false. Rowenna holds p.6's five and Arien stands beside
        /// her; it is deliberately NOT restaged, exactly as Brogan's and Tali's ally arms are not.
        /// </summary>
        VerifiedStagedRowennaWithAlly = 96,

        /// <summary>
        /// The ally staging with Rowenna's own seat at rulebook p.15's Tier II stage on the second
        /// sibling of each colour, which is where Feat of Bravery's rider lives.
        /// </summary>
        VerifiedStagedRowennaWithAllyAtTierTwo = 97,

        /// <summary>
        /// The ally staging with Rowenna's own seat at Tier III on the first sibling of each colour.
        /// Paragon of Grace is the face this arm exists for: its p.10 repeat is offered only when the
        /// rider was performed, and the rider needs the ally.
        /// </summary>
        VerifiedStagedRowennaWithAllyAtTierThree = 98,

        /// <summary>
        /// Milestone 8.61j. The engaged staging with Min the Dragonmonk on the Blue seat, holding
        /// rulebook p.6's five starting cards. The NINETEENTH hero.
        /// </summary>
        VerifiedEngagedStagedMin = 99,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Min holding the first sibling
        /// of each colour: Tiger Stance, Cluster Mine and Vanish.
        /// </summary>
        VerifiedTierTwoEngagedStagedMin = 100,

        /// <summary>
        /// The Tier II staging with Min holding the second sibling of each colour: Viper Stance,
        /// Death Grenade and Inner Strength - the only Tier II arm that reaches a Grenade's
        /// end-of-turn window and the item-bonus doubling.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedMin = 101,

        /// <summary>
        /// The Tier III staging with Min holding the first sibling of each colour - Dragon Stance,
        /// Mine Field and Ruse - and Flurry of Blows unlocked beside her dashboard. This is the only
        /// arm that reaches Mine Field's two blasts, Dragon's "1 or 2 spaces" step and Ruse's
        /// re-place.
        /// </summary>
        VerifiedTierThreeEngagedStagedMin = 102,

        /// <summary>
        /// The Tier III staging with Min holding the second sibling of each colour - Cobra Stance,
        /// Holy Death Grenade and Perfect Self - and Flurry of Blows unlocked. This is the only arm
        /// that reaches the two-hero end-of-turn grenade and Perfect Self's deck-card permanent item.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedMin = 103,

        /// <summary>
        /// Milestone 8.69g. The engaged staging with Brynn the Seeker on the Blue seat, holding
        /// rulebook p.6's five starting cards. The TWENTIETH hero.
        /// </summary>
        VerifiedEngagedStagedBrynn = 104,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Brynn holding the first sibling
        /// of each colour: Elevated Ambush, Cover Tracks and Log Trap.
        /// </summary>
        VerifiedTierTwoEngagedStagedBrynn = 105,

        /// <summary>
        /// The Tier II staging with Brynn holding the second sibling of each colour: Split Attack,
        /// Mountain Guide and True Grit - the only Tier II arm that reaches a split repeat, a guide's
        /// two sentences or the attack-card retrieval.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedBrynn = 106,

        /// <summary>
        /// The Tier III staging with Brynn holding the first sibling of each colour - Peak Precision,
        /// Hide Traces and Deadfall Trap - and Over the Top unlocked beside his dashboard. This is the
        /// only arm that reaches Peak Precision's self-retrieval, Hide Traces' "up to 2" trailing move
        /// and Deadfall Trap's defeat.
        /// </summary>
        VerifiedTierThreeEngagedStagedBrynn = 107,

        /// <summary>
        /// The Tier III staging with Brynn holding the second sibling of each colour - Split Throw,
        /// Expedition Leader and Die Hard - and Over the Top unlocked. This is the only arm that
        /// reaches Split Throw's mandatory repeat, Expedition Leader's second sentence and Die Hard's
        /// four-space conditional move.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedBrynn = 108,

        /// <summary>
        /// Milestone 8.71h. The engaged staging with Hanu the Trickster on the Blue seat, holding
        /// rulebook p.6's five starting cards. The TWENTY-FIRST hero.
        /// </summary>
        VerifiedEngagedStagedHanu = 109,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Hanu holding the first sibling
        /// of each colour: Even the Odds, There and Back Again and Monkey Twist.
        /// </summary>
        VerifiedTierTwoEngagedStagedHanu = 110,

        /// <summary>
        /// The Tier II staging with Hanu holding the second sibling of each colour: Outnumber, This
        /// Way! and Hear Nothing - the only Tier II arm that reaches the Reds' MINION bullet, a
        /// co-move or the plain enemy-hero swap.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedHanu = 111,

        /// <summary>
        /// The Tier III staging with Hanu holding the first sibling of each colour - Trusted Sidekick,
        /// Safe Travels and Monkey Business - and The Ultimate Trick unlocked beside his dashboard.
        /// This is the only arm that reaches the 1..2 modal's disjoint bullets, Safe Travels' trailing
        /// move after the swap back (HA-Q8) and Monkey Business's pair swap with its own move.
        /// </summary>
        VerifiedTierThreeEngagedStagedHanu = 112,

        /// <summary>
        /// The Tier III staging with Hanu holding the second sibling of each colour - Pile On, That
        /// Way! and See Nothing - and The Ultimate Trick unlocked. This is the only arm that reaches
        /// Pile On's two minion bullets (RULING 19), That Way!'s three printed distances and See
        /// Nothing's trailing move from the exchanged space.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedHanu = 113,

        /// <summary>
        /// Milestone 8.71h. The staged board that gives Hanu an ALLY - the one configuration on which
        /// "a friendly hero in radius" has a candidate, because milestone 8.71e composed that
        /// selection with <c>includeSelf</c> false. Hanu holds p.6's five and Arien stands beside him;
        /// it is deliberately NOT restaged, exactly as Brogan's, Tali's and Rowenna's ally arms are
        /// not.
        /// </summary>
        VerifiedStagedHanuWithAlly = 114,

        /// <summary>
        /// The ally staging with Hanu's own seat at rulebook p.15's Tier II stage on the second
        /// sibling of each colour, which is where This Way! lives - the first of his two co-moves, and
        /// the only face on that branch that asks the friendly hero a question on their own seat.
        /// </summary>
        VerifiedStagedHanuWithAllyAtTierTwo = 115,

        /// <summary>
        /// The ally staging with Hanu's own seat at Tier III on the second sibling of each colour.
        /// That Way! is the face this arm exists for: its three printed distances are the widest
        /// cross-seat choice in his set, and the friendly hero it asks is the ally.
        /// </summary>
        VerifiedStagedHanuWithAllyAtTierThree = 116,

        /// <summary>
        /// Milestone 8.75b. The engaged staging with Ignatia the Mad on the Blue seat, holding
        /// rulebook p.6's five starting cards and the Tie Breaker coin showing her SUN face. The
        /// TWENTY-SECOND hero.
        ///
        /// <para>The coin face is part of the content for her and for no hero before her: every one
        /// of her eighteen dealt faces prints TWO texts and the coin picks which one runs, so a
        /// staging that fixed only one face would leave half her set unreachable. The sun and moon
        /// members below are the same hand on the same board with the coin turned over.</para>
        /// </summary>
        VerifiedEngagedStagedIgnatia = 117,

        /// <summary>
        /// The engaged staging with Ignatia holding p.6's five and the coin showing her MOON face -
        /// Chaos Bolt targeting a hero in range where the sun member targets a minion adjacent, and
        /// Path of Ashes scattering magma in radius where the sun member lays a wake.
        /// </summary>
        VerifiedEngagedStagedIgnatiaOnTheMoon = 118,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Ignatia holding the first
        /// sibling of each colour: Erratic Fireblast, Spontaneous Immolation and Path of Cinders.
        /// </summary>
        VerifiedTierTwoEngagedStagedIgnatia = 119,

        /// <summary>
        /// The Tier II staging with Ignatia holding the second sibling of each colour: Crack of Doom,
        /// Searing Heat and Unstable Portal - the only Tier II arm that reaches the adjacent /
        /// at-maximum-range attack pair, either push or either swap.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedIgnatia = 120,

        /// <summary>
        /// The Tier III staging with Ignatia holding the first sibling of each colour - Loosely-Aimed
        /// Firebolts, Violent Conflagration and Path of Flames - and Chaos Incarnate unlocked beside
        /// her dashboard. This is the only arm that reaches the moon repeat on a different hero,
        /// Violent Conflagration's "discards a card, or is defeated" escalation and the up-to-four
        /// magma wake.
        /// </summary>
        VerifiedTierThreeEngagedStagedIgnatia = 121,

        /// <summary>
        /// The Tier III staging with Ignatia holding the second sibling of each colour - Imminent
        /// Eruption, Scorching Blaze and Chaos Gate - and Chaos Incarnate unlocked. This is the only
        /// arm that reaches the sun repeat on a minion, the "2 or 3 spaces" push and Chaos Gate's
        /// trailing one-space move after a swap.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedIgnatia = 122,

        /// <summary>
        /// Milestone 8.75b. The staged board that gives Ignatia an ALLY - the one configuration on
        /// which Searing Heat's and Scorching Blaze's SUN faces have a candidate, because p.17's
        /// Friendly never includes you. Ignatia holds p.6's five and Arien stands beside her; it is
        /// deliberately NOT restaged, exactly as Brogan's, Tali's, Rowenna's and Hanu's ally arms are
        /// not.
        /// </summary>
        VerifiedStagedIgnatiaWithAlly = 123,

        /// <summary>
        /// The ally staging with Ignatia's own seat at rulebook p.15's Tier II stage on the second
        /// sibling of each colour, which is where Searing Heat lives - the first of her two pushes,
        /// and the shorter one ("2 spaces", exactly).
        /// </summary>
        VerifiedStagedIgnatiaWithAllyAtTierTwo = 124,

        /// <summary>
        /// The ally staging with Ignatia's own seat at Tier III on the second sibling of each colour,
        /// with Chaos Incarnate unlocked. Scorching Blaze is the face this arm exists for: "2 or 3
        /// spaces" is the widest straight-line push in her set, and the flip her Ultimate offers turns
        /// the same card from shoving an enemy into carrying a friend.
        /// </summary>
        VerifiedStagedIgnatiaWithAllyAtTierThree = 125,

        /// <summary>Wasp versus Trinkets with his p.6 starting hand.</summary>
        VerifiedEngagedStagedTrinkets = 126,

        /// <summary>Wasp versus Trinkets with Tier III hands and their Ultimates faceup.</summary>
        VerifiedTierThreeEngagedStagedTrinkets = 127,

        /// <summary>Wasp versus Snorri with his p.6 starting hand.</summary>
        VerifiedEngagedStagedSnorri = 128,

        /// <summary>Wasp versus Snorri with Tier III hands and Rune Mastery faceup.</summary>
        VerifiedTierThreeEngagedStagedSnorri = 129,

        /// <summary>Wasp versus Snorri with Arien as his Blue ally, unrestaged (8.93g).</summary>
        VerifiedStagedSnorriWithAlly = 130,

        /// <summary>Snorri on Red against Wasp and Arien on Blue (8.93g).</summary>
        VerifiedStagedSnorriVersusTwo = 131,

        /// <summary>Wasp versus Mrak with his p.6 starting hand (8.112g).</summary>
        VerifiedEngagedStagedMrak = 132,

        /// <summary>Wasp versus Mrak with Tier III hands and Rock and a Hard Place faceup (8.112g).</summary>
        VerifiedTierThreeEngagedStagedMrak = 133,

        /// <summary>Mrak on Red against Wasp and Arien on Blue (8.112g).</summary>
        VerifiedStagedMrakVersusTwo = 134
    }

    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MatchSceneCompositionRoot : MonoBehaviour, IDeferredMatchComposition
    {
        [SerializeField] private PlaceholderTestGridView mapView;
        [SerializeField] private PrimitiveHeroPointerInput pointerInput;
        [SerializeField] private Camera eventCamera;
        [SerializeField] private PrimitiveHeroView[] heroViews;
        [SerializeField] private PrimitiveHeroPointerSelector[] pointerSelectors;
        [SerializeField] private Material assignedHighlightMaterial;

        [Tooltip(
            "Milestone 6.5c: the second board highlight, marking where a projected Push would leave " +
            "its target. Left unassigned it is generated in its own dark color, so it never reads " +
            "as a space the player may click.")]
        [SerializeField] private Material assignedResultingSpaceMaterial;

        [Tooltip(
            "Compose the board while the editor is not playing. Off by default: edit-mode composition " +
            "regenerates the full board on every domain reload and can serialize generated objects " +
            "into the saved scene. Enable only to preview, and do not save the scene while enabled.")]
        [SerializeField] private bool composeInEditMode;

        private HexCornerHighlightTemplate cornerHighlightTemplate;
        private ConfiguredMatchHudRenderer hud;
        private ConfiguredMatchSceneRuntime runtime;

        /// <summary>
        /// Milestone 8.35z37. The seat views and selectors THIS root generated, so a
        /// re-composition retires exactly its own and never anything the scene authored.
        /// </summary>
        private readonly List<PrimitiveHeroView> generatedHeroViews =
            new List<PrimitiveHeroView>();

        private readonly List<PrimitiveHeroPointerSelector> generatedSelectors =
            new List<PrimitiveHeroPointerSelector>();

        /// <summary>
        /// Milestone 8.35z37. Views a re-composition has retired. A destroyed Unity object
        /// stays reachable through a scene search until the end of the frame - milestone 6.1e's
        /// finding F613-05 - so the next composition excludes them by identity rather than trusting
        /// <c>FindObjectsByType</c> to have stopped returning them.
        /// </summary>
        private readonly HashSet<PrimitiveHeroView> retiredHeroViews =
            new HashSet<PrimitiveHeroView>();

        private readonly HashSet<PrimitiveHeroPointerSelector> retiredSelectors =
            new HashSet<PrimitiveHeroPointerSelector>();

        /// <summary>
        /// What the next composition deals, and nothing after it. Milestone 8.4.
        ///
        /// <para>The scene deals the configured match, and until milestone 8.25 replaces this
        /// composition with the official six-player setup there is no authored place to say otherwise.
        /// A Play Mode fixture that needs the mechanics-showcase Skills in a real scene states it here
        /// before loading the scene, and the composition <b>consumes and resets it</b>, so a fixture
        /// that forgets to clear it cannot change what any later scene load deals. That one-shot
        /// property is the whole reason this is a value rather than a mode: nothing can be left on.</para>
        ///
        /// <para>A launched player never writes it, exactly as a launched player never constructs
        /// <see cref="PlayerSmokeTestDriver"/>; it is the same internal development affordance
        /// `DEV-004` covers.</para>
        /// </summary>
        public static SceneCardContent NextComposedCardContent { get; set; }

        /// <summary>
        /// Milestone 8.30b. The development-only seat switcher: the seat the next composed match is
        /// played from, or <c>null</c> for the Director's own runtime.
        /// </summary>
        /// <remarks>
        /// <para><b>Not a player-facing control, and structurally unable to become one.</b> It names
        /// an existing seat and nothing else: it cannot create a seat, cannot move one, and cannot
        /// change what a seat owns, because the binding it selects from is derived from the
        /// authoritative participant graph. Naming a seat that is not bound throws rather than
        /// composing something half-authorized.</para>
        /// <para>It is the same one-shot development affordance as
        /// <see cref="NextComposedCardContent"/> above, consumed and reset by the composition, and
        /// the same one `DEV-004` covers. A private-online client is handed one
        /// <c>ISeatClient</c> for the connection it arrived on and never sees this type at all.</para>
        /// <para>Switching seats therefore means composing again, not re-pointing a live surface -
        /// which is also why no stale callback, selection or continuation can survive a switch.</para>
        /// </remarks>
        public static SeatId? NextComposedSeat { get; set; }

        /// <summary>
        /// ELEVENTH amendment. The printed setup the next composed match plays, or <c>null</c> to
        /// read the launch line - and, failing that, <see cref="MatchFormat.Default"/>.
        /// </summary>
        /// <remarks>
        /// <para>Unlike the two hooks above this is NOT a development-only affordance: a launched
        /// player selects a format with <see cref="GuardsDigital.Network.PrivateSessionLaunchOptions
        /// .MatchFormatFlag"/>, and this property is how a fixture asks for the same thing without a
        /// command line. It is a one-shot in the same way, consumed and reset by the composition, so
        /// a fixture that forgets to clear it cannot change what a later scene load deals.</para>
        /// <para><b>It is read before <see cref="Compose"/> runs.</b> Milestone 8.35z37 is
        /// where that stopped being a wall: <see cref="Recompose"/> composes the scene again from a
        /// <see cref="MatchCompositionPlan"/>, so a format chosen in the lobby is applied by
        /// composing the match when the lobby says to rather than when the scene loads. This
        /// property remains what a fixture uses to ask for a format without a command line.</para>
        /// </remarks>
        public static MatchFormat NextComposedMatchFormat { get; set; }

        /// <summary>
        /// Milestone 8.35z37. The heroes the next composed match fields, or <c>null</c> for
        /// setup step 7's printed line-up - what this build has always dealt.
        /// </summary>
        /// <remarks>
        /// <para>A one-shot on exactly the terms <see cref="NextComposedMatchFormat"/> is on,
        /// consumed and cleared by the composition, so a fixture that forgets to clear it cannot
        /// decide what a later scene load deals. There is no launch flag behind it and deliberately
        /// none: a line-up is chosen by people in a lobby or stated by a fixture, and a command line
        /// that could deal an arbitrary line-up to a shipped player would be a third way to decide
        /// something the lobby now owns.</para>
        /// <para>Defaulting to null rather than to the printed line-up is what keeps every existing
        /// caller byte-identical: <see cref="MatchCompositionPlan"/> resolves null to the printed
        /// line-up, and composing the printed line-up produces the same match the shipped path
        /// produces - which is asserted by comparing two composed sessions' snapshots rather than
        /// argued. See <c>DeferredMatchCompositionTests</c>.</para>
        /// </remarks>
        public static HeroLineup NextComposedLineup { get; set; }

        /// <summary>
        /// TWELFTH amendment. The staged situation the next composed match begins from, or
        /// <c>null</c> to read the launch line - and, failing that, to deal the ordinary match.
        /// </summary>
        /// <remarks>
        /// <para>A one-shot in the same way as the two hooks above, consumed and reset by the
        /// composition, so a fixture that forgets to clear it cannot stage a later scene load.</para>
        /// <para><b>This is a <c>DEV-004</c> development affordance and the surface it opens is the
        /// Director's own.</b> Staging happens strictly before an authority exists, so there is no
        /// moment at which a seat could reach it and nothing for a codec to carry; the launch flag
        /// that feeds it is refused beside every networked role. "Sandbox" here is the staged-board
        /// test environment and has nothing to do with the agent process-isolation sandbox that
        /// <c>Docs/AI/Testing/POLICY.md</c> calls by the same word.</para>
        /// </remarks>
        public static SandboxStagingDescription NextComposedSandboxStaging { get; set; }

        /// <summary>One-shot Director fixture obstacles; never a player command or serialized staging field.</summary>
        public static IReadOnlyList<ObstacleOccupancy> NextComposedStagedObstacles { get; set; }

        /// <summary>
        /// The staging the current runtime was composed from, or <c>null</c> for an ordinary dealt
        /// match. Read by the HUD to draw the Director-only sandbox banner, so the banner states
        /// what was actually composed rather than what a flag asked for.
        /// </summary>
        public SandboxStagingDescription ComposedSandboxStaging { get; private set; }

        /// <summary>
        /// Milestone 8.35z37. The decisions the current runtime was composed from, or
        /// <c>null</c> before anything has been composed.
        /// </summary>
        /// <remarks>
        /// Read by the lobby to know what it is offering to change: the format control shows the
        /// format that is actually composed rather than one a flag asked for, on exactly the rule
        /// <see cref="ComposedSandboxStaging"/> follows for the sandbox banner.
        /// </remarks>
        public MatchCompositionPlan ComposedPlan { get; private set; }

        public ConfiguredMatchSceneRuntime Runtime => runtime;
        public IMatchSession Session => runtime?.Session;
        public ConfiguredMatchHudRenderer Hud => hud;

        /// <summary>
        /// Milestone <c>8.35z54</c>. The board view this root composed, handed out beside
        /// <see cref="Hud"/> for the same reason that one is: a component that needs it must be GIVEN
        /// it rather than go looking. The rehearsal driver reports the board's framing for the fifth
        /// playtest finding's direct negative, and reaching it through
        /// <c>FindAnyObjectByType</c> is exactly the global discovery
        /// <c>Phase4ArchitectureConformanceTests</c> confines to this class - which it caught on the
        /// first run after that reporting was written.
        /// </summary>
        public PlaceholderTestGridView MapView => mapView;
        public bool OwnsGeneratedHighlightMaterial => false;

        /// <summary>
        /// The configured scene owns only the shared corner construction. Legacy destination fill
        /// owners remain nullable for source compatibility and are never allocated by composition.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35z63's violet aura fill is deliberately NOT counted here. This
        /// member reports the BOUNDED SET composition allocates up front - twenty, whatever happens at
        /// the table - and the aura material is allocated on first use and only while an Active effect
        /// with a disk is live, so folding it in would turn a fixed bound into a number that moves with
        /// play. It is owned and audited by <c>BoardInteractionUnityView.OwnedNativeResourceCount</c>
        /// instead, released below beside this template, and its generate-once-and-release lifecycle is
        /// asserted by <c>ActiveEffectBoardTreatmentTests</c>.
        /// </remarks>
        public int OwnedHighlightNativeResourceCount =>
            (cornerHighlightTemplate?.OwnedNativeResourceCount ?? 0);

        public void Compose()
        {
            if (runtime != null)
            {
                return;
            }

            mapView ??= FindAnyObjectByType<PlaceholderTestGridView>();
            pointerInput ??= FindAnyObjectByType<PrimitiveHeroPointerInput>();
            eventCamera ??= Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            // Milestone 8.35z37. A re-composition retires the seat views its predecessor
            // generated, and a retired view stays reachable through a scene search until the end of
            // the frame - the deferred-Destroy trap milestone 6.1e recorded as finding F613-05. So
            // the discovery below excludes them BY IDENTITY rather than trusting the search, which is
            // the same answer 6.1e gave: use what the composition knows, not what the scene returns.
            heroViews = FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view != null && !retiredHeroViews.Contains(view))
                .ToArray();
            pointerSelectors = FindObjectsByType<PrimitiveHeroPointerSelector>()
                .Where(selector => selector != null && !retiredSelectors.Contains(selector))
                .ToArray();

            // Milestone 8.35z37. The hero-view half of this requirement moved BELOW
            // `EnsureViewsForConfiguredSeats`, and a complete Play Mode run is what forced it. A
            // re-composition retires every view its predecessor generated, so on the second pass the
            // discovery above legitimately returns none for any configuration whose seats were all
            // generated rather than authored - and the check then refused, before the step whose whole
            // job is to create them had run. It demanded the output of a later step as a precondition,
            // which only ever held because a FIRST composition happens to inherit the scene's authored
            // views. Infrastructure the scene must supply is checked here; the seats a composition
            // supplies for itself are checked once they exist.
            if (mapView == null || eventCamera == null || pointerInput == null)
            {
                throw new InvalidOperationException(
                    "The configured match composition root requires a map, camera, and pointer input.");
            }

            var catalog = OfficialBoardMapCatalog.Load();
            var map = catalog.Load(OfficialBoardMapCatalog.SelectedMap);
            // Five cards per hero, which is the shape every hand in this project has been specified at
            // since milestone 3.7b. Milestone 6.2d had dealt a sixth here - milestone 6.2's Cache card -
            // because nothing else could put a token on the board, and without one 6.3's Clear and two
            // of 6.6a's three modes were controls with nothing to act on. Blue Sentinel and Blue Feint
            // carry those capabilities now, so the scene keeps them at five.
            var content = TakeRequestedCardContent();
            // ELEVENTH amendment. The format is settled here, before anything is composed, because
            // this is the only moment at which it can be: the runtime below is built once and every
            // later surface reads it. A staged arm ignores it - a staged board's subject is one
            // printed set and its own numbers, not a rulebook setup - so only the configured match
            // consults it.
            var format = TakeRequestedMatchFormat();
            // Milestone 8.35z37. The format and the line-up are one decision from here on,
            // and the plan is what carries it. A caller that chose no line-up gets setup step 7's
            // printed one, so the configured arm below composes exactly the match it always has.
            var plan = new MatchCompositionPlan(format, TakeRequestedLineup());
            ComposedPlan = plan;
            var configuration = content switch
            {
                SceneCardContent.MechanicsShowcaseFixture =>
                    MatchConfiguration.CreateForShowcaseFixture(),
                SceneCardContent.VerifiedEngagedMirror or SceneCardContent.VerifiedMirror =>
                    MatchConfiguration.CreateVerifiedMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedMirror =>
                    MatchConfiguration.CreateVerifiedTierTwoMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierThreeEngagedMirror =>
                    MatchConfiguration.CreateVerifiedTierThreeMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedArien or SceneCardContent.VerifiedStagedArien =>
                    MatchConfiguration.CreateStagedArienForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedArien =>
                    MatchConfiguration.CreateStagedArienTierTwoForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien =>
                    MatchConfiguration.CreateStagedArienTierTwoForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedArien =>
                    MatchConfiguration.CreateStagedArienTierThreeForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedArien =>
                    MatchConfiguration.CreateStagedArienTierThreeForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedBrogan =>
                    MatchConfiguration.CreateStagedBroganForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedBrogan =>
                    MatchConfiguration.CreateStagedBroganTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrogan =>
                    MatchConfiguration.CreateStagedBroganTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedBrogan =>
                    MatchConfiguration.CreateStagedBroganTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrogan =>
                    MatchConfiguration.CreateStagedBroganTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedStagedBroganWithAlly =>
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedStagedBroganWithAllyAtTierTwo =>
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                        TeamSide.Red, BroganAllyStage.TierTwo),
                SceneCardContent.VerifiedStagedBroganWithAllyAtTierThree =>
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                        TeamSide.Red, BroganAllyStage.TierThree),
                SceneCardContent.VerifiedEngagedStagedSabina =>
                    MatchConfiguration.CreateStagedSabinaForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedSabina =>
                    MatchConfiguration.CreateStagedSabinaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SabinaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSabina =>
                    MatchConfiguration.CreateStagedSabinaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SabinaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedSabina =>
                    MatchConfiguration.CreateStagedSabinaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SabinaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSabina =>
                    MatchConfiguration.CreateStagedSabinaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SabinaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedTigerclaw =>
                    MatchConfiguration.CreateStagedTigerclawForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedTigerclaw =>
                    MatchConfiguration.CreateStagedTigerclawTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TigerclawUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw =>
                    MatchConfiguration.CreateStagedTigerclawTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TigerclawUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedTigerclaw =>
                    MatchConfiguration.CreateStagedTigerclawTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TigerclawUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTigerclaw =>
                    MatchConfiguration.CreateStagedTigerclawTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TigerclawUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedXargatha =>
                    MatchConfiguration.CreateStagedXargathaForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedXargatha =>
                    MatchConfiguration.CreateStagedXargathaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.XargathaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedXargatha =>
                    MatchConfiguration.CreateStagedXargathaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.XargathaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedXargatha =>
                    MatchConfiguration.CreateStagedXargathaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.XargathaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedXargatha =>
                    MatchConfiguration.CreateStagedXargathaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.XargathaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedDodger =>
                    MatchConfiguration.CreateStagedDodgerForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedDodger =>
                    MatchConfiguration.CreateStagedDodgerTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.DodgerUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedDodger =>
                    MatchConfiguration.CreateStagedDodgerTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.DodgerUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedDodger =>
                    MatchConfiguration.CreateStagedDodgerTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.DodgerUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedDodger =>
                    MatchConfiguration.CreateStagedDodgerTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.DodgerUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedMisa =>
                    MatchConfiguration.CreateStagedMisaForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedMisa =>
                    MatchConfiguration.CreateStagedMisaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MisaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMisa =>
                    MatchConfiguration.CreateStagedMisaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MisaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedMisa =>
                    MatchConfiguration.CreateStagedMisaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MisaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMisa =>
                    MatchConfiguration.CreateStagedMisaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MisaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedSilverarrow =>
                    MatchConfiguration.CreateStagedSilverarrowForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedSilverarrow =>
                    MatchConfiguration.CreateStagedSilverarrowTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SilverarrowUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSilverarrow =>
                    MatchConfiguration.CreateStagedSilverarrowTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SilverarrowUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedSilverarrow =>
                    MatchConfiguration.CreateStagedSilverarrowTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SilverarrowUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSilverarrow =>
                    MatchConfiguration.CreateStagedSilverarrowTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SilverarrowUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedUrsafar =>
                    MatchConfiguration.CreateStagedUrsafarForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedUrsafar =>
                    MatchConfiguration.CreateStagedUrsafarTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.UrsafarUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedUrsafar =>
                    MatchConfiguration.CreateStagedUrsafarTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.UrsafarUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedUrsafar =>
                    MatchConfiguration.CreateStagedUrsafarTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.UrsafarUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedUrsafar =>
                    MatchConfiguration.CreateStagedUrsafarTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.UrsafarUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedWhisper =>
                    MatchConfiguration.CreateStagedWhisperForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedWhisper =>
                    MatchConfiguration.CreateStagedWhisperTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WhisperUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWhisper =>
                    MatchConfiguration.CreateStagedWhisperTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WhisperUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedWhisper =>
                    MatchConfiguration.CreateStagedWhisperTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WhisperUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWhisper =>
                    MatchConfiguration.CreateStagedWhisperTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WhisperUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedTali =>
                    MatchConfiguration.CreateStagedTaliForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedTali =>
                    MatchConfiguration.CreateStagedTaliTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TaliUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTali =>
                    MatchConfiguration.CreateStagedTaliTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TaliUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedTali =>
                    MatchConfiguration.CreateStagedTaliTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TaliUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTali =>
                    MatchConfiguration.CreateStagedTaliTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.TaliUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedStagedTaliWithAlly =>
                    MatchConfiguration.CreateStagedTaliWithAllyForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedSwift =>
                    MatchConfiguration.CreateStagedSwiftForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedSwift =>
                    MatchConfiguration.CreateStagedSwiftTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SwiftUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSwift =>
                    MatchConfiguration.CreateStagedSwiftTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SwiftUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedSwift =>
                    MatchConfiguration.CreateStagedSwiftTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SwiftUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift =>
                    MatchConfiguration.CreateStagedSwiftTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.SwiftUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedCutter =>
                    MatchConfiguration.CreateStagedCutterForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedCutter =>
                    MatchConfiguration.CreateStagedCutterTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.CutterUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedCutter =>
                    MatchConfiguration.CreateStagedCutterTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.CutterUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedCutter =>
                    MatchConfiguration.CreateStagedCutterTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.CutterUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedCutter =>
                    MatchConfiguration.CreateStagedCutterTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.CutterUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedBain =>
                    MatchConfiguration.CreateStagedBainForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedGarrus =>
                    MatchConfiguration.CreateStagedGarrusForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedWidget =>
                    MatchConfiguration.CreateStagedWidgetForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedWidget =>
                    MatchConfiguration.CreateStagedWidgetTierTwoForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WidgetUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWidget =>
                    MatchConfiguration.CreateStagedWidgetTierTwoForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WidgetUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedWidget =>
                    MatchConfiguration.CreateStagedWidgetTierThreeForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WidgetUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWidget =>
                    MatchConfiguration.CreateStagedWidgetTierThreeForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.WidgetUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierTwoEngagedStagedGarrus =>
                    MatchConfiguration.CreateStagedGarrusTierTwoForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.GarrusUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedGarrus =>
                    MatchConfiguration.CreateStagedGarrusTierTwoForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.GarrusUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedGarrus =>
                    MatchConfiguration.CreateStagedGarrusTierThreeForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.GarrusUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedGarrus =>
                    MatchConfiguration.CreateStagedGarrusTierThreeForInternalTest(TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.GarrusUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierTwoEngagedStagedBain =>
                    MatchConfiguration.CreateStagedBainTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BainUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBain =>
                    MatchConfiguration.CreateStagedBainTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BainUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedBain =>
                    MatchConfiguration.CreateStagedBainTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BainUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBain =>
                    MatchConfiguration.CreateStagedBainTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BainUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedRowenna =>
                    MatchConfiguration.CreateStagedRowennaForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedRowenna =>
                    MatchConfiguration.CreateStagedRowennaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.RowennaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedRowenna =>
                    MatchConfiguration.CreateStagedRowennaTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.RowennaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedRowenna =>
                    MatchConfiguration.CreateStagedRowennaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.RowennaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedRowenna =>
                    MatchConfiguration.CreateStagedRowennaTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.RowennaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedStagedRowennaWithAlly =>
                    MatchConfiguration.CreateStagedRowennaWithAllyForInternalTest(
                        TeamSide.Red, RowennaAllyStage.StartingHand),
                SceneCardContent.VerifiedStagedRowennaWithAllyAtTierTwo =>
                    MatchConfiguration.CreateStagedRowennaWithAllyForInternalTest(
                        TeamSide.Red, RowennaAllyStage.TierTwo),
                SceneCardContent.VerifiedStagedRowennaWithAllyAtTierThree =>
                    MatchConfiguration.CreateStagedRowennaWithAllyForInternalTest(
                        TeamSide.Red, RowennaAllyStage.TierThree),
                SceneCardContent.VerifiedEngagedStagedMin =>
                    MatchConfiguration.CreateStagedMinForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedMin =>
                    MatchConfiguration.CreateStagedMinTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MinUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMin =>
                    MatchConfiguration.CreateStagedMinTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MinUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedMin =>
                    MatchConfiguration.CreateStagedMinTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MinUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMin =>
                    MatchConfiguration.CreateStagedMinTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.MinUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedBrynn =>
                    MatchConfiguration.CreateStagedBrynnForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedBrynn =>
                    MatchConfiguration.CreateStagedBrynnTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BrynnUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrynn =>
                    MatchConfiguration.CreateStagedBrynnTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BrynnUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedBrynn =>
                    MatchConfiguration.CreateStagedBrynnTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BrynnUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrynn =>
                    MatchConfiguration.CreateStagedBrynnTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BrynnUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedHanu =>
                    MatchConfiguration.CreateStagedHanuForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedHanu =>
                    MatchConfiguration.CreateStagedHanuTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.HanuUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedHanu =>
                    MatchConfiguration.CreateStagedHanuTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.HanuUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedHanu =>
                    MatchConfiguration.CreateStagedHanuTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.HanuUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedHanu =>
                    MatchConfiguration.CreateStagedHanuTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.HanuUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedStagedHanuWithAlly =>
                    MatchConfiguration.CreateStagedHanuWithAllyForInternalTest(
                        TeamSide.Red, HanuAllyStage.StartingHand),
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo =>
                    MatchConfiguration.CreateStagedHanuWithAllyForInternalTest(
                        TeamSide.Red, HanuAllyStage.TierTwo),
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree =>
                    MatchConfiguration.CreateStagedHanuWithAllyForInternalTest(
                        TeamSide.Red, HanuAllyStage.TierThree),
                // Milestone 8.75b. Ignatia's eight. The `TeamSide` argument is the Tie Breaker coin's
                // revealed face, and for her it decides WHICH PRINTED TEXT her cards run - so the
                // first two members are one hand on one board under the two faces, and every arm
                // below them stages the sun. 8.75c's journeys flip the coin from either.
                SceneCardContent.VerifiedEngagedStagedIgnatia =>
                    MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Blue),
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon =>
                    MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedIgnatia =>
                    MatchConfiguration.CreateStagedIgnatiaTierTwoForInternalTest(
                        TeamSide.Blue,
                        GuardsDigital.Core.Cards.Verified.IgnatiaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedIgnatia =>
                    MatchConfiguration.CreateStagedIgnatiaTierTwoForInternalTest(
                        TeamSide.Blue,
                        GuardsDigital.Core.Cards.Verified.IgnatiaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedIgnatia =>
                    MatchConfiguration.CreateStagedIgnatiaTierThreeForInternalTest(
                        TeamSide.Blue,
                        GuardsDigital.Core.Cards.Verified.IgnatiaUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedIgnatia =>
                    MatchConfiguration.CreateStagedIgnatiaTierThreeForInternalTest(
                        TeamSide.Blue,
                        GuardsDigital.Core.Cards.Verified.IgnatiaUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedStagedIgnatiaWithAlly =>
                    MatchConfiguration.CreateStagedIgnatiaWithAllyForInternalTest(
                        TeamSide.Blue, IgnatiaAllyStage.StartingHand),
                SceneCardContent.VerifiedStagedIgnatiaWithAllyAtTierTwo =>
                    MatchConfiguration.CreateStagedIgnatiaWithAllyForInternalTest(
                        TeamSide.Blue, IgnatiaAllyStage.TierTwo),
                SceneCardContent.VerifiedStagedIgnatiaWithAllyAtTierThree =>
                    MatchConfiguration.CreateStagedIgnatiaWithAllyForInternalTest(
                        TeamSide.Blue, IgnatiaAllyStage.TierThree),
                SceneCardContent.VerifiedEngagedStagedTrinkets =>
                    MatchConfiguration.CreateStagedTrinketsForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedTierThreeEngagedStagedTrinkets =>
                    MatchConfiguration.CreateStagedTrinketsTierThreeForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedSnorri =>
                    MatchConfiguration.CreateStagedSnorriForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedTierThreeEngagedStagedSnorri =>
                    MatchConfiguration.CreateStagedSnorriTierThreeForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedStagedSnorriWithAlly =>
                    MatchConfiguration.CreateStagedSnorriWithAllyForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedStagedSnorriVersusTwo =>
                    MatchConfiguration.CreateStagedSnorriAgainstWaspWithAllyForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedMrak =>
                    MatchConfiguration.CreateStagedMrakForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedTierThreeEngagedStagedMrak =>
                    MatchConfiguration.CreateStagedMrakTierThreeForInternalTest(TeamSide.Red),
                SceneCardContent.VerifiedStagedMrakVersusTwo =>
                    MatchConfiguration.CreateStagedMrakAgainstWaspWithAllyForInternalTest(TeamSide.Red),
                // SceneCardContent.ConfiguredMatch, which is every arm not named above. Milestone 8.25:
                // it deals rulebook p.5's six official seats, which is what a player loads.
                //
                // Milestone 8.35z37 routes it through the plan's line-up. With no line-up
                // chosen that is the printed one, and composing the printed line-up produces the
                // same match `MatchConfiguration.Create(format)` produces - proven by comparing two
                // composed sessions' snapshot payloads on all four printed formats and under both
                // coins, in DeferredMatchCompositionTests. There is therefore ONE path here rather
                // than a chosen path beside a shipped one, which is the point: a seam a shipped
                // match does not travel through is a seam nothing tests.
                _ => MatchConfiguration.CreateForLineup(format, plan.Lineup)
            };
            // Milestone 8.20. The board's hero identities have to agree with the hands the
            // configuration above dealt: a seat's content identity is what addresses its prepared token
            // and what the projection names it by, so a mirror board under an Arien hand would draw
            // Wasp's face on a hero holding Arien's cards. Each staged arm therefore takes the board its
            // own hands belong on.
            //
            // Milestone 8.25: the default arm - what a player plays - is now rulebook p.5's official
            // six-player board, and it is the only arm that places six heroes.
            var setup = QuickGameSetupAuthority.Create(
                SelectSetupConfiguration(content, plan),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            // Milestone 8.20 adds the staged Arien arms to the same restaging. The staging is about
            // where the two heroes stand and what they stand beside, and it names neither of them - so
            // one hero on each side is the same engagement whichever printed sets they are holding.
            if (content == SceneCardContent.VerifiedEngagedMirror ||
                content == SceneCardContent.VerifiedTierTwoEngagedMirror ||
                content == SceneCardContent.VerifiedTierThreeEngagedMirror ||
                content == SceneCardContent.VerifiedEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierTwoEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierThreeEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedArien ||
                IsStagedBrogan(content) ||
                IsStagedSabina(content) ||
                IsStagedTigerclaw(content) ||
                IsStagedXargatha(content) ||
                IsStagedDodger(content) ||
                IsStagedMisa(content) ||
                IsStagedSilverarrow(content) ||
                IsStagedUrsafar(content) ||
                IsStagedWhisper(content) ||
                IsStagedTali(content) ||
                IsStagedSwift(content) ||
                // Milestone 8.39i. Written in the SAME change as her five members above, which is
                // what milestone 8.38f's repair of this list teaches: a staged arm missing from here
                // composes its two heroes fifteen spaces apart, every approach helper throws
                // `Sequence contains no elements`, and no offline tier can see it.
                IsStagedCutter(content) ||
                // Milestone 8.40j. Written in the SAME change as his five members above, on the same
                // terms: a staged arm missing from here composes its two heroes fifteen spaces apart,
                // every approach helper throws `Sequence contains no elements`, and no offline tier
                // can see it.
                IsStagedBain(content) || IsStagedGarrus(content) || IsStagedWidget(content) ||
                // Milestone 8.57g. Written in the SAME change as her five engaged members above, on
                // the same terms: a staged arm missing from here composes its two heroes fifteen
                // spaces apart, every approach helper throws `Sequence contains no elements`, and no
                // offline tier can see it. Her three ALLY arms are deliberately NOT here, exactly as
                // Brogan's and Tali's are not - a restaging moves two seats and those boards carry
                // three.
                IsStagedRowenna(content) ||
                // Milestone 8.61j. Written in the SAME change as her five engaged members above, on
                // the same terms. She has no ally arm at all, so all five of hers are here.
                IsStagedMin(content) ||
                // Milestone 8.69g. Written in the SAME change as his five engaged members above, on
                // the same terms. He has no ally arm at all either - p.17's Friendly covers the melee
                // minion the restaging already leaves in his ring - so all five of his are here.
                IsStagedBrynn(content) ||
                // Milestone 8.71h. Written in the SAME change as his five engaged members above, on
                // the same terms. His THREE ALLY arms are deliberately absent from this list, exactly
                // as Rowenna's three are: what This Way! and That Way! need is a friendly hero in
                // RADIUS, and the two Blue spawns already are - restaging them would move the ally out
                // of the disk the co-moves measure.
                IsStagedHanu(content) ||
                // Milestone 8.75b. Written in the SAME change as her six engaged members above, on
                // the same terms. Her THREE ALLY arms are deliberately absent from this list, exactly
                // as Rowenna's and Hanu's are: what Searing Heat's and Scorching Blaze's sun faces
                // need is a friendly hero in RADIUS, and the two Blue spawns already are - restaging
                // them would move the ally out of the disk those pushes measure.
                IsStagedIgnatia(content) ||
                content == SceneCardContent.VerifiedEngagedStagedTrinkets ||
                content == SceneCardContent.VerifiedTierThreeEngagedStagedTrinkets ||
                content == SceneCardContent.VerifiedEngagedStagedSnorri ||
                content == SceneCardContent.VerifiedTierThreeEngagedStagedSnorri ||
                content == SceneCardContent.VerifiedEngagedStagedMrak ||
                content == SceneCardContent.VerifiedTierThreeEngagedStagedMrak)
            {
                setup = EngagedMirrorStaging.Restage(map, setup, out _);
            }

            // TWELFTH amendment, the sandbox's second half. The staging slots in exactly here,
            // beside the mirror restaging above, because both answer the same question - what board
            // does this scene begin from - and both answer it before any view is generated or any
            // authority exists. From here the staged setup IS the setup: the views below, the board
            // rebuild and the runtime all read it without knowing a staged board from a dealt one,
            // which is what makes Undo, restart, the event log and replay work unchanged.
            //
            // The composed initial state is carried to the runtime rather than recomposed there,
            // because staging mutates the configuration in place (a staged hand is a real card
            // moved by CardLifecycleAuthority) and composing it twice would deal the hands twice.
            MatchSessionInitialState stagedInitialState = null;
            ComposedSandboxStaging = TakeRequestedSandboxStaging();
            var stagedObstacles = NextComposedStagedObstacles;
            NextComposedStagedObstacles = null;
            if (stagedObstacles != null && ComposedSandboxStaging == null)
                throw new InvalidOperationException(
                    "Staged obstacles require a Director sandbox composition.");
            if (ComposedSandboxStaging != null)
            {
                stagedInitialState = SandboxMatchComposition.Compose(
                    ComposedSandboxStaging, map, setup, configuration, new[] { map },
                    stagedObstacles);
                setup = stagedInitialState.Setup;
            }
            // Milestone 8.21. A configuration may deal a seat the scene authors no view for - the
            // staged Brogan-with-an-ally match deals three - so any such seat gets a generated view
            // and a generated pointer selector before the board is rebuilt. It is the same answer the
            // board already gives a placed token: authored content where there is some, generated
            // where a configuration asks for more, and no fixture reaching into the scene to add one.
            EnsureViewsForConfiguredSeats(setup);

            // Milestone 8.35z37. The other half of the requirement above, asked where it is actually
            // an invariant: every configured seat now has a piece, whether the scene authored it or
            // this composition generated it. A composition that reaches here with no hero view has
            // dealt no seat, which is a broken configuration rather than a scene the fixture forgot
            // to author.
            if (heroViews.Length == 0)
            {
                throw new InvalidOperationException(
                    "The configured match composition root requires at least one hero view; the " +
                    "composed configuration dealt no seat for one to be generated from.");
            }

            mapView.ConfiguredMinionViewsChanged -= RebindConfiguredMinionViews;
            mapView.ConfiguredMinionViewsChanged += RebindConfiguredMinionViews;
            mapView.ConfigureSceneBindings(eventCamera, heroViews);
            mapView.Rebuild(map, setup);

            // Use the sets the rebuild published rather than re-discovering children. In Play Mode a
            // retired view is destroyed with the deferred Destroy and remains reachable through a
            // child search until the end of the frame, so re-discovery admits doomed objects into
            // authoritative board state. See milestone 6.1e / finding F613-05.
            var tiles = mapView.CurrentTiles.ToArray();
            var minions = mapView.CurrentMinionViews.ToArray();
            var obstacles = mapView.CurrentObstacles.ToArray();
            cornerHighlightTemplate = new HexCornerHighlightTemplate(mapView.OuterRadius);
            hud = GetComponent<ConfiguredMatchHudRenderer>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<ConfiguredMatchHudRenderer>();
            }
            hud.PlayfieldLayoutChanged -= mapView.ConfigureCameraViewport;
            hud.PlayfieldLayoutChanged += mapView.ConfigureCameraViewport;

            // 8.35z54, the fifth playtest finding ("map sizing broke a bit somehow on
            // player side"). SUBSCRIBING IS NOT THE SAME AS BEING TOLD, and the difference only
            // shows on a SECOND composition.
            //
            // `PlayfieldLayoutChanged` fires when the HUD RECOMPUTES its layout, and the HUD is one
            // of the components a re-composition deliberately keeps (`GetComponent ?? AddComponent`,
            // then rebind). On a first composition the order is kind: the board is built at Awake
            // and the HUD lays out on its first frame, so the event arrives and sizes the board. On
            // a RE-composition the HUD has already laid out and has no reason to lay out again, so
            // the freshly rebound board would wait for an event that never comes and keep the
            // viewport it was constructed with.
            //
            // Only a client reaches this: a joiner recomposes at the transition to field the chosen
            // line-up, which is exactly the "player side" the finding names, while a host that
            // changes format recomposes from the entry form where no layout has been published yet
            // and is therefore unaffected. Pushing the layout the HUD is ALREADY holding closes it -
            // the same value the next event would carry, delivered now instead of never - and the
            // validity guard is what keeps a composition that genuinely precedes any layout on the
            // event it is still correctly waiting for.
            if (hud.CameraSafeLayout.IsValid)
            {
                mapView.ConfigureCameraViewport(hud.CameraSafeLayout);
            }

            runtime = new ConfiguredMatchSceneRuntime(
                map,
                setup,
                configuration,
                catalog.Definitions,
                heroViews,
                minions,
                tiles,
                obstacles,
                null,
                hud,
                null,
                // Milestone 6.2d: where a view for a newly placed token comes from. The map view owns
                // the outer radius and keeps every generated board object, so it builds them for the
                // same reason it builds tiles and minions rather than the board view doing it.
                mapView.CreateTokenView,
                // Retired fill/rim arguments stay null; every configured board choice uses corners.
                null,
                null,
                // Milestone 8.30b: the development-only seat selection, consumed once.
                TakeRequestedSeat(),
                cornerHighlightTemplate,
                // TWELFTH amendment: the staged initial state, or null for the dealt match the
                // runtime composes for itself exactly as it always has.
                stagedInitialState);

            // Milestone 8.35y. The configured match scene has never had a frame loop - every
            // surface redraws on a projection change - so the kinetic pass brings its own, on this
            // composition root's own object, and it is the only per-frame work in the scene. It
            // isolates its tick and disables itself after a small budget of failures, so the board
            // falls back to exactly the projection-change redraw it did before this milestone.
            Match.KineticCueDriver.Attach(gameObject, runtime.KineticView);

            // Milestone 8.35z12. The spotlight drives the camera every frame while it is up
            // (see AttackSpotlightUnityView's own remarks for why it reasserts rather than owns), so
            // it brings its own isolated, self-disabling driver on the same terms as the two beside
            // it. The camera and the board's own reframing are handed over here rather than guessed
            // at: this root already resolved both.
            runtime.SpotlightView.CameraSource = () => eventCamera;
            runtime.SpotlightView.BoardFramingRestored = mapView.ReapplyCameraFrame;
            Match.AttackSpotlightDriver.Attach(
                gameObject, runtime.SpotlightPresenter, runtime.SpotlightView);

            // Milestone 8.35z6. Defeat's shrink-and-fade needs its own per-frame tick for the
            // same reason the kinetic pass above does, and it stays a SEPARATE driver rather than one
            // more thing threaded through KineticCueDriver: the board's own fade ghosts never reach into
            // the kinetic layer and the kinetic layer never reaches into the board's pool to deliver
            // them. Isolated and self-disabling on the same terms as the driver above.
            Match.BoardDefeatFadeDriver.Attach(gameObject, runtime.BoardView);

            foreach (var selector in pointerSelectors.Where(value => value != null))
            {
                selector.Configure(runtime.BoardView);
            }
            var minionSelectors = ConfigureMinionSelectors(minions);

            pointerInput.Configure(
                eventCamera,
                pointerSelectors,
                minionSelectors,
                runtime.BoardView,
                tiles,
                hud.BlocksPointerAt);

            // TWELFTH amendment. The Director-only banner, announced last so it is drawn over a HUD
            // the runtime above has already configured. It is told what the staging DID rather than
            // what it was, and the summary is derived from the composed staging itself, so the
            // banner cannot describe a board other than the one on screen.
            if (ComposedSandboxStaging != null)
            {
                hud.ShowSandboxStaging(
                    SandboxStagingSummary.Describe(ComposedSandboxStaging));
            }
        }

        private void RebindConfiguredMinionViews(IReadOnlyList<StandardMinionView> minionViews)
        {
            if (runtime == null)
            {
                return;
            }

            var currentViews = (minionViews ?? Array.Empty<StandardMinionView>())
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            runtime.BoardView.ReplaceMinionViews(currentViews);
            pointerInput.ReplaceMinionSelectors(ConfigureMinionSelectors(currentViews));
        }

        private StandardMinionPointerSelector[] ConfigureMinionSelectors(
            IEnumerable<StandardMinionView> minionViews)
        {
            var currentSelectors = (minionViews ?? Array.Empty<StandardMinionView>())
                .Where(value => value != null)
                // Same `??` trap HexTileView.SetActiveEffectAura records at 8.35z63: `??` bypasses
                // Unity's own null overload, so a missing component's marshalled wrapper survives it
                // and nothing is added. Corrected for consistency rather than on an observed throw.
                .Select(view =>
                {
                    var selector = view.GetComponent<StandardMinionPointerSelector>();
                    return selector == null
                        ? view.gameObject.AddComponent<StandardMinionPointerSelector>()
                        : selector;
                })
                .ToArray();
            foreach (var selector in currentSelectors)
            {
                selector.Configure(runtime.BoardView);
            }

            return currentSelectors;
        }

        /// <summary>
        /// Reads <see cref="NextComposedCardContent"/> and puts it back to the configured match, so the
        /// request applies to exactly the composition that consumed it.
        /// </summary>
        /// <summary>
        /// Milestone 8.21. Gives every configured seat a hero view and a pointer selector, generating
        /// one for any seat the scene does not author.
        ///
        /// <para>The scene authors one view per team, which is what every configuration before this
        /// milestone dealt. The staged match whose subject is a card naming a friendly hero deals a
        /// second Blue seat, and that seat needs the same two things every other has: a piece on the
        /// board and something a pointer ray can hit, or the journeys that prove those cards would be
        /// driving a hero nobody can click.</para>
        ///
        /// <para>The generated object carries no artwork of its own. <see cref="PrimitiveHeroView"/>
        /// builds its own mesh and materials from the team and the prepared token, exactly as an
        /// authored one does, so a generated seat and an authored seat are the same piece drawn the
        /// same way.</para>
        /// </summary>
        private void EnsureViewsForConfiguredSeats(QuickGameSetupState setup)
        {
            var generated = new List<PrimitiveHeroView>();
            var selectors = new List<PrimitiveHeroPointerSelector>();
            // Milestone 8.35z37. Remembered so a re-composition can retire exactly what a
            // previous one generated, and nothing the scene itself authored.
            var owned = generatedHeroViews;
            foreach (var seat in setup.Heroes)
            {
                if (heroViews.Any(view => view != null && view.Occupancy.HeroId.Equals(seat.HeroId)))
                {
                    continue;
                }

                var host = new GameObject($"generated-seat ({seat.HeroId.Value})");
                host.transform.SetParent(mapView.transform, false);
                var view = host.AddComponent<PrimitiveHeroView>();
                // The identity has to be set before the rebuild, because that is what the board
                // matches a configured seat to its view by - and it is set at the map's own outer
                // radius rather than at zero, because configuring a view lays it out immediately and
                // `HexWorldLayout.ToWorld` refuses a radius that is not a finite positive value. The
                // rebuild below configures it again at the same number, so this is the seat's real
                // geometry rather than a placeholder the rebuild has to correct.
                view.Configure(
                    new HeroOccupancy(seat.HeroId, seat.Coordinate), mapView.OuterRadius);
                generated.Add(view);
                selectors.Add(host.AddComponent<PrimitiveHeroPointerSelector>());
            }

            if (generated.Count == 0)
            {
                return;
            }

            owned.AddRange(generated);
            generatedSelectors.AddRange(selectors);
            heroViews = heroViews.Concat(generated).ToArray();
            pointerSelectors = (pointerSelectors ?? Array.Empty<PrimitiveHeroPointerSelector>())
                .Concat(selectors)
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.35z37. Composes this scene again, from decisions a lobby made.
        /// </summary>
        /// <remarks>
        /// <para><b>Why a scene composes twice at all, stated exactly, because it is the finding
        /// this milestone turns on.</b> A private session cannot admit anybody before a match exists:
        /// <c>PrivateSessionIdentity.ForComposition</c> derives the whole admission tuple - the
        /// format element, the map reference and the content digest - from a COMPOSED configuration
        /// and setup, and a peer whose tuple does not match is refused at the handshake. So "boot the
        /// scene with no match and compose when the lobby says to" is not reachable in one step: the
        /// lobby would have nothing to admit anyone with. What is reachable, and is what this does,
        /// is to compose a provisional match so the lobby can run, and then compose the REAL one from
        /// the lobby's decisions at the one-way transition out of it.</para>
        ///
        /// <para><b>That is only safe because the provisional match is not a guess.</b> With nothing
        /// chosen, the plan's line-up is setup step 7's printed one, and a match composed from it is
        /// byte-identical to the match this build has always dealt - measured, not asserted, in
        /// <c>DeferredMatchCompositionTests</c>. The provisional match is therefore the shipped match,
        /// and a session that recomposes to the same plan changes nothing at all.</para>
        ///
        /// <para><b>What a re-composition must release, and why so little.</b> The three per-frame
        /// drivers all attach idempotently (<c>GetComponent ?? AddComponent</c>, then rebind), and
        /// both event hookups in <see cref="Compose"/> already unsubscribe before they subscribe. So
        /// the only things that genuinely leak across a second composition are the corner-highlight
        /// template, which owns native resources, and the seat views a previous composition
        /// generated - and the latter are retired by identity rather than by a scene search, for the
        /// deferred-Destroy reason <see cref="Compose"/> records.</para>
        /// </remarks>
        public void Recompose(MatchCompositionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            ReleaseComposition();
            NextComposedMatchFormat = plan.Format;
            NextComposedLineup = plan.IsPrintedLineup ? null : plan.Lineup;
            Compose();
        }

        /// <summary>
        /// Milestone 8.35z37. The session's seam: composes the match this line-up plays and
        /// hands back its host.
        /// </summary>
        /// <remarks>
        /// The format is NOT a parameter, and that is the seam's own rule rather than an omission. A
        /// session's seats are counted from the format, and every live connection, epoch and
        /// reconnect capability the host holds is keyed by seat - so a format changed after anybody
        /// is seated would move the seats out from under them. The format is settled before hosting
        /// begins (see <c>PrivateLobbyDriver</c>'s format control, which is offered on the entry form
        /// and nowhere else), and only the line-up crosses this seam.
        /// </remarks>
        public IMatchHost ComposeForLineup(HeroLineup lineup)
        {
            if (lineup == null) throw new ArgumentNullException(nameof(lineup));

            var current = ComposedPlan;
            if (current == null)
            {
                return null;
            }

            Recompose(current.WithLineup(lineup));
            return runtime?.Host;
        }

        /// <summary>
        /// Milestone 8.35z37. Drops what a composition owns so the next one starts clean.
        /// </summary>
        private void ReleaseComposition()
        {
            foreach (var view in generatedHeroViews)
            {
                if (view != null)
                {
                    retiredHeroViews.Add(view);
                    Destroy(view.gameObject);
                }
            }

            foreach (var selector in generatedSelectors)
            {
                if (selector != null)
                {
                    retiredSelectors.Add(selector);
                }
            }

            generatedHeroViews.Clear();
            generatedSelectors.Clear();
            // Milestone 8.35z63: the board view generates one violet aura fill on first use,
            // released here beside the shared corner construction for the same reason.
            runtime?.BoardView?.ReleaseOwnedResources();
            cornerHighlightTemplate?.Dispose();
            cornerHighlightTemplate = null;
            runtime = null;
        }

        /// <summary>
        /// The board each composed content belongs on.
        ///
        /// <para>Milestone 8.25. The default arm - <see cref="SceneCardContent.ConfiguredMatch"/>, what
        /// a player and the built player load - is rulebook p.5's official six-player configuration.
        /// Every other arm names a staged board, because its subject is one printed set rather than a
        /// table: the mirror for the fixtures that measure a seat against its own reflection, the
        /// one-hero-per-team stagings for the five heroes whose journeys were written on them, and the
        /// two-Blue-seat board for the two cards that name a friendly hero.</para>
        /// </summary>
        /// <remarks>
        /// Milestone 8.35z37: the configured arm composes from the PLAN's line-up rather than
        /// from the format alone. Every other arm is untouched, and that is the whole reason this
        /// method takes a plan rather than a line-up - a staged board's subject is one printed set
        /// and its own numbers, so a line-up means nothing to it and it must not be offered one.
        /// </remarks>
        private static QuickGameSetupConfiguration SelectSetupConfiguration(
            SceneCardContent content, MatchCompositionPlan plan)
        {
            switch (content)
            {
                case SceneCardContent.VerifiedEngagedMirror:
                case SceneCardContent.VerifiedTierTwoEngagedMirror:
                case SceneCardContent.VerifiedTierThreeEngagedMirror:
                case SceneCardContent.VerifiedMirror:
                case SceneCardContent.MechanicsShowcaseFixture:
                    return QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
                case SceneCardContent.ConfiguredMatch:
                    return HeroLineupComposition.Compose(plan.Format, plan.Lineup);
                default:
                    if (content == SceneCardContent.VerifiedStagedTaliWithAlly)
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusTaliWithAllyOneVsTwo();
                    }

                    if (IsBroganWithAlly(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusArienAndBroganOneVsTwo();
                    }

                    if (IsStagedBrogan(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusBroganOneVsOne();
                    }

                    if (IsStagedSabina(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusSabinaOneVsOne();
                    }

                    if (IsStagedTigerclaw(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusTigerclawOneVsOne();
                    }

                    if (IsStagedXargatha(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusXargathaOneVsOne();
                    }

                    if (IsStagedDodger(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusDodgerOneVsOne();
                    }

                    if (IsStagedWhisper(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusWhisperOneVsOne();
                    }

                    if (IsStagedMisa(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusMisaOneVsOne();
                    }

                    if (IsStagedSilverarrow(content))
                    {
                        return QuickGameSetupConfiguration
                            .CreateStagedWaspVersusSilverarrowOneVsOne();
                    }

                    if (IsStagedUrsafar(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusUrsafarOneVsOne();
                    }

                    if (IsStagedWhisper(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusWhisperOneVsOne();
                    }

                    if (IsStagedTali(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusTaliOneVsOne();
                    }

                    if (IsStagedSwift(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusSwiftOneVsOne();
                    }

                    if (IsStagedCutter(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusCutterOneVsOne();
                    }

                    if (IsStagedBain(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusBainOneVsOne();
                    }
                    if (IsStagedGarrus(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusGarrusOneVsOne();
                    }
                    if (IsStagedWidget(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusWidgetOneVsOne();
                    }

                    // Milestone 8.57g. The ALLY arms are asked first, because they are a different
                    // BOARD rather than a different hand: a three-seat configuration composed onto
                    // the one-versus-one setup would leave Arien's cards with no seat to stand on.
                    if (IsRowennaWithAlly(content))
                    {
                        return QuickGameSetupConfiguration
                            .CreateStagedWaspVersusRowennaWithAllyOneVsTwo();
                    }

                    if (IsStagedRowenna(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusRowennaOneVsOne();
                    }

                    // Milestone 8.61j. The nineteenth hero's arm, written in the SAME change as her
                    // five members and her restaging line - the three arms milestone 8.38f had to
                    // add for Swift after the fact, and which every dealing since has shipped
                    // together.
                    if (IsStagedMin(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusMinOneVsOne();
                    }

                    // Milestone 8.69g. The twentieth hero's arm, written in the SAME change as his
                    // five members and his restaging line - the three arms milestone 8.38f had to add
                    // for Swift after the fact, and which every dealing since has shipped together.
                    if (IsStagedBrynn(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusBrynnOneVsOne();
                    }

                    // Milestone 8.71h. The ALLY arms are asked first, for the reason 8.57g's line
                    // above gives: they are a different BOARD rather than a different hand, and a
                    // three-seat configuration composed onto the one-versus-one setup would leave
                    // Arien's cards with no seat to stand on.
                    if (IsHanuWithAlly(content))
                    {
                        return QuickGameSetupConfiguration
                            .CreateStagedWaspVersusHanuWithAllyOneVsTwo();
                    }

                    // The twenty-first hero's arm, written in the SAME change as his eight members
                    // and his restaging line - the three arms milestone 8.38f had to add for Swift
                    // after the fact, and which every dealing since has shipped together.
                    if (IsStagedHanu(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusHanuOneVsOne();
                    }

                    // Milestone 8.75b. The ALLY arms are asked first, for the reason 8.57g's and
                    // 8.71h's lines above give: they are a different BOARD rather than a different
                    // hand, and a three-seat configuration composed onto the one-versus-one setup
                    // would leave Arien's cards with no seat to stand on.
                    if (IsIgnatiaWithAlly(content))
                    {
                        return QuickGameSetupConfiguration
                            .CreateStagedWaspVersusIgnatiaWithAllyOneVsTwo();
                    }

                    // The twenty-second hero's arm, written in the SAME change as her nine members
                    // and her restaging line - the three arms milestone 8.38f had to add for Swift
                    // after the fact, and which every dealing since has shipped together.
                    if (IsStagedIgnatia(content))
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusIgnatiaOneVsOne();
                    }

                    if (content == SceneCardContent.VerifiedEngagedStagedTrinkets ||
                        content == SceneCardContent.VerifiedTierThreeEngagedStagedTrinkets)
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusTrinketsOneVsOne();
                    }

                    if (content == SceneCardContent.VerifiedEngagedStagedSnorri ||
                        content == SceneCardContent.VerifiedTierThreeEngagedStagedSnorri)
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusSnorriOneVsOne();
                    }

                    // Not restaged, like Hanu's ally arms: the Blue spawns already put Arien in
                    // Snorri's Ancestral Radius.
                    if (content == SceneCardContent.VerifiedStagedSnorriWithAlly)
                    {
                        return QuickGameSetupConfiguration
                            .CreateStagedWaspVersusSnorriWithAllyOneVsTwo();
                    }

                    if (content == SceneCardContent.VerifiedStagedSnorriVersusTwo)
                    {
                        return QuickGameSetupConfiguration.CreateStagedSnorriVersusWaspAndArien();
                    }

                    // Milestone 8.112g. Mrak's staged boards, written in the same change as his
                    // SceneCardContent members (8.38f's lesson: without an arm the scene deals his cards
                    // to an `arien`-identity seat).
                    if (content == SceneCardContent.VerifiedEngagedStagedMrak ||
                        content == SceneCardContent.VerifiedTierThreeEngagedStagedMrak)
                    {
                        return QuickGameSetupConfiguration.CreateStagedWaspVersusMrakOneVsOne();
                    }

                    if (content == SceneCardContent.VerifiedStagedMrakVersusTwo)
                    {
                        return QuickGameSetupConfiguration.CreateStagedMrakVersusWaspAndArien();
                    }

                    return QuickGameSetupConfiguration.CreateStagedWaspVersusArienOneVsOne();
            }
        }

        /// <summary>
        /// Whether this content deals Brogan on the Blue seat. Milestone 8.21, and it decides two
        /// things that have to agree: which hero identity the board binds - the content identity is
        /// what addresses the prepared token and what the projection names the piece by - and that the
        /// two heroes are restaged already engaged, which every staged arm is.
        /// </summary>
        private static bool IsStagedBrogan(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrogan;

        /// <summary>
        /// Whether this content puts Sabina on the Blue seat. Milestone 8.22's five arms, which take
        /// the one-hero-per-team setup and the ordinary engaged restaging - she needs no ally seat,
        /// because what her Blue and Green kit commands is minions and the board has twelve.
        /// </summary>
        private static bool IsStagedSabina(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedSabina ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedSabina ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSabina ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedSabina ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSabina;

        /// <summary>
        /// Whether this content puts Tigerclaw on the Blue seat. Milestone 8.23's five arms, which
        /// take the one-hero-per-team setup and the ordinary engaged restaging for the same reason
        /// Sabina's do: nothing his printed set names needs a friendly hero beside him.
        /// </summary>
        private static bool IsStagedTigerclaw(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedTigerclaw ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedTigerclaw ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedTigerclaw ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTigerclaw;

        /// <summary>
        /// Whether this content puts Xargatha on the Blue seat. Milestone 8.24's five arms, on the
        /// one-hero-per-team setup and the ordinary engaged restaging: every clause of hers that names
        /// another unit names an enemy one, so a friendly hero would widen no candidate set she has.
        /// </summary>
        private static bool IsStagedXargatha(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedXargatha ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedXargatha ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedXargatha ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedXargatha ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedXargatha;

        /// <summary>
        /// Whether this content puts Dodger the Warlock on the Blue seat. Milestone 8.35z34's five
        /// arms, on the one-hero-per-team setup and the ordinary engaged restaging: every clause of
        /// hers that names another hero names an enemy one, so a friendly hero would widen no candidate
        /// set she has - and the one thing her kit names on her own side, a friendly minion, is what
        /// p.8's waves already put on the board.
        /// </summary>
        private static bool IsStagedDodger(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedDodger ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedDodger ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedDodger ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedDodger ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedDodger;

        /// <summary>
        /// Whether this content puts Misa the Samurai on the Blue seat. Milestone 8.35z38's five arms,
        /// on the one-hero-per-team setup and the ordinary engaged restaging: no face of hers names a
        /// friendly hero at all, so an ally seat would widen nothing - what her set needs is enemy
        /// units at particular distances, which the engaged position and p.8's waves supply.
        /// </summary>
        private static bool IsStagedMisa(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedMisa ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedMisa ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMisa ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedMisa ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMisa;

        /// <summary>
        /// Whether this content puts Silverarrow the Pathfinder on the Blue seat. Milestone
        /// 8.35z46's five arms, on the one-hero-per-team setup and the ordinary engaged
        /// restaging - and for her the restaging is not a convenience but the only thing that puts an
        /// enemy inside a printed Range at all, because RULING 1 makes six of her seven Attacks reach
        /// exactly their printed number and nothing nearer.
        /// </summary>
        private static bool IsStagedSilverarrow(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedSilverarrow ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedSilverarrow ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSilverarrow ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedSilverarrow ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSilverarrow;
        /// Whether this content puts Ursafar the Savage on the Blue seat. Milestone
        /// 8.35z50's five arms, on the one-hero-per-team setup and the ordinary engaged
        /// restaging: no face of his names a friendly hero at all, so an ally seat would widen nothing -
        /// what his set needs is a target within reach and enemy minions in radius, which the engaged
        /// position and p.8's waves supply.
        /// </summary>
        private static bool IsStagedUrsafar(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedUrsafar ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedUrsafar ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedUrsafar ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedUrsafar ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedUrsafar;

        /// <summary>
        /// Whether this content puts Whisper the Outcast on the Blue seat. Milestone
        /// <c>8.35z53</c>'s five arms, on the one-hero-per-team setup and the ordinary
        /// engaged restaging: no face of hers names a friendly hero, so an ally seat would widen
        /// nothing - what her set needs is an enemy hero in CONTACT and, for five of her faces, one who
        /// has spent a card, and only the first of those is something a staging can arrange.
        /// </summary>
        private static bool IsStagedWhisper(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedWhisper ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedWhisper ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWhisper ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedWhisper ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWhisper;

        /// <summary>
        /// Whether this content deals Tali on the Blue seat, restaged already engaged. Milestone
        /// 8.37's five arms. Does NOT include
        /// <see cref="SceneCardContent.VerifiedStagedTaliWithAlly"/>, which - like
        /// <see cref="IsBroganWithAlly"/>'s own arms - is deliberately not restaged.
        /// </summary>
        /// <summary>
        /// Whether this content is one of milestone 8.38e.s five staged Swift scenes. Milestone 8.38f
        /// added this arm: without it all five fell through to Arien.s setup, which put Arien.s
        /// content identity on the Blue seat and dealt Swift.s cards to it.
        /// </summary>
        private static bool IsStagedSwift(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedSwift ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedSwift ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSwift ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedSwift ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift;

        /// <summary>
        /// Whether this content is one of milestone 8.39i's five staged Cutter scenes. Written in the
        /// same change as those five members, the setup configuration they name and the restaging list
        /// above - the three arms milestone 8.38f had to add for Swift after the fact, and which this
        /// dealing therefore ships together. She needs no ally seat: no face of hers names a friendly
        /// hero, so what her set needs is an enemy hero in radius, enemy minions to swap with and an
        /// obstacle in range, all of which the engaged position and p.8's waves supply.
        /// </summary>
        private static bool IsStagedCutter(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedCutter ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedCutter ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedCutter ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedCutter ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedCutter;

        /// <summary>
        /// Whether this content is one of milestone 8.40j's five staged Bain scenes. Written in the
        /// same change as those five members, the setup configuration they name and the restaging list
        /// above - the three arms milestone 8.38f had to add for Swift after the fact, and which every
        /// dealing since has therefore shipped together. He needs no ally seat: nothing of his names a
        /// FRIENDLY hero, and what his set needs is an enemy hero in radius holding cards, enemy units
        /// to shoot and a board a marker can sit on, all of which the engaged position supplies.
        /// </summary>
        private static bool IsStagedBain(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedBain ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedBain ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBain ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedBain ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBain;

        private static bool IsStagedGarrus(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedGarrus ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedGarrus ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedGarrus ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedGarrus ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedGarrus;

        private static bool IsStagedWidget(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedWidget ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedWidget ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWidget ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedWidget ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWidget;

        /// <summary>
        /// Whether this content is one of milestone 8.57g's five staged Rowenna scenes. Written in the
        /// same change as those five members, the setup configuration they name and the restaging list
        /// above - the three arms milestone 8.38f had to add for Swift after the fact, and which every
        /// dealing since has therefore shipped together.
        /// </summary>
        private static bool IsStagedRowenna(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedRowenna ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedRowenna ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedRowenna ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedRowenna ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedRowenna;

        /// <summary>
        /// Whether this content deals two heroes to the Blue team with Rowenna beside an ally.
        /// Milestone 8.57g's three-hero arms, which take the two-Blue-hero setup and are deliberately
        /// <em>not</em> restaged, exactly as milestone 8.21's Brogan arms are not: what her three Red
        /// riders need is a friendly hero in RADIUS, and the two Blue spawns already are.
        /// </summary>
        private static bool IsRowennaWithAlly(SceneCardContent content) =>
            content == SceneCardContent.VerifiedStagedRowennaWithAlly ||
            content == SceneCardContent.VerifiedStagedRowennaWithAllyAtTierTwo ||
            content == SceneCardContent.VerifiedStagedRowennaWithAllyAtTierThree;

        /// <summary>
        /// Whether this content is one of milestone 8.61j's five staged Min scenes. Written in the
        /// same change as those five members, the setup configuration they name and the restaging
        /// list above - the three arms milestone 8.38f had to add for Swift after the fact, and which
        /// every dealing since has therefore shipped together.
        /// </summary>
        private static bool IsStagedMin(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedMin ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedMin ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMin ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedMin ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMin;

        /// <summary>
        /// Whether this content is one of milestone 8.69g's five staged Brynn scenes. Written in the
        /// same change as those five members, the setup configuration they name and the restaging list
        /// above - the three arms milestone 8.38f had to add for Swift after the fact, and which every
        /// dealing since has therefore shipped together.
        /// </summary>
        private static bool IsStagedBrynn(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedBrynn ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedBrynn ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrynn ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedBrynn ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrynn;

        /// <summary>
        /// Whether this content is one of milestone 8.71h's five staged Hanu scenes. Written in the
        /// same change as those five members, the setup configuration they name and the restaging list
        /// above - the three arms milestone 8.38f had to add for Swift after the fact, and which every
        /// dealing since has therefore shipped together.
        /// </summary>
        private static bool IsStagedHanu(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedHanu ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedHanu ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedHanu ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedHanu ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedHanu;

        /// <summary>
        /// Whether this content deals two heroes to the Blue team with Hanu beside an ally. Milestone
        /// 8.71h's three-hero arms, which take the two-Blue-hero setup and are deliberately
        /// <em>not</em> restaged, exactly as milestone 8.57g's Rowenna arms are not: what This Way!
        /// and That Way! need is a friendly hero in RADIUS, and the two Blue spawns already are.
        /// </summary>
        private static bool IsHanuWithAlly(SceneCardContent content) =>
            content == SceneCardContent.VerifiedStagedHanuWithAlly ||
            content == SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo ||
            content == SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree;

        /// <summary>
        /// Whether this content is one of milestone 8.75b's six staged Ignatia scenes. Written in the
        /// same change as those six members, the setup configuration they name and the restaging list
        /// above - the three arms milestone 8.38f had to add for Swift after the fact, and which every
        /// dealing since has therefore shipped together.
        ///
        /// <para>SIX rather than five, because the Tie Breaker coin's face is content for her: the
        /// first two members are the same starting hand on the same board under her sun and under her
        /// moon, and a build that staged only one of them could never draw half her printed text.
        /// </para>
        /// </summary>
        private static bool IsStagedIgnatia(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedIgnatia ||
            content == SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedIgnatia ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedIgnatia ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedIgnatia ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedIgnatia;

        /// <summary>
        /// Whether this content deals two heroes to the Blue team with Ignatia beside an ally.
        /// Milestone 8.75b's three-hero arms, which take the two-Blue-hero setup and are deliberately
        /// <em>not</em> restaged, exactly as milestone 8.57g's Rowenna arms and 8.71h's Hanu arms are
        /// not: what Searing Heat and Scorching Blaze need is a friendly hero in RADIUS, and the two
        /// Blue spawns already are.
        /// </summary>
        private static bool IsIgnatiaWithAlly(SceneCardContent content) =>
            content == SceneCardContent.VerifiedStagedIgnatiaWithAlly ||
            content == SceneCardContent.VerifiedStagedIgnatiaWithAllyAtTierTwo ||
            content == SceneCardContent.VerifiedStagedIgnatiaWithAllyAtTierThree;

        private static bool IsStagedTali(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedTali ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedTali ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTali ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedTali ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTali;


        /// <summary>
        /// Whether this content deals two heroes to the Blue team with Brogan beside Arien. Milestone
        /// 8.21's three-hero arms, which take the two-hero setup and are deliberately <em>not</em>
        /// restaged: what those cards need is an ally in range, and the two Blue spawns already are.
        /// </summary>
        private static bool IsBroganWithAlly(SceneCardContent content) =>
            content == SceneCardContent.VerifiedStagedBroganWithAlly ||
            content == SceneCardContent.VerifiedStagedBroganWithAllyAtTierTwo ||
            content == SceneCardContent.VerifiedStagedBroganWithAllyAtTierThree;

        private static SceneCardContent TakeRequestedCardContent()
        {
            var requested = NextComposedCardContent;
            NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            return requested;
        }

        /// <summary>
        /// ELEVENTH amendment. Reads <see cref="NextComposedMatchFormat"/> and clears it, falling
        /// back to the launch line's format flag and then to
        /// <see cref="MatchFormat.Default"/> - so a fixture that forgets to clear it cannot decide
        /// which setup a later scene load composes.
        /// </summary>
        /// <remarks>
        /// The property wins over the flag because only one of the two can be a mistake: a Play Mode
        /// fixture setting the property is asking for this composition, while the flag belongs to
        /// the process and would otherwise apply to every scene the Editor loads after it.
        /// A malformed flag is NOT refused here - the parse's own error is reported where every
        /// other malformed launch option is, by the driver attach in <c>Awake</c>, which quits with
        /// the message rather than starting a session nobody configured. Composing the default in
        /// the meantime keeps that one refusal in one place.
        /// </remarks>
        /// <summary>
        /// Milestone 8.35z37. Reads <see cref="NextComposedLineup"/> and clears it, so a
        /// fixture that forgets to clear it cannot decide what a later scene load deals.
        /// </summary>
        /// <remarks>
        /// There is no launch-line fallback, unlike the format's, and that is the design rather than
        /// an omission - see <see cref="NextComposedLineup"/>. Null falls through to
        /// <see cref="MatchCompositionPlan"/>'s printed default.
        /// </remarks>
        private static HeroLineup TakeRequestedLineup()
        {
            var requested = NextComposedLineup;
            NextComposedLineup = null;
            return requested;
        }

        private static MatchFormat TakeRequestedMatchFormat()
        {
            var requested = NextComposedMatchFormat;
            NextComposedMatchFormat = null;
            if (requested != null)
            {
                return requested;
            }

            if (!UnityEngine.Application.isPlaying)
            {
                return MatchFormat.Default;
            }

            var options = GuardsDigital.Network.PrivateSessionLaunchOptions.Parse(
                System.Environment.GetCommandLineArgs());
            return options.IsValid ? options.MatchFormat : MatchFormat.Default;
        }

        /// <summary>
        /// TWELFTH amendment. Reads <see cref="NextComposedSandboxStaging"/> and clears it, falling
        /// back to the launch line's staging flag, and then to <c>null</c> - the ordinary dealt
        /// match. The property wins over the flag for the same reason the format's does: a fixture
        /// setting it is asking for this composition, while the flag belongs to the process and
        /// would otherwise stage every scene the Editor loads after it.
        /// </summary>
        /// <remarks>
        /// An unreadable or malformed staging file is REFUSED rather than silently dealt around.
        /// A tester who asked for a staged board and got a dealt one would draw exactly the wrong
        /// conclusion from whatever they then observed, so this is the one launch fault the scene
        /// itself reports: the format flag can fall back to a default because every format is a
        /// real match, while a staging that did not load leaves nothing honest to compose.
        /// </remarks>
        private static SandboxStagingDescription TakeRequestedSandboxStaging()
        {
            var requested = NextComposedSandboxStaging;
            NextComposedSandboxStaging = null;
            if (requested != null)
            {
                return requested;
            }

            if (!UnityEngine.Application.isPlaying)
            {
                return null;
            }

            var options = GuardsDigital.Network.PrivateSessionLaunchOptions.Parse(
                System.Environment.GetCommandLineArgs());
            if (!options.IsValid || !options.IsSandbox)
            {
                return null;
            }

            string text;
            try
            {
                text = System.IO.File.ReadAllText(options.SandboxStagingPath);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "The sandbox staging file '" + options.SandboxStagingPath +
                    "' could not be read: " + exception.Message, exception);
            }

            if (!SandboxStagingDescription.TryParse(text, out var description, out var error))
            {
                throw new InvalidOperationException(
                    "The sandbox staging file '" + options.SandboxStagingPath +
                    "' is not a readable " + SandboxStagingDescription.FormatTag + " staging: " +
                    error);
            }

            return description;
        }

        /// <summary>
        /// Milestone 8.30b. Reads <see cref="NextComposedSeat"/> and puts it back to the Director, so a
        /// fixture that forgets to clear it cannot decide which seat a later scene load plays from.
        /// </summary>
        private static SeatId? TakeRequestedSeat()
        {
            var requested = NextComposedSeat;
            NextComposedSeat = null;
            return requested;
        }

        private void Awake()
        {
            // Edit-mode composition is opt-in. Running it on every domain reload regenerates the
            // whole board and lets generated objects reach the saved scene, which is what left the
            // seven authored tiles behind. See milestone 6.1e / finding F613-04.
            if (!UnityEngine.Application.isPlaying && !composeInEditMode)
            {
                return;
            }

            Compose();
            AttachSmokeDriverWhenRequested();
            AttachPrivateOnlineDriverWhenRequested();
            AttachBoardVisualCaptureDriverWhenRequested();
            AttachPracticeBotDriverWhenRequested();
        }

        /// <summary>
        /// Milestone 8.35z26. Host-owned practice bots, attached only when the launch asked
        /// for them - the same rule the smoke, capture and private-online hooks all follow, so a
        /// normally launched build carries no bot at all and a malformed request fails loudly instead
        /// of quietly playing nobody's seats.
        /// </summary>
        private void AttachPracticeBotDriverWhenRequested()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (!PracticeBotSeatDriver.TryParseRequested(
                    System.Environment.GetCommandLineArgs(), out var parsed, out var error))
            {
                return;
            }

            if (parsed == null)
            {
                UnityEngine.Debug.LogError("GUARDS_PRACTICEBOT: FAIL " + error);
                return;
            }

            gameObject.AddComponent<PracticeBotSeatDriver>().Configure(this, parsed);
        }

        /// <summary>
        /// Milestone 8.31b. Only a player launched with the capture flag photographs itself; a
        /// normally launched build carries no capture hook, exactly as it carries no smoke hook.
        /// </summary>
        private void AttachBoardVisualCaptureDriverWhenRequested()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            var path = BoardVisualCaptureDriver.RequestedPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            gameObject.AddComponent<BoardVisualCaptureDriver>()
                .Configure(this, path, eventCamera, mapView);
        }

        /// <summary>
        /// Only a player launched with the smoke flag ever constructs the driver. A normally
        /// launched build has no test hook in it at all.
        /// </summary>
        private void AttachSmokeDriverWhenRequested()
        {
            if (!UnityEngine.Application.isPlaying || !PlayerSmokeTestDriver.IsRequested())
            {
                return;
            }

            var driver = gameObject.AddComponent<PlayerSmokeTestDriver>();
            driver.Configure(this);
        }

        /// <summary>
        /// Milestone 8.31. Only a player launched with the private host or client flag constructs
        /// its driver, the same rule the smoke driver follows: a normally launched build carries no
        /// network hook at all, and a malformed private-online launch fails loudly instead of
        /// starting a session nobody configured.
        /// </summary>
        private void AttachPrivateOnlineDriverWhenRequested()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            var options = GuardsDigital.Network.PrivateSessionLaunchOptions.Parse(
                System.Environment.GetCommandLineArgs());
            if (!options.IsValid)
            {
                UnityEngine.Debug.LogError("GUARDS_ONLINE: FAIL " + options.Error);
                UnityEngine.Application.Quit(2);
                return;
            }

            switch (options.Role)
            {
                case GuardsDigital.Network.PrivateSessionRole.Host:
                    gameObject.AddComponent<PrivateHostDriver>().Configure(this, options);
                    return;
                case GuardsDigital.Network.PrivateSessionRole.Client:
                    gameObject.AddComponent<PrivateClientDriver>().Configure(this, options);
                    return;
                // Milestone 8.35a. The interactive lobby: the same opt-in rule again, so a normally
                // launched build carries no lobby either.
                case GuardsDigital.Network.PrivateSessionRole.Lobby:
                    var lobby = gameObject.AddComponent<PrivateLobbyDriver>();
                    lobby.Configure(this);
                    if (PrivateOnlineRehearsalDriver.TryParseRequested(
                        System.Environment.GetCommandLineArgs(), out var rehearsal, out var error))
                    {
                        if (rehearsal == null)
                        {
                            UnityEngine.Debug.LogError("GUARDS_ONLINE: FAIL " + error);
                            UnityEngine.Application.Quit(2);
                            return;
                        }

                        gameObject.AddComponent<PrivateOnlineRehearsalDriver>()
                            .Configure(this, lobby, rehearsal);
                    }

                    // THIRTEENTH amendment. A test-driver child spawned by another process's host -
                    // mutually exclusive with the CI rehearsal flag above by construction (a normal
                    // launch carries neither, and nothing spawns a process asking for both).
                    if (PrivateSeatDriver.TryParseRequested(
                        System.Environment.GetCommandLineArgs(), out var seatDriver, out var seatDriverError))
                    {
                        if (seatDriver == null)
                        {
                            UnityEngine.Debug.LogError("GUARDS_ONLINE: FAIL " + seatDriverError);
                            UnityEngine.Application.Quit(2);
                            return;
                        }

                        gameObject.AddComponent<PrivateSeatDriver>()
                            .Configure(this, lobby, seatDriver);
                    }

                    return;
            }
        }

        private void OnDestroy()
        {
            if (mapView != null)
            {
                mapView.ConfiguredMinionViewsChanged -= RebindConfiguredMinionViews;
                if (hud != null)
                {
                    hud.PlayfieldLayoutChanged -= mapView.ConfigureCameraViewport;
                }
            }
            runtime?.BoardView?.ReleaseOwnedResources();
            runtime = null;
            cornerHighlightTemplate?.Dispose();
            cornerHighlightTemplate = null;
        }
    }
}
