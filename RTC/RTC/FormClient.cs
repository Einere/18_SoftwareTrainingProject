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
using System.Threading;

using SlimDX.DirectSound;
using SlimDX.Multimedia;

using PacketLibrary;

namespace RTC {
    public partial class FormClient :Form {
        private static int SERVER_PORT = 6067;

        TcpClient client = null;
        NetworkStream stream = null;

        Thread SendingThread = null;
        Thread ReceivingThread = null;

        public FormClient() {
            InitializeComponent();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e) {
            this.Invoke(new MethodInvoker(delegate () {
                if (e.KeyChar == Convert.ToChar(Keys.Enter)) {      // Enter키
                    if (btnConnect.Text == "Log-Out") {             // 접속 상태
                        if (txtMessage.Text != string.Empty) {      // 빈 값인지 확인
                            string text = string.Format("<{0}> : {1}", txtID.Text, txtMessage.Text);
                            MSGText msg = new MSGText(text);
                            Send(msg);
                            AddLog(text);
                            txtMessage.Clear();
                            txtMessage.Focus();
                            e.Handled = true;
                        }
                    }
                }
            }));            
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            this.Invoke(new MethodInvoker(delegate () {
                if (btnConnect.Text == "Log-In") { 
                     // pre-condition
                    if (txtServerIP.Text == string.Empty ||
                              txtID.Text == string.Empty    ) {

                        MessageBox.Show("IP와 ID 모두 입력해주세요!");
                        return;
                    }
                    
                    Connect();

                } else {

                    MSGText msg = new MSGText(string.Format("<{0}>님께서 접속 해제하셨습니다.", txtID.Text));
                    Packet.SendPacket(stream, msg);
                    Disconnect();

                }
            }));
        }

        private void FormClient_FormClosed(object sender, FormClosedEventArgs e) {
            this.Invoke(new MethodInvoker(delegate () {
                if (btnConnect.Text == "Log-In") {    //Text에 Login이 뜨면
                    return;
                } else {
                    MSGText msg = new MSGText(string.Format("<{0}>님께서 접속 해제하셨습니다.", txtID.Text));
                    Packet.SendPacket(stream, msg);
                    Disconnect();
                }
            }));

            Application.Exit();
        }

        public void Connect() {
            this.Invoke(new MethodInvoker(delegate () {
                try {
                    client = new TcpClient();  // 소켓 생성
                    client.Connect(txtServerIP.Text, SERVER_PORT);

                    // 송수신 스트림을 가져온다.
                    stream = client.GetStream();
                } catch {
                    MessageBox.Show("해당 서버가 열려있지 않습니다!");
                    return;
                }

                Thread thd_Receive = new Thread(new ThreadStart(Receive));
                thd_Receive.Start();

                MSGText msg = new MSGText(string.Format("<{0}>님께서 접속하셨습니다.", txtID.Text));
                Packet.SendPacket(stream, msg);

                btnConnect.Text = "Log-Out";
                txtMessage.Focus();
            }));
        }
        
        public void Disconnect() {
            this.Invoke(new MethodInvoker(delegate () {
                try {
                    stream.Close();
                    client.Close();
                    ReceivingThread.Abort();
                } catch {
                }

                btnConnect.Text = "Log-In";
            }));
        }

        public void Send(Packet msg) {
            /*
            SendingThread = new Thread(() => Packet.SendPacket(stream, msg));
            SendingThread.Start();
            */

            new Thread(delegate () {
                Packet.SendPacket(stream, msg);
            }).Start();

            //Packet.SendPacket(stream, msg);
        }

        public void Receive() {
            byte[] buffer = null;

            while (true) {
                try {
                    Packet packet = Packet.ReceivePacket(stream, buffer);                    
                    switch (packet.type) {
                        case PacketType.MESSAGE:   
                            ProcessMessage(packet);
                            break;                            

                        default:
                            break;
                    }
                } catch (Exception ex) {
                    AddLog("서버와의 연결을 해제합니다.");
                    Disconnect();
                    break;
                }
            }
        }

        public void AddLog(string text) {
            this.Invoke(new MethodInvoker(delegate () {
                if (txtClientLog.Text == string.Empty) {
                    txtClientLog.Text += text;
                } else {
                    txtClientLog.Text += "\r\n" + text;
                }

                txtClientLog.Focus();
                txtClientLog.SelectionStart = txtClientLog.Text.Length;
                txtClientLog.ScrollToCaret();
                txtMessage.Focus();
            }));
        }

        public void ProcessMessage(Packet packet) {
            MSGText msg = (MSGText)packet;
            AddLog(msg.message);
        }

        // 하위 메소드 들은 음성 채팅을 위한 소스이며 아직 구현 중
        // 참조: http://powerprog.blogspot.kr/2012/05/blog-post_14.html
        private void FormClient_Load(object sender, EventArgs e) {
            DeviceCollection devices = GetAllDevices();
            foreach (DeviceInformation device in devices) {

                cmbDeviceList.Items.Add(device.Description.ToString());

            }            
        }

        DirectSound _soundDevice;
        int devicenum = 0;

        public static DeviceCollection GetAllDevices() {
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