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

    public ErosionJob(NativeArray<float> heightMap, NativeArray<float> erodedHeightMap, ErosionSettings erosionSettings, int mapChunkSize)
    {
        _heightMap = heightMap;
        _erodedHeightMap = erodedHeightMap;
        _mapChunkSize = mapChunkSize;
        _erosionSettings = erosionSettings;
    }

    public void Execute(int threadIndex)
    {
        int x = threadIndex % _mapChunkSize;
        int y = threadIndex / _mapChunkSize;
        int index = y * _mapChunkSize + x;

        float currentHeight = _heightMap[index];
        float minHeight = currentHeight;

        // Bucle para buscar el vecino más bajo
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

                    if (neighborHeight < minHeight)
                    {
                        minHeight = neighborHeight;
                    }
                }
            }
        }

        // Calcula el ángulo de talud y aplica la erosión si es necesario
        float heightDiff = currentHeight - minHeight;
        float angle = Mathf.Atan(heightDiff);

        if (angle > _erosionSettings.talusAngle)
        {
            float sediments = (angle - _erosionSettings.talusAngle) * 0.5f;
            _erodedHeightMap[index] = currentHeight - sediments;
        }
        else
        {
            _erodedHeightMap[index] = currentHeight;
        }
    }
}
