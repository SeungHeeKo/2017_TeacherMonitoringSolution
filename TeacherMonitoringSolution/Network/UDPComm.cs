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
    /**
     * 프로젝트 이름 : TeacherMonitoringSolution
     * GIT HUB ADDRESS : https://github.com/SeungHeeKo/2017_TeacherMonitoringSolution
     * 개발환경 : WPF (C#)
     * 개발 내용 : 20대의 안드로이드 디바이스와 태블릿PC 간의 TCP, UDP를 통한 네트워크 통신 및 제어.
     * 콘텐츠 내용 : 4차 산업혁명 진로체험 VR 실행 중인 20대의 안드로이드와 통신하여 모니터링 및 제어.
     * 핵심개발자 : 고승희 
     * 개발시작일 : 2017년 9월 1일
     * **/
    public class UDPComm
    {
        private const int PORT_NUMBER = 15000;

        private UdpClient udp = null;
        IAsyncResult _asyncResult = null;
        IPAddress localAddress;
        
        public string _delimiter = "_";

        public string ReceiveMessage = null;

        /// <summary>
        /// UDP client socket 생성
        /// 
        /// MainThread가 돌아가는 코드에서 아래와 같이 사용하시면 됩니다.
        /// 
        /// UDPComm udpComm = new UDPComm();
        /// udpComm.OnReceiveMessage += new UDPComm.ReceiveMessageHandler(udpComm_OnReceiveMessage);
        /// Thread udpThread = new Thread(udpComm.InitUDPSocket);
        /// udpThread.IsBackground = true;
        /// udpThread.Start();
        /// 
        /// 
        /// udpComm_OnReceiveMessage는 MainThread에서 UDP 메세지 수신 이벤트를 핸들링하는 함수입니다. 아래와 같이 사용하시면 됩니다.
        /// UDPComm 객체가 별도의 스레드에서 동작하기 때문에 MainThread에서 UDP 메세지(udpComm.ReceiveMessage)를 처리하기 위해서 Dispatcher를 사용하셔야 합니다.
        /// UDPMessageHandling 함수도 마찬가지로 MainThread에서 동작하는 함수입니다.
        /// void udpComm_OnReceiveMessage()
        /// {
        ///     Dispatcher.Invoke((Action)delegate () { UDPMessageHandling(udpComm.ReceiveMessage); });
        /// }
        /// 
        /// </summary>
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
        /// <summary>
        /// UDP 통신 종료
        /// </summary>
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

        /// <summary>
        /// 비동기로 UDP 통신 처리
        /// </summary>
        private void StartListening()
        {
            try
            {
                _asyncResult = udp.BeginReceive(UDPReceive, null);
            }
            catch (Exception ex)
            {

            }
        }
        
        /// <summary>
        /// UDP 메세지를 받는 함수
        /// </summary>
        /// <param name="asyncResult"></param>
        private void UDPReceive(IAsyncResult asyncResult)
        {
            try
            {
                if (udp != null)
                {
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, PORT_NUMBER);
                    byte[] bytes = udp.EndReceive(asyncResult, ref ip);
                    string messageContent = Encoding.UTF8.GetString(bytes);
                    // client IP_message 형태로 메세지 저장
                    ReceiveMessage = ip.Address.ToString() + _delimiter + messageContent;
                    if (OnReceiveMessage != null && localAddress != null && !localAddress.ToString().Equals(ip.Address.ToString()))
                        OnReceiveMessage();

                    // 수신한 UDP 메세지 처리 후 다시 Listening 상태로.
                    StartListening();
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
        
        /// <summary>
        /// 같은 네트워크에 연결된 모든 호스트에 메세지 전송
        /// </summary>
        /// <param name="message">전송할 메세지</param>
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
        /// <summary>
        /// 특정 clientIP에 message 전송
        /// </summary>
        /// <param name="clientIP">특정 IP주소 ex) 192.168.0.1</param>
        /// <param name="message">특정 호스트에 전송할 메세지</param>
        public void SendMessage(string clientIP, string message)
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

        /// <summary>
        /// UDP 메세지 송신 이벤트 처리
        /// </summary>
        /// <param name="message">전송할 메세지</param>
        public delegate void SendMessageHandler(string message);
        public event SendMessageHandler OnSendMessage;

        /// <summary>
        /// UDP 메세지 수신 이벤트 처리
        /// </summary>
        public delegate void ReceiveMessageHandler();
        public event ReceiveMessageHandler OnReceiveMessage;
    }
}

