using System.Collections.Generic;
using UnityEngine;

public class ProceduralWorldManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TerrainGenerator terrainGenerator;
    [SerializeField] private LSystemTreeGenerator treePrefab; // Prefab con el componente LSystemTreeGenerator

    [Header("Configuración de Random Walk")]
    [Tooltip("Cantidad de veces que se ejecutará la caminata aleatoria desde un nuevo punto.")]
    [Min(1)]
    [SerializeField] private int walkIterations = 1; // <--- NUEVO PARÁMETRO

    [Tooltip("Pasos que dará el Random Walk en cada iteración.")]
    [Min(1)]
    [SerializeField] private int totalSteps = 100;

    [Range(0f, 1f)]
    [SerializeField] private float treeSpawnChance = 0.2f; // Probabilidad de plantar un árbol por paso

    [Header("Contenedor")]
    [SerializeField] private Transform treeContainer;

    [ContextMenu("Generar Mundo Completo")]
    public void GenerateFullWorld()
    {
        // -----------------------------------------------------------------
        // PASO 1: Generar Terreno mediante Diamond-Square
        // -----------------------------------------------------------------
        if (terrainGenerator == null)
        {
            Debug.LogError("Asigna el TerrainGenerator en el Inspector.");
            return;
        }

        // Forzamos la generación del terreno con la configuración actual del inspector
        terrainGenerator.GenerateTerrain();

        // Obtenemos el Terrain recién creado
        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No se encontró el objeto Terrain en el generador.");
            return;
        }

        // Limpiar árboles previos
        ClearTrees();

        // -----------------------------------------------------------------
        // PASO 2: Recorrido con Random Walk (Iterado) sobre la grilla
        // -----------------------------------------------------------------
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        int gridWidth = terrainData.heightmapResolution;
        int gridHeight = terrainData.heightmapResolution;

        // Direcciones posibles en la grilla (Norte, Sur, Este, Oeste)
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        // BUCLE EXTERNO DE ITERACIONES DE RANDOM WALK
        for (int iter = 0; iter < walkIterations; iter++)
        {
            // Cada iteración del Random Walk puede comenzar en un punto aleatorio dentro del terreno
            int currentX = Random.Range(0, gridWidth);
            int currentZ = Random.Range(0, gridHeight);

            for (int step = 0; step < totalSteps; step++)
            {
                // Elegir dirección aleatoria
                Vector2Int dir = directions[Random.Range(0, directions.Length)];

                // Avanzar y Clamp para no salir de las dimensiones del terreno
                currentX = Mathf.Clamp(currentX + dir.x, 0, gridWidth - 1);
                currentZ = Mathf.Clamp(currentZ + dir.y, 0, gridHeight - 1);

                // -------------------------------------------------------------
                // PASO 3: Intentar instanciar un árbol L-System en el paso actual
                // -------------------------------------------------------------
                if (Random.value < treeSpawnChance && treePrefab != null)
                {
                    // Convertir posición en la grilla a coordenadas en el mundo 3D
                    float normX = (float)currentX / (gridWidth - 1);
                    float normZ = (float)currentZ / (gridHeight - 1);

                    float worldX = terrainPos.x + (normX * terrainSize.x);
                    float worldZ = terrainPos.z + (normZ * terrainSize.z);

                    // Obtener la altura del terreno en las coordenadas (X, Z)
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                    Vector3 spawnPosition = new Vector3(worldX, worldY, worldZ);

                    // Instanciar el árbol
                    LSystemTreeGenerator newTree = Instantiate(treePrefab, spawnPosition, Quaternion.identity, GetTreeContainer());

                    // Generar la geometría del árbol
                    newTree.GenerateTree();
                }
            }
        }
    }

    public void ClearTrees()
    {
        if (treeContainer != null)
        {
            // Limpiar árboles existentes
            for (int i = treeContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(treeContainer.GetChild(i).gameObject);
            }
        }
    }

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
}