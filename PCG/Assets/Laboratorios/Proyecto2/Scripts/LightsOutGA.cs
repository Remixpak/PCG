using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


//este script basicamente se encarga de resolver el ligths out usando el algortimo genetico
//donde basicamente simula muchos individuos(posibles soluciones), los evalua con el fitness y va mejorando la poblacion gen x gen
//hasta encontrar el que la combinacion de clicks que deje la menor cantidad de luces encendidas(o 0)

/*
el algoritmo basicamente se encarga de tomar una poblacion inicial N y la somete a un proceso evolutivo 
donde cada gen es basicamente una probabilidad de que de click o no en alguna casilla, estos genes se 
evaluan como individuos donde el que tiene el mejor fitness pasa a ser elite y es guardado, luego se 
pasa a la siguiente generacion considerando una cruza entre los padres que sobrevivan a un torneo de 
N tamaño y se evalua al final de mejor a menor donde gana el que tenga mejor fitness y calcule la menor 
cantidad de pasos. Para medir el fitness simulamos los clics de cada individuo sobre una copia del 
tablero, contamos cuantas luces quedaron encendidas y cuantos clics uso en total, y aplicamos la formula 
fitness = 100000 - (lucesEncendidas * 1000) - clicsUsados, que castiga fuerte dejar luces prendidas y 
suave usar clics de mas. El mejor individuo de cada generacion se guarda intacto como elite para que 
nunca se pierda (elitismo), y el resto de la nueva poblacion se arma cruzando dos padres elegidos por 
torneo en un punto aleatorio y mutando cada gen del hijo con probabilidad mutationRate. El algoritmo 
corta cuando alguien apaga todas las luces o cuando llega a maxGenerations, y devuelve el mejor 
individuo encontrado, o sea el que resuelve el tablero usando la menor cantidad de pasos. Ese resultado 
se puede pasar despues al Tabu Search para pulirlo localmente.


#Dentro del algoritmo genetico tambien tuvimos el sigueinte problema:

basicamente el algoritmo interactua en el puzzel y evoluciona super bien y rapido bajando la cantidad de luces con caga generacion que pasa pero
siempre llega a un numero maximo de luces que no es capaz de apagar(1 o 2) por lo que este se queda estancando sin importar si aumenta el numero de generaciones
dond este basicamente se llama problema del optimo local debido a si bien el algoritmo es capaz de llegar a la solucion, por la razon de nuestro fitness este no puede 
realizar estos movimientos debido a que perderia puntaje por lo que se queda bloqueado, donde para solucionar este problema utilizamos el tabu search como busqueda local tomando
la mejor generacion del GA para trabajar con ella-

Esta combinacion con el algoritmo de tabu basicamente crea un algortimo memético debido a que el GA aporta a la busqueda global(explorar todas las soluciones a la vez)
y el tabu aporta a la busqueda local(refinar una solucion concreta moviendo un gen a la vez) y la combinacion de ambos crea un algoritmo memetico. 

el loop sigue estos pasos:

1- se crea la poblacion inicial con genes aleatorios (50/50 de click o no click en cada casilla)
2- se evalua cada individuo con la funcion de fitness simulando sus clics sobre una copia del tablero
3- se ordena la poblacion de mejor a peor fitness
4- se guarda al mejor individuo como elite y pasa intacto a la siguiente generacion (elitismo)
5- se eligen dos padres por torneo (el mejor de N al azar gana)
6- se cruzan los padres en un punto aleatorio con probabilidad crossoverRate(50/50)
7- se muta cada gen del hijo con probabilidad mutationRate (invierte 0 por 1 o 1 por 0)
8- se repite el relleno hasta completar la poblacion de tamaño N
9- se revisa si alguien apago todas las luces: si si, se corta y se devuelve
10- si no, se vuelve al paso 2 con la nueva generacion hasta llegar a maxGenerations
*/

public class LightsOutGA : MonoBehaviour
{
    [Header("Configuración del Algoritmo Genético")]
    public int populationSize = 100;// cantidad de individuos que tiene cada gen
    public int maxGenerations = 50;//cantidad maxima de generaciones antes de rendirse 
    [Range(0f, 1f)] public float crossoverRate = 0.80f;//probabilidad de que los padres se crucen (si no se cruzan el hijo es copia del padre A)
    [Range(0f, 1f)] public float mutationRate = 0.05f; //probabilidad de que el gen mute (se invierte) al crear al hijo

    [Header("Pesos del fitnes")]
    public int clickPenaltyWeight = 50;
    [Min(2)]
    public int tournamentSize = 3; //cantidad de individuos que se eligen para decidir quien es el padre

    [Header("Configuración de Selección por Filtro")]
    [Tooltip("Cantidad de elementos superiores a considerar para seleccionar aleatoriamente a la mejor solución o padres en caso de empates.")]
    [Min(1)]
    public int filterPoolSize = 3;
    private LightsOutBoard boardController; // referencia al script que maneja el tablero


