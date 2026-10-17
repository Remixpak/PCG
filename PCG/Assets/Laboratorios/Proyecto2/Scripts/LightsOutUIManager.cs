using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(LightsOutBoard))]
[RequireComponent(typeof(LightsOutGA))]
[RequireComponent(typeof(LightsOutTabu))]
public class LightsOutUIManager : MonoBehaviour
{
    [Header("Contenedor y Tipografía")]
    [Tooltip("Arrastra aquí el Panel de la derecha donde se generará toda la interfaz.")]
    [SerializeField] private GameObject uiPanelContainer;

    [Tooltip("Fuente TextMeshPro que utilizarán todos los textos creados.")]
    [SerializeField] private TMP_FontAsset uiFont;

    [Header("Estilo Visual")]
    [SerializeField] private Color headerColor = new Color(0.22f, 0.20f, 0.25f, 1f);
    [SerializeField] private Color labelColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color controlBgColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    [SerializeField] private Color popupCardColor = new Color(0.93f, 0.90f, 0.82f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.32f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color accentColor = new Color(0.95f, 0.68f, 0.25f, 1f);
    [SerializeField] private Color stepAppliedColor = new Color(0.22f, 0.55f, 0.30f, 1f);

    // Referencias a los componentes en el mismo GameObject
    private LightsOutBoard boardScript;
    private LightsOutGA gaScript;
    private LightsOutTabu tabuScript;

    // Sub-paneles para alternar entre Parámetros y Popup manteniendo los botones abajo
    private GameObject parametersSubPanel;
    private GameObject resultPopupSubPanel;
    private Transform stepsContentTransform;
    private TMP_Text statsBodyText;
    private TMP_Text popupTitleText;

    // Lista para poder reactivar los botones de "Aplicar" si el jugador presiona "Restablecer Tablero"
    private readonly List<Button> activeStepButtons = new List<Button>();

    // Sistema de Tooltip flotante (compatible con el nuevo Input System via EventSystem)
    private GameObject tooltipObject;
    private TMP_Text tooltipText;
    private RectTransform tooltipRect;
    private Canvas parentCanvas;

    private void Awake()
    {
        boardScript = GetComponent<LightsOutBoard>();
        gaScript = GetComponent<LightsOutGA>();
        tabuScript = GetComponent<LightsOutTabu>();

        ApplyDefaultParameters();
    }

    private void Start()
    {
        if (uiPanelContainer == null)
        {
            Debug.LogError("[LightsOutUIManager] No se ha asignado el 'uiPanelContainer' en el Inspector.");
            return;
        }

        parentCanvas = uiPanelContainer.GetComponentInParent<Canvas>();

        BuildCompleteUI();
        CreateTooltipSystem();
    }

    private void ApplyDefaultParameters()
    {
        boardScript.width = 5;
        boardScript.height = 5;
        boardScript.backwardSteps = 15;
        boardScript.seed = 33;
        boardScript.useRandomSeedOnStart = true;

        gaScript.populationSize = 100;
        gaScript.maxGenerations = 50;
        gaScript.crossoverRate = 0.20f;
        gaScript.mutationRate = 0.05f;

        gaScript.clickPenaltyWeight = 50;
        gaScript.tournamentSize = 5;
        gaScript.filterPoolSize = 3;

        tabuScript.tabuTenure = 5;
        tabuScript.maxTabuIterations = 100;
        tabuScript.clickPenaltyWeight = 50;
    }

    private void BuildCompleteUI()
    {
        foreach (Transform child in uiPanelContainer.transform)
        {
            Destroy(child.gameObject);
        }

        VerticalLayoutGroup rootLayout = GetOrAddComponent<VerticalLayoutGroup>(uiPanelContainer);
        rootLayout.padding = new RectOffset(18, 18, 14, 14);
        rootLayout.spacing = 10f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        TMP_DefaultControls.Resources uiResources = new TMP_DefaultControls.Resources();

        // 1. Sub-Panel de Parámetros
        BuildParametersView(uiResources);

        // 2. Sub-Panel Popup de Resultados
        BuildResultPopupView(uiResources);

        // 3. Sub-Panel Inferior con los botones de acción
        BuildBottomButtonsView(uiResources);

        ShowParametersView();
    }

    #region Construcción de Vista de Parámetros

    private void BuildParametersView(TMP_DefaultControls.Resources uiResources)
    {
        parametersSubPanel = new GameObject("ParametersPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        parametersSubPanel.transform.SetParent(uiPanelContainer.transform, false);

        LayoutElement panelLE = parametersSubPanel.GetComponent<LayoutElement>();
        panelLE.flexibleWidth = 1f;
        panelLE.flexibleHeight = 1f;

        VerticalLayoutGroup vlg = parametersSubPanel.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // --- SECCIÓN: TABLERO ---
        CreateSectionHeader(parametersSubPanel.transform, "Configuración del Tablero");

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Tamaño de Grilla",
            "Define el ancho y alto del tablero cuadrado (N x N).",
            boardScript.width, 2, (val) => { boardScript.width = val; boardScript.height = val; });

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Pasos Hacia Atrás",
            "Cantidad de clics inversos desde el estado meta para garantizar que tenga solución.",
            boardScript.backwardSteps, 1, (val) => boardScript.backwardSteps = val);

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Semilla (Seed)",
            "Valor numérico para generar siempre el mismo tablero aleatorio.",
            boardScript.seed, int.MinValue, (val) => boardScript.seed = val);

