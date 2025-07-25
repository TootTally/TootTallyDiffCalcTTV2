﻿using System.Diagnostics;

namespace TootTallyDiffCalcTTV2
{
    public class TootTallyDiffCalcTTV2
    {
        public static List<Chart> chartList;
        public const string VERSION_LABEL = "5.2.4";
        public const string EXTRA = "1.0.0";
        public const string BUILD_DATE = "21072025";
        public static StreamWriter fileWriter;

        public static void Main()
        {
            while (true)
            {
                Console.Write("Input tmb file name or tmbs directory: ");
                var path = @"" + Console.ReadLine();
                FileStream file = new FileStream($"Output/{DateTime.Now:yyyyMMddHHmmss}.txt", FileMode.OpenOrCreate);
                fileWriter = new StreamWriter(file);

                Console.Clear();
                if (File.Exists(path))
                {
                    if (path.Contains(".ttr"))
                        TestReplayConvertion(path);
                    else
                        OutputSingleChart(path, true);
                }
                else if (Directory.Exists(path))
                {
                    var files = GetAllTmbsPaths(path);
                    chartList = new List<Chart>(files.Count);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    Parallel.ForEach(files, f =>
                    {
                        chartList.Add(ProcessChart(f));
                    });
                    stopwatch.Stop();
                    WriteToConsoleAndFile($"Total calculation time took: {stopwatch.Elapsed.TotalSeconds}s for {chartList.Count} charts and {chartList.Count * 7} diffs");
                    for (int i = 0; i < chartList.Count; i++)
                    {
                        //OutputErrors(chartList[i]);
                    }
                    Console.ForegroundColor = ConsoleColor.Red;
                    WriteToConsoleAndFile("=================================UNRATABLE CHARTS=================================");
                    foreach (var chart in chartList.Where(chart => chart.ratingErrors.Any(error => error.errorLevel == RatingCriterias.ErrorLevel.Error && error.errorType != RatingCriterias.ErrorType.HotStart && error.errorType == RatingCriterias.ErrorType.GhostNote)))
                        WriteToConsoleAndFile($"{chart.name} - {chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Error).Count()} errors");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                    WriteToConsoleAndFile($"Chart {path} couldn't be found");
                fileWriter.Close();
            }
        }

        public static List<string> GetAllTmbsPaths(string path)
        {
            List<string> paths = new List<string>();
            paths.AddRange(Directory.GetFiles(path).Where(s => s.EndsWith(".tmb")));
            foreach (string directory in Directory.GetDirectories(path))
                paths.AddRange(Directory.GetFiles(directory).Where(s => s.EndsWith(".tmb")));
            return paths;
        }

        public static void OutputErrors(Chart chart)
        {
            if (chart.ratingErrors.Count > 0)
            {
                var errorCount = chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Error).Count();
                var warningCount = chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Warning).Count();
                Console.ForegroundColor = errorCount > 0 ? ConsoleColor.Red : ConsoleColor.White;
                WriteToConsoleAndFile($"------------------------------------------------------------------------");
                WriteToConsoleAndFile($"{chart.shortName} has {errorCount} errors and {warningCount} warnings.");
                if (errorCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    WriteToConsoleAndFile($"=================================ERRORS=================================");
                    foreach (var error in chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Error))
                        WriteToConsoleAndFile($"{error.errorType} - #{error.noteID} - {error.timing}s - {error.value}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    WriteToConsoleAndFile($"");
                    WriteToConsoleAndFile($"============================No errors found=============================");
                    WriteToConsoleAndFile($"");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                if (warningCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    WriteToConsoleAndFile($"================================WARNINGS================================");
                    foreach (var error in chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Warning))
                        WriteToConsoleAndFile($"{error.errorType} - #{error.noteID} - {error.timing}s - {error.value}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    WriteToConsoleAndFile($"===========================No Warnings found============================");
                    WriteToConsoleAndFile($"");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.ForegroundColor = ConsoleColor.White;
                WriteToConsoleAndFile($"------------------------------------------------------------------------");
                WriteToConsoleAndFile($"");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                WriteToConsoleAndFile($"------------------------------------------------------------------------");
                WriteToConsoleAndFile($"{chart.shortName} has no errors and no warnings.");
                WriteToConsoleAndFile($"------------------------------------------------------------------------");
                WriteToConsoleAndFile($"");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void OutputSingleChart(string path, bool writeToConsole)
        {
            Chart chart = ChartReader.LoadChart(path);
            if (writeToConsole)
            {
                WriteToConsoleAndFile($"{chart.name} processed in {chart.calculationTime.TotalSeconds}s");
                WriteToConsoleAndFile($"Criterias processed in {chart.criteriaCalculationTime.TotalSeconds}s");
                WriteToConsoleAndFile($"MaxScore: {chart.maxScore}");
                WriteToConsoleAndFile($"GameMaxScore: {chart.gameMaxScore}");
                WriteToConsoleAndFile($"NoteCount: {chart.noteCount}");
                WriteToConsoleAndFile($"TestScore:{chart.GetDynamicDiffRating(1, (int)(chart.noteCount * .8f))}");
                WriteToConsoleAndFile($"TestScore2:{chart.GetDynamicDiffRating(1, (int)(chart.noteCount * .3f))}");
                WriteToConsoleAndFile($"TestScore3:{Utils.CalculateScoreTT(chart, 1f, (int)(chart.noteCount * .8f), .8f)}");
                WriteToConsoleAndFile($"TestScore4:{Utils.CalculateScoreTT(chart, 1f, (int)(chart.noteCount * .3f), .3f)}");
                WriteToConsoleAndFile("=====================================================================================================");
                for (int i = 0; i < 7; i++)
                    DisplayAtSpeed(chart, i);
                WriteToConsoleAndFile("=====================================================================================================");
                WriteToConsoleAndFile("");
                OutputErrors(chart);

                //Trace.WriteLine(File.ReadAllText(path));
                //Trace.WriteLine(ChartReader.CalcSHA256Hash(Encoding.UTF8.GetBytes(File.ReadAllText(path))));
            }

        }

        public static void TestReplayConvertion(string path)
        {
            ReplayData replay = ChartReader.LoadReplay(path);
            WriteToConsoleAndFile($"Replay version: {replay.version}");
            WriteToConsoleAndFile($"Replay score: {replay.finalscore}");
            Chart chart = null;
            var tmbpath = "";
            while (chart == null || chart.name != replay.song)
            {
                Console.Write($"Input path for {replay.song}: ");
                tmbpath = @"" + Console.ReadLine();
                if (File.Exists(tmbpath))
                    try
                    {
                        chart = ChartReader.LoadChart(tmbpath);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Chart not found.");
                        chart = null;
                    }
            }
            Console.WriteLine("Converting replay...");
            var convertedReplay = chart.TryConvertReplay(replay);
            Console.WriteLine($"Converted replay version: {convertedReplay.version}");
            Console.WriteLine($"Converted replay score: {convertedReplay.finalscore}");
            Console.WriteLine($"Converted replay accuracy: {convertedReplay.finalscore / chart.maxScore:P2}");
            File.WriteAllText(path.Replace(".ttr", "-Converted.ttr"), convertedReplay.GetJsonString());
        }

        public static Chart ProcessChart(string path) => ChartReader.LoadChart(path);

        public static Chart ProcessChartJson(string json) => ChartReader.LoadChartFromJson(json);

        public static string GetProcessingTime(string directory)
        {
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                var tempList = new List<Chart>(files.Length);

                Stopwatch stopwatch = new Stopwatch();
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
            WriteToConsoleAndFile($"SPEED: {Utils.GAME_SPEED[speedIndex]:0.00}x rated {chart.GetStarRating(Utils.GAME_SPEED[speedIndex]):0.0000}");
            WriteToConsoleAndFile($"  aim: {aimAnalytics.perfWeightedAverage:0.0000} max: {aimAnalytics.perfMax:0.0000}");
            WriteToConsoleAndFile($"  tap: {tapAnalytics.perfWeightedAverage:0.0000} max: {tapAnalytics.perfMax:0.0000}");
            WriteToConsoleAndFile($"  acc: {accAnalytics.perfWeightedAverage:0.0000} max: {accAnalytics.perfMax:0.0000}");
            WriteToConsoleAndFile("--------------------------------------------");
        }

        public static void WriteToConsoleAndFile(string text)
        {
            Console.WriteLine(text);
            fileWriter.WriteLine(text);
        }

        public static void ForceGC()
        {
            for (int i = 0; i < 3; i++)
                GC.Collect(i, GCCollectionMode.Forced);
        }

    }
}