using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;

using PacketLibrary;

namespace RTC {
    public partial class FormServer :Form {
        private static int SERVER_PORT = 6067;

        TcpListener listener = null;                              // 클라이언트 연결용
        public static ArrayList clientList = new ArrayList();     // 다중 클라이언트를 지원하기 위한 pool
        
        Thread listeningThread = null;
        Thread SendingThread = null;
        Thread ReceivingThread = null;
                    
        public FormServer() {
            InitializeComponent();    
        }

        private void btnServerStart_Click(object sender, EventArgs e) {
            this.Invoke(new MethodInvoker(delegate () {
                if (lblServerState.Text == "서버 상태 : STOP") {

                    listeningThread = new Thread(new ThreadStart(ServerStart));
                    listeningThread.Start();

                    lblServerState.Text = "서버 상태 : START";
                    btnServerStart.Text = "Server Stop";

                } else {
                    
                    ServerStop();                    

                    lblServerState.Text = "서버 상태 : STOP";
                    btnServerStart.Text = "Server Start";
                }
            }));
        }

        private void FormServer_FormClosed(object sender, FormClosedEventArgs e) {
            ServerStop();
            Application.Exit();    
        }
        
        public void ServerStart() {
            listener = new TcpListener(IPAddress.Any, SERVER_PORT);
            listener.Start();
            
            AddLog("Server starting...");

            TcpClient client = null;
            while (true) {
                try {
                    client = listener.AcceptTcpClient();
                    if (client.Connected) {
                        clientList.Add(client); // 1:N 지원
                    }

                    // 1:N 지원

                    /*
                    ReceivingThread = new Thread(() => Receive(client));
                    ReceivingThread.Start();
                    */

                    new Thread(delegate () {
                        Receive(client);
                    }).Start();
                    
                } catch {
                    AddLog("at 1st serverstart... " + clientList.Count.ToString());
                    clientList.Remove(client);
                    AddLog("at 2nd serverstart... "+clientList.Count.ToString());
                }
            }
        }
        
        public void ServerStop() {
            if( listener != null )
                listener.Stop();        //listener가 멈추면
            AddLog("count is " + clientList.Count.ToString());
            try {
                MSGText msg = new MSGText(string.Format("<{0}> : {1}", "서버", "서버를 종료합니다."));
                Task t = Task.Run(() =>
                {
                    foreach (TcpClient client in clientList)
                    {
                        if (client != null && client.Connected)
                        {
                            Packet.SendPacket(client.GetStream(), msg);
                        }
                    }
                });
                t.Wait();
                AddLog("wating is finish~" + clientList.Count.ToString());
                //Send(msg, null);

                for (int i = 0; i < clientList.Count; i++)
                {
                    (clientList[i] as TcpClient).Close();
                    AddLog(i.ToString());
                }
                //foreach (TcpClient client in clientList)
                //{
                //    client.Close();      //soketArray안에 있는 모든 소켓을 루프시키며 닫는다.
                //    AddLog(clientList.Count.ToString());
                //}
            } catch(Exception e) {
                AddLog(e.ToString());
            }
            try
            {
                listeningThread.Abort();
            }
            catch (Exception e)
            {
                AddLog(e.ToString());
            }

            clientList.Clear();
            listener = null;
            AddLog("Server Stop");
        }

        public void AddLog(string text) {
            this.Invoke(new MethodInvoker(delegate () {
                if (txtServerLog.Text == string.Empty) {
                    txtServerLog.Text += text;
                } else {
                    txtServerLog.Text += "\r\n" + text;
                }

                txtServerLog.Focus();
                txtServerLog.SelectionStart = txtServerLog.Text.Length;
                txtServerLog.ScrollToCaret();
                lblServerState.Focus();
            }));
        }

        public void Receive(TcpClient client) {
            NetworkStream stream = client.GetStream();
            byte[] buffer = null;
            while (client.Connected ) {
                try {
                    Packet packet = Packet.ReceivePacket(stream, buffer);

                    switch (packet.type) {
                        case PacketType.MESSAGE:
                            ProcessMessage(packet, client);
                            break;

                        case PacketType.VOICE:

                            break;
                    }
                } catch {
                    clientList.Remove(client); // 1:N 지원
                    break;
                }
            }
        }

        public void Send(Packet msg, TcpClient sender) {
            foreach (TcpClient client in clientList) {
                /*
                if (client != sender) {
                    SendingThread = new Thread(() => Packet.SendPacket(client.GetStream(), msg));
                    SendingThread.Start();
                }
                */

                if (client != null && client.Connected && client != sender) {
                    new Thread(delegate () {
                        Packet.SendPacket(client.GetStream(), msg);
                    }).Start();
                }

               // Packet.SendPacket(client.GetStream(), msg);
            }            
        }

        public void ProcessMessage(Packet packet, TcpClient sender) {
            MSGText msg = (MSGText)packet;
            Send(msg, sender);
            AddLog(msg.message);
        }

        public void ProcessVoice(Packet packet, TcpClient sender) {
            MSGVoice msg = (MSGVoice)packet;
            Send(msg, sender);
            AddLog("receive from " + msg.id);
        }

        private void FormServer_Load(object sender, EventArgs e) {
            IPAddress ip = Dns.GetHostAddresses(Dns.GetHostName()).Where(
                                address => address.AddressFamily == AddressFamily.InterNetwork
                           ).ElementAt(2);
            txtIP.Text = ip.ToString();
        }
    }
}
