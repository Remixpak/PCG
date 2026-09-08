using System.Collections.Generic;
using UnityEngine;

public class ProceduralWorldManager : MonoBehaviour
{
    [Header("Referencia Terreno")]
    [SerializeField] private TerrainGenerator terrainGenerator; // obtenemos como referencia al terrain generator para poder generar el terreno, obtenemos su semilla y otros parametros(el color/decay los ajustamos dentro de codigo )

    [Header("Referencia Bosque")]
    [SerializeField] private LSystemTreeGenerator referenciaBosque;// obtenemos como referencia el l system con el axioma que lo transforma en arbol

    [Header("Referencia Mar")]
    [SerializeField] private LSystemTreeGenerator referenciaCoral;//obtenemos como referencia el l system con el axioma que lo transforma en coral

    [Header("Configuración de Random Walk")]
    [Min(1)]
    [SerializeField] private int walkIterations = 1; // cantidad de veces que se ejecuta el random walk, cada iteracion genera un arbol/coral

    [Min(1)]
    [SerializeField] private int totalSteps = 100;// cantidad de pasos que da el random walk, cada paso es una posicion donde puede generar un arbol/coral

    [Range(0f, 1f)]
    [SerializeField] private float treeSpawnChance = 0.2f;// probabilidad de generar un arbol/coral en cada paso del random walk

    [Header("Contenedor")]
    [SerializeField] private Transform treeContainer;// contenedor donde se instancian los arboles generados por el random walk
    [SerializeField] private Transform coralContainer;// contenedor donde se instancian los corales generados por el random walk

    [ContextMenu("Generar Mundo Completo")]
    public void GenerateFullWorld()// genera el terreno y luego ejecuta el random walk para generar arboles
    {
        if (terrainGenerator == null)// si no se asigna el terrain generator en el inspector, muestra un error y retorna
        {
            Debug.LogError("Asigna el TerrainGenerator en el Inspector.");
            return;
        }

        //parametros para generar un terreno de bosque
        terrainGenerator.GenMethod = TerrainGenerator.GenerationMethod.DiamondSquare;
        terrainGenerator.DiamondIterations = 7;
        terrainGenerator.DiamondRoughness = 0.85f;
        terrainGenerator.DiamondRoughnessDecay = 0.5f;
        terrainGenerator.LowThreshold = 0.35f;
        terrainGenerator.HighThreshold = 0.7f;
        terrainGenerator.LowColor = new Color(0.25f, 0.55f, 0.2f, 1f);
        terrainGenerator.MiddleColor = new Color(0.45f, 0.3f, 0.15f, 1f);
        terrainGenerator.HighColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        terrainGenerator.GenerateTerrain();//generamos el terreno

        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();// obtenemos el componente terrain del terrain generator
        if (terrain == null)
        {
            Debug.LogError("No se encontró el objeto Terrain en el generador.");
            return;
        }

        ClearTrees();

        // obtenemos los datos del terreno para poder calcular la posicion de los arboles/corales
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        int gridWidth = terrainData.heightmapResolution;
        int gridHeight = terrainData.heightmapResolution;

        // definimos las direcciones posibles para el random walk (arriba, abajo, izquierda, derecha)
        //basicamente random walk
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        // ejecutamos el random walk para generar arboles/corales
        for (int iter = 0; iter < walkIterations; iter++)
        {
            int currentX = Random.Range(0, gridWidth);
            int currentZ = Random.Range(0, gridHeight);

            for (int step = 0; step < totalSteps; step++)
            {
                Vector2Int dir = directions[Random.Range(0, directions.Length)];

                currentX = Mathf.Clamp(currentX + dir.x, 0, gridWidth - 1);
                currentZ = Mathf.Clamp(currentZ + dir.y, 0, gridHeight - 1);

                if (Random.value < treeSpawnChance && referenciaBosque != null)
                {
                    float normX = (float)currentX / (gridWidth - 1);
                    float normZ = (float)currentZ / (gridHeight - 1);

                    float worldX = terrainPos.x + (normX * terrainSize.x);
                    float worldZ = terrainPos.z + (normZ * terrainSize.z);

                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                    Vector3 spawnPosition = new Vector3(worldX, worldY, worldZ);

                    LSystemTreeGenerator newTree = Instantiate(referenciaBosque, spawnPosition, Quaternion.identity, GetTreeContainer());
                    newTree.GenerateTree();
                }
            }
        }
    }

