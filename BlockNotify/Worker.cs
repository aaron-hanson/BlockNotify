using System.ComponentModel;
using System.Windows.Forms;

namespace BlockNotify
{
    class Worker : BackgroundWorker
    {
        public Worker(NotifyIcon tray)
        {
            Tray = tray;
        }

        public NotifyIcon Tray { get; set; }
    }
}