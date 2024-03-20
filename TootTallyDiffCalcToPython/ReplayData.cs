using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TootTallyDiffCalcTTV2
{
    public class ReplayData
    {
        public string version { get; set; }
        public string username { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
        public string input { get; set; }
        public string song { get; set; }
        public string uuid { get; set; }
        public float samplerate { get; set; }
        public float scrollspeed { get; set; }
        public int defaultnotelength { get; set; }
        public float gamespeedmultiplier { get; set; }
        public string gamemodifiers { get; set; }
        public float audiolatency { get; set; }
        public int pluginbuilddate { get; set; }
        public string gameversion { get; set; }
        public string songhash { get; set; }
        public int finalscore { get; set; }
        public int maxcombo { get; set; }
        public int[] finalnotetallies { get; set; }
        public List<dynamic[]> framedata { get; set; }
        public List<dynamic[]> notedata { get; set; }
        public List<dynamic[]> tootdata { get; set; }
        public float servertime { get; set; }
        public string hmac { get; set; }

        public void OnDeserialize()
        {
            gamemodifiers ??= "None";
            version ??= "0.0.0";
        }

        public string GetJsonString() => JsonConvert.SerializeObject(this);

        public void FixReplayV1Conversion()
        {
            if (version != "1.0.9") return;

            finalscore = (int)notedata.Last()[1];
        }
    }
}
