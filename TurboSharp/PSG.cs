using System;
using System.Text;
using System.Runtime.InteropServices;
using Tao.Sdl;

namespace TurboSharp
{
    public class PSG
    {
        public const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;

        [UnmanagedFunctionPointer(CALLING_CONVENTION)]
        public unsafe delegate void sdlAudioCallback(
                    void* userdata,
                    void* stream, int len);

        public struct SDL_AudioSpec
        {

            public int freq;
            public short format;
            public byte channels;
            public byte silence;
            public short samples;
            public short padding;
            public int size;
            public sdlAudioCallback callback;
            public object userdata;
        }

        private int           m_SampleRate;
        private SDL_AudioSpec m_ObtainedSpecs;
        private short         m_BaseLine;

        private class PSG_Channel
        {
            public int m_Frequency;
            public float m_RealFrequency;

            public bool m_Enabled;
            public bool m_DDA;
            public int m_Left_Volume;
            public int m_Right_Volume;
            public int m_Volume;
            public int m_DDA_Output;

            public bool m_Noise;
            public int m_NoiseFrequency;
            public float m_RealNoiseFrequency;
            public float m_NoiseIndex;

            public float m_LeftOutput;
            public float m_RightOutput;            

            public int[] m_Buffer;
            public int m_BufferIndex;
            public float m_OutputIndex;
        }

        private int m_Left_Volume;
        private int m_Right_Volume;
        private float m_RealLFOFrequency;
        private int m_LFO_Frequency;

        private bool m_LFO_Enabled;
        private bool m_LFO_Active;
        private int m_LFO_Shift;

        private PSG_Channel[] m_Channels;
        private PSG_Channel m_Selected;
        private static int[] m_NoiseBuffer;
        private static float[] m_VolumeTable;
        private CDRom m_CDRom;

        static PSG()
        {
            m_NoiseBuffer = new int[0x8000];

            int reg = 0x100;
            int i;

            for (i = 0; i < m_NoiseBuffer.Length; i++)
            {
                int bit0 = reg & 0x01;
                int bit1 = (reg & 0x02) >> 1;

                reg = (reg >> 1) | ((bit0 ^ bit1) << 14);
                m_NoiseBuffer[i] = ((bit0 == 1) ? -12 : 12);
            }

            m_VolumeTable = new float[92];

            for (i = 0; i < 92; i++)
            {
                m_VolumeTable[i] = 1024.0f * (float)Math.Pow(10.0, (91-i) * -0.075);
            }
        }

        public PSG(CDRom cdrom)
        {
            m_Channels = new PSG_Channel[8];
            
            for (int i = 0; i < 8; i++)
            {
                m_Channels[i] = new PSG_Channel();
                m_Channels[i].m_Buffer = new int[32];
                m_Channels[i].m_BufferIndex = 0;
            }

            m_Selected = m_Channels[0];
            m_CDRom = cdrom;

            StartMixer(44100, 1024);
        }

        ~PSG()
        {
            Sdl.SDL_CloseAudio();
        }

        public void Mute( bool mute )
        {
            Sdl.SDL_PauseAudio( mute ? 1 : 0 );
        }

        public unsafe void StartMixer(int sampleRate, short bufferLen)
        {
            SDL_AudioSpec desired = new SDL_AudioSpec();
            m_ObtainedSpecs = new SDL_AudioSpec();

            desired.channels = 2;
            desired.format = (short) Sdl.AUDIO_S16SYS;
            desired.freq = sampleRate;
            desired.samples = bufferLen;
            desired.callback = audioCallback;

            IntPtr desiredPtr = Marshal.AllocHGlobal(Marshal.SizeOf(desired));
            IntPtr obtainedPtr = Marshal.AllocHGlobal(Marshal.SizeOf(m_ObtainedSpecs));

            try
            {
                Marshal.StructureToPtr(desired, desiredPtr, false);
                Sdl.SDL_OpenAudio(desiredPtr, obtainedPtr);
                m_ObtainedSpecs = (SDL_AudioSpec)Marshal.PtrToStructure(obtainedPtr, typeof(SDL_AudioSpec));
            }
            finally
            {
                Marshal.FreeHGlobal(desiredPtr);
                Marshal.FreeHGlobal(obtainedPtr);
            }

            m_SampleRate = m_ObtainedSpecs.freq;
            m_BaseLine = m_ObtainedSpecs.silence;

            Mute(false);            
        }

