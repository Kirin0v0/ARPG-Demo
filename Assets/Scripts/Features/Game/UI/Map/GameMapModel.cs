using Common;
using Features.Game.Data;
using Framework.Core.LiveData;
using Map;
using UnityEngine;

namespace Features.Game.UI.Map
{
    public class GameMapModel
    {
        private readonly Rect _displayRestriction;
        private readonly MapManager _mapManager;
        private readonly GameManager _gameManager;
        private readonly float _maxScale;

        private readonly MutableLiveData<GameMapUIData> _currentMap = new();
        public LiveData<GameMapUIData> GetCurrentMap() => _currentMap;

        private readonly MutableLiveData<Vector2> _indicatorPosition = new(); // 地图指示器位置，注意，该位置相对是屏幕地图尺寸的位置，而不是仅对于视口范围
        public LiveData<Vector2> GetIndicatorPosition() => _indicatorPosition;

        public GameMapModel(
            Rect displayRestriction,
            MapManager mapManager,
            GameManager gameManager,
            float maxScale
        )
        {
            _displayRestriction = displayRestriction;
            _mapManager = mapManager;
            _gameManager = gameManager;
            _maxScale = maxScale;
            _mapManager.AfterMapLoad += FetchCurrentMap;
        }

        public void FetchCurrentMap()
        {
            ReleaseCurrentMap();
            var loadMap = _mapManager.Map;
            if (!loadMap)
            {
                _currentMap.SetValue(null);
                _indicatorPosition.SetValue(Vector2.zero);
                return;
            }

            GameApplication.Instance.AddressablesManager.LoadAssetAsync<Sprite>(loadMap.Snapshot.Path,
                sprite =>
                {
                    if (_mapManager.Map != loadMap)
                    {
                        return;
                    }

                    PreserveSpriteAspectRatio(_displayRestriction,
                        new Vector2(sprite.rect.width, sprite.rect.height),
                        out var output);
                    var initialSize = new Vector2(output.width, output.height);
                    var data = new GameMapUIData
                    {
                        Map = loadMap,
                        Snapshot = sprite,
                        WindowSize = initialSize,
                        Offset = Vector2.zero,
                        Scale = 1f,
                        ViewportSize = initialSize,
                        MapSize = initialSize,
                    };
                    _currentMap.SetValue(data);
                    SetIndicationPosition(
                        data.GamePositionToDisplayOffset(
                            _gameManager.Player?.Parameters.position ??
                            loadMap.Snapshot.Center3D, Vector3.one
                        )
                    );
                });

            return;

            void PreserveSpriteAspectRatio(Rect restriction, Vector2 spriteSize, out Rect rect)
            {
                var spriteRatio = spriteSize.x / spriteSize.y;
                var rectRatio = restriction.width / restriction.height;
                rect = new Rect(restriction.x, restriction.y, restriction.width, restriction.height);
                if (spriteRatio > rectRatio)
                {
                    var oldHeight = rect.height;
                    rect.height = rect.width * (1.0f / spriteRatio);
                    rect.y += (oldHeight - rect.height) / 2f;
                }
                else
                {
                    var oldWidth = rect.width;
                    rect.width = rect.height * spriteRatio;
                    rect.x += (oldWidth - rect.width) / 2f;
                }
            }
        }

        public void SetMapOffset(Vector2 offset)
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null)
            {
                return;
            }

            // 限制地图屏幕中心点偏移值
            var data = _currentMap.Value;
            offset = new Vector2(
                Mathf.Clamp(offset.x, -data.OffsetSize.x / 2f, data.OffsetSize.x / 2f),
                Mathf.Clamp(offset.y, -data.OffsetSize.y / 2f, data.OffsetSize.y / 2f)
            );
            data.Offset = offset;
            _currentMap.SetValue(data);

            // 如果地图指示器超出屏幕视口范围，则挪动地图指示器进入视口范围
            var indicatorPosition = _indicatorPosition.Value;
            if (!data.DisplayViewport.Contains(indicatorPosition))
            {
                indicatorPosition = new Vector2(
                    indicatorPosition.x > data.DisplayViewport.xMax ? data.DisplayViewport.xMax :
                    indicatorPosition.x < data.DisplayViewport.xMin ? data.DisplayViewport.xMin : indicatorPosition.x,
                    indicatorPosition.y > data.DisplayViewport.yMax ? data.DisplayViewport.yMax :
                    indicatorPosition.y < data.DisplayViewport.yMin ? data.DisplayViewport.yMin : indicatorPosition.y
                );
                _indicatorPosition.SetValue(indicatorPosition);
            }
        }

        public void SetMapScale(float scale)
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null)
            {
                return;
            }

            var originalScale = _currentMap.Value.Scale;

            // 限制缩放比例
            scale = Mathf.Clamp(scale, 1f, _maxScale);
            var data = _currentMap.Value;
            data.Scale = scale;
            data.MapSize = data.ViewportSize * scale;
            _currentMap.SetValue(data);

            // 重新设置地图偏移值
            SetMapOffset(data.Offset * scale / originalScale);

            // 重新设置地图指示器位置
            SetIndicationPosition(_indicatorPosition.HasValue()
                ? _indicatorPosition.Value * scale / originalScale
                : Vector2.zero);
        }

        public void SetIndicationPosition(Vector2 position)
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null)
            {
                return;
            }

            var originalIndicatorPosition = _indicatorPosition.HasValue() ? _indicatorPosition.Value : Vector2.zero;

            // 限制地图指示器值
            position = new Vector2(
                Mathf.Clamp(position.x, -_currentMap.Value.MapSize.x / 2f, _currentMap.Value.MapSize.x / 2f),
                Mathf.Clamp(position.y, -_currentMap.Value.MapSize.y / 2f, _currentMap.Value.MapSize.y / 2f)
            );
            _indicatorPosition.SetValue(position);

            // 如果地图指示器超出屏幕视口范围，则设置地图屏幕中心点偏移值
            if (!_currentMap.Value.DisplayViewport.Contains(position))
            {
                SetMapOffset(_currentMap.Value.Offset + (position - originalIndicatorPosition));
            }
        }

        public void MoveMap(Vector2 deltaMovement)
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null)
            {
                return;
            }

            SetMapOffset(_currentMap.Value.Offset + deltaMovement);
        }

        public void ScaleMap(float deltaScale)
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null)
            {
                return;
            }

            SetMapScale(_currentMap.Value.Scale + deltaScale);
        }

        public void MoveIndicator(Vector2 deltaMovement)
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null || !_indicatorPosition.HasValue())
            {
                return;
            }

            SetIndicationPosition(_indicatorPosition.Value + deltaMovement);
        }

        public void ReleaseCurrentMap()
        {
            if (!_currentMap.HasValue() || _currentMap.Value == null)
            {
                return;
            }

            GameApplication.Instance?.AddressablesManager?.ReleaseAsset<Sprite>(_currentMap.Value.Map.Snapshot.Path);
            _currentMap.SetValue(null);
        }

        public void Destroy()
        {
            _mapManager.AfterMapLoad -= FetchCurrentMap;
        }
    }
}