call "c:\Program Files\Mono-1.2.2\bin\gmcs" ..\TurboSharp\*.cs -unsafe+ -r:System.Windows.Forms.dll -r:Tao.Sdl.dll -out:TurboSharp.exe
call "c:\Program Files\Mono-1.2.2\bin\mono" TurboSharp.exe