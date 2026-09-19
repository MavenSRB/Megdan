using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The effect bodies Rowenna the Vanguard's printed card text composes to - milestone 8.57a, the
    /// first vocabulary block of the eighteenth hero.
    ///
    /// <para><b>This file is deliberately partial.</b> 8.57a owns five of her nineteen faces - the one
    /// that hands a retrieve to the hero she just targeted, and the four Reds that pay or heal a
    /// team-mate after the attack and may then repeat. Her swaps, her surrenders, her minion placement,
    /// her opening shots and her Ultimate belong to 8.57b through 8.57e, and her whole set is composed
    /// at 8.57f. Nothing here is registered: <see cref="MatchContentCatalogs"/> names no Rowenna
    /// content until 8.57g deals her, which is milestone 7.9's gate - a registration no dealt card can
    /// start moves the pinned registration list, every snapshot's bytes and every corpus digest while
    /// the content never moved.</para>
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

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}
