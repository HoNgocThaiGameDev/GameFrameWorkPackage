using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class UITargetObject : MonoBehaviour
    {
        [SerializeField] Image previewImage;
        public Image PreviewImage => previewImage;
        public void Init(UITargetPanel targetPanel) { }
        public void PlayImpactAnimation() { }
        public void OnFlyingObjectHit() => GameController.ShowCompleteUI();
        public void OnCollectAnimationPlayed() => GameController.ShowCompleteUI();
    }
}
