using System;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public enum WFCDirection { North, East, South, West }

    [Serializable]
    public class WFCTile
    {
        [Header("Datos del patron")]
        public string id;
        public Sprite sprite;
        [Min(0.01f)] public float weight = 1f;
        public bool available = true;

        [Header("Conexiones (true = abierto)")]
        public bool north, east, south, west;

        // Consulta si esta pieza tiene una abertura en una direccion.
        public bool HasOpening(WFCDirection direction)
        {
            switch (direction)
            {
                case WFCDirection.North: return north;
                case WFCDirection.East: return east;
                case WFCDirection.South: return south;
                case WFCDirection.West: return west;
                default: return false;
            }
        }

        /*
         * COMPATIBILIDAD ENTRE PIEZAS
         *
         * Dos piezas pueden ser vecinas cuando sus conexiones
         * enfrentadas son compatibles.
         *
         * Ejemplo:
         *
         * Pieza A: East = true
         * Pieza B: West = true
         *
         * Ambas pueden conectarse horizontalmente.
         *
         * Si A.East = true y B.West = false, la conexion
         * no es valida.
         *
         * Las conexiones cerradas tambien deben coincidir.
         * La direccion indica donde se ubica B respecto de A.
         */


        //comparamos las caras opuestas de dos piezas relativas a su posicion 
        //se evalua al vecino en base a la direccion norte , cara sur debe coincidir con la cara norte de la pieza base
        // ya que esto asegurar continuidad en los bordes
        public bool IsCompatible(WFCTile neighbor, WFCDirection direction)
        {
            switch (direction)
            {
                case WFCDirection.North: return north == neighbor.south;// Compara la cara Norte de esta pieza con la cara Sur de la vecina.
                case WFCDirection.East: return east == neighbor.west;// Compara la cara Este de esta pieza con la cara Oeste de la vecina.
                case WFCDirection.South: return south == neighbor.north;// Compara la cara Sur de esta pieza con la cara Norte de la vecina.
                case WFCDirection.West: return west == neighbor.east;// Compara la cara Oeste de esta pieza con la cara Este de la vecina.
                default: return false;
            }
        }
    }
}
