using LichLord.Buildables;
using LichLord.NonPlayerCharacters;
using LichLord.Props;
using LichLord.World;
using UnityEngine;

namespace LichLord.UI
{
    public class UIFloatingTooltip : UIFloatingWidget
    {
        [Header("UI Elements")]

        [SerializeField] private UIStockpileTooltip _stockpileTooltip;
        [SerializeField] private UICryptTooltip _cryptTooltip;
        [SerializeField] private UINonPlayerCharacterTooltip _npcTooltip;

        private IChunkTrackable _trackableTarget;

        public void SetTooltipTarget(IChunkTrackable target)
        {
            _trackableTarget = target;

            _npcTooltip.gameObject.SetActive(false);
            _cryptTooltip.gameObject.SetActive(false);
            _stockpileTooltip.gameObject.SetActive(false);

            if (_trackableTarget == null)
            {
                base.SetTarget(null);
                return;
            }

            if (_trackableTarget is NonPlayerCharacter npc)
            {
                _npcTooltip.gameObject.SetActive(true);
                base.SetTarget(npc.CachedTransform);
                return;
            }

            if (_trackableTarget is Crypt crypt)
            {
                _cryptTooltip.gameObject.SetActive(true);
                base.SetTarget(crypt.CachedTransform);
                return;
            }

            if (_trackableTarget is Stockpile stockpile)
            { 
                _stockpileTooltip.gameObject.SetActive(true);
                base.SetTarget(stockpile.CachedTransform);
                return;
            }

            if (_trackableTarget is Prop prop)
                base.SetTarget(prop.CachedTransform);
        }

        protected override void OnTick()
        {
            if (_trackableTarget == null)
                return;

            if (_trackableTarget is Crypt crypt)
            {
                _cryptTooltip.SetCryptData(crypt);
            }
            else if (_trackableTarget is NonPlayerCharacter npc)
            {
                _npcTooltip.SetNpcData(npc);
            }
            else if (_trackableTarget is Stockpile stockpile)
            {
                _stockpileTooltip.SetStockpileData(stockpile);
            }

        }
    }
}