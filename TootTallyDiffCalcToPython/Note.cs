namespace TootTallyDiffCalcTTV2
{
    public class Note
    {
        public int count;
        public float pitchStart, pitchDelta, pitchEnd;
        public double position, length;

        public Note(int count, double position, double length, float pitchStart, float pitchDelta, float pitchEnd)
        {
            this.count = count;
            this.position = position;
            this.length = length;
            this.pitchStart = pitchStart;
            this.pitchDelta = pitchDelta;
            this.pitchEnd = pitchEnd;
        }

    }
}
