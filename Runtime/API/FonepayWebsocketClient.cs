using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Darkmatter.Fonepay
{
    internal sealed class FonepayWebsocketClient : IDisposable
    {
        internal event Action<bool> OnQrVerified;
        internal event Action<WebsocketMessage<QRPaymentStatus>> OnPaymentReceived;
        internal event Action<string> OnRawMessage;
        internal event Action<Exception> OnClosed;

        private ClientWebSocket _client;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private bool _disposed;

        internal async Task ConnectAsync(string url, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_client != null)
                throw new InvalidOperationException("WebSocket is already connected or connecting.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var client = new ClientWebSocket();

            try
            {
                await client.ConnectAsync(new Uri(url), _cts.Token);

                _client = client;
                _receiveTask = ReceiveLoop(client, _cts.Token);
            }
            catch
            {
                client.Dispose();
                _cts.Dispose();
                _cts = null;
                throw;
            }
        }

        private async Task ReceiveLoop(
            ClientWebSocket client,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            Exception error = null;

            try
            {
                while (
                    client.State == WebSocketState.Open &&
                    !cancellationToken.IsCancellationRequested)
                {
                    string message = await ReceiveFullMessageAsync(client, buffer, cancellationToken);

                    if (message == null)
                        break;

                    HandleMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during disconnect.
            }
            catch (WebSocketException ex)
            {
                error = ex;
            }
            catch (ObjectDisposedException)
            {
                // Socket disposed during shutdown.
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                OnClosed?.Invoke(error);
            }
        }

        private void HandleMessage(string message)
        {
            OnRawMessage?.Invoke(message);

            var envelope = JsonUtility.FromJson<WebsocketMessage<QRPaymentStatus>>(message);
            if (string.IsNullOrEmpty(envelope.transactionStatus))
                return;

            if (envelope.transactionStatus.Contains("qrVerified"))
            {
                var v = JsonUtility.FromJson<QRVerificationStatus>(envelope.transactionStatus);
                OnQrVerified?.Invoke(v.qrVerified);
            }
            else if (envelope.transactionStatus.Contains("paymentSuccess"))
            {
                OnPaymentReceived?.Invoke(envelope);
            }
        }

        private static async Task<string> ReceiveFullMessageAsync(
            ClientWebSocket client,
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();

            while (true)
            {
                WebSocketReceiveResult result = await client.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (client.State == WebSocketState.CloseReceived)
                    {
                        await client.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        );
                    }

                    return null;
                }

                ms.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                    break;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        internal async Task DisconnectAsync()
        {
            if (_client == null)
                return;

            ClientWebSocket client = _client;
            _client = null;

            try
            {
                _cts?.Cancel();

                if (client.State == WebSocketState.Open ||
                    client.State == WebSocketState.CloseReceived)
                {
                    await client.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client closing",
                        CancellationToken.None
                    );
                }
            }
            catch
            {
                client.Abort();
            }
            finally
            {
                client.Dispose();

                _cts?.Dispose();
                _cts = null;
                _receiveTask = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _cts?.Cancel();
                _client?.Abort();
            }
            catch
            {
                // Ignore cleanup errors.
            }
            finally
            {
                _client?.Dispose();
                _client = null;

                _cts?.Dispose();
                _cts = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FonepayWebsocketClient));
        }
    }
}