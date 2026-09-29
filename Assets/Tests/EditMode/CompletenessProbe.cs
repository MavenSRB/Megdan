using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.11. Compares two object graphs public property by public property, so a copy,
    /// projection or wire round trip that drops a field is caught by reflection rather than by a
    /// hand-written list of the fields someone remembered.
    /// </summary>
    /// <remarks>
    /// <para><b>Why reflection and not the types' own <c>Equals</c>.</b> Milestone 8.71b's dropped
    /// <c>InitiativeOverride</c> was a field no <c>Equals</c> compared: a card's equality is its
    /// identity, and a copy that lost a fact compared equal to the original. So this walks every
    /// public instance property of every value it reaches, and only a value with no public property
    /// at all falls back to <c>Equals</c>.</para>
    /// <para><b>The allow-list is keyed <c>Type.Property</c></b>, by the property's declaring runtime
    /// type's short name, and each entry carries its reason. A key is skipped wherever that type is
    /// reached. <see cref="AssertEveryOmissionReached"/> keeps the list from going stale: an entry
    /// naming a property the check never met is itself a failure.</para>
    /// <para><b>Exercised</b> means the EXPECTED side held a non-default value at that property at
    /// least once. A property that was only ever default would compare equal however it was
    /// copied, so a check reads <see cref="Unexercised"/> and requires it to be empty or declared.</para>
    /// </remarks>
    internal sealed class CompletenessProbe
    {
        private const int MaximumDepth = 24;
        private const int MaximumReportedDifferences = 25;

        private readonly IReadOnlyDictionary<string, string> omitted;
        private readonly HashSet<string> omissionsReached = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> exercised = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> differences = new List<string>();
        private readonly SortedDictionary<string, int> differenceShapes =
            new SortedDictionary<string, int>(StringComparer.Ordinal);
        private string root = string.Empty;

        public CompletenessProbe(IReadOnlyDictionary<string, string> omitted = null)
        {
            this.omitted = omitted ?? new Dictionary<string, string>();
            foreach (var entry in this.omitted)
            {
                Assert.That(entry.Value, Is.Not.Null.And.Not.Empty,
                    "Every deliberate omission names its reason: " + entry.Key);
            }
        }

        public int Comparisons { get; private set; }

        public IReadOnlyCollection<string> Differences => differences;

        /// <summary>Every <c>Type.Property</c> the walk compared at least once.</summary>
        public IReadOnlyCollection<string> Seen => seen;

        /// <summary>Compared keys whose expected value was never anything but its default.</summary>
        public IReadOnlyList<string> Unexercised => seen
            .Where(key => !exercised.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        /// <summary>Compares two graphs, recording every difference under <paramref name="path"/>.</summary>
        public void Compare(object expected, object actual, string path)
        {
            root = path;
            CompareCore(expected, actual, path, 0, new HashSet<(object, object)>(PairComparer.Instance));
        }

        /// <summary>Compares two values of the same declared shape through the properties of
        /// <paramref name="surface"/> only, which is how two implementations of one interface are
        /// compared member by member.</summary>
        public void CompareThrough(
            Type surface, object expected, object actual, string path, ICollection<string> skipMembers = null)
        {
            root = path;
            var visited = new HashSet<(object, object)>(PairComparer.Instance);
            foreach (var property in ReadableProperties(surface)
                .Where(property => skipMembers == null || !skipMembers.Contains(property.Name)))
            {
                CompareProperty(surface, property, expected, actual, path, 0, visited);
            }
        }

        public void AssertNoDifferences(string context)
        {
            Assert.That(differences, Is.Empty,
                context + ": " + differences.Count + " field(s) did not survive, in " +
                differenceShapes.Count + " shape(s):\n  " +
                string.Join("\n  ", differenceShapes.Select(shape => shape.Key + " x" + shape.Value)) +
                "\nInstances" +
                (differences.Count > MaximumReportedDifferences ? " (first " + MaximumReportedDifferences + " shown)" : "") +
                ":\n  " + string.Join("\n  ", differences.Take(MaximumReportedDifferences)));
        }

        public void AssertEveryOmissionReached(string context)
        {
            var stale = omitted.Keys.Where(key => !omissionsReached.Contains(key))
                .OrderBy(key => key, StringComparer.Ordinal).ToArray();
            Assert.That(stale, Is.Empty,
                context + ": an allow-list entry names a property this check never met, so it no " +
                "longer excuses anything and must be removed: " + string.Join(", ", stale));
        }

        /// <summary>Every compared key was exercised, except those <paramref name="declared"/>
        /// names - and every declared key really is unexercised, so the list cannot go stale.</summary>
        public void AssertExercised(string context, IReadOnlyDictionary<string, string> declared = null)
        {
            declared = declared ?? new Dictionary<string, string>();
            var unexercised = Unexercised;
            var undeclared = unexercised.Where(key => !declared.ContainsKey(key)).ToArray();
            Assert.That(undeclared, Is.Empty,
                context + ": these properties were compared but never held a non-default value, so " +
                "a copy that dropped them would still pass. Populate them, or declare why they " +
                "cannot be: " + string.Join(", ", undeclared));
            var nowExercised = declared.Keys.Where(key => !unexercised.Contains(key))
                .OrderBy(key => key, StringComparer.Ordinal).ToArray();
            Assert.That(nowExercised, Is.Empty,
                context + ": these declared-unexercised properties are now exercised (or no longer " +
                "reached); remove them from the declaration: " + string.Join(", ", nowExercised));
        }

        private void CompareCore(
            object expected, object actual, string path, int depth, HashSet<(object, object)> visited)
        {
            Comparisons++;
            if (depth > MaximumDepth)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                if (!(expected == null && actual == null))
                {
                    Record(path, expected, actual);
                }

                return;
            }

            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            var type = expected.GetType();
            if (IsLeaf(type))
            {
                if (!expected.Equals(actual))
                {
                    Record(path, expected, actual);
                }

                return;
            }

            if (expected is IEnumerable expectedItems && !(expected is string))
            {
                if (!(actual is IEnumerable actualItems))
                {
                    Record(path, expected, actual);
                    return;
                }

                var left = expectedItems.Cast<object>().ToArray();
                var right = actualItems.Cast<object>().ToArray();
                if (left.Length != right.Length)
                {
                    Shape(path);
                    differences.Add(path + ": expected " + left.Length + " item(s), got " + right.Length);
                }

                for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
                {
                    CompareCore(left[index], right[index], path + "[" + index + "]", depth + 1, visited);
                }

                return;
            }

            if (actual.GetType() != type)
            {
                Shape(path);
                differences.Add(path + ": expected a " + type.Name + ", got a " + actual.GetType().Name);
                return;
            }

            if (!type.IsValueType && !visited.Add((expected, actual)))
            {
                return;
            }

            var properties = ReadableProperties(type);
            if (properties.Length == 0)
            {
                if (!expected.Equals(actual))
                {
                    Record(path, expected, actual);
                }

                return;
            }

            foreach (var property in properties)
            {
                CompareProperty(type, property, expected, actual, path, depth, visited);
            }
        }

        private void CompareProperty(
            Type owner,
            PropertyInfo property,
            object expected,
            object actual,
            string path,
            int depth,
            HashSet<(object, object)> visited)
        {
            var key = owner.Name + "." + property.Name;
            if (omitted.ContainsKey(key))
            {
                omissionsReached.Add(key);
                return;
            }

            seen.Add(key);
            var left = Read(property, expected);
            var right = Read(property, actual);
            if (IsNonDefault(left))
            {
                exercised.Add(key);
            }

            CompareCore(left, right, path + "." + property.Name, depth + 1, visited);
        }

        private static object Read(PropertyInfo property, object target)
        {
            try
            {
                return property.GetValue(target);
            }
            catch (TargetInvocationException exception)
            {
                // A getter that refuses (a concurrent pause's single-item reads) must refuse on both
                // sides alike; the exception's type is the value compared.
                return new ThrownBy(exception.InnerException?.GetType() ?? exception.GetType());
            }
        }

        private void Record(string path, object expected, object actual)
        {
            Shape(path);
            differences.Add(path + ": expected <" + Describe(expected) + ">, got <" + Describe(actual) + ">");
        }

        private void Shape(string path)
        {
            var shape = System.Text.RegularExpressions.Regex.Replace(
                path.StartsWith(root, StringComparison.Ordinal) ? path.Substring(root.Length) : path,
                @"[d+]", "[]");
            differenceShapes[shape] = differenceShapes.TryGetValue(shape, out var count) ? count + 1 : 1;
        }

        private static string Describe(object value)
        {
            if (value == null) return "null";
            var text = value.ToString();
            return text.Length > 120 ? text.Substring(0, 120) + "..." : text;
        }

        internal static PropertyInfo[] ReadableProperties(Type type)
        {
            var interfaces = type.IsInterface
                ? new[] { type }.Concat(type.GetInterfaces())
                : new[] { type };
            return interfaces
                .SelectMany(value => value.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod() != null)
                .GroupBy(property => property.Name)
                .Select(group => group.First())
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToArray();
        }

        internal static bool IsLeaf(Type type) =>
            type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) ||
            typeof(Type).IsAssignableFrom(type) || type == typeof(ThrownBy);

        internal static bool IsNonDefault(object value)
        {
            if (value == null) return false;
            switch (value)
            {
                case string text: return text.Length > 0;
                case bool flag: return flag;
                case ThrownBy _: return true;
                case IEnumerable items: return items.Cast<object>().Any();
            }

            var type = value.GetType();
            if (type.IsEnum) return Convert.ToInt64(value) != 0;
            if (!type.IsValueType) return true;
            try
            {
                return !value.Equals(Activator.CreateInstance(type));
            }
            catch (Exception)
            {
                // A struct whose own Equals cannot read its default (a null list inside it) is
                // non-default exactly when one of its public properties is.
                return ReadableProperties(type).Any(property =>
                {
                    try { return IsNonDefault(property.GetValue(value)); }
                    catch (TargetInvocationException) { return false; }
                });
            }
        }

        private sealed class ThrownBy : IEquatable<ThrownBy>
        {
            public ThrownBy(Type exceptionType) { ExceptionType = exceptionType; }
            private Type ExceptionType { get; }
            public bool Equals(ThrownBy other) => other != null && other.ExceptionType == ExceptionType;
            public override bool Equals(object obj) => Equals(obj as ThrownBy);
            public override int GetHashCode() => ExceptionType.GetHashCode();
            public override string ToString() => "throws " + ExceptionType.Name;
        }

        private sealed class PairComparer : IEqualityComparer<(object, object)>
        {
            public static readonly PairComparer Instance = new PairComparer();

            public bool Equals((object, object) x, (object, object) y) =>
                ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

            public int GetHashCode((object, object) obj) =>
                unchecked(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Item1) * 397 ^
                    System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Item2));
        }
    }

    /// <summary>
    /// Milestone 9.11. Builds an instance of a type with every constructor parameter set to a
    /// distinguishable non-default value: strings and numbers are drawn from one counter so no two
    /// fields share a value, enums take a defined non-zero member, collections hold two items, and
    /// nested records, identity structs and nullable wrappers are built the same way recursively.
    /// </summary>
    /// <remarks>
    /// <para>A constructor that refuses its arguments is retried with fresh values (so an enum
    /// that must name one particular member is eventually drawn), then the next constructor, then
    /// the type's public static factories. A type whose invariants no draw satisfies is given an
    /// explicit maker with <see cref="With{T}"/>, which is the one place a check states a shape by
    /// hand - and it states it for the populator, never for the comparison.</para>
    /// </remarks>
    internal sealed class CompletenessPopulator
    {
        private const int MaximumDepth = 10;
        private const int Attempts = 24;

        private readonly Dictionary<Type, Func<CompletenessPopulator, object>> makers =
            new Dictionary<Type, Func<CompletenessPopulator, object>>();
        private ulong state;
        private int counter;
        private bool mayLeaveOptionalEmpty;

        public CompletenessPopulator(int seed = 1)
        {
            counter = seed;
            state = Mix(unchecked((ulong)seed));
        }

        public CompletenessPopulator With<T>(Func<CompletenessPopulator, T> make)
        {
            makers[typeof(T)] = populator => make(populator);
            return this;
        }

        /// <summary>When set, every parameter that declares a default is passed that default, which
        /// is how the shapes an optional argument must be absent from (a Confirm answer names no
        /// unit) are reached.</summary>
        public bool OptionalParametersAtDefault { get; set; }

        public int NextInt() => ++counter;

        /// <summary>A seeded draw in [0, count), for a maker choosing among shapes.</summary>
        public int NextChoice(int count) => Draw(count);

        // SplitMix64, so neighbouring seeds draw unrelated shapes and the draw is the same on
        // every runtime the suite compiles under (Unity's and the offline harness's).
        private int Draw(int count)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                return (int)(Mix(state) % (ulong)count);
            }
        }

        private static ulong Mix(ulong value)
        {
            unchecked
            {
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        public string NextString(string prefix = "v") => prefix + NextInt();

        public T Create<T>() => (T)Create(typeof(T), 0);

        public object Create(Type type) => Create(type, 0);

        private object Create(Type type, int depth)
        {
            if (makers.TryGetValue(type, out var make)) return make(this);
            if (depth > MaximumDepth)
            {
                throw new InvalidOperationException("The populator recursed too deep at " + type.Name + ".");
            }

            if (type == typeof(string)) return NextString();
            if (type == typeof(bool)) return true;
            if (type == typeof(int)) return NextInt();
            if (type == typeof(long)) return (long)NextInt();
            if (type == typeof(short)) return (short)NextInt();
            if (type == typeof(byte)) return (byte)NextInt();
            if (type == typeof(uint)) return (uint)NextInt();
            if (type == typeof(ulong)) return (ulong)NextInt();
            if (type == typeof(ushort)) return (ushort)NextInt();
            if (type == typeof(double)) return (double)NextInt();
            if (type == typeof(float)) return (float)NextInt();
            if (type == typeof(byte[])) return new[] { (byte)NextInt(), (byte)NextInt(), (byte)NextInt() };
            if (type.IsEnum) return NextEnum(type);

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) return Create(nullable, depth + 1);

            if (type.IsArray)
            {
                return NewArray(type.GetElementType(), depth);
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();
                if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>) ||
                    definition == typeof(IReadOnlyCollection<>) || definition == typeof(IList<>) ||
                    definition == typeof(ICollection<>))
                {
                    return NewArray(arguments[0], depth);
                }

                if (definition == typeof(KeyValuePair<,>))
                {
                    return Activator.CreateInstance(
                        type, Create(arguments[0], depth + 1), Create(arguments[1], depth + 1));
                }
            }

            return Construct(type, depth);
        }

        private object NextEnum(Type type)
        {
            var values = Enum.GetValues(type).Cast<object>()
                .Where(value => Convert.ToInt64(value) != 0)
                .ToArray();
            if (values.Length == 0) return Enum.ToObject(type, 0);
            return values[Draw(values.Length)];
        }

        private Array NewArray(Type element, int depth)
        {
            var array = Array.CreateInstance(element, 2);
            array.SetValue(Create(element, depth + 1), 0);
            array.SetValue(Create(element, depth + 1), 1);
            return array;
        }

        private object Construct(Type type, int depth)
        {
            var failures = new List<string>();
            // Public constructors first, widest first; internal ones after them, because several
            // identity structs (an agreement's proposal id) are only built inside their assembly.
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(constructor => constructor.IsPublic || constructor.IsAssembly)
                .OrderByDescending(constructor => constructor.IsPublic)
                .ThenByDescending(constructor => constructor.GetParameters().Length)
                .ToArray();
            // Public and internal static factories alike: several vocabulary types (an agreement
            // statement, for one) are made only through internal factories, and the populator is
            // a test of the copy, not of who may call the factory.
            var factories = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => method.ReturnType == type && !method.IsSpecialName &&
                    !method.ContainsGenericParameters && !method.Name.Contains("<") &&
                    (method.IsPublic || method.IsAssembly) &&
                    method.GetParameters().All(parameter => !parameter.IsOut && !parameter.ParameterType.IsByRef))
                .OrderByDescending(method => method.GetParameters().Length)
                .ThenBy(method => method.Name, StringComparer.Ordinal)
                .ToArray();

            var previous = mayLeaveOptionalEmpty;
            try
            {
                for (var attempt = 0; attempt < Attempts; attempt++)
                {
                    // Late attempts may leave an optional reference empty, because some shapes
                    // (a Confirm answer carries no candidate) are valid only without one.
                    mayLeaveOptionalEmpty = attempt >= Attempts / 2 && attempt % 2 == 1;
                    foreach (var constructor in constructors)
                    {
                        if (TryInvoke(() => constructor.Invoke(Arguments(constructor.GetParameters(), depth)),
                            constructor.ToString(), failures, out var built))
                        {
                            return built;
                        }
                    }

                    if (factories.Length > 0)
                    {
                        var factory = factories[Draw(factories.Length)];
                        if (TryInvoke(() => factory.Invoke(null, Arguments(factory.GetParameters(), depth)),
                            factory.Name, failures, out var made))
                        {
                            return made;
                        }
                    }
                }
            }
            finally
            {
                mayLeaveOptionalEmpty = previous;
            }

            if (type.IsValueType && constructors.Length == 0 && factories.Length == 0)
            {
                return Activator.CreateInstance(type);
            }

            throw new InvalidOperationException(
                "The populator could not build a " + type.FullName + "; give it a maker. Last refusals:\n  " +
                string.Join("\n  ", failures.Skip(Math.Max(0, failures.Count - 4))));
        }

        private object[] Arguments(ParameterInfo[] parameters, int depth)
        {
            var leaveEmpty = mayLeaveOptionalEmpty;
            return parameters.Select(parameter =>
                OptionalParametersAtDefault && parameter.IsOptional
                    ? (parameter.HasDefaultValue && parameter.DefaultValue != null
                        ? parameter.DefaultValue
                        : (parameter.ParameterType.IsValueType
                            ? Activator.CreateInstance(parameter.ParameterType)
                            : null)) :
                leaveEmpty && parameter.ParameterType != typeof(string) &&
                    !parameter.ParameterType.IsPrimitive && !parameter.ParameterType.IsEnum &&
                    Draw(2) == 0
                    ? (parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null)
                    : Create(parameter.ParameterType, depth + 1)).ToArray();
        }

        private static bool TryInvoke(
            Func<object> invoke, string name, List<string> failures, out object built)
        {
            try
            {
                built = invoke();
                return built != null;
            }
            catch (TargetInvocationException exception)
            {
                failures.Add(name + ": " + exception.InnerException.Message);
                built = null;
                return false;
            }
            catch (InvalidOperationException exception)
            {
                failures.Add(name + ": " + exception.Message);
                built = null;
                return false;
            }
        }
    }
}
