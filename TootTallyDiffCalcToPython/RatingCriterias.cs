namespace TootTallyDiffCalcTTV2
{
    public static class RatingCriterias
    {

        public static List<RatingError> GetRatingErrors(Chart chart)
        {
            List<RatingError> errors = new List<RatingError>();
            var lastNote = chart.notesDict[2].Last();

            //Warning on note count smaller than 24 notes
            if (chart.notesDict[2].Count < 24)
                errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.NoteCount, 0, 0, chart.notesDict[2].Count));

            //Warning on chart being having less than 10s worth of notes
            if (lastNote.position + lastNote.length - chart.notesDict[2][1].position <= 10f)
                errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.MapLength, chart.notesDict[2].Count - 1, lastNote.position + lastNote.length, lastNote.position + lastNote.length));

            Note previousNote = new Note(-1, 0,0,0,0,0,false);
            var noteList = chart.notesDict[2].GetRange(1,chart.notesDict[2].Count - 1); //Remove first fake note
            for (int i = 0; i < noteList.Count - 1; i++)
            {
                Note currentNote = noteList[i];
                Note nextNote = noteList[i + 1];

                //ONLY APPLICABLE TO UPCOMING OR NEWLY RATED CHARTS
                //notes starts within: error at shorter than 0.8s, warning at 1.25f
                if (currentNote.position <= 0.8f)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.HotStart, i, currentNote.position, currentNote.position));
                else if (currentNote.position <= 1.25f)
                    errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.HotStart, i, currentNote.position, currentNote.position));

                var length = currentNote.length;
                for (int j = i + 1; noteList.Count > j && noteList[j - 1].isSlider; j++) //Only calc full length if you just got into a multi segmented slide
                    length += noteList[j].length;

                //notes shorter than: error at 4.5454s, warning at 4.4s, notice at 4s
                if (length >= 4.5454f)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.LongNote, i, currentNote.position, length));
                else if (length > 4.4f)
                    errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.LongNote, i, currentNote.position, length));

                if (currentNote.length < 0.001f)
                    errors.Add(new RatingError(ErrorLevel.Notice, ErrorType.ShortNote, i, currentNote.position, currentNote.length));

                //sliders velocity more than 3k u/s, warning 2k, Notice at 1k
                var pitchDelta = MathF.Abs(currentNote.pitchDelta);
                if (pitchDelta >= 34.375f)
                {
                    var pitchVelocity = pitchDelta / currentNote.length;
                    if (pitchVelocity >= 3000f)
                        errors.Add(new RatingError(ErrorLevel.Error, ErrorType.SliderVelocity, i, currentNote.position, pitchVelocity));
                    else if (pitchVelocity >= 2000f)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.SliderVelocity, i, currentNote.position, pitchVelocity));
                }
                if (!noteList[i].isSlider)
                {
                    var noteDistance = nextNote.position - (currentNote.position + currentNote.length);
                    var noteDelta = MathF.Abs(currentNote.pitchEnd - nextNote.pitchStart);
                    var noteVelocity = noteDelta / noteDistance;

                    //note velocity more than 6k u/s, warning 3k, notice at 1k
                    if (noteVelocity >= 7500f)
                        errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteVelocity, i, currentNote.position, noteVelocity));
                    else if (noteVelocity >= 4500f)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.NoteVelocity, i, currentNote.position, noteVelocity));

                    //note spacing smaller than: error at 2/60th, warning at 2/45th, notice at 2/25th
                    if (noteDistance <= 2d / 60f)
                        errors.Add(new RatingError(ErrorLevel.Error, ErrorType.Spacing, i, currentNote.position, noteDistance));
                    else if (noteDistance <= 2d / 45f)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.Spacing, i, currentNote.position, noteDistance));
                }

                if (!(previousNote.isSlider && currentNote.isSlider))
                {
                    //Slider Head is shorter than 0.04s 
                    if (previousNote.isSlider && previousNote.length <= .04f)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.SliderHead, i, currentNote.position, previousNote.length));
                    //Slider Tail is shorter than 0.04s
                    if (nextNote.isSlider && nextNote.length <= .04f)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.SliderTail, i, currentNote.position, nextNote.length));
                }


                //Pitch start out of play space
                if (MathF.Abs(currentNote.pitchStart) > 180d)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteStartOutOfBound, i, currentNote.position, currentNote.pitchStart));

                //Pitch end out of play space
                if (currentNote.pitchDelta != 0 && MathF.Abs(currentNote.pitchEnd) > 180d)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteEndOutOfBound, i, currentNote.position, currentNote.pitchEnd));

                //If pitch delta is different than intended
                if (Math.Round(currentNote.pitchDelta, 3) != Math.Round(currentNote.pitchEnd - currentNote.pitchStart, 3))
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteDelta, i, currentNote.position, (currentNote.pitchEnd - currentNote.pitchStart) - currentNote.pitchDelta));

                if (errors.Count > 500) break; //Don't check for more errors if too many notes lol

                previousNote = currentNote;
            }
            return errors;
        }


        public class RatingError
        {
            public ErrorLevel errorLevel;
            public ErrorType errorType;
            public int noteID;
            public float timing;
            public float value;

            public RatingError(ErrorLevel errorLevel, ErrorType errorType, int noteID, float timing, float value)
            {
                this.errorLevel = errorLevel;
                this.errorType = errorType;
                this.noteID = noteID + 1;
                this.timing = timing;
                this.value = value;
            }
        }

        public enum ErrorLevel
        {
            Error,
            Warning,
            Notice,
        }

        public enum ErrorType
        {
            NoteCount,
            ShortNote,
            MapLength,
            HotStart,
            LongNote,
            Spacing,
            SliderVelocity,
            NoteVelocity,
            NoteStartOutOfBound,
            NoteEndOutOfBound,
            NoteDelta,
            SliderHead,
            SliderTail,
        }
    }
}
