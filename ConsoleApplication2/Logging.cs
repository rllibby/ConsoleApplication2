/*
    Copyright © 2012-2013 Sage Software, Inc. All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Repository.Hierarchy;

/*
    Notes: 
    
    - Loading the log4net configuration file can be done in a number of different ways, but the easiest by far is to 
    specify the config file in the AssemblyInfo.cs file. For example:
  
    [assembly: log4net.Config.XmlConfigurator(ConfigFile="ProjectName.log4net", Watch=true)]
  
    If you plan on appending to the event log, the name of the logger will need to be registered as 
    a valid event source.
 
*/

namespace Nephos.Connector.Logging
{
    /// <summary>
    /// The ComponentNames class defines the logger names that are used throughout the connector
    /// </summary>
    public sealed class ComponentNames
    {
        /* Entity names which map to the logger namespaces */
        public const string Base  = "Nephos.Connector";
        public const string Host = "Nephos.Connector.Host";
        public const string UI = "Nephos.Connector.UI";
        public const string Cloud = "Nephos.Connector.Cloud";
        public const string CloudSData = "Nephos.Connector.Cloud.SData";
        public const string Plugin = "Nephos.Connector.Plugin";
        public const string PluginGeneric = "Nephos.Connector.Plugin.Generic";
        public const string PluginSage100 = "Nephos.Connector.Plugin.Sage100";
        public const string PluginSage300 = "Nephos.Connector.Plugin.Sage300";
    }

    /// <summary>
    /// Enumeration for defining the context storage level
    /// </summary>
    public enum ContextLevel
    {
        Global = 0,
        Thread = 1
    }

    /// <summary>
    /// The AndFilter class allows for AND'ing of multiple filter conditions, eg; LoggerMatchFilter and
    /// LevelMatchFilter. If the log event does not pass ALL of the AND'ed filters the log event will be denied.
    /// </summary>
    public class AndFilter : FilterSkeleton
    {
        private bool _AcceptOnMatch = false;
        private readonly IList<IFilter> _Filters = new List<IFilter>();

        /// <summary>
        /// Determines if the logging event passes all the defined filters
        /// </summary>
        /// <param name="loggingEvent">The logging event to evaluate</param>
        /// <returns>Returns Accept if the event can be processed by all filters, or Deny if it cannot</returns>
        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            // No logging event
            if (loggingEvent == null) 
            {
                // Allow it 
                return FilterDecision.Neutral;
            }

            // Iterate the filters
            foreach (IFilter filter in _Filters)
            {
                // Check for logger match so we can implement an EXACT match
                if (filter is LoggerMatchFilter)
                {
                    // Explicit logger name match
                    if (string.Compare((filter as LoggerMatchFilter).LoggerToMatch, loggingEvent.LoggerName) != 0)
                    {
                        // Deny the event
                        return FilterDecision.Deny;

                    }
                } else {          
                    // Normal filter built in logic
                    if (filter.Decide(loggingEvent) != FilterDecision.Accept)
                    {
                        // Anything other than accept is considered a deny
                        return FilterDecision.Deny;
                    }
                }
            }

            // The event has passed all filters
            return FilterDecision.Accept;
        }

        /// <summary>
        /// Returns the count of filters in the collection
        /// </summary>
        public int Count
        {
            get
            {
                return _Filters.Count;
            }
        }

        /// <summary>
        /// Returns the filter at the specified index
        /// </summary>
        /// <param name="index">Index of filter that is desired</param>
        /// <returns>A filter object</returns>
        public IFilter this[int index]
        {
            get
            {
                return _Filters[index];
            }
        }

        /// <summary>
        /// Allows for appending of filters to a collection when the property is set
        /// </summary>
        public IFilter Filter
        {
            set { _Filters.Add(value); }
        }

