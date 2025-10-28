using UnityEngine;

// This script handles all the sound effects in the game.
// It keeps all the audio clips (like click, pop, correct, wrong) in one place
// so that other scripts can easily play them by calling AudioManager.Instance.PlayX().

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // A single global copy that other scripts can access

    [Header("Source")]
    public AudioSource sfxSource; // The audio source that actually plays the sounds

    [Header("Clips")]
    // These are sound files assigned in the Unity Inspector
    public AudioClip clickClip;     // Sound for clicking buttons
    public AudioClip grabClip;      // Sound for picking up a flower
    public AudioClip popClip;       // Sound for placing a flower down
    public AudioClip correctClip;   // Sound for correct answer feedback
    public AudioClip wrongClip;     // Sound for incorrect feedback

    // NEW: plays when the user finishes placing or choosing a bow
    public AudioClip bowPlacedClip;

    private void Awake()
    {
        // This ensures only one AudioManager exists in the scene.
        // If another is created by accident, it destroys itself.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Each of these small methods plays a sound effect, if the clip is assigned.
    // PlayOneShot() means it plays once without interrupting any other sounds.

    public void PlayClick()   { if (clickClip)   sfxSource.PlayOneShot(clickClip); }
    public void PlayGrab()    { if (grabClip)    sfxSource.PlayOneShot(grabClip); }
    public void PlayPop()     { if (popClip)     sfxSource.PlayOneShot(popClip); }
    public void PlayCorrect() { if (correctClip) sfxSource.PlayOneShot(correctClip); }
    public void PlayWrong()   { if (wrongClip)   sfxSource.PlayOneShot(wrongClip); }

    // NEW: sound that plays after the player chooses or places a bow
    public void PlayBowPlaced()
    {
        if (bowPlacedClip) sfxSource.PlayOneShot(bowPlacedClip);
    }
}

