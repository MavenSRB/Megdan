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

        public bool TrySelectFromPointerRay(Ray pointerRay, float maxDistance = 500f)
        {
            if (boardView == null)
            {
                return false;
            }

            var capsule = GetComponent<CapsuleCollider>();
            if (!capsule.Raycast(pointerRay, out _, maxDistance))
            {
                return false;
            }

            var hero = GetComponent<PrimitiveHeroView>();
            return boardView.TryForwardHeroIntent(hero.Occupancy.HeroId);
        }

        private void EnsureColliderShape()
        {
            var capsule = GetComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = PrimitiveHeroView.DefaultBaseRadius;
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
            if (boardView == null || !isActiveAndEnabled || minion.SetupState == null ||
                !GetComponent<SphereCollider>().Raycast(pointerRay, out _, maxDistance))
            {
                return false;
            }

            return boardView.TryForwardMinionIntent(minion.SetupState.MinionId);
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
