using Framework.Common.UI.Panel;
using TMPro;
using UnityEngine;

namespace Features.Splash.UI
{
    public class SplashPanel : BaseUGUIPanel
    {
        [SerializeField] private float animationLoopTime = 2f;
        [SerializeField] private float animationOffset = 10f;

        private TextMeshProUGUI _textTitleBackground;
        private TextMeshProUGUI _textTitle;

        private float _animationTime = 0f;

        protected override void OnInit()
        {
            _textTitleBackground = GetWidget<TextMeshProUGUI>("TextTitleBackground");
            _textTitle = GetWidget<TextMeshProUGUI>("TextTitle");
        }

        protected override void OnShow(object payload)
        {
            _textTitleBackground.transform.position = _textTitle.transform.position;
            _animationTime = 0f;
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 更新动画时间
            _animationTime += Time.deltaTime * Mathf.PI * 2 / animationLoopTime;
            // 使用正弦函数创建来回移动效果
            var offset = Mathf.Abs(Mathf.Sin(_animationTime) * animationOffset);
            // 应用新位置
            _textTitleBackground.rectTransform.position = _textTitle.rectTransform.position + Vector3.right * offset;
        }

        protected override void OnHide()
        {
        }
    }
}