using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-50)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string _MASTER_VOLUME_KEY = "Rushpoint_MasterVolume";
    private const string _SFX_VOLUME_KEY = "Rushpoint_SfxVolume";
    private const string _MUSIC_VOLUME_KEY = "Rushpoint_MusicVolume";
    private const string _MASTER_PARAM = "MasterVolume";
    private const string _SFX_PARAM = "SFXVolume";
    private const string _MUSIC_PARAM = "MusicVolume";
    private const float _DEFAULT_VOLUME = 0.5f;
    private const float _MIN_DB = -80f;

    [SerializeField] private AudioMixer _audioMixer;

    [Range(0f, 1f)]
    [SerializeField] private float _masterVolume = _DEFAULT_VOLUME;
    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = _DEFAULT_VOLUME;
    [Range(0f, 1f)]
    [SerializeField] private float _musicVolume = _DEFAULT_VOLUME;

    public float MasterVolume => _masterVolume;
    public float SfxVolume => _sfxVolume;
    public float MusicVolume => _musicVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSavedVolumePreferences();
    }

    private void Start()
    {
        ApplyAllVolumes();
    }

    private void LoadSavedVolumePreferences()
    {
        _masterVolume = PlayerPrefs.GetFloat(_MASTER_VOLUME_KEY, _DEFAULT_VOLUME);
        _sfxVolume = PlayerPrefs.GetFloat(_SFX_VOLUME_KEY, _DEFAULT_VOLUME);
        _musicVolume = PlayerPrefs.GetFloat(_MUSIC_VOLUME_KEY, _DEFAULT_VOLUME);
    }

    private void ApplyAllVolumes()
    {
        SetMasterVolume(_masterVolume);
        SetSfxVolume(_sfxVolume);
        SetMusicVolume(_musicVolume);
    }

    public void SetMasterVolume(float targetVolume)
    {
        _masterVolume = Mathf.Clamp01(targetVolume);
        ApplyMixerVolume(_MASTER_PARAM, _masterVolume);

        PlayerPrefs.SetFloat(_MASTER_VOLUME_KEY, _masterVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float targetVolume)
    {
        _sfxVolume = Mathf.Clamp01(targetVolume);
        ApplyMixerVolume(_SFX_PARAM, _sfxVolume);

        PlayerPrefs.SetFloat(_SFX_VOLUME_KEY, _sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float targetVolume)
    {
        _musicVolume = Mathf.Clamp01(targetVolume);
        ApplyMixerVolume(_MUSIC_PARAM, _musicVolume);

        PlayerPrefs.SetFloat(_MUSIC_VOLUME_KEY, _musicVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMixerVolume(string parameterName, float normalizedVolume)
    {
        if (_audioMixer == null) return;

        float targetDecibels = normalizedVolume > 0.0001f ? Mathf.Log10(normalizedVolume) * 20f : _MIN_DB;
        _audioMixer.SetFloat(parameterName, targetDecibels);
    }
}