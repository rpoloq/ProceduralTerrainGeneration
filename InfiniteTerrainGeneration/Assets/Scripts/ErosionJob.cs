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

    public ErosionJob(NativeArray<float> heightMap, NativeArray<float> erodedHeightMap, ErosionSettings erosionSettings, int mapChunkSize, float iterFraction)
    {
        _heightMap = heightMap;
        _erodedHeightMap = erodedHeightMap;
        _mapChunkSize = mapChunkSize;
        _iterFraction = iterFraction;
        _erosionSettings = erosionSettings;
    }

    public void Execute(int threadIndex)
    {
        int x = threadIndex % _mapChunkSize;
        int y = threadIndex / _mapChunkSize;
        int index = y * _mapChunkSize + x;
        
        if (_erosionSettings.type == Erosion.Type.Thermal)
        {
            _erodedHeightMap[index] = Erosion.ThermalErosionValue(x, y, _mapChunkSize, _erosionSettings, _heightMap, _iterFraction);
        } else if (_erosionSettings.type == Erosion.Type.Water)
        {
            // WaterErosion(x, y, new Erosion.WaterMapData());
        }
    }

    
    
    
    private void WaterErosion(int x, int y, Erosion.WaterMapData waterMapData)
    {
        int index = y * _mapChunkSize + x;
        float water = waterMapData.waterMap[index];
        float sedimentCapacity = Mathf.Max((water - _erosionSettings.waterSettings.minSedimentCapacity) *
                                           _erosionSettings.waterSettings.sedimentCapacityFactor, 0.0f);
        float totalHeightDiff = 0.0f;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < _mapChunkSize && ny >= 0 && ny < _mapChunkSize)
                {
                    int neighborIndex = ny * _mapChunkSize + nx;
                    float neighborHeight = _heightMap[neighborIndex];
                    float currentHeight = _heightMap[index];
                    totalHeightDiff += currentHeight - neighborHeight;
                }
            }
        }

        float sedimentToTransport = Mathf.Max(totalHeightDiff * _erosionSettings.waterSettings.erosionRate, 0.0f);

        // Actualiza el _waterMap y _heightMap según corresponda
        waterMapData.waterMap[index] -= sedimentToTransport;
        // Actualiza el _heightMap según corresponda
    }
}
