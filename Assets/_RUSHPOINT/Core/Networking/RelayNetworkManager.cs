using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayNetworkManager : MonoBehaviour
{
    public static RelayNetworkManager Instance { get; private set; }

    [SerializeField] private int _maxPlayers = 10;

    public string JoinCode { get; private set; }

    public event Action<string> OnHostStarted;
    public event Action OnClientConnected;
    public event Action<string> OnConnectionFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeServicesAsync();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleNetworkClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleNetworkClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleNetworkClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleNetworkClientDisconnected;
        }
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception exception)
        {
            OnConnectionFailed?.Invoke(exception.Message);
        }
    }

    public async Task<string> StartHostWithRelayAsync()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_maxPlayers);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            OnHostStarted?.Invoke(JoinCode);
            return JoinCode;
        }
        catch (Exception exception)
        {
            OnConnectionFailed?.Invoke(exception.Message);
            return null;
        }
    }

    public async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                OnConnectionFailed?.Invoke("Join Code is empty");
                return false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());
            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                OnConnectionFailed?.Invoke("Failed to start network client transport.");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            OnConnectionFailed?.Invoke(exception.Message);
            return false;
        }
    }

    private void HandleNetworkClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                OnClientConnected?.Invoke();
            }
        }
    }

    private void HandleNetworkClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            OnConnectionFailed?.Invoke("Disconnected from server.");
        }
    }
}