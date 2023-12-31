﻿
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

            return (0.7f * FastPow(starRating, 2) + (12f * starRating) + 0.05f)/1.5f;
            //y = (0.7x^2 + 12x + 0.05)/1.5
        }

        //https://www.desmos.com/calculator/x7c0zutgsn
        public static float CalculateScoreTT(Chart chart, float replaySpeed, float percent)
        {
            var baseTT = CalculateBaseTT(chart.GetDiffRating(replaySpeed));

            float scoreTT;
            if (percent < 0.6f)
                scoreTT = ((21.433f * FastPow(percent, 6)) - 0.028091281f) * baseTT;
            else if (percent < 0.98f)
                scoreTT = ((0.028091281f * MathF.Pow(MathF.E, 6f * percent)) - 0.028091281f) * baseTT;
            else
                scoreTT = FastPow(9.2f * percent - 7.43037117f, 5) * baseTT;
            //y = (0.28091281 * e^6x - 0.028091281) * b

            return scoreTT;
        }

        //https://www.desmos.com/calculator/x7c0zutgsn
        public static float CalculateScoreTT(float[] diffRatings, float replaySpeed, float percent)
        {
            var baseTT = CalculateBaseTT(LerpDiff(diffRatings, replaySpeed));

            float scoreTT;
            if (percent < 0.6f)
                scoreTT = 21.433f * FastPow(percent, 6) * baseTT;
            else if (percent < 0.98f)
                scoreTT = ((0.028091281f * MathF.Pow(MathF.E, 6f * percent)) - 0.028091281f) * baseTT;
            else
                scoreTT = FastPow(9.2f * percent - 7.43037117f, 5) * baseTT;
            //y = (0.28091281 * e^6x - 0.028091281) * b

            return scoreTT;
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
