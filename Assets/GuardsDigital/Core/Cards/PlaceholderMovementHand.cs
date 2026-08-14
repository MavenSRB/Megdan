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
            new SyntheticCardProfile("red-bulwark", "Red Bulwark", 11, AttackRangeKind.NonRanged, 3, 0, 2, 3),
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
            return Create(ownerId, RedProfiles, null, RedSkillProfile);
        }

        public static PlaceholderMovementHand CreateBlueWithSkill(HeroId ownerId)
        {
            return Create(ownerId, BlueProfiles, null, BlueSkillProfile);
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
            SyntheticSkillProfile skillProfile = null)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("A placeholder hand requires a defined owner.", nameof(ownerId));
            }

            var extra = skillProfile == null ? 0 : 1;
            var definitions = new CardDefinition[profiles.Count + extra];
            var instances = new CardInstance[profiles.Count + extra];
            for (var index = 0; index < profiles.Count; index++)
            {
                var profile = profiles[index];
                var definition = CreateDefinition(profile);
                definitions[index] = definition;
                instances[index] = CardInstance.CreateInHand(
                    new CardInstanceId(
                        string.IsNullOrWhiteSpace(instancePrefix)
                            ? $"{profile.Id}-card"
                            : $"{instancePrefix}-{profile.Id}-card"),
                    definition.Reference,
                    ownerId);
            }

            if (skillProfile != null)
            {
                var definition = CreateSkillDefinition(skillProfile);
                definitions[profiles.Count] = definition;
                instances[profiles.Count] = CardInstance.CreateInHand(
                    new CardInstanceId(
                        string.IsNullOrWhiteSpace(instancePrefix)
                            ? $"{skillProfile.Id}-card"
                            : $"{instancePrefix}-{skillProfile.Id}-card"),
                    definition.Reference,
                    ownerId);
            }

            return new PlaceholderMovementHand(
                Array.AsReadOnly(definitions),
                Array.AsReadOnly(instances));
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
                CreateAttackAction(profile.AttackRange, profile.AttackValue, profile.RangeValue),
                CreateMovementAction(profile.MovementValue),
                CreateDefenseAction(profile.DefenseValue)
            };

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-5-1a-rev-3",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                actions);
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

        private sealed class SyntheticCardProfile
        {
            public SyntheticCardProfile(
                string id,
                string displayName,
                int initiative,
                AttackRangeKind attackRange,
                int attackValue,
                int rangeValue,
                int movementValue,
                int defenseValue)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                AttackRange = attackRange;
                AttackValue = attackValue;
                RangeValue = rangeValue;
                MovementValue = movementValue;
                DefenseValue = defenseValue;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public AttackRangeKind AttackRange { get; }
            public int AttackValue { get; }
            public int RangeValue { get; }
            public int MovementValue { get; }
            public int DefenseValue { get; }
        }
    }
}
