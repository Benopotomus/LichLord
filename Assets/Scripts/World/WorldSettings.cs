using UnityEngine;
using Fusion;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WorldSettings", menuName = "LichLord/World/WorldSettings", order = 1)]
public class WorldSettings : ScriptableObject
{
    [SerializeField]
    private Vector2 _worldOrigin = new Vector2(-500f, -500f);
    public Vector2 WorldOrigin => _worldOrigin;
    
    [SerializeField]
    private Vector2 _worldSize = new Vector2(1000f, 1000f);
    public Vector2 WorldSize => _worldSize;
}
