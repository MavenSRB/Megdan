using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public sealed class PlaceholderMovementHand
    {
        private static readonly SyntheticCardProfile[] RedProfiles =
        {
            SyntheticCardProfile.WithPushPlaceSkill("red-needle", "Red Needle", 13, 2, 1),
            SyntheticCardProfile.WithModificationSkill("red-bulwark", "Red Bulwark", 11, 2, 3),
            new SyntheticCardProfile("red-lancer", "Red Lancer", 9, AttackRangeKind.Ranged, 2, 3, 3, 1),
            new SyntheticCardProfile("red-pursuit", "Red Pursuit", 7, AttackRangeKind.NonRanged, 4, 0, 3, 2),
            SyntheticCardProfile.WithDirectMoveSpawnSkill("red-bombard", "Red Bombard", 5, 2, 2)
        };

        private static readonly SyntheticCardProfile[] BlueProfiles =
        {
            SyntheticCardProfile.WithImmunityTimingSkill("blue-feint", "Blue Feint", 12, 3, 2),
            SyntheticCardProfile.WithExchangeSkill("blue-sentinel", "Blue Sentinel", 11, 2, 4),
            new SyntheticCardProfile("blue-scout-shot", "Blue Scout Shot", 8, AttackRangeKind.Ranged, 2, 2, 4, 1),
            new SyntheticCardProfile("blue-hammer", "Blue Hammer", 6, AttackRangeKind.NonRanged, 5, 0, 2, 1),
            SyntheticCardProfile.WithPushRepeatSkill("blue-longbow", "Blue Longbow", 4, 3, 2)
        };

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

        private PlaceholderMovementHand(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            Definitions = definitions;
            Instances = instances;
        }

        public IReadOnlyList<CardDefinition> Definitions { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public static PlaceholderMovementHand Create(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, ownerId.Value);
        }

        public static PlaceholderMovementHand CreateRed(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, null);
        }

        public static PlaceholderMovementHand CreateBlue(HeroId ownerId)
        {
            return Create(ownerId, BlueProfiles, null);
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
        public static PlaceholderMovementHand CreateRedWithSkill(HeroId ownerId)
        {
            return Create(
                ownerId, RedProfiles, null, RedSkillProfile, RedComponentSkillProfile,
                RedDisplacementSkillProfile);
        }

        public static PlaceholderMovementHand CreateBlueWithSkill(HeroId ownerId)
        {
            return Create(
                ownerId, BlueProfiles, null, BlueSkillProfile, BlueComponentSkillProfile,
                BlueDisplacementSkillProfile);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 6.2's Cache card alone, for the configured scene.
        ///
        /// Milestone 6.2d deals this hand rather than the Skill-fixture one. The scene needed exactly
        /// one thing it did not have - a way for a player to put a token on the board through ordinary
        /// commands - and the Cache card already places a Hazard into an adjacent empty space, so this
        /// invents no content and prints nothing onto a card. The other two fixture Skills are left out
        /// deliberately: each exists so a fixture can reach one operation family in isolation, and
        /// dealing them into the scene would widen it well past what 6.2d needs.
        /// </summary>
        public static PlaceholderMovementHand CreateRedWithComponentSkill(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, null, componentSkillProfile: RedComponentSkillProfile);
        }

        public static PlaceholderMovementHand CreateBlueWithComponentSkill(HeroId ownerId)
        {
            return Create(ownerId, BlueProfiles, null, componentSkillProfile: BlueComponentSkillProfile);
        }

        /// <summary>
        /// The five ordinary cards plus milestone 6.7's Repeat card alone.
        ///
        /// Kept to one extra card for the reason every earlier fixture hand is: the ordinary five pin an
        /// action matrix and a digest that must not move, and a repeat fixture needs exactly one action
        /// that can displace something and then ask to happen again.
        /// </summary>
        public static PlaceholderMovementHand CreateRedWithRepeatSkill(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, null, repeatSkillProfile: RedRepeatSkillProfile);
        }

        public static PlaceholderMovementHand CreateBlueWithRepeatSkill(HeroId ownerId)
        {
            return Create(ownerId, BlueProfiles, null, repeatSkillProfile: BlueRepeatSkillProfile);
        }

        internal PlaceholderMovementHandRestoreState CaptureRestoreState() =>
            new PlaceholderMovementHandRestoreState(Definitions, Instances);

        internal static PlaceholderMovementHand Restore(PlaceholderMovementHandRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var definitionArray = state.Definitions;
            var instanceArray = state.Instances;
            if (definitionArray.Count == 0 || instanceArray.Count == 0 ||
                instanceArray.Select(instance => instance.OwnerId).Distinct().Count() != 1 ||
                instanceArray.Any(instance => !definitionArray.Any(definition =>
                    definition.Reference.Equals(instance.DefinitionReference))))
            {
                throw new ArgumentException("A restored placeholder hand requires one owner's definition-backed cards.");
            }

            return new PlaceholderMovementHand(definitionArray, instanceArray);
        }

        private static PlaceholderMovementHand Create(
            HeroId ownerId,
            IReadOnlyList<SyntheticCardProfile> profiles,
            string instancePrefix,
            SyntheticSkillProfile skillProfile = null,
            SyntheticSkillProfile componentSkillProfile = null,
            SyntheticSkillProfile displacementSkillProfile = null,
            SyntheticSkillProfile repeatSkillProfile = null)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("A placeholder hand requires a defined owner.", nameof(ownerId));
            }

            var definitions = new List<CardDefinition>();
            var instances = new List<CardInstance>();
            foreach (var profile in profiles)
            {
                AddCard(definitions, instances, CreateDefinition(profile), profile.Id, instancePrefix, ownerId);
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

            return new PlaceholderMovementHand(
                definitions.AsReadOnly(),
                instances.AsReadOnly());
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
                        SyntheticDisplacementCapabilities.DirectMoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectSubject, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("towardActor", EffectValue.Boolean(false))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.ObjectPushV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            // p.18's own breadth, restated by the fixture hand rather than narrowed:
                            // this card says every adjacent object, and a token standing beside the
                            // actor is one. It is the conservative case the batch's coverage rests on.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.UnitOrToken))
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
                        SyntheticPlacementCapabilities.PlaceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("radius", EffectValue.Integer(2))
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
                        SyntheticDisplacementCapabilities.DirectMoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectSubject, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("towardActor", EffectValue.Boolean(false))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.ObjectPushV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            // p.18's own breadth, restated by the fixture hand rather than narrowed:
                            // this card says every adjacent object, and a token standing beside the
                            // actor is one. It is the conservative case the batch's coverage rests on.
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer(
                                    (int)Rules.Displacement.PushEligiblePieceKind.UnitOrToken))
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
        /// </summary>
        private static ActionDefinition CreateImmunityTimingSkillAction(SyntheticCardProfile profile)
        {
            var root = new EffectNodeId("feint-root");
            var pushImmunity = new EffectNodeId("feint-push-immunity");
            var countsAsMinion = new EffectNodeId("feint-counts-as-minion");

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
                        childNodeIds: new[] { pushImmunity, countsAsMinion },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Mode(pushImmunity, SyntheticModificationCapabilities.GrantPushImmunityV1),
                    Mode(countsAsMinion, SyntheticModificationCapabilities.CountAsMinionV1)
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
                        SyntheticDisplacementCapabilities.PushTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
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
                        childNodeIds: new[] { actorSpace, place }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("radius", EffectValue.Integer(4))
                        },
                        outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) })
                });

            return new ActionDefinition(
                new ActionDefinitionId("primary-push-place-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect);
        }

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
                        SyntheticDisplacementCapabilities.DirectMoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("towardActor", EffectValue.Boolean(false))
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
                        SyntheticDisplacementCapabilities.PushTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", selectTarget, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
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
            private const string PrintedAttackRevision = "phase-5-1a-rev-3";

            public SyntheticCardProfile(
                string id,
                string displayName,
                int initiative,
                AttackRangeKind attackRange,
                int attackValue,
                int rangeValue,
                int movementValue,
                int defenseValue)
                : this(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.Attack, PrintedAttackRevision,
                    attackRange, attackValue, rangeValue, movementValue, defenseValue)
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
                int defenseValue)
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
                int defenseValue) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.ModificationSkill,
                    "phase-6-8a-rev-1", AttackRangeKind.None, 0, 0, movementValue, defenseValue);

            /// <summary>
            /// Milestone 6.8b's converted card, on the same terms: identity, Initiative, printed
            /// Secondary Movement and Defense reaction all stay, and it states no Attack values at all,
            /// because its printed Primary Attack is replaced rather than suppressed.
            /// </summary>
            public static SyntheticCardProfile WithImmunityTimingSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.ImmunityTimingSkill,
                    "phase-6-8b-rev-1", AttackRangeKind.None, 0, 0, movementValue, defenseValue);

            /// <summary>Milestone 6.5a's converted card, on the same terms as the two before it.</summary>
            public static SyntheticCardProfile WithPushPlaceSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.PushPlaceSkill,
                    "phase-6-4c-rev-1", AttackRangeKind.None, 0, 0, movementValue, defenseValue);

            /// <summary>Milestone 6.5b's converted card, on the same terms as the three before it.</summary>
            public static SyntheticCardProfile WithDirectMoveSpawnSkill(
                string id,
                string displayName,
                int initiative,
                int movementValue,
                int defenseValue) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.DirectMoveSpawnSkill,
                    "phase-6-8l-rev-1", AttackRangeKind.None, 0, 0, movementValue, defenseValue);

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
                int defenseValue) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.ExchangeSkill,
                    "phase-6-6a-rev-1", AttackRangeKind.None, 0, 0, movementValue, defenseValue);

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
                int defenseValue) =>
                new SyntheticCardProfile(
                    id, displayName, initiative, SyntheticPrintedPrimaryKind.PushRepeatSkill,
                    "phase-6-4c-rev-1", AttackRangeKind.None, 0, 0, movementValue, defenseValue);

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
        }
    }
}
