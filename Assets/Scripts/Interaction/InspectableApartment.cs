using UnityEngine;
using System.Collections.Generic;

public class InspectableApartment : MonoBehaviour
{
    [Header("Apartment Info")]
    public string apartmentID = "Unit 101";
    public string apartmentName = "Scandinavian Living & Dining";
    public string floorNumber = "1st Floor";
    public string apartmentType = "2-Bedroom Luxury Suite";
    public string areaSqFt = "1,150 sq ft";
    public string priceOrRent = "$2,400 / mo";
    [TextArea(2, 4)]
    public string features = "Open Kitchen • Hardwood Oak Floor • Balcony Access • Panoramic Windows";

    [Header("Camera Anchors")]
    public Transform inspectionCameraAnchor; // Where the camera initially moves to
    public Transform inspectionTargetAnchor; // Center pivot to orbit around
    public float defaultOrbitDistance = 7f;
    public float minOrbitDistance = 2.5f;
    public float maxOrbitDistance = 14f;

    [Header("Visual Highlighting")]
    public List<Renderer> highlightRenderers = new List<Renderer>();
    public Color highlightEmissionColor = new Color(0.2f, 0.7f, 1f, 1f);
    public GameObject interiorLightingGroup;
    public Transform playerSpawnPoint; // Nearby teleport/spawn spot

    private MaterialPropertyBlock propBlock;
    private bool isHovered = false;
    private float pulseTimer = 0f;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (isHovered)
        {
            pulseTimer += Time.deltaTime * 3.5f;
            float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
            Color currentPulseColor = highlightEmissionColor * (0.3f + pulse * 0.7f);

            foreach (var rend in highlightRenderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_EmissionColor", currentPulseColor);
                    rend.SetPropertyBlock(propBlock);
                }
            }
        }
    }

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;
        isHovered = hovered;
        pulseTimer = 0f;

        if (!isHovered)
        {
            foreach (var rend in highlightRenderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_EmissionColor", Color.black);
                    rend.SetPropertyBlock(propBlock);
                }
            }
        }
    }

    public void SetInteriorLightsActive(bool active)
    {
        if (interiorLightingGroup != null)
        {
            interiorLightingGroup.SetActive(active);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (inspectionTargetAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(inspectionTargetAnchor.position, 0.5f);
        }
        if (inspectionCameraAnchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(inspectionCameraAnchor.position, 0.3f);
            if (inspectionTargetAnchor != null)
            {
                Gizmos.DrawLine(inspectionCameraAnchor.position, inspectionTargetAnchor.position);
            }
        }
    }
}
