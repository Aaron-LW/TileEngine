using System.Drawing;
using System.Numerics;
using SDL3;
using Smash;
using Smash.Graphics;
using Smash.Input;

public class TileEngine : Application
{
    private Window _window;
    private Renderer _renderer;
    private Font _font; 

    private float _elapsedTime = 0;
    private double _fps;

    private readonly int _tileWidht;
    private readonly int _tileHeight;

    private float _cameraX;
    private float _cameraY;
    private Vector2 _cameraPosition => new Vector2(_cameraX, _cameraY);

    private const int _cameraSpeed = 1000;

    private readonly int _mapWidth;
    private readonly int _mapHeight;

    private Tile[] _tiles = [];

    private int MAX_TILES => (((int)_window.Width / (int)(_tileWidht * _zoom)) + 2) * (((int)_window.Height / (int)(_tileHeight * _zoom)) + 2);

    private SDL.Vertex[] _vertices = [];
    private int[] _indices = [];

    private Texture2D _textureAtlas;

    private Vector2 _lastWindowBounds;

    private float _zoom = 1;

    public TileEngine(int tileWidth, int tileHeight, int mapWidth, int mapHeight)
    {
        _tileWidht = tileWidth;
        _tileHeight = tileHeight;

        _mapWidth = mapWidth;
        _mapHeight = mapHeight;

        CreateWindowAndRenderer("Smash test test test test test test", 800, 600, out _window, out _renderer);
        SDL.SetWindowResizable(_window.Handle, true);

        AssetManager.SetAssetRootDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
        AssetManager.SetDefaultBlendMode(BlendMode.Blend);
        AssetManager.SetDefaultScaleMode(ScaleMode.Nearest);

        _textureAtlas = AssetManager.LoadTexture("TextureAtlas.png", _renderer);

        AssetManager.AddTextureRegion("DirtAtlas", new TextureRegion("TextureAtlas", 0, 0, 16, 16));
        AssetManager.AddTextureRegion("GrassAtlas", new TextureRegion("TextureAtlas", 0, 16, 16, 16));
        AssetManager.AddTextureRegion("MissingAtlas", new TextureRegion("TextureAtlas", 16, 0, 16, 16));
        AssetManager.AddTextureRegion("StoneAtlas", new TextureRegion("TextureAtlas", 16, 16, 16, 16));

        AssetManager.LoadTexture("Dirt.png", _renderer);
        AssetManager.LoadTexture("Grass.png", _renderer);

        _font = AssetManager.LoadFont("JetBrainsMono-Bold.ttf", 18);

        _renderer.SetRenderBlendMode(BlendMode.None);
        _renderer.SetVSyncEnabled(false);

        _lastWindowBounds = _window.Bounds;

        RecalculateArrayBounds();
        _tiles = new Tile[_mapWidth * _mapHeight];
    }

