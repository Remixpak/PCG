using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[Serializable]
public class PokemonData
{
    public string name;
    public string type1;
    public string type2;

    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int bst;
}

public class PokemonCSVLoader : MonoBehaviour
{
    [Header("CSV")]
    [SerializeField] private TextAsset csvFile;
    [SerializeField] private bool loadOnAwake = true;

    private readonly List<PokemonData> pokemon = new List<PokemonData>();

    public IReadOnlyList<PokemonData> Data => pokemon;
    public bool IsLoaded => pokemon.Count > 0;

    private void Awake()
    {
        if (loadOnAwake)
            Load();
    }

    /*
     * ============================================================
     * OBJETIVO
     * ============================================================
     *
     * El CSV contiene muchas columnas, pero para este laboratorio
     * solo necesitamos una representación reducida:
     *
     * Name, Type 1, Type 2, HP, Att, Spa, Def, Spd, Spe y BST.
     *
     * A partir de esos valores construiremos:
     *
     * offense = max(Att, Spa)
     * defense = max(Def, Spd)
     *
     * EJEMPLO
     * ------------------------------------------------------------
     * Una fila del CSV puede contener:
     *
     * Name = "Charizard"
     * Type 1 = "Fire"
     * Type 2 = "Flying"
     * HP = 78
     * Att = 84
     * Spa = 109
     * Def = 78
     * Spd = 85
     * Spe = 100
     * BST = 534
     *
     * El PokemonData resultante será:
     *
     * {
     *   name = "Charizard",
     *   type1 = "Fire",
     *   type2 = "Flying",
     *   hp = 78,
     *   offense = 109,   // max(84, 109)
     *   defense = 85,    // max(78, 85)
     *   speed = 100,
     *   bst = 534
     * }
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: clear pokemon list
     * 2: verify that csvFile exists
     * 3: split file into lines
     * 4: read first line as header
     * 5: create a map: column name -> column index
     * 6: for each data row:
     * 7:      parse row
     * 8:      read Name
     * 9:      read Type 1 and Type 2
     * 10:     read HP
     * 11:     read Att and Spa
     * 12:     read Def and Spd
     * 13:     read Spe
     * 14:     read BST
     * 15:     create PokemonData
     * 16:     offense = max(Att, Spa)
     * 17:     defense = max(Def, Spd)
     * 18:     add PokemonData to pokemon list
     *
     * RESULTADO ESPERADO
     * ------------------------------------------------------------
     * pokemon = [
     *   PokemonData("Bulbasaur", ...),
     *   PokemonData("Ivysaur", ...),
     *   PokemonData("Venusaur", ...),
     *   ...
     * ]
     *
     * pokemon.Count corresponde a la cantidad de filas válidas.
     */

    [ContextMenu("Load CSV")]
    public void Load()
    {
        // TODO: Implementar la carga siguiendo el pseudocódigo.
        //
        // Puede utilizar directamente los métodos auxiliares:
        // ParseCsvLine(...)
        // BuildColumnMap(...)
        // ReadString(...)
        // ReadInt(...)

        string[] lineas = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); // Dividimos el contenido del CSV en líneas, eliminando líneas vacías

        if (lineas.Length <= 1)// Si el largo de las lineas es menor o igual a 1, significa que no hay datos para procesar, por lo que se retorna sin hacer nada
            return;

        // Obtenemos todos los headers de la primera línea con ParseCsvLine
        List<string> headers = ParseCsvLine(lineas[0]);

        // Construimos el mapa de columnas automáticamente con el método provisto
        Dictionary<string, int> columnMap = BuildColumnMap(headers);

        pokemon.Clear();//limpiamos la lista de pokemon antes de cargar nuevos datos

