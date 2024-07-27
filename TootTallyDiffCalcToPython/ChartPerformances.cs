﻿using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TootTallyDiffCalcTTV2
{
    public class ChartPerformances
    {
        public static readonly float[] weights = {
             1.0000f, 0.8500f, 0.7225f, 0.6141f, 0.5220f, 0.4437f, 0.3771f, 0.3205f,
            0.2724f, 0.2316f, 0.1969f, 0.1674f, 0.1423f, 0.1210f, 0.1029f, 0.0874f,
            0.0743f, 0.0632f, 0.0538f, 0.0457f, 0.0389f, 0.0331f, 0.0281f, 0.0240f,
            0.0204f, 0.0174f, 0.0148f, 0.0126f, 0.0107f, 0.0091f, 0.0078f, 0.0066f,
            0.0056f, 0.0048f, 0.0041f, 0.0035f, 0.0029f, 0.0025f, 0.0021f, 0.0018f,
            0.0015f, 0.0013f, 0.0011f, 0.0009f // :)
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

        public const float AIM_DIV = 175;
        public const float TAP_DIV = 195;
        public const float ACC_DIV = 195;
        public const float AIM_END = 750;
        public const float TAP_END = 15;
        public const float ACC_END = 900;
        public const float MUL_END = 50;
        public const float MAX_DIST = 4f;

        public void CalculatePerformances(int speedIndex)
        {
            var noteList = _chart.notesDict[speedIndex];
            var aimEndurance = 0f;
            var tapEndurance = 0f;
            for (int i = 0; i < NOTE_COUNT; i++) //Main Forward Loop
            {
                var currentNote = noteList[i];
                int noteCount = 0;
                float weightSum = 0f;
                var aimStrain = 0f;
                var tapStrain = 0f;
                for (int j = i - 1; j >= 0 && noteCount < 42 && (MathF.Abs(currentNote.position - noteList[j].position) <= MAX_DIST || i - j <= 2); j--)
                {
                    //if (noteCount <= 10) continue;

                    var prevNote = noteList[j];
                    var nextNote = noteList[j + 1];
                    if (prevNote.position >= nextNote.position) break;

                    var weight = weights[noteCount];
                    noteCount++;
                    weightSum += weight;

                    var deltaTime = nextNote.position - (prevNote.position + prevNote.length);

                    var lengthSum = prevNote.length;
                    var deltaSlideSum = MathF.Abs(prevNote.pitchDelta);
                    if (deltaSlideSum <= CHEESABLE_THRESHOLD)
                        deltaSlideSum *= .75f;
                    var sliderCount = 0;
                    while (prevNote.isSlider)
                    {
                        if (j-- <= 0)
                            break;
                        prevNote = noteList[j];
                        nextNote = noteList[j + 1];
                        deltaTime = nextNote.position - (prevNote.position + prevNote.length);

                        var deltaSlide = MathF.Abs(prevNote.pitchDelta);
                        if (deltaSlide == 0)
                            lengthSum += MathF.Sqrt(prevNote.length);
                        else
                        {
                            lengthSum += prevNote.length;
                            if (deltaSlideSum <= CHEESABLE_THRESHOLD)
                                deltaSlide *= .65f;
                            else
                                sliderCount++;
                            deltaSlideSum += deltaSlide * MathF.Sqrt(sliderCount);
                        }

                    }

                    if (deltaSlideSum != 0)
                    {
                        //Acc Calc
                        aimStrain += ComputeStrain(CalcAccStrain(lengthSum, deltaSlideSum, weight)) / ACC_DIV;
                        aimEndurance += CalcAccEndurance(lengthSum, deltaSlideSum, weight);
                    }

                    //Aim Calc
                    deltaTime += lengthSum * .4f;
                    var aimDistance = MathF.Abs(nextNote.pitchStart - prevNote.pitchEnd);
                    var noteMoved = aimDistance != 0 || deltaSlideSum != 0;

                    if (noteMoved)
                    {
                        aimStrain += ComputeStrain(CalcAimStrain(aimDistance, weight, deltaTime)) / AIM_DIV;
                        aimEndurance += CalcAimEndurance(aimDistance, weight, deltaTime);
                    }

                    //Tap Calc
                    var tapDelta = nextNote.position - prevNote.position;
                    tapStrain += ComputeStrain(CalcTapStrain(tapDelta, weight)) / TAP_DIV;
                    tapEndurance += CalcTapEndurance(tapDelta, weight);
                }

                if (i > 0)
                {
                    var aimThreshold = MathF.Pow(aimStrain, 1.4f) * 3f;
                    var tapThreshold = MathF.Pow(tapStrain, 1.8f) * 3f;
                    if (aimEndurance >= aimThreshold)
                        ComputeEnduranceDecay(ref aimEndurance, (aimEndurance - aimThreshold) / 60f);
                    if (tapEndurance >= tapThreshold)
                        ComputeEnduranceDecay(ref tapEndurance, (tapEndurance - tapThreshold) / 60f);
                }

                if (float.IsNaN(aimStrain) || float.IsNaN(aimEndurance) || float.IsNaN(tapStrain) || float.IsNaN(tapEndurance))
                {
                    Trace.WriteLine("Something fucked up");
                    break;
                }


                aimPerfDict[speedIndex].Add(new DataVector(currentNote.position, aimStrain, aimEndurance, weightSum));
                tapPerfDict[speedIndex].Add(new DataVector(currentNote.position, tapStrain, tapEndurance, weightSum));
                accPerfDict[speedIndex].Add(new DataVector(currentNote.position, 0, 0, 1));
            }
        }
        //public static bool IsSlider(float deltaTime) => !(MathF.Round(deltaTime, 3) > 0);

        //https://www.desmos.com/calculator/e4kskdn8mu
        public static float ComputeStrain(float strain) => a * MathF.Pow(strain + 1, -.012f * MathF.E) - a - (4f * strain) / a;
        private const float a = -50f;

        public static void ComputeEnduranceDecay(ref float endurance, float distanceFromLastNote)
        {
            endurance /= 1 + (.2f * distanceFromLastNote);
        }

        #region AIM
        public static float CalcAimStrain(float distance, float weight, float deltaTime)
        {
            var speed = MathF.Pow(distance, .89f) / MathF.Pow(deltaTime, 1.35f);
            return speed * weight;
        }

        public static float CalcAimEndurance(float distance, float weight, float deltaTime)
        {
            var speed = (MathF.Pow(distance, .85f) / MathF.Pow(deltaTime, 1.11f)) / (AIM_END * MUL_END);
            return speed * weight;
        }
        #endregion

        #region TAP
        public static float CalcTapStrain(float tapDelta, float weight)
        {
            return (16f / MathF.Pow(tapDelta, 1.22f)) * weight;
        }   

        public static float CalcTapEndurance(float tapDelta, float weight)
        {
            return (0.8f / MathF.Pow(tapDelta, 1.1f)) / (TAP_END * MUL_END) * weight;
        }
        #endregion

        #region ACC
        public static float CalcAccStrain(float lengthSum, float slideDelta, float weight)
        {
            var speed = slideDelta / MathF.Pow(lengthSum, 1.18f);
            return speed * weight;
        }

        public static float CalcAccEndurance(float lengthSum, float slideDelta, float weight)
        {
            var speed = (slideDelta / MathF.Pow(lengthSum, 1.08f)) / (ACC_END * MUL_END);
            return speed * weight;
        }
        #endregion

        public void CalculateAnalytics(int gamespeed, float songLengthMult = 1)
        {
            aimAnalyticsDict[gamespeed] = new DataVectorAnalytics(aimPerfDict[gamespeed], songLengthMult);
            tapAnalyticsDict[gamespeed] = new DataVectorAnalytics(tapPerfDict[gamespeed], songLengthMult);
            accAnalyticsDict[gamespeed] = new DataVectorAnalytics(accPerfDict[gamespeed], songLengthMult);
        }

        public const float BIAS = 1.25f;

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

        public float GetDynamicAimRating(float percent, float speed) => GetDynamicSkillRating(percent, speed, aimPerfDict);
        public float GetDynamicTapRating(float percent, float speed) => GetDynamicSkillRating(percent, speed, tapPerfDict);
        public float GetDynamicAccRating(float percent, float speed) => GetDynamicSkillRating(percent, speed, accPerfDict);

        private float GetDynamicSkillRating(float percent, float speed, List<DataVector>[] skillRatingMatrix)
        {
            var index = (int)((speed - 0.5f) / .25f);

            if (skillRatingMatrix[index].Count <= 1 || percent <= 0)
                return 0;
            else if (speed % .25f == 0)
                return CalcSkillRating(percent, skillRatingMatrix[index]);

            var r1 = CalcSkillRating(percent, skillRatingMatrix[index]);
            var r2 = CalcSkillRating(percent, skillRatingMatrix[index + 1]);

            var minSpeed = Utils.GAME_SPEED[index];
            var maxSpeed = Utils.GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Utils.Lerp(r1, r2, by);
        }

        public const float MAP = .05f;
        public const float MACC = .5f;

        private float CalcSkillRating(float percent, List<DataVector> skillRatingArray)
        {
            int maxRange;
            if (percent <= MACC)
                maxRange = (int)Math.Clamp(skillRatingArray.Count * (percent * (MAP / MACC)), 1, skillRatingArray.Count);
            else
                maxRange = (int)Math.Clamp(skillRatingArray.Count * ((percent - MACC) * ((1f-MAP)/(1f-MACC)) + MAP), 1, skillRatingArray.Count);

            var array = skillRatingArray.OrderBy(x => x.performance + x.endurance).ToList().GetRange(0, maxRange);
            var analytics = new DataVectorAnalytics(array, _chart.songLengthMult);
            return analytics.perfWeightedAverage + .01f;
        }

        public const float AIM_WEIGHT = 1.25f;
        public const float TAP_WEIGHT = 1.12f;

        public static readonly float[] HDWeights = { .35f, .03f };
        public static readonly float[] FLWeights = { .55f, .03f };

        public float GetDynamicDiffRating(float percent, float gamespeed, string[] modifiers = null)
        {
            var aimRating = GetDynamicAimRating(percent, gamespeed);
            var tapRating = GetDynamicTapRating(percent, gamespeed);

            if (aimRating == 0 && tapRating == 0) return 0f;

            if (modifiers != null)
            {
                var aimPow = 1f;
                var tapPow = 1f;
                if (modifiers.Contains("HD"))
                {
                    aimPow += HDWeights[0];
                    tapPow += HDWeights[1];
                }
                if (modifiers.Contains("FL"))
                {
                    aimPow += FLWeights[0];
                    tapPow += FLWeights[1];
                }

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
                for(int i = 0; i < dataVectorList.Count; i++)
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