    //representacion del cromosoma (individuo)
    public class Individual
    {
        public int[] genes; // 25 celdas para un tablero 5x5
        public int fitness = 0; // puntaje del individuo , cuanto mas alto es mejor la solucion
        public int lightsLeft = 25;// cuantas luces quedaron apagadas despues de los clicks
        public int clicksUsed = 0;//cuantos clicks se uso en total(sirve para premiar soluciones mas cortas)
        public int generationsRun = 0;
        public Individual(int size)
        {
            genes = new int[size];
        }
    }

    void Start()
    {
        boardController = GetComponent<LightsOutBoard>(); //buscamos el script del tablero para leer su estado 
    }

    //metodo que ejecuta la evolucion
    //basicamente se encarga de devolver al mejor individuo 
    public Individual RunEvolution()
    {
        if (boardController == null) return null; //si no hay tablero asignado no se hace nada 

        Debug.Log("--- INICIANDO ALGORITMO GENÉTICO (GA) ---");

        int[] currentBoard = GetFlatBoardState();//se lee el estado del tablero y se pasa a un array plano con 25 posiciones
        List<Individual> population = CreateInitialPopulation();//se crea la primera poblacion completamente al azar
        Individual bestSolution = null; //se guarda el mejor individuo de cada gen

        int totalGenes = boardController.width * boardController.height;

        //bucle principal donde una iteracion = una generacion
        for (int gen = 0; gen < maxGenerations; gen++)
        {
            //evaluamos a todos los individuos contra el tablero actual para saber que tan buenos son 
            foreach (var ind in population)
            {
                EvaluateFitness(ind, currentBoard);
            }

            //ordenamos la poblacion de mejor a peor fitness (siendo el indice 0 el mejor)
            population.Sort((a, b) => b.fitness.CompareTo(a.fitness));
            //bestSolution = population[0];
            bestSolution = GetFilteredBestSolution(population);

            //imprimimos cada 10 generaciones el estado actual (o si ya encontramos la solucion)
            if (gen % 10 == 0 || bestSolution.lightsLeft == 0)
            {
                Debug.Log($"[GA] Gen {gen} | Luces: {bestSolution.lightsLeft} | Pasos: {bestSolution.clicksUsed} | Fitness: {bestSolution.fitness}");
            }

            //si el mejor invividuo encontro la solucion, se termina todo
            if (bestSolution.lightsLeft == 0)
            {
                Debug.Log($"[GA] ¡SOLUCIÓN ENCONTRADA en Gen {gen} con {bestSolution.clicksUsed} pasos!");
                break;
            }

            List<Individual> newPopulation = new List<Individual>();//si no hay solucion todavia se arma la siguiente gen
            //newPopulation.Add(population[0]); // aplicamos elitismo con el mejor individuo de la poblacion y lo guardamos para no perderlo
            newPopulation.Add(CopyIndividual(bestSolution, totalGenes));
            //rellenamos la nueva poblacion hasta llegar al tamaño definido
            while (newPopulation.Count < populationSize)
            {
                //elegimos dos padres por torneo (basicamente el mejor de N al azar gana)
                Individual parentA = TournamentSelection(population);
                Individual parentB = TournamentSelection(population);
                Individual child = new Individual(totalGenes);

                //segun la prob de cruce, mezcamos los  genes de ambos padres o copiamos al padre A si no se aplica
                if (Random.value < crossoverRate)
                {
                    child = Crossover(parentA, parentB, totalGenes);
                }
                else
                {
                    
                    for (int i = 0; i < totalGenes; i++) child.genes[i] = parentA.genes[i];
                }

                Mutate(child, totalGenes);//aplicamos mutacion al hijo(puede que alguno)
                newPopulation.Add(child);
            }
            //la nueva poblacion reemplaza a la anterior para la siguiente gen
            population = newPopulation;
            bestSolution.generationsRun = gen + 1;
        }

        

        Debug.Log("--- FIN DEL ALGORITMO GENÉTICO ---");
        return bestSolution;// deveolvemos la mejor solucion que hayamos encontrado en todo el proceso
    }

