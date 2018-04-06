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

using SlimDX.DirectSound;
using SlimDX.Multimedia;

using System.Runtime.Serialization.Formatters.Binary;


namespace RTC {
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
                if (btnConnect.Text == "Log-Out") {  //Text가 로그아웃이면                    
                    Message msg = new Message();
                    msg.type = MSG_TYPE.MESSAGE;
                    msg.message = "<" + txtID.Text + "> " + txtMessage.Text;

                    StructSend(msg);
                }

                txtMessage.Clear();
                txtMessage.Focus();

                e.Handled = true;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            if (btnConnect.Text == "Log-In") { //로그인 ok
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
                    btnConnect.Text = "Log-Out";        //접속문구를 열어주고, 연결텍스트에는 Logout을 출력
                    
                    Message msg = new Message();
                    msg.type = MSG_TYPE.MESSAGE;
                    msg.message = "<" + txtID.Text + "> 님께서 접속 하셨습니다.";

                    StructSend(msg);
                    
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
            
            Message msg = new Message();
            msg.type = MSG_TYPE.MESSAGE;
            msg.message = "<" + txtID.Text + "> 님께서 접속해제 하셨습니다.";

            StructSend(msg);
            

            chat.Close();               //Text에 입력된 이름과 문구를 출력
            networkStream.Close();      //그렇지 않으면 ntwStream과 tcpClient를 모두 닫는다.
            tcpClient.Close();
        }

        private void MessageSend(string message, Boolean isMsg) {
            try {
                //보낼 데이터를 읽어 Default 형식의 바이트 스트림으로 변환 해서 전송
                string dataToSend = "\n" + message;
                byte[] data = Encoding.Unicode.GetBytes(dataToSend);
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

        private void StructSend(Message msg) {
            try {
                BinaryFormatter binaryFormatter = new BinaryFormatter();               
                switch ( msg.type ) {
                    case MSG_TYPE.MESSAGE:
                        //msg.message = msg.message;
                        binaryFormatter.Serialize(networkStream, msg);

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

            } catch (System.Exception Err) {
                if (msg.type == MSG_TYPE.COMMAND) {
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
                        //string message = streamReader.ReadLine();
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        Message msg = new Message();
                        msg = (Message)binaryFormatter.Deserialize(networkStream);

                        switch (msg.type) {
                            case MSG_TYPE.MESSAGE:
                                if (msg.message != null && msg.message != "") {
                                    if( this.txtLog.TextLength == 0 ) // 처음 데이터 추가시 \n만 해줌, 처음 데이터 추가시 \r\n하면 두줄이 추가됨
                                        this.txtLog.AppendText("\n" + msg.message);
                                    else
                                        this.txtLog.AppendText("\r\n" + msg.message);
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
                        break;
                    }
                }
            }

            public void Close() {
                networkStream.Close();
                streamReader.Close();
            }
        }


        // 하위 메소드 들은 음성 채팅을 위한 소스이며 아직 구현 중
        // 참조: http://powerprog.blogspot.kr/2012/05/blog-post_14.html
        private void FormClient_Load(object sender, EventArgs e) {
            //DeviceCollection devices = GetAll();

            DeviceCollection coll = DirectSoundCapture.GetDevices();
            foreach (DeviceInformation dev in coll) {
                cmbDeviceList.Items.Add(dev.Description.ToString());
            }
            
        }

        DirectSound _soundDevice;
        int devicenum = 0;

        public static DeviceCollection GetAll() {
            return DirectSound.GetDevices();            
        }

        public void CreateDevice() {
            var dev = DirectSound.GetDevices()[devicenum];
            DirectSound _soundDevice = new DirectSound(dev.DriverGuid);
        }

        public void SetLevel() {
            _soundDevice.SetCooperativeLevel(this.Handle, CooperativeLevel.Normal);
        }

        public void SetBuffer() {
            // 먼저 출력 데이터의 포맷을 설정 
            var waveFormat = new WaveFormat();
            waveFormat.Channels = 1;
            waveFormat.FormatTag = WaveFormatTag.Pcm;
            waveFormat.SamplesPerSecond = 22050;
            waveFormat.BitsPerSample = 16;
            waveFormat.BlockAlignment = 2;
            waveFormat.AverageBytesPerSecond = 2 * 22050;

            // 버퍼의 특성 정의
            var _description = new SoundBufferDescription();
            _description.SizeInBytes = waveFormat.AverageBytesPerSecond / 5;
            _description.Format = waveFormat;
        }
        /*
        public void CreateBuffer() {
            Buffer = new SecondarySoundBuffer(_soundDevice, _description);
        }*/
    }
}