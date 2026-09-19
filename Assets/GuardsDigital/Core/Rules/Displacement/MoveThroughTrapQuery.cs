using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>
    /// One hero's displacement and the spaces it entered and left that something declaring p.18's
    /// move-through trap was standing on. Milestone 8.61e.
    ///
    /// <para>It is a record of a route rather than of a consequence: whose team anybody is on, which
    /// pieces go off and what that costs are all decided where the participants are, and this is what
    /// the board can say on its own.</para>
    /// </summary>
    public sealed class MoveThroughCrossing
    {
        public MoveThroughCrossing(HeroId mover, IReadOnlyList<HexCoordinate> spacesMovedThrough)
        {
            if (!mover.IsDefined)
            {
                throw new ArgumentException("A crossing names the hero who walked it.", nameof(mover));
            }

            if (spacesMovedThrough == null || spacesMovedThrough.Count == 0)
            {
                throw new ArgumentException(
                    "A recorded crossing names at least one space it went through.",
                    nameof(spacesMovedThrough));
            }

            Mover = mover;
            SpacesMovedThrough = spacesMovedThrough;
        }

        public HeroId Mover { get; }

        /// <summary>The spaces entered and left, in the order they were walked.</summary>
        public IReadOnlyList<HexCoordinate> SpacesMovedThrough { get; }
    }

    /// <summary>One token a crossing took off the board, with the kind its removal revealed.</summary>
    public sealed class SprungMoveThroughTrap
    {
        public SprungMoveThroughTrap(PlacedToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            TokenInstanceId = token.Id;
            Definition = token.Definition;
            Coordinate = token.Coordinate;
            ControllerHeroId = token.ControllerHeroId;
            SourceCardInstanceId = token.SourceCardInstanceId;
        }

        public TokenInstanceId TokenInstanceId { get; }

        /// <summary>
        /// The trusted definition the authority held, never a seat's masked substitute - the removal is
        /// the moment p.4's facedown piece stops being facedown, so this is the real kind.
        /// </summary>
        public TokenDefinition Definition { get; }

        /// <summary>Where it stood, which is a space the crossing hero walked over.</summary>
        public HexCoordinate Coordinate { get; }

        public HeroId ControllerHeroId { get; }

        /// <summary>
        /// The card that placed it. p.19 keeps a token's provenance separate from its lifetime, and
        /// this is provenance: it is what a reveal names as the card the piece came off, and what the
        /// consequence's own frame is anchored on, because the crossing hero played nothing.
        /// </summary>
        public CardInstanceId SourceCardInstanceId { get; }

        /// <summary>
        /// Whether this one costs the crossing hero anything - Min's blast, as against her dud. It is
        /// read off the definition's own declaration rather than off its id, so the rules never learn a
        /// content name.
        /// </summary>
        public bool CostsTheMover => Definition.MoveThroughTrapConsequences.Count > 0;
    }

    /// <summary>What one crossing sprang, and the single effect that prices all of it.</summary>
    public sealed class MoveThroughTrapOutcome
    {
        internal MoveThroughTrapOutcome(
            IReadOnlyList<SprungMoveThroughTrap> removed, EffectDefinitionReference consequence)
        {
            Removed = removed;
            Consequence = consequence;
        }

        /// <summary>Every token the crossing removed, in the order the hero walked over them.</summary>
        public IReadOnlyList<SprungMoveThroughTrap> Removed { get; }

        /// <summary>
        /// The one effect that prices this crossing, or an undefined reference when nothing it removed
        /// costs anything. "for every blast removed, that hero discards a card" is one sentence about a
        /// count, so it is one effect naming that many decisions rather than one effect per token: the
        /// engine runs one trigger frame at a time, and a second frame would have nowhere to stand.
        /// </summary>
        public EffectDefinitionReference Consequence { get; }

        public bool HasConsequence => Consequence.IsDefined;

        public bool IsEmpty => Removed.Count == 0;

        public static MoveThroughTrapOutcome None { get; } = new MoveThroughTrapOutcome(
            Array.AsReadOnly(Array.Empty<SprungMoveThroughTrap>()), default);
    }

    /// <summary>
    /// Rulebook p.18's other token sentence: "a unit can never end its movement on the same space as an
    /// obstacle, <b>and any special effects of tokens are still applied</b>" - read for the one printed
    /// face that has such an effect, Min the Dragonmonk's mines: "After an enemy hero moves through a
    /// mine, remove it; for every blast removed, that hero discards a card, if able."
    ///
    /// <para><b>What it is asked, and what it is never asked.</b> It is asked once, after a displacement
    /// has already committed, about the spaces that displacement entered and left. It is asked of no
    /// legality question at all: <see cref="ObstacleQuery"/> owns where a unit may go, and this owns
    /// what was standing on the way. The two are deliberately separate readers of two separate
    /// declarations on the same piece, because p.18 states them as two separate sentences and because a
    /// trap that could refuse a route would be a rule no mine prints.</para>
    ///
    /// <para><b>Who springs one.</b> "an enemy hero" - a hero, on the team opposing the token's
    /// controller. Min's own team walks over her mines, and so does every minion of either side: p.19
    /// separates heroes from minions and the sentence names heroes. A hero her own push carried over
    /// one is still a hero who moved through it, which is the whole of her printed combination, so
    /// nothing here asks whose displacement it was.</para>
    ///
    /// <para><b>What "moves through" is, and is not.</b> It is the spaces a route entered and left -
    /// <see cref="SpacesMovedThrough"/> for a walked route, <see cref="RealisedLine"/> for p.18's
    /// straight-line push or coupled move. A Swap and a Place are excluded by construction rather than
    /// by a check, because neither has a route: p.19 says a Swap "does not count as movement or
    /// placement", a Place puts a piece into a space without crossing any, Fast Travel is p.11's
    /// teleport between Spawn points, and a respawn puts a removed unit back. None of them can hand
    /// this query a space, so none of them can spring a mine.</para>
    ///
    /// <para>No unit ever ends a move on a mine - p.18 forbids it and
    /// <see cref="ObstacleQuery.BlocksEndOfMovement"/> enforces it - so both helpers exclude the
    /// endpoint, and the origin with it: the mover was standing there.</para>
    /// </summary>
    public static class MoveThroughTrapQuery
    {
        /// <summary>
        /// Whether one token definition prints p.18's "after an enemy hero moves through it, remove
        /// it". Stated once, here, so content declares the rule and nothing has to agree with a second
        /// reader about what the declaration looks like - the shape
        /// <see cref="ObstacleQuery.DeclaresMayBeMovedThrough"/> took for the sentence before it.
        /// </summary>
        public static bool Declares(TokenDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            for (var index = 0; index < definition.Properties.Count; index++)
            {
                var property = definition.Properties[index];
                if (property.AffectedObject == TokenPropertyAffectedObject.TheTokenItself &&
                    property.Modification != null &&
                    property.Modification.Kind ==
                        RuleModificationKind.RemovedWhenAnEnemyHeroMovesThrough)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The spaces a walked route entered and left. The route is the whole ordered walk including
        /// the space it started on, exactly as <c>MovementSession.Route</c> holds it, and what comes
        /// back is that walk without its first and last spaces.
        ///
        /// <para>A Movement action that stopped, moved again and stopped again is one route and one
        /// move: a space the hero legally stood on between two legs is an endpoint no obstacle could
        /// have been on, so dropping only the two ends of the whole walk drops exactly the spaces a
        /// trap could never have been standing on anyway.</para>
        /// </summary>
        public static IReadOnlyList<HexCoordinate> SpacesMovedThrough(IReadOnlyList<HexCoordinate> route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (route.Count <= 2)
            {
                return Array.Empty<HexCoordinate>();
            }

            var crossed = new List<HexCoordinate>(route.Count - 2);
            for (var index = 1; index < route.Count - 1; index++)
            {
                crossed.Add(route[index]);
            }

            return crossed.AsReadOnly();
        }

        /// <summary>
        /// The spaces a p.18 straight-line displacement entered and left: every space strictly between
        /// where the pushed or coupled object started and where it came to rest.
        ///
        /// <para>It reads the realised line rather than the requested one, and that is the whole
        /// correctness of it. p.18's push "moves it as far as possible", so a push that was stopped
        /// short never reached the spaces beyond where it stopped and cannot have moved through
        /// anything standing on them. The endpoint itself is excluded because
        /// <see cref="PushPathQuery"/> only ever keeps an empty space, and the origin because the
        /// object was standing on it.</para>
        /// </summary>
        public static IReadOnlyList<HexCoordinate> RealisedLine(
            HexCoordinate origin, HexCoordinate endpoint)
        {
            if (origin.Equals(endpoint) ||
                !StraightLineQuery.TryGetDirection(origin, endpoint, out var direction, out var distance))
            {
                return Array.Empty<HexCoordinate>();
            }

            if (distance <= 1)
            {
                return Array.Empty<HexCoordinate>();
            }

            var crossed = new List<HexCoordinate>(distance - 1);
            for (var step = 1; step < distance; step++)
            {
                crossed.Add(StraightLineQuery.Step(origin, direction, step));
            }

            return crossed.AsReadOnly();
        }

        /// <summary>
        /// The tokens a hero's crossing sprang, in the order the hero walked over them, still standing
        /// on the board.
        /// </summary>
        public static IReadOnlyList<PlacedToken> FindSprung(
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            HeroId mover,
            IReadOnlyList<HexCoordinate> spacesMovedThrough)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (spacesMovedThrough == null)
            {
                throw new ArgumentNullException(nameof(spacesMovedThrough));
            }

            // Participants are how "enemy" is answered, so a caller with none gets the only honest
            // answer rather than a guess about whose side a hero is on.
            if (participants == null || !mover.IsDefined || spacesMovedThrough.Count == 0 ||
                !TryGetSide(participants, mover, out var moverSide))
            {
                return Array.Empty<PlacedToken>();
            }

            var placed = board.PlacedTokens;
            var sprung = new List<PlacedToken>();
            foreach (var space in spacesMovedThrough)
            {
                for (var index = 0; index < placed.Count; index++)
                {
                    var token = placed[index];
                    if (!token.Coordinate.Equals(space) ||
                        !Declares(token.Definition) ||
                        sprung.Any(found => found.Id.Equals(token.Id)))
                    {
                        continue;
                    }

                    // p.19's own sides. A token's controller is the hero whose effect placed it, and
                    // "enemy hero" is a hero on the other team; a controller who has left the roster
                    // is nobody's enemy, which is the same answer the absent participant gets above.
                    if (!TryGetSide(participants, token.ControllerHeroId, out var controllerSide) ||
                        controllerSide == moverSide)
                    {
                        continue;
                    }

                    sprung.Add(token);
                }
            }

            return sprung.AsReadOnly();
        }

        private static bool TryGetSide(
            MatchParticipants participants, HeroId heroId, out TeamSide side)
        {
            side = TeamSide.None;
            if (!heroId.IsDefined ||
                !participants.Heroes.Any(hero => hero.Id.Equals(heroId)))
            {
                return false;
            }

            side = participants.GetTeamForHero(heroId).Side;
            return side != TeamSide.None;
        }
    }

    /// <summary>
    /// p.18's "remove it", applied. The query above decides what a crossing sprang; this takes those
    /// pieces off the board and states what the removal costs, and it is the one place either happens.
    /// </summary>
    public static class MoveThroughTrapOperation
    {
        /// <summary>
        /// Removes every trap the crossing sprang and names the effect that prices them.
        ///
        /// <para><b>Everything is removed before anything is paid for.</b> The printed sentence is in
        /// that order - "remove it; for every blast removed, that hero discards a card" - and so is the
        /// board: a hero who crossed two mines has crossed both by the time either goes off, and a
        /// removal that waited for a discard to be answered would leave a mine standing on a space
        /// nobody is walking over any more.</para>
        ///
        /// <para><b>One consequence, chosen by the count.</b> The declaration is a list indexed by how
        /// many went off at once, so two blasts choose the effect that asks twice. A crossing that
        /// sprang more copies than the declaring definition has entries - which its own supply makes
        /// unreachable - takes the largest it declares rather than none, because the page's own answer
        /// to "more than the supply" is never to stop.</para>
        /// </summary>
        public static MoveThroughTrapOutcome Spring(
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            HeroId mover,
            IReadOnlyList<HexCoordinate> spacesMovedThrough)
        {
            var sprung = MoveThroughTrapQuery.FindSprung(
                board, participants, mover, spacesMovedThrough);
            if (sprung.Count == 0)
            {
                return MoveThroughTrapOutcome.None;
            }

            var removed = new List<SprungMoveThroughTrap>(sprung.Count);
            foreach (var token in sprung)
            {
                removed.Add(new SprungMoveThroughTrap(board.CommitValidatedTokenReclamation(token.Id)));
            }

            return new MoveThroughTrapOutcome(removed.AsReadOnly(), Consequence(removed));
        }

        private static EffectDefinitionReference Consequence(IReadOnlyList<SprungMoveThroughTrap> removed)
        {
            var charging = removed.Where(entry => entry.CostsTheMover).ToArray();
            if (charging.Length == 0)
            {
                return default;
            }

            var declarations = charging
                .Select(entry => entry.Definition.MoveThroughTrapConsequences)
                .Distinct()
                .ToArray();
            if (declarations.Length != 1)
            {
                // Two kinds of charging trap under one hero in one move would be two counts and two
                // effects, and one trigger frame can run one. No content in this build can reach it,
                // so it is refused out loud rather than silently answered with one of them.
                throw new InvalidOperationException(
                    "One crossing sprang traps of two different kinds that each charge the mover; " +
                    "a single crossing has one price, and no printed face composes two.");
            }

            var consequences = declarations[0];
            return consequences[Math.Min(charging.Length, consequences.Count) - 1];
        }
    }
}
