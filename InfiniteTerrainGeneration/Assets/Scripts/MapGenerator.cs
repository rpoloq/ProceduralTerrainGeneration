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

	public const int MapChunkSize = 37;
	public ConfigSettings configSettings;
	public Gradient colorGradient;
	private int _batchSize = 32;


	public void DrawMapInEditor() {
		MapData mapData = GenerateMapDataJob(Vector2.zero);

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
