using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumetricData : MonoBehaviour
{

    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject volumetricData;
    [SerializeField] private float slicingDistanceThreshold = 5.0f;

    private void Update()
    {
        if (Vector3.Distance(mainCamera.transform.position, volumetricData.transform.position) < slicingDistanceThreshold)
        {
            Plane slicingPlane = new Plane(mainCamera.transform.forward, volumetricData.transform.position);
            ApplySlicingEffect(slicingPlane);
        }
    }

    private void ApplySlicingEffect(Plane slicingPlane)
    {
        // Access the material of the volumetric data GameObject
        Renderer renderer = volumetricData.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("VolumetricData: No Renderer component found on volumetricData");
            return;
        }

        Material volumetricMaterial = renderer.material;

        // Set the slicing plane as a shader property
        volumetricMaterial.SetVector("_SlicingPlane", new Vector4(slicingPlane.normal.x, slicingPlane.normal.y, slicingPlane.normal.z, slicingPlane.distance));
    }
}
