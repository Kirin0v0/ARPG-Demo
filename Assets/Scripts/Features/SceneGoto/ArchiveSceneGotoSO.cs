using UnityEngine;

namespace Features.SceneGoto
{
    public struct ArchiveSceneGotoParameters
    {
        public int Id;
    }
    
    [CreateAssetMenu(menuName = "Scene Goto/Archive")]
    public class ArchiveSceneGotoSO: BaseSceneGotoSO
    {
        protected override void OnLoadSceneBefore(object payload)
        {
            base.OnLoadSceneBefore(payload);
            // ArchiveSceneGotoParameters，则不允许进入场景
            if (payload is not ArchiveSceneGotoParameters parameters)
            {
                return;
            }

            // 进入游戏场景前必须设置存档id
            GameApplication.Instance.CurrentArchiveId = parameters.Id;
        }
    }
}