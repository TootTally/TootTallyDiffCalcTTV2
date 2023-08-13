namespace TootTallyDiffCalcTTV2
{
    public static class Utils
    {
        public static readonly float[] GAME_SPEED = { .5f, .75f, 1f, 1.25f, 1.5f, 1.75f, 2f };

        public static double Lerp(double firstFloat, double secondFloat, float by) //Linear easing
        {
            return firstFloat + (secondFloat - firstFloat) * by;
        }

        public static double FastPow(double num, int exp)
        {
            double result = 1.0;
            while (exp > 0)
            {
                if (exp % 2 == 1)
                    result *= num;
                exp >>= 1;
                num *= num;
            }
            return result;
        }

        //TT for S rank (60% score)
        //https://www.desmos.com/calculator/rhwqyp21nr
        public static double CalculateBaseTT(double starRating)
        {

            return 1.05f * FastPow(starRating, 2) + (3f * starRating) + 0.01f;
            //y = 1.05x^2 + 3x + 0.01
        }

        //https://www.desmos.com/calculator/bnyo9f5u1y
        public static double CalculateScoreTT(Chart chart, float replaySpeed, float percent)
        {
            var baseTT = CalculateBaseTT(chart.GetDiffRating(replaySpeed));

            var scoreTT = ((0.028091281 * Math.Pow(Math.E, 6d * percent)) - 0.028091281) * baseTT;
            //y = (0.28091281 * e^6x - 0.028091281) * b

            return scoreTT;
        }

        public static double CalculateScoreTT(float[] diffRatings, float replaySpeed, float percent)
        {
            var baseTT = CalculateBaseTT(LerpDiff(diffRatings, replaySpeed));

            var scoreTT = ((0.028091281 * Math.Pow(Math.E, 6d * percent)) - 0.028091281) * baseTT;
            //y = (0.28091281 * e^6x - 0.028091281) * b

            return scoreTT;
        }

        public static double LerpDiff(float[] diffRatings, float speed)
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
