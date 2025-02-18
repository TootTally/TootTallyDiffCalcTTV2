﻿using Newtonsoft.Json;
using System.Security.Cryptography;

namespace TootTallyDiffCalcTTV2
{
    public static class ChartReader
    {
        private static List<Chart> _allChartList = new List<Chart>();

        public static void AddChartToList(string path) =>
            _allChartList.Add(LoadChart(path));


        public static ReplayData LoadReplay(string path)
        {
            ReplayData replay = JsonConvert.DeserializeObject<ReplayData>(File.ReadAllText(path));
            return replay;
        }

        public static ReplayData LoadReplayFromJson(string json)
        {
            ReplayData replay = JsonConvert.DeserializeObject<ReplayData>(json);
            replay.OnDeserialize();
            return replay;
        }

        public static Chart LoadChart(string path)
        {
            StreamReader reader = new StreamReader(path);
            string json = reader.ReadToEnd();
            Chart chart = null;
            try
            {
                chart = JsonConvert.DeserializeObject<Chart>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"song {path} is not a valid json");
            }
            chart?.OnDeserialize();
            reader.Close();
            return chart;
        }

        public static Chart LoadChartFromJson(string json)
        {
            Chart chart = JsonConvert.DeserializeObject<Chart>(json);
            chart.OnDeserialize();
            return chart;
        }

        public static string CalcSHA256Hash(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string ret = "";
                byte[] hashArray = sha256.ComputeHash(data);
                foreach (byte b in hashArray)
                {
                    ret += $"{b:x2}";
                }
                return ret;
            }
        }

        public static void SaveChartData(string path, string json)
        {
            StreamWriter writer = new StreamWriter(path);
            writer.WriteLine(json);
            writer.Close();
        }
    }
}
