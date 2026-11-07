#pragma warning disable 0649

using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class FeatureAnnouncementPopupData
    {
        [SerializeField] bool showAnnouncementPopup;
        public bool ShowAnnouncementPopup => showAnnouncementPopup;

        [SerializeField] Sprite previewSprite;
        public Sprite PreviewSprite => previewSprite;

        [SerializeField] string description;
        public string Description => description;
    }
}
