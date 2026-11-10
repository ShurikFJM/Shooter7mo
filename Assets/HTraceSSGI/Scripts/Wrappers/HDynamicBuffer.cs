using UnityEngine;

namespace HTraceSSGI.Scripts.Wrappers
{
	public enum BufferType
	{
		ComputeBuffer,
		GraphicsBuffer,
	}
	
	public class HDynamicBuffer
	{
		private ComputeBuffer  _computeBuffer;
		private GraphicsBuffer _graphicsBuffer;

		private readonly BufferType            _bufferType;
		private readonly int                   _stride;
		private          int                   _count;
		private          int                   _countScale;
		private          Vector2Int            _resolution;
		private readonly ComputeBufferType     _computeBufferType;
		private readonly GraphicsBuffer.Target _graphicsBufferType;
		private readonly bool                  _avoidDownscale;

		public ComputeBuffer  ComputeBuffer  => _computeBuffer;
		public GraphicsBuffer GraphicsBuffer => _graphicsBuffer;
		public int            Count          => _count;
		public Vector2Int     Resolution     => _resolution;

		public bool IsCreated => _bufferType == BufferType.GraphicsBuffer ? _graphicsBuffer != null : _computeBuffer != null;

		public HDynamicBuffer(BufferType bufferType, int stride, int countScale = 1, 
			ComputeBufferType computeBufferType = ComputeBufferType.Default, GraphicsBuffer.Target graphicsBufferType = GraphicsBuffer.Target.Structured,
			bool avoidDownscale = false)
		{
			_countScale            = Mathf.Max(1, countScale);
			_stride                = stride;
			_bufferType            = bufferType;
			_computeBufferType     = computeBufferType;
			_graphicsBufferType    = graphicsBufferType;
			_avoidDownscale = avoidDownscale;
		}

		public void ReAllocIfNeeded(Vector2Int newResolution)
		{
			if (_resolution == newResolution)
				return;
			
			if (_avoidDownscale == true && _resolution.x * _resolution.y > newResolution.x * newResolution.y)
				return;
			
			Release();

			_resolution = newResolution;
			_count      = newResolution.x * newResolution.y * _countScale;

			switch (_bufferType)
			{
				case BufferType.ComputeBuffer:
					_computeBuffer = new ComputeBuffer(_count, _stride, _computeBufferType);
					break;
				case BufferType.GraphicsBuffer:
					_graphicsBuffer = new GraphicsBuffer(_graphicsBufferType, _count, _stride);
					break;
			}
		}

		public void SetBuffer(ComputeShader shader, string name, int kernelIndex)
		{
			switch (_bufferType)
			{
				case BufferType.ComputeBuffer:
					shader.SetBuffer(kernelIndex, name, _computeBuffer);
					break;
				case BufferType.GraphicsBuffer:
					shader.SetBuffer(kernelIndex, name, _graphicsBuffer);
					break;
			}
		}

		public void Release()
		{
			_computeBuffer?.Release();
			_computeBuffer = null;

			_graphicsBuffer?.Release();
			_graphicsBuffer = null;
			
			_resolution = Vector2Int.zero;
		}
	}
}
