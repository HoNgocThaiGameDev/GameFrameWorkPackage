using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Slider))]
public sealed class ProgressSliderExactFill : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    private Slider slider;
    private float lastNormalizedValue = -1f;

    private void OnEnable()
    {
        CacheReferences();

        if (slider != null)
            slider.onValueChanged.AddListener(OnSliderValueChanged);

        UpdateFill();
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnValidate()
    {
        CacheReferences();
        UpdateFill();
    }

    private void LateUpdate()
    {
        UpdateFill();
    }

    private void CacheReferences()
    {
        slider = GetComponent<Slider>();

        if (fillImage == null)
        {
            var fillTransform = transform.Find("Fill Area/Progress Fill");
            fillImage = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
        }
    }

    private void OnSliderValueChanged(float _)
    {
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (slider == null || fillImage == null)
        {
            CacheReferences();
            if (slider == null || fillImage == null)
                return;
        }

        var normalizedValue = slider.normalizedValue;
        if (Mathf.Approximately(lastNormalizedValue, normalizedValue) &&
            Mathf.Approximately(fillImage.fillAmount, normalizedValue))
            return;

        fillImage.fillAmount = normalizedValue;
        lastNormalizedValue = normalizedValue;
    }
}
