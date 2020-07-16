using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;


namespace MetricLogsParser
{
    class Program
    {
        private static List<Tuple<string, string>> BadRequestsCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> SubscriptionNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ResourceNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> AlertRuleNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ArmResourceNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> TimeAggregationCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ResourceGroupNotFoundCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> ArmTimeoutCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> InternalServerErrorCollection = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> UnclassifiedColloction = new List<Tuple<string, string>>();
        private static int _numberOfSuccess = 0;
        private static int _numberOfFailures = 0;
        //private static readonly string ResultsFilePath = @"C:\Users\yashmuel\Projects\MetricLogFiles\output\diff.txt";
        private static readonly string ResultsFilePath = @"C:\Users\yashmuel\Projects\MetricLogFiles\output\results25.txt";
        private static readonly string InputFolderPath = @"C:\Users\yashmuel\Projects\MetricLogFiles\input1";
        private static List<string[]> _monitors = new List<string[]>();
        private static List<string> allLines;


        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //InitMonitorsList();
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
                                        "Number of Alert rule Not Found: " + AlertRuleNotFoundCollection.Count + "(" + Math.Round((double)AlertRuleNotFoundCollection.Count/total * 100, 2) + "%)",
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

            results.Add("\n\nAlertNotFound: ");
            results.AddRange(AlertRuleNotFoundCollection.Select(i =>
            {
                string monitorId = GetMonitorId(i.Item1);
                //string location = GetMonitorLocation(monitorId);
                //return location + ", " + monitorId;
                return monitorId;
            }));

            results.Add("\n\nSubscriptionNotFound: ");
            results.AddRange(SubscriptionNotFoundCollection.Select(i => i.ToString()));

            results.Add("\n\nRerun (Internal Server Error + Timeout): ");
            InternalServerErrorCollection.AddRange(ArmTimeoutCollection);
            InternalServerErrorCollection.AddRange(UnclassifiedColloction);
            var x = InternalServerErrorCollection.Distinct();
            results.AddRange(x.Select(i => i.Item1));

            results.Add("\n\nDelete (Resource/RG/Sub Not Found): ");
            ResourceGroupNotFoundCollection.AddRange(ResourceNotFoundCollection);
            ResourceGroupNotFoundCollection.AddRange(SubscriptionNotFoundCollection);
            var y = ResourceGroupNotFoundCollection.Distinct();
            results.AddRange(y.Select(i => i.Item1));

            //results.Add("\n\nSuccess!!!: ");
            //results.AddRange(allLines);

            System.IO.File.WriteAllLines(ResultsFilePath, results);

        }

        public static void ParseFiles()
        {
            string[] files = Directory.GetFiles(InputFolderPath, "*", SearchOption.AllDirectories);
          /*  string inputFolderPath = @"C:\Users\yashmuel\Projects\MetricLogFiles\diff";
            string[] files2 = Directory.GetFiles(inputFolderPath, "*", SearchOption.AllDirectories);
            string[] lines = System.IO.File.ReadAllLines(files2[0]);
            var lines6 = lines.OrderBy(l => l.ToString());
            
            string[] lines2 = System.IO.File.ReadAllLines(files2[1]);
            var lines7 = lines2.OrderBy(l => l.ToString());

            Dictionary<string, string> dictionary = lines.ToDictionary(item => item,
                item => item);
            Dictionary<string, string> dictionary2 = lines2.ToDictionary(item => item,
                item => item);

            System.IO.File.WriteAllLines(ResultsFilePath, lines6);
            //System.IO.File.WriteAllLines(ResultsFilePath, lines7);

            foreach (KeyValuePair<string, string> VARIABLE in dictionary)
            {
                var y = dictionary2.ContainsKey(VARIABLE.Key);
                var k = dictionary2.ContainsKey(
                    "/subscriptions/24d982bc-43e1-4e58-a537-abb3fc74d1c7/resourcegroups/weeu-s03-tnd-rsg-mntr-01/providers/microsoft.insights/metricalerts/dtu%20percentage");
                var j = dictionary2.ContainsKey(
                    "/subscriptions/adcb4650-a703-4ce2-a17d-2b394ad6f835/resourcegroups/rg-sql-Adv365-Alerts/providers/Microsoft.Insights/metricAlerts/Az-SQL-DB-blocked-by-firewall-sql-server-testlab-sql-chkprm-datamart");
                var l = j == y;
                var x = dictionary2.Remove(VARIABLE.Key);
            }
            var lines3 = dictionary2.Where(l => !dictionary.Contains(l));

*/
            foreach (var file in files)
            {
                ParseSingleFile(file);
            }
        }

