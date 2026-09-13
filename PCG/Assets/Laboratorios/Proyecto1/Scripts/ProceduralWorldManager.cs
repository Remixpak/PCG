using System.Collections.Generic;
using UnityEngine;

//este script se encarga de unificar la generacion de terreno, Lsystem y diamond Square implementando el random walk para generar arboles y corales, ademas de manejar la semilla de generacion y la limpieza del terreno
//adicionalmente se maneja un random walk para cada generacion de arboles/colores ya que puede ser escalable para modificar ciertos parametros de generacion de vegetacion
//por lo que se decidio dejar uno para cada terreno para darle indepedencia total a cada mapa 

public class ProceduralWorldManager : MonoBehaviour
{
    [Min(-1)]
    [SerializeField] private int seed = 0; // semilla para generar el terreno, si es -1 se genera una semilla aleatoria

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
    // esto lo manejamos con un bool ToClose que verifica si la generacion de vegetacion esta muy cerca una de otra, lo utilizamos dentro del random walk

    [Range(0f, 1f)]
    [SerializeField] private float treeSpawnChance = 0.2f;// probabilidad de generar un arbol/coral en cada paso del random walk

    [Min(0f)]
    [SerializeField] private float minDistance = 3f; // distancia minimma de generacion entre la vegetacion en general (arboles/corales) para dar grado de realismo, si es 0 se pueden generar arboles/corales pegados entre si

    [Header("Contenedor")]
    [SerializeField] private Transform treeContainer;// contenedor donde se instancian los arboles generados por el random walk
    [SerializeField] private Transform coralContainer;// contenedor donde se instancian los corales generados por el random walk


    //constructor de las variables de terrain generator
    public int Seed { get { return seed; } set { seed = value; } }
    public int WalkIterations { get { return walkIterations; } set { walkIterations = value; } }
    public int TotalSteps { get { return totalSteps; } set { totalSteps = value; } }
    public float TreeSpawnChance { get { return treeSpawnChance; } set { treeSpawnChance = value; } }
    public float MinDistance { get { return minDistance; } set { minDistance = value; } }

    public float TerrainWidth = 100f; // ancho del terreno generado, se puede ajustar desde el inspector o desde la UI
    public float TerrainLength = 100f;// largo del terreno generado, se puede ajustar desde el inspector o desde la UI

    public LSystemTreeGenerator ReferenciaBosque { get { return referenciaBosque; } } // obtenemos la referencia del l system con el axioma que lo transforma en arbol
    public LSystemTreeGenerator ReferenciaCoral { get { return referenciaCoral; } }// obtenemos la referencia del l system con el axioma que lo transforma en coral



    // funcion para generar una semilla aleatoria, se puede llamar desde la UI para generar un mundo diferente cada vez
    public void GenerateSeed()
    {
        seed = Random.Range(0, int.MaxValue);
    }

