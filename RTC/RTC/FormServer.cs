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

using System.Runtime.Serialization.Formatters.Binary;

namespace RTC {
    public partial class FormServer :Form {
        [Serializable]
        public enum MSG_TYPE {
            MESSAGE,
            EDITOR,
            VOICE,
            COMMAND
        };

        [Serializable]
        public struct Message {
            public MSG_TYPE type;
            public string message;
        };

        TcpListener listener = new TcpListener(IPAddress.Any, 6067);
        public static ArrayList soketArray = new ArrayList();
                    
        public FormServer() {
            InitializeComponent();      // 기본      
        }

        private void btnServerStart_Click(object sender, EventArgs e) {
            if (lblServerState.Text == "서버 상태 : STOP") {
                //Listener Start
                listener.Start();
                
                //Client로 부터 연결을 기다리는 Thread생성
                Thread t_WaitSocket = new Thread(new ThreadStart(WaitSocket));
                t_WaitSocket.IsBackground = true;
                t_WaitSocket.Start();
                lblServerState.Text = "서버 상태 : START";
                btnServerStart.Text = "Server Stop";
            } else {
                listener.Stop();        //listener가 멈추면
                foreach (Socket soket in soketArray) {
                    soket.Close();      //soketArray안에 있는 모든 소켓을 루프시키며 닫는다.
                }
                soketArray.Clear();
                lblServerState.Text = "서버 상태 : STOP";
                btnServerStart.Text = "Server Start";
            }
        }

        private void FormServer_FormClosed(object sender, FormClosedEventArgs e) {
            Application.Exit();
            listener.Stop();
        }

        private void WaitSocket() {
            Socket sktClient = null;    //클라이언트를 비우고
            while (true) {
                try {
                    sktClient = listener.AcceptSocket();    //소켓클라이언트에 listener를 연결
                    CHAT chat = new CHAT();
                    chat.Setup(sktClient, this.txtServerLog);
                    //Chatting을 실행하는Thread 생성 
                    Thread thd_ChatProcess = new Thread(new ThreadStart(chat.Process));
                    thd_ChatProcess.Start();
                    
                    /* 
                     * 입장시 접속자 아이피 출력 소스
                     
                    IPEndPoint remoteIpEndPoint = sktClient.RemoteEndPoint as IPEndPoint;
                    IPEndPoint localIpEndPoint = sktClient.LocalEndPoint as IPEndPoint;
                    if (remoteIpEndPoint != null) {
                        // Using the RemoteEndPoint property.
                        txtServerLog.AppendText("I am connected to " + remoteIpEndPoint.Address +  "on port number " + remoteIpEndPoint.Port + "\r\n");
                    }

                    if (localIpEndPoint != null) {
                        // Using the LocalEndPoint property.
                        txtServerLog.AppendText("My local IpAddress is :" + localIpEndPoint.Address + " I am connected on port number " + localIpEndPoint.Port + "\r\n");
                    }
                    */

                } catch (System.Exception) {
                    FormServer.soketArray.Remove(sktClient);
                    break;
                }
            }
        }
    }

    public class CHAT {
        private Encoding encoding = Encoding.GetEncoding("KS_C_5601-1987");
        private TextBox txtLog;
        private Socket socketClient;
        private NetworkStream networkStream;
        private StreamReader streamReader;

        public void Setup(Socket socketClient, TextBox txtLog) {
            this.txtLog = txtLog;
            this.socketClient = socketClient;
            //Network Stream을 생성
            this.networkStream = new NetworkStream(socketClient);
            FormServer.soketArray.Add(socketClient);
            //Stream Reader을 생성
            this.streamReader = new StreamReader(networkStream, encoding);
        }

        public void Process() {
            while (true) {
                try {                 
                    //
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    Message msg = new Message();
                    msg = (Message)binaryFormatter.Deserialize(networkStream);

                    switch (msg.type) {
                        case MSG_TYPE.MESSAGE:
                            if (msg.message != null && msg.message != "") {
                                if (this.txtLog.TextLength == 0)
                                    this.txtLog.AppendText("\n" + msg.message);
                                else
                                    this.txtLog.AppendText("\r\n" + msg.message);

                                //msg.message = msg.message;                                
                                                                
                                lock (FormServer.soketArray) {
                                    foreach (Socket soket in FormServer.soketArray) { //sokretArray안의 soket값들을 돌리면서
                                        NetworkStream stream = new NetworkStream(soket);
                                        binaryFormatter.Serialize(stream, msg);
                                    }
                                }
                            }                            

                            break;
                        case MSG_TYPE.EDITOR:

                            break;
                        case MSG_TYPE.VOICE:

                            break;
                        case MSG_TYPE.COMMAND:

                            break;
                        default:

                            break;
                    }
                } catch (System.Exception) {
                    FormServer.soketArray.Remove(socketClient);
                    break;
                }
            }
        }
    }
}
