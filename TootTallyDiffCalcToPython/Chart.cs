using System.Diagnostics;

namespace TootTallyDiffCalcTTV2
{
    public class Chart
    {
        public float[][] notes;
        public string[][] bgdata;
        public Dictionary<float, List<Note>> notesDict;
        public List<string> note_color_start;
        public List<string> note_color_end;
        public float endpoint;
        public float savednotespacing;
        public float tempo;
        public string timesig;
        public string trackRef;
        public string name;
        public string shortName;
        public string author;
        public string genre;
        public string description;
        public string difficulty;
        public string year;
        public float maxScore;
        public float gameMaxScore;

        public List<Lyrics> lyrics;

        public ChartPerformances performances;
        public List<RatingCriterias.RatingError> ratingErrors;

        public TimeSpan calculationTime;

        public void OnDeserialize()
        {
            notesDict = new Dictionary<float, List<Note>>();
            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                var gamespeed = Utils.GAME_SPEED[i];

                var newTempo = tempo * gamespeed;
                int count = 1;
                maxScore = 0;
                gameMaxScore = 0;
                notesDict[i] = new List<Note>(notes.Length);

                foreach (float[] n in notes)
                {
                    float length = n[1];
                    if (length <= 0)//minLength only applies if the note is less or equal to 0 beats, else it keeps its "lower than minimum" length
                        length = 0.015f;
                    //Taken from HighscoreAccuracy https://github.com/emmett-shark/HighscoreAccuracy/blob/3f4be49f4ef31b8df1533511c7727bf7813c7773/Utils.cs#L30C1-L30C1
                    var champBonus = count - 1 > 23 ? 1.5f : 0;
                    var realCoefficient = (Math.Min(count - 1, 10) + champBonus) * 0.1f + 1f;
                    var noteScore = (int)Math.Floor(Math.Floor(length * 10f * 100f * realCoefficient) * 10f);
                    maxScore += noteScore;
                    gameMaxScore += (int)Math.Floor(Math.Floor(length * 10f * 100f * 1.315f) * 10f);
                    notesDict[i].Add(new Note(count, BeatToSeconds2(n[0], newTempo), BeatToSeconds2(length, newTempo), n[2], n[3], n[4]));
                    count++;
                }
            }

            performances = new ChartPerformances(this);
            ratingErrors = RatingCriterias.GetRatingErrors(this);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                performances.CalculatePerformances(i);
                performances.CalculateAnalytics(i);
                performances.CalculateRatings(i);
            }
            stopwatch.Stop();
            calculationTime = stopwatch.Elapsed;
        }

        // between 0.5f to 2f
        public double GetBaseTT(float speed) => Utils.CalculateBaseTT(GetDiffRating(Math.Clamp(speed, 0.5f, 2f)));

        //Returns the lerped star rating
        public double GetDiffRating(float speed) => performances.GetDiffRating(Math.Clamp(speed, 0.5f, 2f));
        public double GetLerpedStarRating(float speed) => performances.GetDiffRating(Math.Clamp(speed, 0.5f, 2f));

        public double GetAimPerformance(float speed) => performances.aimAnalyticsDict[SpeedToIndex(speed)].perfWeightedAverage;
        public double GetTapPerformance(float speed) => performances.tapAnalyticsDict[SpeedToIndex(speed)].perfWeightedAverage;
        public double GetAccPerformance(float speed) => performances.accAnalyticsDict[SpeedToIndex(speed)].perfWeightedAverage;

        public double GetStarRating(float speed) => performances.starRatingDict[SpeedToIndex(speed)];

        public float SpeedToIndex(float speed) => (int)((Math.Clamp(speed, 0.5f, 2f) - 0.5f) / .25f);

        public class Lyrics
        {
            public string bar;
            public string text;
        }

        public static float BeatToSeconds2(float beat, float bpm) => (60f / bpm) * beat;

    }
}
