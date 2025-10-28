using UnityEngine;
using UnityEngine.UI;

public class LevelProgressBar : MonoBehaviour
{
    private Slider slider;

    [SerializeField] private int totalLevels = 10;

    void Awake()
    {
        slider = GetComponent<Slider>();          // because this script sits on the Slider
        slider.minValue = 0;
        slider.maxValue = totalLevels;
        SetLevel(1);                               // start at level 1
    }

    // Call this whenever you change levels (1..totalLevels)
    public void SetLevel(int level1Based)
    {
        if (slider == null) return;
        slider.value = Mathf.Clamp(level1Based, 1, totalLevels);
    }
}
