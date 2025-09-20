using Camera.Data;
using Framework.Core.LiveData;

namespace Camera
{
    public interface ICameraModel
    {
        LiveData<CameraSceneData> GetScene();
        LiveData<CameraLockData> GetLock();
    }
}