using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GunController : MonoBehaviour
{
    [Header("Refs")]
    public Camera mainCamera;
    public Transform muzzle;               // MuzzlePoint (punta del cañón)
    public ParticleSystem muzzleFlash;     // opcional
    public GameObject hitVfxPrefab;        // VFX en impacto (opcional)

    [Header("Projectile Settings")]
    public GameObject bulletPrefab;
    public LayerMask hitMask = ~0;         // sin Player layer
    public float fireRate = 8f;            // disparos/seg si mantienes
    public float damage = 10f;
    public float aimMaxDistance = 500f;
    public LayerMask aimMask = ~0;
    public float spawnForwardOffset = 0.05f;

    [Header("Ammo / Reload")]
    public int clipSize = 12;              // balas por cargador
    public int ammoInClip = 12;            // actual en el cargador
    public int reserveAmmo = 60;           // balas en mochila
    public float reloadTime = 1.6f;        // segundos
    public bool autoReload = true;         // recarga auto al vaciarse

    public bool IsReloading => _isReloading;

    PlayerInput _playerInput;
    bool _isFiringHeld;
    float _nextShotTime;
    bool _isReloading;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (!mainCamera) mainCamera = Camera.main;

        // Inicial por si editas en play
        ammoInClip = Mathf.Clamp(ammoInClip, 0, clipSize);
    }

    void OnEnable()
    {
        var fire = _playerInput.actions["Fire"];
        fire.started += OnFireStarted;
        fire.performed += OnFirePerformed;
        fire.canceled += OnFireCanceled;

        var reload = _playerInput.actions["Reload"];
        if (reload != null)
        {
            reload.started += OnReloadStarted;
        }
    }

    void OnDisable()
    {
        var fire = _playerInput.actions["Fire"];
        fire.started -= OnFireStarted;
        fire.performed -= OnFirePerformed;
        fire.canceled -= OnFireCanceled;

        var reload = _playerInput.actions["Reload"];
        if (reload != null)
        {
            reload.started -= OnReloadStarted;
        }
    }

    void Update()
    {
        if (_isReloading) return;

        // Auto-fire si mantienes
        if (_isFiringHeld && Time.time >= _nextShotTime)
        {
            TryShoot();
        }
    }

    void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (_isReloading) return;

        _isFiringHeld = true;
        TryShoot();
    }

    void OnFirePerformed(InputAction.CallbackContext ctx) { /* soporte pad */ }

    void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        _isFiringHeld = false;
    }

    void OnReloadStarted(InputAction.CallbackContext ctx)
    {
        TryReloadManual();
    }

    void TryShoot()
    {
        if (_isReloading) return;

        if (ammoInClip <= 0)
        {
            // sin balas en cargador
            if (autoReload && reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }
            _nextShotTime = Time.time + 1f / fireRate;
            return;
        }

        // Disparo válido
        ShootOne();
        ammoInClip--;
        _nextShotTime = Time.time + 1f / fireRate;

        // si se vació y hay reserva, auto-reload
        if (ammoInClip == 0 && autoReload && reserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    void TryReloadManual()
    {
        if (_isReloading) return;
        if (ammoInClip >= clipSize) return;   // ya lleno
        if (reserveAmmo <= 0) return;         // no hay reserva

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        _isReloading = true;

        // (Opcional) feedback: anim, sonido, crosshair color, etc.
        // crosshair?.OnReloadStart(); // si implementas

        yield return new WaitForSeconds(reloadTime);

        int need = clipSize - ammoInClip;
        int take = Mathf.Min(need, reserveAmmo);
        ammoInClip += take;
        reserveAmmo -= take;

        _isReloading = false;

        // crosshair?.OnReloadEnd(); // si implementas
    }

    public void AddReserveAmmo(int amount)
    {
        if (amount <= 0) return;
        reserveAmmo += amount;
    }

    void ShootOne()
    {
        if (muzzleFlash) muzzleFlash.Play();

        if (!mainCamera || !muzzle || !bulletPrefab)
        {
            Debug.LogError("GunController: asigna mainCamera, muzzle y bulletPrefab.");
            return;
        }

        // 1) Punto de mira desde cámara
        Ray camRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint;
        if (Physics.Raycast(camRay, out RaycastHit camHit, aimMaxDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = camHit.point;
        else
            aimPoint = camRay.origin + camRay.direction * aimMaxDistance;

        // 2) Dir desde muzzle hacia el punto de mira (corrige offset 3ra persona)
        Vector3 dir = (aimPoint - muzzle.position).normalized;

        // 3) Ajuste por obstáculo inmediato entre muzzle y aimPoint (coherencia del impacto)
        if (Physics.Raycast(muzzle.position, dir, out RaycastHit muzzleHit, aimMaxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            aimPoint = muzzleHit.point;
            dir = (aimPoint - muzzle.position).normalized;
        }

        // 4) Spawn proyectil
        Vector3 spawnPos = muzzle.position + dir * spawnForwardOffset;
        var go = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(dir));

        var bulletComp = go.GetComponent<Bullet>() ?? go.GetComponentInChildren<Bullet>();
        if (bulletComp)
        {
            bulletComp.damage = damage;
            bulletComp.hitVfxPrefab = hitVfxPrefab;
            bulletComp.Launch(dir);
        }
    }
}