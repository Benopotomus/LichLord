namespace LichLord.UI
{
    using LichLord.Dialog;
    using System;
    using UnityEngine;

    public class UIDialogView : UIGameplayView
    {
        private DialogNode _currentDialogNode;

        [SerializeField] private UIDialogWidget _dialogWidget;

        public void SetDialogNode(DialogNode currentDialogNode)
        {
            if (currentDialogNode == _currentDialogNode)
                return;

            _currentDialogNode = currentDialogNode;

            _dialogWidget.SetDialogNode(currentDialogNode);
        }

        protected override void OnVisible()
        {
            base.OnVisible();

        }

        protected override void OnTick()
        {
            base.OnTick();

        }
    }
}