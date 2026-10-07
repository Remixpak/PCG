using UnityEngine;
using UnityEngine.Tilemaps;

public class LightsOutVisualizer : MonoBehaviour
{
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
}