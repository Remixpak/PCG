using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DungeonStudioWindow : EditorWindow
{
    // 1. Crea una opción en el menú superior de Unity para abrir la ventana
    [MenuItem("Tools/Mixed-Initiative Dungeon Generator")]
    public static void ShowWindow()
    {
        // Abre la ventana (o la enfoca si ya está abierta)
        DungeonStudioWindow window = GetWindow<DungeonStudioWindow>();

        // Define el título de la pestaña
        window.titleContent = new GUIContent("Dungeon Studio");

        // Define un tamaño mínimo recomendado para que no se rompa tu UI
        window.minSize = new Vector2(800, 600);
    }

    public void CreateGUI()
    {
        // Carga el archivo UXML desde la ruta exacta en tu proyecto
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/DungeonStudio/Editor/DungeonStudioWindow.uxml"
        );

        if (visualTree != null)
        {
            visualTree.CloneTree(rootVisualElement);
        }
        else
        {
            Debug.LogError("No se encontró DungeonStudioWindow.uxml. Verifica la ruta.");
        }
    }
}