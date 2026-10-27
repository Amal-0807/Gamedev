using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace BlockBlaster
{

    public sealed class PlaneVisual : MonoBehaviour
    {
        ARPlane plane;
        Mesh mesh;
        MeshRenderer fill;
        LineRenderer edge;

        void Awake()
        {
            plane = GetComponent<ARPlane>();
            var child = new GameObject("Detected table");
            child.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Table boundary" };
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            fill = child.AddComponent<MeshRenderer>();
            fill.sharedMaterial = Visuals.Material(new Color(0.20f, 0.94f, 0.91f, 0.12f));
            edge = child.AddComponent<LineRenderer>();
            edge.useWorldSpace = false;
            edge.loop = true;
            edge.widthMultiplier = 0.0025f;
            edge.sharedMaterial = Visuals.Material(Visuals.Cyan);
            plane.boundaryChanged += Changed;
            Rebuild();
        }

        public void SetVisible(bool visible) { fill.enabled = visible; edge.enabled = visible; }
        void Changed(ARPlaneBoundaryChangedEventArgs _) => Rebuild();
        void Rebuild()
        {
            int n = plane.boundary.Length;
            mesh.Clear();
            edge.positionCount = n;
            if (n < 3) return;
            var vertices = new Vector3[n];
            var triangles = new int[(n - 2) * 3];
            for (int i = 0; i < n; i++)
                vertices[i] = new Vector3(plane.boundary[i].x, 0.002f, plane.boundary[i].y);
            for (int i = 0; i < n - 2; i++)
            { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            edge.SetPositions(vertices);
        }
        void OnDestroy() { if (plane != null) plane.boundaryChanged -= Changed; if (mesh != null) Destroy(mesh); }
    }
}
