using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nephos.Connector.Logging;
using log4net;
using log4net.Core;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            ILog log = LogManager.GetLogger(ComponentNames.PluginSage100);

            LogHelper.FilterEventLogAppender(log, Level.Debug, Level.Info);

            LogHelper.Error(log, "This is error 2", 10);

            LogHelper.FilterLogger(log, Level.Error);

            LogHelper.Debug(log, "This is debug 1", 12);

            LogHelper.FilterLogger(log, Level.Debug);

            LogHelper.Debug(log, "This is debug 2", 12);

            LogHelper.FilterEventLogAppender(log, Level.Debug, Level.Fatal);

            LogHelper.Error(log, "This is error 3", 18, new Exception("This blows!"));

            Console.ReadLine();
        }
    }
}
