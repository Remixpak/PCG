using UnityEngine;
using UnityEngine.InputSystem;




//este script sirve para probar la generacion de los tiles
namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public class WFCTester : MonoBehaviour
    {
        [Header("Referencia")]
        [SerializeField] private WFCGenerator generator;

        private void Update()
        {
            if (generator == null || Keyboard.current == null) return;

            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                // Ahora capturamos si la inicializacion fue exitosa o no.
                bool exito = generator.Initialize();

                if (exito)
                {
                    Debug.Log("EXITO: WFC Inicializado correctamente. Revisa la jerarquía. Presiona Espacio para avanzar.");
                }
                else
                {
                    // Si falla, le pedimos al generador que nos diga exactamente donde se estanco.
                    Debug.LogError("ERROR: El WFC falló al iniciar. Razón: " + generator.Status);
                }
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                generator.Step();
            }
        }
    }
}