using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "PropPointMarkupData", menuName = "LichLord/Props/PropPointMarkupData")]
    public class LevelPropsMarkupData : ScriptableObject
    {
        public PropMarkupData[] propMarkupDatas;
    }
}