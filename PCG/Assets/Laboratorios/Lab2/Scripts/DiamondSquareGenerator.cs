using UnityEngine;

public static class DiamondSquareGenerator
{
    // -------------------------------------------------------------------------
    // DIAMOND-SQUARE
    // -------------------------------------------------------------------------
    //
    // Objetivo:
    // Generar un terreno mediante subdivisiones sucesivas, agregando nuevas
    // alturas a escalas cada vez menores.
    //
    // A diferencia de los métodos anteriores, Diamond-Square no calcula cada
    // punto de manera independiente. El algoritmo comienza con cuatro valores
    // en las esquinas y va completando progresivamente los puntos faltantes.
    //
    // La resolución final debe cumplir:
    //
    //      resolution = 2^iterations + 1
    //
    // Por ejemplo:
    //
    //      iterations = 5  -> resolution = 33
    //      iterations = 6  -> resolution = 65
    //      iterations = 7  -> resolution = 129
    //
    //
    // -------------------------------------------------------------------------
    // ESCALA DE TRABAJO
    // -------------------------------------------------------------------------
    //
    // stepSize representa el tamaño de las regiones que se están procesando
    // durante una iteración.
    //
    // Al comenzar:
    //
    //      stepSize = resolution - 1
    //
    // Por ejemplo, para una resolución de 9:
    //
    //      stepSize = 8
    //
    // Inicialmente solo conocemos las cuatro esquinas:
    //
    //      O-------------------------------O
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      O-------------------------------O
    //
    // Después de ejecutar Diamond Step y Square Step se reduce:
    //
    //      stepSize = stepSize / 2
    //
    // Por lo tanto:
    //
    //      8 -> 4 -> 2 -> 1
    //
    // Cada iteración trabaja así sobre regiones más pequeñas del terreno.
    //
    //
    // -------------------------------------------------------------------------
    // ROUGHNESS
    // -------------------------------------------------------------------------
    //
    // Las nuevas alturas no corresponden solamente al promedio de sus vecinos.
    // También se añade una perturbación aleatoria:
    //
    //      nuevaAltura = promedio + perturbación
    //
    // donde la perturbación pertenece aproximadamente al intervalo:
    //
    //      [-roughness, +roughness]
    //
    // En cada nueva escala se reduce su magnitud mediante:
    //
    //      currentRoughness =
    //          currentRoughness * roughnessDecay
    //
    // Ejemplo:
    //
    //      roughness = 0.40
    //      roughnessDecay = 0.50
    //
    //      Iteración 1 -> +/- 0.40
    //      Iteración 2 -> +/- 0.20
    //      Iteración 3 -> +/- 0.10
    //      Iteración 4 -> +/- 0.05
    //
    // De esta forma, las primeras iteraciones generan características de gran
    // escala y las posteriores añaden detalles cada vez más pequeños.
    //
    //
    // -------------------------------------------------------------------------
    // PROCESO GENERAL
    // -------------------------------------------------------------------------
    //
    // En cada iteración:
    //
    //      1. Ejecutar Diamond Step.
    //      2. Ejecutar Square Step.
    //      3. Reducir stepSize a la mitad.
    //      4. Reducir currentRoughness utilizando roughnessDecay.
    //
    // El proceso termina cuando stepSize llega a 1.
    //
    public static float[,] GenerateHeightmap(
        int iterations,
        int seed,
        float roughness,
        float roughnessDecay)
    {
        int resolution = (1 << iterations) + 1;

        float[,] heights = new float[resolution, resolution];
        System.Random random = new System.Random(seed);

        // Las cuatro esquinas iniciales se entregan implementadas.
        heights[0, 0] = (float)random.NextDouble();
        heights[0, resolution - 1] = (float)random.NextDouble();
        heights[resolution - 1, 0] = (float)random.NextDouble();
        heights[resolution - 1, resolution - 1] = (float)random.NextDouble();

        int stepSize = resolution - 1;
        //stepSize representa el tamaño de las regiones que se están procesando durante una iteración.
        float currentRoughness = roughness;

        while (stepSize > 1)
        {
            //# declaramos las funciones de diamod step 
            //height: matriz bidimensional que hace de mapa de alturas del terreno (basicamente la topografia)
            //setpsize: es el numero que define el paso o distancia entre dos puntos en la iteracion y que se va reduciendo en cada ciclo y que dicta el detalle 
            //currentRoughness: determina la fuerza/magnitud de la perturbacion aleatoria para calcular las alturas, esta se multiplica por el decay en cada iteracion para ir disminuyendo y se encarga de formar cordilleras
            //roughnessDecay: es basicamente el suavizado del terreno a medida que el algoritmo agrega detalle mas pequeños


            // # basicamente el algoritmo funciona de forma en que el diamond step se encarga de tomar un cuadrado de la matriz unidimensional y y calculamos el centro de este mediante los vertices del cuadrado dando saltos
            // donde luego de calcular el punto de equilibrio utiliando los 4 vertices del cuadrado se genera una perturbacion aleatoria(hacia arriba/abajo) para elevar o hundir ese cuadrado y generar un "monte"
            //se guarda la altura generado de ese cuadrado para asegurarse que este normalizado ([0,1])
            //luego la funcion de square step se encarga de tomar ese cuadrado de la matriz y crea un patron como un tablero de ajedrez donde recorre cada esquina para verificar si hay espacios que rellenar
            //al realizar el calculo este hace saltos mas o menos grandes dejando 4 cuadrantes dentro del cuadrado como si fuera una cruz donde los saltos se encargan si este espacio a rellenar esta cerca de un borde
            //si esta cerca de un borde no lo considera para no salirse del mapa, luego de calcular la  cruz se le agrega cierta imperfeccion al monte creado para hacerlo mas realista y se guarda la altura final


            DiamondStep(heights, stepSize, currentRoughness, random); // Ejecutar DiamondStep usando los parametros: heights, stepSize, currentRoughness y random.
            SquareStep(heights, stepSize, currentRoughness, random); // Ejecutar SquareStep usando los mismos parametros: heights, stepSize, currentRoughness y random.

            stepSize /= 2; // Reducir stepSize a la mitad
            currentRoughness *= roughnessDecay; // Reducir currentRoughness utilizando roughnessDecay

            // TODO: PROCESO ITERATIVO
            //
            // 1. Ejecutar DiamondStep utilizando:
            //      heights, stepSize, currentRoughness y random.
            //
            // 2. Ejecutar SquareStep utilizando los mismos parámetros.
            //
            // Después de ambos pasos se cambia a una escala menor.
        }

        return heights;
    }

