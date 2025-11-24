using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTarget;

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    public float groundedOffset = -0.05f;

    [Header("Cámara (rotación del target)")]
    public float mouseLookSensitivity = 0.12f;
    public float gamepadLookSensitivity = 0.6f;
    public float minPitch = -50f;
    public float maxPitch = 75f;

    [Header("Alineación con cámara")]
    public bool rotateBodyWithCamera = true;
    public float alignSpeedIdle = 10f;
    public float alignSpeedMove = 18f;

    [Header("Dash")]
    public float dashDistance = 6f;        // cuánto avanza
    public float dashDuration = 0.15f;     // cuánto dura
    public float dashCooldown = 0.8f;      // CD antes del próximo
    public float dashFriction = 0f;        // opcional (0 = sin fricción durante dash)
    public bool dashOnlyOnGround = true;  // solo en suelo

    CharacterController _cc;
    PlayerInputActions _input;
    Vector2 _moveInput;
    Vector2 _lookInput;
    Vector3 _velocity; // incluye y-vel

    float _yaw;
    float _pitch;
    bool _usingGamepadLook;

    // dash state
    bool _isDashing;
    float _dashEndTime;
    float _nextDashReadyTime;
    Vector3 _dashVelocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _input = new PlayerInputActions();
    }

    void OnEnable()
    {
        _input.Enable();
        _input.Gameplay.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _input.Gameplay.Move.canceled += ctx => _moveInput = Vector2.zero;

        _input.Gameplay.Look.performed += ctx =>
        {
            _lookInput = ctx.ReadValue<Vector2>();
            // true si la entrada viene de un gamepad (right stick)
            _usingGamepadLook = ctx.control.device is Gamepad;
        };
        _input.Gameplay.Look.canceled += ctx =>
        {
            _lookInput = Vector2.zero;
        };

        // captura de cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // dash input
        var dash = _input.Gameplay.Dash;
        if (dash != null) dash.started += OnDashStarted;
    }

    void OnDisable()
    {
        _input.Gameplay.Move.performed -= null;
        _input.Gameplay.Move.canceled -= null;
        _input.Gameplay.Look.performed -= null;
        _input.Gameplay.Look.canceled -= null;

        var dash = _input.Gameplay.Dash;
        if (dash != null) dash.started -= OnDashStarted;

        _input.Disable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        UpdateCameraTarget();
        if (_isDashing) DashUpdate();
        else MoveCharacter();
    }

    void UpdateCameraTarget()
    {
        if (!cameraTarget) return;

        float sens = _usingGamepadLook ? gamepadLookSensitivity : mouseLookSensitivity;

        _yaw += _lookInput.x * sens;
        _pitch -= _lookInput.y * sens;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        cameraTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    void MoveCharacter()
    {
        // Direcciones relativas a cámara en XZ
        Vector3 camForward = cameraTarget ? cameraTarget.forward : Camera.main.transform.forward;
        Vector3 camRight = cameraTarget ? cameraTarget.right : Camera.main.transform.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        Vector3 inputDir = (camForward * _moveInput.y + camRight * _moveInput.x);
        Vector3 horizontalVel = inputDir * moveSpeed;

        // Gravedad y suelo
        bool grounded = _cc.isGrounded || GroundedCheck();
        if (grounded && _velocity.y < 0f) _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;

        // Mover
        Vector3 motion = horizontalVel + new Vector3(0f, _velocity.y, 0f);
        _cc.Move(motion * Time.deltaTime);

        // Alinear cuerpo
        bool hasMoveInput = _moveInput.sqrMagnitude > 0.0001f;
        AlignBodyToCameraYaw(hasMoveInput);
    }

    void AlignBodyToCameraYaw(bool isMoving)
    {
        if (!rotateBodyWithCamera || cameraTarget == null) return;

        Vector3 camEuler = cameraTarget.rotation.eulerAngles;
        Quaternion targetRot = Quaternion.Euler(0f, camEuler.y, 0f);
        float spd = isMoving ? alignSpeedMove : alignSpeedIdle;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, spd * Time.deltaTime);
    }

    bool GroundedCheck()
    {
        return Physics.SphereCast(transform.position + Vector3.up * 0.1f, _cc.radius * 0.9f, Vector3.down,
                                  out _, _cc.height * 0.5f + 0.2f);
    }

    // ==== DASH ====

    void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (Time.time < _nextDashReadyTime) return;
        if (dashOnlyOnGround && !(_cc.isGrounded || GroundedCheck())) return;
        if (_isDashing) return;

        // Dirección del dash: si hay input, usa input; si no, hacia donde mira la cámara
        Vector3 camForward = cameraTarget ? cameraTarget.forward : (Camera.main ? Camera.main.transform.forward : Vector3.forward);
        Vector3 camRight = cameraTarget ? cameraTarget.right : (Camera.main ? Camera.main.transform.right : Vector3.right);
        camForward.y = 0f; camRight.y = 0f; camForward.Normalize(); camRight.Normalize();

        Vector3 dashDir;
        if (_moveInput.sqrMagnitude > 0.0001f)
            dashDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
        else
            dashDir = camForward;

        StartDash(dashDir);
    }

    void StartDash(Vector3 dir)
    {
        _isDashing = true;
        _dashEndTime = Time.time + dashDuration;
        _nextDashReadyTime = Time.time + dashCooldown;

        // velocidad instantánea para cubrir 'dashDistance' en 'dashDuration'
        float dashSpeed = dashDistance / Mathf.Max(0.01f, dashDuration);
        _dashVelocity = dir * dashSpeed;

        // anula caída durante dash
        _velocity.y = 0f;
    }

    void DashUpdate()
    {
        // Mover únicamente por dash en XZ
        Vector3 dashMotion = _dashVelocity * Time.deltaTime;

        // fricción opcional durante dash
        if (dashFriction > 0f)
        {
            float decel = dashFriction * Time.deltaTime;
            _dashVelocity = Vector3.MoveTowards(_dashVelocity, Vector3.zero, decel);
        }

        _cc.Move(dashMotion);

        if (Time.time >= _dashEndTime)
        {
            _isDashing = false;
        }
    }
}
