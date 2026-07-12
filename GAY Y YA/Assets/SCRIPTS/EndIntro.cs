using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndIntro : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nombreEscenaDestino;
    [SerializeField] private float tiempoDelay = 2.0f;

    void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += AlTerminarVideo;
            videoPlayer.prepareCompleted += AlTerminarPrecarga;
        }
    }

    void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= AlTerminarVideo;
            videoPlayer.prepareCompleted -= AlTerminarPrecarga;
        }
    }

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Prepare();
        }
    }

    void AlTerminarPrecarga(VideoPlayer vp)
    {
        vp.Play();
    }

    void AlTerminarVideo(VideoPlayer vp)
    {
        StartCoroutine(CambiarEscenaConDelay());
    }

    IEnumerator CambiarEscenaConDelay()
    {
        yield return new WaitForSeconds(tiempoDelay);

        SceneManager.LoadScene(nombreEscenaDestino);
    }
}
