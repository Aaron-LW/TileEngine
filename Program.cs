using SDL3;
using Smash;
using Smash.Input;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (!SDL.Init(SDL.InitFlags.Video)) throw new Exception($"SDL coulnd't initialize: {SDL.GetError()}");
        if (!TTF.Init()) throw new Exception($"TTF couldn't be initialized: {SDL.GetError()}");

        SmashTest smashTest = new();
        DeltaTimeCounter deltaTimeCounter = new();

        smashTest.Start();

        bool running = true;
        while (running)
        {
            InputHandler.Update();
            deltaTimeCounter.Update();

            while (SDL.PollEvent(out SDL.Event e))
            {
                if (e.Type == (uint)SDL.EventType.Quit)
                {
                    running = false;
                }

                InputHandler.Event(e);
            }

            smashTest.Update(deltaTimeCounter.Seconds);
            smashTest.Render();
        }
    }
}