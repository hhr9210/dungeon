using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WorldBuilder : MonoBehaviour
{
    // 世界尺寸
    [Tooltip("世界的宽度。")]
    public int worldWidth = 50;
    [Tooltip("世界的高度。")]
    public int worldHeight = 50;

    // 世界单元格的预制体
    [Tooltip("墙壁预制体，立方体。")]
    public GameObject wallPrefab;
    [Tooltip("地板预制体，平面。")]
    public GameObject floorPrefab;
    [Tooltip("起点预制体。注意：这将移动场景中现有的Player GameObject。")]
    public GameObject startPrefab;
    [Tooltip("终点预制体，玩家需要到达的目标。")]
    public GameObject endPrefab;

    // 物品预制体
    [Tooltip("宝箱预制体，可以放置在房间单元格中。")]
    public GameObject treasureChestPrefab;

    // 角色预制体
    [Tooltip("NPC预制体的列表，用于在房间中生成。")]
    public GameObject[] npcPrefabs;
    [Tooltip("敌人预制体的列表，用于在开放区域和房间中生成。")]
    public GameObject[] enemyPrefabs;

    // 房间生成设置
    [Header("Room Generation Settings")]
    [Tooltip("要生成的房间数量。")]
    public int numberOfRooms = 5;
    [Tooltip("房间的最小尺寸（宽度和高度）。")]
    public int minRoomDimension = 5;
    [Tooltip("房间的最大尺寸（宽度和高度）。")]
    public int maxRoomDimension = 15;
    [Tooltip("生成房间的最大尝试次数，以防止无限循环。")]
    public int maxRoomAttempts = 100;
    [Tooltip("房间之间强制的最小墙壁间隔（以单元格为单位）。这也为走廊提供了空间。")]
    public int minRoomSeparation = 3;

    // 地牢生成设置
    [Header("Dungeon Generation Settings")]
    [Tooltip("走廊的最小宽度。")]
    public int minCorridorWidth = 1;
    [Tooltip("走廊的最大宽度。")]
    public int maxCorridorWidth = 3;

    // 物品、NPC和敌人生成设置
    [Header("Item & NPC & Enemy Generation Settings")]
    [Tooltip("每个房间生成宝箱的概率。")]
    [Range(0f, 1f)]
    public float chestSpawnChanceInRoom = 0.5f; // 旧的基于房间的概率
    [Tooltip("世界中生成的宝箱最小数量。")]
    public int minChestsPerWorld = 5;
    [Tooltip("世界中生成的宝箱最大数量。")]
    public int maxChestsPerWorld = 15;

    [Tooltip("每个房间生成NPC的概率。")]
    [Range(0f, 1f)]
    public float npcSpawnChancePerRoom = 0.7f;
    [Tooltip("每个房间生成的最小NPC数量。")]
    public int minNpcPerRoom = 1;
    [Tooltip("每个房间生成的最大NPC数量。")]
    public int maxNpcPerRoom = 3;

    [Tooltip("世界中生成的敌人最小数量。")]
    public int minEnemiesPerWorld = 10;
    [Tooltip("世界中生成的敌人最大数量。")]
    public int maxEnemiesPerWorld = 20;
    [Tooltip("每个房间生成敌人的概率。")]
    [Range(0f, 1f)]
    public float enemySpawnChanceInRoom = 0.4f; // 旧的基于房间的概率
    [Tooltip("每个房间生成的最小敌人数量。")]
    public int minEnemyPerRoom = 1;
    [Tooltip("每个房间生成的最大敌人数量。")]
    public int maxEnemyPerRoom = 4;

    // 新增：玩家生成设置
    [Header("Player Spawn Settings")]
    [Tooltip("玩家起点周围禁止生成敌人的最小距离。")]
    public float minDistanceToEnemies = 5f;
    [Tooltip("玩家起点和终点之间的最小距离。")]
    public float minDistanceBetweenStartAndEnd = 20f;
    [Tooltip("玩家起点距离墙壁的最小单元格距离。")]
    public int minDistanceToWall = 2;


    // 内部世界数据表示:
    // 0 - 地板, 1 - 墙壁, 2 - 起点, 3 - 终点, 4 - 门 (暂未使用此值), 5 - 宝箱, 6 - NPC, 7 - 敌人, 8 - 房间地板
    private int[,] worldGrid;

    // 房间数据结构，用于支持非矩形房间
    public class RoomData
    {
        public RectInt BoundingBox; // 房间的包围盒
        public HashSet<Vector2Int> FloorCells = new HashSet<Vector2Int>(); // 房间内的地板单元格
    }

    // 存储生成的房间
    private List<RoomData> rooms = new List<RoomData>();

    // 存储起点位置，以便在Build3DWorld中访问
    private Vector2Int _calculatedStartPoint;
    // 存储终点位置，以便在SpawnEnemies中访问
    private Vector2Int _calculatedEndPoint;

    void Start()
    {
        GenerateAndBuildWorld();
    }

    public void GenerateAndBuildWorld()
    {
        ClearPreviousWorldInstances();

        worldGrid = new int[worldWidth, worldHeight];
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldHeight; z++)
            {
                worldGrid[x, z] = 1; // 1代表墙壁
            }
        }

        rooms.Clear(); // 每次生成前清空房间列表

        // 1. 生成房间 (支持不同形状)
        GenerateRooms();

        // 2. 使用类似迷宫的走廊连接房间
        ConnectRoomsWithMaze();

        // 3. 在开放地板区域 (0) 或房间地板区域 (8) 设置起点和终点
        SetStartAndEndPoints();

        // 4. 生成宝箱
        SpawnChests();

        // 5. 在房间内生成NPC
        SpawnNPCs();

        // 6. 在开放区域和房间中生成敌人
        SpawnEnemies();

        // 7. 根据世界网格实例化3D对象并移动玩家
        Build3DWorld();
    }

    private void ClearPreviousWorldInstances()
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            // 确保不销毁 startPrefab 本身，如果它已经存在于场景中
            if (startPrefab != null && child.gameObject == startPrefab)
            {
                continue;
            }
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject go in childrenToDestroy)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }

        // 额外清理可能在场景中残留的startPrefab克隆体
        if (startPrefab != null)
        {
            string originalPrefabName = startPrefab.name;
            GameObject[] allPotentialClones = GameObject.FindObjectsOfType<GameObject>()
                                                     .Where(go => go.name.Contains(originalPrefabName) && go.name.EndsWith("(Clone)"))
                                                     .ToArray();

            foreach (GameObject go in allPotentialClones)
            {
                if (go != null && go != startPrefab)
                {
                    Destroy(go);
                    Debug.Log($"在生成开始时销毁了残留的'{go.name}'克隆体。");
                }
            }
        }
    }


    /// <summary>
    /// 生成各种形状的房间（矩形、圆形、L形），并在世界网格中标记它们。
    /// </summary>
    void GenerateRooms()
    {
        int roomsGenerated = 0;
        for (int attempts = 0; attempts < maxRoomAttempts && roomsGenerated < numberOfRooms; attempts++)
        {
            RoomData newRoom = null;
            float roomType = Random.value;

            if (roomType < 0.6f) // 60%的几率为矩形房间
            {
                newRoom = GenerateRectangularRoom();
            }
            else if (roomType < 0.8f) // 20%的几率为圆形房间
            {
                newRoom = GenerateCircularRoom();
            }
            else // 20%的几率为L形房间
            {
                newRoom = GenerateLShapedRoom();
            }

            if (newRoom != null && !CheckRoomOverlap(newRoom))
            {
                MarkRoomInGrid(newRoom);
                rooms.Add(newRoom);
                roomsGenerated++;
            }
        }

        if (roomsGenerated < numberOfRooms)
        {
            Debug.LogWarning($"未能生成所有 {numberOfRooms} 个房间。实际生成了 {roomsGenerated} 个房间。请尝试减小房间尺寸或增加 maxRoomAttempts。");
        }
    }

    // 辅助函数，用于生成一个矩形房间
    private RoomData GenerateRectangularRoom()
    {
        int roomWidth = Random.Range(minRoomDimension, maxRoomDimension + 1);
        int roomHeight = Random.Range(minRoomDimension, maxRoomDimension + 1);

        if (roomWidth < 3) roomWidth = 3;
        if (roomHeight < 3) roomHeight = 3;

        int roomX = Random.Range(minRoomSeparation, worldWidth - roomWidth - minRoomSeparation + 1);
        int roomZ = Random.Range(minRoomSeparation, worldHeight - roomHeight - minRoomSeparation + 1);

        if (roomX < 0 || roomZ < 0 || roomX + roomWidth > worldWidth || roomZ + roomHeight > worldHeight)
        {
            return null;
        }

        RoomData newRoom = new RoomData();
        newRoom.BoundingBox = new RectInt(roomX, roomZ, roomWidth, roomHeight);

        for (int x = roomX; x < roomX + roomWidth; x++)
        {
            for (int z = roomZ; z < roomZ + roomHeight; z++)
            {
                if (x == roomX || x == roomX + roomWidth - 1 || z == roomZ || z == roomZ + roomHeight - 1)
                {
                    // 房间周长稍后在网格中标记为墙壁
                }
                else
                {
                    newRoom.FloorCells.Add(new Vector2Int(x, z));
                }
            }
        }
        return newRoom;
    }

    // 辅助函数，用于生成一个圆形房间
    private RoomData GenerateCircularRoom()
    {
        int radius = Random.Range(minRoomDimension / 2, maxRoomDimension / 2 + 1);
        Vector2Int center = new Vector2Int(
            Random.Range(radius + minRoomSeparation, worldWidth - radius - minRoomSeparation),
            Random.Range(radius + minRoomSeparation, worldHeight - radius - minRoomSeparation)
        );

        if (center.x - radius < 0 || center.x + radius >= worldWidth ||
            center.y - radius < 0 || center.y + radius >= worldHeight)
        {
            return null;
        }

        RoomData newRoom = new RoomData();
        newRoom.BoundingBox = new RectInt(center.x - radius, center.y - radius, radius * 2 + 1, radius * 2 + 1);

        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int z = center.y - radius; z <= center.y + radius; z++)
            {
                if (Vector2Int.Distance(center, new Vector2Int(x, z)) <= radius)
                {
                    newRoom.FloorCells.Add(new Vector2Int(x, z));
                }
            }
        }
        return newRoom;
    }

    // 辅助函数，用于生成一个L形房间
    private RoomData GenerateLShapedRoom()
    {
        int width1 = Random.Range(minRoomDimension, maxRoomDimension / 2 + 1);
        int height1 = Random.Range(minRoomDimension, maxRoomDimension + 1);
        int width2 = Random.Range(minRoomDimension, maxRoomDimension + 1);
        int height2 = Random.Range(minRoomDimension, maxRoomDimension / 2 + 1);

        int roomX = Random.Range(minRoomSeparation, worldWidth - maxRoomDimension - minRoomSeparation + 1);
        int roomZ = Random.Range(minRoomSeparation, worldHeight - maxRoomDimension - minRoomSeparation + 1);

        RoomData newRoom = new RoomData();

        // 定义构成“L”的两个矩形
        RectInt rect1 = new RectInt(roomX, roomZ, width1, height1);
        RectInt rect2 = new RectInt(roomX, roomZ + height1 - height2, width2, height2);

        // 计算合并后的包围盒
        int minX = Mathf.Min(rect1.xMin, rect2.xMin);
        int minZ = Mathf.Min(rect1.yMin, rect2.yMin);
        int maxX = Mathf.Max(rect1.xMax, rect2.xMax);
        int maxZ = Mathf.Max(rect1.yMax, rect2.yMax);

        newRoom.BoundingBox = new RectInt(minX, minZ, maxX - minX, maxZ - minZ);

        // 将两个矩形的所有单元格添加到房间的地板单元格中
        for (int x = rect1.xMin; x < rect1.xMax; x++)
        {
            for (int z = rect1.yMin; z < rect1.yMax; z++)
            {
                newRoom.FloorCells.Add(new Vector2Int(x, z));
            }
        }
        for (int x = rect2.xMin; x < rect2.xMax; x++)
        {
            for (int z = rect2.yMin; z < rect2.yMax; z++)
            {
                newRoom.FloorCells.Add(new Vector2Int(x, z));
            }
        }

        // 验证房间是否在世界范围内，并有足够的间隔
        if (newRoom.BoundingBox.xMin < minRoomSeparation ||
            newRoom.BoundingBox.yMin < minRoomSeparation ||
            newRoom.BoundingBox.xMax >= worldWidth - minRoomSeparation ||
            newRoom.BoundingBox.yMax >= worldHeight - minRoomSeparation)
        {
            return null;
        }

        return newRoom;
    }

    /// <summary>
    /// 检查新房间是否与现有房间重叠，包括间隔缓冲区。
    /// </summary>
    bool CheckRoomOverlap(RoomData newRoom)
    {
        RectInt expandedNewRoomRect = new RectInt(newRoom.BoundingBox.xMin - minRoomSeparation,
                                                 newRoom.BoundingBox.yMin - minRoomSeparation,
                                                 newRoom.BoundingBox.width + minRoomSeparation * 2,
                                                 newRoom.BoundingBox.height + minRoomSeparation * 2);

        foreach (RoomData existingRoom in rooms)
        {
            RectInt expandedExistingRoomRect = new RectInt(existingRoom.BoundingBox.xMin - minRoomSeparation,
                                                           existingRoom.BoundingBox.yMin - minRoomSeparation,
                                                           existingRoom.BoundingBox.width + minRoomSeparation * 2,
                                                           existingRoom.BoundingBox.height + minRoomSeparation * 2);

            if (expandedNewRoomRect.Overlaps(expandedExistingRoomRect))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 在世界网格中标记房间的地板和墙壁单元格。
    /// </summary>
    void MarkRoomInGrid(RoomData room)
    {
        // 首先，将所有内部单元格标记为房间地板
        foreach (Vector2Int cell in room.FloorCells)
        {
            if (cell.x >= 0 && cell.x < worldWidth && cell.y >= 0 && cell.y < worldHeight)
            {
                worldGrid[cell.x, cell.y] = 8; // 8代表房间地板
            }
        }

        // 然后，在房间的包围盒周围标记一圈墙壁
        for (int x = room.BoundingBox.xMin - 1; x <= room.BoundingBox.xMax; x++)
        {
            for (int z = room.BoundingBox.yMin - 1; z <= room.BoundingBox.yMax; z++)
            {
                if (x >= 0 && x < worldWidth && z >= 0 && z < worldHeight)
                {
                    Vector2Int currentCell = new Vector2Int(x, z);
                    // 如果该单元格不是房间的地板，并且它在周长内，则将其标记为墙壁
                    if (!room.FloorCells.Contains(currentCell))
                    {
                        worldGrid[x, z] = 1;
                    }
                }
            }
        }
    }


    /// <summary>
    /// 使用修改后的Prim's算法连接房间，生成类似迷宫的走廊。
    /// </summary>
    void ConnectRoomsWithMaze()
    {
        if (rooms.Count < 2) return;

        // 一个列表，用于跟踪已连接的房间。
        List<RoomData> connectedRooms = new List<RoomData>();

        // 一个列表，存储从已连接房间到未连接房间的所有边（潜在的走廊）。
        List<CorridorEdge> edges = new List<CorridorEdge>();

        // 从一个随机房间开始。
        RoomData startRoom = rooms[Random.Range(0, rooms.Count)];
        connectedRooms.Add(startRoom);

        // 添加从起始房间到所有其他房间的所有潜在走廊。
        AddEdgesFromRoom(startRoom, connectedRooms, edges);

        // 持续连接房间，直到所有房间都被连接。
        while (connectedRooms.Count < rooms.Count && edges.Count > 0)
        {
            // 找到列表中最短的边（走廊）。
            CorridorEdge shortestEdge = edges.OrderBy(e => e.Distance).First();

            RoomData roomToConnectTo = shortestEdge.ToRoom;

            // 在两个房间之间开辟一条路径。
            CarveCorridor(shortestEdge.FromRoom.BoundingBox, shortestEdge.ToRoom.BoundingBox);

            // 添加新连接的房间并更新边。
            connectedRooms.Add(roomToConnectTo);
            AddEdgesFromRoom(roomToConnectTo, connectedRooms, edges);

            // 移除所有连接到新连接房间的边。
            edges.RemoveAll(e => e.ToRoom == roomToConnectTo);
        }
    }

    // 一个辅助类，用于表示两个房间之间的潜在走廊。
    private class CorridorEdge
    {
        public RoomData FromRoom;
        public RoomData ToRoom;
        public float Distance;
    }

    // 辅助函数，用于添加从一个房间到所有未连接房间的所有有效边。
    private void AddEdgesFromRoom(RoomData room, List<RoomData> connectedRooms, List<CorridorEdge> edges)
    {
        foreach (RoomData otherRoom in rooms)
        {
            if (!connectedRooms.Contains(otherRoom))
            {
                edges.Add(new CorridorEdge
                {
                    FromRoom = room,
                    ToRoom = otherRoom,
                    Distance = Vector2.Distance(room.BoundingBox.center, otherRoom.BoundingBox.center)
                });
            }
        }
    }


    /// <summary>
    /// 使用简单的L形路径在两个房间之间开辟一条走廊。
    /// 这现在是迷宫生成器的辅助函数。
    /// 该路径将把墙壁单元格（1）变成地板单元格（0）。
    /// </summary>
    void CarveCorridor(RectInt room1, RectInt room2)
    {
        Vector2Int startPoint = GetRandomPointOnRoomPerimeter(room1);
        Vector2Int endPoint = GetRandomPointOnRoomPerimeter(room2);

        int currentCorridorWidth = Random.Range(minCorridorWidth, maxCorridorWidth + 1);
        if (currentCorridorWidth <= 0) currentCorridorWidth = 1;

        if (Random.value < 0.5f)
        {
            CarvePathSegment(startPoint.x, endPoint.x, startPoint.y, true, currentCorridorWidth);
            CarvePathSegment(startPoint.y, endPoint.y, endPoint.x, false, currentCorridorWidth);
        }
        else
        {
            CarvePathSegment(startPoint.y, endPoint.y, startPoint.x, false, currentCorridorWidth);
            CarvePathSegment(startPoint.x, endPoint.x, endPoint.y, true, currentCorridorWidth);
        }
    }

    void CarvePathSegment(int startCoord, int endCoord, int fixedCoord, bool isXAxis, int width)
    {
        int min = Mathf.Min(startCoord, endCoord);
        int max = Mathf.Max(startCoord, endCoord);

        int halfCorridorWidth = width / 2;
        int expansionRadius = 1;

        for (int i = min; i <= max; i++)
        {
            for (int w = -halfCorridorWidth; w < halfCorridorWidth + (width % 2); w++)
            {
                int x, z;
                if (isXAxis)
                {
                    x = i;
                    z = fixedCoord + w;
                }
                else
                {
                    x = fixedCoord + w;
                    z = i;
                }

                if (x >= 0 && x < worldWidth && z >= 0 && z < worldHeight)
                {
                    // 只有在当前单元格是墙壁(1)或走廊(0)时才将其变成地板
                    if (worldGrid[x, z] == 1 || worldGrid[x, z] == 0)
                    {
                        worldGrid[x, z] = 0;
                    }
                }

                // 额外检查周围的墙壁，将它们也变成地板，以确保走廊是开放的
                for (int dx = -expansionRadius; dx <= expansionRadius; dx++)
                {
                    for (int dz = -expansionRadius; dz <= expansionRadius; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        int expandedX = x + dx;
                        int expandedZ = z + dz;

                        if (expandedX >= 0 && expandedX < worldWidth && expandedZ >= 0 && expandedZ < worldHeight)
                        {
                            if (worldGrid[expandedX, expandedZ] == 1)
                            {
                                worldGrid[expandedX, expandedZ] = 0;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取给定房间包围盒周长上的一个随机点。
    /// 该点将作为走廊开辟的起始/结束点。
    /// </summary>
    Vector2Int GetRandomPointOnRoomPerimeter(RectInt room)
    {
        List<Vector2Int> perimeterPoints = new List<Vector2Int>();
        // 收集包围盒周长上的点
        for (int x = room.xMin; x < room.xMax; x++)
        {
            perimeterPoints.Add(new Vector2Int(x, room.yMin));
            perimeterPoints.Add(new Vector2Int(x, room.yMax - 1));
        }
        for (int z = room.yMin; z < room.yMax; z++)
        {
            perimeterPoints.Add(new Vector2Int(room.xMin, z));
            perimeterPoints.Add(new Vector2Int(room.xMax - 1, z));
        }

        // 过滤出有效的点（在世界范围内且不是房间内部的地板）
        List<Vector2Int> validPerimeterPoints = new List<Vector2Int>();
        foreach (Vector2Int p in perimeterPoints.Distinct())
        {
            if (p.x >= 0 && p.x < worldWidth && p.y >= 0 && p.y < worldHeight)
            {
                // 要墙壁(1)或房间外缘地板(8)的点。
                // 避免使用房间内部的地板单元格，以防止走廊穿过房间。
                if (worldGrid[p.x, p.y] == 1 || worldGrid[p.x, p.y] == 8)
                {
                    validPerimeterPoints.Add(p);
                }
            }
        }

        if (validPerimeterPoints.Count > 0)
        {
            return validPerimeterPoints[Random.Range(0, validPerimeterPoints.Count)];
        }

        Debug.LogWarning($"房间 {room} 没有可连接的周长点。返回中心点作为备用。");
        return new Vector2Int(Mathf.RoundToInt(room.center.x), Mathf.RoundToInt(room.center.y));
    }


    void SetStartAndEndPoints()
    {
        List<Vector2Int> floorCells = new List<Vector2Int>();
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldHeight; z++)
            {
                if (worldGrid[x, z] == 0 || worldGrid[x, z] == 8) // 查找所有地板和房间地板单元格
                {
                    floorCells.Add(new Vector2Int(x, z));
                }
            }
        }

        if (floorCells.Count < 2)
        {
            Debug.LogError("世界中可用空间太少，无法设置起点和终点！请增大世界尺寸或房间间隔。");
            return;
        }

        // 步骤1：过滤掉靠近墙壁的单元格
        List<Vector2Int> nonWallCells = new List<Vector2Int>();
        foreach (var cell in floorCells)
        {
            if (!IsNearWall(cell, minDistanceToWall))
            {
                nonWallCells.Add(cell);
            }
        }

        if (nonWallCells.Count < 2)
        {
            Debug.LogWarning("没有足够的远离墙壁的单元格来放置起点和终点。将使用所有地板单元格。");
            nonWallCells = floorCells;
        }

        // 步骤2：找到距离最远的两个点
        Vector2Int startCandidate = nonWallCells[Random.Range(0, nonWallCells.Count)];
        Vector2Int furthestPoint = GetFurthestPoint(startCandidate, nonWallCells);
        Vector2Int startPoint = GetFurthestPoint(furthestPoint, nonWallCells);
        Vector2Int endPoint = GetFurthestPoint(startPoint, nonWallCells);

        // 如果距离太近，选择一个随机点作为终点
        if (Vector2.Distance(startPoint, endPoint) < minDistanceBetweenStartAndEnd)
        {
             do
            {
                int randomIndex = Random.Range(0, nonWallCells.Count);
                endPoint = nonWallCells[randomIndex];
            } while (Vector2.Distance(startPoint, endPoint) < minDistanceBetweenStartAndEnd && nonWallCells.Count > 1);
        }

        // 标记起点和终点
        _calculatedStartPoint = startPoint;
        _calculatedEndPoint = endPoint;
        worldGrid[_calculatedStartPoint.x, _calculatedStartPoint.y] = 2; // 标记为起点
        worldGrid[_calculatedEndPoint.x, _calculatedEndPoint.y] = 3; // 标记为终点
    }

    /// <summary>
    /// 辅助方法：找到给定点列表中距离起点最远的点。
    /// </summary>
    private Vector2Int GetFurthestPoint(Vector2Int fromPoint, List<Vector2Int> points)
    {
        Vector2Int furthest = fromPoint;
        float maxDistance = 0f;
        foreach (var p in points)
        {
            float dist = Vector2.Distance(fromPoint, p);
            if (dist > maxDistance)
            {
                maxDistance = dist;
                furthest = p;
            }
        }
        return furthest;
    }

    /// <summary>
    /// 辅助方法：检查一个单元格是否靠近墙壁。
    /// </summary>
    private bool IsNearWall(Vector2Int cell, int distance)
    {
        for (int x = cell.x - distance; x <= cell.x + distance; x++)
        {
            for (int z = cell.y - distance; z <= cell.y + distance; z++)
            {
                if (x >= 0 && x < worldWidth && z >= 0 && z < worldHeight)
                {
                    if (worldGrid[x, z] == 1) // 1代表墙壁
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }


    /// <summary>
    /// 生成宝箱方法，通过世界范围内的最小/最大数量来控制
    /// </summary>
    void SpawnChests()
    {
        if (treasureChestPrefab == null)
        {
            Debug.LogWarning("宝箱预制体未设置，无法生成宝箱。");
            return;
        }

        int numChestsToSpawn = Random.Range(minChestsPerWorld, maxChestsPerWorld + 1);
        List<Vector2Int> availableCells = new List<Vector2Int>();

        // 收集所有可用的地板单元格，包括走廊和房间
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldHeight; z++)
            {
                if (worldGrid[x, z] == 0 || worldGrid[x, z] == 8)
                {
                    if (worldGrid[x, z] != 2 && worldGrid[x, z] != 3) // 排除起点和终点
                    {
                        availableCells.Add(new Vector2Int(x, z));
                    }
                }
            }
        }

        // 从可用单元格中随机选取位置并生成宝箱
        for (int i = 0; i < numChestsToSpawn && availableCells.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableCells.Count);
            Vector2Int spawnPos = availableCells[randomIndex];
            worldGrid[spawnPos.x, spawnPos.y] = 5; // 标记为宝箱
            availableCells.RemoveAt(randomIndex);
        }
    }

    /// <summary>
    /// 生成NPC方法，通过房间的概率和数量来控制
    /// </summary>
    void SpawnNPCs()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogWarning("NPC预制体列表未设置或为空，无法生成NPC。");
            return;
        }

        foreach (RoomData room in rooms)
        {
            if (Random.value < npcSpawnChancePerRoom)
            {
                int numNpcsToSpawn = Random.Range(minNpcPerRoom, maxNpcPerRoom + 1);
                // 在房间内部寻找可用的生成单元格
                List<Vector2Int> availableRoomCells = room.FloorCells.Where(cell =>
                    worldGrid[cell.x, cell.y] == 8 &&
                    worldGrid[cell.x, cell.y] != 2 && worldGrid[cell.x, cell.y] != 3 &&
                    worldGrid[cell.x, cell.y] != 5 && worldGrid[cell.x, cell.y] != 7).ToList();

                for (int i = 0; i < numNpcsToSpawn && availableRoomCells.Count > 0; i++)
                {
                    int randomIndex = Random.Range(0, availableRoomCells.Count);
                    Vector2Int npcPos = availableRoomCells[randomIndex];

                    worldGrid[npcPos.x, npcPos.y] = 6; // 标记为NPC
                    availableRoomCells.RemoveAt(randomIndex);
                }
            }
        }
    }

    /// <summary>
    /// 生成敌人方法，通过世界范围内的最小/最大数量来控制
    /// </summary>
    void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("敌人预制体列表未设置或为空，无法生成敌人。");
            return;
        }

        int numEnemiesToSpawn = Random.Range(minEnemiesPerWorld, maxEnemiesPerWorld + 1);
        List<Vector2Int> availableCells = new List<Vector2Int>();

        // 收集所有可用的地板单元格，包括走廊和房间
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldHeight; z++)
            {
                if (worldGrid[x, z] == 0 || worldGrid[x, z] == 8)
                {
                    // 排除起点、终点、宝箱和NPC的位置
                    if (worldGrid[x, z] != 2 && worldGrid[x, z] != 3 && worldGrid[x, z] != 5 && worldGrid[x, z] != 6)
                    {
                        // 新增：排除距离起点太近的位置
                        if (Vector2.Distance(new Vector2(x, z), _calculatedStartPoint) > minDistanceToEnemies)
                        {
                            availableCells.Add(new Vector2Int(x, z));
                        }
                    }
                }
            }
        }

        // 从可用单元格中随机选取位置并生成敌人
        for (int i = 0; i < numEnemiesToSpawn && availableCells.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableCells.Count);
            Vector2Int spawnPos = availableCells[randomIndex];
            worldGrid[spawnPos.x, spawnPos.y] = 7; // 标记为敌人
            availableCells.RemoveAt(randomIndex);
        }
    }

    void Build3DWorld()
    {
        float wallHeight = wallPrefab != null ? wallPrefab.transform.localScale.y : 1f;

        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldHeight; z++)
            {
                GameObject instantiatedObject = null;
                Vector3 floorPosition = new Vector3(x, 0, z);

                // 首先生成所有非墙壁单元格的地板
                if (worldGrid[x, z] != 1)
                {
                    if (floorPrefab != null)
                    {
                        instantiatedObject = Instantiate(floorPrefab, floorPosition, Quaternion.identity);
                        instantiatedObject.transform.parent = transform;
                    }
                }
                else // 如果是墙壁，但位于边界，则也生成地板
                {
                    bool isAtWorldEdge = (x == 0 || x == worldWidth - 1 || z == 0 || z == worldHeight - 1);
                    if (isAtWorldEdge)
                    {
                        if (floorPrefab != null)
                        {
                            instantiatedObject = Instantiate(floorPrefab, floorPosition, Quaternion.identity);
                            instantiatedObject.transform.parent = transform;
                        }
                        continue; // 跳过墙壁生成
                    }
                }

                switch (worldGrid[x, z])
                {
                    case 1: // 墙壁
                        if (wallPrefab != null)
                        {
                            bool isExposedWall = false;
                            // 检查相邻单元格，如果相邻单元格不是墙壁，则该墙壁是“暴露的”
                            int[] dx = { 0, 0, 1, -1 };
                            int[] dz = { 1, -1, 0, 0 };

                            for (int i = 0; i < 4; i++)
                            {
                                int nx = x + dx[i];
                                int nz = z + dz[i];

                                if (nx >= 0 && nx < worldWidth && nz >= 0 && nz < worldHeight)
                                {
                                    if (worldGrid[nx, nz] != 1)
                                    {
                                        isExposedWall = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    // 处于世界边缘的墙壁也是暴露的
                                    isExposedWall = true;
                                    break;
                                }
                            }

                            if (isExposedWall)
                            {
                                instantiatedObject = Instantiate(wallPrefab, new Vector3(x, wallHeight / 2f, z), Quaternion.identity);
                            }
                        }
                        break;
                    case 2: // 起点
                        if (startPrefab != null)
                        {
                            float playerHeight = startPrefab.transform.localScale.y;
                            Vector3 targetPosition = new Vector3(_calculatedStartPoint.x, playerHeight / 2f, _calculatedStartPoint.y);
                            startPrefab.transform.position = targetPosition;
                            startPrefab.SetActive(true); // 确保预制体是激活的
                            instantiatedObject = startPrefab;
                        }
                        break;
                    case 3: // 终点
                        if (endPrefab != null)
                        {
                            float actualEndHeight = endPrefab.transform.localScale.y;
                            instantiatedObject = Instantiate(endPrefab, new Vector3(x, actualEndHeight / 2f, z), Quaternion.identity);
                        }
                        break;
                    case 5: // 宝箱
                        if (treasureChestPrefab != null)
                        {
                            float chestHeight = treasureChestPrefab.transform.localScale.y;
                            instantiatedObject = Instantiate(treasureChestPrefab, new Vector3(x, 0f, z), Quaternion.identity);
                        }
                        break;
                    case 6: // NPC
                        if (npcPrefabs != null && npcPrefabs.Length > 0)
                        {
                            GameObject selectedNpcPrefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
                            if (selectedNpcPrefab != null)
                            {
                                float actualNpcHeight = selectedNpcPrefab.transform.localScale.y;
                                instantiatedObject = Instantiate(selectedNpcPrefab, new Vector3(x, 0f, z), Quaternion.identity);
                            }
                            else
                            {
                                Debug.LogWarning($"在 (x:{x}, z:{z}) 处尝试生成NPC，但选择的NPC预制体为空。");
                            }
                        }
                        break;
                    case 7: // 敌人
                        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
                        {
                            GameObject selectedEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                            if (selectedEnemyPrefab != null)
                            {
                                float actualEnemyHeight = selectedEnemyPrefab.transform.localScale.y;
                                instantiatedObject = Instantiate(selectedEnemyPrefab, new Vector3(x, 0f, z), Quaternion.identity);
                            }
                            else
                            {
                                Debug.LogWarning($"在 (x:{x}, z:{z}) 处尝试生成敌人，但选择的敌人预制体为空。");
                            }
                        }
                        break;
                }

                // 将除了startPrefab以外的所有对象作为子对象
                if (instantiatedObject != null && instantiatedObject != startPrefab && instantiatedObject.transform.parent != transform)
                {
                    instantiatedObject.transform.parent = transform;
                }
            }
        }
    }
}