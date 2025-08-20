namespace Hellscape.Domain {
    public enum TileTag : byte { Suburbs, Industrial, Downtown, Park, Blocked }

    public sealed class CityGrid {
        public readonly int Width, Height; 
        public readonly int CenterX, CenterY; 
        public readonly int CenterRadius;
        private readonly TileTag[,] tags;

        public CityGrid(int width, int height, int centerRadius) {
            Width = width; 
            Height = height; 
            CenterRadius = centerRadius;
            CenterX = width / 2; 
            CenterY = height / 2; 
            tags = new TileTag[width, height];
            Generate();
        }

        public TileTag GetTag(int x, int y) => tags[x, y];

        public int RadiusIndex(int x, int y) { 
            int dx = x - CenterX, dy = y - CenterY; 
            return System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)); 
        }

        private void Generate() {
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var r = RadiusIndex(x, y);
                    tags[x, y] = r < CenterRadius * 0.5f ? TileTag.Downtown : 
                                r < CenterRadius ? TileTag.Industrial : TileTag.Suburbs;
                }
            }
        }
    }
}