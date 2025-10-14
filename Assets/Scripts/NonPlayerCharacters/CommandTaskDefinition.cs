using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "CommandTaskDefinition", menuName = "LichLord/NonPlayerCharacters/CommandTaskDefinition")]
    public class CommandTaskDefinition : ScriptableObject
    {
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;

        [SerializeField]
        protected ECommandTaskType _taskType;
        public ECommandTaskType TaskType => _taskType;

    }

    public enum ECommandTaskType : byte
    {
        None,
        Lumbering,
        Mining,
        Foraging,
        Herbalism,
        Transport
    }
}
