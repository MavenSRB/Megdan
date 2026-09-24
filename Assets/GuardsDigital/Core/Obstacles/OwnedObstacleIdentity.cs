using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Obstacles
{
    /// <summary>A trusted obstacle kind and its placing hero identify one persistent miniature.</summary>
    public static class OwnedObstacleIdentity
    {
        public static ObstacleId For(string kind, HeroId owner)
        {
            if (string.IsNullOrWhiteSpace(kind))
                throw new ArgumentException("Obstacle kind is required.", nameof(kind));
            if (!owner.IsDefined)
                throw new ArgumentException("Obstacle owner is required.", nameof(owner));
            return new ObstacleId($"{kind}:{owner.Value}");
        }
    }
}
