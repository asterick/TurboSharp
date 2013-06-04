using System;
using System.Text;
using System.Windows.Forms;
using Tao.Sdl;

namespace TurboSharp
{
    class Program
    {
        TurboGraphics tg;

        [STAThread]
        public static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }

        public Program()
        {
            tg = new TurboGraphics();
        }
        
        public void Run()
        {
            bool running = true;
            bool mute = false;

            Sdl.SDL_Event e;

            OpenFileDialog ofn = new OpenFileDialog();            
            ofn.Filter = "PCE Roms (*.pce)|*.pce";
            ofn.Title = "Open PCE Rom";

            if (ofn.ShowDialog() == DialogResult.Cancel)
                return;

            tg.LoadRom(ofn.FileName, false);             
            tg.Reset();

            while (running)
            {
                while (Sdl.SDL_PollEvent(out e) != 0)
                {
                    if (e.type == Sdl.SDL_QUIT)
                    {
                        running = false;
                    }
                    else if (e.type == Sdl.SDL_KEYUP)
                    {
                        tg.KeyUp(e.key.keysym.sym);
                    }
                    else if (e.type == Sdl.SDL_KEYDOWN)
                    {
                        switch (e.key.keysym.sym)
                        {
                            case Sdl.SDLK_F5:
                                tg.Mute(mute = !mute);
                                break;
                            case Sdl.SDLK_ESCAPE:
                                running = false;
                                break;
                            case Sdl.SDLK_F1:
                                tg.Mute(true);
                                ofn.Filter = "PC-Engine Roms (*.pce)|*.pce";
                                ofn.Title = "Open PCE Rom";
                                if (ofn.ShowDialog() != DialogResult.Cancel)
                                {
                                    tg.LoadRom(ofn.FileName, false);
                                    tg.Reset();
                                }
                                tg.Mute(mute);
                                break;
                            case Sdl.SDLK_F2:
                                tg.Mute(true);
                                ofn.Filter = "PC-Engine Bitswapped Roms (*.pce)|*.pce";
                                ofn.Title = "Open PCE Rom";
                                if (ofn.ShowDialog() != DialogResult.Cancel)
                                {
                                    tg.LoadRom(ofn.FileName, true);
                                    tg.Reset();
                                }
                                tg.Mute(mute);
                                break;
                            case Sdl.SDLK_F3:
                                tg.Mute(true);
                                ofn.Filter = "PC-Engine CD (*.cue)|*.cue";
                                ofn.Title = "Open PC Engine CD Image";
                                if (ofn.ShowDialog() != DialogResult.Cancel)
                                {
                                    tg.LoadCue(ofn.FileName);
                                }
                                tg.Mute(mute);
                                break;
                            case Sdl.SDLK_F12:
                                tg.Reset();
                                break;
                            default:
                                tg.KeyDown(e.key.keysym.sym);
                                break;
                        }
                    }

                }

                tg.Update();                
            }
        }

    }
}
