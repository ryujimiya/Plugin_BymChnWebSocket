//プラグインのファイル名は、「Plugin_*.dll」という形式にして下さい。
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using FNF.Utility;
using FNF.Controls;
using FNF.XmlSerializerSetting;
using FNF.BouyomiChanApp;

namespace Plugin_BymChnWebSocket {
    public class Plugin_BymChnWebSocket : IPlugin
    {
        /// <summary>
        /// 名前(IPlugin必須)
        /// </summary>
        string IPlugin.Name => "棒読みちゃんWebSocket";

        /// <summary>
        /// バージョン(IPlugin必須)
        /// </summary>
        string IPlugin.Version => "1.0.0.0";

        /// <summary>
        /// キャプション(IPlugin必須)
        /// </summary>
        string IPlugin.Caption => "棒読みちゃんWebSocket";

        /// <summary>
        /// 設定フォームデータ(IPlugin必須)
        /// </summary>
        ISettingFormData IPlugin.SettingFormData => null;

        /// <summary>
        /// WebSocketサーバー
        /// </summary>
        WebSocketServer server;

        /// <summary>
        /// プラグインが開始された(IPlugin必須)
        /// </summary>
        void IPlugin.Begin()
        {
            StartServer();
        }

        /// <summary>
        /// プラグインが終了された(IPlugin必須)
        /// </summary>
        void IPlugin.End()
        {
            StopServer();
        }


        /// <summary>
        /// WebSocketサーバーを開始する
        /// </summary>
        public void StartServer()
        {
            server = new WebSocketServer();
            server.Port = 50002;
            server.Path = "/ws/";
            server.OnConnected += Server_OnConnected;
            server.OnDisconnected += Server_OnDisconnected;
            server.OnDataReceived += Server_OnDataReceived;
            server.Start();
        }

        /// <summary>
        /// WebSocketサーバーを終了する
        /// </summary>
        public void StopServer()
        {
            server.Stop();
        }

        /// <summary>
        /// WebSocketが接続された
        /// </summary>
        /// <param name="client"></param>
        private void Server_OnConnected(System.Net.Sockets.TcpClient client)
        {
            System.Diagnostics.Debug.WriteLine("Server_OnConnected");
        }

        /// <summary>
        /// WebSocketが切断された
        /// </summary>
        /// <param name="client"></param>
        private void Server_OnDisconnected(System.Net.Sockets.TcpClient client)
        {
            System.Diagnostics.Debug.WriteLine("Server_OnDisconnected");
        }

        /// <summary>
        /// データを受信した
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        private void Server_OnDataReceived(System.Net.Sockets.TcpClient client, byte[] data)
        {
            System.Diagnostics.Debug.WriteLine("Server_OnDataReceived {data.Length:{0}", data.Length.ToString());

            short command; //[0-1]  (16Bit) コマンド          （ 0:メッセージ読み上げ）
            short speed; //[2-3]  (16Bit) 速度              （-1:棒読みちゃん画面上の設定）
            short tone; //[4-5]  (16Bit) 音程              （-1:棒読みちゃん画面上の設定）
            short volume; //[6-7]  (16Bit) 音量              （-1:棒読みちゃん画面上の設定）
            short voice; //[8-9]  (16Bit) 声質              （ 0:棒読みちゃん画面上の設定、1:女性1、2:女性2、3:男性1、4:男性2、5:中性、6:ロボット、7:機械1、8:機械2、10001～:SAPI5）
            byte code; //[10]   ( 8Bit) 文字列の文字コード（ 0:UTF-8, 1:Unicode, 2:Shift-JIS）
            long len; //[11-14](32Bit) 文字列の長さ
            byte[] buf; // 文字列

            int pos = 0;
            command = (short)(data[pos++] + (data[pos++] << 8));
            speed = (short)(data[pos++] + (data[pos++] << 8));
            tone = (short)(data[pos++] + (data[pos++] << 8));
            volume = (short)(data[pos++] + (data[pos++] << 8));
            voice = (short)(data[pos++] + (data[pos++] << 8));
            code = data[pos++];
            len = data[pos++] + (data[pos++] << 8) +
                      (data[pos++] << 16) + (data[pos++] << 24);
            buf = new byte[len];
            for (int i = 0; i < len; i++)
            {
                buf[i] = data[pos++];
            }
            var text = Encoding.UTF8.GetString(buf);

            System.Diagnostics.Debug.WriteLine("command = {0}", command.ToString());
            System.Diagnostics.Debug.WriteLine("speed = {0}", speed.ToString());
            System.Diagnostics.Debug.WriteLine("tone = {0}", tone.ToString());
            System.Diagnostics.Debug.WriteLine("volume = {0}", volume.ToString());
            System.Diagnostics.Debug.WriteLine("voice = {0}", voice.ToString());
            System.Diagnostics.Debug.WriteLine("code = {0}", code.ToString());
            System.Diagnostics.Debug.WriteLine("len = {0}", len.ToString());
            System.Diagnostics.Debug.WriteLine("text = " + text);

            Pub.AddTalkTask(text, speed, tone, volume, (VoiceType)voice);

            client.Close();
        }
    }
}
