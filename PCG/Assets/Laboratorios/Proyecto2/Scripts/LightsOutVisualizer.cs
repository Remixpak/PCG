using System;
using UnityEngine;
using UnityEngine.UI;

public class LightsOutVisualizer : MonoBehaviour
{
    [Header("Interfaz de Usuario")]
    [SerializeField] private GridLayoutGroup uiGrid;
    [SerializeField] private Button uiButtonPrefab;
    [SerializeField] private Sprite buttonOffSprite;
    [SerializeField] private Sprite buttonOnSprite;

    [Header("Ajuste Dinámico")]
    [SerializeField] private bool keepSquareAspect = true; // Mantiene los botones 1:1

    private Image[,] buttonImages;
    private int[,] currentBoard;
    private int boardWidth;
    private int boardHeight;

    public event Action<int, int> OnCellClicked;

    public void RenderBoard(int[,] board, int width, int height)
    {
        if (uiGrid == null || uiButtonPrefab == null)
        {
            Debug.LogError("Faltan referencias de UI en el visualizador.");
            return;
        }

        boardWidth = width;
        boardHeight = height;
        currentBoard = (int[,])board.Clone();

        if (buttonImages == null || buttonImages.GetLength(0) != width || buttonImages.GetLength(1) != height)
        {
            RebuildGridUI(width, height);
        }

        UpdateVisuals();
    }

    private void RebuildGridUI(int width, int height)
    {
        foreach (Transform child in uiGrid.transform)
        {
            Destroy(child.gameObject);
        }

        // Configurar restricción de columnas y calcular el tamaño dinámico de celda
        uiGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        uiGrid.constraintCount = width;

        AdjustCellSize(width, height);

        buttonImages = new Image[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Button newBtn = Instantiate(uiButtonPrefab, uiGrid.transform);
                newBtn.name = $"Cell_{x}_{y}";

                int capturedX = x;
                int capturedY = y;

                newBtn.onClick.AddListener(() => HandleButtonClick(capturedX, capturedY));
                buttonImages[x, y] = newBtn.GetComponent<Image>();
            }
        }
    }

    /// <summary>
    /// Calcula y aplica el Cell Size óptimo para que la grilla encaje en su RectTransform.
    /// </summary>
    private void AdjustCellSize(int columns, int rows)
    {
        // Asegura que el RectTransform tenga sus dimensiones calculadas si se llama en Awake/Start
        Canvas.ForceUpdateCanvases();

        RectTransform gridRect = uiGrid.GetComponent<RectTransform>();

        float totalWidth = gridRect.rect.width;
        float totalHeight = gridRect.rect.height;

        // Restar el Padding configurado en el Inspector del GridLayoutGroup
        float availableWidth = totalWidth - uiGrid.padding.left - uiGrid.padding.right;
        float availableHeight = totalHeight - uiGrid.padding.top - uiGrid.padding.bottom;

        // Restar el Spacing total entre celdas (hay N-1 espacios entre N celdas)
        availableWidth -= uiGrid.spacing.x * (columns - 1);
        availableHeight -= uiGrid.spacing.y * (rows - 1);

        // Calcular el tamaño máximo por eje (evitando números negativos por seguridad)
        float cellWidth = Mathf.Max(0f, availableWidth / columns);
        float cellHeight = Mathf.Max(0f, availableHeight / rows);

        if (keepSquareAspect)
        {
            // Usar el lado menor para garantizar que los botones sean cuadrados y no se salgan en ningún eje
            float squareSize = Mathf.Min(cellWidth, cellHeight);
            uiGrid.cellSize = new Vector2(squareSize, squareSize);
        }
        else
        {
            // Estira los botones para llenar todo el rectángulo aunque se deformen
            uiGrid.cellSize = new Vector2(cellWidth, cellHeight);
        }
    }

    // Si la ventana cambia de tamaño en tiempo de ejecución, reajustar automáticamente
    private void OnRectTransformDimensionsChange()
    {
        if (uiGrid != null && boardWidth > 0 && boardHeight > 0)
        {
            AdjustCellSize(boardWidth, boardHeight);
        }
    }

    private void HandleButtonClick(int x, int y)
    {
        ToggleCell(x, y);
        ToggleCell(x + 1, y);
        ToggleCell(x - 1, y);
        ToggleCell(x, y + 1);
        ToggleCell(x, y - 1);

        UpdateVisuals();
        OnCellClicked?.Invoke(x, y);

        if (IsBoardSolved())
        {
            Debug.Log("¡Tablero resuelto!");
        }
    }

    private void ToggleCell(int x, int y)
    {
        if (x >= 0 && x < boardWidth && y >= 0 && y < boardHeight)
        {
            currentBoard[x, y] ^= 1;
        }
    }

    public void UpdateVisuals()
    {
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (buttonImages[x, y] != null)
                {
                    buttonImages[x, y].sprite = (currentBoard[x, y] == 1) ? buttonOnSprite : buttonOffSprite;
                }
            }
        }
    }

    public bool IsBoardSolved()
    {
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (currentBoard[x, y] == 1) return false;
            }
        }
        return true;
    }
}