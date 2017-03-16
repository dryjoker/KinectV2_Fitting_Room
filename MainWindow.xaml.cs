using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Face; //https://www.nuget.org/packages/Microsoft.Kinect.Face.x64/  // use this to set Kinect Face Tracking (x64)
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Drawing;
using System.Globalization;
using Microsoft.Win32;
using System.Windows.Forms;
using OpenCvSharp;

namespace KinectV2_PhotoBooth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            drawingGroup = new DrawingGroup();
            imageSource = new DrawingImage(drawingGroup);
            this.DataContext = this;
            InitializeComponent();
        }



        KinectSensor _sensor = null;
        MultiSourceFrameReader _reader;

        //BgRemoveHelper _bgRemovalHelper;
        BackgroundRemovalTool _bgRemovalHelper;


        // WPF
        DrawingGroup drawingGroup;
        DrawingImage imageSource;
        int displayWidth;
        int displayHeight;
        Rect displayRect;

        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }


        /// <summary>
        /// Add music and sound effect 
        /// learned from : http://www.cnblogs.com/maruko/archive/2013/04/19/WPF-Sound.html
        /// music(BGM) from youtube :
        /// music1:https://www.youtube.com/watch?v=CMyVicWuO_M&index=1&list=PLBRzriavSF5lkaQL0RMgxvVsZ2M61m4gQ //2:55
        /// music2:https://www.youtube.com/watch?v=2K80ZqhKNuU //00:43
        /// </summary>
        MediaPlayer player = new MediaPlayer();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //haarCascade = new HaarCascade(@"haarcascade_frontalface_alt_tree.xml");
            _sensor = KinectSensor.GetDefault();

            //music load
            player.Open(new Uri(@"Resources\res_music\Happy Background Music.mp3", UriKind.Relative));
            //player.Open(new Uri(@"Resources\res_music\Jesu Joy of Mans Desiring.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            player.Play();
            
            player.MediaEnded += Player_MediaEnded;

            if (_sensor != null) //當取得  sensor 之後
            {
                _sensor.Open(); //開啟

                //Initialize the background removal tool.
                //_bgRemovalHelper = new BgRemoveHelper(_sensor.CoordinateMapper);
                _bgRemovalHelper = new BackgroundRemovalTool(_sensor.CoordinateMapper);


                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                FrameDescription frameDescription = _sensor.ColorFrameSource.FrameDescription;
                displayWidth = frameDescription.Width;
                displayHeight = frameDescription.Height;
                displayRect = new Rect(0, 0, displayWidth, displayHeight);


            }
        }
        //循環設定音樂播放
        /// <summary>
        /// learned from : http://stackoverflow.com/questions/5975388/c-sharp-wpf-how-to-repeat-mediaelement-playback-from-mediaended-event-handler-wi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Player_MediaEnded(object sender, EventArgs e)
        {
            player.Position = TimeSpan.Zero;
            player.Play();
        }

        /// <summary>
        /// Add the background remove effect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
            using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
            using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
                {
                    // 3) Update the image source.
                    //Camera.Source = _bgRemovalHelper.removeBackground(colorFrame, depthFrame, bodyIndexFrame);

                    //Camera.Source = _bgRemovalHelper.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);
                    var bitmap = _bgRemovalHelper.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);

                    Camera.Source = bitmap;
                }
            }
        }

        /// <summary>
        /// Add the jump to next page function
        /// </summary>
        Grid g;
        private void switchPage(Grid preGrid, Grid nextGrid)//跳頁用的method
        {
            preGrid.Visibility = Visibility.Collapsed;//目前此頁設置隱藏
            g = nextGrid;//下一網格頁面覆蓋
            Canvas.SetLeft(g, 0);
            Canvas.SetTop(g, 0);
            nextGrid.Visibility = Visibility.Visible;
        }

        //button sound 
        //http://zh-tw.soundeffect-lab.info/sound/button/


        MediaPlayer btnToPageSound = new MediaPlayer();

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnToPageSound.Open(new Uri(@"Resources\res_music\decision7.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnToPageSound.Play();
            switchPage(Page1, Page2);
        }
        private void btnToPage3_Click_1(object sender, RoutedEventArgs e)
        {
            btnToPageSound.Open(new Uri(@"Resources\res_music\decision7.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnToPageSound.Play();
            switchPage(Page2, Page3);
            image2.Source = image1.Source; // 風景背景
            Camera2.Source = Camera.Source;// 去背後的人體前景
        }

        private void btnToPage4_Click(object sender, RoutedEventArgs e)
        {
            btnToPageSound.Open(new Uri(@"Resources\res_music\decision7.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnToPageSound.Play();
            switchPage(Page3, Page4);
            image3.Source = image2.Source; // 風景背景
            Camera3.Source = Camera2.Source; // 去背後的人體前景
            ImageBorder2.Source = ImageBorder.Source;//相框
        }

        MediaPlayer btnSound = new MediaPlayer();

        //背景選擇按鈕系列
        private void bg1_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            image1.Source = s1.Source;
        }
        private void bg2_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            image1.Source = s2.Source;
        }
        private void bg3_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            image1.Source = s3.Source;
        }
        private void bg4_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            image1.Source = s4.Source;
        }

        private void seg5_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            image1.Source = s5.Source;
        }

        private void seg6_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            image1.Source = s6.Source;
        }

        //相框選擇按鈕系列
        private void seg2_1_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            ImageBorder.Source = b1.Source;
        }

        private void seg2_2_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            ImageBorder.Source = b2.Source;
        }

        private void seg2_3_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            ImageBorder.Source = b3.Source;
        }

        private void seg2_4_Selected(object sender, RoutedEventArgs e)
        {
            btnSound.Open(new Uri(@"Resources\res_music\decision15.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnSound.Play();
            ImageBorder.Source = b4.Source;
        }


        Timer timer;
        int sec;

        FileStream fs;
        string path;
        BitmapEncoder encoder;

        /// <summary>
        /// Add Take photo effect (store photo into my picture on computer)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        RenderTargetBitmap renderBitmap;
        string myPhotos;
        string time;
        DrawingVisual dv;
        System.Windows.Forms.OpenFileDialog openFileDialog;

        private void btnTakePhoto_Click(object sender, RoutedEventArgs e)
        {
            TimerText.Text = "";
            // Create a render target to which we'll render our composite image
            //CompositeImage3.ActualWidth = txtWidth.Text;//不能指派只能唯讀

            //RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)CompositeImage3.ActualWidth, (int)CompositeImage3.ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            if (int.Parse(txtWidth.Text) != 0 && int.Parse(txtHeight.Text) != 0)
                renderBitmap = new RenderTargetBitmap(int.Parse(txtWidth.Text), int.Parse(txtHeight.Text), 96.0, 96.0, PixelFormats.Pbgra32);

            dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush brush = new VisualBrush(CompositeImage3);
                dc.DrawRectangle(brush, null, new Rect(new System.Windows.Point(), new System.Windows.Size(CompositeImage3.ActualWidth, CompositeImage3.ActualHeight)));
            }

            

            //string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            //倒數的秒數
            sec = 10;
            timer = new Timer();
            //設定計時器的速度
            timer.Interval = 1000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }
        //倒數計時
        private void timer_Tick(object sender, EventArgs e)
        {
            if (sec > 0)
            {
                TimerText.Text = sec + "  seconds";
                sec--;
            }
            else
            {
                timer.Stop();
                TimerText.Text = "Time's up!";
                renderBitmap.Render(dv);

                encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                myPhotos = openFileDialog.InitialDirectory.ToString();
                path = System.IO.Path.Combine(myPhotos, "KinectScreenshot-CoordinateMapping-" + time + ".png");

                fs = new FileStream(path, FileMode.Create);
                encoder.Save(fs);
            }
            
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string str = comboBox.Text;

            ComboBoxItem cbi = (ComboBoxItem)comboBox.SelectedItem;

            if ((string)cbi.Content == "640*480")
            {
                
                //renderBitmap = new RenderTargetBitmap(640, 480, 96.0, 96.0, PixelFormats.Pbgra32);
            }
            if ((string)cbi.Content == "512*424")
            {
                renderBitmap = new RenderTargetBitmap(512, 424, 96.0, 96.0, PixelFormats.Pbgra32);
            }
        }

        private void GoFirstPage_Click(object sender, RoutedEventArgs e)
        {
            btnToPageSound.Open(new Uri(@"Resources\res_music\decision7.mp3", UriKind.Relative));//右側屬性要記得改成copy always
            btnToPageSound.Play();
            switchPage(Page4, Page1);
        }

        private void btnPrintPreview_Click(object sender, RoutedEventArgs e)
        {
            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();
            dc.DrawImage(renderBitmap, new Rect { Width = renderBitmap.Width, Height = renderBitmap.Height });
            dc.Close();

            var pdialog = new System.Windows.Controls.PrintDialog();
            if (pdialog.ShowDialog() == true)
            {
                pdialog.PrintVisual(vis, "My Image");
            }
        }

    }
}

