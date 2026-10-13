using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LightsOutBoard))]
public class LightsOutBoardEditor : Editor
{
    // mejor tener este script para generar rapidamente un tablero desde el editor 

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LightsOutBoard board = (LightsOutBoard)target;

        EditorGUILayout.Space(15);

        // Boton para generar un nuevo tablero desde el Editor
        if (GUILayout.Button("Generar Nuevo Tablero (Backward)", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                board.InitializeBoard();
            }
            else
            {
                Debug.LogWarning("La generación requiere estar en modo Play.");
            }
        }

        EditorGUILayout.Space(10); // Un pequeño espacio para separar los botones

        // Boton para resolver el tablero usando el Algoritmo Genético
        if (GUILayout.Button("Resolver con Algoritmo Genético (GA)", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                // Buscamos el componente LightsOutGA en el mismo GameObject
                LightsOutGA ga = board.GetComponent<LightsOutGA>();

                if (ga != null)
                {
                    ga.RunEvolution();
                }
                else
                {
                    Debug.LogError("Falta adjuntar el script LightsOutGA al objeto del tablero.");
                }
            }
            else
            {
                Debug.LogWarning("El algoritmo genético requiere estar en modo Play para ejecutarse.");
            }
        }


        EditorGUILayout.Space(10); // espacio de seperacion entre botones

        //boton para resolver el tablero usando el algoritmo genetico + el tabu tomandolo desde el punto donde deja la mejor generacion de individuos para buscar los mejores movimiento en la cantidad de iteraciones fijadas o menos
        if (GUILayout.Button("Resolver con GA + Tabu Search", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                LightsOutGA ga = board.GetComponent<LightsOutGA>();
                LightsOutTabu tabu = board.GetComponent<LightsOutTabu>();

                if (ga != null && tabu != null)
                {
                    // ejecutamos el algotirmo genetico 
                    LightsOutGA.Individual bestGA = ga.RunEvolution();

                    // si el algortimo genetico no logro llegar a 0 pasamos a ejecutar el tabu search
                    if (bestGA != null && bestGA.lightsLeft > 0)
                    {
                        Debug.LogWarning($"[ORQUESTADOR] GA estancado con {bestGA.lightsLeft} luces. Pasando genes a Tabu Search...");
                        tabu.RunTabuSearch(bestGA.genes);
                    }
                    else if (bestGA != null && bestGA.lightsLeft == 0)
                    {
                        Debug.Log("[ORQUESTADOR] GA encontró la solución perfecta. Tabu Search no fue necesario.");
                    }
                }
                else
                {
                    Debug.LogError("Faltan adjuntar los scripts LightsOutGA o LightsOutTabu al objeto.");
                }
            }

        }
    }
}