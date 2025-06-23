using System;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public struct FPlayerSaveData
    {
        public string playerName;
        public Vector3 position;
        public Quaternion rotation;
        public EMovementState moveState;

        public FPlayerSaveData(string playerName, 
            Vector3 position, 
            Quaternion rotation,
            EMovementState moveState)
        {
            this.playerName = playerName;
            this.position = position;
            this.rotation = rotation;
            this.moveState = moveState;

        }
    }
}