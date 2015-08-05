using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;

    public MeshFilter floors;
    public MeshFilter walls;

    public float wallHeight = 5.0f;

    List<Vector3> vertics;
    List<int> triangles;

    Dictionary<int, List<Triangle>> trianglesDictionary;
    List<List<int>> outLines;

    HashSet<int> checkedVertices;

    public void GenerateMesh(int[,] map, float squareSize)
    {
        vertics = new List<Vector3>();
        triangles = new List<int>();

        trianglesDictionary = new Dictionary<int, List<Triangle>>();
        outLines = new List<List<int>>();
        checkedVertices = new HashSet<int>();

        squareGrid = new SquareGrid(map, squareSize);

        CreateFloorMesh();
        CreateWallMesh();
    }

    void CreateFloorMesh()
    {
        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        floors.mesh = mesh;

        mesh.vertices = vertics.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    void CreateWallMesh()
    {
        GetOutLines();

        List<Vector3> wallVertics = new List<Vector3>();
        List<int> wallTriangles = new List<int>();

        foreach (var outLine in outLines)
        {
            for (int i = 0; i < outLine.Count - 1; i++)
            {
                int startIndex = wallVertics.Count;
                wallVertics.Add(vertics[outLine[i]]);
                wallVertics.Add(vertics[outLine[i + 1]]);
                wallVertics.Add(vertics[outLine[i]] - Vector3.up * wallHeight);
                wallVertics.Add(vertics[outLine[i + 1]] - Vector3.up * wallHeight);

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
            }
        }

        Mesh wallMesh = new Mesh();
        walls.mesh = wallMesh;

        wallMesh.vertices = wallVertics.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();
    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                square.centerLeft.isOutterLine = true;
                square.centerBottom.isOutterLine = true;
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                square.centerBottom.isOutterLine = true;
                square.centerRight.isOutterLine = true;
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                square.centerRight.isOutterLine = true;
                square.centerTop.isOutterLine = true;
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                square.centerTop.isOutterLine = true;
                square.centerLeft.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;


            // 2 points:
            case 3:
                square.centerLeft.isOutterLine = true;
                square.centerRight.isOutterLine = true;
                MeshFromPoints(square.bottomRight, square.bottomLeft, square.centerLeft, square.centerRight);
                break;
            case 6:
                square.centerTop.isOutterLine = true;
                square.centerBottom.isOutterLine = true;
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                square.centerTop.isOutterLine = true;
                square.centerBottom.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                square.centerRight.isOutterLine = true;
                square.centerLeft.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                square.centerTop.isOutterLine = true;
                square.centerRight.isOutterLine = true;
                square.centerBottom.isOutterLine = true;
                square.centerLeft.isOutterLine = true;
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                square.centerTop.isOutterLine = true;
                square.centerRight.isOutterLine = true;
                square.centerBottom.isOutterLine = true;
                square.centerLeft.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 3 point:
            case 7:
                square.centerTop.isOutterLine = true;
                square.centerLeft.isOutterLine = true;
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                square.centerTop.isOutterLine = true;
                square.centerRight.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                square.centerRight.isOutterLine = true;
                square.centerBottom.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                square.centerBottom.isOutterLine = true;
                square.centerLeft.isOutterLine = true;
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertics(points);

        if (points.Length >= 3)
            CreateTriangles(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangles(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangles(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangles(points[0], points[4], points[5]);
    }

    void AssignVertics(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertics.Count;
                vertics.Add(points[i].position);
            }
        }
    }

    void CreateTriangles(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);

        if (a.isOutterLine)
        {
            AddTriangleToDictionary(a.vertexIndex, triangle);
        }
        if (b.isOutterLine)
        {
            AddTriangleToDictionary(b.vertexIndex, triangle);
        }
        if (c.isOutterLine)
        {
            AddTriangleToDictionary(c.vertexIndex, triangle);
        }
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (trianglesDictionary.ContainsKey(vertexIndexKey))
        {
            //Contains the vertexIndexKey, Add the new triangle to the list of the index
            trianglesDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            //Doesn't contain the vertexIndexKey, create a new key and add the triangle
            trianglesDictionary.Add(vertexIndexKey, new List<Triangle>());
            trianglesDictionary[vertexIndexKey].Add(triangle);
        }
    }

    int NextOutLineVertex(int vertexIndex)
    {
        var triangleList = trianglesDictionary[vertexIndex];

        int nextVertexIndex = -1;

        foreach (var triangle in triangleList)
        {
            nextVertexIndex = triangle.NextVertexClockwise(vertexIndex);

            //GetNextVertexIndexClockwise
            if (trianglesDictionary.ContainsKey(nextVertexIndex))
            {
                break;
            }
        }
        return nextVertexIndex;
    }

    List<int> GetOutLine(int vertexStart)
    {
        List<int> verticesList = new List<int>();
        verticesList.Add(vertexStart);

        int nextVertexIndex = vertexStart;
        while (true)
        {
            checkedVertices.Add(nextVertexIndex);
            nextVertexIndex = NextOutLineVertex(nextVertexIndex);
            verticesList.Add(nextVertexIndex);
            if (nextVertexIndex == vertexStart)
            {
                break;
            }
        }
        return verticesList;
    }

    void GetOutLines()
    {
        foreach (var vertex in trianglesDictionary)
        {
            if (!checkedVertices.Contains(vertex.Key))
            {
                outLines.Add(GetOutLine(vertex.Key));
            }
        }
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);

            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    controlNodes[x, y] = new ControlNode(new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2f, 0, -mapHeight / 2 + y * squareSize + squareSize / 2f), map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x, y], controlNodes[x + 1, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centerTop, centerLeft, centerRight, centerBottom;

        public int configuration = 0;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomLeft, ControlNode _bottomRight)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomLeft = _bottomLeft;
            bottomRight = _bottomRight;

            centerTop = topLeft.right;
            centerLeft = bottomLeft.above;
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;

            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;
        public bool isOutterLine = false;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active;

        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize)
            : base(_pos)
        {
            active = _active;
            above = new Node(_pos + Vector3.forward * squareSize / 2f);
            right = new Node(_pos + Vector3.right * squareSize / 2f);
        }
    }

    struct Triangle
    {
        public int VertexIndexA;
        public int VertexIndexB;
        public int VertexIndexC;

        public Triangle(int a, int b, int c)
        {
            VertexIndexA = a;
            VertexIndexB = b;
            VertexIndexC = c;
        }

        public int NextVertexClockwise(int vertexIndex)
        {
            if (vertexIndex == VertexIndexA)
                return VertexIndexB;
            else if (vertexIndex == VertexIndexB)
                return VertexIndexC;
            else if (vertexIndex == VertexIndexC)
                return VertexIndexA;
            else
                return -1;
        }
    }
}
