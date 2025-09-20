using System;
using System.Collections.Generic;
using Framework.Common.Debug;
using UnityEngine;

namespace Combo.Graph.Editor.GUI
{
    public enum ComboGraphStyle
    {
        Normal = 0,
        Blue,
        Mint,
        Green,
        Yellow,
        Orange,
        Red,
        NormalOn,
        BlueOn,
        MintOn,
        GreenOn,
        YellowOn,
        OrangeOn,
        RedOn,
        SM_Normal,
        SM_Blue,
        SM_Mint,
        SM_Green,
        SM_Yellow,
        SM_Orange,
        SM_Red,
        SM_NormalOn,
        SM_BuleOn,
        SM_MintOn,
        SM_GreenOn,
        SM_YellowOn,
        SM_OrangeOn,
        SM_RedOn,
    }

    public class ComboGraphNodeStyle
    {
        private readonly Dictionary<ComboGraphStyle, GUIStyle> _styles = new();

        private bool _initial = false;

        public ComboGraphNodeStyle()
        {
            InitStyles();
        }

        public GUIStyle GetStyle(ComboGraphStyle comboGraphStyle)
        {
            if (!_initial)
            {
                InitStyles();
            }

            return _styles[comboGraphStyle];
        }

        public void ApplyZoomFactory(float zoomFactory)
        {
            for (var i = 0; i < _styles.Count; i++)
            {
                var style = GetStyle((ComboGraphStyle)i);
                style.fontSize = (int)Mathf.Lerp(5, 30, zoomFactory);
                if (i < 14)
                {
                    style.contentOffset = new Vector2(0, Mathf.Lerp(-30, -20, zoomFactory));
                }
            }
        }

        private void InitStyles()
        {
            for (int i = 0; i <= 6; i++)
            {
                _styles.Add((ComboGraphStyle)i, new GUIStyle($"flow node {i}"));
                _styles.Add((ComboGraphStyle)(i + 7), new GUIStyle($"flow node {i} on"));
                _styles.Add((ComboGraphStyle)(i + 14), new GUIStyle($"flow node hex {i}"));
                _styles.Add((ComboGraphStyle)(i + 21), new GUIStyle($"flow node hex {i} on"));
            }

            _initial = _styles[0].name != "StyleNotFoundError";
            if (!_initial)
            {
                _styles.Clear();
            }
        }
    }
}