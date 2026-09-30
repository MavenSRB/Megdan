using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 9.21b. One answer to "which of you acts" (RZ-Q4): the selected hero, the card whose
    /// resolution asked, and the body that performs the action.
    /// </summary>
    public sealed class ActingBodyCommand : IEquatable<ActingBodyCommand>
    {
        public ActingBodyCommand(HeroId heroId, CardInstanceId cardInstanceId, int body)
        {
            if (!heroId.IsDefined || !cardInstanceId.IsDefined)
            {
                throw new ArgumentException("An acting-body answer names the selected hero and card.");
            }

            if (body < HeroOccupancy.FirstBody)
            {
                throw new ArgumentOutOfRangeException(nameof(body), body, "A body ordinal is 1 or more.");
            }

            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            Body = body;
        }

        public HeroId HeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public int Body { get; }

        public bool Equals(ActingBodyCommand other) =>
            other != null && HeroId.Equals(other.HeroId) &&
            CardInstanceId.Equals(other.CardInstanceId) && Body == other.Body;

        public override bool Equals(object obj) => Equals(obj as ActingBodyCommand);

        public override int GetHashCode() =>
            ((HeroId.GetHashCode() * 397) ^ CardInstanceId.GetHashCode()) * 397 ^ Body;
    }

    /// <summary>
    /// Milestone 9.21b. The pending decision "which of you acts" (RZ-Q4, settled by the user as (A)):
    /// a hero standing on more than one space whose card resolves (p.7) picks the body that performs
    /// the action BEFORE the action is chosen, and that body stays the acting body for the whole
    /// action - before and after text, the secondary Movement, Fast Travel, Clear and Hold. One answer
    /// per standing body, in body order.
    /// </summary>
    public sealed class PendingActingBodyChoice
    {
        public PendingActingBodyChoice(
            HeroId heroId,
            CardInstanceId cardInstanceId,
            IEnumerable<HeroOccupancy> bodies)
        {
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            Bodies = Array.AsReadOnly((bodies ?? throw new ArgumentNullException(nameof(bodies)))
                .OrderBy(body => body.Body)
                .ToArray());
            if (Bodies.Count < 2 || Bodies.Any(body => !body.HeroId.Equals(heroId)))
            {
                throw new ArgumentException(
                    "An acting-body choice lists two or more standing bodies of the selected hero.",
                    nameof(bodies));
            }

            Invocations = Array.AsReadOnly(Bodies
                .Select(body => new ActingBodyCommand(heroId, cardInstanceId, body.Body))
                .ToArray());
        }

        public HeroId HeroId { get; }
        public CardInstanceId CardInstanceId { get; }

        /// <summary>The standing bodies, each with the space it stands on, in body order.</summary>
        public IReadOnlyList<HeroOccupancy> Bodies { get; }

        public IReadOnlyList<ActingBodyCommand> Invocations { get; }
    }

    public static class ActingBodyRules
    {
        /// <summary>
        /// The pick standing for this selected hero and card, or null. It stands only for a hero that
        /// declares more than one body (9.21a's supply), whose card has not had its acting body settled,
        /// and on whom more than one body stands; every other hero - and this one with one body - is
        /// never asked (the turn authority settles the one body itself).
        /// </summary>
        public static PendingActingBodyChoice CreateChoice(
            HeroId heroId,
            CardInstanceId cardInstanceId,
            AdjacentMoveAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.HeroBodySupplyOf(heroId) < 2 || board.TryGetActingBody(heroId, out _))
            {
                return null;
            }

            var bodies = board.GetHeroBodies(heroId);
            return bodies.Count < 2
                ? null
                : new PendingActingBodyChoice(heroId, cardInstanceId, bodies);
        }
    }
}
