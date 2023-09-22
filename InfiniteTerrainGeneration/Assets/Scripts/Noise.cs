using UnityEngine;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public static class Noise {

	public enum Type {Perlin, Simplex, Voronoi};
	
	
	public static float[] GenerateNoiseMap(int mapSize, HeightMapSettings parameters, float2 centre) {
	    float[] noiseMap = new float[mapSize * mapSize];
		
	    System.Random prng = new System.Random((int)parameters.seed);
	    Vector2[] octaveOffsets = new Vector2[parameters.octaves];

	    float maxPossibleHeight = 0;
	    float amplitude = 1;
	    float frequency = 1;

	    for (int i = 0; i < parameters.octaves; i++) {
	        float offsetX = prng.Next(-100000, 100000) + (centre.x + parameters.offset.x);
	        float offsetY = prng.Next(-100000, 100000) - (centre.y + parameters.offset.y);
	        octaveOffsets[i] = new Vector2(offsetX, offsetY);

	        maxPossibleHeight += amplitude;
	        amplitude *= parameters.persistance;
	    }

	    if (parameters.noiseScale <= 0) {
	        parameters.noiseScale = 0.0001f;
	    }

	    float maxLocalNoiseHeight = float.MinValue;
	    float minLocalNoiseHeight = float.MaxValue;

	    float halfWidth = mapSize / 2f;
	    float halfHeight = mapSize / 2f;

	    for (int y = 0; y < mapSize; y++) {
	        for (int x = 0; x < mapSize; x++) {

	            amplitude = 1;
	            frequency = 1;
	            float noiseHeight = 0;

	            for (int i = 0; i < parameters.octaves; i++) {
	                float sampleX = (x - halfWidth + octaveOffsets[i].x) / parameters.noiseScale * frequency;
	                float sampleY = (y - halfHeight + octaveOffsets[i].y) / parameters.noiseScale * frequency;

	                float perlinValue = SampleNoiseValue(new float2(sampleX, sampleY), parameters.noiseType);
	                
	                noiseHeight += perlinValue * amplitude;

	                amplitude *= parameters.persistance;
	                frequency *= parameters.lacunarity;
	            }

	            if (noiseHeight > maxLocalNoiseHeight) {
	                maxLocalNoiseHeight = noiseHeight;
	            } else if (noiseHeight < minLocalNoiseHeight) {
	                minLocalNoiseHeight = noiseHeight;
	            }
	            noiseMap[y * mapSize + x] = noiseHeight;
	        }
	    }

	    for (int y = 0; y < mapSize; y++) {
	        for (int x = 0; x < mapSize; x++) {
	            float normalizedHeight = (noiseMap[y * mapSize + x] + 1) / (maxPossibleHeight / 0.9f);
	            noiseMap[y * mapSize + x] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
	            
	        }
	    }

	    return noiseMap;
	}
	public static float GenerateNoiseValue(float2 position, HeightMapSettings parameters)
	{
		var random = new Unity.Mathematics.Random(parameters.seed);
		NativeArray<Vector2> octaveOffsets = new NativeArray<Vector2>(parameters.octaves, Allocator.Temp);

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < parameters.octaves; i++)
		{
			float offsetX = random.NextFloat(-100000, 100000) + parameters.offset.x;
			float offsetY = random.NextFloat(-100000, 100000) - parameters.offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= parameters.persistance;
		}

		if (parameters.noiseScale <= 0)
		{
			parameters.noiseScale = 0.0001f;
		}

		float noiseHeight = 0;
		amplitude = 1;
		
		for (int i = 0; i < parameters.octaves; i++)
		{
			float sampleX = (position.x + octaveOffsets[i].x) / parameters.noiseScale * frequency;
			float sampleY = (position.y + octaveOffsets[i].y) / parameters.noiseScale * frequency;

			float perlinValue = SampleNoiseValue(new float2(sampleX, sampleY), parameters.noiseType);
			noiseHeight += perlinValue * amplitude;

			amplitude *= parameters.persistance;
			frequency *= parameters.lacunarity;
		}

		float normalizedHeight = (noiseHeight + 1) / (maxPossibleHeight/0.9f);
		noiseHeight = Mathf.Clamp(normalizedHeight,0, maxPossibleHeight);

		octaveOffsets.Dispose();
		
		return noiseHeight;
	}

	private static float SampleNoiseValue(float2 sample, Type type)
	{
		float noiseValue = 0.0f;
		switch (type)
		{
			case Type.Perlin:
				noiseValue = Mathf.PerlinNoise(sample.x, sample.y) * 2 - 1;
				break;
			case Type.Simplex:
				noiseValue = noise.snoise(sample) * 2 - 1;
				break;
			case Type.Voronoi:
				float2 cellularResult = noise.cellular(sample);
				float distanceToClosest = math.sqrt(cellularResult.x * cellularResult.x + cellularResult.y * cellularResult.y);
				noiseValue = distanceToClosest/1.45f - 0.1f;
				break;
		}

		return noiseValue;
	}

}
