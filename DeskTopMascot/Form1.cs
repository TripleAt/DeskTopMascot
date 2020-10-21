using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;  // for DllImport
using System.Security.Permissions;


enum State { none, blink };

namespace DeskTopMascot
{
    public partial class Form1 : Form
    {
        System.Drawing.Image img;
        System.Drawing.Image img2;
        Bitmap bmpSrc;
        Rectangle rect;
        Rectangle destRect;
        string body_string = @"./grp/character2.png";
        string face_string = @"./grp/kao.png";
        int anime = 0;
        int time = 0;
        // Random クラスの新しいインスタンスを生成する
        Random cRandom = new System.Random();
        State anim;
        DateTime startDt;
        TimeSpan rand_time;
        bool saisei_flg = false;
        Graphics g;
        DateTime now_time;
        PowerStatus ps = SystemInformation.PowerStatus;     //電源情報

        int[] animation =
        {
            0, 1,
            1, 3,
            2, 3,
            3,10,
            2, 3,
            0, 3,
            2, 3,
            3,28,
            2, 4,
            0, 1,
            -999, 0,
        };

        //パワーオン等の設定を使えるようにする。
        [HostProtectionAttribute(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
        public delegate void PowerModeChangedEventHandler(
            Object sender,
            Microsoft.Win32.PowerModeChangedEventArgs e
        );

        public Form1()
        {
            //透過を対応させる
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw, true);
            InitializeComponent();



            //フォームの境界線をなくす
            this.FormBorderStyle = FormBorderStyle.None;



            timer1 = new Timer();
            timer1.Interval = 16;
            timer1.Enabled = true;
            timer1.Tick += new EventHandler(DrawPlay);
            timer1.Start();


            this.DoubleBuffered = true;  // ダブルバッファリング
            //瞬きのアニメーションのタイムのセット
            time = animation[anime + 1];
            //アニメーションの状態をなしにする
            anim = State.none;

            //次瞬きする瞬間まで……
            rand_time = new TimeSpan(0, 0, cRandom.Next(5, 12));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //全体の透過
           // this.Opacity = 0.9;
            //ウィンドウの移動処理
            CharBody.MouseDown +=
                new MouseEventHandler(Form1_MouseDown);
            CharBody.MouseMove +=
                new MouseEventHandler(Form1_MouseMove);

            bmpSrc = new Bitmap(CharBody.Width, CharBody.Height);

            //formの色を透過色に//フォームを背景色で透過させる
            this.TransparencyKey = this.BackColor;


            img = System.Drawing.Image.FromFile(@body_string);
            img2 = System.Drawing.Image.FromFile(@face_string);
            
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            g = Graphics.FromImage(bmpSrc);
            g.TranslateTransform(-img.Width / 2, -img.Height / 2);
            //画像を描画
            rect = new Rectangle(0, 0, img.Width, img.Height);
            destRect = new Rectangle(182, 103, 112, 80);

           //イベントをイベント ハンドラに関連付ける
            //フォームコンストラクタなどの適当な位置に記述してもよい
            Microsoft.Win32.SystemEvents.PowerModeChanged +=
                new Microsoft.Win32.PowerModeChangedEventHandler(
                    SystemEvents_PowerModeChanged);
            //PictureBox1に表示する
            CharBody.Image = bmpSrc;


            startDt = DateTime.Now;

            
        }


        //フォームのFormClosedイベントハンドラ
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //イベントを解放する
            //フォームDisposeメソッド内の基本クラスのDisposeメソッド呼び出しの前に
            //記述してもよい
            Microsoft.Win32.SystemEvents.PowerModeChanged -=
                new Microsoft.Win32.PowerModeChangedEventHandler(
                    SystemEvents_PowerModeChanged);
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            //this.Invalidate();  // 再描画を促す
        }


        //描画処理
        void DrawPlay(object sender, EventArgs e)
        {
            //   img = System.Drawing.Image.FromFile(@body_string);
            //   img2 = System.Drawing.Image.FromFile(@face_string);
            now_time = DateTime.Now;

            CharBody.Image = null;

            //アニメーションの選択
            switch (anim)
            {
                case State.blink:
                    if (time == 0)
                    {
                        anime++;
                        anime++;
                        time = animation[anime + 1];
                        if (animation[anime] == -999)
                        {
                            anime = 0;
                            time = animation[anime + 1];
                            anim = State.none;
                            rand_time = new TimeSpan(0, 0, cRandom.Next(5, 12));
                            startDt = DateTime.Now;
                        }
                    }
                    else
                    {
                        time--;
                    }
                    break;

                case State.none:
                    TimeSpan ts = now_time - startDt; // 時間の差分を取得

                    //  一定時間経過
                    if (ts > rand_time)
                    {
                        anim = State.blink;
                    }
                    break;

            }


            g = Graphics.FromImage(bmpSrc);

            //全体を透過で塗りつぶす
            g.Clear(this.BackColor);

            {   //キャラクターの表示処理

                g.TranslateTransform(-img.Width / 2, -img.Height / 2);
                g.TranslateTransform(img.Width / 2, img.Height / 2, MatrixOrder.Append);
                g.DrawImage(img, rect);         //画像を描画
                g.DrawImage(img2, destRect, new Rectangle(112 * animation[anime], 0, 112, 80), GraphicsUnit.Pixel);
            }

            {       //時刻の表示処理
                PointF point = new PointF(6.0f, 256.0f);         // 文字を描画する原点（左上）
                StringFormat format = new StringFormat();
                GraphicsPath path = new GraphicsPath();
                Pen pen = new Pen(Brushes.GhostWhite, 10);       // 輪郭の色・太さ

                //現在の時刻を描画する
                path.AddString(now_time.ToLongTimeString(), new FontFamily("Arial Black"), (int)FontStyle.Regular, 100.0f, point, format);

                g.DrawPath(pen, path);                          //文字列の外を塗りつぶす
                g.FillPath(Brushes.Black, path);                //文字列の中を塗りつぶす
            }
            {
                string str = ps.BatteryLifePercent > 100 ? "(不明)" :"" + (ps.BatteryLifePercent * 100);
                PointF point = new PointF(6.0f, 256.0f);         // 文字を描画する原点（左上）
                StringFormat format = new StringFormat();
                GraphicsPath path = new GraphicsPath();
                Pen pen = new Pen(Brushes.GhostWhite, 2);       // 輪郭の色・太さ
                path.AddString("バッテリー残量:" + str + "%", new FontFamily("Arial Black"), (int)FontStyle.Regular, 20.0f, point, format);


                g.DrawPath(pen, path);                          //文字列の外を塗りつぶす
                g.FillPath(Brushes.Black, path);                //文字列の中を塗りつぶす
            }
            //リソースを解放する
            //img.Dispose();
            //g.Dispose();

            CharBody.Image = bmpSrc;            //PictureBox1に表示する

            //1時間に1度手前へウィンドウを持ってくる。
            if (now_time.Minute == 0 && now_time.Second == 0)
            {
                this.TopMost = true;
                this.TopMost = false;
                saisei_flg = true;
                if (now_time.Hour >= 12)
                    PlaySound("./se/gogo.wav");
                else
                    PlaySound("./se/gozen.wav");

            }
            if (saisei_flg)
                PlaySound2("./se/" + now_time.Hour % 12 + ".wav");
        }



