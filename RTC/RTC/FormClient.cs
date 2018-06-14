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

using Microsoft.DirectX.DirectSound;
using g711audio;

using PacketLibrary;

namespace RTC {
    public partial class FormClient :Form {
        private static int SERVER_PORT = 6067;

        /* 
         * for text chat
         */ 
        TcpClient client = null;
        NetworkStream stream = null;

        Thread SendingThread = null;
        Thread ReceivingThread = null;

        /*
         * for voice chat
         */
        private CaptureBufferDescription captureBufferDescription;
        private AutoResetEvent autoResetEvent;
        private Notify notify;
        private WaveFormat waveFormat;
        private Capture capture;
        private int bufferSize;
        private CaptureBuffer captureBuffer;
        private UdpClient udpClient = null;                //Listens and sends data on port 1550, used in synchronous mode.
        private Device device;
        private SecondaryBuffer playbackBuffer;
        private BufferDescription playbackBufferDescription;
        private Socket clientSocket;
        private bool bStop;                         //Flag to end the Start and Receive threads.
        private IPEndPoint otherPartyIP;            //IP of party we want to make a call.
        private EndPoint otherPartyEP;
        private volatile bool bIsCallActive;                 //Tells whether we have an active call.
        private Vocoder vocoder;
        private byte[] byteData = new byte[1024];   //Buffer to store the data received.
        private volatile int nUdpClientFlag;                 //Flag used to close the udpClient socket.


