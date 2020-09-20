using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageToTerrainConverter : MonoBehaviour{
    public Terrain terrain;
    public Texture2D terrainMap;

    //terrain settings
    public float treeDensity = 0.06f;
    public float treeMinMargin = 1.5f;

    public float rockDensity = 0.01f;

    //prefabs
    public GameObject treePrefab;
    public GameObject rockPrefab;

    //pixelDataLists
    private List<Vector2> treePixelPositions = new List<Vector2>();
    private List<Vector2> rockPixelPositions = new List<Vector2>();

    //prefabLists
    


    private Vector3 terrainSize = new Vector3(0, 0, 0);


    void Start(){
        //set random seed
        Random.InitState(420);

        //get terrain size
        terrainSize = terrain.terrainData.size;


        //iterate through image and save pixelData in list
        for(int x=0; x<=100; x++){
            for(int y=0; y<=100; y++){
                
                Colors terrainType = getTerrainDataFromImage(terrainMap, x/100f, y/100f);

                if(terrainType == Colors.WALD){
                    treePixelPositions.Add(new Vector2(x/100f * terrainSize.x, y/100f * terrainSize.z));
                }
            }
        }

        for(int x=0; x<=100; x++){
            for(int y=0; y<=100; y++){
                
                Colors terrainType = getTerrainDataFromImage(terrainMap, x/100f, y/100f);

                if(terrainType == Colors.WIESE){
                    rockPixelPositions.Add(new Vector2(x/100f * terrainSize.x, y/100f * terrainSize.z));
                }
            }
        }
        
        setHeightMap();
        smoothTerrain(1);
        smoothTerrain(1);
        smoothTerrain(2);
        smoothTerrain(3);
        smoothTerrain(4);

        paintTerrain();

        generateTrees(treePrefab, treePixelPositions, treeDensity);
        generateTrees(rockPrefab, rockPixelPositions, rockDensity);
        generateGrass();
    }


    private void paintTerrain(){
        // Get a reference to the terrain data
        TerrainData terrainData = terrain.terrainData;
 
        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[, ,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
         
        for (int y = 0; y < terrainData.alphamapHeight; y++){
            for (int x = 0; x < terrainData.alphamapWidth; x++){

                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y/(float)terrainData.alphamapHeight;
                float x_01 = (float)x/(float)terrainData.alphamapWidth;
                 
                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight),Mathf.RoundToInt(x_01 * terrainData.heightmapWidth) );
                 
                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01,x_01);
      
                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01,x_01);
                 
                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];
                 
     
                //grass
                splatWeights[0] = 1 - Mathf.Clamp01(Mathf.Clamp01(steepness*steepness/(terrainData.heightmapHeight/5.0f)) - 0.6f);

                //rock
                splatWeights[1] = Mathf.Clamp01(Mathf.Clamp01(steepness*steepness/(terrainData.heightmapHeight/5.0f)) - 0.6f);

                if(height > 3){
                    splatWeights[1] += Mathf.Pow(height - 3, 2) / 10f;
                }

                //snow
                if(height > 8){
                    splatWeights[2] = 100 * Mathf.Clamp01(height - 8.6f);
                }

                float z = 0;
                foreach(float f in splatWeights){
                    z += f;
                }
                 
                // Loop through each terrain texture
                for(int i = 0; i<terrainData.alphamapLayers; i++){
                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;
                     
                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }
      
        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    private void setHeightMap(){
        float[,] heightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapTexture.width, terrain.terrainData.heightmapTexture.height);

        for(int x=0; x<heightMap.GetLength(0); x++){
            for(int y=0; y<heightMap.GetLength(1); y++){
                
                Colors terrainType = getTerrainDataFromImage(terrainMap, y * 1f/heightMap.GetLength(1), x * 1f/heightMap.GetLength(0));

                if(terrainType == Colors.WASSER){
                    heightMap[x, y] = 0;
                }else{
                    float perlinScale = 0.02f;
                    float perlinMultiplier = 0.004f;

                    if(terrainType == Colors.BERG || terrainType == Colors.SCHNEE){
                        perlinMultiplier *= 6;
                    }

                    heightMap[x, y] = Mathf.PerlinNoise(x * perlinScale, y * perlinScale) * perlinMultiplier + 0.002f;
                }
            }
        }

        terrain.terrainData.SetHeights(0, 0, heightMap); 
    }

    private void smoothTerrain(int pixelDistance){
        float[,] heightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapTexture.width, terrain.terrainData.heightmapTexture.height);
        float[,] smoothedHeightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapTexture.width, terrain.terrainData.heightmapTexture.height);

        for(int x=0; x<heightMap.GetLength(0); x++){
            for(int y=0; y<heightMap.GetLength(1); y++){
                float heightAverage = 0;

                heightAverage += getClampedValueFromHeightMap(x, (y-pixelDistance), heightMap);
                heightAverage += getClampedValueFromHeightMap(x, (y+pixelDistance), heightMap);
                heightAverage += getClampedValueFromHeightMap((x+pixelDistance), y, heightMap);
                heightAverage += getClampedValueFromHeightMap((x-pixelDistance), y, heightMap);

                heightAverage /= 4f;

                smoothedHeightMap[x, y] = heightAverage;
            }
        }

        //apply height map
        terrain.terrainData.SetHeights(0, 0, smoothedHeightMap); 
    }

    private float getClampedValueFromHeightMap(int x, int y, float[,] heightMap){
        return heightMap[Mathf.Clamp(x, 0, heightMap.GetLength(0) - 1), Mathf.Clamp(y, 0, heightMap.GetLength(1) - 1)];
    }

    private void generateGrass(){
        //Get all of layer zero.
        int[,] map = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, 0);

        // For each pixel in the detail map...
        for (int y = 0; y < terrain.terrainData.detailHeight; y++){
            for (int x = 0; x < terrain.terrainData.detailWidth; x++){
                // If the pixel value is below the threshold then
                // set it to zero.
                Colors terrainType = getTerrainDataFromImage(terrainMap, y * 1f/terrain.terrainData.detailHeight, x * 1f/terrain.terrainData.detailWidth);

                if(terrainType == Colors.WIESE || (terrainType == Colors.WALD && Random.value <= 0.3f) || ((terrainType == Colors.BERG || terrainType == Colors.SCHNEE) && Random.value <= 0.01f)){
                    map[x, y] = 1;
                }else{
                    map[x, y] = 0;
                }

            }
        }

        // Assign the modified map back.
        terrain.terrainData.SetDetailLayer(0, 0, 0, map);  
    }

    private void generateTrees(GameObject prefab, List<Vector2> pixelPositions, float density){
        List<GameObject> trees = new List<GameObject>();

        foreach(Vector2 pixelPos in pixelPositions){

            if(Random.value <= density){

                //check for margin
                bool isPlantable = true;
                foreach(GameObject tree in trees){

                    //calc distance
                    float distance = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(tree.transform.position.z - pixelPos.y), 2) + Mathf.Pow(Mathf.Abs(tree.transform.position.x - pixelPos.x), 2));
                    
                    if(distance < treeMinMargin){
                        isPlantable = false;
                        break;
                    }
                }

                if(!isPlantable){
                    continue;
                }


                float height = getTerrainHeight(pixelPos.x, pixelPos.y);
                Vector3 pos = new Vector3(pixelPos.x, height, pixelPos.y);

                GameObject go = Instantiate(prefab, pos, Quaternion.Euler(0, Random.value * 360, 0));

                float scaleFactor = Random.value * 0.5f - 0.2f;
                go.transform.localScale += new Vector3(scaleFactor, scaleFactor, scaleFactor);
                
                trees.Add(go);
            }
        }
    }

    private float getTerrainHeight(float x, float y){
        Vector3 vec3 = new Vector3(x, 0, y);
        return terrain.SampleHeight(vec3);
    }

    private Colors getTerrainDataFromImage(Texture2D img, float x, float y){
        if(x < 0 || x > 1 || y < 0 || y > 1){
            return Colors.WIESE;
        }

        //get abs position
        int xPos = (int) Mathf.Round(x * img.width);
        int yPos = (int) Mathf.Round(y * img.height);

        //convert to hex color
        Color c = img.GetPixel(xPos, yPos);
        string hexColor = ColorUtility.ToHtmlStringRGB(c);

        return ColorPicker.getColorFromHex(hexColor);
    }
}