    private Individual GetFilteredBestSolution(List<Individual> population)
    {
        if (population == null || population.Count == 0) return null;

        // Limitamos el tamaño del pool al tamaño actual de la población
        int poolLimit = Mathf.Clamp(filterPoolSize, 1, population.Count);

        // Buscamos cuántos individuos tienen exactamente el mismo fitness máximo o están dentro del pool
        int topFitness = population[0].fitness;
        List<Individual> candidates = new List<Individual>();

        for (int i = 0; i < poolLimit; i++)
        {
            // Opcional: podemos incluir a todos los que tengan el mismo fitness top o simplemente los primeros N
            candidates.Add(population[i]);
        }

        // Seleccionamos uno al azar de esos mejores candidatos para romper empates de manera justa
        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    // FUNCIÓN HEURÍSTICA / FITNESS 
    //basicamente evalua que tan buena es la solucion de un individuo donde a mayor fitness es mejor
    private void EvaluateFitness(Individual ind, int[] baseBoard)
    {
        //clonamos el tablero base para no modificar el real mientras lo simulamos
        int[] simulatedBoard = (int[])baseBoard.Clone();
        ind.clicksUsed = 0;
        int totalGenes = ind.genes.Length;
        // Aplicar los clics del individuo al tablero simulado
        for (int i = 0; i < totalGenes; i++)
        {
            if (ind.genes[i] == 1)
            {
                SimulateClick(simulatedBoard, i);
                ind.clicksUsed++;
            }
        }

        // Contar cuántas luces quedaron encendidas luego de aplicar los clicks
        ind.lightsLeft = 0;
        foreach (int light in simulatedBoard)
        {
            if (light == 1) ind.lightsLeft++;
        }

        // El calculo del fitness es basicamente:
        // -100 puntos por cada luz encendida
        //  se penaliza por cada click extra restando una cantidad que nosotros definamos  para premiar la eficacia
        ind.fitness = 100000 - (ind.lightsLeft * 1000) - (ind.clicksUsed * clickPenaltyWeight);
    }

    // OPERADORES GENÉTICOS (Inicialización, Cruce y Mutación)
    // basicamente el metodo arma la primera poblacion con genes completamente aleatorios (50% de click por cada celda)
    private List<Individual> CreateInitialPopulation()
    {
        List<Individual> pop = new List<Individual>();
        int totalGenes = boardController.width * boardController.height;
        for (int i = 0; i < populationSize; i++)
        {
            Individual ind = new Individual(totalGenes);
            for (int j = 0; j < totalGenes; j++)
            {
                ind.genes[j] = Random.value > 0.5f ? 1 : 0; // 50% de probababilidad de que el gen sea 1 click
            }
            pop.Add(ind);
        }
        return pop;
    }

    //metodo de seleccion por torneo donde basicamente:
    //se eligen N individuos al azar y se devuelve al que tenga mejor fitness
    private Individual TournamentSelection(List<Individual> population)
    {
        Individual best = null;
        for (int i = 0; i < tournamentSize; i++)
        {
            int randomIndex = Random.Range(0, population.Count);
            Individual candidate = population[randomIndex];
            if (best == null || candidate.fitness > best.fitness)
            {
                best = candidate;
            }
        }
        return best;
    }

    //metodo de cruce que basicamente corta los genes en una posicion aleatoria y mezcla al padre a con el b
    // basicamente se revisa cada celda y se tira una moneda (50/50), si sale cara el hijo copia al padre A en esa casilla y si sale cruz copia al padre B para asi
    //combinar las estrategias de ambos padres para poder apagar las luces del tablero
    // enfocandolo de esta forma y quitandole el "corte" evitamos que la jugada se rompa ya que con el corte la mitad de arriba con la de abajo rompia las jugadas
    private Individual Crossover(Individual parentA, Individual parentB, int totalGenes)
    {
        Individual child = new Individual(totalGenes);

        for (int i = 0; i < totalGenes; i++)
        {
            // 50% de probabilidad de heredar el gen del padre A o del padre B
            if (Random.value < 0.5f)
            {
                child.genes[i] = parentA.genes[i];
            }
            else
            {
                child.genes[i] = parentB.genes[i];
            }
        }
        return child;
    }

    //el metodo de mutacion basiscamente recorre todos los genes y con la probabilidad de mutationrate invierte cada uno
    //# invertir: basicamente invierte la seleccion 0->1 / 1-> 0
    private void Mutate(Individual ind, int totalGenes)
    {
        for (int i = 0; i < totalGenes; i++)
        {
            if (Random.value < mutationRate)
            {
                ind.genes[i] = 1 - ind.genes[i]; // Cambia 0 por 1 o 1 por 0
            }
        }
    }

    // metodo de matrix auxiliar
    // basicamente convierte la matriz de 5x5 del tablero real en un array plano de 25 posiciones
    private int[] GetFlatBoardState()
    {
        int width = boardController.width;
        int height = boardController.height;
        int[] flat = new int[width * height];
        int index = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flat[index++] = boardController.GetState(x, y);
            }
        }
        return flat;
    }

    //metodo que simula un clic en una celda del tablero plano (con indice de 0 a 24) y afecta a sus vecinos
    private void SimulateClick(int[] board, int index)
    {
        int width = boardController.width;
        int height = boardController.height;
        int x = index % width;
        int y = index / width;

        //investimos la celda y sus 4 vecinos directos igual que en el juego real
        ToggleFlat(board, x, y, width, height);     // Centro
        ToggleFlat(board, x + 1, y, width, height); // Derecha
        ToggleFlat(board, x - 1, y, width, height); // Izquierda
        ToggleFlat(board, x, y + 1, width, height); // Arriba
        ToggleFlat(board, x, y - 1, width, height); // Abajo
    }

    //metodo auxiliar que invierte una celda del tablero plano validando los limites
    private void ToggleFlat(int[] board, int x, int y, int width, int height)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            int index = y * width + x;
            board[index] = 1 - board[index];
        }
    }
    private Individual CopyIndividual(Individual source, int totalGenes)
    {
        Individual copy = new Individual(totalGenes);
        for (int i = 0; i < totalGenes; i++)
        {
            copy.genes[i] = source.genes[i];
        }
        copy.fitness = source.fitness;
        copy.lightsLeft = source.lightsLeft;
        copy.clicksUsed = source.clicksUsed;
        return copy;
    }
}