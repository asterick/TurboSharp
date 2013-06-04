using System;
using System.Text;
using System.IO;
using System.Collections;

namespace TurboSharp
{
    public class CDRom
    {
        private enum TrackType
        {
            UNKNOWN,
            WAVE,
            BINARY,
            LEADOUT
        }

        private enum CDState
        {
            RESET,
            COMMAND_RECEIVED,
            COMMAND_DONE
        }

        private class CDTrack
        {
            public FileStream m_DataSource;
            public int m_Track;
            public int m_Frame;
            public int m_WaveStart;
            public int m_MSF_M;
            public int m_MSF_S;
            public int m_MSF_F;

            // Redbook Playback
            public int m_WaveOffset;
            public int m_WaveEnd;

            public TrackType m_TrackType;
        }

        private SaveMemoryBank m_BRAM;
        private RamBank[] m_Ram;
        private CDTrack[] m_CDTracks;
        private bool m_CDInserted;
        private CDTrack m_Playing;
        private byte[] m_PlayChunk;
        private bool m_AudioPaused;
        private ADPCM m_ADPCM;
        private CDState m_CDRomState;

        private short m_OutLeft;
        private short m_OutRight;

        private byte m_CDControlReg;
        private byte m_CDResetRegister;

        private float m_FadeCurrent;
        private float m_FadeStep;
        private bool m_FadeDown;
        private bool m_FadeUp;

        private bool m_DataReady;
        private bool m_DataTransferDone;
        private bool m_PCMChannel;

        private const float m_PlayFrequency = 44100.0f;

        public CDRom()
        {
            m_BRAM = new SaveMemoryBank();
            m_Ram = new RamBank[32];
            m_CDInserted = false;
            m_ADPCM = new ADPCM();

            for (int i = 0; i < 32; i++)
            {
                m_Ram[i] = new RamBank();
            }

            m_Playing = null;
            m_AudioPaused = false;
            m_PlayChunk = new byte[0x20000];

            m_FadeCurrent = 1.0f;
            m_FadeDown = false;
            m_FadeUp = false;
            m_FadeStep = 0.0f;

            m_DataReady = false;
            m_DataTransferDone = false;

            m_CDRomState = CDState.RESET;
        }

        public SaveMemoryBank GetSaveMemory()
        {
            return m_BRAM;
        }

        public RamBank GetRam(int i)
        {
            return m_Ram[i];
        }

        public bool IRQWaiting()
        {
            // SHOULD DO CD STATE
            return m_ADPCM.GetState() != ADPCMState.ADPCM_STOPPED || m_DataTransferDone || m_DataReady;
        }

        public int WaveSize(FileStream f)
        {
            // THIS SHOULD ACTUALLY PROCESS
                       
            return (int)f.Length - 44;            
        }

        private void FadeAudio( bool down, float step )
        {
            m_FadeDown = down;
            m_FadeUp   = !down;
            m_FadeStep = step;
        }

        public unsafe void MixCD(short* buffer, int length)
        {
            if( m_AudioPaused || m_Playing == null )
                return ;

            int count = (int)m_Playing.m_DataSource.Length - m_Playing.m_WaveOffset;

            if( length < count )
                count = length;
            

            m_Playing.m_DataSource.Position = m_Playing.m_WaveOffset;
            m_Playing.m_DataSource.Read(m_PlayChunk, 0, count);

            int i = 0;

            length /= 2;

            while (length > 0)
            {
                // Mix the channels

                if (m_FadeUp || m_FadeDown)
                {
                    *(buffer++) += m_OutLeft = (short)((m_PlayChunk[i + 1] << 8 | m_PlayChunk[i]) * m_FadeCurrent);
                    i += 2;
                    *(buffer++) += m_OutRight = (short)((m_PlayChunk[i + 1] << 8 | m_PlayChunk[i]) * m_FadeCurrent);
                    i += 2;

                    if (m_FadeDown)
                    {
                        if (m_FadeCurrent > 0.0f)
                            m_FadeCurrent -= m_FadeStep;
                        else
                            m_FadeDown = false;
                    }
                    else
                    {
                        if (m_FadeCurrent < 1.0f)
                            m_FadeCurrent += m_FadeStep;
                        else
                            m_FadeUp = false;
                    }
                }
                else if( m_FadeCurrent > 0.0f )
                {
                    *(buffer++) += m_OutLeft = (short)(m_PlayChunk[i + 1] << 8 | m_PlayChunk[i]);
                    i += 2;
                    *(buffer++) += m_OutRight = (short)(m_PlayChunk[i + 1] << 8 | m_PlayChunk[i]);
                    i += 2;
                }
                
                length -= 4;
            }

            m_Playing.m_WaveOffset += count;

            if (m_Playing.m_WaveOffset > m_Playing.m_WaveEnd)
                m_Playing = null;
        }

