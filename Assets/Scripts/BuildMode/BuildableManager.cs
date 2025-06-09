using Fusion;
using LichLord.Props;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableManager : ContextBehaviour
    {
        [SerializeField] private BuildableSpawner _buildableSpawner;
        [SerializeField] private BuildableReplicator buildReplicatorPrefab;
        [SerializeField] private BuildableSaveLoadManager saveLoadManager;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float despawnRadius = 60f;

        [SerializeField] private List<BuildableRuntimeState> _runtimeBuildStates = new List<BuildableRuntimeState>();
        [SerializeField] private Dictionary<int, BuildableRuntimeState> _deltaStates = new Dictionary<int, BuildableRuntimeState>();
        [SerializeField] private List<BuildableReplicator> _buildableReplicators = new List<BuildableReplicator>();

        private List<BuildableLoadState> _loadStates = new List<BuildableLoadState>();

        public override void Spawned()
        {
            _buildableSpawner.OnBuildableSpawned += OnBuildableSpawned;

            //LoadBaseLevelProps();

            if (HasStateAuthority)
            {
                ApplySavedDelta();
            }
        }

        public void AddReplicator(BuildableReplicator replicator)
        {
            _buildableReplicators.Add(replicator);
        }

        public void PlaceBuilding(Vector3 position, int definitionId)
        {
            Debug.Log("PlaceBuilding: " + definitionId);
        }

        private void ApplySavedDelta()
        {
            saveLoadManager.LoadSavedPropStates(_runtimeBuildStates, _loadStates, _deltaStates);

            foreach (BuildableRuntimeState deltaState in _deltaStates.Values)
            {
                int guid = deltaState.guid;
                BuildableRuntimeState changedState = _runtimeBuildStates[guid];
                changedState.position = deltaState.position;
                changedState.rotation = deltaState.rotation;
                changedState.definitionId = deltaState.definitionId;
                changedState.stateData = deltaState.stateData;
            }

            int propReplicatorCount = (_deltaStates.Count + BuildableConstants.MAX_BUILD_REPS - 1) / BuildableConstants.MAX_BUILD_REPS;

            for (int i = 0; i < propReplicatorCount; i++)
            {
                var buildableReplicator = Runner.Spawn(buildReplicatorPrefab, Vector3.zero, Quaternion.identity);
                _buildableReplicators.Add(buildableReplicator);

                int startIndex = i * PropConstants.MAX_PROP_REPS;
                int endIndex = Mathf.Min(startIndex + BuildableConstants.MAX_BUILD_REPS, _deltaStates.Count);

                int index = 0;
                foreach (BuildableRuntimeState deltaState in _deltaStates.Values)
                {
                    if (index >= startIndex && index < endIndex)
                    {
                        buildableReplicator.AddBuildable(deltaState, true);
                    }
                    index++;
                }
            }

            Runner.Spawn(buildReplicatorPrefab, Vector3.zero, Quaternion.identity);
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter playerCreature);

            if (playerCreature == null)
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Runner.LocalAlpha;

            // Ensure an empty replicator exists on the master client
            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                EnsureEmptyReplicator();
            }

            for (int i = 0; i < _runtimeBuildStates.Count; i++)
            {
                BuildableRuntimeState propState = _runtimeBuildStates[i];
                BuildableLoadState propLoadState = _loadStates[i];

                float distance = Vector3.Distance(viewPosition, propState.position);
                bool shouldBeActive = distance <= spawnRadius;

                if (shouldBeActive && propLoadState.LoadState == ELoadState.None)
                {
                    propLoadState.LoadState = ELoadState.Loading;
                    _buildableSpawner.SpawnProp(propState);
                }
                else if (shouldBeActive && propLoadState.LoadState == ELoadState.Loaded)
                {
                    RefreshRuntimeState(propState);
                    propLoadState.Buildable.UpdateBuildable(propState, renderDeltaTime);
                }
                else if (!shouldBeActive && distance > despawnRadius && propLoadState.LoadState == ELoadState.Loaded)
                {
                    DespawnProp(propState.guid);
                }
            }
        }

        private void EnsureEmptyReplicator()
        {
            // Check if there is at least one completely empty replicator (zero entries)
            bool hasEmptyReplicator = false;
            foreach (var replicator in _buildableReplicators)
            {
                if (replicator.DataCount == 0)
                {
                    hasEmptyReplicator = true;
                    break;
                }
            }

            // If no empty replicator exists, spawn a new one
            if (!hasEmptyReplicator)
            {
                var newReplicator = Runner.Spawn(buildReplicatorPrefab, Vector3.zero, Quaternion.identity);
            }
        }

        private void RefreshRuntimeState(BuildableRuntimeState propState)
        {
            bool replicatorFound = false;

            for (int i = 0; i < _buildableReplicators.Count; i++)
            {
                if (_buildableReplicators[i].TryGetBuildableData(propState.guid, out FBuildableData propData))
                {
                    replicatorFound = true;
                    propState.stateData = propData.StateData;
                }
            }

            // if no replicator, reset default state
            if (!replicatorFound)
            {
                propState.stateData = 0;
            }
        }

        private void OnBuildableSpawned(BuildableRuntimeState propRuntimeState, Buildable buildable)
        {
            int guid = propRuntimeState.guid;
            BuildableLoadState buildableLoadState = _loadStates[guid];
            buildableLoadState.Buildable = buildable;
            buildableLoadState.LoadState = ELoadState.Loaded;
            buildable.OnSpawned(propRuntimeState, this);
        }

        private void DespawnProp(int guid)
        {
            BuildableLoadState buildableLoadState = _loadStates[guid];
            if (buildableLoadState.LoadState == ELoadState.Loaded)
            {
                buildableLoadState.Buildable.StartRecycle();
                buildableLoadState.LoadState = ELoadState.None;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            if (runner.IsSharedModeMasterClient)
            {
                saveLoadManager.SaveRuntimeState(_deltaStates);
            }
        }

        public class BuildableLoadState
        {
            public Buildable Buildable;
            public ELoadState LoadState;
        }

        public enum ELoadState
        {
            None,
            Loading,
            Loaded,
            Unloading,
        }
    }
}