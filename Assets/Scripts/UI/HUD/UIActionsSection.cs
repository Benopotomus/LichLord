
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIActionsSection : UIWidget
    {
        [SerializeField] private GridLayoutGroup _spellIconsSection;
        [SerializeField] private GridLayoutGroup _summonIconsSection;
        [SerializeField] private GridLayoutGroup _commandIconsSection;

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

            if (state is SpellcastState)
            {
                SetLayout(EActiveLayout.SpellMode);
            }
            else if (state is BuildModeState)
            {
                SetLayout(EActiveLayout.BuildMode);
            }
            else if (state is SummonModeState)
            {
                SetLayout(EActiveLayout.SummonMode);
            }
            else if (state is CommandModeState)
            {
                SetLayout(EActiveLayout.CommandMode);
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
                    _spellIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(false);
                    _commandIconsSection.SetActive(false);
                    _buildIconsSection.SetActive(false);

                    _manueverIcon.SetActive(false);
                    _buildCategorySection.SetActive(false);
                    _buildIcon.SetActive(false);
                    break;
                case EActiveLayout.SpellMode:
                    _spellIconsSection.SetActive(true);
                    _summonIconsSection.SetActive(false);
                    _commandIconsSection.SetActive(false);
                    _buildIconsSection.SetActive(false);

                    _manueverIcon.SetActive(true);
                    _buildCategorySection.SetActive(false);
                    _buildIcon.SetActive(false);
                    break;
                case EActiveLayout.SummonMode:
                    _spellIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(true);
                    _commandIconsSection.SetActive(false);
                    _buildIconsSection.SetActive(false);

                    _manueverIcon.SetActive(true);
                    _buildCategorySection.SetActive(false);
                    _buildIcon.SetActive(false);
                    break;
                case EActiveLayout.CommandMode:
                    _spellIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(false);
                    _commandIconsSection.SetActive(true);
                    _buildIconsSection.SetActive(false);

                    _manueverIcon.SetActive(true);
                    _buildCategorySection.SetActive(false);
                    _buildIcon.SetActive(false);
                    break;
                case EActiveLayout.BuildMode:
                    _spellIconsSection.SetActive(false);
                    _summonIconsSection.SetActive(false);
                    _commandIconsSection.SetActive(false);
                    _buildIconsSection.SetActive(true);

                    _manueverIcon.SetActive(false);
                    _buildIcon.SetActive(false);
                    _buildCategorySection.SetActive(true);
                    break;
            }

            _layout = newLayout;
        }

        public enum EActiveLayout
        { 
            None,
            SpellMode,
            BuildMode,
            SummonMode,
            CommandMode,
        }
    }
}
