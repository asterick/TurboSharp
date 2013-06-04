using System;
using System.Text;
using System.IO;

namespace TurboSharp
{
    public class SaveMemoryBank : MemoryBank
    {
        private byte[] m_Ram;
        private bool m_WriteProtect;

        public SaveMemoryBank()
        {
            m_Ram = new byte[0x800];

            try
            {
                FileStream file = new FileStream("cabinet.brm", FileMode.Open, FileAccess.Read);
                file.Read(m_Ram, 0, m_Ram.Length);
                file.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine("No BRAM available to load: ", e.Message);
            }

            m_WriteProtect = true;
        }

        ~SaveMemoryBank()
        {
            FileStream file = new FileStream("cabinet.brm", FileMode.OpenOrCreate, FileAccess.Write);
            file.Write(m_Ram, 0, m_Ram.Length);
            file.Close();
        }

        public void WriteProtect(bool protect)
        {
            m_WriteProtect = protect;
        }

        public override void WriteAt(int address, byte data)
        {
            if (address < 0x800 && !m_WriteProtect)
                m_Ram[address] = data;
        }

        public override byte ReadAt(int address)
        {
            if (address < 0x800 && !m_WriteProtect)
                return m_Ram[address];
            
            return 0xFF;
        }
    }
}
