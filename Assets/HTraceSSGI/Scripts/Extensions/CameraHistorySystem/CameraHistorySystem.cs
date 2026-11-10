using System;

namespace HTraceSSGI.Scripts.Extensions.CameraHistorySystem
{
    public class CameraHistorySystem<T> where T : struct, ICameraHistoryData
    {
        private const int MaxCameraCount = 4; // minimum 2

        private int _cameraHistoryIndex;
        private readonly T[] _cameraHistoryData = new T[MaxCameraCount];


        public int UpdateCameraHistoryIndex(int currentCameraHash)
        {
            _cameraHistoryIndex = GetCameraHistoryDataIndex(currentCameraHash);
            return _cameraHistoryIndex;
        }

        private int GetCameraHistoryDataIndex(int cameraHash)
        {
            // Unroll manually for MAX_CAMERA_COUNT = 4
            if (_cameraHistoryData[0].GetHash() == cameraHash) return 0;
            if (_cameraHistoryData[1].GetHash() == cameraHash) return 1;
            if (_cameraHistoryData[2].GetHash() == cameraHash) return 2;
            if (_cameraHistoryData[3].GetHash() == cameraHash) return 3;
            return -1; // new camera
        }

        public void UpdateCameraHistoryData()
        {
            bool cameraHasChanged = _cameraHistoryIndex == -1;

            if (cameraHasChanged)
            {
                const int lastIndex = MaxCameraCount - 1;

                if (_cameraHistoryData[lastIndex] is IDisposable disposable)
                    disposable.Dispose();

                // Shift the camera history data back by one
                Array.Copy(_cameraHistoryData, 0, _cameraHistoryData, 1, lastIndex);

                _cameraHistoryIndex = 0;
                _cameraHistoryData[0] = new T(); //it's critical
            }
        }

        public ref T GetCameraData()
        {
            return ref _cameraHistoryData[_cameraHistoryIndex];
        }

        public T[] GetCameraDatas()
        {
            return _cameraHistoryData;
        }

        public void SetCameraData(T data)
        {
            _cameraHistoryData[_cameraHistoryIndex] = data;
        }

        public void Cleanup()
        {
            for (int index = 0; index < _cameraHistoryData.Length; index++)
            {
                _cameraHistoryData[index] = default;
            }
        }
    }
}
