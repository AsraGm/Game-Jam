using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Animations;

public class MainScreen : MonoBehaviour
{
    public float fadeOutTime = 1f;
    public Image logoImage;
    public GameObject mainScreen;
    public GameObject textPressSpace;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(FadeOutLogo());
        }
    }

    public IEnumerator FadeOutLogo()
    {
        float elapsedTime = 0f;
        Color originalColor = logoImage.color;
        textPressSpace.SetActive(false);

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            logoImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        logoImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        mainScreen.SetActive(false);
    }
}
