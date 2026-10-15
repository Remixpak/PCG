using System;
using UnityEngine;
using UnityEngine.UI;

public class LightsOutVisualizer : MonoBehaviour
{
    [Header("Interfaz de Usuario")]
    [SerializeField] private GridLayoutGroup uiGrid; // El contenedor con el componente GridLayoutGroup
    [SerializeField] private Button uiButtonPrefab;  // Prefab del botón (debe tener componente Button e Image)
    [SerializeField] private Sprite buttonOffSprite; // Sprite para el estado 0 (Apagado)
    [SerializeField] private Sprite buttonOnSprite;  // Sprite para el estado 1 (Encendido)

    // Matrices en caché para evitar recrear objetos en cada clic
    private Image[,] buttonImages;
    private int[,] currentBoard;
    private int boardWidth;
    private int boardHeight;

    // Evento opcional por si otro script (ej. un GameManager) quiere escuchar los clics o verificar victoria
    public event Action<int, int> OnCellClicked;

    /// <summary>
    /// Inicializa o reconstruye el tablero completo en la UI.
    /// </summary>
    public void RenderBoard(int[,] board, int width, int height)
    {
        if (uiGrid == null || uiButtonPrefab == null)
        {
            Debug.LogError("Faltan referencias de UI (uiGrid o uiButtonPrefab) en el visualizador.");
            return;
        }

        boardWidth = width;
        boardHeight = height;

        // Copiamos el estado del tablero para poder manipularlo al jugar
        currentBoard = (int[,])board.Clone();

        // Si las dimensiones cambiaron o no se han creado los botones aún, regeneramos la grilla
        if (buttonImages == null || buttonImages.GetLength(0) != width || buttonImages.GetLength(1) != height)
        {
            RebuildGridUI(width, height);
        }

        // Actualizamos los sprites según la matriz
        UpdateVisuals();
    }

    /// <summary>
    /// Limpia el contenedor y crea los botones asignándoles su evento de clic.
    /// </summary>
    private void RebuildGridUI(int width, int height)
    {
        // 1. Limpiar botones previos si existían
        foreach (Transform child in uiGrid.transform)
        {
            Destroy(child.gameObject);
        }

        // 2. Configurar las columnas del GridLayoutGroup automáticamente
        uiGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        uiGrid.constraintCount = width;

        buttonImages = new Image[width, height];

        // 3. Instanciar botones.
        // Nota: Si tu GridLayoutGroup tiene Start Corner = Upper Left (por defecto), 
        // recorremos 'y' de arriba hacia abajo o en el orden de tu matriz para que coincida visualmente.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Button newBtn = Instantiate(uiButtonPrefab, uiGrid.transform);
                newBtn.name = $"Cell_{x}_{y}";

                // Variables locales necesarias para capturar el valor correcto en el cierre lambda (closure)
                int capturedX = x;
                int capturedY = y;

                newBtn.onClick.AddListener(() => HandleButtonClick(capturedX, capturedY));

                // Guardamos la referencia al componente Image del botón para cambiar su sprite rápido
                buttonImages[x, y] = newBtn.GetComponent<Image>();
            }
        }
    }

    /// <summary>
    /// Lógica al presionar un botón: aplica la regla de Lights Out (invertir celda y adyacentes).
    /// </summary>
    private void HandleButtonClick(int x, int y)
    {
        // Invertir la celda presionada y sus 4 vecinos ortogonales
        ToggleCell(x, y);
        ToggleCell(x + 1, y);
        ToggleCell(x - 1, y);
        ToggleCell(x, y + 1);
        ToggleCell(x, y - 1);

        UpdateVisuals();

        // Notificar a otros scripts si es necesario
        OnCellClicked?.Invoke(x, y);

        if (IsBoardSolved())
        {
            Debug.Log("¡Tablero resuelto!");
        }
    }

    private void ToggleCell(int x, int y)
    {
        // Verificar que la coordenada esté dentro de los límites del tablero
        if (x >= 0 && x < boardWidth && y >= 0 && y < boardHeight)
        {
            // Operación XOR con 1: si es 0 pasa a 1, si es 1 pasa a 0
            currentBoard[x, y] ^= 1;
        }
    }

    /// <summary>
    /// Refresca únicamente los sprites de los botones existentes sin instanciar nada.
    /// </summary>
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

    /// <summary>
    /// Comprueba si todas las luces están apagadas (estado 0).
    /// </summary>
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