using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] // [중요] 렌더러 필수
public class HighPolySphere : MonoBehaviour
{
    [Range(2, 6)] public int subdivisions = 4;
    [SerializeField] public float radius = 1f;

    void Start()
    {
        CreateHighPolySphere();
    }

    void CreateHighPolySphere()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        // [안전장치] 재질이 없으면 기본 재질 할당
        if (renderer.sharedMaterial == null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
        }

        Mesh mesh = new Mesh();
        mesh.name = "HighPolySphere";

        // 임시 구체 생성해서 데이터 복사
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh originalMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;

        mesh.vertices = originalMesh.vertices;
        mesh.triangles = originalMesh.triangles;
        mesh.uv = originalMesh.uv;
        mesh.normals = originalMesh.normals;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        filter.mesh = mesh;
        Destroy(tempSphere);
    }
}