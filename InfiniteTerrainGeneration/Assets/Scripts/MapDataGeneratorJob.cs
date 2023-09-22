using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MapDataGeneratorJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction] private NativeArray<float> _heightMap;
    public NativeArray<float> HeightMap
    {
        get { return _heightMap; }
        set { _heightMap = value; }
    }
    
    [NativeDisableParallelForRestriction] private NativeArray<Color> _colMap;
    private readonly HeightMapSettings _heightMapSettings;
    private readonly int _mapChunkSize;
    private readonly float2 _centre;
    [NativeDisableParallelForRestriction] private NativeArray<Color> _colorGradient;
    public MapDataGeneratorJob(HeightMapSettings heightMapSettings, int mapChunkSize, float2 centre, NativeArray<Color> colorGradient)
    {
        _heightMapSettings = heightMapSettings;
        _mapChunkSize = mapChunkSize;
        _centre = centre;
        _colMap = new NativeArray<Color>(mapChunkSize * mapChunkSize, Allocator.TempJob);
        _heightMap = new NativeArray<float>(mapChunkSize * mapChunkSize, Allocator.TempJob);
        _colorGradient = colorGradient;
    }

    public void Execute(int threadIndex)
    {
        int x = threadIndex % _mapChunkSize;
        int y = threadIndex / _mapChunkSize;
        float2 pos = new float2(x, -y);

        float height = Noise.GenerateNoiseValue(_centre + pos, _heightMapSettings);

        _colMap[threadIndex] = _colorGradient[Mathf.Clamp(Mathf.Abs(Mathf.RoundToInt(height * 100)), 0, 99)];

        _heightMap[threadIndex] = height;
    }

    public MapData ReturnMapData() => new MapData(_heightMap, _colMap);

    public void Dispose()
    {
        _colMap.Dispose();
        _heightMap.Dispose();
    }
} 