    [ContextMenu("Generar Mundo Completo")]
    public void GenerateFullWorld()// genera el terreno y luego ejecuta el random walk para generar arboles
    {
        if (terrainGenerator == null)// si no se asigna el terrain generator en el inspector, muestra un error y retorna
        {
            Debug.LogError("Asigna el TerrainGenerator en el Inspector.");
            return;
        }

        ClearTrees();//limpiamos el terreno de arboles/corales antes de generar un nuevo mundo
        ClearCorals();

        terrainGenerator.TerrainWidth = TerrainWidth;
        terrainGenerator.TerrainLength = TerrainLength;

        //parametros para generar un terreno de bosque predefinidos, se pueden ajustar desde el inspector o desde la UI
        terrainGenerator.GenMethod = TerrainGenerator.GenerationMethod.DiamondSquare;
        terrainGenerator.DiamondIterations = 7;
        terrainGenerator.DiamondRoughness = 0.85f;
        terrainGenerator.DiamondRoughnessDecay = 0.5f;
        terrainGenerator.LowThreshold = 0.35f;
        terrainGenerator.HighThreshold = 0.7f;
        terrainGenerator.LowColor = new Color(0.25f, 0.55f, 0.2f, 1f);
        terrainGenerator.MiddleColor = new Color(0.45f, 0.3f, 0.15f, 1f);
        terrainGenerator.HighColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        // si se asigna una semilla, se utiliza para generar el terreno y los arboles/corales, si no se asigna una semilla, se genera una semilla aleatoria
        if (seed != -1)
        {
            terrainGenerator.Seed = seed;
            Random.InitState(seed);
        }

        terrainGenerator.GenerateTerrain();//generamos el terreno

        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();// obtenemos el componente terrain del terrain generator
        if (terrain == null)
        {
            Debug.LogError("No se encontró el objeto Terrain en el generador.");
            return;
        }

        

        // obtenemos los datos del terreno para poder calcular la posicion de los arboles/corales
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        int gridWidth = terrainData.heightmapResolution;
        int gridHeight = terrainData.heightmapResolution;

        // definimos las direcciones posibles para el random walk (arriba, abajo, izquierda, derecha)
        // basicamente random walk
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        List<Vector3> spawnedPositions = new List<Vector3>();

        // ejecutamos el random walk para generar arboles
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

                    // verificar altura
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                    // no spawnear arboles en la parte mas alta del terreno
                    float maxAllowedHeight = terrainPos.y + (terrainSize.y * 0.7f);
                    if (worldY <= maxAllowedHeight)
                    {
                        Vector3 spawnPosition = new Vector3(worldX, worldY, worldZ);

                        bool tooClose = false; // verificamos si la posicion de spawn esta demasiado cerca de otra posicion ya generada, si es asi no generamos el arbol/coral
                        foreach (Vector3 pos in spawnedPositions)// verificamos si la posicion de spawn esta demasiado cerca de otra posicion ya generada, si es asi no generamos el arbol/coral
                        {
                            if (Vector3.Distance(spawnPosition, pos) < minDistance)// si la distancia entre la posicion de spawn y otra posicion ya generada es menor a la distancia minima, no generamos el arbol/coral
                            {
                                tooClose = true;// si la distancia entre la posicion de spawn y otra posicion ya generada es menor a la distancia minima, no generamos el arbol/coral
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            LSystemTreeGenerator newTree = Instantiate(referenciaBosque, spawnPosition, Quaternion.identity, GetTreeContainer());
                            newTree.GenerateTree();
                            spawnedPositions.Add(spawnPosition);
                        }
                    }
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

        ClearTrees();//limpiamos el terreno de arboles/corales antes de generar un nuevo mundo
        ClearCorals();

        terrainGenerator.TerrainWidth = TerrainWidth;
        terrainGenerator.TerrainLength = TerrainLength;

        //parametros para generar un terreno de mar predefinidos, se pueden ajustar desde el inspector o desde la UI
        terrainGenerator.GenMethod = TerrainGenerator.GenerationMethod.DiamondSquare;
        terrainGenerator.DiamondIterations = 7;
        terrainGenerator.DiamondRoughness = 0.4f;
        terrainGenerator.DiamondRoughnessDecay = 0.4f;
        terrainGenerator.LowThreshold = 0.3f;
        terrainGenerator.HighThreshold = 0.75f;
        terrainGenerator.LowColor = new Color(0.039f, 0.110f, 0.157f, 1f);
        terrainGenerator.MiddleColor = new Color(0.102f, 0.294f, 0.361f, 1f);
        terrainGenerator.HighColor = new Color(0.761f, 0.698f, 0.502f, 1f);

        // si se asigna una semilla, se utiliza para generar el terreno y los arboles/corales, si no se asigna una semilla, se genera una semilla aleatoria
        if (seed != -1)
        {
            terrainGenerator.Seed = seed;
            Random.InitState(seed);
        }

        terrainGenerator.GenerateTerrain();//generamos el terreno

        // obtenemos el componente terrain del terrain generator
        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No se encontró el objeto Terrain en el generador.");
            return;
        }

        ClearCorals();
        ClearTrees();

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

        List<Vector3> spawnedPositions = new List<Vector3>();

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

                    // verificar altura
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                    float maxAllowedHeight = terrainPos.y + (terrainSize.y * 0.75f);
                    if (worldY <= maxAllowedHeight)
                    {
                        Vector3 spawnPosition = new Vector3(worldX, worldY, worldZ);

                        bool tooClose = false;// verificamos si la posicion de spawn esta demasiado cerca de otra posicion ya generada, si es asi no generamos el arbol/coral
                        foreach (Vector3 pos in spawnedPositions)// verificamos si la posicion de spawn esta demasiado cerca de otra posicion ya generada, si es asi no generamos el arbol/coral
                        {
                            if (Vector3.Distance(spawnPosition, pos) < minDistance)// si la distancia entre la posicion de spawn y otra posicion ya generada es menor a la distancia minima, no generamos el arbol/coral
                            {
                                tooClose = true;// si la distancia entre la posicion de spawn y otra posicion ya generada es menor a la distancia minima, no generamos el arbol/coral
                                break;
                            }
                        }

                        if (!tooClose)// si la distancia entre la posicion de spawn y otra posicion ya generada es mayor a la distancia minima, generamos el arbol/coral
                        {
                            LSystemTreeGenerator newCoral = Instantiate(referenciaCoral, spawnPosition, Quaternion.identity, GetCoralContainer());// instanciamos el l system con el axioma que lo transforma en coral en la posicion de spawn
                            newCoral.GenerateTree();// generamos el coral
                            spawnedPositions.Add(spawnPosition);// agregamos la posicion de spawn a la lista de posiciones generadas para verificar la distancia minima en la siguiente iteracion
                        }
                    }
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