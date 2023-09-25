using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float mainSpeed = 20;
    public float rotationSpeed = 60;

    void Update()
    {
        Vector3 movementRotation = GetRotationInput();
        if(movementRotation.sqrMagnitude > 0)
        {
            movementRotation = movementRotation * rotationSpeed * Time.deltaTime;
            transform.eulerAngles = movementRotation + transform.eulerAngles;
        }

        movementRotation = GetMovementInput();
        if (movementRotation.sqrMagnitude > 0)
        {
            movementRotation = movementRotation * mainSpeed * Time.deltaTime;
            transform.Translate(movementRotation);
        }
    }

    private Vector3 GetMovementInput()
    {
        Vector3 movement = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            movement += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += new Vector3(1, 0, 0);
        }
        return movement;
    }

    private Vector3 GetRotationInput()
    {
        Vector3 rotation = new Vector3();
        if (Input.GetKey(KeyCode.DownArrow))
        {
            rotation += new Vector3(1, 0, 0);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            rotation += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rotation += new Vector3(0, 1, 0);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotation += new Vector3(0, -1, 0);
        }
        return rotation;
    }
}
