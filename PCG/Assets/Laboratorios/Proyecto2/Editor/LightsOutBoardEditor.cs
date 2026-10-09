using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LightsOutBoard))]
public class LightsOutBoardEditor : Editor
{
    //mejor tener este script para generar rapidamente un tablero desde el editor 

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
    }
}