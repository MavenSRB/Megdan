using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.83. THE ROSTER-WIDE SWEEP for the two shapes milestone 8.82 found on ONE card,
    /// run over every hero's composed content offline and turned into a standing rule.
    ///
    /// <para><b>Why it exists.</b> 8.82 closed a defect a player reported in five words - "Swift is
    /// not being offered blue minion to move" - and the block that closed it routed two more of the
    /// same shape on two other faces. The user's own words for what that means are "every time we play
    /// i find new broken cards"; a per-card fix answers the card, and only a sweep answers the
    /// sentence. So the two shapes are stated here as claims about the WHOLE corpus, checked from the
    /// composed definitions rather than from anybody's memory of which cards do what.</para>
    ///
    /// <para><b>What it can see and what it cannot.</b> The composition is data and the printed text
    /// is not, so every rule below is a claim about DECLARATIONS - which member, which policy, which
    /// literal - held against an allow-list whose entries each quote the clause that justifies them.
    /// That is deliberate: a card that prints an exact distance is not a defect and never will be, so
    /// the rule cannot be "no exact distances". What the rule really says is "every exact distance in
    /// this roster has been read off a printed card and written down here", which is a claim a later
    /// milestone cannot satisfy by accident.</para>
    ///
    /// <para><b>Unity-independent</b>, so it runs in the cross-platform harness as well as the Windows
    /// tier - the same reason <see cref="OptionalDistanceConformanceTests"/> is.</para>
    /// </summary>
    public sealed class RosterMoveLeashSweepTests
    {
        // -------------------------------------------------------------------------------------------
        // (a) and (b): the two ways a card says "to a space in radius"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// MILESTONE 8.82's CAUSE, stated as a rule. `DirectMoveDestinations.Admit` reads a radius
        /// policy with no radius as admitting nothing - "an undeclared bound is not an absent bound" -
        /// so a node that declares the DECLARED member and binds it no number has no destination on
        /// any board, and a selection that declares such a node as its consumer offers nobody at all.
        /// That is exactly what Mark for Death did, on every board, from the milestone that composed
        /// it until the milestone that fixed it.
        /// </summary>
        [Test]
        public void EveryDeclaredRadiusLeash_BindsTheNumberItsMemberIsFor()
        {
            var unbound = WholeCorpus()
                .SelectMany(entry => entry.Definition.Nodes.Select(node => (entry.Owner, entry.Definition, Node: node)))
                .Where(row => Literal(
                        row.Node,
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName) ==
                    (int)DirectMoveDestinationPolicy.WithinActorRadius)
                .Where(row => Literal(
                    row.Node, SyntheticDisplacementCapabilities.ActorRadiusParameterName) == null)
                .Select(row => $"{row.Owner} | {row.Definition.Id.Value} | {row.Node.Id.Value}")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(unbound, Is.Empty,
                "A node declaring DirectMoveDestinationPolicy.WithinActorRadius and binding it no " +
                "actorRadius admits NO destination anywhere, and narrows its selection's offer to " +
                "nobody through the consumer declaration. If the sentence writes the number down, " +
                "bind it; if it says \"in radius\", declare WithinActorPrintedRadius instead.");
        }

        /// <summary>
        /// (b) THE SHAPE SABINA HAD. Two members draw the same disk and differ only in where its width
        /// comes from: <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> takes the number the
        /// SENTENCE writes down, and <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/>
        /// reads the card's own Radius ICON with p.16's items composed onto it. p.9 settles which a
        /// given sentence wants - "if the card text mentions Range/Radius, it refers to this value" -
        /// so a face that says "to a space in radius" and declares a literal equal to its own printed
        /// icon is stating the right number for the wrong reason, and stops being right the moment the
        /// hero holds a Radius item.
        ///
        /// <para>Her three Green command faces were exactly that until 8.83, and the pair came apart
        /// one ring wide: the SELECTION above the leash is a printed-icon arm, so an item widened who
        /// she could command and not where she could put them.</para>
        /// </summary>
        [Test]
        public void TheDeclaredRadiusLeash_IsBoundOnlyWhereTheSentenceWritesTheNumberDown()
        {
            var declared = WholeCorpus()
                .SelectMany(entry => entry.Definition.Nodes.Select(node => (entry.Owner, entry.Definition, Node: node)))
                .Where(row => Literal(
                        row.Node,
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName) ==
                    (int)DirectMoveDestinationPolicy.WithinActorRadius)
                .Select(row =>
                    $"{row.Definition.Id.Value} | {row.Node.Id.Value} | " +
                    Literal(row.Node, SyntheticDisplacementCapabilities.ActorRadiusParameterName))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.EqualTo(new[]
            {
                // Xargatha's Siren's Call - "if able, move the target up to 3 spaces TO A SPACE
                // ADJACENT TO YOU". The bound is a number the sentence writes down, and her face
                // prints a Range icon and NO Radius icon at all, so there is nothing for p.16's Radius
                // item to raise and the printed member would admit nothing. This is the member's
                // correct use and the reason both members exist.
                "xargatha-sirens-call-skill | siren-move | 1"
            }), "Every declared-radius leash in the roster is a sentence that writes its number down. " +
                "A new row here is either such a sentence - add it with the clause quoted - or a face " +
                "that says \"in radius\" and should declare WithinActorPrintedRadius, which is the " +
                "defect milestone 8.83 corrected on Sabina's three Green command faces.");
        }

        // -------------------------------------------------------------------------------------------
        // (c): "up to N spaces" against a declaration that names an exact number
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// (c) THE SHAPE SWIFT, XARGATHA AND TIGERCLAW HAD. Rulebook p.11 separates two wordings in so
        /// many words: "unless the distance is optional ('up to 2 spaces'), or the move itself is
        /// optional ('you may move'), you must move the exact number of spaces". Only
        /// <see cref="EffectFailurePolicy.UpTo"/> makes the DISTANCE the decision -
        /// <c>AdmittedDistances.ForDeclared</c> is the one statement of it, and it reads `may`,
        /// `if able` and `Mandatory` identically, as the declared number and nothing else.
        ///
        /// <para><b>Two declarations are outside the rule and neither is an exception to it.</b> A
        /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/> move is read as `up to` whatever its
        /// own policy says, because milestone 8.21 made the declared number a BUDGET for reaching one
        /// named space rather than a count of steps - Brogan's charges are why. And a node whose
        /// distance is not a literal at all took it from another node, so there is no printed number
        /// here to judge.</para>
        ///
        /// <para><b>Every other exact distance in the roster is listed below with the clause that
        /// prints it.</b> Three families were NOT in that list when 8.83 measured it, and all three
        /// are corrected in the same commit: Swift's three jump Skills ("Push ... UP TO 1 / 2 spaces",
        /// declared `may`, which also forced Drop Trooper's "up to TWO enemy units" to be both),
        /// Xargatha's Charm, Control and Dominate ("you may move an enemy minion in radius UP TO 2
        /// spaces", declared `Mandatory`), and Tigerclaw's Master Thief ("you may move UP TO 2
        /// spaces", declared `may`).</para>
        /// </summary>
        [Test]
        public void EveryExactDisplacementDistance_IsASentenceThatPrintsAnExactNumber()
        {
            var measured = ExactDistanceDeclarations()
                .Select(row => row.Key)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(measured, Is.EqualTo(PrintedExactDistances.Keys
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()),
                "A displacement node declares an exact distance that this roster's printed text does " +
                "not account for, or an accounted-for one has gone. Read the card: a sentence that " +
                "says \"up to N spaces\" declares EffectFailurePolicy.UpTo, and a sentence that names " +
                "the number declares the number and is added below with that clause quoted.\n" +
                "Measured:\n" + string.Join("\n", ExactDistanceDeclarations()
                    .Select(row => $"  {row.Key} -> {row.Value}")));
        }

        /// <summary>
        /// The rule above compares two sets and would be inert if either could go empty unnoticed, and
        /// it would be a rule about nothing if the corpus it reads were empty. Both are measured here
        /// rather than assumed, which is <see cref="OptionalDistanceConformanceTests"/>'s own
        /// "the subject set is the declared one and is not empty".
        /// </summary>
        [Test]
        public void TheSweptCorpusAndTheAllowList_AreBothNonEmpty()
        {
            Assert.That(WholeCorpus().Count(), Is.GreaterThan(300),
                "The sweep has to have a corpus to walk.");
            Assert.That(PrintedExactDistances, Is.Not.Empty,
                "An empty allow-list would make the rule above assert that no card in the game names " +
                "a distance, which is false of the first card anybody reads.");
            Assert.That(DistanceCapabilityIds(), Is.EquivalentTo(new[]
            {
                "move-adjacent-to-enemy-hero-for-push",
                "synthetic-direct-move",
                "synthetic-object-push",
                "synthetic-push-target"
            }), "The capabilities that move something a stated number of spaces changed. A new one " +
                "joins this sweep by declaring an integer `distance`, exactly as it joins " +
                "OptionalDistanceConformanceTests - and its cards owe rows below.");
        }

        /// <summary>
        /// The corpus below is enumerated hero by hero, by name, so a hero without a line is a hero
        /// nothing sweeps. This case derives what the enumeration owes from the verified content
        /// classes the build actually contains, exactly as
        /// <c>AttackTargetBindingConditionalChooseOfferSweepTests</c> does.
        /// </summary>
        [Test]
        public void TheSweptCorpus_CoversEveryVerifiedContentClassTheBuildContains()
        {
            var declared = typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .Select(type => type.Name.Substring(0, type.Name.Length - "CardContent".Length)
                    .ToLowerInvariant())
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.Not.Empty, "The reflection must find the content classes at all.");
            Assert.That(
                WholeCorpus().Select(entry => entry.Owner).Distinct()
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                Is.EquivalentTo(declared),
                "Every verified hero's corpus is swept, and nothing is swept that is not one.");
        }

        /// <summary>
        /// The inventory this milestone measured the roster from, kept so the next one can re-measure
        /// instead of re-deriving. It asserts nothing; the four cases above are what assert.
        /// </summary>
        [Test, Explicit("Offline inventory printer - run it and read stdout.")]
        public void PrintEveryLeashAndDistanceDeclarationInTheRoster()
        {
            foreach (var (owner, definition) in WholeCorpus())
            {
                foreach (var node in definition.Nodes)
                {
                    var policy = Literal(
                        node, SyntheticDisplacementCapabilities.DestinationPolicyParameterName);
                    var distance = Literal(node, "distance");
                    if (policy == null && distance == null)
                    {
                        continue;
                    }

                    TestContext.WriteLine(
                        $"{owner} | {definition.Id.Value} | {node.Id.Value} | {node.FailurePolicy} | " +
                        $"{node.Capability} | " +
                        $"policy={(policy.HasValue ? ((DirectMoveDestinationPolicy)policy.Value).ToString() : "-")} | " +
                        $"actorRadius={Show(Literal(node, SyntheticDisplacementCapabilities.ActorRadiusParameterName))} | " +
                        $"distance={Show(distance)} | " +
                        $"least={Show(Literal(node, SyntheticDisplacementCapabilities.LeastDistanceParameterName))} | " +
                        $"maxTargets={Show(Literal(node, SyntheticDisplacementCapabilities.MaximumTargetsParameterName))}");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // The allow-list: every exact distance this roster prints, with the clause that prints it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Keyed by <c>definition | node</c>, valued by the printed clause that names the number. A
        /// row here is a claim that somebody read the card, so the value is the card's own words and
        /// never a description of the node.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> PrintedExactDistances =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["arien-noble-blade-attack-text | noble-move"] =
                    "Noble Blade: \"You may move another unit that is adjacent to the target 1 space\"",

                ["brogan-brutal-jab-skill | shove-move"] =
                    "Brutal Jab: \"You may move 1 space\" (its PUSH prints \"up to 1 space\" and declares UpTo)",

                ["brynn-cover-tracks-skill | tracking-swap-move"] =
                    "Cover Tracks: \"You may move 1 space\"",
                ["brynn-decoy-skill | decoy-1-minion-move"] =
                    "Decoy: \"Move an enemy minion in radius 1 space\"",
                ["brynn-decoy-skill | decoy-1-hero-move"] =
                    "Decoy: \"Move an enemy hero in radius who is adjacent to 3 or more obstacles 1 space\"",
                ["brynn-decoy-skill | decoy-2-after-minion-minion-move"] =
                    "Decoy, second pass: the same two bullets, \"1 space\"",
                ["brynn-decoy-skill | decoy-2-after-minion-hero-move"] =
                    "Decoy, second pass: the same two bullets, \"1 space\"",
                ["brynn-decoy-skill | decoy-2-after-hero-minion-move"] =
                    "Decoy, second pass: the same two bullets, \"1 space\"",
                ["brynn-decoy-skill | decoy-2-after-hero-hero-move"] =
                    "Decoy, second pass: the same two bullets, \"1 space\"",

                ["dodger-burning-skull-attack-text | shove-move"] =
                    "Burning Skull: \"Move up to 1 minion adjacent to you 1 SPACE\" - the \"up to\" is the COUNT",
                ["dodger-blazing-skull-attack-text | shove-move"] =
                    "Blazing Skull: \"Move each of up to 2 minions adjacent to you 1 SPACE\"",
                ["dodger-blazing-skull-attack-text | shove-move-second"] =
                    "Blazing Skull: the second of the two minions, at the same \"1 space\"",

                ["garrus-blunt-force-attack-text | push"] =
                    "Blunt Force: \"push that hero 3 spaces ignoring obstacles\"",
                ["garrus-send-flying-attack-text | push"] =
                    "Send Flying: \"push that hero 3 spaces ignoring obstacles\"",
                ["garrus-menace-skill | unit-move"] =
                    "Menace: \"Move an enemy unit in range 1 space to a space farther away from you\"",
                ["garrus-threaten-skill | unit-move"] =
                    "Threaten: \"Move an enemy unit in range 1 space to a space farther away from you\"",
                ["garrus-terrify-skill | unit-move"] =
                    "Terrify: \"Move an enemy unit in range 1 space to a space farther away from you\"",
                ["garrus-form-up-skill | unit-move"] =
                    "Form Up!: \"Move a friendly unit in range 1 space to a space closer to you\"",
                ["garrus-testudo-skill | unit-move"] =
                    "Testudo!: \"Move a friendly unit in range 1 space to a space closer to you\"",
                ["garrus-light-pilum-skill | move"] =
                    "Light Pilum: \"You may move 1 space\"",

                ["ignatia-chaos-gate-text | ignatia-gate-sun-move"] =
                    "Chaos Gate (sun): \"you may move that unit 1 space\"",
                ["ignatia-chaos-gate-text | ignatia-gate-moon-move"] =
                    "Chaos Gate (moon): \"you may move 1 space\"",

                ["min-tiger-stance-attack-text | tiger-move-step"] =
                    "Tiger Stance: \"You may move 1 space to a space adjacent to the target\"",

                ["misa-power-shot-attack-text | shove-move"] =
                    "Power Shot: \"you may move it 1 space, to a space farther away from you\"",
                ["misa-thunder-shot-attack-text | shove-move"] =
                    "Thunder Shot: \"you may move it 1 space, to a space farther away from you\"",

                ["mrak-stomping-step-skill | stomp-move"] =
                    "Stomping Step: \"Move a unit in radius which is adjacent to terrain, or to a rock token, 1 space\"",
                ["mrak-ground-shaker-skill | stomp-move"] =
                    "Ground Shaker: the same \"1 space\", May repeat once on a different target",
                ["mrak-treacherous-ground-skill | shift-move"] =
                    "Treacherous Ground: \"You may move a unit in range 1 space to a space adjacent to terrain, or a rock token\"",
                ["mrak-rockslide-skill | shift-move"] =
                    "Rockslide: the same \"1 space\" at Range 4",
                ["mrak-avalanche-skill | shift-move"] =
                    "Avalanche: the same \"1 space\", May repeat once",

                ["mortimer-knife-of-the-living-dead-attack-text | knife-1-move-step"] =
                    "Knife of the Living Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-knife-of-the-living-dead-attack-text | knife-2-move-step"] =
                    "Knife of the Living Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-knife-of-the-living-dead-attack-text | knife-3-move-step"] =
                    "Knife of the Living Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-knife-of-the-living-dead-attack-text | knife-4-move-step"] =
                    "Knife of the Living Dead: \"Move a Zombie token in radius 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-knife-of-the-living-dead-attack-text | knife-5-move-step"] =
                    "Knife of the Living Dead: \"Move a Zombie token in radius 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-crawling-dead-attack-text | dead-1-move-step"] =
                    "Crawling Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-walking-dead-attack-text | dead-1-move-step"] =
                    "Walking Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-walking-dead-attack-text | dead-2-move-step"] =
                    "Walking Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-braains-attack-text | brains-1-move-step"] =
                    "Braains...!: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-braains-attack-text | brains-2-move-step"] =
                    "Braains...!: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-racing-dead-attack-text | dead-1-move-step"] =
                    "Racing Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-racing-dead-attack-text | dead-2-move-step"] =
                    "Racing Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-racing-dead-attack-text | dead-3-move-step"] =
                    "Racing Dead: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-racing-dead-attack-text | dead-4-move-step"] =
                    "Racing Dead: \"Move a Zombie token in radius 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-racing-dead-attack-text | dead-5-move-step"] =
                    "Racing Dead: \"Move a Zombie token in radius 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-braaaaaaains-attack-text | brains-1-move-step"] =
                    "Braaaaaaains...!: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-braaaaaaains-attack-text | brains-2-move-step"] =
                    "Braaaaaaains...!: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-braaaaaaains-attack-text | brains-3-move-step"] =
                    "Braaaaaaains...!: \"Move a Zombie token in radius 1 space\"",
                ["mortimer-braaaaaaains-attack-text | brains-4-move-step"] =
                    "Braaaaaaains...!: \"Move a Zombie token in radius 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-braaaaaaains-attack-text | brains-5-move-step"] =
                    "Braaaaaaains...!: \"Move a Zombie token in radius 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-corpse-slam-skill | slam-1-push"] =
                    "Corpse Slam: \"that Zombie token may push a unit or a token adjacent to it 1 space\"",
                ["mortimer-corpse-slam-skill | slam-1-step-move"] =
                    "Corpse Slam: \"Move 1 space\"",
                ["mortimer-morbid-mosh-skill | slam-1-push"] =
                    "Morbid Mosh: \"that Zombie token may push a unit or a token adjacent to it 1 space\"",
                ["mortimer-morbid-mosh-skill | slam-1-step-move"] =
                    "Morbid Mosh: \"Move 1 space\"",
                ["mortimer-morbid-mosh-skill | slam-2-push"] =
                    "Morbid Mosh: \"that Zombie token may push a unit or a token adjacent to it 1 space\"",
                ["mortimer-morbid-mosh-skill | slam-2-step-move"] =
                    "Morbid Mosh: \"Move 1 space\"",
                ["mortimer-robbing-zombies-skill | rob-1-step-move"] =
                    "Robbing Zombies: \"Move 1 space\"",
                ["mortimer-robbing-zombies-skill | rob-2-step-move"] =
                    "Robbing Zombies: \"Move 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-1-push"] =
                    "Macabre Mayhem: \"that Zombie token may push a unit or a token adjacent to it 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-1-step-move"] =
                    "Macabre Mayhem: \"Move 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-2-push"] =
                    "Macabre Mayhem: \"that Zombie token may push a unit or a token adjacent to it 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-2-step-move"] =
                    "Macabre Mayhem: \"Move 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-3-push"] =
                    "Macabre Mayhem: \"that Zombie token may push a unit or a token adjacent to it 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-3-step-move"] =
                    "Macabre Mayhem: \"Move 1 space\"",
                ["mortimer-macabre-mayhem-skill | slam-4-push"] =
                    "Macabre Mayhem: \"that Zombie token may push a unit or a token adjacent to it 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-macabre-mayhem-skill | slam-4-step-move"] =
                    "Macabre Mayhem: \"Move 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-macabre-mayhem-skill | slam-5-push"] =
                    "Macabre Mayhem: \"that Zombie token may push a unit or a token adjacent to it 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-macabre-mayhem-skill | slam-5-step-move"] =
                    "Macabre Mayhem: \"Move 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-stalking-scalpers-skill | rob-1-step-move"] =
                    "Stalking Scalpers: \"Move 1 space\"",
                ["mortimer-stalking-scalpers-skill | rob-2-step-move"] =
                    "Stalking Scalpers: \"Move 1 space\"",
                ["mortimer-stalking-scalpers-skill | rob-3-step-move"] =
                    "Stalking Scalpers: \"Move 1 space\"",
                ["mortimer-stalking-scalpers-skill | rob-4-step-move"] =
                    "Stalking Scalpers: \"Move 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-stalking-scalpers-skill | rob-5-step-move"] =
                    "Stalking Scalpers: \"Move 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-stage-dive-skill | dive-1-move-step"] =
                    "Stage Dive: \"Move a Zombie token in range 1 space\"",
                ["mortimer-crowd-drift-skill | dive-1-move-step"] =
                    "Crowd Drift: \"Move a Zombie token in range 1 space\"",
                ["mortimer-crowd-drift-skill | dive-2-move-step"] =
                    "Crowd Drift: \"Move a Zombie token in range 1 space\"",
                ["mortimer-gathering-horde-skill | horde-1-move-step"] =
                    "Gathering Horde: \"Move a Zombie token in range 1 space\"",
                ["mortimer-gathering-horde-skill | horde-2-move-step"] =
                    "Gathering Horde: \"Move a Zombie token in range 1 space\"",
                ["mortimer-crowd-surf-skill | dive-1-move-step"] =
                    "Crowd Surf: \"Move a Zombie token in range 1 space\"",
                ["mortimer-crowd-surf-skill | dive-2-move-step"] =
                    "Crowd Surf: \"Move a Zombie token in range 1 space\"",
                ["mortimer-crowd-surf-skill | dive-3-move-step"] =
                    "Crowd Surf: \"Move a Zombie token in range 1 space\"",
                ["mortimer-crowd-surf-skill | dive-4-move-step"] =
                    "Crowd Surf: \"Move a Zombie token in range 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-crowd-surf-skill | dive-5-move-step"] =
                    "Crowd Surf: \"Move a Zombie token in range 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-army-of-darkness-skill | horde-1-move-step"] =
                    "Army of Darkness: \"Move a Zombie token in range 1 space\"",
                ["mortimer-army-of-darkness-skill | horde-2-move-step"] =
                    "Army of Darkness: \"Move a Zombie token in range 1 space\"",
                ["mortimer-army-of-darkness-skill | horde-3-move-step"] =
                    "Army of Darkness: \"Move a Zombie token in range 1 space\"",
                ["mortimer-army-of-darkness-skill | horde-4-move-step"] =
                    "Army of Darkness: \"Move a Zombie token in range 1 space\" (Master of Puppets' extra pass)",
                ["mortimer-army-of-darkness-skill | horde-5-move-step"] =
                    "Army of Darkness: \"Move a Zombie token in range 1 space\" (Master of Puppets' extra pass)",

                ["rowenna-throw-the-gauntlet-skill | gauntlet-move"] =
                    "Throw the Gauntlet: \"that hero may move 1 space\"",

                // Milestone 9.21f. Razzle's two exact moves of another body; the four siblings print
                // "up to 2 / up to 3 spaces" and declare UpTo, and the retreat's distance is the push's
                // realised count, bound from a node rather than a literal.
                ["razzle-alleyoop-skill-text | razzle-alleyoop-other-body-move"] =
                    "Alleyoop: \"Move one other you 1 space\" (Group Performance and Team Spirit print \"up to 2 / 3 spaces\")",
                ["razzle-tightrope-movement-text | razzle-tightrope-other-body-move"] =
                    "Tightrope: \"you may move another one of you 1 space\" (High Wire and Wire Dancers print \"up to 2 / 3 spaces\")",

                ["sabina-point-blank-shot-attack-text | shot-push"] =
                    "Point Blank Shot: \"If the target is adjacent to you, push it 1 space\"",
                ["sabina-troop-movement-skill | troop-move"] =
                    "Troop Movement: \"Move a friendly minion in radius 1 space, to a space in radius\"",
                ["sabina-marching-orders-skill | troop-move"] =
                    "Marching Orders: \"Move a friendly minion in radius 1 space, to a space in radius\"",
                ["sabina-path-to-victory-skill | troop-move"] =
                    "Path to Victory: \"Move a friendly minion in radius 1 space, to a space in radius\"",

                ["silverarrow-disorient-skill | disorient-shove"] =
                    "Disorient: \"Move an enemy unit adjacent to you 1 space\"",
                ["silverarrow-disorient-skill | disorient-step"] =
                    "Disorient: \"if you do, you may move 1 space\"",

                ["tigerclaw-hit-and-run-attack-text | after-move"] =
                    "Hit and Run: \"After the attack: You may move 1 space\"",
                ["tigerclaw-combat-reflexes-attack-text | before-move"] =
                    "Combat Reflexes: \"Before the attack: You may move 1 space\"",
                ["tigerclaw-combat-reflexes-after-attack-text | reflexes-move"] =
                    "Combat Reflexes: \"If you did not move before the attack, you may move 1 space\"",
                ["tigerclaw-leaping-strike-attack-text | before-move"] =
                    "Leaping Strike: \"Before the attack: You may move 1 space\"",
                ["tigerclaw-leaping-strike-after-attack-text | after-move"] =
                    "Leaping Strike: \"After the attack: You may move 1 space\"",
                ["tigerclaw-sidestep-defense-text | sidestep-move"] =
                    "Sidestep: \"Block a ranged attack. You may move 1 space\"",
                ["tigerclaw-evade-defense-text | evade-move"] =
                    "Evade: \"Block a ranged attack. You may move 1 space\"",
                // Milestone 9.2: two rows the sweep could not see until it walked Active-effect
                // trigger bodies through PrintedWordingCorpus.
                ["arien-living-tsunami-effect | tsunami-move"] =
                    "Living Tsunami: \"before performing an Attack action, you may move 1 space\"",
                ["misa-focus-step-move | focus-step-move"] =
                    "Focus: \"Before you perform a primary action, you may move 1 space\"",
                ["tigerclaw-light-fingered-skill | theft-opening-move"] =
                    "Light-Fingered: \"You may move 1 space\"",
                ["tigerclaw-light-fingered-skill | theft-follow-up-move"] =
                    "Light-Fingered: \"if you do, you may move 1 space\"",
                ["tigerclaw-pick-pocket-skill | theft-follow-up-move"] =
                    "Pick Pocket: \"if you do, you may move 1 space\" (its OPENING prints \"up to 2 spaces\")",

                ["wasp-lift-up-skill | orbit-move"] =
                    "Lift Up: \"Move a unit, or a token, in radius 1 space, without moving it away from you or closer to you\"",
                ["wasp-control-gravity-skill | orbit-move"] =
                    "Control Gravity: the same sentence, \"1 space\"",
                ["wasp-center-of-mass-skill | orbit-move"] =
                    "Center of Mass: the same sentence, \"1 space\"",

                ["whisper-seeds-of-fear-skill | spawn-move"] =
                    "Seeds of Fear: \"Move that unit 1 space\" (its two siblings print \"up to 2 / 3 spaces\")",

                ["widget-safe-landing-skill | move-pyro"] =
                    "Safe Landing: \"You may move Pyro in range 1 space\""
            };

        // -------------------------------------------------------------------------------------------
        // The measurement
        // -------------------------------------------------------------------------------------------

        private static IEnumerable<KeyValuePair<string, string>> ExactDistanceDeclarations()
        {
            var distanceCapabilities = DistanceCapabilityIds();
            foreach (var (owner, definition) in WholeCorpus())
            {
                foreach (var node in definition.Nodes)
                {
                    if (!node.Capability.IsDefined ||
                        !distanceCapabilities.Contains(node.Capability.CapabilityId.Value) ||
                        node.FailurePolicy == EffectFailurePolicy.UpTo)
                    {
                        continue;
                    }

                    var distance = Literal(node, "distance");
                    if (distance == null)
                    {
                        // The number came from another node, so this declaration prints none.
                        continue;
                    }

                    // Milestone 8.21's budget reading: a named destination makes the declared number
                    // the most this move may spend reaching one space the text already picked out, and
                    // the handler reads such a node as `up to` whatever its own policy says.
                    if (Literal(node, SyntheticDisplacementCapabilities.DestinationPolicyParameterName)
                        == (int)DirectMoveDestinationPolicy.NamedSpace)
                    {
                        continue;
                    }

                    yield return new KeyValuePair<string, string>(
                        $"{definition.Id.Value} | {node.Id.Value}",
                        $"{owner}, {node.FailurePolicy}, distance {distance}");
                }
            }
        }

        private static HashSet<string> DistanceCapabilityIds()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();
            return new HashSet<string>(
                catalog.Registered
                    .Where(reference => catalog.Load(reference).Schema.Parameters.Any(parameter =>
                        string.Equals(parameter.Name, "distance", StringComparison.Ordinal) &&
                        parameter.ValueKind == EffectValueKind.Integer))
                    .Select(reference => reference.CapabilityId.Value),
                StringComparer.Ordinal);
        }

        /// <summary>The integer a node binds to <paramref name="parameterName"/>, or null where it binds none.</summary>
        private static int? Literal(EffectNodeDefinition node, string parameterName)
        {
            if (node.Inputs == null)
            {
                return null;
            }

            foreach (var input in node.Inputs)
            {
                if (string.Equals(input.ParameterName, parameterName, StringComparison.Ordinal) &&
                    input.SourceKind == EffectBindingSourceKind.Literal &&
                    input.Literal.Kind == EffectValueKind.Integer)
                {
                    return input.Literal.IntegerValue;
                }
            }

            return null;
        }

        private static string Show(int? value) => value.HasValue ? value.Value.ToString() : "-";

        // -------------------------------------------------------------------------------------------
        // The corpus - every hero's composed content, dealt or not. One hero, one AllDefinitions call.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 9.2. Every body every dealt face runs, from the roster-derived
        /// <see cref="PrintedWordingCorpus"/> - which walks Active-effect trigger bodies too, and takes
        /// in a newly dealt hero without anybody adding a line here (8.83 listed its heroes by hand).
        ///
        /// <para>Milestone 9.8a. A hero being composed but not yet dealt (Mortimer until 9.8f) is on
        /// no roster, so his content class's own faces join here - the sweep reads his bodies from
        /// his first block rather than from his dealing.</para>
        /// </summary>
        private static IEnumerable<(string Owner, EffectDefinition Definition)> WholeCorpus()
        {
            var dealt = PrintedWordingCorpus.All
                .SelectMany(face => face.Bodies.Select(body => (Owner: face.Hero, Body: body)))
                .ToArray();
            var dealtOwners = new HashSet<string>(dealt.Select(row => row.Owner), StringComparer.Ordinal);
            var undealt = VerifiedContentCorpus.ContentClasses()
                .Where(type => !dealtOwners.Contains(VerifiedContentCorpus.OwnerOf(type)))
                .SelectMany(type => VerifiedContentCorpus.AllDefinitionsOf(type)
                    .SelectMany(card => card.ActionDefinitions)
                    .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                    .Where(body => body != null)
                    .Select(body => (Owner: VerifiedContentCorpus.OwnerOf(type), Body: body)));
            return dealt.Concat(undealt)
                .GroupBy(row => row.Body.Reference)
                .Select(group => (group.First().Owner, group.First().Body));
        }
    }
}
