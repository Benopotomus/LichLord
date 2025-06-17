using UnityEngine;

namespace LichLord.UI
{
    public class ManeuverSlot : UIWidget
    {
        [SerializeField]
        private int _slot;

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;


        }

    }
}
