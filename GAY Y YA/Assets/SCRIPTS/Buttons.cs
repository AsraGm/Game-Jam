using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Buttons : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private float sceneChangeDelay = 0.2f;

    public void ShowWindow(GameObject target)
    {
        if (target != null)
            target.SetActive(true);
    }

    public void HideWindow(GameObject target)
    {
        if (target != null)
            target.SetActive(false);
    }

    public void LoadAScene(string sceneToLoad)
    {
        StartCoroutine(LoadSceneDelayed(sceneToLoad));
    }

    private IEnumerator LoadSceneDelayed(string sceneToLoad)
    {
        yield return new WaitForSeconds(sceneChangeDelay);
        SceneManager.LoadScene(sceneToLoad);
    }

    public void CloseGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

