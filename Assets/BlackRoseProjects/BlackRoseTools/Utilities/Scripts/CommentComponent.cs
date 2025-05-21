using UnityEngine;

namespace BlackRoseProjects.Utility
{
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/Utilities/Editor/Icons/ScriptComment.png")]
    internal class CommentComponent : MonoBehaviour
    {
        [SerializeField, TextArea] internal string text;
        [SerializeField] private bool selectObjectOnSceneEnable;

#if UNITY_EDITOR
        void Start()
        {
            if (selectObjectOnSceneEnable)
            {
                UnityEditor.Selection.activeObject = gameObject;
            }
        }
#endif
    }
}