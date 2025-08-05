using Fusion;
using UnityEngine;

namespace LichLord
{
    public class PlayerCurrencyComponent : ContextBehaviour
    {
        [Networked] private int _wood { get; set; }
        public int Wood => _wood;

        [Networked] private int _stone { get; set; }
        public int Stone => _stone;

        [Networked] private int _iron { get; set; }
        public int Iron => _iron;

        [Networked] private int _souls { get; set; }
        public int Souls => _souls;

        public override void Spawned()
        {
            base.Spawned();
            ReplicateToAll(false);
        }

        public void AddCurrency(CurrencyDefinition currencyDefinition, int resourceCount)
        {
            switch (currencyDefinition.CurrencyType)
            {
                case ECurrencyType.Stone:
                    _stone += resourceCount;
                    Debug.Log(_stone);
                    break;
            }

        }
    }
}
