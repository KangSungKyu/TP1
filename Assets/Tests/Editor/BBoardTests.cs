using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

[TestFixture]
public class BBoardTests
{
    [Test]
    public void BTile_SetAttribute_UpdatesAttr()
    {
        var go = new GameObject("tile");
        var tile = go.AddComponent<BTile>();
        tile.SetAttribute(BTileAttribute.StartPoint);
        Assert.AreEqual(BTileAttribute.StartPoint, tile.attr);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void FindPathWithBFS_VariousSizes_FindPathOnEmptyGrids()
    {
        int[] sizes = new int[] { 4, 6, 8, 10 };
        foreach (var size in sizes)
        {
            int w = size, h = size;
            var boardGo = new GameObject($"board_size_{w}x{h}");
            var board = boardGo.AddComponent<BBoard>();

            BTile[,] tiles = new BTile[h, w];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var tgo = new GameObject($"tile_{x}_{y}_{w}");
                    var t = tgo.AddComponent<BTile>();
                    t.x = x; t.y = y; t.type = BTileType.Empty; t.attr = BTileAttribute.None;
                    tiles[y, x] = t;
                }
            }

            // mark start (0,0) and end (w-1,h-1)
            tiles[0, 0].attr = BTileAttribute.StartPoint;
            tiles[h - 1, w - 1].attr = BTileAttribute.EndPoint;

            var ttype = typeof(BBoard);
            ttype.GetField("width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, w);
            ttype.GetField("height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, h);
            ttype.GetField("tiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, tiles);

            var path = board.FindPathWithBFS();
            Assert.IsNotNull(path, $"Expected a path for size {w}x{h} on an empty grid");
            Assert.IsTrue(path.Length >= 2, "Path should contain at least start and end");
            Assert.AreEqual(BTileAttribute.StartPoint, path[0].attr);
            Assert.AreEqual(BTileAttribute.EndPoint, path[path.Length - 1].attr);

            Object.DestroyImmediate(boardGo);
            foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
        }
    }

    [Test]
    public void FindPathWithBFS_FindsPathOnEmptyGrid()
    {
        int w = 4, h = 4;
        var boardGo = new GameObject("board");
        var board = boardGo.AddComponent<BBoard>();

        // create tiles array and attach simple BTile components
        BTile[,] tiles = new BTile[h, w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var tgo = new GameObject($"tile_{x}_{y}");
                var t = tgo.AddComponent<BTile>();
                t.x = x; t.y = y; t.type = BTileType.Empty; t.attr = BTileAttribute.None;
                tiles[y, x] = t;
            }
        }
    }

    [Test]
    public void FindPathWithBFS_ReturnsNull_WhenFullyBlocked()
    {
        int w = 4, h = 4;
        var boardGo = new GameObject("board_blocked");
        var board = boardGo.AddComponent<BBoard>();

        BTile[,] tiles = new BTile[h, w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var tgo = new GameObject($"tile_{x}_{y}");
                var t = tgo.AddComponent<BTile>();
                t.x = x; t.y = y; t.type = BTileType.Block; t.attr = BTileAttribute.None;
                tiles[y, x] = t;
            }
        }

        // start and end but surrounded by blocks
        tiles[0, 0].attr = BTileAttribute.StartPoint; tiles[0, 0].type = BTileType.Empty;
        tiles[2, 2].attr = BTileAttribute.EndPoint; tiles[2, 2].type = BTileType.Empty;

        var ttype = typeof(BBoard);
        ttype.GetField("width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, w);
        ttype.GetField("height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, h);
        ttype.GetField("tiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, tiles);

        var path = board.FindPathWithBFS();
        Assert.IsNull(path, "Expected null path when grid is fully blocked between start and end");

        Object.DestroyImmediate(boardGo);
        foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
    }

    [Test]
    public void FindPathWithBFS_PathAppearsAfterRemovingBlock()
    {
        int w = 4, h = 1; // single row for simple path
        var boardGo = new GameObject("board_repair");
        var board = boardGo.AddComponent<BBoard>();

        BTile[,] tiles = new BTile[h, w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var tgo = new GameObject($"tile_{x}_{y}");
                var t = tgo.AddComponent<BTile>();
                t.x = x; t.y = y; t.type = BTileType.Empty; t.attr = BTileAttribute.None;
                tiles[y, x] = t;
            }
        }

        // start (0,0) -> end (2,0); block middle to prevent path
        tiles[0, 0].attr = BTileAttribute.StartPoint;
        tiles[0, 2].attr = BTileAttribute.EndPoint;
        tiles[0, 1].type = BTileType.Block;

        var ttype = typeof(BBoard);
        ttype.GetField("width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, w);
        ttype.GetField("height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, h);
        ttype.GetField("tiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, tiles);

        var path = board.FindPathWithBFS();
        Assert.IsNull(path, "Path should be null when middle tile is blocked");

        // remove block and retry
        tiles[0, 1].type = BTileType.Empty;
        var path2 = board.FindPathWithBFS();
        Assert.IsNotNull(path2, "Path should exist after removing blocking tile");
        Assert.AreEqual(BTileAttribute.StartPoint, path2[0].attr);
        Assert.AreEqual(BTileAttribute.EndPoint, path2[path2.Length - 1].attr);

        Object.DestroyImmediate(boardGo);
        foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
    }
}
