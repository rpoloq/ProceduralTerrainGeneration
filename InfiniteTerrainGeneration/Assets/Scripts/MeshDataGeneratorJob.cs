using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct MeshDataGeneratorJob : IJobParallelFor
{
    [ReadOnly] private int _size;
    [ReadOnly] private MeshSettings _meshSettings;
    [ReadOnly] private NativeArray<float> _heightMap;
    [NativeDisableParallelForRestriction] private NativeArray<Vector3> _vertices;
    [NativeDisableParallelForRestriction] private NativeArray<Vector2> _uvs;
    [NativeDisableParallelForRestriction] private NativeArray<int> _triangles;

    public MeshDataGeneratorJob(int size, MeshSettings meshSettings, NativeArray<float> heightMap, NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<int> triangles)
    {
        _size = size;
        _meshSettings = meshSettings;
        _heightMap = heightMap;
        _vertices = vertices;
        _uvs = uvs;
        _triangles = triangles;
    }

    public void Execute(int index)
    {
        int y = index / _size;
        int x = index % _size;
        int vertexIndex = y * _size + x;

        float height = _heightMap[vertexIndex];
        // float curveHeight = meshHeightCurve.Evaluate(height) * meshHeightMultiplier;

        _vertices[index] = new Vector3(x - _size * 0.5f, height * _meshSettings.meshHeightMultiplier, _size * 0.5f - y);
        _uvs[index] = new Vector2(x / (float)_size, y / (float)_size);

        if (x < (_size - 1) && y < (_size - 2))
        {
            int a = vertexIndex;
            int b = a + 1;
            int c = (y + 1) * _size + x;
            int d = c + 1;
            
            _triangles[index * 6] = a;
            _triangles[index * 6 + 1] = b;
            _triangles[index * 6 + 2] = c;
            _triangles[index * 6 + 3] = b;
            _triangles[index * 6 + 4] = d;
            _triangles[index * 6 + 5] = c;
        }
    }

    // public MeshData ReturnMeshData() => _meshData;

}