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
            }

            using (var s = context.Assets.Open("ffmpeg", Android.Content.Res.Access.Streaming)) {
                using (var fout = System.IO.File.OpenWrite(ffmpegFile.AbsolutePath)) {
                    s.CopyTo(fout);
                }
            }

            if (!ffmpegFile.CanExecute()) {
                ffmpegFile.SetExecutable(true);
            }
        }

        public static Task Run(
            Context context, 
            string[] cmd, 
            Action<string> logger = null) {

            return Task.Run( async () =>
            {
                Instance.Init(context);

                var process = Java.Lang.Runtime.GetRuntime().Exec(cmd);

                var startTime = DateTime.UtcNow;

                do
                {

                    await Task.Delay(100);

                    var now = DateTime.UtcNow;

                    if ((now - startTime).TotalSeconds > 60) {
                        throw new TimeoutException();
                        startTime = now;
                    }

                    try
                    {
                        process.ExitValue();

                        // seems process is completed...
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
                

            });

        }


    }
}