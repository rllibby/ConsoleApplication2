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
            ILog log = LogManager.GetLogger(ComponentNames.Plugin);

            /*
             
                Single bug fix example:
                LogHelper.FilterEventLogAppender(log, Level.Debug, Level.Info);
            */

            LogHelper.Error(log, "This is error 2", 10);


            LogHelper.FilterLogger(log, Level.Error);

            LogHelper.Debug(log, "This is debug 1", 12);

            LogHelper.FilterLogger(log, Level.Debug);

            LogHelper.Debug(log, "This is debug 2", 12);

            LogHelper.FilterEventLogAppender(log, Level.Warn, Level.Fatal);

            LogHelper.Debug(log, "This is debug x", 18, new Exception("Testing"));
            LogHelper.Info(log, "This is info x", 18, new Exception("Testing"));
            LogHelper.Warn(log, "This is warn x", 18, new Exception("Testing"));
            LogHelper.Error(log, "This is error x", 18, new Exception("Testing"));
            LogHelper.Fatal(log, "This is fatal x", 18, new Exception("Testing"));

            Console.ReadLine();
        }
    }
}
