using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's Blue shard bodies - milestone 8.75d. Two lines fork off Abrupt Combustion:
    /// the PUSH line (Searing Heat -> Scorching Blaze), composed here, moves a hero in a straight line;
    /// and the DISCARD line (Abrupt Combustion -> Spontaneous Immolation -> Violent Conflagration),
    /// whose "enemy hero adjacent to a token or a minion discards a card / remove-or-defeat a minion"
    /// clauses land in a later slice.
    ///
    /// <para>Each card is coin-branched (<see cref="IgnatiaEffectContent.CoinBranched"/>): the sun arm
    /// moves a FRIENDLY hero, the moon arm an ENEMY hero, the same distance either way - which is what
    /// the two printed lines say.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        static partial void BlueBodyImpl(
            CardDefinitionId cardId, string revision, ref EffectDefinition body)
        {
            if (cardId.Equals(IgnatiaCardContent.SearingHeat))
            {
                // "Move a friendly/enemy hero in radius 2 spaces in a straight line." - exactly two.
                body = PushAHero(cardId, revision, distance: 2, leastDistance: 2);
            }
            else if (cardId.Equals(IgnatiaCardContent.ScorchingBlaze))
            {
                // "Move a friendly/enemy hero in radius 2 or 3 spaces in a straight line."
                body = PushAHero(cardId, revision, distance: 3, leastDistance: 2);
            }
            else if (cardId.Equals(IgnatiaCardContent.AbruptCombustion) ||
                cardId.Equals(IgnatiaCardContent.SpontaneousImmolation))
            {
                // ☀ "An enemy hero in radius, adjacent to a token, or a minion, discards a card, if
                // able." ☾ "Remove an enemy minion in radius adjacent to an enemy hero."
                body = DiscardLine(cardId, revision, escalatesToDefeat: false);
            }
            else if (cardId.Equals(IgnatiaCardContent.ViolentConflagration))
            {
                // ☀ "An enemy hero in radius, adjacent to a token, or a minion, discards a card, OR is
                // defeated." ☾ "Defeat an enemy minion in radius adjacent to an enemy hero."
                body = DiscardLine(cardId, revision, escalatesToDefeat: true);
            }
        }

        // -----------------------------------------------------------------------------------------
        // Milestone 8.75d. The DISCARD line - Abrupt Combustion -> Spontaneous Immolation -> Violent
        // Conflagration. Each is coin-branched: the sun arm makes a qualifying enemy hero discard (and,
        // for Violent, defeats one who cannot), the moon arm removes (Abrupt/Spontaneous) or defeats
        // (Violent) a qualifying enemy minion. The two printed subjects no shared selection can express
        // are minted in IgnatiaDiscardTargetingContent; everything else is registered vocabulary -
        // synthetic-discard-chosen-card (both revisions), synthetic-defeat-hero, synthetic-defeat-minion,
        // synthetic-remove, synthetic-is-flagged.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One coin-branched discard card. <paramref name="escalatesToDefeat"/> is Violent
        /// Conflagration's Tier III change and it changes both arms at once: the sun discard becomes
        /// "discards a card, or is defeated" and the moon consequence becomes a Defeat rather than a
        /// Remove - the two words its higher Tier prints.
        /// </summary>
        private static EffectDefinition DiscardLine(
            CardDefinitionId cardId, string revision, bool escalatesToDefeat)
        {
            var sunArm = new EffectNodeId("ignatia-discard-sun");
            var moonArm = new EffectNodeId("ignatia-discard-moon");

            var nodes = new List<EffectNodeDefinition>();
            nodes.AddRange(DiscardSunArm(sunArm, escalatesToDefeat));
            nodes.AddRange(RemoveOrDefeatMoonArm(moonArm, defeats: escalatesToDefeat));

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        /// <summary>
        /// ☀ "An enemy hero in radius, adjacent to a token, or a minion, discards a card, if able" -
        /// or, at Violent Conflagration's Tier, "discards a card, or is defeated".
        ///
        /// <para><b>The selection declares no consumer, on both readings.</b> "If able" makes a hero
        /// with an empty hand a legal target that simply gives nothing (Rowenna's Opening Shots' own
        /// reading), and "or is defeated" makes an empty-handed hero the one the second arm removes
        /// (Wasp's Electroblast's) - so an enemy hero adjacent to a magma token or a minion is offered
        /// regardless of what he holds.</para>
        ///
        /// <para><b>"Or is defeated" is one mandatory step with two ways to complete</b> - ledger
        /// question 13's reading, composed exactly as Electroblast composes it: revision 2 of the
        /// discard reports whether the hero was unable, a Conditional over that flag reaches the Defeat,
        /// and nobody is given a decision between the two.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> DiscardSunArm(
            EffectNodeId arm, bool escalatesToDefeat)
        {
            var select = new EffectNodeId("ignatia-discard-sun-select");
            var discard = new EffectNodeId("ignatia-discard-sun-discard");
            var escalation = new EffectNodeId("ignatia-discard-sun-escalation");
            var defeat = new EffectNodeId("ignatia-discard-sun-defeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: escalatesToDefeat
                        ? new[] { select, discard, escalation }
                        : new[] { select, discard }),

                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    IgnatiaDiscardTargetingContent.EnemyHeroAdjacentToMagmaOrMinionV1,
                    inputs: new[]
                    {
                        // p.12: a Skill's effect is the action, so the immunity question this
                        // selection asks is the Skill's own.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            IgnatiaDiscardTargetingContent.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            IgnatiaDiscardTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)
                    })
            };

            if (escalatesToDefeat)
            {
                // "discards a card, or is defeated" - revision 2 always applies and reports which way
                // it went. Mandatory, because ledger question 13 makes it one step with two outcomes.
                nodes.Add(new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, select,
                            IgnatiaDiscardTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    }));

                // The second arm, reached only where the discard could not happen. A branch, not a
                // question: p.10 gives nobody a decision here.
                nodes.Add(new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard,
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }));

                nodes.Add(new EffectNodeDefinition(
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, select,
                            IgnatiaDiscardTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    }));
            }
            else
            {
                // "discards a card, if able" - p.17's keyword over the discard alone: a chosen hero
                // with an empty hand gives nothing and the Skill goes on.
                nodes.Add(new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, select,
                            IgnatiaDiscardTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    }));
            }

            return nodes;
        }

        /// <summary>
        /// ☾ "Remove an enemy minion in radius adjacent to an enemy hero" - or, at Violent
        /// Conflagration's Tier, "Defeat an enemy minion in radius adjacent to an enemy hero".
        ///
        /// <para><b>Remove and Defeat are p.19's and p.13's own operations.</b> A Remove takes the
        /// minion off the board and collects no coins (<c>synthetic-remove</c> with a minion-miniature
        /// policy, taking the chosen minion's SPACE); a Defeat runs p.13's list
        /// (<c>synthetic-defeat-minion</c>, taking the chosen minion's IDENTITY). The selection
        /// publishes the space only for the Remove arm, because a fact a node did not declare is a
        /// snapshot the build refuses to read back.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> RemoveOrDefeatMoonArm(
            EffectNodeId arm, bool defeats)
        {
            var select = new EffectNodeId("ignatia-discard-moon-select");
            var consequence = new EffectNodeId("ignatia-discard-moon-consequence");

            var selectOutputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding(
                    IgnatiaDiscardTargetingContent.ChosenMinionOutputName, EffectValueKind.Minion)
            };
            if (!defeats)
            {
                // The Remove takes a coordinate, so the selection publishes where its answer stands.
                selectOutputs.Add(new EffectOutputBinding(
                    IgnatiaDiscardTargetingContent.ChosenMinionSpaceOutputName,
                    EffectValueKind.Coordinate));
            }

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { select, consequence }),

                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    IgnatiaDiscardTargetingContent.EnemyMinionAdjacentToEnemyHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            IgnatiaDiscardTargetingContent.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource)
                    },
                    outputs: selectOutputs)
            };

            if (defeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    consequence, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatMinionV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.MinionParameter, select,
                            IgnatiaDiscardTargetingContent.ChosenMinionOutputName,
                            EffectValueKind.Minion)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                            EffectValueKind.Minion)
                    }));
            }
            else
            {
                nodes.Add(new EffectNodeDefinition(
                    consequence, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.RemoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", select,
                            IgnatiaDiscardTargetingContent.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            RemoveEligiblePieceKind.MinionMiniature)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                    }));
            }

            return nodes;
        }

        /// <summary>
        /// One coin-branched "Move a hero in radius N spaces in a straight line": the sun arm moves a
        /// friendly hero, the moon arm an enemy hero. A direct Move is a straight line by rule (p.11),
        /// so nothing else states the line; <paramref name="leastDistance"/> is the printed floor
        /// ("2 or 3" is a floor of two beside a distance of three; "2 spaces" states two as both).
        /// </summary>
        private static EffectDefinition PushAHero(
            CardDefinitionId cardId, string revision, int distance, int leastDistance)
        {
            var sunArm = new EffectNodeId("ignatia-push-sun");
            var sunSelect = new EffectNodeId("ignatia-push-sun-select");
            var sunMove = new EffectNodeId("ignatia-push-sun-move");
            var moonArm = new EffectNodeId("ignatia-push-moon");
            var moonSelect = new EffectNodeId("ignatia-push-moon-select");
            var moonMove = new EffectNodeId("ignatia-push-moon-move");

            var nodes = new List<EffectNodeDefinition>();
            nodes.AddRange(PushArm(
                sunArm, sunSelect, sunMove, SourceTeamRelation.AlliedWithSource, distance, leastDistance));
            nodes.AddRange(PushArm(
                moonArm, moonSelect, moonMove, SourceTeamRelation.OpposingSource, distance, leastDistance));

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        private static IEnumerable<EffectNodeDefinition> PushArm(
            EffectNodeId arm,
            EffectNodeId select,
            EffectNodeId move,
            SourceTeamRelation relation,
            int distance,
            int leastDistance) =>
            new[]
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { select, move }),

                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName, relation),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(move))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                // Milestone 8.75c. The policy is `up to` because that is the ONLY reading under which
                // `leastDistance` is read at all: `AdmittedDistances.ForDeclared` admits
                // [leastDistance, distance] under `up to` and exactly [distance, distance] under every
                // other policy. Composed as mandatory, Scorching Blaze's printed "2 or 3 spaces"
                // offered only the threes and its Tier II sibling's "2 spaces" was right by accident.
                // The two printed shapes are then one declaration - a floor equal to the distance is
                // "2 spaces", a floor below it is "2 or 3 spaces" - which is what Brogan's charges get
                // from `NamedSpace` and what this arm has no named destination to get it from.
                //
                // Nothing about whether the step happens changes: the selection above declares this
                // node as its consumer, so a hero who could not be moved is never offered, and the
                // move that follows always has somewhere to go.
                new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", select,
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                            EffectValue.Integer(leastDistance)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("moved", EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            SyntheticDisplacementCapabilities.DidMoveOutputName,
                            EffectValueKind.Boolean)
                    })
            };
    }
}
