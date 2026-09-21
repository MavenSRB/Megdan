using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's effect bodies for the three Red faces whose text hangs off the Attack's own
    /// target: Split Attack, Split Throw and Peak Precision. Milestone 8.69b.
    ///
    /// <para><b>All three print one condition and three different consequences.</b> "If you target a
    /// hero who is adjacent to 3 or more obstacles" is
    /// <see cref="BrynnComponentContent.AttackTargetWasAdjacentToObstaclesV1"/> asked once, gated by
    /// milestone 8.69a's <see cref="GateOnPredicate"/>, and what hangs under the gate is a may-repeat,
    /// an if-able repeat, or an optional retrieval of the card itself.</para>
    ///
    /// <para><b>Where each face's text sits inside rulebook p.14, and why the two are not the
    /// same.</b> The split repeats are applied at step 1 -
    /// <see cref="CardTextTiming.BeforeTheAttack"/>, Arien's Violent Torrent's own moment, whose
    /// remarks in <c>CardActionResolutionAuthority</c> record that "the node is reached with the target
    /// already chosen". That is the moment BR-Q5 names: the gate is about the target this instance
    /// bound, and p.10 performs the repeat after this instance finishes whichever step asked for it,
    /// so nothing is gained by asking later and a live board is read while the target is still standing
    /// on it. Peak Precision's retrieval prints "After the attack:" in so many words and is applied at
    /// step 7.</para>
    ///
    /// <para><b>The +2 on Peak Precision is not here.</b> It is the same
    /// <c>RosterValueContributionDefinition</c> High Ground and Elevated Ambush declare - milestone
    /// 8.69a's <see cref="BrynnCardContent.RaisedAttack"/>, BR-Q6 - read when p.14 computes the Attack
    /// total. A node that added it would be a second statement of a rule the definition already
    /// makes.</para>
    /// </summary>
    public static partial class BrynnEffectContent
    {
        /// <summary>The shared prefix the three faces' nodes are named from.</summary>
        private const string SplitPrefix = "brynn-split";

        /// <summary>"If you target a hero who is adjacent to 3 or more obstacles" as one node.</summary>
        public static EffectNodeDefinition AttackTargetWasAdjacentToObstaclesNode(
            EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                BrynnComponentContent.AttackTargetWasAdjacentToObstaclesV1,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        BrynnComponentContent.PresentOutputName, EffectValueKind.Boolean)
                });

        /// <summary>
        /// "Target a unit in range. If you target a hero who is adjacent to 3 or more obstacles, may
        /// repeat once on a different unit adjacent to you." - Split Attack, and Split Throw's "repeat
        /// once on a different target unit" over the identical first two sentences.
        ///
        /// <para><b>The first sentence is not composed</b>, on the reading milestone 8.69a already
        /// took of High Ground's: "Target a unit in range" is rulebook p.13's own reading of a Ranged
        /// Attack's printed Range icon, which the action definition already carries, and every instance
        /// of the action - printed or repeated - inherits it by being that action. Silverarrow's Rain
        /// of Arrows records the same thing about "at maximum range", and RULING 38 is why.</para>
        ///
        /// <para><b>Split Attack is `may` and Split Throw is `if able`, which is the printed word and
        /// its absence.</b> Split Attack prints "may repeat" and p.10 gives the unassigned decision to
        /// the card's controller, so the node names the Actor as chooser and opens p.10's
        /// confirm-or-decline. Split Throw prints no "may" at all, so nobody is asked - and a board
        /// with no different legal unit left is a step that cannot happen rather than an action that
        /// stops. <see cref="EffectFailurePolicy.IfAble"/> is exactly that pair of statements and is
        /// the ONLY way this build can make a repeat mandatory-where-possible:
        /// <c>EffectNodeDefinition</c> refuses a mandatory <see cref="EffectNodeKind.RepeatAction"/>
        /// outright - "a mandatory repeat is unbounded" - and BR-Q5's own second half asks for the
        /// impossible step to leave the first instance's outcome standing, which is what a skip does
        /// and a failure does not. Rain of Arrows' two clauses are the precedent, word for word.</para>
        ///
        /// <para><b>"on a different unit adjacent to you" is narrower than the action it repeats, and
        /// that is the whole of the new binding.</b> Split Attack prints Range 2; the repeat reaches
        /// p.19's innermost ring alone, because the card's own words override p.10's "the entire action
        /// again" - which is the only reading that leaves "adjacent to you" saying anything. Split
        /// Throw prints "a different target unit" and therefore keeps the action's own reach, which is
        /// <see cref="RepeatTargetBinding.DifferentTarget"/> unchanged since milestone 8.10. Both
        /// exclude the unit the first instance took, and neither narrows p.19's unit KINDS: "unit" is
        /// the same word the first sentence uses, so a repeat onto a minion is legal on both.</para>
        ///
        /// <para><b>A repeated instance never asks again</b> - p.10's "You cannot repeat a repeated
        /// action!" - and neither face prints a chain, so <c>CardTextRepeatChain</c>'s single-clause
        /// answer bars it without either card saying so.</para>
        /// </summary>
        public static EffectDefinition SplitRepeat(
            CardDefinitionId cardId,
            string contentRevision,
            RepeatTargetBinding binding,
            bool printsMay)
        {
            var root = new EffectNodeId(SplitPrefix + "-repeat-root");
            var predicate = new EffectNodeId(SplitPrefix + "-repeat-target-boxed-in");
            var gate = new EffectNodeId(SplitPrefix + "-repeat-gate");
            var repeat = new EffectNodeId(SplitPrefix + "-repeat");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    // The predicate is an ordinary sibling rather than the gate's own question,
                    // because milestone 8.40c's rule is that a Conditional's branch cannot bind that
                    // Conditional's output - Rowenna's Code of Chivalry's shape, for the same reason.
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { predicate, gate }),

                    AttackTargetWasAdjacentToObstaclesNode(predicate),

                    GateOnPredicate(gate, predicate, repeat),

                    printsMay
                        ? new EffectNodeDefinition(
                            repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                            chooser: EffectChooserRole.Actor,
                            maximumRepeats: 1,
                            repeatTargetBinding: binding)
                        : new EffectNodeDefinition(
                            repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                            maximumRepeats: 1,
                            repeatTargetBinding: binding)
                });
        }

        /// <summary>
        /// "If you target a hero adjacent to 3 or more obstacles obstacles, +2 [Attack] attack and
        /// After the attack: You may retrieve this card." - Peak Precision's second sentence, minus the
        /// +2 that is a value rather than a step. (The doubled "obstacles" is a printing slip
        /// transcribed as printed and carries no rule.)
        ///
        /// <para><b>One condition gates both halves, which is what the printed "and" says</b> - BR-Q7 -
        /// so the gate below is the same predicate the Attack total reads through milestone 8.69a's
        /// value-query member, asked at the step the retrieval happens at.</para>
        ///
        /// <para><b>"this card" is the card being resolved, and p.14 puts the sentence at step 7 with
        /// the action still under way</b>, which is a state no retrieval in this build had ever
        /// reached: <c>retrieve-card@1</c>'s two pools are p.8's resting cards.
        /// <see cref="RosterCardRetrievalKind.OwnedResolvingSourceCard"/> is the third pool, stated
        /// rather than folded into either, and <c>restrictToCard</c> names the card from
        /// <see cref="EffectRoleKind.SourceCard"/> so nobody is asked WHICH card - Tali's Guardian
        /// Spirit's exact binding, and milestone 6.5a's reading of a question with one answer.</para>
        ///
        /// <para><b>The action still completes.</b> Retrieving the card does not end the Attack, which
        /// is already resolving; what it changes is where the card is when p.14 is over. The two seams
        /// that would otherwise have faulted on a finished action whose card is in hand -
        /// the turn's completion and Initiative's acknowledgement - name that one shape through
        /// <c>CardLifecycleAuthority.WasRetrievedDuringItsOwnAction</c>.</para>
        ///
        /// <para><b>"You may" is the Optional wrapper and not the capability's own policy</b>, because
        /// a restricted retrieval has nothing to ask: with the card named, the handler retrieves
        /// directly rather than opening a one-candidate prompt, so the decision the printed "may" gives
        /// the actor has to be the node above it. Min's Tiger Stance draws the same distinction.</para>
        /// </summary>
        public static EffectDefinition PeakPrecisionRetrieval(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId(SplitPrefix + "-retrieve-root");
            var predicate = new EffectNodeId(SplitPrefix + "-retrieve-target-boxed-in");
            var gate = new EffectNodeId(SplitPrefix + "-retrieve-gate");
            var may = new EffectNodeId(SplitPrefix + "-retrieve-may");
            var retrieve = new EffectNodeId(SplitPrefix + "-retrieve");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { predicate, gate }),

                    AttackTargetWasAdjacentToObstaclesNode(predicate),

                    GateOnPredicate(gate, predicate, may),

                    new EffectNodeDefinition(
                        may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { retrieve },
                        chooser: EffectChooserRole.Actor),

                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            // p.10 forbids retrieving another hero's cards, so one source supplies
                            // both halves - and this card is his own.
                            EffectInputBinding.FromRole(
                                "owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind", RosterCardRetrievalKind.OwnedResolvingSourceCard),
                            EffectInputBinding.FromRole(
                                SyntheticRosterCapabilities.RestrictToCardParameter,
                                EffectRoleKind.SourceCard, EffectValueKind.CardInstance)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        })
                });
        }
    }
}
