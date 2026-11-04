using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum TutorialStep
{
    Movement,
    AimAndShoot,
    WeaponHandling,
    PickupBomb,
    PlantBomb,
    Completed
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private GameObject _tutorialUIRoot;
    [SerializeField] private TMP_Text _stepTitleText;
    [SerializeField] private TMP_Text _stepDescriptionText;
    [SerializeField] private TMP_Text _stepProgressText;
    [SerializeField] private GameObject _completionPanel;
    [SerializeField] private Button _returnToMenuButton;
    [SerializeField] private GameObject _targetsContainer;
    [SerializeField] private int _requiredTargetsHit = 3;
    [SerializeField] private GameObject _tutorialBombPrefab;
    [SerializeField] private Transform _bombSpawnPoint;
    [SerializeField] private Transform _plantSiteIndicator;

    [SerializeField] private Transform _playerSpawnPoint;

    private TutorialStep _currentStep = TutorialStep.Movement;
    private int _targetsHitCount = 0;
    private bool _hasMoved;
    private bool _hasJumped;
    private bool _hasSwitchedWeapon;
    private bool _hasReloaded;

    private BombInteractor _localBombInteractor;
    private WeaponInventory _localWeaponInventory;
    private Bomb _activeBomb;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_returnToMenuButton != null)
        {
            _returnToMenuButton.onClick.AddListener(FinishTutorialAndReturnToMenu);
        }

        if (_completionPanel != null) _completionPanel.SetActive(false);
        if (_targetsContainer != null) _targetsContainer.SetActive(false);
        if (_plantSiteIndicator != null) _plantSiteIndicator.gameObject.SetActive(false);
    }

    private IEnumerator Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }

        while (NetworkManager.Singleton == null ||
               NetworkManager.Singleton.SpawnManager == null ||
               NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() == null)
        {
            yield return null;
        }

        GameObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
        _localBombInteractor = localPlayer.GetComponent<BombInteractor>();
        _localWeaponInventory = localPlayer.GetComponent<WeaponInventory>();

        if (_playerSpawnPoint != null)
        {
            localPlayer.transform.position = _playerSpawnPoint.position;
            localPlayer.transform.rotation = _playerSpawnPoint.rotation;
        }

        SetTutorialStep(TutorialStep.Movement);
    }

    private void Update()
    {
        switch (_currentStep)
        {
            case TutorialStep.Movement:
                CheckMovementStep();
                break;

            case TutorialStep.AimAndShoot:
                break;

            case TutorialStep.WeaponHandling:
                CheckWeaponHandlingStep();
                break;

            case TutorialStep.PickupBomb:
                CheckPickupBombStep();
                break;

            case TutorialStep.PlantBomb:
                CheckPlantBombStep();
                break;
        }
    }

    private string GetKey(string actionName, string fallback)
    {
        if (InputManager.Instance == null) return fallback;
        bool isGamepad = InputManager.Instance.IsGamepadActive;
        int bindingIndex = InputManager.Instance.GetBindingIndex(actionName, isGamepad);

        if (bindingIndex >= 0)
        {
            string display = InputManager.Instance.GetBindingDisplayString(actionName, bindingIndex);
            if (!string.IsNullOrEmpty(display)) return display;
        }

        if (isGamepad)
        {
            if (actionName == "Move") return "Left Stick";
            if (actionName == "Jump") return "A";
            if (actionName == "Shoot") return "RT";
            if (actionName == "Reload") return "X";
            if (actionName == "Interact") return "Y";
            if (actionName == "Slot1" || actionName == "Slot2") return "D-Pad";
        }

        return fallback;
    }

    public void SetTutorialStep(TutorialStep step)
    {
        _currentStep = step;

        switch (step)
        {
            case TutorialStep.Movement:
                _stepTitleText.text = "FASE 1: MOVIMIENTO BÁSICO";
                _stepDescriptionText.text = $"Usa [{GetKey("Move", "WASD")}] para desplazarte y [{GetKey("Jump", "Espacio")}] para saltar.";
                _stepProgressText.text = "Muévete por el área";
                break;

            case TutorialStep.AimAndShoot:
                if (_targetsContainer != null) _targetsContainer.SetActive(true);
                _targetsHitCount = 0;
                _stepTitleText.text = "FASE 2: COMBATE Y DISPARO";
                _stepDescriptionText.text = $"Apunta con la cámara y dispara con [{GetKey("Shoot", "Click Izquierdo")}] a las dianas.";
                UpdateTargetsProgress();
                break;

            case TutorialStep.WeaponHandling:
                _hasSwitchedWeapon = false;
                _hasReloaded = false;
                _stepTitleText.text = "FASE 3: GESTIÓN DE ARMAS";
                _stepDescriptionText.text = $"Cambia de arma usando [{GetKey("Slot2", "1/2/3")}] y recarga con [{GetKey("Reload", "R")}].";
                _stepProgressText.text = "0/2 Acciones completadas";
                break;

            case TutorialStep.PickupBomb:
                SpawnTutorialBomb();
                _stepTitleText.text = "FASE 4: EL EXPLOSIVO C4";
                _stepDescriptionText.text = $"Acércate al artefacto y pulsa [{GetKey("Interact", "E")}] para recoger la bomba.";
                _stepProgressText.text = "Bomba en el suelo";
                break;

            case TutorialStep.PlantBomb:
                if (_plantSiteIndicator != null) _plantSiteIndicator.gameObject.SetActive(true);
                _stepTitleText.text = "FASE 5: PLANTADO EN EL ZONA A";
                _stepDescriptionText.text = $"Entra en la zona marcada y mantén presionado [{GetKey("Interact", "E")}] hasta armar el explosivo.";
                _stepProgressText.text = "Dirígete a la zona de plantado";
                break;

            case TutorialStep.Completed:
                if (_plantSiteIndicator != null) _plantSiteIndicator.gameObject.SetActive(false);
                if (_tutorialUIRoot != null) _tutorialUIRoot.SetActive(false);
                if (_completionPanel != null)
                {
                    _completionPanel.SetActive(true);
                    if (_returnToMenuButton != null)
                    {
                        EventSystem.current?.SetSelectedGameObject(_returnToMenuButton.gameObject);
                    }
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    private void CheckMovementStep()
    {
        if (InputManager.Instance == null) return;

        if (InputManager.Instance.MovementInput.sqrMagnitude > 0.1f) _hasMoved = true;
        if (InputManager.Instance.WasJumpTriggered) _hasJumped = true;

        if (_hasMoved && _hasJumped)
        {
            StartCoroutine(TransitionDelay(TutorialStep.AimAndShoot));
        }
    }

    public void OnTargetDestroyed()
    {
        if (_currentStep != TutorialStep.AimAndShoot) return;

        _targetsHitCount++;
        UpdateTargetsProgress();

        if (_targetsHitCount >= _requiredTargetsHit)
        {
            StartCoroutine(TransitionDelay(TutorialStep.WeaponHandling));
        }
    }

    private void UpdateTargetsProgress()
    {
        _stepProgressText.text = $"Dianas destruidas: {_targetsHitCount} / {_requiredTargetsHit}";
    }

    private void CheckWeaponHandlingStep()
    {
        if (InputManager.Instance == null) return;

        if (InputManager.Instance.WasSlot1Triggered || InputManager.Instance.WasSlot2Triggered || InputManager.Instance.WasSlot3Triggered)
        {
            _hasSwitchedWeapon = true;
        }

        if (InputManager.Instance.WasReloadTriggered)
        {
            _hasReloaded = true;
        }

        int count = (_hasSwitchedWeapon ? 1 : 0) + (_hasReloaded ? 1 : 0);
        _stepProgressText.text = $"{count}/2 Acciones completadas";

        if (_hasSwitchedWeapon && _hasReloaded)
        {
            StartCoroutine(TransitionDelay(TutorialStep.PickupBomb));
        }
    }

    private void SpawnTutorialBomb()
    {
        if (_activeBomb == null)
        {
            _activeBomb = FindAnyObjectByType<Bomb>();
        }

        if (_activeBomb != null && _bombSpawnPoint != null)
        {
            _activeBomb.ServerResetBomb(_bombSpawnPoint.position);
        }
    }

    private void CheckPickupBombStep()
    {
        if (_localBombInteractor != null && _localBombInteractor.IsCarryingBomb)
        {
            StartCoroutine(TransitionDelay(TutorialStep.PlantBomb));
        }
    }

    private void CheckPlantBombStep()
    {
        if (_activeBomb != null && _activeBomb.CurrentState == BombState.Planted)
        {
            SetTutorialStep(TutorialStep.Completed);
        }
    }

    private IEnumerator TransitionDelay(TutorialStep nextStep)
    {
        _stepProgressText.text = "<color=#00FF66>¡Completado!</color>";
        yield return new WaitForSeconds(1.2f);
        SetTutorialStep(nextStep);
    }

    public void FinishTutorialAndReturnToMenu()
    {
        StartCoroutine(ShutdownAndLoadMenuRoutine());
    }

    private IEnumerator ShutdownAndLoadMenuRoutine()
    {
        Time.timeScale = 1f;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            while (NetworkManager.Singleton.ShutdownInProgress)
            {
                yield return null;
            }
            Destroy(NetworkManager.Singleton.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }
}