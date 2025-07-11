using LichLord.World;
using System;
using UnityEngine;

namespace LichLord.Props
{
    public class PropRuntimeState
    {
        public Chunk chunk; // Owning chunk
        public int guid; // Unique identifier
        public int definitionId; // PropDefinition.TableID

        // Not replicated
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation

        // Terrain
        // Only for terrains
        public string terrainId;            // Unique ID of the terrain
        public Vector3 terrainTreePosition; // Tree's world-space position

        public Terrain terrain;

        public int treeIndex = -1;
        public int originalPrototypeIndex = -1;

        private PropDefinition _definition;
        public PropDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.PropTable.TryGetDefinition(definitionId);

                return _definition;
            }
        }

        private FPropData _data = new FPropData();
        public FPropData Data => _data;

        public PropRuntimeState(int guid, 
            Chunk chunk,
            Vector3 position, 
            Quaternion rotation, 
            int definitionId,
            string terrainId)
        {
            this.guid = guid;
            this.chunk = chunk;
            this.definitionId = definitionId;
            this.position = position;
            this.rotation = rotation;
            this.terrainId = terrainId;
            this.terrain = GetTerrainById(terrainId);

            PropDefinition definition = Global.Tables.PropTable.TryGetDefinition(definitionId);
            PropDataDefinition dataDefinition = definition.PropDataDefinition;

            _data.GUID = guid;
            _data.DefinitionID = definitionId;
            dataDefinition.InitializeData(ref _data, definition);
        }

        public PropRuntimeState(PropRuntimeState other)
        {
            this.guid = other.guid;
            this.definitionId = other.definitionId;
            this.position = other.position;
            this.rotation = other.rotation;
            this.terrainId = other.terrainId;
            this.terrain = other.terrain;

            FPropData otherData = other.Data;
            _data.Copy(ref otherData);
            _data.GUID = guid;
            _data.DefinitionID = definitionId;
        }

        public EPropState GetState()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            return dataDefinition.GetState(ref _data);
        }

        public int GetHealth()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            return dataDefinition.GetHealth(ref _data);
        }

        public void ApplyDamage(int damage, int tick)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;

            // Create a propdata and set its state data to get current health
            dataDefinition.ApplyDamage(ref _data, damage);

            _hitReactEndTick = _hitReactTicks + tick;
        }

        public void CopyData(ref FPropData propData)
        { 
            _data.Copy(ref propData);
        }

        // Runtime Values

        EPropState _currentState;
        int _hitReactTicks = 8;
        int _hitReactEndTick;

        // Updates on the server if the RuntimePropState is loaded
        // Does not require the monobehaviour to exist
        public bool AuthorityUpdate(int tick)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            _currentState = dataDefinition.GetState(ref _data);

            switch (_currentState)
            {
                case EPropState.HitReact:
                    if (tick > _hitReactEndTick)
                    {
                        _currentState = EPropState.Idle;
                        dataDefinition.SetState( _currentState, ref _data);
                        return true;
                    }
                    break;
                case EPropState.Destroyed:
                    break;
            }

            return false;
        }

        public Terrain GetTerrainById(string id)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++)
            {
                TerrainID tid = terrains[i].GetComponent<TerrainID>();
                if (tid != null && tid.ID == id)
                    return terrains[i];
            }
            return null;
        }
    }
}