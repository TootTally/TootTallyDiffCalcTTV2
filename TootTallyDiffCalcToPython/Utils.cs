namespace TootTallyDiffCalcTTV2
{
    public static class Utils
    {
        public static readonly float[] GAME_SPEED = { .5f, .75f, 1f, 1.25f, 1.5f, 1.75f, 2f };

        public static float Lerp(float firstFloat, float secondFloat, float by) //Linear easing
        {
            return firstFloat + (secondFloat - firstFloat) * by;
        }

        public static float FastPow(double num, int exp)
        {
            double result = 1.0;
            while (exp > 0)
            {
                if (exp % 2 == 1)
                    result *= num;
                exp >>= 1;
                num *= num;
            }
            return (float)result;
        }

        //TT for S rank (60% score)
        //https://www.desmos.com/calculator/rhwqyp21nr
        public static float CalculateBaseTT(float starRating)
        {
            return (0.5f * FastPow(starRating, 2) + (7f * starRating) + 0.05f);
            //y = (0.7x^2 + 12x + 0.05)/1.5
        }

        public static float CalculateScoreTT(Chart chart, float replaySpeed, float percent, string[] modifiers = null) =>
            CalculateBaseTT(chart.GetDynamicDiffRating(replaySpeed, percent, modifiers) * GetMultiplier(percent));

        public static float CalculateScoreTT(float[] diffRatings, float replaySpeed, float percent) =>
            CalculateBaseTT(LerpDiff(diffRatings, replaySpeed)) * GetMultiplier(percent);

        //OLD: https://www.desmos.com/calculator/6rle1shggs
        public static readonly Dictionary<float, float> accToMultDict = new Dictionary<float, float>()
        {
            { 1f, 46f },
            { .999f, 39f },
            { .996f, 32.6f },
            { .993f, 27.5f },
            { .99f, 23.5f },
            { .985f, 19.8f },
            { .98f, 16.8f },
            { .97f, 14.2f },
            { .96f, 12f },
            { .95f, 11f },
            { .925f, 8.9f },
            { .875f, 6.8f },
            { .8f, 4.5f },
            { .7f, 2.7f },
            { .6f, 1.4f },
            { .5f, 0.6f },
            { .25f, 0.2f },
            { 0, 0 },
        };

        public static float GetMultiplier(float percent)
        {
            int index;
            for (index = 1; index < accToMultDict.Count && accToMultDict.Keys.ElementAt(index) > percent; index++) ;
            var percMax = accToMultDict.Keys.ElementAt(index);
            var percMin = accToMultDict.Keys.ElementAt(index - 1);
            var by = (percent - percMin) / (percMax - percMin);
            return Lerp(accToMultDict[percMin], accToMultDict[percMax], by);
        }

        public static float LerpDiff(float[] diffRatings, float speed)
        {
            var index = (int)((speed - 0.5f) / .25f);
            if (speed % .25f == 0)
                return diffRatings[index];

            var minSpeed = GAME_SPEED[index];
            var maxSpeed = GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Lerp(diffRatings[index], diffRatings[index + 1], by);
        }
    }
}
