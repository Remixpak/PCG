# Lights Out - Algoritmo Memético (GA + Tabu Search)

Este proyecto resuelve el clásico puzzle **Lights Out** utilizando un **Algoritmo Memético**, el cual combina la exploración global de un **Algoritmo Genético (GA)** con el refinamiento local de una **Búsqueda Tabú (Tabu Search)**.

## Integrantes
- Simón Gallardo
- Francisco Galeno
- Raúl Sobarzo

---

## Enlaces
- **Juego Jugable (itch.io):** https://hikidetsu.itch.io/pcg-ligthsout

## Puzzle y Representación Computacional
Se seleccionó el puzzle **Lights Out**. Computacionalmente, representamos tanto el estado del tablero como las soluciones (individuos) a través de un **arreglo unidimensional de 25 enteros** (simulando la grilla de 5x5). 
Cada índice representa una celda específica del tablero, donde el valor numérico (0 o 1) indica si se debe efectuar la acción de "clic" en esa posición. Esta representación binaria permite que los algoritmos crucen, muten y evalúen las soluciones de forma directa y matemática. Además, la generación del nivel se hace mediante la técnica **Backward from Goal State**, garantizando siempre tableros con solución.

## Técnicas de Inteligencia Artificial Utilizadas

1. **Algoritmo Genético (GA) - Exploración Global:**
   - **Población Inicial:** Generación aleatoria de soluciones candidatas (genes 0 o 1).
   - **Fitness:** Función de evaluación que premia apagar luces y penaliza clics innecesarios.
   - **Selección:** Torneo para elegir a los mejores padres, aplicando elitismo.
   - **Cruce Uniforme (Uniform Crossover):** A diferencia del cruce de un punto, el cruce uniforme evalúa celda por celda en la matriz 2D (5x5), heredando con 50% de probabilidad de cada padre. Esto evita la destrucción de estrategias espaciales al cruzar tableros.
   - **Mutación:** Probabilidad de invertir un gen para mantener diversidad.
   - **Limitación (Óptimo Local):** El GA es excelente para reducir luces rápidamente, pero puede atascarse en 1 o 2 luces (óptimo local) debido a las penalizaciones de fitness por clics extras necesarios para resolver el tablero completo.

2. **Tabu Search - Refinamiento Local:**
   - **Objetivo:** Tomar la mejor solución incompleta del GA y escapar del óptimo local.
   - **Tabú de Acción:** Implementamos un arreglo donde cada índice representa una celda. Si se hace clic, esa celda queda bloqueada ("tabú") durante `Tabu Tenure` iteraciones.
   - **Solución al estancamiento:** Al usar un tabú de *acción* en lugar de *estado*, forzamos al algoritmo a explorar nuevos caminos y aceptar soluciones temporalmente peores, evitando que deshaga sus propios movimientos en un bucle infinito.
   - **Criterio de Aspiración:** Permite ignorar la restricción tabú si el movimiento resulta en un "mejor global" nunca antes visto.

---

## Instrucciones Básicas de Ejecución y Configuración

El sistema fue desarrollado en Unity y cuenta con una interfaz dinámica para configurar, ejecutar y visualizar los algoritmos.

### 1. Configuración de la Interfaz (UI)
- Todo se gestiona desde el componente `LightsOutUIManager`.
- Al ejecutar la escena, se desplegará un panel lateral derecho dividido en secciones.
- Puedes ocultar información con los "Tooltips" o alternar pestañas.

### 2. Configuración del Tablero
Desde la sección "Configuración del Tablero", puedes definir:
- **Tamaño de Grilla:** Por defecto 5x5.
- **Pasos Hacia Atrás:** Clics inversos para generar el tablero y asegurar que tenga solución.
- **Semilla (Seed):** Permite fijar un tablero específico o usar una aleatoria.

### 3. Ejecución de los Algoritmos
En la parte inferior de la pantalla encontrarás los botones de acción:
1. **Generar Nuevo Tablero:** Crea una nueva instancia del puzzle.
2. **Resolver con Algoritmo Genético (GA):** Ejecuta solo la búsqueda global. Mostrará un popup evidenciando si encontró la solución o si cayó en un óptimo local.
3. **Resolver con GA + Tabu Search:** Ejecuta la secuencia completa del algoritmo memético. El GA entregará su mejor individuo al Tabu Search para que este culmine la resolución (llegar a 0 luces).

---

## Principales Parámetros Configurables

Puedes ajustar el comportamiento de los algoritmos en tiempo real desde la UI:

### Algoritmo Genético (GA)
- **Tamaño de Población:** Cantidad de individuos evaluados por generación.
- **Generaciones Máximas:** Límite de ciclos evolutivos.
- **Tasa de Cruce (Crossover):** Probabilidad de heredar genes de distintos padres.
- **Tasa de Mutación:** Probabilidad de invertir aleatoriamente un gen.
- **Penalización por Clic:** Resta puntos de fitness para premiar soluciones cortas.
- **Tamaño de Torneo / Pool de Filtro:** Controlan la agresividad de la selección de padres.

### Búsqueda Local (Tabu Search)
- **Tabu Tenure:** La cantidad de iteraciones (turnos) que una celda específica permanecerá bloqueada después de ser alterada, obligando al sistema a probar nuevas celdas.
- **Iteraciones Máximas:** Límite de intentos de la búsqueda local para encontrar el 0 absoluto antes de rendirse.
