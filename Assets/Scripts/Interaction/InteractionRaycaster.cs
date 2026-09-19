using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float maxInteractionDistance = 9f;
    public LayerMask interactableMask = ~0;
    public Camera playerCamera;

    private InspectableApartment currentTarget = null;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.FreeRoam)
        {
            ClearTarget();
            return;
        }

        CheckForInteractable();

        if (currentTarget != null && InputBridge.GetKeyDown(KeyCode.E))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InspectApartment(currentTarget);
            }
        }
    }

    private void CheckForInteractable()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        InspectableApartment foundApartment = null;

        // Optimized single raycast
        if (Physics.Raycast(ray, out hit, maxInteractionDistance, interactableMask, QueryTriggerInteraction.Collide))
        {
            foundApartment = hit.collider.GetComponentInParent<InspectableApartment>();
        }

        if (foundApartment != currentTarget)
        {
            if (currentTarget != null)
            {
                currentTarget.SetHovered(false);
            }

            currentTarget = foundApartment;

            if (currentTarget != null)
            {
                currentTarget.SetHovered(true);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowInteractionPrompt(true, $"[E] Inspect {currentTarget.apartmentID} • {currentTarget.apartmentName}");
                }
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowInteractionPrompt(false, "");
                }
            }
        }
    }

    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.SetHovered(false);
            currentTarget = null;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInteractionPrompt(false, "");
        }
    }
}
