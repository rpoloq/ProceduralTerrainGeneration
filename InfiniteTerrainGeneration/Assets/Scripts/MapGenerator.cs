using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class MapGenerator : MonoBehaviour {

	public const int MapChunkSize = 49;
	public ConfigSettings configSettings;
	public Gradient colorGradient;
	private int _batchSize = 32;


	public void DrawMapInEditor() {
		// MapData mapData = GenerateMapDataJob(Vector2.zero);
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (configSettings.editorPreviewSettings.drawMode == EditorPreviewSettings.DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap,MapChunkSize, MapChunkSize ));
		} else if (configSettings.editorPreviewSettings.drawMode == EditorPreviewSettings.DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colourMap, MapChunkSize, MapChunkSize));
		} else if (configSettings.editorPreviewSettings.drawMode == EditorPreviewSettings.DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, configSettings.meshSettings, MapChunkSize, configSettings.meshSettings.editorPreviewLOD), 
				TextureGenerator.TextureFromColourMap (mapData.colourMap, MapChunkSize, MapChunkSize));
		}
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback)
	{
		MapData mapData = GenerateMapDataJob (centre);
		callback(mapData);
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData;

		if (lod < 1)
		{
			meshData = GenerateMeshDataJob(mapData, configSettings.meshSettings, MapChunkSize,lod);
		}
		else
		{
			meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, configSettings.meshSettings, MapChunkSize, lod);
		}
		
		callback(meshData);
	}

	MapData GenerateMapData(Vector2 centre) {
		float[] noiseMap = Noise.GenerateNoiseMap (MapChunkSize, configSettings.heightMapSettings, new float2(centre.x, centre.y));

		if (configSettings.erosionSettings.activateErosion)
		{
			for (int i = 0; i < configSettings.erosionSettings.cicles; i++)
			{
				noiseMap = ThermalErosion(noiseMap, MapChunkSize, configSettings.erosionSettings,
					i / (float)configSettings.erosionSettings.cicles);
			}
		}

		// noiseMap[0] = 0;
		// noiseMap[MapChunkSize] = 1.5f;
		// noiseMap[MapChunkSize * (MapChunkSize - 1)] = 2.5f;
		// noiseMap[MapChunkSize * MapChunkSize - 1] = 3.5f;
		
		Color[] colourMap = new Color[MapChunkSize * MapChunkSize];

		
		for (int y = 0; y < MapChunkSize; y++)
		{
			for (int x = 0; x < MapChunkSize; x++)
			{
				int index = y * MapChunkSize + x;
				colourMap[index] = colorGradient.Evaluate(noiseMap[index]);
			}
		}

		return new MapData (noiseMap, colourMap);
	}
	
	public float[] ThermalErosion(float[] heightMap, int mapChunkSize, ErosionSettings erosionSettings, float iterFraction)
	{
		int size = mapChunkSize;

		float[]modifiedHeightMap = new float[heightMap.Length];

		for (int index = 0; index < size * size; index++)
		{
			int x = index % size;
			int y = index / size;

			bool isInsideBorder = InsideBorder(x, y, erosionSettings.borderSize, size);

			float currentHeight = heightMap[index];
			float minHeight = currentHeight;

			for (int dy = -1; dy <= 1; dy++)
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					int nx = x + dx;
					int ny = y + dy;

					if (nx >= 0 && nx < size && ny >= 0 && ny < size)
					{
						int neighborIndex = ny * size + nx;
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
			float erodedValue = 0.0f;

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

			modifiedHeightMap[index] = erodedValue;
		}

		return modifiedHeightMap;
	}
	
	private bool InsideBorder(int x, int y, int borderSize, int mapChunkSize)
	{
		// Comprueba si está dentro del borde en el eje X.
		bool insideX = x >= borderSize && x < mapChunkSize - borderSize;

		// Comprueba si está dentro del borde en el eje Z, teniendo en cuenta la dirección.
		bool insideZ = y >= borderSize && y < mapChunkSize - borderSize;
		
		if (y  == 1  || y  == (MapChunkSize * MapChunkSize - MapChunkSize/2))
			Console.WriteLine("Comprobar si esta en el borde");
		// Devuelve true si está dentro del borde en ambos ejes.
		return insideX && insideZ;
	}

	MapData GenerateMapDataJob(Vector2 centre) {
		
		NativeArray<Color> gradientColorArray = CreateGradientColor();
		
		MapDataGeneratorJob mapDataGeneratorJob = new MapDataGeneratorJob(configSettings.heightMapSettings, MapChunkSize, 
			new float2(centre.x, centre.y), gradientColorArray);
		mapDataGeneratorJob.Schedule(MapChunkSize * MapChunkSize, _batchSize).Complete();

		MapData mapData = mapDataGeneratorJob.ReturnMapData();
		mapDataGeneratorJob.Dispose();
		gradientColorArray.Dispose();
		
		if (configSettings.erosionSettings.activateErosion)
		{
			NativeArray<float> erodedHeightMap = new NativeArray<float>(mapData.heightMap, Allocator.TempJob);

			erodedHeightMap = ApplyErosion(erodedHeightMap);
			
			erodedHeightMap.CopyTo(mapData.heightMap);
			erodedHeightMap.Dispose();
		}

		return mapData;
	}

	private NativeArray<Color> CreateGradientColor()
	{
		var gradientColorArray = new NativeArray<Color>(100, Allocator.Persistent);

		for (int i = 0; i < 100; i++)
		{
			gradientColorArray[i] = colorGradient.Evaluate(i / 100f);
		}

		return gradientColorArray;
	}

	NativeArray<float> ApplyErosion(NativeArray<float> heightMap)
	{
		int cicles = configSettings.erosionSettings.cicles;
		for (int i = 0; i < cicles; i++)
		{
			float iterFraction = (float)i / cicles;
				
			ErosionJob erosionJob = new ErosionJob(heightMap, configSettings.erosionSettings, MapChunkSize, iterFraction);
			erosionJob.Schedule(MapChunkSize * MapChunkSize, _batchSize).Complete();
			erosionJob.GetErodedHeightMap().CopyTo(heightMap);
			erosionJob.Dispose();
		}
		return heightMap;
	}
	MeshData GenerateMeshDataJob(MapData mapData, MeshSettings meshSettings, int size, int levelOfDetail) {
    
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(size * size, Allocator.TempJob);
        NativeArray<Vector2> uvs = new NativeArray<Vector2>(size * size, Allocator.TempJob);
        NativeArray<int> triangles = new NativeArray<int>((size - 1) * (size - 1) * 6, Allocator.TempJob);
        NativeArray<float> heightMap = new NativeArray<float>(mapData.heightMap, Allocator.TempJob);
    
        MeshDataGeneratorJob meshGenerationJob = new MeshDataGeneratorJob(size, meshSimplificationIncrement: 1, meshSettings, heightMap, vertices, uvs, triangles);
    
        meshGenerationJob.Schedule(vertices.Length, 1440).Complete();
    
        MeshData meshData = new MeshData(vertices.ToArray(), triangles.ToArray(), uvs.ToArray());
    
        vertices.Dispose();
        uvs.Dispose();
        triangles.Dispose();
        heightMap.Dispose();
    
        return meshData;
    }

	

	void OnValidate() {
		if (configSettings.heightMapSettings.lacunarity < 1) {
			configSettings.heightMapSettings.lacunarity = 1;
		}
		if (configSettings.heightMapSettings.octaves < 0) {
			configSettings.heightMapSettings.octaves = 0;
		}
		if (configSettings.erosionSettings.talusAngle > 0) {
			configSettings.erosionSettings.talusAngle *= Mathf.PI / 180f;
		}
	}
}

public struct MapData {
	public readonly float[] heightMap;
	public readonly Color[] colourMap;

	public MapData (float[] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
	
	public MapData (NativeArray<float> heightMap, NativeArray<Color> colourMap)
	{
		this.heightMap = heightMap.ToArray();
		this.colourMap = colourMap.ToArray();
	}
}
