using System;

namespace R2DsgRnd
{
    public abstract class RandomizeMode
    {
        public abstract bool ShouldRandomize();
    }

    public class RandomizeModeInterval : RandomizeMode
    {
        public float IntervalInSeconds
        {
            get => IntervalInFrames / 60f;
            set => IntervalInFrames = (int) (value * 60);
        }

        private int IntervalInFrames;

        public RandomizeModeInterval(float intervalInSeconds)
        {
            IntervalInSeconds = intervalInSeconds;
        }

        public override bool ShouldRandomize() => DsgVarRandomizer.Frame % IntervalInFrames == 0;
    }
}