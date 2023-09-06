namespace TootTallyDiffCalcTTV2
{
    public static class RatingCriterias
    {

        public static List<RatingError> GetRatingErrors(Chart chart)
        {
            List<RatingError> errors = new List<RatingError>();
            var lastNote = chart.notesDict[2].Last();
            if (chart.notesDict[2].Count < 24)
                errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.NoteCount, 0, 0, chart.notesDict[2].Count));
            if (lastNote.position + lastNote.length - chart.notesDict[2][0].position <= 10f)
                errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.MapLength, chart.notesDict[2].Count - 1, lastNote.position + lastNote.length, lastNote.position + lastNote.length));

            for (int i = 0; i < chart.notesDict[2].Count - 1; i++)
            {

                Note currentNote = chart.notesDict[2][i];
                Note nextNote = chart.notesDict[2][i + 1];



                //notes starts within 1s of chart start
                if (currentNote.position <= 0.8d)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.HotStart, i, currentNote.position, currentNote.position));
                else if (currentNote.position <= 1.25f)
                    errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.HotStart, i, currentNote.position, currentNote.position));


                float length = currentNote.length;
                for (int j = i + 1; chart.notesDict[2].Count < j && chart.notesDict[2][j].position - (chart.notesDict[2][j - 1].position + chart.notesDict[2][j - 1].length) <= 0d; j++)
                    length += chart.notesDict[2][j].length;

                //notes shorter than 4.5s
                if (length >= 4.5454d)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.MaxLength, i, currentNote.position, length));
                else if (length > 4.4d)
                    errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.MaxLength, i, currentNote.position, length));

                if (currentNote.length > 0 && currentNote.length < 0.001f)
                    errors.Add(new RatingError(ErrorLevel.Notice, ErrorType.NoteLength, i, currentNote.position, length));

                //sliders velocity more than 2.5k u/s, warning 1.25k
                if (Math.Abs(currentNote.pitchDelta) >= 34.375d)
                    if (Math.Abs(currentNote.pitchDelta) / currentNote.length >= 2500d)
                        errors.Add(new RatingError(ErrorLevel.Error, ErrorType.SliderVelocity, i, currentNote.position, Math.Abs(currentNote.pitchDelta) / currentNote.length));
                    else if (Math.Abs(currentNote.pitchDelta) / currentNote.length >= 1250d)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.SliderVelocity, i, currentNote.position, Math.Abs(currentNote.pitchDelta) / currentNote.length));

                //note velocity more than 10k u/s, warning 5k
                if (currentNote.pitchDelta == 0 && Math.Round(nextNote.position - (currentNote.position + currentNote.length), 3) > 0)
                    if (Math.Abs(currentNote.pitchEnd - nextNote.pitchStart) / (nextNote.position - (currentNote.position + currentNote.length)) >= 5000d)
                        errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteVelocity, i, currentNote.position, Math.Abs(currentNote.pitchEnd - nextNote.pitchStart) / (nextNote.position - (currentNote.position + currentNote.length))));
                    else if (Math.Abs(currentNote.pitchEnd - nextNote.pitchStart) / (nextNote.position - (currentNote.position + currentNote.length)) >= 2500d)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.NoteVelocity, i, currentNote.position, Math.Abs(currentNote.pitchEnd - nextNote.pitchStart) / (nextNote.position - (currentNote.position + currentNote.length))));


                //note spacing bigger than 2/60th of 1s
                if (currentNote.pitchDelta == 0 && Math.Round(nextNote.position - (currentNote.position + currentNote.length), 3) > 0)
                    if (nextNote.position - (currentNote.position + currentNote.length) <= 2d / 60d)
                        errors.Add(new RatingError(ErrorLevel.Error, ErrorType.Spacing, i, currentNote.position, nextNote.position - (currentNote.position + currentNote.length)));
                    else if (nextNote.position - (currentNote.position + currentNote.length) <= 2d / 45d)
                        errors.Add(new RatingError(ErrorLevel.Warning, ErrorType.Spacing, i, currentNote.position, nextNote.position - (currentNote.position + currentNote.length)));

                //Pitch start out of play space
                if (Math.Abs(nextNote.pitchStart) > 180d)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteStartOutOfBound, i, currentNote.position, nextNote.pitchStart));

                //Pitch end out of play space
                if (nextNote.pitchStart != nextNote.pitchEnd && Math.Abs(nextNote.pitchEnd) > 180d)
                    errors.Add(new RatingError(ErrorLevel.Error, ErrorType.NoteEndOutOfBound, i, currentNote.position, nextNote.pitchEnd));
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
            NoteLength,
            MapLength,
            HotStart,
            MaxLength,
            Spacing,
            SliderVelocity,
            NoteVelocity,
            NoteStartOutOfBound,
            NoteEndOutOfBound,

        }
    }
}
