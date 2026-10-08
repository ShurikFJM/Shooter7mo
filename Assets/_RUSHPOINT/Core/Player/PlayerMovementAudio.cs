using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerMovementAudio : NetworkBehaviour
{
    [Header("Audio Source 3D")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Footsteps SFX (Normal movement)")]
    [SerializeField] private AudioClip[] _footstepClips;
    [SerializeField] private float _stepInterval = 0.45f;
    [SerializeField] private float _minMoveSpeedThreshold = 1.2f;

    [Header("Jump & Landing SFX (Always audible)")]
    [SerializeField] private AudioClip[] _jumpClips;
    [SerializeField] private AudioClip[] _landClips;
    [SerializeField] private float _minFallDistanceForLandSound = 0.35f;

    private CharacterController _characterController;
    private NetworkPlayerController _playerController;
    private float _stepTimer;
    private bool _wasGroundedLastFrame = true;
    private float _highestAirPointY;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerController = GetComponent<NetworkPlayerController>();

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource != null)
        {
            _audioSource.spatialBlend = 1f; // 3D Audio
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = 2f;
            _audioSource.maxDistance = 25f;
            _audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        // Solo el dueño de la entidad evalúa su movimiento e inputs para disparar la red
        if (!IsOwner) return;

        CheckLandingAndAirState();
        CheckFootsteps();
    }

    private void CheckFootsteps()
    {
        if (!_characterController.isGrounded)
        {
            _stepTimer = _stepInterval;
            return;
        }

        // Si se presiona Shift (caminar lento) o Ctrl (agacharse), silencio absoluto
        if (IsStealthKeyPressed())
        {
            _stepTimer = _stepInterval;
            return;
        }

        // Medir velocidad en el plano horizontal (ignorar eje vertical Y)
        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);

        if (horizontalVelocity.magnitude > _minMoveSpeedThreshold)
        {
            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                _stepTimer = _stepInterval;
                PlayFootstepServerRpc();
            }
        }
        else
        {
            _stepTimer = _stepInterval;
        }
    }

    private void CheckLandingAndAirState()
    {
        bool isGrounded = _characterController.isGrounded;

        if (!isGrounded)
        {
            // Registrar el punto más alto mientras está en el aire
            if (transform.position.y > _highestAirPointY)
            {
                _highestAirPointY = transform.position.y;
            }
        }
        else
        {
            // Aterrizó este frame viniendo del aire
            if (!_wasGroundedLastFrame)
            {
                float fallDistance = _highestAirPointY - transform.position.y;
                if (fallDistance >= _minFallDistanceForLandSound)
                {
                    PlayLandServerRpc();
                }
            }

            _highestAirPointY = transform.position.y;
        }

        _wasGroundedLastFrame = isGrounded;
    }

    /// <summary>
    /// Llamar este método desde tu script de salto (ej. en NetworkPlayerController cuando salta).
    /// Si o sí suena, ignorando Shift o Ctrl.
    /// </summary>
    public void OnPlayerJumped()
    {
        if (!IsOwner) return;
        _highestAirPointY = transform.position.y;
        PlayJumpServerRpc();
    }

    private bool IsStealthKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool ctrlHeld = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        return shiftHeld || ctrlHeld;
#else
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        return shiftHeld || ctrlHeld;
#endif
    }

    // ------------------------------------------------------------------ Netcode RPCs

    [Rpc(SendTo.Server)]
    private void PlayFootstepServerRpc()
    {
        PlayFootstepClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayFootstepClientRpc()
    {
        PlayRandomClip(_footstepClips, 0.7f, 0.9f, 1.1f);
    }

    [Rpc(SendTo.Server)]
    private void PlayJumpServerRpc()
    {
        PlayJumpClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayJumpClientRpc()
    {
        PlayRandomClip(_jumpClips, 1.0f, 0.95f, 1.05f);
    }

    [Rpc(SendTo.Server)]
    private void PlayLandServerRpc()
    {
        PlayLandClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayLandClientRpc()
    {
        PlayRandomClip(_landClips, 1.0f, 0.9f, 1.1f);
    }

    private void PlayRandomClip(AudioClip[] clips, float volume, float minPitch, float maxPitch)
    {
        if (clips == null || clips.Length == 0 || _audioSource == null) return;

        int index = Random.Range(0, clips.Length);
        AudioClip clip = clips[index];

        if (clip != null)
        {
            _audioSource.pitch = Random.Range(minPitch, maxPitch);
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}