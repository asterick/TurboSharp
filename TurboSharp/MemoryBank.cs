using System;
using System.Text;

namespace TurboSharp
{
    public class MemoryBank
    {
        protected int m_MemoryPage;

        public void SetMemoryPage(int page)
        {
            m_MemoryPage = page;
        }

        virtual public byte ReadAt(int address)
        {
            Console.WriteLine("Unknown memory access at address {0:x}-{1:x}", m_MemoryPage, address);
            return 0xFF;
        }

        virtual public void WriteAt(int address, byte data)
        {
            Console.WriteLine("Unknown memory access at address {0:x}-{1:x} -> {2:x}", m_MemoryPage, address, data);
        }
    }
}
