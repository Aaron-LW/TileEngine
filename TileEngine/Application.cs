using System.Drawing;
using System.Numerics;
using SDL3;
using Smash;
using Smash.Graphics;
using Smash.Input;

public class SmashTest : Application
{
    private Window _window;
    private Renderer _renderer;
    private Font _font; 

    private float _elapsedTime = 0;
    private double _fps;

    private const int _tileWidht = 4;
    private const int _tileHeight = 4;

    private float _cameraX;
    private float _cameraY;

    private const int _cameraSpeed = 1000;

    private const int _mapWidth = 2000;
    private const int _mapHeight = 2000;

    private Tile[] _tiles = new Tile[_mapWidth * _mapHeight];

    private const int MAX_TILES = (1920 / _tileWidht + 2) * (1080 / _tileHeight + 2);

    private SDL.Vertex[] _vertices = new SDL.Vertex[MAX_TILES * 4];
    private int[] _indices = new int[MAX_TILES * 6];

    private const float _minBrightness = 0.2f;

    private List<Dynamite> _dynamites = new();
    private List<Explosion> _explosions = new();

    private Texture2D _textureAtlas;
    private Texture2D _dynamiteTexture;

    public SmashTest()
    {
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
        _dynamiteTexture = AssetManager.LoadTexture("Dynamite.png", _renderer);

        _font = AssetManager.LoadFont("JetBrainsMono-Bold.ttf", 18);

        _renderer.SetRenderBlendMode(BlendMode.None);
        _renderer.SetVSyncEnabled(false);
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

                _tiles[y * _mapWidth + x] = new Tile(x * _tileWidht, y * _tileHeight, AssetManager.Get(textureName), _minBrightness);
            }
        }

        for (int index = 0; index < _tiles.Length; index++)
        {
            if (index - _mapWidth < 0)
            {
                _tiles[index].Brightness = 1f;

                int baseDepth = 16;
                for (int depth = baseDepth; depth > 0; depth--)
                {
                    _tiles[index + ((baseDepth + 1 - depth) * _mapWidth)].Brightness = Math.Max(1f - 0.065f * (baseDepth + 1 - depth), _minBrightness);
                }
            }
        }

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

        float cameraSpeedModifier = InputHandler.IsKeyDown(SDL.Keycode.LShift) ? 1500 : 0;
        if (InputHandler.IsKeyDown(SDL.Keycode.D))
        {
            _cameraX += (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.A))
        {
            _cameraX -= (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.S))
        {
            _cameraY += (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.W))
        {
            _cameraY -= (_cameraSpeed + cameraSpeedModifier) * (float)deltaTime;
        }

        if (InputHandler.IsRightMousePressed())
        {
            float tileX = InputHandler.MouseX + _cameraX;
            float tileY = InputHandler.MouseY + _cameraY;

            //_tiles[(int)tileY * _mapWidth + (int)tileX] = null!;
            _dynamites.Add(new Dynamite(tileX, tileY));
        }

        for (int i = 0; i < _dynamites.Count; i++)
        {
            if (_dynamites[i].DetonationTime > 0)
            {
                _dynamites[i].DetonationTime -= (float)deltaTime;
            }
            else
            {
                Detonate(_dynamites[i]);
            }
        }

        for (int i = 0; i < _explosions.Count; i++)
        {
            if (_explosions[i].CurrentRadius < _explosions[i].MaxRadius)
            {
                float oldRadius = _explosions[i].CurrentRadius;
                _explosions[i].CurrentRadius += _explosions[i].Speed / (_explosions[i].CurrentRadius / 35 + 1) * (float)deltaTime;

                List<Vector2> positions = GetPositionsInRing(oldRadius, _explosions[i].CurrentRadius, _explosions[i].CenterX, _explosions[i].CenterY);
                DestroyTilesInPositions(positions);

                List<Vector2> nextPositions = GetPositionsInRing(_explosions[i].CurrentRadius, _explosions[i].CurrentRadius + _explosions[i].Speed / (_explosions[i].CurrentRadius / 35 + 1) * 4 * (float)deltaTime, _explosions[i].CenterX, _explosions[i].CenterY);
                foreach (Vector2 position in nextPositions)
                {
                    float tileX = MathF.Floor(position.X / _tileWidht);
                    float tileY = MathF.Floor(position.Y / _tileHeight);

                    if (tileX < 0 || tileX >= _mapWidth || tileY < 0 || tileY >= _mapHeight) continue;

                    Tile? tile = _tiles[(int)tileY * _mapWidth + (int)tileX];
                    if (tile != null) tile.RedModifier = 600;
                }
            }
            else
            {
                _explosions.RemoveAt(i);
            }
        }
    }

    public override void Render()
    {
        _renderer.Clear(Color.CornflowerBlue);

        int startTileX = (int)_cameraX / _tileWidht;
        int startTileY = (int)_cameraY / _tileHeight;

        int tilesX = 1920 / _tileWidht + 1;
        int tilesY = 1080 / _tileHeight + 1;

        int drawnTiles = 0;

        for (int y = startTileY; y < startTileY + tilesY; y++)
        {
            for (int x = startTileX; x < startTileX + tilesX; x++)
            {
                if (x >= _mapWidth || y >= _mapHeight || x < 0 || y < 0) continue;
                
                //_renderer.RenderTexture(tile.Texture, tile.X - (int)_cameraX, tile.Y - (int)_cameraY, color, scale);
                //RenderRect(tile.X - (int)_cameraX, tile.Y - (int)_cameraY, _tileWidht, _tileHeight);

                Tile? tile = _tiles[y * _mapWidth + x];
                if (tile == null) continue;

                int baseIndex = drawnTiles * 4;

                float xPos = tile.X - _cameraX;
                float yPos = tile.Y - _cameraY;

                //Positions
                _vertices[baseIndex + 0].Position.X = xPos;
                _vertices[baseIndex + 0].Position.Y = yPos;

                _vertices[baseIndex + 1].Position.X = xPos + _tileWidht;
                _vertices[baseIndex + 1].Position.Y = yPos;

                _vertices[baseIndex + 2].Position.X = xPos + _tileWidht;
                _vertices[baseIndex + 2].Position.Y = yPos + _tileHeight;

                _vertices[baseIndex + 3].Position.X = xPos;
                _vertices[baseIndex + 3].Position.Y = yPos + _tileHeight;

                SDL.FColor topVertexColor = new SDL.FColor { R = tile.Brightness + tile.RedModifier, G = tile.Brightness, B = tile.Brightness, A = 1.0f };
                SDL.FColor bottomVertexColor = new SDL.FColor { R = tile.Brightness - 0.065f + tile.RedModifier - 0.065f, G = tile.Brightness - 0.065f, B = tile.Brightness , A = 1.0f};

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

        foreach (Dynamite dynamite in _dynamites)
        {
            float scale = (float)Math.Max(Math.Abs(Math.Sin(dynamite.DetonationTime * 15)), 0.4f);
            _renderer.RenderTexture(_dynamiteTexture, dynamite.X - _cameraX - (scale * 8), dynamite.Y - _cameraY - (scale * 2.5f), Color.White, scale);
        }

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

    private void RenderRect(int x, int y, int widht, int height)
    {
        //_vertices[_vertexIndex + 0] = new SDL.Vertex { Position = new SDL.FPoint { X = x, Y = y},                  Color = _topVertexColor, TexCoord = new SDL.FPoint { X = 0, Y = 0 } }; // Top left
        //_vertices[_vertexIndex + 1] = new SDL.Vertex { Position = new SDL.FPoint { X = x + widht, Y = y},          Color = _topVertexColor, TexCoord = new SDL.FPoint { X = 1, Y = 0 } }; // Top right
        //_vertices[_vertexIndex + 2] = new SDL.Vertex { Position = new SDL.FPoint { X = x + widht, Y = y + height}, Color = _topVertexColor, TexCoord = new SDL.FPoint { X = 1, Y = 1 } }; // Bottom right
        //_vertices[_vertexIndex + 3] = new SDL.Vertex { Position = new SDL.FPoint { X = x, Y = y + height},         Color = _topVertexColor, TexCoord = new SDL.FPoint { X = 0, Y = 1 } }; // Bottom left

        //_vertexIndex += 4;
    }

    private List<Vector2> GetPositionsInCircle(int centerX, int centerY, int radius, int step)
    {
        List<Vector2> positions = new();
        int rSquared = radius * radius;

        for (int y = centerY - radius; y <= centerY + radius; y += step)
        {
            for (int x = centerX - radius; x <= centerX + radius; x += step)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy <= rSquared)
                {
                    positions.Add(new Vector2(x, y));
                }
            }
        }

        return positions;
    }

    private void Detonate(Dynamite dynamite)
    {
        _explosions.Add(new Explosion(dynamite.X, dynamite.Y, 300, 500));
        _dynamites.Remove(dynamite);
    }

    private void DestroyTilesInPositions(List<Vector2> positions)
    {
        foreach (Vector2 position in positions)
        {
            float tileX = MathF.Floor(position.X / _tileWidht);
            float tileY = MathF.Floor(position.Y / _tileHeight);

            if (tileX < 0 || tileY < 0 || tileX >= _mapWidth || tileY >= _mapHeight) continue;
            _tiles[(int)tileY * _mapWidth + (int)tileX] = null!;
        }
    }

    private List<Vector2> GetPositionsInRing(float innerRadius, float outerRadius, float centerX, float centerY)
    {
        List<Vector2> positions = new();
        int outer = (int)MathF.Ceiling(outerRadius);
        int inner = (int)MathF.Floor(innerRadius);

        int outerSquared = outer * outer;
        int innerSquared = inner * inner;

        for (int dy = -outer; dy <= outer; dy++)
        {
            for (int dx = -outer; dx <= outer; dx++)
            {
                int distSquared = dx * dx + dy * dy;

                if (distSquared <= outerSquared && distSquared > innerSquared)
                {
                    int x = (int)centerX + dx;
                    int y = (int)centerY + dy;

                    positions.Add(new Vector2(x, y));
                }
            }
        }

        return positions;
    }
}