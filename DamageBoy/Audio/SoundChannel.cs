using DamageBoy.Core;
using OpenTK.Audio.OpenAL;
using System;
using static DamageBoy.Audio.Sound;

namespace DamageBoy.Audio
{
    class SoundChannel : IDisposable
    {
        readonly Action<BufferStates> bufferStateChangeCallback;

        readonly int[] alBuffers;
        readonly byte[] soundData;
        int soundDataPosition;
        int alSource;

        BufferStates bufferState;

        public const int BUFFERS_PER_CHANNEL = 2;
        public const int BUFFER_SIZE = 4096;

        public SoundChannel(Action<BufferStates> bufferStateChangeCallback)
        {
            this.bufferStateChangeCallback = bufferStateChangeCallback;

            alBuffers = new int[BUFFERS_PER_CHANNEL];
            for (int b = 0; b < BUFFERS_PER_CHANNEL; b++) alBuffers[b] = AL.GenBuffer();

            soundData = new byte[BUFFER_SIZE];
            soundDataPosition = 0;

            SetBufferState(BufferStates.Uninitialized);
        }

        public void Dispose()
        {
            SetBufferState(BufferStates.Uninitialized);

            for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
            {
                AL.DeleteBuffer(alBuffers[b]);
                alBuffers[b] = 0;
            }

            DeleteSource();
        }

        public void ClearBuffer()
        {
            Array.Fill<byte>(soundData, 127);
        }

        public void Update()
        {
            InitializeSource();

            AL.GetSource(alSource, ALGetSourcei.SourceState, out int sourceState);

            switch ((ALSourceState)sourceState)
            {
                case ALSourceState.Initial:

                    for (int b = 0; b < BUFFERS_PER_CHANNEL; b++)
                    {
                        AL.BufferData(alBuffers[b], ALFormat.Stereo8, ref soundData[0], BUFFER_SIZE, Constants.SAMPLE_RATE);
                        AL.SourceQueueBuffer(alSource, alBuffers[b]);
                    }

                    AL.SourcePlay(alSource);

                    break;

                case ALSourceState.Playing:

                    AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out int buffersProcessed);

                    if (buffersProcessed > 0)
                    {
                        if (soundDataPosition < BUFFER_SIZE)
                        {
                            SetBufferState(BufferStates.Underrun);
                            break;
                        }
                        else
                        {
                            SetBufferState(BufferStates.Ok);
                        }

                        if (buffersProcessed != 1) Utils.Log(LogType.Warning, "Buffers processed: " + buffersProcessed);

                        int unqueuedBuffer = AL.SourceUnqueueBuffer(alSource);

                        AL.BufferData(unqueuedBuffer, ALFormat.Stereo8, ref soundData[0], BUFFER_SIZE, Constants.SAMPLE_RATE);
                        AL.SourceQueueBuffer(alSource, unqueuedBuffer);

                        soundDataPosition = 0;
                    }
                    else
                    {
                        if (soundDataPosition >= BUFFER_SIZE)
                        {
                            SetBufferState(BufferStates.Overrun);
                        }
                        else
                        {
                            SetBufferState(BufferStates.Ok);
                        }
                    }

                    break;

                case ALSourceState.Paused:

                    Utils.Log(LogType.Error, "Audio source is paused. This should not be happening.");

                    break;

                case ALSourceState.Stopped:

                    Utils.Log(LogType.Warning, "Audio buffer underrun. Reinitializing it...");

                    // Need to fully delete the source before starting playback again.
                    // I haven't seen a way to unqueue all buffers, which is required
                    // to not have old audio playing when starting playback again).
                    // SourceUnqueueBuffer() only unqueues processed ones, not all of them.

                    DeleteSource();

                    break;
            }
        }

        public void AddToAudioBuffer(byte leftValue, byte rightValue)
        {
            if (soundDataPosition >= BUFFER_SIZE)
            {
                //Utils.Log(LogType.Warning, "Trying to add data to the audio buffer while it's full.");
                return;
            }

            soundData[soundDataPosition + 0] = leftValue;
            soundData[soundDataPosition + 1] = rightValue;
            soundDataPosition += 2;

            if (soundDataPosition >= BUFFER_SIZE)
            {
                SetBufferState(BufferStates.Overrun);
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

            //audioState = AudioStates.Buffering;
            //SetBufferState(BufferStates.Ok);
            soundDataPosition = 0;
        }

        void SetBufferState(BufferStates bufferState)
        {
            if (this.bufferState == bufferState) return;
            this.bufferState = bufferState;

            bufferStateChangeCallback?.Invoke(bufferState);
        }

        //static void Resample(byte[] source, int sourceLength, byte[] destination, int destinationLength)
        //{
        //    float d = (float)sourceLength / destinationLength;

        //    for (int b = 0; b < destinationLength; b++)
        //    {
        //        //destination[b] = source[(int)(b * d)];

        //        if (b < sourceLength)
        //            destination[b] = source[b];
        //        else
        //            destination[b] = 127;
        //    }
        //}
    }
}