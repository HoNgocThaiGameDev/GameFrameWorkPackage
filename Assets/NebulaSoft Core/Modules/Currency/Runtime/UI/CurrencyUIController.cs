using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public class CurrencyUIController : MonoBehaviour
    {
        private const float DISALBE_PANEL_IN_SECONDS = 5.0f;

        [SerializeField] StaticCurrencyUI[] staticPanels;

        [SerializeField] GameObject panelObject;
        [SerializeField] Transform parentTrasnform;
        public Transform PanelsParent => parentTrasnform;

        private Pool panelPool;

        private Dictionary<CurrencyType, CurrencyUI> activePanelsUI;
        private List<CurrencyType> currenciesToHide = new List<CurrencyType>();

        public void Init(Currency[] currencies)
        {
            panelPool = new Pool(panelObject, parentTrasnform);

            activePanelsUI = new Dictionary<CurrencyType, CurrencyUI>();

            for(int i = 0; i < staticPanels.Length; i++)
            {
                Currency currency = CurrencyController.GetCurrency(staticPanels[i].CurrencyType);

                CurrencyUI currencyUI = staticPanels[i].CurrencyPanel;
                currencyUI.Init(currency);
                currencyUI.Show();

                activePanelsUI.Add(staticPanels[i].CurrencyType, staticPanels[i].CurrencyPanel);
            }

            for (int i = 0; i < currencies.Length; i++)
            {
                if (!activePanelsUI.ContainsKey(currencies[i].CurrencyType) && (currencies[i].Data.DisplayAlways || currencies[i].Amount > 0))
                {
                    GameObject currencyObject = panelPool.GetPooledObject();
                    currencyObject.transform.SetParent(parentTrasnform);
                    currencyObject.transform.ResetLocal();
                    currencyObject.transform.SetAsLastSibling();
                    currencyObject.SetActive(true);

                    CurrencyUI currencyUI = currencyObject.GetComponent<CurrencyUI>();
                    currencyUI.Init(currencies[i]);
                    currencyUI.Show();

                    activePanelsUI.Add(currencies[i].CurrencyType, currencyUI);
                }

                currencies[i].OnCurrencyChanged += RedrawCurrency;
            }
        }

        private void OnDestroy()
        {
            CurrencyController.UnsubscribeGlobalCallback(RedrawCurrency);

            activePanelsUI = null;

            panelPool?.Destroy();
        }

        public CurrencyUI GetCurrencyUI(CurrencyType type)
        {
            if (activePanelsUI.ContainsKey(type))
            {
                return activePanelsUI[type];
            }
            else
            {
                return ActivateCurrency(type);
            }
        }

        public void ActivateAllExistingCurrencies()
        {
            Currency[] activeCurrencies = CurrencyController.Currencies;
            for (int i = 0; i < activeCurrencies.Length; i++)
            {
                if (activeCurrencies[i].Amount > 0)
                    ActivateCurrency(activeCurrencies[i].CurrencyType);
            }
        }

        public void RedrawCurrency(Currency currency, int amount)
        {
            CurrencyType type = currency.CurrencyType;

            if (activePanelsUI.ContainsKey(type))
            {
                activePanelsUI[type].Redraw();

                if(currency.Amount == 0)
                {
                    if (!currency.Data.DisplayAlways)
                    {
                        activePanelsUI[type].DisableAfter(DISALBE_PANEL_IN_SECONDS, delegate
                        {
                            if (activePanelsUI.ContainsKey(type))
                                activePanelsUI.Remove(type);
                        });
                    }
                }
                else
                {
                    activePanelsUI[type].KillDisable();
                }
            }
            else
            {
                ActivateCurrency(type);
            }
        }

        public CurrencyUI ActivateCurrency(CurrencyType type)
        {
            // Check if panel is disabled
            if (!activePanelsUI.ContainsKey(type))
            {
                if (currenciesToHide.Contains(type))
                    return null;

                // Get object from pool
                GameObject currencyObject = panelPool.GetPooledObject();
                currencyObject.transform.SetParent(parentTrasnform);
                currencyObject.transform.ResetLocal();
                currencyObject.transform.SetAsLastSibling();
                currencyObject.SetActive(true);

                // Get currency from database
                Currency currency = CurrencyController.GetCurrency(type);

                // Get UI panel component
                CurrencyUI currencyUI = currencyObject.GetComponent<CurrencyUI>();
                currencyUI.Init(currency);
                currencyUI.Show();

                activePanelsUI.Add(type, currencyUI);

                return currencyUI;
            }
            // Rewrite panel state
            else
            {
                CurrencyUI currencyUI = activePanelsUI[type];

                // Check if panel require disable reset
                if (!currencyUI.Currency.Data.DisplayAlways)
                {
                    currencyUI.KillDisable();
                    currencyUI.ShowImmediately();
                }

                // Redraw
                currencyUI.Redraw();

                return currencyUI;
            }
        }

        public void DisableCurrency(CurrencyType type)
        {
            if(!currenciesToHide.Contains(type))
            {
                currenciesToHide.Add(type);
            }

            // check if panel is active state
            if (activePanelsUI.ContainsKey(type) && !CurrencyController.GetCurrency(type).Data.DisplayAlways)
            {
                activePanelsUI[type].Hide();
                activePanelsUI.Remove(type);
            }
        }


        [System.Serializable]
        private class StaticCurrencyUI
        {
            public CurrencyType CurrencyType;
            public CurrencyUI CurrencyPanel;
        }
    }
}