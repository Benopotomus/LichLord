using System;
using UnityEngine;

namespace LichLord.Dialog
{
    [Serializable]
    public class DialogResponseAction
    {
        public ESceneContextCategory target;
        public string functionName;

        // optional parameter
        public string parameter;

        public void Invoke(SceneContext context)
        {
            if (string.IsNullOrEmpty(functionName))
                return;

            object arg = string.IsNullOrEmpty(parameter) ? null : parameter;

            // Try to parse parameter into int
            if (!string.IsNullOrEmpty(parameter))
            {
                if (int.TryParse(parameter, out int intValue))
                    arg = intValue;
                else
                    arg = parameter; // fallback to string
            }

            switch (target)
            {
                case ESceneContextCategory.None:
                    break;

                case ESceneContextCategory.MissionManager:
                    context.MissionManager?.SendMessage(functionName, arg, SendMessageOptions.DontRequireReceiver);
                    break;

                case ESceneContextCategory.DialogManager:
                    context.DialogManager?.SendMessage(functionName, arg, SendMessageOptions.DontRequireReceiver);
                    break;

                case ESceneContextCategory.NonPlayerCharacterManager:
                    context.NonPlayerCharacterManager?.SendMessage(functionName, arg, SendMessageOptions.DontRequireReceiver);
                    break;

                case ESceneContextCategory.InvasionManager:
                    context.InvasionManager?.SendMessage(functionName, arg, SendMessageOptions.DontRequireReceiver);
                    break;

                default:
                    Debug.LogWarning($"FDialogResponseAction: Unknown target {target}");
                    break;
            }
        }
    }
}
