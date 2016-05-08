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

        public void Init(Context context) {
            if (_initialized)
                return;

            // do all initialization...
            var filesDir = context.FilesDir;

            ffmpegFile = new Java.IO.File(filesDir + "/ffmpeg");

            if (ffmpegFile.Exists()) {
                if(ffmpegFile.CanExecute())
                    ffmpegFile.SetExecutable(false);
                ffmpegFile.Delete();
                System.Diagnostics.Debug.WriteLine($"ffmpeg file deleted at {ffmpegFile.AbsolutePath}");
            }

            using (var s = context.Assets.Open("ffmpeg", Android.Content.Res.Access.Streaming)) {
                using (var fout = System.IO.File.OpenWrite(ffmpegFile.AbsolutePath)) {
                    s.CopyTo(fout);
                }
            }

            System.Diagnostics.Debug.WriteLine($"ffmpeg file copied at {ffmpegFile.AbsolutePath}");

            if (!ffmpegFile.CanExecute()) {
                ffmpegFile.SetExecutable(true);
                System.Diagnostics.Debug.WriteLine($"ffmpeg file made executable");
            }

            _initialized = true;
        }

        public static Task Run(
            Context context, 
            string cmd, 
            Action<string> logger = null) {

            TaskCompletionSource<int> task = new TaskCompletionSource<int>();

            Task.Run( async () =>
            {
                Instance.Init(context);

                bool success = false;


                System.Diagnostics.Debug.WriteLine($"ffmpeg initialized");

                var process = Java.Lang.Runtime.GetRuntime().Exec( Instance.ffmpegFile.CanonicalPath + " " + cmd );

                System.Diagnostics.Debug.WriteLine($"ffmpeg started");

                var startTime = DateTime.UtcNow;

                do
                {

                    await Task.Delay(100);

                    var now = DateTime.UtcNow;

                    if ((now - startTime).TotalSeconds > 60) {
                        throw new TimeoutException();
                    }

                    startTime = now;


                    try
                    {
                        success = process.ExitValue() == 0;

                        // seems process is completed...
                        System.Diagnostics.Debug.WriteLine($"ffmpeg finished");
                        break;
                    }
                    catch (Java.Lang.IllegalThreadStateException e)
                    {
                        // do nothing...
                    }

                    using (var br = new Java.IO.BufferedReader(new Java.IO.InputStreamReader(process.ErrorStream)))
                    {
                        string line = null;
                        while ((line = br.ReadLine()) != null) {
                            System.Diagnostics.Debug.WriteLine(line);
                            logger?.Invoke(line);
                        }
                    }

                } while (true);

                using (var br = new Java.IO.BufferedReader(
                    new Java.IO.InputStreamReader(
                        success ? process.InputStream : process.ErrorStream)))
                {
                    string line = null;
                    while ((line = br.ReadLine()) != null)
                    {
                        System.Diagnostics.Debug.WriteLine(line);
                        logger?.Invoke(line);
                    }
                }


                task.SetResult(0);
            });

            return task.Task;

        }


    }
}