using UnityEngine;
using UnityEngine.SceneManagement;

public class SCENECHANGER : MonoBehaviour
{
    private void Start()
    {
        Application.targetFrameRate = 60;
    }
    public void Lv1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lv 1");
    }
    public void EXIT()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("CREDITS");
    }

    public void REALEXIST()
    {
        Application.Quit();
    }
}
