using System;
using System.Text;

namespace TurboSharp
{
    class ExtendedRomBank : MemoryBank
    {
        private byte[][] m_Rom;
        private static int m_Bank;

        public ExtendedRomBank(byte[][] page)
        {
            m_Rom = new byte[page.Length][];

            for (int i = 0; i < page.Length; i++)
            {
                m_Rom[i] = new byte[0x2000];
                page[i].CopyTo(m_Rom[i], 0);
            }

            m_Bank = 0;
        }

        public override byte ReadAt(int address)
        {
            return m_Rom[m_Bank][address];
        }

        public override void WriteAt(int address, byte data)
        {
            if ((address & 0x1FFC) == 0x1FF0)
                m_Bank = address & 3;            
        }
    }
}
