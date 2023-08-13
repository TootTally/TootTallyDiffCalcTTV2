namespace TootTallyDiffCalcTTV2
{
    public class Note
    {
        public int count;
        public float pitchStart, pitchDelta, pitchEnd;
        public float position, length;

        public Note(int count, float position, float length, float pitchStart, float pitchDelta, float pitchEnd)
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
