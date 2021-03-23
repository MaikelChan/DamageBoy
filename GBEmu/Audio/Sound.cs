using GBEmu.Core;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;

namespace GBEmu.Audio
{
    class Sound : IDisposable
    {
        readonly ALDevice device;
        readonly ALContext context;

        readonly int[][] alBuffers;
        readonly int[] alSources;
        //readonly bool[] sourcesPlaying;

        readonly byte[][] soundData;
        int[] soundDataPosition;

        bool isInitialized;
        //SoundState previousState;

        enum BufferStates { EnqueuingData, Playing }
        BufferStates bufferState;

        const int BUFFERS_PER_CHANNEL = 2;
        const int BUFFER_SIZE = 4 * 1024;

        public Sound()
        {
            try
            {
                IEnumerable<string> devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
                Utils.Log(LogType.Info, $"Audio Devices: {string.Join(", ", devices)}");

                // Get the default device, then go though all devices and select the AL soft device if it exists.
                string deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
                foreach (var d in devices)
                {
                    if (d.Contains("OpenAL Soft"))
                    {
                        deviceName = d;
                        break;
                    }
                }

                device = ALC.OpenDevice(deviceName);
                context = ALC.CreateContext(device, (int[])null);
                ALC.MakeContextCurrent(context);

                CheckALError("Start");

                string vend = AL.Get(ALGetString.Vendor);
                string vers = AL.Get(ALGetString.Version);
                string rend = AL.Get(ALGetString.Renderer);
                ALContextAttributes attrs = ALC.GetContextAttributes(device);
                ALC.GetInteger(device, AlcGetInteger.MajorVersion, 1, out int alcMajorVersion);
                ALC.GetInteger(device, AlcGetInteger.MinorVersion, 1, out int alcMinorVersion);

                Utils.Log(LogType.Info, $"Vendor: {vend}");
                Utils.Log(LogType.Info, $"Version: {vers}");
                Utils.Log(LogType.Info, $"Renderer: {rend}");
                Utils.Log(LogType.Info, $"Attributes: {attrs}");
                Utils.Log(LogType.Info, $"ALC Version: {alcMajorVersion}.{alcMinorVersion}");

                CheckALError("Before generating buffer");

                alBuffers = new int[APU.SOUND_CHANNEL_COUNT][];
                soundData = new byte[APU.SOUND_CHANNEL_COUNT][];
                soundDataPosition = new int[APU.SOUND_CHANNEL_COUNT];
                for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
                {
                    alBuffers[sc] = new int[BUFFERS_PER_CHANNEL];
                    for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
                    {
                        alBuffers[sc][b] = AL.GenBuffer();
                    }

                    soundData[sc] = new byte[BUFFERS_PER_CHANNEL * BUFFER_SIZE];
                }

                CheckALError("After generating buffer");

                AL.Listener(ALListenerf.Gain, 0.15f);

                alSources = new int[APU.SOUND_CHANNEL_COUNT];
                //sourcesPlaying = new bool[APU.SOUND_CHANNEL_COUNT];

                bufferState = BufferStates.EnqueuingData;

                isInitialized = true;
            }
            catch (DllNotFoundException ex)
            {
                Utils.Log(LogType.Error, ex.Message);
                Utils.Log(LogType.Error, "OpenAL required. No audio will be played.");
            }
        }

        public void Dispose()
        {
            if (!isInitialized) return;
            isInitialized = false;

            for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
            {
                DeleteSource(sc);

                alBuffers[sc] = new int[BUFFERS_PER_CHANNEL];
                for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
                {
                    AL.DeleteBuffer(alBuffers[sc][b]);
                }
            }

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(context);
            ALC.CloseDevice(device);
        }

        //byte[] buf = new byte[APU.SAMPLE_RATE];