        public FormClient() {
            InitializeComponent();
            UDP_Initialize();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e) {
            this.Invoke(new MethodInvoker(delegate () {
                if (e.KeyChar == Convert.ToChar(Keys.Enter)) {      // Enter키
                    if (btnConnect.Text == "Log-Out") {             // 접속 상태
                        if (txtMessage.Text.Trim() != string.Empty) {      // 빈 값인지 확인
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
                    UDP_UninitializeCall();

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

            if (bIsCallActive) {
                UDP_UninitializeCall();
                UDP_DropCall();
            }

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
            
            Packet.SendPacket(stream, msg);
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

                        case PacketType.VOICE:
                            /* ProcessVoice(packet); */
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

        



        public void btnVoiceChat_Click(object sender, EventArgs e) {
            this.Invoke(new MethodInvoker(() => {
                if( btnVoiceChat.Text.Equals("Start Voice Chat")) {
                    UDP_Call();
                    UDP_InitializeCall();
                } else {
                    UDP_DropCall();
                }
            }));
        }

        private void UDP_Initialize() {
            try {
                device = new Device();
                device.SetCooperativeLevel(this, CooperativeLevel.Normal);

                CaptureDevicesCollection captureDeviceCollection = new CaptureDevicesCollection();

                DeviceInformation deviceInfo = captureDeviceCollection[0];

                capture = new Capture(deviceInfo.DriverGuid);

                short channels = 1; //Stereo.
                short bitsPerSample = 16; //16Bit, alternatively use 8Bits.
                int samplesPerSecond = 22050; //11KHz use 11025 , 22KHz use 22050, 44KHz use 44100 etc.

                //Set up the wave format to be captured.
                waveFormat = new WaveFormat();
                waveFormat.Channels = channels;
                waveFormat.FormatTag = WaveFormatTag.Pcm;
                waveFormat.SamplesPerSecond = samplesPerSecond;
                waveFormat.BitsPerSample = bitsPerSample;
                waveFormat.BlockAlign = (short)(channels * (bitsPerSample / (short)8));
                waveFormat.AverageBytesPerSecond = waveFormat.BlockAlign * samplesPerSecond;

                captureBufferDescription = new CaptureBufferDescription();
                captureBufferDescription.BufferBytes = waveFormat.AverageBytesPerSecond / 5;//approx 200 milliseconds of PCM data.
                captureBufferDescription.Format = waveFormat;

                playbackBufferDescription = new BufferDescription();
                playbackBufferDescription.BufferBytes = waveFormat.AverageBytesPerSecond / 5;
                playbackBufferDescription.Format = waveFormat;
                playbackBuffer = new SecondaryBuffer(playbackBufferDescription, device);

                bufferSize = captureBufferDescription.BufferBytes;

                bIsCallActive = false;
                nUdpClientFlag = 0;

                //Using UDP sockets
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                EndPoint ourEP = new IPEndPoint(IPAddress.Any, 1450);
                //Listen asynchronously on port 1450 for coming messages (Invite, Bye, etc).
                clientSocket.Bind(ourEP);

                //Receive data from any IP.
                EndPoint remoteEP = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));

                byteData = new byte[1024];
                //Receive data asynchornously.
                clientSocket.BeginReceiveFrom(byteData,
                                           0, byteData.Length,
                                           SocketFlags.None,
                                           ref remoteEP,
                                           new AsyncCallback(UDP_OnReceive),
                                           null);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-Initialize ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UDP_Call() {
            try {
                this.Invoke(new MethodInvoker(() => {
                    btnVoiceChat.Text = "Stop Voice Chat";
                    btnVoiceChat.ForeColor = Color.Red;
                }));

                //Get the IP we want to call.
                otherPartyIP = new IPEndPoint(IPAddress.Parse(txtServerIP.Text), 6068);
                otherPartyEP = (EndPoint)otherPartyIP;

                //Get the vocoder to be used.
                vocoder = Vocoder.ALaw;
                
                //Send an invite message.
                UDP_SendMessage(Command.Invite, otherPartyEP);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-Call ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UDP_OnSend(IAsyncResult ar) {
            try {
                clientSocket.EndSendTo(ar);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-OnSend ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
         * Commands are received asynchronously. OnReceive is the handler for them.
         */
        private void UDP_OnReceive(IAsyncResult ar) {
            try {
                EndPoint receivedFromEP = new IPEndPoint(IPAddress.Any, 0);

                //Get the IP from where we got a message.
                clientSocket.EndReceiveFrom(ar, ref receivedFromEP);

                //Convert the bytes received into an object of type Data.
                Data msgReceived = new Data(byteData);

                
                vocoder = msgReceived.vocoder;
                otherPartyEP = receivedFromEP;
                otherPartyIP = (IPEndPoint)receivedFromEP;
            

                byteData = new byte[1024];

                //Get ready to receive more commands.
                clientSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref receivedFromEP, new AsyncCallback(UDP_OnReceive), null);
            } catch (Exception ex) {
                UDP_UninitializeCall();
                MessageBox.Show(ex.Message, "VoiceChat-OnReceive ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
         * Send synchronously sends data captured from microphone across the network on port 1550.
         */
        private void UDP_Send() {
            try {
                //The following lines get audio from microphone and then send them 
                //across network.

                captureBuffer = new CaptureBuffer(captureBufferDescription, capture);

                UDP_CreateNotifyPositions();

                int halfBuffer = bufferSize / 2;

                captureBuffer.Start(true);

                bool readFirstBufferPart = true;
                int offset = 0;

                MemoryStream memStream = new MemoryStream(halfBuffer);
                bStop = false;
                while (!bStop) {
                    autoResetEvent.WaitOne();
                    memStream.Seek(0, SeekOrigin.Begin);
                    captureBuffer.Read(offset, memStream, halfBuffer, LockFlag.None);
                    readFirstBufferPart = !readFirstBufferPart;
                    offset = readFirstBufferPart ? 0 : halfBuffer;

                    //TODO: Fix this ugly way of initializing differently.

                    //Choose the vocoder. And then send the data to other party at port 1550.

                    
                    byte[] dataToWrite = ALawEncoder.ALawEncode(memStream.GetBuffer());
                    udpClient.Send(dataToWrite, dataToWrite.Length, otherPartyIP.Address.ToString(), 6068);
                    
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-Send ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                captureBuffer.Stop();

                //Increment flag by one.
                nUdpClientFlag += 1;

                //When flag is two then it means we have got out of loops in Send and Receive.
                while (nUdpClientFlag != 2) { }

                //Clear the flag.
                nUdpClientFlag = 0;

                //Close the socket.
                udpClient.Close();
            }
        }

        /*
         * Receive audio data coming on port 1550 and feed it to the speakers to be played.
         */
        private void UDP_Receive() {
            try {
                bStop = false;
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                
                while (!bStop) {
                    //Receive data.
                    byte[] byteData = udpClient.Receive(ref remoteEP);

                    string c_ip = remoteEP.Address.ToString();


                    //G711 compresses the data by 50%, so we allocate a buffer of double
                    //the size to store the decompressed data.
                    byte[] byteDecodedData = new byte[byteData.Length * 2];

                    //Decompress data using the proper vocoder.                    
                    ALawDecoder.ALawDecode(byteData, out byteDecodedData);

                    //Play the data received to the user.
                    playbackBuffer = new SecondaryBuffer(playbackBufferDescription, device);
                    playbackBuffer.Write(0, byteDecodedData, LockFlag.None);
                    playbackBuffer.Play(0, BufferPlayFlags.Default);
                }
            } catch (Exception ex) {
                //MessageBox.Show(ex.Message, "VoiceChat-Receive ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                nUdpClientFlag += 1;
            }
        }

        private void UDP_CreateNotifyPositions() {
            try {
                autoResetEvent = new AutoResetEvent(false);
                notify = new Notify(captureBuffer);
                BufferPositionNotify bufferPositionNotify1 = new BufferPositionNotify();
                bufferPositionNotify1.Offset = bufferSize / 2 - 1;
                bufferPositionNotify1.EventNotifyHandle = autoResetEvent.SafeWaitHandle.DangerousGetHandle();
                BufferPositionNotify bufferPositionNotify2 = new BufferPositionNotify();
                bufferPositionNotify2.Offset = bufferSize - 1;
                bufferPositionNotify2.EventNotifyHandle = autoResetEvent.SafeWaitHandle.DangerousGetHandle();

                notify.SetNotificationPositions(new BufferPositionNotify[] { bufferPositionNotify1, bufferPositionNotify2 });
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-CreateNotifyPositions ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UDP_UninitializeCall() {
            //Set the flag to end the Send and Receive threads.
            bStop = true;

            bIsCallActive = false;
            this.Invoke(new MethodInvoker(() => {
                btnVoiceChat.Text = "Start Voice Chat";
                btnVoiceChat.ForeColor = Color.Black;
            }));
        }

        private void UDP_DropCall() {
            try {
                this.Invoke(new MethodInvoker(() => {
                    btnVoiceChat.Text = "Start Voice Chat";
                    btnVoiceChat.ForeColor = Color.Black;
                }));

                //Send a Bye message to the user to end the call.
                UDP_SendMessage(Command.Bye, otherPartyEP);
                UDP_UninitializeCall();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-DropCall ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UDP_InitializeCall() {
            try {
                //Start listening on port 1500.
                if (udpClient != null)
                    udpClient.Close();

                udpClient = new UdpClient(1550);

                Thread senderThread = new Thread(new ThreadStart(UDP_Send));
                Thread receiverThread = new Thread(new ThreadStart(UDP_Receive));
                bIsCallActive = true;

                //Start the receiver and sender thread.
                receiverThread.Start();
                senderThread.Start();
                this.Invoke(new MethodInvoker(() => {
                    btnVoiceChat.Text = "Stop Voice Chat";
                    btnVoiceChat.ForeColor = Color.Red;
                }));
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-InitializeCall ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
         * Send a message to the remote party.
         */
        private void UDP_SendMessage(Command cmd, EndPoint sendToEP) {
            try {
                //Create the message to send.
                Data msgToSend = new Data();

                msgToSend.strName = txtID.Text;   //Name of the user.
                msgToSend.cmdCommand = cmd;         //Message to send.
                msgToSend.vocoder = vocoder;        //Vocoder to be used.

                byte[] message = msgToSend.ToByte();

                //Send the message asynchronously.
                clientSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, sendToEP, new AsyncCallback(UDP_OnSend), null);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-SendMessage ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}