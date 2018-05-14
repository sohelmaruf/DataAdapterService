using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace HistoricalAdapter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new HistoricalAdapterService()
            };
            ServiceBase.Run(ServicesToRun);
            //Application.Run(new ServiceForm());
        }
    }
}
