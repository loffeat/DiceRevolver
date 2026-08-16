using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class FloatingArmAimVisual : MonoBehaviour
    {
        [SerializeField] private TopDownPlayerController player;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer armRenderer;
        [SerializeField] private Transform armVisual;
        [SerializeField] private int bodySortingOrder = 10;
        [SerializeField] private int armFrontSortingOrder = 12;
        [SerializeField] private int armBackSortingOrder = 8;

        private void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            Vector3 aim = player.AimDirection;
            bool aimingRight = aim.x >= 0f;
            bool aimingTowardCamera = aim.z <= 0f;

            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = aimingRight;
                bodyRenderer.sortingOrder = bodySortingOrder;
            }

            if (armRenderer != null)
            {
                armRenderer.sortingOrder = aimingTowardCamera ? armFrontSortingOrder : armBackSortingOrder;
            }

            if (armVisual != null)
            {
                Vector3 localScale = armVisual.localScale;
                localScale.y = Mathf.Abs(localScale.y) * (aimingRight ? 1f : -1f);
                armVisual.localScale = localScale;
            }
        }
    }
}
