using System.Collections.Generic;
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
    /// The effect bodies Rowenna the Vanguard's printed card text composes to - milestones 8.57a,
    /// 8.57b and 8.57c, the first three vocabulary blocks of the eighteenth hero.
    ///
    /// <para><b>This file is deliberately partial.</b> 8.57a owns six of her nineteen faces - the one
    /// that hands a retrieve to the hero she just targeted, and the five Reds that pay or heal a
    /// team-mate after the attack and may then repeat. 8.57b adds four: the three Blue faces that swap
    /// with a friendly unit, and her Silver basic Throw the Gauntlet. 8.57c adds the two Blue faces
    /// that defeat a surrendering enemy. Her minion placement, her opening shots and her Ultimate
    /// belong to 8.57d and 8.57e, and her whole set is composed at 8.57f. Nothing here is registered:
    /// <see cref="MatchContentCatalogs"/> names no Rowenna content until 8.57g deals her, which is
    /// milestone 7.9's gate - a registration no dealt card can start moves the pinned registration
    /// list, every snapshot's bytes and every corpus digest while the content never moved.</para>
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

        /// <summary>
        /// Milestone 8.57c. Glorious Triumph's "TRIPLE assist coins" as the number the card prints.
        ///
        /// <para>It is written here rather than at the binding for the reason every printed number in
        /// this file is: the definition is the card's own words and values, and a bare <c>3</c> inside
        /// a node list says nothing about which word it came from.</para>
        /// </summary>
        private const int TripledAssistCoins = 3;

        /// <summary>
        /// Milestone 8.46a. What her own decision controls say - the consequence in the printed text's
        /// own words, never the mechanism.
        ///
        /// <para>Only the two Paragons declare one at this block, because only a <c>RepeatAction</c>
        /// on <c>may</c> opens rulebook p.10's confirm-or-decline among these five bodies -
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery"/> is the rule, and
        /// the retrieves' own <c>may</c> rides on the retrieving hero's card prompt rather than on a
        /// pause of its own.</para>
        ///
        /// <para>They are not wired into <see cref="DecisionLabelCatalog"/> yet, for the same reason
        /// nothing else of hers is registered: the catalog is reachable content, and 8.57g is what
        /// makes her cards reachable.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionStep(RowennaCardContent.ParagonOfGrace, RewardPrefix + "-repeat",
                "Attack a different target", "Stop here");
            yield return DecisionStep(RowennaCardContent.ParagonOfValor, RewardPrefix + "-repeat",
                "Attack a different target", "Stop here");
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
        /// identically here. p.10 gives an unassigned decision to the card's controller, and "repeat
        /// once" assigns none.</para>
        ///
        /// <para>The repeated instance is an ordinary Attack: it chooses its own target out of the
        /// smaller set, takes its own Defense response, and applies this same rider again - which may
        /// name a different friendly hero, because nothing about the first instance's choice is carried
        /// over.</para>
        /// </summary>
        private static EffectNodeDefinition Repeat(EffectNodeId id) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                chooser: EffectChooserRole.Actor,
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

        /// <summary>
        /// The acting hero's own space, published so the Place can name the piece "yourself" refers
        /// to. Milestone 6.4a's bridge, and the same one Arien's three leaps use.
        /// </summary>
        private const string ActorSpaceOutput = "space";

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}
