using _2DREPOCODE.Enums;
using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles procedural map generation, tile management, and pathfinding.
    /// Creates the facility layout where players scavenge for items.
    /// Responsibility: Axel
    /// </summary>
    public class MapHandler
    {
        // === Map Dimensions ===
        /// <summary>
        /// Width of the map in tiles.
        /// </summary>
        public int MapWidth { get; private set; }

        /// <summary>
        /// Height of the map in tiles.
        /// </summary>
        public int MapHeight { get; private set; }

        // === Map Storage ===
        /// <summary>
        /// 2D array storing all tiles. [x, y] format.
        /// </summary>
        private MapTile[,] tiles;

        // === Special Tile Tracking ===
        private List<(int x, int y)> playerSpawnPoints;
        private List<(int x, int y)> shelfLocations;
        private List<(int x, int y)> ventLocations;
        private (int x, int y) extractionPoint;

        // === Random Generation ===
        private Random random;
        private int seed;

        /// <summary>
        /// Initializes the MapHandler with default size.
        /// </summary>
        public MapHandler()
        {
            MapWidth = 50;
            MapHeight = 50;
            tiles = new MapTile[MapWidth, MapHeight];
            playerSpawnPoints = new List<(int x, int y)>();
            shelfLocations = new List<(int x, int y)>();
            ventLocations = new List<(int x, int y)>();
            extractionPoint = (0, 0);
            seed = Environment.TickCount;
            random = new Random(seed);
        }

        /// <summary>
        /// Initializes the MapHandler with custom dimensions and seed.
        /// </summary>
        public MapHandler(int width, int height, int? seed = null)
        {
            MapWidth = width;
            MapHeight = height;
            tiles = new MapTile[MapWidth, MapHeight];
            playerSpawnPoints = new List<(int x, int y)>();
            shelfLocations = new List<(int x, int y)>();
            ventLocations = new List<(int x, int y)>();
            extractionPoint = (0, 0);
            this.seed = seed ?? Environment.TickCount;
            random = new Random(this.seed);

            Console.WriteLine($"MapHandler initialized: {MapWidth}x{MapHeight}, Seed: {this.seed}");
        }

        // === Map Generation ===

        /// <summary>
        /// Generates a procedural dungeon map using a simple room-and-corridor algorithm.
        /// This is a basic implementation - can be expanded with more complex algorithms.
        /// </summary>
        public void GenerateMap()
        {
            Console.WriteLine("Generating procedural facility map...");

            // Clear existing data
            playerSpawnPoints.Clear();
            shelfLocations.Clear();
            ventLocations.Clear();

            // Step 1: Fill with walls
            InitializeAllWalls();

            // Step 2: Generate rooms
            int roomCount = random.Next(8, 15);
            List<Room> rooms = GenerateRooms(roomCount);

            // Step 3: Carve out rooms
            foreach (Room room in rooms)
            {
                CarveRoom(room);
            }

            // Step 4: Connect rooms with corridors
            ConnectRooms(rooms);

            // Step 5: Place special tiles
            PlaceExtractionPoint(rooms[0]); // First room = extraction zone
            PlacePlayerSpawns(rooms[0], 4); // Up to 4 player spawns
            PlaceShelves(rooms);
            PlaceVents(rooms);
            PlaceObstacles(rooms);
            PlaceDarkZones(rooms);

            Console.WriteLine($"Map generated! Rooms: {rooms.Count}, Shelves: {shelfLocations.Count}, Vents: {ventLocations.Count}");
        }

        /// <summary>
        /// Fills the entire map with wall tiles.
        /// </summary>
        private void InitializeAllWalls()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    tiles[x, y] = new MapTile(x, y, TileType.Wall);
                }
            }
        }

        /// <summary>
        /// Generates a list of non-overlapping rooms.
        /// </summary>
        private List<Room> GenerateRooms(int roomCount)
        {
            List<Room> rooms = new List<Room>();

            for (int i = 0; i < roomCount; i++)
            {
                // Random room size
                int roomWidth = random.Next(6, 12);
                int roomHeight = random.Next(6, 12);

                // Random position
                int roomX = random.Next(1, MapWidth - roomWidth - 1);
                int roomY = random.Next(1, MapHeight - roomHeight - 1);

                Room newRoom = new Room(roomX, roomY, roomWidth, roomHeight);

                // Check for overlap
                bool overlaps = false;
                foreach (Room existingRoom in rooms)
                {
                    if (newRoom.Intersects(existingRoom))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    rooms.Add(newRoom);
                }
            }

            return rooms;
        }

        /// <summary>
        /// Carves out a room by setting tiles to floor.
        /// </summary>
        private void CarveRoom(Room room)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    if (IsInBounds(x, y))
                    {
                        tiles[x, y] = new MapTile(x, y, TileType.Floor);
                    }
                }
            }
        }

        /// <summary>
        /// Connects all rooms with corridors.
        /// </summary>
        private void ConnectRooms(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                Room roomA = rooms[i];
                Room roomB = rooms[i + 1];

                // Get center points
                (int x1, int y1) = roomA.GetCenter();
                (int x2, int y2) = roomB.GetCenter();

                // Carve L-shaped corridor
                if (random.Next(2) == 0)
                {
                    // Horizontal then vertical
                    CarveCorridor(x1, y1, x2, y1);
                    CarveCorridor(x2, y1, x2, y2);
                }
                else
                {
                    // Vertical then horizontal
                    CarveCorridor(x1, y1, x1, y2);
                    CarveCorridor(x1, y2, x2, y2);
                }
            }
        }

        /// <summary>
        /// Carves a straight corridor between two points.
        /// </summary>
        private void CarveCorridor(int x1, int y1, int x2, int y2)
        {
            int x = x1;
            int y = y1;

            while (x != x2 || y != y2)
            {
                if (IsInBounds(x, y))
                {
                    tiles[x, y] = new MapTile(x, y, TileType.Floor);
                }

                if (x < x2) x++;
                else if (x > x2) x--;

                if (y < y2) y++;
                else if (y > y2) y--;
            }
        }

        // === Special Tile Placement ===

        /// <summary>
        /// Places the extraction point in a room.
        /// </summary>
        private void PlaceExtractionPoint(Room room)
        {
            (int x, int y) = room.GetCenter();
            tiles[x, y] = new MapTile(x, y, TileType.ExtractionPoint);
            extractionPoint = (x, y);
        }

        /// <summary>
        /// Places player spawn points in a room.
        /// </summary>
        private void PlacePlayerSpawns(Room room, int maxSpawns)
        {
            int spawnsPlaced = 0;
            int attempts = 0;

            while (spawnsPlaced < maxSpawns && attempts < 50)
            {
                int x = random.Next(room.X + 1, room.X + room.Width - 1);
                int y = random.Next(room.Y + 1, room.Y + room.Height - 1);

                if (tiles[x, y].Type == TileType.Floor)
                {
                    tiles[x, y] = new MapTile(x, y, TileType.PlayerSpawn);
                    playerSpawnPoints.Add((x, y));
                    spawnsPlaced++;
                }

                attempts++;
            }
        }

        /// <summary>
        /// Places shelf tiles throughout rooms where items spawn.
        /// </summary>
        private void PlaceShelves(List<Room> rooms)
        {
            foreach (Room room in rooms)
            {
                // Place 2-5 shelves per room
                int shelfCount = random.Next(2, 6);
                int placed = 0;
                int attempts = 0;

                while (placed < shelfCount && attempts < 30)
                {
                    int x = random.Next(room.X + 1, room.X + room.Width - 1);
                    int y = random.Next(room.Y + 1, room.Y + room.Height - 1);

                    if (tiles[x, y].Type == TileType.Floor)
                    {
                        tiles[x, y] = new MapTile(x, y, TileType.Shelf);
                        shelfLocations.Add((x, y));
                        placed++;
                    }

                    attempts++;
                }
            }
        }

        /// <summary>
        /// Places vent tiles (shortcuts for shrunk players).
        /// </summary>
        private void PlaceVents(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                // Random chance to create a vent shortcut between rooms
                if (random.Next(3) == 0)
                {
                    Room roomA = rooms[i];
                    Room roomB = rooms[i + 1];

                    // Place vents at room edges
                    int x1 = random.Next(roomA.X, roomA.X + roomA.Width);
                    int y1 = random.Next(roomA.Y, roomA.Y + roomA.Height);

                    if (tiles[x1, y1].Type == TileType.Wall)
                    {
                        tiles[x1, y1] = new MapTile(x1, y1, TileType.Vent);
                        ventLocations.Add((x1, y1));
                    }
                }
            }
        }

        /// <summary>
        /// Places obstacle tiles (for hiding when shrunk).
        /// </summary>
        private void PlaceObstacles(List<Room> rooms)
        {
            foreach (Room room in rooms)
            {
                int obstacleCount = random.Next(1, 4);
                int placed = 0;
                int attempts = 0;

                while (placed < obstacleCount && attempts < 20)
                {
                    int x = random.Next(room.X + 1, room.X + room.Width - 1);
                    int y = random.Next(room.Y + 1, room.Y + room.Height - 1);

                    if (tiles[x, y].Type == TileType.Floor)
                    {
                        tiles[x, y] = new MapTile(x, y, TileType.Obstacle);
                        placed++;
                    }

                    attempts++;
                }
            }
        }

        /// <summary>
        /// Designates some rooms as dark zones (require flashlight).
        /// </summary>
        private void PlaceDarkZones(List<Room> rooms)
        {
            foreach (Room room in rooms)
            {
                // 30% chance room is dark
                if (random.Next(10) < 3)
                {
                    for (int x = room.X; x < room.X + room.Width; x++)
                    {
                        for (int y = room.Y; y < room.Y + room.Height; y++)
                        {
                            if (tiles[x, y].Type == TileType.Floor)
                            {
                                tiles[x, y].SetLightLevel(0.0f); // Complete darkness
                            }
                        }
                    }
                }
            }
        }

        // === Tile Access ===

        /// <summary>
        /// Gets the tile at the specified position.
        /// Returns null if out of bounds.
        /// </summary>
        public MapTile? GetTile(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return null;
            }
            return tiles[x, y];
        }

        /// <summary>
        /// Sets a tile at the specified position.
        /// </summary>
        public void SetTile(int x, int y, MapTile tile)
        {
            if (IsInBounds(x, y))
            {
                tiles[x, y] = tile;
            }
        }

        /// <summary>
        /// Checks if coordinates are within map bounds.
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
        }

        // === Special Locations ===

        /// <summary>
        /// Gets all player spawn points.
        /// </summary>
        public List<(int x, int y)> GetPlayerSpawnPoints()
        {
            return new List<(int x, int y)>(playerSpawnPoints);
        }

        /// <summary>
        /// Gets all shelf locations (where items spawn).
        /// </summary>
        public List<(int x, int y)> GetShelfLocations()
        {
            return new List<(int x, int y)>(shelfLocations);
        }

        /// <summary>
        /// Gets the extraction point location.
        /// </summary>
        public (int x, int y) GetExtractionPoint()
        {
            return extractionPoint;
        }

        /// <summary>
        /// Gets all vent locations.
        /// </summary>
        public List<(int x, int y)> GetVentLocations()
        {
            return new List<(int x, int y)>(ventLocations);
        }

        // === Pathfinding Helpers ===

        /// <summary>
        /// Gets all walkable neighbors of a tile.
        /// </summary>
        public List<(int x, int y)> GetWalkableNeighbors(int x, int y, bool allowVentsIfShrunk = false)
        {
            List<(int x, int y)> neighbors = new List<(int x, int y)>();

            // Check all 4 directions
            (int x, int y)[] directions = { (x, y - 1), (x + 1, y), (x, y + 1), (x - 1, y) };

            foreach ((int nx, int ny) in directions)
            {
                MapTile? tile = GetTile(nx, ny);
                if (tile != null && tile.IsWalkable)
                {
                    // Special case: vents require shrinking
                    if (tile.Type == TileType.Vent && !allowVentsIfShrunk)
                    {
                        continue;
                    }

                    neighbors.Add((nx, ny));
                }
            }

            return neighbors;
        }

        // === Map Debugging ===

        /// <summary>
        /// Prints an ASCII representation of the map to console.
        /// Useful for debugging procedural generation.
        /// </summary>
        public void PrintMap(int startX = 0, int startY = 0, int width = 50, int height = 30)
        {
            Console.WriteLine($"=== MAP (Seed: {seed}) ===");

            for (int y = startY; y < Math.Min(startY + height, MapHeight); y++)
            {
                StringBuilder line = new StringBuilder();
                for (int x = startX; x < Math.Min(startX + width, MapWidth); x++)
                {
                    MapTile tile = tiles[x, y];
                    line.Append(GetTileChar(tile.Type));
                }
                Console.WriteLine(line.ToString());
            }
        }

        /// <summary>
        /// Converts a TileType to an ASCII character for visualization.
        /// </summary>
        private char GetTileChar(TileType type)
        {
            return type switch
            {
                TileType.Floor => '.',
                TileType.Wall => '#',
                TileType.Door => 'D',
                TileType.ExtractionPoint => 'E',
                TileType.PlayerSpawn => 'S',
                TileType.Shelf => '$',
                TileType.Vent => 'V',
                TileType.Obstacle => 'O',
                TileType.Hazard => 'X',
                _ => '?'
            };
        }

        /// <summary>
        /// Gets map statistics for debugging.
        /// </summary>
        public string GetMapStats()
        {
            int floorCount = 0;
            int wallCount = 0;

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (tiles[x, y].Type == TileType.Floor) floorCount++;
                    else if (tiles[x, y].Type == TileType.Wall) wallCount++;
                }
            }

            return $"Map Stats:\n" +
                   $"  Size: {MapWidth}x{MapHeight}\n" +
                   $"  Seed: {seed}\n" +
                   $"  Floor Tiles: {floorCount}\n" +
                   $"  Wall Tiles: {wallCount}\n" +
                   $"  Shelves: {shelfLocations.Count}\n" +
                   $"  Vents: {ventLocations.Count}\n" +
                   $"  Player Spawns: {playerSpawnPoints.Count}";
        }
    }

    // === Helper Class: Room ===
    /// <summary>
    /// Represents a rectangular room in the procedural dungeon.
    /// </summary>
    internal class Room
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Room(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the center point of the room.
        /// </summary>
        public (int x, int y) GetCenter()
        {
            return (X + Width / 2, Y + Height / 2);
        }

        /// <summary>
        /// Checks if this room intersects with another room.
        /// </summary>
        public bool Intersects(Room other)
        {
            return X < other.X + other.Width &&
                   X + Width > other.X &&
                   Y < other.Y + other.Height &&
                   Y + Height > other.Y;
        }
    }
}
