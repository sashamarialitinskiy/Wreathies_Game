using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    public CanvasGroup cg;
    public float min = 0.4f, max = 1f, speed = 2f;

    void Reset(){ cg = GetComponent<CanvasGroup>(); }
    void Update()
    {
        if (!cg) return;
        float k = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        cg.alpha = Mathf.Lerp(min, max, k);
    }
}
