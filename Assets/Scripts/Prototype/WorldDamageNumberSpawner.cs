using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TargetDummy))]
    public sealed class WorldDamageNumberSpawner : MonoBehaviour
    {
        [SerializeField, InspectorName("伤害来源")] private TargetDummy target;
        [SerializeField, InspectorName("伤害数字模板")] private WorldDamageNumber template;
        [SerializeField, InspectorName("飘字容器")] private Transform container;
        [SerializeField, InspectorName("生成位置偏移")] private Vector3 worldOffset = new Vector3(0.65f, 0.8f, 0.55f);
        [SerializeField, Min(0f), InspectorName("横向随机范围")] private float horizontalJitter = 0.18f;
        [SerializeField, Min(0f), InspectorName("纵向随机范围")] private float verticalJitter = 0.12f;

        private bool subscribed;

        public int SpawnedCount { get; private set; }
        public WorldDamageNumber LastSpawned { get; private set; }

        private void OnEnable()
        {
            if (target == null)
            {
                target = GetComponent<TargetDummy>();
            }

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(TargetDummy damageSource, WorldDamageNumber damageNumberTemplate, Transform spawnContainer)
        {
            Unsubscribe();
            target = damageSource;
            template = damageNumberTemplate;
            container = spawnContainer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (subscribed || target == null)
            {
                return;
            }

            target.DamageReceived += HandleDamageReceived;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || target == null)
            {
                return;
            }

            target.DamageReceived -= HandleDamageReceived;
            subscribed = false;
        }

        private void HandleDamageReceived(DamageInfo damage)
        {
            if (template == null)
            {
                return;
            }

            Transform spawnParent = container != null ? container : transform;
            WorldDamageNumber view = Instantiate(template, spawnParent, false);
            Camera mainCamera = Camera.main;
            Vector3 right = mainCamera != null ? mainCamera.transform.right : Vector3.right;
            Vector3 up = mainCamera != null ? mainCamera.transform.up : Vector3.forward;
            Vector3 jitter =
                right * Random.Range(-horizontalJitter, horizontalJitter) +
                up * Random.Range(-verticalJitter, verticalJitter);

            view.transform.position = damage.HitPosition + worldOffset + jitter;
            view.SetDamage(damage.Amount);
            view.gameObject.SetActive(true);

            LastSpawned = view;
            SpawnedCount++;
        }
    }
}
