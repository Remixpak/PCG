LABORATORIO - SEARCH-BASED PCG CON POKEMON
VERSION ESTUDIANTE

ARCHIVOS
-------
1. PokemonCSVLoader.cs
2. GeneticAlgorithm.cs
3. EvolutionaryStrategy.cs
4. PokemonTeamController.cs

OBJETIVO GENERAL
----------------
Utilizar un algoritmo evolutivo para generar equipos de seis Pokemon
que cumplan propiedades de poder y diversidad.

El contenido procedural generado NO es un Pokemon nuevo.
El contenido generado es un equipo:

    Team = [Pokemon1, Pokemon2, Pokemon3, Pokemon4, Pokemon5, Pokemon6]

REPRESENTACION
--------------
Cada Pokemon está almacenado dentro del dataset.

Ejemplo:
    dataset[25]  -> Pikachu
    dataset[131] -> Lapras
    dataset[94]  -> Gengar

Por lo tanto:
    genes = [25, 131, 94, 212, 135, 445]

representa un equipo de seis Pokemon.

TRABAJO A IMPLEMENTAR
---------------------
PokemonCSVLoader.cs
    - Implementar Load().

GeneticAlgorithm.cs
    - Implementar Generate()
    - CreateInitialPopulation()
    - TournamentSelection()
    - Crossover()
    - Mutate()

EvolutionaryStrategy.cs
    - Implementar Generate()
    - CreateInitialParents()
    - Mutate()
    - RandomDifferentPokemon()

PokemonTeamController.cs
    - Se entrega implementado.
    - Incluye configuración, fitness, selección del contenido final y UI.

IDEA CENTRAL
------------
Optimizar solamente una estadística podría producir resultados muy
similares entre sí. Por ejemplo, maximizar únicamente BST favorecería
repetidamente Pokemon extremadamente fuertes.

En este laboratorio buscamos diferentes equipos válidos:
    - BST promedio dentro de un rango.
    - Pokemon diferentes.
    - Diversidad de tipos.
    - Diversidad de roles.

Por eso la función de fitness considera tanto PODER como DIVERSIDAD.
