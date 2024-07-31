﻿using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TootTallyDiffCalcTTV2
{
    public class ChartPerformances
    {
        public static readonly float[] weights = {
             1.0000f, 0.9200f, 0.8464f, 0.7787f, 0.7164f, 0.6591f, 0.6064f, 0.5578f,
            0.5132f, 0.4722f, 0.4344f, 0.3996f, 0.3677f, 0.3383f, 0.3112f, 0.2863f,
            0.2634f, 0.2423f, 0.2229f, 0.2051f, 0.1887f, 0.1736f, 0.1597f, 0.1469f,
            0.1352f, 0.1244f, 0.1144f, 0.1053f, 0.0968f, 0.0891f, 0.0820f, 0.0754f,
            0.0694f, 0.0638f, 0.0587f, 0.0540f, 0.0497f, 0.0457f, 0.0421f, 0.0387f,
            0.0356f, 0.0328f, 0.0301f, 0.0277f, 0.0255f, 0.0235f, 0.0216f, 0.0199f,
            0.0183f, 0.0168f, 0.0155f, 0.0142f, 0.0131f, 0.0120f, 0.0111f, 0.0102f,
            0.0094f, 0.0086f, 0.0079f, 0.0073f, 0.0067f, 0.0062f, 0.0057f, 0.0052f,
            0.0048f // :)
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

        public const float AIM_DIV = 375;
        public const float TAP_DIV = 200;
        public const float ACC_DIV = 375;
        public const float AIM_END = 750;
        public const float TAP_END = 15;
        public const float ACC_END = 900;
        public const float MUL_END = 50;
        public const float MAX_DIST = 8f;

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
                for (int j = i - 1; j >= 0 && noteCount < 64 && (MathF.Abs(currentNote.position - noteList[j].position) <= MAX_DIST || i - j <= 2); j--)
                {
                    //if (noteCount <= 10) continue;

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
                        aimStrain += ComputeStrain(CalcAccStrain(lengthSum, deltaSlideSum, weight)) / ACC_DIV;
                        aimEndurance += CalcAccEndurance(lengthSum, deltaSlideSum, weight);
                    }

                    //Aim Calc
                    var aimDistance = MathF.Abs(nextNote.pitchStart - prevNote.pitchEnd);
                    var noteMoved = aimDistance != 0 || deltaSlideSum != 0;

                    if (noteMoved)
                    {
                        aimStrain += ComputeStrain(CalcAimStrain(aimDistance, weight, deltaTime)) / AIM_DIV;
                        aimEndurance += CalcAimEndurance(aimDistance, weight, deltaTime);
                    }

                    //Tap Calc
                    var tapDelta = nextNote.position - prevNote.position;
                    tapStrain += ComputeStrain(CalcTapStrain(tapDelta, weight, aimDistance)) / TAP_DIV;
                    tapEndurance += CalcTapEndurance(tapDelta, weight, aimDistance);
                }

                if (i > 0)
                {
                    var endDivider = 61f - MathF.Min(currentNote.position - noteList[i - 1].position, 5f) * 12f;
                    var aimThreshold = MathF.Pow(aimStrain, 1.08f) * 1.2f;
                    var tapThreshold = MathF.Pow(tapStrain, 1.08f) * 1.2f;
                    if (aimEndurance >= aimThreshold)
                        ComputeEnduranceDecay(ref aimEndurance, (aimEndurance - aimThreshold) / endDivider);
                    if (tapEndurance >= tapThreshold)
                        ComputeEnduranceDecay(ref tapEndurance, (tapEndurance - tapThreshold) / endDivider);
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
        public static float ComputeStrain(float strain) => a * MathF.Pow(strain + 1, -.016f * MathF.E) - a - (5f * strain) / a;
        private const float a = -40f;

        public static void ComputeEnduranceDecay(ref float endurance, float distanceFromLastNote)
        {
            endurance /= 1 + (.2f * distanceFromLastNote);
        }

        #region AIM
        public static float CalcAimStrain(float distance, float weight, float deltaTime)
        {
            var speed = (distance * .85f) / MathF.Pow(deltaTime, 1.35f);
            return speed * weight;
        }

        public static float CalcAimEndurance(float distance, float weight, float deltaTime)
        {
            var speed = ((distance * .25f) / MathF.Pow(deltaTime, 1.15f)) / (AIM_END * MUL_END);
            return speed * weight;
        }
        #endregion

        #region TAP
        public static float CalcTapStrain(float tapDelta, float weight, float aimDistance)
        {
            var baseValue = MathF.Min(Utils.Lerp(8f, 16f, aimDistance / (CHEESABLE_THRESHOLD * 3f)), 20f);
            return (baseValue / MathF.Pow(tapDelta, 1.35f)) * weight;
        }

        public static float CalcTapEndurance(float tapDelta, float weight, float aimDistance)
        {
            var baseValue = MathF.Min(Utils.Lerp(.15f, .35f, aimDistance / (CHEESABLE_THRESHOLD * 3f)), .5f);
            return (baseValue / MathF.Pow(tapDelta, 1.3f)) / (TAP_END * MUL_END) * weight;
        }
        #endregion

        #region ACC
        public static float CalcAccStrain(float lengthSum, float slideDelta, float weight)
        {
            var speed = slideDelta / MathF.Pow(lengthSum, 1.18f);
            return speed * weight;
        }

        public float CalcAccEndurance(float lengthSum, float slideDelta, float weight)
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
                maxRange = (int)Math.Clamp(skillRatingArray.Count * ((percent - MACC) * ((1f - MAP) / (1f - MACC)) + MAP), 1, skillRatingArray.Count);

            var array = skillRatingArray.OrderBy(x => x.performance + x.endurance).ToList().GetRange(0, maxRange);
            var analytics = new DataVectorAnalytics(array, _chart.songLengthMult);
            return analytics.perfWeightedAverage + .01f;
        }

        public const float AIM_WEIGHT = 1.25f;
        public const float TAP_WEIGHT = 1.12f;

        public static readonly float[] HDWeights = { .34f, .02f };
        public static readonly float[] FLWeights = { .55f, .02f };
        public const float BIAS = .75f;

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