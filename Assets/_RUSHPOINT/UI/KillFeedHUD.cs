using UnityEngine;

public class KillFeedHUD : MonoBehaviour
{
    public static KillFeedHUD Instance;

    [Header("UI - Imágenes por Conteo de Kills")]
    [Tooltip("Arrastra los objetos en orden: \nElement 0 = 0 Kills (Default)\nElement 1 = 1 Kill\nElement 2 = 2 Kills\nElement 3 = 3 Kills\nElement 4 = 4 Kills\nElement 5 = 5 Kills\nElement 6 = 6 Kills")]
    public GameObject[] killImages;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip killSound;
    public AudioClip headshotKillSound;

    private int totalKills = 0;

    public int TotalKills => totalKills;

    void Awake()
    {
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Al iniciar la partida, mostramos la imagen por defecto (0 kills)
        ResetKills();
    }

    public void TriggerKillNotification(bool isHeadshot)
    {
        // 1. Incrementar el contador de bajas
        totalKills++;

        // 2. Actualizar la imagen en pantalla y reproducir su animación
        UpdateKillImageDisplay();

        // 3. Reproducir el sonido correspondiente
        AudioClip clipToPlay = isHeadshot ? (headshotKillSound != null ? headshotKillSound : killSound) : killSound;
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void UpdateKillImageDisplay()
    {
        if (killImages == null || killImages.Length == 0) return;

        // Determinamos el índice seguro para el arreglo
        int targetIndex = Mathf.Clamp(totalKills, 0, killImages.Length - 1);

        // Apagar absolutamente todas las imágenes
        for (int i = 0; i < killImages.Length; i++)
        {
            if (killImages[i] != null)
            {
                killImages[i].SetActive(false);
            }
        }

        // Encender únicamente la imagen correspondiente a las kills actuales
        GameObject activeKillObject = killImages[targetIndex];
        if (activeKillObject != null)
        {
            activeKillObject.SetActive(true);

            // Reiniciar e interrumpir la animación desde el segundo 0
            RestartObjectAnimation(activeKillObject);
        }
    }

    private void RestartObjectAnimation(GameObject obj)
    {
        if (obj == null) return;

        // Si utiliza el sistema de Animator de Unity
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.Play(0, -1, 0f);
        }

        // Si utiliza el sistema de Animation Legacy
        Animation legacyAnim = obj.GetComponent<Animation>();
        if (legacyAnim != null)
        {
            legacyAnim.Stop();
            legacyAnim.Rewind();
            legacyAnim.Play();
        }
    }

    // Llama a esta función para reiniciar el contador a 0 Kills (Ej: nueva ronda)
    public void ResetKills()
    {
        totalKills = 0;
        UpdateKillImageDisplay();
    }
}