    // -------------------------------------------------------------------------
    // DIAMOND STEP
    // -------------------------------------------------------------------------
    //
    // Objetivo:
    // Calcular el punto central de cada cuadrado utilizando sus cuatro esquinas.
    //
    // Para un cuadrado:
    //
    //      A-----------------------B
    //      |                       |
    //      |                       |
    //      |           X           |
    //      |                       |
    //      |                       |
    //      C-----------------------D
    //
    // el nuevo punto X se calcula utilizando:
    //
    //      promedio = (A + B + C + D) / 4
    //
    // y posteriormente:
    //
    //      X = promedio + RandomOffset(random, roughness)
    //
    //
    // -------------------------------------------------------------------------
    // halfStep
    // -------------------------------------------------------------------------
    //
    // halfStep corresponde a la mitad del tamaño de la región actual:
    //
    //      halfStep = stepSize / 2
    //
    // Si:
    //
    //      stepSize = 8
    //
    // entonces:
    //
    //      halfStep = 4
    //
    // Si la esquina superior izquierda de una región se encuentra en (0,0),
    // su centro estará entonces en:
    //
    //      (4,4)
    //
    // Visualmente:
    //
    //      (0,0) ---------------------- (8,0)
    //        O---------------------------O
    //        |                           |
    //        |                           |
    //        |             X             |
    //        |           (4,4)           |
    //        |                           |
    //        O---------------------------O
    //      (0,8) ---------------------- (8,8)
    //
    //
    // -------------------------------------------------------------------------
    // RECORRIDO
    // -------------------------------------------------------------------------
    //
    // En una iteración pueden existir varios cuadrados.
    //
    // Por ejemplo:
    //
    //      O-----------O-----------O
    //      |           |           |
    //      |     X     |     X     |
    //      |           |           |
    //      O-----------O-----------O
    //      |           |           |
    //      |     X     |     X     |
    //      |           |           |
    //      O-----------O-----------O
    //
    // El recorrido debe saltar de un centro al siguiente utilizando stepSize.
    //
    // Para cada centro:
    //
    //      topLeft     -> y - halfStep, x - halfStep
    //      topRight    -> y - halfStep, x + halfStep
    //      bottomLeft  -> y + halfStep, x - halfStep
    //      bottomRight -> y + halfStep, x + halfStep
    //
    // Finalmente, la nueva altura debe mantenerse en el rango [0,1].
    //
    private static void DiamondStep(
        //la funcion principal del diamondstep es buscar los centros vacios de los cuadrados y le asigna una altura basandose en sus esquinas

        float[,] heights,
        int stepSize,
        float roughness,
        System.Random random)
    {
        int resolution = heights.GetLength(0);
        int halfStep = stepSize / 2;


        //damos saltos usando halfstep para ubicarnos en el centro de cada cuadrado
        for(int y = halfStep; y< resolution; y += stepSize) // recorremos las filas separadas por stepSize, comenzando en halfStep
        {
            for(int x= halfStep; x < resolution; x += stepSize) //recorremos las columnas separadas por stepSize, comenzando en halfStep
            {
                //estando en el centro de los cuadrdados hacemos una suma de las 4 esquinas de este
                float sum = heights[y- halfStep, x- halfStep] + //esquina superior iszquierda
                            heights[y- halfStep, x+ halfStep] +// esquina superior derecha
                            heights[y+ halfStep, x- halfStep] +//esquina inferio izquierda
                            heights[y+ halfStep, x+ halfStep];//esquina inferior derecha

                float average = sum / 4.0f; //calculamos el equilibrio que seria el promedio de las 4 esquina del cuadrado
                float offset = RandomOffset(random, roughness); //generamos perturbacion aleatoria para elevar o hundir ese cuadrado 
                heights[y, x] = Mathf.Clamp(average + offset, 0f, 1f);//guardamos la nueva altura en el punto central, manteniéndola en el rango [0,1]
            }
        }
        // TODO: DIAMOND STEP
        //
        // 1. Recorrer los centros de cada cuadrado.
        // 2. Obtener las cuatro esquinas utilizando halfStep.
        // 3. Calcular el promedio de las cuatro alturas.
        // 4. Añadir RandomOffset(random, roughness).
        // 5. Mantener la nueva altura dentro del rango [0,1].
        // 6. Guardar el resultado en el punto central.
    }

