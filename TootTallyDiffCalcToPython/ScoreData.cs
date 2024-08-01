using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TootTallyDiffCalcTTV2
{
    public struct ScoreData
    {
        public int score;
        public int max_combo;
        public string player;
        public string grade;
        public float percentage;
        public int perfect;
        public int nice;
        public int okay;
        public int meh;
        public int nasty;
        public float tt;
        public bool is_rated;
        public int song_id;
        public float replay_speed;
        public string[] modifiers;

        public int GetHitCount => perfect + nice;
    }
}
