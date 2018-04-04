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

namespace RTC {
    public partial class FormServer :Form {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6067);
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
                    string message = streamReader.ReadLine();
                    if (message != null && message != "") {
                        this.txtLog.AppendText(message + "\r\n");    //줄 바꿈을 하며 텍스트 내용을 더해나감 
                        byte[] sendData = Encoding.Default.GetBytes(message + "\r\n");
                        ArrayList remove_soketArray = new ArrayList();
                        lock (FormServer.soketArray) {
                            foreach (Socket soket in FormServer.soketArray) { //sokretArray안의 soket값들을 돌리면서
                                NetworkStream stream = new NetworkStream(soket);
                                stream.Write(sendData, 0, sendData.Length);
                            }
                        }
                    }
                } catch (System.Exception) {
                    FormServer.soketArray.Remove(socketClient);
                    break;
                }
            }
        }
    }
}
