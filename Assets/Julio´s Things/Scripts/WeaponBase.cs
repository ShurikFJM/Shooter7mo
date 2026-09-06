using System.Collections;
using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    [Header("Configuración Base")]
    public WeaponData data;
    public Transform firePoint;
    public SimpleFPSController playerController;

    [Header("Efectos de Feedback (CS2 / TF2)")]
    public Material tracerMaterial; // Material brillante/emisivo para la estela de la bala
    public ParticleSystem muzzleFlash; // Sistema de partículas para el fogonazo del cañón

    [Header("Retroceso Visual Procedural")]
    public Transform weaponModelTransform; // Objeto hijo que contiene el modelo del arma
    public Vector3 kickbackOffset = new Vector3(0f, 0.02f, -0.08f); // Retroceso hacia atrás
    public Vector3 kickbackRotation = new Vector3(-3f, 1f, 0f); // Rotación de la patada
    public float returnSpeed = 15f; // Velocidad para volver a la posición original

    // Estado Interno
    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    // Control del Patrón de Retroceso
    private int currentShotIndex = 0;
    private float lastShotTime = 0f;

    // Vectores para la patada procedural
    private Vector3 targetPosition;
    private Vector3 currentPosition;
    private Quaternion targetRotation;
    private Quaternion currentRotation;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => data != null ? data.maxAmmo : 0;
    public bool IsReloading => isReloading;

    void Start()
    {
        if (data != null) currentAmmo = data.maxAmmo;
        if (firePoint == null && Camera.main != null) firePoint = Camera.main.transform;
        if (playerController == null) playerController = GetComponentInParent<SimpleFPSController>();
        if (weaponModelTransform == null) weaponModelTransform = transform;
    }

    void Update()
    {
        // Recuperar la posición y rotación original del arma (Interpolación suave)
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, Time.deltaTime * returnSpeed);
        targetRotation = Quaternion.Slerp(targetRotation, Quaternion.identity, Time.deltaTime * returnSpeed);

        weaponModelTransform.localPosition = Vector3.Lerp(weaponModelTransform.localPosition, targetPosition, Time.deltaTime * returnSpeed * 2f);
        weaponModelTransform.localRotation = Quaternion.Slerp(weaponModelTransform.localRotation, targetRotation, Time.deltaTime * returnSpeed * 2f);

        if (isReloading) return;

        // Resetear patrón de retroceso si deja de disparar
        if (Time.time - lastShotTime > data.recoilResetTime)
        {
            currentShotIndex = 0;
        }

        // Recarga
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < data.maxAmmo)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }

        // Entrada de disparo
        bool shootInput = data.isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (shootInput && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + data.fireRate;
                Shoot();
            }
            else
            {
                StartCoroutine(ReloadCoroutine());
            }
        }
    }

    void Shoot()
    {
        currentAmmo--;
        lastShotTime = Time.time;

        Transform camTransform = Camera.main.transform;

        // 1. Obtener el offset del patrón de retroceso actual
        Vector2 recoilOffset = Vector2.zero;
        if (data.recoilPattern != null && data.recoilPattern.Length > 0)
        {
            int index = Mathf.Min(currentShotIndex, data.recoilPattern.Length - 1);
            recoilOffset = data.recoilPattern[index];
        }

        // 2. Calcular dispersión en grados
        float totalSpread = data.baseSpread + (currentShotIndex * data.spreadPerShot);

        float currentSpeed = playerController != null ? playerController.CurrentHorizontalSpeed : 0f;
        totalSpread += currentSpeed * data.movementSpreadMultiplier;

        CharacterController cc = playerController != null ? playerController.GetComponent<CharacterController>() : null;
        if (cc != null && !cc.isGrounded)
        {
            totalSpread += data.airSpreadMultiplier;
        }

        currentShotIndex++;

        // Generar la imprecisión aleatoria
        Vector2 randomSpread = Random.insideUnitCircle * totalSpread;

        // 3. Grados de desviación (Yaw = Horizontal, Pitch = Vertical)
        float yawInDegrees = recoilOffset.x + randomSpread.x;
        float pitchInDegrees = recoilOffset.y + randomSpread.y;

        // 4. Transformar el vector usando Quaternions (Precisión 100% matemática)
        Quaternion spreadRotation = Quaternion.Euler(-pitchInDegrees, yawInDegrees, 0f);
        Vector3 finalDirection = camTransform.rotation * spreadRotation * Vector3.forward;

        // 5. Raycast desde los ojos del jugador
        Vector3 rayOrigin = camTransform.position;
        Vector3 targetPoint;

        if (Physics.Raycast(rayOrigin, finalDirection, out RaycastHit hit, data.range))
        {
            targetPoint = hit.point;
            CreateImpactVisual(hit);
        }
        else
        {
            targetPoint = rayOrigin + finalDirection * data.range;
        }

        // Feedback Visual
        targetPosition += kickbackOffset;
        targetRotation *= Quaternion.Euler(kickbackRotation);
        if (muzzleFlash != null) muzzleFlash.Play();
        StartCoroutine(RenderTracer(firePoint.position, targetPoint));
    }

    IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        // Crear un objeto temporal con LineRenderer
        GameObject tracerObj = new GameObject("BulletTracer");
        LineRenderer line = tracerObj.AddComponent<LineRenderer>();

        line.startWidth = 0.03f; // Grosor al inicio
        line.endWidth = 0.01f;   // Grosor al final
        line.material = tracerMaterial != null ? tracerMaterial : new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.yellow;
        line.endColor = new Color(1f, 0.5f, 0f, 0f); // Color naranja transparente

        line.SetPosition(0, start);
        line.SetPosition(1, start);

        float duration = 0.04f; // Duración ultrarrápida del viaje
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / duration);
            line.SetPosition(1, currentPos);
            yield return null;
        }

        line.SetPosition(1, end);
        Destroy(tracerObj, 0.02f); // Destruir la trazadora
    }

    void CreateImpactVisual(RaycastHit hit)
    {
        if (data.impactPrefab != null)
        {
            GameObject impact = Instantiate(data.impactPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 4f);
        }
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(data.reloadTime);
        currentAmmo = data.maxAmmo;
        isReloading = false;
        currentShotIndex = 0;
    }
}