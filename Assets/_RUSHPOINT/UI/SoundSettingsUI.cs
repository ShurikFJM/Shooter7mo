using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;

    private void Start()
    {
        InitializeVolumeSliders();
    }

    private void OnEnable()
    {
        RegisterSliderEvents();
    }

    private void OnDisable()
    {
        UnregisterSliderEvents();
    }

    private void InitializeVolumeSliders()
    {
        if (SoundManager.Instance == null) return;

        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.minValue = 0f;
            _masterVolumeSlider.maxValue = 1f;
            _masterVolumeSlider.value = SoundManager.Instance.MasterVolume;
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.minValue = 0f;
            _sfxVolumeSlider.maxValue = 1f;
            _sfxVolumeSlider.value = SoundManager.Instance.SfxVolume;
        }

        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.minValue = 0f;
            _musicVolumeSlider.maxValue = 1f;
            _musicVolumeSlider.value = SoundManager.Instance.MusicVolume;
        }
    }

    private void RegisterSliderEvents()
    {
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
        }

        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        }
    }

    private void UnregisterSliderEvents()
    {
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
        }

        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
        }
    }

    private void HandleMasterVolumeChanged(float targetVolume)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.SetMasterVolume(targetVolume);
    }

    private void HandleSfxVolumeChanged(float targetVolume)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.SetSfxVolume(targetVolume);
    }

    private void HandleMusicVolumeChanged(float targetVolume)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.SetMusicVolume(targetVolume);
    }
}