        public void Update(SoundState state)
        {
            if (!isInitialized) return;

            //// Channel 1

            //if (previousState.Channel1Frequency != state.Channel1Frequency)
            //{
            //    AL.SourceStop(alSources[0]);
            //    //sourcesPlaying[0] = false;

            //    AL.Source(alSources[0], ALSourcei.Buffer, 0);

            //    byte[] wave = GenerateSquareWave(state.Channel1Frequency, state.Channel1WavePattern, APU.SAMPLE_RATE);
            //    AL.BufferData(alBuffers[0], ALFormat.Mono8, ref wave[0], wave.Length * sizeof(byte), APU.SAMPLE_RATE);

            //    AL.Source(alSources[0], ALSourcei.Buffer, alBuffers[0]);

            //    if (state.Channel1Enabled) AL.SourcePlay(alSources[0]);
            //}

            //if (previousState.Channel1Enabled != state.Channel1Enabled)
            //{
            //    if (state.Channel1Enabled) AL.SourcePlay(alSources[0]);
            //    else AL.SourceStop(alSources[0]);
            //}

            //AL.Source(alSources[0], ALSourcef.Gain, state.Channel1Volume);

            //// Channel 2

            //if (previousState.Channel2Frequency != state.Channel2Frequency)
            //{
            //    AL.SourceStop(alSources[1]);
            //    //sourcesPlaying[1] = false;

            //    AL.Source(alSources[1], ALSourcei.Buffer, 0);

            //    byte[] wave = GenerateSquareWave(state.Channel2Frequency, state.Channel2WavePattern, APU.SAMPLE_RATE);
            //    AL.BufferData(alBuffers[1], ALFormat.Mono8, ref wave[0], wave.Length * sizeof(byte), APU.SAMPLE_RATE);

            //    AL.Source(alSources[1], ALSourcei.Buffer, alBuffers[1]);

            //    if (state.Channel2Enabled) AL.SourcePlay(alSources[1]);
            //}

            //if (previousState.Channel2Enabled != state.Channel2Enabled)
            //{
            //    if (state.Channel2Enabled) AL.SourcePlay(alSources[1]);
            //    else AL.SourceStop(alSources[1]);
            //}

            //AL.Source(alSources[1], ALSourcef.Gain, state.Channel2Volume);




            //buf[soundDataPosition[1]] = state.Channel2Data;
            //soundDataPosition[1]++;

            //if (soundDataPosition[1] >= APU.SAMPLE_RATE)
            //{
            //    Utils.Log("Updated LOOOOOOOOOOOOOOOOOOL");

            //    soundDataPosition[1] = 0;

            //    AL.SourceStop(alSources[1]);
            //    AL.Source(alSources[1], ALSourcei.Buffer, 0);

            //    AL.BufferData(alBuffers[1][0], ALFormat.Mono8, ref buf[0], APU.SAMPLE_RATE, APU.SAMPLE_RATE);

            //    AL.Source(alSources[1], ALSourcei.Buffer, alBuffers[1][0]);

            //    AL.SourcePlay(alSources[1]);
            //}

            ProcessChannel(1, state.Channel2Data);


            // Channel 3

            //buf[soundDataPosition[2]] = state.Channel3Data;
            //soundDataPosition[2]++;

            //if (soundDataPosition[2] >= APU.SAMPLE_RATE)
            //{
            //    Utils.Log("Updated LOOOOOOOOOOOOOOOOOOL");

            //    soundDataPosition[2] = 0;

            //    AL.SourceStop(alSources[2]);
            //    AL.Source(alSources[2], ALSourcei.Buffer, 0);

            //    AL.BufferData(alBuffers[2][0], ALFormat.Mono8, ref buf[0], APU.SAMPLE_RATE, APU.SAMPLE_RATE);

            //    AL.Source(alSources[2], ALSourcei.Buffer, alBuffers[2][0]);

            //    AL.SourcePlay(alSources[2]);
            //}

            //if (bufferState == BufferStates.EnqueuingData)
            //{
            //    soundData[2][soundDataPosition[2]] = state.Channel3Data;
            //    soundDataPosition[2]++;

            //    if (soundDataPosition[2] >= soundData[2].Length)
            //    {
            //        soundDataPosition[2] = 0;

            //        for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
            //        {
            //            AL.BufferData(alBuffers[2][b], ALFormat.Mono8, ref soundData[2][b * BUFFER_SIZE], BUFFER_SIZE, APU.SAMPLE_RATE);
            //            AL.SourceQueueBuffer(alSources[2], alBuffers[2][b]);
            //        }

            //        bufferState = BufferStates.Playing;
            //        AL.SourcePlay(alSources[2]);
            //    }
            //}
            //else
            //{
            //    AL.GetSource(alSources[2], ALGetSourcei.BuffersProcessed, out int buffersProcessed);

            //    if (buffersProcessed < 1)
            //    {
            //        throw new Exception($"There are {buffersProcessed} audio buffers to unqueue in channel 3. There should be 1.");
            //    }

            //    int unqueuedBuffer = AL.SourceUnqueueBuffer(alSources[2]);

            //    AL.BufferData(unqueuedBuffer, ALFormat.Mono8, ref state.Channel3Buffer[0], APU.BUFFER_SIZE, APU.SAMPLE_RATE);
            //    AL.SourceQueueBuffer(alSources[2], unqueuedBuffer);
            //}


        }

        public void Stop()
        {
            for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
            {
                DeleteSource(sc);
            }
        }

