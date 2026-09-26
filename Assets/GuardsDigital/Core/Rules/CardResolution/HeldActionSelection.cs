using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>Already selected command arguments held while a before-action rule asks a choice.</summary>
    public sealed class HeldActionSelection
    {
        public HeldActionSelection(ActionKind kind, ActionDefinitionId replacedActionId = default,
            HexCoordinate? destination = null, IEnumerable<TokenInstanceId> tokens = null,
            ActionResolutionStepId defenseChoiceId = default,
            CardInstanceId defenseCardId = default,
            bool resumesOwedAfterActionRepeat = false,
            bool resumesOwedAttackRepeat = false)
        {
            if (resumesOwedAfterActionRepeat && resumesOwedAttackRepeat)
                throw new ArgumentException("A held selection resumes exactly one owed repeat.");
            var resumesRepeat = resumesOwedAfterActionRepeat || resumesOwedAttackRepeat;
            if ((!resumesRepeat && kind != ActionKind.Hold && kind != ActionKind.FastTravel &&
                    kind != ActionKind.Clear && kind != ActionKind.Defense) ||
                (resumesOwedAttackRepeat && kind != ActionKind.Attack))
                throw new ArgumentException("Only a general action or Defense carries held selections.");
            if ((!resumesRepeat && (kind == ActionKind.FastTravel || kind == ActionKind.Clear)) !=
                    replacedActionId.IsDefined ||
                (!resumesRepeat && kind == ActionKind.FastTravel) != destination.HasValue ||
                (!resumesRepeat && kind == ActionKind.Defense) != defenseChoiceId.IsDefined ||
                (!resumesRepeat && kind == ActionKind.Defense) != defenseCardId.IsDefined)
                throw new ArgumentException("Held selection arguments disagree with their action kind.");
            var selected = (tokens ?? Array.Empty<TokenInstanceId>()).ToArray();
            if (selected.Any(token => !token.IsDefined) ||
                selected.Distinct().Count() != selected.Length ||
                ((!resumesRepeat && kind != ActionKind.Clear) || resumesRepeat) && selected.Length != 0)
                throw new ArgumentException("Held Clear selections must be distinct defined token identities.");
            Kind = kind;
            ReplacedActionId = replacedActionId;
            Destination = destination;
            Tokens = Array.AsReadOnly(selected);
            DefenseChoiceId = defenseChoiceId;
            DefenseCardId = defenseCardId;
            ResumesOwedAfterActionRepeat = resumesOwedAfterActionRepeat;
            ResumesOwedAttackRepeat = resumesOwedAttackRepeat;
        }

        public ActionKind Kind { get; }
        public ActionDefinitionId ReplacedActionId { get; }
        public HexCoordinate? Destination { get; }
        public IReadOnlyList<TokenInstanceId> Tokens { get; }
        public ActionResolutionStepId DefenseChoiceId { get; }
        public CardInstanceId DefenseCardId { get; }
        public bool ResumesOwedAfterActionRepeat { get; }
        public bool ResumesOwedAttackRepeat { get; }
    }
}
