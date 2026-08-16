using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceRevolver.Prototype
{
    public sealed class GunController : MonoBehaviour
    {
        [SerializeField] private TopDownPlayerController player;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float holdDistance = 0.85f;
        [SerializeField] private float holdHeight = 0.72f;
        [SerializeField] private float shotsPerSecond = 5f;

        private float nextShotTime;

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            UpdatePose();
            TryFire();
        }

        private void UpdatePose()
        {
            Vector3 aimDirection = player.AimDirection;
            transform.position = player.transform.position + aimDirection * holdDistance + Vector3.up * holdHeight;
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        }

        private void TryFire()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || projectilePrefab == null || muzzle == null)
            {
                return;
            }

            if (!mouse.leftButton.isPressed || Time.time < nextShotTime)
            {
                return;
            }

            nextShotTime = Time.time + 1f / shotsPerSecond;
            Projectile projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
            projectile.Launch(player.AimDirection);
        }
    }
}
