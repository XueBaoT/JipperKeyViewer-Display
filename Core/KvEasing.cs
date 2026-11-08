

using System;

namespace JipperKeyViewer.Core
{
    public static class KvEasing
    {

        public static readonly string[] Names =
        {
            "linear", "smoothstep", "smootherstep",
            "ease-in-sine", "ease-out-sine", "ease-in-out-sine",
            "ease-in-quad", "ease-out-quad", "ease-in-out-quad",
            "ease-in-cubic", "ease-out-cubic", "ease-in-out-cubic",
            "ease-in-quart", "ease-out-quart", "ease-in-out-quart",
            "ease-in-quint", "ease-out-quint", "ease-in-out-quint",
            "ease-in-expo", "ease-out-expo", "ease-in-out-expo",
            "ease-in-circ", "ease-out-circ", "ease-in-out-circ",
            "ease-in-back", "ease-out-back", "ease-in-out-back",
        };

        public const string Default = "ease-out-cubic";


        public static int IndexOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            for (int i = 0; i < Names.Length; i++)
                if (string.Equals(Names[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }


        public static string Normalize(string name) => Names[IndexOf(name)];


        public static float Ease(string name, float t)
        {


            if (float.IsNaN(t)) t = 0f;
            t = Math.Clamp(t, 0f, 1f);
            switch (Normalize(name))
            {
                case "smoothstep": return t * t * (3f - 2f * t);
                case "smootherstep": return t * t * t * (t * (t * 6f - 15f) + 10f);

                case "ease-in-sine": return 1f - MathF.Cos(t * MathF.PI * 0.5f);
                case "ease-out-sine": return MathF.Sin(t * MathF.PI * 0.5f);
                case "ease-in-out-sine": return -(MathF.Cos(MathF.PI * t) - 1f) * 0.5f;

                case "ease-in-quad": return t * t;
                case "ease-out-quad": return 1f - (1f - t) * (1f - t);
                case "ease-in-out-quad": return t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) * 0.5f;

                case "ease-in-cubic": return t * t * t;
                case "ease-out-cubic": return 1f - MathF.Pow(1f - t, 3f);
                case "ease-in-out-cubic": return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) * 0.5f;

                case "ease-in-quart": return t * t * t * t;
                case "ease-out-quart": return 1f - MathF.Pow(1f - t, 4f);
                case "ease-in-out-quart": return t < 0.5f ? 8f * t * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 4f) * 0.5f;

                case "ease-in-quint": return t * t * t * t * t;
                case "ease-out-quint": return 1f - MathF.Pow(1f - t, 5f);
                case "ease-in-out-quint": return t < 0.5f ? 16f * t * t * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 5f) * 0.5f;

                case "ease-in-expo": return t <= 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);
                case "ease-out-expo": return t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);

                case "ease-in-out-expo":
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return t < 0.5f
                        ? MathF.Pow(2f, 20f * t - 10f) * 0.5f
                        : (2f - MathF.Pow(2f, -20f * t + 10f)) * 0.5f;

                case "ease-in-circ": return 1f - MathF.Sqrt(MathF.Max(0f, 1f - t * t));
                case "ease-out-circ": return MathF.Sqrt(MathF.Max(0f, 1f - (t - 1f) * (t - 1f)));
                case "ease-in-out-circ":
                    if (t < 0.5f) return (1f - MathF.Sqrt(MathF.Max(0f, 1f - 4f * t * t))) * 0.5f;
                    return (MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow(-2f * t + 2f, 2f))) + 1f) * 0.5f;

                case "ease-in-back": return Back(t, 1.70158f, false);
                case "ease-out-back": return Back(t, 1.70158f, true);
                case "ease-in-out-back": return BackInOut(t);

                default: return t;
            }
        }

        private static float Back(float t, float c1, bool outwards)
        {
            const float c3 = 1.70158f + 1f;
            if (!outwards) return c3 * t * t * t - 1.70158f * t * t;
            float p = t - 1f;
            return 1f + c3 * p * p * p + 1.70158f * p * p;
        }

        private static float BackInOut(float t)
        {
            const float c2 = 1.70158f * 1.525f;
            if (t < 0.5f)
            {
                float p = 2f * t;
                return p * p * ((c2 + 1f) * p - c2) * 0.5f;
            }
            float q = 2f * t - 2f;
            return (q * q * ((c2 + 1f) * q + c2) + 2f) * 0.5f;
        }
    }
}
