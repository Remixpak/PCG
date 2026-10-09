using UnityEngine;
using UnityEngine.Tilemaps;

public class LightsOutVisualizer : MonoBehaviour


// Este script se encarga de visualizar el estado del tablero de Lights Out en un Tilemap de Unity.
//basicamente es el apartado visual del tablero
{
    [Header("Referencias de Tilemap")]
    public Tilemap tilemap;
    public TileBase tileOff; // Asignar el Tile para estado 0 (Apagado)
    public TileBase tileOn;  // Asignar el Tile para estado 1 (Encendido)

    [Header("Ajuste de Centrado")]
    public bool centerGrid = true;

    public void RenderBoard(int[,] board, int width, int height)
    {
        if (tilemap == null)
        {
            Debug.LogError("No se ha asignado el Tilemap en el visualizador.");
            return;
        }

        // Limpiar cualquier tile previo para evitar superposiciones
        tilemap.ClearAllTiles();

        // Offset para centrar la grilla respecto al origen (0,0) del mundo
        Vector3Int offset = centerGrid
            ? new Vector3Int(-width / 2, -height / 2, 0)
            : Vector3Int.zero;

        // Recorrer la matriz de datos y pintar los tiles correspondientes
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
}