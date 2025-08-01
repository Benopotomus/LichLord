using System;
using UnityEngine;

namespace LichLord.Props
{
    [Serializable]
    public class StrongholdMarkupData
    {
        public int guid;
        public Vector3 position;
        public Quaternion rotation; // Forward direction (unit vector)
    }
}