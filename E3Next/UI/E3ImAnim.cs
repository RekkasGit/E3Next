using System.Runtime.CompilerServices;

namespace MonoCore
{
    public enum ImAnimEaseType
    {
        Linear = 0,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InSine,
        OutSine,
        InOutSine,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InBack,
        OutBack,
        InOutBack,
        InElastic,
        OutElastic,
        InOutElastic,
        InBounce,
        OutBounce,
        InOutBounce,
        Steps,
        CubicBezier,
        Spring,
        Custom
    }

    public enum ImAnimPolicy
    {
        Crossfade = 0,
        Cut = 1,
        Queue = 2
    }

    public enum ImAnimColorSpace
    {
        Srgb = 0,
        SrgbLinear = 1,
        Hsv = 2,
        Oklab = 3,
        Oklch = 4
    }

    public static class E3ImAnim
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imanim_UpdateBeginFrame();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static float imanim_TweenFloat(uint id, uint channelId, float target, float duration,
            int easeType, float p0, float p1, float p2, float p3, int policy, float dt, float initValue);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static float[] imanim_TweenColor(uint id, uint channelId, float targetR, float targetG, float targetB, float targetA,
            float duration, int easeType, float p0, float p1, float p2, float p3, int policy, int colorSpace, float dt,
            float initR, float initG, float initB, float initA);

        public static void BeginFrame()
        {
            imanim_UpdateBeginFrame();
        }

        public static float TweenFloat(uint id, uint channelId, float target, float duration,
            ImAnimEaseType ease = ImAnimEaseType.OutCubic,
            ImAnimPolicy policy = ImAnimPolicy.Crossfade,
            float dt = -1f,
            float initValue = 0f)
        {
            return imanim_TweenFloat(id, channelId, target, duration, (int)ease, 0f, 0f, 0f, 0f, (int)policy, dt, initValue);
        }

        public static float[] TweenColor(uint id, uint channelId, float targetR, float targetG, float targetB, float targetA, float duration,
            ImAnimEaseType ease = ImAnimEaseType.OutCubic,
            ImAnimPolicy policy = ImAnimPolicy.Crossfade,
            ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab,
            float dt = -1f,
            float initR = 1f,
            float initG = 1f,
            float initB = 1f,
            float initA = 1f)
        {
            return imanim_TweenColor(id, channelId, targetR, targetG, targetB, targetA, duration,
                (int)ease, 0f, 0f, 0f, 0f, (int)policy, (int)colorSpace, dt, initR, initG, initB, initA);
        }
    }
}
