using System.IO;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Core Settings", menuName = "Data/Core/Core Settings")]
    public class CoreSettings : ScriptableObject
    {
        [Header("Path")]
        [SerializeField] string dataFolder = Path.Combine("Assets", "Project Files", "Data");
        public string DataFolder => dataFolder;

        [SerializeField] string scenesFolder = Path.Combine("Assets", "Project Files", "Game", "Scenes");
        public string ScenesFolder => scenesFolder;

        [Header("Init")]
        [SerializeField] string initSceneName = "Init";
        public string InitSceneName => initSceneName;

        [SerializeField] bool autoLoadInitializer = true;
        public bool AutoLoadInitializer => autoLoadInitializer;

        [Header("Editor")]
        [SerializeField] bool useCustomInspector = true;
        public bool UseCustomInspector => useCustomInspector;

        [SerializeField] bool useHierarchyIcons = true;
        public bool UseHierarchyIcons => useHierarchyIcons;

        [Header("Ads")]
        [SerializeField] Color adsDummyBackgroundColor = new Color(0.1f, 0.2f, 0.35f, 1.0f);
        public Color AdsDummyBackgroundColor => adsDummyBackgroundColor;

        [SerializeField] Color adsDummyMainColor = new Color(0.15f, 0.37f, 0.6f, 1.0f);
        public Color AdsDummyMainColor => adsDummyMainColor;

        [Header("Other")]
        [SerializeField] bool showNebulaSoftPromotions = false;
        public bool ShowNebulaSoftPromotions => showNebulaSoftPromotions;
    }
}
