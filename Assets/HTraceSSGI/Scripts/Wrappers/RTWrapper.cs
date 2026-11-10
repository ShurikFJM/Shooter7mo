//pipelinedefine
#define H_URP

using HTraceSSGI.Scripts.Globals;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HTraceSSGI.Scripts.Wrappers
{
	public class RTWrapper
	{
		private RenderTextureDescriptor _dscr;
		
		public RTHandle      rt;
#if NONE
		public RenderTexture rt;
#endif
		
		
#if NONE
		public void HTextureAlloc(string name, Vector2 resolutionWidthHeight, GraphicsFormat graphicsFormat, int volumeDepthOrSlices = -1, int depthBufferBits = 0,
			TextureDimension textureDimension = TextureDimension.Unknown,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true) //useDynamicScale default = true for Upscalers switch between Hardware and Software
		{
			volumeDepthOrSlices = volumeDepthOrSlices == -1 ? HRenderer.TextureXrSlices : volumeDepthOrSlices;
			textureDimension    = textureDimension == TextureDimension.Unknown ? TextureDimension.Tex2D : textureDimension;
			
			_dscr                   = new RenderTextureDescriptor(Mathf.CeilToInt(resolutionWidthHeight.x), Mathf.CeilToInt(resolutionWidthHeight.y));
			_dscr.graphicsFormat    = graphicsFormat;
			_dscr.volumeDepth       = volumeDepthOrSlices;
			_dscr.depthBufferBits   = depthBufferBits; //only 24 bits contains Stencil buffer
			_dscr.dimension         = textureDimension;
			_dscr.useMipMap         = useMipMap;
			_dscr.autoGenerateMips  = autoGenerateMips;
			_dscr.enableRandomWrite = enableRandomWrite;
			_dscr.useDynamicScale   = useDynamicScale;
				
			rt      = new RenderTexture(_dscr);
			rt.name = name;
			rt.Create();
		}
		
		public void HTextureAlloc(string name, Vector2 resolutionWidthHeight, RenderTextureFormat renderTextureFormat, int volumeDepthOrSlices = -1, int depthBufferBits = 0,
			TextureDimension textureDimension = TextureDimension.Unknown,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true) //useDynamicScale default = true for Upscalers switch between Hardware and Software
		{
			volumeDepthOrSlices = volumeDepthOrSlices == -1 ? HRenderer.TextureXrSlices : volumeDepthOrSlices;
			textureDimension    = textureDimension == TextureDimension.Unknown ? TextureDimension.Tex2D : textureDimension;
			
			_dscr                   = new RenderTextureDescriptor(Mathf.CeilToInt(resolutionWidthHeight.x), Mathf.CeilToInt(resolutionWidthHeight.y));
			_dscr.colorFormat       = renderTextureFormat;
			_dscr.volumeDepth       = volumeDepthOrSlices;
			_dscr.depthBufferBits   = depthBufferBits; //only 24 bits contains Stencil buffer
			_dscr.dimension         = textureDimension;
			_dscr.useMipMap         = useMipMap;
			_dscr.autoGenerateMips  = autoGenerateMips;
			_dscr.enableRandomWrite = enableRandomWrite;
			_dscr.useDynamicScale   = useDynamicScale;
				
			rt      = new RenderTexture(_dscr);
			rt.name = name;
			rt.Create();
		}
		
		public void HTextureAlloc(string name, int width, int height, GraphicsFormat graphicsFormat, int volumeDepthOrSlices = -1, int depthBufferBits = 0, TextureDimension textureDimension = TextureDimension.Tex2D,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true)
		{
			volumeDepthOrSlices     = volumeDepthOrSlices == -1 ? HRenderer.TextureXrSlices : volumeDepthOrSlices;
			textureDimension        = textureDimension == TextureDimension.Unknown ? TextureDimension.Tex2D : textureDimension;
			
			_dscr                   = new RenderTextureDescriptor(width, height);
			_dscr.graphicsFormat    = graphicsFormat;
			_dscr.volumeDepth       = volumeDepthOrSlices;
			_dscr.depthBufferBits   = depthBufferBits; //only 24 bits contains Stencil buffer
			_dscr.dimension         = textureDimension;
			_dscr.useMipMap         = useMipMap;
			_dscr.autoGenerateMips  = autoGenerateMips;
			_dscr.enableRandomWrite = enableRandomWrite;
			_dscr.useDynamicScale   = useDynamicScale;
			
			rt      = new RenderTexture(_dscr);
			rt.name = name;
			rt.Create();
		}

		public void HTextureAlloc(string name, int width, int height, RenderTextureFormat renderTextureFormat, int volumeDepth = -1, int depthBufferBits = 0,
			TextureDimension textureDimension = TextureDimension.Unknown,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true) //useDynamicScale default = true for Upscalers switch between Hardware and Software
		{
			volumeDepth = volumeDepth == -1 ? HRenderer.TextureXrSlices : volumeDepth;
			textureDimension    = textureDimension == TextureDimension.Unknown ? TextureDimension.Tex2D : textureDimension;

			_dscr                   = new RenderTextureDescriptor(width, height);
			_dscr.colorFormat       = renderTextureFormat;
			_dscr.volumeDepth       = volumeDepth;
			_dscr.depthBufferBits   = depthBufferBits; //only 24 bits contains Stencil buffer
			_dscr.dimension         = textureDimension;
			_dscr.useMipMap         = useMipMap;
			_dscr.autoGenerateMips  = autoGenerateMips;
			_dscr.enableRandomWrite = enableRandomWrite;
			_dscr.useDynamicScale   = useDynamicScale;

			rt      = new RenderTexture(_dscr);
			rt.name = name;
			rt.Create();
		}


		public void HRelease()
		{
			if (rt != null) 
				rt.Release();
		}
#endif
		
		public void HTextureAlloc(string name, Vector2 scaleFactor, GraphicsFormat graphicsFormat, int volumeDepthOrSlices = -1, int depthBufferBits = 0,
			TextureDimension textureDimension = TextureDimension.Unknown,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true) //useDynamicScale default = true for Upscalers switch between Hardware and Software
		{
			volumeDepthOrSlices = volumeDepthOrSlices == -1 ? TextureXR.slices : volumeDepthOrSlices;
			textureDimension = textureDimension == TextureDimension.Unknown ? TextureXR.dimension : textureDimension;
			
			rt = RTHandles.Alloc(scaleFactor, volumeDepthOrSlices, dimension: textureDimension, colorFormat: graphicsFormat, name: name,
				enableRandomWrite: enableRandomWrite, useMipMap: useMipMap, useDynamicScale: useDynamicScale, autoGenerateMips: autoGenerateMips,
				depthBufferBits: (DepthBits)depthBufferBits);
		}
		
		public void HTextureAlloc(string name, ScaleFunc scaleFunc, GraphicsFormat graphicsFormat, int volumeDepthOrSlices = -1, int depthBufferBits = 0,
			TextureDimension textureDimension = TextureDimension.Unknown,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true) //useDynamicScale default = true for Upscalers switch between Hardware and Software
		{
			volumeDepthOrSlices = volumeDepthOrSlices == -1 ? TextureXR.slices : volumeDepthOrSlices;
			textureDimension    = textureDimension == TextureDimension.Unknown ? TextureXR.dimension : textureDimension;
			
			rt = RTHandles.Alloc(scaleFunc, volumeDepthOrSlices, dimension: textureDimension, colorFormat: graphicsFormat, name: name,
				enableRandomWrite: enableRandomWrite, useMipMap: useMipMap, useDynamicScale: useDynamicScale, autoGenerateMips: autoGenerateMips,
				depthBufferBits: (DepthBits)depthBufferBits);
		}
		
		public void HTextureAlloc(string name, int width, int height, GraphicsFormat graphicsFormat, int volumeDepthOrSlices = -1, int depthBufferBits = 0,
			TextureDimension textureDimension = TextureDimension.Unknown,
			bool useMipMap = false, bool autoGenerateMips = false, bool enableRandomWrite = true, bool useDynamicScale = true) //useDynamicScale default = true for Upscalers switch between Hardware and Software
		{
			volumeDepthOrSlices = volumeDepthOrSlices == -1 ? TextureXR.slices : volumeDepthOrSlices;
			textureDimension    = textureDimension == TextureDimension.Unknown ? TextureXR.dimension : textureDimension;
			
			rt = RTHandles.Alloc(width, height, volumeDepthOrSlices, dimension: textureDimension, colorFormat: graphicsFormat, name: name, 
				enableRandomWrite: enableRandomWrite, useMipMap: useMipMap, useDynamicScale: useDynamicScale, autoGenerateMips: autoGenerateMips,
				depthBufferBits: (DepthBits)depthBufferBits);
		}
		
		public void HRelease()
		{
			RTHandles.Release(rt);
		}
		
		public void ReAllocateIfNeeded(string name, ref RenderTextureDescriptor inputDescriptor, GraphicsFormat graphicsFormat = GraphicsFormat.None,
			int width = -1, int height = -1, int volumeDepth = 1,
			TextureDimension dimension = TextureDimension.Tex2D,
			bool enableRandomWrite = true, bool useMipMap = false, bool autoGenerateMips = false,
			bool useDynamicScale = false)
		{
			if (dimension != TextureDimension.Tex2D || width > 0 || height > 0) //conditions to create new descriptor
			{
				RenderTextureDescriptor newDesc = inputDescriptor;
				
				newDesc.width = width == -1 ? inputDescriptor.width : width;
				newDesc.height = height == -1 ? inputDescriptor.height : height;
				newDesc.volumeDepth = volumeDepth;
				newDesc.dimension = dimension;
				newDesc.graphicsFormat = graphicsFormat;
				newDesc.autoGenerateMips = autoGenerateMips;
				newDesc.useMipMap = useMipMap;
				newDesc.enableRandomWrite = enableRandomWrite;
				newDesc.msaaSamples = 1;
				newDesc.useDynamicScale = false;

#if UNITY_2023_3_OR_NEWER
				RenderingUtils.ReAllocateHandleIfNeeded(ref rt, newDesc, name: name);
#else
				RenderingUtils.ReAllocateIfNeeded(ref rt, newDesc, name: name);
#endif
				return;
			}
			
			inputDescriptor.volumeDepth = volumeDepth;
			inputDescriptor.graphicsFormat = graphicsFormat;
			inputDescriptor.autoGenerateMips = autoGenerateMips;
			inputDescriptor.useMipMap = useMipMap;
			inputDescriptor.enableRandomWrite = enableRandomWrite;
			inputDescriptor.msaaSamples = 1;
			inputDescriptor.useDynamicScale = false;

#if UNITY_2023_3_OR_NEWER
			RenderingUtils.ReAllocateHandleIfNeeded(ref rt, inputDescriptor, name: name);
#else
			RenderingUtils.ReAllocateIfNeeded(ref rt, inputDescriptor, name: name);
#endif
		}
	}
}
