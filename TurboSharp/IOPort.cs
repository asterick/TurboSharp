using System;
using System.Text;
using Tao.Sdl;

namespace TurboSharp
{
    public class IOPort
    {
        private bool m_SEL;
        private bool m_CLR;

        private bool m_Up;
        private bool m_Down;
        private bool m_Left;
        private bool m_Right;
        private bool m_Button1;
        private bool m_Button2;
        private bool m_Run;
        private bool m_Select;

        public IOPort()
        {
            m_Up = false;
            m_Down = false;
            m_Left = false;
            m_Right = false;
            m_Button1 = false;
            m_Button2 = false;
            m_Run = false;
            m_Select = false;
        }

        public void KeyDown(int key)
        {
            switch (key)
            {
                case Sdl.SDLK_UP:
                    m_Up = true;
                    break;
                case Sdl.SDLK_DOWN:
                    m_Down = true;
                    break;
                case Sdl.SDLK_RIGHT:
                    m_Right = true;
                    break;
                case Sdl.SDLK_LEFT:
                    m_Left = true;
                    break;
                case Sdl.SDLK_x:
                    m_Button1 = true;
                    break;
                case Sdl.SDLK_z:
                    m_Button2 = true;
                    break;
                case Sdl.SDLK_RETURN:
                    m_Run = true;
                    break;
                case Sdl.SDLK_TAB:
                    m_Select = true;
                    break;
            }
        }

        public void KeyUp(int key)
        {
            switch (key)
            {
                case Sdl.SDLK_UP:
                    m_Up = false;
                    break;
                case Sdl.SDLK_DOWN:
                    m_Down = false;
                    break;
                case Sdl.SDLK_RIGHT:
                    m_Right = false;
                    break;
                case Sdl.SDLK_LEFT:
                    m_Left = false;
                    break;
                case Sdl.SDLK_x:
                    m_Button1 = false;
                    break;
                case Sdl.SDLK_z:
                    m_Button2 = false;
                    break;
                case Sdl.SDLK_RETURN:
                    m_Run = false;
                    break;
                case Sdl.SDLK_TAB:
                    m_Select = false;
                    break;
            }
        }

        public void Write(byte data)
        {
            m_CLR = (data & 2) != 0;
            m_SEL = (data & 1) != 0;
        }

        public byte Read()
        {
            if( m_CLR )
                return 0xB0;
            else if (m_SEL)
                return (byte)(
                    0xB0 |
                    (m_Left  ? 0 : 0x08) |
                    (m_Down  ? 0 : 0x04) |
                    (m_Right ? 0 : 0x02) |
                    (m_Up ? 0 : 0x01));
            else
                return (byte)(
                    0xB0 |
                    (m_Run     ? 0 : 0x08) |
                    (m_Select  ? 0 : 0x04) |
                    (m_Button2 ? 0 : 0x02) |
                    (m_Button1 ? 0 : 0x01));
        }
    }
}
