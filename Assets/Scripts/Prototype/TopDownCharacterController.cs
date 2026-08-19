using UnityEngine;

namespace DiceRevolver.Prototype
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class TopDownCharacterController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float turnSpeed = 24f;
        [SerializeField] private bool rotateBodyTowardAim = true;

        private CharacterController characterController;

        public abstract Vector3 AimWorldPoint { get; protected set; }
        public abstract Vector3 AimDirection { get; protected set; }
        public abstract Vector2 MoveInput { get; protected set; }
        public abstract bool FireHeld { get; protected set; }
        public abstract bool ReloadPressedThisFrame { get; protected set; }
        public bool IsMoving => MoveInput.sqrMagnitude > 0.01f;

        protected virtual void Awake()
        {
            SnapToGameplayPlane();
            characterController = GetComponent<CharacterController>();
        }

        protected virtual void Update()
        {
            RefreshControlIntent(Time.time);
            Move(Time.deltaTime);
            FaceAimDirection(Time.deltaTime);
        }

        public abstract void RefreshControlIntent(float time);

        private void Move(float deltaTime)
        {
            Vector2 moveInput = Vector2.ClampMagnitude(MoveInput, 1f);
            Vector3 desiredMove = new Vector3(moveInput.x, 0f, moveInput.y);
            characterController.Move(desiredMove * moveSpeed * deltaTime);
            SnapToGameplayPlane();
        }

        private void FaceAimDirection(float deltaTime)
        {
            if (!rotateBodyTowardAim || AimDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(AimDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        private void SnapToGameplayPlane()
        {
            Vector3 position = transform.position;
            position.y = 0f;
            transform.position = position;
        }
    }
}
