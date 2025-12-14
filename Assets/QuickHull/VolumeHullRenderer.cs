using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;


namespace GK {
    public class VolumeHullRenderer : MonoBehaviour
    {
        public static VolumeHullRenderer Instance { get; private set;}
        public event EventHandler<OnMeasurementEventArgs> OnMeasurementEvent;
        public class OnMeasurementEventArgs : EventArgs
        {
            public float measurementValue;
        }

        [SerializeField] private GameObject RockPrefab;
        private ConvexHullCalculator calc = new();
        private List<Vector3> verts = new ();
        private  List<int> tris = new();
        private  List<Vector3> normals = new();
        private List<Vector3> points = new();

        private void Awake() {
            if (Instance == null) {
                Instance = this;	
            } else {
                Debug.Log("Duplicated Volume Hull Renderer");
            }
	    }

        private void Start() {
            InputActionsManager.Instance.InputActions.XRILeftHand.MenuButton.performed += MenuButton_Performed;
        }

        private void MenuButton_Performed(InputAction.CallbackContext context)
        {
            if (context.interaction is not HoldInteraction) {
                return;
            } 

            if (ToolsPanelUI.Instance.GetMode() != ToolsPanelUI.Modes.Measure ||
                MeasurementToolsUI.Instance.GetMeasurementTypes() != MeasurementToolsUI.MeasurementTypes.Volume ||
                MeasurementManager.Instance.GetCurrentVolumeMeasurementMethod() != MeasurementManager.VolumeMeasurementMethods.Sphere)
            {
                return;
            }

            if (points.Count >= 4) {
                return;
            }	

            Mesh mesh = GenerateHull();

            float volumeMeasurement = VolumeOfMesh(mesh);

            Debug.Log("Volume measurement: " + volumeMeasurement.ToString("F2"));

            OnMeasurementEvent?.Invoke(this, new OnMeasurementEventArgs {
                measurementValue = volumeMeasurement
		    });
        }

        float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float v321 = p3.x * p2.y * p1.z;
            float v231 = p2.x * p3.y * p1.z;
            float v312 = p3.x * p1.y * p2.z;
            float v132 = p1.x * p3.y * p2.z;
            float v213 = p2.x * p1.y * p3.z;
            float v123 = p1.x * p2.y * p3.z;
            return 1.0f / 6.0f * (-v321 + v231 + v312 - v132 - v213 + v123);
        }

        float VolumeOfMesh(Mesh mesh)
        {
            float volume = 0;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 p1 = vertices[triangles[i + 0]];
                Vector3 p2 = vertices[triangles[i + 1]];
                Vector3 p3 = vertices[triangles[i + 2]];
                volume += SignedVolumeOfTriangle(p1, p2, p3);
            }
            return Mathf.Abs(volume);
        }



        private Mesh GenerateHull() {
            GetPoints();

            calc.GenerateHull(points, true, ref verts, ref tris, ref normals);

            var rock = Instantiate(RockPrefab);

            rock.transform.SetParent(transform, false);
            rock.transform.localPosition = Vector3.zero;
            rock.transform.localRotation = Quaternion.identity;
            rock.transform.localScale = Vector3.one;

                        
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(normals);

            rock.GetComponent<MeshFilter>().sharedMesh = mesh;
            rock.GetComponent<MeshCollider>().sharedMesh = mesh;     

            return mesh;       
        }


        private void GetPoints()
        {
            //find curved points in children
            var spherePrefabArray = GetComponentsInChildren<SpherePrefab>();

            foreach (var spherePrefab in spherePrefabArray) {
                points.Add(spherePrefab.transform.position);
            }
        }
    }
}