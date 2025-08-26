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

        public TimeSpan calculationTime, criteriaCalculationTime;
        public int noteCount;
        public float songLength, songLengthMult;

        public void OnDeserialize()
        {
            notesDict = new Dictionary<float, List<Note>>();
            var noteCount = 0;
            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                var gamespeed = Utils.GAME_SPEED[i];

                var newTempo = tempo * gamespeed;
                int count = 1;
                notesDict[i] = new List<Note>(notes.Length) { new Note(0, 0, .015f, 0, 0, 0, false) };
                var sortedNotes = notes.OrderBy(x => x[0]).ToArray();
                for (int j = 0; j < sortedNotes.Length; j++)
                {
                    float length = sortedNotes[j][1];
                    if (length <= 0)//minLength only applies if the note is less or equal to 0 beats, else it keeps its "lower than minimum" length
                        length = 0.015f;
                    bool isSlider;
                    if (i > 0)
                        isSlider = notesDict[0][j + 1].isSlider;
                    else
                        isSlider = j + 1 < sortedNotes.Length && IsSlider(sortedNotes[j], sortedNotes[j + 1]);
                    if (i == 0 && !isSlider)
                        noteCount++;
                    notesDict[i].Add(new Note(count, BeatToSeconds2(sortedNotes[j][0], newTempo), BeatToSeconds2(length, newTempo), sortedNotes[j][2], sortedNotes[j][3], sortedNotes[j][4], isSlider));
                    count++;
                }
            }
            this.noteCount = noteCount;
            CalcScores();

            if (notesDict[2].Count > 2)
                songLength = notesDict[2].Last().position - notesDict[2][1].position;
            if (songLength < 1) songLength = 1;
            //songLengthMult = MathF.Pow((songLength + .5f) / 5f, -MathF.E * .3f) + .82f; //https://www.desmos.com/calculator/c18soumkcb
            //songLengthMult = MathF.Pow((songLength + .5f) / 2.5f, -MathF.E * .18f) + .74f; //https://www.desmos.com/calculator/c18soumkcb
            songLengthMult = MathF.Pow((songLength + .5f) / 10f, -MathF.E * .18f) + .67f; //https://www.desmos.com/calculator/c18soumkcb

            performances = new ChartPerformances(this);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ratingErrors = RatingCriterias.GetRatingErrors(this);
            stopwatch.Stop();
            criteriaCalculationTime = stopwatch.Elapsed;

            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                performances.CalculatePerformances(i);
                performances.CalculateAnalytics(i, songLengthMult);
                performances.CalculateRatings(i);
            }
            stopwatch.Stop();
            calculationTime = stopwatch.Elapsed;
        }

        public static float GetLength(float length) => Math.Clamp(length, .2f, 5f) * 8f + 10f;

        public int GetNoteCount()
        {
            var noteCount = 0;
            for (int i = 0; i < notes.Length; i++)
            {
                while (i + 1 < notes.Length && IsSlider(notes[i], notes[i + 1])) { i++; }
                noteCount++;
            }
            return noteCount;
        }

        public void CalcScores()
        {
            maxScore = 0;
            gameMaxScore = 0;
            var noteCount = 0;
            for (int i = 0; i < notes.Length; i++)
            {
                var length = notes[i][1];
                while (i + 1 < notes.Length && notes[i][0] + notes[i][1] + .025f >= notes[i + 1][0])
                {
                    length += notes[i + 1][1];
                    i++;
                }
                var champBonus = noteCount > 23 ? 1.5d : 0d;
                var realCoefficient = (Math.Min(noteCount, 10) + champBonus) * 0.1d + 1d;
                var clampedLength = GetLength(length);
                var noteScore = (int)(Math.Floor((float)((double)clampedLength * 100d * realCoefficient)) * 10f);
                maxScore += noteScore;
                gameMaxScore += (int)Math.Floor(Math.Floor(clampedLength * 100f * 1.315f) * 10f);
                noteCount++;
            }
        }

        public void CalcPerformances()
        {
            performances = new ChartPerformances(this);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < Utils.GAME_SPEED.Length; i++)
            {
                performances.CalculatePerformances(i);
                performances.CalculateAnalytics(i, songLengthMult);
                performances.CalculateRatings(i);
            }
            stopwatch.Stop();
            calculationTime = stopwatch.Elapsed;
        }

        // between 0.5f to 2f
        public float GetBaseTT(float speed) => Utils.CalculateBaseTT(GetDiffRating(Math.Clamp(speed, 0.5f, 2f)));

        //Returns the lerped star rating
        public float GetDiffRating(float speed) => performances.GetDiffRating(Math.Clamp(speed, 0.5f, 2f));

        public float GetDynamicDiffRating(float speed, int hitCount, string[] modifiers = null) => performances.GetDynamicDiffRating(hitCount, speed, modifiers);

        public float GetLerpedStarRating(float speed) => performances.GetDiffRating(Math.Clamp(speed, 0.5f, 2f));

        public float GetAimPerformance(float speed) => performances.aimAnalyticsDict[SpeedToIndex(speed)].perfWeightedAverage;
        public float GetTapPerformance(float speed) => performances.tapAnalyticsDict[SpeedToIndex(speed)].perfWeightedAverage;
        public float GetAccPerformance(float speed) => performances.accAnalyticsDict[SpeedToIndex(speed)].perfWeightedAverage;

        public float GetStarRating(float speed) => performances.starRatingDict[SpeedToIndex(speed)];

        public int SpeedToIndex(float speed) => (int)((Math.Clamp(speed, 0.5f, 2f) - 0.5f) / .25f);

        public class Lyrics
        {
            public string bar;
            public string text;
        }

        public static float BeatToSeconds2(float beat, float bpm) => 60f / bpm * beat;


        public static int GetConvertionVersion(ReplayData replay)
        {
            if (replay.version == "0.0.0")
                return replay.notedata.First().Length >= 6 ? 0 : -1;
            else
                return string.Compare(replay.version, "2.0.0") < 0 ? 1 : 2;
        }

        public ReplayData TryConvertReplay(ReplayData replay)
        {
            var id = GetConvertionVersion(replay);
            if (id == -1)
            {
                Console.WriteLine($"Replay {replay.uuid} cannot be converted.");
                return replay;
            }
            else
                return id == 0 || id == 1 ? ConvertReplayV1(replay) : ConvertReplayV2(replay);
        }


        public ReplayData ConvertReplayV2(ReplayData replay)
        {
            bool wasSlider = false;
            bool releasedBetweenNotes;
            int currentScore = 0;
            float health = 0; // 0 to 100
            int combo = 0;
            int highestCombo = 0;
            int multiplier = 0; // 0 to 10
            int[] noteTally = new int[5];

            List<dynamic[]> convertedNoteData = new List<dynamic[]>();
            float[] nextNote = null;
            //Loop through all the notes in a chart
            for (int i = 0; i < notes.Length; i++)
            {
                wasSlider = false;
                releasedBetweenNotes = (int)replay.notedata[i][1] == 1;
                float[] currNote = notes[i];
                if (i + 1 < notes.Length)
                    nextNote = notes[i + 1];
                List<LengthAccPair> noteLengths = new List<LengthAccPair>()
                {
                    new LengthAccPair(currNote[1], (float)replay.notedata[i][0])
                };

                //Scroll forward until the next note is no longer a slider
                while (i + 1 < notes.Length && nextNote != null && IsSlider(currNote, nextNote))
                {
                    wasSlider = true;
                    currNote = notes[++i];
                    noteLengths.Add(new LengthAccPair(currNote[1], (float)replay.notedata[i][0])); //Create note length and note acc pair to weight later
                    if (i + 1 >= notes.Length)
                        break;
                    nextNote = notes[i + 1];
                }

                float noteAcc = 0f;
                float totalLength = 0f;
                if (wasSlider)
                {
                    //Get total length of all slider bodies
                    totalLength = noteLengths.Select(x => x.length).Sum();
                    for (int j = 0; j < noteLengths.Count; j++)
                        noteAcc += noteLengths[j].acc * (noteLengths[j].length / totalLength); //Length weighted acc sum of all slider bodies
                }
                else
                {
                    //If its not a slider, just take the acc and length of it
                    noteAcc = (float)replay.notedata[i][0];
                    totalLength = currNote[1];
                }

                //Calc the score before doing the combo and health because fucking base game logic is MIND BLOWING I know
                currentScore += GetScore(noteAcc, totalLength, multiplier, health == 100);

                //Calc new health
                var healthDiff = releasedBetweenNotes ? GetHealthDiff(noteAcc) : -15f;

                if (health == 100 && healthDiff < 0)
                    health = 0;
                else if (health != 100)
                    health += healthDiff;
                health = Math.Clamp(health, 0, 100);

                //Get the note tally
                int tally = 0;
                if (noteAcc > 95f) tally = 4;
                else if (noteAcc > 88f) tally = 3;
                else if (noteAcc > 79f) tally = 2;
                else if (noteAcc > 70f) tally = 1;
                noteTally[4 - tally]++;
                //Only increase combo if you get more than 79% acc + update highest if needed
                if (tally > 2 && releasedBetweenNotes)
                {
                    if (++combo > highestCombo)
                        highestCombo = combo;
                }
                else
                    combo = 0;

                multiplier = Math.Min(combo, 10);

                convertedNoteData.Add(new dynamic[9]
                {
                    noteAcc,
                    releasedBetweenNotes ? 1 : 0,
                    i,
                    combo,
                    multiplier,
                    currentScore,
                    health,
                    highestCombo,
                    tally
                });
            }

            replay.notedata = convertedNoteData;
            replay.finalnotetallies = noteTally;
            replay.finalscore = convertedNoteData.Last()[5];
            replay.maxcombo = highestCombo;
            replay.version = "2.0.9";

            return replay;
        }

        public ReplayData ConvertReplayV1(ReplayData replay)
        {
            bool wasSlider = false;
            bool releasedBetweenNotes;
            int currentScore = 0;
            float health = 0; // 0 to 100
            float previousHealth = 0;
            int combo = 0;
            int highestCombo = 0;
            int multiplier = 0; // 0 to 10
            int[] noteTally = new int[5];

            List<dynamic[]> convertedNoteData = new List<dynamic[]>();
            float[] nextNote = null;
            //Loop through all the notes in a chart
            for (int i = 0; i < notes.Length; i++)
            {
                wasSlider = false;
                var replayHealth = (int)replay.notedata[i][3];
                releasedBetweenNotes = !(replayHealth < previousHealth && ((float)replay.notedata[i][5] / 1000f) > 79f);
                previousHealth = replayHealth;

                float[] currNote = notes[i];
                if (i + 1 < notes.Length)
                    nextNote = notes[i + 1];
                List<LengthAccPair> noteLengths = new List<LengthAccPair>
                {
                    new LengthAccPair(currNote[1], (float)replay.notedata[i][5] / 1000f)
                };

                //Scroll forward until the next note is no longer a slider
                while (i + 1 < notes.Length && nextNote != null && IsSlider(currNote, nextNote))
                {
                    wasSlider = true;
                    currNote = notes[++i];
                    noteLengths.Add(new LengthAccPair(currNote[1], (float)replay.notedata[i][5] / 1000f)); //Create note length and note acc pair to weight later
                    if (i + 1 >= notes.Length)
                        break;
                    nextNote = notes[i + 1];
                }

                float noteAcc = 0f;
                float totalLength = 0f;
                if (wasSlider)
                {
                    //Get total length of all slider bodies
                    totalLength = noteLengths.Select(x => x.length).Sum();
                    for (int j = 0; j < noteLengths.Count; j++)
                        noteAcc += noteLengths[j].acc * (noteLengths[j].length / totalLength); //Length weighted acc sum of all slider bodies
                }
                else
                {
                    //If its not a slider, just take the acc and length of it
                    noteAcc = (float)replay.notedata[i][5] / 1000f;
                    totalLength = currNote[1];
                }

                //Calc the score before doing the combo and health because fucking base game logic is MIND BLOWING I know
                currentScore += GetScore(noteAcc, totalLength, multiplier, health == 100);

                //Calc new health
                var healthDiff = releasedBetweenNotes ? GetHealthDiff(noteAcc) : -15f;

                if (health == 100 && healthDiff < 0)
                    health = 0;
                else if (health != 100)
                    health += healthDiff;
                health = Math.Clamp(health, 0, 100);

                //Get the note tally
                int tally = 0;
                if (noteAcc > 95f) tally = 4;
                else if (noteAcc > 88f) tally = 3;
                else if (noteAcc > 79f) tally = 2;
                else if (noteAcc > 70f) tally = 1;
                noteTally[4 - tally]++;
                //Only increase combo if you get more than 79% acc + update highest if needed
                if (tally > 2 && releasedBetweenNotes)
                {
                    if (++combo > highestCombo)
                        highestCombo = combo;
                }
                else
                    combo = 0;

                multiplier = Math.Min(combo, 10);
                convertedNoteData.Add(new dynamic[9]
                {
                    i,
                    currentScore,
                    multiplier,
                    (int)health,
                    tally,
                    (int)(noteAcc * 1000f),
                    combo,
                    releasedBetweenNotes ? 1 : 0,
                    highestCombo
                });
            }

            replay.notedata = convertedNoteData;
            replay.finalnotetallies = noteTally;
            replay.finalscore = convertedNoteData.Last()[1]; //Supposed to be [1]
            replay.maxcombo = highestCombo;
            replay.version = "1.0.9";

            return replay;
        }

        public static bool IsSlider(float[] currNote, float[] nextNote) => currNote[0] + currNote[1] + .025f >= nextNote[0];
        public static float GetHealthDiff(float acc) => Math.Clamp((acc - 79f) * 0.2193f, -15f, 4.34f);
        public static int GetScore(float acc, float totalLength, float mult, bool champ)
        {
            var baseScore = Math.Clamp(totalLength, 0.2f, 5f) * 8f + 10f;
            return (int)Math.Floor(baseScore * acc * ((mult + (champ ? 1.5f : 0f)) * .1f + 1f)) * 10;
        }

        public void Dispose()
        {
            notes = null;
            bgdata = null;
            lyrics?.Clear();
            notesDict?.Clear();
            ratingErrors?.Clear();
            performances.Dispose();
        }

        public class LengthAccPair
        {
            public float length, acc;

            public LengthAccPair(float length, float acc)
            {
                this.length = length;
                this.acc = acc;
            }
        }

    }
}
