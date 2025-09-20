using System;

namespace Features.Game.Data
{
    public struct GameUIData
    {
        private bool _enable;
        private int _invisibleReferenceCount;
        private object _payload;

        public bool Enable => _enable;
        public bool Visible => Enable && _invisibleReferenceCount == 0;
        public object Payload => _payload;

        public GameUIData Open(object payload = null)
        {
            return new GameUIData
            {
                _enable = true,
                _invisibleReferenceCount = _invisibleReferenceCount,
                _payload = payload,
            };
        }

        public GameUIData Close()
        {
            return new GameUIData
            {
                _enable = false,
                _invisibleReferenceCount = _invisibleReferenceCount,
                _payload = null,
            };
        }

        public GameUIData Hide()
        {
            return new GameUIData
            {
                _enable = _enable,
                _invisibleReferenceCount = Math.Max(_invisibleReferenceCount + 1, 0),
                _payload = _payload,
            };
        }

        public GameUIData Show(object payload = null)
        {
            return new GameUIData
            {
                _enable = _enable,
                _invisibleReferenceCount = Math.Max(_invisibleReferenceCount - 1, 0),
                _payload = payload ?? _payload,
            };
        }

        public GameUIData ToVisible(object payload = null)
        {
            return new GameUIData
            {
                _enable = _enable,
                _invisibleReferenceCount = 0,
                _payload = payload ?? _payload,
            };
        }
    }
}