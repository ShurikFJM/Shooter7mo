using System.Collections;
using UnityEngine;
using TMPro;

public class KillFeedHUD : MonoBehaviour
{
    public static KillFeedHUD Instance;

    [Header("UI Elementos")]
    public GameObject killImageObject;     
    public TextMeshProUGUI killCountText;  

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip killSound;
    public AudioClip headshotKillSound;

    [Header("Configuración de Tiempos")]
    public float displayDuration = 2f;     

    [Header("Feedback de Bajas Consecutivas (Punch UI)")]
    public bool useScalePunch = true;       
    public float punchScaleAmount = 1.25f;  
    public float punchDuration = 0.1f;      

    private int totalKills = 0;
    private Coroutine hideCoroutine;
    private Coroutine punchCoroutine;
    private Vector3 originalImageScale = Vector3.one;
    private Vector3 originalTextScale = Vector3.one;

    private Animator imageAnimator;
    private Animation imageLegacyAnim;

    void Awake()
    {
        Instance = this;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (killImageObject != null)
        {
            originalImageScale = killImageObject.transform.localScale;
            imageAnimator = killImageObject.GetComponent<Animator>();
            imageLegacyAnim = killImageObject.GetComponent<Animation>();
        }

        if (killCountText != null)
        {
            originalTextScale = killCountText.transform.localScale;
        }

        SetUIActive(false);
    }

    public void TriggerKillNotification(bool isHeadshot)
    {
        totalKills++;

        if (killCountText != null)
        {
            killCountText.text = totalKills.ToString();
        }

        AudioClip clipToPlay = isHeadshot ? (headshotKillSound != null ? headshotKillSound : killSound) : killSound;
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }

        SetUIActive(true);

        RestartUIAnimations();

        if (useScalePunch)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(PunchScaleRoutine());
        }

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HidePanelRoutine());
    }

    private void RestartUIAnimations()
    {
      
        if (imageAnimator != null)
        {
            imageAnimator.Rebind();
            imageAnimator.Update(0f);
            imageAnimator.Play(0, -1, 0f);
        }

        if (imageLegacyAnim != null)
        {
            imageLegacyAnim.Stop();
            imageLegacyAnim.Rewind();
            imageLegacyAnim.Play();
        }

        if (killCountText != null)
        {
            Animator textAnim = killCountText.GetComponent<Animator>();
            if (textAnim != null)
            {
                textAnim.Rebind();
                textAnim.Update(0f);
                textAnim.Play(0, -1, 0f);
            }

            Animation textLegacyAnim = killCountText.GetComponent<Animation>();
            if (textLegacyAnim != null)
            {
                textLegacyAnim.Stop();
                textLegacyAnim.Rewind();
                textLegacyAnim.Play();
            }
        }
    }

    IEnumerator PunchScaleRoutine()
    {
        float timer = 0f;

        if (killImageObject != null) killImageObject.transform.localScale = originalImageScale * punchScaleAmount;
        if (killCountText != null) killCountText.transform.localScale = originalTextScale * punchScaleAmount;

        while (timer < punchDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / punchDuration;

            if (killImageObject != null)
                killImageObject.transform.localScale = Vector3.Lerp(originalImageScale * punchScaleAmount, originalImageScale, progress);

            if (killCountText != null)
                killCountText.transform.localScale = Vector3.Lerp(originalTextScale * punchScaleAmount, originalTextScale, progress);

            yield return null;
        }

        if (killImageObject != null) killImageObject.transform.localScale = originalImageScale;
        if (killCountText != null) killCountText.transform.localScale = originalTextScale;
    }

    IEnumerator HidePanelRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        SetUIActive(false);
    }

    private void SetUIActive(bool active)
    {
        if (killImageObject != null) killImageObject.SetActive(active);
        if (killCountText != null) killCountText.gameObject.SetActive(active);
    }

    public void ResetKills()
    {
        totalKills = 0;
        if (killCountText != null) killCountText.text = "0";
    }
}