using System;

namespace UnityEngine
{
    public struct Color32
    {
        public byte a;
        public byte b;
        public byte g;
        public byte r;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color32(System.Windows.Media.Color color)
        {
            return new Color32(color.R, color.G, color.B, color.A);
        }
    }

    public static class Mathf
    {
        public static readonly float Epsilon = float.Epsilon;

        public static float Abs(float f)
        {
            return Math.Abs(f);
        }

        public static int CeilToInt(float f)
        {
            return (int)Math.Ceiling(f);
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}