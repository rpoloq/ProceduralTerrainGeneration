using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour {

	const float Scale = 5f;

	const float ViewerMoveThresholdForChunkUpdate = 25f;
	const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

	public LODInfo[] detailLevels;
	public static float maxViewDst;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 _viewerPositionOld;
	static MapGenerator _mapGenerator;
	int _chunkSize;
	int _chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		_mapGenerator = FindObjectOfType<MapGenerator> ();

		maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		_chunkSize = MapGenerator.MapChunkSize - 1;
		_chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / _chunkSize);

		UpdateVisibleChunks ();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / Scale;

		if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) {
			_viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
	}
		
	void UpdateVisibleChunks() {

		for (int i = 0; i < _terrainChunksVisibleLastUpdate.Count; i++) {
			_terrainChunksVisibleLastUpdate [i].SetVisible (false);
		}
		_terrainChunksVisibleLastUpdate.Clear ();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / _chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / _chunkSize);

		for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (_terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					_terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else {
					_terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, _chunkSize, detailLevels, transform, mapMaterial));
				}
			}
		}
	}

	public class TerrainChunk {

		GameObject _meshObject;
		Vector2 _position;
		Bounds _bounds;

		MeshRenderer _meshRenderer;
		MeshFilter _meshFilter;
		MeshCollider _meshCollider;
		
		LODInfo[] _detailLevels;
		LODMesh[] _lodMeshes;

		MapData _mapData;
		bool _mapDataReceived;
		int _previousLODIndex = -1;

		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
			this._detailLevels = detailLevels;
			
			// En el momento de escalar la posición del chunk a coordenadas abosultas, el vértice extra de diferencia entre el número de vértices por lado
			// y el número de subdividiones por lado hará que por se separen unos chunks de otros tantas unidades como diferencia haya entre vétices y subdiviones por lado
			// multiplicado por el númeor de chunks que se hayan instanciado.
			_position = coord * (size - 1);
			_bounds = new Bounds(_position,Vector2.one * size);
			Vector3 positionV3 = new Vector3(_position.x,0,_position.y);

			_meshObject = new GameObject("Terrain Chunk");
			_meshRenderer = _meshObject.AddComponent<MeshRenderer>();
			_meshFilter = _meshObject.AddComponent<MeshFilter>();
			_meshCollider = _meshObject.AddComponent<MeshCollider>();
			_meshRenderer.material = material;

			_meshObject.transform.position = positionV3 * Scale;
			_meshObject.transform.parent = parent;
			_meshObject.transform.localScale = Vector3.one * Scale;
			// _meshObject.tag = "Ground";
			
			SetVisible(false);

			_lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {
				_lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
			}

			_mapGenerator.RequestMapData(_position,OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
			this._mapData = mapData;
			_mapDataReceived = true;

			Texture2D texture = TextureGenerator.TextureFromColourMap (mapData.colourMap, MapGenerator.MapChunkSize, MapGenerator.MapChunkSize);
			_meshRenderer.material.mainTexture = texture;

			UpdateTerrainChunk ();
		}

	

		public void UpdateTerrainChunk() {
			if (_mapDataReceived) {
				float viewerDstFromNearestEdge = Mathf.Sqrt (_bounds.SqrDistance (viewerPosition));
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				if (visible) {
					int lodIndex = 0;

					for (int i = 0; i < _detailLevels.Length - 1; i++) {
						if (viewerDstFromNearestEdge > _detailLevels [i].visibleDstThreshold) {
							lodIndex = i + 1;
						} else {
							break;
						}
					}

					if (lodIndex != _previousLODIndex) {
						LODMesh lodMesh = _lodMeshes [lodIndex];
						if (lodMesh.hasMesh) {
							_previousLODIndex = lodIndex;
							_meshFilter.mesh = lodMesh.mesh;
							_meshCollider.sharedMesh = lodMesh.mesh;
						} else if (!lodMesh.hasRequestedMesh) {
							lodMesh.RequestMesh (_mapData);
						}
					}

					_terrainChunksVisibleLastUpdate.Add (this);
				}

				SetVisible (visible);
			}
		}

		public void SetVisible(bool visible) {
			_meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return _meshObject.activeSelf;
		}

	}

	class LODMesh {

		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int _lod;
		System.Action _updateCallback;

		public LODMesh(int lod, System.Action updateCallback) {
			this._lod = lod;
			this._updateCallback = updateCallback;
		}

		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh ();
			hasMesh = true;

			_updateCallback ();
		}

		public void RequestMesh(MapData mapData) {
			hasRequestedMesh = true;
			_mapGenerator.RequestMeshData (mapData, _lod, OnMeshDataReceived);
		}

	}

	[System.Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
	}

}
