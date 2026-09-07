using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using GuardsDigital.Presentation.Match;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrimitiveHeroView))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PrimitiveHeroPointerSelector : MonoBehaviour
    {
        private BoardInteractionUnityView boardView;

        public void Configure(BoardInteractionUnityView passiveBoardView)
        {
            boardView = passiveBoardView ?? throw new System.ArgumentNullException(nameof(passiveBoardView));
            EnsureColliderShape();
        }

        /// <summary>
        /// Whether this hero is an ANSWER to something the match is currently asking, and if so,
        /// answers it. A ray that hits the piece but that the authority has no use for returns false
        /// here, so the chain this sits in keeps looking.
        /// </summary>
        public bool TrySelectFromPointerRay(Ray pointerRay, float maxDistance = 500f)
        {
            if (!IsUnderPointer(pointerRay, maxDistance))
            {
                return false;
            }

            var hero = GetComponent<PrimitiveHeroView>();
            return boardView.TryForwardHeroIntent(hero.Occupancy.HeroId);
        }

        /// <summary>
        /// Milestone 8.35z48. The same pick, asked as the OTHER question: nothing on this ray is an
        /// answer to anything, so does clicking this hero at least mean "show me that hero's tray"?
        ///
        /// <para>It is deliberately a second method rather than a fallback inside
        /// <see cref="TrySelectFromPointerRay"/> above. Pointing the tray succeeds for almost any
        /// hero, so a chain that asks one selector both questions at once can never get past its
        /// first hit - which is the whole of the defect this milestone removes. Splitting them lets
        /// <see cref="PrimitiveHeroPointerInput"/> ask every authority-backed question on the ray
        /// first and only then come back for this one.</para>
        /// </summary>
        public bool TryViewFromPointerRay(Ray pointerRay, float maxDistance = 500f)
        {
            if (!IsUnderPointer(pointerRay, maxDistance))
            {
                return false;
            }

            var hero = GetComponent<PrimitiveHeroView>();
            return boardView.TryForwardHeroViewIntent(hero.Occupancy.HeroId);
        }

        private bool IsUnderPointer(Ray pointerRay, float maxDistance)
        {
            if (boardView == null)
            {
                return false;
            }

            var capsule = GetComponent<CapsuleCollider>();
            return capsule.Raycast(pointerRay, out _, maxDistance);
        }

        public bool TryGetHeroFromPointerRay(
            Ray pointerRay, out GuardsDigital.Core.Units.Heroes.HeroId heroId,
            float maxDistance = 500f)
        {
            heroId = default;
            if (!isActiveAndEnabled ||
                !GetComponent<CapsuleCollider>().Raycast(pointerRay, out _, maxDistance))
                return false;
            heroId = GetComponent<PrimitiveHeroView>().Occupancy.HeroId;
            return heroId.IsDefined;
        }

        /// <summary>
        /// Builds the pointer target from the piece the hero is actually drawn as.
        ///
        /// Milestone 8.11b: <see cref="PrimitiveHeroView.SelectionRadius"/> rather than the prism's own
        /// base radius, because the official token covers its space and is wider than the prism it
        /// replaces. It is the same rule milestone 8.11a recorded when it raised the prism: the pointer
        /// target grows with the piece, so picking stays exactly what a player sees. The hero this
        /// forwards is unchanged either way - it is read from the view's own occupancy, never from the
        /// collider.
        /// </summary>
        private void EnsureColliderShape()
        {
            var hero = GetComponent<PrimitiveHeroView>();
            var capsule = GetComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = hero != null ? hero.SelectionRadius : PrimitiveHeroView.DefaultBaseRadius;
            capsule.height = PrimitiveHeroView.DefaultHeight;
            capsule.center = Vector3.up * (PrimitiveHeroView.DefaultHeight * 0.5f);
        }

        private void Awake()
        {
            EnsureColliderShape();
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(StandardMinionView))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class StandardMinionPointerSelector : MonoBehaviour
    {
        private BoardInteractionUnityView boardView;

        public void Configure(BoardInteractionUnityView passiveBoardView)
        {
            boardView = passiveBoardView ?? throw new System.ArgumentNullException(nameof(passiveBoardView));
            EnsureColliderShape();
        }

        public bool TrySelectFromPointerRay(Ray pointerRay, float maxDistance = 500f)
        {
            var minion = GetComponent<StandardMinionView>();
            if (boardView == null || !isActiveAndEnabled || minion.Occupancy == null ||
                !GetComponent<SphereCollider>().Raycast(pointerRay, out _, maxDistance))
            {
                return false;
            }

            return boardView.TryForwardMinionIntent(minion.Occupancy.MinionId);
        }

        private void EnsureColliderShape()
        {
            var sphere = GetComponent<SphereCollider>();
            sphere.radius = 0.35f;
            sphere.center = Vector3.zero;
        }

        private void Awake()
        {
            EnsureColliderShape();
        }
    }
}
