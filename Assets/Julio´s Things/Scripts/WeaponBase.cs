using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponBase : MonoBehaviour
{
    [Header("Configuración Base")]
    public WeaponData data;
    public Transform firePoint;
    public SimpleFPSController playerController;

    [Header("Animaciones y Componentes")]
    public Animator weaponAnimator;

    [Header("Efectos de Feedback")]
    public Material tracerMaterial;
    public ParticleSystem muzzleFlash;

    [Header("Retroceso Visual Procedural")]
    public Transform weaponModelTransform;
    public Vector3 kickbackOffset = new Vector3(0f, 0.02f, -0.08f);
    public Vector3 kickbackRotation = new Vector3(-3f, 1f, 0f);
    public float returnSpeed = 15f;

    // Estado Interno
    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    // Control de Retroceso
    private int currentShotIndex = 0;
    private float lastShotTime = 0f;

    // Posicionamiento, Audio y Corrutinas
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Coroutine muzzleFlashCoroutine;
    private AudioSource audioSource;

    private readonly int shootTriggerHash = Animator.StringToHash("Shoot");
    private readonly int reloadTriggerHash = Animator.StringToHash("Reload");

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => data != null ? data.maxAmmo : 0;
    public bool IsReloading => isReloading;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (data != null) currentAmmo = data.maxAmmo;
        if (firePoint == null && Camera.main != null) firePoint = Camera.main.transform;
        if (playerController == null) playerController = GetComponentInParent<SimpleFPSController>();
        if (weaponModelTransform == null) weaponModelTransform = transform;

        if (muzzleFlash != null)
        {
            muzzleFlash.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, Time.deltaTime * returnSpeed);
        targetRotation = Quaternion.Slerp(targetRotation, Quaternion.identity, Time.deltaTime * returnSpeed);

        weaponModelTransform.localPosition = Vector3.Lerp(weaponModelTransform.localPosition, targetPosition, Time.deltaTime * returnSpeed * 2f);
        weaponModelTransform.localRotation = Quaternion.Slerp(weaponModelTransform.localRotation, targetRotation, Time.deltaTime * returnSpeed * 2f);

        if (isReloading) return;

        if (Time.time - lastShotTime > data.recoilResetTime)
        {
            currentShotIndex = 0;
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < data.maxAmmo)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }

        bool shootInput = data.isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (shootInput && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + data.fireRate;
                Shoot();
            }
        }
    }

    void Shoot()
    {
        currentAmmo--;
        lastShotTime = Time.time;

        // 1. Sonido de disparo aleatorio
        PlayRandomShootSound();

        // 2. Disparar animación
        if (weaponAnimator != null)
        {
            weaponAnimator.ResetTrigger(shootTriggerHash);
            weaponAnimator.SetTrigger(shootTriggerHash);
        }

        // 3. Raycast
        Camera mainCam = Camera.main;
        Ray centerRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayOrigin = centerRay.origin;

        Vector2 recoilOffset = Vector2.zero;
        if (data.recoilPattern != null && data.recoilPattern.Length > 0)
        {
            int index = Mathf.Min(currentShotIndex, data.recoilPattern.Length - 1);
            recoilOffset = data.recoilPattern[index];
        }

        float totalSpread = data.baseSpread + (currentShotIndex * data.spreadPerShot);

        bool isMoving = playerController != null && playerController.IsMoving;
        bool inAir = playerController != null && !playerController.IsGrounded;

        float minSpreadOffset = 0f;

        if (inAir)
        {
            totalSpread += data.airSpreadMultiplier;
            minSpreadOffset = data.airSpreadMultiplier * 0.5f;
        }
        else if (isMoving)
        {
            totalSpread += data.movementSpreadMultiplier;
            minSpreadOffset = data.movementSpreadMultiplier * 0.35f;
        }

        currentShotIndex++;

        Vector2 randomSpread;
        if (minSpreadOffset > 0f)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minSpreadOffset, totalSpread);
            randomSpread = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        else
        {
            randomSpread = Random.insideUnitCircle * totalSpread;
        }

        float yawInDegrees = recoilOffset.x + randomSpread.x;
        float pitchInDegrees = recoilOffset.y + randomSpread.y;

        Quaternion spreadRotation = Quaternion.Euler(-pitchInDegrees, yawInDegrees, 0f);
        Vector3 finalDirection = mainCam.transform.rotation * spreadRotation * Vector3.forward;

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

        targetPosition += kickbackOffset;
        targetRotation *= Quaternion.Euler(kickbackRotation);

        TriggerMuzzleFlash();
        StartCoroutine(RenderTracer(firePoint.position, targetPoint));
    }

    void PlayRandomShootSound()
    {
        if (data.shootSounds != null && data.shootSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, data.shootSounds.Length);
            AudioClip clip = data.shootSounds[randomIndex];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        if (data.reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(data.reloadSound);
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger(reloadTriggerHash);
        }

        yield return new WaitForSeconds(data.reloadTime);

        currentAmmo = data.maxAmmo;
        isReloading = false;
        currentShotIndex = 0;
    }

    void TriggerMuzzleFlash()
    {
        if (muzzleFlash == null) return;

        if (muzzleFlashCoroutine != null)
        {
            StopCoroutine(muzzleFlashCoroutine);
        }

        muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
    }

    IEnumerator MuzzleFlashRoutine()
    {
        muzzleFlash.gameObject.SetActive(true);
        muzzleFlash.Clear();
        muzzleFlash.Play();

        yield return new WaitForSeconds(0.1f);

        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.gameObject.SetActive(false);
    }

    void CreateImpactVisual(RaycastHit hit)
    {
        if (data.impactPrefabs != null && data.impactPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, data.impactPrefabs.Length);
            GameObject selectedPrefab = data.impactPrefabs[randomIndex];

            if (selectedPrefab != null)
            {
                Quaternion impactRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 180f, 0);
                GameObject impact = Instantiate(selectedPrefab, hit.point + hit.normal * 0.01f, impactRotation);
                Destroy(impact, 4f);
            }
        }
    }

    IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObj = new GameObject("BulletTracer");
        LineRenderer line = tracerObj.AddComponent<LineRenderer>();

        line.startWidth = 0.02f;
        line.endWidth = 0.005f;
        line.material = tracerMaterial != null ? tracerMaterial : new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.yellow;
        line.endColor = new Color(1f, 0.4f, 0f, 0f);

        line.SetPosition(0, start);
        line.SetPosition(1, start);

        float duration = 0.03f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsedTime / duration);
            line.SetPosition(1, currentPos);
            yield return null;
        }

        line.SetPosition(1, end);
        Destroy(tracerObj, 0.02f);
    }
}