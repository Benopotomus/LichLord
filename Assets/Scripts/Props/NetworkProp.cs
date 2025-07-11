using Fusion;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Props
{
    public class NetworkProp : ContextBehaviour, IHitTarget, IChunkTrackable
    {
        [Networked]
        private ref FPropData _data => ref MakeRef<FPropData>();

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField] protected PropRuntimeState _propRuntimeState;
        public PropRuntimeState RuntimeState => _propRuntimeState;

        [SerializeField] protected PropDefinition _propDefinition;

        // IChunkTrackable
        private Chunk _currentChunk;
        public Chunk CurrentChunk { get => _currentChunk; set => _currentChunk = value; }

        public Vector3 Position => CachedTransform.position;

        public bool IsAttackable => true;

        // Debug values
        public int Debug_TreeIndex;
        public int Debug_OriginalPrototypeIndex;
        public string Debug_TerrainName;
        public Vector3 Debug_TerrainOrigin;
        public Vector3 Debug_TerrainSize;
        public Vector3 Debug_TreeNormalizedPos;
        public Vector3 Debug_TreeWorldPos;
        public TreeInstance Debug_TreeInstance;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propDefinition = propRuntimeState.Definition;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;

            _currentChunk = propRuntimeState.chunk;

            HideMatchingTree(ref _propRuntimeState, hiddenPrototypeIndex: 2); // Make sure index 2 is a hidden/invisible tree prototype
        }

        public virtual void Deactivate()
        {
            CachedTransform.position = Vector3.zero;
            UnhideTree(ref _propRuntimeState);
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {
            // Implement hit logic here
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {
            // Implement damage logic here
        }

        public void HideMatchingTree(ref PropRuntimeState runtimeState, int hiddenPrototypeIndex, float maxWorldDist = 1.0f)
        {
            Terrain terrain = runtimeState.terrain;
            if (terrain == null) return;

            TerrainData terrainData = terrain.terrainData;
            TreeInstance[] trees = terrainData.treeInstances;
            Vector3 terrainOrigin = terrain.transform.position;
            Vector3 terrainSize = terrainData.size;

            Vector3 worldPos = runtimeState.position;

            float minDist = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < trees.Length; i++)
            {
                Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrainSize) + terrainOrigin;
                float dist = Vector3.Distance(treeWorldPos, worldPos);

                if (dist < maxWorldDist && dist < minDist)
                {
                    minDist = dist;
                    closestIndex = i;
                }
            }

            if (closestIndex >= 0)
            {
                TreeInstance tree = trees[closestIndex];
                runtimeState.treeIndex = closestIndex;
                runtimeState.originalPrototypeIndex = tree.prototypeIndex;

                // Assign debug data
                Debug_TreeIndex = closestIndex;
                Debug_OriginalPrototypeIndex = tree.prototypeIndex;
                Debug_TerrainName = terrain.name;
                Debug_TerrainOrigin = terrainOrigin;
                Debug_TerrainSize = terrainSize;
                Debug_TreeInstance = tree;
                Debug_TreeNormalizedPos = tree.position;
                Debug_TreeWorldPos = Vector3.Scale(tree.position, terrainSize) + terrainOrigin;

                // Hide the tree
                tree.prototypeIndex = hiddenPrototypeIndex;
                trees[closestIndex] = tree;
                terrainData.treeInstances = trees;

                Debug.Log($"✅ Hid tree #{closestIndex} at {Debug_TreeWorldPos} on terrain {Debug_TerrainName}");
            }
            else
            {
                Debug.LogWarning("❌ No matching tree found within world distance threshold.");
            }
        }

        public void UnhideTree(ref PropRuntimeState runtimeState)
        {
            Terrain terrain = runtimeState.terrain;
            if (terrain == null || runtimeState.treeIndex < 0)
                return;

            TreeInstance[] trees = terrain.terrainData.treeInstances;

            if (runtimeState.treeIndex < trees.Length)
            {
                TreeInstance tree = trees[runtimeState.treeIndex];
                tree.prototypeIndex = runtimeState.originalPrototypeIndex;
                trees[runtimeState.treeIndex] = tree;
                terrain.terrainData.treeInstances = trees;

                Debug.Log($"🔄 Restored tree #{runtimeState.treeIndex} to prototype {runtimeState.originalPrototypeIndex}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Debug_TreeWorldPos, 0.5f);
        }
    }
}
