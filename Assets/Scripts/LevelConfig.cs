using UnityEngine;

[CreateAssetMenu(menuName = "Wreathies/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string id;
    public bool isPractice;                         // mark first 1–2 levels true
    public TutorialDirector.Step[] tutorial;    // steps shown only in practice

    // Example level data (adapt to your project)
    public ClientData client;
    public float timeLimit = 60f;
    public int goalCount = 1;
    public bool enableTimer = true;
    public bool enableScoring = true;
}
