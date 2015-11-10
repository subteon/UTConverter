using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace UTConverter
{

    public partial class Form1 : Form
    {
        private MyClipboardViewer viewer;

        public Form1()
        {

            viewer = new MyClipboardViewer(this);

            // イベントハンドラを登録
            viewer.ClipboardHandler += this.OnClipBoardChanged;

            InitializeComponent();
        }

        // クリップボードにテキストがコピーされると呼び出される
        private void OnClipBoardChanged(object sender, ClipboardEventArgs args)
        {
            string input_str;
            int i = 0;

            this.textBox1.Text = args.Text;
            input_str = this.textBox1.Text;

            // unixtime or datetime を確認
            DateTime dt;
            if (DateTime.TryParse(input_str, out dt))
            {
                // date -> unixtime
                this.label2.Text = "DateTime -> UnixTime";

                var dto = new DateTimeOffset(dt, new TimeSpan(+09, 00, 00));
                var dtot = dto.ToUnixTimeSeconds();

                string dt_str = dtot.ToString();
                this.textBox2.Text = dt_str;

            }
            else if ( int.TryParse(input_str, out i) )
            {
                // unixtime -> date
                this.label2.Text = "UnixTime -> DateTime";

                //int input_int = int.Parse(input_str);

                var dto = (DateTimeOffset.FromUnixTimeSeconds(int.Parse(input_str)).ToLocalTime());

                string dt_str = dto.ToString();
                this.textBox2.Text = dt_str;

            }
            else
            {
                this.label2.Text = "DateTimeに変換できません。";
                this.textBox2.Text = "";
            }

        }

        // to unitime
        /*
        public static int UnixTime(DateTime now)
        {
            //(long)new DateTime(1970, 1, 1, 9, 0, 0, 0).ToFileTimeUtc();
            return (int)(now.ToFileTimeUtc() / 10000000 - 11644506000L);
        }
        */

        private void Form1_Load(object sender, EventArgs e)
        {

            string dt_str = "";

            // unixtime : date
            this.label2.Text = "^:UnixTime  v:DateTime";

            DateTime now_unix_time_dt = DateTime.Now;
            TimeSpan ts = new TimeSpan(0, 0, 0, 0);
            now_unix_time_dt = now_unix_time_dt + ts;

            dt_str = now_unix_time_dt.ToString();
            this.textBox2.Text = dt_str;

            var dto = new DateTimeOffset(now_unix_time_dt, new TimeSpan(+09, 00, 00));
            var dtot = dto.ToUnixTimeSeconds();

            dt_str = dtot.ToString();
            this.textBox1.Text = dt_str;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1_Load("",null);
        }

        public class ClipboardEventArgs : EventArgs
        {
            private string text;

            public string Text
            {
                get { return this.text; }
            }

            public ClipboardEventArgs(string str)
            {
                this.text = str;
            }
        }

        public delegate void cbEventHandler(
                                object sender, ClipboardEventArgs ev);

        [System.Security.Permissions.PermissionSet(
              System.Security.Permissions.SecurityAction.Demand,
              Name = "FullTrust")]
        internal class MyClipboardViewer : NativeWindow
        {
            [DllImport("user32")]
            public static extern IntPtr SetClipboardViewer(
                    IntPtr hWndNewViewer);

            [DllImport("user32")]
            public static extern bool ChangeClipboardChain(
                    IntPtr hWndRemove, IntPtr hWndNewNext);

            [DllImport("user32")]
            public extern static int SendMessage(
                    IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

            private const int WM_DRAWCLIPBOARD = 0x0308;
            private const int WM_CHANGECBCHAIN = 0x030D;
            private IntPtr nextHandle;

            private Form parent;
            public event cbEventHandler ClipboardHandler;

            public MyClipboardViewer(Form f)
            {
                f.HandleCreated
                            += new EventHandler(this.OnHandleCreated);
                f.HandleDestroyed
                            += new EventHandler(this.OnHandleDestroyed);
                this.parent = f;
            }

            internal void OnHandleCreated(object sender, EventArgs e)
            {
                AssignHandle(((Form)sender).Handle);
                // ビューアを登録
                nextHandle = SetClipboardViewer(this.Handle);
            }

            internal void OnHandleDestroyed(object sender, EventArgs e)
            {
                // ビューアを解除
                bool sts = ChangeClipboardChain(this.Handle, nextHandle);
                ReleaseHandle();
            }

            protected override void WndProc(ref Message msg)
            {
                switch (msg.Msg)
                {

                    case WM_DRAWCLIPBOARD:
                        if (Clipboard.ContainsText())
                        {
                            // クリップボードの内容がテキストの場合のみ
                            if (ClipboardHandler != null)
                            {
                                // クリップボードの内容を取得してハンドラを呼び出す
                                ClipboardHandler(this,
                                    new ClipboardEventArgs(Clipboard.GetText()));
                            }
                        }
                        if ((int)nextHandle != 0)
                            SendMessage(
                                nextHandle, msg.Msg, msg.WParam, msg.LParam);
                        break;

                    // クリップボード・ビューア・チェーンが更新された
                    case WM_CHANGECBCHAIN:
                        if (msg.WParam == nextHandle)
                        {
                            nextHandle = (IntPtr)msg.LParam;
                        }
                        else if ((int)nextHandle != 0)
                            SendMessage(
                                nextHandle, msg.Msg, msg.WParam, msg.LParam);
                        break;
                }
                base.WndProc(ref msg);
            }
        }

    }




}
