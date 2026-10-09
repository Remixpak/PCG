using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class LightsOutVisualizer : MonoBehaviour
{

    //basicamente este script se encarga de renderizar la grilla en el Tilemap, usando los Tiles asignados para cada estado (0 o 1)
    //en pocas palabras es la parte grafica asi que no se toca

    [Header("Referencias de Tilemap")]
    public Tilemap tilemap;
    public TileBase tileOff; // Asignar el Tile para estado 0
    public TileBase tileOn;  // Asignar el Tile para estado 1

    [Header("Ajuste de Centrado")]
    public bool centerGrid = true;

    public void RenderBoard(int[,] board, int width, int height)
    {
        if (tilemap == null)
        {
            Debug.LogError("No se ha asignado el Tilemap en el visualizador.");
            return;
        }

        tilemap.ClearAllTiles();

        // Offset para centrar la grilla respecto al origen (0,0)
        Vector3Int offset = centerGrid 
            ? new Vector3Int(-width / 2, -height / 2, 0) 
            : Vector3Int.zero;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0) + offset;
                TileBase selectedTile = (board[x, y] == 1) ? tileOn : tileOff;

                tilemap.SetTile(tilePosition, selectedTile);
            }
        }
    }

    public void LightTile(int x, int y)
    {

    }

    public Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 1. Verificar si hay un mouse conectado y si se presionó el botón izquierdo en este frame
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 2. Obtener la posición del mouse en la pantalla como Vector2
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // 3. Convertir la posición de la pantalla a posición en el mundo
            // Pasamos un Vector3 para asegurarnos de que la cámara lo proyecte correctamente
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));

            // Asegurarse de que el eje Z sea 0 (o la altura de tu tilemap)
            mouseWorldPos.z = 0;

            // 4. Convertir esa posición del mundo a coordenadas de la celda (Grid)
            Vector3Int cellPosition = tilemap.WorldToCell(mouseWorldPos);

            // 5. Comprobar si hay un tile en esa posición
            if (tilemap.HasTile(cellPosition))
            {
                TileBase clickedTile = tilemap.GetTile(cellPosition);

                Debug.Log($"Tile clickeado en la coordenada: {cellPosition}");
                Debug.Log($"El tile es de tipo: {clickedTile.name}");

                // Aquí puedes agregar tu lógica (construir, destruir, mover personaje, etc.)
            }
        }
    }
}