        private void PlayAudioTrack( int msfStart, int msfEnd )
        {
            if (!m_CDInserted)
                return;

            for (int i = 0; i < m_CDTracks.Length; i++)
            {
                if (m_CDTracks[i].m_Frame < msfStart)
                    continue;

                msfStart -= m_CDTracks[i].m_Frame;
                msfEnd -= m_CDTracks[i].m_Frame;

                m_Playing = m_CDTracks[i];
                m_Playing.m_WaveOffset = m_Playing.m_WaveStart + msfStart * 2352;
                m_Playing.m_WaveEnd = m_Playing.m_WaveStart + msfEnd * 2352;

                return ;
            }

            m_Playing = null;
        }        

        public void LoadCue( string file )
        {
            StreamReader text = new StreamReader(file);
            Queue q = new Queue();            
            CDTrack track = null;

            Console.WriteLine("Opening CD: {0}", file);

            int frame = 0;
            int i;

            while (!text.EndOfStream)
            {
                string[] line = text.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);                

                switch( line[0] )
                {
                    case "FILE":
                        if( track != null )
                            q.Enqueue(track);
                        track = new CDTrack();

                        file = "";

                        for( i = 1; i < line.Length; i++ )
                        {
                            file += line[i];

                            if (file[0] != '"')
                            {
                                i++;
                                break;
                            }
                            if( file[file.Length-1] == '"' )
                            {
                                file = file.Substring(1, file.Length-2);
                                i++;
                                break;
                            }
                        }

                        track.m_DataSource = new FileStream(file, FileMode.Open, FileAccess.Read);

                        switch (line[i])
                        {
                            case "WAVE":
                                track.m_TrackType = TrackType.WAVE;
                                break;
                            case "BINARY":
                                track.m_TrackType = TrackType.BINARY;
                                break;
                            default:
                                Console.WriteLine("Unknown track type {0}", line[i]);
                                track.m_TrackType = TrackType.UNKNOWN;
                                return;
                        }

                        track.m_Frame = frame;

                        break;
                    case "TRACK":
                        i = Convert.ToInt32(line[1]);
                        if (i - 1 == q.Count)
                        {                            
                            switch (line[2])
                            {
                                case "AUDIO":
                                    if (track.m_TrackType != TrackType.WAVE)
                                    {
                                        Console.WriteLine("AUDIO line with an unknown track data source");
                                        return;
                                    }

                                    i = WaveSize(track.m_DataSource);
                                    track.m_WaveStart = (int)track.m_DataSource.Length - i;

                                    frame += i / 2352;

                                    if( i < 0 )
                                    {
                                        Console.WriteLine("Audio track was not in the proper format (16b PCM Stereo)");
                                        return ;
                                    }
                                   
                                    break;
                                case "MODE1/2048":
                                    if (track.m_TrackType != TrackType.BINARY)
                                    {
                                        Console.WriteLine("MODE1/2048 line with an unknown track data source");
                                        return;
                                    }

                                    // This is a mode1 / 2048 line (no subq channel info)
                                    frame += (int)track.m_DataSource.Length / 2048;

                                    break;
                                default:
                                    Console.WriteLine("Unknown Data Source {0}", line[2]);
                                    break;
                            }                            
                        }
                        else
                        {
                            Console.WriteLine("Cue file TRACK number is out of order");
                            return;
                        }
                        break;
                    case "PREGAP":
                        {
                            string[] msf_s = line[1].Split(new char[] {':'});
                            int msf = (Convert.ToInt32(msf_s[0]) * 60 + Convert.ToInt32(msf_s[1])) * 75 + Convert.ToInt32(msf_s[2]);

                            frame += msf;
                            track.m_Frame += msf;
                        }
                        break;
                    case "POSTGAP":
                        {
                            string[] msf_s = line[1].Split(new char[] {':'});
                            int msf = (Convert.ToInt32(msf_s[0]) * 60 + Convert.ToInt32(msf_s[1])) * 75 + Convert.ToInt32(msf_s[2]);

                            frame += msf;
                        }
                        break;
                    case "INDEX":
                        // THIS IS IGNORED (I don't think the system actually uses it)
                        break;
                }
            }

            // Shove the last track to the TOC
            m_CDTracks = new CDTrack[q.Count+2];
            m_CDTracks[q.Count] = track;

            // Append leadout track
            track = new CDTrack();
            m_CDTracks[m_CDTracks.Length - 1] = track;
            track.m_Frame = frame;
            track.m_Track = 0;
            track.m_TrackType = TrackType.LEADOUT;

            for (i = 0; q.Count > 0; i++)
            {
                m_CDTracks[i] = (CDTrack)q.Dequeue();
            }

            for (i = 0; i < m_CDTracks.Length; i++)
            {
                track = m_CDTracks[i];

                int f = track.m_Frame + 150;
                track.m_MSF_M = f / 75 / 60;
                track.m_MSF_S = f / 75 % 60;
                track.m_MSF_F = f % 75;
                track.m_Track = i + 1;
            }
            
            m_CDInserted = true;
        }        

        public void WriteCommand(byte data)
        {
            if (m_CDRomState == CDState.RESET)
            {
                switch (data)
                {
                    case 0x81:
                        m_CDRomState = CDState.RESET;
                        m_DataReady = false;
                        m_DataTransferDone = false;
                        break;
                    default:
                        Console.WriteLine("Unknown command {0:x}", data);
                        break;
                }
            }
            else
            {
                // STUFF CD-ROM COMMAND IN
            }
        }

