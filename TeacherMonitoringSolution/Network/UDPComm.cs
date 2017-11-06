using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TeacherMonitoringSolution.Network
{
    public class UDPComm
    {
        private const int PORT_NUMBER = 15000;

        private UdpClient udp = null;
        IAsyncResult _asyncResult = null;
        IPAddress localAddress;

        private string _mountMessage = "HMDMount";
        private string _unmountMessage = "HMDUnmount";
        private string _connectMessage = "connected";
        private string _interactionMessage = "Interaction";
        private string _pauseMessage = "Pause";
        private string _title_marsMessage = "Mars";
        private string _title_studentMessage = "Student";
        private string _stoppedMessage = "Stopped";
        private string _finishMessage = "Finished";
        private volatile bool _shouldStop;

        public string _delimiter = "_";

        public void InitUDPSocket()
        {
            try
            {
                if (udp != null)
                {
                    throw new Exception("Already started, stop first");
                }

                udp = new UdpClient();

                IPEndPoint localEp = new IPEndPoint(IPAddress.Any, PORT_NUMBER);

                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.ExclusiveAddressUse = false;
                udp.Client.Bind(localEp);

                string hostName = Dns.GetHostName();
                string myIP = Dns.GetHostEntry(hostName).AddressList[0].ToString();
                localAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                StartListening();
            }
            catch (Exception ex)
            {

            }
        }
        public void Stop()
        {
            try
            {
                if (udp != null)
                {
                    udp.Close();
                    udp = null;
                }
            }
            catch { }
        }

        private void StartListening()
        {
            try
            {
                // ver1
                //_asyncResult = udp.BeginReceive(UDPReceive, new object());
                _asyncResult = udp.BeginReceive(UDPReceive, null);

                // ver2
                //while (!_shouldStop)
                //{
                //    var localEp = new IPEndPoint(IPAddress.Any, PORT_NUMBER);

                //    udp.Client.Bind(localEp);

                //    string hostName = Dns.GetHostName();
                //    string myIP = Dns.GetHostEntry(hostName).AddressList[0].ToString();
                //    localAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                //    var data = udp.Receive(ref localEp);
                //}

                //Stop();


                // ver3
                //var localEp = new IPEndPoint(IPAddress.Any, PORT_NUMBER);

                //udp.Client.Bind(localEp);

                //string hostName = Dns.GetHostName();
                //string myIP = Dns.GetHostEntry(hostName).AddressList[0].ToString();
                //localAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                //_asyncResult = udp.BeginReceive(Receive, null);
            }
            catch (Exception ex)
            {

            }
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }


        public string ReceiveMessage = null;
        private void UDPReceive(IAsyncResult asyncResult)
        {
            try
            {
                if (udp != null)
                {
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, PORT_NUMBER);
                    byte[] bytes = udp.EndReceive(asyncResult, ref ip);
                    string messageContent = Encoding.UTF8.GetString(bytes);
                    ReceiveMessage = ip.Address.ToString() + _delimiter + messageContent;
                    if (OnReceiveMessage != null && localAddress != null && !localAddress.ToString().Equals(ip.Address.ToString()))
                        OnReceiveMessage();

                    StartListening();

#if DEBUG
                    //OnReceiveMessage(message);
                    //Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    //{
                    //    ((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(ip.Address.ToString() + "  " + message);
                    //}));

#else
                    //if (OnReceiveMessage != null && localAddress != null && !localAddress.ToString().Equals(ip.Address.ToString()))
                    //{
                    //    //OnReceiveMessage(message);
                    //    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    //    {
                    //        ((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(message);
                    //    }));
                    //}
#endif
                    //if (OnReceiveMessage != null && localAddress != null && !localAddress.ToString().Equals(ip.Address.ToString()))
                    //    MessageHandler(ip, ReceiveMessage);
                }
            }
            catch (SocketException se)
            {
                Trace.WriteLine(string.Format("SocketException : {0}", se.Message));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Exception : {0}", ex.Message));
            }
        }
        
        private void MessageHandler(IPEndPoint ip, string message)
        {
            if (message.Equals(_unmountMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).ClientDisconnected((ip.Address.ToString()));
                });
            }
            else if (message.Equals(_mountMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).ClientConnected((ip.Address.ToString()));
                });
            }
            else if (message.Equals(_connectMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).ClientConnected((ip.Address.ToString()));
                });
            }
            else if (message.Equals(_finishMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).ClientDisconnected((ip.Address.ToString()));
                });
            }
            else if (message.Equals(_stoppedMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).SetClientInteractionMode((ip.Address.ToString()), message);
                });
            }
            else if (message.Equals(_interactionMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).SetClientInteractionMode((ip.Address.ToString()), message);
                });
            }
            else if (message.Equals(_pauseMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).SetClientVideoPause((ip.Address.ToString()));
                });
            }
            else if (message.Equals(_title_marsMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).SetClientVideoTitle((ip.Address.ToString()), _title_marsMessage);
                });
            }
            else if (message.Equals(_title_studentMessage))
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).SetClientVideoTitle((ip.Address.ToString()), _title_studentMessage);
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
                    ((MainWindow)System.Windows.Application.Current.MainWindow).SetClientVideoPosition((ip.Address.ToString()), message);
                });
            }
        }
        //private void MessageHandler(IPEndPoint ip, string message)
        //{
        //    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,  new Action(delegate {
        //        //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
        //        ((MainWindow)System.Windows.Application.Current.MainWindow).MessageHandling(ip, message);
        //    }));


        //}

        public void SendBroadcastMessage(string message)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), PORT_NUMBER);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            client.Send(bytes, bytes.Length, ip);
            client.Close();

            if (OnSendMessage != null)
                OnSendMessage(message);
        }
        public void SendBroadcastMessage(string clientIP, string message)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(clientIP), PORT_NUMBER);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            client.Send(bytes, bytes.Length, ip);
            client.Close();

            if (OnSendMessage != null)
                OnSendMessage(message);
        }

        public delegate void SendMessageHandler(string message);
        public event SendMessageHandler OnSendMessage;

        public delegate void ReceiveMessageHandler();
        public event ReceiveMessageHandler OnReceiveMessage;
    }
}

