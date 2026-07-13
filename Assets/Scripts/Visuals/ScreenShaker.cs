using UnityEngine;
using System.Collections;

public class ScreenShaker : MonoBehaviour
{
    public static ScreenShaker Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;

    private Vector3 originalPosition;
    private float remainingTime = 0f;
    private float currentMagnitude = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        originalPosition = cameraTransform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        remainingTime += duration;
        currentMagnitude = Mathf.Max(currentMagnitude, magnitude);
    }

    private void Update()
    {
        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            Vector3 offset = Random.insideUnitSphere * currentMagnitude;
            offset.z = 0f;

            cameraTransform.localPosition = originalPosition + offset;
        }
        else
        {
            remainingTime = 0f;
            currentMagnitude = 0f;
            cameraTransform.localPosition = originalPosition;
        }
    }
}