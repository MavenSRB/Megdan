using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Every printed face one verified content class declares, for the corpus sweeps that reflect over
    /// the assembly rather than naming heroes. Milestone 8.61h.
    ///
    /// <para><b>Why one reading rather than five copies of a property name.</b> Five sweeps ask the
    /// same question of the same classes - "show me your faces" - and each of them asked it by looking
    /// for a public static <c>AllDefinitions</c>. That name is what a FINISHED set exposes, and it is
    /// minted by the block that composes the whole of one: milestone 8.57f did it for Rowenna's
    /// nineteen, and 8.61i will do it for Min's. A set being composed by several blocks at once cannot
    /// have it yet - a C# partial class cannot merge one property from several files, so the first
    /// block to declare it would own the whole list and every other block's faces would be invisible
    /// to these sweeps until somebody noticed.</para>
    ///
    /// <para><b>So this reads every face list the class declares</b> - the finished
    /// <c>AllDefinitions</c> where there is one, and otherwise every public static member whose name
    /// ends in <c>Definitions</c> and whose value is a list of <see cref="CardDefinition"/>, which is
    /// the per-block shape each partial already carries. A class that declares neither is still a
    /// failure, because a content class no sweep can see is exactly the hole these guards exist to
    /// close.</para>
    ///
    /// <para>Duplicates are removed by card id rather than by reference: a block's own list and a
    /// finished <c>AllDefinitions</c> naming the same face are one face, and a sweep that saw it twice
    /// would report one defect twice.</para>
    /// </summary>
    internal static class VerifiedCardContentFaces
    {
        /// <summary>Every verified content class the build declares, in a stable order.</summary>
        internal static IEnumerable<Type> ContentClasses() =>
            typeof(GuardsDigital.Core.Cards.Verified.WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace ==
                        typeof(GuardsDigital.Core.Cards.Verified.WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

        /// <summary>The hero name a content class belongs to, as the corpus sweeps spell it.</summary>
        internal static string OwnerOf(Type contentClass) => contentClass.Name
            .Substring(0, contentClass.Name.Length - "CardContent".Length)
            .ToLowerInvariant();

        /// <summary>
        /// Every face <paramref name="contentClass"/> declares, from whichever lists it exposes. Fails
        /// the calling case when it exposes none, so a content class this sweep cannot read is a red
        /// rather than a silent skip.
        /// </summary>
        internal static IReadOnlyList<CardDefinition> Of(Type contentClass)
        {
            if (contentClass == null)
            {
                throw new ArgumentNullException(nameof(contentClass));
            }

            var faces = new List<CardDefinition>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var declared in Lists(contentClass))
            {
                foreach (var card in declared)
                {
                    if (card != null && seen.Add(card.Id.Value))
                    {
                        faces.Add(card);
                    }
                }
            }

            Assert.That(
                faces, Is.Not.Empty,
                $"{contentClass.Name} exposes AllDefinitions or a per-block face list, or this sweep " +
                "cannot see its faces.");
            return faces;
        }

        private static IEnumerable<IEnumerable<CardDefinition>> Lists(Type contentClass)
        {
            foreach (var property in contentClass
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(property => Names(property.Name) &&
                    typeof(IEnumerable<CardDefinition>).IsAssignableFrom(property.PropertyType))
                .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                yield return (IEnumerable<CardDefinition>)property.GetValue(null) ??
                    Array.Empty<CardDefinition>();
            }

            foreach (var field in contentClass
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => Names(field.Name) &&
                    typeof(IEnumerable<CardDefinition>).IsAssignableFrom(field.FieldType))
                .OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                yield return (IEnumerable<CardDefinition>)field.GetValue(null) ??
                    Array.Empty<CardDefinition>();
            }
        }

        /// <summary>
        /// A face list is named for what it holds. <c>DealtDefinitions</c> is deliberately included -
        /// it is a subset of a finished set and its duplicates are removed above - and a member named
        /// anything else is not a list of printed faces this sweep should read.
        /// </summary>
        private static bool Names(string memberName) =>
            memberName.EndsWith("Definitions", StringComparison.Ordinal);
    }
}
