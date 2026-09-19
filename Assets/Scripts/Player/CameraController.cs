using UnityEngine;

public enum CameraMode
{
    FreeRoam,
    TransitionToInspect,
    InspectApartment,
    TransitionToFreeRoam,
    Overview
}

public class CameraController : MonoBehaviour
{
    [Header("Target References")]
    public Transform playerBody;
    public Transform playerCameraHolder;

    [Header("Look Settings (Free Roam)")]
    public float mouseSensitivity = 2.2f;
    public float lookSmoothTime = 0.03f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Inspection Orbit Settings")]
    public float orbitSensitivity = 3.5f;
    public float zoomSensitivity = 3f;
    public float orbitDamping = 10f;
    public float transitionSpeed = 3.5f;

    [Header("Overview Drone Settings")]
    public Transform overviewAnchor;
    public float overviewRotateSpeed = 4f;

    public CameraMode CurrentMode { get; private set; } = CameraMode.FreeRoam;

    private float pitch = 0f;
    private float yaw = 0f;
    private float smoothPitch = 0f;
    private float smoothYaw = 0f;
    private float pitchVel = 0f;
    private float yawVel = 0f;

    private InspectableApartment currentApartment;
    private Vector3 inspectPivot;
    private float currentOrbitYaw = 0f;
    private float currentOrbitPitch = 20f;
    private float currentOrbitDist = 6f;
    private float targetOrbitYaw = 0f;
    private float targetOrbitPitch = 20f;
    private float targetOrbitDist = 6f;

    private Vector3 transitionStartPos;
    private Quaternion transitionStartRot;
    private float transitionT = 0f;

    private void Start()
    {
        if (playerBody != null)
        {
            yaw = playerBody.eulerAngles.y;
            smoothYaw = yaw;
        }
        LockCursor(false);
    }

    private void LateUpdate()
    {
        switch (CurrentMode)
        {
            case CameraMode.FreeRoam:
                UpdateFreeRoamCamera();
                break;
            case CameraMode.TransitionToInspect:
                UpdateTransitionToInspect();
                break;
            case CameraMode.InspectApartment:
                UpdateInspectCamera();
                break;
            case CameraMode.TransitionToFreeRoam:
                UpdateTransitionToFreeRoam();
                break;
            case CameraMode.Overview:
                UpdateOverviewCamera();
                break;
        }
    }

