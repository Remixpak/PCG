using System;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryStrategy : MonoBehaviour
{
    [Header("(mu + lambda) Evolution Strategy")]


    /*FUNCIONAMIENTO:

    basicamente este escript implementa una estrategia evolutiva de mu + lambda
    el cual se usa para genera de forma procedural equipos optimos para 6 pokemones(6 genes diferentes)
    que cumplen con condiciones de poder/diversidad

    El sistema crea un conjunto inicial de padres aleatorio y a lo largo de varias generaciones produce decendientes a traves de la mutacion sin cruzamiento
    evalua el fitness de todos los candidadtos y combina padres e hijo en un grupo de competencia donde se seleccionan a los mejores ciclo tras ciclo asegurarndo la supervivencia

    ¿que diferencia tiene con el genetico?

    basicamente este algoritmo selecciona padres al azar, creando lambda hijos mediante mutacion y junta a todos los padres actuales con los hijos en una sola lista combinada
    donde de esta lista los rodena por fitness y los mejores MU sobreviven lo que permite que los padres compitan directamente contra sus hijos

    para hacerlo mas sencillo este no utiliza cruzes si no clonar a los padres y aplicarle mutacion.

    DE FORMA ABREVIADA! => este algoritmo clona a un padre y lo muta haciendo que tome un pokemon con diferente indice para mejorar el equipo

     * Palabras clave
     * 
     * #Gen:  cada una de las 6 posiciones de un equipo guarda el numero de indice de un pokemon en el dataset
     * #Fitness: es basicamente la aptitud que es una puntuacion numerica que mide que tan bueno/poderoso o equilibrado es el equipo
     * #Mutacion: cambio aleatorio donde un pokemon es reemplazado por otro del dataset
     * # MU + LAMBDa : representa la cantidad de padres que sobreviven sumada a la cantidad neuva de hijos que se generan donde compiten por los nuevos puestos

    #Recordatorio que el el algoritmo buscara el mejor equipo que cumpla con el fitness de 1000


    Parametros que maneja el algoritmo

    Mu: cantidad de padres que sobreviven y compiten en cada generacion
    Lambda: cantidad de hijos nuevos que se generan por cada mutacion en cada ciclo
    Generations: numero de ciclos evolutivos que repite el algoritmo
    MutationRate: probabilidada de que un pokemon cambie por otro del dataset


    */
    [Min(1)]// necesitamos asegurar que exista al menos 1 padre que sobreviva en las generaciones
    [SerializeField] private int mu = 15;// fijamos en 15 la cantidad de padres que sobreviven

    [Min(1)]//necesitamos aseugrar que exista al menos 1 hijo generado en el ciclo evolutivo
    [SerializeField] private int lambda = 45; //fijamos en 45 la cantidad de hijos que pueden ser  generados

    [Min(1)]//necesitamos aseugar que exista al menos una generacion
    [SerializeField] private int generations = 80; //fijamos 80 generaciones 

    [Range(0f, 1f)]//fijamos el rango de 0 a 1 de la probabilidad mutacion
    [SerializeField] private float mutationRate = 0.20f;// fijamos la probabilidad en un numero bajo

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    /*
     * mu = cantidad de padres que sobreviven.
     *
     * mu = 15
     * parents.Count = 15
     *
     * parents = [
     *   [25, 131, 94, 212, 135, 445],
     *   [6, 150, 197, 376, 59, 248],
     *   ...
     *   [...] // padre 14
     * ]
     */

    /*
     * lambda = cantidad de hijos generados en cada generación.
     *
     * lambda = 45
     * offspring.Count = 45
     *
     * Cada hijo se obtiene copiando un padre y aplicando mutación.
     */


    /*
     * mutationRate = probabilidad de mutar cada gen.
     *
     * mutationRate = 0.20
     *
     * Cada una de las 6 posiciones tiene 20% de probabilidad
     * de ser reemplazada por otro Pokémon.
     */

    /*
     * ============================================================
     * (mu + lambda) EVOLUTION STRATEGY
     * ============================================================
     *
     * DIFERENCIA PRINCIPAL CON GA
     * ------------------------------------------------------------
     * - NO utilizamos crossover.
     * - Los hijos se crean desde un padre.
     * - La principal fuente de variación es la mutación.
     * - Padres e hijos compiten juntos.
     *
     * PSEUDOCÓDIGO GENERAL
     * ------------------------------------------------------------
     * 1: create mu random parents
     * 2: evaluate parents
     *
     * 3: repeat for N generations:
     * 4:      create empty offspring
     *
     * 5:      repeat lambda times:
     * 6:          choose random parent
     * 7:          child = copy(parent)
     * 8:          mutate(child)
     * 9:          evaluate(child)
     * 10:         add child to offspring
     *
     * 11:     combined = parents + offspring
     * 12:     sort combined by fitness
     * 13:     keep best mu individuals as new parents
     *
     * 14: select generated content from final parents
     *
     * EJEMPLO
     * ------------------------------------------------------------
     * mu = 2
     * lambda = 3
     *
     * parents:
     * A -> fitness 0.80
     * B -> fitness 0.72
     *
     * offspring:
     * C -> fitness 0.88
     * D -> fitness 0.65
     * E -> fitness 0.91
     *
     * combined = [A, B, C, D, E]
     *
     * ordenado:
     * E = 0.91
     * C = 0.88
     * A = 0.80
     * B = 0.72
     * D = 0.65
     *
     * Como mu = 2:
     * newParents = [E, C]
     */

    
    //esta funcion se encarga de ejecutar el ciclo completo de la estrategia evolutiva de mu + lambda
    public PokemonTeamCandidate Generate(
        IReadOnlyList<PokemonData> dataset, 
        PokemonTeamGenerationConfig config)
    {
        //verificamos que el dataset no sea nulo o este vacio antes de continuar
        if (dataset == null || dataset.Count == 0) 
            return null;

        System.Random random = new System.Random(); //inicializamos el generador de numeros aleatorios 

        List<PokemonTeamCandidate> parents = CreateInitialParents(mu, dataset.Count, config, random);//creamos la poblacion inicial de "mu" padres aleatorios

        EvaluatePopulation(parents, dataset, config);//evaluamos el fitness(equilibrio del equipo) de cada uno de los padres iniciales creados 

        //repetimos el ciclo evolutivo durante la cantidad de  generaciones que establezcamos
        for (int gen = 0; gen < generations; gen++)
        {
            List<PokemonTeamCandidate> offspring = new List<PokemonTeamCandidate>();//creamos una lista vacia para almacenar a los hijos generados en la gen

            //repetimos el proceso un numero de "lambda" veces para crear a todos los decendientes 
            for (int i = 0; i < lambda; i++)
            {
                PokemonTeamCandidate randomParent = parents[random.Next(parents.Count)];//seleccionamos un padre de forma aleatoria entre los actuales
                int[] clonedGenes = (int[])randomParent.genes.Clone();//clonamos los genes del padre seleccionado para pasarselos al hijo
                PokemonTeamCandidate child = new PokemonTeamCandidate(randomParent.genes.Length);//instanciamos un nuevo candidadto a hijo indicando su longitud de su equipo(basicamente 6 genes = max equipo total = 1 gen por pokemon en el equipo)
                child.genes = clonedGenes; // asignamos los genes clonados al nuevo hijo

                Mutate(child, dataset.Count, random);//aplicamos la mutacion al hijo para alterar alguno de sus pokemon segun la probabilidad de 0.20 fijada
                //#mutar un pokemon basicamente lo que hace es cambiar su indice por alguno aleatorio del dataset
                

                PokemonTeamFitness.Evaluate(child, dataset, config);//evaluamos el valor del fitness del hijo mutado 
                offspring.Add(child); //agregamos el hijo evaluado a la lista de desendientes 
            }

            List<PokemonTeamCandidate> combined = new List<PokemonTeamCandidate>(parents);//combinamos los padres actuales y los hijos generados en una sola lista
            combined.AddRange(offspring);//añadimos todos los hijos a la lista combinada
            combined.Sort((a, b) => b.fitness.CompareTo(a.fitness)); // ordenamos la lista combinada por fitness de manera descendente donde los mejores van primero
            parents.Clear();//limpiamos la lista de padres par dejar el espacio a los mojones sobrevivimientes 

            //seleccionamos y conservamos unicamente a los mejores "mu" individuos como los nuevos padres
            for (int i = 0; i < Mathf.Min(mu, combined.Count); i++)
            {
                parents.Add(combined[i]);
            }

        }

        // TODO 1: Crear "mu" padres aleatorios.
        // TODO 2: Evaluar padres.
        // TODO 3: Repetir durante "generations".
        //
        // En cada generación:
        // - crear lambda hijos
        // - elegir un padre aleatorio para cada hijo
        // - copiarlo
        // - mutarlo
        // - evaluarlo
        //
        // Después:
        // combined = parents + offspring
        // ordenar combined por fitness
        // conservar solamente los mejores "mu"

        parents.Sort((a, b) => b.fitness.CompareTo(a.fitness)); // ordenamos por ultima vez la lista final de padres para asegurar que el elemento sea el optimo

        return parents[0];//retornamos el mejor candidadto(equipo) encontdado tras todas las generaciones
 
    }

    /*
     * ============================================================
     * CREAR PADRES INICIALES
     * ============================================================
     *
     * mu = 15
     * teamSize = 6
     *
     * parents.Count = 15
     *
     * Cada padre puede verse así:
     * [25, 131, 94, 212, 135, 445]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: create empty parents
     * 2: repeat parentCount times:
     * 3:      create candidate with teamSize genes
     * 4:      assign random dataset index to every gene
     * 5:      add candidate to parents
     * 6: return parents
     */

    //esta funcion se encarga de instanciar y poblar la lista inicial de padres con equipos con indices aleatorios
    private List<PokemonTeamCandidate> CreateInitialParents(
            int parentCount,
            int datasetSize,
            PokemonTeamGenerationConfig config,
            System.Random random)
    {

        List<PokemonTeamCandidate> parents = new List<PokemonTeamCandidate>(); //creamos la lista vacia que contendra los candidatos iniciales

        int teamSize = 6; // definimos el tamaño en 6 ya que es el tamaño dentro de pokemon


        //repetimos el proceso tantas veces como el cantidad de padres(mu) se requiera
        for (int i = 0; i < parentCount; i++)
        {
            // Creamos el candidato pasándole el tamaño del equiupo 
            PokemonTeamCandidate candidate = new PokemonTeamCandidate(teamSize);

            //iteramos por cada una de las 6 posiciones del equipo
            for (int j = 0; j < teamSize; j++)
            {
                // Asignamos el índice aleatorio directamente a la posición de sus genes
                candidate.genes[j] = random.Next(datasetSize);
            }

            parents.Add(candidate);//agregamos el candidato completo a la lista de padres
        }

        return parents;//retornamos la lsita completa de padres iniciales
    }

    /*
     * ============================================================
     * MUTACIÓN
     * ============================================================
     *
     * Padre:
     * [25, 131, 94, 212, 135, 445]
     *
     * Copia:
     * [25, 131, 94, 212, 135, 445]
     *
     * Después de mutar:
     * [25, 131, 700, 212, 135, 445]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: mutated = false
     * 2: for each gene:
     * 3:      if random < mutationRate:
     * 4:          replace gene with another random Pokemon
     * 5:          mutated = true
     *
     * 6: if no gene was mutated:
     * 7:      choose one random gene
     * 8:      replace it with another Pokemon
     *
     * Queremos asegurar que cada hijo tenga al menos una variación.
     */

    //funcion que recorre los genes de un candidato y aplica cambios aleatorios basados en la tasa de mutacion
    private void Mutate(
        PokemonTeamCandidate candidate,
        int datasetSize,
        System.Random random)
    {
        bool mutated = false;//booleano para comprobar si al menos un gen sufrio modificaciones


        //recorremos cada posicion(gen) del equipo pokemon del candidato
        for (int i = 0; i < candidate.genes.Length; i++)
        {
            //geneeramos un numero aleatorio solo si es menor que la probabilidad de mutacion 
            if (random.NextDouble() < mutationRate)
            {
                candidate.genes[i] = RandomDifferentPokemon(candidate.genes[i], datasetSize, random);//remplazamos el pokemon actual por otro indice diferente del dataset
                mutated = true;//marcamos como verdadero que la mutacion ya ocurrio
            }
        }

        //si ningun gen muto firazamos la mutacion en un gen aleatorio para variar al hijo
        if (!mutated && candidate.genes.Length > 0)

        {
            int randomIndex = random.Next(candidate.genes.Length);//seleccionamos un indice de gen aleatorio dentro del equipo
            candidate.genes[randomIndex] = RandomDifferentPokemon(candidate.genes[randomIndex], datasetSize, random);//reemplazamos ese pokemon especifico por uno diferente
        }


    }

    /*
     * Retorna un índice diferente al actual.
     *
     * current = 94
     * datasetSize = 1032
     *
     * resultado posible = 700
     *
     * Debe estar entre 0 y datasetSize - 1
     * y no debe ser igual a current.
     */

    //metodo auxiliar para garantizar que al mutar se elija un pokemon diferente al mutado
    private int RandomDifferentPokemon(
        int current,
        int datasetSize,
        System.Random random)
    {
        if (datasetSize <= 1) //si el dataset tiene 1 o menos elementos se devuelve el mismo
            return current;

        int newIndex;

        //ciclo que se repite hasta encontrar un dice que sea diferente
        do
        {
            newIndex = random.Next(datasetSize);
        } while (newIndex == current);

        return newIndex;//retornamos al indice nuevo
    }

    //metodo para evaluar el fitness de una lista completo de candidatos

    private void EvaluatePopulation(
        List<PokemonTeamCandidate> population,
        IReadOnlyList<PokemonData> dataset,
        PokemonTeamGenerationConfig config)
    {
        //iteramos por cada candidato en la poblacion para calcular su aptitud 
        foreach (PokemonTeamCandidate candidate in population)
        {
            PokemonTeamFitness.Evaluate(candidate, dataset, config);
        }
    }
}
