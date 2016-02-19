using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SinzkPeek
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Directory.Exists("down"))
                Directory.CreateDirectory("down");
            var oldlogs = new HashSet<string>();
            if (File.Exists("old.log"))
            {
                Console.WriteLine("Continue from last session.");
                using (var sr = new StreamReader("old.log"))
                    while (!sr.EndOfStream)
                        oldlogs.Add(sr.ReadLine());
            }

            Console.WriteLine("Downloading list of logs...");
            var logs = new List<string>();
            using (var wc = new WebClient())
            using (var ns = wc.OpenRead("http://api.sinzk.com/cloudmc/Log/"))
            using (var sr = new StreamReader(ns))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var match = Regex.Match(line, "\\d{8}\\.txt");
                    if (match.Success && oldlogs.Contains(match.Value) == false)
                    {
                        logs.Add(match.Value);
                        Console.WriteLine("Found new log " + match.Value);
                    }
                }
            }

            foreach (var log in logs)
            {
                Console.WriteLine("Analysing " + log);
                var date = log.Replace(".txt", "");
                if (!Directory.Exists("down/" + date))
                    Directory.CreateDirectory("down/" + date);
                using (var wc = new WebClient())
                using (var ns = wc.OpenRead("http://api.sinzk.com/cloudmc/Log/" + log))
                using (var sr = new StreamReader(ns, Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        var matches = Regex.Matches(line, "http:\\\\/\\\\/www.sinzk.com\\\\/\\\\/Upload\\\\/[^\"]+");
                        foreach (Match match in matches)
                        {
                            var match2 = Regex.Match(match.Value, "/[^/]+$");
                            if (match2.Success)
                            {
                                var fn = match2.Value.Substring(1);
                                if (File.Exists("down/" + date + "/" + fn) && new FileInfo("down/" + date + "/" + fn).Length > 0) continue;
                                Console.Write("Downloading " + fn + " ... ");
                                try
                                {
                                    using (var wc2 = new WebClient())
                                        wc2.DownloadFile(match.Value.Replace("\\", ""), "down/" + date + "/" + fn);
                                    Console.WriteLine("Success");
                                }
                                catch (Exception e)
                                {
                                    try
                                    {
                                        File.Delete("down/" + date + "/" + fn);
                                    }
                                    catch (Exception) { }
                                    Console.WriteLine(e.Message);
                                }
                            }
                        }
                    }
                    using (var sw = new StreamWriter("old.log", true))
                        sw.WriteLine(log);
                }
            }
            Console.WriteLine("All completed");
        }
    }
}
