using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public enum WFCPhase
    {
        NotInitialized, ReadyToObserve, Propagating,
        Completed, Contradiction
    }

    public class WFCGenerator : MonoBehaviour
    {
        [Header("Tile Sets")]
        [SerializeField] private List<WFCTileSet> tileSets = new()
        {
            new WFCTileSet { id = "TileSet_01" },
            new WFCTileSet { id = "TileSet_02" }
        };
        [SerializeField, Min(0)] private int activeTileSetIndex = 0;

        [Header("Mapa")]
        [SerializeField, Min(1)] private int width = 16;
        [SerializeField, Min(1)] private int height = 12;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        [Header("Generacion")]
        [SerializeField] private bool randomSeed = false;
        [SerializeField] private int seed = 12345;

        // --------------------------------------------------------
        // ESTRUCTURAS DEL ALGORITMO
        // --------------------------------------------------------

        /*
         * domains[x,y]:
         * Contiene los indices de los patrones que todavia pueden
         * ocupar la posicion (x,y).
         *
         * compatible[d,a,b]:
         * Indica si el patron B puede ubicarse en la direccion d
         * respecto del patron A.
         *
         * pendingArcs:
         * Relaciones entre celdas pendientes de comprobar
         * durante la propagacion.
         */

        private List<int>[,] domains;
        private SpriteRenderer[,] visualGrid; // malla para poder tener representacion visual
        private bool[,,] compatible;
        private Queue<WFCArc> pendingArcs = new();
        private System.Random random;
        private bool firstObservation;

        private struct WFCArc
        {
            public int x, y, direction;

            public WFCArc(int x, int y, int direction)
            {
                this.x = x;
                this.y = y;
                this.direction = direction;
            }
        }

        private static readonly Vector2Int[] Offsets =
        {
            new Vector2Int(0, 1),   // North
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, -1),  // South
            new Vector2Int(-1, 0)   // West
        };

        public WFCPhase Phase { get; private set; } = WFCPhase.NotInitialized;
        public string Status { get; private set; } = "Sin inicializar.";

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int ActiveTileSetIndex => activeTileSetIndex;
        public bool IsInitialized => domains != null;

        public WFCTileSet ActiveTileSet =>
            tileSets != null && activeTileSetIndex >= 0 &&
            activeTileSetIndex < tileSets.Count
                ? tileSets[activeTileSetIndex] : null;

        // ========================================================
        // CONTROL GENERAL
        // ========================================================

        /*
         * El algoritmo WFC desarrolla cuatro tareas principales:
         *
         * 1. Preparar patrones.
         * 2. Construir restricciones.
         * 3. Generar mediante observacion y propagacion.
         * 4. Representar el resultado.
         *
         * Durante la generacion se trabaja con asignaciones
         * parciales. Los dominios disminuyen progresivamente
         * hasta que cada celda tenga un unico patron.
         *
         * Referencia: Karth y Smith (2017), Seccion 4.
         */

        public void SelectTileSet(int index)
        {
            if (tileSets == null || index < 0 || index >= tileSets.Count)
                return;

            activeTileSetIndex = index;
            Clear();
        }

        public bool Initialize()
        {
            Clear();

            if (ActiveTileSet == null || ActiveTileSet.Tiles.Count == 0)
            {
                Status = "El TileSet seleccionado no contiene patrones.";
                Debug.LogWarning(Status, this);
                return false;
            }

            random = new System.Random(
                randomSeed ? Environment.TickCount : seed);

            firstObservation = true;

            // Preparar los dominios iniciales.
            if (!PatternsFromSample() || domains == null)
            {
                Status = "Pendiente: PatternsFromSample().";
                return false;
            }

            // Construir las relaciones de compatibilidad.
            BuildPropagator();

            Phase = WFCPhase.ReadyToObserve;
            Status = "Inicializado. Listo para observar.";

            OutputObservations();
            return true;
        }

        public void Step()
        {
            switch (Phase)
            {
                case WFCPhase.ReadyToObserve:
                    Observe();
                    break;

                case WFCPhase.Propagating:
                    Propagate();
                    break;
            }

            OutputObservations();
        }

        public void Clear()
        {
            domains = null;
            compatible = null;
            pendingArcs.Clear();

            firstObservation = true;
            Phase = WFCPhase.NotInitialized;
            Status = "Sin inicializar.";

            // Limpieza visual para cuando reiniciamos el mapa:
            if (visualGrid != null)
            {
                foreach (var renderer in visualGrid)
                {
                    if (renderer != null) Destroy(renderer.gameObject);
                }
                visualGrid = null;
            }
        }

        // ========================================================
        // ETAPA 1: PATTERNS FROM SAMPLE
        // ========================================================

        /*
         * OBJETIVO:
         * Preparar los patrones disponibles y los dominios
         * iniciales de todas las posiciones de la matriz.
         *
         * En esta version utilizamos Simple Tiled WFC:
         * los patrones corresponden a piezas definidas en
         * el TileSet, no a subimagenes extraidas automaticamente.
         *
         * EJEMPLO:
         *
         * Si el TileSet contiene cuatro patrones:
         *
         *   P0, P1, P2, P3
         *
         * Pero P2 esta deshabilitado, el dominio inicial es:
         *
         *   D(x,y) = {P0, P1, P3}
         *
         * Todas las celdas comienzan con esos patrones
         * disponibles. Durante WFC se iran descartando opciones.
         */


        /*
         * el metodo filtra el tileset activo descartando cualquier pieza donde avalaible sea false
         * construye la matriz bidimensional domains[x,y] basada en el ancho y alto del mapa ademas de rellenar cada celda con la lista completa de indices validos
         */
        private bool PatternsFromSample()
        {
            List<int> validPatterns = new List<int>();

            // Identifica y filtra los patrones que estan habilitados en el TileSet activo.
            for (int i = 0; i < ActiveTileSet.Tiles.Count; i++)
            {
                if (ActiveTileSet.Tiles[i].available)
                {
                    validPatterns.Add(i);
                }
            }
            // Si no hay patrones validos habilitados, la inicializacion falla.
            if (validPatterns.Count == 0)
            {
                return false;
            }

            domains = new List<int>[width, height];// Crea la matriz bidimensional de dominios basada en el ancho y alto del mapa.

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    domains[x, y] = new List<int>(validPatterns);// Inicializa cada celda con la lista completa de indices validos (estado de maxima entropia).
                }
            }

            return true;
        }



        // ========================================================
        // ETAPA 2: BUILD PROPAGATOR
        // ========================================================

        /*
         * OBJETIVO:
         * Construir un indice con las relaciones de compatibilidad
         * entre todos los patrones disponibles.
         *
         * La compatibilidad depende de la direccion relativa.
         *
         * EJEMPLO:
         *
         * Un camino horizontal tiene:
         *
         *   North = 0, East = 1, South = 0, West = 1
         *
         * Si queremos colocar otra pieza al Este, esta debe
         * tener una conexion compatible hacia el Oeste.
         *
         * Esto permite descartar combinaciones incorrectas
         * sin evaluar nuevamente sus conexiones durante
         * cada propagacion.
         *
         * compatible[d,a,b] almacena esta relacion.
         */


        /*
         * el metodo instancia la matriz bidimensional compatible [direccion,piezaA,piezaB]
         * cruza cada pieza del tileset contra todos los demas en las 4 direcciones posibles
         */

        private void BuildPropagator()
        {
            int count = ActiveTileSet.Tiles.Count;
            compatible = new bool[4, count, count];// Instancia la matriz tridimensional de cache [direccion, piezaA, piezaB].

            // Recorre cada pieza del tileset para usarla como pieza base (A).
            for (int a = 0; a < count; a++)
            {
                WFCTile tileA = ActiveTileSet.Tiles[a];

                // Recorre nuevamente el tileset para compararla con cada posible pieza vecina (B).
                for (int b = 0; b < count; b++)
                {
                    WFCTile tileB = ActiveTileSet.Tiles[b];

                    // Evalua la compatibilidad en las 4 direcciones posibles.
                    for (int d = 0; d < 4; d++)
                    {
                        compatible[d, a, b] = tileA.IsCompatible(tileB, (WFCDirection)d);// Almacena el resultado para convertir la validacion geometrica en una consulta rapida de memoria.
                    }
                }
            }
        }

        // ========================================================
        // ETAPA 3A: OBSERVE
        // ========================================================

        /*
         * OBJETIVO:
         * Seleccionar una celda que todavia no este resuelta
         * y asignarle uno de sus patrones posibles.
         *
         * EJEMPLO:
         *
         * Antes de observar:
         *
         *   D(2,3) = {P0, P2, P5, P7}
         *
         * Si se selecciona P5:
         *
         *   D(2,3) = {P5}
         *
         * La celda queda resuelta, pero sus vecinos pueden
         * mantener patrones incompatibles con esta decision.
         *
         * Por lo tanto, debe ejecutarse la propagacion.
         */


        /*
         * el metodo ejecuta un paso de observacion donde selecciona una celda que aun no esta resulta
         * y la colapsa a un unico patron posible donde luego prepara el sistema para propagar los cambio a los decinos
         */

        public bool Observe()
        {
            //Verifica que el algoritmo este en la fase correcta antes de actuar.
            if (Phase != WFCPhase.ReadyToObserve)
                return false;

            // En el primer paso elige una celda al azar. En los siguientes, 
            // usa la heuristica de menor entropia (la celda con menos opciones).
            Vector2Int cell = firstObservation ? FindRandomUnresolvedCell() : FindLowestEntropy();

            // Si devuelve (-1, -1), significa que ya no quedan celdas por resolver o hubo un error.
            if (cell.x == -1 || cell.y == -1)
            {
                CheckCompletion();
                return false;
            }

            // Obtiene los patrones posibles para la celda y elige uno basandose en su peso.
            List<int> domain = domains[cell.x, cell.y];
            int chosenPattern = ChooseWeightedPattern(domain);

            // Colapsa la celda: elimina todas las demas posibilidades y deja solo la elegida.
            domain.Clear();
            domain.Add(chosenPattern);

            // Encola a los vecinos de esta celda para que la Etapa 3B revise 
            // si esta nueva asignacion reduce sus propias posibilidades.
            EnqueueOutgoing(cell.x, cell.y);

            // Actualiza el estado del generador para pasar a la fase de propagacion.
            firstObservation = false;
            Phase = WFCPhase.Propagating;
            Status = $"Observando ({cell.x}, {cell.y}). Propagacion pendiente.";

            return true;
        }


        // --------------------------------------------------------
        // SELECCION DE CELDA
        // --------------------------------------------------------


        /*
         * el metodo busca las celdas de la grilla que todavia tiene mas de un patron disponible
         * y elige una completamente al azar donde se utiliza solo el primer paso 
         */

        private Vector2Int FindRandomUnresolvedCell()
        {
            List<Vector2Int> unresolved = new List<Vector2Int>();

            // Recorre toda la matriz buscando celdas pendientes.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Si el dominio de la celda tiene mas de 1 elemento, no esta resuelta.
                    if (domains[x, y] != null && domains[x, y].Count > 1)
                    {
                        unresolved.Add(new Vector2Int(x, y));
                    }
                }
            }

            //Si la lista esta vacia, no hay celdas disponibles (retorna invalido).
            if (unresolved.Count == 0) return new Vector2Int(-1, -1);

            //Devuelve una posicion aleatoria dentro de la lista de celdas pendientes.
            return unresolved[random.Next(unresolved.Count)];
        }



        /*
         * encuentra la celda con menor "entropia" (basicamentel a que tiene mayor certeza sobre que pieza debe ir ahi)
         * evalua las celdas sumando los pesos de sus patrones
         */


        private Vector2Int FindLowestEntropy()
        {
            float minEntropy = float.MaxValue;
            Vector2Int bestCell = new Vector2Int(-1, -1);

            // Recorre toda la grilla para evaluar las celdas.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Ignora posiciones que ya estan resueltas (1 patron) o en contradiccion (0 patrones).
                    if (domains[x, y] == null || domains[x, y].Count <= 1) continue;

                    float sumOfWeights = 0f;
                    float sumOfWeightLogWeight = 0f;

                    // Aplica la formula de entropia considerando el peso de cada patron disponible.
                    foreach (int patternIndex in domains[x, y])
                    {
                        float weight = ActiveTileSet.Tiles[patternIndex].weight;
                        sumOfWeights += weight;
                        sumOfWeightLogWeight += weight * Mathf.Log(weight);
                    }

                    // Calculo matematico de la entropia de Shannon.
                    float entropy = Mathf.Log(sumOfWeights) - (sumOfWeightLogWeight / sumOfWeights);

                    // Añade un pequeñisimo ruido aleatorio. Esto sirve para desempatar 
                    // si dos celdas tienen exactamente la misma entropia.
                    float noise = (float)random.NextDouble() * 0.0001f;
                    float entropyWithNoise = entropy + noise;

                    //Si es la entropia mas baja encontrada hasta ahora, la guarda como la mejor candidata.
                    if (entropyWithNoise < minEntropy)
                    {
                        minEntropy = entropyWithNoise;
                        bestCell = new Vector2Int(x, y);
                    }
                }
            }

            return bestCell;
        }

        // --------------------------------------------------------
        // SELECCION DE PATRON
        // --------------------------------------------------------

        /*
         *el metodo selecciona un patron de una lista de opciones utilizando sus pesos como probabilidad
         * donde funciona como una ruleta donde las piezas con mas peso ocupa una franja mas grande
        */


        private int ChooseWeightedPattern(IReadOnlyList<int> domain)
        {
            float totalWeight = 0f;

            // Suma el peso total de todos los patrones que siguen disponibles en la celda.
            foreach (int patternIndex in domain)
            {
                totalWeight += ActiveTileSet.Tiles[patternIndex].weight;
            }

            // Tira la "bolita de la ruleta": un valor aleatorio entre 0 y el peso total acumulado.
            float randomValue = (float)random.NextDouble() * totalWeight;
            float currentWeight = 0f;

            // Recorre los patrones, sumando sus pesos hasta alcanzar o superar el valor aleatorio.
            foreach (int patternIndex in domain)
            {
                currentWeight += ActiveTileSet.Tiles[patternIndex].weight;

                // El primer patron que hace que la suma supere el valor aleatorio es el ganador.
                if (randomValue <= currentWeight)
                {
                    return patternIndex;
                }
            }

            // Retorno de seguridad (por errores minimos de redondeo de floats), devuelve el primero.
            return domain[0];
        }

        // ========================================================
        // ETAPA 3B: PROPAGATE
        // ========================================================

        /*
         * OBJETIVO:
         * Actualizar los dominios de las celdas relacionadas
         * despues de una observacion o de otra reduccion.
         *
         * EJEMPLO:
         *
         * Se observa un camino horizontal en la celda A.
         * Su conexion Este esta abierta.
         *
         * La celda B, ubicada al Este, tiene:
         *
         *   D(B) = {Horizontal, Vertical}
         *
         * Horizontal tiene West = 1.
         * Vertical tiene West = 0.
         *
         * Por lo tanto, Vertical deja de ser valido:
         *
         *   D(B) = {Horizontal}
         *
         * La reduccion de B puede afectar a sus propios
         * vecinos. Este proceso continua hasta que no
         * queden restricciones pendientes.

         
        

        /*
         *el metodo propagar basicamente procesa una reaccion en cadena donde se revisa una cola de relaciones pendinetes entre las celdas vecinas 
         * donde la celda A se redujo por una observacion previa y evalua la celda B si tiene patrones que ya no pueden conectarse
         * con las opciones restantes de A, se elimina. Si B pierde opciones se avisa a los vecinos de B y asi sucesivamente.
         */

        public bool Propagate()
        {
            //  Verificamos que el sistema este en estado de propagacion.
            if (Phase != WFCPhase.Propagating)
                return false;

            //  Mientras existan relaciones pendientes en la cola por revisar
            while (pendingArcs.Count > 0)
            {
                // Sacamos la siguiente relacion (arco) de la cola.
                WFCArc arc = pendingArcs.Dequeue();

                int x = arc.x;
                int y = arc.y;
                int d = arc.direction; // Direccion hacia el vecino (0=Norte, 1=Este, etc.)

                // Calculamos las coordenadas de la celda vecina usando los offsets.
                int nx = x + Offsets[d].x;
                int ny = y + Offsets[d].y;

                //  Si la celda vecina esta fuera del mapa, pasamos a la siguiente iteracion.
                if (!IsInBounds(nx, ny))
                    continue;

                List<int> sourceDomain = domains[x, y];
                List<int> neighborDomain = domains[nx, ny];

                //  Si el vecino ya se quedo sin posibilidades (error), no hacemos nada con el.
                if (neighborDomain == null || neighborDomain.Count == 0)
                    continue;

                bool neighborChanged = false;

                // Revisamos los patrones del vecino. Iteramos en reversa (de atras hacia adelante) 
                // porque vamos a eliminar elementos de la lista y no queremos romper el indice del ciclo (for).
                for (int i = neighborDomain.Count - 1; i >= 0; i--)
                {
                    int neighborPattern = neighborDomain[i];
                    bool hasSupport = false;

                    // Revisamos si AL MENOS UN patron de la celda origen es compatible 
                    // con este patron vecino en esta direccion especifica.
                    foreach (int sourcePattern in sourceDomain)
                    {
                        // Usamos nuestra matriz de cache de la Etapa 2 para una consulta instantanea.
                        if (compatible[d, sourcePattern, neighborPattern])
                        {
                            hasSupport = true;
                            break; // Con uno que lo soporte es suficiente, dejamos de buscar.
                        }
                    }

                    // Si ningun patron de la celda origen permite que este patron vecino exista, lo eliminamos.
                    if (!hasSupport)
                    {
                        neighborDomain.RemoveAt(i);
                        neighborChanged = true;
                    }
                }

                // Si el dominio del vecino sufrio modificaciones (perdio patrones)...
                if (neighborChanged)
                {
                    // Si se quedo en 0 opciones, es una contradiccion (camino sin salida logica).
                    if (neighborDomain.Count == 0)
                    {
                        ReportContradiction(nx, ny);
                        return false;
                    }

                    // Como el vecino cambio, ahora sus propios vecinos podrian verse afectados. 
                    // Encolamos todas las relaciones salientes de este vecino para revisarlas mas tarde.
                    EnqueueOutgoing(nx, ny);
                }
            }

            // Si la cola se vacio y no hubo contradicciones, terminamos la reaccion en cadena.
            if (Phase != WFCPhase.Contradiction)
            {
                Phase = WFCPhase.ReadyToObserve;
                Status = "Propagacion completada. Listo para la siguiente observacion.";

                // Chequeamos si el mapa entero ya fue resuelto.
                CheckCompletion();
            }

            return true;
        }

        // --------------------------------------------------------
        // HERRAMIENTAS DE PROPAGACION
        // --------------------------------------------------------

        /*
         * WFCArc representa una relacion dirigida:
         *
         *   (x,y) ---- direccion ----> vecino
         *
         * EnqueueOutgoing agrega las relaciones entre una
         * celda y sus vecinos existentes.
         *
         * La cola pendingArcs permite revisar estas relaciones
         * durante la propagacion.
         *
         * Este metodo se entrega implementado.
         */


        //estos metodos de aqui no se tocaron por el momento para el lab 7 :p

        private void EnqueueOutgoing(int x, int y)
        {
            for (int d = 0; d < 4; d++)
            {
                int nx = x + Offsets[d].x;
                int ny = y + Offsets[d].y;

                if (IsInBounds(nx, ny))
                    pendingArcs.Enqueue(new WFCArc(x, y, d));
            }
        }

        // Se utiliza cuando alguna celda pierde todas
        // sus posibilidades de asignacion.
        //tampoco este
        private void ReportContradiction(int x, int y)
        {
            Phase = WFCPhase.Contradiction;
            Status = $"Contradiccion en ({x},{y}): dominio vacio.";
            Debug.LogWarning(Status, this);
        }

        // Comprueba si todas las posiciones tienen un unico
        // patron y ya no existen relaciones pendientes.
        //ni este
        private void CheckCompletion()
        {
            if (domains == null || Phase == WFCPhase.Contradiction ||
                pendingArcs.Count > 0)
                return;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (domains[x, y] == null || domains[x, y].Count != 1)
                        return;

            Phase = WFCPhase.Completed;
            Status = "Generacion completada.";
        }

        // ========================================================
        // ETAPA 4: OUTPUT OBSERVATIONS
        // ========================================================

        /*
         * OBJETIVO:
         * Representar el estado actual de la generacion.
         *
         * Durante WFC, una celda puede encontrarse:
         *
         * - Sin resolver: mantiene multiples patrones.
         * - Resuelta: tiene un unico patron.
         * - En contradiccion: no tiene patrones disponibles.
         *
         * EJEMPLO:
         *
         *   8  4  2
         *   4  1  2
         *   8  4  8
         *
         * Los numeros representan las posibilidades restantes
         * por celda. La posicion central ya esta resuelta.
         *
         * La visualizacion deberia permitir observar como
         * los dominios cambian durante la generacion.
         *
         * Puede utilizarse SpriteRenderer, Gizmos, Canvas
         * u otros elementos de Unity.
         */


        /*
         * este metodo se encarga de dibujar eel estado actual en la escena de unity para poder ver los resultados
         * si es la priemra vez que se ejecuta instancia un gameobject para cada celda con un sprite renderere. luego en cada paso
         * actualiza el color y el sprite permitiendo destinguir las posiciones resuletas de aquellas multiples posibilidades
         */

        private void OutputObservations()
        {
            Debug.Log($"--- INICIANDO DIBUJO --- Mapa: {width}x{height}");

            if (domains == null)
            {
                Debug.LogWarning("Error: domains es null, cancelando dibujo.");
                return;
            }

            if (visualGrid == null)
            {
                Debug.Log("Creando la grilla de GameObjects...");
                visualGrid = new SpriteRenderer[width, height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        GameObject cellObject = new GameObject();
                        cellObject.transform.SetParent(this.transform);
                        cellObject.transform.position = GetCellWorldPosition(x, y);
                        visualGrid[x, y] = cellObject.AddComponent<SpriteRenderer>();
                    }
                }
                Debug.Log($"Se crearon {width * height} objetos en la jerarquía.");
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    List<int> domain = domains[x, y];
                    SpriteRenderer renderer = visualGrid[x, y];

                    if (domain == null || domain.Count == 0)
                    {
                        renderer.sprite = null;
                        renderer.color = Color.red;
                        renderer.gameObject.name = $"Cell_{x}_{y} [CONTRADICCION]";
                    }
                    else if (domain.Count == 1)
                    {
                        int patternIndex = domain[0];
                        renderer.sprite = ActiveTileSet.Tiles[patternIndex].sprite;
                        renderer.color = Color.white;
                        renderer.gameObject.name = $"Cell_{x}_{y} [Resuelta]";
                    }
                    else
                    {
                        renderer.sprite = ActiveTileSet.Tiles[0].sprite;// se le asgina el primer psirte del tileset para que se tenga un lienzo
                        float intensity = (float)domain.Count / ActiveTileSet.Tiles.Count;// se calcula la intensidad donde mientras menos opciones queden mas oscuro se pondra 
                        renderer.color = new Color(intensity, intensity, intensity, 0.5f);//se pinta el lienzo de gris
                        renderer.gameObject.name = $"Cell_{x}_{y} [{domain.Count} opciones]";
                    }
                }
            }
            Debug.Log("--- DIBUJO COMPLETADO ---");
        }

        // ========================================================
        // CONSULTAS PARA LA VISUALIZACION
        // ========================================================

        // Verifica si una posicion pertenece a la matriz.
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        // Obtiene la cantidad de patrones posibles.
        // Devuelve -1 para una posicion invalida.
        public int GetDomainCount(int x, int y)
        {
            if (!IsInitialized || !IsInBounds(x, y))
                return -1;

            return domains[x, y] != null
                ? domains[x, y].Count : -1;
        }

        // Permite consultar los patrones restantes.
        // Los valores corresponden a indices del TileSet.
        public IReadOnlyList<int> GetDomain(int x, int y)
        {
            if (!IsInitialized || !IsInBounds(x, y))
                return null;

            return domains[x, y];
        }

        // Obtiene el centro de una celda en coordenadas
        // globales, considerando las dimensiones de la matriz.
        public Vector3 GetCellWorldPosition(int x, int y)
        {
            Vector3 position = new Vector3(
                (x - (width - 1) * 0.5f) * cellSize,
                (y - (height - 1) * 0.5f) * cellSize,
                0f);

            return transform.TransformPoint(position);
        }
    }
}
