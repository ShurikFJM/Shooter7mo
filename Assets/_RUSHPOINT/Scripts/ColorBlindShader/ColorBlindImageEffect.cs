using UnityEngine;

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
