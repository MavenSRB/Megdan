using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public enum CoordinationTargetKind { Hex = 1, Card = 2, Cursor = 3 }

    /// <summary>Explicit set/clear intent. The admitted connection supplies the sender.</summary>
    public sealed class CoordinationCommand
    {
        public CoordinationCommand(long expectedRevision, CoordinationTargetKind kind,
            bool clear, MapDefinitionReference map, HexCoordinate hex = default,
            HeroId cardOwner = default, CardInstanceId card = default, int generation = 0)
        {
            if (expectedRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
            if (!Enum.IsDefined(typeof(CoordinationTargetKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (generation < 0) throw new ArgumentOutOfRangeException(nameof(generation));
            Generation = generation; ExpectedRevision = expectedRevision; Kind = kind; Clear = clear;
            Map = map; Hex = hex; CardOwner = cardOwner; Card = card;
        }
        public int Generation { get; }
        public long ExpectedRevision { get; }
        public CoordinationTargetKind Kind { get; }
        public bool Clear { get; }
        public MapDefinitionReference Map { get; }
        public HexCoordinate Hex { get; }
        public HeroId CardOwner { get; }
        public CardInstanceId Card { get; }
    }

    public sealed class TeammateMarker
    {
        public TeammateMarker(HeroId sender, long revision, HexCoordinate? hex,
            HeroId cardOwner, CardInstanceId card, HexCoordinate? cursor = null)
        { Sender = sender; Revision = revision; Hex = hex; CardOwner = cardOwner; Card = card; Cursor = cursor; }
        public HexCoordinate? Cursor { get; }
        public HeroId Sender { get; }
        public long Revision { get; }
        public HexCoordinate? Hex { get; }
        public HeroId CardOwner { get; }
        public CardInstanceId Card { get; }
    }

    public interface IExperimentalReadModel
    {
        ExperimentalMatchOptions Experiments { get; }
        MatchClockState Clock { get; }
        IReadOnlyList<CardInstance> AlliedHandCards { get; }
        IReadOnlyList<HeroCardInspection> CardInspections { get; }
        IReadOnlyList<TeammateMarker> TeammateMarkers { get; }
        long CoordinationRevision { get; }
        int CoordinationGeneration { get; }
    }
    public interface ICoordinationClient { bool SubmitCoordination(CoordinationCommand command); }

    internal sealed class TeamCoordination
    {
        private readonly Dictionary<HeroId, TeammateMarker> markers = new Dictionary<HeroId, TeammateMarker>();
        public long Revision { get; private set; }
        public int Generation { get; private set; }
        private MapDefinitionReference? currentMap;
        public IReadOnlyList<TeammateMarker> ForSeat(MatchSeat seat, MatchSeatBinding seats) =>
            Array.AsReadOnly(markers.Values.Where(m => seats.Seats.Any(s =>
                s.Owns(m.Sender) && s.TeamSide == seat.TeamSide)).OrderBy(m => m.Sender.Value, StringComparer.Ordinal).ToArray());
        public bool Submit(MatchSeat sender, CoordinationCommand command, MatchSessionProjection source,
            MatchSeatBinding seats)
        {
            if (sender == null || command == null || !command.Map.Equals(source.SelectedMap.Reference)) return false;
            Revalidate(source, seats);
            if (command.Generation != Generation) return false;
            markers.TryGetValue(sender.HeroId, out var previous);
            if (command.Kind == CoordinationTargetKind.Cursor)
            {
                if (!command.Clear && !source.SelectedMap.Spaces.Any(s => s.Coordinate.Equals(command.Hex))) return false;
                var cursor = command.Clear ? (HexCoordinate?)null : command.Hex;
                if (Nullable.Equals(cursor, previous?.Cursor)) return true;
                markers[sender.HeroId] = new TeammateMarker(sender.HeroId, previous?.Revision ?? 0,
                    previous?.Hex, previous?.CardOwner ?? default, previous?.Card ?? default, cursor);
                Revision++;
                return true;
            }
            if (command.ExpectedRevision != (previous?.Revision ?? 0)) return false;
            if (!command.Clear && command.Kind == CoordinationTargetKind.Hex &&
                !source.SelectedMap.Spaces.Any(s => s.Coordinate.Equals(command.Hex))) return false;
            if (!command.Clear && command.Kind == CoordinationTargetKind.Card &&
                (!seats.Seats.Any(s => s.Owns(command.CardOwner) && s.TeamSide == sender.TeamSide && !s.Owns(sender.HeroId)) ||
                 !source.Cards.Any(c => c.Id.Equals(command.Card) && c.OwnerId.Equals(command.CardOwner) && c.State.Location == CardLocation.Hand))) return false;
            var hex = command.Kind == CoordinationTargetKind.Hex ? (command.Clear ? (HexCoordinate?)null : command.Hex) : previous?.Hex;
            var owner = command.Kind == CoordinationTargetKind.Card ? (command.Clear ? default : command.CardOwner) : previous?.CardOwner ?? default;
            var card = command.Kind == CoordinationTargetKind.Card ? (command.Clear ? default : command.Card) : previous?.Card ?? default;
            markers[sender.HeroId] = new TeammateMarker(sender.HeroId, (previous?.Revision ?? 0) + 1, hex, owner, card, previous?.Cursor);
            Revision++;
            return true;
        }
        public void Revalidate(MatchSessionProjection source, MatchSeatBinding seats)
        {
            if (currentMap.HasValue && !currentMap.Value.Equals(source.SelectedMap.Reference)) Clear();
            currentMap = source.SelectedMap.Reference;
            foreach (var marker in markers.Values.ToArray())
            {
                if (!seats.Seats.Any(s => s.Owns(marker.Sender))) { markers.Remove(marker.Sender); Revision++; continue; }
                var hex = marker.Hex.HasValue && source.SelectedMap.Spaces.Any(s => s.Coordinate.Equals(marker.Hex.Value)) ? marker.Hex : null;
                var sender = seats.Seats.First(s => s.Owns(marker.Sender));
                var validCard = marker.Card.IsDefined && seats.Seats.Any(s => s.Owns(marker.CardOwner) && s.TeamSide == sender.TeamSide) &&
                    source.Cards.Any(c => c.Id.Equals(marker.Card) && c.OwnerId.Equals(marker.CardOwner) && c.State.Location == CardLocation.Hand);
                if (!Nullable.Equals(hex, marker.Hex) || (marker.Card.IsDefined && !validCard))
                {
                    markers[marker.Sender] = new TeammateMarker(marker.Sender, marker.Revision + 1, hex,
                        validCard ? marker.CardOwner : default, validCard ? marker.Card : default, marker.Cursor);
                    Revision++;
                }
            }
        }
        public void Clear() { markers.Clear(); Revision++; Generation++; }
        public void Remove(HeroId sender) { if (markers.Remove(sender)) Revision++; }
    }
}
