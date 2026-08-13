using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1k, finding F613-13. Reads what compiled code actually references instead of what
    /// its source text happens to spell.
    ///
    /// The architecture fixtures used to ask "does this file contain the substring
    /// <c>MovementReachabilityQuery.FindReachableDestinations(</c>". That question has both failure
    /// modes at once: a pure rename breaks the assertion while changing no behavior, and a
    /// fully-qualified or aliased call slips past it while changing behavior. Milestone 6.1h measured
    /// the second one - a fully-qualified <c>new GuardsDigital.Core.Rules.Movement.MovementSession(</c>
    /// in the dispatcher evaded every token - and recorded it as this milestone's subject.
    ///
    /// This scanner asks the compiled question instead: does any method on this type emit a call whose
    /// resolved member is that member. Both sides of the comparison come from the type system, so a
    /// rename moves them together, and the spelling at the call site is irrelevant because the
    /// compiler has already erased it.
    ///
    /// Test infrastructure only. Nothing here is referenced by production code.
    /// </summary>
    public static class ILConformanceScanner
    {
        private const BindingFlags DeclaredMembers =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .GroupBy(opCode => opCode.Value)
            .ToDictionary(group => group.Key, group => group.First());

        /// <summary>Every method body declared by these types, including compiler-generated nested
        /// types, so a call made inside a lambda or an iterator is not invisible.</summary>
        public static IReadOnlyList<ScannedMethod> Scan(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return types
                .SelectMany(WithNestedTypes)
                .Distinct()
                .SelectMany(DeclaredBodies)
                .Select(Read)
                .ToArray();
        }

        public static IReadOnlyList<ScannedMethod> Scan(Type type) => Scan(new[] { type });

        /// <summary>
        /// One violation line per forbidden member each scanned type references. The forbidden set is
        /// supplied as resolved <see cref="MemberInfo"/>s, never as names.
        /// </summary>
        public static IReadOnlyList<string> FindForbiddenReferences(
            IEnumerable<Type> types,
            IEnumerable<MemberInfo> forbiddenMembers,
            string description)
        {
            var forbidden = (forbiddenMembers ?? throw new ArgumentNullException(nameof(forbiddenMembers)))
                .Select(Identity)
                .ToArray();

            return Scan(types)
                .SelectMany(method => method.Instructions
                    .Where(instruction => instruction.Member != null)
                    .Where(instruction => forbidden.Contains(Identity(instruction.Member)))
                    .Select(instruction => $"{method.Description}: {description}: {Describe(instruction.Member)}"))
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every token this scan could not resolve. A rule cannot be evaded through a construct the
        /// scanner silently skipped, so callers assert this is empty rather than ignoring it.
        /// </summary>
        public static IReadOnlyList<string> UnresolvedTokens(IEnumerable<Type> types) =>
            Scan(types).SelectMany(method => method.UnresolvedTokens).ToArray();

        public static bool References(IEnumerable<Type> types, MemberInfo member)
        {
            var identity = Identity(member ?? throw new ArgumentNullException(nameof(member)));
            return Scan(types).Any(method => method.Instructions
                .Any(instruction => instruction.Member != null && Identity(instruction.Member) == identity));
        }

        /// <summary>
        /// Resolves a member that must exist. Every rule names its forbidden members this way, so a
        /// member that is renamed or deleted fails loudly here instead of quietly disarming a rule.
        /// </summary>
        public static MethodInfo Method(Type type, string name, params Type[] parameterTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var candidates = type.GetMethods(DeclaredMembers | BindingFlags.FlattenHierarchy)
                .Where(method => string.Equals(method.Name, name, StringComparison.Ordinal))
                .Where(method => parameterTypes == null || parameterTypes.Length == 0 ||
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(parameterTypes))
                .ToArray();

            if (candidates.Length == 0)
            {
                throw new ArgumentException(
                    $"{type.Name} declares no method '{name}'. A conformance rule cannot name a member " +
                    "that does not exist; correct the rule rather than deleting it.");
            }

            return candidates[0];
        }

        public static IEnumerable<MethodInfo> Methods(Type type, string name) =>
            (type ?? throw new ArgumentNullException(nameof(type)))
            .GetMethods(DeclaredMembers | BindingFlags.FlattenHierarchy)
            .Where(method => string.Equals(method.Name, name, StringComparison.Ordinal));

        public static IEnumerable<ConstructorInfo> Constructors(Type type) =>
            (type ?? throw new ArgumentNullException(nameof(type))).GetConstructors(DeclaredMembers);

        /// <summary>
        /// Every type an assembly defines, including nested and compiler-generated ones. A partially
        /// loadable assembly yields what it can rather than throwing, so one bad type cannot silence
        /// every rule that scans it.
        /// </summary>
        public static IReadOnlyList<Type> TypesIn(Assembly assembly)
        {
            try
            {
                return (assembly ?? throw new ArgumentNullException(nameof(assembly))).GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }

        public static ConstructorInfo Constructor(Type type, params Type[] parameterTypes)
        {
            var constructor = (type ?? throw new ArgumentNullException(nameof(type)))
                .GetConstructors(DeclaredMembers)
                .FirstOrDefault(candidate => parameterTypes == null || parameterTypes.Length == 0 ||
                    candidate.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(parameterTypes));

            if (constructor == null)
            {
                throw new ArgumentException(
                    $"{type.Name} declares no matching constructor. A conformance rule cannot name a " +
                    "member that does not exist; correct the rule rather than deleting it.");
            }

            return constructor;
        }

        public static PropertyInfo Property(Type type, string name)
        {
            var property = (type ?? throw new ArgumentNullException(nameof(type)))
                .GetProperty(name, DeclaredMembers | BindingFlags.FlattenHierarchy);
            if (property == null)
            {
                throw new ArgumentException(
                    $"{type.Name} declares no property '{name}'. A conformance rule cannot name a " +
                    "member that does not exist; correct the rule rather than deleting it.");
            }

            return property;
        }

        public static string Describe(MemberInfo member) =>
            member == null ? "<unresolved>" : $"{member.DeclaringType?.FullName ?? "?"}.{member.Name}";

        public static IEnumerable<Type> WithNestedTypes(Type type)
        {
            if (type == null)
            {
                yield break;
            }

            yield return type;
            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var descendant in WithNestedTypes(nested))
                {
                    yield return descendant;
                }
            }
        }

        private static IEnumerable<MethodBase> DeclaredBodies(Type type)
        {
            IEnumerable<MethodBase> members;
            try
            {
                members = type.GetMethods(DeclaredMembers)
                    .Cast<MethodBase>()
                    .Concat(type.GetConstructors(DeclaredMembers));
            }
            catch (TypeLoadException)
            {
                yield break;
            }

            foreach (var member in members)
            {
                if (member.IsAbstract ||
                    (member.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0)
                {
                    continue;
                }

                yield return member;
            }
        }

        private static ScannedMethod Read(MethodBase method)
        {
            var instructions = new List<ScannedInstruction>();
            var unresolved = new List<string>();
            byte[] il = null;
            try
            {
                il = method.GetMethodBody()?.GetILAsByteArray();
            }
            catch (Exception exception)
            {
                unresolved.Add($"{DescribeMethod(method)}: body unavailable ({exception.GetType().Name})");
            }

            if (il == null)
            {
                return new ScannedMethod(method, instructions, unresolved);
            }

            var position = 0;
            var index = 0;
            while (position < il.Length)
            {
                short code = il[position++];
                if (code == 0xFE && position < il.Length)
                {
                    code = unchecked((short)(0xFE00 | il[position++]));
                }

                if (!OpCodesByValue.TryGetValue(code, out var opCode))
                {
                    unresolved.Add($"{DescribeMethod(method)}: unknown opcode 0x{code:X4} at byte {position - 1}");
                    break;
                }

                MemberInfo member = null;
                string text = null;
                switch (opCode.OperandType)
                {
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        position += 1;
                        break;
                    case OperandType.InlineVar:
                        position += 2;
                        break;
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                    case OperandType.InlineSig:
                        position += 4;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        position += 8;
                        break;
                    case OperandType.InlineString:
                        text = ResolveString(method, BitConverter.ToInt32(il, position), unresolved);
                        position += 4;
                        break;
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        member = ResolveMember(method, BitConverter.ToInt32(il, position), unresolved);
                        position += 4;
                        break;
                    case OperandType.InlineSwitch:
                        var caseCount = BitConverter.ToInt32(il, position);
                        position += 4 + (4 * caseCount);
                        break;
                    default:
                        unresolved.Add(
                            $"{DescribeMethod(method)}: unhandled operand type {opCode.OperandType} for {opCode.Name}");
                        position = il.Length;
                        break;
                }

                instructions.Add(new ScannedInstruction(index++, opCode, member, text));
            }

            return new ScannedMethod(method, instructions, unresolved);
        }

        private static MemberInfo ResolveMember(MethodBase method, int token, ICollection<string> unresolved)
        {
            try
            {
                return method.Module.ResolveMember(token, GenericTypeArguments(method), GenericMethodArguments(method));
            }
            catch (Exception exception)
            {
                unresolved.Add(
                    $"{DescribeMethod(method)}: member token 0x{token:X8} ({exception.GetType().Name})");
                return null;
            }
        }

        private static string ResolveString(MethodBase method, int token, ICollection<string> unresolved)
        {
            try
            {
                return method.Module.ResolveString(token);
            }
            catch (Exception exception)
            {
                unresolved.Add(
                    $"{DescribeMethod(method)}: string token 0x{token:X8} ({exception.GetType().Name})");
                return null;
            }
        }

        private static Type[] GenericTypeArguments(MethodBase method) =>
            method.DeclaringType != null && method.DeclaringType.IsGenericType
                ? method.DeclaringType.GetGenericArguments()
                : null;

        private static Type[] GenericMethodArguments(MethodBase method) =>
            method.IsGenericMethod ? method.GetGenericArguments() : null;

        /// <summary>
        /// Module plus metadata token. A constructed generic method and its definition share a
        /// metadata row, so <c>FindAnyObjectByType&lt;Camera&gt;()</c> matches the rule that names
        /// <c>FindAnyObjectByType</c> without the rule having to enumerate instantiations.
        /// </summary>
        private static string Identity(MemberInfo member)
        {
            try
            {
                return $"{member.Module.ModuleVersionId:N}:{member.MetadataToken:X8}";
            }
            catch (InvalidOperationException)
            {
                return $"{member.DeclaringType?.FullName}.{member.Name}";
            }
        }

        private static string DescribeMethod(MethodBase method) =>
            $"{method.DeclaringType?.FullName ?? "?"}.{method.Name}";

        public readonly struct ScannedInstruction
        {
            public ScannedInstruction(int index, OpCode opCode, MemberInfo member, string text)
            {
                Index = index;
                OpCode = opCode;
                Member = member;
                Text = text;
            }

            /// <summary>Ordinal position in the body, so "the guard precedes the use" is expressible.</summary>
            public int Index { get; }
            public OpCode OpCode { get; }
            public MemberInfo Member { get; }
            public string Text { get; }
        }

        public sealed class ScannedMethod
        {
            public ScannedMethod(
                MethodBase method,
                IReadOnlyList<ScannedInstruction> instructions,
                IReadOnlyList<string> unresolvedTokens)
            {
                Method = method;
                Instructions = instructions;
                UnresolvedTokens = unresolvedTokens;
            }

            public MethodBase Method { get; }
            public IReadOnlyList<ScannedInstruction> Instructions { get; }
            public IReadOnlyList<string> UnresolvedTokens { get; }
            public string Description => DescribeMethod(Method);

            public bool Calls(MemberInfo member)
            {
                var identity = Identity(member);
                return Instructions.Any(instruction =>
                    instruction.Member != null && Identity(instruction.Member) == identity);
            }

            public int FirstIndexOf(MemberInfo member)
            {
                var identity = Identity(member);
                foreach (var instruction in Instructions)
                {
                    if (instruction.Member != null && Identity(instruction.Member) == identity)
                    {
                        return instruction.Index;
                    }
                }

                return -1;
            }

            public int FirstIndexOf(OpCode opCode)
            {
                foreach (var instruction in Instructions)
                {
                    if (instruction.OpCode == opCode)
                    {
                        return instruction.Index;
                    }
                }

                return -1;
            }

            /// <summary>
            /// String literals that feed a call to <paramref name="member"/>. The literal is the last
            /// <c>ldstr</c> before the call, which is how a compiler emits a constant argument.
            /// </summary>
            public IEnumerable<string> StringsPassedTo(MemberInfo member)
            {
                var identity = Identity(member);
                string pending = null;
                foreach (var instruction in Instructions)
                {
                    if (instruction.Text != null)
                    {
                        pending = instruction.Text;
                        continue;
                    }

                    if (instruction.Member != null && Identity(instruction.Member) == identity && pending != null)
                    {
                        yield return pending;
                    }

                    if (instruction.Member != null)
                    {
                        pending = null;
                    }
                }
            }
        }
    }
}
