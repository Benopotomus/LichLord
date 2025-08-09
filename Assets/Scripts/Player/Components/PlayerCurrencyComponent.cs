using Fusion;
using UnityEngine;

namespace LichLord
{
    public class PlayerCurrencyComponent : ContextBehaviour
    {
        [SerializeField] private CurrencyDefinition _woodDefinition;
        [Networked] private int _wood { get; set; }
        public int Wood => _wood;

        [SerializeField] private CurrencyDefinition _stoneDefinition;
        [Networked] private int _stone { get; set; }
        public int Stone => _stone;

        [SerializeField] private CurrencyDefinition _ironDefinition;
        [Networked] private int _iron { get; set; }
        public int Iron => _iron;

        [SerializeField] private CurrencyDefinition _goldDefinition;
        [Networked] private int _gold { get; set; }
        public int Gold => _gold;

        [SerializeField] private CurrencyDefinition _soulsDefinition;
        [Networked] private int _souls { get; set; }
        public int Souls => _souls;

        private int _woodMax = 100;
        private int _stoneMax = 100;
        private int _ironMax = 100;
        private int _goldMax = 100;
        private int _soulsMax = 100;

        public override void Spawned()
        {
            base.Spawned();
            ReplicateToAll(false);
        }

        public bool HasRoomForCurrency(CurrencyDefinition currencyDefinition, int resourceCount)
        {
            switch (currencyDefinition.CurrencyType)
            {
                case ECurrencyType.Wood:
                    return (_woodMax >= (_wood + resourceCount));
                case ECurrencyType.Stone:
                    return (_stoneMax >= (_stone + resourceCount));
                case ECurrencyType.Iron:
                    return (_ironMax >= (_iron + resourceCount));
                case ECurrencyType.Gold:
                    return (_goldMax >= (_gold + resourceCount));
                case ECurrencyType.Souls:
                    return (_soulsMax >= (_souls + resourceCount));
            }

            return false;
        }

        public void AddCurrency(CurrencyDefinition currencyDefinition, int resourceCount)
        {
            switch (currencyDefinition.CurrencyType)
            {
                case ECurrencyType.Wood:
                    _wood += resourceCount;
                    break;
                case ECurrencyType.Stone:
                    _stone += resourceCount;
                    break;
                case ECurrencyType.Iron:
                    _iron += resourceCount;
                    break;
                case ECurrencyType.Gold:
                    _gold += resourceCount;
                    break;
                case ECurrencyType.Souls:
                    _souls += resourceCount;
                    break;
            }

        }

        public CurrencyDefinition GetCurrencyDefinition(ECurrencyType currencyType)
        {
            switch (currencyType)
            {
                case ECurrencyType.Wood:
                    return _woodDefinition;
                case ECurrencyType.Stone:
                    return _stoneDefinition;
                case ECurrencyType.Iron:
                    return _ironDefinition;
                case ECurrencyType.Gold:
                    return _goldDefinition;
                case ECurrencyType.Souls:
                    return _soulsDefinition;
            }

            return null;
        }

        public int GetCurrencyCount(ECurrencyType currencyType)
        {
            switch (currencyType)
            {
                case ECurrencyType.Wood:
                    return _wood;
                case ECurrencyType.Stone:
                    return _stone;
                case ECurrencyType.Iron:
                    return _iron;
                case ECurrencyType.Gold:
                    return _gold;
                case ECurrencyType.Souls:
                    return _souls;
            }

            return 0;
        }

        public int GetCurrencyMax(ECurrencyType currencyType)
        {
            switch (currencyType)
            {
                case ECurrencyType.Wood:
                    return _woodMax;
                case ECurrencyType.Stone:
                    return _stoneMax;
                case ECurrencyType.Iron:
                    return _ironMax;
                case ECurrencyType.Gold:
                    return _goldMax;
                case ECurrencyType.Souls:
                    return _soulsMax;
            }

            return 0;
        }
    }
}
