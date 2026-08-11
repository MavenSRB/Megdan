using GuardsDigital.Presentation.Units.Heroes;
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

        public HeroSelectionController SelectionController => selectionController;

        public void Configure(HeroSelectionController controller)
        {
            selectionController = controller;
            EnsureColliderShape();
        }

        public bool TrySelectFromPointerRay(Ray pointerRay, float maxDistance = 500f)
        {
            if (selectionController == null)
            {
                return false;
            }

            var capsule = GetComponent<CapsuleCollider>();
            if (!capsule.Raycast(pointerRay, out _, maxDistance))
            {
                return false;
            }

            return selectionController.TrySelectHero(GetComponent<PrimitiveHeroView>());
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
