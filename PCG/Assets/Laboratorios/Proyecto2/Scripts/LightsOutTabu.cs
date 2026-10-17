using UnityEngine;
using System.Collections.Generic;
//este script basicamente se encarga de resolver el tablero del ligths usando el tabu como busqueda local
//donde basicamente parte de una solucion inicial(del GA) y va probando cambiar un click a la vez buscando mejorar
// y usa una lista "tabu" para no volver a los mismos movimientos durante unas cuentas iteraciones
// PSDT: Tabu ternure es basicamente durante cuantas iteraciones estara bloquedo cierto movimiento


/*
el algoritmo basicamente se encarga de resolver el tablero de LightsOut usando Tabu Search (busqueda local),
partiendo de una solucion inicial (por ejemplo los genes que devolvio el GA) y mejorandola paso a paso.
La idea es probar cambiar un clic a la vez, quedarnos con el mejor vecino aunque sea peor que el actual 
para escapar de minimos locales, y usar una lista tabu para no volver a los mismos movimientos durante 
unas cuantas iteraciones. Cada solucion tiene 25 genes (uno por casilla), se evalua simulando sus clics 
sobre una copia del tablero y aplicando fitness = 100000 - (lucesEncendidas * 1000) - clicsUsados, que 
castiga fuerte dejar luces prendidas y suave usar clics de mas. Siempre guardamos el mejor global visto 
en todo el recorrido, y si un vecino es mejor que ese mejor global se permite aunque este en la lista 
tabu (criterio de aspiracion). El algoritmo corta cuando alguien apaga todas las luces o cuando llega a 
maxTabuIterations, y devuelve la mejor solucion encontrada junto con la lista de movimientos para 
aplicarla en el tablero.

El loop sigue estos pasos:

1- se recibe una solucion inicial (los genes que devolvio el GA)
2- se evalua esa solucion con la funcion de fitness
3- se guarda como mejor global hasta ahora
4- se arranca con una lista tabu vacia (25 posiciones en 0)
5- se prueba invertir cada uno de los 25 genes y se evalua cada vecino
6- se revisa si ese movimiento esta en la lista tabu todavia
7- si esta tabu pero mejora el mejor global, se permite igual (aspiracion)
8- se elige el mejor vecino valido de la iteracion
9- si no hay ningun vecino valido, se corta
10- se mueve la solucion actual a ese vecino
11- se marca ese movimiento como tabu por tabuTenure iteraciones
12- si la nueva solucion mejora el mejor global, se actualiza
13- si alguien apago todas las luces, se corta y se devuelve
14- si no, se vuelve al paso 5 hasta llegar a maxTabuIterations
*/

public class LightsOutTabu : MonoBehaviour
{
    [Header("Configuración de Tabu Search")]
    public int tabuTenure = 5;//el numero de iteraciones para las cuales queda prohibido un movimiento despues de usarlo
    public int maxTabuIterations = 100;//cuantas iteraciones como maximo va a recorrer la busqueda antes de rendirse

    [Header("Pesos del click para el fitness")]
    public int clickPenaltyWeight = 50;
    private LightsOutBoard boardController;// referencia al script del tablero real para leer el estado

    //representacion de la solucion
    //es igual que en el GA ya que solo nos importa si hacemos click(1) o no (0)
    public class TabuSolution
    {
        public int[] genes; //25 celdas para el tablero de 5x5
        public int fitness = 0; // puntaje de la solucion, cuanto mas alto mejor
        public int lightsLeft = 25;//cuantas luces quedaron encendidas
        public int clicksUsed = 0;//cuantos click se usaron en total
        public int iterationsRun = 0;
        public TabuSolution(int size)
        {
            genes = new int[size];
        }
    }

    void Start()
    {
        boardController = GetComponent<LightsOutBoard>();//buscamos el script del tablero para leer su estado 
    }

    //metodo principal que ejecuta la busqueda
    //basicamente recibe los genes de la ultima solucion del GA y los mejora
    //cambio importante en el metodo:

