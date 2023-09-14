using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private bool _isJumping = false;

    private bool _isRotating = false;
    private Quaternion _initialRotation;
    private float _airTimeStart;
    public float rotationDuration = 1.0f; // Duración de la rotación en segundos

    private void Update()
    {
        // Detectar si el jugador está en el aire
        if (!_isJumping && !Physics.Raycast(transform.position, Vector3.down, 1.0f))
        {
            _isJumping = true;
        }

        // Rotación de 360 grados en el aire
        if (_isJumping && Input.GetKeyDown(KeyCode.G) && !_isRotating)
        {
            StartCoroutine(Rotate360());
        }
    }

    private IEnumerator Rotate360()
    {
        _isRotating = true;
        _initialRotation = transform.rotation;
        _airTimeStart = Time.time;

        while (Time.time - _airTimeStart < rotationDuration)
        {
            float rotationProgress = (Time.time - _airTimeStart) / rotationDuration;
            transform.rotation = Quaternion.Slerp(_initialRotation, Quaternion.Euler(0, 360, 0), rotationProgress);
            yield return null;
        }

        transform.rotation = _initialRotation;
        _isRotating = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isJumping = false;

            // Si el jugador toca el suelo, reiniciar la rotación
            if (_isRotating)
            {
                StopCoroutine(Rotate360());
                transform.rotation = _initialRotation;
                _isRotating = false;
            }
        }
    }
}