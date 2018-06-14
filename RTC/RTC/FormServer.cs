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


using Microsoft.DirectX.DirectSound;
using g711audio;

namespace RTC {
    public partial class FormServer :Form {
        private static int SERVER_PORT = 6067;

        TcpListener listener = null;                              // 클라이언트 연결용
        public static ArrayList clientList = new ArrayList();     // 다중 클라이언트를 지원하기 위한 pool

        Thread listeningThread = null;
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



        public FormServer() {
            InitializeComponent();
            UDP_Initialize();
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

                    UDP_UninitializeCall();

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
            UDP_InitializeCall();
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
                    clientList.Remove(client);
                }
            }
        }
        
        public void ServerStop() {
            if( listener != null )
                listener.Stop();        //listener가 멈추면

            try {
                listeningThread.Abort();
                UDP_UninitializeCall();
            } catch {
            }
            
            try {
                MSGText msg = new MSGText(string.Format("<{0}> : {1}", "서버", "서버를 종료합니다."));
                Send(msg, null);

                foreach (TcpClient client in clientList) {
                    client.Close();      //soketArray안에 있는 모든 소켓을 루프시키며 닫는다.
                }
            } catch {
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
                            /* ProcessVoice(packet, client); */
                            break;

                        default:
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
        
        private void FormServer_Load(object sender, EventArgs e) {
            IPAddress ip = Dns.GetHostAddresses(Dns.GetHostName()).Where(
                                address => address.AddressFamily == AddressFamily.InterNetwork
                           ).ElementAt(2);
            txtIP.Text = ip.ToString();
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
                EndPoint ourEP = new IPEndPoint(IPAddress.Any, 9450);
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
                //Get the IP we want to call.
                foreach(TcpClient c in clientList) {
                    if (c.Connected) {
                        string c_ip = ((IPEndPoint)c.Client.RemoteEndPoint).Address.ToString();

                        otherPartyIP = new IPEndPoint(IPAddress.Parse(c_ip), 1550);
                        otherPartyEP = (EndPoint)otherPartyIP;

                        //Get the vocoder to be used.
                        vocoder = Vocoder.ALaw;

                        //Send an invite message.
                        UDP_SendMessage(Command.Invite, otherPartyEP);
                    }
                }
                
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
                UDP_InitializeCall();


                byteData = new byte[1024];

                //Get ready to receive more commands.
                clientSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref receivedFromEP, new AsyncCallback(UDP_OnReceive), null);
            } catch (Exception ex) {
                UDP_UninitializeCall();
                MessageBox.Show(ex.Message, "VoiceChat-OnReceive ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    string sender_ip = remoteEP.Address.ToString();

                    foreach (TcpClient c in clientList) {                        
                        if (c.Connected) {
                            string c_ip = ((IPEndPoint)c.Client.RemoteEndPoint).Address.ToString();

                            if (!c_ip.Equals(sender_ip)) {
                                udpClient.Send(byteData, byteData.Length, c_ip, 1550);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "VoiceChat-Receive ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        }

        private void UDP_DropCall() {
            try {
                //Send a Bye message to the user to end the call.
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
                
                udpClient = new UdpClient(6068);

                Thread receiverThread = new Thread(new ThreadStart(UDP_Receive));
                bIsCallActive = true;

                //Start the receiver and sender thread.
                receiverThread.Start();
                
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

                msgToSend.strName = "Server";   //Name of the user.
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
