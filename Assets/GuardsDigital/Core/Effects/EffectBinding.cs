using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The closed set of things an effect input or output can be. Deliberately a fixed union over the
    /// rules domain rather than an open payload: an untyped dictionary is exactly what
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> forbids, and a closed set lets the definition validator
    /// type-check every binding before a match starts.
    /// </summary>
    public enum EffectValueKind
    {
        None = 0,
        Boolean = 1,
        Integer = 2,
        Hero = 3,
        Minion = 4,
        CardInstance = 5,
        Coordinate = 6
    }

    /// <summary>Where an input binding reads its value from.</summary>
    public enum EffectBindingSourceKind
    {
        None = 0,

        /// <summary>A constant written into the definition.</summary>
        Literal = 1,

        /// <summary>A role the execution frame supplies, such as the actor or the chosen target.</summary>
        Role = 2,

        /// <summary>A named output of an earlier node that is guaranteed to have executed.</summary>
        NodeOutput = 3
    }

    /// <summary>The roles an execution frame can bind. Named explicitly so no binding can invent one.</summary>
    public enum EffectRoleKind
    {
        None = 0,
        Actor = 1,
        SourceCard = 2,
        Controller = 3,
        Chooser = 4,
        ChosenTarget = 5,
        ChosenSpace = 6
    }

    /// <summary>Who owns a decision. The resolver never chooses on a player's behalf.</summary>
    public enum EffectChooserRole
    {
        None = 0,
        Actor = 1,
        Controller = 2,
        OpposingHero = 3
    }

    /// <summary>
    /// One typed value flowing through the effect frame. Serializable by construction: every arm is an
    /// identity or a primitive, never an object reference into live match state.
    /// </summary>
    public readonly struct EffectValue : IEquatable<EffectValue>
    {
        private EffectValue(
            EffectValueKind kind,
            bool booleanValue,
            int integerValue,
            HeroId heroId,
            MinionId minionId,
            CardInstanceId cardInstanceId,
            HexCoordinate coordinate)
        {
            Kind = kind;
            BooleanValue = booleanValue;
            IntegerValue = integerValue;
            HeroId = heroId;
            MinionId = minionId;
            CardInstanceId = cardInstanceId;
            Coordinate = coordinate;
        }

        public EffectValueKind Kind { get; }
        public bool BooleanValue { get; }
        public int IntegerValue { get; }
        public HeroId HeroId { get; }
        public MinionId MinionId { get; }
        public CardInstanceId CardInstanceId { get; }
        public HexCoordinate Coordinate { get; }

        public bool IsDefined => Kind != EffectValueKind.None;

        public static EffectValue Boolean(bool value) =>
            new EffectValue(EffectValueKind.Boolean, value, 0, default, default, default, default);

        public static EffectValue Integer(int value) =>
            new EffectValue(EffectValueKind.Integer, false, value, default, default, default, default);

        public static EffectValue Hero(HeroId value)
        {
            if (!value.IsDefined)
            {
                throw new ArgumentException("A Hero effect value requires a defined hero.", nameof(value));
            }

            return new EffectValue(EffectValueKind.Hero, false, 0, value, default, default, default);
        }

        public static EffectValue Minion(MinionId value)
        {
            if (!value.IsDefined)
            {
                throw new ArgumentException("A minion effect value requires a defined minion.", nameof(value));
            }

            return new EffectValue(EffectValueKind.Minion, false, 0, default, value, default, default);
        }

        public static EffectValue Card(CardInstanceId value)
        {
            if (!value.IsDefined)
            {
                throw new ArgumentException("A card effect value requires a defined card.", nameof(value));
            }

            return new EffectValue(EffectValueKind.CardInstance, false, 0, default, default, value, default);
        }

        public static EffectValue Space(HexCoordinate value) =>
            new EffectValue(EffectValueKind.Coordinate, false, 0, default, default, default, value);

        public bool Equals(EffectValue other)
        {
            if (Kind != other.Kind)
            {
                return false;
            }

            switch (Kind)
            {
                case EffectValueKind.Boolean:
                    return BooleanValue == other.BooleanValue;
                case EffectValueKind.Integer:
                    return IntegerValue == other.IntegerValue;
                case EffectValueKind.Hero:
                    return HeroId.Equals(other.HeroId);
                case EffectValueKind.Minion:
                    return MinionId.Equals(other.MinionId);
                case EffectValueKind.CardInstance:
                    return CardInstanceId.Equals(other.CardInstanceId);
                case EffectValueKind.Coordinate:
                    return Coordinate.Equals(other.Coordinate);
                default:
                    return true;
            }
        }

        public override bool Equals(object obj) => obj is EffectValue other && Equals(other);

        public override int GetHashCode()
        {
            switch (Kind)
            {
                case EffectValueKind.Boolean:
                    return ((int)Kind * 397) ^ (BooleanValue ? 1 : 0);
                case EffectValueKind.Integer:
                    return ((int)Kind * 397) ^ IntegerValue;
                case EffectValueKind.Hero:
                    return ((int)Kind * 397) ^ HeroId.GetHashCode();
                case EffectValueKind.Minion:
                    return ((int)Kind * 397) ^ MinionId.GetHashCode();
                case EffectValueKind.CardInstance:
                    return ((int)Kind * 397) ^ CardInstanceId.GetHashCode();
                case EffectValueKind.Coordinate:
                    return ((int)Kind * 397) ^ Coordinate.GetHashCode();
                default:
                    return (int)Kind;
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case EffectValueKind.Boolean:
                    return BooleanValue ? "true" : "false";
                case EffectValueKind.Integer:
                    return IntegerValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case EffectValueKind.Hero:
                    return HeroId.ToString();
                case EffectValueKind.Minion:
                    return MinionId.ToString();
                case EffectValueKind.CardInstance:
                    return CardInstanceId.ToString();
                case EffectValueKind.Coordinate:
                    return Coordinate.ToString();
                default:
                    return "none";
            }
        }
    }

    /// <summary>One typed parameter a node feeds its capability handler.</summary>
    public readonly struct EffectInputBinding : IEquatable<EffectInputBinding>
    {
        private EffectInputBinding(
            string parameterName,
            EffectBindingSourceKind sourceKind,
            EffectValueKind valueKind,
            EffectRoleKind role,
            EffectNodeId sourceNodeId,
            string sourceOutputName,
            EffectValue literal)
        {
            ParameterName = parameterName;
            SourceKind = sourceKind;
            ValueKind = valueKind;
            Role = role;
            SourceNodeId = sourceNodeId;
            SourceOutputName = sourceOutputName;
            Literal = literal;
        }

        public string ParameterName { get; }
        public EffectBindingSourceKind SourceKind { get; }
        public EffectValueKind ValueKind { get; }
        public EffectRoleKind Role { get; }
        public EffectNodeId SourceNodeId { get; }
        public string SourceOutputName { get; }
        public EffectValue Literal { get; }

        public static EffectInputBinding FromLiteral(string parameterName, EffectValue literal)
        {
            RequireParameterName(parameterName);
            if (!literal.IsDefined)
            {
                throw new ArgumentException(
                    "A literal input binding requires a defined value.", nameof(literal));
            }

            return new EffectInputBinding(
                parameterName, EffectBindingSourceKind.Literal, literal.Kind,
                EffectRoleKind.None, default, null, literal);
        }

        public static EffectInputBinding FromRole(
            string parameterName,
            EffectRoleKind role,
            EffectValueKind valueKind)
        {
            RequireParameterName(parameterName);
            if (role == EffectRoleKind.None)
            {
                throw new ArgumentException("A role input binding requires a role.", nameof(role));
            }

            RequireValueKind(valueKind);
            return new EffectInputBinding(
                parameterName, EffectBindingSourceKind.Role, valueKind, role, default, null, default);
        }

        public static EffectInputBinding FromNodeOutput(
            string parameterName,
            EffectNodeId sourceNodeId,
            string sourceOutputName,
            EffectValueKind valueKind)
        {
            RequireParameterName(parameterName);
            if (!sourceNodeId.IsDefined)
            {
                throw new ArgumentException(
                    "A node-output input binding requires a source node.", nameof(sourceNodeId));
            }

            if (string.IsNullOrWhiteSpace(sourceOutputName))
            {
                throw new ArgumentException(
                    "A node-output input binding requires an output name.", nameof(sourceOutputName));
            }

            RequireValueKind(valueKind);
            return new EffectInputBinding(
                parameterName, EffectBindingSourceKind.NodeOutput, valueKind,
                EffectRoleKind.None, sourceNodeId, sourceOutputName, default);
        }

        private static void RequireParameterName(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException(
                    "An effect input binding requires a parameter name.", nameof(parameterName));
            }
        }

        private static void RequireValueKind(EffectValueKind valueKind)
        {
            if (valueKind == EffectValueKind.None)
            {
                throw new ArgumentException(
                    "An effect input binding requires a typed value kind.", nameof(valueKind));
            }
        }

        public bool Equals(EffectInputBinding other) =>
            string.Equals(ParameterName, other.ParameterName, StringComparison.Ordinal) &&
            SourceKind == other.SourceKind &&
            ValueKind == other.ValueKind &&
            Role == other.Role &&
            SourceNodeId.Equals(other.SourceNodeId) &&
            string.Equals(SourceOutputName, other.SourceOutputName, StringComparison.Ordinal) &&
            Literal.Equals(other.Literal);

        public override bool Equals(object obj) => obj is EffectInputBinding other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(ParameterName ?? string.Empty) ^ ((int)SourceKind * 397);

        public override string ToString() => $"{ParameterName}:{SourceKind}";
    }

    /// <summary>One typed fact a node publishes for later nodes to bind.</summary>
    public readonly struct EffectOutputBinding : IEquatable<EffectOutputBinding>
    {
        public EffectOutputBinding(string outputName, EffectValueKind valueKind)
        {
            if (string.IsNullOrWhiteSpace(outputName))
            {
                throw new ArgumentException(
                    "An effect output binding requires a name.", nameof(outputName));
            }

            if (valueKind == EffectValueKind.None)
            {
                throw new ArgumentException(
                    "An effect output binding requires a typed value kind.", nameof(valueKind));
            }

            OutputName = outputName;
            ValueKind = valueKind;
        }

        public string OutputName { get; }
        public EffectValueKind ValueKind { get; }

        public bool Equals(EffectOutputBinding other) =>
            string.Equals(OutputName, other.OutputName, StringComparison.Ordinal) &&
            ValueKind == other.ValueKind;

        public override bool Equals(object obj) => obj is EffectOutputBinding other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(OutputName ?? string.Empty) ^ ((int)ValueKind * 397);

        public override string ToString() => $"{OutputName}:{ValueKind}";
    }

    /// <summary>One produced output value, bound to the node that produced it.</summary>
    public readonly struct EffectOutcomeFact : IEquatable<EffectOutcomeFact>
    {
        public EffectOutcomeFact(string outputName, EffectValue value)
        {
            if (string.IsNullOrWhiteSpace(outputName))
            {
                throw new ArgumentException("An effect outcome requires an output name.", nameof(outputName));
            }

            if (!value.IsDefined)
            {
                throw new ArgumentException("An effect outcome requires a defined value.", nameof(value));
            }

            OutputName = outputName;
            Value = value;
        }

        public string OutputName { get; }
        public EffectValue Value { get; }

        public bool Equals(EffectOutcomeFact other) =>
            string.Equals(OutputName, other.OutputName, StringComparison.Ordinal) &&
            Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is EffectOutcomeFact other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(OutputName ?? string.Empty) ^ Value.GetHashCode();

        public override string ToString() => $"{OutputName}={Value}";
    }
}
