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
using Net.Ypresto.Androidtranscoder.Format;
using Net.Ypresto.Androidtranscoder;
using Java.Lang;

namespace Xamarin.MP4Transcoder
{
    /// <summary>
    /// 
    /// </summary>
    public class Transcoder
    {
        private IMediaFormatStrategy strategy;

        private Transcoder(IMediaFormatStrategy strategy)
        {
            this.strategy = strategy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitrate"></param>
        /// <param name="audioBitrate"></param>
        /// <param name="audioChannels"></param>
        /// <returns></returns>
        public static Transcoder For720pFormat(int bitrate, int audioBitrate, int audioChannels)
        {
            return new MP4Transcoder.Transcoder(MediaFormatStrategyPresets.CreateAndroid720pStrategy(bitrate,audioBitrate,audioChannels));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitrate"></param>
        /// <returns></returns>
        public static Transcoder For720pFormat(int bitrate)
        {
            return new MP4Transcoder.Transcoder(MediaFormatStrategyPresets.CreateAndroid720pStrategy(bitrate));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Transcoder For720pFormat()
        {
            return new MP4Transcoder.Transcoder(MediaFormatStrategyPresets.CreateAndroid720pStrategy());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Transcoder For960x540Format()
        {
            return new MP4Transcoder.Transcoder(MediaFormatStrategyPresets.CreateExportPreset960x540Strategy());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public static Transcoder For(IMediaFormatStrategy strategy) {
            return new MP4Transcoder.Transcoder(strategy);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="presets"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task ConvertAsync(Java.IO.File inputFile, Java.IO.File outputFile, Action<double> progress = null) {
            try
            {
                TaskCompletionSource<object> taskSource = new TaskCompletionSource<object>();

                MediaTranscoder.Instance.TranscodeVideo(inputFile.AbsolutePath, outputFile.AbsolutePath, strategy, new TranscoderListener
                {
                    TranscodeProgress = progress,
                    TranscodeFailed = e =>
                    {
                        taskSource.TrySetException(e);
                    },
                    TranscodeCanceled = () =>
                    {
                        taskSource.TrySetCanceled();
                    },
                    TranscodeCompleted = () =>
                    {
                        taskSource.SetResult(null);
                    }
                });
                await taskSource.Task;
            }
            catch (Java.Lang.Exception ex) {
                if (ex.Message == "MediaFormatStrategy returned pass-through for both video and audio. No transcoding is necessary.") {
                    await Task.Run(()=> {
                        if (System.IO.File.Exists(outputFile.AbsolutePath)) {
                            System.IO.File.Delete(outputFile.AbsolutePath);
                        }
                        progress(0.3);
                        System.IO.File.Copy(inputFile.AbsolutePath, outputFile.AbsolutePath);
                        progress(1);
                    });
                }
            }
        }

    }


    /// <summary>
    /// 
    /// </summary>
    public class TranscoderListener : Java.Lang.Object, MediaTranscoder.IListener
    {
        /// <summary>
        /// 
        /// </summary>
        public Action TranscodeCanceled { get; set; }
        void MediaTranscoder.IListener.OnTranscodeCanceled()
        {
            TranscodeCanceled?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        public Action TranscodeCompleted { get; set; }
        void MediaTranscoder.IListener.OnTranscodeCompleted()
        {
            TranscodeCompleted?.Invoke();
        }


        /// <summary>
        /// 
        /// </summary>
        public Action<Java.Lang.Exception> TranscodeFailed { get; set; }
        void MediaTranscoder.IListener.OnTranscodeFailed(Java.Lang.Exception p0)
        {
            TranscodeFailed?.Invoke(p0);
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<double> TranscodeProgress { get; set; }
        void MediaTranscoder.IListener.OnTranscodeProgress(double p0)
        {
            TranscodeProgress?.Invoke(p0);
        }
    }
}