using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerMovementAudio : NetworkBehaviour
{
    private const float SPATIAL_BLEND_3D = 1f;
    private const float MIN_AUDIO_DISTANCE = 2f;
    private const float MAX_AUDIO_DISTANCE = 25f;
    private const float DEFAULT_FOOTSTEP_VOLUME = 0.7f;
    private const float DEFAULT_ACTION_VOLUME = 1f;
    private const float FOOTSTEP_MIN_PITCH = 0.9f;
    private const float FOOTSTEP_MAX_PITCH = 1.1f;
    private const float JUMP_MIN_PITCH = 0.95f;
    private const float JUMP_MAX_PITCH = 1.05f;
    private const float LAND_MIN_PITCH = 0.9f;
    private const float LAND_MAX_PITCH = 1.1f;
    private const float MIN_DISTANCE_DELTA_SQR = 0.00001f;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _footstepClips;
    [SerializeField] private float _stepDistance = 1.4f;
    [SerializeField] private AudioClip[] _jumpClips;
    [SerializeField] private AudioClip[] _landClips;
    [SerializeField] private float _minFallDistanceForLandSound = 0.35f;

    private CharacterController _characterController;
    private NetworkPlayerController _playerController;
    private Vector3 _lastGroundedPosition;
    private float _accumulatedStepDistance;
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
            _audioSource.spatialBlend = SPATIAL_BLEND_3D;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = MIN_AUDIO_DISTANCE;
            _audioSource.maxDistance = MAX_AUDIO_DISTANCE;
            _audioSource.playOnAwake = false;
        }

        _lastGroundedPosition = transform.position;
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            _lastGroundedPosition = transform.position;
            return;
        }

        CheckLandingAndAirState();
        CheckFootstepsByDistance();
    }

    private void CheckFootstepsByDistance()
    {
        if (!_characterController.isGrounded)
        {
            _lastGroundedPosition = transform.position;
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 displacementVector = new Vector3(currentPosition.x - _lastGroundedPosition.x, 0f, currentPosition.z - _lastGroundedPosition.z);
        float frameDistance = displacementVector.magnitude;

        _lastGroundedPosition = currentPosition;

        if (IsStealthKeyPressed())
        {
            return;
        }

        if (displacementVector.sqrMagnitude > MIN_DISTANCE_DELTA_SQR)
        {
            _accumulatedStepDistance += frameDistance;

            if (_accumulatedStepDistance >= _stepDistance)
            {
                _accumulatedStepDistance = 0f;
                PlayFootstepServerRpc();
            }
        }
    }

    private void CheckLandingAndAirState()
    {
        bool isGrounded = _characterController.isGrounded;

        if (!isGrounded)
        {
            if (transform.position.y > _highestAirPointY)
            {
                _highestAirPointY = transform.position.y;
            }
        }
        else
        {
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

    public void OnPlayerJumped()
    {
        if (!IsOwner)
        {
            return;
        }

        _highestAirPointY = transform.position.y;
        PlayJumpServerRpc();
    }

    private bool IsStealthKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard currentKeyboard = Keyboard.current;
        if (currentKeyboard == null)
        {
            return false;
        }

        bool shiftHeld = currentKeyboard.leftShiftKey.isPressed || currentKeyboard.rightShiftKey.isPressed;
        bool ctrlHeld = currentKeyboard.leftCtrlKey.isPressed || currentKeyboard.rightCtrlKey.isPressed;
        return shiftHeld || ctrlHeld;
#else
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        return shiftHeld || ctrlHeld;
#endif
    }

    [Rpc(SendTo.Server)]
    private void PlayFootstepServerRpc()
    {
        PlayFootstepClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayFootstepClientRpc()
    {
        PlayRandomClip(_footstepClips, DEFAULT_FOOTSTEP_VOLUME, FOOTSTEP_MIN_PITCH, FOOTSTEP_MAX_PITCH);
    }

    [Rpc(SendTo.Server)]
    private void PlayJumpServerRpc()
    {
        PlayJumpClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayJumpClientRpc()
    {
        PlayRandomClip(_jumpClips, DEFAULT_ACTION_VOLUME, JUMP_MIN_PITCH, JUMP_MAX_PITCH);
    }

    [Rpc(SendTo.Server)]
    private void PlayLandServerRpc()
    {
        PlayLandClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayLandClientRpc()
    {
        PlayRandomClip(_landClips, DEFAULT_ACTION_VOLUME, LAND_MIN_PITCH, LAND_MAX_PITCH);
    }

    private void PlayRandomClip(AudioClip[] audioClips, float audioVolume, float minPitchValue, float maxPitchValue)
    {
        if (audioClips == null || audioClips.Length == 0 || _audioSource == null)
        {
            return;
        }

        int targetIndex = Random.Range(0, audioClips.Length);
        AudioClip selectedClip = audioClips[targetIndex];

        if (selectedClip != null)
        {
            _audioSource.pitch = Random.Range(minPitchValue, maxPitchValue);
            _audioSource.PlayOneShot(selectedClip, audioVolume);
        }
    }
}