    // -------------------------------------------------------------------------
    // SQUARE STEP
    // -------------------------------------------------------------------------
    //
    // Objetivo:
    // Completar los puntos restantes utilizando los valores generados durante
    // Diamond Step y los valores ya existentes.
    //
    // Después de Diamond Step tenemos conceptualmente:
    //
    //      O-----------------------O
    //      |                       |
    //      |                       |
    //      |           X           |
    //      |                       |
    //      |                       |
    //      O-----------------------O
    //
    // Square Step calcula los puntos intermedios de los lados:
    //
    //      O-----------S-----------O
    //      |                       |
    //      |                       |
    //      S           X           S
    //      |                       |
    //      |                       |
    //      O-----------S-----------O
    //
    // Cada punto S utiliza los vecinos existentes en las cuatro direcciones:
    //
    //                  arriba
    //                    O
    //                    |
    //                    |
    //      izquierda O---S---O derecha
    //                    |
    //                    |
    //                    O
    //                  abajo
    //
    // Su valor corresponde a:
    //
    //      promedio de vecinos válidos
    //                  +
    //      RandomOffset(random, roughness)
    //
    //
    // -------------------------------------------------------------------------
    // PUNTOS DE BORDE
    // -------------------------------------------------------------------------
    //
    // No todos los puntos poseen cuatro vecinos.
    //
    // Por ejemplo, un punto ubicado en el borde superior:
    //
    //             borde del terreno
    //
    //          O-------S-------O
    //                  |
    //                  |
    //                  O
    //
    // no posee un vecino hacia arriba.
    //
    // Por lo tanto, se deben considerar únicamente los vecinos cuya posición
    // se encuentre dentro de la matriz.
    //
    // En lugar de dividir siempre por 4:
    //
    //      promedio = suma / cantidadDeVecinosValidos
    //
    // Un punto interior normalmente tendrá 4 vecinos.
    // Un punto del borde normalmente tendrá 3.
    //
    //
    // -------------------------------------------------------------------------
    // RECORRIDO ALTERNADO
    // -------------------------------------------------------------------------
    //
    // El Square Step no recorre una grilla rectangular normal. Las posiciones
    // que deben calcularse están desplazadas en filas alternadas.
    //
    // Después del Diamond Step, el patrón que buscamos es:
    //
    //          S       S       S
    //
    //      S       S       S
    //
    //          S       S       S
    //
    //      S       S       S
    //
    // Es decir, una fila comienza desplazada respecto de la siguiente.
    //
    // Visualmente:
    //
    //      O-----S-----O-----S-----O
    //      |           |           |
    //      S     X     S     X     S
    //      |           |           |
    //      O-----S-----O-----S-----O
    //      |           |           |
    //      S     X     S     X     S
    //      |           |           |
    //      O-----S-----O-----S-----O
    //
    // Por esto, para cada fila se debe determinar si el primer punto comienza
    // en:
    //
    //      x = halfStep
    //
    // o en:
    //
    //      x = 0
    //
    // dependiendo de la fila actual.
    //
    // Posteriormente los puntos de una misma fila se encuentran separados por
    // stepSize.
    //
    // Finalmente, cada nueva altura debe mantenerse en [0,1].
    //
    private static void SquareStep(
        //la funcion principal del squarestep es la de rellenar los huecos generados en el mapa que quedaron en el medio de los lados dandoles cierta estructura 

        float[,] heights,
        int stepSize,
        float roughness,
        System.Random random)
    {
        int resolution = heights.GetLength(0);
        int halfStep = stepSize / 2;

        // recorremos el mapa de arriba a abajo en pasos cortos mediante el halfstep
        for(int y=0; y < resolution; y += halfStep)
        {

            //se crea un patron como un tablero de ajedrez en el cuadrados del diamond step y empezamos pegados a los bordes desde las filas
            //y en otras nos desplazamos medio paso para rellenar los huecos de los lados de los cuadrados del diamond step
            int StartX = (y % stepSize == 0) ? halfStep : 0;//comenzamos desplazandonos medio paso y si no desde 0 


            //avanzamos de izqueirda a derecha dando pasos largos para evitar pisar lo ya calculado
            for (int x = StartX; x < resolution; x += stepSize)
            {
                float sum = 0f; // inicializamos la suma de los vecinos a 0
                int neighbours = 0; // inicializamos el contador de vecinos válidos a 0

                // en lugar de mirarse como diagonal se crea una cruz en el cuadrado 
                //pero como como algunos puntos estan en la orilla del mapa se comprueba si existen vecinos validos para no salirse de la matriz


                // calculamos si seguimos dentro del mapa desde la izquierda
                if (x - halfStep >= 0)
                {
                    sum += heights[y, x - halfStep];
                    neighbours++;
                }

                // calculamos si seguimos dentro del mapa desde la derecha
                if (x + halfStep < resolution)
                {
                    sum += heights[y, x + halfStep];
                    neighbours++;
                }

                // calculamos si seguimos dentro del mapa desde arriba
                if (y- halfStep >= 0)
                {
                    sum += heights[y - halfStep, x];
                    neighbours++;
                }
                // calculamos si seguimos dentro del mapa desde abajo
                if (y + halfStep < resolution)
                {
                    sum += heights[y + halfStep, x];
                    neighbours++;
                }


                //calculamos el punto de equilibrio con los vecinos que encontramos
                //normalmente serian 4(por la cruz) pero si estamos en un borde solo se encuentran 3
                float average = sum / neighbours;

                //le agregamos un grado de imperfeccion al "cerro" que se genera bajandolo o subiendolo un poco
                float offset = RandomOffset(random, roughness);

                //guardamos la altura final normalizada para asegurarnos que nunca se pase de 1 o baje de 0
                heights[y, x] = Mathf.Clamp(average + offset, 0f, 1f);

            }
        }



        // TODO: SQUARE STEP
        //
        // 1. Recorrer las filas separadas por halfStep.
        // 2. Determinar si la fila comienza en x = 0 o x = halfStep.
        // 3. Recorrer los puntos de esa fila utilizando stepSize.
        // 4. Para cada punto:
        //      - Inicializar suma y contador.
        //      - Comprobar vecino izquierdo.
        //      - Comprobar vecino derecho.
        //      - Comprobar vecino superior.
        //      - Comprobar vecino inferior.
        // 5. Calcular el promedio utilizando solo los vecinos válidos.
        // 6. Añadir RandomOffset(random, roughness).
        // 7. Mantener el resultado dentro de [0,1].
        // 8. Guardar la nueva altura.
    }

    // -------------------------------------------------------------------------
    // PERTURBACIÓN ALEATORIA
    // -------------------------------------------------------------------------
    //
    // Este método se entrega implementado.
    //
    // Su objetivo es generar la perturbación que se añade al promedio de los
    // vecinos durante Diamond Step y Square Step.
    //
    // Devuelve un valor dentro del intervalo:
    //
    //      [-magnitude, +magnitude]
    //
    // Por ejemplo:
    //
    //      magnitude = 0.4
    //
    // puede producir valores entre:
    //
    //      -0.4 y +0.4
    //
    // Durante las iteraciones posteriores, magnitude disminuye debido a
    // roughnessDecay, produciendo variaciones progresivamente menores.
    private static float RandomOffset(System.Random random, float magnitude)
    {
        float randomValue = (float)random.NextDouble();

        return Mathf.Lerp(-magnitude, magnitude, randomValue);
    }
}