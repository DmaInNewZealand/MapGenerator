using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;

    [Range(0, 5)]
    public int borderSize;

    [Range(0, 100)]
    public int randomFillPercent;

    [Range(0, 5)]
    public int smoothTimes;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 50)]
    public int threshold = 30;

    int[,] map;

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        map = new int[width, height];
        randomFillMap();

        for (int i = 0; i < smoothTimes; i++)
        {
            SmoothMap();
        }

        ProcessMap();

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map, 1f);
    }

    //void OnDrawGizmos()
    //{
    //    if (map != null)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            for (int y = 0; y < height; y++)
    //            {
    //                Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
    //                Vector3 pos = new Vector3(-width / 2 + x + .5f, -5, -height / 2 + y + .5f);
    //                Gizmos.DrawCube(pos, Vector3.one);
    //            }
    //        }
    //    }
    //}


    List<Coord> GetRegionTiles(int startX, int startY, int[,] mapFlags)
    {
        List<Coord> tileList = new List<Coord>();
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();

        mapFlags[startX, startY] = 1;
        queue.Enqueue(new Coord(startX, startY));

        while (queue.Count > 0)
        {
            var tile = queue.Dequeue();
            tileList.Add(tile);

            //put his adjacent tiles 
            for (int x = tile.TileX - 1; x <= tile.TileX + 1; x++)
            {
                for (int y = tile.TileY - 1; y <= tile.TileY + 1; y++)
                {
                    if (IsInMap(x, y) && (x == tile.TileX || y == tile.TileY))
                    {
                        if (mapFlags[x, y] != 1 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }
        return tileList;
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    regions.Add(GetRegionTiles(x, y, mapFlags));
                }
            }
        }

        return regions;
    }

    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (var wallRegion in wallRegions)
        {
            if (wallRegion.Count < threshold)
            {
                foreach (var tile in wallRegion)
                {
                    map[tile.TileX, tile.TileY] = 0;
                }
            }
        }

        List<List<Coord>> floorRegions = GetRegions(0);
        List<Room> surrivedRooms = new List<Room>();

        foreach (var floorRegion in floorRegions)
        {
            if (floorRegion.Count < threshold)
            {
                foreach (var tile in floorRegion)
                {
                    map[tile.TileX, tile.TileY] = 1;
                }
            }
            else
            {
                surrivedRooms.Add(new Room(floorRegion));
            }
        }

        CalculateRoomConnections(surrivedRooms);
    }

    void CalculateRoomConnections(List<Room> surrivedRooms)
    {
        if (surrivedRooms.Count <= 1)
        {
            //only 1 room or no room
            return;
        }

        surrivedRooms.Sort();

        int[,] distances = new int[surrivedRooms.Count, surrivedRooms.Count];
        Coord[,] closeTiles = new Coord[surrivedRooms.Count, surrivedRooms.Count];

        for (int x = 0; x < surrivedRooms.Count; x++)
        {
            for (int y = x; y < surrivedRooms.Count; y++)
            {
                if (x != y)
                {
                    distances[x, y] = Room.CloseDistance(surrivedRooms[x], surrivedRooms[y], out closeTiles[x, y], out closeTiles[y, x]);
                    distances[y, x] = distances[x, y];
                }
                else
                {
                    distances[x, y] = 0;
                    closeTiles[x, y] = new Coord();
                }
            }
        }

        do
        {
            int closestRoom = 0;
            int closestDistance = int.MaxValue;

            for (int x = 0; x < surrivedRooms.Count; x++)
            {
                if (distances[0, x] == 0)// self or connected room
                    continue;
                else if (distances[0, x] < closestDistance)
                {
                    closestDistance = distances[0, x];
                    closestRoom = x;
                }
            }

            if (closestRoom == 0)
            {
                //no more rooms
                break;
            }
            else
            {
                distances[0, closestRoom] = 0;
                distances[closestRoom, 0] = 0;

                for (int i = 1; i < surrivedRooms.Count; i++)
                {
                    if (distances[0, i] > distances[closestRoom, i])
                    {
                        //set distance
                        distances[0, i] = distances[closestRoom, i];
                        distances[i, 0] = distances[closestRoom, i];

                        //set coord
                        closeTiles[0, i] = closeTiles[closestRoom, i];
                        closeTiles[i, 0] = closeTiles[i, closestRoom];
                    }
                }
            }
        }
        while (true);

        for (int x = 1; x < surrivedRooms.Count; x++)
        {
            CreatePassage(closeTiles[0, x], closeTiles[x, 0]);
        }
    }

    void CreatePassage(Coord tileA, Coord tileB)
    {
        //Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 5);

        List<Coord> line = new List<Coord>();

        int x = tileA.TileX;
        int y = tileA.TileY;

        int dx = tileB.TileX - tileA.TileX;
        int dy = tileB.TileY - tileA.TileY;

        //How many steps from A to B
        int step;
        float adjustX = 0.0f;
        float adjustY = 0.0f;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy)) //x>=y
        {
            step = Mathf.Abs(dx);
            adjustY = 0.5f;
        }
        else
        {
            step = Mathf.Abs(dy);
            adjustX = 0.5f;
        }

        for (int i = 0; i <= step; i++)
        {
            line.Add(new Coord(x + (int)(dx * i / step + adjustX), y + (int)(dy * i / step + adjustY)));
        }

        foreach (var point in line)
        {
            DrawCircle(point, 1, map[tileA.TileX, tileA.TileY]);
        }
    }

    private Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width / 2 + .5f + tile.TileX, 2, -height / 2 + .5f + tile.TileY);
    }

    void DrawCircle(Coord point, int r, int type)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)// in the circle
                {
                    if (IsInMap(x + point.TileX, y + point.TileY))//in the map
                    {
                        map[x + point.TileX, y + point.TileY] = type;
                    }
                }
            }
        }
    }

    void randomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random randGenerator = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < borderSize || y < borderSize || x >= width - borderSize || y >= height - borderSize)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = randGenerator.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int wallCount = GetSurroundingCount(x, y);
                if (wallCount > 4)
                {
                    map[x, y] = 1;
                }
                else if (wallCount < 4)
                {
                    map[x, y] = 0;
                }
            }
        }
    }

    int GetSurroundingCount(int gridX, int gridY)
    {
        int wallCount = 0;

        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMap(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    private bool IsInMap(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    struct Coord
    {
        public int TileX;
        public int TileY;

        public Coord(int x, int y)
        {
            TileX = x;
            TileY = y;
        }
    }

    class Room : System.IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edges;

        public int roomSize;

        public Room()
        {

        }

        public Room(List<Coord> roomTiles)
        {
            tiles = roomTiles;
            roomSize = roomTiles.Count;

            HashSet<Coord> tilesHashSet = new HashSet<Coord>();
            foreach (var tile in roomTiles)
            {
                tilesHashSet.Add(tile);
            }

            edges = new List<Coord>();

            foreach (var tile in roomTiles)
            {
                for (int x = tile.TileX - 1; x <= tile.TileX + 1; x++)
                {
                    for (int y = tile.TileY - 1; y <= tile.TileY + 1; y++)
                    {
                        if (x == tile.TileX || y == tile.TileY)
                        {
                            if (!tilesHashSet.Contains(new Coord(x, y)))
                                edges.Add(tile);
                        }
                    }
                }
            }
        }

        public static int CloseDistance(Room roomA, Room roomB, out Coord tileA, out Coord tileB)
        {
            int bestDistance = int.MaxValue;

            tileA = new Coord();
            tileB = new Coord();

            foreach (var roomATile in roomA.edges)
            {
                foreach (var roomBTile in roomB.edges)
                {
                    var distance = (int)(Mathf.Pow((roomATile.TileX - roomBTile.TileX), 2) + Mathf.Pow((roomATile.TileY - roomBTile.TileY), 2));
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        tileA = roomATile;
                        tileB = roomBTile;
                    }
                }
            }
            return bestDistance;
        }

        public int CompareTo(Room other)
        {
            return other.roomSize.CompareTo(this.roomSize);
        }
    }
}
