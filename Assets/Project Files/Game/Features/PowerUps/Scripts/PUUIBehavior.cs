using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    /// <summary>Visual-only state for a Power Up bar item.</summary>
    public class PUUIBehavior : MonoBehaviour
    {
        [SerializeField] Image backgroundImage;
        [SerializeField] Image iconImage;
        [SerializeField] GameObject defaultElementsObjects;
        [SerializeField] GameObject amountContainerObject;
        [SerializeField] TextMeshProUGUI amountText;
        [SerializeField] GameObject amountPurchaseObject;
        [SerializeField] GameObject busyStateVisualsObject;
        [SerializeField] GameObject selectedOutlineObject;
        [SerializeField] GameObject timerObject;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] Image timerBackground;
        [SerializeField] GameObject lockStateObject;
        [SerializeField] TextMeshProUGUI lockText;

        public void ShowPreview(int amount, Color backgroundColor)
        {
            if (backgroundImage != null) backgroundImage.color = backgroundColor;
            if (iconImage != null) iconImage.color = Color.white;
            if (defaultElementsObjects != null) defaultElementsObjects.SetActive(true);
            if (amountContainerObject != null) amountContainerObject.SetActive(true);
            if (amountText != null) amountText.text = amount.ToString();
            if (amountPurchaseObject != null) amountPurchaseObject.SetActive(false);
            if (busyStateVisualsObject != null) busyStateVisualsObject.SetActive(false);
            if (selectedOutlineObject != null) selectedOutlineObject.SetActive(false);
            if (timerObject != null) timerObject.SetActive(false);
            if (lockStateObject != null) lockStateObject.SetActive(false);
        }
    }
}

