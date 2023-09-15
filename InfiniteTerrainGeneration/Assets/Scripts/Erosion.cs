using System;
using Unity.Collections;
using UnityEngine;

public static class Erosion {
    
    public enum Type{Thermal, Water};    
    
    public static float ThermalErosionValue(int x, int y, int mapChunkSize, ErosionSettings erosionSettings, NativeArray<float> heightMap, float iterFraction)
    {
        float erodedValue = 0.0f;
        
        int index = y * mapChunkSize + x;
        
        // Verifica si el punto está dentro del área del borde
        bool isInsideBorder = CheckIsInsideBorder(x, y, erosionSettings.borderSize, mapChunkSize);
        
        float currentHeight = heightMap[index];
        float minHeight = currentHeight;

        // Bucle para buscar el vecino más bajo
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

        // Calcula el ángulo de talud y aplica la erosión si es necesario
        float heightDiff = currentHeight - minHeight;
        float angle = Mathf.Atan(heightDiff);
        float borderMaxReduction = erosionSettings.borderMaxReduction;
        float currentBorderReduction = borderMaxReduction * iterFraction;
        
        if (angle > erosionSettings.thermalSettings.talusAngle)
        {
            if (!isInsideBorder)
            {
                // Si está en el borde, no aplicar erosión, devolver la altura disminuída según la iteración actual
                erodedValue = currentHeight - currentBorderReduction;
            }
            else
            {
                float sediments = (angle - erosionSettings.thermalSettings.talusAngle) * 0.5f;
                erodedValue = currentHeight - sediments;
            }
        }
        else
        {
            erodedValue = currentHeight;
        }

        return erodedValue;
    }

    private static bool CheckIsInsideBorder(int x, int y, int borderSize, int mapChunkSize)
    {
        return x >= borderSize && 
               x < mapChunkSize - borderSize &&
               y >= borderSize && 
               y < mapChunkSize - borderSize;
    }

    public static void WaterErosionValue(int x, int y, int mapChunkSize, ErosionSettings erosionSettings, NativeArray<float> heightMap) {
        int mapSize = heightMap.Length;
        int mapWidth = Mathf.RoundToInt(Mathf.Sqrt(mapSize));

        // Parámetros de erosión (ajusta según tus necesidades)
        float erosionRate = 0.01f;
        float sedimentCapacityFactor = 3.0f;
        float minSedimentCapacity = 0.01f;

        // Bucle principal para cada celda del mapa
        for (y = 0; y < mapWidth; y++) {
            for (x = 0; x < mapWidth; x++) {
                int index = y * mapWidth + x;

                // Calcula la cantidad de agua en esta celda
                float water = 0.2f;

                // Calcula la sedimentación máxima que puede llevar esta cantidad de agua
                float sedimentCapacity = Mathf.Max((water - minSedimentCapacity) * sedimentCapacityFactor, 0.0f);

                // Calcula la diferencia de altura entre la celda actual y sus vecinos
                float totalHeightDiff = 0.0f;

                for (int dy = -1; dy <= 1; dy++) {
                    for (int dx = -1; dx <= 1; dx++) {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapWidth) {
                            int neighborIndex = ny * mapWidth + nx;
                            float neighborHeight = heightMap[neighborIndex];
                            float currentHeight = heightMap[index];
                            totalHeightDiff += currentHeight - neighborHeight;
                        }
                    }
                }

                // Calcula la cantidad de sedimentos que se transportarán
                float sedimentToTransport = Mathf.Max(totalHeightDiff * erosionRate, 0.0f);

                // Actualiza la cantidad de agua y sedimentos en esta celda
                // waterMapData.waterMap[index] -= sedimentToTransport;
                // waterMapData.waterMap[index] += sedimentToTransport;
            }
        }
    }
    
    public struct WaterMapData {
        public float[] waterMap;
        public float waterAmount;
        public float evaporation;
    }

    
}
