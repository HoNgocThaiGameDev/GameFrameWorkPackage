#pragma warning disable 0414

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{

    // UI Module v0.9.0
    public class UITouchHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool isMouseDown = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("[UI Module] On screen touched.");
            isMouseDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isMouseDown = false;
        }
    }
}