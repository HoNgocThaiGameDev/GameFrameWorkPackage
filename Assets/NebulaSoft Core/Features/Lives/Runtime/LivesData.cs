using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Lives Data", menuName = "Data/Lives")]
    public class LivesData : ScriptableObject
    {
        [SerializeField] int maxLivesCount = 5;
        public int MaxLivesCount => maxLivesCount;

        [Tooltip("In seconds")]
        [SerializeField] int oneLifeRestorationDuration = 1200;
        public int OneLifeRestorationDuration => oneLifeRestorationDuration;

        [Header("Reward")]
        [SerializeField] Sprite rewardPreviewSprite;
        public Sprite RewardPreviewSprite => rewardPreviewSprite;

        [SerializeField] GameObject rewardPreviewPrefab;
        public GameObject RewardPreviewPrefab => rewardPreviewPrefab;

        [SerializeField] GameObject rewardMaxLivesPreviewPrefab;
        public GameObject RewardMaxLivesPreviewPrefab => rewardMaxLivesPreviewPrefab;

        [SerializeField] GameObject rewardInfiniteModePreviewPrefab;
        public GameObject RewardInfiniteModePreviewPrefab => rewardInfiniteModePreviewPrefab;
    }
}