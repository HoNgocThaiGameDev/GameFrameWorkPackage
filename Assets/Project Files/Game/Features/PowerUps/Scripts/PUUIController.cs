using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    /// <summary>UI-only Power Up bar used when gameplay data is not part of this SDK project.</summary>
    public class PUUIController : MonoBehaviour
    {
        [SerializeField] Transform containerTransform;
        [SerializeField] GameObject itemPrefab;
        [SerializeField] RectTransform floatingTextRectTransform;
        [SerializeField] TextMeshProUGUI floatingText;
        [SerializeField] float floatingTextDelay = 1.0f;
        [SerializeField] bool showSelectionPanel;
        [SerializeField] GameObject selectionPanelObject;
        [SerializeField] Image selectionIconImage;
        [SerializeField] TextMeshProUGUI selectionDescriptionText;
        [SerializeField] Button selectionCloseButton;

        readonly List<PUUIBehavior> uiBehaviors = new();
        bool initialized;
        public PUUIBehavior[] UIBehaviors => uiBehaviors.ToArray();

        void Awake() => Init();

        public void Init()
        {
            if (initialized || containerTransform == null || itemPrefab == null) return;
            initialized = true;
            Color[] colors = { new(0.98f, 0.43f, 0.32f), new(0.28f, 0.67f, 0.94f), new(0.57f, 0.48f, 0.91f), new(0.96f, 0.72f, 0.24f) };
            for (int i = 0; i < colors.Length; i++)
            {
                GameObject item = Instantiate(itemPrefab, containerTransform);
                item.name = $"Power Up {i + 1}";
                item.SetActive(true);
                PUUIBehavior behavior = item.GetComponent<PUUIBehavior>();
                if (behavior == null) continue;
                behavior.ShowPreview(i + 1, colors[i]);
                uiBehaviors.Add(behavior);
            }
        }

        public void HidePanels() { foreach (PUUIBehavior behavior in uiBehaviors) if (behavior != null) behavior.gameObject.SetActive(false); }
        public void ShowPanels() { foreach (PUUIBehavior behavior in uiBehaviors) if (behavior != null) behavior.gameObject.SetActive(true); }
    }
}

