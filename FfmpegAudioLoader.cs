using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

public unsafe class FfmpegAudioLoader
{
    public void ConfigureFfmpegRootPath()
    {
        var projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..");
        ffmpeg.RootPath = Path.GetFullPath($"{projectRoot}/FfmpegLibraries/Windows");
    }

    public AudioData Load(string filePath)
    {
        AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();
        if (ffmpeg.avformat_open_input(&pFormatContext, filePath, null, null) != 0)
        {
            throw new Exception("Could not open file.");
        }

        if (ffmpeg.avformat_find_stream_info(pFormatContext, null) < 0)
        {
            ffmpeg.avformat_close_input(&pFormatContext);
            throw new Exception("Could not find stream info.");
        }

        AVStream* pStream = null;
        for (int i = 0; i < pFormatContext->nb_streams; i++)
        {
            if (pFormatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                pStream = pFormatContext->streams[i];
                break;
            }
        }

        if (pStream == null)
        {
            ffmpeg.avformat_close_input(&pFormatContext);
            throw new Exception("Could not find audio stream.");
        }

        AVCodec* pCodec = ffmpeg.avcodec_find_decoder(pStream->codecpar->codec_id);
        if (pCodec == null)
        {
            ffmpeg.avformat_close_input(&pFormatContext);
            throw new Exception("Could not find codec.");
        }

        AVCodecContext* pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
        if (ffmpeg.avcodec_parameters_to_context(pCodecContext, pStream->codecpar) < 0)
        {
            ffmpeg.avcodec_free_context(&pCodecContext);
            ffmpeg.avformat_close_input(&pFormatContext);
            throw new Exception("Could not copy codec parameters to context.");
        }

        if (ffmpeg.avcodec_open2(pCodecContext, pCodec, null) < 0)
        {
            ffmpeg.avcodec_free_context(&pCodecContext);
            ffmpeg.avformat_close_input(&pFormatContext);
            throw new Exception("Could not open codec.");
        }

        AVPacket* pPacket = ffmpeg.av_packet_alloc();
        AVFrame* pFrame = ffmpeg.av_frame_alloc();

        // Target format: Float, Stereo, 44100Hz
        AVSampleFormat targetFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
        int targetChannels = 2;
        int targetSampleRate = 44100;
        AVChannelLayout targetLayout;
        ffmpeg.av_channel_layout_default(&targetLayout, targetChannels);

        SwrContext* pSwrContext = null;
        ffmpeg.swr_alloc_set_opts2(&pSwrContext, &targetLayout, targetFormat, targetSampleRate,
            &pCodecContext->ch_layout, pCodecContext->sample_fmt, pCodecContext->sample_rate, 0, null);
        ffmpeg.swr_init(pSwrContext);

        List<float> allSamples = new List<float>();

        while (ffmpeg.av_read_frame(pFormatContext, pPacket) >= 0)
        {
            if (pPacket->stream_index == pStream->index)
            {
                if (ffmpeg.avcodec_send_packet(pCodecContext, pPacket) >= 0)
                {
                    while (ffmpeg.avcodec_receive_frame(pCodecContext, pFrame) >= 0)
                    {
                        int maxTargetNbSamples = (int)ffmpeg.av_rescale_rnd(
                            ffmpeg.swr_get_delay(pSwrContext, pCodecContext->sample_rate) + pFrame->nb_samples,
                            targetSampleRate, pCodecContext->sample_rate, AVRounding.AV_ROUND_UP);

                        float* pOutputBuffer = null;
                        ffmpeg.av_samples_alloc((byte**)&pOutputBuffer, null, targetChannels, maxTargetNbSamples, targetFormat, 0);

                        byte** pData = (byte**)&pFrame->data;
                        int convertedSamples = ffmpeg.swr_convert(pSwrContext, (byte**)&pOutputBuffer, maxTargetNbSamples,
                            pData, pFrame->nb_samples);

                        if (convertedSamples > 0)
                        {
                            for (int i = 0; i < convertedSamples * targetChannels; i++)
                            {
                                allSamples.Add(pOutputBuffer[i]);
                            }
                        }

                        ffmpeg.av_freep(&pOutputBuffer);
                    }
                }
            }
            ffmpeg.av_packet_unref(pPacket);
        }

        // Cleanup
        ffmpeg.swr_free(&pSwrContext);
        ffmpeg.av_frame_free(&pFrame);
        ffmpeg.av_packet_free(&pPacket);
        ffmpeg.avcodec_free_context(&pCodecContext);
        ffmpeg.avformat_close_input(&pFormatContext);

        return new AudioData
        {
            Samples = allSamples.ToArray(),
            SampleRate = targetSampleRate,
            Channels = targetChannels
        };
    }

    public struct AudioData
    {
        public float[] Samples { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
    }
}

