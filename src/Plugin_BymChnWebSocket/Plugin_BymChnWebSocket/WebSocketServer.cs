using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Plugin_BymChnWebSocket
{
    delegate void Connected(TcpClient client);
    delegate void Disconnected(TcpClient client);
    delegate void DataReceived(TcpClient client, byte[] data);

    class WebSocketServer
    {
        /// <summary>
        /// ポート
        /// </summary>
        public int Port { get; set; }

        public string Path { get; set; }

        /// <summary>
        /// イベントハンドラ
        /// </summary>
        public event Connected OnConnected;
        public event Disconnected OnDisconnected;
        public event DataReceived OnDataReceived;

        private Thread listenerThread;
        TcpListener listener;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public WebSocketServer()
        {
        }

        /// <summary>
        /// 開始する
        /// </summary>
        public void Start()
        {
            listenerThread = new Thread(listenerThreadProc);
            listenerThread.Start();
        }

        /// <summary>
        /// 終了する
        /// </summary>
        public void Stop()
        {
            listener.Stop();
            listenerThread.Abort();
        }

        /// <summary>
        /// リッスンスレッド
        /// </summary>
        private void listenerThreadProc()
        {
            IPAddress ipAddress = IPAddress.Parse("::1");
            var clientThreadList = new List<Thread>();

            // リッスン開始
            listener = new TcpListener(ipAddress, Port);
            listener.Start(100);

            // メインループ
            while (true)
            {
                try
                {
                    // 接続要求を処理する
                    var client = listener.AcceptTcpClient();
                    System.Diagnostics.Debug.WriteLine("AcceptTcpClient : {0}", client.Client.RemoteEndPoint.ToString());
                    var clientThread = new Thread(threadClient);
                    clientThread.Start(client);
                    clientThreadList.Add(clientThread);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine("listenerThreadProc exception:" + exception.ToString());
                    break;
                }
            }

            // 後片付け
            foreach (var clientThread in clientThreadList)
            {
                if (clientThread.IsAlive)
                {
                    clientThread.Abort();
                }
            }
            listener.Stop();
        }

        /// <summary>
        /// クライアントスレッド
        /// </summary>
        /// <param name="arg"></param>
        private void threadClient(object arg)
        {
            TcpClient client = arg as TcpClient;

            bool ret;
            ret = handleHandshake(client);
            if  (ret)
            {
                OnConnected(client);
                while (ret && client != null && client.Client != null)
                {
                    try
                    {
                        ret = handleRecvData(client);
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine("threadClient exception=" + exception.ToString());
                        break;
                    }
                }
                OnDisconnected(client);
            }
            client.Close();
        }

        /// <summary>
        /// WebSocketのハンドシェイク
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private bool handleHandshake(TcpClient client)
        {
            const string RequestPattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" +
                                   @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+" +
                                   @"\r\n" +
                                   @"(?<body>.+)?";
            bool ret = false;
            if (client == null || client.Client == null)
            {
                return ret;
            }
            // 受信処理
            int bufSize = client.Client.ReceiveBufferSize;
            byte[] buf = new byte[bufSize];
            client.Client.Receive(buf);

            String handshake = Encoding.UTF8.GetString(buf, 0, bufSize);
            System.Diagnostics.Debug.WriteLine("handshake=");
            System.Diagnostics.Debug.WriteLine(handshake);
            var regex = new Regex(RequestPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var match = regex.Match(handshake);
            var method = match.Groups["method"].Value;
            var path = match.Groups["path"].Value;
            var fields = match.Groups["field_name"].Captures;
            var values = match.Groups["field_value"].Captures;
            var headers = new Dictionary<string, string>();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i].Value;
                var value = values[i].Value;
                headers[field] = value;
            }
            if (method != "GET")
            {
                return ret;
            }
            if (path != Path)
            {
                return ret;
            }
            if (headers["Upgrade"] != "websocket")
            {
                return ret;
            }
            if (headers["Connection"] != "Upgrade")
            {
                return ret;
            }
            if (headers["Sec-WebSocket-Version"] != "13")
            {
                return ret;
            }
            var secWebsocketKey = headers["Sec-WebSocket-Key"];
            var originalText = secWebsocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] b = Encoding.UTF8.GetBytes(originalText);

            var sha1 = new SHA1CryptoServiceProvider();
            var hash = sha1.ComputeHash(b);
            string secWebSocketAccept = Convert.ToBase64String(hash);

            string response =
                "HTTP/1.1 101 OK\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + secWebSocketAccept + "\r\n" +
                "\r\n";
            System.Diagnostics.Debug.WriteLine("response=");
            System.Diagnostics.Debug.WriteLine(response);
            byte[] bytes = Encoding.UTF8.GetBytes(response);
            client.Client.Send(bytes);

            ret = true;
            return ret;
        }

        /// <summary>
        /// データの受信
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private bool handleRecvData(TcpClient client)
        {
            bool ret = false;
            if (client == null || client.Client == null)
            {
                return ret;
            }
            // 受信処理
            int bufSize = client.Client.ReceiveBufferSize;
            byte[] buf = new byte[bufSize];
            client.Client.Receive(buf);

            Int64 payloadLen = buf[1] & 0x7F;
            int offset = 2;
            if (payloadLen == 126)
            {
                // ペイロード長拡張16ビット
                payloadLen = (buf[2] << 8) + buf[3];
                offset += 2;
            }
            else if (payloadLen == 127)
            {
                // ペイロード長拡張64ビット
                payloadLen = (buf[2] << 56) + (buf[3] << 48) +
                    (buf[4] << 40) + (buf[5] << 32) +
                    (buf[6] << 24) + (buf[7] << 16) +
                    (buf[8] << 8) + buf[9];
                offset += 8;
            }
            else
            {
                // 拡張無し
            }
            bool mask = ((buf[1] & 0x80) == 0x80);
            if (mask)
            {
                var maskingKey = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    maskingKey[i] = buf[offset + i];
                }
                offset += 4;

                // unmask
                for (int i = 0; i < payloadLen; i++)
                {
                    buf[offset + i] ^= maskingKey[i % 4];
                }
            }

            System.Diagnostics.Debug.WriteLine("payloadLen={0}", payloadLen.ToString());

            byte[] data = new byte[payloadLen];
            int pos = offset;
            for (int i = 0; i < payloadLen; i++)
            {
                data[i] = buf[pos++];
            }
            OnDataReceived(client, data);

            ret = true;
            return ret;
        }
    }
}
