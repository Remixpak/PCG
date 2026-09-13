using UnityEngine;
using UnityEngine.UI;
using TMPro;


//basicamente este script permite manejar los parametros basicos de la generacion del mundo
//como la seed, el numero de iteraciones, la probabilidad de spawn de arboles, etc

public class RuntimeUIManager : MonoBehaviour
{
    public ProceduralWorldManager worldManager;
    public Transform freeFlyCamera;

    //obtenemos como referencia dentro del inspector los elementos de la UI que vamos a usar para manejar los parametros de generacion del mundo
    [Header("UI - Inputs")]
    public TMP_InputField seedInput;
    public TMP_InputField walkIterationsInput;
    public TMP_InputField totalStepsInput;
    public Slider treeSpawnChanceSlider;
    public TextMeshProUGUI treeSpawnChanceText;
    public TMP_InputField terrainWidthInput;
    public TMP_InputField terrainLengthInput;
    public TMP_InputField minDistanceInput;

    //obtenemios como referencia dentro del inspector los botones de la UI que vamos a usar para generar el mundo y limpiar el mundo
    [Header("UI - Buttons")]
    public Button btnRandomizeSeed;
    public Button btnGenerateMountain;
    public Button btnGenerateSea;
    public Button btnClearAll;

    //obtenemos como referencia dentro del inspector los elementos de la UI que vamos a usar para manejar la visibilidad de la UI
    [Header("UI - Visibility")]
    public GameObject uiContainer;
    public Button btnToggleUI;

    private void Start()
    {
        btnRandomizeSeed.onClick.AddListener(RandomizeSeed);
        btnGenerateMountain.onClick.AddListener(GenerateMountain);//agregamos un listener al boton de generar montañas para que llame a la funcion GenerateMountain cuando se haga click en el boton
        btnGenerateSea.onClick.AddListener(GenerateSea);//agregamos un listener al boton de generar mar para que llame a la funcion GenerateSea cuando se haga click en el boton
        btnClearAll.onClick.AddListener(Clear);//agregamos un listener al boton de limpiar para que llame a la funcion Clear cuando se haga click en el boton

        treeSpawnChanceSlider.onValueChanged.AddListener(UpdateSpawnChanceText);//agregamos un listener al slider de probabilidad de spawn de arboles para que llame a la funcion UpdateSpawnChanceText cuando se cambie el valor del slider
        //utilizamos un slider para poder manejar valores fijos en la configuracion ya que si es alta la probabilidad > 1 se muere unity
        UpdateSpawnChanceText(treeSpawnChanceSlider.value);//llamamos a la funcion UpdateSpawnChanceText para que actualice el texto de la probabilidad de spawn de arboles al valor inicial del slider

        if (btnToggleUI != null)
        {
            btnToggleUI.onClick.AddListener(ToggleVisibility);
        }

        worldManager.GenerateFullWorld();
        FocusCameraOnTerrain();
    }

    private void ToggleVisibility() //funcion para manejar la visibilidad de la UI
    {
        if (uiContainer != null)
        {
            uiContainer.SetActive(!uiContainer.activeSelf);
        }
    }

    private void UpdateSpawnChanceText(float value) //funcion para actualizar el texto de la probabilidad de spawn de arboles al valor del slider
    {
        if (treeSpawnChanceText != null)
        {
            treeSpawnChanceText.text = value.ToString("F2");
        }
    }

    private void ApplySettings()//funcion para aplicar los valores de la UI a los parametros de generacion del mundo
    {
        //aplicamos los valores de la UI a los parametros de generacion del mundo
        if (int.TryParse(seedInput.text, out int parsedSeed))
        {
            worldManager.Seed = parsedSeed;
        }
        else//si no se puede parsear el valor del input de seed, se asigna un valor por defecto de -1 para que se genere un mundo aleatorio
        {
            worldManager.Seed = -1;
        }

        //aplicamos los valores de la UI a los parametros de generacion del mundo
        if (int.TryParse(walkIterationsInput.text, out int parsedIterations))
        {
            worldManager.WalkIterations = parsedIterations;
        }
        //aplicamos los valores de la UI a los parametros de generacion del mundo
        if (int.TryParse(totalStepsInput.text, out int parsedSteps))
        {
            worldManager.TotalSteps = parsedSteps;
        }

        worldManager.TreeSpawnChance = treeSpawnChanceSlider.value;

        //aplicamos los valores de la UI a los parametros de generacion del mundo
        if (float.TryParse(terrainWidthInput.text, out float parsedWidth))
        {
            worldManager.TerrainWidth = parsedWidth;
        }

        //aplicamos los valores de la UI a los parametros de generacion del mundo
        if (float.TryParse(terrainLengthInput.text, out float parsedLength))
        {
            worldManager.TerrainLength = parsedLength;
        }

        //aplicamos los valores de la UI a los parametros de generacion del mundo
        if (float.TryParse(minDistanceInput.text, out float parsedMinDistance))
        {
            worldManager.MinDistance = parsedMinDistance;
        }
    }

    private void FocusCameraOnTerrain() //funcion para enfocar la camara en el centro del terreno generado ya que si no tenemos que acercarnos demasiado con la camara 
    {
        if (freeFlyCamera != null)//si la camara no es nula, obtenemos el ancho y largo del terreno generado y calculamos el centro del terreno para posicionar la camara en el centro del terreno y mirar hacia el centro del terreno
        {
            float width = worldManager.TerrainWidth;//obtenemos el ancho del terreno generado
            float length = worldManager.TerrainLength;//obtenemos el largo del terreno generado

            float maxDim = Mathf.Max(width, length);//obtenemos el maximo entre el ancho y largo del terreno generado para posicionar la camara a una distancia adecuada del terreno

            Terrain terrain = FindObjectOfType<Terrain>();//buscamos el objeto de tipo Terrain en la escena para obtener su posicion y calcular el centro del terreno generado
            if (terrain != null)
            {
                Vector3 terrainPos = terrain.transform.position;
                Vector3 centerPos = new Vector3(terrainPos.x + (width / 2f), terrainPos.y, terrainPos.z + (length / 2f));//calculamos el centro del terreno generado

                freeFlyCamera.position = new Vector3(centerPos.x, terrainPos.y + (maxDim * 0.6f), terrainPos.z - (maxDim * 0.2f));//posicionamos la camara a una distancia adecuada del terreno generado
                freeFlyCamera.LookAt(centerPos);//hacemos que la camara mire hacia el centro del terreno generado
            }
        }
    }

    private void RandomizeSeed()
    {
        int newSeed = Random.Range(int.MinValue, int.MaxValue);
        seedInput.text = newSeed.ToString();
        ApplySettings();
    }

    private void GenerateMountain()//funcion para generar el mundo de montañas
    {
        ApplySettings();
        worldManager.GenerateFullWorld();
        FocusCameraOnTerrain();
    }

    private void GenerateSea()//funcion para generar el mundo de mar
    {
        ApplySettings();
        worldManager.GenerateSeaWorld();
        FocusCameraOnTerrain();
    }

    private void Clear()//funcion para limpiar el mundo generado
    {
        worldManager.ClearForestWorld();
        worldManager.ClearSeaWorld();
    }
}