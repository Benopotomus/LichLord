using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterRuntimeState
    {
        public int PredictionTimeoutTick; // Max lifetime of predictive state

        private int _index;
        public int Index => _index;

        private NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        FNonPlayerCharacterData _data = new FNonPlayerCharacterData();
        public FNonPlayerCharacterData Data => _data;

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(_data.DefinitionID);

                return _definition;
            }
        }

        private NonPlayerCharacterDataDefinition _dataDefinition;
        public NonPlayerCharacterDataDefinition DataDefinition
        {
            get
            {
                if (_dataDefinition == null)
                    _dataDefinition = Definition.DataDefinition;

                return _dataDefinition;
            }
        }

        public NonPlayerCharacterRuntimeState(NonPlayerCharacterReplicator replicator, int index, ref FNonPlayerCharacterData data)
        {
            _replicator = replicator;
            _index = index;
            CopyData(ref data);
        }

        public void CopyData(ref FNonPlayerCharacterData other)
        { 
            _data.Copy(ref other);
            _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(_data.DefinitionID);
            
            if(_definition != null ) 
                _dataDefinition = Definition.DataDefinition;
        }

        public void ApplyDamage(int damage, int hitReactIndex)
        {
            NonPlayerCharacterDataUtility.ApplyDamage(ref _data, _definition, damage, hitReactIndex);
            _replicator.ReplicateRuntimeState(this);
        }

        public bool IsActive()
        {
            return NonPlayerCharacterDataUtility.GetNPCState(ref _data) != ENonPlayerState.Inactive;
        }

        public ETeamID GetTeam()
        { 
            return NonPlayerCharacterDataUtility.GetTeamID(ref _data);
        }

        public ENonPlayerState GetState()
        {
            return NonPlayerCharacterDataUtility.GetNPCState(ref _data);
        }

        public int GetAnimationIndex()
        {
            return NonPlayerCharacterDataUtility.GetAnimationIndex(ref _data);
        }

        public void SetAnimationIndex(int index)
        {
            NonPlayerCharacterDataUtility.SetAnimationIndex(index, ref _data);
            _replicator.ReplicateRuntimeState(this);
        }

        public Vector3 GetPosition()
        {
            return _data.Position;
        }

        public Quaternion GetRotation()
        {
            return _data.Rotation;
        }

        public float GetYaw()
        {
            return _data.Yaw;
        }

        public byte GetRawCompressedYaw()
        {
            return _data.RawCompressedYaw;
        }

        public int GetTargetPlayerIndex()
        { 
            return _data.TargetPlayerIndex; 
        }

        public void SetState(ENonPlayerState newState)
        {
            NonPlayerCharacterDataUtility.SetNPCState(newState, ref _data);
            _replicator.ReplicateRuntimeState(this);
        }

        public bool IsInvasionNPC()
        { 
            return NonPlayerCharacterDataUtility.IsInvasionNPC(ref _data);
        }
    }
}
