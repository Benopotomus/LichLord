using Fusion;
using UnityEngine;

namespace LichLord
{
    public class DestructibleBox : NetworkBehaviour
    {
        [Networked] public int Health { get; set; } = 1000; // Networked health
        [SerializeField] private GameObject visualModel; // Visual representation (e.g., box mesh)
        private int _localHealth; // Local state for visual feedback

        public override void Spawned()
        {
            Health = 100; // Initialize health
            _localHealth = Health;
            UpdateVisualState();
        }

        public void ApplyDamage(int damage)
        {
            if (HasStateAuthority)
            {
                int oldHealth = Health;
                Health = Mathf.Max(0, Health - damage);
                Debug.Log($"[DestructibleBox] Health updated from {oldHealth} to {Health} for {Object.Id}");
                if (Health <= 0)
                {
                    Runner.Despawn(Object); // Destroy box when health reaches 0
                }
            }
        }

        public void UpdateVisualStateWithEvent(int damageAmount)
        {
            // Update local health for visuals only, without modifying networked Health
            if (!HasStateAuthority)
            {
                _localHealth = Mathf.Max(0, _localHealth - damageAmount);
                UpdateVisualState();
                Debug.Log($"[DestructibleBox] Local health updated to {_localHealth} for visuals on {Object.Id}");
            }
        }

        private void UpdateVisualState()
        {
            // Update visuals without lerp (e.g., scale based on health)
            float healthPercent = _localHealth / 100f;
            visualModel.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, healthPercent);
        }

        public override void Render()
        {
            // Sync local state with networked state, update visuals only if changed
            if (_localHealth != Health)
            {
                _localHealth = Health;
                UpdateVisualState();
                Debug.Log($"[DestructibleBox] Synced local health to {Health} for {Object.Id}");
            }
        }
    }
}