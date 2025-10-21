using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TootTallyDiffCalcTTV2
{
    public class ChartPerformances
    {
        public static readonly float[] weights = {
             1.0000f, 0.9000f, 0.8100f, 0.7290f, 0.6561f, 0.5905f, 0.5314f, 0.4783f,
             0.4305f, 0.3874f, 0.3487f, 0.3138f, 0.2824f, 0.2542f, 0.2288f, 0.2059f,
             0.1853f, 0.1668f, 0.1501f, 0.1351f, 0.1216f, 0.1094f, 0.0985f, 0.0887f,
             0.0798f, 0.0718f, 0.0646f, 0.0582f, 0.0524f, 0.0472f, 0.0425f, 0.0383f,
             0.0345f, 0.0311f, 0.0280f, 0.0252f, 0.0227f, 0.0204f, 0.0184f, 0.0166f,
             0.0149f, 0.0134f, 0.0121f, 0.0109f, 0.0098f, 0.0088f, 0.0079f, 0.0071f,
             0.0064f, 0.0057f, 0.0051f, 0.0046f, 0.0041f, 0.0037f, 0.0033f, 0.0030f,
             0.0027f, 0.0024f, 0.0022f, 0.0020f, 0.0018f, 0.0016f, 0.0015f, 0.0013f // :)
        };
        public const float CHEESABLE_THRESHOLD = 34.375f;

        public List<DataVector>[] aimPerfDict;
        public DataVectorAnalytics[] aimAnalyticsDict;

        public List<DataVector>[] tapPerfDict;
        public DataVectorAnalytics[] tapAnalyticsDict;

        public List<DataVector>[] accPerfDict;
        public DataVectorAnalytics[] accAnalyticsDict;

        public float[] aimRatingDict;
        public float[] tapRatingDict;
        public float[] accRatingDict;
        public float[] starRatingDict;

        private Chart _chart;
        private readonly int NOTE_COUNT;

        public ChartPerformances(Chart chart)
        {
            aimPerfDict = new List<DataVector>[7];
            tapPerfDict = new List<DataVector>[7];
            accPerfDict = new List<DataVector>[7];
            aimRatingDict = new float[7];
            tapRatingDict = new float[7];
            accRatingDict = new float[7];
            starRatingDict = new float[7];
            aimAnalyticsDict = new DataVectorAnalytics[7];
            tapAnalyticsDict = new DataVectorAnalytics[7];
            accAnalyticsDict = new DataVectorAnalytics[7];

            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                aimPerfDict[i] = new List<DataVector>(chart.noteCount);
                tapPerfDict[i] = new List<DataVector>(chart.noteCount);
                accPerfDict[i] = new List<DataVector>(chart.noteCount);
            }
            _chart = chart;
            NOTE_COUNT = _chart.notesDict[0].Count;
        }

        public const float AIM_DIV = 45;
        public const float TAP_DIV = 50;
        public const float ACC_DIV = 20;
        public const float AIM_END = 35;
        public const float TAP_END = 8;
        public const float ACC_END = 180;
        public const float MUL_END = 50;
        public const float MAX_DIST = 8f;
        public const float VEL_DIV = 25f;

        public void CalculatePerformances(int speedIndex)
        {
            var noteList = _chart.notesDict[speedIndex];
            var aimEndurance = 0f;
            var tapEndurance = 0f;
            var lastVelocity = 0f;
            var velocityDebuff = 1f;
            for (int i = 0; i < NOTE_COUNT; i++) //Main Forward Loop
            {
                var currentNote = noteList[i];
                int noteCount = 0;
                float weightSum = 0f;
                var aimStrain = 0f;
                var tapStrain = 0f;
                for (int j = i - 1; j >= 0 && noteCount < 6 && (MathF.Abs(currentNote.position - noteList[j].position) <= MAX_DIST || i - j <= 2); j--)
                {
                    var prevNote = noteList[j];
                    var nextNote = noteList[j + 1];
                    if (prevNote.position >= nextNote.position) break;

                    var weight = weights[noteCount];
                    noteCount++;
                    weightSum += weight;

                    var lengthSum = prevNote.length;
                    var deltaSlideSum = MathF.Abs(prevNote.pitchDelta);
                    if (deltaSlideSum <= CHEESABLE_THRESHOLD)
                        deltaSlideSum *= .35f;
                    while (prevNote.isSlider)
                    {
                        if (j-- <= 0)
                            break;
                        prevNote = noteList[j];
                        nextNote = noteList[j + 1];

                        if (prevNote.pitchDelta == 0)
                            lengthSum += prevNote.length * .85f;
                        else
                        {
                            var deltaSlide = MathF.Abs(prevNote.pitchDelta);
                            lengthSum += prevNote.length;
                            if (deltaSlide <= CHEESABLE_THRESHOLD)
                                deltaSlide *= .25f;
                            deltaSlideSum += deltaSlide;
                        }

                    }
                    var deltaTime = nextNote.position - prevNote.position;

                    if (deltaSlideSum != 0)
                    {
                        //Acc Calc
                        aimStrain += CalcAccStrain(lengthSum, deltaSlideSum, weight) / ACC_DIV;
                        aimEndurance += CalcAccEndurance(lengthSum, deltaSlideSum, weight);
                    }

                    //Aim Calc
                    var aimDistance = MathF.Abs(nextNote.pitchStart - prevNote.pitchEnd);
                    var currVelocity = MathF.Abs(aimDistance / deltaTime);
                    var velocityDebuffDelta = ComputeVelocityDebuff(lastVelocity, currVelocity) - velocityDebuff;
                    velocityDebuff += velocityDebuffDelta / VEL_DIV;

                    if (aimDistance != 0)
                    {
                        aimStrain += CalcAimStrain(aimDistance, weight, deltaTime) * velocityDebuff;
                        aimEndurance += CalcAimEndurance(aimDistance, weight, deltaTime) * velocityDebuff;
                    }

                    //Tap Calc
                    tapStrain += CalcTapStrain(deltaTime, weight, aimDistance) * velocityDebuff;
                    tapEndurance += CalcTapEndurance(deltaTime, weight, aimDistance) * velocityDebuff;

                    lastVelocity = currVelocity;
                }
                aimStrain = ComputeStrain(aimStrain) / AIM_DIV;
                tapStrain = ComputeStrain(tapStrain) / TAP_DIV;
                if (i > 0)
                {
                    var endTapDivider = 61f - MathF.Min(currentNote.position - noteList[i - 1].position, 5f) * 12f;
                    var endAimDivider = MathF.Min(MathF.Abs(currentNote.pitchEnd - currentNote.pitchStart), CHEESABLE_THRESHOLD) / CHEESABLE_THRESHOLD * 51f + 10f;
                    var aimThreshold = MathF.Sqrt(aimStrain) * 1.5f;//MathF.Pow(aimStrain, .8f) * 1.25f;
                    var tapThreshold = MathF.Sqrt(tapStrain) * 2.25f;//MathF.Pow(tapStrain, .8f) * 3.5f;
                    if (aimEndurance >= aimThreshold)
                        ComputeEnduranceDecay(ref aimEndurance, (aimEndurance - aimThreshold) / endAimDivider);
                    if (tapEndurance >= tapThreshold)
                        ComputeEnduranceDecay(ref tapEndurance, (tapEndurance - tapThreshold) / endTapDivider);
                }

                if (float.IsNaN(aimStrain) || float.IsNaN(aimEndurance) || float.IsNaN(tapStrain) || float.IsNaN(tapEndurance))
                {
                    Trace.WriteLine("Something fucked up");
                    break;
                }


                aimPerfDict[speedIndex].Add(new DataVector(currentNote.position, aimStrain, aimEndurance, weightSum + (aimStrain / 10f)));
                tapPerfDict[speedIndex].Add(new DataVector(currentNote.position, tapStrain, tapEndurance, weightSum + (tapStrain / 10f)));
                accPerfDict[speedIndex].Add(new DataVector(currentNote.position, 0, 0, 1));
            }
        }
        //public static bool IsSlider(float deltaTime) => !(MathF.Round(deltaTime, 3) > 0);

        //https://www.desmos.com/calculator/tkunxszosp
        //public static float ComputeStrain(float strain) => a * MathF.Pow(strain + 1, -.0325f * MathF.E) - a - (3f * strain) / a;
        public static float ComputeStrain(float strain) => a * MathF.Pow(strain + 1, b * MathF.E) - a - (MathF.Pow(strain, p) / a);
        private const float a = -35f;
        private const float b = -.5f;
        private const float p = 1.25f;

        public static float ComputeVelocityDebuff(float lastVelocity, float currentVelocity) => MathF.Min(MathF.Abs(currentVelocity - lastVelocity) * .03f + .65f, 1f);

        public static void ComputeEnduranceDecay(ref float endurance, float distanceFromLastNote)
        {
            endurance /= 1 + (.2f * distanceFromLastNote);
        }

        #region AIM
        public static float CalcAimStrain(float distance, float weight, float deltaTime)
        {
            var speed = MathF.Sqrt(distance + 50) * 1.25f / MathF.Pow(deltaTime, 1.38f);
            return speed * weight;
        }

        public static float CalcAimEndurance(float distance, float weight, float deltaTime)
        {
            var speed = MathF.Sqrt(distance + 25) * .65f / MathF.Pow(deltaTime, 1.08f) / (AIM_END * MUL_END);
            return speed * weight;
        }
        #endregion

        #region TAP
        public static float CalcTapStrain(float tapDelta, float weight, float aimDistance)
        {
            var baseValue = MathF.Min(Utils.Lerp(4.5f, 5f, aimDistance / CHEESABLE_THRESHOLD), 5.5f);
            return (baseValue / MathF.Pow(tapDelta / 1.15f, 1.38f)) * weight;
        }

        public static float CalcTapEndurance(float tapDelta, float weight, float aimDistance)
        {
            var baseValue = MathF.Min(Utils.Lerp(.08f, .16f, aimDistance / CHEESABLE_THRESHOLD), .2f);
            return (baseValue / MathF.Pow(tapDelta / 2.75f, 1.08f)) / (TAP_END * MUL_END) * weight;
        }
        #endregion

        #region ACC
        public static float CalcAccStrain(float lengthSum, float slideDelta, float weight)
        {
            var speed = slideDelta * 4f / MathF.Pow(lengthSum, 1.16f);
            return speed * weight;
        }

        public float CalcAccEndurance(float lengthSum, float slideDelta, float weight)
        {
            var speed = slideDelta * .25f / MathF.Pow(lengthSum, 1.08f) / (ACC_END * MUL_END);
            return speed * weight;
        }
        #endregion

        public void CalculateAnalytics(int gamespeed, float songLengthMult = 1)
        {
            aimAnalyticsDict[gamespeed] = new DataVectorAnalytics(aimPerfDict[gamespeed], songLengthMult);
            tapAnalyticsDict[gamespeed] = new DataVectorAnalytics(tapPerfDict[gamespeed], songLengthMult);
            accAnalyticsDict[gamespeed] = new DataVectorAnalytics(accPerfDict[gamespeed], songLengthMult);
        }


        public void CalculateRatings(int gamespeed)
        {
            var aimRating = aimRatingDict[gamespeed] = aimAnalyticsDict[gamespeed].perfWeightedAverage + 0.01f;
            var tapRating = tapRatingDict[gamespeed] = tapAnalyticsDict[gamespeed].perfWeightedAverage + 0.01f;

            if (aimRating != 0 && tapRating != 0)
            {
                var totalRating = aimRating + tapRating;
                var aimPerc = aimRating / totalRating;
                var tapPerc = tapRating / totalRating;
                var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
                var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
                var totalWeight = aimWeight + tapWeight;
                starRatingDict[gamespeed] = ((aimRating * aimWeight) + (tapRating * tapWeight)) / totalWeight;
            }
            else
                starRatingDict[gamespeed] = 0f;
        }

        public float GetDynamicAimRating(int hitCount, float speed) => GetDynamicSkillRating(hitCount, speed, aimPerfDict);
        public float GetDynamicTapRating(int hitCount, float speed) => GetDynamicSkillRating(hitCount, speed, tapPerfDict);
        public float GetDynamicAccRating(int hitCount, float speed) => GetDynamicSkillRating(hitCount, speed, accPerfDict);

        private float GetDynamicSkillRating(int hitCount, float speed, List<DataVector>[] skillRatingMatrix)
        {
            var index = (int)((speed - 0.5f) / .25f);

            if (skillRatingMatrix[index].Count <= 1 || hitCount <= 0)
                return 0;
            else if (speed % .25f == 0)
                return CalcSkillRating(hitCount, skillRatingMatrix[index]);

            var r1 = CalcSkillRating(hitCount, skillRatingMatrix[index]);
            var r2 = CalcSkillRating(hitCount, skillRatingMatrix[index + 1]);

            var minSpeed = Utils.GAME_SPEED[index];
            var maxSpeed = Utils.GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Utils.Lerp(r1, r2, by);
        }

        public const float MAP = .05f;
        public const float MACC = .5f;

        private float CalcSkillRating(int hitCount, List<DataVector> skillRatingArray)
        {
            int maxRange;

            float percent = 1f;
            if (hitCount < _chart.noteCount)
                percent = MathF.Min((float)hitCount * 1.1f / _chart.noteCount, 1f);

            if (percent <= MACC)
                maxRange = (int)Math.Clamp(skillRatingArray.Count * (percent * (MAP / MACC)), 1, skillRatingArray.Count);
            else
                maxRange = (int)Math.Clamp(skillRatingArray.Count * ((percent - MACC) * ((1f - MAP) / (1f - MACC)) + MAP), 1, skillRatingArray.Count);

            var array = skillRatingArray.OrderBy(x => x.performance + x.endurance).ToList().GetRange(0, maxRange);
            var analytics = new DataVectorAnalytics(array, _chart.songLengthMult);
            return analytics.perfWeightedAverage + .01f;
        }

        public const float AIM_WEIGHT = 1.25f;
        public const float TAP_WEIGHT = 1f;

        public static readonly float[] HDWeights = { .11f, .09f };
        public static readonly float[] FLWeights = { .3f, .10f };
        public static readonly float[] EZWeights = { -.32f, -.32f };
        public const float BIAS = .75f;

        public float GetDynamicDiffRating(int hitCount, float gamespeed, string[] modifiers = null)
        {
            var aimRating = GetDynamicAimRating(hitCount, gamespeed);
            var tapRating = GetDynamicTapRating(hitCount, gamespeed);

            if (aimRating == 0 && tapRating == 0) return 0f;

            if (modifiers != null)
            {
                var aimPow = 1f;
                var tapPow = 1f;
                var isEZModeOn = modifiers.Contains("EZ");
                var mult = isEZModeOn ? .4f : 1f;
                if (modifiers.Contains("HD"))
                {
                    aimPow += HDWeights[0] * mult;
                    tapPow += HDWeights[1] * mult;
                }
                if (modifiers.Contains("FL"))
                {
                    aimPow += FLWeights[0] * mult;
                    tapPow += FLWeights[1] * mult;
                }
                if (isEZModeOn)
                {
                    aimPow += EZWeights[0];
                    tapPow += EZWeights[1];
                }

                if (aimPow <= 0) aimPow = .01f;
                if (tapPow <= 0) tapPow = .01f;

                aimRating *= aimPow;
                tapRating *= tapPow;
            }
            var totalRating = aimRating + tapRating;
            var aimPerc = aimRating / totalRating;
            var tapPerc = tapRating / totalRating;
            var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
            var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
            var totalWeight = aimWeight + tapWeight;

            return ((aimRating * aimWeight) + (tapRating * tapWeight)) / totalWeight;
        }

        public void Dispose()
        {
            aimPerfDict = null;
            aimAnalyticsDict = null;
            aimRatingDict = null;
            tapPerfDict = null;
            tapAnalyticsDict = null;
            tapRatingDict = null;
            starRatingDict = null;
        }

        public float GetDiffRating(float speed)
        {
            var index = (int)((speed - 0.5f) / .25f);
            if (speed % .25f == 0)
                return starRatingDict[index];

            var minSpeed = Utils.GAME_SPEED[index];
            var maxSpeed = Utils.GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Utils.Lerp(starRatingDict[index], starRatingDict[index + 1], by);
        }

        public struct DataVector
        {
            public float performance;
            public float endurance;
            public float time;
            public float weight;

            public DataVector(float time, float performance, float endurance, float weight)
            {
                this.time = time;
                this.endurance = endurance;
                this.performance = performance;
                this.weight = weight;
            }
        }

        public struct DataVectorAnalytics
        {
            public float perfMax, perfSum, perfWeightedAverage;
            public float weightSum;

            public DataVectorAnalytics(List<DataVector> dataVectorList, float songLengthMult)
            {
                perfMax = perfSum = perfWeightedAverage = 0;
                weightSum = 1;

                if (dataVectorList.Count <= 0) return;

                CalculateWeightSum(dataVectorList, songLengthMult);
                CalculateData(dataVectorList);
            }

            public void CalculateWeightSum(List<DataVector> dataVectorList, float songLengthMult)
            {
                for (int i = 0; i < dataVectorList.Count; i++)
                    weightSum += dataVectorList[i].weight;
                weightSum *= songLengthMult;
            }

            public void CalculateData(List<DataVector> dataVectorList)
            {
                for (int i = 0; i < dataVectorList.Count; i++)
                {
                    if (dataVectorList[i].performance > perfMax)
                        perfMax = dataVectorList[i].performance;

                    perfSum += (dataVectorList[i].performance + dataVectorList[i].endurance) * (dataVectorList[i].weight / weightSum);
                }
                perfWeightedAverage = perfSum;
            }
        }
        public static float BeatToSeconds2(float beat, float bpm) => 60f / bpm * beat;

    }

}