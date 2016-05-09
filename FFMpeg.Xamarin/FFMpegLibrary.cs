using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;

namespace FFMpeg.Xamarin
{
    public class FFMpegLibrary
    {

        public static FFMpegLibrary Instance = new FFMpegLibrary();

        private bool _initialized = false;

        private Java.IO.File ffmpegFile;

        public void Init(Context context)
        {
            if (_initialized)
                return;

            // do all initialization...
            var filesDir = context.FilesDir;

            ffmpegFile = new Java.IO.File(filesDir + "/ffmpeg");

            if (ffmpegFile.Exists())
            {
                if (ffmpegFile.CanExecute())
                    ffmpegFile.SetExecutable(false);
                ffmpegFile.Delete();
                System.Diagnostics.Debug.WriteLine($"ffmpeg file deleted at {ffmpegFile.AbsolutePath}");
            }

            using (var s = context.Assets.Open("ffmpeg", Android.Content.Res.Access.Streaming))
            {
                using (var fout = System.IO.File.OpenWrite(ffmpegFile.AbsolutePath))
                {
                    s.CopyTo(fout);
                }
            }

            System.Diagnostics.Debug.WriteLine($"ffmpeg file copied at {ffmpegFile.AbsolutePath}");

            if (!ffmpegFile.CanExecute())
            {
                ffmpegFile.SetExecutable(true);
                System.Diagnostics.Debug.WriteLine($"ffmpeg file made executable");
            }

            _initialized = true;
        }

        public static async Task<int> Run(Context context, string cmd, Action<string> logger = null) {

            TaskCompletionSource<int> source = new TaskCompletionSource<int>();

            await Task.Run(() => {
                try {
                    int n = _Run(context, cmd, logger);
                    source.SetResult(n);
                } catch (Exception ex) {
                    source.SetException(ex);
                }
            });

            return await source.Task;
        }

        private static int _Run(
            Context context,
            string cmd,
            Action<string> logger = null)
        {

            TaskCompletionSource<int> task = new TaskCompletionSource<int>();

            Instance.Init(context);


            System.Diagnostics.Debug.WriteLine($"ffmpeg initialized");

            //var process = Java.Lang.Runtime.GetRuntime().Exec( Instance.ffmpegFile.CanonicalPath + " " + cmd );

            var startInfo = new System.Diagnostics.ProcessStartInfo(Instance.ffmpegFile.CanonicalPath, cmd);

            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            var process = new System.Diagnostics.Process();

            process.StartInfo = startInfo;


            bool finished = false;

            string error = null;

            process.Start();


            Task.Run(() =>
            {
                using (var reader = process.StandardError)
                {
                    string processOutput = "";
                    do
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                            break;
                        logger?.Invoke(line);
                        processOutput += line;
                    } while (!finished);
                    error = processOutput;
                }
            });

            process.WaitForExit();

            return process.ExitCode;


        }


    }
}