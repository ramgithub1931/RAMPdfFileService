using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RAMPdfFileService
{
    public partial class RAMPdfFileService : ServiceBase
    {
        private DirectoryWatcher _directoryWatcher;

        public RAMPdfFileService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DirectoryWatcherConfiguration configuration = null;
            string configurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            if (File.Exists(configurationPath))
            {                
                using (StreamReader r = Helpers.GetStreamReader(configurationPath))
                {
                    string json = r.ReadToEnd();
                    configuration = JsonConvert.DeserializeObject<DirectoryWatcherConfiguration>(json);
                }

                if (configuration != null)
                {
                    _directoryWatcher = new DirectoryWatcher(configuration);
                    _directoryWatcher.Start();
                }
                else
                {
                    EventLogManager.WriteError(new Exception("No configuration."));
                }
            }
            else
            {
                EventLogManager.WriteError(new Exception("No config.json file found."));
            }
        }

        protected override void OnStop()
        {
            _directoryWatcher?.Stop();
        }
    }
}
