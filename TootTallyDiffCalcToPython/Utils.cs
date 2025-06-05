using Newtonsoft.Json;
using System.Numerics;
using System.Text.Json.Nodes;

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

        public static float CalculateScoreTT(Chart chart, ScoreData score) =>
            CalculateBaseTT(chart.GetDynamicDiffRating(score.replay_speed, score.GetHitCount, score.modifiers)) * GetMultiplier(score.percentage, score.modifiers);

        public static float CalculateScoreTT(Chart chart, float replaySpeed, int hitCount, float percent, string[] modifiers = null) =>
            CalculateBaseTT(chart.GetDynamicDiffRating(replaySpeed, hitCount, modifiers)) * GetMultiplier(percent, modifiers);

        public static float CalculateScoreTT(float[] diffRatings, float replaySpeed, float percent, string[] modifiers = null) =>
            CalculateBaseTT(LerpDiff(diffRatings, replaySpeed)) * GetMultiplier(percent, modifiers);

        //OLD: https://www.desmos.com/calculator/6rle1shggs
        public static readonly Dictionary<float, float> accToMultDict = new Dictionary<float, float>()
        {
            { 1f, 40.2f },
            { .999f, 32.4f },
            { .996f, 27.2f },
            { .993f, 23.2f },
            { .99f, 20.5f },
            { .985f, 18.1f },
            { .98f, 16.1f },
            { .97f, 13.8f },
            { .96f, 11.8f },
            { .95f, 10.8f },
            { .925f, 9.2f },
            { .9f, 8.2f },
            { .875f, 7.5f },
            { .85f, 7f },
            { .8f, 6f },
            { .7f, 4f },
            { .6f, 2.2f },
            { .5f, 0.65f },
            { .25f, 0.2f },
            { 0, 0 },
        };

        public static readonly Dictionary<float, float> ezAccToMultDict = new Dictionary<float, float>()
        {
             { 1f, 15.4f },    //{ 1f, 15.4f },    //{ 1f, 15.4f },    //V3{ 1f, 10.4f },   //V2{ 1f, 7.5f },   //V1{ 1f, 7.1f },
             { .999f, 12.6f }, //{ .999f, 12.6f }, //{ .999f, 12.6f }, //{ .999f, 10.2f },  //{ .999f, 6.05f }, //{ .999f, 7f },
             { .996f, 11.6f }, //{ .996f, 10.8f }, //{ .996f, 10.6f }, //{ .996f, 9.8f },   //{ .996f, 5.05f }, //{ .996f, 6.8f },
             { .993f, 11f }, //{ .993f, 10.6f },  //{ .993f, 9.2f },  //{ .993f, 9.5f },   //{ .993f, 4.35f }, //{ .993f, 6.6f },
             { .99f, 10.6f },   //{ .99f, 9.2f },   //{ .99f, 8.6f },   //{ .99f, 9.2f },    //{ .99f, 3.8f },   //{ .99f, 6.4f },
             { .985f, 10f },  //{ .985f, 8.6f },    //{ .985f, 8f },    //{ .985f, 8.75f },  //{ .985f, 3.4f },  //{ .985f, 6.2f },
             { .98f, 9.6f },   //{ .98f, 8.2f },   //{ .98f, 7.6f },   //{ .98f, 8.3f },    //{ .98f, 3f },     //{ .98f, 5.9f },
             { .97f, 9f },   //{ .97f, 7.6f },     //{ .97f, 7f },     //{ .97f, 7.5f },    //{ .97f, 2.5f },   //{ .97f, 5.45f },
             { .96f, 8.6f },   //{ .96f, 7.2f },   //{ .96f, 6.6f },   //{ .96f, 6.6f },    //{ .96f, 2.2f },   //{ .96f, 5.15f },
             { .95f, 8.3f },   //{ .95f, 6.9f },   //{ .95f, 6.2f },   //{ .95f, 6f },      //{ .95f, 2f },     //{ .95f, 4.75f },
             { .925f, 7.6f },  //{ .925f, 6.1f },  //{ .925f, 5.5f },  //{ .925f, 5.25f },  //{ .925f, 1.75f }, //{ .925f, 4.1f },
             { .9f, 6.8f },    //{ .9f, 5.5f },    //{ .9f, 4.8f },    //{ .9f, 4.65f },    //{ .9f, 1.55f },   //{ .9f, 3.6f },
             { .875f, 6.2f },  //{ .875f, 4.8f },  //{ .875f, 4.2f },  //{ .875f, 4.35f },  //{ .875f, 1.45f }, //{ .875f, 3.1f },
             { .85f, 5.6f },   //{ .85f, 4.2f },   //{ .85f, 3.8f },   //{ .85f, 3.9f },    //{ .85f, 1.3f },   //{ .85f, 2.6f },
             { .8f, 4.6f },    //{ .8f, 3.3f },      //{ .8f, 3f },      //{ .8f, 3.45f },    //{ .8f, 1.15f },   //{ .8f, 2.1f },
             { .7f, 2.5f },   //{ .7f, 1.95f },   //{ .7f, 1.75f },   //{ .7f, 2.25f },    //{ .7f, .75f },    //{ .7f, 1.4f },
             { .6f, 1.12f },    //{ .6f, .84f },    //{ .6f, .84f },    //{ .6f, 1.23f },    //{ .6f, .41f },    //{ .6f, .8f },
             { .5f, .22f },    //{ .5f, .22f },    //{ .5f, .22f },    //{ .5f, .33f },     //{ .5f, .11f },    //{ .5f, .4f },
             { .25f, .03f },   //{ .25f, .03f },   //{ .25f, .03f },   //{ .25f, .06f },    //{ .25f, .02f },   //{ .25f, .05f },
             { 0, 0 },         //{ 0, 0 },         //{ 0, 0 },         //{ 0, 0 },          //{ 0, 0 },         //{ 0, 0 },
        };

        public static float GetMultiplier(float percent, string[] modifiers = null)
        {
            var multDict = (modifiers != null && modifiers.Contains("EZ")) ? ezAccToMultDict : accToMultDict;
            int index;
            for (index = 1; index < multDict.Count && multDict.Keys.ElementAt(index) > percent; index++) ;
            var percMax = multDict.Keys.ElementAt(index);
            var percMin = multDict.Keys.ElementAt(index - 1);
            var by = (percent - percMin) / (percMax - percMin);
            var mult = Lerp(multDict[percMin], multDict[percMax], by);
            return mult;
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

        public static List<Vector4> ConvertChartToVector(List<Note> notes, string fileName, float lengthMult = 1)
        {
            var list = new List<Vector4>();
            var lastTime = 0f;
            var lastPosition = 0f;
            for (int i = 1; i < notes.Count; i++)
            {
                var spaceDuration = notes[i].position - lastTime;
                lastTime = notes[i].position + notes[i].length;
                var noteDuration = notes[i].length;
                if (spaceDuration != 0f)
                    list.Add(new Vector4(spaceDuration * lengthMult, lastPosition, notes[i].pitchStart, 0));
                list.Add(new Vector4(noteDuration * lengthMult, notes[i].pitchStart, notes[i].pitchEnd, 1));
            }
            
            var json = JsonConvert.SerializeObject(list);
            ChartReader.SaveChartData($"{fileName}.json", json);
            return list;
        }
    }
}
