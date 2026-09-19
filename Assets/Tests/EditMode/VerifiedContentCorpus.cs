using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.61g. Every verified content class the build contains, and every printed face each
    /// one declares - the question five corpus sweeps ask of the assembly rather than of a list, and
    /// which they asked in five copies until a hero arrived whose set is composed by a PARALLEL WAVE.
    ///
    /// <para><b>What changed and why.</b> Every hero before the nineteenth was composed by one
    /// milestone, so the moment a <c>&lt;Hero&gt;CardContent</c> class existed it also declared
    /// <c>AllDefinitions</c>, and each sweep could read that property and assert it was there. Min's
    /// nineteen faces are composed by five blocks at once on five branches, and the aggregate is
    /// deliberately minted by none of them - it belongs to the block that assembles the set, because an
    /// aggregate declared twice is a compile error rather than a merge. So a content class can now be
    /// real, composed and half-written, and a sweep that demanded the aggregate would be demanding a
    /// member the project's own convention forbids each block to write.</para>
    ///
    /// <para><b>The guard is not weakened by this; it is widened.</b> A class declaring
    /// <c>AllDefinitions</c> is read exactly as before. A class that declares none has EVERY public
    /// static list of card definitions it does declare swept instead - which is more faces than the
    /// aggregate would have covered while the set is partial, not fewer - and a class that declares
    /// neither still fails loudly, which is the hole the assertions exist for.</para>
    /// </summary>
    internal static class VerifiedContentCorpus
    {
        /// <summary>
        /// Every static <c>*CardContent</c> class in the verified content namespace, in name order.
        /// </summary>
        internal static IEnumerable<Type> ContentClasses() =>
            typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

        /// <summary>The hero name a content class belongs to, as the sweeps label their rows.</summary>
        internal static string OwnerOf(Type contentClass) =>
            contentClass.Name
                .Substring(0, contentClass.Name.Length - "CardContent".Length)
                .ToLowerInvariant();

        /// <summary>
        /// Every printed face <paramref name="contentClass"/> declares - its <c>AllDefinitions</c>
        /// where it has one, and otherwise the union of every per-block list it does declare.
        /// </summary>
        internal static IReadOnlyList<CardDefinition> AllDefinitionsOf(Type contentClass)
        {
            var aggregate = Read(contentClass, "AllDefinitions");
            if (aggregate != null)
            {
                return aggregate;
            }

            var declared = contentClass
                .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(member =>
                    member.MemberType == MemberTypes.Property ||
                    member.MemberType == MemberTypes.Field)
                .Select(member => Read(contentClass, member.Name))
                .Where(cards => cards != null)
                .SelectMany(cards => cards)
                .GroupBy(card => card.Reference)
                .Select(group => group.First())
                .ToArray();

            Assert.That(declared, Is.Not.Empty,
                contentClass.Name + " declares no card definitions at all - neither AllDefinitions " +
                "nor a per-block list - so this sweep would skip it silently.");
            return declared;
        }

        /// <summary>
        /// Every DEALT face a content class contributes, or none where it declares no
        /// <c>DealtDefinitions</c> - which is a hero whose set is still being composed and who
        /// therefore deals nothing at all.
        /// </summary>
        internal static IReadOnlyList<CardDefinition> DealtDefinitionsOf(Type contentClass) =>
            Read(contentClass, "DealtDefinitions") ?? Array.Empty<CardDefinition>();

        private static IReadOnlyList<CardDefinition> Read(Type contentClass, string memberName)
        {
            var property = contentClass.GetProperty(
                memberName, BindingFlags.Public | BindingFlags.Static);
            var value = property != null
                ? property.GetValue(null)
                : contentClass
                    .GetField(memberName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            return value is IEnumerable<CardDefinition> cards ? cards.ToArray() : null;
        }
    }
}
