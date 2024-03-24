using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TootTallyDiffCalcTTV2
{
    public class ChartPerformances
    {
        public static readonly float[] weights = {1f, 0.85f, 0.7225f, 0.6141f, 0.5220f, 0.4437f, 0.3771f, 0.3205f, 0.2724f, 0.2316f,
                                                    0.1968f, 0.1673f, 0.1422f, 0.1209f, 0.1027f, 0.0873f, 0.0742f, 0.0631f, 0.0536f, 0.0455f,
                                                    0.0387f, 0.0329f, 0.0280f, 0.0238f, 0.0202f, 0.0171f,}; //lol

        public DataVector[][] aimPerfDict;
        public DataVectorAnalytics[] aimAnalyticsDict;

        public DataVector[][] tapPerfDict;
        public DataVectorAnalytics[] tapAnalyticsDict;

        public DataVector[][] accPerfDict;
        public DataVectorAnalytics[] accAnalyticsDict;

        public float[] aimRatingDict;
        public float[] tapRatingDict;
        public float[] accRatingDict;
        public float[] starRatingDict;

        private Chart _chart;
        private readonly int NOTE_COUNT;

        public ChartPerformances(Chart chart)
        {
            aimPerfDict = new DataVector[7][];
            tapPerfDict = new DataVector[7][];
            accPerfDict = new DataVector[7][];
            aimRatingDict = new float[7];
            tapRatingDict = new float[7];
            accRatingDict = new float[7];
            starRatingDict = new float[7];
            aimAnalyticsDict = new DataVectorAnalytics[7];
            tapAnalyticsDict = new DataVectorAnalytics[7];
            accAnalyticsDict = new DataVectorAnalytics[7];

            var length = chart.notes.Length;
            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                aimPerfDict[i] = new DataVector[length];
                tapPerfDict[i] = new DataVector[length];
                accPerfDict[i] = new DataVector[length];
                for (int j = 0; j < length - 1; j++)
                {
                    aimPerfDict[i][j] = new DataVector();
                    tapPerfDict[i][j] = new DataVector();
                    accPerfDict[i][j] = new DataVector();
                }
            }
            _chart = chart;
            NOTE_COUNT = _chart.notesDict[0].Count;
        }

        public const float AIM_DIV = 275;
        public const float TAP_DIV = 100;
        public const float ACC_DIV = 325;
        public const float AIM_END = 300;
        public const float TAP_END = 25;
        public const float ACC_END = 450;
        public const float MUL_END = 100;
        public const float MAX_DIST = 4;

        public void CalculatePerformances(int speedIndex)
        {
            var noteList = _chart.notesDict[speedIndex];
            var aimEndurance = 0f;
            var tapEndurance = 0f;
            for (int i = 0; i < NOTE_COUNT - 1; i++) //Main Forward Loop
            {
                var currentNote = noteList[i];
                int noteCount = 0;
                float weightSum = 0f;
                var aimStrain = 0f;
                var tapStrain = 0f;
                for (int j = i - 1; j > 0 && MathF.Abs(currentNote.position - noteList[j].position) <= 4f; j--)
                {
                    var prevNote = noteList[j];
                    var nextNote = noteList[j + 1];
                    var weight = Utils.FastPow(.85f, noteCount);
                    noteCount++;
                    if (weight < 0.0001f) continue;

                    weightSum += weight;

                    var deltaTime = nextNote.position - (prevNote.position + prevNote.length);
                    var lengthSum = prevNote.length;
                    var deltaSlide = MathF.Abs(prevNote.pitchDelta);

                    while (IsSlider(deltaTime))
                    {
                        if (j-- <= 0)
                            break;
                        prevNote = noteList[j];
                        nextNote = noteList[j + 1];
                        deltaTime = nextNote.position - (prevNote.position + prevNote.length);

                        lengthSum += deltaTime == 0 ? MathF.Sqrt(prevNote.length) : prevNote.length;
                        deltaSlide += MathF.Abs(prevNote.pitchDelta);
                    }

                    if (deltaSlide != 0)
                    {
                        //Acc Calc
                        aimStrain += ComputeStrain(CalcAccStrain(lengthSum, deltaSlide, weight)) / ACC_DIV;
                        aimEndurance += CalcAccEndurance(lengthSum, deltaSlide, weight);
                    }

                    //Aim Calc
                    deltaTime += lengthSum * .4f;
                    var aimDistance = MathF.Abs(nextNote.pitchStart - prevNote.pitchEnd);
                    var noteMoved = aimDistance != 0 || deltaSlide != 0;

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
                    var aimThreshold = MathF.Pow(aimStrain, 1.6f) * 5f;
                    var tapThreshold = MathF.Pow(tapStrain, 1.6f) * 5f;
                    if (aimEndurance >= aimThreshold)
                        ComputeEnduranceDecay(ref aimEndurance, (aimEndurance - aimThreshold) / 50f);
                    if (tapEndurance >= tapThreshold)
                        ComputeEnduranceDecay(ref tapEndurance, (tapEndurance - tapThreshold) / 50f);
                }

                aimPerfDict[speedIndex][i].SetValues(currentNote.position, aimStrain, aimEndurance, noteCount + weightSum);
                tapPerfDict[speedIndex][i].SetValues(currentNote.position, tapStrain, tapEndurance, noteCount + weightSum);
            }
        }
        public static bool IsSlider(float deltaTime) => !(MathF.Round(deltaTime, 3) > 0);

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
            var speed = distance / MathF.Pow(deltaTime, 1.25f);
            return speed * weight;
        }

        public static float CalcAimEndurance(float distance, float weight, float deltaTime)
        {
            var speed = (distance / MathF.Pow(deltaTime, 1.02f)) / (AIM_END * MUL_END);
            return speed * weight;
        }
        #endregion

        #region TAP
        public static float CalcTapStrain(float tapDelta, float weight)
        {
            return (11f / MathF.Pow(tapDelta, 1.25f)) * weight;
        }

        public static float CalcTapEndurance(float tapDelta, float weight)
        {
            return (1.1f / MathF.Pow(tapDelta, 1.02f)) / (TAP_END * MUL_END) * weight;
        }
        #endregion

        #region ACC
        public static float CalcAccStrain(float lengthSum, float slideDelta, float weight)
        {
            var speed = slideDelta / MathF.Pow(lengthSum, 1.25f);
            return speed * weight;
        }

        public static float CalcAccEndurance(float lengthSum, float slideDelta, float weight)
        {
            var speed = (slideDelta / MathF.Pow(lengthSum, 1.02f)) / (ACC_END * MUL_END);
            return speed * weight;
        }
        #endregion

        public void CalculateAnalytics(int gamespeed)
        {
            aimAnalyticsDict[gamespeed] = new DataVectorAnalytics(aimPerfDict[gamespeed]);
            tapAnalyticsDict[gamespeed] = new DataVectorAnalytics(tapPerfDict[gamespeed]);
            accAnalyticsDict[gamespeed] = new DataVectorAnalytics(accPerfDict[gamespeed]);
        }

        public const float BIAS = .75f;

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

        private float GetDynamicSkillRating(float percent, float speed, DataVector[][] skillRatingMatrix)
        {
            var index = (int)((speed - 0.5f) / .25f);

            if (skillRatingMatrix[index].Length <= 1 || percent <= 0)
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

        public const float MAP = .01f;
        public const float MACC = .3f;

        private float CalcSkillRating(float percent, DataVector[] skillRatingArray)
        {
            int maxRange;
            if (percent <= MACC)
                maxRange = (int)Math.Clamp(skillRatingArray.Length * (percent * (MAP / MACC)), 1, skillRatingArray.Length);
            else
                maxRange = (int)Math.Clamp(skillRatingArray.Length * ((percent - MACC) * ((1f-MAP)/(1f-MACC)) + MAP), 1, skillRatingArray.Length);

            DataVector[] array = skillRatingArray.OrderBy(x => x.performance).ToList().GetRange(0, maxRange).ToArray();
            var analytics = new DataVectorAnalytics(array);
            return analytics.perfWeightedAverage + .01f;
        }

        public const float AIM_WEIGHT = 1.2f;
        public const float TAP_WEIGHT = 1.15f;

        public static readonly float[] HDWeights = { .12f, .09f };
        public static readonly float[] FLWeights = { .18f, .05f };

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

                aimRating = MathF.Pow(aimRating + 1f, aimPow) - 1f;
                tapRating = MathF.Pow(tapRating + 1f, tapPow) - 1f;
            }
            var totalRating = aimRating + tapRating;
            var aimPerc = aimRating / totalRating;
            var tapPerc = tapRating / totalRating;
            var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
            var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
            var totalWeight = aimWeight + tapWeight;

            return ((aimRating * aimWeight) + (tapRating * tapWeight)) / totalWeight;
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

        public class DataVector
        {
            public float performance;
            public float endurance;
            public float time;
            public float weight;

            public void SetValues(float time, float performance, float endurance, float weight)
            {
                this.time = time;
                this.endurance = endurance;
                this.performance = performance;
                this.weight = weight;
            }
        }

        public class DataVectorAnalytics
        {
            public float perfMax = 0f, perfSum = 0f, perfWeightedAverage = 0f;
            public float weightSum = 0f;

            public DataVectorAnalytics(DataVector[] dataVectorList)
            {
                if (dataVectorList.Length <= 0) return;

                CalculateWeightSum(dataVectorList);
                CalculateData(dataVectorList);
            }

            public void CalculateWeightSum(DataVector[] dataVectorList)
            {
                for(int i = 0; i < dataVectorList.Length; i++)
                {
                    if (dataVectorList[i] == null) continue;

                    weightSum += dataVectorList[i].weight;
                }
            }

            public void CalculateData(DataVector[] dataVectorList)
            {
                for (int i = 0; i < dataVectorList.Length; i++)
                {
                    if (dataVectorList[i] == null)
                        continue;

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
