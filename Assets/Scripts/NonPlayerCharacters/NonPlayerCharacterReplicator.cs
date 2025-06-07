using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterReplicator : ContextBehaviour, IStateAuthorityChanged
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

        [SerializeField] private NonPlayerCharacterSpawner _spawner;

        private List<NPCLoadState> _loadStates = new List<NPCLoadState>();
        private HashSet<int> _freeIndices = new HashSet<int>();

        public override void Spawned()
        {
            base.Spawned();

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _loadStates.Add(new NPCLoadState());
                _freeIndices.Add(i); // Initially, all indices are free
            }

            Context.NonPlayerCharacterManager.AddReplicator(this);
            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;

            if (HasStateAuthority)
            {
                RebuildFreeIndices();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _spawner.OnSpawned -= OnNonPlayerCharacterSpawned;
        }

        public void StateAuthorityChanged()
        {
            Debug.Log($"StateAuthority Changed, HasStateAuthority: {HasStateAuthority}");
            if (HasStateAuthority)
            {
                RebuildFreeIndices();
            }
        }

        private void RebuildFreeIndices()
        {
            _freeIndices.Clear();
            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var data = _npcDatas.GetRef(i);
                if (!NonPlayerCharacterDataUtility.IsActive(ref data))
                {
                    _freeIndices.Add(i);
                }
            }
        }

        public bool TryGetNPCData(int index, out FNonPlayerCharacterData data)
        {
            data = _npcDatas.GetRef(index);
            return true;
        }

        public void UpdateNPCData(FNonPlayerCharacterData updatedData)
        {
            int index = NonPlayerCharacterDataUtility.GetIndex(ref updatedData);
            var currentData = _npcDatas.GetRef(index);
            bool wasActive = NonPlayerCharacterDataUtility.IsActive(ref currentData);
            bool willBeActive = NonPlayerCharacterDataUtility.IsActive(ref updatedData);

            _npcDatas.Set(index, updatedData);

            if (!willBeActive && wasActive)
            {
                _freeIndices.Add(index); // NPC became inactive
            }
            else if (willBeActive && !wasActive)
            {
                _freeIndices.Remove(index); // NPC became active
            }
        }

        public void UpdateNPCData(FNonPlayerCharacterSpawnParams spawnParams)
        {
            FNonPlayerCharacterData data = new FNonPlayerCharacterData();
            NonPlayerCharacterDefinition definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(spawnParams.definitionId);
            if (definition != null)
            {
                NonPlayerCharacterDataUtility.InitializeData(ref data, definition, spawnParams.index, spawnParams.teamID);
                data.Position = spawnParams.position;
                data.Rotation = spawnParams.rotation;
                _npcDatas.Set(spawnParams.index, data);
            }
        }

        public bool HasFreeIndex()
        {
            return GetFreeIndex() >= 0;
        }

        public int GetFreeIndex()
        {
            if (_freeIndices.Count > 0)
            {
                return _freeIndices.First();
            }
            return -1;
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

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                FNonPlayerCharacterData data = _npcDatas.Get(i);

                bool shouldBeActive = NonPlayerCharacterDataUtility.IsActive(ref data);

                NPCLoadState loadState = _loadStates[i];

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(ref data);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    if (hasAuthority)
                        loadState.NPC.AuthorityUpdate(ref data, renderDeltaTime);
                    else
                        loadState.NPC.RemoteUpdate(ref data, renderDeltaTime, ping);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPC(i);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Context.IsGameplayActive())
                return;

            if (!PlayerCreature.TryGetLocalPlayer(Runner, out PlayerCreature playerCreature))
                return;

            int tick = Runner.Tick;

            float ping = (float)Runner.GetPlayerRtt(playerCreature.Object.StateAuthority);
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                 FNonPlayerCharacterData data = _npcDatas.Get(i);
                NPCLoadState loadState = _loadStates[i];

                if (loadState.LoadState == ELoadState.Loaded)
                {
                    loadState.NPC.OnFixedUpdate(ref data, tick);
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

        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            NPCLoadState loadState = _loadStates[spawnParams.index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;
            character.OnSpawned(ref spawnParams, Context.NonPlayerCharacterManager, this);
        }

        public void ApplyDamage(int index, Vector3 impulse, int damage)
        {
            NPCLoadState loadState = _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.NPC.ApplyDamage(impulse, damage);
            }
        }
    }
}