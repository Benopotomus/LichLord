using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "PropPointMarkupData", menuName = "LichLord/Props/PropPointMarkupData")]
    public class PropPointMarkupData : ScriptableObject
    {
        public PropPointData[] propPoints;
    }
}