    [ContextMenu("Generar Mundo Mar")]
    public void GenerateSeaWorld() // genera el terreno y luego ejecuta el random walk para generar corales
    {
        if (terrainGenerator == null)// si no se asigna el terrain generator en el inspector, muestra un error y retorna
        {
            Debug.LogError("Asigna el TerrainGenerator en el Inspector.");
            return;
        }

        //parametros para generar un terreno de mar
        terrainGenerator.GenMethod = TerrainGenerator.GenerationMethod.DiamondSquare;
        terrainGenerator.DiamondIterations = 7;
        terrainGenerator.DiamondRoughness = 0.45f;
        terrainGenerator.DiamondRoughnessDecay = 0.55f;
        terrainGenerator.LowThreshold = 0.3f;
        terrainGenerator.HighThreshold = 0.75f;
        terrainGenerator.LowColor = new Color(0.039f, 0.110f, 0.157f, 1f);
        terrainGenerator.MiddleColor = new Color(0.102f, 0.294f, 0.361f, 1f);
        terrainGenerator.HighColor = new Color(0.761f, 0.698f, 0.502f, 1f);

        terrainGenerator.GenerateTerrain();//generamos el terreno

        // obtenemos el componente terrain del terrain generator
        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No se encontró el objeto Terrain en el generador.");
            return;
        }

        ClearCorals();

        // obtenemos los datos del terreno para poder calcular la posicion de los corales
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        int gridWidth = terrainData.heightmapResolution;
        int gridHeight = terrainData.heightmapResolution;


        //random walk 
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        // ejecutamos el random walk para generar corales
        for (int iter = 0; iter < walkIterations; iter++)
        {
            int currentX = Random.Range(0, gridWidth);
            int currentZ = Random.Range(0, gridHeight);

            for (int step = 0; step < totalSteps; step++)
            {
                Vector2Int dir = directions[Random.Range(0, directions.Length)];

                currentX = Mathf.Clamp(currentX + dir.x, 0, gridWidth - 1);
                currentZ = Mathf.Clamp(currentZ + dir.y, 0, gridHeight - 1);

                if (Random.value < treeSpawnChance && referenciaCoral != null)
                {
                    float normX = (float)currentX / (gridWidth - 1);
                    float normZ = (float)currentZ / (gridHeight - 1);

                    float worldX = terrainPos.x + (normX * terrainSize.x);
                    float worldZ = terrainPos.z + (normZ * terrainSize.z);

                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                    Vector3 spawnPosition = new Vector3(worldX, worldY, worldZ);

                    LSystemTreeGenerator newCoral = Instantiate(referenciaCoral, spawnPosition, Quaternion.identity, GetCoralContainer());
                    newCoral.GenerateTree();
                }
            }
        }
    }

    //limpiamos el terreno de bosque
    public void ClearForestWorld()
    {
        ClearTrees();
        DeleteTerrain();
    }

    //limpiamos el terreno de mar
    public void ClearSeaWorld()
    {
        ClearCorals();
        DeleteTerrain();
    }

    //limpiamos los arboles generados por el random walk
    public void ClearTrees()
    {
        if (treeContainer != null)
        {
            for (int i = treeContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(treeContainer.GetChild(i).gameObject);
            }
        }
    }

    //limpiamos los corales generados por el random walk
    public void ClearCorals()
    {
        if (coralContainer != null)
        {
            for (int i = coralContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(coralContainer.GetChild(i).gameObject);
            }
        }
    }

    // eliminamos el terreno generado por el terrain generator
    public void DeleteTerrain()
    {
        if (terrainGenerator != null)
        {
            terrainGenerator.DeleteTerrain();
        }
    }
    // obtenemos el contenedor de arboles, si no existe lo creamos
    private Transform GetTreeContainer()
    {
        if (treeContainer == null)
        {
            GameObject container = new GameObject("Trees_Container");
            container.transform.SetParent(transform);
            treeContainer = container.transform;
        }
        return treeContainer;
    }

    // obtenemos el contenedor de corales, si no existe lo creamos
    private Transform GetCoralContainer()
    {
        if (coralContainer == null)
        {
            GameObject container = new GameObject("Corals_Container");
            container.transform.SetParent(transform);
            coralContainer = container.transform;
        }
        return coralContainer;
    }
}