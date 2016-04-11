# xamarin-android-ffmpeg
Xamarin Android FFMpeg binding

Usage

        public class VideoConverter 
        {

            public VideoConverter()
            {

            }

            public File convertFile(
                File inputFile, 
                Action<string> logger = null, 
                Action<int,int> onProgress = null)
            {
                File ouputFile = new File(inputFile.CanonicalPath + ".mpg");

                ouputFile.DeleteOnExit();

                List<string> cmd = new List<string>();
                cmd.Add("-y");
                cmd.Add("-i");
                cmd.Add(inputFile.CanonicalPath);

                MediaMetadataRetriever m = new MediaMetadataRetriever();
                m.SetDataSource(inputFile.CanonicalPath);

                string rotate = m.ExtractMetadata(Android.Media.MetadataKey.VideoRotation);

                int r = 0;

                if (!string.IsNullOrWhiteSpace(rotate)) {
                    r = int.Parse(rotate);
                }

                cmd.Add("-b:v");
                cmd.Add("1M");

                cmd.Add("-b:a");
                cmd.Add("128k");


                switch (r)
                {
                    case 270:
                        cmd.Add("-vf scale=-1:480,transpose=cclock");
                        break;
                    case 180:
                        cmd.Add("-vf scale=-1:480,transpose=cclock,transpose=cclock");
                        break;
                    case 90:
                        cmd.Add("-vf scale=480:-1,transpose=clock");
                        break;
                    case 0:
                        cmd.Add("-vf scale=-1:480");
                        break;
                    default:

                        break;
                }

                cmd.Add("-f");
                cmd.Add("mpeg");

                cmd.Add(ouputFile.CanonicalPath);

                string cmdParams = string.Join(" ", cmd);

                int total = 0;
                int current = 0;

                Com.Github.Hiteshsondhi88.Libffmpeg.FFmpeg
                        .GetInstance(AuditionApplication.Current)
                        .ExecuteAndWait(cmdParams,
                            new VideoConverterListener {
                                OnFailure = (f) => {
                                    logger?.Invoke(f);
                                },
                                OnProgress = (s) => {
                                    logger?.Invoke(s);
                                    int n = Extract(s, "Duration:", ",");
                                    if (n != -1) {
                                        total = n;
                                    }
                                    n = Extract(s, "time=", " bitrate=");
                                    if (n != -1) {
                                        current = n;
                                        onProgress?.Invoke(current, total);
                                    }
                                }
                            
                            }
                );

                return ouputFile;
            }

            int Extract(String text, String start, String end)
            {
                int i = text.IndexOf(start);
                if (i != -1)
                {
                    text = text.Substring(i + start.Length);
                    i = text.IndexOf(end);
                    if (i != -1)
                    {
                        text = text.Substring(0, i);
                        return parseTime(text);
                    }
                }
                return -1;
            }

            public static int parseTime(String time)
            {
                time = time.Trim();
                String[] tokens = time.Split(':');
                int hours = int.Parse(tokens[0]);
                int minutes = int.Parse(tokens[1]);
                float seconds = float.Parse(tokens[2]);
                int s = (int)seconds * 100;
                return hours * 360000 + minutes * 60100 + s;
            }

        }

        public class VideoConverterListener :
            Java.Lang.Object,
            Com.Github.Hiteshsondhi88.Libffmpeg.IFFmpegExecuteResponseHandler
        {
            public Action<string> OnFailure { get; set; }

            void IFFmpegExecuteResponseHandler.OnFailure(string p0)
            {
                OnFailure?.Invoke(p0);
            }

            public Action OnFinish { get; set; }

            void IResponseHandler.OnFinish()
            {
                OnFinish?.Invoke();
            }

            public Action<string> OnProgress { get; set; }
            void IFFmpegExecuteResponseHandler.OnProgress(string p0)
            {
                OnProgress?.Invoke(p0);
            }

            public Action OnStart { get; set; }

            void IResponseHandler.OnStart()
            {
                OnStart?.Invoke();
            }

            public Action<string> OnSuccess { get; set; }

            void IFFmpegExecuteResponseHandler.OnSuccess(string p0)
            {
                OnSuccess?.Invoke(p0);
            }
        }