        public unsafe void audioCallback(void* userdata, void* buffer, int len)
        {
            short* stream = (short*)buffer;
            int i,c;

            len /= 2;

            for (i = 0; i < len; i += 2)
            {
                float left = 0;
                float right = 0;

                if (m_LFO_Enabled && m_LFO_Active)
                {
                    PSG_Channel channel = m_Channels[1];
                    int LFOFreq  = channel.m_Buffer[(int)channel.m_OutputIndex];

                    channel.m_OutputIndex += m_RealLFOFrequency;
                    while (channel.m_BufferIndex >= 32)
                        channel.m_BufferIndex -= 32;

                    // Sign extend the frequency
                    if ((LFOFreq & 0x10) != 0)
                        LFOFreq |= -16;

                    channel = m_Channels[0];
                    channel.m_RealFrequency = 3584160.0f / m_SampleRate / (channel.m_Frequency + (LFOFreq << m_LFO_Shift) + 1);
                }

                for (c = 0; c < 6; c++)
                {
                    PSG_Channel channel = m_Channels[c];

                    // LFO disables channel 1
                    if (!channel.m_Enabled || (m_LFO_Enabled && c == 1) )
                        continue;
                    
                    int sample;

                    if (channel.m_DDA)
                        sample = channel.m_DDA_Output;
                    else if (channel.m_Noise)
                    {
                        sample = m_NoiseBuffer[(int)channel.m_NoiseIndex];
                        channel.m_NoiseIndex += channel.m_RealFrequency;

                        while (channel.m_NoiseIndex >= 0x8000)
                            channel.m_NoiseIndex -= 0x8000;
                    }
                    else
                    {
                        sample = channel.m_Buffer[(int)channel.m_OutputIndex];
                        channel.m_OutputIndex += channel.m_RealFrequency;

                        while (channel.m_OutputIndex >= 32)
                            channel.m_OutputIndex -= 32;
                    }

                    left += sample * channel.m_LeftOutput;
                    right += sample * channel.m_RightOutput;
                }

                *(stream++) = (short)(right + m_BaseLine);
                *(stream++) = (short)(left + m_BaseLine);
            }

            m_CDRom.MixCD((short*)buffer, len*2);
        }
        
        public void Write(int address, byte data)
        {
            switch (address)
            {
                // Global audio registers
                case 0x800:
                    m_Selected = m_Channels[data & 0x07];
                    break;
                case 0x801:
                    m_Left_Volume = (data >> 4);
                    m_Right_Volume = (data & 0x0F);
                    break;
                
                case 0x808:
                    m_LFO_Frequency = data;
                    break;
                case 0x809:                    
                    m_LFO_Enabled = (data & 0x80) != 0;

                    switch (data & 0x3)
                    {
                        case 0:
                            m_LFO_Active = false;
                            break;
                        case 1:
                            m_LFO_Active = true;
                            m_LFO_Shift = 0;
                            break;
                        case 2:
                            m_LFO_Active = true;
                            m_LFO_Shift = 4;
                            break;
                        case 3:
                            m_LFO_Active = true;
                            m_LFO_Shift = 8;
                            break;
                    }
                    if (m_LFO_Enabled && m_LFO_Active)
                        Console.WriteLine("LFO MODE HAS BEEN ACTIVATED");
                    break;
                // Per Channel Registers
                case 0x802:
                    m_Selected.m_Frequency = (m_Selected.m_Frequency & 0x0F00) | data;
                    break;
                case 0x803:
                    m_Selected.m_Frequency = (m_Selected.m_Frequency & 0x00FF) | ((data << 8) & 0x0F00);
                    break;
                case 0x804:
                    m_Selected.m_Enabled = (data & 0x80) != 0;
                    m_Selected.m_DDA     = (data & 0x40) != 0;
                    m_Selected.m_Volume = data & 0x0F;
                    break;
                case 0x805:
                    m_Selected.m_Left_Volume = (data >> 4);
                    m_Selected.m_Right_Volume = (data & 0x0F);
                    break;
                case 0x806:
                    m_Selected.m_DDA_Output = data & 0x1F;
                    m_Selected.m_Buffer[m_Selected.m_BufferIndex] = (data & 0x1F) - 0x10;
                    m_Selected.m_BufferIndex = (m_Selected.m_BufferIndex + 1) & 0x1F;
                    break;
                case 0x807:
                    m_Selected.m_Noise = ((data & 0x80) != 0);
                    m_Selected.m_NoiseFrequency = (data & 0x1F ^ 0x1F);
                    m_Selected.m_RealNoiseFrequency = 112005.0f / m_SampleRate / (m_Selected.m_NoiseFrequency + 1);
                    break;
                default:
                    Console.WriteLine("PSG Access at {0:x} -> {1:x}", address, data);
                    break;
            }

            for (int i = 0; i < 6; i++)
            {
                PSG_Channel channel = m_Channels[i];
                channel.m_RealFrequency = 3584160.0f / m_SampleRate / (channel.m_Frequency + 1);
                channel.m_LeftOutput = m_VolumeTable[(channel.m_Left_Volume + m_Left_Volume) * 2 + channel.m_Volume];
                channel.m_RightOutput = m_VolumeTable[(channel.m_Right_Volume + m_Right_Volume) * 2 + channel.m_Volume];

                if (i < 4)
                    channel.m_Noise = false;
            }

            m_RealLFOFrequency = 3584160.0f / m_SampleRate / ((m_Channels[1].m_Frequency + 1) * m_LFO_Frequency);
        }
    }
}
