using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float turnSpeed = 24f;
        [SerializeField] private bool rotateBodyTowardAim = true;

        private CharacterController characterController;
        private Camera mainCamera;
        private Plane groundPlane;

        public Vector3 AimWorldPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public Vector2 MoveInput { get; private set; }
        public bool IsMoving => MoveInput.sqrMagnitude > 0.01f;

        private void Awake()
        {
            SnapToGameplayPlane();
            characterController = GetComponent<CharacterController>();
            mainCamera = Camera.main;
            groundPlane = new Plane(Vector3.up, Vector3.zero);
        }

        private void Update()
        {
            UpdateAim();
            Move();
            FaceAimDirection();
        }

        private void Move()
        {
            MoveInput = ReadMoveInput();
            Vector3 desiredMove = new Vector3(MoveInput.x, 0f, MoveInput.y);

            if (desiredMove.sqrMagnitude > 1f)
            {
                desiredMove.Normalize();
            }

            characterController.Move(desiredMove * moveSpeed * Time.deltaTime);
            SnapToGameplayPlane();
        }

        private void SnapToGameplayPlane()
        {
            Vector3 position = transform.position;
            position.y = 0f;
            transform.position = position;
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            input.x += keyboard.dKey.isPressed ? 1f : 0f;
            input.x -= keyboard.aKey.isPressed ? 1f : 0f;
            input.y += keyboard.wKey.isPressed ? 1f : 0f;
            input.y -= keyboard.sKey.isPressed ? 1f : 0f;
            return input;
        }

        private void UpdateAim()
        {
            Mouse mouse = Mouse.current;
            if (mainCamera == null || mouse == null)
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (!groundPlane.Raycast(ray, out float enter))
            {
                return;
            }

            AimWorldPoint = ray.GetPoint(enter);
            Vector3 aim = AimWorldPoint - transform.position;
            aim.y = 0f;

            if (aim.sqrMagnitude > 0.0001f)
            {
                AimDirection = aim.normalized;
            }
        }

        private void FaceAimDirection()
        {
            if (!rotateBodyTowardAim)
            {
                return;
            }

            if (AimDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(AimDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
