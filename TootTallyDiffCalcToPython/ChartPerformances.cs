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
                aimPerfDict[i] = new DataVector[length - 1];
                tapPerfDict[i] = new DataVector[length - 1];
                accPerfDict[i] = new DataVector[length - 1];
                for (int j = 0; j < length - 1; j++)
                {
                    aimPerfDict[i][j] = new DataVector();
                    tapPerfDict[i][j] = new DataVector();
                    accPerfDict[i][j] = new DataVector();
                }
            }
            _chart = chart;
        }

        public const float ENDURANCE_DECAY = 1.004f;
        public const float AIM_DIV = 33;
        public const float TAP_DIV = 22;
        public const float ACC_DIV = 25;
        public const float AIM_END = 1500;
        public const float TAP_END = 50;
        public const float ACC_END = 6000;

        public void CalculatePerformances(int speedIndex)
        {
            var noteList = _chart.notesDict[speedIndex];
            var aimEndurance = 0f;
            var tapEndurance = 0f;
            var accEndurance = 0f;

            for (int i = 0; i < noteList.Count - 1; i++) //Main Forward Loop
            {
                var currentNote = noteList[i];
                var lengthSum = currentNote.length;
                var aimStrain = 0f;
                var tapStrain = 0f;
                var accStrain = 0f;

                ComputeEnduranceDecay(ref aimEndurance);
                ComputeEnduranceDecay(ref tapEndurance);
                ComputeEnduranceDecay(ref accEndurance);

                for (int j = i - 1; j > 0 && j > i - 10 && MathF.Abs(currentNote.position - noteList[j].position) <= 4.5f; j--)
                {
                    var prevNote = noteList[j];
                    var nextNote = noteList[j + 1];
                    var weight = weights[i - j - 1];
                    lengthSum += MathF.Sqrt(prevNote.length) * weight;
                    var deltaTime = nextNote.position - (prevNote.position + prevNote.length);
                    if (!IsSlider(deltaTime))
                    {
                        deltaTime += prevNote.length * .4f;
                        var aimDistance = MathF.Abs(nextNote.pitchStart - prevNote.pitchEnd);
                        var noteMoved = aimDistance != 0;
                        if (noteMoved)
                        {
                            //Aim Calc
                            aimStrain += MathF.Sqrt(CalcAimStrain(prevNote, aimDistance, weight, deltaTime)) / AIM_DIV;
                            aimEndurance += CalcAimEndurance(weight, aimDistance, deltaTime);
                        }

                        var tapDelta = nextNote.position - prevNote.position;
                        //Tap Calc
                        tapStrain += MathF.Sqrt(CalcTapStrain(tapDelta, weight)) / TAP_DIV;
                        tapEndurance += CalcTapEndurance(tapDelta, weight);
                    }

                    if (prevNote.pitchDelta != 0)
                    {
                        //Acc Calc
                        var slideDelta = MathF.Pow(MathF.Abs(prevNote.pitchDelta), 1.25f);
                        accStrain += MathF.Sqrt(CalcAccStrain(prevNote, slideDelta, weight)) / ACC_DIV;
                        accEndurance += CalcAccEndurance(prevNote, slideDelta, weight);
                    }
                }

                aimPerfDict[speedIndex][i].SetValues(currentNote.position, aimStrain + CalcNerfedEndurance(aimEndurance), lengthSum);
                tapPerfDict[speedIndex][i].SetValues(currentNote.position, tapStrain + CalcNerfedEndurance(tapEndurance), lengthSum);
                accPerfDict[speedIndex][i].SetValues(currentNote.position, accStrain + CalcNerfedEndurance(accEndurance), lengthSum);
            }
        }
        public static bool IsSlider(float deltaTime) => !(MathF.Round(deltaTime, 3) > 0);

        public static void ComputeEnduranceDecay(ref float endurance)
        {
            if (endurance > 1f)
                endurance /= ENDURANCE_DECAY;
        }

        public static float CalcNerfedEndurance(float endurance)
        {
            return endurance > 1 ? MathF.Sqrt(endurance) : endurance;
        }

        public static float CalcAimStrain(Note prevNote, float distance, float weight, float deltaTime)
        {
            //Calc the space between two notes if they aren't connected sliders
            distance = MathF.Pow(distance, .9f);
            if (prevNote.pitchDelta != 0)
                distance *= .5f;

            if (deltaTime > 1)
                return distance / MathF.Sqrt(deltaTime) * weight;
            else
                return distance / MathF.Pow(deltaTime, 1.35f) * weight;
        }

        public static float CalcTapStrain(float tapDelta, float weight)
        {
            if (tapDelta > 1)
                return 1.5f / tapDelta * weight;
            else
                return 1.5f / MathF.Pow(tapDelta, 1.75f) * weight;
        }


        public static float CalcAccStrain(Note prevNote, float slideDelta, float weight)
        {
            if (prevNote.pitchDelta <= 34.375f)
                slideDelta *= .25f;

            if (prevNote.length > 1)
                return slideDelta / MathF.Sqrt(prevNote.length) * weight;
            else
                return slideDelta / MathF.Pow(prevNote.length, 1.35f) * weight;

        }

        public static float CalcAimEndurance(float weight, float distance, float deltaTime)
        {
            float endurance = distance / (deltaTime * 3f) / AIM_END;
            return endurance * weight;
        }


        public static float CalcTapEndurance(float tapDelta, float weight)
        {
            float endurance = 0.45f / MathF.Pow(tapDelta, 1.1f);
            return endurance / TAP_END * weight;
        }

        public static float CalcAccEndurance(Note prevNote, float slideDelta, float weight)
        {
            var endurance = slideDelta / (prevNote.length * 2f) / ACC_END;
            return endurance * weight;
        }

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
            var accRating = accRatingDict[gamespeed] = accAnalyticsDict[gamespeed].perfWeightedAverage + 0.01f;

            if (aimRating != 0 && tapRating != 0 && accRating != 0)
            {
                var totalRating = aimRating + tapRating + accRating;
                var aimPerc = aimRating / totalRating;
                var tapPerc = tapRating / totalRating;
                var accPerc = accRating / totalRating;
                var aimWeight = (aimPerc + BIAS) * 1.2f;
                var tapWeight = (tapPerc + BIAS) * 1.15f;
                var accWeight = (accPerc + BIAS) * 1.1f;
                var totalWeight = aimWeight + tapWeight + accWeight;
                starRatingDict[gamespeed] = ((aimRating * aimWeight) + (tapRating * tapWeight) + (accRating * accWeight)) / totalWeight;
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

        public const float MAP = 1f;
        public const float MACC = 1f;

        private float CalcSkillRating(float percent, DataVector[] skillRatingArray)
        {
            int maxRange = (int)Math.Clamp(skillRatingArray.Length * percent, 1, skillRatingArray.Length);
            /*if (percent < .60)
                maxRange = (int)Math.Clamp(skillRatingArray.Length * (percent * (.25f / .60f)), 1, skillRatingArray.Length);
            else
                maxRange = (int)Math.Clamp(skillRatingArray.Length * ((percent - .60f) * (.75f/.40f) + .25f), 1, skillRatingArray.Length);*/

            DataVector[] array = skillRatingArray.OrderBy(x => x.performance).ToList().GetRange(0, maxRange).ToArray();
            var analytics = new DataVectorAnalytics(array);
            return analytics.perfWeightedAverage + .01f;
        }

        public const float AIM_WEIGHT = 1.2f;
        public const float TAP_WEIGHT = 1.15f;
        public const float ACC_WEIGHT = 1.1f;

        public static readonly float[] HDWeights = { .28f, .08f, .32f };
        public static readonly float[] FLWeights = { .35f, .12f, .08f };

        public float GetDynamicDiffRating(float percent, float gamespeed, string[] modifiers = null)
        {
            var aimRating = GetDynamicAimRating(percent, gamespeed);
            var tapRating = GetDynamicTapRating(percent, gamespeed);
            var accRating = GetDynamicAccRating(percent, gamespeed);

            if (aimRating == 0 && tapRating == 0 && accRating == 0) return 0f;

            if (modifiers != null)
            {
                var aimPow = 1f;
                var tapPow = 1f;
                var accPow = 1f;
                if (modifiers.Contains("HD"))
                {
                    aimPow += HDWeights[0];
                    tapPow += HDWeights[1];
                    accPow += HDWeights[2];
                }
                if (modifiers.Contains("FL"))
                {
                    aimPow += FLWeights[0];
                    tapPow += FLWeights[1];
                    accPow += FLWeights[2];
                }

                aimRating = MathF.Pow(aimRating, aimPow);
                tapRating = MathF.Pow(tapRating, tapPow);
                accRating = MathF.Pow(accRating, accPow);
            }
            var totalRating = aimRating + tapRating + accRating;
            var aimPerc = aimRating / totalRating;
            var tapPerc = tapRating / totalRating;
            var accPerc = accRating / totalRating;
            var aimWeight = (aimPerc + BIAS) * AIM_WEIGHT;
            var tapWeight = (tapPerc + BIAS) * TAP_WEIGHT;
            var accWeight = (accPerc + BIAS) * ACC_WEIGHT;
            var totalWeight = aimWeight + tapWeight + accWeight;

            return ((aimRating * aimWeight) + (tapRating * tapWeight) + (accRating * accWeight)) / totalWeight;
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
            public float time;
            public float weight;

            public void SetValues(float time, float performance, float weight)
            {
                this.time = time;
                this.performance = performance;
                this.weight = weight;
            }
        }

        public class DataVectorAnalytics
        {
            public float perfMax, perfSum, perfWeightedAverage;

            public DataVectorAnalytics(DataVector[] dataVectorList)
            {
                perfSum = 0f;
                var weightSum = 0f;
                for (int i = 0; i < dataVectorList.Length; i++)
                {
                    if (dataVectorList[i] == null)
                        continue;

                    if (dataVectorList[i].performance > perfMax)
                        perfMax = dataVectorList[i].performance;

                    perfSum += dataVectorList[i].performance * dataVectorList[i].weight;
                    weightSum += dataVectorList[i].weight;
                }
                if (weightSum == 0f)
                    perfWeightedAverage = 0;
                else
                    perfWeightedAverage = perfSum / weightSum;
            }
        }
        public static float BeatToSeconds2(float beat, float bpm) => 60f / bpm * beat;

    }

}
