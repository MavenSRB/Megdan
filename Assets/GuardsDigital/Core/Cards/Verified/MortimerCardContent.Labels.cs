using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.40 (the user, 2026-10-03: "never have option X label on the button, always write
    /// what the button does"). The words on every control Mortimer's composed faces raise: the two
    /// bullets of each "Choose one" / "Choose up to N times" pass, the "up to" step that opens each
    /// pass after the first, and Corpse Slam's line of "that Zombie token may push". Derived from
    /// <see cref="DecisionLabelRequirementQuery"/> over his dealt faces - the coverage check proves
    /// the two sets equal - and joined to <see cref="DecisionLabelCatalog"/> here rather than at his
    /// dealing (9.8f), which left his controls reading "OPTION 1" and "OPTION 2".
    ///
    /// <para>One row per pass, because every pass is its own node (<see cref="MortimerRepeatedChoice"/>):
    /// a face printing "up to three times" declares five passes once Master of Puppets raises it, and
    /// each wrapper after the first says that the press chooses AGAIN and that declining stops - which
    /// is ruling W10's reading of the count (milestone 9.39).</para>
    /// </summary>
    public static partial class MortimerCardContent
    {
        private const string MoveZombieRadius = "Move a Zombie token in radius 1 space";
        private const string MoveZombieRange = "Move a Zombie token in range 1 space";
        private const string MoveZombieRangeUpTo = "Move a Zombie token in range up to 1 space";

        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            // --- Red Attacks and the Gold basic (MortimerAttackFaces) ---------------------------------
            foreach (var row in Passes(
                         MortimerAttackFaces.KnifeOfTheLivingDead, MortimerAttackFaces.KnifePrefix, 3,
                         "Move or remove a Zombie token", "Do neither",
                         (MortimerAttackFaces.MoveBullet(MortimerAttackFaces.KnifePrefix, 0), MoveZombieRadius),
                         (MortimerAttackFaces.RemoveBullet(0), "Remove a Zombie token beside the target for +1 Attack")))
                yield return row;

            foreach (var (face, passes) in new[]
                     {
                         (MortimerAttackFaces.CrawlingDead, 1), (MortimerAttackFaces.WalkingDead, 2),
                         (MortimerAttackFaces.RacingDead, 3)
                     })
            {
                foreach (var row in Passes(
                             face, MortimerAttackFaces.DeadPrefix, passes,
                             "Move a Zombie token or make another enemy hero discard", "Do neither",
                             (MortimerAttackFaces.MoveBullet(MortimerAttackFaces.DeadPrefix, 0), MoveZombieRadius),
                             (MortimerAttackFaces.DiscardBullet(0), "Another enemy hero beside a Zombie token discards a card")))
                    yield return row;
            }

            foreach (var (face, passes) in new[]
                     {
                         (MortimerAttackFaces.Braains, 2), (MortimerAttackFaces.Braaaaaaains, 3)
                     })
            {
                foreach (var row in Passes(
                             face, MortimerAttackFaces.BrainsPrefix, passes,
                             "Move a Zombie token or retrieve a card", "Do neither",
                             (MortimerAttackFaces.MoveBullet(MortimerAttackFaces.BrainsPrefix, 0), MoveZombieRadius),
                             (MortimerAttackFaces.RetrieveBullet(0), "Retrieve a discarded card")))
                    yield return row;
            }

            // --- Blues (MortimerBlueFaces) -----------------------------------------------------------
            foreach (var (face, passes) in new[]
                     {
                         (MortimerBlueFaces.CorpseSlam, 1), (MortimerBlueFaces.MorbidMosh, 2),
                         (MortimerBlueFaces.MacabreMayhem, 3)
                     })
            {
                foreach (var row in Passes(
                             face, MortimerBlueFaces.SlamPrefix, passes,
                             "Move a Zombie token or move 1 space", "Do neither",
                             (MortimerBlueFaces.ShoveBullet(0), MoveZombieRangeUpTo),
                             (MortimerBlueFaces.StepBullet(MortimerBlueFaces.SlamPrefix, 0), "Move 1 space")))
                    yield return row;

                // "; that Zombie token may push a unit or a token adjacent to it 1 space."
                for (var pass = 1; pass <= Count(passes); pass++)
                {
                    yield return DecisionStep(face, MortimerBlueFaces.ShoveBullet(pass) + "-then",
                        "Push a unit or a token beside the Zombie 1 space", "Push nothing");
                }
            }

            foreach (var (face, passes) in new[]
                     {
                         (MortimerBlueFaces.RobbingZombies, 2), (MortimerBlueFaces.StalkingScalpers, 3)
                     })
            {
                foreach (var row in Passes(
                             face, MortimerBlueFaces.RobPrefix, passes,
                             "Move a Zombie token for a coin or move 1 space", "Do neither",
                             (MortimerBlueFaces.CoinBullet(0), "Move a Zombie token up to 1 space and gain 1 coin"),
                             (MortimerBlueFaces.StepBullet(MortimerBlueFaces.RobPrefix, 0), "Move 1 space")))
                    yield return row;
            }

            // --- Greens (MortimerGreenFaces) ---------------------------------------------------------
            foreach (var (face, passes) in new[]
                     {
                         (MortimerGreenFaces.StageDive, 1), (MortimerGreenFaces.CrowdDrift, 2),
                         (MortimerGreenFaces.CrowdSurf, 3)
                     })
            {
                foreach (var row in Passes(
                             face, MortimerGreenFaces.DivePrefix, passes,
                             "Move or swap with a Zombie token", "Do neither",
                             (MortimerGreenFaces.MoveBullet(MortimerGreenFaces.DivePrefix, 0), MoveZombieRange),
                             (MortimerGreenFaces.SwapBullet(0), "Swap with a Zombie token in range")))
                    yield return row;
            }

            foreach (var (face, passes) in new[]
                     {
                         (MortimerGreenFaces.GatheringHorde, 2), (MortimerGreenFaces.ArmyOfDarkness, 3)
                     })
            {
                foreach (var row in Passes(
                             face, MortimerGreenFaces.HordePrefix, passes,
                             "Move a Zombie token or replace a minion", "Do neither",
                             (MortimerGreenFaces.MoveBullet(MortimerGreenFaces.HordePrefix, 0), MoveZombieRange),
                             (MortimerGreenFaces.ReplaceBullet(0), "Replace an enemy minion beside two Zombies with one")))
                    yield return row;
            }
        }

        /// <summary>
        /// Every pass a face prints (and the two Master of Puppets raises on a three-pass face): the
        /// wrapper step - absent on a bare "Choose one" - and the two bullets. The bullet node names are
        /// given for pass 0 and re-numbered per pass, because the content mints them by pass.
        /// </summary>
        private static IEnumerable<DecisionLabelDeclaration> Passes(
            CardDefinitionId face, string prefix, int printedPasses, string chooseOnce, string chooseNothing,
            (string Node, string Label) first, (string Node, string Label) second)
        {
            for (var pass = 1; pass <= Count(printedPasses); pass++)
            {
                if (printedPasses > 1)
                {
                    yield return pass == 1
                        ? DecisionStep(face, MortimerRepeatedChoice.UpToNode(prefix, pass), chooseOnce, chooseNothing)
                        : DecisionStep(face, MortimerRepeatedChoice.UpToNode(prefix, pass), chooseOnce + " again", "Stop here");
                }

                yield return DecisionArm(face, Renumber(first.Node, pass), first.Label);
                yield return DecisionArm(face, Renumber(second.Node, pass), second.Label);
            }
        }

        private static int Count(int printedPasses) =>
            printedPasses == MortimerRepeatedChoice.PrintedMostPasses
                ? MortimerRepeatedChoice.MasterOfPuppetsPasses
                : printedPasses;

        /// <summary>"knife-0-move" for pass 3 is "knife-3-move": the pass number is the middle segment.</summary>
        private static string Renumber(string passZeroNode, int pass) => passZeroNode.Replace("-0-", "-" + pass + "-");
    }
}
