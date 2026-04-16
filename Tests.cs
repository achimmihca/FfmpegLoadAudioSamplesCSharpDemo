using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

[TestFixture]
public class Tests
{
    private readonly FfmpegAudioLoader ffmpegAudioLoader = new FfmpegAudioLoader();
    
    [Test]
    public void ShouldLoadWithDefaultSampleRateAndChannels()
    {
        ffmpegAudioLoader.ConfigureFfmpegRootPath();
        
        // Use a specific file with known properties: wav.wav is usually 44100Hz Stereo
        var testAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "TestMedia");
        if (!Directory.Exists(testAudioDir)) testAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestMedia");
        var filePath = Path.Combine(testAudioDir, "wav.wav");
        
        var audioDataDefault = ffmpegAudioLoader.Load(filePath);
        Assert.NotNull(audioDataDefault);
        Assert.AreEqual(44100, audioDataDefault.SampleRate);
        Assert.AreEqual(2, audioDataDefault.Channels);
        Assert.Greater(audioDataDefault.Samples.Length, 0);
        
        Console.WriteLine($"Loaded wav.wav with default parameters. Rate: {audioDataDefault.SampleRate}, Channels: {audioDataDefault.Channels}, Samples: {audioDataDefault.Samples.Length}");
    }

    [Test]
    [TestCaseSource(nameof(GetAudioFiles))]
    public void ShouldLoadAudioSamples(string filePath)
    {
        ffmpegAudioLoader.ConfigureFfmpegRootPath();
        
        // Load with explicit target parameters
        var audioDataExplicit = ffmpegAudioLoader.Load(filePath, 44100, 2);
        Assert.NotNull(audioDataExplicit);
        Assert.NotNull(audioDataExplicit.Samples);
        Assert.Greater(audioDataExplicit.Samples.Length, 0);
        Assert.AreEqual(44100, audioDataExplicit.SampleRate);
        Assert.AreEqual(2, audioDataExplicit.Channels);

        // Load with default parameters (original rate/channels)
        var audioDataDefault = ffmpegAudioLoader.Load(filePath);
        Assert.NotNull(audioDataDefault);
        Assert.NotNull(audioDataDefault.Samples);
        Assert.Greater(audioDataDefault.Samples.Length, 0);
        
        Console.WriteLine($"Loaded audio. Explicit(44100, 2) Samples: {audioDataExplicit.Samples.Length}. Default({audioDataDefault.SampleRate}, {audioDataDefault.Channels}) Samples: {audioDataDefault.Samples.Length}. Path: '{Path.GetFileName(filePath)}'");
    }

    [Test]
    public void ShouldThrowDetailedExceptionForNonExistentFile()
    {
        ffmpegAudioLoader.ConfigureFfmpegRootPath();
        var exception = Assert.Throws<Exception>(() => ffmpegAudioLoader.Load("non_existent_file.wav"));
        
        // Error code for "No such file or directory" is usually -2
        Assert.That(exception.Message, Contains.Substring("Could not open file:"));
        Assert.That(exception.Message, Contains.Substring("Code: -2"));
        Console.WriteLine($"Caught expected exception: {exception.Message}");
    }

    [Test]
    public void ShouldLoadAudioSamplesAndSaveToWav()
    {
        ffmpegAudioLoader.ConfigureFfmpegRootPath();
        
        var testAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "TestMedia");
        if (!Directory.Exists(testAudioDir)) testAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestMedia");
        var filePath = Path.Combine(testAudioDir, "wav.wav");
        
        var audioData = ffmpegAudioLoader.Load(filePath);
        Assert.NotNull(audioData);
        Assert.Greater(audioData.Samples.Length, 0);

        var outputWav = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output_test.wav");
        if (File.Exists(outputWav)) File.Delete(outputWav);

        WavFileWriter.Write(outputWav, audioData.Samples, audioData.SampleRate, audioData.Channels);
        
        Assert.IsTrue(File.Exists(outputWav), "Output WAV file should be created");
        Console.WriteLine($"Saved and verified samples. Path: {outputWav}");
    }

    public static IEnumerable<string> GetAudioFiles()
    {
        var testAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "TestMedia");
        if (!Directory.Exists(testAudioDir))
        {
            // Fallback for different build output structures
            testAudioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestMedia");
        }
        
        if (!Directory.Exists(testAudioDir))
        {
             throw new DirectoryNotFoundException($"Could not find TestAudio directory at {Path.GetFullPath(testAudioDir)}");
        }

        return Directory.GetFiles(testAudioDir);
    }
}
