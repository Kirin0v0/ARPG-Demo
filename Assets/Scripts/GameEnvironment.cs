using Framework.Common.Debug;
using UnityEngine;

public static class GameEnvironment
{
    public static T FindEnvironmentComponent<T>() where T : MonoBehaviour
    {
        // 创建时尝试寻找输入信息管理类，如果没找到就报错
        var environments = GameObject.FindGameObjectsWithTag("Environment");
        foreach (var environment in environments)
        {
            if (environment.TryGetComponent<T>(out var component))
            {
                return component;
            }
        }

        DebugUtil.LogError($"Can't find the {typeof(T).Name} in the scene");
        return null;
    }
}