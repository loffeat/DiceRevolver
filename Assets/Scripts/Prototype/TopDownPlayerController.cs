using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    public sealed class TopDownPlayerController : TopDownCharacterController
    {
        private Camera mainCamera;
        private Plane groundPlane;

        public override Vector3 AimWorldPoint { get; protected set; }
        public override Vector3 AimDirection { get; protected set; } = Vector3.forward;
        public override Vector2 MoveInput { get; protected set; }
        public override bool FireHeld
        {
            get => Mouse.current != null && Mouse.current.leftButton.isPressed;
            protected set { }
        }
        public override bool ReloadPressedThisFrame
        {
            get => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
            protected set { }
        }

        protected override void Awake()
        {
            base.Awake();
            mainCamera = Camera.main;
            groundPlane = new Plane(Vector3.up, Vector3.zero);
        }

        public override void RefreshControlIntent(float time)
        {
            UpdateAim();
            MoveInput = ReadMoveInput();
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

    }
}
