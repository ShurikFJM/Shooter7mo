using UnityEngine;

/// <summary>
/// SOLO para pipeline Built-in. Ponlo en la cámara. En URP no hace falta (usa el Fullscreen Pass).
/// </summary>
[RequireComponent(typeof(Camera))]
public class ColorBlindImageEffect : MonoBehaviour
{
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        var cb = ColorBlindManager.Instance;
        if (cb != null && cb.Material != null) Graphics.Blit(src, dst, cb.Material);
        else Graphics.Blit(src, dst);
    }
}
