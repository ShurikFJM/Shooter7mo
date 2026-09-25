using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Zona capturable del modo Dominación. Requiere un Collider marcado como Trigger.
/// El progreso de captura vive en el servidor (CaptureValue: -1 = Azul total,
/// +1 = Rojo total, 0 = neutral) y se replica a todos los clientes, que solo
/// leen el valor para actualizar su propia UI/material — ningún cliente decide
/// el resultado de la captura.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DominationZone : NetworkBehaviour
{
    [Header("Identificación")]
    [SerializeField] private string zoneId = "A";

    [Header("Captura")]
    [Tooltip("Qué tan rápido cambia el progreso por segundo cuando solo un equipo está presente (1 = pasa de neutral a totalmente dominada en 1s).")]
    [SerializeField] private float captureSpeed = 0.2f;
    [Tooltip("Puntos otorgados por segundo al equipo dueño de la zona. Pon 0 para desactivar el otorgamiento automático.")]
    [SerializeField] private int pointsPerSecond = 1;

    [Header("Visual")]
    [SerializeField] private Renderer zoneRenderer;
    [Tooltip("Nombre de la propiedad de color del shader. Usa '_BaseColor' en URP/HDRP Lit, o '_Color' en Built-in Standard.")]
    [SerializeField] private string colorPropertyName = "_BaseColor";
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color redColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color blueColor = new Color(0.15f, 0.4f, 0.85f);

    /// <summary>-1 = Azul total, +1 = Rojo total, 0 = neutral.</summary>
    public NetworkVariable<float> CaptureValue = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Collider zoneCollider;
    private MaterialPropertyBlock propertyBlock;
    private readonly HashSet<PlayerTeam> redPlayers = new HashSet<PlayerTeam>();
    private readonly HashSet<PlayerTeam> bluePlayers = new HashSet<PlayerTeam>();
    private float scoreTickTimer;

    public string ZoneId => zoneId;

    /// <summary>Equipo dueño actual de la zona, o Neutral si está disputada o aún no se termina de capturar.</summary>
    public Team OwnerTeam
    {
        get
        {
            if (CaptureValue.Value >= 1f) return Team.Red;
            if (CaptureValue.Value <= -1f) return Team.Blue;
            return Team.Neutral;
        }
    }

    /// <summary>True si hay jugadores de ambos equipos dentro al mismo tiempo (la captura se congela).</summary>
    public bool IsContested => redPlayers.Count > 0 && bluePlayers.Count > 0;

    /// <summary>Se dispara (en el servidor) cuando la zona pasa a estar totalmente dominada por un equipo.</summary>
    public event System.Action<DominationZone, Team> OnZoneCapturedByTeam;

    /// <summary>Se dispara (en el servidor) una vez por segundo mientras la zona tiene dueño. Engancha aquí tu ScoreManager.</summary>
    public event System.Action<DominationZone, Team, int> OnZoneScoreTick;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
        propertyBlock = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        CaptureValue.OnValueChanged += HandleCaptureValueChanged;
        HandleCaptureValueChanged(CaptureValue.Value, CaptureValue.Value);
    }

    public override void OnNetworkDespawn()
    {
        CaptureValue.OnValueChanged -= HandleCaptureValueChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerTeam team = other.GetComponentInParent<PlayerTeam>();
        if (team == null) return;

        if (team.CurrentTeam.Value == Team.Red) redPlayers.Add(team);
        else if (team.CurrentTeam.Value == Team.Blue) bluePlayers.Add(team);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerTeam team = other.GetComponentInParent<PlayerTeam>();
        if (team == null) return;

        redPlayers.Remove(team);
        bluePlayers.Remove(team);
    }

    private void Update()
    {
        if (!IsServer) return;

        redPlayers.RemoveWhere(p => p == null);
        bluePlayers.RemoveWhere(p => p == null);

        UpdateCapture();
        UpdateScoreTick();
    }

    private void UpdateCapture()
    {
        bool redPresent = redPlayers.Count > 0;
        bool bluePresent = bluePlayers.Count > 0;

        // Nadie presente, o disputada por ambos equipos: sin cambios.
        if (redPresent == bluePresent) return;

        Team previousOwner = OwnerTeam;

        float delta = captureSpeed * Time.deltaTime;
        float newValue = Mathf.Clamp(CaptureValue.Value + (redPresent ? delta : -delta), -1f, 1f);
        CaptureValue.Value = newValue;

        Team newOwner = OwnerTeam;
        if (newOwner != Team.Neutral && newOwner != previousOwner)
        {
            OnZoneCapturedByTeam?.Invoke(this, newOwner);
        }
    }

    private void UpdateScoreTick()
    {
        if (pointsPerSecond <= 0) return;

        Team owner = OwnerTeam;
        if (owner == Team.Neutral)
        {
            scoreTickTimer = 0f;
            return;
        }

        scoreTickTimer += Time.deltaTime;
        if (scoreTickTimer >= 1f)
        {
            scoreTickTimer -= 1f;
            OnZoneScoreTick?.Invoke(this, owner, pointsPerSecond);
            // TODO: enganchar con tu ScoreManager, ej. ScoreManager.Instance.AddPoints(owner, pointsPerSecond);
        }
    }

    private void HandleCaptureValueChanged(float previous, float current)
    {
        if (zoneRenderer == null) return;

        Color targetColor = current >= 0f
            ? Color.Lerp(neutralColor, redColor, Mathf.Clamp01(current))
            : Color.Lerp(neutralColor, blueColor, Mathf.Clamp01(-current));

        zoneRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyName, targetColor);
        zoneRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box) Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere) Gizmos.DrawSphere(sphere.center, sphere.radius);
    }
}