        /// <summary>
        /// Determines if the log event will be accpeted or denied on a filter match
        /// </summary>
        public bool AcceptOnMatch
        {
            get { return _AcceptOnMatch; }
            set { _AcceptOnMatch = value; }
        }
    }

    /// <summary>
    /// The LogHelper static class is used to simplify some of the log4net api processes; eg obtaining a reference to an in 
    /// memory logger, setting / clearing property contexts, etc.
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Get the in process memory appender
        /// </summary>
        /// <returns>The memory appender, or null if there is no appender</returns>
        private static MemoryAppender GetMemoryAppender()
        {
            // Get the default hierarchy for log4net
            Hierarchy hierarchy = (LogManager.GetRepository() as Hierarchy);

            // Locate the memory appender, if there is one
            return (MemoryAppender)hierarchy.Root.GetAppender("MemoryAppender");
        }

        /// <summary>
        /// Returns a list of LoggingEvent objects that match the desired logger name filter.
        /// </summary>
        /// <param name="loggerFilter">The logger name to filter on</param>
        /// <param name="exactMatch">True to match only the exact logger name, or false to perform a heirarcal (starts with) match</param>
        /// <returns>List of zero or more logging events</returns>
        public static List<LoggingEvent> GetEvents(string loggerFilter, bool exactMatch = false)
        {
            List<LoggingEvent> eventList = new List<LoggingEvent>();

            // Locate the memory appender, if there is one
            MemoryAppender appender = GetMemoryAppender();

            // Check the appender
            if (appender != null)
            {
                // Get events
                LoggingEvent[] events = appender.GetEvents();
                
                // Iterate the events
                foreach (LoggingEvent eventItem in events)
                {
                    // Check the event logger name; extactMatch means match exactly, otherwise use StartsWith
                    if (exactMatch ? (string.Compare(eventItem.LoggerName, loggerFilter) == 0) : eventItem.LoggerName.StartsWith(loggerFilter))
                    {
                        // Add event to the filtered list
                        eventList.Add(eventItem);
                    }
                }
             }

            // Return event list
            return eventList;
        }
        
        /// <summary>
        /// Sets the property "EventID" in the thread context. This value will be used when the appender
        /// generates a Windows event log entry.
        /// </summary>
        /// <param name="eventID">The event ID to write to the event log entry</param>
        public static void SetEventID(int eventID)
        {
            // Set thread context
            SetContext(ContextLevel.Thread, "EventID", eventID);
        }

        /// <summary>
        /// Clears the property "EventID" from the thread context.
        /// </summary>
        public static void ClearEventID()
        {
            // Clear the thread context
            ClearContext(ContextLevel.Thread, "EventID");
        }

        /// <summary>
        /// Sets a property name value pair in the desired logging context
        /// </summary>
        /// <param name="level">Defines the context level (global / thread) for the property storage</param>
        /// <param name="propertyName">The desired property name</param>
        /// <param name="propertyValue">The desired property value</param>
        /// <returns>The current property value for the specified context name, or null if not set</returns>
        public static object SetContext(ContextLevel level, string propertyName, object propertyValue)
        {
            object retValue = (level == ContextLevel.Global) ? GlobalContext.Properties[propertyName] : ThreadContext.Properties[propertyName];

            // Check the context level
            if (level == ContextLevel.Global)
            {
                // Set global context
                GlobalContext.Properties[propertyName] = propertyValue;
            }
            else
            {
                // Set the thread context
                ThreadContext.Properties[propertyName] = propertyValue;
            }

            // Return previous value for the context name (or null)
            return retValue;
        }

        /// <summary>
        /// Clears a property name value pair from the desired logging context
        /// </summary>
        /// <param name="level">Defines the context level (global / thread) for the property storage</param>
        /// <param name="propertyName">The desired property name</param>
        /// <returns>The current property value for the specified context name, or null if not set</returns>
        public static object ClearContext(ContextLevel level, string propertyName)
        {
            object retValue = (level == ContextLevel.Global) ? GlobalContext.Properties[propertyName] : ThreadContext.Properties[propertyName];

            // Check the context level
            if (level == ContextLevel.Global)
            {
                // Remove global context
                GlobalContext.Properties.Remove(propertyName);
            }
            else
            {
                // Remove thread context
                ThreadContext.Properties.Remove(propertyName);
            }

            // Return previous value for the context name (or null)
            return retValue;
        }

        /// <summary>
        /// Generates a Debug log event and seeds the thread context property EventID with the 
        /// specified value
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="message">The log message</param>
        /// <param name="eventID">The event id to set in the thread context</param>
        /// <param name="e">An optional exception object to pass to the log event</param>
        public static void Debug(ILog log, string message, int eventID, Exception e = null)
        {
            // Check to see if Debug is enabled for the log
            if (log.IsDebugEnabled)
            {
                // Set the event id
                SetEventID(eventID);
                // Check exception object
                if (e == null)
                {
                    // Log the message
                    log.Debug(message);
                }
                else
                {
                    // Log the message plus exception
                    log.Debug(message, e);
                }
                // Clear the event id
                ClearEventID();
            }
        }

        /// <summary>
        /// Generates an Info log event and seeds the thread context property EventID with the 
        /// specified value
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="message">The log message</param>
        /// <param name="eventID">The event id to set in the thread context</param>
        /// <param name="e">An optional exception object to pass to the log event</param>
        public static void Info(ILog log, string message, int eventID, Exception e = null)
        {
            // Check to see if Info is enabled for the log
            if (log.IsInfoEnabled)
            {
                // Set the event id
                SetEventID(eventID);
                // Check exception object
                if (e == null)
                {
                    // Log the message
                    log.Info(message);
                }
                else
                {
                    // Log the message plus exception
                    log.Info(message, e);
                }
                // Clear the event id
                ClearEventID();
            }
        }

        /// <summary>
        /// Generates a Warn log event and seeds the thread context property EventID with the 
        /// specified value
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="message">The log message</param>
        /// <param name="eventID">The event id to set in the thread context</param>
        /// <param name="e">An optional exception object to pass to the log event</param>
        public static void Warn(ILog log, string message, int eventID, Exception e = null)
        {
            // Check to see if Warn is enabled for the log
            if (log.IsWarnEnabled)
            {
                // Set the event id
                SetEventID(eventID);
                // Check exception object
                if (e == null)
                {
                    // Log the message
                    log.Warn(message);
                }
                else
                {
                    // Log the message plus exception
                    log.Warn(message, e);
                }
                // Clear the event id
                ClearEventID();
            }
        }

        /// <summary>
        /// Generates an Error log event and seeds the thread context property EventID with the 
        /// specified value
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="message">The log message</param>
        /// <param name="eventID">The event id to set in the thread context</param>
        /// <param name="e">An optional exception object to pass to the log event</param>
        public static void Error(ILog log, string message, int eventID, Exception e = null)
        {
            // Check to see if Error is enabled for the log
            if (log.IsErrorEnabled)
            {
                // Set the event id
                SetEventID(eventID);
                // Check exception object
                if (e == null)
                {
                    // Log the message
                    log.Error(message);
                }
                else
                {
                    // Log the message plus exception
                    log.Error(message, e);
                }
                // Clear the event id
                ClearEventID();
            }
        }

        /// <summary>
        /// Generates a Fatal log event and seeds the thread context property EventID with the 
        /// specified value
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="message">The log message</param>
        /// <param name="eventID">The event id to set in the thread context</param>
        /// <param name="e">An optional exception object to pass to the log event</param>
        public static void Fatal(ILog log, string message, int eventID, Exception e = null)
        {
            // Check to see if Fatal is enabled for the log
            if (log.IsFatalEnabled)
            {
                // Set the event id
                SetEventID(eventID);
                // Check exception object
                if (e == null)
                {
                    // Log the message
                    log.Fatal(message);
                }
                else
                {
                    // Log the message plus exception
                    log.Fatal(message, e);
                }
                // Clear the event id
                ClearEventID();
            }
        }

        /// <summary>
        /// A runtime mechanism for setting a logger's min and max filter level for events. 
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="minLevel">Minimum logging level. Any event with a level lower than this will not be logged to the event log</param>
        public static void FilterLogger(ILog log, log4net.Core.Level minLevel)
        {
            // Get the repository logger for the specified log
            Logger logger = (Logger)log.Logger.Repository.GetLogger(log.Logger.Name);

            // Set the min level for events
            logger.Level = minLevel;
        }

        /// <summary>
        /// A runtime mechanism for setting a logger's (event log) appender min and max filter level for events. 
        /// </summary>
        /// <param name="log">The log interface to act upon</param>
        /// <param name="minLevel">Minimum logging level. Any event with a level lower than this will not be logged to the event log</param>
        /// <param name="maxLevel">Maximum logging level. Any event with a level higher than this will not be logged to the event log</param></param>
        public static void FilterEventLogAppender(ILog log, log4net.Core.Level minLevel, log4net.Core.Level maxLevel)
        {
            // Get the appenders 
            IAppender[] appenders = log.Logger.Repository.GetAppenders();

            // Iterate the appenders
            foreach (IAppender appender in appenders)
            {
                // Check appender
                if (appender is EventLogAppender)
                {
                    // Get as event log appender
                    EventLogAppender eventLogAppender = (appender as EventLogAppender);
                    // Check the application log name against the logger name
                    if (string.Compare(eventLogAppender.ApplicationName, log.Logger.Name) == 0) {
                        // We have the correct appender, now get the filter head
                        if ((eventLogAppender.FilterHead != null) && (eventLogAppender.FilterHead is AndFilter))
                        {
                            // Get the AndFilter
                            AndFilter andFilter = (eventLogAppender.FilterHead as AndFilter);
                            // Walk the filters in the AND 
                            for (int i = 0; i < andFilter.Count; i++)
                            {
                                // Check the filter
                                if (andFilter[i] is LevelRangeFilter)
                                {
                                    // Get the range filter
                                    LevelRangeFilter rangeFilter = (andFilter[i] as LevelRangeFilter);
                                    // Update the min and max level
                                    rangeFilter.LevelMin = minLevel;
                                    rangeFilter.LevelMax = maxLevel;
                                    // Done
                                    break;
                                }
                            }
                        }
                        // Done processing appenders
                        break;
                    }
                }
            }
        }

    }
}
