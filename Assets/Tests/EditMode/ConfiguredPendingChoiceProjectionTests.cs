using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.11e's application-level inventory. Individual authority fixtures own each rule
    /// permutation; these cases own the shared claim that an answer and the projection it produces
    /// cross the session boundary together.
    /// </summary>
    public sealed class ConfiguredPendingChoiceProjectionTests
    {
        private static readonly string[] ChoiceProjectionMembers =
        {
            nameof(MatchSessionProjection.PendingInitiativeChoice),
            nameof(MatchSessionProjection.PendingMovementChoice),
            nameof(MatchSessionProjection.PendingAttackTargetChoice),
            nameof(MatchSessionProjection.PendingDefenseChoice),
            nameof(MatchSessionProjection.PendingEffectChoice),
            nameof(MatchSessionProjection.PendingDisplacedMinionCorrection),
            nameof(MatchSessionProjection.PendingMinionBattleChoice),
            nameof(MatchSessionProjection.PendingHeroRespawnChoice),
            nameof(MatchSessionProjection.PendingSystemTransitionStep),
            // Milestone 8.21. A player answers it by paying one of the projected prices or declining,
            // so it is a choice rather than a companion envelope - the same classification every other
            // family whose answer is a typed invocation has.
            nameof(MatchSessionProjection.PendingRemovalPrevention)
        };

        private static readonly string[] CompanionProjectionMembers =
        {
            nameof(MatchSessionProjection.PendingMovementResolution),
            nameof(MatchSessionProjection.PendingAttackResolution),
            nameof(MatchSessionProjection.PendingSkillResolution),
            nameof(MatchSessionProjection.PendingEffectChoicePreview),
            nameof(MatchSessionProjection.PendingDefensePreview),
            nameof(MatchSessionProjection.PendingLanePushRequest),
            nameof(MatchSessionProjection.PendingSystemTransitionDescription),
            nameof(MatchSessionProjection.PendingImmediateLanePush)
        };

        private static readonly TransitionCase[] Matrix =
        {
            Row("Initiative", "Choose tied actor"),
            Row("Movement", "Continue"),
            Row("Movement", "Stop"),
            Row("Attack target", "Hero"),
            Row("Attack target", "Minion"),
            Row("Defense", "Numeric Defense"),
            Row("Defense", "Fulfilled Block"),
            Row("Defense", "Unfulfilled Block"),
            Row("Defense", "Decline"),
            Row("Effect/Skill", EffectChoiceCommandKind.Select.ToString()),
            Row("Effect/Skill", EffectChoiceCommandKind.Deselect.ToString()),
            Row("Effect/Skill", EffectChoiceCommandKind.Confirm.ToString()),
            Row("Effect/Skill", EffectChoiceCommandKind.Decline.ToString()),
            Row("Effect/Skill", EffectChoiceCommandKind.Stop.ToString()),
            Row("Effect/Attack or Block", EffectChoiceCommandKind.Select.ToString()),
            Row("Effect/Attack or Block", EffectChoiceCommandKind.Deselect.ToString()),
            Row("Effect/Attack or Block", EffectChoiceCommandKind.Confirm.ToString()),
            Row("Effect/Attack or Block", EffectChoiceCommandKind.Decline.ToString()),
            Row("Effect/Attack or Block", EffectChoiceCommandKind.Stop.ToString()),
            Row("Displaced minion correction", DisplacedMinionCorrectionCommandKind.SelectSubject.ToString()),
            Row("Displaced minion correction", DisplacedMinionCorrectionCommandKind.SelectDestination.ToString()),
            Row("Minion Battle", MinionBattleChoiceCommandKind.Select.ToString()),
            Row("Minion Battle", MinionBattleChoiceCommandKind.Deselect.ToString()),
            Row("Minion Battle", MinionBattleChoiceCommandKind.Confirm.ToString()),
            Row("Hero respawn", HeroRespawnCommandKind.Respawn.ToString()),
            Row("Hero respawn", HeroRespawnCommandKind.ResolveWithoutAction.ToString()),
            Row("System transition", "Lane-Push respawn/Place"),
            Row("System transition", "Level-Up upgrade")
        };

        [Test]
        public void EveryConfiguredChoiceAnswerPublishesTheCurrentProjectionOrCommitsNothing()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var exercisedFamilies = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < 160 && session.Projection.Phase != RoundResolutionPhase.GameEnded; index++)
            {
                var before = session.Projection;
                var family = CurrentChoiceFamily(before);
                var offered = DeterministicMatchLoop.EnumerateLegalCommands(before);
                Assert.That(offered, Is.Not.Empty,
                    $"The configured match stalled at {before.Phase}, round {before.RoundIndex}, turn {before.TurnIndex}.");
                var command = offered[0];
                var result = session.Execute(command);

                Assert.That(result.Accepted, Is.True,
                    $"The projection offered {command.Kind}, but authority refused it: {result.Detail}");
                Assert.That(result.Projection, Is.SameAs(session.Projection));
                Assert.That(result.Projection, Is.Not.SameAs(before));
                Assert.That(result.Projection.Version, Is.EqualTo(before.Version + 1));
                Assert.That(result.Projection.AcceptedCommands.Last(), Is.EqualTo(command));
                Assert.That(result.Projection.AuthoritativeSnapshot,
                    Is.Not.EqualTo(before.AuthoritativeSnapshot),
                    "An accepted configured input must publish the state it actually committed.");

                if (family == null)
                {
                    continue;
                }

                exercisedFamilies.Add(family);
                var after = session.Projection;
                var stale = session.Execute(command);
                Assert.That(stale.Accepted, Is.False,
                    $"The previous {family} invocation remained consumable after it was accepted.");
                Assert.That(stale.Projection, Is.SameAs(after));
                Assert.That(session.Projection, Is.SameAs(after));
            }

            Assert.That(exercisedFamilies, Does.Contain("Movement"));
            Assert.That(exercisedFamilies, Does.Contain("Attack target"));
            Assert.That(exercisedFamilies, Does.Contain("Defense"));
            Assert.That(exercisedFamilies, Does.Contain("Effect"));
        }

        [Test]
        public void EveryConfiguredChoiceFamilyAndAnswerKindHasATransitionCase()
        {
            var projectedPendingMembers = typeof(MatchSessionProjection).GetProperties()
                .Select(property => property.Name)
                .Where(name => name.StartsWith("Pending", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            var classifiedMembers = ChoiceProjectionMembers
                .Concat(CompanionProjectionMembers)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(classifiedMembers, Is.EqualTo(projectedPendingMembers),
                "A new projected pending family must be classified as a player choice or a companion envelope.");

            AssertEnumKindsCovered<EffectChoiceCommandKind>("Effect/Skill", "Effect/Attack or Block");
            AssertEnumKindsCovered<DisplacedMinionCorrectionCommandKind>("Displaced minion correction");
            AssertEnumKindsCovered<MinionBattleChoiceCommandKind>("Minion Battle");
            AssertEnumKindsCovered<HeroRespawnCommandKind>("Hero respawn");
            Assert.That(Matrix.Select(value => value.Key).Distinct().Count(), Is.EqualTo(Matrix.Length),
                "Each family/answer row must have one unambiguous owner in the transition matrix.");
        }

        private static string CurrentChoiceFamily(MatchSessionProjection projection)
        {
            if (projection.PendingInitiativeChoice != null) return "Initiative";
            if (projection.PendingMovementChoice != null) return "Movement";
            if (projection.PendingAttackTargetChoice != null) return "Attack target";
            if (projection.PendingDefenseChoice != null) return "Defense";
            if (projection.PendingEffectChoice != null) return "Effect";
            if (projection.PendingDisplacedMinionCorrection != null) return "Correction";
            if (projection.PendingMinionBattleChoice != null) return "Minion Battle";
            if (projection.PendingHeroRespawnChoice != null) return "Hero respawn";
            if (projection.PendingSystemTransitionStep != null) return "System transition";
            return null;
        }

        private static void AssertEnumKindsCovered<T>(params string[] families) where T : Enum
        {
            var expected = Enum.GetValues(typeof(T)).Cast<T>()
                .Where(value => !string.Equals(value.ToString(), "None", StringComparison.Ordinal))
                .Select(value => value.ToString())
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            foreach (var family in families)
            {
                Assert.That(Matrix.Where(value => value.Family == family).Select(value => value.Answer)
                        .Where(expected.Contains).OrderBy(value => value, StringComparer.Ordinal),
                    Is.EqualTo(expected),
                    $"{family} must deliberately own every non-None {typeof(T).Name} value.");
            }
        }

        private static TransitionCase Row(string family, string answer) =>
            new TransitionCase(family, answer);

        private sealed class TransitionCase
        {
            public TransitionCase(string family, string answer)
            {
                Family = family;
                Answer = answer;
            }

            public string Family { get; }
            public string Answer { get; }
            public string Key => $"{Family}/{Answer}";
        }
    }
}
