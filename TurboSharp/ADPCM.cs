using System;
using System.Collections.Generic;
using System.Text;

namespace TurboSharp
{
    public enum ADPCMState : byte
    {
        ADPCM_STOPPED = 0,
        ADPCM_HALF_PLAYED = 0x4,
        ADPCM_FULL_PLAYED = 0x8,
        ADPCM_NORMAL = 0xC
    }

    class ADPCM
    {
        private ushort m_Address;
        private ADPCMState m_State;
        private byte m_DMA_Register;

        public byte ReadData()
        {
            Console.WriteLine("UNHANDLED ADPCM DATA READ");
            // TODO
            return 0;
        }

        public ADPCMState GetState()
        {
            return m_State;
        }

        public void WriteData(byte data)
        {
            Console.WriteLine("UNHANDLED ADPCM DATA WRITE {0:x}",data);
            // TODO
        }

        public void SetAddressLo(byte data)
        {
            m_Address = (ushort)((m_Address & 0xFF00) | data);
        }

        public void SetAddressHi(byte data)
        {
            m_Address = (ushort)((m_Address & 0x00FF) | (data << 8));
        }

        public void SetPlayRate(byte data)
        {
            Console.WriteLine("UNHANDLED ADPCM PLAYBACK RATE SET {0:x}", data);
            // TODO
        }

        public void AddressControl(byte data)
        {
            Console.WriteLine("UNHANDLED ADPCM ADDRESS CONTROL {0:x}", data);
            // TODO
        }

        public void FadeAudio(bool down, float step)
        {
            Console.WriteLine("UNHANDLED ADPCM AUDIO FADE");
            // TODO
        }

        public byte ReadDMAControl()
        {
            return (byte)(m_DMA_Register & 0xFE);
        }

        public void WriteDMAControl( byte data )
        {
            Console.WriteLine("UNHANDLED ADPCM DMA START");
            m_DMA_Register = data;
        }

        public byte ReadStatus()
        {
            Console.WriteLine("UNHANDLED ADPCM READ STATUS");
            return 0;
        }
    }
}