        public byte ReadAt(int address)
        {
            Console.WriteLine("CD-ROM ACCESS {0:X}", address);

            switch (address & 0x18CF)
            {
                case 0x18C1:
                    return 0xAA;
                case 0x18C2:
                    return 0x55;
                case 0x18C3:
                    return 0;
                case 0x18C5:
                    return 0xAA;
                case 0x18C6:
                    return 0x55;
                case 0x18C7:
                    return 0x03;
            }

            switch (address & 0xf)
            {
                case 0x00:  // CDC Status Register
                    // TODO                    
                    return 0x0;
                case 0x01:  // CDC Command / Status / Data
                    // TODO
                    break;
                case 0x02:  // CD Control register (IRQ)
                    return m_CDControlReg;
                case 0x03:  // BRAM LOCK / CD Status
                    m_BRAM.WriteProtect(true);
                    m_PCMChannel = !m_PCMChannel;

                    // TODO
                    return 0;
                case 0x04:  // CD Reset register
                    return m_CDResetRegister;
                case 0x05:  // PCM Data (lower half of current output)
                    if (m_PCMChannel)
                        return (byte)(m_OutRight & 0xFF);
                    else
                        return (byte)(m_OutLeft & 0xFF);
                case 0x06:  // PCM Data (Upper half of current output
                    if (m_PCMChannel)
                        return (byte)(m_OutRight >> 8);
                    else
                        return (byte)(m_OutLeft >> 8);                    
                case 0x07:  // Read Subchannel Data
                    // TODO
                    Console.WriteLine("UNHANDLED SUBCHANNEL READ");
                    return 0;                    
                case 0x08:  // CD Read
                    // TODO
                    break;
                case 0x0A:  // ADPCM Read
                    return m_ADPCM.ReadData();
                case 0x0B:  // ADPCM DMA Control Register
                    return m_ADPCM.ReadDMAControl();
                case 0x0C:  // ADPCM STATUS REGISTER
                    return m_ADPCM.ReadStatus();
                case 0x0D:  // ADPCM Address control register
                    return 0;
            }

            return 0xFF;
        }

        public void WriteAt(int address, byte data)
        {
            switch (address & 0xf)
            {
                case 0x00:  // CDC Status Register
                    m_CDRomState = CDState.RESET;
                    m_DataReady = false;
                    m_DataTransferDone = false;
                    return;
                case 0x01:  // CDC Command / Status / Data
                    WriteCommand(data);
                    return;
                case 0x02:  // CD CONTROL REGISTER (IRQ mask)
                    m_CDControlReg = data;
                    return;
                case 0x04:  // CD RESET
                    m_CDResetRegister = data;
                    if( (data & 0x02) != 0 )
                        m_CDRomState = CDState.RESET;
                    return;
                case 0x07:  // BRAM ENABLE
                    m_BRAM.WriteProtect((data & 0x80) == 0);
                    return;
                case 0x08:  // ADPCM LSB ADDRESS LATCH
                    m_ADPCM.SetAddressLo(data);
                    return;
                case 0x09:  // ADPCM MSB ADDRESS LATCH
                    m_ADPCM.SetAddressHi(data);
                    return;
                case 0x0A:  // ADPCM RAM ACCESS REGISTER
                    m_ADPCM.WriteData(data);
                    return;
                case 0x0B:  // ADPCM DMA CONTROL
                    m_ADPCM.WriteDMAControl(data);
                    return;
                case 0x0D:  // ADPCM ADDRESS CONTROL
                    m_ADPCM.AddressControl(data);
                    return;
                case 0x0E:  // ADPCM PLAYBACK RATE
                    m_ADPCM.SetPlayRate(data);
                    return;
                case 0x0F:  // ADPCM AND CD AUDIO FADER
                    switch (data & 0xF)
                    {
                        case 0x0:
                        case 0x1:
                            FadeAudio(false, 1 / (m_PlayFrequency * 2.0f));
                            m_ADPCM.FadeAudio(false, 1 / (m_PlayFrequency * 2.0f));
                            break;
                        case 0x8:
                        case 0x9:
                            FadeAudio(true, 1 / (m_PlayFrequency * 6.5f));
                            break;
                        case 0xA:
                            m_ADPCM.FadeAudio(true, 1 / (m_PlayFrequency * 6.5f));
                            break;
                        case 0xC:
                        case 0xD:
                            FadeAudio(true, 1 / (m_PlayFrequency * 2.0f));
                            break;
                        case 0xE:
                            m_ADPCM.FadeAudio(true, 1 / (m_PlayFrequency * 2.0f));
                            break;
                    }
                    return;

                // --- READ ONLY REGISTERS 

                case 0x03:  // CD STATUS REGISTER
                case 0x0C:  // ADPCM STATUS REGISTER
                    return;
            }
            Console.WriteLine("CD-ROM ACCESS {0:X} -> {1:X}", address, data);
        }
    }
}
