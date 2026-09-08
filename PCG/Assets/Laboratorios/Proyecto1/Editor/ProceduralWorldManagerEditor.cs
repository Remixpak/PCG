#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralWorldManager))]
public class ProceduralWorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralWorldManager manager = (ProceduralWorldManager)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Acciones de Generación", EditorStyles.boldLabel);

        if (GUILayout.Button("Generar Nueva Semilla", GUILayout.Height(40)))
        {
            manager.GenerateSeed();
        }

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generar Mundo Completo (Terreno + Random Walk + Árboles)", GUILayout.Height(40)))
        {
            manager.GenerateFullWorld();
        }

        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
        if (GUILayout.Button("Generar Mundo Acuático (Terreno + Random Walk + Coral)", GUILayout.Height(40)))
        {
            manager.GenerateSeaWorld();
        }

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);

        if (GUILayout.Button("Limpiar Terreno con Árboles", GUILayout.Height(30)))
        {
            manager.ClearForestWorld();
        }

        if (GUILayout.Button("Limpiar Terreno con Corales", GUILayout.Height(30)))
        {
            manager.ClearSeaWorld();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif