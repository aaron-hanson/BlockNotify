using BlockNotify.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlockNotify
{
    class CustomApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private readonly Worker _theWorker;
        private static readonly WebClient WebClient = new WebClient();

        public CustomApplicationContext()
        {
            WebClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.103 Safari/537.36");

            ToolStripMenuItem menuExit = new ToolStripMenuItem("Exit");
            ContextMenuStrip theMainMenu = new ContextMenuStrip();
            theMainMenu.Items.AddRange(new ToolStripItem[] { menuExit });

            _tray = new NotifyIcon
            {
                Icon = Resources.TrayIcon,
                ContextMenuStrip = theMainMenu,
                Text = "Bitcoin Block Notifier",
                Visible = true
            };

            ThreadExit += OnThreadExit;
            menuExit.Click += MenuExit_Click;

            _theWorker = new Worker(_tray) { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _theWorker.DoWork += DoWork;
            _theWorker.ProgressChanged += ProgressChanged;
            _theWorker.Disposed += DisposeWorker;
            _theWorker.RunWorkerAsync();
        }

        private void DoWork(object sender, EventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker == null) return;

            int prevHeight = -1;

            while (true)
            {
                string text = WebClient.DownloadString(@"https://api.blocktrail.com/v1/btc/block/latest?api_key=9467d3e54e43eff55455f03bfed34b7c1f5a73c7");

                BlocktrailLatestBlock latest = JsonConvert.DeserializeObject<BlocktrailLatestBlock>(text);

                if (int.Parse(latest.Height) > prevHeight)
                {
                    prevHeight = int.Parse(latest.Height);
                    worker.ReportProgress(0, latest);
                }
                

                //{
                //    "hash":"00000000000000000584cdd8ec00b13ea91e79d3fb34095278cc718f4fff37c9",
                //    "height":351000,
                //    "block_time":"2015-04-06T18:48:55+0000",
                //    "arrival_time":"2015-04-06T18:50:21+0000",
                //    "nonce":2803835628,
                //    "difficulty":49446390688,
                //    "merkleroot":"18a6fc4911d368a64e7dfe110f391b291ff25a2aeb199963fe746e3fa811087d",
                //    "prev_block":"00000000000000000c5c3b34c5bffb9bd42ad5957aa7378bb24f5e96b74e6a8b",
                //    "next_block":null,
                //    "byte_size":903732,
                //    "confirmations":1,
                //    "transactions":2016,
                //    "value":1087783157220,
                //    "miningpool_name":"Eligius",
                //    "miningpool_url":"http:\/\/eligius.st",
                //    "miningpool_slug":"eligius"}

                Thread.Sleep(10000);
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BlocktrailLatestBlock latest = (BlocktrailLatestBlock)e.UserState;
            _tray.ShowBalloonTip(int.MaxValue, "BLOCK FOUND", latest.ToString(), ToolTipIcon.Info);
        }

        private void DisposeWorker(object sender, EventArgs e)
        {
            WebClient.Dispose();
        }

        private void OnThreadExit(object sender, EventArgs e)
        {
            _theWorker.Dispose();
            _tray.Visible = false;
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            _theWorker.Dispose();
            Application.Exit();
        }

    }

    class BlocktrailLatestBlock
    {
        [JsonProperty(PropertyName = "height")]
        public string Height { get; set; }

        [JsonProperty(PropertyName = "miningpool_name")]
        public string MiningPoolName { get; set; }

        public override string ToString()
        {
            return string.Format("Height: {1}{0}{2}", Environment.NewLine, Height, MiningPoolName);
        }
    }
}
