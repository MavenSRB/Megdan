using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Match;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrimitiveHeroView))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PrimitiveHeroPointerSelector : MonoBehaviour
    {
        [SerializeField]
        private HeroSelectionController selectionController;

        private BoardInteractionUnityView boardView;

        public HeroSelectionController SelectionController => selectionController;

        public void Configure(HeroSelectionController controller)
        {
            boardView = null;
            selectionController = controller;
            EnsureColliderShape();
        }

        public void Configure(BoardInteractionUnityView passiveBoardView)
        {
            boardView = passiveBoardView ?? throw new System.ArgumentNullException(nameof(passiveBoardView));
            selectionController = null;
            EnsureColliderShape();
        }

        public bool TrySelectFromPointerRay(Ray pointerRay, float maxDistance = 500f)
        {
            if (boardView == null && selectionController == null)
            {
                return false;
            }

            var capsule = GetComponent<CapsuleCollider>();
            if (!capsule.Raycast(pointerRay, out _, maxDistance))
            {
                return false;
            }

            var hero = GetComponent<PrimitiveHeroView>();
            return boardView != null
                ? boardView.TryForwardHeroIntent(hero.Occupancy.HeroId)
                : selectionController.TrySelectHero(hero);
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
}
