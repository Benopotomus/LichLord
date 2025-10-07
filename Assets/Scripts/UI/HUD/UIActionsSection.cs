
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIActionsSection : UIWidget
    {
        [SerializeField] private HorizontalLayoutGroup _maneuverIconsSection;
        [SerializeField] private HorizontalLayoutGroup _summonIconsSection;
        [SerializeField] private HorizontalLayoutGroup _buildIconsSection;
        [SerializeField] private HorizontalLayoutGroup _buildCategorySection;

        [SerializeField] private Image _manueverIcon;
        [SerializeField] private Image _buildIcon;


        private EActiveLayout _layout;

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            var state = pc.FSM.StateMachine.ActiveState;

            if (state is IdleState)
            {
                SetLayout(EActiveLayout.Maneuvers);
            }
            else if (state is BuildModeState)
            {
                SetLayout(EActiveLayout.Buildables);
            }
            else if (state is SummonModeState)
            {
                SetLayout(EActiveLayout.Summoning);
            }
            else
            {
                SetLayout(EActiveLayout.None);
            }
        }

        private void SetLayout(EActiveLayout newLayout)
        {
            if (newLayout == _layout) 
                return;

            switch (newLayout)
            { 
                case EActiveLayout.None:
                    _maneuverIconsSection.SetActive(false);
                    _manueverIcon.SetActive(false);
                    _buildIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(false);
                    _buildCategorySection.SetActive(false);
                    _buildIcon.SetActive(false);
                    break;
                case EActiveLayout.Maneuvers:
                    _maneuverIconsSection.SetActive(true);
                    _manueverIcon.SetActive(false);
                    _buildIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(false);
                    _buildCategorySection.SetActive(false);
                    _buildIcon.SetActive(true);
                    break;
                case EActiveLayout.Buildables:
                    _buildIconsSection.SetActive(true);
                    _buildIcon.SetActive(false);
                    _maneuverIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(false);
                    _buildCategorySection.SetActive(true);
                    _manueverIcon.SetActive(true);
                    break;
                case EActiveLayout.Summoning:
                    _summonIconsSection.SetActive(true);
                    _buildIconsSection.SetActive(false);
                    _buildIcon.SetActive(false);
                    _maneuverIconsSection.SetActive(false);
                    _buildCategorySection.SetActive(false);
                    _manueverIcon.SetActive(false);
                    break;
            }

            _layout = newLayout;
        }

        public enum EActiveLayout
        { 
            None,
            Maneuvers,
            Buildables,
            Summoning,
        }
    }
}
