using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

[TestFixture]
public class Tests
{
    private readonly FfmpegAudioLoader ffmpegAudioLoader = new FfmpegAudioLoader();
    
    [Test]
    [TestCaseSource(nameof(GetAudioFiles))]
    public void ShouldLoadAudioSamples(string filePath)
    {
        ffmpegAudioLoader.ConfigureFfmpegRootPath();
        var audioData = ffmpegAudioLoader.Load(filePath);
        
        Assert.NotNull(audioData);
        Assert.NotNull(audioData.Samples);
        Assert.Greater(audioData.Samples.Length, 0);
        Assert.AreEqual(44100, audioData.SampleRate);
        Assert.AreEqual(2, audioData.Channels);

        Console.WriteLine($"Loaded audio samples. Channels: {audioData.Channels}, SampleRate: {audioData.SampleRate}, Samples.Length: {audioData.Samples.Length}, Path: '{Path.GetFileName(filePath)}'");
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
