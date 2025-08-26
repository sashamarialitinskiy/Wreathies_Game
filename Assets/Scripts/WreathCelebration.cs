using System.Collections;
using UnityEngine;

public class WreathCelebration : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer glowSR;        // drag the Glow child’s SpriteRenderer here
    public ParticleSystem sparklePS;     // drag the Particle System here

    [Header("Visuals")]
    public float pulseSpeed = 2f;        // how fast the glow “breathes”
    public float minAlpha   = 0.15f;     // lowest glow opacity
    public float maxAlpha   = 0.50f;     // highest glow opacity
    public float maxScale   = 1.08f;     // slight scale-up for the glow

    Coroutine playing;

    public void Play(float seconds)
    {
        if (playing != null) StopCoroutine(playing);

        if (glowSR)  glowSR.enabled = true;
        if (sparklePS) { sparklePS.Clear(true); sparklePS.Play(true); }

        playing = StartCoroutine(Run(seconds));
    }

    public void Stop()
    {
        if (playing != null) StopCoroutine(playing);

        // reset visuals
        if (glowSR)
        {
            var c = glowSR.color; c.a = 0f; glowSR.color = c;
            glowSR.transform.localScale = Vector3.one;
        }
        if (sparklePS) sparklePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        playing = null;
    }

    IEnumerator Run(float seconds)
    {
        float t = 0f;
        // start with alpha 0
        if (glowSR)
        {
            var c0 = glowSR.color; c0.a = 0f; glowSR.color = c0;
        }

        while (t < seconds)
        {
            // ping-pong 0…1
            float p = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

            if (glowSR)
            {
                // alpha pulse
                var c = glowSR.color;
                c.a = Mathf.Lerp(minAlpha, maxAlpha, p);
                glowSR.color = c;

                // tiny scale pulse
                float s = Mathf.Lerp(1f, maxScale, p);
                glowSR.transform.localScale = new Vector3(s, s, 1f);
            }

            t += Time.deltaTime;
            yield return null;
        }

        // tidy up
        Stop();
    }
}
