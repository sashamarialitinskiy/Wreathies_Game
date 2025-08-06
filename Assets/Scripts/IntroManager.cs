using UnityEngine;
using System.Collections.Generic;

public class IntroManager : MonoBehaviour
{
    public List<GameObject> introPanels; // Assign in inspector
    private int currentIndex = 0;

    private void Start()
    {
        ShowOnlyCurrentPanel();
    }

    public void NextSlide()
    {
        currentIndex++;
        if (currentIndex >= introPanels.Count)
        {
            // Show client choice panel (safe entry point)
            GameManager.Instance.clientChoicePanel.SetActive(true);
            gameObject.SetActive(false);
        }
        else
        {
            ShowOnlyCurrentPanel();
        }
    }

    void ShowOnlyCurrentPanel()
    {
        for (int i = 0; i < introPanels.Count; i++)
        {
            introPanels[i].SetActive(i == currentIndex);
        }
    }
}