    /* se cambio el tabu de esado por un tabu de accion 
     * basicamente antes el metodo guardaba el tablero completo (tabu de estado), lo que causaba que el agente caminara en circulos deshaciendo sus propios click
     * por lo que ahora hacemos el "tabu de accion" ya que guardamos un arreglo simple donde cada posicion representa una celda y se anota hasta que iteracion esta
     * prohibido volver a tocarla
       
    *lo que obliga basicamenta al algortimo a avanzar y aceptar malos putnajes temporales para poder escapar del optimo local (la solucion que dio el GA) en lugar de dar pasos atras
        


    */
    public TabuSolution RunTabuSearch(int[] initialGenes)
    {
        if (boardController == null) return null; // si no hay tablero no hace nada 
        Debug.Log("--- INICIANDO TABU SEARCH (BÚSQUEDA LOCAL) ---");

        int width = boardController.width;
        int height = boardController.height;
        int totalGenes = width * height;

        int[] currentBoard = GetFlatBoardState(width, height); //leemos el tablero actual y lo pasamos a un array plano de 25 pos

        //lista tabu donde guardamos en que iteracion cada celda deja de ser prohibida
        int[] tabuList = new int[totalGenes];

        //arrancamos con la solucion inicial que nos pasa el GA
        TabuSolution currentSolution = new TabuSolution(totalGenes);
        initialGenes.CopyTo(currentSolution.genes, 0);
        EvaluateFitness(currentSolution, currentBoard, totalGenes);

        //guardamos tambien una copia como "mejor global" hasta ahora
        TabuSolution globalBest = new TabuSolution(totalGenes);
        CopySolutionData(currentSolution, globalBest, totalGenes);

        //cada iteracion prueba invertir cada uno de los 25 genes
        for (int iter = 0; iter < maxTabuIterations; iter++)
        {
            //guardamos el mejor vecino que encontramos en la iteracion
            TabuSolution bestNeighbor = null;
            int[] bestNeighborGenes = null;
            int bestFlipIndex = -1; //guardamos que celda tocamos para hacerla tabu luego

            globalBest.iterationsRun = iter + 1;

            //probvamos a invertir cada gen y evaluamos su estado
            for (int i = 0; i < totalGenes; i++)
            {
                //clonamos la solucion actual para no modificarla mientras exploramos
                TabuSolution neighbor = new TabuSolution(totalGenes);
                currentSolution.genes.CopyTo(neighbor.genes, 0);

                //invertimos el gen n (simulamos probar el movimiento opuesto)
                neighbor.genes[i] = 1 - neighbor.genes[i];
                EvaluateFitness(neighbor, currentBoard, totalGenes);

                //revisamos si el movimiento esta prohibido (si la celda esta castigada)
                bool isTabu = tabuList[i] > iter;
                bool aspiration = neighbor.fitness > globalBest.fitness;// la aspiracion es un criterio que dice que si es mejor que el global lo permitimos aunque sea tabu

                //solo consideramos el vecino si no es tabu o si cumple con aspiracion
                if (!isTabu || aspiration)
                {
                    //nos quedamos con el mejor vecino de la iteracion
                    if (bestNeighbor == null || neighbor.fitness > bestNeighbor.fitness)
                    {
                        bestNeighbor = neighbor;
                        bestNeighborGenes = neighbor.genes;
                        bestFlipIndex = i; // anotamos cual fue la celda ganadora
                    }
                }
            }

            if (bestNeighbor == null) break; // si no encontramos ningun vecino valido se corta la busqueda

            //nos movemos al mejor vecino y marcamos ese movimiento como tabu por un tiempo
            currentSolution = bestNeighbor;
            tabuList[bestFlipIndex] = iter + tabuTenure; // castigamos la celda tocada para no volver a usarla pronto

            //si este nuevo estado es mejor que el global, actualizamos el mejor global
            if (currentSolution.fitness > globalBest.fitness)
            {
                CopySolutionData(currentSolution, globalBest, totalGenes);
            }

            // LOG para ver el estado actual
            Debug.Log($"[TABU] Iter {iter} | Luces: {currentSolution.lightsLeft} | Pasos: {currentSolution.clicksUsed} | Fitness: {currentSolution.fitness}");

            //si ya apagamos todas las luces, terminamos
            if (globalBest.lightsLeft == 0)
            {
                Debug.Log($"[TABU] ¡SOLUCIÓN ENCONTRADA en iteración {iter} con {globalBest.clicksUsed} pasos!");
                break;
            }
        }
        //si terminamos las iteraciones sin apagar todo, se muestra cual fue el mejor resultado
        if (globalBest.lightsLeft > 0)
        {
            Debug.Log($"FIN TABU SEARCH. No se encontró 0 luces. Mejor resultado: {globalBest.lightsLeft} luces restantes.");
        }

        // imprimimos la lista final de movimientos para comprobar que funciona
        LogFinalMoves(globalBest.genes, currentBoard, width, height, totalGenes);

        Debug.Log("--- FIN DE TABU SEARCH ---");
        return globalBest;
    }

