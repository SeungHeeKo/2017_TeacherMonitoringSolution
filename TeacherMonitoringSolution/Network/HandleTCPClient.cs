using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeacherMonitoringSolution.Network
{
    class HandleTCPClient
    {
        TcpClient clientSocket;
        int clientNo;

        public void startClient(TcpClient ClientSocket, int clientNo)
        {
            this.clientSocket = ClientSocket;
            this.clientNo = clientNo;

            Thread t_hanlder = new Thread(doChat);
            t_hanlder.IsBackground = true;
            t_hanlder.Start();
        }

        public delegate void MessageDisplayHandler(string text);
        public event MessageDisplayHandler OnReceived;

        public delegate void CalculateClientCounter();
        public event CalculateClientCounter OnCalculated;


        int BUFSIZE = 256;
        int servPort = 1551;
        public static String ClientOrder = "";
        public static int bytesRcvd;
        public string receivedTCPMessage;
        public void RunServer()
        {
            NetworkStream netStream = null;
            byte[] rcvBuffer = new byte[BUFSIZE];
            while (true)
            {
                netStream = clientSocket.GetStream();
                {
                    rcvBuffer = new byte[BUFSIZE];
                    bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length);
                    //ClientOrder = (Encoding.ASCII.GetString(rcvBuffer)).Substring(0, bytesRcvd);
                    ClientOrder = (Encoding.ASCII.GetString(rcvBuffer,0, bytesRcvd));
                    netStream.Close();
                    clientSocket.Close();
                }
            }
        }
        private void doChat()
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
                        stream = clientSocket.GetStream();
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

                            receivedTCPMessage = "Data Received : " + receivedTCPMessage;

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

                if (OnCalculated != null)
                    OnCalculated();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("doChat - Exception : {0}", ex.Message));

                if (clientSocket != null)
                {
                    clientSocket.Close();
                    stream.Close();
                }

                if (OnCalculated != null)
                    OnCalculated();
            }
        }

    }
}
