using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SubMessageSequence : MonoBehaviour
{
    [Tooltip("Order of mini-messages to play (each has a CanvasGroup).")]
    public List<CanvasGroup> messages = new List<CanvasGroup>();

    [Header("Timing (seconds)")]
    public float fadeIn = 0.2f;
    public float hold   = 1.2f;
    public float fadeOut= 0.2f;

    [Header("Typewriter")]
    public bool useTypewriter = true;
    public float charDelay = 0.02f;

    [Header("Autoplay")]
    public bool playOnEnable = true;

    [Header("Events")]
    public UnityEvent onSequenceFinished;

    void OnEnable()
    {
        if (playOnEnable) StartCoroutine(Play());
    }

    public IEnumerator Play()
    {
        // start hidden
        foreach (var g in messages) if (g) g.alpha = 0f;

        for (int i = 0; i < messages.Count; i++)
        {
            var g = messages[i];
            if (!g) continue;

            yield return Fade(g, 0f, 1f, fadeIn);

            if (useTypewriter) yield return TypeAllTMP(g.transform, charDelay);

            yield return WaitUnscaled(hold);

            yield return Fade(g, 1f, 0f, fadeOut);
        }

        onSequenceFinished?.Invoke();
    }

    IEnumerator Fade(CanvasGroup g, float a, float b, float t)
    {
        if (!g) yield break;
        if (t <= 0f) { g.alpha = b; yield break; }
        float t0 = 0f; g.alpha = a;
        while (t0 < t)
        {
            t0 += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(a, b, t0 / t);
            yield return null;
        }
        g.alpha = b;
    }

    IEnumerator TypeAllTMP(Transform root, float delayPerChar)
    {
        var tmps = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
            yield return TypeOneTMP(tmps[i], delayPerChar);
    }

    IEnumerator TypeOneTMP(TMP_Text tmp, float delayPerChar)
    {
        if (!tmp) yield break;
        tmp.ForceMeshUpdate();
        int total = tmp.textInfo.characterCount;

        int originalMax = tmp.maxVisibleCharacters;
        tmp.maxVisibleCharacters = 0;

        for (int i = 0; i < total; i++)
        {
            tmp.maxVisibleCharacters = i + 1;
            yield return WaitUnscaled(delayPerChar);
        }

        // ensure fully visible after typing
        tmp.maxVisibleCharacters = total;
    }

    static IEnumerator WaitUnscaled(float s)
    {
        float t = 0f;
        while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
    }
}
