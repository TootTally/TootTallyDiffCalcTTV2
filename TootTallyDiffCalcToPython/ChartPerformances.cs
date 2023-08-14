using System.Globalization;
using System.Runtime.InteropServices;

namespace TootTallyDiffCalcTTV2
{
    public class ChartPerformances
    {
        public static readonly double[] weights = {1d, 0.945d, 0.893025d, 0.84390863d, 0.79749365d, 0.7536315d, 0.71218177d, 0.67301177d, 0.63599612d, 0.60101634d,
                                                   0.56796044d,   0.53672261d, 0.50720287d,0.47930671d,0.45294484d,0.42803288d,0.40449107,0.38224406,0.36122064d,0.3413535,
                                                   0.32257906d,  0.30483721d,0.28807116,0.27222725d,0.25725475d,0.25725475d,}; //lol

        public Dictionary<float, List<DataVector>> aimPerfDict;
        public Dictionary<float, DataVectorAnalytics> aimAnalyticsDict;

        public Dictionary<float, List<DataVector>> tapPerfDict;
        public Dictionary<float, DataVectorAnalytics> tapAnalyticsDict;

        public Dictionary<float, List<DataVector>> accPerfDict;
        public Dictionary<float, DataVectorAnalytics> accAnalyticsDict;

        public Dictionary<float, double> aimRatingDict;
        public Dictionary<float, double> tapRatingDict;
        public Dictionary<float, double> accRatingDict;
        public Dictionary<float, double> starRatingDict;

        private Chart _chart;

        public ChartPerformances(Chart chart)
        {
            aimPerfDict = new Dictionary<float, List<DataVector>>(7);
            tapPerfDict = new Dictionary<float, List<DataVector>>(7);
            accPerfDict = new Dictionary<float, List<DataVector>>(7);
            aimRatingDict = new Dictionary<float, double>(7);
            tapRatingDict = new Dictionary<float, double>(7);
            accRatingDict = new Dictionary<float, double>(7);
            starRatingDict = new Dictionary<float, double>(7);
            aimAnalyticsDict = new Dictionary<float, DataVectorAnalytics>(7);
            tapAnalyticsDict = new Dictionary<float, DataVectorAnalytics>(7);
            accAnalyticsDict = new Dictionary<float, DataVectorAnalytics>(7);


            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                aimPerfDict[i] = new List<DataVector>(chart.notes.Length) { new DataVector(0, 0, 0) };
                tapPerfDict[i] = new List<DataVector>(chart.notes.Length) { new DataVector(0, 0, 0) };
                accPerfDict[i] = new List<DataVector>(chart.notes.Length) { new DataVector(0, 0, 0) };
            }
            _chart = chart;
        }

        private const double MIN_TIMEDELTA = 1d / 120d;
        public void CalculatePerformances(int speedIndex)
        {

            var noteList = _chart.notesDict[speedIndex];
            var MAX_TIME = BeatToSeconds2(0.05, float.Parse(_chart.tempo) * Utils.GAME_SPEED[speedIndex]);
            var AVERAGE_NOTE_LENGTH = noteList.Average(n => n.length);
            var TOTAL_NOTE_LENGTH = noteList.Sum(n => n.length);
            var aimEndurance = 0.05d;
            var tapEndurance = 0.15d;
            var endurance_decay = 1.006d;

            var noteListSpan = CollectionsMarshal.AsSpan(noteList);
            for (int i = 0; i < noteListSpan.Length - 1; i++) //Main Forward Loop
            {
                var currentNote = noteListSpan[i];
                var previousNote = currentNote;
                var comboMultiplier = 0.8d;
                var directionMultiplier = 1d;
                var lengthSum = 0d;
                Direction currentDirection = Direction.Null, previousDirection = Direction.Null;

                var lenCount = 0;
                for (int j = i + 1; j < noteListSpan.Length && j < i + 10; j++)
                {
                    //Combo Calc
                    lengthSum += noteListSpan[j].length;
                    lenCount++;
                }
                comboMultiplier += lengthSum;

                //Second Forward Loop up to 26 notes and notes are at max 4 seconds appart
                var aimStrain = 0d;
                var tapStrain = 0d;
                var accStrain = 0d;

                for (int j = i + 1; j < noteListSpan.Length && j < i + 26 && noteListSpan[j].position - (currentNote.position + currentNote.length) <= 4; j++)
                {
                    var nextNote = noteListSpan[j];
                    var weight = weights[j - i - 1];
                    var endDecayMult = Math.Pow(endurance_decay, weight);
                    if (aimEndurance > 1f)
                        aimEndurance /= endDecayMult;
                    if (tapEndurance > 1f)
                        tapEndurance /= endDecayMult;

                    //Aim Calc
                    aimStrain += Math.Sqrt(CalcAimStrain(nextNote, previousNote, ref currentDirection, ref previousDirection, weight, ref directionMultiplier, aimEndurance, MAX_TIME)) / 26f;
                    aimEndurance += CalcAimEndurance(nextNote, previousNote, weight, directionMultiplier, MAX_TIME);

                    //Tap Calc
                    tapStrain += Math.Sqrt(CalcTapStrain(nextNote, previousNote, weight, comboMultiplier, tapEndurance, MIN_TIMEDELTA)) / 58f;
                    tapEndurance += CalcTapEndurance(nextNote, previousNote, weight);

                    //Acc Calc
                    accStrain += Math.Sqrt(CalcAccStrain(nextNote, previousNote, weight, comboMultiplier, directionMultiplier, AVERAGE_NOTE_LENGTH)) / 52f; // I can't figure that out yet

                    previousNote = nextNote;

                }
                var lenWeight = currentNote.length / TOTAL_NOTE_LENGTH;
                if (aimStrain != 0)
                    aimPerfDict[speedIndex].Add(new DataVector(currentNote.position, aimStrain, lenWeight));
                if (tapStrain != 0)
                tapPerfDict[speedIndex].Add(new DataVector(currentNote.position, tapStrain, lenWeight));
                if (accStrain != 0)
                accPerfDict[speedIndex].Add(new DataVector(currentNote.position, accStrain, lenWeight));
            }
        }

