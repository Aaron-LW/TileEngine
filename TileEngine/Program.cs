using SDL3;
using Smash;
using Smash.Input;

internal static class Program
{
    private static void Main(string[] args)
    {
        SmashEngine.Init();

        TileEngine tileEngine = new TileEngine(4, 4, 2000, 2000);
        tileEngine.Start();

        while (!tileEngine.ApplicationShouldClose())
        {
            SmashEngine.Update();

            tileEngine.Update(SmashEngine.DeltaTime);
            tileEngine.Render();
        }

        tileEngine.End();
        SmashEngine.Stop();
    }
}