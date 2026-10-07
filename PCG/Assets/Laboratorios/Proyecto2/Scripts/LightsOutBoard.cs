using UnityEngine;

public class LightsOutBoard : MonoBehaviour
{
    [Header("Configuración de Grilla")]
    public int width = 5;
    public int height = 5;

    [Header("Generación por Semilla Numérica")]
    public int seed = 12345;
    public bool useRandomSeedOnStart = false;

    [Header("Visuales")]
    public LightsOutVisualizer visualizer;

    // Matriz del estado del tablero: 0 = Apagado, 1 = Encendido
    private int[,] board;

    void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        board = new int[width, height];

        // Determinar el valor numérico de la semilla
        int currentSeed = seed;

        if (useRandomSeedOnStart)
        {
            // Se usa el valor de Ticks truncado a int32
            currentSeed = (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF);
        }

        // System.Random acepta un entero 'int' directamente como parámetro de semilla
        System.Random pseudoRandom = new System.Random(currentSeed);

        // Llenar la matriz con 0s y 1s
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Genera 0 o 1 de forma determinista segun el entero de la semilla
                board[x, y] = pseudoRandom.Next(0, 2);
            }
        }

        // Notificar al visualizador para que pinte la grilla
        if (visualizer != null)
        {
            visualizer.RenderBoard(board, width, height);
        }
    }

    //metodo de acceso al estado de la casilla
    public int GetState(int x, int y)
    {
        return board[x, y];
    }
}