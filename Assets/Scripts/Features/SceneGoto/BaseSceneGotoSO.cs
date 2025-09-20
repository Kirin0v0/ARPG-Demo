using Events;
using Events.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = Framework.Common.Function.SceneManager;

namespace Features.SceneGoto
{
    public abstract class BaseSceneGotoSO : ScriptableObject
    {
        [SerializeField] private string sceneName;
        public string GotoSceneName => sceneName;

        public void Goto(object payload)
        {
            GameApplication.Instance.EventCenter.TriggerEvent<GotoSceneEventParameter>(GameEvents.BeforeGotoScene, new()
            {
                SceneGoto = this
            });
            OnLoadSceneBefore(payload);
            SceneManager.Instance.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            OnLoadSceneAfter(payload);
            GameApplication.Instance.EventCenter.TriggerEvent<GotoSceneEventParameter>(GameEvents.AfterGotoScene, new()
            {
                SceneGoto = this
            });
        }

        protected virtual void OnLoadSceneBefore(object payload)
        {
        }

        protected virtual void OnLoadSceneAfter(object payload)
        {
        }
    }
}