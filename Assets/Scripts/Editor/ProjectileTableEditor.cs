using UnityEditor;
using LichLord.Projectiles;

namespace LichLord.Editor
{
    [CustomEditor(typeof(LichLord.Projectiles.ProjectileTable))]
    public class ProjectileTableEditor : ObjectTableEditor<
        ProjectileDefinition,
        ProjectileTable>
    {

    }
}