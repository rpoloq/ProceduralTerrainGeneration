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

	public const int MapChunkSize = 241;
	public ConfigSettings configSettings;
	public Gradient colorGradient;
	
	
	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (configSettings.editorPreviewSettings.drawMode == EditorPreviewSettings.DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap,MapChunkSize, MapChunkSize ));
		} else if (configSettings.editorPreviewSettings.drawMode == EditorPreviewSettings.DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colourMap, MapChunkSize, MapChunkSize));
		} else if (configSettings.editorPreviewSettings.drawMode == EditorPreviewSettings.DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, configSettings.meshSettings, MapChunkSize), 
				TextureGenerator.TextureFromColourMap (mapData.colourMap, MapChunkSize, MapChunkSize));
		}
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback)
	{
		MapData mapData = GenerateMapDataJob (centre);
		callback(mapData);
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		// MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, configSettings.meshSettings, MapChunkSize);
		MeshData meshData = GenerateMeshDataJob(mapData, configSettings.meshSettings, MapChunkSize);
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
		
		NativeArray<Color> gradientColorArray = new NativeArray<Color>(100, Allocator.Persistent);

		for (int i = 0; i < 100; i++)
		{
			gradientColorArray[i] = colorGradient.Evaluate(i / 100f);
		}
		
		MapDataGeneratorJob mapDataGeneratorJob = new MapDataGeneratorJob(configSettings.heightMapSettings, MapChunkSize, new float2(centre.x, centre.y), gradientColorArray);
		mapDataGeneratorJob.Schedule(MapChunkSize * MapChunkSize, 64).Complete();

		MapData mapData = mapDataGeneratorJob.ReturnMapData();
		mapDataGeneratorJob.Dispose();
		gradientColorArray.Dispose();
		
		if (configSettings.erosionSettings.activateErosion)
		{
			NativeArray<float> erodedHeightMap = new NativeArray<float>(mapData.heightMap, Allocator.TempJob);
			for (int i = 0; i < configSettings.erosionSettings.cicles; i++)
			{
				erodedHeightMap = ApplyErosion(erodedHeightMap, configSettings.erosionSettings);
			}
			erodedHeightMap.CopyTo(mapData.heightMap);
			erodedHeightMap.Dispose();
		}

		return mapData;
	}

	NativeArray<float> ApplyErosion(NativeArray<float> heightMap, ErosionSettings erosionSettings)
	{
		NativeArray<float> erodedHeightMap = new NativeArray<float>(heightMap, Allocator.TempJob);

		ErosionJob erosionJob = new ErosionJob(heightMap, erodedHeightMap, erosionSettings, MapChunkSize);
		erosionJob.Schedule(MapChunkSize * MapChunkSize, 64).Complete();

		erodedHeightMap.CopyTo(heightMap);
		erodedHeightMap.Dispose();

		return heightMap;
	}

	MeshData GenerateMeshDataJob(MapData mapData, MeshSettings meshSettings, int size) {

		NativeArray<Vector3> vertices = new NativeArray<Vector3>(size * size, Allocator.TempJob);
		NativeArray<Vector2> uvs = new NativeArray<Vector2>(size * size, Allocator.TempJob);
		NativeArray<int> triangles = new NativeArray<int>((size - 1) * (size - 1) * 6, Allocator.TempJob);
		NativeArray<float> heightMap = new NativeArray<float>(mapData.heightMap, Allocator.TempJob);

		MeshDataGeneratorJob meshGenerationJob = new MeshDataGeneratorJob(size, meshSettings, heightMap, vertices, uvs, triangles);

		meshGenerationJob.Schedule(size * size, 64).Complete(); // Schedule the Job with the desired batch size and wait to complete.
		
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
		if (configSettings.erosionSettings.thermalSettings.talusAngle > 0) {
			configSettings.erosionSettings.thermalSettings.talusAngle *= Mathf.PI / 180f;
		}
	}

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
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
