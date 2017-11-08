using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;
using TeacherMonitoringSolution.Network;


namespace TeacherMonitoringSolution
{
    /**
     * 프로젝트 이름 : TeacherMonitoringSolution
     * GIT HUB ADDRESS : https://github.com/SeungHeeKo/2017_TeacherMonitoringSolution
     * 개발환경 : WPF (C#)
     * 개발 내용 : 20대의 안드로이드 디바이스와 TCP, UDP를 통한 네트워크 통신 및 제어.
     * 콘텐츠 내용 : 4차 산업혁명 진로체험 VR 실행 중인 20대의 안드로이드와 통신하여 모니터링 및 제어.
     * 핵심개발자 : 고승희 
     * 개발시작일 : 2017년 9월 1일
     * **/
    public partial class MainWindow : Window
    {
        bool isColorTest, isVideoOnOffVersion;
        public enum VideoState
        {
            Play,
            Pause,
            Stop,
            None,
            None_Pause  // Pause된 상태에서 None 상태로 바뀐 경우
        }
        public enum VideoTitle
        {
            Mars,
            Student
        }

        public enum ClientState
        {
            Connected,
            Play,
            Interaction,
            Stop,
            Disconnected
        }
        
        //int connectImage = 0;   // 0 : white img    1 : white gif   2 : yellow img
        MediaElement[] videos;
        Label[] videoPosition, videoTitleLable; // label_content
        TextBlock[] waitingTB, periodTB;
        //String currDirectory = AppDomain.CurrentDomain.BaseDirectory;
        DispatcherTimer timer;
        BitmapImage imageSource_play, imageSource_pause;
        BitmapImage imageSourceBackgrounds, imageSource_topBar;

        int[] videoStates, videoTitle;
        private TimeSpan totalTime;
        private int currId, newId, playingId;
        string fileName;
        Label titleLabel;

        double currPosD;

        int num, titleNum, totalTitleNum, totalBackgroundImage, clientNum, imgNum;

        bool isFirst, isDragging, isReverseTime, isPlayBtnClicked;

        Constants _constants;

        UDPComm udpComm;
        TCPComm tcpComm;

        Image[] images;
        MediaElement _mediaElement_Mars, _mediaElement_Student;
        int _totalVideoTime_mars, _totalVideoTime_student;
        
        string _marsTitle, _studentTitle;
        string _marsFilePath, _studentFilePath;
        string _playAllMessage, _pauseAllMessage;

        private String serverIP;

        Char getDoublePositionDelimiter, ipDelimiter, messageHandlingDelimiter;


        Thread udpThread, tcpThread;
        public MainWindow()
        {
            GCLatencyMode oldMode = GCSettings.LatencyMode;

            // Make sure we can always go to the catch block, 
            // so we can set the latency mode back to `oldMode`
            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;

                // Generation 2 garbage collection is now
                // deferred, except in extremely low-memory situations
            }
            finally
            {
                // ALWAYS set the latency mode back
                GCSettings.LatencyMode = oldMode;
            }
            InitializeVariable();
            LoadBackgroundImage();
            InitializeComponent();


            Application.Current.MainWindow.WindowState = WindowState.Maximized;

            for (int i = 0; i < 20; i++)
            {
                videoStates[i] = (int)VideoState.None;
                videoTitle[i] = (int)VideoTitle.Mars;
            }

            Loaded += MainWindow_Loaded;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;


            udpComm = new UDPComm();
            udpComm.OnReceiveMessage += new UDPComm.ReceiveMessageHandler(udpComm_OnReceiveMessage);
            //udpComm.OnSendMessage += new UDPComm.SendMessageHandler(udpComm_OnSendMessage);
            //udpComm.OnGetUDPMessage += udpComm_OnGetUDPMessage;


            //udpThread = new Thread(new ThreadStart(udpComm.InitUDPSocket));
            udpThread = new Thread(udpComm.InitUDPSocket);
            udpThread.IsBackground = true;
            udpThread.Start();


            //udpComm.Start();
        }