        // un for que recorre las filas de datos a partir de la línea 1
        for (int i = 1; i < lineas.Length; i++)
        {
            List<string> values = ParseCsvLine(lineas[i]);//parseamos la línea actual para obtener los valores de cada columna
            if (values.Count == 0)//si no hay valores en la línea actual, se continúa con la siguiente iteración
                continue;

            string name = ReadString(values, columnMap.ContainsKey("Name") ? columnMap["Name"] : 1);//leemos el nombre del pokemon, si no existe la columna "Name", se asume que está en la columna 1
            string type1 = ReadString(values, columnMap.ContainsKey("Type 1") ? columnMap["Type 1"] : 2);//leemos el tipo 1 del pokemon, si no existe la columna "Type 1", se asume que está en la columna 2
            string type2 = ReadString(values, columnMap.ContainsKey("Type 2") ? columnMap["Type 2"] : 3);//leemos el tipo 2 del pokemon, si no existe la columna "Type 2", se asume que está en la columna 3
            int hp = ReadInt(values, columnMap.ContainsKey("HP") ? columnMap["HP"] : 5);//leemos los puntos de vida del pokemon, si no existe la columna "HP", se asume que está en la columna 5
            int att = ReadInt(values, columnMap.ContainsKey("Att") ? columnMap["Att"] : 6);//leemos el ataque del pokemon, si no existe la columna "Att", se asume que está en la columna 6
            int spa = ReadInt(values, columnMap.ContainsKey("Spa") ? columnMap["Spa"] : 7);//leemos el ataque especial del pokemon, si no existe la columna "Spa", se asume que está en la columna 7
            int def = ReadInt(values, columnMap.ContainsKey("Def") ? columnMap["Def"] : 8);//leemos la defensa del pokemon, si no existe la columna "Def", se asume que está en la columna 8
            int spd = ReadInt(values, columnMap.ContainsKey("Spd") ? columnMap["Spd"] : 9);//leemos la velocidad del pokemon, si no existe la columna "Spd", se asume que está en la columna 9
            int speed = ReadInt(values, columnMap.ContainsKey("Spe") ? columnMap["Spe"] : 10);//leemos la velocidad del pokemon, si no existe la columna "Spe", se asume que está en la columna 10
            int bst = ReadInt(values, columnMap.ContainsKey("BST") ? columnMap["BST"] : 11);//leemos el total de estadísticas del pokemon, si no existe la columna "BST", se asume que está en la columna 11

            PokemonData data = new PokemonData//creamos un nuevo objeto PokemonData con los valores leídos
            {
                name = name,
                type1 = type1,
                type2 = type2,
                hp = hp,
                offense = Math.Max(att, spa),
                defense = Math.Max(def, spd),
                speed = speed,
                bst = bst
            };

            pokemon.Add(data);//agregamos el objeto PokemonData a la lista de pokemon

            //descomentar para probar que funcione la carga del csv
            //Debug.Log($"[PokemonCSVLoader] Cargado: {data.name} | Tipos: {data.type1}/{data.type2} | OFF: {data.offense} | DEF: {data.defense} | BST: {data.bst}");
        }

        //Debug.LogWarning($"[PokemonCSVLoader] Carga completa. Total de Pokémon válidos: {pokemon.Count}");
    }

    // ============================================================
    // INFRAESTRUCTURA ENTREGADA
    // No es necesario modificar los métodos siguientes.
    // ============================================================

    private static Dictionary<string, int> BuildColumnMap(List<string> headers)
    {
        Dictionary<string, int> result =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i].Trim();

            if (!result.ContainsKey(header))
                result.Add(header, i);
        }

        return result;
    }

    private static string ReadString(List<string> values, int index)
    {
        if (index < 0 || index >= values.Count)
            return string.Empty;

        return values[index].Trim();
    }

    private static int ReadInt(List<string> values, int index)
    {
        string raw = ReadString(values, index);

        if (int.TryParse(
            raw,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int value))
        {
            return value;
        }

        return 0;
    }

    /// <summary>
    /// Parser CSV entregado.
    /// Permite leer campos entre comillas y comas internas.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        List<string> values = new List<string>();
        StringBuilder current = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());

        return values;
    }
}
