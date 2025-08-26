using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BowGlow : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    Outline[] outlines;
    Shadow[]  shadows;

    void Awake()
    {
        outlines = GetComponents<Outline>();
        shadows  = GetComponents<Shadow>();
        Set(false);
    }

    public void OnPointerEnter(PointerEventData e) => Set(true);
    public void OnPointerExit (PointerEventData e) => Set(false);
    public void OnSelect      (BaseEventData e)    => Set(true);
    public void OnDeselect    (BaseEventData e)    => Set(false);

    void Set(bool on)
    {
        foreach (var o in outlines) if (o) o.enabled = on;
        foreach (var s in shadows)  if (s) s.enabled = on;
    }
}
