using UnityEngine;
using DG.Tweening;
using DWD.Pooling;
using Fusion;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject, IHitTarget
    {
        protected PropManager _propManager;
        protected PropRuntimeState _propRuntimeState;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;
        }

        public virtual void UpdateRuntimeState()
        { 
        
        }

        public virtual void UpdateProp(PropRuntimeState propState, float renderDeltaTime)
        {
            if (propState.stateData == 1)
            {
                gameObject.SetActive(false);
            }
            else
                gameObject.SetActive(true);

        }

        protected virtual void UpdateAllStates() { }

        public void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {
            if (_propManager == null || _propRuntimeState == null)
            {
                Debug.LogWarning($"[Prop] Cannot process hit: PropManager or PropRuntimeState is null for guid {_propRuntimeState?.guid}.");
                return;
            }

            NetworkRunner runner = _propManager.Runner;

            Debug.Log($"[Prop] Processing hit for guid {_propRuntimeState.guid} with damage 9001");

            foreach (PlayerRef player in runner.ActivePlayers)
            {
                NetworkObject playerObj = runner.GetPlayerObject(player);
                if (playerObj != null)
                {
                    /// How do i just send this to the masterclient player?
                    RelayPlayer relayPlayer = playerObj.GetComponent<RelayPlayer>();

                    relayPlayer.RaiseEvent(new DamageEvent
                    {
                        guid = _propRuntimeState.guid,
                        impulse = Vector3.zero,
                        damage = 9001
                    });
                }
            }
        }
    }
}