using UnityEngine;

public class TerrainDetector : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;
    private int alphamapWidth;
    private int alphamapHeight;
    private float[,,] splatmapData;
    private int numTextures;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        alphamapWidth = terrainData.alphamapWidth;
        alphamapHeight = terrainData.alphamapHeight;

        // Grab the texture data
        splatmapData = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
        numTextures = splatmapData.GetLength(2);
    }

    public int GetDominantTextureIndex(Vector3 worldPos)
    {
        // 1. Convert World Position to Terrain Local Position
        Vector3 terrainPos = worldPos - transform.position;

        // 2. Convert to Splatmap Coordinates (0 to 1, then to width/height)
        float mapX = (terrainPos.x / terrainData.size.x) * alphamapWidth;
        float mapZ = (terrainPos.z / terrainData.size.z) * alphamapHeight;

        // 3. Round to integer for array access
        int xIndex = Mathf.FloorToInt(mapX);
        int zIndex = Mathf.FloorToInt(mapZ);

        // Safety check to prevent crash if player walks off terrain edge
        if (xIndex < 0 || xIndex >= alphamapWidth || zIndex < 0 || zIndex >= alphamapHeight)
            return 0;

        // 4. Check which texture has the highest weight (opacity) at this point
        float maxOpacity = 0;
        int dominantIndex = 0;

        for (int i = 0; i < numTextures; i++)
        {
            if (splatmapData[zIndex, xIndex, i] > maxOpacity)
            {
                maxOpacity = splatmapData[zIndex, xIndex, i];
                dominantIndex = i;
            }
        }

        return dominantIndex;
    }
}