        public static void ParseSingleFile(string path)
        {
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            string[] lines2 = System.IO.File.ReadAllLines(path);


            var lst = System.IO.File.ReadAllLines(@"C:\Users\yashmuel\Projects\MetricLogFiles\output\allLines.txt").ToList();
            
            foreach (var line in lines2)
            {
                if (line.Contains("Getting alert rule from uri:") || line.Contains("Error: Can't find alert rule"))
                {
                    lines.Remove(line);
                }
            }

            string[] lines3 = lines.ToArray();

            var summeryLine = lines3.FirstOrDefault(l => l.Contains("rules failed to updated"));
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
            int index = findIndexOfLine("Rules that failed:", lines3);
            if (index == -1)
            {
                return;
            }
            for (int i = index +1; i <= index + numOfFailed && i< lines3.Length; i++)
            {
                failedLines.Add(lines3[i]);
            }
                

            var realFailedLines = new List<Tuple<string, string>>();
            foreach (var failedLine in failedLines)
            {
                var realFailedLine = lines3.FirstOrDefault(line => line.Contains(failedLine));
                realFailedLines.Add(new Tuple<string, string>(failedLine, realFailedLine));
            }
            List<string> allLines = lst.Select(ruleId =>
            {
                string ruleArmId = ruleId.Substring(ruleId.IndexOf("/"));
                return ruleArmId;
            }).ToList();
            allLines.RemoveAll(item => failedLines.Contains(item));
            allLines = allLines.Distinct().ToList();
            var s = failedLines.Distinct().ToList();
            var a = lst.Distinct().ToList();
            var badRequests = realFailedLines.Where(l => l.Item2.Contains("BadRequest")).ToList();
            BadRequestsCollection.AddRange(badRequests);

            var resourceNotFound = realFailedLines.Where(l => l.Item2.Contains("ResourceNotFound")).ToList();

            ResourceNotFoundCollection.AddRange(resourceNotFound.Where(l => l.Item2.Contains("under resource group")));
            SubscriptionNotFoundCollection.AddRange(resourceNotFound.Where(l => l.Item2.Contains("SubscriptionNotFound"))); 
            ArmResourceNotFoundCollection.AddRange(resourceNotFound.Where(l => l.Item2.Contains("Arm resource"))); 
            TimeAggregationCollection.AddRange(badRequests.Where(l => l.Item2.Contains("Time aggregation must be one of")));
            ResourceGroupNotFoundCollection.AddRange(resourceNotFound.Where(l => l.Item2.Contains("Resource group") && l.Item2.Contains("could not be found")));
            ArmTimeoutCollection.AddRange(badRequests.Where(l => l.Item2.Contains("Arm request client timeout")));

            var internalServerErrors = realFailedLines.Where(l => l.Item2.Contains("InternalServerError") || l.Item2.Contains("Internal Server Error, error: The server encountered an internal error"));
            InternalServerErrorCollection.AddRange(internalServerErrors);
            AlertRuleNotFoundCollection.AddRange(realFailedLines.Where(l => l.Item2.Contains("Alert rule ") && l.Item2.Contains("not found")));
            UnclassifiedColloction.AddRange(realFailedLines.Where(l => !internalServerErrors.Contains(l) && !badRequests.Contains(l) && !AlertRuleNotFoundCollection.Contains(l) && !resourceNotFound.Contains(l)));
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

        public static string GetMonitorId(string ruleId)
        {
            string[] words = ruleId.Split('/');
            /*string monitorName = words[8].Replace("%20", " ", StringComparison.CurrentCulture);
            monitorName = monitorName.Replace("%5B", "[", StringComparison.InvariantCultureIgnoreCase);
            monitorName = monitorName.Replace("%5D", "]", StringComparison.InvariantCultureIgnoreCase);*/
            string monitorName = WebUtility.UrlDecode(words[8]);
            string monitorId = words[2] + '/' + words[4] + '/' + monitorName;
            return monitorId;
        }

        public static string GetMonitorLocation(string monitorId)
        {
            var monitor = _monitors.FirstOrDefault(m => m[3] == monitorId);
            return monitor[0] + '/' + monitor[1] + '/' + monitor[2];
        }

        public static void InitMonitorsList()
        {
            string inputFilePath = @"C:\Users\yashmuel\Projects\MetricLogFiles\Health-11-07-20\monitor3.csv";
            int i = 0;
            List<string> lines = new List<string>();
            using (StreamReader sr = new StreamReader(inputFilePath))
            {
                string currentLine;
                // currentLine will be null when the StreamReader reaches the end of file
                while ((currentLine = sr.ReadLine()) != null)
                {
                    i++;
                    string[] arr = currentLine.Split(',');
                    _monitors.Add(arr);
                }
            }
        }
    }

    
}
