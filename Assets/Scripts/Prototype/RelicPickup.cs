using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RelicPickup : MonoBehaviour
    {
        [SerializeField, InspectorName("遗物")] private RelicDefinition relic;
        [SerializeField, InspectorName("拾取后销毁")] private bool destroyAfterPickup = true;
        [SerializeField, InspectorName("允许重复拾取")] private bool allowRespawn = false;

        private bool collected;
        private void OnTriggerEnter(Collider other)
        {
            if (collected || relic == null)
            {
                return;
            }

            TopDownCharacterController character = other.GetComponentInParent<TopDownCharacterController>();
            if (character == null)
            {
                return;
            }

            DiceRevolverGun gun = character.GetComponentInChildren<DiceRevolverGun>(true);
            if (gun == null)
            {
                return;
            }

            bool picked = gun.AddRelic(relic);
            if (picked || !allowRespawn)
            {
                collected = true;

                if (destroyAfterPickup)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
