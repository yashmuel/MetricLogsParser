using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MetricLogsParser
{
    class Program
    {
        private static List<Tuple<string, string>> BadRequestsCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> SubscriptionNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ResourceNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ArmResourceNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> TimeAggregationCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ResourceGroupNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ArmTimeoutCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> InternalServerErrorCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> UnclassifiedColloction = new List<Tuple<string, string>>();
        private static int _numberOfSuccess = 0;
        private static int _numberOfFailures = 0;
        private static readonly string ResultsFilePath = @"C:\Users\yashmuel\Projects\MetricLogFiles\output\results4.txt";
        private static readonly string InputFolderPath = @"C:\Users\yashmuel\Projects\MetricLogFiles\input1";


        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ParseFiles();
            WriteResults();
        }

        public static void WriteResults()
        {
            //cleanup the file
            System.IO.File.WriteAllText(ResultsFilePath, string.Empty);
            
            //write results to file
            int total = InternalServerErrorCollection.Count + BadRequestsCollection.Count;
            List<string> results = new[] { "Results:", "Number of Successes: " + _numberOfSuccess, "Number of failures: " + _numberOfFailures, "Number of classified failures: " + total,
                                        "Number of 500 - Internal Server Error: " + InternalServerErrorCollection.Count + "(" + Math.Round((double)InternalServerErrorCollection.Count/total * 100, 2) + "%)",
                                        "Number of Unclassified: " + UnclassifiedColloction.Count + "(" + Math.Round((double)UnclassifiedColloction.Count/total * 100, 2) + "%)",
                                        "Number of 400 - Bad Requests: " + BadRequestsCollection.Count + "(" + Math.Round((double)BadRequestsCollection.Count/total * 100, 2) + "%)",
                                        "       Includes: ",
                                        "       Number of Subscription Not Found: " + SubscriptionNotFoundCollection.Count + "(" + Math.Round((double)SubscriptionNotFoundCollection.Count/total * 100, 2) + "%)",
                                        "       Number of Resource Not Found: " + ResourceNotFoundCollection.Count + "(" + Math.Round((double)ResourceNotFoundCollection.Count/total * 100, 2) + "%)",
                                        "       Number of ArmResource - Action group Not Found: " + ArmResourceNotFoundCollection.Count + "(" + Math.Round((double)ArmResourceNotFoundCollection.Count/total * 100, 2) + "%)",
                                        "       Number of Time Aggregation: " + TimeAggregationCollection.Count + "(" + Math.Round((double)TimeAggregationCollection.Count/total * 100, 2) + "%)",
                                        "       Number of Resource Group Not Found: " + ResourceGroupNotFoundCollection.Count + "(" + Math.Round((double)ResourceGroupNotFoundCollection.Count/total * 100, 2) + "%)",
                                        "       Number of Arm timeout: " + ArmTimeoutCollection.Count + "(" + Math.Round((double)ArmTimeoutCollection.Count/total * 100, 2) + "%)",
                                        "\n\n500 : Internal Server Error"

            }.ToList();
            
            results.AddRange(InternalServerErrorCollection.Select(i => i.ToString()));
            //Unclassified
            results.Add("\n\nUnclassified list: ");
            results.AddRange(UnclassifiedColloction.Select(i => i.ToString()));

            //Bad requests
            results.Add("\n\nArmTimeout: ");
            results.AddRange(ArmTimeoutCollection.Select(i => i.ToString()));

            results.Add("\n\nResourceGroupNotFound: ");
            results.AddRange(ResourceGroupNotFoundCollection.Select(i => i.ToString()));

            results.Add("\n\nTimeAggregation: ");
            results.AddRange(TimeAggregationCollection.Select(i => i.ToString()));

            results.Add("\n\nArmResourceNotFound: ");
            results.AddRange(ArmResourceNotFoundCollection.Select(i => i.ToString()));

            results.Add("\n\nResourceNotFound: ");
            results.AddRange(ResourceNotFoundCollection.Select(i => i.ToString()));

            results.Add("\n\nSubscriptionNotFound: ");
            results.AddRange(SubscriptionNotFoundCollection.Select(i => i.ToString()));

            results.Add("\n\nRerun (Internal Server Error + Timeout): ");
            InternalServerErrorCollection.AddRange(ArmTimeoutCollection);
            results.AddRange(InternalServerErrorCollection.Select(i => i.Item1));



            System.IO.File.WriteAllLines(ResultsFilePath, results);

        }

        public static void ParseFiles()
        {
            string[] files = Directory.GetFiles(InputFolderPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                ParseSingleFile(file);
            }
        }

        public static void ParseSingleFile(string path)
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            var summeryLine = lines.FirstOrDefault(l => l.Contains("rules failed to updated"));
            if (summeryLine == null)
                return;
            Regex pattern = new Regex(@"(?<numOfSuccess>\d+) rules were updated succesffully, (?<numOfFailed>\d+) rules failed to updated.");
            Match match = pattern.Match(summeryLine);
            if (!match.Success)
            {
                throw new Exception();
            }
            int numOfSuccess = Int32.Parse((match.Groups["numOfSuccess"].Value));
            _numberOfSuccess += numOfSuccess;
            int numOfFailed = Int32.Parse(match.Groups["numOfFailed"].Value);
            _numberOfFailures += numOfFailed;
            var failedLines = new List<string>();
            int index = findIndexOfLine("Rules that failed:", lines);
            if (index == -1)
            {
                return;
            }
            for (int i = index +1; i <= index + numOfFailed && i< lines.Length; i++)
            {
                failedLines.Add(lines[i]);
            }
                

            var realFailedLines = new List<Tuple<string, string>>();
            foreach (var failedLine in failedLines)
            {
                var realFailedLine = lines.FirstOrDefault(line => line.Contains(failedLine));
                realFailedLines.Add(new Tuple<string, string>(failedLine, realFailedLine));
            }

            var badRequests = realFailedLines.Where(l => l.Item2.Contains("BadRequest")).ToList();
            BadRequestsCollection.AddRange(badRequests);

            ResourceNotFoundCollection.AddRange(badRequests.Where(l => l.Item2.Contains("under resource group")));
            SubscriptionNotFoundCollection.AddRange(badRequests.Where(l => l.Item2.Contains("SubscriptionNotFound"))); 
            ArmResourceNotFoundCollection.AddRange(badRequests.Where(l => l.Item2.Contains("Arm resource"))); 
            TimeAggregationCollection.AddRange(badRequests.Where(l => l.Item2.Contains("Time aggregation must be one of")));
            ResourceGroupNotFoundCollection.AddRange(badRequests.Where(l => l.Item2.Contains("Resource group") && l.Item2.Contains("could not be found")));
            ArmTimeoutCollection.AddRange(badRequests.Where(l => l.Item2.Contains("Arm request client timeout")));

            var internalServerErrors = realFailedLines.Where(l => l.Item2.Contains("InternalServerError") || l.Item2.Contains("Internal Server Error, error: The server encountered an internal error"));
            InternalServerErrorCollection.AddRange(internalServerErrors);
            UnclassifiedColloction.AddRange(realFailedLines.Where(l => !internalServerErrors.Contains(l) && !badRequests.Contains(l)));
        }
        public static int findIndexOfLine(string line, string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == line)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    
}
