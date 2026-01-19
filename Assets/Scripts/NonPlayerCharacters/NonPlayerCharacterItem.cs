using Unity.Entities;

public struct WeaponTag : IComponentData
{
    public int WeaponIndex;  // Unique ID for this weapon type (1, 2, 3, ...)
}