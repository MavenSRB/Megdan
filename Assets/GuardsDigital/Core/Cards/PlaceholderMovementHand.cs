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
            new SyntheticCardProfile("red-needle", "Red Needle", 13, AttackRangeKind.NonRanged, 2, 0, 2, 1),
            SyntheticCardProfile.WithModificationSkill("red-bulwark", "Red Bulwark", 11, 2, 3),
            new SyntheticCardProfile("red-lancer", "Red Lancer", 9, AttackRangeKind.Ranged, 2, 3, 3, 1),
            new SyntheticCardProfile("red-pursuit", "Red Pursuit", 7, AttackRangeKind.NonRanged, 4, 0, 3, 2),
            new SyntheticCardProfile("red-bombard", "Red Bombard", 5, AttackRangeKind.Ranged, 3, 2, 2, 2)
        };

        private static readonly SyntheticCardProfile[] BlueProfiles =
        {
            new SyntheticCardProfile("blue-feint", "Blue Feint", 12, AttackRangeKind.NonRanged, 2, 0, 3, 2),
            new SyntheticCardProfile("blue-sentinel", "Blue Sentinel", 11, AttackRangeKind.NonRanged, 3, 0, 2, 4),
            new SyntheticCardProfile("blue-scout-shot", "Blue Scout Shot", 8, AttackRangeKind.Ranged, 2, 2, 4, 1),
            new SyntheticCardProfile("blue-hammer", "Blue Hammer", 6, AttackRangeKind.NonRanged, 5, 0, 2, 1),
            new SyntheticCardProfile("blue-longbow", "Blue Longbow", 4, AttackRangeKind.Ranged, 3, 3, 3, 2)
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
        /// </summary>
        private static readonly SyntheticSkillProfile RedDisplacementSkillProfile =
            new SyntheticSkillProfile("red-shove", "Red Shove", 16);

        private static readonly SyntheticSkillProfile BlueDisplacementSkillProfile =
            new SyntheticSkillProfile("blue-shove", "Blue Shove", 1);

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
            SyntheticSkillProfile displacementSkillProfile = null)
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

            var effect = new EffectDefinition(
                new EffectDefinitionId($"placeholder-{profile.Id}-skill"),
                "phase-6-8k-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { moveBranch, push },
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
                        SyntheticTargetingCapabilities.ChooseTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(true))
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
                        SyntheticDisplacementCapabilities.ObjectPushV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-6-8k-rev-1",
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
            ModificationSkill = 1
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
