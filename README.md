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
2. **Random Walk (Caminata Aleatoria):** Actúa como el agente de distribución. En lugar de colocar vegetación mediante una cuadrícula estricta, el Random Walk traza senderos y zonas de concentración, leyendo las alturas del terreno para plantar "semillas" de manera orgánica según probabilidades definidas. Además, cuenta con reglas de entorno, como evitar la generación de vegetación en los picos más altos (limitando el crecimiento en las cimas nevadas de las montañas o en las zonas más elevadas del fondo marino).
3. **L-Systems (Sistemas de Lindenmayer):** Encargado de la generación geométrica de la botánica. Lee las posiciones dejadas por el Random Walk e interpreta una serie de axiomas y reglas de reescritura paralela (Turtle Graphics 3D) para construir modelos tridimensionales únicos de pinos o corales ramificados.

---

## Instrucciones Básicas de Ejecución y Configuración

Para poner en marcha el proyecto (tanto en el Editor como para su posterior exportación a **itch.io**), sigue estos pasos:

### 1. Configuración de Componentes en la Escena
1. Abre la escena principal del proyecto en Unity.
2. Agrega un `GameObject` a la escena y asígnale el componente `TerrainGenerator`.
3. Agrega dos `GameObjects` y asígnales el componente `LSystemTreeGenerator`. Configura los axiomas y reglas de uno para generar vegetación de montaña (árboles) y los del otro para generar vegetación submarina (corales).
4. Selecciona el `GameObject` que contiene el script `ProceduralWorldManager` y, en el Inspector, asigna las referencias de los componentes creados anteriormente.

### 2. Configuración de la Cámara Libre (`FreeFlyCamera`)
1. Selecciona tu **Main Camera** en la Jerarquía de Unity.
2. Añade el script de control de vuelo libre (`FreeFlyCamera`) como un componente de la cámara para permitir la exploración fluida del entorno generado.

### 3. Configuración de la Interfaz de Usuario (UI) y Contenedor
1. En tu **Canvas**, crea un objeto vacío (Empty GameObject) o Panel y agrupa dentro de él todos los elementos de texto, *Inputs* y botones de configuración para utilizarlo como contenedor principal.
2. Crea un botón independiente fuera de ese contenedor que servirá para alternar la visibilidad de la interfaz.
3. Crea un `GameObject` vacío en la escena (puedes llamarlo `UI_Controller`), asígnale el script **`RuntimeUIManager`** y arrastra las referencias correspondientes en su Inspector:
   * El `ProceduralWorldManager` y la `Main Camera` (para el foco automático al generar).
   * Los campos de texto (*Input Fields*), el *Slider* de probabilidad con su respectivo texto indicador, y los botones de acción (Montaña, Mar y Limpiar).
   * El contenedor de la UI y el botón de alternar visibilidad (*Toggle*).

### 4. Ejecución y Controles
1. Puedes generar los mundos directamente desde el Inspector usando los botones del `ProceduralWorldManager` o ejecutando el juego en modo Play / WebGL usando la UI en pantalla.
2. Haz clic en **Generar Montaña** o **Generar Mar** para crear el entorno. La cámara se moverá automáticamente al centro del mapa.
3. **Controles de exploración:** Usa **WASD** para moverte, **Q / E** para bajar o subir, **Shift** para ir más rápido, y mantén presionado el **Clic Derecho** para rotar la vista libremente con el ratón.
4. Utiliza el botón **Ocultar/Mostrar** de la interfaz para alternar una vista limpia del paisaje.

---

## Principales Parámetros Configurables

El proyecto es altamente personalizable tanto desde el Inspector como desde la interfaz en tiempo real. Los parámetros clave son:

### 1. Control de Generación Global
* **Seed (Semilla):** El núcleo del determinismo del proyecto. Si se establece en `-1`, el sistema elegirá una semilla aleatoria, generando mundos únicos en cada clic. Si se establece un valor numérico específico (ej. `12345`), el terreno y el recorrido exacto del Random Walk serán idénticos en cada generación.
* **Terrain Width / Length (Ancho y Largo):** Dimensiones métricas del terreno generado en el mundo 3D.

### 2. Parámetros del Random Walk
* **Walk Iterations:** Cantidad de caminatas independientes que se iniciarán. Valores bajos crean senderos aislados, mientras que valores altos generan pequeños bosques o arrecifes densos repartidos por el mapa.
* **Total Steps:** El número de pasos (casillas) que el agente avanzará en cada iteración antes de detenerse.
* **Tree Spawn Chance:** Probabilidad (manejada mediante un *Slider* en tiempo real de 0.0 a 1.0) de que el agente instancie la flora en su posición actual durante la caminata.
* **Min Distance (Distancia Mínima):** Control espacial que asegura una separación métrica mínima entre cada árbol o coral para evitar superposiciones de mallas.
* **Límite de Altitud (Interno):** En el bioma de bosque y mar, la vegetación está restringida para no generarse en el 30% superior del mapa, simulando líneas de nieve o zonas altas del relieve.

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
