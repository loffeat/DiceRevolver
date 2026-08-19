using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class PlayerMovementAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private TopDownCharacterController player;
        [SerializeField] private Animator animator;
        [SerializeField] private string walkingParameter = "isWalking";

        private void Update()
        {
            if (player == null || animator == null)
            {
                return;
            }

            animator.SetBool(walkingParameter, player.IsMoving);
        }
    }
}
