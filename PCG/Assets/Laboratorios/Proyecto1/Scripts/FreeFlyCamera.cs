using UnityEngine;
using UnityEngine.InputSystem;


//este script basicamente nos permite mover la camara en el modo play del editor de unity, para poder ver el mundo generado desde diferentes angulos y posiciones mediante WASD
public class FreeFlyCamera : MonoBehaviour
{
    public float movementSpeed = 20f;//velocidad de movimiento de la camara
    public float fastMovementSpeed = 50f;//velocidad de movimiento de la camara cuando se presiona shift
    public float lookSensitivity = 0.2f;//sensibilidad del mouse para rotar la camara

    private float rotationX = 0f;//rotacion de la camara en el eje X
    private float rotationY = 0f;//rotacion de la camara en el eje Y
    private bool isLooking = false;//variable para saber si estamos mirando con el mouse o no

    

    void Update()
    {
        //si el mouse no es nulo, entonces verificamos si se presiono el boton derecho del mouse para activar la rotacion de la camara, y si se solto el boton derecho del mouse para desactivar la rotacion de la camara
        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                isLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                isLooking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        //si estamos mirando con el mouse, entonces obtenemos el delta del mouse y lo multiplicamos por la sensibilidad para rotar la camara, y luego aplicamos la rotacion a la camara
        if (isLooking && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            rotationX += mouseDelta.x * lookSensitivity;
            rotationY -= mouseDelta.y * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }

        //movemos la camara en el espacio 3D utilizando WASD y QE para subir y bajar, y multiplicamos la velocidad de movimiento por el tiempo delta para que sea independiente del framerate
        float currentSpeed = movementSpeed;
        float moveX = 0f;
        float moveZ = 0f;
        float moveY = 0f;

        //asignamos las teclas de movimiento a las variables de movimiento, y si se presiona shift, aumentamos la velocidad de movimiento
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftShiftKey.isPressed) currentSpeed = fastMovementSpeed;

            if (Keyboard.current.wKey.isPressed) moveZ += 1f;
            if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
            if (Keyboard.current.aKey.isPressed) moveX -= 1f;
            if (Keyboard.current.dKey.isPressed) moveX += 1f;

            if (Keyboard.current.eKey.isPressed) moveY += 1f;
            if (Keyboard.current.qKey.isPressed) moveY -= 1f;
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ + transform.up * moveY;
        transform.position += move * currentSpeed * Time.deltaTime;
    }
}