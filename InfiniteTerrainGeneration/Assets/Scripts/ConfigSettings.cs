using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct ConfigSettings
{
    [Header("Editor Preview")]
    public EditorPreviewSettings editorPreviewSettings;
    
    [Header("Heightmap")]
    public HeightMapSettings heightMapSettings;
    
    [Header("Erosion")]
    public ErosionSettings erosionSettings;

    [Header("Mesh")]
    public MeshSettings meshSettings;
}

[System.Serializable]
public struct HeightMapSettings {
    public Noise.NormalizeMode normalizeMode;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public uint seed;
    public Vector2 offset;
}

[System.Serializable]
public struct MeshSettings {
    public float meshHeightMultiplier;
    [Range(0,1)]
    public float waterLevel;
    [Range(0,6)]
    public int editorPreviewLOD;
}

[System.Serializable]
public struct EditorPreviewSettings
{
    public enum DrawMode {NoiseMap, ColourMap, Mesh};
    public DrawMode drawMode;
    public bool autoUpdate;
}


[System.Serializable]
public struct ErosionSettings
{
    public bool activateErosion;
    public Erosion.Type type;
    [FormerlySerializedAs("iterations")] 
    public int cicles;
    public int borderSize;
    public float borderMaxReduction;

    [Header("Thermal Erosion Params")]
    public ThermalSettings thermalSettings;
    
    [Header("Water Erosion Params")]
    public WaterSettings waterSettings;

    [System.Serializable]
    public struct ThermalSettings
    {
        public float talusAngle;
    }
    
    [System.Serializable]
    public struct WaterSettings
    {
        public float erosionRate;
        public float sedimentCapacityFactor;
        public float minSedimentCapacity;
    }
    // Default values
    //     activateErosion = false;
    //     erosionType = Erosion.Type.Thermal;
    //     erosionIterations = 0;
    //     talusAngle = 90;
    //     erosionRate = 0.01f;
    //     sedimentCapacityFactor = 3.0f;
    //     minSedimentCapacity = 0.01f;
    //
}

