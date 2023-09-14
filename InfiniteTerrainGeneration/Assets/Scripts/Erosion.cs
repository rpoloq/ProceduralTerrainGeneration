using System;
using Unity.Collections;
using UnityEngine;

public static class Erosion {
    
    public enum ErosionType{Thermal, Water};    
    
    public static void ApplyThermalErosion(ref NativeArray<float> heightMap, float talusAngle, int iterations) {
        int mapSize = heightMap.Length;
        int mapWidth = Mathf.RoundToInt(Mathf.Sqrt(mapSize));
        
        // Se el ángulo de talud a radianes pasa a radianes
        talusAngle *= (Mathf.PI / 180.0f);
        
        // Bucle principal para cada iteración
        for (int iteration = 0; iteration < iterations; iteration++) {
            for (int y = 0; y < mapWidth; y++) {
                for (int x = 0; x < mapWidth; x++) {
                    int index = y * mapWidth + x;

                    // Calcula la diferencia de altura con los vecinos
                    float currentHeight = heightMap[index];
                    float minHeight = currentHeight;
                    int minNeighborIndex = index;

                    for (int dy = -1; dy <= 1; dy++) {
                        for (int dx = -1; dx <= 1; dx++) {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapWidth) {
                                int neighborIndex = ny * mapWidth + nx;
                                float neighborHeight = heightMap[neighborIndex];

                                if (neighborHeight < minHeight) {
                                    minHeight = neighborHeight;
                                    minNeighborIndex = neighborIndex;
                                }
                            }
                        }
                    }

                    // Calcula el ángulo de talud
                    float heightDiff = currentHeight - minHeight;
                    float angle = Mathf.Atan(heightDiff);

                    // Si el ángulo de talud es mayor que el ángulo de talud máximo (talusAngle), erosiona
                    if (angle > talusAngle) {
                        float sediments = (angle - talusAngle) * 0.5f;
                        heightMap[index] -= sediments;
                        heightMap[minNeighborIndex] += sediments;
                    }
                }
            }
        }
    }
    
    public static void ApplyWaterErosion(float[] heightMap, WaterMap waterMap) {
        int mapSize = heightMap.Length;
        int mapWidth = Mathf.RoundToInt(Mathf.Sqrt(mapSize));

        // Parámetros de erosión (ajusta según tus necesidades)
        float erosionRate = 0.01f;
        float sedimentCapacityFactor = 3.0f;
        float minSedimentCapacity = 0.01f;

        // Bucle principal para cada celda del mapa
        for (int y = 0; y < mapWidth; y++) {
            for (int x = 0; x < mapWidth; x++) {
                int index = y * mapWidth + x;

                // Calcula la cantidad de agua en esta celda
                float water = waterMap.waterMap[index];

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
                waterMap.waterMap[index] -= sedimentToTransport;
                waterMap.waterMap[index] += sedimentToTransport;
            }
        }
        

    }
    
    public class WaterMap {
        public float[] waterMap;
        public float waterAmount;
        public float evaporation;
    }

    
}
