using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonFlashSwap : MonoBehaviour
{
    public Sprite normalSprite;   // leave empty to auto-use current sprite
    public Sprite pressedSprite;  // the one you want to show for 1 second
    public float duration = 1f;

    Button btn;
    Image img;
    Selectable.Transition savedTransition;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = btn.targetGraphic as Image;
        if (normalSprite == null && img != null) normalSprite = img.sprite;
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // prevent the Button from auto-swapping sprites while we control it
        savedTransition = btn.transition;
        btn.transition  = Selectable.Transition.None;

        // optional: block double-clicks
        btn.interactable = false;

        if (img && pressedSprite) img.sprite = pressedSprite;
        yield return new WaitForSeconds(duration);

        if (img && normalSprite) img.sprite = normalSprite;

        // clear UI selection so Highlighted state doesn't override our sprite
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        btn.transition  = savedTransition;
        btn.interactable = true;
    }
}
