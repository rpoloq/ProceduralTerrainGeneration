using System;
using Unity.Collections;
using UnityEngine;

public static class Erosion {
    
    public static float ThermalErosionValue(int x, int y, int mapChunkSize, ErosionSettings erosionSettings, NativeArray<float> heightMap, float iterFraction)
    {
        float erodedValue = 0.0f;
        
        int index = y * mapChunkSize + x;
        
        bool isInsideBorder = InsideBorder(x, y, erosionSettings.borderSize, mapChunkSize);
        
        float currentHeight = heightMap[index];
        float minHeight = currentHeight;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < mapChunkSize && ny >= 0 && ny < mapChunkSize)
                {
                    int neighborIndex = ny * mapChunkSize + nx;
                    float neighborHeight = heightMap[neighborIndex];

                    if (neighborHeight < minHeight)
                    {
                        minHeight = neighborHeight;
                    }
                }
            }
        }

        float heightDiff = currentHeight - minHeight;
        float angle = Mathf.Atan(heightDiff);
        float borderMaxReduction = erosionSettings.borderMaxReduction;
        float currentBorderReduction = borderMaxReduction * iterFraction;
        
        if (angle > erosionSettings.talusAngle)
        {
            if (!isInsideBorder)
            {
                erodedValue = currentHeight - currentBorderReduction;
            }
            else
            {
                float sediments = (angle - erosionSettings.talusAngle) * 0.5f;
                erodedValue = currentHeight - sediments;
            }
        }
        else
        {
            erodedValue = currentHeight;
        }

        return erodedValue;
    }

    private static bool InsideBorder(int x, int y, int borderSize, int mapChunkSize)
    {
        return x >= borderSize && 
               x < mapChunkSize - borderSize &&
               y >= borderSize && 
               y < mapChunkSize - borderSize;
    }
}
