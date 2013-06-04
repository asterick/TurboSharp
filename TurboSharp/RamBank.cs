using System;
using System.Text;

namespace TurboSharp
{
    public class RamBank : MemoryBank
    {
        private byte[] m_Ram;

        public RamBank()
        {
            m_Ram = new byte[0x2000];
        }

        public override byte ReadAt(int address)
        {
            return m_Ram[address];
        }

        public override void WriteAt(int address, byte data)
        {
            m_Ram[address] = data;            
        }
    }
}
