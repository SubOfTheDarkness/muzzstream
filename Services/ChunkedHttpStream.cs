using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace muzzstream.Services;

public class ChunkedHttpStream : Stream {
    private readonly HttpClient _client;
    private readonly string _url;
    private readonly long _totalLength;
    
    private long _currentPosition;
    private byte[]? _currentChunk;
    private int _chunkPosition;

    public ChunkedHttpStream(HttpClient client, string url, long totalLength, byte[] firstChunk) {
        _client = client;
        _url = url;
        _totalLength = totalLength;
        _currentChunk = firstChunk;
        _chunkPosition = 0;
        _currentPosition = 0;
    }

    // SharpAudio вызывает исключительно этот метод во внутреннем цикле
    public override int Read(byte[] buffer, int offset, int count) {
        if (_currentPosition >= _totalLength) return 0;

        // Если текущий фиксированный чанк закончился, запрашиваем следующий СИНХРОННО
        if (_currentChunk == null || _chunkPosition >= _currentChunk.Length) {
            if (!FetchNextChunkSync()) {
                return 0; // Конец файла или ошибка сети
            }
        }

        if (_currentChunk == null) return 0;

        int bytesAvailable = _currentChunk.Length - _chunkPosition;
        int bytesToCopy = Math.Min(bytesAvailable, count);

        Buffer.BlockCopy(_currentChunk, _chunkPosition, buffer, offset, bytesToCopy);
        
        _chunkPosition += bytesToCopy;
        _currentPosition += bytesToCopy;

        return bytesToCopy;
    }

    private bool FetchNextChunkSync() {
        try {
            System.Diagnostics.Debug.WriteLine($"[NET_STREAM] Скачивание следующего чанка с позиции: {_currentPosition} байт...");
            
            var request = new HttpRequestMessage(HttpMethod.Get, _url);
            request.Headers.Range = new RangeHeaderValue(_currentPosition, null);

            // Используем СИНХРОННЫЙ Send вместо SendAsync, чтобы избежать взаимной блокировки (Deadlock)
            using var response = _client.Send(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) {
                System.Diagnostics.Debug.WriteLine($"[NET_ERROR] Сервер вернул ошибку: {response.StatusCode}");
                return false;
            }

            // Читаем контент ответа синхронно в массив байт
            using var stream = response.Content.ReadAsStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            
            _currentChunk = ms.ToArray();
            _chunkPosition = 0;

            System.Diagnostics.Debug.WriteLine($"[NET_STREAM] Успешно получен следующий чанк размером: {_currentChunk.Length} байт.");
            return _currentChunk.Length > 0;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"[NET_CRITICAL] Ошибка при докачке чанка: {ex.Message}");
            return false;
        }
    }

    // Асинхронный метод просто перенаправляем в синхронный, так как в плеере он не используется
    public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken = default) {
        return System.Threading.Tasks.Task.FromResult(Read(buffer, offset, count));
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _totalLength;
    
    public override long Position {
        get => _currentPosition;
        set => throw new NotSupportedException();
    }

    public override void Flush() { }
    public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.CompletedTask;
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => throw new NotSupportedException();
}
