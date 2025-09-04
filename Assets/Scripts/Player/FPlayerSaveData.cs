using JetBrains.Annotations;
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

        public int tutorialProgress;

        public FPlayerSaveData(string playerName, 
            Vector3 position, 
            Quaternion rotation,
            EMovementState moveState,
            int tutorialProgress)
        {
            this.playerName = playerName;
            this.position = position;
            this.rotation = rotation;
            this.moveState = moveState;

            this.tutorialProgress = tutorialProgress;
        }

        public bool IsValid()
        {
            if(playerName == null)
                return false;
        
            return true;
        }
    }
}