using System;

namespace Ovomium.Features.SlopeWalk
{
    /// <summary>Calcul pur du ralentissement en montée (sans Unity ni Valheim, testé dans Ovomium.Tests).</summary>
    internal static class SlopeSlowdown
    {
        /// <summary>Pente en degrés à partir de laquelle le ralentissement commence.</summary>
        public const float StartAngle = 20f;

        /// <summary>
        /// Part de montée du déplacement, de 0 à 1 : 1 droit dans la pente, 0 en traversée et en descente. La normale
        /// du sol penche vers l'aval, donc monter = aller à l'opposé de sa composante horizontale.
        /// </summary>
        public static float Uphill(float moveX, float moveZ, float normalX, float normalZ)
        {
            float moveLength = (float)Math.Sqrt(moveX * moveX + moveZ * moveZ);
            float normalLength = (float)Math.Sqrt(normalX * normalX + normalZ * normalZ);
            if (moveLength <= 0f || normalLength <= 0f)
                return 0f;
            return Clamp01(-(moveX * normalX + moveZ * normalZ) / (moveLength * normalLength));
        }

        /// <summary>Part de vitesse à retirer, de 0 à 1 : rampe de <see cref="StartAngle"/> à <paramref name="maxAngle"/>.</summary>
        public static float Target(float angle, float uphill, float maxAngle, float maxPercent)
        {
            float range = maxAngle - StartAngle;
            float ramp = range > 0f ? Clamp01((angle - StartAngle) / range) : (angle >= maxAngle ? 1f : 0f);
            return ramp * Clamp01(uphill) * Clamp01(maxPercent / 100f);
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
