using System;
using System.Text;

namespace TurboSharp
{
    public class RomBank : MemoryBank
    {
        private byte[] m_Rom;

        public RomBank(byte[] page)
        {
            m_Rom = new byte[0x2000];
            page.CopyTo(m_Rom, 0);
        }

        public override byte ReadAt(int address)
        {
            return m_Rom[address];
        }

        public override void WriteAt(int address, byte data)
        {
            Console.WriteLine("Unknown rom access at address {0:x}-{1:x} -> {2:x}", m_MemoryPage, address, data);
        }
    }
}
