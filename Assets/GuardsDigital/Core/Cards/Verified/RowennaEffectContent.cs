using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The effect bodies Rowenna the Vanguard's printed card text composes to - milestones 8.57a
    /// through 8.57e, the five vocabulary blocks of the eighteenth hero.
    ///
    /// <para><b>Every body her set names is here, and seventeen of her nineteen faces attach one.</b>
    /// 8.57a owns six - the one that hands a retrieve to the hero she just targeted, and the five Reds
    /// that pay or heal a team-mate after the attack and may then repeat. 8.57b adds four: the three
    /// Blue faces that swap with a friendly unit, and her Silver basic Throw the Gauntlet. 8.57c adds
    /// the two Blue faces that defeat a surrendering enemy. 8.57d adds the five Greens - three that
    /// place a minion after her walk and two that empty an enemy's hand by one card. 8.57e adds her
    /// Purple Ultimate, which is a typed rule rather than an action body. The two faces that attach
    /// nothing are that Ultimate - p.16's permanent ability prints no action at all - and p.20's boxed
    /// Earn the Spurs, whose whole sentence stands under the established odd-player deferral. 8.57f
    /// composed the set around these bodies and minted the revision they are all built under.
    /// Nothing here is registered: <see cref="MatchContentCatalogs"/> names no Rowenna content until
    /// 8.57g deals her, which is milestone 7.9's gate - a registration no dealt card can start moves
    /// the pinned registration list, every snapshot's bytes and every corpus digest while the content
    /// never moved.</para>
    ///
    /// <para><b>What 8.57d costs the shared vocabulary is one DECLARATION and one PARAMETER.</b>
    /// <c>synthetic-place-in-radius-beside-a-named-hero@1</c> is the printed-Radius twin of 8.57b's
    /// declaration over the <c>PlaceHandler</c> type three arms have registered since milestone 6.6,
    /// held out of the registered set until she is dealt; and
    /// <c>synthetic-radius-hero-target@1</c> gains
    /// <see cref="SyntheticTargetingCapabilities.BothDiscardPilesEmptyParameterName"/>, the negative
    /// complement of <c>hasCardInDiscard</c> asked of two heroes at once. The parameter is optional, so
    /// every body composed before this milestone offers exactly what it did, and the declaration is a
    /// further reference to a registered handler TYPE rather than a new one. Everything else these five
    /// faces name was built milestones ago: the after-Movement timing is 8.35z31's, the adjacency is
    /// <c>presence-count@1</c>, the modal is a <see cref="EffectNodeKind.Choose"/>, the minion
    /// selections are <c>synthetic-radius-minion-target@1</c>, bullet 2's destination is Wasp's
    /// Telekinesis' own <c>synthetic-place@2</c>, the forced discard is
    /// <c>synthetic-discard-chosen-card@1</c>, and the repeat is p.10's.</para>
    ///
    /// <para><b>What 8.57c costs the shared vocabulary is one PARAMETER on a selection, one parameter
    /// and one OUTPUT on the Defeat, and one argument at rulebook p.13's single spend site.</b>
    /// <c>choose-target@2</c> gains
    /// <see cref="SyntheticRosterCapabilities.RequiresEmptyHandParameter"/> - the complement of
    /// milestone 8.40f's <c>minimumCardsInHand</c>, carried under that family's own name;
    /// <c>synthetic-defeat-hero@1</c> gains
    /// <see cref="SyntheticConsequenceCapabilities.AssistCoinMultiplierParameter"/> and publishes it
    /// back for a node that declared it, so the multiplier rides to the spend site the way the request
    /// itself does. All three are optional, so every body composed before this milestone offers,
    /// publishes and prices exactly what it did. No new capability and no new handler type.</para>
    ///
    /// <para><b>What 8.57b costs the shared vocabulary is one DECLARATION, one PARAMETER on each of two
    /// registered capabilities, and one OUTPUT.</b> <c>synthetic-place-in-range-beside-a-named-hero@1</c>
    /// is a further declaration over the <c>PlaceHandler</c> type three arms have registered since
    /// milestone 6.6, held out of the registered set until she is dealt;
    /// <c>synthetic-range-unit-target@1</c> gains the one clause her Blue faces print about their
    /// candidates; and <c>synthetic-direct-move@3</c> gains a mover-chooser and the Boolean form of the
    /// success it already reports, which is
    /// <see cref="SyntheticRosterCapabilities.DidRetrieveOutputName"/>'s and
    /// <see cref="SyntheticRosterCapabilities.DidGrantOutputName"/>'s third twin. Every one of them is
    /// optional, so each body composed before this milestone offers and publishes exactly what it
    /// did.</para>
    ///
    /// <para><b>What these five faces cost the shared vocabulary is two OUTPUTS and no new
    /// capability.</b> <c>synthetic-attack-took-a-hero@1</c> gains
    /// <see cref="SyntheticConsequenceCapabilities.AttackTargetHeroOutput"/> - the same fact it already
    /// answers as a Boolean, published as the hero itself, because Code of Chivalry is the first face
    /// in the corpus whose consequence is about the Attack's target rather than merely conditioned on
    /// it. <c>gain-resource@1</c> gains
    /// <see cref="SyntheticRosterCapabilities.DidGrantOutputName"/>, which is milestone 8.40i's
    /// <c>didRetrieve</c> for coins and exists for the reason that output's remarks state. Everything
    /// else these bodies name was built years of milestones ago: the retrieval with its own owner and
    /// chooser is <c>retrieve-card@1</c>, the friendly hero inside the printed Radius is
    /// <c>synthetic-radius-hero-target@1</c> with <see cref="SourceTeamRelation.AlliedWithSource"/>,
    /// the condition is <c>synthetic-is-flagged@1</c> over a <see cref="EffectNodeKind.Conditional"/>,
    /// and "repeat once on a different target" is p.10's repeat with milestone 8.10's
    /// <see cref="RepeatTargetBinding.DifferentTarget"/> - Wasp's Thunder Boomerang's exact
    /// declaration.</para>
    ///
    /// <para><b>p.10's retrieval rule is the whole reading of "may retrieve".</b> "You can never
    /// retrieve another hero's cards", so every retrieve here names ONE hero as both the
    /// <c>owner</c> and the <c>chooser</c> - the card comes out of that hero's own discard pile and
    /// that hero picks which one, which is also p.10's rule that a step's choice belongs to whoever
    /// performs it. The "may" rides on the same prompt: a <c>May</c> Capability node makes the
    /// handler's own card choice declinable and the handler names the exact chooser, so the hero being
    /// offered the retrieve is the hero who may decline it. Silverarrow's Fae Healing recorded a
    /// bounded fidelity note about an enclosing <c>may</c> reaching the controller instead; that note
    /// describes an <see cref="EffectNodeKind.Optional"/> wrapper, which none of these five faces
    /// has.</para>
    ///
    /// <para><b>No image, anywhere.</b> A definition is its values, its words and its relationships.
    /// </para>
    /// </summary>
    public static class RowennaEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // Node ids. Shared across the faces that share a factory, because two cards printing one
        // sentence resolve through one body shape - and the Paragons' repeat is named per card by
        // the decision-label declaration below, which keys on the card as well as the node.
        // -----------------------------------------------------------------------------------------

        private const string ChivalryPrefix = "chivalry";
        private const string RewardPrefix = "reward";
        private const string GuardPrefix = "guard";
        private const string GauntletPrefix = "gauntlet";
        private const string SurrenderPrefix = "surrender";
        private const string MeleePrefix = "melee";
        private const string VolleyPrefix = "volley";

        /// <summary>
        /// Milestone 8.57d. The two passes Grand Melee's "up to two times" prints, named rather than
        /// numbered so a node id reads as what it is.
        ///
        /// <para>They exist because rulebook p.10's count is over PERFORMANCES and this vocabulary has
        /// no loop: <see cref="EffectDefinitionValidator"/> refuses a cycle in the node table, and
        /// <see cref="EffectNodeKind.RepeatAction"/> repeats the whole ACTION - which on a Movement card
        /// would repeat the walk the text is printed beside. So a printed count of two is two sibling
        /// subtrees, which is exactly what Wasp's Kinetic Push does with its own "up to 2": "the card's
        /// own 'up to 2' is why there are exactly two".</para>
        /// </summary>
        private static readonly string[] MeleePasses = { "first", "second" };

        /// <summary>
        /// Milestone 8.57c. Glorious Triumph's "TRIPLE assist coins" as the number the card prints.
        ///
        /// <para>It is written here rather than at the binding for the reason every printed number in
        /// this file is: the definition is the card's own words and values, and a bare <c>3</c> inside
        /// a node list says nothing about which word it came from.</para>
        /// </summary>
        private const int TripledAssistCoins = 3;

        // -----------------------------------------------------------------------------------------
        // Milestone 8.57e. Fabled Lance, her Purple Tier IV - rulebook p.16's permanent ability.
        // -----------------------------------------------------------------------------------------

        /// <summary>The ability's stable identity inside its card, and its derived source's suffix.</summary>
        public const string FabledLanceRuleId = "rowenna-fabled-lance";

        /// <summary>
        /// The <c>ContinuousRuleSource.RuleKind</c> the unlocked card supplies for exactly as long as
        /// rulebook p.16 says it does - while the card sits beside her dashboard and she is on the
        /// board. Nothing stores it, and a restored match derives it again from the same card.
        /// </summary>
        public const string FabledLanceRuleKind = "RowennaFabledLance";

        /// <summary>
        /// The Range value Fabled Lance prints, and the number its own sentence names: "count as having
        /// a printed Range value of 2". It is one constant read twice on purpose - the card's p.9 icon
        /// and the rule's declared amount are the same printed number, and writing it twice is how the
        /// icon and the ability come to disagree.
        /// </summary>
        public const int FabledLancePrintedRange = 2;

        /// <summary>
        /// Milestone 8.57e. "All your attack actions gain the &quot;Ranged&quot; subtype, target a unit
        /// in range, and count as having a printed Range value of 2." - Fabled Lance, printed whole.
        ///
        /// <para><b>A standing ability rather than a declaration or a trigger, and the card decides
        /// which.</b> <c>UltimateRuleDefinition.PermanentDeclaration</c> is for a sentence no
        /// modification family can carry - Ursafar's "you are always enraged" - and is read by NAME.
        /// This sentence changes three things p.13 and p.9 already decide about an Attack, so it is a
        /// typed rule:
        /// <see cref="RuleModificationKind.AttackActionsCountAsRangedAtPrintedRange"/>, whose own
        /// remarks record why the three printed clauses are one family and why the number is the
        /// printed icon rather than a bonus on it.</para>
        ///
        /// <para><b>The Range icon is printed content and is carried as such.</b> Her shard's table
        /// reads <c>Ultimate / R 2</c> and milestone 8.3 put p.9's one icon on this type for exactly
        /// this reason. Ledger question 12's approved reading - a +1 Radius item raises Tali's printed
        /// Ultimate Radius - has no second effect here: her Range item is composed ONCE, by
        /// <c>ItemBonusQuery</c>, onto the Attack card whose printed Range this ability supplies, so
        /// Paragon of Grace's item makes her reach three and never four.</para>
        ///
        /// <para><b>What it deliberately does not add.</b> Nothing about rulebook p.19's straight line,
        /// because the face prints none - "target a unit in range" is p.13's plain ceiling, and
        /// Silverarrow's and Swift's faces are what a straight-line clause looks like when a card means
        /// one. Nothing about Radius either: her Attack faces' riders read their own printed Radius
        /// disk through <c>RadiusValueQuery</c>, and p.9 keeps the two icons apart.</para>
        /// </summary>
        public static UltimateRuleDefinition FabledLanceRule() =>
            UltimateRuleDefinition.PermanentAbility(
                FabledLanceRuleId,
                FabledLanceRuleKind,
                new[]
                {
                    RuleModification.ForAttackActionsCountAsRangedAtPrintedRange(
                        FabledLancePrintedRange)
                },
                new[]
                {
                    new PrintedValueDefinition(
                        PrintedValueChannel.Range, FabledLancePrintedRange)
                });

        /// <summary>
        /// Milestone 8.46a. What her own decision controls say - the consequence in the printed text's
        /// own words, never the mechanism.
        ///
        /// <para>The five Red bodies declare none. The Paragons' repeat is automatic since milestone
        /// 9.1 (ruling W1), so it opens no confirm-or-decline, and the retrieves' own <c>may</c> rides
        /// on the retrieving hero's card prompt rather than on a pause of its own.</para>
        ///
        /// <para>They are not wired into <see cref="DecisionLabelCatalog"/> yet, for the same reason
        /// nothing else of hers is registered: the catalog is reachable content, and 8.57g is what
        /// makes her cards reachable.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            // Milestone 8.57d. Her three Green Movement faces each open p.10's confirm-or-decline once
            // per printed performance and then a modal per performance, so the labels are declared per
            // card AND per pass - Grand Melee asks the same two questions twice, and a player answering
            // the second must be able to see that it is the second.
            foreach (var card in new[]
            {
                RowennaCardContent.CloseQuarters, RowennaCardContent.Melee
            })
            {
                yield return DecisionStep(card, MeleeNodeName(0, "may"),
                    "Place a minion", "Place nothing");
                yield return DecisionArm(card, MeleeNodeName(0, "friendly"),
                    "Place a friendly minion beside an enemy hero");
                yield return DecisionArm(card, MeleeNodeName(0, "enemy"),
                    "Place an enemy minion beside yourself");
            }

            yield return DecisionStep(RowennaCardContent.GrandMelee, MeleeNodeName(0, "may"),
                "Place a minion", "Place nothing");
            yield return DecisionArm(RowennaCardContent.GrandMelee, MeleeNodeName(0, "friendly"),
                "Place a friendly minion beside an enemy hero");
            yield return DecisionArm(RowennaCardContent.GrandMelee, MeleeNodeName(0, "enemy"),
                "Place an enemy minion beside yourself");
            yield return DecisionStep(RowennaCardContent.GrandMelee, MeleeNodeName(1, "may"),
                "Place a second minion", "Stop here");
            yield return DecisionArm(RowennaCardContent.GrandMelee, MeleeNodeName(1, "friendly"),
                "Place a friendly minion beside an enemy hero");
            yield return DecisionArm(RowennaCardContent.GrandMelee, MeleeNodeName(1, "enemy"),
                "Place an enemy minion beside yourself");

            yield return DecisionStep(RowennaCardContent.OpeningVolley, VolleyPrefix + "-repeat",
                "Make another hero discard", "Stop here");
        }

        // -----------------------------------------------------------------------------------------
        // p.14's Attack text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of her printed Attacks applies at the p.14 moment it names, or <c>null</c> for
        /// a face this block does not own.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(RowennaCardContent.CodeOfChivalry))
            {
                return CreateChivalrousRetrieval(cardId, contentRevision);
            }

            if (cardId.Equals(RowennaCardContent.TokenOfGratitude))
            {
                return CreateAfterAttackReward(cardId, contentRevision, coins: 1, repeats: false);
            }

            if (cardId.Equals(RowennaCardContent.FairShare))
            {
                return CreateAfterAttackReward(cardId, contentRevision, coins: 1, repeats: false);
            }

            if (cardId.Equals(RowennaCardContent.ParagonOfGrace))
            {
                return CreateAfterAttackReward(cardId, contentRevision, coins: 1, repeats: true);
            }

            if (cardId.Equals(RowennaCardContent.FeatOfBravery))
            {
                return CreateAfterAttackRetrieval(cardId, contentRevision, repeats: false);
            }

            if (cardId.Equals(RowennaCardContent.ParagonOfValor))
            {
                return CreateAfterAttackRetrieval(cardId, contentRevision, repeats: true);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's moments <see cref="AttackTextFor"/>'s body is applied at, named by
        /// the keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Code of Chivalry prints "Before the attack:" and the four Reds print "After the
        /// attack", so she is the first hero in the roster whose Gold basic carries step 1's text while
        /// every one of her composed Reds carries step 7's.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(RowennaCardContent.CodeOfChivalry))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            if (cardId.Equals(RowennaCardContent.TokenOfGratitude) ||
                cardId.Equals(RowennaCardContent.FairShare) ||
                cardId.Equals(RowennaCardContent.FeatOfBravery) ||
                cardId.Equals(RowennaCardContent.ParagonOfGrace) ||
                cardId.Equals(RowennaCardContent.ParagonOfValor))
            {
                return CardTextTiming.AfterTheAttack;
            }

            return CardTextTiming.None;
        }

        // -----------------------------------------------------------------------------------------
        // Compositions
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Target a unit adjacent to you. Before the attack: If you target a hero, both you and the
        /// target may retrieve a discarded card." - Code of Chivalry, her Gold basic.
        ///
        /// <para><b>The targeting sentence is p.13's own and is not composed.</b> "Target a unit
        /// adjacent to you" is what a non-Ranged Attack already reaches - the page says so "even if the
        /// card has a radius icon" - so the action carries p.13's default target policy and this body
        /// is only the rider after it. The Attack takes its own target the ordinary way, which is why
        /// this text runs at step 1 with the target already recorded.</para>
        ///
        /// <para><b>"If you target a hero" is a question about the enclosing Attack, asked plainly
        /// rather than as the Conditional's own predicate.</b> Milestone 8.40c's rule is that a
        /// Conditional's branch cannot bind that Conditional's own output, and both consequences here
        /// are about the hero the question is about - so <c>synthetic-attack-took-a-hero@1</c> runs as
        /// an ordinary sibling publishing both halves of its one fact, and the Conditional over
        /// <c>synthetic-is-flagged@1</c> reads the Boolean. That is Bain's Drinking Buddies' shape
        /// exactly, and the reason it is his shape is the same one.</para>
        ///
        /// <para><b>An Attack that took a minion reaches the else that is not there, which is the text
        /// being over.</b> p.13 lets a "unit" be a minion, so "no" is an ordinary answer: the
        /// Conditional applies, takes no branch, and the Attack carries on to step 2 with nothing
        /// offered to anybody.</para>
        ///
        /// <para><b>Two independent retrieves, in printed order, each belonging to its own hero.</b>
        /// "Both you and the target may retrieve a discarded card" is not one decision with two
        /// subjects: p.10 forbids retrieving another hero's cards, so each node names one hero as owner
        /// and chooser, each opens that hero's own prompt over that hero's own pile, and each may be
        /// declined without touching the other. Rowenna's is first because the sentence says "you and
        /// the target" - and a hero with an empty discard pile is simply not asked, which is
        /// <c>May</c>'s own reading of a step that cannot be taken rather than a prompt with no
        /// answers.</para>
        /// </summary>
        private static EffectDefinition CreateChivalrousRetrieval(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId(ChivalryPrefix + "-root");
            var target = new EffectNodeId(ChivalryPrefix + "-target");
            var gate = new EffectNodeId(ChivalryPrefix + "-gate");
            var both = new EffectNodeId(ChivalryPrefix + "-both");
            var mine = new EffectNodeId(ChivalryPrefix + "-retrieve-mine");
            var theirs = new EffectNodeId(ChivalryPrefix + "-retrieve-theirs");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, gate }),

                    // Asking the enclosing Attack who it took changes nothing and can always be
                    // answered, so it is mandatory and never stops the page.
                    new EffectNodeDefinition(
                        target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.AttackTookAHeroV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.TookAHeroOutput,
                                EffectValueKind.Boolean),
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.AttackTargetHeroOutput,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { both },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", target,
                                SyntheticConsequenceCapabilities.TookAHeroOutput,
                                EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            // A predicate that publishes its answer must have the node declare it, or a
                            // restore re-deriving the recorded outcome finds an output the node does
                            // not admit and refuses the payload. Milestone 8.11 found that on Thunder
                            // Boomerang's own conditional.
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        both, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { mine, theirs }),
                    Retrieve(mine),
                    Retrieve(
                        theirs, target,
                        SyntheticConsequenceCapabilities.AttackTargetHeroOutput)
                });
        }

        /// <summary>
        /// One hero's optional retrieval of one of their own discarded cards.
        ///
        /// <para>Written once because four of the five faces this block owns print the clause and three
        /// of them mean a hero somebody else named. <paramref name="namedBy"/> is the node that
        /// published that hero and <paramref name="heroOutput"/> is the output it published them on, or
        /// both absent for the acting hero's own retrieval - p.10's "you can never retrieve another
        /// hero's cards" is why ONE source supplies BOTH the owner and the chooser, and never one
        /// hero's pile with another hero's choice.</para>
        /// </summary>
        private static EffectNodeDefinition Retrieve(
            EffectNodeId id, EffectNodeId namedBy = default, string heroOutput = null)
        {
            var inputs = new List<EffectInputBinding>();
            if (namedBy.IsDefined)
            {
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    "owner", namedBy, heroOutput, EffectValueKind.Hero));
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    "chooser", namedBy, heroOutput, EffectValueKind.Hero));
            }
            else
            {
                inputs.Add(EffectInputBinding.FromRole(
                    "owner", EffectRoleKind.Actor, EffectValueKind.Hero));
                inputs.Add(EffectInputBinding.FromRole(
                    "chooser", EffectRoleKind.Actor, EffectValueKind.Hero));
            }

            inputs.Add(EffectInputBinding.FromClosedVocabulary(
                "kind", RosterCardRetrievalKind.OwnedDiscardedCard));

            return new EffectNodeDefinition(
                id, EffectNodeKind.Capability, EffectFailurePolicy.May,
                SyntheticRosterCapabilities.RetrieveCardV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding("retrieved", EffectValueKind.CardInstance),

                    // Milestone 8.40i's Boolean. Only Paragon of Valor's repeat reads it, and a node
                    // that declares an output it does not consume publishes a fact nothing asks for -
                    // so it is declared on every retrieval here rather than on one, for the reason the
                    // clause is written once: the four faces print one sentence, and a body that
                    // published different facts per card would be four sentences.
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.DidRetrieveOutputName, EffectValueKind.Boolean)
                });
        }

        /// <summary>
        /// "Target a unit adjacent to you. After the attack: A friendly hero in radius gains 1 coin[.
        /// If they do, repeat once on a different target]." - Token of Gratitude and Fair Share without
        /// the second sentence, Paragon of Grace with it.
        ///
        /// <para><b>One factory for three faces, and the Radius is NOT a number any of them carries.</b>
        /// Token of Gratitude prints Radius 1, Fair Share prints 3 and Paragon of Grace prints 3;
        /// <c>synthetic-radius-hero-target@1</c> reads the acting card's own printed Radius with p.16's
        /// items composed onto it, which is what a distance written into the definition could not do.
        /// What separates the three is the second sentence and nothing else.</para>
        ///
        /// <para><b>"A friendly hero in radius" is Rowenna's choice and never Rowenna.</b> The
        /// selection names <see cref="SourceTeamRelation.AlliedWithSource"/> and leaves
        /// <c>includeSelf</c> unset, which is the reading Widget's shard settled for the same printed
        /// word: p.17 needed a special rule to take "yourself" out of "friendly", and a card that says
        /// "a friendly hero" means somebody else. The activity it asks immunity for is the Attack this
        /// text is printed inside, exactly as Wasp's after-attack selections do, so the offer asks p.18
        /// the same question the Attack itself asked.</para>
        ///
        /// <para><b>No friendly hero in radius is the text stopping, not a coin paid to nobody.</b> The
        /// selection is mandatory - "gains 1 coin" prints no "may" and no "if able" - so an empty disk
        /// is <c>ImpossibleMandatory</c>, p.10 keeps everything already applied, and the page carries
        /// on past a text that is over. The repeat is a later sibling, so it is never reached and never
        /// offered, which is exactly what "if they do" asks for.</para>
        ///
        /// <para><b>"If they do" reads the grant's own output.</b> The coins are mandatory once a hero
        /// is named, so the only ways the clause does not happen are the ways the step cannot - and
        /// <c>gain-resource@1</c> publishes <see cref="SyntheticRosterCapabilities.DidGrantOutputName"/>
        /// only on success. An unbound flag reads as false, which is the printed "if".</para>
        /// </summary>
        private static EffectDefinition CreateAfterAttackReward(
            CardDefinitionId cardId, string contentRevision, int coins, bool repeats)
        {
            var root = new EffectNodeId(RewardPrefix + "-root");
            var ally = new EffectNodeId(RewardPrefix + "-ally");
            var coin = new EffectNodeId(RewardPrefix + "-coin");
            var gate = new EffectNodeId(RewardPrefix + "-gate");
            var repeat = new EffectNodeId(RewardPrefix + "-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: repeats
                        ? new[] { ally, coin, gate }
                        : new[] { ally, coin }),
                FriendlyHeroInRadius(ally, consumer: coin),
                new EffectNodeDefinition(
                    coin, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "recipient", ally,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.DidGrantOutputName, EffectValueKind.Boolean)
                    })
            };

            if (repeats)
            {
                nodes.Add(Gate(gate, coin, SyntheticRosterCapabilities.DidGrantOutputName, repeat));
                nodes.Add(Repeat(repeat));
            }

            return new EffectDefinition(
                AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Target a unit adjacent to you. After the attack, a friendly hero in radius may retrieve a
        /// discarded card[; If they do, repeat once on a different target]." - Feat of Bravery without
        /// the second clause, Paragon of Valor with it.
        ///
        /// <para><b>It is the coin factory's twin with the rider swapped, and the difference the swap
        /// makes is the "may".</b> The coins are Rowenna's to pay and happen to land on somebody else;
        /// the retrieval is the chosen hero's own act, out of their own pile, and theirs to decline -
        /// so the node names that hero as owner AND chooser and carries <c>May</c>, which is what puts
        /// the card prompt and its decline in front of them rather than in front of her.</para>
        ///
        /// <para><b>The selection declares the retrieval as its consumer, which narrows who is
        /// offered.</b> A hero with an empty discard pile has nothing this step could do, and this
        /// catalog's rule 15 says a selection offers only the objects its consumer can act on - Bain's
        /// Drinking Buddies is the same clause with the same narrowing, and greying a hero who could
        /// only decline is the reading that milestone 8.35z15 settled for a prompt with no legal
        /// answer.</para>
        ///
        /// <para><b>"If they do" is the retrieval's own Boolean and a decline means no repeat.</b>
        /// <c>retrieve-card@1</c> publishes <c>didRetrieve</c> only on success, which is milestone
        /// 8.40i's output built for exactly this grammar: the sentence after the semicolon is about the
        /// whole Attack rather than about the hero who retrieved, so nothing in it can be bound from
        /// the first clause and the condition needs the fact as a flag.</para>
        /// </summary>
        private static EffectDefinition CreateAfterAttackRetrieval(
            CardDefinitionId cardId, string contentRevision, bool repeats)
        {
            var root = new EffectNodeId(RewardPrefix + "-root");
            var ally = new EffectNodeId(RewardPrefix + "-ally");
            var retrieve = new EffectNodeId(RewardPrefix + "-retrieve");
            var gate = new EffectNodeId(RewardPrefix + "-gate");
            var repeat = new EffectNodeId(RewardPrefix + "-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: repeats
                        ? new[] { ally, retrieve, gate }
                        : new[] { ally, retrieve }),
                FriendlyHeroInRadius(ally, consumer: retrieve),
                Retrieve(
                    retrieve, ally, SyntheticTargetingCapabilities.ChosenHeroOutputName)
            };

            if (repeats)
            {
                nodes.Add(Gate(
                    gate, retrieve, SyntheticRosterCapabilities.DidRetrieveOutputName, repeat));
                nodes.Add(Repeat(repeat));
            }

            return new EffectDefinition(
                AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "A friendly hero in radius" - p.9's disk measured from Rowenna at the acting card's own
        /// printed Radius, narrowed to her own side and excluding her.
        /// </summary>
        private static EffectNodeDefinition FriendlyHeroInRadius(
            EffectNodeId id, EffectNodeId consumer) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: new[]
                {
                    // The enclosing action is the Attack this text is printed on, and p.18 declares
                    // immunity per activity - so the offer asks immunity the same question the Attack
                    // itself asked.
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                        SourceTeamRelation.AlliedWithSource),

                    // Neither face prints "not adjacent to you", so nothing is taken out of the disk's
                    // innermost ring.
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                        EffectValue.Boolean(false)),

                    // "friendly" excludes her, which is the same reading Widget's "another friendly
                    // hero" carries and the one p.17's special rule for "yourself" implies.
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName,
                        EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValue.Node(consumer))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                });

        /// <summary>
        /// "If they do" - the rider's own performed flag, read in the shape a
        /// <see cref="EffectNodeKind.Conditional"/> takes. A step that was declined or could not be
        /// taken publishes nothing, and an unbound flag reads as false.
        /// </summary>
        private static EffectNodeDefinition Gate(
            EffectNodeId id, EffectNodeId rider, string performedOutput, EffectNodeId branch) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { branch },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "flag", rider, performedOutput, EffectValueKind.Boolean)
                },
                outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });

        /// <summary>
        /// "repeat once on a different target" - p.10's repeat of the whole Attack action.
        ///
        /// <para>"once" is the declared maximum, counted across the whole chain rather than per
        /// instance, so the repeated Attack's own copy of this text reaches a node that is no longer
        /// eligible and offers nothing - there is never a third order. "on a different target" excludes
        /// the unit THIS instance took rather than naming a selection node, because an Attack's target
        /// is the action's own state; that is milestone 8.10's
        /// <see cref="RepeatTargetBinding.DifferentTarget"/>, minted for Thunder Boomerang and declared
        /// identically here. No "may" is printed, so the repeat is performed whenever a different
        /// target is legal: ruling W1 (Rules/PRINTED_WORDING.md, milestone 9.1).</para>
        ///
        /// <para>The repeated instance is an ordinary Attack: it chooses its own target out of the
        /// smaller set, takes its own Defense response, and applies this same rider again - which may
        /// name a different friendly hero, because nothing about the first instance's choice is carried
        /// over.</para>
        /// </summary>
        private static EffectNodeDefinition Repeat(EffectNodeId id) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                maximumRepeats: 1,
                repeatTargetBinding: RepeatTargetBinding.DifferentTarget);

        // -----------------------------------------------------------------------------------------
        // p.12's Skill primaries - milestone 8.57b
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of her printed Skills performs, or <c>null</c> for a face no block owns yet.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(RowennaCardContent.StandGuard) ||
                cardId.Equals(RowennaCardContent.ProtectTheWeak))
            {
                return CreateProtectiveSwap(cardId, contentRevision, retrieves: false);
            }

            if (cardId.Equals(RowennaCardContent.DefendTheInnocent))
            {
                return CreateProtectiveSwap(cardId, contentRevision, retrieves: true);
            }

            if (cardId.Equals(RowennaCardContent.ThrowTheGauntlet))
            {
                return CreateThrownGauntlet(cardId, contentRevision);
            }

            if (cardId.Equals(RowennaCardContent.AcceptSurrender))
            {
                return CreateAcceptedSurrender(
                    cardId, contentRevision,
                    assistCoinMultiplier: SyntheticConsequenceCapabilities.UnmultipliedAssistCoins);
            }

            if (cardId.Equals(RowennaCardContent.GloriousTriumph))
            {
                return CreateAcceptedSurrender(
                    cardId, contentRevision, assistCoinMultiplier: TripledAssistCoins);
            }

            if (cardId.Equals(RowennaCardContent.OpeningShots))
            {
                return CreateOpeningShots(cardId, contentRevision, repeats: false);
            }

            if (cardId.Equals(RowennaCardContent.OpeningVolley))
            {
                return CreateOpeningShots(cardId, contentRevision, repeats: true);
            }

            return null;
        }

        /// <summary>
        /// "Swap with a friendly unit in range which is adjacent to an enemy hero, or has a card in
        /// the discard[; you may retrieve a discarded card]." - Stand Guard and Protect the Weak
        /// without the second clause, Defend the Innocent with it.
        ///
        /// <para><b>One factory for three faces, and the Range is NOT a number any of them carries.</b>
        /// Stand Guard prints Range 3 and both upgrades print 4;
        /// <c>synthetic-range-unit-target@1</c> reads the acting card's own printed Range with p.16's
        /// items composed onto it, which is what a distance written into the definition could not do.
        /// What separates the three is the second clause and nothing else.</para>
        ///
        /// <para><b>It is the printed-icon UNIT selection and not Whisper's
        /// <c>choose-swap-subject@1</c>, and the difference is the whole grammar of the sentence.</b>
        /// Hers is one selection over a UNION of two differently reached sets - "a unit adjacent to
        /// you, OR an enemy hero in range with a card in the discard" - where the "or" joins two
        /// reaches. Rowenna's "or" joins two QUALIFIERS inside one reach: every candidate is a friendly
        /// unit inside her printed Range, and the disjunction says which of them the sentence admits.
        /// Composing this on the union contract would attach a doctrine her text does not carry, so it
        /// composes on the selection whose statement it actually is - the same one Arien's "Swap with
        /// an enemy minion in range" has used since milestone 8.20, with p.19's Swap performed by
        /// <c>synthetic-swap-actor@1</c> after it.</para>
        ///
        /// <para><b>Rowenna is never among her own candidates.</b> A disk drawn on a hero contains
        /// that hero, and the unit arm asks for it without self - so the actor is out before any
        /// qualifier is asked, which is what "swap WITH a friendly unit" means: an exchange with
        /// yourself is nothing. The side is <see cref="SourceTeamRelation.AlliedWithSource"/>, which
        /// is her own, and p.19's units are heroes and minions both.</para>
        ///
        /// <para><b>The qualifier is one clause and the selection carries it as one.</b> See
        /// <see cref="SyntheticTargetingCapabilities
        /// .AdjacentToAnEnemyHeroOrHoldingADiscardParameterName"/>: two booleans would read the
        /// printed "or" as "and", and a minion - which owns no cards at all - can satisfy it only by
        /// standing beside an enemy hero.</para>
        ///
        /// <para><b>The Swap is declared as the selection's consumer, which narrows who is offered.</b>
        /// A unit p.18's immunity or a standing restriction will not let her exchange places with is
        /// not one to offer and then refuse - milestone 8.35l's rule, and Arien's own Swap is where it
        /// was learned. The selection's own immunity question is the enclosing action's, which p.12
        /// makes the Skill: "card text has no activity of its own".</para>
        ///
        /// <para><b>Defend the Innocent's retrieval is HERS, optional, and after the swap.</b> The
        /// semicolon puts it second and p.10 forbids retrieving another hero's cards, so the node
        /// names the actor as both owner and chooser. It is a later sibling of a mandatory step: a
        /// swap with no legal subject stops the card at the selection, and the retrieve is never
        /// reached and never offered - which is p.10's own mandatory-step order rather than anything
        /// this definition arranges.</para>
        /// </summary>
        private static EffectDefinition CreateProtectiveSwap(
            CardDefinitionId cardId, string contentRevision, bool retrieves)
        {
            var root = new EffectNodeId(GuardPrefix + "-root");
            var subject = new EffectNodeId(GuardPrefix + "-subject");
            var swap = new EffectNodeId(GuardPrefix + "-swap");
            var retrieve = new EffectNodeId(GuardPrefix + "-retrieve");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: retrieves
                        ? new[] { subject, swap, retrieve }
                        : new[] { subject, swap }),
                new EffectNodeDefinition(
                    subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeUnitTargetV1,
                    inputs: new[]
                    {
                        // A Skill's effect is the action (p.12), so the immunity question this
                        // selection asks is the Skill's own.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),

                        // "a friendly unit" - her own side, and p.19's two unit kinds both.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AlliedWithSource),

                        // "which is adjacent to an enemy hero, or has a card in the discard" - one
                        // printed clause, carried as one.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities
                                .AdjacentToAnEnemyHeroOrHoldingADiscardParameterName,
                            EffectValue.Boolean(true)),

                        // The Swap decides whether a unit in range is one this card can actually
                        // take, and the narrowing puts it the SPACE the selection publishes, which is
                        // the value the Swap reads - milestone 8.35l's correction, learned on Arien's
                        // own Swap.
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(swap))
                    },
                    outputs: new[]
                    {
                        // All three, because a unit selection publishes whichever kind was named and
                        // an undeclared recorded outcome is a snapshot this build refuses to read
                        // back - the reason Arien's minion selection declares both of its own.
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", subject,
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate),

                        // "a friendly unit", which p.19 makes a unit.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                        new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                    })
            };

            if (retrieves)
            {
                nodes.Add(Retrieve(retrieve));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Place yourself into a space in range adjacent to an enemy hero in range; that hero may
        /// move 1 space; if they do, gain 2 coins." - Throw the Gauntlet, her Silver basic, and the
        /// only sentence in the corpus that hands an OPPONENT a decision about their own position.
        ///
        /// <para><b>The hero is named before the space, and the semicolon is why.</b> "that hero"
        /// points back at the enemy whose adjacency qualified the destination, and a space next to two
        /// enemy heroes would leave the pronoun ambiguous if the space were chosen first. So an
        /// ordinary <c>synthetic-range-hero-target@1</c> selection names him - the same printed Range
        /// the Place reads, so neither half of "in range" can invent reach the other does not have -
        /// and the Place then offers the spaces beside him. A hero with no empty qualifying space is
        /// not offered at all, because the Place is declared as that selection's consumer.</para>
        ///
        /// <para><b>The Place is p.18's and not a move.</b> "Place yourself" names the piece, so the
        /// subject reaches the Place through the actor-space bridge rather than through a selection -
        /// milestone 6.5a's reason exactly. It is mandatory: the face prints no "may", so an enemy
        /// hero named with nowhere beside him left stops the Skill there, which is p.10's own answer
        /// for an impossible mandatory step.</para>
        ///
        /// <para><b>"That hero may move 1 space" is the OPPONENT's decision, and RO-Q2 is the
        /// reading.</b> "May" makes it theirs to decline, p.19 makes it a card-text move rather than a
        /// Movement action - so movement-action restrictions do not block it, movement-action buffs do
        /// not widen it and there is no Fast Travel to substitute - and the distance is exact: p.11's
        /// "up to" is not printed, so the hero moves one space or not at all. The decision is issued
        /// to him by name through <c>synthetic-direct-move@3</c>'s own chooser rather than by an
        /// enclosing <see cref="EffectNodeKind.Optional"/>: <see cref="EffectChooserRole.BoundHero"/>
        /// reaches only a <see cref="EffectNodeKind.Choose"/> today, and a <c>May</c> Capability node
        /// whose handler names its chooser is the route Code of Chivalry's own second retrieve already
        /// takes - the hero being offered the step is the hero who may decline it.</para>
        ///
        /// <para><b>A hero with nowhere to go is not asked.</b> The move reports itself impossible and
        /// <c>May</c> turns that into a step not taken, so no prompt is opened and no coins follow -
        /// which is the same thing a decline produces and the correct thing for both.</para>
        ///
        /// <para><b>"If they do, gain 2 coins" gates on the MOVE and pays ROWENNA.</b> The coins are
        /// hers - the sentence's subject never changed from "yourself" - so nothing about them can be
        /// bound from the opponent's answer, and the condition reads
        /// <see cref="SyntheticDisplacementCapabilities.DidMoveOutputName"/>, published only when the
        /// move was actually committed. A decline publishes nothing and an unbound flag reads as
        /// false, which is exactly what the printed "if" asks for.</para>
        /// </summary>
        private static EffectDefinition CreateThrownGauntlet(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId(GauntletPrefix + "-root");
            var actorSpace = new EffectNodeId(GauntletPrefix + "-actor-space");
            var hero = new EffectNodeId(GauntletPrefix + "-hero");
            var place = new EffectNodeId(GauntletPrefix + "-place");
            var move = new EffectNodeId(GauntletPrefix + "-move");
            var gate = new EffectNodeId(GauntletPrefix + "-gate");
            var coins = new EffectNodeId(GauntletPrefix + "-coins");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, hero, place, move, gate }),

                    // Her own space, published before the hero is named so that the Place the
                    // selection is narrowed against can already be asked about its subject. Reading a
                    // hero's own coordinate decides nothing and mutates nothing.
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        hero, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            // p.12 again: the Skill's own activity, so an enemy immune to her Skill
                            // cannot be named at all - which is p.18's "you cannot target that unit".
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),

                            // "other than the attacker" is a Block rider's clause and this is a
                            // Skill with no attacker to exclude.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),

                            // The Place is what decides whether an enemy hero in range is one this
                            // card can actually reach: a hero with no empty space beside him inside
                            // her Range is greyed rather than offered and then refused.
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(place))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, ActorSpaceOutput,
                                EffectValueKind.Coordinate),

                            // The subject is already fixed to the acting hero, so this declaration
                            // narrows nothing - p.18's own unrestricted breadth.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                PlaceEligiblePieceKind.GamePiece),

                            // "in range" is measured from the hero whose card it is, which p.9 makes
                            // the anchor of every printed icon.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                PlaceDestinationAnchor.ActingHero),

                            // The face prints no Spawn-point clause, so no space is refused for
                            // carrying one.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                PlaceSpawnPointExclusion.None),

                            // "adjacent to an enemy hero in range" - the hero the node above named,
                            // by the space he is standing in.
                            EffectInputBinding.FromNodeOutput(
                                SyntheticPlacementCapabilities.BesideSpaceParameterName, hero,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            // The hero has not moved - a Place moves HER - so the space the selection
                            // published is still where he is standing.
                            EffectInputBinding.FromNodeOutput(
                                "mover", hero,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),

                            // The card says how far and nothing about where, so nothing is filtered
                            // by where anybody stands - p.11's own breadth.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted),

                            // "that hero may move" - his decision, on his own seat's prompt.
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.ChooserParameterName, hero,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.DidMoveOutputName,
                                EffectValueKind.Boolean)
                        }),

                    Gate(gate, move, SyntheticDisplacementCapabilities.DidMoveOutputName, coins),

                    new EffectNodeDefinition(
                        coins, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.GainResourceV1,
                        inputs: new[]
                        {
                            // "gain" with no other subject is the acting hero's own gain, which is
                            // who "yourself" was at the start of the sentence.
                            EffectInputBinding.FromRole(
                                "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral("amount", EffectValue.Integer(2))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding("amount", EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "Defeat an enemy hero adjacent to you with no cards in hand[; your friendly heroes gain
        /// triple assist coins]." - Accept Surrender without the second clause, Glorious Triumph with
        /// it, and the only sentence in the corpus that defeats a hero for holding nothing.
        ///
        /// <para><b>One factory for two faces, and the only difference is a number.</b> Both print
        /// the identical first sentence at the identical reach; what the Tier III adds is the
        /// multiplier on the coins its own Defeat pays out. Nothing else about the two bodies differs,
        /// which is why writing them twice would be writing one sentence twice.</para>
        ///
        /// <para><b>"Adjacent to you" is card TEXT and is never read off an icon.</b> Neither face
        /// prints a Range or a Radius - see her shard's printed table - so the reach is the number the
        /// sentence itself writes down, which is what the written-distance roster selection takes and
        /// what a printed-icon selection could not: p.16's items raise a printed icon and never invent
        /// one, so a face with no icon has no bound for an item to widen. That is Silverarrow's
        /// milestone 8.35z46 lesson stated for a second hero, and it is why this composes on
        /// <c>choose-target@2</c> with <c>reach</c> 1 rather than on
        /// <c>synthetic-radius-hero-target@1</c>.</para>
        ///
        /// <para><b>"With no cards in hand" narrows WHO may be chosen.</b> It is part of the printed
        /// subject rather than a condition on what happens to him, which is the reading milestone
        /// 8.21's <c>playedAttackThisTurn</c>, 8.35z28's <c>hasCardInDiscard</c> and 8.40f's
        /// <c>minimumCardsInHand</c> each record for the same grammar - and here the reading is
        /// load-bearing rather than merely tidy, because the consequence is a Defeat: a hero offered
        /// and then refused would have been removed from the board first. The narrowing is
        /// <see cref="SyntheticRosterCapabilities.RequiresEmptyHandParameter"/>, whose name is the
        /// printed-icon family's own so that a card author cannot declare one half of the pair and
        /// mistype the other, and it asks
        /// <see cref="GuardsDigital.Core.Cards.HeroHandQuery"/> - so "in hand" means here exactly what
        /// p.10 means and what every seat's own hand count already shows. A hero who has played,
        /// resolved or discarded every card he had qualifies; a hero holding one does not.</para>
        ///
        /// <para><b>Nobody qualifying is the card having no legal target, and the offer greys.</b> The
        /// selection is mandatory - the face prints no "may" and no "if able" - so an empty candidate
        /// set is <c>ImpossibleMandatory</c>, which is p.10's own answer, and the selection's
        /// <c>Anticipate</c> derives that from the same candidate list its execution uses. That is
        /// what makes the Skill's own offer unavailable rather than a card that starts and stops.
        /// </para>
        ///
        /// <para><b>The Defeat is p.13's ordinary one, reached through the one spend site.</b>
        /// <c>synthetic-defeat-hero@1</c> declares the consequence and performs none of it - milestone
        /// 8.10's shape and 8.22's effect-invoked entrance - so the defeated hero's team spends Life
        /// counters by its lowest Tier, Rowenna collects Defeat coins equal to his Level, her
        /// team-mates collect assist coins by that Tier, his unresolved card resolves, his Active
        /// effects cancel, his markers return, and the match can END here if the last counter goes.
        /// Every one of those is the Attack's own list, unchanged, because it is the same method.
        /// </para>
        ///
        /// <para><b>"Your friendly heroes gain triple assist coins" multiplies ONE column, and RO-Q3
        /// is the reading.</b> The assist coins her team-mates collect for THIS Defeat are tripled -
        /// Tier I to 3, Tier II to 6, Tier III to 9. Her own Defeat coins are not: she collects Defeat
        /// coins rather than assist coins, and p.13 gives those two different sentences. The Life
        /// counters are a third sentence and do not move either. The multiplier rides to the spend
        /// site with the request and is composed into the number there rather than paid as a second
        /// grant afterwards, so each team-mate collects once - which is what the scoreboard counts and
        /// what a restore taken mid-consequence cannot double.</para>
        ///
        /// <para><b>Rowenna is never among her own candidates and neither is a friendly hero.</b> The
        /// selection is <see cref="SourceTeamRelation.OpposingSource"/> and the roster arm excludes
        /// the actor unconditionally, which is the printed "enemy hero". A hero immune to her Skill is
        /// not a candidate at all - p.12 makes the Skill the activity and p.18's "you cannot target
        /// that unit" is asked about it - which is the one exclusion the empty-hand clause could
        /// otherwise be blamed for.</para>
        /// </summary>
        private static EffectDefinition CreateAcceptedSurrender(
            CardDefinitionId cardId, string contentRevision, int assistCoinMultiplier)
        {
            var root = new EffectNodeId(SurrenderPrefix + "-root");
            var victim = new EffectNodeId(SurrenderPrefix + "-victim");
            var defeat = new EffectNodeId(SurrenderPrefix + "-defeat");

            var multiplied =
                assistCoinMultiplier > SyntheticConsequenceCapabilities.UnmultipliedAssistCoins;
            var defeatInputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromNodeOutput(
                    SyntheticConsequenceCapabilities.HeroParameter, victim,
                    SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
            };
            if (multiplied)
            {
                defeatInputs.Add(EffectInputBinding.FromLiteral(
                    SyntheticConsequenceCapabilities.AssistCoinMultiplierParameter,
                    EffectValue.Integer(assistCoinMultiplier)));
            }

            var defeatOutputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding(
                    SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
            };
            if (multiplied)
            {
                // Declared only by the face that prints the clause, because a node that declared an
                // output it does not carry would publish a fact nothing asks for - and the spend site
                // reads the request's own number, so a face publishing the page's own schedule would
                // be saying the same thing twice.
                defeatOutputs.Add(new EffectOutputBinding(
                    SyntheticConsequenceCapabilities.AssistCoinMultiplierOutput,
                    EffectValueKind.Integer));
            }

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { victim, defeat }),

                    new EffectNodeDefinition(
                        victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            // "adjacent to you" - measured from her, at the one space p.19 makes the
                            // word mean.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),

                            // "an enemy hero" - p.19's hero and not its unit, on the side opposing
                            // hers.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                RosterUnitKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),

                            // p.12: a Skill's effect is the action, so the immunity question this
                            // selection asks is the Skill's own. There is no enclosing Attack to
                            // inherit one from.
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),

                            // "with no cards in hand".
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.RequiresEmptyHandParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            // One output, because the selection admits one kind: a hero.
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        }),

                    new EffectNodeDefinition(
                        defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatHeroV1,
                        inputs: defeatInputs,
                        outputs: defeatOutputs)
                });
        }

        // -----------------------------------------------------------------------------------------
        // p.11's Movement primary and its "after movement" text - milestone 8.57d
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of her printed Movement faces applies around its own walk, or <c>null</c> for a
        /// face that prints no such text.
        /// </summary>
        public static EffectDefinition MovementTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(RowennaCardContent.CloseQuarters) ||
                cardId.Equals(RowennaCardContent.Melee))
            {
                return CreateCloseQuartersRider(cardId, contentRevision, performances: 1);
            }

            if (cardId.Equals(RowennaCardContent.GrandMelee))
            {
                return CreateCloseQuartersRider(cardId, contentRevision, performances: 2);
            }

            return null;
        }

        /// <summary>
        /// Which side of the walk a Green Movement face's text belongs to, decided by the face's own
        /// words.
        ///
        /// <para>p.11 states the default and allows the exception in one sentence: "Unless it says
        /// otherwise, the card text comes into effect before movement." All three of her Movement faces
        /// say otherwise - "After movement" - so all three carry milestone 8.35z31's member, which
        /// Ursafar's three swap faces minted for the identical printed words. A rider that ran first
        /// would be asking about adjacency to a hero she has not walked up to yet, which is the whole
        /// reason the page allows the exception.</para>
        /// </summary>
        public static CardTextTiming MovementTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(RowennaCardContent.CloseQuarters) ||
            cardId.Equals(RowennaCardContent.Melee) ||
            cardId.Equals(RowennaCardContent.GrandMelee)
                ? CardTextTiming.AfterTheMovement
                : CardTextTiming.None;

        /// <summary>
        /// "After movement, if you are adjacent to an enemy hero, you may Choose one - (1) Place a
        /// friendly minion in radius into a space adjacent to that enemy hero. (2) Place an enemy
        /// minion in radius into a space adjacent to you." - Close Quarters at Radius 2 and Melee at
        /// Radius 3; Grand Melee prints the same two bullets under "Choose up to two times".
        ///
        /// <para><b>One factory for three faces, and the Radius is NOT a number any of them carries.</b>
        /// Close Quarters prints Radius 2 and both upgrades print 3;
        /// <c>synthetic-radius-minion-target@1</c> and the Place beside it read the acting card's own
        /// printed Radius with p.16's items composed onto it, which is what a distance written into
        /// the definition could not do. What separates the three is the COUNT and nothing else.</para>
        ///
        /// <para><b>"After movement" is milestone 8.35z31's timing and not a node here.</b> The card
        /// declares <see cref="CardTextTiming.AfterTheMovement"/>, the turn authority parks the
        /// continuation, the walk runs underneath it, and this body enters at its own root when the
        /// Movement ACTION completes - which is the same seam Ursafar's three swap faces have used
        /// since that milestone. Three consequences fall out of that rather than being arranged: a
        /// walk of zero spaces still completes and still applies the text (p.11 says so in so many
        /// words), a card-text move is not a Movement action and reaches this seam never, and the
        /// adjacency below is read against the board as the walk left it.</para>
        ///
        /// <para><b>"If you are adjacent to an enemy hero" is a PRESENCE question and names nobody.</b>
        /// It is <c>presence-count@1</c> at a written reach of one over p.19's heroes on the opposing
        /// side, read through a <see cref="EffectNodeKind.Conditional"/> - Xargatha's Ritual of Death
        /// is the same shape for the same grammar. It has to be a presence rather than a selection
        /// because the second bullet needs no hero at all: naming one up front would put a decision in
        /// front of a player who is about to place an enemy minion beside herself, which the sentence
        /// never asks for. RO-Q4 is that reading, and this is where it lives.</para>
        ///
        /// <para><b>"That enemy hero" is therefore named INSIDE the first bullet, once per
        /// performance.</b> The shard settles the first half - she names one when she takes bullet 1 -
        /// and RO-Q7 settles what Grand Melee does with it: each performance of bullet 1 names its own,
        /// because the condition pinned nobody and the two performances are two separate choices made
        /// against a board that changed in between. The alternative - one naming carried across both -
        /// cannot even be composed honestly: a player who takes bullet 2 first would reach the second
        /// performance with nothing named.</para>
        ///
        /// <para><b>The two bullets are one modal and the count is siblings.</b> "Choose one" is a
        /// <see cref="EffectNodeKind.Choose"/> of exactly one of two arms, and "you may" is the
        /// <see cref="EffectNodeKind.Optional"/> above it, which is Whisper's Death Seeker's shape for
        /// the identical construction. Grand Melee prints "Choose up to two times" with no "may", so
        /// its two wrappers carry <see cref="EffectFailurePolicy.UpTo"/> instead - p.10's own word for
        /// a count whose floor is zero - and they are two sibling subtrees because this vocabulary has
        /// no loop and <c>RepeatAction</c> would repeat the Movement itself. Each performance is
        /// carried out before the next is offered, which is what the second modal being a later sibling
        /// means, and taking the same bullet twice is therefore ordinary rather than special.</para>
        ///
        /// <para><b>Bullet 1 names the minion BEFORE the hero, which is the printed order and also the
        /// only order that greys correctly.</b> Milestone 8.35l's offer walk judges a sequence by its
        /// FIRST step, so a bullet whose first node is the minion selection is refused outright when no
        /// friendly minion stands in the printed Radius - which is what "a bullet with no legal minion
        /// is not offered" means. Written the other way round the walk would reach the hero selection
        /// first, find an adjacent enemy the rider's own predicate has already guaranteed, and offer a
        /// bullet with nothing to place.</para>
        ///
        /// <para><b>Neither selection declares a consumer, and both absences are measurement.</b> The
        /// minion's Place binds <c>besideSpace</c> from a node that has not run when the minions are
        /// offered, and milestone 6.8l refuses to put a question to a consumer whose other required
        /// input is still unpublished; the hero's selection is <c>choose-target@2</c>, which implements
        /// neither of the interfaces that narrowing is asked of. So the honest statement of this
        /// block's boundary is: an enemy hero hemmed in with no empty space beside him is offered, and
        /// naming him reports the Place impossible, which stops the bullet with everything before it
        /// applied. The immunity and restriction questions a consumer could otherwise have carried are
        /// asked anyway, by the Place itself and by the minion selection's own
        /// <see cref="RuleActivity.Place"/> - exactly as Xargatha's Charm names <c>DirectMove</c> for
        /// the minion her own Movement text moves.</para>
        ///
        /// <para><b>The hero selection names the ENCLOSING action's activity, because nothing is done
        /// to him.</b> He is the landmark the destination clause is measured from - the minion is what
        /// gets placed - so the immunity question his selection asks is the one her Movement action
        /// already asked, which is the reading her own Reds' after-attack selections take of a hero the
        /// text merely names. "Adjacent to you" is written into the sentence rather than printed as an
        /// icon, so it is the written-distance roster arm at a reach of one and p.16's items never
        /// raise it - Silverarrow's milestone 8.35z46 lesson, stated again.</para>
        ///
        /// <para><b>Both Places are p.18's and neither is a move.</b> The piece goes into an empty
        /// space, no path is walked, obstacles and occupancy are the page's own, a placed minion keeps
        /// its identity, its side and its Heavy-ness, and nothing about it is defeated - which is why
        /// milestone 8.53's scoreboard cannot move for one. Bullet 2's destination is Wasp's
        /// Telekinesis' exact declaration: a written radius of one measured from the acting hero, which
        /// is what "adjacent to you" means and what no radius around the piece could ask.</para>
        /// </summary>
        private static EffectDefinition CreateCloseQuartersRider(
            CardDefinitionId cardId, string contentRevision, int performances)
        {
            var root = new EffectNodeId(MeleePrefix + "-root");
            var presence = new EffectNodeId(MeleePrefix + "-presence");
            var gate = new EffectNodeId(MeleePrefix + "-gate");
            var choices = new EffectNodeId(MeleePrefix + "-choices");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { presence, gate }),

                new EffectNodeDefinition(
                    presence, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.PresenceCountV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),

                        // "adjacent", which p.19 makes the one ring around her and which p.16's items
                        // never raise, so the number is written here rather than read off an icon.
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),

                        // She is the origin of the disk and never inside her own count of enemies, and
                        // this text encloses no target; both are declared false rather than left to a
                        // default, because the schema requires them and a card that means neither
                        // should say so.
                        EffectInputBinding.FromLiteral("excludeActor", EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral("excludeTarget", EffectValue.Boolean(false))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.CountOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }),

                Gate(gate, presence, SyntheticRosterCapabilities.PresentOutput, choices),

                new EffectNodeDefinition(
                    choices, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: Enumerable
                        .Range(0, performances)
                        .Select(pass => new EffectNodeId(MeleeNodeName(pass, "may")))
                        .ToArray())
            };

            for (var pass = 0; pass < performances; pass++)
            {
                nodes.AddRange(MeleePerformance(pass, performances));
            }

            return new EffectDefinition(
                MovementTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// One performance of the printed modal: the optional step that opens it, the two-arm choice,
        /// and both arms' own nodes.
        ///
        /// <para>Written once and instantiated per pass, because the two passes Grand Melee prints are
        /// the same sentence read twice - a body that spelled them differently would be saying the card
        /// prints two different things.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> MeleePerformance(int pass, int performances)
        {
            var may = new EffectNodeId(MeleeNodeName(pass, "may"));
            var modal = new EffectNodeId(MeleeNodeName(pass, "modal"));
            var friendly = new EffectNodeId(MeleeNodeName(pass, "friendly"));
            var friendlyMinion = new EffectNodeId(MeleeNodeName(pass, "friendly-minion"));
            var hero = new EffectNodeId(MeleeNodeName(pass, "hero"));
            var friendlyPlace = new EffectNodeId(MeleeNodeName(pass, "friendly-place"));
            var enemy = new EffectNodeId(MeleeNodeName(pass, "enemy"));
            var enemyMinion = new EffectNodeId(MeleeNodeName(pass, "enemy-minion"));
            var enemyPlace = new EffectNodeId(MeleeNodeName(pass, "enemy-place"));

            yield return new EffectNodeDefinition(
                may,
                EffectNodeKind.Optional,

                // "you may Choose one" on the two faces that print one performance; "Choose UP TO two
                // times" on the face that prints two, where zero is a legal count rather than a
                // declined step. Both open p.10's confirm-or-decline and neither can be taken twice.
                performances == 1 ? EffectFailurePolicy.May : EffectFailurePolicy.UpTo,
                childNodeIds: new[] { modal },
                chooser: EffectChooserRole.Actor);

            yield return new EffectNodeDefinition(
                modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { friendly, enemy },
                chooser: EffectChooserRole.Actor,
                minimumSelections: 1,
                maximumSelections: 1);

            // ---- Bullet 1 ----
            yield return new EffectNodeDefinition(
                friendly, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { friendlyMinion, hero, friendlyPlace });

            yield return MinionInRadius(
                friendlyMinion, SourceTeamRelation.AlliedWithSource, consumer: default);

            yield return new EffectNodeDefinition(
                hero, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource),

                    // Nothing is done to this hero - the minion is what is placed - so the immunity
                    // question is the enclosing Movement action's own.
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Movement)

                    // NO consumer is declared here, and the reason is measurement rather than taste.
                    // Milestone 6.8l's narrowing is asked of a handler that implements the selection
                    // and anticipated-outcome interfaces, and `choose-target@2` implements neither -
                    // so a declaration naming the Place would be a statement nothing in this build can
                    // read. The bound is therefore where it can be read: the MINION is offered only
                    // when one stands in radius, and a hero hemmed in with no empty space beside him
                    // is named and then reports the Place impossible, which stops the bullet with
                    // everything before it applied. That is milestone 8.35l's declared boundary rather
                    // than a gap this definition invents, and widening the written-distance selection
                    // to carry a consumer is a foundation change no printed text here asks for.
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),

                    // p.18's Place is measured from a space, so a selection whose consumer is a Place
                    // publishes where its answer is standing as well as who it is.
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                });

            yield return new EffectNodeDefinition(
                friendlyPlace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.PlaceInRadiusBesideANamedHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", friendlyMinion,
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate),

                    // "a friendly MINION", which is p.19's minion and not its unit.
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.EligiblePieceParameterName,
                        PlaceEligiblePieceKind.MinionMiniature),

                    // The reach bound is measured from the hero whose card it is, which p.9 makes the
                    // anchor of every printed icon - and it is the card's own Radius read a second
                    // time, which cannot exclude a space the sentence admits. See the declaration's
                    // own remarks for the proof.
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                        PlaceDestinationAnchor.ActingHero),

                    // The face prints no Spawn-point clause, so no space is refused for carrying one.
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                        PlaceSpawnPointExclusion.None),

                    // "adjacent to that enemy hero" - the hero the node above named, by the space he
                    // is standing in.
                    EffectInputBinding.FromNodeOutput(
                        SyntheticPlacementCapabilities.BesideSpaceParameterName, hero,
                        SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                });

            // ---- Bullet 2 ----
            yield return new EffectNodeDefinition(
                enemy, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { enemyMinion, enemyPlace });

            yield return MinionInRadius(
                enemyMinion, SourceTeamRelation.OpposingSource, consumer: enemyPlace);

            yield return new EffectNodeDefinition(
                enemyPlace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.PlaceV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", enemyMinion,
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate),

                    // "adjacent to you" is card text and p.16 never raises it, so the one is written
                    // down here rather than read off an icon - Wasp's Telekinesis, word for word.
                    EffectInputBinding.FromLiteral("radius", EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.EligiblePieceParameterName,
                        PlaceEligiblePieceKind.MinionMiniature),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                        PlaceDestinationAnchor.ActingHero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                });
        }

        /// <summary>
        /// "A friendly minion in radius" or "an enemy minion in radius" - p.9's disk measured from
        /// Rowenna at the acting card's own printed Radius, narrowed to one side.
        ///
        /// <para>The activity is <see cref="RuleActivity.Place"/> rather than the enclosing action's,
        /// because this minion is what the sentence performs its operation ON - which is the reading
        /// Xargatha's Charm records for the minion its own Movement text moves. An immune minion is
        /// therefore not a candidate at all, which is p.18's "you cannot target that unit" asked where
        /// the piece is named rather than after it has been picked.</para>
        /// </summary>
        private static EffectNodeDefinition MinionInRadius(
            EffectNodeId id, SourceTeamRelation relation, EffectNodeId consumer)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Place),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, relation)
            };
            if (consumer.IsDefined)
            {
                inputs.Add(EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer)));
            }

            return new EffectNodeDefinition(
                id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        EffectValueKind.Minion),

                    // p.18's Place takes a space, so the selection publishes where its answer stands
                    // as well as which minion it is.
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate)
                });
        }

        /// <summary>
        /// One node id inside a performance of the modal. Spelled through one method so the two passes
        /// cannot come to disagree about how they are named, and so a decision label declared for a
        /// pass names the same node the body builds.
        /// </summary>
        private static string MeleeNodeName(int pass, string part) =>
            MeleePrefix + "-" + MeleePasses[pass] + "-" + part;

        /// <summary>
        /// "If both you and an enemy hero in radius have no cards in the discard, that hero discards a
        /// card, if able[. May repeat once.]" - Opening Shots without the second sentence, Opening
        /// Volley with it, and the only sentence in the corpus whose condition is that two piles are
        /// EMPTY.
        ///
        /// <para><b>One factory for two faces, and the only difference is the repeat.</b> Both print
        /// the identical first sentence at the identical printed Radius 3; what the Tier III adds is
        /// p.10's own repeat. Writing them twice would be writing one sentence twice.</para>
        ///
        /// <para><b>The printed "if" is the candidate SET and not a Conditional, and that reading is
        /// load-bearing.</b> Whichever party holds a card, the sentence names nobody - so the clause is
        /// carried as
        /// <see cref="SyntheticTargetingCapabilities.BothDiscardPilesEmptyParameterName"/> on the
        /// selection, an unsatisfiable clause empties the candidate list, and a mandatory selection
        /// with nothing to name is <c>ImpossibleMandatory</c>, which milestone 8.35l's offer machinery
        /// greys rather than accepting and then refusing. A Conditional would have let the card be
        /// played and do nothing. Note which pile does what: HERS empties the list entirely - a card in
        /// her own discard leaves this face no legal target on any board - while each candidate's own
        /// pile removes only that candidate.</para>
        ///
        /// <para><b>"That hero discards a card" is HIS decision, from his own hand.</b>
        /// <c>discard-chosen-card@1</c> names the hero whose hand is read and whose choice it is, which
        /// is p.10's own rule that a step's choice belongs to whoever performs it - so the prompt opens
        /// on his seat and the card he gives up is his to pick. Nothing here declares a colour or names
        /// a card, because the sentence does neither.</para>
        ///
        /// <para><b>"If able" is the node's policy and an empty hand is an ordinary outcome.</b> A hero
        /// holding nothing gives nothing, the step is skipped in silence, and the Skill carries on -
        /// which is why the selection declares NO consumer: a hero with an empty hand is a legal target
        /// of a sentence that says "if able", and greying him would read the clause as "who can
        /// discard".</para>
        ///
        /// <para><b>Opening Volley's "May repeat once" is p.10's repeat of the whole Skill, and it
        /// re-reads the predicate.</b> The repeated instance derives its own candidates from the board
        /// and the piles as they now stand: the hero who just discarded owns a card in his discard and
        /// drops out, while Rowenna - who discarded nothing - is still empty-handed in her own pile, so
        /// the repeat usually needs a second eligible hero and is simply not offered when there is
        /// none. Nothing excludes the first target by name, because the face prints no "different
        /// target" clause and the predicate already does the work; a first target whose hand was empty
        /// discarded nothing, stays eligible, and may legally be named again. p.10 gives an unassigned
        /// decision to the card's controller, and "may repeat once" assigns none.</para>
        /// </summary>
        private static EffectDefinition CreateOpeningShots(
            CardDefinitionId cardId, string contentRevision, bool repeats)
        {
            var root = new EffectNodeId(VolleyPrefix + "-root");
            var target = new EffectNodeId(VolleyPrefix + "-target");
            var discard = new EffectNodeId(VolleyPrefix + "-discard");
            var repeat = new EffectNodeId(VolleyPrefix + "-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: repeats
                        ? new[] { target, discard, repeat }
                        : new[] { target, discard }),

                new EffectNodeDefinition(
                    target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        // p.12: a Skill's effect is the action, so the immunity question this
                        // selection asks is the Skill's own.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),

                        // The face prints no "not adjacent to you", so nothing is taken out of the
                        // disk's innermost ring.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),

                        // "an ENEMY hero" already excludes her, and the flag is declared false rather
                        // than left out so the face says what it means.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName,
                            EffectValue.Boolean(false)),

                        // "If both you and an enemy hero in radius have no cards in the discard" -
                        // one printed clause with two subjects, carried as one.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.BothDiscardPilesEmptyParameterName,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }),

                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, target,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    })
            };

            if (repeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// The acting hero's own space, published so the Place can name the piece "yourself" refers
        /// to. Milestone 6.4a's bridge, and the same one Arien's three leaps use.
        /// </summary>
        private const string ActorSpaceOutput = "space";

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");

        private static EffectDefinitionId MovementTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-movement-text");
    }
}
