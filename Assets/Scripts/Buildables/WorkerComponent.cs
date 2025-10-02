using Fusion;
using LichLord.Buildables;
using LichLord.Items;
using LichLord.World;
using Pathfinding;
using System.Net.NetworkInformation;
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
        public int MaxWorkerCount { get; private set; } = 8;

        [SerializeField]
        private int _activeWorkerCount;
        public int ActiveWorkerCount => _activeWorkerCount;

        protected NonPlayerCharacter[] _workerCharacters = new NonPlayerCharacter[BuildableConstants.MAX_WORKERS_PER_STRONGHOLD];
       
        protected int[] _nextRespawnProgressTick = new int[BuildableConstants.MAX_WORKERS_PER_STRONGHOLD];

        private int _ticksPerRespawnProgress = 32;
        private int _maxRespawnProgress = 10;

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

        public override void Render()
        {
            base.Render();
            int tick = Runner.Tick;

            UpdateRespawns(tick);
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

                TrySpawnWorker(_stronghold.StrongholdID, localIndex, summableDefinition.NonPlayerCharacterDefinition);
            }
            else
            {
                ClearWorkerData(localIndex);
            }
        }

        private FItemSlotData GetItemSlotDataForWorkerIndex(int workerIndex)
        {
            FContainerSlotData containerSlotData = _stronghold.ContainerSlotData;

            int fullIndex =  containerSlotData.StartIndex + workerIndex;

            return Context.ContainerManager.GetItemSlotData(fullIndex);
        }

        public void UpdateRespawns(int tick)
        {
            for (int i = 0; i < BuildableConstants.MAX_WORKERS_PER_STRONGHOLD; i++)
            {
                ref FWorkerData workerData = ref _workerDatas.GetRef(i);

                if (!workerData.IsAssigned)
                    continue;

                if(!workerData.IsRespawning)
                    continue;

                if (tick > _nextRespawnProgressTick[i])
                {
                    workerData.RespawnProgress++;
                    _nextRespawnProgressTick[i] = Runner.Tick + _ticksPerRespawnProgress;
                    Debug.Log(workerData.RespawnProgress);

                    if (workerData.RespawnProgress > _maxRespawnProgress)
                    {
                        var itemSlotData = GetItemSlotDataForWorkerIndex(i);
                        if (itemSlotData.ItemData.IsValid())
                        {
                            SummonableDefinition summableDefinition = Global.Tables.ItemTable.TryGetDefinition(itemSlotData.ItemData.DefinitionID) as SummonableDefinition;
                            if (summableDefinition == null)
                                continue;

                            TrySpawnWorker(_stronghold.StrongholdID, i, summableDefinition.NonPlayerCharacterDefinition);
                        }
                    }
                }
            }
        }

        public void OnWorkerStateChanged(int workerIndex, ENPCState newState)
        {
            //Debug.Log("worker state changed " + newState);

            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            if (!workerData.IsAssigned)
                return;

            switch (newState)
            {
                case ENPCState.Inactive:
                case ENPCState.Dead:

                    if (workerData.IsRespawning)
                        return;

                    workerData.WorkerActive = false;
                    workerData.IsRespawning = true;
                    workerData.RespawnProgress = 0;
                    _nextRespawnProgressTick[workerIndex] = Runner.Tick + _ticksPerRespawnProgress;
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
                _workerDatas.Set(workerSaveData.index, workerData);
            }
        }

        public void AddWorkerCharacter(NonPlayerCharacter character, int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.WorkerActive = true;

            if (HasStateAuthority)
            {
                if (_workerCharacters[workerIndex] != null)
                {
                    character.RuntimeState.SetState(ENPCState.Inactive);
                    return;
                }
            }

            _workerCharacters[workerIndex] = character;

            _activeWorkerCount++;
        }

        public void RemoveWorkerCharacter(NonPlayerCharacter character, int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.WorkerActive = false;

            _workerCharacters[workerIndex] = null;

            _activeWorkerCount--;
        }

        public void ClearWorkerData(int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.IsAssigned = false;
            workerData.WorkerActive = false;
            workerData.IsRespawning = false;
        }

        public void TrySpawnWorker(int strongholdId, int workerIndex, NonPlayerCharacterDefinition definition)
        {
            if (!HasStateAuthority)
                return;

            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            if (workerData.WorkerActive)
                return;

            if (_workerCharacters[workerIndex] != null)
                return;

            workerData.WorkerActive = true;
            workerData.IsRespawning = false;

            Vector3 randomOffset = new Vector3(
                    Random.Range(-0.25f, 0.25f),
                    0f, // Keep Y fixed
                    Random.Range(-0.25f, 0.25f)
                );

            Vector3 spawnPosition = GetNavMeshPosition(_stronghold.Position, _stronghold.Position + randomOffset);

            Context.NonPlayerCharacterManager.SpawnNPCWorker(spawnPosition,
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
    }
}
