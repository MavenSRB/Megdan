using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>A grant-specific target exclusion, not immunity and not a presence restriction.</summary>
    public readonly struct ActionTargetExclusion
    {
        public ActionTargetExclusion(HeroId actor, RuleObject target)
        {
            if (!actor.IsDefined || (target.Kind != RuleObjectKind.Hero && target.Kind != RuleObjectKind.Minion))
                throw new ArgumentException("A borrowed Skill exclusion names its actor and one unit.");
            ActorId = actor; Target = target;
        }
        public HeroId ActorId { get; }
        public RuleObject Target { get; }
    }

    /// <summary>
    /// The existing serialized turn-key set holds the unit an Attack-granted Skill cannot affect.
    /// The scope ends when that card's whole action/borrow chain resolves, not after its first choice.
    /// Encoded identity segments preserve arbitrary stable IDs without delimiter ambiguity.
    /// </summary>
    public static class BorrowedSkillTargetScope
    {
        public const string Prefix = "borrowed-skill-target:";
        public static string Key(HeroId actor, RuleObject target)
        {
            var exclusion = new ActionTargetExclusion(actor, target);
            return Prefix + Encode(exclusion.ActorId.Value) + ":" +
                (target.Kind == RuleObjectKind.Hero ? "hero:" + Encode(target.HeroId.Value) : "minion:" + Encode(target.MinionId.Value));
        }
        public static bool ContainsScope(IEnumerable<string> keys) => keys != null && keys.Any(IsScope);
        public static bool IsScope(string key) => key != null && key.StartsWith(Prefix, StringComparison.Ordinal);
        public static IReadOnlyList<ActionTargetExclusion> Read(IEnumerable<string> keys) =>
            (keys ?? Array.Empty<string>()).Where(IsScope).OrderBy(k => k, StringComparer.Ordinal).Select(ReadKey).ToArray();
        private static ActionTargetExclusion ReadKey(string key)
        {
            var parts = key.Substring(Prefix.Length).Split(':');
            if (parts.Length != 3) throw new ArgumentException("Malformed borrowed Skill target scope.");
            var actor = new HeroId(Decode(parts[0]));
            var target = parts[1] == "hero" ? RuleObject.ForHero(new HeroId(Decode(parts[2]))) :
                parts[1] == "minion" ? RuleObject.ForMinion(new MinionId(Decode(parts[2]))) :
                throw new ArgumentException("A borrowed Skill target scope names a unit kind.");
            return new ActionTargetExclusion(actor, target);
        }
        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
}