    private void UpdateFreeRoamCamera()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = InputBridge.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = InputBridge.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        smoothPitch = Mathf.SmoothDamp(smoothPitch, pitch, ref pitchVel, lookSmoothTime);
        smoothYaw = Mathf.SmoothDamp(smoothYaw, yaw, ref yawVel, lookSmoothTime);

        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
        }

        transform.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);

        if (playerCameraHolder != null)
        {
            transform.position = playerCameraHolder.position;
        }
    }

    public void StartInspect(InspectableApartment apartment)
    {
        currentApartment = apartment;
        if (apartment == null) return;

        transitionStartPos = transform.position;
        transitionStartRot = transform.rotation;
        transitionT = 0f;

        inspectPivot = apartment.inspectionTargetAnchor != null 
            ? apartment.inspectionTargetAnchor.position 
            : apartment.transform.position;

        Vector3 toAnchor = (apartment.inspectionCameraAnchor != null 
            ? apartment.inspectionCameraAnchor.position 
            : inspectPivot + Vector3.back * 6f + Vector3.up * 2f) - inspectPivot;

        currentOrbitDist = toAnchor.magnitude;
        targetOrbitDist = Mathf.Clamp(currentOrbitDist, apartment.minOrbitDistance, apartment.maxOrbitDistance);

        Quaternion lookRot = Quaternion.LookRotation(-toAnchor.normalized, Vector3.up);
        targetOrbitYaw = lookRot.eulerAngles.y;
        targetOrbitPitch = lookRot.eulerAngles.x;
        currentOrbitYaw = targetOrbitYaw;
        currentOrbitPitch = targetOrbitPitch;

        CurrentMode = CameraMode.TransitionToInspect;
        LockCursor(false);
    }

    private void UpdateTransitionToInspect()
    {
        transitionT += Time.deltaTime * transitionSpeed;
        float t = Mathf.SmoothStep(0f, 1f, transitionT);

        Vector3 targetPos = CalculateOrbitPosition(targetOrbitYaw, targetOrbitPitch, targetOrbitDist, inspectPivot);
        Quaternion targetRot = Quaternion.LookRotation((inspectPivot - targetPos).normalized, Vector3.up);

        transform.position = Vector3.Lerp(transitionStartPos, targetPos, t);
        transform.rotation = Quaternion.Slerp(transitionStartRot, targetRot, t);

        if (transitionT >= 1f)
        {
            CurrentMode = CameraMode.InspectApartment;
        }
    }

    private void UpdateInspectCamera()
    {
        if (currentApartment == null) return;

        if (InputBridge.GetMouseButton(0) || InputBridge.GetMouseButton(1))
        {
            float mouseX = InputBridge.GetAxis("Mouse X") * orbitSensitivity;
            float mouseY = InputBridge.GetAxis("Mouse Y") * orbitSensitivity;

            targetOrbitYaw += mouseX;
            targetOrbitPitch -= mouseY;
            targetOrbitPitch = Mathf.Clamp(targetOrbitPitch, -30f, 75f);
        }

        float scroll = InputBridge.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetOrbitDist -= scroll * zoomSensitivity * 4f;
            targetOrbitDist = Mathf.Clamp(targetOrbitDist, currentApartment.minOrbitDistance, currentApartment.maxOrbitDistance);
        }

        currentOrbitYaw = Mathf.Lerp(currentOrbitYaw, targetOrbitYaw, Time.deltaTime * orbitDamping);
        currentOrbitPitch = Mathf.Lerp(currentOrbitPitch, targetOrbitPitch, Time.deltaTime * orbitDamping);
        currentOrbitDist = Mathf.Lerp(currentOrbitDist, targetOrbitDist, Time.deltaTime * orbitDamping);

        Vector3 camPos = CalculateOrbitPosition(currentOrbitYaw, currentOrbitPitch, currentOrbitDist, inspectPivot);
        transform.position = camPos;
        transform.rotation = Quaternion.LookRotation((inspectPivot - camPos).normalized, Vector3.up);
    }

    public void ExitInspect()
    {
        transitionStartPos = transform.position;
        transitionStartRot = transform.rotation;
        transitionT = 0f;

        if (playerBody != null)
        {
            yaw = playerBody.eulerAngles.y;
            smoothYaw = yaw;
        }
        pitch = 0f;
        smoothPitch = 0f;

        CurrentMode = CameraMode.TransitionToFreeRoam;
    }

    private void UpdateTransitionToFreeRoam()
    {
        transitionT += Time.deltaTime * transitionSpeed;
        float t = Mathf.SmoothStep(0f, 1f, transitionT);

        Vector3 targetPos = playerCameraHolder != null ? playerCameraHolder.position : (playerBody != null ? playerBody.position + Vector3.up * 1.6f : Vector3.up * 1.6f);
        Quaternion targetRot = playerBody != null ? Quaternion.Euler(0f, playerBody.eulerAngles.y, 0f) : Quaternion.identity;

        transform.position = Vector3.Lerp(transitionStartPos, targetPos, t);
        transform.rotation = Quaternion.Slerp(transitionStartRot, targetRot, t);

        if (transitionT >= 1f)
        {
            CurrentMode = CameraMode.FreeRoam;
            LockCursor(true);
        }
    }

    public void SnapToOverview()
    {
        if (overviewAnchor != null)
        {
            transform.position = overviewAnchor.position;
            transform.rotation = overviewAnchor.rotation;
        }
        CurrentMode = CameraMode.Overview;
        LockCursor(false);
    }

    public void EnterOverview()
    {
        CurrentMode = CameraMode.Overview;
        LockCursor(false);
    }

    public void ExitOverview()
    {
        ExitInspect();
    }

    private void UpdateOverviewCamera()
    {
        if (overviewAnchor != null)
        {
            transform.position = overviewAnchor.position;
            transform.rotation = overviewAnchor.rotation;
            overviewAnchor.Rotate(Vector3.up, overviewRotateSpeed * Time.deltaTime, Space.World);
        }
    }

    private Vector3 CalculateOrbitPosition(float yAngle, float xAngle, float dist, Vector3 center)
    {
        Quaternion rot = Quaternion.Euler(xAngle, yAngle, 0f);
        Vector3 dir = rot * Vector3.back;
        return center + dir * dist;
    }

    public void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}