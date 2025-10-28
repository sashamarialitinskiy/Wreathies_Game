using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// This script controls what appears inside the "Client Box" —
// the UI that shows either a friendly greeting (“Hi there!”) or
// the full client request (“I want my wreath: 1/2 red, 1/2 blue”).
// It handles fading between the two views and showing the correct images and text.

[DisallowMultipleComponent] // Ensures only one copy of this script can exist on a GameObject
public class ClientBoxController : MonoBehaviour
{
    // =================== UI GROUPS ===================
    [Header("Groups under ClientBox")]
    public CanvasGroup greetingGroup;     // The “Hi there!” greeting section (simple version)
    public CanvasGroup fullGroup;         // The full client info section (name + request)

    // =================== GREETING UI ===================
    [Header("Greeting")]
    public TMP_Text greetingLabel;        // The greeting text (“Hi there!”)
    [TextArea] public string defaultGreeting = "Hi there!"; // Default greeting if nothing else is set
    public bool startInGreeting = true;   // If true, show only the greeting when the scene starts

    // =================== FULL REQUEST UI ===================
    [Header("Full content (separate fields)")]
    public TMP_Text nameLabel;            // The client’s name (“Hi, I’m ___.”)
    public TMP_Text requestLabel;         // The client’s request (“I want my wreath: …”)
    public Image requestImage;            // The preview picture of the requested wreath

    // =================== AVATAR (SHARED BETWEEN STATES) ===================
    [Header("Avatar (shared)")]
    [Tooltip("Put this Image as a direct child of ClientBox (NOT inside Greeting/Full).")]
    public Image avatarImage;             // The client’s face image — shown in both modes
    public bool avatarVisibleInGreeting = true;  // Should avatar be visible during greeting?
    public bool avatarVisibleInFull = true;      // Should avatar be visible during full request?

    // =================== FADE ANIMATION SETTINGS ===================
    [Header("Fade")]
    public float fadeIn = 0.25f;          // How long to fade in when revealing the full request
    public float fadeOut = 0.15f;         // How long to fade out when hiding the greeting

    // Runs automatically when the object is enabled in Unity
    void OnEnable()
    {
        // Start by showing either the greeting or the full request view
        if (startInGreeting) ShowGreetingOnly(defaultGreeting);
        else ShowFullImmediate();
    }

    // =================== PUBLIC METHODS ===================

    /// <summary>
    /// Show only the greeting message (“Hi there!”) and hide the rest.
    /// </summary>
    public void ShowGreetingOnly(string textOverride = null)
    {
        // Use the provided greeting text if given, otherwise the default
        if (greetingLabel)
            greetingLabel.text = string.IsNullOrEmpty(textOverride) ? defaultGreeting : textOverride;

        // Hide the full view and show the greeting
        SetGroup(fullGroup, false, 0f);
        SetGroup(greetingGroup, true, 1f);

        // Show or hide avatar depending on settings
        SetAvatarVisible(avatarVisibleInGreeting);

        // Make sure this whole box is visible
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Fill in the full view (name, request, images).
    /// Call this before showing the full request.
    /// </summary>
    public void SetFullContent(string nameText = null, string requestText = null,
                               Sprite requestSprite = null, Sprite avatarSprite = null)
    {
        // Update each field only if a new value was passed in
        if (nameLabel && !string.IsNullOrEmpty(nameText)) nameLabel.text = nameText;
        if (requestLabel && !string.IsNullOrEmpty(requestText)) requestLabel.text = requestText;
        if (requestImage && requestSprite) requestImage.sprite = requestSprite;
        if (avatarImage && avatarSprite) avatarImage.sprite = avatarSprite;
    }

    /// <summary>
    /// Fades from the greeting to the full client request.
    /// </summary>
    public IEnumerator RevealFullPrepared()
    {
        gameObject.SetActive(true);

        // Fade out the greeting section
        if (greetingGroup)
        {
            yield return FadeTo(greetingGroup, 0f, fadeOut); // fade alpha to 0
            SetGroup(greetingGroup, false, 0f);              // then hide it
        }

        // Fade in the full client info section
        if (fullGroup)
        {
            SetGroup(fullGroup, true, fullGroup.alpha);      // make sure it's active
            SetAvatarVisible(avatarVisibleInFull);           // show/hide avatar if needed
            yield return FadeTo(fullGroup, 1f, fadeIn);      // fade alpha up to 1 (visible)
        }
        else
        {
            // Safety backup: if fullGroup isn’t set, just enable the individual pieces
            EnableIfExists(nameLabel?.gameObject);
            EnableIfExists(requestLabel?.gameObject);
            EnableIfExists(requestImage?.gameObject);
            SetAvatarVisible(avatarVisibleInFull);
        }
    }

    /// <summary>
    /// Instantly switch to the full state (no fade transition).
    /// </summary>
    public void ShowFullImmediate()
    {
        SetGroup(greetingGroup, false, 0f); // Hide greeting
        SetGroup(fullGroup, true, 1f);      // Show full info immediately
        SetAvatarVisible(avatarVisibleInFull);
    }

    // =================== HELPER METHODS ===================

    // Show or hide the avatar image
    void SetAvatarVisible(bool on)
    {
        if (!avatarImage) return; // Do nothing if avatar not assigned
        var go = avatarImage.gameObject;
        if (go.activeSelf != on) go.SetActive(on); // Only toggle if needed
    }

    // Turns an object ON safely if it exists
    static void EnableIfExists(GameObject go)
    {
        if (go) go.SetActive(true);
    }

    // Controls CanvasGroup visibility and interactivity
    static void SetGroup(CanvasGroup g, bool on, float alpha)
    {
        if (!g) return;
        g.gameObject.SetActive(on);   // Enable or disable the group
        g.alpha = alpha;              // Set transparency
        bool interact = on && alpha >= 0.999f; // Interactable only if fully visible
        g.interactable = interact;
        g.blocksRaycasts = interact;
    }

    // Smoothly fades a CanvasGroup to a target transparency (alpha)
    IEnumerator FadeTo(CanvasGroup g, float target, float time)
    {
        if (!g) yield break;                     // If missing, skip
        if (time <= 0f) { g.alpha = target; yield break; } // Instant change if no time

        float start = g.alpha, t = 0f;

        // Gradually change alpha over time
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, target, t / time); // Linear fade
            yield return null; // wait one frame
        }

        g.alpha = target; // Set final alpha
    }
}
