using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionConeMesh : MonoBehaviour
{
    public float viewAngle = 60f;
    public float viewDistance = 15f;
    public int resolution = 30;

    private Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateCone();
    }

    void CreateCone()
    {
        int vertexCount = resolution + 1;

        Vector3[] vertices = new Vector3[vertexCount + 1];
        int[] triangles = new int[resolution * 3];

        vertices[0] = Vector3.zero;

        float angleStep = viewAngle / resolution;

        for (int i = 0; i <= resolution; i++)
        {
            float angle = -viewAngle / 2 + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            vertices[i + 1] = dir * viewDistance;
        }

        int triIndex = 0;

        for (int i = 0; i < resolution; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}