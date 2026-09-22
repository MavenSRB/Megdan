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
        /// friendly hero, the moon arm an enemy hero. <paramref name="leastDistance"/> is the printed
        /// floor ("2 or 3" is a floor of two beside a distance of three; "2 spaces" states two as
        /// both).
        ///
        /// <para><b>The straight line is carried at milestone 8.75b, and it was not before.</b> A
        /// direct Move is NOT a straight line - p.11's Movement reachability turns corners, and
        /// <c>synthetic-direct-move@3</c> under
        /// <see cref="DirectMoveDestinationPolicy.Unrestricted"/> offered the whole disk at the printed
        /// distance: ten spaces where the six files reach four (<c>IgnatiaPushTests</c> measured
        /// exactly that and recorded it as owed). The correction is
        /// <c>choose-line-destination@1</c>'s new <c>subjectSpace</c> anchor - the one contract in this
        /// build whose subject is p.19's Straight Line written in card text - walked from the CHOSEN
        /// hero's space rather than from the actor's, with the Move behind it pinned to the one space
        /// that offer named (<see cref="DirectMoveDestinationPolicy.NamedSpace"/>). It is Hanu's Fight
        /// and Flight shape exactly, with somebody else's feet under it.</para>
        /// </summary>
        private static EffectDefinition PushAHero(
            CardDefinitionId cardId, string revision, int distance, int leastDistance)
        {
            var sunArm = new EffectNodeId("ignatia-push-sun");
            var sunSelect = new EffectNodeId("ignatia-push-sun-select");
            var sunLine = new EffectNodeId("ignatia-push-sun-line");
            var sunMove = new EffectNodeId("ignatia-push-sun-move");
            var moonArm = new EffectNodeId("ignatia-push-moon");
            var moonSelect = new EffectNodeId("ignatia-push-moon-select");
            var moonLine = new EffectNodeId("ignatia-push-moon-line");
            var moonMove = new EffectNodeId("ignatia-push-moon-move");

            var nodes = new List<EffectNodeDefinition>();
            nodes.AddRange(PushArm(
                sunArm, sunSelect, sunLine, sunMove, SourceTeamRelation.AlliedWithSource,
                distance, leastDistance));
            nodes.AddRange(PushArm(
                moonArm, moonSelect, moonLine, moonMove, SourceTeamRelation.OpposingSource,
                distance, leastDistance));

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        /// <summary>The space the straight-line offer publishes - where the pushed hero's file ends.</summary>
        private const string PushLineDestinationOutput = "destination";

        private static IEnumerable<EffectNodeDefinition> PushArm(
            EffectNodeId arm,
            EffectNodeId select,
            EffectNodeId line,
            EffectNodeId move,
            SourceTeamRelation relation,
            int distance,
            int leastDistance) =>
            new[]
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { select, line, move }),

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

                        // Milestone 8.75b: the consumer is the LINE rather than the Move, because the
                        // line is now the node that decides whether a named hero can be moved at all -
                        // a teammate boxed in on every file has no destination, and the narrowing that
                        // keeps him out of the offer has to ask the question that refuses him.
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(line))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                // "N spaces in a straight line", asked of the CHOSEN hero's six files. Both bounds are
                // stated because the printed distance is a count rather than a ceiling - milestone
                // 8.59's correction, which is that an offer carrying only a ceiling lights spaces the
                // card cannot move to - and "2 or 3" states a floor below its distance.
                new EffectNodeDefinition(
                    line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseLineDestinationV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            "maximumDistance", EffectValue.Integer(distance)),
                        EffectInputBinding.FromLiteral(
                            "minimumDistance", EffectValue.Integer(leastDistance)),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticRosterCapabilities.LineSubjectSpaceParameter, select,
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            PushLineDestinationOutput, EffectValueKind.Coordinate)
                    }),

                // The Move is pinned to the space the line named: p.11's walk still carries the piece,
                // p.18's obstacles still stop it, and `NamedSpace` is what keeps the destination the
                // one the straight-line offer admitted rather than anywhere else at that distance.
                new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
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
                            DirectMoveDestinationPolicy.NamedSpace),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                            line, PushLineDestinationOutput, EffectValueKind.Coordinate)
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
