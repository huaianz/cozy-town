using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("平移")]
    [SerializeField] private bool enableDrag = false;

    [Header("缩放")]
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 40f;
    [SerializeField] private float wheelZoomSpeed = 1.5f;
    [SerializeField] private float pinchZoomSpeed = 0.006f;

    [Header("农场边界")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-12.5f, -6.5f);
    [SerializeField] private Vector2 boundsMax = new Vector2(11.5f, 5.5f);

    private Camera cam;
    private Vector2 lastPinchMidpoint;
    private float lastPinchDistance;
    private bool pinching;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindObjectOfType<GridMapMangaer>() == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        if (mainCamera.GetComponent<CameraController>() != null)
        {
            return;
        }

        mainCamera.gameObject.AddComponent<CameraController>();
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (cam == null)
        {
            return;
        }

        if (Input.touchCount >= 2)
        {
            HandlePinch();
        }
        else
        {
            HandleDrag();
            HandleWheelZoom();
        }

        ClampPosition();
    }

    private void HandleDrag()
    {
        if (!enableDrag || !PointerInput.IsPressed)
        {
            return;
        }

        Vector2 delta = PointerInput.Delta;

        if (delta == Vector2.zero)
        {
            return;
        }

        float worldPerPixel = (cam.orthographicSize * 2f) / Screen.height;
        transform.position -= new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0f);
    }

    private void HandleWheelZoom()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        Zoom(scroll * wheelZoomSpeed);
    }

    private void HandlePinch()
    {
        Touch first = Input.GetTouch(0);
        Touch second = Input.GetTouch(1);

        Vector2 midpoint = (first.position + second.position) * 0.5f;
        float distance = Vector2.Distance(first.position, second.position);

        if (!pinching || first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
        {
            pinching = true;
            lastPinchMidpoint = midpoint;
            lastPinchDistance = distance;
            return;
        }

        Zoom((lastPinchDistance - distance) * pinchZoomSpeed);

        Vector2 panDelta = midpoint - lastPinchMidpoint;
        float worldPerPixel = (cam.orthographicSize * 2f) / Screen.height;
        transform.position -= new Vector3(panDelta.x * worldPerPixel, panDelta.y * worldPerPixel, 0f);

        lastPinchMidpoint = midpoint;
        lastPinchDistance = distance;
    }

    private void Zoom(float amount)
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - amount, minZoom, maxZoom);
    }

    private void ClampPosition()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = boundsMin.x + halfWidth;
        float maxX = boundsMax.x - halfWidth;
        float minY = boundsMin.y + halfHeight;
        float maxY = boundsMax.y - halfHeight;

        Vector3 position = transform.position;

        if (minX > maxX)
        {
            position.x = (boundsMin.x + boundsMax.x) * 0.5f;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minY > maxY)
        {
            position.y = (boundsMin.y + boundsMax.y) * 0.5f;
        }
        else
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        transform.position = position;
    }

    public void SetArea(Vector2 center, Vector2 size)
    {
        boundsMin = center - size * 0.5f;
        boundsMax = center + size * 0.5f;

        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        if (cam != null)
        {
            float aspect = cam.aspect > 0.1f ? cam.aspect : 1.7f;
            float fitHeight = size.y * 0.5f;
            float fitWidth = size.x * 0.5f / aspect;
            float fit = Mathf.Max(fitHeight, fitWidth) * 1.08f;

            cam.orthographicSize = Mathf.Clamp(fit, minZoom, maxZoom);
        }

        Focus(center);
    }

    public void Focus(Vector2 center)
    {
        Vector3 position = transform.position;
        position.x = center.x;
        position.y = center.y;
        transform.position = position;

        ClampPosition();
    }

    private void LateUpdate()
    {
        if (pinching && Input.touchCount < 2)
        {
            pinching = false;
        }
    }
}