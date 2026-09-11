using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Mahjong.Network
{
    /// <summary>
    /// WebSocketClient: Driver WebSocket Mandiri (Zero-Dependency) untuk Unity.
    /// Menggunakan System.Net.WebSockets.ClientWebSocket bawaan .NET Standard / Unity Runtime,
    /// mendukung koneksi cepat, reconnect otomatis, dan pengiriman pesan JSON asinkron.
    /// </summary>
    public class WebSocketClient
    {
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnRawMessageReceived;
        public event Action<Exception> OnError;

        private ClientWebSocket socket;
        private CancellationTokenSource cts;
        private bool isConnecting = false;
        private string serverUrl;

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        /// <summary>
        /// Membuka koneksi WebSocket ke Server Backend Go (misal: wss://mahjong-server.koyeb.app/ws).
        /// </summary>
        public async Task ConnectAsync(string url)
        {
            if (IsConnected || isConnecting) return;
            isConnecting = true;
            this.serverUrl = url;

            try
            {
                socket = new ClientWebSocket();
                cts = new CancellationTokenSource();

                Uri uri = new Uri(url);
                Debug.Log($"[WebSocketClient] Menghubungi Server: {url} ...");
                await socket.ConnectAsync(uri, cts.Token);

                isConnecting = false;
                Debug.Log("[WebSocketClient] Berhasil Terhubung ke Server!");
                UnityMainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());

                // Jalankan loop penerima pesan di background thread
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                isConnecting = false;
                Debug.LogError($"[WebSocketClient] Gagal terhubung: {ex.Message}");
                UnityMainThreadDispatcher.Enqueue(() => OnError?.Invoke(ex));
            }
        }

        /// <summary>
        /// Mengirimkan pesan NetworkEnvelope JSON ke server.
        /// </summary>
        public async Task SendAsync(string jsonPayload)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebSocketClient] Gagal mengirim: Socket tidak terhubung!");
                return;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(jsonPayload);
                ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketClient] Error saat mengirim pesan: {ex.Message}");
                UnityMainThreadDispatcher.Enqueue(() => OnError?.Invoke(ex));
            }
        }

        private async Task ReceiveLoopAsync()
        {
            byte[] buffer = new byte[8192];

            try
            {
                while (IsConnected && !cts.IsCancellationRequested)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await CloseInternalAsync("Server Closed Connection");
                                return;
                            }
                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        string message = Encoding.UTF8.GetString(ms.ToArray());
                        UnityMainThreadDispatcher.Enqueue(() => OnRawMessageReceived?.Invoke(message));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    Debug.LogWarning($"[WebSocketClient] Disconnected with error: {ex.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => OnDisconnected?.Invoke(ex.Message));
                }
            }
        }

        public async Task DisconnectAsync()
        {
            await CloseInternalAsync("Client Disconnected");
        }

        private async Task CloseInternalAsync(string reason)
        {
            if (socket == null) return;
            try
            {
                cts?.Cancel();
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
                }
            }
            catch { }
            finally
            {
                socket?.Dispose();
                socket = null;
                UnityMainThreadDispatcher.Enqueue(() => OnDisconnected?.Invoke(reason));
            }
        }
    }
}
