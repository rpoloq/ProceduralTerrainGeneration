using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct ErosionJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<float> _heightMap;
    [NativeDisableParallelForRestriction] private NativeArray<float> _erodedHeightMap;
    private readonly int _mapChunkSize;
    private readonly ErosionSettings _erosionSettings;
    private readonly float _iterFraction;

    public ErosionJob(NativeArray<float> heightMap, ErosionSettings erosionSettings, int mapChunkSize, float iterFraction)
    {
        _erosionSettings = erosionSettings;
        _heightMap = heightMap;
        _erodedHeightMap = new NativeArray<float>(heightMap, Allocator.TempJob);
        _mapChunkSize = mapChunkSize;
        _iterFraction = iterFraction;
    }

    public void Execute(int threadIndex)
    {
        int x = threadIndex % _mapChunkSize;
        int y = threadIndex / _mapChunkSize;
        int index = y * _mapChunkSize + x;
        
        _erodedHeightMap[index] = Erosion.ThermalErosionValue(x, y, _mapChunkSize, _erosionSettings, _heightMap, _iterFraction);
    }

    public NativeArray<float> GetErodedHeightMap() => _erodedHeightMap;

    public void Dispose()
    {
        _erodedHeightMap.Dispose();
    }

}