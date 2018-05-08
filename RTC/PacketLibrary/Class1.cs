using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PacketLibrary {
    public enum PacketType {
        NONE,
        MESSAGE,
        INIT
    }
    
    [Serializable]
    public class Packet {
        public PacketType type;

        public Packet() {
            this.type = PacketType.NONE;
        }

        public Packet(PacketType _type) {
            this.type = _type;
        }

        public static byte[] Serialize(Object o) {
            MemoryStream ms = new MemoryStream(1024 * 4);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return ms.ToArray();
        }

        public static Object Deserialize(byte[] bt) {
            MemoryStream ms = new MemoryStream(1024 * 4);
            foreach (byte b in bt) {
                ms.WriteByte(b);
            }

            ms.Position = 0;
            BinaryFormatter bf = new BinaryFormatter();
            Object obj = bf.Deserialize(ms);
            ms.Close();
            return obj;
        }

        public static void BufferClear(byte[] bt) {
            bt.Initialize();
        }

        public static void SendPacket(NetworkStream stream, Packet msg) {
            int length = Serialize(msg).Length;   // 메세지의 총 길이를 얻는다.

            byte[] sendBuffer = new byte[length];               // 버퍼를 동적 할당하고,
            Serialize(msg).CopyTo(sendBuffer, 0); // 메세지를 직렬화하여 버퍼에 넣는다.

            // 우선 보낼 메세지의 총 길이를 먼저 보낸다.
            byte[] bufferSize = BitConverter.GetBytes(sendBuffer.Length);
            stream.Write(bufferSize, 0, bufferSize.Length);
            stream.Flush();

            // 메세지를 보낸다.
            stream.Write(sendBuffer, 0, sendBuffer.Length);
            stream.Flush();

            // 버퍼 초기화
            sendBuffer.Initialize();
        }

        public static Packet ReceivePacket(NetworkStream stream, byte[] buffer) {
            // 메세지를 읽어온다.
            byte[] bufferLength = new byte[sizeof(int)];
            int nRead = stream.Read(bufferLength, 0, bufferLength.Length);
            if (nRead == 0) { //유효하지 않은 메세지
                return null;
            }

            // 메세지의 길이를 읽어온다.
            int length = BitConverter.ToInt32(bufferLength, 0);
            buffer = new byte[length];
            stream.Read(buffer, 0, buffer.Length);

            // 메세지를 읽어온다.
            Packet packet = (Packet)Deserialize(buffer);
            return packet;
        }
    }

    [Serializable]
    public class MSGText : Packet {
        public string message = null;

        public MSGText() : base(PacketType.NONE) {
            message = null;
        }

        public MSGText(string _message) : base(PacketType.MESSAGE) {
            message = _message;
        }

        public MSGText(PacketType _type, string _message) : base(_type) {
            message = _message;
        }
    }    
}