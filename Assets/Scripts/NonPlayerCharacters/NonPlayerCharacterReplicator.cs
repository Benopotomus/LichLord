using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterReplicator : ContextBehaviour
    {
        public class NPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        public enum ELoadState
        {
            None,
            Loading,
            Loaded,
            Unloading,
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }
        public int DataCount => _dataCount;

        [SerializeField] private NonPlayerCharacterSpawner _spawner;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float despawnRadius = 60f;

        [SerializeField] private List<NonPlayerCharacterRuntimeState> _runtimeStates = new List<NonPlayerCharacterRuntimeState>();
        private List<NPCLoadState> _loadStates = new List<NPCLoadState>();

        public override void Spawned()
        {
            base.Spawned();

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var runtimeState = new NonPlayerCharacterRuntimeState();
                runtimeState.index = i;

                _runtimeStates.Add(runtimeState);
                _loadStates.Add(new NPCLoadState());
            }

            Context.NonPlayerCharacterManager.AddReplicator(this);
            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;
        }

        public bool TryGetNPCData(int index, out FNonPlayerCharacterData data)
        {
            data = _npcDatas.GetRef(index);
            return true;
        }

        public void UpdateNPCData(int index, FNonPlayerCharacterData updatedData)
        {
            _npcDatas.Set(index, updatedData);
        }

        public void UpdateNPCData(NonPlayerCharacterRuntimeState runtimeState)
        {
            FNonPlayerCharacterData data = new FNonPlayerCharacterData
            {
                DefinitionID = runtimeState.definitionId,
                Transform = new FWorldTransform
                {
                    Position = runtimeState.position,
                    Rotation = runtimeState.rotation
                },
                //Velocity = runtimeState.velocity,
                StateData = runtimeState.stateData,
                Health = runtimeState.health,
            };

            _npcDatas.Set(runtimeState.index, data);
        }

        public void AddNPC(FNonPlayerCharacterData data)
        {
            if (_dataCount >= NonPlayerCharacterConstants.MAX_NPC_REPS)
            {
                Debug.LogWarning("Trying to add a prop data to a replicator when there's no room");
                return;
            }

            _npcDatas.Set(_dataCount, data);

            _dataCount++;
        }

        public bool HasFreeSlot()
        {
            return _dataCount < NonPlayerCharacterConstants.MAX_NPC_REPS;
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            if (!PlayerCreature.TryGetLocalPlayer(Runner, out PlayerCreature playerCreature))
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Time.deltaTime;
            float ping = (float)Runner.GetPlayerRtt(playerCreature.Object.StateAuthority);
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;
            bool shouldBeActive = true;

            for (int i = 0; i < _dataCount; i++)
            {
                ref FNonPlayerCharacterData data = ref _npcDatas.GetRef(i);

                var runtimeState = _runtimeStates[i];
                NPCLoadState loadState = _loadStates[i];

                runtimeState.SetState(ref data);
                runtimeState.replicator = this;

                float distance = Vector3.Distance(viewPosition, data.Transform.Position);

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(runtimeState);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    if (hasAuthority)
                        loadState.NPC.AuthorityUpdate(ref data, renderDeltaTime);
                    else
                        loadState.NPC.RemoteUpdate(ref data, renderDeltaTime, ping);
                }
                else if (!shouldBeActive && distance > despawnRadius && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPC(i);
                }
            }
        }

        private void DespawnNPC(int index)
        {
            NPCLoadState loadState = _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.NPC.StartRecycle();
                loadState.LoadState = ELoadState.None;
            }
        }

        private void OnNonPlayerCharacterSpawned(NonPlayerCharacterRuntimeState state, NonPlayerCharacter character)
        {
            NPCLoadState loadState = _loadStates[state.index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;
            character.OnSpawned(state, Context.NonPlayerCharacterManager);
        }
    }
}