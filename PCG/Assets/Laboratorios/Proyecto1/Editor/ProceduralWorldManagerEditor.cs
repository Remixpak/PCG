#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralWorldManager))]
public class ProceduralWorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Dibuja los campos predeterminados del Script (walkIterations, totalSteps, treeSpawnChance, etc.)
        DrawDefaultInspector();

        ProceduralWorldManager manager = (ProceduralWorldManager)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Acciones de Generación", EditorStyles.boldLabel);

        // Botón principal para ejecutar el flujo completo
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generar Mundo Completo (Terreno + Random Walk + Árboles)", GUILayout.Height(40)))
        {
            manager.GenerateFullWorld();
        }

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("Limpiar Árboles", GUILayout.Height(25)))
        {
            manager.ClearTrees();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif