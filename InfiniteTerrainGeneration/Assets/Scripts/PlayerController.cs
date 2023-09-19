using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float forwardSpeed = 10f;
    public float rotationSpeed = 30f;
    public float sprintDuration = 5f;
    public ParticleSystem exhaustParticles;
    public float normalParticleSpeed = 5f;
    public float sprintParticleSpeed = 10f;

    private Rigidbody _rb;
    private Transform _transform;
    private bool _isSprinting = false;
    private float _sprintTimer = 0f;
    private float _currentSpeed = 0f;

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
        HandleSprint();
    }

    private void InitializeComponents()
    {
        _rb = GetComponent<Rigidbody>();
        _transform = transform;
        _currentSpeed = forwardSpeed;
        _transform.position += new Vector3(0, 30, 0);
    }

    private void HandleRotation()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float ascentInput = Input.GetKey(KeyCode.Q) ? 1f : 0f; // Presiona 'Q' para ascender.
        float descentInput = Input.GetKey(KeyCode.E) ? 1f : 0f; // Presiona 'E' para descender.
        float verticalInput = ascentInput - descentInput;

        // Rotación suave en función de las teclas A, D, Q y E.
        float rotateHorizontal = horizontalInput * rotationSpeed * Time.deltaTime;
        float rotateVertical = verticalInput * rotationSpeed * Time.deltaTime;

        _transform.Rotate(Vector3.up, rotateHorizontal);
        _transform.Rotate(Vector3.right, rotateVertical);
    }

    private void HandleMovement()
    {
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 forwardVelocity = _transform.forward * _currentSpeed * verticalInput;
        
        _rb.velocity = forwardVelocity;
    }

    private void HandleSprint()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !_isSprinting)
        {
            StartSprint();
        }

        if (_isSprinting)
        {
            _sprintTimer += Time.deltaTime;
            if (_sprintTimer >= sprintDuration)
            {
                StopSprint();
            }
        }
    }

    private void StartSprint()
    {
        _isSprinting = true;
        exhaustParticles.Play();
        _currentSpeed *= 2;
        var mainModule = exhaustParticles.main;
        mainModule.startSpeed = sprintParticleSpeed;
    }

    private void StopSprint()
    {
        _isSprinting = false;
        exhaustParticles.Stop();
        _currentSpeed = forwardSpeed;
        var mainModule = exhaustParticles.main;
        mainModule.startSpeed = normalParticleSpeed;
        _sprintTimer = 0f;
    }
}
