using System.Diagnostics;

namespace TootTallyDiffCalcTTV2
{
    public struct Chart
    {
        public float[][] notes;
        public string[][] bgdata;
        public Note[] notesArray;
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

        public ChartPerformances performances;
        public List<RatingCriterias.RatingError> ratingErrors;

        public TimeSpan calculationTime, criteriaCalculationTime;
        public int noteCount;
        public float songLength;

        public void OnDeserialize()
        {
            notesArray = new Note[notes.Length + 1];
            notesArray[0] = new Note(0, 0, .015f, 0, 0, 0, false);
            var sortedNotes = notes.OrderBy(x => x[0]).ToArray();
            for (int i = 0; i < sortedNotes.Length; i++)
            {
                float length = sortedNotes[i][1];
                if (length <= 0)//minLength only applies if the note is less or equal to 0 beats, else it keeps its "lower than minimum" length
                    length = 0.015f;
                bool isSlider = i + 1 < sortedNotes.Length && IsSlider(sortedNotes[i], sortedNotes[i + 1]);
                notesArray[i + 1] = new Note(i + 1, BeatToSeconds2(sortedNotes[i][0], tempo), BeatToSeconds2(length, tempo), sortedNotes[i][2], sortedNotes[i][3], sortedNotes[i][4], isSlider);
            }

            noteCount = GetNoteCount();
            CalcScores();
            if (notesArray.Length > 2)
                songLength = notesArray.Last().position - notesArray[1].position;
            if (songLength < 1) songLength = 1;

            performances = new ChartPerformances(this);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ratingErrors = RatingCriterias.GetRatingErrors(this);
            stopwatch.Stop();
            criteriaCalculationTime = stopwatch.Elapsed;
            notes = null;
        }

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

        public static float GetLength(float length) => Math.Clamp(length, .2f, 5f) * 8f + 10f;

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
                performances.CalculatePerformance(i);
                performances.CalculateAnalytics(i);
                performances.CalculateRatings(i);
            }
            stopwatch.Stop();
            calculationTime = stopwatch.Elapsed;
        }


        // between 0.5f to 2f
        //public float GetBaseTT(float speed) => Utils.CalculateBaseTT(GetDiffRating(Math.Clamp(speed, 0.5f, 2f)));

        //Returns the lerped star rating
        //public float GetDiffRating(float speed) => performances.GetDiffRating(Math.Clamp(speed, 0.5f, 2f));

        public float GetDynamicDiffRating(float speed, int hitCount, string[] modifiers = null) => performances.GetDynamicDiffRating(hitCount, speed, modifiers);
        public float GetDynamicTTRating(float speed, int hitCount, float multiplier, string[] modifiers = null) => performances.GetDynamicTTRating(hitCount, speed, multiplier, modifiers);
        public float GetMaxTTRating(float speed, string[] modifiers = null) => performances.GetDynamicTTRating(notesArray.Length, speed, 1, modifiers);

        //public float GetLerpedStarRating(float speed) => performances.GetDiffRating(Math.Clamp(speed, 0.5f, 2f));

        public float GetAimPerformance(float speed) => performances.aimAnalyticsArray[SpeedToIndex(speed)].perfWeightedAverage;
        public float GetTapPerformance(float speed) => performances.tapAnalyticsArray[SpeedToIndex(speed)].perfWeightedAverage;

        public float GetStarRating(float speed) => performances.starRatingDict[SpeedToIndex(speed)];

        public int SpeedToIndex(float speed) => (int)((Math.Clamp(speed, 0.5f, 2f) - 0.5f) / .25f);

        public static float BeatToSeconds2(float beat, float bpm) => 60f / bpm * beat;

        #region Replays
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
            notesArray = null;
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
        #endregion
    }
}
