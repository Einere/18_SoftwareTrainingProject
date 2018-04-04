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
    public partial class FormClient :Form {
        TcpClient tcpClient = null;
        NetworkStream networkStream = null;
        //Chatting을 실행하는 Class 인스턴스화시킴
        CHAT chat = new CHAT();

        public FormClient() {
            InitializeComponent();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == Convert.ToChar(Keys.Enter)) {
                if (btnConnect.Text == "Log-Out")   //Text가 로그아웃이면
                    {
                    MessageSend("<" + txtID.Text + "> " + txtMessage.Text, true);    //메시지를 보냄
                }

                txtMessage.Clear();
                txtMessage.Focus();

                e.Handled = true;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            if (btnConnect.Text == "Log-In")        //로그인 ok
                {
                try {
                    //IP Address 할당
                    IPAddress ipaAddress = IPAddress.Parse(txtServerIP.Text);
                    //TCP Client 선언
                    tcpClient = new TcpClient();
                    //TCP Client연결
                    tcpClient.Connect(ipaAddress, 6067);
                    networkStream = tcpClient.GetStream();
                    chat.Setup(networkStream, this.txtClientLog);
                    //Thread를 생성하고 Start
                    Thread thd_Receive = new Thread(new ThreadStart(chat.Process));
                    thd_Receive.Start();
                    MessageSend("<" + txtID.Text + "> 님께서 접속 하셨습니다.", true);
                    btnConnect.Text = "Log-Out";        //접속문구를 열어주고, 연결텍스트에는 Logout을 출력
                } catch (System.Exception Err) {
                    MessageBox.Show("Chatting Server 오류발생 또는 Start 되지 않았거나\n\n" + Err.Message, "Client");
                }
            } else {
                MessageSend("<" + txtID.Text + "> 님께서 접속해제 하셨습니다.", false);
                btnConnect.Text = "Log-In";
                chat.Close();
                networkStream.Close();
                tcpClient.Close();
            }
        }

        private void FormClient_FormClosed(object sender, FormClosedEventArgs e) {
            if (btnConnect.Text == "Log-In") {    //Text에 Login이 뜨면
                return;
            }

            MessageSend("<" + txtID.Text + "> 님께서 접속해제 하셨습니다.", false);
            chat.Close();     //Text에 입력된 이름과 문구를 출력
            networkStream.Close();      //그렇지 않으면 ntwStream과 tcpClient를 모두 닫는다.
            tcpClient.Close();
        }

        private void MessageSend(string message, Boolean isMsg) {
            try {
                //보낼 데이터를 읽어 Default 형식의 바이트 스트림으로 변환 해서 전송
                string dataToSend = message + "\r\n";
                byte[] data = Encoding.Default.GetBytes(dataToSend);
                networkStream.Write(data, 0, data.Length);
            } catch (System.Exception Err) {
                if (isMsg == true) {
                    MessageBox.Show("Chatting Server가 오류발생 또는 Start 되지 않았거나\n\n" + Err.Message, "Client");
                    btnConnect.Text = "Log-In";
                    chat.Close();
                    networkStream.Close();
                    tcpClient.Close();
                }
            }
        }

        public class CHAT {
            private Encoding encoding = Encoding.GetEncoding("KS_C_5601-1987");
            private TextBox txtLog;
            private NetworkStream networkStream;
            private StreamReader streamReader;

            public void Setup(NetworkStream networkStream, TextBox txtLog) {
                this.txtLog = txtLog;
                this.networkStream = networkStream;
                this.streamReader = new StreamReader(networkStream, encoding);
            }

            public void Process() {
                while (true) {
                    try {
                        string message = streamReader.ReadLine();

                        if (message != null && message != "") {
                            this.txtLog.AppendText(message + "\r\n");
                        }
                    } catch (System.Exception) {
                        break;
                    }
                }
            }

            public void Close() {
                networkStream.Close();
                streamReader.Close();
            }
        }
    }
}