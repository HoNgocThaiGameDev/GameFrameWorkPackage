using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaSoft
{
    public class DevPanelDebugToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] DevPanelSettings devPanelSettings;
        [SerializeField] float holdTime = 10f;

        private bool isHolding = false;
        private Coroutine holdCoroutine;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isHolding)
            {
                isHolding = true;
                holdCoroutine = StartCoroutine(HoldProgress());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isHolding)
            {
                isHolding = false;

                if (holdCoroutine != null)
                {
                    StopCoroutine(holdCoroutine);
                    holdCoroutine = null;
                }
            }
        }

        private IEnumerator HoldProgress()
        {
            float timer = 0f;

            while (timer < holdTime)
            {
                if (!isHolding)
                    yield break;

                timer += Time.unscaledDeltaTime;

                yield return null;
            }

            OnHoldComplete();
        }

        private void OnHoldComplete()
        {
            SystemMessage.ShowMessage("Dev Panel is enabled for this session!", 3f);

            devPanelSettings.SetState(true);

            DevPanelEnabler.LinkSettings(devPanelSettings);
            DevPanelEnabler.UpdateState();
        }
    }
}
