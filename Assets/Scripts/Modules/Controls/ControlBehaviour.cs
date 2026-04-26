using UnityEngine;

public abstract class ControlBehaviour : MonoBehaviour
{
    protected Ship ship;

    protected virtual void Start()
    {
        ship = GetComponent<Ship>();
    }

    public Vector3 ClampPositionToCameraBounds(Vector3 targetPosition)
    {
        Camera cam = Camera.main;

        float screenHeight = cam.orthographicSize;
        float screenWidth = screenHeight * cam.aspect;

        Vector3 cameraCenter = cam.transform.position;
        
        float clampedX = Mathf.Clamp(targetPosition.x, cameraCenter.x - screenWidth, cameraCenter.x + screenWidth);
        float clampedY = Mathf.Clamp(targetPosition.y, cameraCenter.y - screenHeight, cameraCenter.y + screenHeight);
        
        return new Vector3(clampedX, clampedY, targetPosition.z);
    }
}
