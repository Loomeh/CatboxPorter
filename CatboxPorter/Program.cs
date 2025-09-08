using FFMpegCore;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Globalization;
using Windows.UI.Notifications;

namespace CatboxPorter
{
    public class Program
    {
        // Usage: CatboxPorter.exe "<filePath>" [discord]
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || args.Length > 2)
                {
                    return Fail("Usage: CatboxPorter.exe \"<filePath>\" [discord]");
                }

                string filePath = args[0];
                bool toDiscord = args.Length == 2;

                if (toDiscord && !string.Equals(args[1], "discord", StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("If a second argument is provided, it must be 'discord'.\nUsage: CatboxPorter.exe \"<filePath>\" [discord]");
                }

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return Fail("File not found or path is empty.");
                }

                if (toDiscord)
                {
                    await UploadDiscordAsync(filePath);
                }
                else
                {
                    await UploadNormalAsync(filePath);
                }

                return 0;
            }
            catch (Exception ex)
            {
                ShowErrorToast("Catbox Porter failed to process your file...");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static int Fail(string message)
        {
            ShowErrorToast(message);
            Console.Error.WriteLine(message);
            return 1;
        }

        private static async Task UploadNormalAsync(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var httpClient = new HttpClient();
            var uploader = new Uploader(httpClient);
            using var cts = new CancellationTokenSource();

            // Upload file
            var fileUrl = await UploadWithToastAsync(
                uploader,
                fs,
                Path.GetFileName(filePath),
                tag: "upload",
                group: "catbox",
                toastTitle: "Catbox Porter",
                ct: cts.Token
            );
            SetClipboardTextSta(fileUrl);
        }

        private static async Task UploadDiscordAsync(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var httpClient = new HttpClient();
            var uploader = new Uploader(httpClient);
            using var cts = new CancellationTokenSource();

            string extension = Path.GetExtension(filePath);

            // Check for common video file extensions.
            switch (extension)
            {
                case (".mp4"):
                    break;
                case (".webm"):
                    break;
                case (".mkv"):
                    break;
                case (".avi"):
                    break;
                case (".mov"):
                    break;
                default:
                    ShowErrorToast("Video extension not supported.");
                    Environment.Exit(0);
                    break;
            }

            // Ensure both ffmpeg.exe and ffprobe.exe exist
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = Path.Combine(exePath, "ffmpeg.exe");
            string ffprobePath = Path.Combine(exePath, "ffprobe.exe");
            if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
            {
                ShowErrorToast("FFmpeg/FFprobe cannot be found.");
                Environment.Exit(1);
            }

            GlobalFFOptions.Configure(options => options.BinaryFolder = exePath);

            // Get first frame of video
            var mediaInfo = FFProbe.Analyse(filePath);

            if (mediaInfo.PrimaryVideoStream == null)
            {
                ShowErrorToast("No video stream found in the file.");
                Environment.Exit(1);
            }

            int videoWidth = mediaInfo.PrimaryVideoStream.Width;
            int videoHeight = mediaInfo.PrimaryVideoStream.Height;

            string thumbPath = Path.Combine(
                Path.GetTempPath(),
                $"{Path.GetFileNameWithoutExtension(filePath)}_{Guid.NewGuid():N}.png");

            string thumbnailUrl = "";

            try
            {
                await FFMpeg.SnapshotAsync(
                    filePath,
                    thumbPath,
                    new Size(videoWidth, videoHeight),
                    TimeSpan.FromMilliseconds(0));

                using var thumbFile = File.OpenRead(thumbPath);

                // Upload thumbnail WITHOUT toast/progress
                thumbnailUrl = await uploader.UploadFileAsync(
                    thumbFile,
                    Path.GetFileName(thumbPath),
                    ct: cts.Token
                );
            }
            finally
            {
                try { if (File.Exists(thumbPath)) File.Delete(thumbPath); } catch { /* ignore */ }
            }

            // Upload video (with toast/progress)
            string videoUrl = await UploadWithToastAsync(
                uploader,
                fs,
                Path.GetFileName(filePath),
                tag: "upload-video",
                group: "catbox-discord",
                toastTitle: "Catbox Porter (Discord)",
                ct: cts.Token
            );

            // Generate Discord URL
            string discordUrl = "https://x266.mov/e/" + videoUrl + "?i=" + thumbnailUrl + "&w=" + videoWidth + "&h=" + videoHeight;
            string discordString = $"[{Path.GetFileName(filePath)}]({discordUrl})";
            SetClipboardTextSta(discordString);
        }

        private static async Task<string> UploadWithToastAsync(
            Uploader uploader,
            Stream stream,
            string fileName,
            string tag,
            string group,
            string toastTitle,
            CancellationToken ct)
        {
            int sequenceNumber = 0;

            // Initial toast with bindable progress
            new ToastContentBuilder()
                .AddText(toastTitle)
                .AddText($"Uploading {fileName}")
                .AddVisualChild(new AdaptiveProgressBar
                {
                    Title = "Progress",
                    Value = new BindableProgressBarValue("progressValue"),
                    ValueStringOverride = new BindableString("progressValueString"),
                    Status = new BindableString("progressStatus")
                })
                .Show(toast =>
                {
                    toast.Tag = tag;
                    toast.Group = group;

                    toast.ExpirationTime = DateTime.Now.AddHours(1);

                    toast.Data = new NotificationData(
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "progressValue", "0" },      // 0..1
                            { "progressValueString", "0%" },
                            { "progressStatus", "Starting..." }
                        },
                        (uint)sequenceNumber
                    );
                });

            // Progress callback: update the toast progress bar
            var prog = new Progress<UploadProgressStream.Progress>(p =>
            {
                try
                {
                    var data = new NotificationData();

                    if (p.Percent.HasValue)
                    {
                        double percent = p.Percent.Value; // 0..100
                        double toastValue = Math.Clamp(percent / 100.0, 0.0, 1.0);

                        data.Values["progressValue"] = toastValue.ToString("0.####", CultureInfo.InvariantCulture);
                        data.Values["progressValueString"] = $"{percent:0.#}%";
                        data.Values["progressStatus"] = "Uploading";
                    }
                    else
                    {
                        data.Values["progressStatus"] = p.BytesSent > 0
                            ? $"{p.BytesSent:N0} bytes sent..."
                            : "Uploading...";
                    }

                    data.SequenceNumber = (uint)Interlocked.Increment(ref sequenceNumber);
                    ToastNotificationManagerCompat.CreateToastNotifier().Update(data, tag, group);
                }
                catch
                {
                    // Ignore toast update errors; continue upload
                }
            });

            // Perform the upload
            string url = await uploader.UploadFileAsync(
                stream,
                fileName,
                progress: prog,
                ct: ct
            );

            // Final update: complete the progress bar and set status
            try
            {
                var done = new NotificationData();
                done.Values["progressValue"] = "1";
                done.Values["progressValueString"] = "100%";
                done.Values["progressStatus"] = "Completed";
                done.SequenceNumber = (uint)Interlocked.Increment(ref sequenceNumber);
                ToastNotificationManagerCompat.CreateToastNotifier().Update(done, tag, group);
            }
            catch
            {
                // Ignore toast update errors
            }

            // Show a completion toast
            Thread.Sleep(50);
            ToastNotificationManagerCompat.History.Clear();
            Thread.Sleep(200);

            new ToastContentBuilder()
                .AddText(toastTitle)
                .AddText("Upload complete!")
                .AddText("Link copied to clipboard.")
                .Show();

            Console.WriteLine("Upload complete:");
            Console.WriteLine(url);

            return url;
        }

        private static void ShowErrorToast(string message)
        {
            new ToastContentBuilder()
                .AddText("Catbox Porter Error")
                .AddText(message)
                .Show();
        }

        // Ensures clipboard calls run on an STA thread even after awaits (console apps resume on MTA threads).
        private static void SetClipboardTextSta(string text)
        {
            Exception? error = null;
            var t = new Thread(() =>
            {
                try
                {
                    Clipboard.SetText(text, TextDataFormat.UnicodeText);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            t.Join();
            if (error is not null)
            {
                Console.Error.WriteLine(error);
                ShowErrorToast("Failed to copy to clipboard.");
            }
        }
    }
}