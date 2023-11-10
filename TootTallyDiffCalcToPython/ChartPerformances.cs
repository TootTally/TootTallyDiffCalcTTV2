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
            }
            _chart = chart;
        }

        public const float ENDURANCE_DECAY = 1.025f;

        public void CalculatePerformances(int speedIndex)
        {
            var noteList = _chart.notesDict[speedIndex];
            var aimEndurance = .5f;
            var tapEndurance = .5f;
            var accEndurance = .5f;

            var spanList = CollectionsMarshal.AsSpan(noteList);
            var firstNotePosition = spanList[0].position;
            for (int i = 0; i < spanList.Length - 1; i++) //Main Forward Loop
            {
                var currentNote = spanList[i];
                var previousNote = currentNote;

                var lengthSum = 0f;
                for (int j = i; j < i + 10 && j < spanList.Length; j++)
                    lengthSum += spanList[j].length * weights[j - i];

                var timeMinute = (currentNote.position - firstNotePosition) / 60f;
                var timeMultiplier = MathF.Max(((1f + timeMinute / 50f) * (1f - MathF.Pow(MathF.E, -.6f * timeMinute + .5f))) + .33f, .33f);

                var aimStrain = 0f;
                var tapStrain = 0f;
                var accStrain = 0f;

                var increaseDecay = currentNote.position - firstNotePosition < 30f;
                ComputeEnduranceDecay(ref aimEndurance, increaseDecay);
                ComputeEnduranceDecay(ref tapEndurance, increaseDecay);
                ComputeEnduranceDecay(ref accEndurance, increaseDecay);

                for (int j = i + 1; j < spanList.Length && j < i + 10 && spanList[j].position - (currentNote.position + currentNote.length) <= 10; j++)
                {
                    var nextNote = spanList[j];
                    var MAX_TIME = previousNote.length * .66f;
                    var weight = weights[j - i - 1];

                    //Aim Calc
                    aimStrain += CalcAimStrain(nextNote, previousNote, weight, aimEndurance, MAX_TIME) / 70f;
                    aimEndurance += CalcAimEndurance(nextNote, previousNote, weight, MAX_TIME);

                    //Tap Calc
                    tapStrain += CalcTapStrain(nextNote, previousNote, weight, tapEndurance) / 40f;
                    tapEndurance += CalcTapEndurance(nextNote, previousNote, weight);

                    //Acc Calc
                    accStrain += CalcAccStrain(nextNote, weight, accEndurance) / 30f;
                    accEndurance += CalcAccEndurance(nextNote, weight);

                    previousNote = nextNote;

                }
                aimPerfDict[speedIndex][i] = new DataVector(currentNote.position, MathF.Sqrt(aimStrain * timeMultiplier), lengthSum);
                tapPerfDict[speedIndex][i] = new DataVector(currentNote.position, MathF.Sqrt(tapStrain * timeMultiplier), lengthSum);
                if (accStrain != 0)
                    accPerfDict[speedIndex][i] = new DataVector(currentNote.position, MathF.Sqrt(accStrain * timeMultiplier), lengthSum);
            }
        }

        public static void ComputeEnduranceDecay(ref float endurance, bool increaseDecay)
        {
            if (endurance > 1f)
                endurance /= ENDURANCE_DECAY * (increaseDecay ? 1.125f : 1f);
            else if (endurance > .5f && increaseDecay)
                endurance /= ENDURANCE_DECAY * 1.1f;
        }

        public static float CalcAimStrain(Note nextNote, Note previousNote, float weight, float endurance, float MAX_TIME)
        {
            if (IsSlider(nextNote, previousNote)) return 0;

            //Calc the space between two notes if they aren't connected sliders
            var distance = MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd);
            if (nextNote.pitchDelta != 0)
                distance *= .45f;

            var t = nextNote.position - (previousNote.position + previousNote.length);
            float speed = distance / MathF.Max(t, MAX_TIME);

            //return the weighted speed with all the multiplier
            return speed * weight * endurance;
        }

        public static float CalcTapStrain(Note nextNote, Note previousNote, float weight, float endurance)
        {
            if (IsSlider(nextNote, previousNote)) return 0;

            var timeDelta = nextNote.position - previousNote.position;
            var strain = 6f / MathF.Pow(timeDelta, 1.08f);

            return strain * weight * endurance;
        }

        public static float CalcAccStrain(Note nextNote, float weight, float endurance)
        {
            if (nextNote.pitchDelta == 0) return 0;

            var strain = MathF.Abs(nextNote.pitchDelta) / nextNote.length;

            return strain * weight * endurance;
        }

        public static float CalcAimEndurance(Note nextNote, Note previousNote, float weight, float MAX_TIME)
        {
            if (IsSlider(nextNote, previousNote)) return 0;

            var distance = MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd);
            var t = nextNote.position - (previousNote.position + previousNote.length);
            float endurance = distance / MathF.Max(t, MAX_TIME * 3f) / 6200f;

            return endurance * weight;
        }

        public static bool IsSlider(Note nextNote, Note previousNote) => !(MathF.Round(nextNote.position - (previousNote.position + previousNote.length), 3) > 0);

        public static float CalcTapEndurance(Note nextNote, Note previousNote, float weight)
        {

            if (IsSlider(nextNote, previousNote)) return 0;

            float timeDelta = nextNote.position - previousNote.position;
            float endurance = 0.45f / MathF.Pow(timeDelta, 1.1f) / 55f;

            return endurance * weight;
        }

        public static float CalcAccEndurance(Note nextNote, float weight)
        {
            if (nextNote.pitchDelta == 0) return 0;

            var endurance = MathF.Abs(nextNote.pitchDelta * .5f) / nextNote.length / 5000f; //This is equal to 0 if its not a slider

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
                var aimWeight = (aimPerc + .16f) * 1.2f;
                var tapWeight = (tapPerc + .16f) * 1.35f;
                var accWeight = (accPerc + .16f) * 1.1f;
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

            public DataVector(float time, float performance, float weight)
            {
                this.time = time;
                this.performance = performance;
                this.weight = weight;
            }
        }

        public class DataVectorAnalytics
        {
            public float perfMax, perfMin, perfSum, perfWeightedAverage;

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
                    else if (dataVectorList[i].performance < perfMin)
                        perfMin = dataVectorList[i].performance;

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