    public override void Start()
    {
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                string textureName = "Katzi";
                if (y == 0) textureName = "GrassAtlas";
                if (y > 0 && y <= 5) textureName = "DirtAtlas";
                if (y > 5) textureName = "StoneAtlas";

                _tiles[y * _mapWidth + x] = new Tile(x * _tileWidht, y * _tileHeight, AssetManager.Get(textureName), 0.5f);
            }
        }

        base.Start();
    }

    public override void Update(double deltaTime)
    {
        _elapsedTime += (float)deltaTime;

        if (_elapsedTime > 0.7f)
        {
            _fps = 1 / deltaTime;
            _elapsedTime = 0;
        }

        if (_lastWindowBounds != _window.Bounds)
        {
            RecalculateArrayBounds();
            _lastWindowBounds = _window.Bounds;
        }

        float cameraSpeedModifier = InputHandler.IsKeyDown(SDL.Keycode.LShift) ? 1500 : 0;
        if (InputHandler.IsKeyDown(SDL.Keycode.D)) _cameraX += (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        if (InputHandler.IsKeyDown(SDL.Keycode.A)) _cameraX -= (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        if (InputHandler.IsKeyDown(SDL.Keycode.S)) _cameraY += (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        if (InputHandler.IsKeyDown(SDL.Keycode.W)) _cameraY -= (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;

        if (InputHandler.ScrollWheelDelta != 0)
        {
            Vector2 beforeZoomMouseWorldPos = (InputHandler.MousePosition / _zoom) + _cameraPosition;

            _zoom += InputHandler.ScrollWheelDelta * 0.1f;
            _zoom = Math.Max(_zoom, 0.1f);

            Vector2 afterZoomMouseWorldPos = (InputHandler.MousePosition / _zoom) + _cameraPosition;

            _cameraX += beforeZoomMouseWorldPos.X - afterZoomMouseWorldPos.X;
            _cameraY += beforeZoomMouseWorldPos.Y - afterZoomMouseWorldPos.Y;

            RecalculateArrayBounds();
        }

        if (InputHandler.IsRightMouseDown())
        {
            float tileX = ((InputHandler.MouseX / _zoom) + _cameraX) / _tileWidht;
            float tileY = ((InputHandler.MouseY / _zoom) + _cameraY) / _tileHeight;

            if (tileX < 0 || tileY < 0 || tileX > _mapWidth || tileY > _mapHeight) { }
            else _tiles[(int)tileY * _mapWidth + (int)tileX] = null!;
        }
    }

    public override void Render()
    {
        _renderer.Clear(Color.CornflowerBlue);

        int startTileX = (int)_cameraX / _tileWidht;
        int startTileY = (int)_cameraY / _tileHeight;

        int tilesX = ((int)_window.Width / (int)(_tileWidht * _zoom)) + 2;
        int tilesY = ((int)_window.Height / (int)(_tileHeight * _zoom)) + 2;

        int drawnTiles = 0;

        for (int y = startTileY; y < startTileY + tilesY; y++)
        {
            for (int x = startTileX; x < startTileX + tilesX; x++)
            {
                if (x >= _mapWidth || y >= _mapHeight || x < 0 || y < 0) continue;
                
                Tile? tile = _tiles[y * _mapWidth + x];
                if (tile == null) continue;

                int baseIndex = drawnTiles * 4;

                float xPos = (tile.X - _cameraX) * _zoom;
                float yPos = (tile.Y - _cameraY) * _zoom;

                //Positions
                _vertices[baseIndex + 0].Position.X = xPos;
                _vertices[baseIndex + 0].Position.Y = yPos;

                _vertices[baseIndex + 1].Position.X = xPos + _tileWidht * _zoom;
                _vertices[baseIndex + 1].Position.Y = yPos;

                _vertices[baseIndex + 2].Position.X = xPos + _tileWidht * _zoom;
                _vertices[baseIndex + 2].Position.Y = yPos + _tileHeight * _zoom;

                _vertices[baseIndex + 3].Position.X = xPos;
                _vertices[baseIndex + 3].Position.Y = yPos + _tileHeight * _zoom;

                SDL.FColor topVertexColor = new SDL.FColor { R = tile.Brightness + tile.RedModifier, G = tile.Brightness, B = tile.Brightness, A = 1.0f };
                SDL.FColor bottomVertexColor = new SDL.FColor { R = tile.Brightness + tile.RedModifier, G = tile.Brightness, B = tile.Brightness , A = 1.0f};

                //Color
                _vertices[baseIndex + 0].Color = topVertexColor;
                _vertices[baseIndex + 1].Color = topVertexColor;
                _vertices[baseIndex + 2].Color = bottomVertexColor;
                _vertices[baseIndex + 3].Color = bottomVertexColor;

                //TexCoords
                _vertices[baseIndex + 0].TexCoord = new SDL.FPoint { X = tile.TexX, Y = tile.TexY};
                _vertices[baseIndex + 1].TexCoord = new SDL.FPoint { X = tile.TexX + 0.5f, Y = tile.TexY};
                _vertices[baseIndex + 2].TexCoord = new SDL.FPoint { X = tile.TexX + 0.5f, Y = tile.TexY + 0.5f};
                _vertices[baseIndex + 3].TexCoord = new SDL.FPoint { X = tile.TexX, Y = tile.TexY + 0.5f};

                drawnTiles++;
           }
        }

        RenderGeometry(_textureAtlas.Handle, drawnTiles);

        _renderer.RenderText(_font, $"Fps: {Math.Round(_fps)}", new Vector2(30), Color.White);
        _renderer.RenderText(_font, "1", new Vector2(30, 60), Color.White);

        _renderer.RenderPresent();
    }

    public override void End()
    {
        _renderer.Destroy();
        _window.Destroy();
        AssetManager.DestroyAllTextures();

        base.End();
    }

    private void RenderGeometry(nint textureHandle, int length)
    {
        SDL.RenderGeometry(_renderer.Handle, textureHandle, _vertices, length * 4, _indices, length * 6);
    }

    private void RecalculateArrayBounds()
    {
        _vertices = new SDL.Vertex[MAX_TILES * 4];
        _indices = new int[MAX_TILES * 6];

        for (int i = 0; i < MAX_TILES; i++)
        {
            int baseIndex = i * 4;
            _indices[i * 6 + 0] = baseIndex + 0;
            _indices[i * 6 + 1] = baseIndex + 1;
            _indices[i * 6 + 2] = baseIndex + 2;
            _indices[i * 6 + 3] = baseIndex + 0;
            _indices[i * 6 + 4] = baseIndex + 2;
            _indices[i * 6 + 5] = baseIndex + 3;
        }
    }
}