using System;
using UnityEngine;

namespace Framework.DataStructure
{
    [Serializable]
    public class SerializableColor
    {
        public float r, g, b, a;

        public static SerializableColor Clear() => new SerializableColor(0.0f, 0.0f, 0.0f, 0.0f);

        public SerializableColor()
        {
        }

        public SerializableColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}