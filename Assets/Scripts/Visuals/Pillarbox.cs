using System;
using UnityEngine;
using UnityEngine.UIElements;

// Keeps the game within the phone shapes it supports, on every platform and in the editor. A screen from 9:16 to 9:21
// (width:height) is used whole. A wider one, such as a tablet or a desktop window, gets black bars at the sides, and a
// taller one gets them above and below. The main camera renders only into the game area, a camera behind it clears the
// bars, and UI roots passed to FitToArea are laid out inside the area.
public class Pillarbox : MonoBehaviour
{
    private const float WidestAspect = 9f / 16f; // width ÷ height
    private const float TallestAspect = 9f / 21f;
    private static readonly Rect FullScreen = new(0f, 0f, 1f, 1f);

    // The game area as a share of the screen, like Camera.rect: (0, 0) is the bottom left.
    public static Rect Area { get; private set; } = FullScreen;
    public static event Action AreaChanged;

    private Camera barsCamera, gameCamera;
    private int screenWidth, screenHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var host = new GameObject(nameof(Pillarbox));
        DontDestroyOnLoad(host);
        host.AddComponent<Pillarbox>();
    }

    private void Awake()
    {
        barsCamera = gameObject.AddComponent<Camera>();
        barsCamera.clearFlags = CameraClearFlags.SolidColor;
        barsCamera.backgroundColor = Color.black;
        barsCamera.cullingMask = 0;
        barsCamera.orthographic = true;
        barsCamera.useOcclusionCulling = false;
        barsCamera.allowHDR = false;
        barsCamera.allowMSAA = false;
        barsCamera.enabled = false;
    }

    private void Update()
    {
        Camera main = Camera.main;
        if (Screen.width == screenWidth && Screen.height == screenHeight && main == gameCamera)
            return;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        gameCamera = main;
        if (screenWidth <= 0 || screenHeight <= 0) // a minimised window
            return;

        Rect area = AreaFor(screenWidth / (float)screenHeight);
        if (gameCamera != null)
        {
            gameCamera.rect = area;
            barsCamera.depth = gameCamera.depth - 1;
        }
        // Without bars the game camera covers the screen, and the bars camera would only cost a render.
        barsCamera.enabled = area != FullScreen;

        if (area == Area)
            return;
        Area = area;
        AreaChanged?.Invoke();
    }

    private static Rect AreaFor(float aspect)
    {
        if (aspect > WidestAspect)
        {
            float width = WidestAspect / aspect;
            return new Rect((1f - width) / 2f, 0f, width, 1f);
        }
        if (aspect < TallestAspect)
        {
            float height = aspect / TallestAspect;
            return new Rect(0f, (1f - height) / 2f, 1f, height);
        }
        return FullScreen;
    }

    // Lays out a UIDocument's root inside the game area. Call it again on AreaChanged.
    public static void FitToArea(VisualElement root)
    {
        root.style.position = Position.Absolute;
        root.style.left = Length.Percent(Area.xMin * 100f);
        root.style.right = Length.Percent((1f - Area.xMax) * 100f);
        root.style.top = Length.Percent((1f - Area.yMax) * 100f);
        root.style.bottom = Length.Percent(Area.yMin * 100f);
    }
}
