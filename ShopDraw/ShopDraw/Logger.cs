using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShopDraw
{
    public class Logger
    {
        public static log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static List<object> _Archive;
        public static string LOGGER_LOCATION = "ShopDrawLog\\ShopDraw.log";
        public static RelayCommand showNotifyCmd { get; set; }
        public static void Setup()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            PatternLayout patternLayout = new PatternLayout
            {
                ConversionPattern = "%date [%thread] %-5level - %message%newline"
            };
            patternLayout.ActivateOptions();

            string AppFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            RollingFileAppender roller = new RollingFileAppender
            {
                AppendToFile = false,
                File = Path.Combine(AppFolder, LOGGER_LOCATION),
                Layout = patternLayout,
                MaxSizeRollBackups = 20,
                MaximumFileSize = "1GB",
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true
            };


            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            MemoryAppender memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            //hierarchy.Root.Level = Level.;
            hierarchy.Configured = true;
            //log4net.Config.BasicConfigurator.Configure(hierarchy);
        }
        /// <summary>
        /// Use to write exception
        /// </summary>
        /// <param name="obj"></param>
        public static void Fatal(object obj, bool isShowNotify = true)
        {
            log.Fatal(obj);
            if (isShowNotify)
            {
                ActiveNotify(obj);
            }
        }

        public static void ActiveNotify(object obj)
        {
            if (showNotifyCmd != null)
                showNotifyCmd.Execute(obj);
        }


        /// <summary>
        /// Use to show log and note
        /// </summary>
        /// <param name="obj"></param>
        public static void Infor(object obj)
        {
            log.Info(obj);
        }

        public static void CurrentMethod()
        {
            try
            {
                var st = new StackTrace();
                var sf = st.GetFrame(1);

                log.Info("Current Method: " + sf.GetMethod().Name);
            }
            catch (Exception ex)
            {
                Fatal(ex);
            }
        }

    }
}
