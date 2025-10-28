using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastScan : MonoBehaviour
{
    public GraphicRaycaster raycaster;   // assign your top Canvas raycaster (or leave null to auto-find)
    public EventSystem eventSystem;      // auto-found if left null

    PointerEventData ped;
    readonly List<RaycastResult> results = new();

    void Awake()
    {
        if (!raycaster)    raycaster    = FindObjectOfType<GraphicRaycaster>(true);
        if (!eventSystem)  eventSystem  = FindObjectOfType<EventSystem>(true);
        ped = new PointerEventData(eventSystem);
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;   // log on click

        ped.position = Input.mousePosition;
        results.Clear();
        raycaster.Raycast(ped, results);

        var sb = new StringBuilder();
        sb.AppendLine("UI hits (front→back):");
        if (results.Count == 0) sb.AppendLine("(none)");
        for (int i = 0; i < results.Count; i++)
        {
            var tr = results[i].gameObject.transform;
            sb.Append(i == 0 ? "→ " : "  ");
            sb.AppendLine(GetPath(tr));
        }
        Debug.Log(sb.ToString());
    }

    static string GetPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Add(t.name); t = t.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
