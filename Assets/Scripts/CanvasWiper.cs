using System.Collections;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CanvasWiper : MonoBehaviour
{
    Image img;
    Material mat;
    public float wipeTime = 2;
    private float wipeTimer;
    private Vector2 wipePosition;
    private Vector2 screenDimensions;
    private Coroutine wipeCoroutine;
    public static CanvasWiper global;

    void Awake()
    {
        global = this;
    }

    void Start()
    {
        img = GetComponent<Image>();
        mat = img.materialForRendering;
        screenDimensions = new Vector2(Screen.width, Screen.height);
        wipePosition = screenDimensions;
        mat.SetFloat("_size", 0);
        StartCoroutine(loadIn());
    }

    public IEnumerator loadIn()
    {
        yield return new WaitForSeconds(0.2f);
        WipeInToPlayer();
    }

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void WipeOut()
    {
        StartCoroutine(wipeScreen(screenDimensions / 2, true));
    }

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void WipeIn()
    {
        StartCoroutine(wipeScreen(screenDimensions / 2, false));
    }

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void WipeOutToPlayer()
    {
        Vector2 playerPosition = Camera.main.WorldToScreenPoint(
            PlatformerController2D.global.transform.position
        );
        StartCoroutine(wipeScreen(playerPosition, true));
    }

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void WipeInToPlayer()
    {
        Vector2 playerPosition = Camera.main.WorldToScreenPoint(
            PlatformerController2D.global.transform.position
        );
        StartCoroutine(wipeScreen(playerPosition, false));
    }

    public IEnumerator wipeScreen(Vector2 wipePosition, bool fadeout)
    {
        if (wipeCoroutine != null)
            StopCoroutine(wipeCoroutine);

        wipeCoroutine = StartCoroutine(wipeScreenInternal(wipePosition, fadeout));
        yield return wipeCoroutine;
    }

    private IEnumerator wipeScreenInternal(Vector2 wipePosition, bool fadeout)
    {
        wipePosition = new Vector2(
            Mathf.Clamp(wipePosition.x, 0, screenDimensions.x),
            Mathf.Clamp(wipePosition.y, 0, screenDimensions.y)
        );
        this.wipePosition = wipePosition;
        wipeTimer = wipeTime;
        float[] distances =
        {
            Vector2.Distance(wipePosition, Vector2.zero),
            Vector2.Distance(wipePosition, screenDimensions),
            Vector2.Distance(wipePosition, new Vector2(0, screenDimensions.y)),
            Vector2.Distance(wipePosition, new Vector2(screenDimensions.x, 0)),
        };
        // Debug.DrawLine(wipePosition, Vector2.zero, Color.red, 20);
        // Debug.DrawLine(wipePosition, screenDimensions, Color.red, 20);
        // Debug.DrawLine(wipePosition, new Vector2(0, screenDimensions.y), Color.red, 20);
        // Debug.DrawLine(wipePosition, new Vector2(screenDimensions.x, 0), Color.red, 20);
        float maxSize = distances.Max();
        while (true)
        {
            float targetSize = maxSize * (1 - Mathf.Pow(wipeTimer, 2) / Mathf.Pow(wipeTime, 2));
            if (fadeout)
                targetSize = maxSize * Mathf.Pow(wipeTimer, 2) / Mathf.Pow(wipeTime, 2);
            mat.SetFloat("_size", targetSize);
            mat.SetVector("_offset", new Vector2(wipePosition.x, Screen.height - wipePosition.y));
            wipeTimer -= Time.deltaTime;
            if (wipeTimer <= 0)
                break;
            yield return new WaitForEndOfFrame();
        }
        if (fadeout)
        {
            mat.SetFloat("_size", 0);
        }
        else
        {
            mat.SetFloat("_size", maxSize);
        }
        yield return null;
    }
}
