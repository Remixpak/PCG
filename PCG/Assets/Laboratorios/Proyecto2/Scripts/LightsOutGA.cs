using System.Collections.Generic;
using UnityEngine;


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
6- se cruzan los padres en un punto aleatorio con probabilidad crossoverRate
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

    [Min(2)]
    public int tournamentSize = 3; //cantidad de individuos que se eligen para decidir quien es el padre

    private LightsOutBoard boardController; // referencia al script que maneja el tablero


    //representacion del cromosoma (individuo)
    public class Individual
    {
        public int[] genes = new int[25]; // 25 celdas para un tablero 5x5
        public int fitness = 0; // puntaje del individuo , cuanto mas alto es mejor la solucion
        public int lightsLeft = 25;// cuantas luces quedaron apagadas despues de los clicks
        public int clicksUsed = 0;//cuantos clicks se uso en total(sirve para premiar soluciones mas cortas)
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
            bestSolution = population[0];


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
            newPopulation.Add(population[0]); // aplicamos elitismo con el mejor individuo de la poblacion y lo guardamos para no perderlo

            //rellenamos la nueva poblacion hasta llegar al tamaño definido
            while (newPopulation.Count < populationSize)
            {
                //elegimos dos padres por torneo (basicamente el mejor de N al azar gana)
                Individual parentA = TournamentSelection(population);
                Individual parentB = TournamentSelection(population);
                Individual child;

                //segun la prob de cruce, mezcamos los  genes de ambos padres o copiamos al padre A si no se aplica
                if (Random.value < crossoverRate)
                {
                    child = Crossover(parentA, parentB);
                }
                else
                {
                    child = new Individual();
                    for (int i = 0; i < 25; i++) child.genes[i] = parentA.genes[i];
                }

                Mutate(child);//aplicamos mutacion al hijo(puede que alguno)
                newPopulation.Add(child);
            }
            //la nueva poblacion reemplaza a la anterior para la siguiente gen
            population = newPopulation;
        }

        Debug.Log("--- FIN DEL ALGORITMO GENÉTICO ---");
        return bestSolution;// deveolvemos la mejor solucion que hayamos encontrado en todo el proceso
    }



    // FUNCIÓN HEURÍSTICA / FITNESS 
    //basicamente evalua que tan buena es la solucion de un individuo donde a mayor fitness es mejor
    private void EvaluateFitness(Individual ind, int[] baseBoard)
    {
        //clonamos el tablero base para no modificar el real mientras lo simulamos
        int[] simulatedBoard = (int[])baseBoard.Clone();
        ind.clicksUsed = 0;

        // Aplicar los clics del individuo al tablero simulado
        for (int i = 0; i < 25; i++)
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
        //  se penaliza por cada click extra restando -1 punto para premiar la eficacia
        ind.fitness = 100000 - (ind.lightsLeft * 1000) - ind.clicksUsed;
    }

    // OPERADORES GENÉTICOS (Inicialización, Cruce y Mutación)
    // basicamente el metodo arma la primera poblacion con genes completamente aleatorios (50% de click por cada celda)
    private List<Individual> CreateInitialPopulation()
    {
        List<Individual> pop = new List<Individual>();
        for (int i = 0; i < populationSize; i++)
        {
            Individual ind = new Individual();
            for (int j = 0; j < 25; j++)
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
    private Individual Crossover(Individual parentA, Individual parentB)
    {
        Individual child = new Individual();
        int crossoverPoint = Random.Range(1, 24); // Punto de corte aleatorio entre 1 a 23 (no es 0 ni 24 para que los padres aporten algo)

        for (int i = 0; i < 25; i++)
        {
            if (i < crossoverPoint)
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
    private void Mutate(Individual ind)
    {
        for (int i = 0; i < 25; i++)
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
        int[] flat = new int[25];
        int index = 0;
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                flat[index++] = boardController.GetState(x, y);
            }
        }
        return flat;
    }

    //metodo que simula un clic en una celda del tablero plano (con indice de 0 a 24) y afecta a sus vecinos
    private void SimulateClick(int[] board, int index)
    {
        int x = index % 5;
        int y = index / 5;

        //investimos la celda y sus 4 vecinos directos igual que en el juego real
        ToggleFlat(board, x, y);     // Centro
        ToggleFlat(board, x + 1, y); // Derecha
        ToggleFlat(board, x - 1, y); // Izquierda
        ToggleFlat(board, x, y + 1); // Arriba
        ToggleFlat(board, x, y - 1); // Abajo
    }

    //metodo auxiliar que invierte una celda del tablero plano validando los limites
    private void ToggleFlat(int[] board, int x, int y)
    {
        if (x >= 0 && x < 5 && y >= 0 && y < 5)
        {
            int index = y * 5 + x;
            board[index] = 1 - board[index];
        }
    }
}