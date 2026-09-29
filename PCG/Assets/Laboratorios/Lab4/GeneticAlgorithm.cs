using System;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm : MonoBehaviour
{
    [Header("Genetic Algorithm")]

    //Movi los atributos del script para dejarlo mas ordenados poniendolos al inicio pero igual quedan los comentarios de cada uno abajo

    [Min(2)]//fijamos el valor en 2 minimo ya que necesitamos por lo menos 2 individuos para poder cruzarlos y generar descendencia.
    [SerializeField] private int populationSize = 50;//fijamos el valor en 50 individuos para la población inicial.

    [Min(1)]//fijamos el valor en 1 minimo ya que necesitamos por lo menos 1 generación para poder evaluar la población inicial.
    [SerializeField] private int generations = 80;//fijamos el valor en 80 generaciones para poder evaluar la población inicial y generar nuevas poblaciones.

    [Range(0f, 1f)]//fijamos el rango entre 0 y1 para la probabilidad de cruzar dos padres.
    [SerializeField] private float crossoverRate = 0.80f;//fijamos el valor en 0.80 para la probabilidad de cruzar dos padres.

    [Range(0f, 1f)]//fijamos el rango entre 0 y1 para la probabilidad de mutar un individuo.
    [SerializeField] private float mutationRate = 0.12f;//fijamos el valor en 0.12 para la probabilidad de mutar un individuo.

    [Min(2)]//necesitamos minimo 2 individuos para poder seleccionar un padre, por lo que fijamos el valor en 2.
    [SerializeField] private int tournamentSize = 3;//fijamos el valor en 3 para la cantidad de individuos que compiten para seleccionar un padre.

    [Min(0)]//fijamos el valor en 0 para la cantidad de individuos que pasan directamente a la siguiente generación.    
    [SerializeField] private int elitism = 2;//fijamos el valor en 2 para la cantidad de individuos que pasan directamente a la siguiente generación.

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;


    /*
     * populationSize = cantidad de individuos de la población.
     *
     * Si populationSize = 50 y teamSize = 6:
     *
     * population = [
     *   [25, 131, 94, 212, 135, 445],   // individuo 0
     *   [6, 150, 197, 376, 59, 248],     // individuo 1
     *   [143, 448, 130, 214, 94, 445],   // individuo 2
     *   ...
     *   [...]                              // individuo 49
     * ]
     *
     * population.Count = 50
     * population[i].genes.Length = 6
     *
     * Cada gen es el índice de un Pokémon dentro del dataset.
     */

    /*
     * generations = cantidad de veces que repetimos el ciclo evolutivo.
     *
     * generations = 80
     *
     * Generation 0  -> población inicial
     * Generation 1  -> nueva población
     * ...
     * Generation 80 -> población final
     */

    /*
     * crossoverRate = probabilidad de cruzar dos padres.
     *
     * crossoverRate = 0.80  -> aproximadamente 80%.
     */

    /*
     * mutationRate = probabilidad de reemplazar cada gen.
     *
     * mutationRate = 0.12
     *
     * Cada una de las 6 posiciones del equipo tiene 12%
     * de probabilidad de ser reemplazada por otro Pokémon.
     */

    /*
     * tournamentSize = cantidad de individuos que compiten
     * para seleccionar un padre.
     *
     * tournamentSize = 3
     *
     * candidatos:
     * A -> fitness 0.72
     * B -> fitness 0.91
     * C -> fitness 0.84
     *
     * seleccionado -> B
     */
    /*
     * elitism = mejores individuos que pasan directamente
     * a la siguiente generación.
     *
     * elitism = 2
     *
     * nextPopulation comienza con:
     * [bestIndividual, secondBestIndividual]
     */
    /*
     * ============================================================
     * ALGORITMO GENÉTICO
     * ============================================================
     *
     * PSEUDOCÓDIGO GENERAL
     * ------------------------------------------------------------
     * 1: create random population
     * 2: evaluate every individual
     *
     * 3: repeat for N generations:
     * 4:      sort population by fitness
     * 5:      create empty nextPopulation
     * 6:      copy elite individuals
     *
     * 7:      while nextPopulation is not full:
     * 8:          parentA = tournament selection
     * 9:          parentB = tournament selection
     *
     * 10:         if random < crossoverRate:
     * 11:             child = crossover(parentA, parentB)
     * 12:         else:
     * 13:             child = copy(parentA)
     *
     * 14:         mutate(child)
     * 15:         evaluate(child)
     * 16:         add child to nextPopulation
     *
     * 17:     population = nextPopulation
     *
     * 18: select generated content from final population
     *
     * EJEMPLO DE UNA GENERACIÓN
     * ------------------------------------------------------------
     * populationSize = 4
     *
     * A = [25, 131, 94, 212, 135, 445] fitness 0.92
     * B = [6, 150, 197, 376, 59, 248]   fitness 0.87
     * C = [1, 4, 7, 25, 39, 52]         fitness 0.63
     * D = [10, 11, 12, 13, 14, 15]      fitness 0.55
     *
     * elitism = 1
     *
     * nextPopulation empieza como [A]
     * y luego se completa con hijos hasta volver a tener 4 individuos.
     */
    public PokemonTeamCandidate Generate(
        IReadOnlyList<PokemonData> dataset,
        PokemonTeamGenerationConfig config)
    {
        if (dataset == null || dataset.Count == 0)
            return null;

        System.Random random = new System.Random();


        // TODO 1: Crear population con CreateInitialPopulation(...)
        List<PokemonTeamCandidate> population = CreateInitialPopulation(dataset.Count, config, random);
        // TODO 2: Evaluar la población inicial.
        EvaluatePopulation(population, dataset, config);
        // TODO 3: Repetir el ciclo evolutivo durante "generations".
        for (int g = 0; g < generations; g++)
        {
            // Ordenar población por fitness de mayor a menor
            population.Sort((a, b) => b.fitness.CompareTo(a.fitness));

            if (logProgress)
            {
                Debug.Log($"[GeneticAlgorithm] Gen {g}: Mejor Fitness = {population[0].fitness:F4}");
            }

            List<PokemonTeamCandidate> nextPopulation = new List<PokemonTeamCandidate>();

            // Conservar elitismo
            int actualElitism = Math.Min(elitism, populationSize);
            for (int i = 0; i < actualElitism; i++)
            {
                // Copia simple o referencia directa (los mejores pasan intactos)
                nextPopulation.Add(population[i]);
            }

            // Completar la nueva población
            while (nextPopulation.Count < populationSize)
            {
                PokemonTeamCandidate parentA = TournamentSelection(population, random);
                PokemonTeamCandidate parentB = TournamentSelection(population, random);

                PokemonTeamCandidate child;

                if (random.NextDouble() < crossoverRate)
                {
                    child = Crossover(parentA, parentB, random);
                }
                else
                {
                    // Copiar directamente los genes del Padre A
                    child = new PokemonTeamCandidate(parentA.genes.Length);
                    for (int i = 0; i < parentA.genes.Length; i++)
                    {
                        child.genes[i] = parentA.genes[i];
                    }
                    
                }

                // Mutar y evaluar al hijo
                Mutate(child, dataset.Count, random);
                PokemonTeamFitness.Evaluate(child, dataset, config);

                nextPopulation.Add(child);
            }

            population = nextPopulation;
        }
        //
        // Dentro de cada generación:
        // - ordenar por fitness
        // - conservar elite
        // - seleccionar padres
        // - realizar crossover
        // - mutar
        // - evaluar hijos
        // - completar nextPopulation
        // - reemplazar population

        /*Debug.LogWarning(
            "[GeneticAlgorithm] TODO: implementar Generate()."
        );*/
        population.Sort((a, b) => b.fitness.CompareTo(a.fitness));
        return population[0];


    }

    /*
     * ============================================================
     * CREAR POBLACIÓN INICIAL
     * ============================================================
     *
     * populationSize = 50
     * teamSize = 6
     * datasetSize = 1032
     *
     * Un individuo posible:
     * [25, 131, 94, 212, 135, 445]
     *
     * Cada valor debe estar entre 0 y datasetSize - 1.
     *
     * Resultado:
     * population.Count = 50
     * population[i].genes.Length = 6
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: create empty population
     * 2: repeat populationSize times:
     * 3:      create candidate with teamSize genes
     * 4:      for each gene:
     * 5:          assign random dataset index
     * 6:      add candidate to population
     * 7: return population
     */
    private List<PokemonTeamCandidate> CreateInitialPopulation(
        int datasetSize,
        PokemonTeamGenerationConfig config,
        System.Random random)
    {
        //Crea una lista de datasetSize cantidad de candidatos
        List<PokemonTeamCandidate> population = new List<PokemonTeamCandidate>();

        for (int i = 0; i < populationSize; i++)
        {
            PokemonTeamCandidate candidate = new PokemonTeamCandidate(config.teamSize);
            
            for (int j = 0; j < config.teamSize; j++)
            {
                candidate.genes[j] = random.Next(0, datasetSize);
            }

            population.Add(candidate);
        }

        return population;
    }

    /*
     * ============================================================
     * TOURNAMENT SELECTION
     * ============================================================
     *
     * tournamentSize = 3
     *
     * candidate A -> fitness 0.62
     * candidate B -> fitness 0.91
     * candidate C -> fitness 0.78
     *
     * ganador -> candidate B
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: best = null
     * 2: repeat tournamentSize times:
     * 3:      choose random candidate from population
     * 4:      if best is null OR candidate fitness > best fitness:
     * 5:          best = candidate
     * 6: return best
     */
    private PokemonTeamCandidate TournamentSelection(
        List<PokemonTeamCandidate> population,
        System.Random random)
    {
        //toma una cantidad tournamentSize de candidatos y evalua su funcion fitness para saber cual es el mejor
        //luego retorna el mas optimo
        PokemonTeamCandidate best = null;

        for (int i = 0; i < tournamentSize; i++)
        {
            int randomIndex = random.Next(0, population.Count);
            PokemonTeamCandidate candidate = population[randomIndex];

            if (best == null || candidate.fitness > best.fitness)
            {
                best = candidate;
            }
        }

        return best;
    }

    /*
     * ============================================================
     * CROSSOVER
     * ============================================================
     *
     * parentA:
     * [25, 131, 94 | 212, 135, 445]
     *
     * parentB:
     * [6, 150, 197 | 376, 59, 248]
     *
     * crossoverPoint = 3
     *
     * child:
     * [25, 131, 94 | 376, 59, 248]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: create empty child
     * 2: choose crossoverPoint between 1 and geneCount - 1
     * 3: for each gene position:
     * 4:      if position < crossoverPoint:
     * 5:          copy gene from parentA
     * 6:      else:
     * 7:          copy gene from parentB
     * 8: return child
     */
    private PokemonTeamCandidate Crossover(
        PokemonTeamCandidate parentA,
        PokemonTeamCandidate parentB,
        System.Random random)
    {
        int geneCount = parentA.genes.Length;
        int[] childGenes = new int[geneCount];

        // Punto de corte entre 1 y geneCount - 1 para garantizar que reciba genes de ambos padres
        int crossoverPoint = random.Next(1, geneCount);

        for (int i = 0; i < geneCount; i++)
        {
            if (i < crossoverPoint)
            {
                childGenes[i] = parentA.genes[i];
            }
            else
            {
                childGenes[i] = parentB.genes[i];
            }
        }

        PokemonTeamCandidate child = new PokemonTeamCandidate(geneCount);

        for (int i = 0; i < geneCount; i++)
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

    /*
     * ============================================================
     * MUTACIÓN
     * ============================================================
     *
     * mutationRate = 0.12
     *
     * Antes:
     * [25, 131, 94, 212, 135, 445]
     *
     * Si muta la posición 3:
     * [25, 131, 94, 700, 135, 445]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: for each gene:
     * 2:      generate random value between 0 and 1
     * 3:      if random value < mutationRate:
     * 4:          replace gene with random dataset index
     */
    private void Mutate(
        PokemonTeamCandidate candidate,
        int datasetSize,
        System.Random random)
    {
        /*
        Revisa cada gen de manera individual.
        Con una probabilidad equivalente a mutationRate (12%), reemplaza el Pokémon de esa casilla por cualquier otro del dataset generado al azar.
        */
        for (int i = 0; i < candidate.genes.Length; i++)
        {
            if (random.NextDouble() < mutationRate)
            {
                candidate.genes[i] = random.Next(0, datasetSize);
            }
        }
    }

    private void EvaluatePopulation(
        List<PokemonTeamCandidate> population,
        IReadOnlyList<PokemonData> dataset,
        PokemonTeamGenerationConfig config)
    {
        foreach (PokemonTeamCandidate candidate in population)
        {
            PokemonTeamFitness.Evaluate(candidate, dataset, config);
        }
    }
}
