using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void LoadLevel(int levelIndex)
    {
        Debug.Log("Loading level: " + levelIndex);
        // TODO: Later you could load a Unity scene with SceneManager.LoadScene
    }
}
