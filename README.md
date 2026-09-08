# Generador de Biomas Procedurales en Unity

Este proyecto se centra en la Generación Procedural de Contenido (PCG) para crear ecosistemas dinámicos y orgánicos en Unity. El sistema es capaz de generar dos tipos de entornos distintos: un **bioma de montaña** y un **bioma submarino**. 

El flujo de trabajo combina tres potentes algoritmos: primero se genera la topografía tridimensional del terreno, luego un agente explora la superficie para distribuir elementos orgánicamente y, finalmente, se construye la geometría detallada de la flora utilizando gramáticas formales.

### Integrantes
* Francisco Galeno
* Simón Gallardo
* Raúl Sobarzo

---

## Técnicas de PCG Utilizadas

1. **Diamond-Square:** Utilizado para la generación de mapas de altura (*Heightmaps*). Crea la topografía base, generando picos escarpados para las montañas o valles ondulados y suaves para el lecho marino mediante la manipulación de la rugosidad fractal.
2. **Random Walk (Caminata Aleatoria):** Actúa como el agente de distribución. En lugar de colocar vegetación mediante una cuadrícula estricta, el Random Walk traza senderos y zonas de concentración, leyendo las alturas del terreno para plantar "semillas" de manera orgánica según probabilidades definidas. Además, cuenta con reglas de entorno, como evitar la generación de vegetación en los picos más altos de las montañas (límite de altitud).
3. **L-Systems (Sistemas de Lindenmayer):** Encargado de la generación geométrica de la botánica. Lee las posiciones dejadas por el Random Walk e interpreta una serie de axiomas y reglas de reescritura paralela (Turtle Graphics 3D) para construir modelos tridimensionales únicos de pinos o corales ramificados.

---

## Instrucciones Básicas de Ejecución

Gracias al uso de *Custom Editors* en Unity, la generación del entorno se puede realizar directamente desde el Inspector sin necesidad de entrar al modo *Play*.

1. Abre la escena principal del proyecto en Unity.
2. Agrega a la escena un `GameObject` y asígnale el componente `TerrainGenerator`.
3. Agrega a la escena dos `GameObjects` y asígnales el componente `LSystemTreeGenerator`. Configura los axiomas y reglas de uno para generar vegetación de montaña (árboles) y los del otro para generar vegetación submarina (corales).
4. Selecciona el `GameObject` en la jerarquía que contiene el script `ProceduralWorldManager` y, en el Inspector, asigna las referencias de los componentes creados en los pasos anteriores.
5. En la ventana del **Inspector**, desplázate hasta la sección inferior llamada **"Acciones de Generación"**.
6. Haz clic en **Generar Mundo Completo (Terreno + Random Walk + Árboles)** para crear instantáneamente la montaña con sus respectivos bosques.
7. Haz clic en **Generar Mundo Acuático (Terreno + Random Walk + Coral)** para generar el lecho marino. El código ajustará automáticamente los parámetros del terreno (colores y suavidad) para simular el fondo oceánico y plantará corales.
8. Utiliza los botones **Limpiar Terreno con Árboles** o **Limpiar Terreno con Corales** para borrar la topografía y geometría generada de manera segura, preparando la escena para una nueva generación.

---

## Principales Parámetros Configurables

El proyecto es altamente personalizable desde el Inspector del `ProceduralWorldManager`. Los parámetros clave son:

### 1. Control de Generación Global
* **Seed (Semilla):** El núcleo del determinismo del proyecto. Si se establece en `-1`, el sistema elegirá una semilla aleatoria, generando mundos únicos en cada clic. Si se establece un valor numérico específico (ej. `12345`), el terreno y el recorrido exacto del Random Walk serán idénticos en cada generación.

### 2. Parámetros del Random Walk
* **Walk Iterations:** Cantidad de caminatas independientes que se iniciarán. Valores bajos crean senderos aislados, mientras que valores altos generan pequeños bosques o arrecifes densos repartidos por el mapa.
* **Total Steps:** El número de pasos (casillas) que el agente avanzará en cada iteración antes de detenerse.
* **Tree Spawn Chance:** Probabilidad (de 0.0 a 1.0) de que el agente instancie la flora en su posición actual durante la caminata.
* **Límite de Altitud (Interno):** En el bioma de bosque, los árboles están restringidos para no generarse en el 30% superior del mapa, simulando líneas de nieve o falta de oxígeno en cimas.

### 3. Parámetros de Flora (Referencias L-System)
* **Generation Mode:** Define si el crecimiento está restringido a un plano (`TwoD`) o si utiliza guiñada, cabeceo y alabeo para expandirse en todas direcciones (`ThreeD`).
* **Axiom & Rules:** El carácter inicial y el diccionario de reglas gramaticales que determinan la forma estructural de la planta (ej. `F -> F[&+F][^--F][\-F][/+F]`).
* **Iterations:** La cantidad de veces que se aplicará el reemplazo paralelo para hacer crecer el árbol/coral. *(Nota: Mantener entre 3 y 4 en modo 3D para preservar el rendimiento).*
* **Angle:** El ángulo de rotación de las ramas generado en cada ramificación.
* **Segment Length & Branch Radius:** Determinan el largo y grosor de los cilindros que componen la estructura final.

### 4. Parámetros de Terreno (Automáticos)
Al presionar los botones de generación, el script ajusta automáticamente parámetros del algoritmo Diamond-Square como:
* **Diamond Roughness:** Agresividad de los desniveles (alta para montañas, baja para mar).
* **Diamond Roughness Decay:** Suavizado del ruido en los detalles menores.
* **Colores de Altura:** Adaptación visual con paletas asignadas según la elevación (colores oceánicos vs. montañas terrestres).
