using UnityEngine;
using System.Collections;
using System.Linq;
using Unity.Collections;

public static class MeshGenerator {

	public static MeshData GenerateTerrainMesh(float[] heightMap, MeshSettings parameters, int size) {

		float topLeftX = (size - 1) / -2f;
		float topLeftZ = (size - 1) / 2f;

		int meshSimplificationIncrement = (parameters.editorPreviewLOD == 0) ? 1 : parameters.editorPreviewLOD * 2;
		int verticesPerLine = (size - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
		int vertexIndex = 0;

		for (int y = 0; y < size; y += meshSimplificationIncrement) {
			for (int x = 0; x < size; x += meshSimplificationIncrement)
			{
				int index = y * size + x;
				float height = heightMap[index] < parameters.waterLevel ?  parameters.waterLevel : heightMap[index];
				meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, height * parameters.meshHeightMultiplier, topLeftZ - y);
				meshData.uvs[vertexIndex] = new Vector2(x / (float)size, y / (float)size);

				if (x < size - 1 && y < size - 1) {
					meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
					meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
				}

				vertexIndex++;
			}
		}

		return meshData;
	}

}

public struct MeshData
{
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;
	public int triangleIndex;

	public MeshData(int meshWidth, int meshHeight)
	{
		triangleIndex = 0;
		vertices = new Vector3[meshWidth * meshHeight];
		uvs = new Vector2[meshWidth * meshHeight];
		triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
	}
	
	public MeshData(Vector3[] vertices, int[] triangles, Vector2[] uvs)
	{
		triangleIndex = 0;
		this.vertices = vertices;
		this.uvs = uvs;
		this.triangles = triangles;
	}

	public void AddTriangle(int a, int b, int c)
	{
		triangles[triangleIndex] = a;
		triangles[triangleIndex + 1] = b;
		triangles[triangleIndex + 2] = c;
		triangleIndex += 3;
	}

	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals();
		return mesh;
	}
}
