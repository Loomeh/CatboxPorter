using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace CatboxPorter
{
    public class Uploader
    {
        private readonly HttpClient _http;
        private readonly string? _userHash;
        private static readonly Uri ApiUri = new("https://catbox.moe/user/api.php");

        public Uploader(HttpClient http, string? userHash = null)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _userHash = userHash;

            // Prefer HTTP/2 to avoid chunked transfer on the request body.
            _http.DefaultRequestHeaders.ExpectContinue = false;
            _http.DefaultRequestVersion = HttpVersion.Version20;
            _http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

            // Some endpoints/CDNs reject requests without a UA.
            if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CatboxPorter", "1.0"));
            }
        }

        public async Task<string> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string? contentType = null,
            IProgress<UploadProgressStream.Progress>? progress = null,
            CancellationToken ct = default
        )
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("fileupload", Encoding.UTF8), "reqtype");
            if (!string.IsNullOrWhiteSpace(_userHash))
                content.Add(new StringContent(_userHash, Encoding.UTF8), "userhash");

            long? total = fileStream.CanSeek ? fileStream.Length - fileStream.Position : null;

            using var ups = new UploadProgressStream(fileStream, progress, leaveInnerOpen: true);
            var fileContent = new StreamContent(ups);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");
            if (total.HasValue) fileContent.Headers.ContentLength = total.Value;

            content.Add(fileContent, "fileToUpload", string.IsNullOrWhiteSpace(fileName) ? "upload.bin" : fileName);

            // Let HttpClient negotiate (HTTP/2 preferred).
            using var req = new HttpRequestMessage(HttpMethod.Post, ApiUri)
            {
                Content = content
            };

            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var body = (await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false)).Trim();
            EnsureCatboxSuccess(body);
            return body;
        }

        private static void EnsureCatboxSuccess(string responseText)
        {
            if (responseText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                responseText.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            throw new HttpRequestException($"Catbox error: {responseText}");
        }
    }
}