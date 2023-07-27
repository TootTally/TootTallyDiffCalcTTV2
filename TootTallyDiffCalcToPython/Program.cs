using System.Diagnostics;
using System.IO;

namespace TootTallyDiffCalcTTV2
{
    public class TootTallyDiffCalcTTV2
    {
        #region hellooffbeatwitch
        public static List<Chart> chartList;
        public static void Main()
        {
            while (true)
            {
                Console.Write("Input tmb file name or tmbs directory: ");
                var path = @"" + Console.ReadLine();
                if (File.Exists(path))
                {
                    OutputSingleChart(path, true);
                }
                else if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path);
                    chartList = new List<Chart>(files.Length);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    Parallel.ForEach(files, f =>
                    {
                        chartList.Add(ProcessChart(f));
                    });
                    stopwatch.Stop();
                    Console.WriteLine($"Total calculation time took: {stopwatch.Elapsed.TotalSeconds}s for {chartList.Count} charts and {chartList.Count * 7} diffs");
                } 
                else
                    Console.WriteLine($"Chart {path} couldn't be found");
            }
        }

        public static void OutputSingleChart(string path, bool writeToConsole)
        {
            Chart chart = ChartReader.LoadChart(path);
            if (writeToConsole)
            {
                Console.WriteLine($"{chart.name} processed in {chart.calculationTime.TotalSeconds}s");
                Console.WriteLine("=====================================================================================================");
                for (int i = 0; i < 7; i++)
                    DisplayAtSpeed(chart, i);
                Console.WriteLine("=====================================================================================================");
            }
            
        }
        #endregion

        public static Chart ProcessChart(string path) => ChartReader.LoadChart(path);

        public static Chart ProcessChartJson(string json) => ChartReader.LoadChartFromJson(json);

        public static string GetProcessingTime(string directory)
        {
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                var tempList = new List<Chart>(files.Length);

                Stopwatch stopwatch = new();
                stopwatch.Start();
                Parallel.ForEach(files, f =>
                {
                    tempList.Add(ProcessChart(f));
                });
                stopwatch.Stop();
                return $"Total calculation time took: {stopwatch.Elapsed.TotalSeconds}s for {tempList.Count} charts and {tempList.Count * 7} diffs";
            }
            return "Directory couldn't be found";
        }

        public static void DisplayAtSpeed(Chart chart, int speedIndex)
        {
            ChartPerformances.DataVectorAnalytics aimAnalytics = chart.performances.aimAnalyticsDict[speedIndex];
            ChartPerformances.DataVectorAnalytics tapAnalytics = chart.performances.tapAnalyticsDict[speedIndex];
            ChartPerformances.DataVectorAnalytics accAnalytics = chart.performances.accAnalyticsDict[speedIndex];
            Console.WriteLine($"SPEED: {chart.GAME_SPEED[speedIndex]:0.00}x rated {chart.GetStarRating(speedIndex):0.0000}");
            Console.WriteLine($"  aim: {aimAnalytics.perfWeightedAverage:0.0000} min: {aimAnalytics.perfMin:0.0000} max: {aimAnalytics.perfMax:0.0000}");
            Console.WriteLine($"  tap: {tapAnalytics.perfWeightedAverage:0.0000} min: {tapAnalytics.perfMin:0.0000} max: {tapAnalytics.perfMax:0.0000}");
            Console.WriteLine($"  acc: {accAnalytics.perfWeightedAverage:0.0000} min: {accAnalytics.perfMin:0.0000} max: {accAnalytics.perfMax:0.0000}");
            Console.WriteLine("--------------------------------------------");
        }

    }
}