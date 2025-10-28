using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SubMessageSequence : MonoBehaviour
{
    public enum Transition { Fade, Pop, Instant }

    [Tooltip("Order of mini-messages to play (each has a CanvasGroup).")]
    public List<CanvasGroup> messages = new List<CanvasGroup>();

    [Header("Timing (seconds)")]
    public float fadeIn  = 0.2f;
    public float hold    = 1.2f;
    public float fadeOut = 0.2f;

    [Header("Pop settings")]
    public Transition inMode  = Transition.Fade;   // set to Pop for “pop in”
    public Transition outMode = Transition.Fade;   // Fade / Pop / Instant
    public float popDuration  = 0.18f;
    public float popInScale   = 1.08f;
    public float popOutScale  = 0.92f;

    [Header("Options")]
    public bool deactivateOnHide = false;
    public bool useTypewriter = true;
    public float charDelay = 0.02f;
    public bool playOnEnable = true;

    [Header("Events")]
    public UnityEvent onSequenceFinished;

    void OnEnable()
    {
        if (playOnEnable) StartCoroutine(Play());
    }

    public System.Collections.IEnumerator Play()
    {
        // start hidden
        for (int i = 0; i < messages.Count; i++)
        {
            var g = messages[i];
            if (!g) continue;
            g.alpha = 0f;
            g.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < messages.Count; i++)
        {
            var g = messages[i];
            if (!g) continue;

            // APPEAR
            switch (inMode)
            {
                case Transition.Fade:    yield return Fade(g, 0f, 1f, fadeIn); break;
                case Transition.Pop:     yield return Pop(g, true,  popDuration, popInScale); break;
                case Transition.Instant: g.alpha = 1f; g.transform.localScale = Vector3.one; break;
            }

            if (useTypewriter) yield return TypeAllTMP(g.transform, charDelay);

            // HOLD
            yield return WaitUnscaled(hold);

            // DISAPPEAR
            switch (outMode)
            {
                case Transition.Fade:    yield return Fade(g, 1f, 0f, fadeOut); break;
                case Transition.Pop:     yield return Pop(g, false, popDuration, popOutScale); g.alpha = 0f; break;
                case Transition.Instant: g.alpha = 0f; break;
            }

            if (deactivateOnHide) g.gameObject.SetActive(false);
        }

        onSequenceFinished?.Invoke();
    }

    System.Collections.IEnumerator Fade(CanvasGroup g, float a, float b, float t)
    {
        if (!g) yield break;
        if (t <= 0f) { g.alpha = b; yield break; }
        float x = 0f; g.alpha = a;
        while (x < t)
        {
            x += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(a, b, x / t);
            yield return null;
        }
        g.alpha = b;
    }

    System.Collections.IEnumerator Pop(CanvasGroup g, bool appearing, float duration, float targetScale)
    {
        if (!g) yield break;
        g.gameObject.SetActive(true);
        g.alpha = 1f;

        Vector3 start = appearing ? Vector3.one * (2f - targetScale) : Vector3.one;
        Vector3 end   = appearing ? Vector3.one : Vector3.one * targetScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            g.transform.localScale = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }
        g.transform.localScale = end;
    }

    System.Collections.IEnumerator TypeAllTMP(Transform root, float delayPerChar)
    {
        var tmps = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
            yield return TypeOneTMP(tmps[i], delayPerChar);
    }

    System.Collections.IEnumerator TypeOneTMP(TMP_Text tmp, float delayPerChar)
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
        tmp.maxVisibleCharacters = total;
    }

    static System.Collections.IEnumerator WaitUnscaled(float s)
    {
        float t = 0f;
        while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
    }
}
