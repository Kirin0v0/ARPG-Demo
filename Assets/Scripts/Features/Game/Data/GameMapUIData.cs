using Map;
using UnityEngine;

namespace Features.Game.Data
{
    public class GameMapUIData
    {
        public MapObject Map;
        public Sprite Snapshot;
        public Vector2 WindowSize; // 窗口尺寸

        // 以下皆为屏幕参数
        public Vector2 Offset; // 屏幕中心点偏移，不是游戏地图中心点偏移
        public float Scale; // 屏幕缩放，影响UI缩放和屏幕范围
        public Vector2 ViewportSize; // 屏幕显示视口尺寸，不受到Scale影响
        public Vector2 MapSize; // 屏幕地图总尺寸，受到Scale影响

        public Vector3 GameOffset // 游戏地图中心点偏移
        {
            get
            {
                return DisplayOffsetToGamePosition(Offset);
            }
        }

        public Rect DisplayViewport // 屏幕显示视口范围，不受到Scale影响
        {
            get
            {
                return new Rect(Offset.x - ViewportSize.x / 2f, Offset.y - ViewportSize.y / 2f, ViewportSize.x,
                    ViewportSize.y);
            }
        }

        public Vector2 OffsetSize // 屏幕中心点偏移尺寸，受到Scale影响
        {
            get
            {
                return new Vector2(Mathf.Max(MapSize.x - ViewportSize.x, 0f),
                    Mathf.Max(MapSize.y - ViewportSize.y, 0f));
            }
        }

        /// <summary>
        /// 限制游戏位置
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 LimitGamePosition(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, Map.Snapshot.Center2D.x - Map.Snapshot.Size.x / 2,
                    Map.Snapshot.Center2D.x + Map.Snapshot.Size.x / 2),
                position.y,
                Mathf.Clamp(position.z, Map.Snapshot.Center2D.y - Map.Snapshot.Size.y / 2,
                    Map.Snapshot.Center2D.y + Map.Snapshot.Size.y / 2)
            );
        }

        /// <summary>
        /// 游戏位置转为屏幕中心点偏移值
        /// </summary>
        /// <returns></returns>
        public Vector2 GamePositionToDisplayOffset(Vector3 gamePosition, Vector3 displayScale)
        {
            // 限制游戏位置在地图范围内
            gamePosition = LimitGamePosition(gamePosition);
            // 计算离屏幕中心点对应的地图位置的偏移值
            var offset = gamePosition - (Map.Snapshot.Center3D + GameOffset);
            // 将游戏坐标系转为屏幕坐标系
            var displayPixel = GameUnitToDisplayPixel(offset);
            return new Vector2(displayPixel.x * displayScale.x, displayPixel.y * displayScale.y);
        }

        /// <summary>
        /// 游戏位置转为屏幕位置
        /// </summary>
        /// <returns></returns>
        public Vector3 GamePositionToDisplayPosition(Vector3 displayCenter, Vector3 gamePosition, Vector3 displayScale)
        {
            var offset = GamePositionToDisplayOffset(gamePosition, displayScale);
            return displayCenter + new Vector3(offset.x, offset.y, 0);
        }

        public Vector3 DisplayOffsetToGamePosition(Vector2 displayOffset)
        {
            // 限制屏幕中心点偏移范围
            var offsetX = Mathf.Clamp(displayOffset.x, -MapSize.x / 2, MapSize.x / 2);
            var offsetY = Mathf.Clamp(displayOffset.y, -MapSize.y / 2, MapSize.y / 2);
            // 将屏幕坐标系转为游戏坐标系
            return DisplayPixelToGameUnit(new Vector2(offsetX, offsetY));
        }

        private Vector2 GameUnitToDisplayPixel(Vector3 unit)
        {
            var x = unit.x / Map.Snapshot.Size.x * MapSize.x;
            var y = unit.z / Map.Snapshot.Size.y * MapSize.y;
            return new Vector2(x, y);
        }

        private Vector3 DisplayPixelToGameUnit(Vector2 pixel)
        {
            var x = pixel.x / MapSize.x * Map.Snapshot.Size.x;
            var z = pixel.y / MapSize.y * Map.Snapshot.Size.y;
            return new Vector3(x, 0, z);
        }
    }
}