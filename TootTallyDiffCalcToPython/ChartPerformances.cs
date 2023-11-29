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

        public const float ENDURANCE_DECAY = 1.025f;

        public void CalculatePerformances(int speedIndex)
        {
            var noteList = _chart.notesDict[speedIndex];
            var aimEndurance = 0f;
            var tapEndurance = 0f;
            var accEndurance = 0f;

            for (int i = 0; i < noteList.Count - 1; i++) //Main Forward Loop
            {
                var lengthSum = 0f;
                for (int j = i; j > i - 10 && j > 0; j--)
                    lengthSum += noteList[j].length * weights[i - j];

                var aimStrain = 0f;
                var tapStrain = 0f;
                var accStrain = 0f;

                var increaseDecay = (noteList[i].position - noteList[0].position) * Utils.GAME_SPEED[speedIndex] < 30f;
                ComputeEnduranceDecay(ref aimEndurance, increaseDecay);
                ComputeEnduranceDecay(ref tapEndurance, increaseDecay);
                ComputeEnduranceDecay(ref accEndurance, increaseDecay);

                for (int j = i - 1; j > 0 && j > i - 10 && noteList[i].position - noteList[j].position <= 8; j--)
                {
                    var nextNote = noteList[j];
                    var previousNote = noteList[j + 1];
                    var MAX_TIME = previousNote.length * .66f;
                    var weight = weights[i - j - 1];

                    if (!IsSlider(previousNote, nextNote))
                    {
                        //Aim Calc
                        aimStrain += MathF.Sqrt(CalcAimStrain(previousNote, nextNote, weight, MAX_TIME)) / 26f;
                        aimEndurance += CalcAimEndurance(previousNote, nextNote, weight, MAX_TIME);

                        //Tap Calc
                        tapStrain += MathF.Sqrt(CalcTapStrain(previousNote, nextNote, weight)) / 13f;
                        tapEndurance += CalcTapEndurance(previousNote, nextNote, weight);
                    }
                    
                    if (previousNote.pitchDelta != 0)
                    {
                        //Acc Calc
                        accStrain += MathF.Sqrt(CalcAccStrain(previousNote, weight)) / 15f;
                        accEndurance += CalcAccEndurance(previousNote, weight);
                    }
                }

                aimPerfDict[speedIndex][i].SetValues(noteList[i].position, aimStrain + aimEndurance, lengthSum);
                tapPerfDict[speedIndex][i].SetValues(noteList[i].position, tapStrain + tapEndurance, lengthSum);
                accPerfDict[speedIndex][i].SetValues(noteList[i].position, accStrain + accEndurance, lengthSum);
            }
        }

        public static void ComputeEnduranceDecay(ref float endurance, bool increaseDecay)
        {
            if (endurance > 0f)
            {
                var enduranceDecay = ENDURANCE_DECAY;
                if (increaseDecay)
                    enduranceDecay += endurance >= 1f ? .3f : .2f;
                endurance /= enduranceDecay;
            }
        }

        public static float CalcAimStrain(Note nextNote, Note previousNote, float weight, float MAX_TIME)
        {
            //Calc the space between two notes if they aren't connected sliders
            var distance = MathF.Pow(MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd), .9f);
            if (nextNote.pitchDelta != 0)
                distance *= .35f;
            float speed = distance / MathF.Max(nextNote.position - (previousNote.position + previousNote.length), MAX_TIME);
            
            //return the weighted speed with all the multiplier
            return speed * weight;
        }

        public static float CalcTapStrain(Note nextNote, Note previousNote, float weight) =>
            3f / MathF.Pow(nextNote.position - previousNote.position, 1.15f) * weight;

        public static float CalcAccStrain(Note nextNote, float weight)
        {
            var strain = MathF.Abs(nextNote.pitchDelta) / nextNote.length;
            if (nextNote.pitchDelta <= 34.375f)
                strain *= .5f;

            return strain * weight;
        }

        public static float CalcAimEndurance(Note nextNote, Note previousNote, float weight, float MAX_TIME)
        {
            float endurance = MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd) / MathF.Max(nextNote.position - (previousNote.position + previousNote.length), MAX_TIME * 3f) / 8000f;
            return endurance * weight;
        }

        public static bool IsSlider(Note nextNote, Note previousNote) => !(MathF.Round(nextNote.position - (previousNote.position + previousNote.length), 3) > 0);

        public static float CalcTapEndurance(Note nextNote, Note previousNote, float weight)
        {
            float endurance = 0.45f / MathF.Pow(nextNote.position - previousNote.position, 1.1f) / 80f;
            return endurance * weight;
        }

        public static float CalcAccEndurance(Note nextNote, float weight)
        {
            var endurance = MathF.Abs(nextNote.pitchDelta * .5f) / nextNote.length / 1200f; //This is equal to 0 if its not a slider
            if (nextNote.pitchDelta <= 34.375f)
                endurance *= .5f;

            return endurance * weight;
        }

        public void CalculateAnalytics(int gamespeed)
        {
            aimAnalyticsDict[gamespeed] = new DataVectorAnalytics(aimPerfDict[gamespeed]);
            tapAnalyticsDict[gamespeed] = new DataVectorAnalytics(tapPerfDict[gamespeed]);
            accAnalyticsDict[gamespeed] = new DataVectorAnalytics(accPerfDict[gamespeed]);
        }

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
                var aimWeight = (aimPerc + .5f) * 1.2f;
                var tapWeight = (tapPerc + .5f) * 1.15f;
                var accWeight = (accPerc + .5f) * 1.1f;
                var totalWeight = aimWeight + tapWeight + accWeight;
                starRatingDict[gamespeed] = ((aimRating * aimWeight) + (tapRating * tapWeight) + (accRating * accWeight)) / totalWeight;
            }
            else
                starRatingDict[gamespeed] = 0f;
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
