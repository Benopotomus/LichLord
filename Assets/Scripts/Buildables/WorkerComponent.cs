using Fusion;
using LichLord.Buildables;
using LichLord.Items;
using LichLord.World;
using Pathfinding;
using System;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class WorkerComponent : ContextBehaviour
    {
        [SerializeField] 
        private Stronghold _stronghold;

        [Networked, Capacity(BuildableConstants.MAX_WORKERS_PER_STRONGHOLD)]
        protected NetworkArray<FWorkerData> _workerDatas { get; }

        [Networked]
        public int MaxWorkerCount { get; private set; } = 6;

        [SerializeField]
        private int _activeWorkerCount;
        public int ActiveWorkerCount => _activeWorkerCount;

        protected NonPlayerCharacter[] _workerCharacters = new NonPlayerCharacter[BuildableConstants.MAX_WORKERS_PER_STRONGHOLD];
        public NonPlayerCharacter[] WorkerCharacters => _workerCharacters;

        public Action<NonPlayerCharacter[]> OnWorkersChanged;

        public override void Spawned()
        {
            base.Spawned();
            Context.ContainerManager.OnItemSlotChanged += OnItemSlotChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            Context.ContainerManager.OnItemSlotChanged -= OnItemSlotChanged;
        }

        private void OnItemSlotChanged(int fullIndex, FItemSlotData data)
        {
            FContainerSlotData containerSlotData = _stronghold.ContainerSlotData;
            if (fullIndex < containerSlotData.StartIndex ||
                fullIndex > containerSlotData.EndIndex)
                return;

            int localIndex = fullIndex - containerSlotData.StartIndex; 

            if (localIndex < 0 || localIndex > BuildableConstants.MAX_WORKERS_PER_STRONGHOLD)
                return;

            if (data.ItemData.IsValid())
            {
                SummonableDefinition summableDefinition = Global.Tables.ItemTable.TryGetDefinition(data.ItemData.DefinitionID) as SummonableDefinition;
                if (summableDefinition == null)
                    return;

                ref FWorkerData workerData = ref _workerDatas.GetRef(localIndex);
                workerData.IsAssigned = true;

                TrySpawnWorkerFromCenter(_stronghold.StrongholdID, localIndex, summableDefinition.NonPlayerCharacterDefinition);
            }
            else
            {
                ClearWorkerData(localIndex);
            }
        }

        private void DestroyItemForWorker(int workerIndex)
        {
            FContainerSlotData containerSlotData = _stronghold.ContainerSlotData;
            int fullIndex = containerSlotData.StartIndex + workerIndex;
            Context.ContainerManager.SetItemSlotData(fullIndex, new FItemData());
        }

        public void OnWorkerStateChanged(int workerIndex, ENPCState newState)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            if (!workerData.IsAssigned)
                return;

            switch (newState)
            {
                case ENPCState.Inactive:
                case ENPCState.Dead:

                    DestroyItemForWorker(workerIndex);
                    break;
            }
        }

        public ref FWorkerData GetWorkerData(int i)
        {
            return ref _workerDatas.GetRef(i);
        }

        public void LoadWorkerData(FWorkerSaveData[] workerSaveDatas)
        {
            foreach (FWorkerSaveData workerSaveData in workerSaveDatas)
            {
                FWorkerData workerData = workerSaveData.ToNetworkWorker();
                _workerDatas.Set(workerSaveData.workerIndex, workerData);
            }
        }

        public void AddWorkerCharacter(NonPlayerCharacter character, int workerIndex)
        {
            //Debug.Log("Add Worker " + character.LocalIndex + ", " + workerIndex);
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            if (HasStateAuthority)
            {
                if (_workerCharacters[workerIndex] != null)
                {
                    character.RuntimeState.SetState(ENPCState.Inactive);
                    return;
                }
            }

            _workerCharacters[workerIndex] = character;
            OnWorkersChanged?.Invoke(_workerCharacters);
            _activeWorkerCount++;
        }

        public void RemoveWorkerCharacter(NonPlayerCharacter character, int workerIndex)
        {
            //Debug.Log("Remove Worker " + character.LocalIndex + ", " + workerIndex);
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            _workerCharacters[workerIndex] = null;
            OnWorkersChanged?.Invoke(_workerCharacters);
            _activeWorkerCount--;
        }

        public void ClearWorkerData(int workerIndex)
        {
            if (_workerCharacters[workerIndex] != null)
            {
                _workerCharacters[workerIndex].RuntimeState.SetState(ENPCState.Dead);
                _workerCharacters[workerIndex].RuntimeState.InvalidateWorker();
                _workerCharacters[workerIndex].UpdateWorkerData();
                _workerCharacters[workerIndex] = null;
            }

            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.IsAssigned = false;
            workerData.WorkerActive = false;
        }

        public void TrySpawnWorkerFromCenter(int strongholdId, int workerIndex, NonPlayerCharacterDefinition definition)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.25f, 0.25f),
                0f, // Keep Y fixed
                UnityEngine.Random.Range(-0.25f, 0.25f)
            );

            Vector3 spawnPosition = GetNavMeshPosition(_stronghold.Position, _stronghold.Position + randomOffset);

            TrySpawnWorker(spawnPosition, strongholdId, workerIndex, definition);
        }

        public int GetEmptyWorkerSlot()
        {
            FContainerSlotData containerSlotData = _stronghold.ContainerSlotData;
            int emptyIndex = Context.ContainerManager.GetEmptyItemIndex(_stronghold.ContainerIndex);

            if (emptyIndex == -1)
                return -1;

            int localIndex = emptyIndex - containerSlotData.StartIndex;

            if (localIndex < 0 || localIndex > MaxWorkerCount)
                return -1;

            return localIndex;
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SummonWorker(byte playerIndex, FWorldPosition compressedSpawnPosition, FItemData itemData)
        {
            Vector3 spawnPosition = compressedSpawnPosition.Position;

            //spawnPosition = GetNavMeshPosition(spawnPosition, spawnPosition);

            int emptyWorkerSlot = GetEmptyWorkerSlot();

            if (emptyWorkerSlot == -1)
                return;

            if (!itemData.IsValid())
                return;

            SummonableDefinition summableDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID) as SummonableDefinition;
            if (summableDefinition == null)
                return;

            ref FWorkerData workerData = ref _workerDatas.GetRef(emptyWorkerSlot);
            workerData.IsAssigned = true;
            
            //workerData.CommandTargetPosition = compressedSpawnPosition;
            

            TrySpawnWorker(spawnPosition, _stronghold.StrongholdID, emptyWorkerSlot, summableDefinition.NonPlayerCharacterDefinition);
            Context.ContainerManager.AddItemToContainer(_stronghold.ContainerIndex, itemData); 
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_PickupWorker(byte playerIndex, ushort npcFullIndex)
        {
            var pc = Context.NetworkGame.GetPlayerByIndex(playerIndex);
            if (pc == null)
                return;

            // Get worker for full index
            for (int i = 0; i < MaxWorkerCount; i++)
            {
                ref FWorkerData workerData = ref _workerDatas.GetRef(i);
                if (workerData.NPCIndex != npcFullIndex)
                    continue;

                var fullItemIndex = GetFullItemIndexForWorker(i);

                var itemAtIndex = Context.ContainerManager.GetItemSlotData(fullItemIndex);

                pc.Inventory.AddItemToInventory(itemAtIndex.ItemData);

                Context.ContainerManager.SetItemSlotData(fullItemIndex, new FItemData());
            }
        }

        public void TrySpawnWorker(Vector3 spawnPosition, int strongholdId, int workerIndex, NonPlayerCharacterDefinition definition)
        {
            if (!HasStateAuthority)
                return;

            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.TasksData.RawData = 255;

            // Check to see if NPC data already exists for me
            var npcRuntimeState = Context.NonPlayerCharacterManager.GetNpcRuntimeStateAtIndex(workerData.NPCIndex);

            if (npcRuntimeState != null)
            {
                if (npcRuntimeState.GetWorkerIndex() == workerIndex)
                    return;
            }

            if (workerData.WorkerActive)
                return;

            workerData.WorkerActive = true;

            workerData.NPCIndex = (short)Context.NonPlayerCharacterManager.SpawnNPCWorker(spawnPosition,
                definition,
                ETeamID.PlayerTeam,
                strongholdId,
                workerIndex);
        }

        private Vector3 GetNavMeshPosition(Vector3 centerPosition, Vector3 samplePosition)
        {
            var graph = AstarPath.active;
            if (graph == null)
            {
                return samplePosition;
            }

            NNInfo info = graph.GetNearest(samplePosition, NNConstraint.Walkable);

            if (info.node is TriangleMeshNode triangleMeshNode)
            {
                // Get the triangle's vertices
                Int3[] vertices = new Int3[3];
                triangleMeshNode.GetVertices(out vertices[0], out vertices[1], out vertices[2]);

                // Choose the closest vertex to the candidate position
                int closestVertexIndex = 0;
                float minDistance = Vector3.Distance(centerPosition, (Vector3)vertices[0]);
                for (int i = 1; i < 3; i++)
                {
                    float distance = Vector3.Distance(centerPosition, (Vector3)vertices[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestVertexIndex = i;
                    }
                }

                return (Vector3)vertices[closestVertexIndex];

            }

            return samplePosition;
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_ToggleTask(byte workerIndex, byte taskIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.TasksData.ToggleTask(taskIndex);
        }

        public int GetFullItemIndexForWorker(int workerIndex)
        {
            if (workerIndex < 0 || workerIndex > MaxWorkerCount)
                return -1;

            FContainerSlotData containerSlotData = _stronghold.ContainerSlotData;
            int fullIndex = containerSlotData.StartIndex + workerIndex;

            // Validate the full index is within container bounds
            if (fullIndex < containerSlotData.StartIndex || fullIndex > containerSlotData.EndIndex)
                return -1;

            return fullIndex;
        }
    }
}
