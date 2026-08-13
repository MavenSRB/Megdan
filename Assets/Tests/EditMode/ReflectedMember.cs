using System;
using System.Reflection;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Resolves a private member a fixture reaches by name, failing at the lookup when the member does
    /// not exist rather than returning null.
    /// </summary>
    /// <remarks>
    /// A reflected name is a string, so no compiler checks it. Milestone 6.1s renamed
    /// <c>StandardMinionView.setupState</c> to <c>occupancy</c> and three fixtures kept the old string;
    /// each would have failed later, on a null dereference, naming neither the type nor the member. The
    /// static half of this guard is code-truth check 5 in <c>Tools/Validate-LivingDocumentation.ps1</c>,
    /// which catches the rename before any Unity run - including on the non-Windows host, where these
    /// fixtures cannot run at all. This is the runtime half: when a name does slip through, the failure
    /// says which type and which member.
    ///
    /// Deliberately duplicated in the Play Mode assembly rather than shared. A common test-utility
    /// assembly would be shared test infrastructure, which escalates any milestone touching it to T3
    /// under Docs/AI/Testing/POLICY.md - too much ceremony for one method and three call sites.
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
