namespace LichLord.UI
{
    public class UIWorldCurrencySlot : UICurrencySlot
    {
        private int _localValue = 0;

        protected override void OnVisible()
        {
            base.OnVisible();
        }

        protected override void OnTick()
        {
            base.OnTick();

            if (!Context.IsGameplayActive())
                return;

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            var currencyComponent = pc.Currency;
            var currencyType = _definition.CurrencyType;
            var playerValue = currencyComponent.GetCurrencyCount(currencyType);
            int newValue = playerValue;

            if (Context.ContainerManager.AllCurrencies.TryGetValue(currencyType, out int worldValue))
                newValue += worldValue;

            if (newValue != _localValue)
            {
                _text.text = newValue.ToString();
                _localValue = newValue;
            }

        }
    }
}