        public static double CalcAimStrain(Note nextNote, Note previousNote, ref Direction currentDirection, ref Direction previousDirection, double weight, ref double directionMultiplier, double endurance, double MAX_TIME)
        {
            double speed = 0d;

            //Calc the space between two notes if they aren't connected sliders
            if (!IsSlider(nextNote, previousNote))
            {
                //check for the direction of the space
                if (nextNote.pitchStart - previousNote.pitchEnd != 0)
                    if (previousNote.pitchStart - nextNote.pitchEnd > 0)
                        currentDirection = Direction.Up;
                    else
                        currentDirection = Direction.Down;
                else
                    currentDirection = Direction.Null;

                //Calculate the speed in units per seconds, capped at a minimum of 0.05 beats for the time to prevent bad mapping practices
                var distance = MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd) * 0.45f;
                var t = nextNote.position - (previousNote.position + previousNote.length);
                speed = distance / Math.Max(t, MAX_TIME);

                //Add directionalMultiplier bonus
                if (CheckDirectionChange(previousDirection, currentDirection))
                    directionMultiplier *= 1.02f;

                previousDirection = currentDirection; //update direction before looking at slider
            }

            if (nextNote.pitchDelta != 0) //Calc extra speed if its a slider
            {
                speed += (MathF.Abs(nextNote.pitchDelta) / nextNote.length) * 0.35f;
                currentDirection = nextNote.pitchDelta > 0 ? Direction.Up : nextNote.pitchDelta < 0 ? Direction.Down : Direction.Null; //Set direction for slider
                //Add directionalMultiplier bonus for slider
                if (CheckDirectionChange(previousDirection, currentDirection))
                    directionMultiplier *= 1.04f;

                previousDirection = currentDirection; //update direction from slider
            }
            //return the weighted speed with all the multiplier
            return speed * weight * directionMultiplier * endurance;
        }

        public static double CalcTapStrain(Note nextNote, Note previousNote, double weight, double comboMultiplier, double endurance, double MAX_TIMEDELTA)
        {
            var tapStrain = 0d;
            if (!IsSlider(nextNote, previousNote))
            {
                var timeDelta = Math.Max(nextNote.position - previousNote.position, MAX_TIMEDELTA);
                var strain = 10.5f / Math.Pow(timeDelta, 1.75f);
                tapStrain = strain * weight * comboMultiplier * endurance;
            }
            return tapStrain;
        }

        public static bool CheckDirectionChange(Direction prevDir, Direction currDir) => (prevDir != currDir && prevDir != Direction.Null && currDir != Direction.Null);

        public static double CalcAccStrain(Note nextNote, Note previousNote, double weight, double comboMultiplier, double directionMultiplier, double AVERAGE_NOTE_LENGTH)
        {
            var accStrain = 0d;
            var lengthmult = Math.Sqrt((nextNote.length * 1.01f) / AVERAGE_NOTE_LENGTH);

            if (!IsSlider(nextNote, previousNote))
            {
                var noteHeight = MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd) / (nextNote.position - previousNote.position);
                var strain = noteHeight * lengthmult;
                accStrain = strain * weight * directionMultiplier * comboMultiplier;
            }
            if (nextNote.pitchDelta != 0)
            {
                var sliderHeight = Math.Abs(nextNote.pitchDelta * 1.15f) / nextNote.length; //Promote height over speed
                var strain = sliderHeight * lengthmult;
                accStrain = strain * weight * directionMultiplier * comboMultiplier;
            }

            return accStrain;
        }



        public static double CalcAimEndurance(Note nextNote, Note previousNote, double weight, double directionalMultiplier, double MAX_TIME)
        {
            var endurance = 0d;
            var enduranceAimStrain = 0d;

            if (!IsSlider(nextNote, previousNote))
            {
                var distance = Math.Sqrt(MathF.Abs(nextNote.pitchStart - previousNote.pitchEnd)) * nextNote.length;
                var t = nextNote.position - (previousNote.position + previousNote.length);
                enduranceAimStrain = distance / Math.Max(t, MAX_TIME) / 45f;
            }

            if (nextNote.pitchDelta != 0) //Calc extra speed if its a slider
                enduranceAimStrain += (MathF.Abs(nextNote.pitchDelta) / (nextNote.length)) / 85f; //This is equal to 0 if its not a slider

            endurance += Math.Sqrt(enduranceAimStrain) / 125f;

            return endurance * weight * directionalMultiplier;
        }

        public static double CalcTapEndurance(Note nextNote, Note previousNote, double weight)
        {
            var endurance = 0d;

            var enduranceTapStrain = 0d;
            if (!IsSlider(nextNote, previousNote))
            {
                var timeDelta = nextNote.position - previousNote.position;
                enduranceTapStrain = 0.45f / Math.Pow(timeDelta, 1.75f);
            }

            endurance += Math.Sqrt(enduranceTapStrain) / 200f;

            return endurance * weight;
        }

        public static bool IsSlider(Note nextNote, Note previousNote) => !(Math.Round(nextNote.position - (previousNote.position + previousNote.length),3) > 0);

        public void CalculateAnalytics(float gamespeed)
        {
            aimAnalyticsDict[gamespeed] = new DataVectorAnalytics(aimPerfDict[gamespeed]);
            tapAnalyticsDict[gamespeed] = new DataVectorAnalytics(tapPerfDict[gamespeed]);
            accAnalyticsDict[gamespeed] = new DataVectorAnalytics(accPerfDict[gamespeed]);
        }

        public void CalculateRatings(float gamespeed)
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
                var aimWeight = (aimPerc + 0.25f) * 1.3f;
                var tapWeight = (tapPerc + 0.25f) * 1.05f;
                var accWeight = (accPerc + 0.25f) * 1.1f;
                var totalWeight = aimWeight + tapWeight + accWeight;
                starRatingDict[gamespeed] = ((aimRating * aimWeight) + (tapRating * tapWeight) + (accRating * accWeight)) / totalWeight;
            }
            else
                starRatingDict[gamespeed] = 0f;
        }

        public double GetDiffRating(float speed)
        {
            var index = (int)((speed - 0.5f) / .25f);
            if (speed % .25f == 0)
                return starRatingDict[index];

            var minSpeed = Utils.GAME_SPEED[index];
            var maxSpeed = Utils.GAME_SPEED[index + 1];
            var by = (speed - minSpeed) / (maxSpeed - minSpeed);
            return Utils.Lerp(starRatingDict[index], starRatingDict[index + 1], by);
        }


        public enum Direction
        {
            Null,
            Up,
            Down
        }

        public class DataVector
        {
            public double performance;
            public double time;
            public double weight;

            public DataVector(double time, double performance, double weight)
            {
                this.time = time;
                this.performance = performance;
                this.weight = weight;
            }

        }

        public class DataVectorAnalytics
        {
            public double perfAverage, perfMax, perfMin, perfSum, perfWeightedAverage;

            public DataVectorAnalytics(List<DataVector> dataVectorList)
            {
                var weightedAverage = CalculateWeightedAverage(dataVectorList);
                perfWeightedAverage = double.IsNaN(weightedAverage) ? 0 : weightedAverage;
                perfMax = dataVectorList.Max(x => x.performance);
                perfMin = dataVectorList.Min(x => x.performance);
                perfAverage = dataVectorList.Average(x => x.performance);
            }

            public static double CalculateWeightedAverage(List<DataVector> dataVectorList) => dataVectorList.Sum(x => x.performance * x.weight) / dataVectorList.Sum(x => x.weight);
        }
        public static double BeatToSeconds2(double beat, float bpm) => (60d / bpm) * beat;

    }

}
