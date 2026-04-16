using System;
using System.IO;
using System.Text;

public static class WavFileWriter
{
    public static void Write(string filePath, float[] samples, int sampleRate, int channels)
    {
        using (var fs = new FileStream(filePath, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            // RIFF header
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + samples.Length * 2); // File size - 8
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // Subchunk1Size (16 for PCM)
            bw.Write((short)1); // AudioFormat (1 for PCM)
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2); // ByteRate
            bw.Write((short)(channels * 2)); // BlockAlign
            bw.Write((short)16); // BitsPerSample

            // data chunk
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(samples.Length * 2); // Subchunk2Size

            foreach (var sample in samples)
            {
                // Clamp and convert float to 16-bit PCM
                var s = (short)(Math.Max(-1.0f, Math.Min(1.0f, sample)) * 32767);
                bw.Write(s);
            }
        }
    }
    public static (float[] samples, int sampleRate, int channels) Read(string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Open))
        using (var br = new BinaryReader(fs))
        {
            var riff = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (riff != "RIFF") throw new Exception("Invalid WAV file (RIFF)");
            br.ReadInt32(); // Size
            var wave = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (wave != "WAVE") throw new Exception("Invalid WAV file (WAVE)");

            int sampleRate = 0;
            int channels = 0;
            float[] samples = null;

            while (fs.Position < fs.Length)
            {
                var chunkId = Encoding.ASCII.GetString(br.ReadBytes(4));
                var chunkSize = br.ReadInt32();

                if (chunkId == "fmt ")
                {
                    var format = br.ReadInt16();
                    channels = br.ReadInt16();
                    sampleRate = br.ReadInt32();
                    br.ReadInt32(); // ByteRate
                    br.ReadInt16(); // BlockAlign
                    var bitsPerSample = br.ReadInt16();

                    if (format != 1 || bitsPerSample != 16)
                        throw new Exception("Only 16-bit PCM WAV is supported for reading.");

                    if (chunkSize > 16) br.ReadBytes(chunkSize - 16);
                }
                else if (chunkId == "data")
                {
                    var sampleCount = chunkSize / 2;
                    samples = new float[sampleCount];
                    for (int i = 0; i < sampleCount; i++)
                    {
                        samples[i] = br.ReadInt16() / 32767.0f;
                    }
                }
                else
                {
                    br.ReadBytes(chunkSize);
                }
            }

            return (samples, sampleRate, channels);
        }
    }
}
