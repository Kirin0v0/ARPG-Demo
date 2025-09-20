using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Features
{
    public class PlaceholderPanel : MonoBehaviour
    {
        [Title("UI关联")] [SerializeField] private TextMeshProUGUI textTitleBackground;
        [SerializeField] private TextMeshProUGUI textTitle;

        [Title("动画配置")] [SerializeField] private float animationLoopTime = 2f;
        [SerializeField] private float animationOffset = 10f;

        private float _animationTime = 0f;

        private void OnEnable()
        {
            textTitleBackground.transform.position = textTitle.transform.position;
            _animationTime = 0f;
        }

        private void Update()
        {
            // 更新动画时间
            _animationTime += Time.unscaledDeltaTime * Mathf.PI * 2 / animationLoopTime;
            // 使用正弦函数创建来回移动效果
            var offset = Mathf.Abs(Mathf.Sin(_animationTime) * animationOffset);
            // 应用新位置
            textTitleBackground.rectTransform.position = textTitle.rectTransform.position + Vector3.right * offset;
        }
    }
}