        CreateToggleRow(parametersSubPanel.transform, "Usar Semilla Aleatoria al Iniciar",
            "Si está activo, ignora la semilla fija y genera un tablero distinto cada vez.",
            boardScript.useRandomSeedOnStart, (val) => boardScript.useRandomSeedOnStart = val);

        // --- SECCIÓN: ALGORITMO GENÉTICO ---
        CreateSectionHeader(parametersSubPanel.transform, "Algoritmo Genético (GA)");

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Tamaño de Población",
            "Número total de individuos evaluados en cada generación.",
            gaScript.populationSize, 2, (val) => gaScript.populationSize = val);

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Generaciones Máximas",
            "Límite máximo de ciclos evolutivos antes de detener el algoritmo genético.",
            gaScript.maxGenerations, 1, (val) => gaScript.maxGenerations = val);

        CreateSteppedSliderRow(parametersSubPanel.transform, "Tasa de Cruce (Crossover)",
            "Probabilidad (entre 0 y 1, en pasos de 0.05) de cruzar los genes de dos padres.",
            gaScript.crossoverRate, 0f, 1f, 0.05f, (val) => gaScript.crossoverRate = val);

        CreateSteppedSliderRow(parametersSubPanel.transform, "Tasa de Mutación",
            "Probabilidad (entre 0 y 1, en pasos de 0.05) de invertir aleatoriamente un gen.",
            gaScript.mutationRate, 0f, 1f, 0.05f, (val) => gaScript.mutationRate = val);

        // --- SECCIÓN: GA - FITNESS ---
        CreateSectionHeader(parametersSubPanel.transform, "GA - Fitness y Selección");

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Penalización por Clic",
            "Puntos restados al fitness por cada clic utilizado, premiando soluciones cortas.",
            gaScript.clickPenaltyWeight, 0, (val) =>
            {
                gaScript.clickPenaltyWeight = val;
                tabuScript.clickPenaltyWeight = val;
            });

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Tamaño de Torneo",
            "Cantidad de individuos al azar que compiten para ser seleccionados como padres.",
            gaScript.tournamentSize, 2, (val) => gaScript.tournamentSize = val);

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Tamaño del Pool de Filtro",
            "Cantidad de mejores individuos entre los cuales se elige la élite para desempatar.",
            gaScript.filterPoolSize, 1, (val) => gaScript.filterPoolSize = val);

        CreateSectionHeader(parametersSubPanel.transform, "Búsqueda Local (Tabu Search)");

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Tabu Tenure",
            "Cantidad de iteraciones que un movimiento permanece en la lista tabú.",
            tabuScript.tabuTenure, 1, (val) => tabuScript.tabuTenure = val);

        CreateIntInputRow(parametersSubPanel.transform, uiResources, "Iteraciones Máximas",
            "Límite máximo de ciclos para la búsqueda local.",
            tabuScript.maxTabuIterations, 1, (val) => tabuScript.maxTabuIterations = val);

    }

    #endregion

    #region Construcción del Popup de Resultados

    private void BuildResultPopupView(TMP_DefaultControls.Resources uiResources)
    {
        resultPopupSubPanel = new GameObject("ResultPopupPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        resultPopupSubPanel.transform.SetParent(uiPanelContainer.transform, false);

        LayoutElement popupLE = resultPopupSubPanel.GetComponent<LayoutElement>();
        popupLE.flexibleWidth = 1f;
        popupLE.flexibleHeight = 1f;
        popupLE.preferredHeight = 360f;

        Image popupBg = resultPopupSubPanel.GetComponent<Image>();
        popupBg.color = popupCardColor;

        VerticalLayoutGroup popupVLG = resultPopupSubPanel.GetComponent<VerticalLayoutGroup>();
        popupVLG.padding = new RectOffset(14, 14, 12, 12);
        popupVLG.spacing = 10f;
        popupVLG.childControlWidth = true;
        popupVLG.childControlHeight = true;
        popupVLG.childForceExpandWidth = true;
        popupVLG.childForceExpandHeight = false;

        // 1. FILA SUPERIOR DEL POPUP: Título "Resultado" + Botón "[X]"
        GameObject topBar = new GameObject("PopupTopBar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        topBar.transform.SetParent(resultPopupSubPanel.transform, false);

        LayoutElement topBarLE = topBar.GetComponent<LayoutElement>();
        topBarLE.preferredHeight = 34f;
        topBarLE.flexibleWidth = 1f;

        HorizontalLayoutGroup topBarHLG = topBar.GetComponent<HorizontalLayoutGroup>();
        topBarHLG.spacing = 8f;
        topBarHLG.childAlignment = TextAnchor.MiddleLeft;
        topBarHLG.childControlWidth = true;
        topBarHLG.childControlHeight = true;
        topBarHLG.childForceExpandWidth = false;

        GameObject titleObj = new GameObject("PopupTitle", typeof(RectTransform), typeof(LayoutElement));
        titleObj.transform.SetParent(topBar.transform, false);
        LayoutElement titleLE = titleObj.GetComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;
        titleLE.preferredHeight = 32f;

        popupTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        popupTitleText.text = "Resultado";
        popupTitleText.fontSize = 20f;
        popupTitleText.fontStyle = FontStyles.Bold;
        popupTitleText.color = headerColor;
        popupTitleText.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(popupTitleText);

        GameObject closeBtnObj = TMP_DefaultControls.CreateButton(uiResources);
        closeBtnObj.name = "Btn_ClosePopup";
        closeBtnObj.transform.SetParent(topBar.transform, false);

        LayoutElement closeLE = GetOrAddComponent<LayoutElement>(closeBtnObj);
        closeLE.preferredWidth = 34f;
        closeLE.minWidth = 34f;
        closeLE.preferredHeight = 32f;

        Image closeImg = closeBtnObj.GetComponent<Image>();
        if (closeImg != null) closeImg.color = new Color(0.75f, 0.28f, 0.28f, 1f);

        TMP_Text closeTxt = closeBtnObj.GetComponentInChildren<TMP_Text>();
        if (closeTxt != null)
        {
            closeTxt.text = "X";
            closeTxt.fontSize = 16f;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.color = Color.white;
            ApplyFont(closeTxt);
        }

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(ShowParametersView);

        // 2. CUERPO CENTRAL DEL POPUP: 2 Columnas (Izquierda: Pasos con botón Aplicar | Derecha: Estadísticas)
        GameObject bodyRow = new GameObject("PopupBodyColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        bodyRow.transform.SetParent(resultPopupSubPanel.transform, false);

        LayoutElement bodyLE = bodyRow.GetComponent<LayoutElement>();
        bodyLE.flexibleWidth = 1f;
        bodyLE.flexibleHeight = 1f;
        bodyLE.preferredHeight = 290f;

        HorizontalLayoutGroup bodyHLG = bodyRow.GetComponent<HorizontalLayoutGroup>();
        bodyHLG.spacing = 12f;
        bodyHLG.childAlignment = TextAnchor.UpperLeft;
        bodyHLG.childControlWidth = true;
        bodyHLG.childControlHeight = true;
        bodyHLG.childForceExpandWidth = true;
        bodyHLG.childForceExpandHeight = true;

        // --- COLUMNA IZQUIERDA: "Pasos" + ScrollView ---
        GameObject leftCol = new GameObject("LeftCol_Steps", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        leftCol.transform.SetParent(bodyRow.transform, false);

        LayoutElement leftLE = leftCol.GetComponent<LayoutElement>();
        leftLE.flexibleWidth = 1.15f; // Ligeramente más ancho para alojar el índice + X/Y + botón Aplicar
        leftLE.preferredWidth = 210f;
        leftLE.flexibleHeight = 1f;

        VerticalLayoutGroup leftVLG = leftCol.GetComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 6f;
        leftVLG.childControlWidth = true;
        leftVLG.childControlHeight = true;
        leftVLG.childForceExpandWidth = true;
        leftVLG.childForceExpandHeight = false;

        CreateSubColumnHeader(leftCol.transform, "Pasos");

        GameObject scrollViewObj = CreateStepsScrollView();
        scrollViewObj.transform.SetParent(leftCol.transform, false);

        // --- COLUMNA DERECHA: "Estadísticas" ---
        GameObject rightCol = new GameObject("RightCol_Stats", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        rightCol.transform.SetParent(bodyRow.transform, false);

        LayoutElement rightLE = rightCol.GetComponent<LayoutElement>();
        rightLE.flexibleWidth = 1f;
        rightLE.preferredWidth = 185f;
        rightLE.flexibleHeight = 1f;

        VerticalLayoutGroup rightVLG = rightCol.GetComponent<VerticalLayoutGroup>();
        rightVLG.spacing = 6f;
        rightVLG.childControlWidth = true;
        rightVLG.childControlHeight = true;
        rightVLG.childForceExpandWidth = true;
        rightVLG.childForceExpandHeight = false;

        CreateSubColumnHeader(rightCol.transform, "Estadísticas");

        GameObject statsCard = new GameObject("StatsBox", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        statsCard.transform.SetParent(rightCol.transform, false);

        LayoutElement statsCardLE = statsCard.GetComponent<LayoutElement>();
        statsCardLE.flexibleWidth = 1f;
        statsCardLE.flexibleHeight = 1f;
        statsCardLE.preferredHeight = 240f;

        Image statsCardImg = statsCard.GetComponent<Image>();
        statsCardImg.color = new Color(1f, 1f, 1f, 0.55f);

        VerticalLayoutGroup statsCardVLG = statsCard.GetComponent<VerticalLayoutGroup>();
        statsCardVLG.padding = new RectOffset(10, 10, 10, 10);
        statsCardVLG.childControlWidth = true;
        statsCardVLG.childControlHeight = true;
        statsCardVLG.childForceExpandWidth = true;
        statsCardVLG.childForceExpandHeight = true;

        GameObject statsTextObj = new GameObject("StatsText", typeof(RectTransform));
        statsTextObj.transform.SetParent(statsCard.transform, false);

        statsBodyText = statsTextObj.AddComponent<TextMeshProUGUI>();
        statsBodyText.fontSize = 14f;
        statsBodyText.color = labelColor;
        statsBodyText.alignment = TextAlignmentOptions.TopLeft;
        statsBodyText.enableWordWrapping = true;
        ApplyFont(statsBodyText);
    }

    private void CreateSubColumnHeader(Transform parent, string title)
    {
        GameObject obj = new GameObject($"SubHeader_{title}", typeof(RectTransform), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        LayoutElement le = obj.GetComponent<LayoutElement>();
        le.preferredHeight = 24f;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = headerColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(tmp);
    }

    private GameObject CreateStepsScrollView()
    {
        DefaultControls.Resources uiRes = new DefaultControls.Resources();

        GameObject scrollObj = new GameObject("StepsScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        LayoutElement scrollLE = scrollObj.GetComponent<LayoutElement>();
        scrollLE.flexibleWidth = 1f;
        scrollLE.flexibleHeight = 1f;
        scrollLE.preferredHeight = 240f;

        Image scrollBg = scrollObj.GetComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.7f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(scrollObj.transform, false);

        Image vpImg = viewport.GetComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);

        RectTransform vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.pivot = new Vector2(0f, 1f);
        vpRect.offsetMin = new Vector2(4f, 4f);
        vpRect.offsetMax = new Vector2(-22f, -4f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentVLG = content.GetComponent<VerticalLayoutGroup>();
        contentVLG.padding = new RectOffset(4, 4, 4, 4);
        contentVLG.spacing = 5f;
        contentVLG.childAlignment = TextAnchor.UpperCenter;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;

        ContentSizeFitter contentCSF = content.GetComponent<ContentSizeFitter>();
        contentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        stepsContentTransform = content.transform;

        GameObject scrollbarObj = DefaultControls.CreateScrollbar(uiRes);
        scrollbarObj.name = "VerticalScrollbar";
        scrollbarObj.transform.SetParent(scrollObj.transform, false);

        RectTransform sbRect = scrollbarObj.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1f, 0f);
        sbRect.anchorMax = new Vector2(1f, 1f);
        sbRect.pivot = new Vector2(1f, 1f);
        sbRect.anchoredPosition = Vector2.zero;
        sbRect.sizeDelta = new Vector2(18f, 0f);

        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

        if (scrollbarObj.TryGetComponent(out Image sbBg))
            sbBg.color = new Color(0.80f, 0.80f, 0.80f, 1f);

        if (scrollbar.handleRect != null && scrollbar.handleRect.TryGetComponent(out Image handleImg))
            handleImg.color = buttonColor;

        scrollRect.viewport = vpRect;
        scrollRect.content = contentRect;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        return scrollObj;
    }

    #endregion

    #region Construcción de los Botones Fijos Inferiores

    private void BuildBottomButtonsView(TMP_DefaultControls.Resources uiResources)
    {
        GameObject buttonsContainer = new GameObject("BottomButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        buttonsContainer.transform.SetParent(uiPanelContainer.transform, false);

        LayoutElement containerLE = buttonsContainer.GetComponent<LayoutElement>();
        containerLE.flexibleWidth = 1f;
        containerLE.preferredHeight = 124f;

        VerticalLayoutGroup vlg = buttonsContainer.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // FILA 1: "Generar Nuevo Tablero (Backward)" + "Restablecer Tablero" lado a lado
        GameObject topButtonsRow = new GameObject("BoardButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        topButtonsRow.transform.SetParent(buttonsContainer.transform, false);

        LayoutElement rowLE = topButtonsRow.GetComponent<LayoutElement>();
        rowLE.preferredHeight = 34f;
        rowLE.flexibleWidth = 1f;

        HorizontalLayoutGroup rowHLG = topButtonsRow.GetComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 8f;
        rowHLG.childAlignment = TextAnchor.MiddleCenter;
        rowHLG.childControlWidth = true;
        rowHLG.childControlHeight = true;
        rowHLG.childForceExpandWidth = true;
        rowHLG.childForceExpandHeight = true;

        // Botón Izquierdo: Generar Nuevo Tablero
        CreateActionButton(topButtonsRow.transform, uiResources, "Generar Nuevo Tablero", () =>
        {
            boardScript.InitializeBoard();
            ShowParametersView();
        }, 1.3f);

        // Botón Derecho: Restablecer el Tablero Actual al estado inicial
        CreateActionButton(topButtonsRow.transform, uiResources, "Restablecer Tablero", () =>
        {
            boardScript.ResetCurrentBoard();
            ResetStepButtonsState();
        }, 1f);

        // FILA 2: Resolver solo con Algoritmo Genético (GA)
        CreateActionButton(buttonsContainer.transform, uiResources, "Resolver con Algoritmo Genético (GA)", () =>
        {
            LightsOutGA.Individual bestGA = gaScript.RunEvolution();
            if (bestGA != null)
            {
                ShowGAResultPopup(bestGA);
            }
        });

        // FILA 3: Resolver con Algoritmo Memético (GA + Tabu Search)
        CreateActionButton(buttonsContainer.transform, uiResources, "Resolver con GA + Tabu Search", () =>
        {
            LightsOutGA.Individual bestGA = gaScript.RunEvolution();
            if (bestGA != null && bestGA.genes != null)
            {
                LightsOutTabu.TabuSolution bestTabu = tabuScript.RunTabuSearch(bestGA.genes);
                if (bestTabu != null)
                {
                    ShowMemeticResultPopup(bestGA, bestTabu);
                }
            }
        });
    }

    #endregion

    #region Lógica de Poblado y Alternancia del Popup

    private void ShowParametersView()
    {
        HideTooltip();
        if (parametersSubPanel != null) parametersSubPanel.SetActive(true);
        if (resultPopupSubPanel != null) resultPopupSubPanel.SetActive(false);
    }

    private void ShowGAResultPopup(LightsOutGA.Individual gaResult)
    {
        HideTooltip();
        parametersSubPanel.SetActive(false);
        resultPopupSubPanel.SetActive(true);

        popupTitleText.text = "Resultado (Algoritmo Genético)";

        bool solved = gaResult.lightsLeft == 0;
        PopulateStepsList(gaResult.genes, solved);

        string estado = solved ? "<color=#1E7E34><b>¡Resuelto!</b></color>" : "<color=#B82E2E><b>Incompleto (Óptimo Local)</b></color>";

        statsBodyText.text =
            $"<b>Estado:</b> {estado}\n\n" +
            $"<b>Luces encendidas:</b> {gaResult.lightsLeft}\n" +
            $"<b>Pasos (Clics):</b> {gaResult.clicksUsed}\n" +
            $"<b>Fitness final:</b> {gaResult.fitness}\n\n" +
            $"<b>Generaciones:</b> {gaResult.generationsRun} / {gaScript.maxGenerations}\n" +
            $"<b>Población:</b> {gaScript.populationSize}";
    }

    private void ShowMemeticResultPopup(LightsOutGA.Individual gaResult, LightsOutTabu.TabuSolution tabuResult)
    {
        HideTooltip();
        parametersSubPanel.SetActive(false);
        resultPopupSubPanel.SetActive(true);

        popupTitleText.text = "Resultado (GA + Tabu Search)";

        bool solved = tabuResult.lightsLeft == 0;
        PopulateStepsList(tabuResult.genes, solved);

        string estado = solved ? "<color=#1E7E34><b>¡Resuelto!</b></color>" : "<color=#B82E2E><b>Incompleto</b></color>";

        statsBodyText.text =
            $"<b>Estado:</b> {estado}\n\n" +
            $"<b>Luces finales:</b> {tabuResult.lightsLeft} <i>(GA dejó {gaResult.lightsLeft})</i>\n" +
            $"<b>Pasos (Clics):</b> {tabuResult.clicksUsed}\n" +
            $"<b>Fitness final:</b> {tabuResult.fitness}\n\n" +
            $"<b>Gen. GA:</b> {gaResult.generationsRun} / {gaScript.maxGenerations}\n" +
            $"<b>Iter. Tabu:</b> {tabuResult.iterationsRun} / {tabuScript.maxTabuIterations}\n" +
            $"<b>Tabu Tenure:</b> {tabuScript.tabuTenure}";
    }

    /// <summary>
    /// Rellena la lista desplazable con el índice del paso, coordenadas X/Y y el botón para aplicarlo en el tablero.
    /// </summary>
    private void PopulateStepsList(int[] genes, bool solved)
    {
        activeStepButtons.Clear();

        foreach (Transform child in stepsContentTransform)
        {
            Destroy(child.gameObject);
        }

        if (!solved)
        {
            CreateStepInfoItem("Sin solución exacta (0 luces).");
            return;
        }

        int width = boardScript.width;
        int stepCount = 0;

        for (int i = 0; i < genes.Length; i++)
        {
            if (genes[i] == 1)
            {
                int x = i % width;
                int y = i / width;
                stepCount++;
                CreateInteractiveStepItem(stepCount, x, y);
            }
        }

        if (stepCount == 0)
        {
            CreateStepInfoItem("0 pasos (Ya estaba resuelto)");
        }
    }

    /// <summary>
    /// Crea una fila con formato: "[Índice]. X:col Y:fila" a la izquierda y un botón "Aplicar" a la derecha.
    /// </summary>
    private void CreateInteractiveStepItem(int stepIndex, int x, int y)
    {
        GameObject itemObj = new GameObject($"Step_{stepIndex}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        itemObj.transform.SetParent(stepsContentTransform, false);

        LayoutElement itemLE = itemObj.GetComponent<LayoutElement>();
        itemLE.preferredHeight = 32f;
        itemLE.flexibleWidth = 1f;

        Image bg = itemObj.GetComponent<Image>();
        bg.color = controlBgColor;

        HorizontalLayoutGroup hlg = itemObj.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 6, 3, 3);
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Etiqueta con el índice del paso y las coordenadas X e Y
        GameObject txtObj = new GameObject("StepLabel", typeof(RectTransform), typeof(LayoutElement));
        txtObj.transform.SetParent(itemObj.transform, false);

        LayoutElement txtLE = txtObj.GetComponent<LayoutElement>();
        txtLE.flexibleWidth = 1f;
        txtLE.preferredHeight = 26f;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{stepIndex}.</b> X:{x}  Y:{y}";
        tmp.fontSize = 13.5f;
        tmp.color = labelColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(tmp);

        // Botón para aplicar este paso directamente sobre el tablero visual
        TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources();
        GameObject applyBtnObj = TMP_DefaultControls.CreateButton(resources);
        applyBtnObj.name = $"Btn_ApplyStep_{stepIndex}";
        applyBtnObj.transform.SetParent(itemObj.transform, false);

        LayoutElement btnLE = GetOrAddComponent<LayoutElement>(applyBtnObj);
        btnLE.preferredWidth = 68f;
        btnLE.minWidth = 64f;
        btnLE.preferredHeight = 24f;

        Image btnImg = applyBtnObj.GetComponent<Image>();
        if (btnImg != null) btnImg.color = buttonColor;

        TMP_Text btnTxt = applyBtnObj.GetComponentInChildren<TMP_Text>();
        if (btnTxt != null)
        {
            btnTxt.text = "Aplicar";
            btnTxt.fontSize = 12f;
            btnTxt.color = Color.white;
            ApplyFont(btnTxt);
        }

        Button applyBtn = applyBtnObj.GetComponent<Button>();
        int targetX = x;
        int targetY = y;

        applyBtn.onClick.AddListener(() =>
        {
            if (boardScript.visualizer != null)
            {
                boardScript.visualizer.HandleButtonClick(targetX, targetY);

                // Marcar visualmente el paso como aplicado para que el jugador no pierda el hilo
                applyBtn.interactable = false;
                if (btnImg != null) btnImg.color = stepAppliedColor;
                if (btnTxt != null) btnTxt.text = "Listo";
            }
        });

        activeStepButtons.Add(applyBtn);
    }

    /// <summary>
    /// Si el usuario presiona "Restablecer Tablero", reactiva todos los botones "Aplicar" de la lista actual.
    /// </summary>
    private void ResetStepButtonsState()
    {
        foreach (Button btn in activeStepButtons)
        {
            if (btn != null)
            {
                btn.interactable = true;
                if (btn.TryGetComponent(out Image btnImg))
                    btnImg.color = buttonColor;

                TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                    txt.text = "Aplicar";
            }
        }
    }

    private void CreateStepInfoItem(string message)
    {
        GameObject itemObj = new GameObject("StepInfoItem", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        itemObj.transform.SetParent(stepsContentTransform, false);

        LayoutElement itemLE = itemObj.GetComponent<LayoutElement>();
        itemLE.preferredHeight = 32f;
        itemLE.flexibleWidth = 1f;

        Image bg = itemObj.GetComponent<Image>();
        bg.color = controlBgColor;

        VerticalLayoutGroup vlg = itemObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(itemObj.transform, false);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 13.5f;
        tmp.color = labelColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(tmp);
    }

    #endregion

    #region Creadores de Filas de Controles

    private void CreateSectionHeader(Transform parent, string title)
    {
        GameObject headerObj = new GameObject($"Header_{title}", typeof(RectTransform), typeof(LayoutElement));
        headerObj.transform.SetParent(parent, false);

        LayoutElement le = headerObj.GetComponent<LayoutElement>();
        le.preferredHeight = 24f;
        le.flexibleWidth = 1f;

        TextMeshProUGUI tmp = headerObj.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = headerColor;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        ApplyFont(tmp);
    }

    private GameObject CreateBaseRow(Transform parent, string rowName, string labelText, string tooltipDescription)
    {
        GameObject rowObj = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObj.transform.SetParent(parent, false);

        LayoutElement rowLE = rowObj.GetComponent<LayoutElement>();
        rowLE.preferredHeight = 28f;
        rowLE.flexibleWidth = 1f;

        HorizontalLayoutGroup hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
        labelObj.transform.SetParent(rowObj.transform, false);

        LayoutElement labelLE = labelObj.GetComponent<LayoutElement>();
        labelLE.preferredWidth = 210f;
        labelLE.flexibleWidth = 1f;
        labelLE.preferredHeight = 26f;

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 14f;
        labelTmp.color = labelColor;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.raycastTarget = true;
        ApplyFont(labelTmp);

        UILabelTooltipTrigger trigger = labelObj.AddComponent<UILabelTooltipTrigger>();
        trigger.Initialize(this, tooltipDescription);

        return rowObj;
    }

    private void CreateIntInputRow(
        Transform parent,
        TMP_DefaultControls.Resources resources,
        string labelText,
        string tooltip,
        int initialValue,
        int minValue,
        Action<int> onValueChanged)
    {
        GameObject rowObj = CreateBaseRow(parent, $"Row_{labelText}", labelText, tooltip);

        GameObject inputObj = TMP_DefaultControls.CreateInputField(resources);
        inputObj.name = $"Input_{labelText}";
        inputObj.transform.SetParent(rowObj.transform, false);

        LayoutElement inputLE = GetOrAddComponent<LayoutElement>(inputObj);
        inputLE.preferredWidth = 110f;
        inputLE.minWidth = 90f;
        inputLE.preferredHeight = 26f;

        Image bg = inputObj.GetComponent<Image>();
        if (bg != null) bg.color = controlBgColor;

        TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.text = initialValue.ToString();

        foreach (TMP_Text t in inputObj.GetComponentsInChildren<TMP_Text>(true))
        {
            ApplyFont(t);
            t.fontSize = 14f;
            t.color = Color.black;
        }

        int currentValidValue = initialValue;
        inputField.onEndEdit.AddListener((strVal) =>
        {
            if (int.TryParse(strVal, out int parsed))
            {
                if (parsed < minValue) parsed = minValue;
                currentValidValue = parsed;
                inputField.SetTextWithoutNotify(parsed.ToString());
                onValueChanged?.Invoke(parsed);
            }
            else
            {
                inputField.SetTextWithoutNotify(currentValidValue.ToString());
            }
        });
    }

    private void CreateSteppedSliderRow(
        Transform parent,
        string labelText,
        string tooltip,
        float initialValue,
        float min,
        float max,
        float step,
        Action<float> onValueChanged)
    {
        GameObject rowObj = CreateBaseRow(parent, $"Row_{labelText}", labelText, tooltip);

        GameObject rightContainer = new GameObject("SliderContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rightContainer.transform.SetParent(rowObj.transform, false);

        LayoutElement containerLE = rightContainer.GetComponent<LayoutElement>();
        containerLE.preferredWidth = 155f;
        containerLE.minWidth = 135f;
        containerLE.preferredHeight = 26f;

        HorizontalLayoutGroup hlg = rightContainer.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        DefaultControls.Resources uiRes = new DefaultControls.Resources();
        GameObject sliderObj = DefaultControls.CreateSlider(uiRes);
        sliderObj.name = $"Slider_{labelText}";
        sliderObj.transform.SetParent(rightContainer.transform, false);

        LayoutElement sliderLE = GetOrAddComponent<LayoutElement>(sliderObj);
        sliderLE.preferredWidth = 105f;
        sliderLE.flexibleWidth = 1f;
        sliderLE.preferredHeight = 20f;

        Transform bgTrans = sliderObj.transform.Find("Background");
        if (bgTrans != null && bgTrans.TryGetComponent(out Image bgImg))
            bgImg.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        Transform fillArea = sliderObj.transform.Find("Fill Area/Fill");
        if (fillArea != null && fillArea.TryGetComponent(out Image fillImg))
            fillImg.color = accentColor;

        Transform handle = sliderObj.transform.Find("Handle Slide Area/Handle");
        if (handle != null && handle.TryGetComponent(out Image handleImg))
            handleImg.color = headerColor;

        GameObject valTextObj = new GameObject("ValueText", typeof(RectTransform), typeof(LayoutElement));
        valTextObj.transform.SetParent(rightContainer.transform, false);
        LayoutElement valLE = valTextObj.GetComponent<LayoutElement>();
        valLE.preferredWidth = 42f;
        valLE.preferredHeight = 24f;

        TextMeshProUGUI valTmp = valTextObj.AddComponent<TextMeshProUGUI>();
        valTmp.fontSize = 13f;
        valTmp.color = labelColor;
        valTmp.alignment = TextAlignmentOptions.MidlineRight;
        ApplyFont(valTmp);

        int totalSteps = Mathf.RoundToInt((max - min) / step);
        int initialStep = Mathf.RoundToInt((initialValue - min) / step);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = totalSteps;
        slider.wholeNumbers = true;
        slider.value = initialStep;

        float startRealValue = initialStep * step;
        valTmp.text = startRealValue.ToString("0.00");

        slider.onValueChanged.AddListener((stepIndex) =>
        {
            float snappedValue = (float)Math.Round(min + (stepIndex * step), 2);
            valTmp.text = snappedValue.ToString("0.00");
            onValueChanged?.Invoke(snappedValue);
        });
    }

    private void CreateToggleRow(
        Transform parent,
        string labelText,
        string tooltip,
        bool initialValue,
        Action<bool> onValueChanged)
    {
        GameObject rowObj = CreateBaseRow(parent, $"Row_{labelText}", labelText, tooltip);

        GameObject toggleWrapper = new GameObject("ToggleWrapper", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        toggleWrapper.transform.SetParent(rowObj.transform, false);

        LayoutElement wrapperLE = toggleWrapper.GetComponent<LayoutElement>();
        wrapperLE.preferredWidth = 110f;
        wrapperLE.preferredHeight = 26f;

        HorizontalLayoutGroup hlg = toggleWrapper.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        DefaultControls.Resources uiRes = new DefaultControls.Resources();
        GameObject toggleObj = DefaultControls.CreateToggle(uiRes);
        toggleObj.name = $"Toggle_{labelText}";
        toggleObj.transform.SetParent(toggleWrapper.transform, false);

        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(22f, 22f);

        Transform internalLabel = toggleObj.transform.Find("Label");
        if (internalLabel != null) internalLabel.gameObject.SetActive(false);

        Transform bgTransform = toggleObj.transform.Find("Background");
        if (bgTransform != null)
        {
            RectTransform bgRect = bgTransform.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(22f, 22f);

            if (bgTransform.TryGetComponent(out Image bgImage))
                bgImage.color = controlBgColor;
        }

        Transform checkTransform = toggleObj.transform.Find("Background/Checkmark");
        if (checkTransform != null)
        {
            RectTransform checkRect = checkTransform.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4f, 4f);
            checkRect.offsetMax = new Vector2(-4f, -4f);

            if (checkTransform.TryGetComponent(out Image checkImage))
                checkImage.color = accentColor;
        }

        Toggle toggle = toggleObj.GetComponent<Toggle>();
        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener((isOn) => onValueChanged?.Invoke(isOn));
    }

    private void CreateActionButton(
        Transform parent,
        TMP_DefaultControls.Resources resources,
        string buttonText,
        Action onClickAction,
        float flexibleWidthWeight = 1f)
    {
        GameObject btnObj = TMP_DefaultControls.CreateButton(resources);
        btnObj.name = $"Btn_{buttonText}";
        btnObj.transform.SetParent(parent, false);

        LayoutElement btnLE = GetOrAddComponent<LayoutElement>(btnObj);
        btnLE.preferredHeight = 34f;
        btnLE.flexibleWidth = flexibleWidthWeight;

        Image btnImage = btnObj.GetComponent<Image>();
        if (btnImage != null) btnImage.color = buttonColor;

        TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = buttonText;
            tmp.fontSize = 13.5f;
            tmp.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            ApplyFont(tmp);
        }

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => onClickAction?.Invoke());
    }

    #endregion

    #region Sistema de Tooltip (100% compatible con Input System)

    private void CreateTooltipSystem()
    {
        if (parentCanvas == null) return;

        tooltipObject = new GameObject("UI_Tooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        tooltipObject.transform.SetParent(parentCanvas.transform, false);
        tooltipObject.transform.SetAsLastSibling();

        CanvasGroup cg = tooltipObject.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Image bg = tooltipObject.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        VerticalLayoutGroup vlg = tooltipObject.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = tooltipObject.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.pivot = new Vector2(0f, 1f);

        GameObject textObj = new GameObject("TooltipText", typeof(RectTransform), typeof(LayoutElement));
        textObj.transform.SetParent(tooltipObject.transform, false);

        LayoutElement textLE = textObj.GetComponent<LayoutElement>();
        textLE.preferredWidth = 250f;

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 13f;
        tooltipText.color = Color.white;
        tooltipText.enableWordWrapping = true;
        ApplyFont(tooltipText);

        tooltipObject.SetActive(false);
    }

    public void ShowTooltip(string description, Vector2 pointerScreenPosition)
    {
        if (tooltipObject == null || string.IsNullOrEmpty(description)) return;

        tooltipText.text = description;
        tooltipObject.SetActive(true);
        tooltipObject.transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        UpdateTooltipPosition(pointerScreenPosition);
    }

    public void UpdateTooltipPosition(Vector2 pointerScreenPosition)
    {
        if (tooltipRect == null) return;

        if (pointerScreenPosition.x + tooltipRect.rect.width + 20f > Screen.width)
        {
            tooltipRect.pivot = new Vector2(1f, 1f);
            tooltipRect.position = pointerScreenPosition + new Vector2(-12f, -12f);
        }
        else
        {
            tooltipRect.pivot = new Vector2(0f, 1f);
            tooltipRect.position = pointerScreenPosition + new Vector2(12f, -12f);
        }
    }

    public void HideTooltip()
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    #endregion

    #region Utilidades

    private void ApplyFont(TMP_Text textComponent)
    {
        if (uiFont != null && textComponent != null)
        {
            textComponent.font = uiFont;
        }
    }

    private T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = obj.AddComponent<T>();
        return comp;
    }

    #endregion
}

public class UILabelTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    private LightsOutUIManager uiManager;
    private string tooltipDescription;

    public void Initialize(LightsOutUIManager manager, string description)
    {
        uiManager = manager;
        tooltipDescription = description;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.ShowTooltip(tooltipDescription, eventData.position);
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.UpdateTooltipPosition(eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.HideTooltip();
        }
    }
}