using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class FieldManager : MonoBehaviour {
    public static FieldManager fieldManager;

    int worldSizeX;
    int worldSizeY;

    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap breakTilemap;
    [SerializeField] CompositeCollider2D compositeCollider2D;

    int[,] curTilesHealth;
    int[,] maxTilesHealth;

    Texture2D lightTexture;
    bool[,] curLightArr;
    bool[,] befLightArr;
    SpriteMask worldLight2D;

    void Awake() {
        if (fieldManager == null) {
            fieldManager = this;
        }
    }
    void Start() {
        worldSizeX = groundTilemap.size.x;
        worldSizeY = groundTilemap.size.y;
 
        curTilesHealth = new int[worldSizeX, worldSizeY];
        maxTilesHealth = new int[worldSizeX, worldSizeY];

        lightTexture = new Texture2D(worldSizeX, worldSizeY);
        lightTexture.filterMode = FilterMode.Point;
        for (int i = 0; i < worldSizeX; i++) {
            for (int j = 0; j < worldSizeY; j++) {
                lightTexture.SetPixel(i, j, new Color(0, 0, 0, 0));
            }
        }

        curLightArr = new bool[worldSizeX, worldSizeY];
        befLightArr = new bool[worldSizeX, worldSizeY];
        for (int i = 0; i < worldSizeX; i++) {
            if (groundTilemap.GetTile<BlockClass>(new Vector3Int(i, worldSizeY - 1, 0)) == null) {
                PathFinder(i, worldSizeY - 1, true, true);
                break;
            }
        }
        befLightArr = (bool[,])curLightArr.Clone();

        for (int i = 0; i < worldSizeX; i++) {
            for (int j = 0; j < worldSizeY; j++) {
                if (curLightArr[i, j]) {
                    LightUpTexture(i, j);
                }

                BlockClass thisTile = groundTilemap.GetTile<BlockClass>(new Vector3Int(i, j, 0));
                if (thisTile != null) {
                    for (int k = 0; k < (int)BlockEnum.BLOCKENUM.LENGTH; k++) {
                        if ((int)thisTile.blockEnum == k) {
                            curTilesHealth[i, j] = BlockEnum.blockHealth[k];
                            break;
                        }
                    }
                }
            }
        }

        lightTexture.Apply();
        maxTilesHealth = (int[,])curTilesHealth.Clone();

        GameObject worldLight = new GameObject("Sprite Mask");
        worldLight2D = worldLight.AddComponent<SpriteMask>();
        worldLight2D.sprite = Sprite.Create(lightTexture, new Rect(0, 0, worldSizeX, worldSizeY), Vector2.zero, 1);

        DrawShadowCaster2D(true);
    }

    void DrawShadowCaster2D(bool isInitial = false) {
        ShadowCaster2D shadowCaster2D;
        List<Vector2> pathPoints2D = new List<Vector2>();
        List<Vector3> pathPoints3D = new List<Vector3>();
        if (isInitial) {
            compositeCollider2D.transform.GetChild(1).gameObject.AddComponent<ShadowCaster2D>();
            compositeCollider2D.transform.GetChild(0).gameObject.AddComponent<ShadowCaster2D>();
            compositeCollider2D.gameObject.AddComponent<CompositeShadowCaster2D>();
        }
        compositeCollider2D.GetPath(0, pathPoints2D);
        for (int i = 0; i < pathPoints2D.Count; i++) {
            pathPoints3D.Add(pathPoints2D[i]);
        }
        shadowCaster2D = compositeCollider2D.transform.GetChild(1).GetComponent<ShadowCaster2D>();
        typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shadowCaster2D, pathPoints3D.ToArray());
        typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shadowCaster2D, Random.Range(int.MinValue, int.MaxValue));
        shadowCaster2D.Update();

        pathPoints2D.Clear();
        pathPoints3D.Clear();

        compositeCollider2D.GetPath(1, pathPoints2D);
        for (int i = 0; i < pathPoints2D.Count; i++) {
            pathPoints3D.Add(pathPoints2D[i]);
        }
        shadowCaster2D = compositeCollider2D.transform.GetChild(0).GetComponent<ShadowCaster2D>();
        typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shadowCaster2D, pathPoints3D.ToArray());
        typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shadowCaster2D, Random.Range(int.MinValue, int.MaxValue));
        shadowCaster2D.Update();

        int pathCount = compositeCollider2D.pathCount;
        int childCount = compositeCollider2D.transform.childCount;
        if (pathCount > childCount) {
            for (int i = childCount; i < pathCount; i++) {
                ObjectPools.objectPools.SpawnShadowCasterFromPool().transform.SetParent(compositeCollider2D.transform, false);
            }
        }
        else if (pathCount < childCount) {
            for (int i = pathCount; i < childCount; i++) {
                ObjectPools.objectPools.WithdrawShadowCasterToPool(compositeCollider2D.transform.GetChild(i).gameObject.GetComponent<ShadowCaster2D>());
            }
        }
        for (int i = 2; i < pathCount; i++) {
            pathPoints2D.Clear();
            pathPoints3D.Clear();

            compositeCollider2D.GetPath(i, pathPoints2D);
            for (int j = 0; j < pathPoints2D.Count; j++) {
                pathPoints3D.Add(pathPoints2D[j]);
            }
            shadowCaster2D = compositeCollider2D.transform.GetChild(i).GetComponent<ShadowCaster2D>();
            typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shadowCaster2D, pathPoints3D.ToArray());
            typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shadowCaster2D, Random.Range(int.MinValue, int.MaxValue));
            shadowCaster2D.Update();
        }
    }

    void PathFinder(int x, int y, bool isHorizontal, bool isPlus, bool isOrigin = false) {
        if (!curLightArr[x, y]) {
            curLightArr[x, y] = true;
            if (isOrigin) {
                TakeBranch(x, y, true, false);
                TakeBranch(x, y, true, true);
                TakeBranch(x, y, false, false);
                TakeBranch(x, y, false, true);
            }
            else {
                if (isHorizontal) {
                    if (!isPlus) {
                        TakeBranch(x, y, true, false);
                        TakeBranch(x, y, false, false);
                        TakeBranch(x, y, false, true);
                    }
                    else {
                        TakeBranch(x, y, true, true);
                        TakeBranch(x, y, false, false);
                        TakeBranch(x, y, false, true);
                    }
                }
                else {
                    if (!isPlus) {
                        TakeBranch(x, y, true, false);
                        TakeBranch(x, y, true, true);
                        TakeBranch(x, y, false, false);
                    }
                    else {
                        TakeBranch(x, y, true, false);
                        TakeBranch(x, y, true, true);
                        TakeBranch(x, y, false, true);
                    }
                }
            }
        }
    }
    void TakeBranch(int x, int y, bool isHorizontal, bool isPlus) {
        if (isHorizontal) {
            if (isPlus) {
                if (x != worldSizeX - 1) {
                    if (groundTilemap.GetTile<BlockClass>(new Vector3Int(x + 1, y, 0)) == null) {
                        PathFinder(x + 1, y, true, true);
                    }
                }
            }
            else {
                if (x != 0) {
                    if (groundTilemap.GetTile<BlockClass>(new Vector3Int(x - 1, y, 0)) == null) {
                        PathFinder(x - 1, y, true, false);
                    }
                }
            }
        }
        else {
            if (isPlus) {
                if (y != worldSizeY - 1) {
                    if (groundTilemap.GetTile<BlockClass>(new Vector3Int(x, y + 1, 0)) == null) {
                        PathFinder(x, y + 1, false, true);
                    }
                }
            }
            else {
                if (y != 0) {
                    if (groundTilemap.GetTile<BlockClass>(new Vector3Int(x, y - 1, 0)) == null) {
                        PathFinder(x, y - 1, false, false);
                    }
                }
            }
        }
    }
    void LightUpTexture(int x, int y) {
        lightTexture.SetPixel(x, y, Color.white);
        if (x != 0) {
            lightTexture.SetPixel(x - 1, y, Color.white);
        }
        if (x != worldSizeX - 1) {
            lightTexture.SetPixel(x + 1, y, Color.white);
        }
        if (y != 0) {
            lightTexture.SetPixel(x, y - 1, Color.white);
        }
        if (y != worldSizeY - 1) {
            lightTexture.SetPixel(x, y + 1, Color.white);
        }
    }
}
