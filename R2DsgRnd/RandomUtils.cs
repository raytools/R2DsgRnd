using System;
using Ray2Mod.Game.Structs;

namespace R2DsgRnd
{
    static class RandomUtils
    {

        public static float RandomFloat(this Random rand, float v1, float v2)
        {
            return (float)(v1 + rand.NextDouble() * (v2 - v1));
        }

        public static Vector3 RandomVector3(this Random rand)
        {
            return new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f).Normalized();
        }
    }
}