using LichLord.Projectiles;
using LichLord.Buildables;
using LichLord.Props;
using LichLord.NonPlayerCharacters;
using System;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    [CreateAssetMenu(fileName = "GlobalTables", menuName = "LichLord/Tables/GlobalTables")]
    public class GlobalTables : ScriptableObject
    {
        public ProjectileTable ProjectileTable;
        public PropTable PropTable;
        public BuildableTable BuildableTable;
        public NonPlayerCharacterTable NonPlayerCharacterTable;
        public ManeuverTable ManeuverTable;

        /*
        public ItemTable ItemTable;
        public MarkupPropTable MarkupPropTable;
        public HeroTable HeroTable;
        public MonsterTable MonsterTable;
        public TileScriptTable TileScriptTable;
        public GameplayEffectTable GameplayEffectTable;
        public ExecutionTable ExecutionTable;

        public ImpactTable ImpactTable;
        public EnchantmentTable EnchantmentTable;
        public StatNameTable StatNameTable;
        public ItemTypeTable ItemTypeTable;
        public LevelConfigTable LevelConfigTable;
        public LevelSequenceTable LevelSequenceTable;
        */
    }
}
