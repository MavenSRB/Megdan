using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    internal sealed class TimeoutCannotProgressException : InvalidOperationException
    {
        public TimeoutCannotProgressException(string message) : base(message) { }
    }

    /// <summary>The explicitly opted-in timeout policy. It only selects currently offered typed commands.</summary>
    internal static class TimeoutChoicePolicy
    {
        public static bool CanUseUndoFallback(
            MatchSessionProjection projection,
            MatchClockKind kind,
            MatchSeat seat,
            HeroId actor,
            bool undoAuthorized)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return undoAuthorized && projection.PendingTurnRelease == null &&
                kind == MatchClockKind.Resolution && seat?.Owns(actor) == true &&
                !projection.HasPendingAttackResolution && projection.PendingAttackTargetChoice == null;
        }

        public static DirectorCommand Choose(MatchSessionProjection p, IReadOnlyList<DirectorCommand> offered,
            Func<int, int> random, IReadOnlyDictionary<RuleObject, int> votes = null)
        {
            var commands = offered.Where(c => c.Kind != DirectorCommandKind.DeselectMinionBattleRemoval &&
                c.EffectChoice?.Kind != EffectChoiceCommandKind.Deselect).ToArray();
            if (commands.Length == 0) throw new TimeoutCannotProgressException("Timeout has no completing legal command.");
            var end = commands.FirstOrDefault(c => c.Kind == DirectorCommandKind.PerformHold || c.Kind == DirectorCommandKind.ReleaseTurn);
            if (end != null) return end;
            var decline = commands.FirstOrDefault(c => c.Kind == DirectorCommandKind.StopMovement ||
                c.Kind == DirectorCommandKind.DeclineDefense || c.Kind == DirectorCommandKind.ResolveCardWithoutRespawn ||
                (c.Kind == DirectorCommandKind.AnswerRemovalPrevention && c.RemovalPrevention.Equals(p.PendingRemovalPrevention.DeclineInvocation)) ||
                c.EffectChoice?.Kind == EffectChoiceCommandKind.Decline || c.EffectChoice?.Kind == EffectChoiceCommandKind.Stop);
            if (decline != null) return decline;
            var effect = p.PendingEffectChoice;
            if (effect?.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot)
            {
                var selected = effect.SelectedCandidates.Select(c => c.Value.IntegerValue - 1).ToArray();
                if (selected.Select(v => v / 4).Distinct().Count() != selected.Length ||
                    selected.Select(v => v % 4).Distinct().Count() != selected.Length)
                    throw new InvalidOperationException("Existing rune selections are not a partial permutation.");
                commands = commands.Where(c => c.EffectChoice.Kind != EffectChoiceCommandKind.Select ||
                    !selected.Any(v => v / 4 == (c.EffectChoice.Candidate.Value.IntegerValue - 1) / 4 ||
                        v % 4 == (c.EffectChoice.Candidate.Value.IntegerValue - 1) % 4)).ToArray();
            }
            var confirm = commands.FirstOrDefault(c => c.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals ||
                c.EffectChoice?.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm != null) return confirm;
            if (votes != null && commands.All(c => c.Kind == DirectorCommandKind.SelectMinionBattleRemoval))
            {
                int Rank(DirectorCommand c) => votes.TryGetValue(c.MinionId.IsDefined ? RuleObject.ForMinion(c.MinionId) : RuleObject.ForHero(c.HeroId), out var n) ? n : 0;
                var highest = commands.Max(Rank); commands = commands.Where(c => Rank(c) == highest).ToArray();
            }
            else if (commands.All(c => Card(p,c).IsDefined))
            {
                var ranked = commands.Select(c => new { Command = c, Rank = Initiative(p, Card(p,c)),
                    Second = c.SecondCardInstanceId.IsDefined ? Initiative(p,c.SecondCardInstanceId) : 0 }).ToArray();
                // Some printed cards have no initiative; their offered set follows random legal.
                if (ranked.All(r => r.Rank > 0))
                {
                    var first = ranked.Min(r => r.Second > 0 ? Math.Min(r.Rank,r.Second) : r.Rank);
                    var subset = ranked.Where(r => (r.Second > 0 ? Math.Min(r.Rank,r.Second) : r.Rank) == first).ToArray();
                    var second = subset.Min(r => Math.Max(r.Rank,r.Second));
                    commands = subset.Where(r => Math.Max(r.Rank,r.Second) == second).Select(r=>r.Command).ToArray();
                }
            }
            else if (p.SelectedActor.IsDefined)
            {
                var body=p.ActingBodies.FirstOrDefault(b=>b.HeroId.Equals(p.SelectedActor.ActorId)).Body;
                var actor = p.Heroes.FirstOrDefault(h => h.HeroId.Equals(p.SelectedActor.ActorId) && (body==0 || h.Body==body));
                var points = commands.Select(c => Position(p,c)).ToArray();
                if (actor.HeroId.IsDefined && points.All(v => v.HasValue))
                {
                    int Distance(HexCoordinate a, HexCoordinate b) => (Math.Abs(a.Q-b.Q)+Math.Abs(a.R-b.R)+Math.Abs((a.Q+a.R)-(b.Q+b.R)))/2;
                    var nearest = points.Min(v => Distance(actor.Coordinate,v.Value));
                    commands = commands.Where((c,i)=>Distance(actor.Coordinate,points[i].Value)==nearest).ToArray();
                }
            }
            if (commands.Length == 0) throw new TimeoutCannotProgressException("Timeout cannot complete the current constrained selection.");
            return commands[random(commands.Length)];
        }
        private static CardInstanceId Card(MatchSessionProjection p, DirectorCommand c)
        {
            if (c.CardInstanceId.IsDefined) return c.CardInstanceId;
            if (c.EffectChoice?.Candidate.Value.Kind == EffectValueKind.CardInstance) return c.EffectChoice.Candidate.Value.CardInstanceId;
            if (c.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                p.PendingSystemTransitionStep.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId))
                return HeroLevelUpOption.Read(c.SystemTransitionChoice.OptionId).TakenCardInstanceId;
            return default;
        }
        private static int Initiative(MatchSessionProjection p, CardInstanceId id)
        {
            var card=p.Cards.FirstOrDefault(c=>c.Id.Equals(id));
            return card == null ? 0 : p.Definitions.First(d=>d.Reference.Equals(card.DefinitionReference)).Initiative.Value;
        }
        private static HexCoordinate? Position(MatchSessionProjection p, DirectorCommand c)
        {
            if (c.Kind == DirectorCommandKind.ChooseMovementDestination || c.Kind == DirectorCommandKind.ChooseCorrectionDestination) return c.Coordinate;
            // A mixed hero/minion set deliberately fails this all-spatial rank and remains random legal.
            if (c.Kind == DirectorCommandKind.ChooseAttackTarget && c.HeroId.IsDefined)
                return p.Heroes.Where(h=>h.HeroId.Equals(c.HeroId) && (c.HeroBody==0 || h.Body==c.HeroBody)).Select(h=>(HexCoordinate?)h.Coordinate).FirstOrDefault();
            if (c.EffectChoice?.Candidate.Value.Kind == EffectValueKind.Coordinate) return c.EffectChoice.Candidate.Value.Coordinate;
            if (c.EffectChoice?.Candidate.Value.Kind == EffectValueKind.Hero)
                return p.Heroes.Where(h=>h.HeroId.Equals(c.EffectChoice.Candidate.Value.HeroId)).Select(h=>(HexCoordinate?)h.Coordinate).FirstOrDefault();
            return null;
        }
    }
}
