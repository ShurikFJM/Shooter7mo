//pipelinedefine
#define H_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace HTraceSSGI.Scripts.Extensions
{
    public class ExtensionsURP
    {
#if UNITY_2023_3_OR_NEWER
	    public static TextureHandle CreateTexture(string name, RenderGraph rg, ref TextureDesc desc, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None,
		    bool enableRandomWrite = true, bool useMipMap = false, bool autoGenerateMips = false)
	    {
		    desc.name = name;
		    desc.format = format;
		    desc.depthBufferBits = depthBufferBits;
		    desc.enableRandomWrite = enableRandomWrite;
		    desc.useMipMap = useMipMap;
		    desc.autoGenerateMips = autoGenerateMips;
		    desc.msaaSamples = MSAASamples.None;
		    return rg.CreateTexture(desc);
	    }
#endif //UNITY_2023_3_OR_NEWER
    }
}
