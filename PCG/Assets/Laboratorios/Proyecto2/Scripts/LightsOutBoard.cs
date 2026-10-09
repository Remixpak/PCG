using System.Runtime.CompilerServices;
using UnityEngine;

public class LightsOutBoard : MonoBehaviour
{
    [Header("Configuración de Grilla")]

    // Tamaño de la grilla (ancho x alto)
    public int width = 5;
    public int height = 5;

    [Header("Generación por Backward from Goal State")]
    // Número de pasos hacia atrás desde el estado meta (todo apagado)
    public int backwardSteps = 5;

    [Header("Generación por Semilla Numérica")]
    public int seed = 12345;
    public bool useRandomSeedOnStart = false;

    [Header("Visuales")]
    public LightsOutVisualizer visualizer;

    // Matriz del estado del tablero: 0 = Apagado, 1 = Encendido
    private int[,] board;

    void Start()
    {
        board = BackwardsFromGoalState(width, seed);
        if (visualizer != null)
        {
            visualizer.RenderBoard(board, width, height);
        }
    }

    private int[,] BackwardsFromGoalState(int dimension = 5, int iterations = 5, int seed = -1)
    {
        int[,] newBoard = new int[width, height];

        // Generar seed aleatorio si no se proporciona una
        if (seed == -1)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        Random.InitState(seed);

        // Llenar la matriz con 0s (estado final)
        for(int i = 0; i < dimension; i++)
        {
            for(int j = 0; j < dimension; j++)
            {
                newBoard[i, j] = 0;
            }
        }

        // Realizar iteraciones para encender y apagar luces aleatoriamente, simulando movimientos hacia atrás desde el estado final
        // Los movimientos en el juego son encender una 'cruz' de luces, que alterna entre estado de encendido y apagado
        for(int i = 0; i < iterations; i++)
        {
            int x = Random.Range(0, dimension);
            int y = Random.Range(0, dimension);

            newBoard[x, y] = 1 - newBoard[x, y]; // Alternar el estado de la luz seleccionada
            if(x > 0) newBoard[x - 1, y] = 1 - newBoard[x - 1, y]; // Alternar la luz a la izquierda
            if(x < dimension - 1) newBoard[x + 1, y] = 1 - newBoard[x + 1, y]; // Alternar la luz a la derecha
            if(y > 0) newBoard[x, y - 1] = 1 - newBoard[x, y - 1]; // Alternar la luz de arriba
            if(y < dimension - 1) newBoard[x, y + 1] = 1 - newBoard[x, y + 1]; // Alternar la luz de abajo
        }

        return newBoard;
    }

    public void InitializeBoard()
    {
        board = new int[width, height];

        // Inicializamos todas las luces en 0 (Estado meta / Goal State)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                board[x, y] = 0;
            }
        }

        // Determinamos el valor numérico de la semilla ANTES de imprimirla
        int currentSeed = seed;

        if (useRandomSeedOnStart)
        {
            // Usamos Guid para garantizar una semilla única en cada ejecución ya que System.Random no tiene un método directo para obtener una semilla aleatoria
            currentSeed = System.Guid.NewGuid().GetHashCode();
        }

        // Imprimimos los logs de inicio con la semilla ya calculada y correcta para poder visualizar el estado inicial de la grilla
        Debug.Log("--- INICIO DE GENERACIÓN BACKWARD FROM GOAL STATE ---");
        Debug.Log($"---- Semilla actual: {currentSeed} ----");
        LogBoardState("Estado Inicial (Todo apagado):");

        // System.Random acepta un entero 'int' directamente como parámetro de semilla
        System.Random pseudoRandom = new System.Random(currentSeed);

        // Aplicar los pasos hacia atrás del algoritmo (Backward)
        for (int i = 0; i < backwardSteps; i++)
        {
            int randX = pseudoRandom.Next(0, width);
            int randY = pseudoRandom.Next(0, height);

            // Simulamos el clic inverso en la celda y sus vecinos
            ToggleCellAndNeighbors(randX, randY);

            // Imprimimos en consola cómo quedó la matriz en este paso
            LogBoardState($"Paso {i + 1} (Clic inverso en X:{randX}, Y:{randY}):");
        }

        Debug.Log("--- FIN DE GENERACIÓN ---");

        // Notificar al visualizador para que pinte la grilla
        if (visualizer != null)
        {
            visualizer.RenderBoard(board, width, height);
        }
    }

    // Metodo que invierte el estado de una celda y sus 4 vecinos directos
    private void ToggleCellAndNeighbors(int x, int y)
    {
        ToggleCell(x, y);
        ToggleCell(x + 1, y);
        ToggleCell(x - 1, y);
        ToggleCell(x, y + 1);
        ToggleCell(x, y - 1);
    }

    // Metodo auxiliar seguro para cambiar el estado validando los límites
    private void ToggleCell(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            board[x, y] = 1 - board[x, y];
        }
    }

    // Metodo auxiliar para imprimir el estado actual de la matriz en la consola de Unity
    private void LogBoardState(string message)
    {
        string boardString = message + "\n";
        for (int y = height - 1; y >= 0; y--) // imprimos de arriba a abajo para que coincida visualmente
        {
            string rowStr = "[ ";
            for (int x = 0; x < width; x++)
            {
                rowStr += board[x, y] + " ";
            }
            boardString += rowStr + "]\n";
        }
        Debug.Log(boardString);
    }

    // Metodo de acceso al estado de la casilla
    public int GetState(int x, int y)
    {
        return board[x, y];
    }
}