        private void InitializeVariable()
        {
            _constants = new Constants();
            imageSourceBackgrounds = new BitmapImage();
            imageSource_topBar = new BitmapImage();
            
            videos = new MediaElement[20];
            images = new Image[20];
            videoPosition = new Label[20];
            videoTitleLable = new Label[20];
            waitingTB = new TextBlock[20];
            periodTB = new TextBlock[20];
            videoStates = new int[20];
            videoTitle = new int[20];

            udpComm = null;
            tcpComm = null;

            totalBackgroundImage = 5;
            num = 0;
            titleNum = 0;
            totalTitleNum = 2;
            currPosD = 0;
            clientNum = 100;
            imgNum = 0;
            playingId = -1;
            _totalVideoTime_mars = 408747;
            _totalVideoTime_student = 314048;
            
            isColorTest = false;
            isVideoOnOffVersion = true;

            isFirst = true;
            isDragging = false;
            isReverseTime = false;
            isPlayBtnClicked = false;
            
            fileName = "";
            _marsTitle = "우주 생활";
            _studentTitle = "중학생의 일상";

            _marsFilePath = "Mars.mp4";
            _studentFilePath = "Student.mp4";
            _playAllMessage = "PlayAll";
            _pauseAllMessage = "PauseAll";
            serverIP = "192.168.1.10";

            getDoublePositionDelimiter = ':';
            ipDelimiter = '.';
            messageHandlingDelimiter = '_';
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tcpComm = new TCPComm();
            //tcpComm.Start();
            // socket start
            tcpThread = new Thread(tcpComm.InitSocket);
            tcpThread.IsBackground = true;
            tcpThread.Start();
            tcpComm.OnReceived += TcpComm_OnReceived;

            LoadDefaultImage();
            addTitle(StringEnum.GetStringValue(WindowVideoTitle.Mars), 0, 4);
            for (int row = 0; row < _constants.maxClient / 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    addDefaultView(col, row);
                    addVideoPosition(col, row);
                }
            }
            loadVideo();
            
        }

        private void TcpComm_OnReceived(string tcpMessage)
        {
            //MessageHandling(tcpMessage);
            //ThreadPool.QueueUserWorkItem(delegate { MessageHandling(tcpMessage); });
            Dispatcher.Invoke((Action)delegate () { TCPMessageHandling(tcpMessage); });
        }

        void udpComm_OnReceiveMessage()
        {
            //Trace.WriteLine(string.Format("received message : {0}", message));
            //PrintMessage(message);

            //MessageHandling(udpComm.ReceiveMessage);
            Dispatcher.Invoke((Action)delegate () { UDPMessageHandling(udpComm.ReceiveMessage); });

        }

        void udpComm_OnSendMessage(string message)
        {
            //Trace.WriteLine(string.Format("sent message : {0}", message));

        }
        private void LoadDefaultImage()
        {
            imageSource_play = new BitmapImage(new Uri("Resources/buttonPlay.png", UriKind.Relative));
            imageSource_pause = new BitmapImage(new Uri("Resources/buttonPause.png", UriKind.Relative));
            imageSource_topBar = new BitmapImage(new Uri("Resources/Black.png", UriKind.Relative));
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (currId < 0 || currId >= _constants.maxClient)
                return;

            if (videos[currId] != null)
            {
                if (videos[currId].Source != null)
                {
                    if (!isDragging)
                    {
                        timeSlider.Value = videos[currId].Position.TotalSeconds;
                    }
                }
            }
        }

        private void onMediaOpened(object sender, RoutedEventArgs e)
        {
            if (currId < 0 || currId >= _constants.maxClient)
                return;

            if (videos[currId] == null)
                return;

            if (videos[currId].NaturalDuration.HasTimeSpan)
            {
                totalTime = videos[currId].NaturalDuration.TimeSpan;
                timeSlider.Maximum = totalTime.TotalSeconds;
                timeSlider.SmallChange = 1;
                timeSlider.LargeChange = Math.Min(10, totalTime.Seconds / 10);

            }
            timer.Start();
        }

        // Drag 불가능하도록 설정
        private void onDragStarted(object sender, DragStartedEventArgs e)
        {
            if (!string.IsNullOrEmpty(fileName))
                isDragging = true;
        }
        private void onDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (currId < 0 || currId >= _constants.maxClient)
                return;

            if (videos[currId] != null)
            {
                isDragging = false;

                //videos[currId].Position = TimeSpan.FromSeconds(timeSlider.Value);
            }
        }
        
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayBtnClicked)
            {
                if (isFirst)
                {
                    if (titleNum < 0 || titleNum >= totalTitleNum)
                        return;
                    switch (titleNum)
                    {
                        case (int)WindowVideoTitle.Mars:
                            sendMessageToAll(_marsFilePath);
                            break;
                        case (int)WindowVideoTitle.Student:
                            sendMessageToAll(_studentFilePath);
                            break;
                    }
                    isFirst = false;
                }
                else
                {
                    sendMessageToAll(_playAllMessage);
                }

                btnPlayImage.Source = imageSource_pause;

            }
            else
            {
                btnPlayImage.Source = imageSource_play;
                sendMessageToAll(_pauseAllMessage);
                if (playingId >= 0 && playingId < _constants.maxClient)
                {
                    if (videos[playingId] != null)
                    {
                        if (videos[playingId].HasVideo)
                        {
                            // 이미 실행중인 경우 pause
                            if (videoStates[playingId] == (int)VideoState.Play)
                            {
                                videos[playingId].Pause();
                                videoStates[playingId] = (int)VideoState.Pause;
                            }
                        }
                    }
                }
            }
            isPlayBtnClicked = !isPlayBtnClicked;


        }

        //private void LoadDefaultView()
        //{

        //}
        
        private void addDefaultView(int col, int row)
        {
            // 학생들의 state를 보여주는 배경 사진
            //BitmapImage imageSourceBackground = new BitmapImage();
            imageSourceBackgrounds = backgroundImages[(int)ClientState.Disconnected];
            Image imageBackground = new Image();
            imageBackground.Width = 272;
            imageBackground.Height = 182;
            imageBackground.Margin = new Thickness(0, 30, 0, 0);
            imageBackground.Source = imageSourceBackgrounds;
            //imageSourceBackground = new BitmapImage(new Uri("Resources/disconnected.png", UriKind.Relative));
            images[imgNum++] = imageBackground;


            // 상단바 (배경)
            Image topBarImage = new Image();
            topBarImage.Source = imageSource_topBar;
            topBarImage.Width = 272;
            topBarImage.VerticalAlignment = VerticalAlignment.Top;

            // 상단바 (번호)
            Label topBarNumber = new Label();
            topBarNumber.Width = 100;
            topBarNumber.Height = 50;
            topBarNumber.Content = clientNum % 100;
            clientNum++;
            topBarNumber.VerticalAlignment = VerticalAlignment.Top;
            topBarNumber.HorizontalAlignment = HorizontalAlignment.Left;
            topBarNumber.Margin = new Thickness(120, 7, 0, 0);
            topBarNumber.FontSize = 17;
            topBarNumber.Foreground = System.Windows.Media.Brushes.Gray;

            // 'waiting' 문구
            TextBlock waitingTextBlock = new TextBlock();
            waitingTextBlock.Foreground = System.Windows.Media.Brushes.White;
            waitingTextBlock.VerticalAlignment = VerticalAlignment.Bottom;
            waitingTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            waitingTextBlock.FontSize = 17;
            waitingTextBlock.Margin = new Thickness(-20, 0, 0, 10);
            waitingTextBlock.Text = "waiting";
            waitingTB[row * 5 + col] = waitingTextBlock;

            // '......' 애니메이션 문구
            TextBlock textBlock = new TextBlock();            
            textBlock.Style = FindResource("AniamateTextBlock") as Style;
            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.FontSize = 20;
            textBlock.Margin = new Thickness(65, 0, 0, 10);
            textBlock.Text = "......";
            periodTB[row * 5 + col] = textBlock;
            
            Grid.SetColumn(waitingTextBlock, col);
            Grid.SetRow(waitingTextBlock, row);
            Grid.SetZIndex(waitingTextBlock, 1);

            Grid.SetColumn(textBlock, col);
            Grid.SetRow(textBlock, row);
            Grid.SetZIndex(textBlock, 1);

            Grid.SetColumn(topBarNumber, col);
            Grid.SetRow(topBarNumber, row);
            Grid.SetZIndex(topBarNumber, 1);

            Grid.SetColumn(imageBackground, col);
            Grid.SetRow(imageBackground, row);
            Grid.SetZIndex(imageBackground, 0);

            Grid.SetColumn(topBarImage, col);
            Grid.SetRow(topBarImage, row);

            VideoHolder.Children.Add(waitingTextBlock);
            VideoHolder.Children.Add(textBlock);
            VideoHolder.Children.Add(topBarNumber);
            VideoHolder.Children.Add(imageBackground);
            VideoHolder.Children.Add(topBarImage);
        }

        private void loadVideo()
        {
            _mediaElement_Mars = new MediaElement();
            _mediaElement_Mars.Margin = new Thickness(0, 43, 0, 0);
            _mediaElement_Mars.Width = 272;
            _mediaElement_Mars.Height = 175;
            _mediaElement_Mars.LoadedBehavior = MediaState.Manual;
            _mediaElement_Mars.ScrubbingEnabled = true;

            _mediaElement_Mars.Source = new Uri(_marsFilePath, UriKind.RelativeOrAbsolute);
            _mediaElement_Mars.Position += TimeSpan.FromSeconds(1);
            _mediaElement_Mars.MediaOpened += onMediaOpened;


            _mediaElement_Student = new MediaElement();
            _mediaElement_Student.Margin = new Thickness(0, 43, 0, 0);
            _mediaElement_Student.Width = 272;
            _mediaElement_Student.Height = 175;
            _mediaElement_Student.LoadedBehavior = MediaState.Manual;
            _mediaElement_Student.ScrubbingEnabled = true;

            _mediaElement_Student.Source = new Uri(_studentFilePath, UriKind.RelativeOrAbsolute);
            _mediaElement_Student.Position += TimeSpan.FromSeconds(1);
            _mediaElement_Student.MediaOpened += onMediaOpened;

        }
        private void setVideoView(int col, int row)
        {
            bool isMars = false;

            if (currId < 0 || currId >= _constants.maxClient)
                return;

            if (videoTitle[currId] == (int)VideoTitle.Mars)
            {
                isMars = true;
                Grid.SetColumn(_mediaElement_Mars, col);
                Grid.SetRow(_mediaElement_Mars, row);
                Grid.SetZIndex(_mediaElement_Mars, 0);
            }
            else
            {
                Grid.SetColumn(_mediaElement_Student, col);
                Grid.SetRow(_mediaElement_Student, row);
                Grid.SetZIndex(_mediaElement_Student, 0);
            }

            if (videos[currId] != null)
            {
                if (videos[currId].HasVideo)
                {
                    // 이미 실행중일 경우 return
                    if (videoStates[currId] == (int)VideoState.Play)
                    {
                        // sync 맞춤
                        videos[currId].Position = TimeSpan.FromSeconds(GetDoublePosition(currId));
                        return;
                    }
                    // 일시정지 중일 경우 현재 position 값에 해당하는 영상 이미지 출력
                    else if (videoStates[currId] == (int)VideoState.Pause)
                    {
                        double curr = GetDoublePosition(currId);
                        if (curr != 0)
                        {
                            if (isMars)
                            {
                                _mediaElement_Mars.Play();
                                videos[currId].Position = TimeSpan.FromSeconds(curr);
                                _mediaElement_Mars.Pause();
                            }
                            else
                            {
                                _mediaElement_Student.Play();
                                videos[currId].Position = TimeSpan.FromSeconds(curr);
                                _mediaElement_Student.Pause();
                            }
                        }
                        return;
                    }
                }
            }

            if (videoStates[currId] == (int)VideoState.None_Pause)
            {
                double curr = GetDoublePosition(currId);
                if (curr != 0)
                {
                    if (isMars)
                    {
                        VideoHolder.Children.Add(_mediaElement_Mars);

                        videos[currId] = _mediaElement_Mars;
                        _mediaElement_Mars.Play();
                        videos[currId].Position = TimeSpan.FromSeconds(curr);
                        _mediaElement_Mars.Pause();
                        videoStates[currId] = (int)VideoState.Pause;
                    }
                    else
                    {
                        VideoHolder.Children.Add(_mediaElement_Student);

                        videos[currId] = _mediaElement_Student;
                        _mediaElement_Student.Play();
                        videos[currId].Position = TimeSpan.FromSeconds(curr);
                        _mediaElement_Student.Pause();
                        videoStates[currId] = (int)VideoState.Pause;
                    }
                }
                return;
            }

            if (isMars)
            {
                VideoHolder.Children.Add(_mediaElement_Mars);

                videos[currId] = _mediaElement_Mars;
                _mediaElement_Mars.Play();
                videos[currId].Position = TimeSpan.FromSeconds(GetDoublePosition(currId));
                videoStates[currId] = (int)VideoState.Play;
            }
            else
            {
                VideoHolder.Children.Add(_mediaElement_Student);

                videos[currId] = _mediaElement_Student;
                _mediaElement_Student.Play();
                videos[currId].Position = TimeSpan.FromSeconds(GetDoublePosition(currId));
                videoStates[currId] = (int)VideoState.Play;
            }
        }

        private void addVideoPosition(int col, int row)
        {
            // 영상 재생 시간
            Label videoPlayTimeLabel = new Label();
            videoPlayTimeLabel.Width = 272;
            videoPlayTimeLabel.Height = 175;
            videoPlayTimeLabel.Content = _connectMessage;
            videoPlayTimeLabel.VerticalAlignment = VerticalAlignment.Center;
            videoPlayTimeLabel.HorizontalAlignment = HorizontalAlignment.Left;
            videoPlayTimeLabel.VerticalContentAlignment = VerticalAlignment.Bottom;
            videoPlayTimeLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
            videoPlayTimeLabel.Margin = new Thickness(0, 0, 0, 0);
            videoPlayTimeLabel.FontSize = 17;
            // label_content : "connected"
            videoPlayTimeLabel.Foreground = System.Windows.Media.Brushes.White;
            videoPlayTimeLabel.Visibility = Visibility.Hidden;

            // 영상 제목
            Label videoTitleLabel = new Label();
            videoTitleLabel.Width = 272;
            videoTitleLabel.Height = 175;
            //videoTitleLabel.Content = "title";
            videoTitleLabel.Content = "";
            videoTitleLabel.VerticalAlignment = VerticalAlignment.Center;
            videoTitleLabel.HorizontalAlignment = HorizontalAlignment.Left;
            videoTitleLabel.VerticalContentAlignment = VerticalAlignment.Center;
            videoTitleLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
            videoTitleLabel.Margin = new Thickness(0, 0, 0, 45);
            videoTitleLabel.FontSize = 13;
            videoTitleLabel.Foreground = System.Windows.Media.Brushes.White;
            videoTitleLabel.Visibility = Visibility.Hidden;

            Grid.SetColumn(videoTitleLabel, col);
            Grid.SetRow(videoTitleLabel, row);
            Grid.SetZIndex(videoTitleLabel, 0);

            Grid.SetColumn(videoPlayTimeLabel, col);
            Grid.SetRow(videoPlayTimeLabel, row);
            Grid.SetZIndex(videoPlayTimeLabel, 0);

            VideoHolder.Children.Add(videoTitleLabel);
            VideoHolder.Children.Add(videoPlayTimeLabel);

            videoPosition[num] = videoPlayTimeLabel;
            videoTitleLable[num++] = videoTitleLabel;
        }

        private void btnFF_Click(object sender, RoutedEventArgs e)
        {
            titleNum++;
            if (titleNum >= totalTitleNum)
                titleNum = 0;

            setTitle(titleNum);
        }

        private void sendMessageToAll(string message)
        {
            udpComm.SendBroadcastMessage(message);
        }


        private void btnRW_Click(object sender, RoutedEventArgs e)
        {
            titleNum--;
            if (titleNum < 0)
                titleNum = totalTitleNum - 1;

            setTitle(titleNum);
        }

        private void setTitle(int num)
        {
            if (titleLabel != null)
                VideoHolder.Children.Remove(titleLabel);
            switch (num)
            {
                case (int)WindowVideoTitle.Mars:
                    addTitle(StringEnum.GetStringValue(WindowVideoTitle.Mars), 0, 4);
                    break;
                case (int)WindowVideoTitle.Student:
                    addTitle(StringEnum.GetStringValue(WindowVideoTitle.Student), 0, 4);
                    break;
            }
        }

        private void addTitle(string title, int col, int row)
        {
            titleLabel = new Label();
            titleLabel.Content = title;
            titleLabel.VerticalAlignment = VerticalAlignment.Center;
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.Margin = new Thickness(0, 0, 0, 5);
            titleLabel.FontSize = 12;
            titleLabel.Foreground = System.Windows.Media.Brushes.White;

            Grid.SetColumn(titleLabel, col);
            Grid.SetRow(titleLabel, row);
            Grid.SetColumnSpan(titleLabel, 5);
            VideoHolder.Children.Add(titleLabel);

        }

        public void PrintMessage(string message)
        {
            //textBox.Text += message;
            //textBox.Text += "\n";
        }

        public void SetClientVideoPause(string clientNum)
        {
            if (clientNum.Equals(serverIP))
                return;
            int clientIdx = 0;
            //Char delimiter = '.';
            string[] lastNum = clientNum.Split(ipDelimiter);
            //string[] lastNum = clientNum.Split(new string[] { "." }, StringSplitOptions.None);

            clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;

            if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                return;

            if (videos[clientIdx] != null)
            {
                // 실행중이었을 경우 
                if (videos[clientIdx].HasVideo)
                {
                    if (videoStates[clientIdx] == (int)VideoState.Play)
                    {
                        // 영상 일시정지
                        //videos[clientIdx].Pause();
                        //videoStates[clientIdx] = (int)VideoState.Pause;

                        // 영상 해제
                        videos[clientIdx].Stop();
                        VideoHolder.Children.Remove(videos[clientIdx]);
                        videos[clientIdx] = null;
                    }
                }
            }
            videoPosition[clientIdx].Content = _connectMessage;
            SetBackgroundImage(clientIdx, (int)ClientState.Connected);
            videoPosition[clientIdx].Foreground = System.Windows.Media.Brushes.White;
        }

        public void SetClientVideoTitle(string clientNum, string title)
        {
            if (clientNum.Equals(serverIP))
                return;
            int clientIdx = 0;
            //Char delimiter = '.';
            string[] lastNum = clientNum.Split(ipDelimiter);
            //string[] lastNum = clientNum.Split(new string[] { "." }, StringSplitOptions.None);
            clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;

            if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                return;

            SetBackgroundImage(clientIdx, (int)ClientState.Connected);

            if (title.Equals(_title_marsMessage))
            {
                videoTitle[clientIdx] = (int)VideoTitle.Mars;
                if (!videoTitleLable[clientIdx].Content.Equals(_marsTitle))
                    videoTitleLable[clientIdx].Content = _marsTitle;
            }
            else if (title.Equals(_title_studentMessage))
            {
                videoTitle[clientIdx] = (int)VideoTitle.Student;
                if (!videoTitleLable[clientIdx].Content.Equals(_studentTitle))
                    videoTitleLable[clientIdx].Content = _studentTitle;
            }

            if (videoTitleLable[clientIdx].Visibility == Visibility.Hidden)
                videoTitleLable[clientIdx].Visibility = Visibility.Visible;

            // waiting...... textBlock 가시 상태일 경우
            if (waitingTB[clientIdx].Visibility == Visibility.Visible)
            {
                // waiting...... textBlock 숨김
                waitingTB[clientIdx].Visibility = Visibility.Hidden;
                periodTB[clientIdx].Visibility = Visibility.Hidden;
                // 영상 재생 시간 label 시각화
                videoPosition[clientIdx].Visibility = Visibility.Visible;
            }
        }

        public double GetDoublePosition(int clientNum)
        {
            string posString = null;
            double resultPos = 0;

            if (clientNum < 0 || clientNum >= _constants.maxClient)
                return resultPos;

            if (videoPosition[clientNum] == null)
                return resultPos;

            posString = videoPosition[clientNum].Content.ToString();
            // 공백 제거
            posString = posString.Trim();
            //Char delimiter = ':';
            //string[] posMin_Sec = posString.Split(new char[] { ':' });
            string[] posMin_Sec = posString.Split(getDoublePositionDelimiter);
            if (posMin_Sec[0].Equals(_connectMessage))
            {
                SetBackgroundImage(clientNum, (int)ClientState.Connected);
                return resultPos;
            }
            else
                SetBackgroundImage(clientNum, (int)ClientState.Play);

            int posMin = Int32.Parse(posMin_Sec[0]);
            int posSec = Int32.Parse(posMin_Sec[1]);

            if (posMin > 0)
                posSec += posMin * 60;

            if (isReverseTime)
            {
                posSec = GetVideoTotalTime(clientNum) - posSec;
            }

            return posSec;
        }
        private int GetVideoTotalTime(int clientNum)
        {
            int totalTime = 0;

            if (clientNum < 0 || clientNum >= _constants.maxClient)
                return totalTime;

            switch (videoTitle[clientNum])
            {
                case (int)VideoTitle.Mars:
                    totalTime = _totalVideoTime_mars;
                    break;
                case (int)VideoTitle.Student:
                    totalTime = _totalVideoTime_student;
                    break;
                default:
                    totalTime = _totalVideoTime_mars;
                    break;

            }

            return totalTime;
        }
        public void SetClientVideoPosition(string clientNum, string content)
        {
            if (clientNum.Equals(serverIP))
                return;
            int clientIdx = 0;
            //Char delimiter = '.';
            //string[] lastNum = clientNum.Split(new string[] { "." }, StringSplitOptions.None);
            string[] lastNum = clientNum.Split(ipDelimiter);
            PrintMessage(lastNum[lastNum.Length - 1]);
            int currPos = 0;
            if (!Int32.TryParse(content, out currPos))
            {
                return;
            }
            currPos = Int32.Parse(content);
            //currPos += 60000;
            if (isReverseTime)
            {
                currPosD = currPos / 1000;
                clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;

                if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                    return;
                currPos = GetVideoTotalTime(clientIdx) - currPos;
            }

            currPos = currPos / 1000;
            if (!isReverseTime)
                currPosD = currPos;

            int posMinute_int = 0, posSecond_int = 0;
            string posMinute_str = null, posSecond_str = null;

            if (currPos >= 60)
            {
                posMinute_int = currPos / 60;
                posSecond_int = currPos % 60;

                if (posMinute_int < 10)
                    posMinute_str = "0" + Convert.ToString(posMinute_int);
                else
                    posMinute_str = Convert.ToString(posMinute_int);
                if (posSecond_int < 10)
                    posSecond_str = "0" + Convert.ToString(posSecond_int);
                else
                    posSecond_str = Convert.ToString(posSecond_int);
            }
            else
            {
                posMinute_str = "00";
                posSecond_int = currPos % 60;
                if (posSecond_int < 10)
                    posSecond_str = "0" + Convert.ToString(posSecond_int);
                else
                    posSecond_str = Convert.ToString(posSecond_int);
            }
            content = posMinute_str + " : " + posSecond_str;

            clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;

            if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                return;

            videoPosition[clientIdx].Content = content;
            SetBackgroundImage(clientIdx, (int)ClientState.Play);

            // 영상이 일시정지 중일 경우 다시 재생
            if (videos[clientIdx] != null)
            {
                if (videos[clientIdx].HasVideo && videoStates[clientIdx] == (int)VideoState.Pause)
                {
                    videos[clientIdx].Position = TimeSpan.FromSeconds(currPosD);
                    videos[clientIdx].Play();
                    videoStates[clientIdx] = (int)VideoState.Play;
                }
            }
        }
        BitmapImage[] backgroundImages;
        private void LoadBackgroundImage()
        {
            backgroundImages = new BitmapImage[totalBackgroundImage];

            backgroundImages[(int)ClientState.Connected] = new BitmapImage(new Uri("./Resources/connected.png", UriKind.Relative));
            backgroundImages[(int)ClientState.Play] = new BitmapImage(new Uri("Resources/play.png", UriKind.Relative));
            backgroundImages[(int)ClientState.Interaction] = new BitmapImage(new Uri("Resources/interacton.png", UriKind.Relative));
            backgroundImages[(int)ClientState.Stop] = new BitmapImage(new Uri("Resources/stop.png", UriKind.Relative));
            backgroundImages[(int)ClientState.Disconnected] = new BitmapImage(new Uri("Resources/disconnected.png", UriKind.Relative));

        }
        public void SetBackgroundImage(int clientIdx, int status)
        {
            //imageSourceBackgrounds = new BitmapImage();
            switch (status)
            {
                case (int)ClientState.Connected:
                    imageSourceBackgrounds = backgroundImages[(int)ClientState.Connected];
                    break;
                case (int)ClientState.Play:
                    imageSourceBackgrounds = backgroundImages[(int)ClientState.Play];
                    break;
                case (int)ClientState.Interaction:
                    imageSourceBackgrounds = backgroundImages[(int)ClientState.Interaction];
                    break;
                case (int)ClientState.Stop:
                    imageSourceBackgrounds = backgroundImages[(int)ClientState.Stop];
                    break;
                case (int)ClientState.Disconnected:
                    imageSourceBackgrounds = backgroundImages[(int)ClientState.Disconnected];
                    break;

            }
            images[clientIdx].Source = imageSourceBackgrounds;
        }
        public void SetClientInteractionMode(string clientNum, string content)
        {
            if (clientNum.Equals(serverIP))
                return;
            int clientIdx = 0;
            //Char delimiter = '.';
            //string[] lastNum = clientNum.Split(new string[] { "." }, StringSplitOptions.None);
            string[] lastNum = clientNum.Split(ipDelimiter);
            PrintMessage(lastNum[lastNum.Length - 1]);

            clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;

            if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                return;
            videoPosition[clientIdx].Content = content;
            if (content.Equals(_interactionMessage))
            {
                SetBackgroundImage(clientIdx, (int)ClientState.Interaction);
            }
            else if (content.Equals(_stoppedMessage))
            {
                SetBackgroundImage(clientIdx, (int)ClientState.Stop);
            }

            // 사용자로부터 interaction mode에 진입했다는 UDP 패킷을 받았지만 
            // 영상 재생 시간 label이 숨김 상태일 경우 숨김 해제
            if (!videoPosition[clientIdx].IsVisible)
            {
                // 영상 재생 시간 및 제목 label 시각화
                videoPosition[clientIdx].Visibility = Visibility.Visible;
                videoTitleLable[clientIdx].Visibility = Visibility.Visible;
            }
            // waiting...... textBlock 가시 상태일 경우
            if (waitingTB[clientIdx].IsVisible)
            {
                // waiting...... textBlock 숨김
                waitingTB[clientIdx].Visibility = Visibility.Hidden;
                periodTB[clientIdx].Visibility = Visibility.Hidden;
            }
            // 영상이 재생중이었을 경우 영상 해제
            if (videos[clientIdx] != null)
            {
                if (videos[clientIdx].HasVideo)
                {
                    if (videoStates[clientIdx] == (int)VideoState.Play || videoStates[clientIdx] == (int)VideoState.Pause)
                    {
                        videos[clientIdx].Stop();
                        VideoHolder.Children.Remove(videos[clientIdx]);
                        videos[clientIdx] = null;
                    }
                    prevRow = -1;
                    prevCol = -1;
                    if (videoStates[clientIdx] == (int)VideoState.Pause)
                        videoStates[clientIdx] = (int)VideoState.None_Pause;
                }
            }
            videoStates[clientIdx] = (int)VideoState.None;
        }

        int prevRow = -1, prevCol = -1;
        private void VideoHolder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(VideoHolder);

            int row = 0;
            int col = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            // calc row mouse was over
            foreach (var rowDefinition in VideoHolder.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            // calc col mouse was over
            foreach (var columnDefinition in VideoHolder.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }

            if (row > _constants.maxClient / 5 || col > _constants.maxClient / 5)
                return;

            // 현재 연결이 waiting......일 경우 영상 재생하지 못하도록 return
            if (waitingTB[row * 5 + col].IsVisible)
                return;

            // 현재 인터렉션 모드로 들어갔을 경우
            if (videoPosition[row * 5 + col].Content.Equals(_interactionMessage))
            {
                // '인터렉션 중엔 프리뷰를 볼 수 없습니다.' 이미지 출력 후 종료
                return;
            }
            // 영상 재생 중지 시
            if (videoPosition[row * 5 + col].Content.Equals(_stoppedMessage))
            {
                return;
            }
            else if (videoPosition[row * 5 + col].Content.Equals(_connectMessage))
            {
                // 특정 학생만 영상 재생 신호를 받지 못했을 경우 개별로 신호 전송
                int ipLast = 100 + row * 5 + col;
                string clientIP = "192.168.1." + ipLast.ToString();

                if (videoTitleLable[row * 5 + col] != null)
                {
                    if (videoTitleLable[row * 5 + col].Content.Equals(""))
                    {
                        if (titleNum < 0 || titleNum >= totalTitleNum)
                            return;
                        switch (titleNum)
                        {
                            case (int)WindowVideoTitle.Mars:
                                udpComm.SendBroadcastMessage(clientIP, _marsFilePath);
                                break;
                            case (int)WindowVideoTitle.Student:
                                udpComm.SendBroadcastMessage(clientIP, _studentFilePath);
                                break;
                        }
                    }
                }


                udpComm.SendBroadcastMessage(clientIP, _playAllMessage);
                return;
            }

            // 초기 상태
            if (prevRow == -1)
            {
                prevRow = row;
                prevCol = col;
                currId = prevRow * 5 + prevCol;

                if (currId < 0 || currId >= _constants.maxClient)
                    return;
                setVideoView(col, row);
            }

            // 한 번 이상 눌렀을 경우
            else if ((prevRow != row || prevCol != col))
            {

                if (currId < 0 || currId >= _constants.maxClient)
                    return;
                // 이전 영상이 재생 혹은 일시정지 중이었을 경우 영상 해제
                if (videos[currId] != null)
                {
                    if (videos[currId].HasVideo)
                    {
                        if (videoStates[currId] == (int)VideoState.Play || videoStates[currId] == (int)VideoState.Pause)
                        {
                            videos[currId].Stop();
                            VideoHolder.Children.Remove(videos[currId]);
                            videos[currId] = null;
                            if (videoStates[currId] == (int)VideoState.Pause)
                                videoStates[currId] = (int)VideoState.None_Pause;
                            else
                                videoStates[currId] = (int)VideoState.None;
                        }
                    }
                }

                prevRow = row;
                prevCol = col;
                newId = prevRow * 5 + prevCol;
                currId = newId;
                setVideoView(col, row);
            }

            // 같은 칸을 한 번 더 눌렀을 경우
            // 영상 재생 중 -> 영상 해제
            // 아닐 경우 무시
            else
            {
                if (isVideoOnOffVersion)
                {
                    if (currId < 0 || currId >= _constants.maxClient)
                        return;
                    if (videos[currId] != null)
                    {
                        if (videos[currId].HasVideo)
                        {
                            if (videoStates[currId] == (int)VideoState.Play || videoStates[currId] == (int)VideoState.Pause)
                            {
                                videos[currId].Stop();
                                VideoHolder.Children.Remove(videos[currId]);
                                videos[currId] = null;
                                prevRow = -1;
                                prevCol = -1;
                                if (videoStates[currId] == (int)VideoState.Pause)
                                    videoStates[currId] = (int)VideoState.None_Pause;
                                else
                                    videoStates[currId] = (int)VideoState.None;
                            }
                        }
                    }
                    // 영상이 없을 경우 재생
                    else
                    {
                        prevRow = row;
                        prevCol = col;
                        newId = prevRow * 5 + prevCol;
                        if (newId < 0 || newId >= _constants.maxClient)
                            return;
                        currId = newId;
                        setVideoView(col, row);
                    }
                }

            }

            playingId = currId;

            PrintMessage(row + ", " + col + "  " + currId);
        }

        public void ClientConnected(string clientNum)
        {
            if (clientNum.Equals(serverIP))
                return;
            int clientIdx = 0;
            //Char delimiter = '.';
            string[] lastNum = clientNum.Split(ipDelimiter);
            //string[] lastNum = clientNum.Split(new string[] { "." }, StringSplitOptions.None);
            PrintMessage(lastNum[lastNum.Length - 1]);

            clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;
            if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                return;
            SetBackgroundImage(clientIdx, (int)ClientState.Connected);

            // waiting...... textBlock 숨김
            waitingTB[clientIdx].Visibility = Visibility.Hidden;
            periodTB[clientIdx].Visibility = Visibility.Hidden;
            // 영상 재생 시간 label 시각화
            videoPosition[clientIdx].Visibility = Visibility.Visible;
        }

        public void ClientDisconnected(string clientNum)
        {
            if (clientNum.Equals(serverIP))
                return;
            int clientIdx = 0;
            //Char delimiter = '.';
            string[] lastNum = clientNum.Split(ipDelimiter);
            //string[] lastNum = clientNum.Split(new string[] { "." }, StringSplitOptions.None);
            PrintMessage(lastNum[lastNum.Length - 1]);

            clientIdx = Int32.Parse(lastNum[lastNum.Length - 1]) % 100;
            if (clientIdx < 0 || clientIdx >= _constants.maxClient)
                return;


            // 영상이 재생중이었을 경우 영상 해제
            if (videos[clientIdx] != null)
            {
                if (videos[clientIdx].HasVideo)
                {
                    if (videoStates[clientIdx] == (int)VideoState.Play || videoStates[clientIdx] == (int)VideoState.Pause)
                    {
                        videos[clientIdx].Stop();
                        VideoHolder.Children.Remove(videos[clientIdx]);
                        videos[clientIdx] = null;
                    }
                    prevRow = -1;
                    prevCol = -1;
                    if (videoStates[clientIdx] == (int)VideoState.Pause)
                        videoStates[clientIdx] = (int)VideoState.None_Pause;
                }
            }
            videoStates[clientIdx] = (int)VideoState.None;

            SetBackgroundImage(clientIdx, (int)ClientState.Disconnected);

            // waiting...... textBlock 시각화
            waitingTB[clientIdx].Visibility = Visibility.Visible;
            periodTB[clientIdx].Visibility = Visibility.Visible;
            // 영상 재생 시간 label 숨김
            videoPosition[clientIdx].Visibility = Visibility.Hidden;
        }

        private const string _mountMessage = "HMDMount";
        private const string _unmountMessage = "HMDUnmount";
        private const string _connectMessage = "connected";
        private const string _interactionMessage = "Interaction";
        private const string _pauseMessage = "Pause";
        private const string _stoppedMessage = "Stopped";
        private const string _finishMessage = "Finished";
        private const string _title_marsMessage = "Mars";
        private const string _title_studentMessage = "Student";
        public void TCPMessageHandling(string messageFromStudent)
        {
            //Char delimiter = '_';
            string[] ip_Message = messageFromStudent.Split(messageHandlingDelimiter);

            if (string.IsNullOrEmpty(ip_Message[0]) || string.IsNullOrEmpty(ip_Message[1]))
                return;
            switch (ip_Message[1])
            {
                case _unmountMessage:
                    ClientDisconnected(ip_Message[0]);
                    break;
                case _mountMessage:
                    ClientConnected(ip_Message[0]);
                    break;
                case _connectMessage:
                    ClientConnected(ip_Message[0]);
                    break;
                case _finishMessage:
                    ClientDisconnected(ip_Message[0]);
                    break;
                case _stoppedMessage:
                    SetClientInteractionMode(ip_Message[0], ip_Message[1]);
                    break;
                case _interactionMessage:
                    SetClientInteractionMode(ip_Message[0], ip_Message[1]);
                    break;
                case _pauseMessage:
                    SetClientVideoPause(ip_Message[0]);
                    break;
                case _title_marsMessage:
                    SetClientVideoTitle(ip_Message[0], _title_marsMessage);
                    break;
                case _title_studentMessage:
                    SetClientVideoTitle(ip_Message[0], _title_studentMessage);
                    break;
            }
        }
        public void UDPMessageHandling(string messageFromStudent)
        {
            //Char delimiter = '_';
            string[] ip_Message = messageFromStudent.Split(messageHandlingDelimiter);

            if (string.IsNullOrEmpty(ip_Message[0]) || string.IsNullOrEmpty(ip_Message[1]))
                return;

            switch (ip_Message[1])
            {
                default:
                    SetClientVideoPosition(ip_Message[0], ip_Message[1]);
                    break;
            }
        }
    }
}
