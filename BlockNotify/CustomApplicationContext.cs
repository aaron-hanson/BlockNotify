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
        private const int PollDelayMillis = 30000;

        private List<BlocktrailBlock> LatestBlocks { get; set; }

        public CustomApplicationContext()
        {
            WebClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.103 Safari/537.36");

            LatestBlocks = new List<BlocktrailBlock>();

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
            _tray.DoubleClick += Tray_DoubleClick;

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

                BlocktrailBlock latest = JsonConvert.DeserializeObject<BlocktrailBlock>(text);

                if (int.Parse(latest.Height) > prevHeight)
                {
                    LatestBlocks.Add(latest);
                    prevHeight = int.Parse(latest.Height);
                    worker.ReportProgress(0, latest);
                }

                Thread.Sleep(PollDelayMillis);
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BlocktrailBlock latest = (BlocktrailBlock)e.UserState;
            _tray.ShowBalloonTip(25000, "BLOCK FOUND", latest.ToString(), ToolTipIcon.Info);
        }

        private void Tray_DoubleClick(object sender, EventArgs e)
        {
            _tray.ShowBalloonTip(25000, "Latest Blocks", GetLatestBlocksString(), ToolTipIcon.Info);
        }

        private string GetLatestBlocksString()
        {
            return string.Join(Environment.NewLine, LatestBlocks.OrderByDescending(x => x.HeightInt).Take(10).Select(x => x.ToShortString()));
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

    class BlocktrailBlock
    {
        [JsonProperty(PropertyName = "height")]
        public string Height { get; set; }
        public int HeightInt { get { return int.Parse(Height); } }

        [JsonProperty(PropertyName = "miningpool_name")]
        public string MiningPoolName { get; set; }

        [JsonProperty(PropertyName = "block_time")]
        public string BlockTime { get; set; }
        public DateTime BlockDateTime { get { return DateTime.Parse(BlockTime); } }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
        public decimal ValueDec { get { return decimal.Parse(Value) / 100000000; } }

        public string ToShortString()
        {
            return string.Format("{0} [H={1}] {2} ({3} BTC)", BlockDateTime.ToShortTimeString(), Height, MiningPoolName, ValueDec);
        }

        public override string ToString()
        {
            return string.Format("{1}{0}{2}{0}{3}{0}{4} BTC", Environment.NewLine, BlockDateTime, Height, MiningPoolName, ValueDec);
        }
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

}
