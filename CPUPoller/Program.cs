using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.Web;

namespace CPUPoller
{
    // ==============================================================================================================
    class Options
    {
        [Option('i', "interval",DefaultValue=1000,HelpText="polling interval in ms")]
        public int pollingInterval { get; set; }

        [Option('a',"action",Required=true,HelpText="action - display/ send")]
        public string action { get; set; }

        [Option('c',"cep",Required=true,HelpText="name of CEP machine")]
        public string cep { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                                      (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    // ==============================================================================================================
    class CPUPoller
    {
        private PerformanceCounter performanceCounter;
        private string URLBase;

        public CPUPoller(string cepName)
        {
            var processorCategory = PerformanceCounterCategory.GetCategories()
                .FirstOrDefault(cat => cat.CategoryName == "Processor");
            var countersInCategory = processorCategory.GetCounters("_Total");
            performanceCounter = countersInCategory.First(cnt => cnt.CounterName == "% Processor Time");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("http://{0}:80/CEP/Input/PushData?", cepName);
            sb.AppendFormat("source={0}", "CPUPoller");
            sb.AppendFormat("&ci={0}", System.Environment.MachineName);
            sb.AppendFormat("&kpi={0}", "CPU");

            URLBase = sb.ToString();
        }

        public void DisplayCounter()
        {
            Console.WriteLine("{0}\t{1} = {2}",
                performanceCounter.CategoryName, performanceCounter.CounterName, performanceCounter.NextValue());
        }

        // http://localhost:8080/CEP/Input/PushData?val=15.649917%0D%0A&datetime=2013-03-22+10%3A27%3A56.251838&kpi=CPU&ci=mlsi&source=Nagios
        public void SendCounter()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(URLBase);
            sb.AppendFormat("&datetime={0}", HttpUtility.UrlEncode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")));
            sb.AppendFormat("&val={0}", performanceCounter.NextValue().ToString());

            Console.WriteLine("URL is {0}", sb.ToString());

            WebRequest request = WebRequest.Create(sb.ToString());
            request.Timeout = 30;
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    Console.WriteLine(response.ToString());
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("[CPUPoller.SendCounter] caught exception {0}", e.Message);
            }
        }
    }

    // ==============================================================================================================
    public class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                CPUPoller cpuPoller = new CPUPoller(options.cep);

                if (options.action == "display")
                {
                    while (!Console.KeyAvailable)
                    {
                        cpuPoller.DisplayCounter(); System.Threading.Thread.Sleep(options.pollingInterval);
                    }
                }
                else
                {
                    while (!Console.KeyAvailable)
                    {
                        cpuPoller.SendCounter(); System.Threading.Thread.Sleep(options.pollingInterval);
                    }
                }
            }
        }
    }
}
