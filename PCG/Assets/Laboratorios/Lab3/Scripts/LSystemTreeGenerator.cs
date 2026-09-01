using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystemTreeGenerator : MonoBehaviour
{
    public enum GenerationMode
    {
        TwoD,
        ThreeD
    }


    // -------------------------------------------------------------------------
    // CONFIGURACIÓN
    // -------------------------------------------------------------------------

    [Header("Generation")]

    [SerializeField]
    private GenerationMode generationMode =
        GenerationMode.TwoD;

    [SerializeField]
    private bool autoUpdate = true;


    [Header("2D Configuration")]

    [SerializeField]
    private string axiom2D = "F";

    [SerializeField]
    private List<LSystemRule> rules2D =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "F",
                successor = "F[+F]F[-F]F"
            }
        };

    [Range(0, 5)]
    [SerializeField]
    private int iterations2D = 3;

    [Range(1f, 90f)]
    [SerializeField]
    private float angle2D = 25f;


    [Header("3D Configuration")]

    [SerializeField]
    private string axiom3D = "F";

    [SerializeField]
    private List<LSystemRule> rules3D =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "F",
                successor = "F[+F][-F][&F][^F]"
            }
        };

    [Range(0, 5)]
    [SerializeField]
    private int iterations3D = 3;

    [Range(1f, 90f)]
    [SerializeField]
    private float angle3D = 30f;


    [Header("Shared Visualization")]

    [Min(0.05f)]
    [SerializeField]
    private float segmentLength = 1f;

    [Min(0.005f)]
    [SerializeField]
    private float branchRadius = 0.06f;

    [SerializeField]
    private Material branchMaterial;


    [Header("Height Color")]

    [SerializeField]
    private bool useHeightColor = true;

    [SerializeField]
    private Color lowColor =
        new Color(
            0.35f,
            0.17f,
            0.06f,
            1f
        );

    [SerializeField]
    private Color highColor =
        new Color(
            0.15f,
            0.55f,
            0.18f,
            1f
        );

    [Min(0.1f)]
    [SerializeField]
    private float colorHeight = 8f;


    [Header("Debug")]

    [SerializeField]
    private bool showDerivation = true;


    [HideInInspector]
    [SerializeField]
    private Transform generatedRoot;

    private int branchCounter = 0;


    // -------------------------------------------------------------------------
    // ESTADO DE LA TORTUGA
    // -------------------------------------------------------------------------
    //
    // Turtle Graphics interpreta una cadena como una secuencia de instrucciones.
    //
    // Para poder recorrer dicha cadena es necesario mantener el estado actual
    // de la "tortuga".
    //
    // En este laboratorio el estado está compuesto por:
    //
    //      position
    //
    //          posición actual desde la cual continuará el crecimiento.
    //
    //      rotation
    //
    //          orientación actual que determina la dirección del siguiente
    //          movimiento.
    //
    // Ambos valores son importantes al trabajar con ramificaciones.
    //
    private struct TurtleState
    {
        public Vector3 position;
        public Quaternion rotation;

        public TurtleState(
            Vector3 position,
            Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }


    // -------------------------------------------------------------------------
    // GENERACIÓN DEL L-SYSTEM
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // El proceso posee dos etapas diferentes:
    //
    //      1. GENERACIÓN SIMBÓLICA
    //
    //          ParallelGrammarGenerator.Generate(...)
    //
    //          produce una cadena mediante las reglas implementadas en
    //          el ejercicio anterior.
    //
    //
    //      2. INTERPRETACIÓN
    //
    //          Interpret(...)
    //
    //          transforma los símbolos de esa cadena en movimientos y
    //          rotaciones.
    //
    //
    // Esta separación es importante:
    //
    // La gramática NO genera directamente la geometría.
    //
    // Primero genera una representación simbólica y posteriormente dicha
    // representación es interpretada.
    //
    public void GenerateTree()
    {
        ClearGeneratedTree();
        EnsureGeneratedRoot();

        branchCounter = 0;

        string activeAxiom;
        List<LSystemRule> activeRules;
        int activeIterations;
        float activeAngle;


        if (generationMode ==
            GenerationMode.TwoD)
        {
            activeAxiom =
                axiom2D;

            activeRules =
                rules2D;

            activeIterations =
                iterations2D;

            activeAngle =
                angle2D;
        }
        else
        {
            activeAxiom =
                axiom3D;

            activeRules =
                rules3D;

            activeIterations =
                iterations3D;

            activeAngle =
                angle3D;
        }


        List<string> derivation =
            new List<string>();


        string sequence =
            ParallelGrammarGenerator.Generate(
                activeAxiom,
                activeRules,
                activeIterations,
                derivation
            );


        if (showDerivation)
        {
            PrintDerivation(
                activeAxiom,
                activeRules,
                derivation,
                sequence
            );
        }


        Interpret(
            sequence,
            activeAngle
        );
    }


    // -------------------------------------------------------------------------
    // TURTLE GRAPHICS
    // -------------------------------------------------------------------------
    //
    // La cadena generada anteriormente contiene símbolos que ahora deben ser
    // interpretados como instrucciones.
    //
    //
    // -------------------------------------------------------------------------
    // MOVIMIENTO
    // -------------------------------------------------------------------------
    //
    // F:
    //
    //      avanzar en la dirección actual y dibujar un segmento.
    //
    // f:
    //
    //      avanzar la misma distancia pero sin dibujar.
    //
    // La dirección depende de la orientación actual de la tortuga.
    //
    //
    // -------------------------------------------------------------------------
    // ROTACIÓN 2D
    // -------------------------------------------------------------------------
    //
    // + y - modifican la orientación utilizando el ángulo configurado.
    //
    // Ambos representan rotaciones opuestas.
    //
    // Esto permite que futuros movimientos F continúen en otra dirección.
    //
    //
    // -------------------------------------------------------------------------
    // RAMIFICACIONES
    // -------------------------------------------------------------------------
    //
    // Los símbolos [ y ] permiten generar estructuras ramificadas.
    //
    // Al encontrar:
    //
    //      [
    //
    // se debe recordar el estado actual.
    //
    // Posteriormente:
    //
    //      ]
    //
    // permite volver a ese estado y continuar desde allí.
    //
    // Debido a que pueden existir ramas dentro de otras ramas, los estados
    // deben almacenarse utilizando una pila:
    //
    //      Stack<TurtleState>
    //
    // El último estado almacenado debe ser el primero en recuperarse.
    //
    //
    // -------------------------------------------------------------------------
    // EXTENSIÓN 3D
    // -------------------------------------------------------------------------
    //
    // En modo 2D las rotaciones mantienen la estructura sobre un plano.
    //
    // En modo ThreeD se incorporan nuevas rotaciones:
    //
    //      & y ^       Pitch
    //
    //      \ y /       Roll
    //
    // Estas rotaciones permiten cambiar la orientación sobre ejes adicionales
    // y generar ramas fuera del plano original.
    //
    // Los operadores adicionales solamente deben actuar en modo ThreeD.
    //
    //
    // -------------------------------------------------------------------------
    // QUATERNIONS
    // -------------------------------------------------------------------------
    //
    // Unity representa la orientación utilizando Quaternion.
    //
    // Para aplicar las rotaciones puede utilizarse:
    //
    //      Quaternion.AngleAxis(...)
    //
    // y combinar la nueva rotación con la orientación actual.
    //
    // La figura incluida en la guía muestra como referencia los movimientos
    // yaw, pitch y roll.
    //
    //
    private void Interpret(
        string sequence,
        float activeAngle)
    {
        // TODO: TURTLE GRAPHICS 2D Y 3D
        //
        // Recorrer los símbolos de sequence e implementar:
        //
        // 1. El estado inicial de la tortuga.
        // 2. Movimiento con y sin dibujo mediante F y f.
        // 3. Rotaciones 2D mediante + y -.
        // 4. Guardado y recuperación del estado mediante [ y ].
        // 5. Rotaciones adicionales del modo 3D mediante &, ^, \ y /.
        //
        // Utilizar:
        //
        //      Stack<TurtleState>
        //      CreateBranch(...)
        //      Quaternion
        //
        // Considerar que posición y orientación deben recuperarse juntas
        // al retornar desde una rama.

        //# Implementamos el metodo de ejecucion

        // # F: La tortuga avanza desde la direccion actual y dibuja un segmento
        // # f: la trotuga avanza la misma distancia pero sin dibujar(no genera segmento)
        // # +: rotamos la tortuga hacia la izquierda
        // # -: la tortuga rota hacia la derecha
        // # [: entra en la cola el estado de la tortuga
        // #  ] : sacamos(se recupera) el estado de la torgua con sus movimientos 

        // # para 3d
        // #(pitch) & : rotamos la tortuga hacia abajo en modo 3D
        // #(pitch)^: rotamos la tortuga hacia arriba en modo 3D
        // # (roll) \\: rotamos la tortuga hacia la izquierda en modo 3D
        // # (roll) //: rotamos la tortuga hacia la derecha en modo 3D



        Vector3 currentPosition = Vector3.zero; // Posición inicial de la tortuga
        Quaternion currentRotation = Quaternion.identity; // Orientación inicial de la tortuga
        Stack<TurtleState> stateStack = new Stack<TurtleState>(); //pila donde almacenamos el estado de la tortuga(que accion hizo)

        foreach (char symbol in sequence)
        {
            if (symbol == 'F') //si el el simbolo de la secuencia es F, la tortuga avanza 10 pixeles desde la direccion actual y dibuja un segmento
            {
                Vector3 nextPosition = currentPosition + (currentRotation * Vector3.up * segmentLength); // calculamos su siguiente posicion mediante la posicion actual, la rotacion y la longitud del segmento
                CreateBranch(currentPosition, nextPosition);//creamos el segmento entre la posicion actual y la siguiente
                currentPosition = nextPosition;//actualizamos la posicion actual de la tortuga a la siguiente
            }

            else if (symbol == 'f')
            {
                currentPosition += currentRotation * Vector3.up * segmentLength; //la tortuga avanza la misma distancia pero sin dibujar mediante la posicion actual, la rotacion y la longitud del segmento
            }

            else if (symbol == '+')
            {
                //# si cambiamos el vector3. por algunos de estos angulos:
                // Vector3.up: aumenta hacia arriba
                // vector3.down: aumenta hacia abajo
                // vector 3.left: aumenta hacia la izquierda
                // Vector3.right: aumenta hacia la derecha
                // vector3.forward: aumenta hacia adelante
                //vector3.back: aumenta hacia atras
                //vector3.zero: no aumenta hacia ningun lado
                //vector3.one: aumenta hacia todos los lados

                //debido al twod y como estamos en un plano 2d al utilizar rigth y rigth en + y - al momento de avanzar la tortuga esta contrapone ambos angulos por lo que las ramas quedan al mismo lado, por eso
                //si usamos forward en ambos crece para ambos lados
                currentRotation = currentRotation * Quaternion.AngleAxis(activeAngle, Vector3.forward);//calculamos la nueva rotacion de la tortuga hacia la izquierda mediante el angulo activo y el eje Y
            }

            else if (symbol == '-')
            {


                currentRotation = currentRotation * Quaternion.AngleAxis(-activeAngle, Vector3.forward);//calculamos la nueva rotacion de la tortuga hacia la derecha mediante el angulo activo y el eje y
            }

            else if (symbol == '[')
            {
                stateStack.Push(new TurtleState(currentPosition, currentRotation)); //guardarmos el estado actual de la tortuga en la pila 
            }
            else if (symbol == ']') //si el simbolo de la secuenecia es ], se recupera el estado de la tortuga desde la pila 
            {
                if (stateStack.Count > 0)//si la pila tiene elementos, se recupera el estado de la tortuga desde la pila y se actualiza la posicion y rotacion actual de la tortuga
                {
                    TurtleState state = stateStack.Pop();//se recuperar(se saca) el estado de la tortuga de la pila
                    currentPosition = state.position;//la posicion actual de la tortuga se actualiza con la posicion del estado recuperado
                    currentRotation = state.rotation;//la rotacion actual de la tortuga se actualiza con la rotacion del estado recuperado
                }
            }
            else if (symbol == '&')// si el simbolo de la secuencia es &, se realiza una rotacion hacia abajo de la tortuga en modo 3D
            {
                if (generationMode == GenerationMode.ThreeD) // si el modo de generacion es 3D, se calcula la nueva rotacion de la tortuga hacia abajo mediante el angulo activo y el eje X
                {
                    currentRotation = currentRotation * Quaternion.AngleAxis(activeAngle, Vector3.right); // calculamos la rotacion actual de la tortuga hacia abajo mediante el angulo activo y el eje X
                }
            }
            else if (symbol == '^') // si el simbolo de la secuencia es ^, se realiza una rotacion hacia arriba de la tortuga en modo 3D
            {
                if (generationMode == GenerationMode.ThreeD)// si el modo de generacion es 3D, se calcula la nueva rotacion de la tortuga hacia arriba mediante el angulo activo y el eje X
                {
                    currentRotation = currentRotation * Quaternion.AngleAxis(-activeAngle, Vector3.right);//calculamos la rotacion actual mediante el angulo activo y el eje X hacia arriba
                }
            }
            else if (symbol == '\\') // si el simbolo de la secuencia es \\, se realiza una rotacion hacia la izquierda de la tortuga en modo 3D
            {
                if (generationMode == GenerationMode.ThreeD)// si el modo de generacion es 3D, se calcula la nueva rotacion de la tortuga hacia la izquierda mediante el angulo activo y el eje Z
                {
                    currentRotation = currentRotation * Quaternion.AngleAxis(activeAngle, Vector3.forward);// calculamos la rotacion actual de la tortuga hacia la izquierda mediante el angulo activo y el eje Z
                }
            }
            else if (symbol == '/')// si el simbolo de la secuencia es /, se realiza una rotacion hacia la derecha de la tortuga en modo 3D
            {
                if (generationMode == GenerationMode.ThreeD)// si el modo de generacion es 3D, se calcula la nueva rotacion de la tortuga hacia la derecha mediante el angulo activo y el eje Z
                {
                    currentRotation = currentRotation * Quaternion.AngleAxis(-activeAngle, Vector3.forward);// calculamos la rotacion actual de la tortuga hacia la derecha mediante el angulo activo y el eje Z
                }
            }

        }
    }


    // -------------------------------------------------------------------------
    // CREACIÓN DE SEGMENTOS
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // CreateBranch() recibe dos posiciones y crea un cilindro entre ellas.
    //
    // La lógica de visualización no forma parte de la implementación evaluada.
    //
    private void CreateBranch(
        Vector3 start,
        Vector3 end)
    {
        Vector3 direction =
            end - start;

        float length =
            direction.magnitude;

        if (length <= 0f)
        {
            return;
        }


        GameObject branch =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        branchCounter++;

        branch.name =
            "Branch_" +
            branchCounter.ToString("0000");


        branch.transform.SetParent(
            generatedRoot,
            false
        );


        Vector3 middlePoint =
            (start + end) *
            0.5f;


        branch.transform.localPosition =
            middlePoint;


        branch.transform.localRotation =
            Quaternion.FromToRotation(
                Vector3.up,
                direction.normalized
            );


        branch.transform.localScale =
            new Vector3(
                branchRadius,
                length * 0.5f,
                branchRadius
            );


        Renderer renderer =
            branch.GetComponent<Renderer>();


        if (renderer != null &&
            branchMaterial != null)
        {
            renderer.sharedMaterial =
                branchMaterial;
        }


        if (renderer != null &&
            useHeightColor)
        {
            ApplyHeightColor(
                renderer,
                middlePoint.y
            );
        }


        Collider collider =
            branch.GetComponent<Collider>();

        if (collider != null)
        {
            SafeDestroy(
                collider
            );
        }
    }


    // -------------------------------------------------------------------------
    // COLOR SEGÚN ALTURA
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    private void ApplyHeightColor(
        Renderer renderer,
        float height)
    {
        float safeHeight =
            Mathf.Max(
                0.1f,
                colorHeight
            );


        float t =
            Mathf.InverseLerp(
                0f,
                safeHeight,
                height
            );


        Color branchColor =
            Color.Lerp(
                lowColor,
                highColor,
                t
            );


        MaterialPropertyBlock block =
            new MaterialPropertyBlock();


        renderer.GetPropertyBlock(
            block
        );


        Material material =
            renderer.sharedMaterial;


        if (material != null)
        {
            if (material.HasProperty(
                "_BaseColor"))
            {
                block.SetColor(
                    "_BaseColor",
                    branchColor
                );
            }


            if (material.HasProperty(
                "_Color"))
            {
                block.SetColor(
                    "_Color",
                    branchColor
                );
            }
        }


        renderer.SetPropertyBlock(
            block
        );
    }


    // -------------------------------------------------------------------------
    // INFRAESTRUCTURA DE VISUALIZACIÓN
    // -------------------------------------------------------------------------

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
        {
            return;
        }


        Transform existing =
            transform.Find(
                "Generated Tree"
            );


        if (existing != null)
        {
            generatedRoot =
                existing;

            return;
        }


        GameObject root =
            new GameObject(
                "Generated Tree"
            );


        root.transform.SetParent(
            transform,
            false
        );


        root.transform.localPosition =
            Vector3.zero;

        root.transform.localRotation =
            Quaternion.identity;

        root.transform.localScale =
            Vector3.one;


        generatedRoot =
            root.transform;
    }


    public void ClearGeneratedTree()
    {
        if (generatedRoot == null)
        {
            Transform existing =
                transform.Find(
                    "Generated Tree"
                );


            if (existing != null)
            {
                generatedRoot =
                    existing;
            }
        }


        if (generatedRoot != null)
        {
            SafeDestroy(
                generatedRoot.gameObject
            );

            generatedRoot =
                null;
        }


        branchCounter = 0;
    }


    // -------------------------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------------------------

    private void PrintDerivation(
        string activeAxiom,
        List<LSystemRule> activeRules,
        List<string> derivation,
        string finalSequence)
    {
        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "===== L-SYSTEM TREE ====="
        );


        output.AppendLine();

        output.AppendLine(
            "MODE:"
        );

        output.AppendLine(
            generationMode.ToString()
        );


        output.AppendLine();

        output.AppendLine(
            "AXIOM:"
        );

        output.AppendLine(
            activeAxiom
        );


        output.AppendLine();

        output.AppendLine(
            "RULES:"
        );


        foreach (LSystemRule rule in activeRules)
        {
            if (rule == null)
            {
                continue;
            }


            output.AppendLine(
                rule.predecessor +
                " -> " +
                rule.successor
            );
        }


        output.AppendLine();

        output.AppendLine(
            "DERIVATION:"
        );


        for (int i = 0;
             i < derivation.Count;
             i++)
        {
            output.AppendLine(
                "Iteration " +
                i +
                ": " +
                derivation[i]
            );
        }


        output.AppendLine();

        output.AppendLine(
            "FINAL STRING:"
        );

        output.AppendLine(
            finalSequence
        );


        Debug.Log(
            output.ToString(),
            this
        );
    }


    private void SafeDestroy(
        Object target)
    {
        if (target == null)
        {
            return;
        }


        if (Application.isPlaying)
        {
            Destroy(
                target
            );
        }
        else
        {
            DestroyImmediate(
                target
            );
        }
    }
}