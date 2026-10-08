using System.Runtime.CompilerServices;
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