using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;
using UnityEngine;
using static Commons;

public class BBoard : MonoBehaviour
{
    private struct FillBoardTileConfig
    {
        public int BlockCount;
        public int AttackCount;
        public int GuardCount;
        public int SkillCount;
        public int ShieldCount;
    }

    [SerializeField]
    private Transform tileGrid = null;
    [SerializeField]
    private Transform tilePivot = null;
    [SerializeField]
    private SpriteRendererFillAmount timeGauge = null;
    [SerializeField]
    private SpriteRenderer cover = null;

    public BBoardType Type => type;
    public BBoardDrawState DrawState => drawState;
    public int Width => width;
    public int Height => height;
    public List<Vector2Int> PointList => pointList;
    public UnitBase Owner => owner;
    public float TimeRatio => currentTimer.Value / maxTimer.Value;

    private bool isInit = false;
    private bool startTimer = false;
    private BBoardType type = BBoardType.None;
    private BBoardDrawState drawState = BBoardDrawState.None;
    private Vector2Int startPoint = new Vector2Int(-1, -1);
    private Vector2Int endPoint = new Vector2Int(-1, -1);
    private int width = 0;
    private int height = 0;
    private BTile[,] tiles = null; //y, x
    private List<Vector2Int> pointList = new List<Vector2Int>();
    private event System.Action onPathComplete = null;
    private System.Action onBoardTimeOver = null;
    private UnitBase owner = null;
    private ReactiveProperty<float> currentTimer = new ReactiveProperty<float>(0.0f);
    private ReactiveProperty<float> maxTimer = new ReactiveProperty<float>(0.0f);
    private IDisposable disTimer = null;
    private IDisposable disCurTimer = null;
    // reference to owner pool for tile images so we can return them on release
    private SimplePool<BTile> tilePoolRef = null;
    // track Image instances acquired from the pool for this board
    private List<BTile> pooledTiles = new List<BTile>();

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; x++)
            {
                string s = "";

                if (tiles[y, x].attr == BTileAttribute.None)
                {
                    s = $"{(int)tiles[y, x].type}";
                }
                else if (tiles[y, x].attr == BTileAttribute.StartPoint)
                {
                    s = "S";
                }
                else if (tiles[y, x].attr == BTileAttribute.EndPoint)
                {
                    s = "E";
                }

                bool inP = pointList.Contains(new Vector2Int(x, y));

                if (inP)
                {
                    sb.Append("[");
                }

                sb.Append(s);

                if (inP)
                {
                    sb.Append("]");
                }

                sb.Append(" ");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public void SubscribeOnPathComplete(System.Action callback)
    {
        onPathComplete += callback;
    }

    public static BBoard CreateEmptyBoard(BBoardType type, SimplePool<BBoard> pool, SimplePool<BTile> tilePool, Transform attachParent, int width, int height, float maxTimer)
    {
        BBoard board = pool.Get();

        if(board != null)
        {
            board.transform.SetParent(attachParent, false);

            int calcWidth = Mathf.Clamp(width, Commons.BOARD_WIDTH_MIN, Commons.BOARD_WIDTH_MAX);
            int calcHeight = Mathf.Clamp(height, Commons.BOARD_HEIGHT_MIN, Commons.BOARD_HEIGHT_MAX);

            board.isInit = false;
            board.startTimer = false;
            board.type = type;
            board.drawState = BBoardDrawState.None;
            board.startPoint = new Vector2Int(-1, -1);
            board.endPoint = new Vector2Int(-1, -1);
            board.width = calcWidth;
            board.height = calcHeight;
            board.tiles = new BTile[calcHeight, calcWidth];

            // remember tile pool reference for release
            board.tilePoolRef = tilePool;

            float boardW = board.width * Commons.DEFAULT_TILE_WIDTH;
            float boardH = board.height * Commons.DEFAULT_TILE_HEIGHT;
            float wRatio = boardW / Commons.DEFAULT_BOARD_WIDTH;
            float hRatio = boardH / Commons.DEFAULT_BOARD_HEIGHT;

            board.ResizeBoard();

            Bounds boardBound = board.tileGrid.GetComponent<SpriteRenderer>().bounds;
            Vector3 boardMin = board.transform.InverseTransformPoint(boardBound.min);

            float tw = boardBound.size.x / board.width;
            float th = boardBound.size.y / board.height;
            Vector3 boardAnchor = boardMin + new Vector3(tw * 0.5f, th * 0.5f, 0.0f);
            Vector3 gaugePos = board.timeGauge.transform.parent.transform.position;

            gaugePos.x = boardBound.min.x - board.timeGauge.GetComponent<SpriteRenderer>().bounds.size.x * 0.75f;
            board.timeGauge.transform.parent.localScale = new Vector3(0.15f, hRatio, 1.0f);
            board.timeGauge.transform.parent.transform.position = gaugePos;
            
            board.timeGauge.SetHorizontal(false);

            // Create children in row-major order (y outer, x inner) so indexing y*width + x matches
            for (int y = 0; y < calcHeight; y++)
            {
                for (int x = 0; x < calcWidth; x++)
                {
                    // get tile from pool; if null, create a fallback Image to ensure grid consistency
                    BTile tile = tilePool.Get();

                    if (tile != null)
                    {
                        SpriteRenderer spr = tile.GetComponent<SpriteRenderer>();

                        float tx = boardAnchor.x + x * tw;
                        float ty = boardAnchor.y + y * th;
                        
                        tile.type = BTileType.Empty;
                        tile.attr = BTileAttribute.None;
                        tile.x = x;
                        tile.y = y;

                        tile.transform.SetParent(board.tilePivot, false);

                        tile.transform.localPosition = new Vector2(tx, ty);

                        board.SetTile(x, y, ResourceManager.Instance.GetResource<Sprite>(Commons.ResKey_BaseTile));
                        // track pooled tile so we can release it later
                        board.pooledTiles.Add(tile);

                        board.tiles[y, x] = tile;
                    }
                    else
                    {
                        Debug.LogError("CreateEmptyBoard: failed to get tile from pool - creating fallback Image");
                        GameObject go = new GameObject($"Tile_{x}_{y}", typeof(Transform), typeof(BTile));
                        SpriteRenderer spr = go.GetComponent<SpriteRenderer>();
                        spr.sprite = ResourceManager.Instance.GetResource<Sprite>(Commons.ResKey_BaseTile);
                        go.transform.SetParent(board.tilePivot.transform, false);
                    }
                }
            }

            board.disCurTimer = board.currentTimer.Subscribe((v) => board.timeGauge.SetFillAmount(1.0f - board.TimeRatio));

            board.maxTimer.Value = maxTimer;
            board.currentTimer.Value = 0.0f;
        }
        
        return board;
    }

    public void SetOwner(UnitBase unit)
    {
        owner = unit;
    }

    public void InitBoard(uint[] skillList = null)
    {
        if (isInit)
            return;

        //FillBoard(type, skillList);
        OnOffCover(true);

        isInit = true;
    }

    public void OnOffCover(bool onoff)
    {
        cover.gameObject.SetActive(onoff);
    }

    public void PlayFadeCover()
    {
        cover.DOFade(0.0f, 1.0f).OnComplete(()=>OnOffCover(false)).Play();
    }

    public void FillBoard(BBoardType boardType, uint[] skillList = null)
    {
        //offensive -> block, attack, skill, shield
        //defensive -> block, guard, skill
        type = boardType;

        FillBoardTileConfig config = default;

        switch (type)
        {
            case BBoardType.Offensive:
                config = new FillBoardTileConfig()
                {
                    BlockCount = UnityEngine.Random.Range(0, (int)(width * height * 0.15f)),
                    AttackCount = UnityEngine.Random.Range(1, (int)(width * height * 0.25f)),
                    GuardCount = 0,
                    SkillCount = skillList != null ? UnityEngine.Random.Range(1, skillList.Length) : 0,
                    ShieldCount = UnityEngine.Random.Range(1, (int)(width * height * 0.25f)),
                };
                break;
            case BBoardType.Defensive:
                config = new FillBoardTileConfig()
                {
                    BlockCount = UnityEngine.Random.Range(0, (int)(width * height * 0.15f)),
                    AttackCount = 0,
                    GuardCount = UnityEngine.Random.Range(1, (int)(width * height * 0.15f)),
                    SkillCount = 0,
                    ShieldCount = 0,
                };
                break;
            default:
                Debug.LogError($"none board type!");
                break;
        }

        // Use Perlin noise to create clustered distributions of tile types
        SetStartEndPoint();

        // collect candidate cells excluding start/end
        List<(int x, int y, float noise)> cells = new List<(int, int, float)>();

        float scale = Mathf.Clamp01(1.0f / Mathf.Max(4, Mathf.Min(width, height))) * 1.5f; // scale relative to board size
        float ox = UnityEngine.Random.Range(0f, 1000f);
        float oy = UnityEngine.Random.Range(0f, 1000f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if ((x == startPoint.x && y == startPoint.y) || (x == endPoint.x && y == endPoint.y))
                    continue;

                float nx = (x + ox) * scale;
                float ny = (y + oy) * scale;
                float n = Mathf.PerlinNoise(nx, ny);
                cells.Add((x, y, n));
            }
        }

        // sort descending so high-noise areas cluster
        cells.Sort((a, b) => b.noise.CompareTo(a.noise));

        int total = cells.Count;
        int idx = 0;

        FillTile(config, cells, ref idx, BTileType.Block, Commons.ResKey_BlockTile);
        FillTile(config, cells, ref idx, BTileType.Attack, Commons.ResKey_AttackTile);
        FillTile(config, cells, ref idx, BTileType.Guard, Commons.ResKey_GuardTile);
        FillTile(config, cells, ref idx, BTileType.Skill, Commons.ResKey_SkillTile, skillList); //skill
        FillTile(config, cells, ref idx, BTileType.Shield, Commons.ResKey_ShieldTile);

        // After placement, ensure there is at least one path between start and end.
        // If not, try to repair by removing random block tiles until a path is found.

        // validate path existence
        BTile[] pathCheck = FindPathWithBFS();
        if (pathCheck == null)
        {
            const int MAX_REPAIR_ATTEMPTS = 200;
            int repairAttempts = 0;

            // Helper: find a path that minimizes the number of block tiles traversed using 0-1 BFS
            System.Func<List<Vector2Int>> FindMinBlockPath = () =>
            {
                int INF = 1000000000;
                int[,] dist = new int[height, width];
                int[,] px = new int[height, width];
                int[,] py = new int[height, width];

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        dist[i, j] = INF;
                        px[i, j] = -1;
                        py[i, j] = -1;
                    }
                }

                var deque = new LinkedList<Vector2Int>();
                dist[startPoint.y, startPoint.x] = 0;
                deque.AddFirst(new Vector2Int(startPoint.x, startPoint.y));

                int[] dx = new int[4] { 1, -1, 0, 0 };
                int[] dy = new int[4] { 0, 0, 1, -1 };

                while (deque.Count > 0)
                {
                    var cur = deque.First.Value;
                    deque.RemoveFirst();

                    if (cur.x == endPoint.x && cur.y == endPoint.y)
                        break;

                    for (int k = 0; k < 4; k++)
                    {
                        int nx = cur.x + dx[k];
                        int ny = cur.y + dy[k];

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;

                        int w = (tiles[ny, nx].type == BTileType.Block) ? 1 : 0;
                        if (dist[ny, nx] > dist[cur.y, cur.x] + w)
                        {
                            dist[ny, nx] = dist[cur.y, cur.x] + w;
                            px[ny, nx] = cur.x;
                            py[ny, nx] = cur.y;

                            if (w == 0)
                                deque.AddFirst(new Vector2Int(nx, ny));
                            else
                                deque.AddLast(new Vector2Int(nx, ny));
                        }
                    }
                }

                if (dist[endPoint.y, endPoint.x] == INF)
                    return null;

                // reconstruct path
                List<Vector2Int> path = new List<Vector2Int>();
                int cx = endPoint.x, cy = endPoint.y;
                while (!(cx == startPoint.x && cy == startPoint.y))
                {
                    path.Add(new Vector2Int(cx, cy));
                    int tx = px[cy, cx];
                    int ty = py[cy, cx];
                    if (tx == -1)
                        break;
                    cx = tx; cy = ty;
                }

                path.Add(new Vector2Int(startPoint.x, startPoint.y));
                path.Reverse();
                return path;
            };

            List<Vector2Int> minPath = FindMinBlockPath();

            while (pathCheck == null && repairAttempts < MAX_REPAIR_ATTEMPTS && minPath != null && minPath.Count > 0)
            {
                // remove blocks that appear on the minimal-block path, from start->end order
                bool removedAny = false;
                foreach (var p in minPath)
                {
                    if (tiles[p.y, p.x].type == BTileType.Block)
                    {
                        tiles[p.y, p.x].type = BTileType.Empty;
                        SetTile(p.x, p.y, ResourceManager.Instance.GetResource<Sprite>(Commons.ResKey_BaseTile));
                        SetTileColor(p.x, p.y, Color.white);
                        repairAttempts++;
                        removedAny = true;

                        // after each removal, check if path exists without blocks
                        pathCheck = FindPathWithBFS();
                        if (pathCheck != null || repairAttempts >= MAX_REPAIR_ATTEMPTS)
                            break;
                    }
                }

                if (!removedAny)
                {
                    // No blocks on min path (shouldn't happen), break to avoid infinite loop
                    break;
                }

                if (pathCheck != null)
                    break;

                // recompute minimal block path after removals
                minPath = FindMinBlockPath();
            }

            if (pathCheck == null)
            {
                Debug.LogError($"FillBoard: unable to ensure path after {repairAttempts} repair attempts (min-block strategy)");
            }
            else
            {
                Debug.Log($"FillBoard: repaired path after {repairAttempts} block removals (min-block strategy)");
            }
        }

        drawState = BBoardDrawState.None;
    }

    public void ReleaseBoard()
    {
        isInit = false;
        startTimer = false;
        type = BBoardType.None;
        drawState = BBoardDrawState.None;
        startPoint = new Vector2Int(-1, -1);
        endPoint = new Vector2Int(-1, -1);
        width = 0;
        height = 0;

        // release tile images back to their pool
        if (tilePoolRef != null)
        {
            foreach (var img in pooledTiles)
            {
                if (img != null)
                {
                    tilePoolRef.Release(img);
                }
            }

            pooledTiles.Clear();
            tilePoolRef = null;
        }

        disCurTimer?.Dispose();

        ForceStopBoardTimer();
        ClearDrawLine();
        SetOwner(null);
        OnOffCover(true);

        tiles = null;
    }

    public void ClearBoard()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tiles[y, x].type = BTileType.Empty;
                tiles[y, x].attr = BTileAttribute.None;

                SetTile(x, y, ResourceManager.Instance.GetResource<Sprite>(Commons.ResKey_BaseTile));
                SetTileColor(x, y, Color.white);
            }
        }

        startTimer = false;
    }

    public void ClearDrawLine()
    {
        pointList.Clear();
    }

    public void ResetDrawLine()
    {
        if (drawState == BBoardDrawState.Drawing)
        {
            drawState = BBoardDrawState.None;
            // reset tile colors along the current path
            foreach (Vector2Int p in pointList)
            {
                if (tiles[p.y, p.x].attr == BTileAttribute.StartPoint || tiles[p.y, p.x].attr == BTileAttribute.EndPoint)
                {
                    continue;
                }
                else
                {
                    SetTileColor(p.x, p.y, Color.white);
                }
            }

            ClearDrawLine();
            pointList.Add(startPoint);
            SetTileColor(startPoint.x, startPoint.y, Color.green);
            SetTileColor(endPoint.x, endPoint.y, Color.red);
        }
    }

    public bool TryGetTile(int x, int y, out BTile tile)
    {
        tile = null;
        
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false;
        }
        
        tile = tiles[y, x];
        
        return true;
    }

    public BTile[] FindPathWithBFS()
    {
        int sx = -1, sy = -1, ex = -1, ey = -1;

        // locate start and end (tiles[y,x])
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (tiles[y, x].attr == BTileAttribute.StartPoint)
                {
                    sx = x; sy = y;
                }
                else if (tiles[y, x].attr == BTileAttribute.EndPoint)
                {
                    ex = x; ey = y;
                }
            }
        }

        if (sx == -1 || ex == -1)
            return null;

        bool[,] visited = new bool[height, width];
        int[,] prevX = new int[height, width];
        int[,] prevY = new int[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                prevX[y, x] = -1;
                prevY[y, x] = -1;
            }
        }

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(sx, sy));
        visited[sy, sx] = true;

        bool found = false;

        int[] dx = new int[4] { 1, -1, 0, 0 };
        int[] dy = new int[4] { 0, 0, 1, -1 };

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();

            if (cur.x == ex && cur.y == ey)
            {
                found = true;
                break;
            }

            for (int i = 0; i < 4; i++)
            {
                int nx = cur.x + dx[i];
                int ny = cur.y + dy[i];

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                if (visited[ny, nx])
                    continue;

                BTile ntile = tiles[ny, nx];

                // cannot pass through blocks
                if (ntile.type == BTileType.Block)
                    continue;

                visited[ny, nx] = true;
                prevX[ny, nx] = cur.x;
                prevY[ny, nx] = cur.y;
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }

        if (!found)
            return null;

        // reconstruct path
        List<BTile> path = new List<BTile>();
        int cx = ex, cy = ey;

        while (!(cx == sx && cy == sy))
        {
            path.Add(tiles[cy, cx]);
            int px = prevX[cy, cx];
            int py = prevY[cy, cx];
            if (px == -1)
            {
                // something went wrong
                return null;
            }
            cx = px; cy = py;
        }

        // add start
        path.Add(tiles[sy, sx]);
        path.Reverse();

        return path.ToArray();
    }

    public void DoMove(int dirX, int dirY)
    {
        if(drawState == BBoardDrawState.Finished)
        {
            //Debug.Log("DoMove: already finished, cannot move");
            return;
        }

        if(!startTimer)
        {
            //Debug.Log("locked board");
            return;
        }

        if(currentTimer.Value >= maxTimer.Value)
        {
            //Debug.Log("DoMove : time over");
            return;
        }

        drawState = BBoardDrawState.Drawing;

        Vector2Int lastPoint = pointList.Last();
        int newX = lastPoint.x + dirX;
        int newY = lastPoint.y + dirY;

        if (newX < 0 || newX >= width || newY < 0 || newY >= height)
        {
            //Debug.Log($"DoMove: cannot move out of bounds to ({newX},{newY})");
            return;
        }

        BTile nextTile = tiles[newY, newX];

        // cannot move to block
        if (nextTile.type == BTileType.Block)
        {
            //Debug.Log($"DoMove: cannot move to block tile at ({newX},{newY})");
            return;
        }

        if (pointList.Contains(new Vector2Int(newX, newY)))
        {
            //Debug.Log($"DoMove: cannot move to already visited tile at ({newX},{newY})");
            return;
        }

        pointList.Add(new Vector2Int(newX, newY));
        SetTileColor(newX, newY, Color.green);

        if(newX == endPoint.x && newY == endPoint.y)
        {
            drawState = BBoardDrawState.Finished;
            onPathComplete?.Invoke();
            //Debug.Log("DoMove: reached the end point!");
        }
    }

    public void UnDo()
    {
        if (drawState == BBoardDrawState.Finished)
        {
            //Debug.Log("DoMove: already finished, cannot move");
            return;
        }

        if (!startTimer)
        {
            //Debug.Log("locked board");
            return;
        }

        if (currentTimer.Value >= maxTimer.Value)
        {
            //Debug.Log("DoMove : time over");
            return;
        }

        if (pointList.Count > 1)
        {
            drawState = BBoardDrawState.Drawing;

            Vector2Int lastPoint = pointList.Last();

            pointList.RemoveAt(pointList.Count - 1);

            SetTileColor(lastPoint.x, lastPoint.y, Color.white);
        }
    }

    public void SetDirection(int dirX,int dirY)
    {
        if(pointList.Count > 1)
        {
            int nx = pointList.Last().x + dirX;
            int ny = pointList.Last().y + dirY;

            if(pointList.ElementAt(pointList.Count - 2).x == nx && pointList.ElementAt(pointList.Count - 2).y == ny)
            {
                UnDo();
            }
            else
            {
                DoMove(dirX, dirY);
            }
        }
        else
        {
            DoMove(dirX, dirY);
        }

        //test print
        //ToString();
    }

    public Vector3Int GetInvertY(int x, int y)
    {
        return new Vector3Int(x, height - 1 - y, 0);
    }

    public void SetTile(int x, int y, Sprite tile)
    {
        if (0 <= x && x < width && 0 <= y && y < height)
        { 
            int idx = y * width + x;
            
            if(idx < tilePivot.transform.childCount)
            {
                tilePivot.transform.GetChild(idx).GetComponent<SpriteRenderer>().sprite = tile;
            }
        }
    }
    
    public void SetTileColor(int x, int y, Color col)
    {
        if (0 <= x && x < width && 0 <= y && y < height)
        {
            int idx = y * width + x;

            if (idx < tilePivot.transform.childCount)
            {
                tilePivot.transform.GetChild(idx).GetComponent<SpriteRenderer>().color = col;
            }
        }
    }

    public ApplyStatusData GetApplyStatusFromPath()
    {
        ApplyStatusData result = (ApplyStatusData)default;

        if (drawState == BBoardDrawState.Finished)
        {
            for (int i = 0; i < pointList.Count; i++)
            {
                Vector2Int p = pointList[i];
                BTile t = tiles[p.y, p.x];

                if (t.type == BTileType.Attack)
                {
                    result.Atk += 1f;
                }
                else if (t.type == BTileType.Guard)
                {
                    result.Def += 1f;
                }
                else if(t.type == BTileType.Shield)
                {
                    result.ShieldCrushTime += 1f;
                }
            }

            result.Dodge = pointList.Count * 10.0f;
        }

        return result;
    }

    public void StartBoardTimer(System.Action timeOverAct = null)
    {
        startTimer = true;
        currentTimer.Value = 0.0f;
        onBoardTimeOver = timeOverAct;

        disTimer?.Dispose();
        disTimer = Observable.Interval(System.TimeSpan.FromSeconds(0.1))
            .Where(_ => currentTimer.Value < maxTimer.Value)
            .Subscribe(_ =>
            {
                currentTimer.Value += 0.1f;

                if (currentTimer.Value >= maxTimer.Value)
                {
                    startTimer = false;
                    onBoardTimeOver?.Invoke();
                    disTimer?.Dispose();
                }
            });
    }

    public void ForceBoardTimeOver()
    {
        onBoardTimeOver?.Invoke();
    }

    public void ForceStopBoardTimer()
    {
        disTimer?.Dispose();
        disTimer = null;

        startTimer = false;
        currentTimer.Value = maxTimer.Value;
    }

    public List<(BTileType type, uint idx)> GetTileTypeListInPath()
    {
        List<(BTileType type, uint idx)> result = pointList.Select((s) => { return (tiles[s.y, s.x].type, tiles[s.y,s.x].skillIdx); }).ToList();

        return result;
    }

    private void ResizeBoard()
    {
        float wRatio = (float)width / (float)Commons.BOARD_WIDTH_MIN;
        float hRatio = (float)height / (float)Commons.BOARD_HEIGHT_MIN;
        Vector3 resizeScale = new Vector3(wRatio, hRatio, 1f);

        tileGrid.transform.localScale = resizeScale;
        cover.transform.localScale = resizeScale;
    }

    private bool IsAround(Vector2Int center, Vector2Int pos)
    {
        Vector2Int[] arounds = new Vector2Int[]
        {
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(-1, 0),                        new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        };

        bool res = false;

        for (int i = 0; i < arounds.Length; ++i)
        {
            Vector2Int tp = center + arounds[i];

            if (tp.x == pos.x && tp.y == pos.y)
            {
                res = true;
                break;
            }
        }

        return res;
    }

    private void FillTile(FillBoardTileConfig config, List<(int x, int y, float noise)> cells, ref int idx, BTileType tileType, string resKey_Tile, uint[] skillList = null)
    {
        int total = cells.Count;

        int tileCount = tileType switch
        {
            BTileType.Empty => 0,
            BTileType.Block => config.BlockCount,
            BTileType.Attack => config.AttackCount,
            BTileType.Guard => config.GuardCount,
            BTileType.Skill => config.SkillCount,
            BTileType.Shield => config.ShieldCount,

            _ => throw new NotImplementedException()
        };

        // clamp counts to available cells
        tileCount = Mathf.Min(tileCount, total - idx);

        for (int i = 0; i < tileCount && idx < total; i++, idx++)
        {
            var c = cells[idx];
            Vector2Int cellPos = new Vector2Int(c.x, c.y);

            if (tileType == BTileType.Block)
            {
                if (IsAround(startPoint, cellPos) || IsAround(endPoint, cellPos))
                    continue;
            }

            tiles[c.y, c.x].type = tileType;
            SetTile(c.x, c.y, ResourceManager.Instance.GetResource<Sprite>(resKey_Tile));

            if (tileType == BTileType.Skill && skillList != null)
            {
                uint skillIdx = skillList[UnityEngine.Random.Range(0, skillList.Length)];
                tiles[c.y, c.x].skillIdx = skillIdx; //009001 

                string ht = $"#ff{0:00}{skillIdx}";

                if (ColorUtility.TryParseHtmlString(ht, out var col))
                    SetTileColor(c.x, c.y, col);
            }
        }
    }

    private void SetStartEndPoint()
    {
        System.Func<bool> actInitStartEnd = () =>
        {
            // 1. 시작점 랜덤 선택
            int sx = Util.GetRandom(0, width, null);
            int sy = Util.GetRandom(0, height, null);

            if (tiles == null || tiles[sy, sx] == null) 
                return false;

            if (tiles[sy, sx].type == BTileType.Block) 
                return false; // 블록이면 실패

            // 2. 종점 선택 (시작점과 같지 않고, 주변(isAround)도 아닌 곳)
            int ex = -1, ey = -1;
            bool found = false;

            // 후보군을 좁히기 위해 전체 순회하며 조건 체크
            List<Vector2Int> candidates = new List<Vector2Int>();
            Vector2Int startPos = new Vector2Int(sx, sy);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);

                    // 조건: 블록이 아님 && 시작점이 아님 && 시작점과 인접하지 않음
                    if (tiles[y, x].type != BTileType.Block &&
                        currentPos != startPos &&
                        !IsAround(startPos, currentPos))
                    {
                        candidates.Add(currentPos);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                Vector2Int endPos = candidates[Util.GetRandom(0, candidates.Count, null)];
                ex = endPos.x;
                ey = endPos.y;
                found = true;
            }

            if (!found)
            {
                //Debug.Log("InitBoard: failed to find valid Start/End pair");
                return false;
            }

            tiles[sy, sx].attr = BTileAttribute.StartPoint;
            tiles[ey, ex].attr = BTileAttribute.EndPoint;

            //Debug.Log($"InitBoard: Start=({sx},{sy}) End=({ex},{ey})");
            return true;
        };

        bool placed = actInitStartEnd();
        BTile[] findPath = null;

        if (placed)
            findPath = FindPathWithBFS();

        int attempts = 0;
        const int MAX_ATTEMPTS = 100;

        while (findPath == null && attempts++ < MAX_ATTEMPTS)
        {
            bool repl = actInitStartEnd();

            if (repl)
                findPath = FindPathWithBFS();
        }

        if (findPath == null)
        {
            Debug.LogError($"InitBoard: failed to find path after {MAX_ATTEMPTS} attempts (width:{width},height:{height})");
            // leave board uninitialized to avoid infinite loop; caller can handle this case
            return;
        }

        startPoint = new Vector2Int(findPath[0].x, findPath[0].y);
        endPoint = new Vector2Int(findPath[findPath.Length - 1].x, findPath[findPath.Length - 1].y);

        pointList.Add(startPoint);

        tiles[startPoint.y, startPoint.x].attr = BTileAttribute.StartPoint;
        tiles[endPoint.y, endPoint.x].attr = BTileAttribute.EndPoint;

        SetTileColor(startPoint.x, startPoint.y, Color.green);
        SetTileColor(endPoint.x, endPoint.y, Color.red);
    }

}