    private void CopySolutionData(TabuSolution source, TabuSolution destination, int totalGenes)
    {
        for (int i = 0; i < totalGenes; i++)
        {
            destination.genes[i] = source.genes[i];
        }
        destination.fitness = source.fitness;
        destination.lightsLeft = source.lightsLeft;
        destination.clicksUsed = source.clicksUsed;
    }

    //metodo auxiliar que traduce los genes finales a una lista legible de clicks (en eje x e y)
    private void LogFinalMoves(int[] genes, int[] initialBoard, int width, int height, int totalGenes)
    {
        Debug.Log("[TABU] Secuencia final para resolver el tablero (puedes aplicarlos en cualquier orden):\n");
        int[] simulatedBoard = (int[])initialBoard.Clone();
        int moveCount = 1;

        for (int i = 0; i < totalGenes; i++)
        {
            if (genes[i] == 1)
            {
                int x = i % width;
                int y = i / height;

                SimulateClick(simulatedBoard, i, width, height);

                string boardString = $"Paso {moveCount} (Clic en X:{x}, Y:{y}):\n";
                for (int row = height - 1; row >= 0; row--)
                {
                    boardString += "[ ";
                    for (int col = 0; col < width; col++)
                    {
                        boardString += simulatedBoard[row * width + col] + " ";
                    }
                    boardString += "]\n";
                }

                Debug.Log(boardString);
                moveCount++;
            }
        }
    }

    //metodo que evalua que tan buena es la solucion, mejor fitness = mejor solucion
    private void EvaluateFitness(TabuSolution sol, int[] baseBoard,  int totalGenes)
    {
        //clonamos el tablero base para no modificar el real mientras simulamos
        int[] simulatedBoard = (int[])baseBoard.Clone();
        sol.clicksUsed = 0;

        int width = boardController.width;
        int height = boardController.height;

        //aplicamos los clicks de la solucion al tablero simulado
        for (int i = 0; i < totalGenes; i++)
        {
            if (sol.genes[i] == 1)
            {
                SimulateClick(simulatedBoard, i, width, height);
                sol.clicksUsed++;
            }
        }

        //contamos cuantas luces quedaron encendidas
        sol.lightsLeft = 0;
        foreach (int light in simulatedBoard)
        {
            if (light == 1) sol.lightsLeft++;
        }


        //calculamos el finess en base:
        //-1000 por cada luz encendida y -1 por cada click extra
        //se premia soluciones cortas y que apaguen todo basicamente igual que en el GA
        sol.fitness = 100000 - (sol.lightsLeft * 1000) - (sol.clicksUsed * clickPenaltyWeight);
    }

    //metodo que convierte la matriz 5x5 del tablero real en un array plano de 25 posiciones
    private int[] GetFlatBoardState(int width, int height)
    {
        int[] flat = new int[width * height];
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++) flat[index++] = boardController.GetState(x, y);
        }
        return flat;
    }

    //metodo que simula un clic en una cewlda del tablero plano y a y afectra a sus vecinos (basicamente la logica al clickear en el juego original)
    private void SimulateClick(int[] board, int index, int width, int height)
    {
        //convertimos el indice plano a coordenadas x,y
        int x = index % width;
        int y = index / width;
        //invertimos la celda y sus 4 vecinos directos, igual que en el juego real
        ToggleFlat(board, x, y, width, height);
        ToggleFlat(board, x + 1, y, width, height);
        ToggleFlat(board, x - 1, y, width, height);
        ToggleFlat(board, x, y + 1, width, height);
        ToggleFlat(board, x, y - 1, width, height);
    }

    //metodo auxiliar seguro que invierte una celda del tablero plano validando los limites
    private void ToggleFlat(int[] board, int x, int y, int width, int height)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            int index = y * width + x;
            board[index] = 1 - board[index];
        }
    }
}