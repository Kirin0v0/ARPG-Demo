using System;
using System.Collections.Generic;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Common.UI.RecyclerView
{
    public class RecyclerViewHolder : MonoBehaviour
    {
        public const int FlagBound = 1 << 0;
        public const int FlagUpdate = 1 << 1;
        public const int FlagRemoved = 1 << 2;

        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if (!_rectTransform)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        public RecyclerView RecyclerView { get; set; }

        private int _position = RecyclerView.NoPosition;

        public int Position
        {
            set => _position = value;
            get
            {
                if (NeedsUpdate() || IsRemoved())
                {
                    return RecyclerView.NoPosition;
                }

                return _position;
            }
        }

        public int ViewType { get; set; } = RecyclerView.InvalidType;

        public int Flags { get; private set; } = 0;

        public void AddFlags(int flags)
        {
            Flags |= flags;
        }

        public void SetFlags(int flags, int mask)
        {
            Flags = (Flags & ~mask) | (flags & mask);
        }

        public bool HasFlags(int flags) => (Flags & flags) != 0;

        public bool IsBound() => (Flags & FlagBound) != 0;

        public bool NeedsUpdate() => (Flags & FlagUpdate) != 0;

        public bool IsRemoved() => (Flags & FlagRemoved) != 0;

        public void Reset()
        {
            Flags = 0;
            Position = RecyclerView.NoPosition;
        }
    }
}