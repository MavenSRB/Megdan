using System;
using System.Reflection;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Resolves a private member a fixture reaches by name, failing at the lookup when the member does
    /// not exist rather than returning null.
    /// </summary>
    /// <remarks>
    /// The Edit Mode assembly declares its own copy; see that one for why this is duplicated rather
    /// than shared, and for the static half of the guard in
    /// <c>Tools/Validate-LivingDocumentation.ps1</c>.
    /// </remarks>
    internal static class ReflectedMember
    {
        internal static FieldInfo RequiredInstanceField(Type type, string name)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingMemberException(
                    $"{type.Name} declares no non-public instance field '{name}'. It was most likely " +
                    "renamed: the reflected name is a string the compiler cannot check.");
            }

            return field;
        }
    }
}
