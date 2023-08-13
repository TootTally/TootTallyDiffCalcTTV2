using System.Diagnostics;
using System.Globalization;

namespace TootTallyDiffCalcTTV2
{
    public class TootTallyDiffCalcTTV2
    {
        #region hellooffbeatwitch
        public static List<Chart> chartList;
        public const string VERSION_LABEL = "2.1.0";
        public const string BUILD_DATE = "08122023";
        public static CultureInfo ci;

        public static void Main()
        {
            ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            while (true)
            {
                Console.Write("Input tmb file name or tmbs directory: " + AppDomain.CurrentDomain.BaseDirectory);
                var path = @"" + AppDomain.CurrentDomain.BaseDirectory + Console.ReadLine();
                Console.Clear();
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
                        if (f.Contains(".tmb"))
                            chartList.Add(ProcessChart(f));
                    });
                    stopwatch.Stop();
                    Console.WriteLine($"Total calculation time took: {stopwatch.Elapsed.TotalSeconds}s for {chartList.Count} charts and {chartList.Count * 7} diffs");
                    for (int i = 0; i < chartList.Count; i++)
                    {
                        OutputErrors(chartList[i]);
                    }
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("=================================UNRATABLE CHARTS=================================");
                    foreach (var chart in chartList.Where(chart => chart.ratingErrors.Any(error => error.errorLevel == RatingCriterias.ErrorLevel.Error && error.errorType == RatingCriterias.ErrorType.Spacing)))
                        Console.WriteLine($"{chart.name} - {chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Error).Count()} errors");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                    Console.WriteLine($"Chart {path} couldn't be found");
            }
        }

        public static void OutputErrors(Chart chart)
        {
            if (chart.ratingErrors.Count > 0)
            {
                var errorCount = chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Error).Count();
                var warningCount = chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Warning).Count();
                Console.ForegroundColor = errorCount > 0 ? ConsoleColor.Red : ConsoleColor.White;
                Console.WriteLine($"------------------------------------------------------------------------");
                Console.WriteLine($"{chart.shortName} has {errorCount} errors and {warningCount} warnings.");
                if (errorCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"=================================ERRORS=================================");
                    foreach (var error in chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Error))
                        Console.WriteLine($"{error.errorType} - #{error.noteID} - {error.timing}s - {error.value}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"");
                    Console.WriteLine($"============================No errors found=============================");
                    Console.WriteLine($"");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                if (warningCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"================================WARNINGS================================");
                    foreach (var error in chart.ratingErrors.Where(error => error.errorLevel == RatingCriterias.ErrorLevel.Warning))
                        Console.WriteLine($"{error.errorType} - #{error.noteID} - {error.timing}s - {error.value}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"===========================No Warnings found============================");
                    Console.WriteLine($"");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"------------------------------------------------------------------------");
                Console.WriteLine($"");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"------------------------------------------------------------------------");
                Console.WriteLine($"{chart.shortName} has no errors and no warnings.");
                    Console.WriteLine($"------------------------------------------------------------------------");
                Console.WriteLine($"");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void OutputSingleChart(string path, bool writeToConsole)
        {
            Chart chart = ChartReader.LoadChart(path);
            if (writeToConsole)
            {
                Console.WriteLine($"{chart.name} processed in {chart.calculationTime.TotalSeconds}s");
                Console.WriteLine($"MaxScore: {chart.maxScore}");
                Console.WriteLine($"GameMaxScore: {chart.gameMaxScore}");
                Console.WriteLine("=====================================================================================================");
                for (int i = 0; i < 7; i++)
                    DisplayAtSpeed(chart, i);
                Console.WriteLine("=====================================================================================================");
                Console.WriteLine("");
                OutputErrors(chart);

                //Trace.WriteLine(File.ReadAllText(path));
                //Trace.WriteLine(ChartReader.CalcSHA256Hash(Encoding.UTF8.GetBytes(File.ReadAllText(path))));
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
            Console.WriteLine($"SPEED: {Utils.GAME_SPEED[speedIndex]:0.00}x rated {chart.GetStarRating(Utils.GAME_SPEED[speedIndex]):0.0000}");
            Console.WriteLine($"  aim: {aimAnalytics.perfWeightedAverage:0.0000} min: {aimAnalytics.perfMin:0.0000} max: {aimAnalytics.perfMax:0.0000}");
            Console.WriteLine($"  tap: {tapAnalytics.perfWeightedAverage:0.0000} min: {tapAnalytics.perfMin:0.0000} max: {tapAnalytics.perfMax:0.0000}");
            Console.WriteLine($"  acc: {accAnalytics.perfWeightedAverage:0.0000} min: {accAnalytics.perfMin:0.0000} max: {accAnalytics.perfMax:0.0000}");
            Console.WriteLine("--------------------------------------------");
        }

    }
}