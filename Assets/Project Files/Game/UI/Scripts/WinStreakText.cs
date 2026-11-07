using NebulaSoft;
using TMPro;
using UnityEngine;

public sealed class WinStreakText : MonoBehaviour
{
    [SerializeField] private TMP_Text streakText;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        WinStreakProgress.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        WinStreakProgress.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (streakText != null)
            streakText.text = $"x{WinStreakProgress.Current}";
    }

    private void CacheReferences()
    {
        if (streakText == null)
            streakText = transform.Find("Count")?.GetComponent<TMP_Text>();
    }
}
