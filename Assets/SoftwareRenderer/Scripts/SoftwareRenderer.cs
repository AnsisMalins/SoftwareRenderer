using System.Collections.Generic;
using UnityEngine;

public sealed class SoftwareRenderer : MonoBehaviour
{
    public Color color = Color.white;
    public Mesh mesh;

    internal float radius;
    internal static List<SoftwareRenderer> renderers;
    internal int[] triangles;
    internal Vector3[] vertices;

    static SoftwareRenderer()
    {
        renderers = new List<SoftwareRenderer>();
    }

    private void Reset()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
            mesh = meshFilter.sharedMesh;
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            color = renderer.sharedMaterial.color;
    }

    private void Start()
    {
        // These properties allocate and return a new array every time they're accessed, so we must
        // cache them.
        triangles = mesh.triangles;
        vertices = mesh.vertices;

        radius = GetRadius(vertices, transform.lossyScale);
    }

    private void OnEnable()
    {
        renderers.Add(this);
    }

    private void OnDisable()
    {
        renderers.Remove(this);
    }

    internal Color gizmoColor;
    private void OnDrawGizmosSelected()
    {
        if (mesh == null)
            return;

        Gizmos.color = Application.isPlaying ? gizmoColor : color;
        Gizmos.DrawWireSphere(transform.position, GetRadius(mesh.vertices, transform.lossyScale));
    }

    // Bounding sphere centered on the local origin. Very loose fit, but simple and fast.
    private static float GetRadius(Vector3[] vertices, Vector3 s)
    {
        float sqrRadius = 0;
        for (int i = 0; i < vertices.Length; i++)
            sqrRadius = Mathf.Max(sqrRadius, vertices[i].sqrMagnitude);
        return Mathf.Sqrt(sqrRadius) * Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }

    private static float Max(float a, float b, float c)
    {
        return a > b ? a > c ? a : c : b > c ? b : c;
    }
}