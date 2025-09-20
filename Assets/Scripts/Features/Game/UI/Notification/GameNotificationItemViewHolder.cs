using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Features.Game.UI.Notification
{
    public class GameNotificationItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI textTitle;
        [SerializeField] private TextMeshProUGUI textDescription;

        private GameScene _gameScene;
        private int _version = -1;

        public void Init(GameScene gameScene)
        {
            _gameScene = gameScene;
        }

        public void Bind(GameNotificationItemUIData data)
        {
            // 每次绑定都会变更版本
            _version++;

            // 判断缩略图加载方式并加载缩略图
            switch (data.ThumbnailLoadType)
            {
                case GameNotificationThumbnailLoadType.Sprite:
                {
                    imgIcon.gameObject.SetActive(data.Thumbnail != null);
                    imgIcon.sprite = data.Thumbnail;
                }
                    break;
                case GameNotificationThumbnailLoadType.Atlas:
                {
                    // 如果没有缩略图数据，就不显示缩略图，否则就显示缩略图
                    if (string.IsNullOrEmpty(data.ThumbnailAtlas) || string.IsNullOrEmpty(data.ThumbnailName))
                    {
                        imgIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        imgIcon.gameObject.SetActive(true);
                        var version = _version;
                        _gameScene.LoadAssetAsyncTemporary<SpriteAtlas>(data.ThumbnailAtlas,
                            handle =>
                            {
                                if (version != _version) return;
                                imgIcon.sprite = handle.GetSprite(data.ThumbnailName);
                            });
                    }
                }
                    break;
                default:
                {
                    imgIcon.gameObject.SetActive(false);
                }
                    break;
            }

            // 如果没有标题数据，就不显示标题，否则就显示标题
            if (string.IsNullOrEmpty(data.Title))
            {
                textTitle.gameObject.SetActive(false);
            }
            else
            {
                textTitle.gameObject.SetActive(true);
                textTitle.text = data.Title;
            }

            // 如果没有描述数据，就不显示描述，否则就显示描述
            if (string.IsNullOrEmpty(data.Description))
            {
                textDescription.gameObject.SetActive(false);
            }
            else
            {
                textDescription.gameObject.SetActive(true);
                textDescription.text = data.Description;
            }
        }

        public void Unbind()
        {
        }
    }
}