using GBEmu.Core;
using OpenTK.Audio.OpenAL;
using System;

namespace GBEmu.Audio
{
    class SoundChannel : IDisposable
    {
        readonly int[] alBuffers;
        readonly byte[] soundData;
        readonly byte[] resampledSoundData;
        int soundDataPosition;
        BufferStates bufferState;
        int alSource;

        enum BufferStates { EnqueuingData, Playing }

        public const int BUFFERS_PER_CHANNEL = 2;
        public const int BUFFER_SIZE = 8 * 1024;

        public SoundChannel()
        {
            alBuffers = new int[BUFFERS_PER_CHANNEL];
            soundData = new byte[BUFFERS_PER_CHANNEL * BUFFER_SIZE];
            resampledSoundData = new byte[BUFFER_SIZE];
            soundDataPosition = 0;
            bufferState = BufferStates.EnqueuingData;

            for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
            {
                alBuffers[b] = AL.GenBuffer();
            }
        }

        public void Dispose()
        {
            for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
            {
                AL.DeleteBuffer(alBuffers[b]);
            }

            DeleteSource();
        }

        public void ProcessChannel(byte data)
        {
            if (bufferState == BufferStates.EnqueuingData)
            {
                soundData[soundDataPosition] = data;
                soundDataPosition++;

                if (soundDataPosition < BUFFERS_PER_CHANNEL * BUFFER_SIZE) return;

                soundDataPosition = 0;
                InitializeSource();

                for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
                {
                    AL.BufferData(alBuffers[b], ALFormat.Mono8, ref soundData[b * BUFFER_SIZE], BUFFER_SIZE, APU.SAMPLE_RATE);
                    AL.SourceQueueBuffer(alSource, alBuffers[b]);
                }

                bufferState = BufferStates.Playing;
                AL.SourcePlay(alSource);
            }
            else
            {
                AL.GetSource(alSource, ALGetSourcei.SourceState, out int sourceState);
                if (sourceState != (int)ALSourceState.Playing)
                {
                    DeleteSource();
                    return;
                }

                if (soundDataPosition < BUFFERS_PER_CHANNEL * BUFFER_SIZE)
                {
                    soundData[soundDataPosition] = data;
                    soundDataPosition++;
                }

                AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out int buffersProcessed);

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

                    Resample(soundData, soundDataPosition, resampledSoundData, BUFFER_SIZE);
                    //Utils.Log($"Buffers processed: {buffersProcessed}, Buffer Size: {soundDataPosition[1]}, New Buffer Dize: {newSoundData.Length}");

                    int unqueuedBuffer = AL.SourceUnqueueBuffer(alSource);

                    AL.BufferData(unqueuedBuffer, ALFormat.Mono8, ref resampledSoundData[0], BUFFER_SIZE, APU.SAMPLE_RATE);
                    AL.SourceQueueBuffer(alSource, unqueuedBuffer);

                    soundDataPosition = 0;
                }
            }
        }

        static void Resample(byte[] source, int sourceLength, byte[] destination, int destinationLength)
        {
            float d = (float)sourceLength / destinationLength;

            for (int b = 0; b < destinationLength; b++)
            {
                //destination[b] = source[(int)(b * d)];

                if (b < sourceLength)
                    destination[b] = source[b];
                else
                    destination[b] = 127;
            }
        }

        void InitializeSource()
        {
            if (alSource > 0) return;

            alSource = AL.GenSource();
            AL.Source(alSource, ALSourcef.Gain, 1f);
            AL.Source(alSource, ALSourceb.Looping, false);
        }

        public void DeleteSource()
        {
            if (alSource <= 0) return;

            AL.SourceStop(alSource);
            AL.DeleteSource(alSource);
            alSource = 0;

            bufferState = BufferStates.EnqueuingData;
            soundDataPosition = 0;
        }
    }
}