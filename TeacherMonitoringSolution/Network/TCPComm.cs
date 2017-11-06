using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TeacherMonitoringSolution.Network
{
    public class TCPComm
    {
        private const int PORT_NUMBER = 15000;

        TcpListener tcp = null;
        TcpClient client = null;
        Socket clientSocket = null;
        static int counter = 0;


        public delegate void MessageDisplayHandler(string text);
        public event MessageDisplayHandler OnReceived;

        int BUFSIZE = 256;
        public string _delimiter = "_";
        public static int bytesRcvd;
        public string receivedTCPMessage;
        
        //Thread tcpThread;

        public TCPComm()
        {
        }
        public void Start()
        {
            //// socket start
            //tcpThread = new Thread(InitSocket);
            //tcpThread.IsBackground = true;
            //tcpThread.Start();
        }

        public void InitSocket()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, PORT_NUMBER);

                tcp = new TcpListener(ipep);
                client = default(TcpClient);
                tcp.Start();
                while (true)
                {
                    try
                    {
                        counter++;
                        client = tcp.AcceptTcpClient();                        

                        Thread tcpReceiveThread = new Thread(TcpReceive);
                        tcpReceiveThread.IsBackground = true;
                        tcpReceiveThread.Start();
                    }
                    catch (SocketException se)
                    {
                        Trace.WriteLine(string.Format("InitSocket - SocketException : {0}", se.Message));
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(string.Format("InitSocket - Exception : {0}", ex.Message));
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                clientSocket.Close();
            }
        }

        private void TcpReceive()
        {
            NetworkStream stream = null;
            try
            {
                byte[] buffer = new byte[BUFSIZE];
                receivedTCPMessage = string.Empty;
                int bytes = 0;
                int MessageCount = 0;

                while (true)
                {
                    try
                    {
                        MessageCount++;
                        stream = client.GetStream();
                        bytes = stream.Read(buffer, 0, buffer.Length);
                        if (bytes == 0)
                            break;
                        receivedTCPMessage = Encoding.UTF8.GetString(buffer);
                        //receivedTCPMessage = Encoding.Unicode.GetString(buffer, 0, bytes);
                        if (receivedTCPMessage.Length <= 0)
                            break;
                        receivedTCPMessage = receivedTCPMessage.Substring(0, receivedTCPMessage.IndexOf("$"));
                        if (!string.IsNullOrEmpty(receivedTCPMessage))
                        {

                            //receivedTCPMessage = "Data Received : " + receivedTCPMessage;
                            receivedTCPMessage = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + _delimiter + receivedTCPMessage;
                            //_responses.Enqueue(receivedTCPMessage);
                            if (OnReceived != null)
                                OnReceived(receivedTCPMessage);
                        }


                        //receivedTCPMessage = "Server to client(" + clientNo.ToString() + ") " + MessageCount.ToString();
                        //if (OnReceived != null)
                        //    OnReceived(receivedTCPMessage);

                        //byte[] sbuffer = Encoding.Unicode.GetBytes(receivedTCPMessage);
                        //stream.Write(sbuffer, 0, sbuffer.Length);
                        //stream.Flush();

                        //receivedTCPMessage = " >> " + receivedTCPMessage;
                        //if (OnReceived != null)
                        //{
                        //    OnReceived(receivedTCPMessage);
                        //    OnReceived("");
                        //}

                        //Byte[] bytes = new Byte[10025];
                        //stream.Read(bytes, 0, (int)clientSocket.ReceiveBufferSize);
                        //string dataClient = System.Text.Encoding.ASCII.GetString(bytes);
                        //Console.WriteLine("Data from Client: " + dataClient);
                        //if (OnReceived != null)
                        //    OnReceived(dataClient);

                        //stream.Flush();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            catch (SocketException se)
            {
                Trace.WriteLine(string.Format("doChat - SocketException : {0}", se.Message));

                if (clientSocket != null)
                {
                    clientSocket.Close();
                    stream.Close();
                }

                //if (OnCalculated != null)
                //    OnCalculated();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("doChat - Exception : {0}", ex.Message));

                if (clientSocket != null)
                {
                    clientSocket.Close();
                    stream.Close();
                }

                //if (OnCalculated != null)
                //    OnCalculated();
            }
        }

        private void DisplayText(string text)
        {
            //Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)(delegate {
            //    //((MainWindow)System.Windows.Application.Current.MainWindow).PrintMessage(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + " " + counter);
            //    ((MainWindow)System.Windows.Application.Current.MainWindow).textBox.Text += text;
            //}));
        }
        private void CalculateCounter()
        {
            counter--;
        }
    }
}