        static void CheckALError(string str)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                Utils.Log(LogType.Error, $"ALError at '{str}': {AL.GetErrorString(error)}");
            }
        }

        void ProcessChannel(int index, byte data)
        {
            if (bufferState == BufferStates.EnqueuingData)
            {
                soundData[index][soundDataPosition[index]] = data;
                soundDataPosition[index]++;

                if (soundDataPosition[index] >= BUFFERS_PER_CHANNEL * BUFFER_SIZE)
                {
                    soundDataPosition[index] = 0;

                    InitializeSource(index);

                    for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
                    {
                        AL.BufferData(alBuffers[index][b], ALFormat.Mono8, ref soundData[index][b * BUFFER_SIZE], BUFFER_SIZE, APU.SAMPLE_RATE);
                        AL.SourceQueueBuffer(alSources[index], alBuffers[index][b]);
                    }

                    bufferState = BufferStates.Playing;
                    AL.SourcePlay(alSources[index]);
                }
            }
            else
            {
                AL.GetSource(alSources[index], ALGetSourcei.SourceState, out int sourceState);
                if (sourceState != (int)ALSourceState.Playing)
                {
                    DeleteSource(index);
                    bufferState = BufferStates.EnqueuingData;
                    soundDataPosition[index] = 0;

                    return;
                }

                if (soundDataPosition[index] < BUFFERS_PER_CHANNEL * BUFFER_SIZE)
                {
                    soundData[index][soundDataPosition[index]] = data;
                    soundDataPosition[index]++;
                }

                AL.GetSource(alSources[index], ALGetSourcei.BuffersProcessed, out int buffersProcessed);

                if (buffersProcessed > 0)
                {
                    //if (soundDataPosition[index] >= BUFFERS_PER_CHANNEL * BUFFER_SIZE)
                    //{
                    //    Utils.Log(LogType.Error, $"There are {buffersProcessed} audio buffers to unqueue in channel 3. There should be 1.");
                    //}

                    //while (soundDataPosition[index] < BUFFER_SIZE)
                    //{
                    //    soundData[index][soundDataPosition[index]] = 127;
                    //    soundDataPosition[index]++;
                    //}

                    byte[] newSoundData = Resample(index, soundDataPosition[index], BUFFER_SIZE);
                    Utils.Log($"Buffers processed: {buffersProcessed}, Buffer Size: {soundDataPosition[1]}, New Buffer Dize: {newSoundData.Length}");

                    int unqueuedBuffer = AL.SourceUnqueueBuffer(alSources[index]);

                    AL.BufferData(unqueuedBuffer, ALFormat.Mono8, ref newSoundData[0], BUFFER_SIZE, APU.SAMPLE_RATE);
                    AL.SourceQueueBuffer(alSources[index], unqueuedBuffer);

                    soundDataPosition[index] = 0;
                }
            }
        }

        void InitializeSource(int index)
        {
            if (alSources[index] > 0) return;

            alSources[index] = AL.GenSource();
            AL.Source(alSources[index], ALSourcef.Gain, 1f);
            AL.Source(alSources[index], ALSourceb.Looping, false);
        }

        void DeleteSource(int index)
        {
            if (alSources[index] <= 0) return;

            AL.SourceStop(alSources[index]);
            AL.DeleteSource(alSources[index]);
            alSources[index] = 0;
        }

        byte[] Resample(int index, int sourceLength, int destinationLength)
        {
            byte[] newBuffer = new byte[destinationLength];
            float d = (float)sourceLength / destinationLength;

            for (int b = 0; b < destinationLength; b++)
            {
                newBuffer[b] = soundData[index][(int)(b * d)];
            }

            return newBuffer;
        }

        //static byte[] GenerateSquareWave(float frequency, Core.APU.WavePatternDuties wavePattern, float sampleRate)
        //{
        //    float waveLength = 1 / frequency;
        //    int bufferLength = Math.Max(1, (int)(waveLength * sampleRate));
        //    byte[] buffer = new byte[bufferLength];

        //    float percentage;
        //    switch (wavePattern)
        //    {
        //        default:
        //        case Core.APU.WavePatternDuties.Percent12_5: percentage = 0.875f; break;
        //        case Core.APU.WavePatternDuties.Percent25: percentage = 0.75f; break;
        //        case Core.APU.WavePatternDuties.Percent50: percentage = 0.50f; break;
        //        case Core.APU.WavePatternDuties.Percent75: percentage = 0.25f; break;
        //    }

        //    percentage = percentage * 2 - 1;

        //    for (int b = 0; b < buffer.Length; b++)
        //    {
        //        float sin = MathF.Sin((b * frequency * MathF.PI * 2) / sampleRate);
        //        //buffer[b] = (short)(sin * byte.MaxValue);
        //        buffer[b] = sin >= percentage ? byte.MaxValue : byte.MinValue;
        //    }

        //    return buffer;
        //}
    }
}