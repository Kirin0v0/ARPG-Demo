using System;
using Framework.Common.UI;
using Package.Data;
using Package.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Features.Game.UI.Package
{
    [RequireComponent(typeof(ListenStateButton))]
    public class GamePackageGridView : MonoBehaviour
    {
        [SerializeField] private Image imgHighlighted;
        [SerializeField] private Image imgFocused;
        [SerializeField] private Image imgThumbnail;

        private ListenStateButton _selectable;
        private GameScene _gameScene;
        
        public PackageGroup Data { private set; get; }

        private void Awake()
        {
            _selectable = GetComponent<ListenStateButton>();
            _selectable.OnSelectionStateNormal += OnStateNormal;
            _selectable.OnSelectionStateHighlighted += OnStateHighlighted;
            _selectable.OnSelectionStatePressed += OnStateHighlighted;
            _selectable.OnSelectionStateSelected += OnStateSelected;
        }

        private void OnDestroy()
        {
            _selectable.OnSelectionStateNormal += OnStateNormal;
            _selectable.OnSelectionStateHighlighted += OnStateHighlighted;
            _selectable.OnSelectionStatePressed += OnStateHighlighted;
            _selectable.OnSelectionStateSelected += OnStateSelected;
            _selectable = null;
        }

        public void Init(GameScene gameScene)
        {
            _gameScene = gameScene;
        }

        public void SetPackageGroup(PackageGroup packageGroup)
        {
            Data = packageGroup;
            imgThumbnail.gameObject.SetActive(false);
            if (Data != null)
            {
                _gameScene.LoadAssetAsyncTemporary<SpriteAtlas>(
                    packageGroup.Data.ThumbnailAtlas,
                    handle =>
                    {
                        if (Data != packageGroup)
                        {
                            return;
                        }

                        imgThumbnail.gameObject.SetActive(true);
                        imgThumbnail.sprite = handle.GetSprite(packageGroup.Data.ThumbnailName);
                    });
            }
        }

        private void OnStateNormal()
        {
            imgHighlighted.gameObject.SetActive(false);
            imgFocused.gameObject.SetActive(false);
        }

        private void OnStateHighlighted()
        {
            imgHighlighted.gameObject.SetActive(true);
            imgFocused.gameObject.SetActive(false);
        }

        private void OnStateSelected()
        {
            imgHighlighted.gameObject.SetActive(false);
            imgFocused.gameObject.SetActive(true);
        }
    }
}