        private void テストToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();       //終了処理
        }



        //----------------------------------------------------------------------------------
        //          ウィンドウの移動
        //----------------------------------------------------------------------------------
        //マウスのクリック位置を記憶
        private Point mousePoint;

        //Form1のMouseDownイベントハンドラ
        //マウスのボタンが押されたとき
        private void Form1_MouseDown(object sender,
            System.Windows.Forms.MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                //位置を記憶する
                mousePoint = new Point(e.X, e.Y);
            }
        }

        //Form1のMouseMoveイベントハンドラ
        //マウスが動いたとき
        private void Form1_MouseMove(object sender,
            System.Windows.Forms.MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.Left += e.X - mousePoint.X;
                this.Top += e.Y - mousePoint.Y;
            }
        }





        //----------------------------------------------------------------------------------
        //          音関連
        //----------------------------------------------------------------------------------
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        public static extern int mciSendString(
            string lpszCommand,        // コマンド文字列
            string lpszReturnString,    // 情報を受け取るバッファ
            int cchReturn,                  // バッファのサイズ
            IntPtr hwndCallback         // コールバックウィンドウのハンドル
        );

        [DllImport("winmm.dll")]
        extern static int mciSendString(string s1, StringBuilder s2, int i1, int i2);
        // 返り値 = 0 @正常終了
        // s1     = Command String
        // s2     = Return String
        // i1     = Return String Size
        // i2     = Callback Hwnd

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        extern static int GetWindowText(IntPtr hWnd, StringBuilder lpStr, int nMaxCount);

        //WAVEファイルを再生する
        private  void PlaySound(string waveFile)
        {
            StopSound();
            string cmd;
            //ファイルを開く
            cmd = "open \"" + waveFile + "\" type mpegvideo alias " + "MyMediaFile";
            if (mciSendString(cmd, null, 0, IntPtr.Zero) != 0)
                return;

            //再生する
            cmd = "play " + "MyMediaFile";
            mciSendString(cmd, null, 0, IntPtr.Zero);
        }


        //WAVEファイルを再生する
        private void PlaySound2(string waveFile)
        {
            StringBuilder str_builder = new StringBuilder(64);
            if (mciSendString("status MyMediaFile mode", str_builder, str_builder.Capacity, 0) == 0)
            {
                
                if (str_builder.ToString() != "playing")
                {
                    StopSound();
                    string cmd;
                    //ファイルを開く
                    cmd = "open \"" + waveFile + "\" type mpegvideo alias " + "MyMediaFile";
                    if (mciSendString(cmd, null, 0, IntPtr.Zero) != 0)
                        return;

                    //再生する
                    cmd = "play " + "MyMediaFile";
                    mciSendString(cmd, null, 0, IntPtr.Zero);
                    saisei_flg = false;
                }
            }
        }

        //再生されている音を止める
        private void StopSound()
        {
            string cmd;

            // 再生しているWAVEを停止する
            cmd = "stop " + "MyMediaFile";
            mciSendString(cmd, null, 0, IntPtr.Zero);

            // 閉じる
            cmd = "close " + "MyMediaFile";
            mciSendString(cmd, null, 0, IntPtr.Zero);
        }





        //----------------------------------------------------------------------------------
        //        パワーオフ等の感知
        //----------------------------------------------------------------------------------
        private void SystemEvents_PowerModeChanged(object sender,
            Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case Microsoft.Win32.PowerModes.StatusChange:
                    //Console.WriteLine("電源の状態が変化しました。");
                    //PlaySound("./se/itterassyai.wav");
                    break;
                case Microsoft.Win32.PowerModes.Suspend:
                    //Console.WriteLine("OSが中断状態になりました。");
                    break;
                case Microsoft.Win32.PowerModes.Resume:
                    //Console.WriteLine("OSが中断状態から復帰しました。");
                    PlaySound("./se/okaeri.wav");
                    break;
            }
        }


    }

}
