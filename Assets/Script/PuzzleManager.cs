﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : DragablePlane
{
    public Puzzle puzzle;


    /// <summary>
    /// 第一块 拼图 的索引
    /// </summary>
    public int firstPieceIndex = 3;


    /// <summary>
    /// 拼图 预制体
    /// </summary>
    public GameObject piecePrefab;

    /// <summary>
    /// 拼图 图像
    /// </summary>
    public Sprite pieceImage;

    /// <summary>
    /// 拼图的 形状
    /// </summary>
    public Texture markImage;

    /// <summary>
    /// 自动连接 的最大 距离
    /// </summary>
    public float largestSize = 30.0f;


    /// <summary>
    /// 计算显示 的 标准尺寸
    /// </summary>
    public const float displayX = 640, displayY = 480;


    /// <summary>
    /// 拼图的标准缩放值
    /// </summary>
    public Vector2 pieceScale;

    /// <summary>
    /// 拼图和标准尺寸缩放比率
    /// </summary>
    public Vector2 displayRatio = new Vector2(1, 1);

    /// <summary>
    /// 每块拼图的像素大小
    /// </summary>
    public Vector2 pieceSize;

    /// <summary>
    /// 每块拼图的实际显示的大小
    /// </summary>
    public Vector2 displaySize;


    /// <summary>
    /// 拼图是否已经创建
    /// </summary>
    public bool pieceCreated;


    /// <summary>
    /// 碰撞测试的 缓存大小
    /// </summary>
    protected override int raycastHitCacheSize
    {
        // 总的 拼图个数
        get { return puzzle.totalCount; }
    }

    protected override void Awake()
    {

        // 加载拼图预制体
        if(piecePrefab == null)
            piecePrefab = Resources.Load("Piece") as GameObject;

        childLayer = piecePrefab.layer;

        if (puzzle == null)
            puzzle = new Puzzle(this);

        base.Awake();

    }




    /// <summary>
    /// 获取 第 (x,y) 个拼图，下标从 0 开始
    /// 原点在 左下角
    /// </summary>
    /// <param name="x">第 x 列 </param>
    /// <param name="y">第 y 列 </param>
    /// <returns></returns>
    public GameObject GetPiece(int x, int y)
    {
        // 找到 拼图 的索引
        int index = firstPieceIndex + x * puzzle.count.y + y;

        // 如果 小于 当前 已有的拼图个数，直接返回
        if (index < transform.childCount)
            return transform.GetChild(index).gameObject;

        // 创建一个新的拼图实例
        return Instantiate(piecePrefab, transform);
    }




    /// <summary>
    /// 清理现有的拼图
    /// </summary>
    public void ClearPiece()
    {
        // 如果没有生成拼图，返回
        if (!pieceCreated) return;

        // 遍历所有拼图对象
        for (int i = 0; i < puzzle.count.x; i++)
            for (int j = 0; j < puzzle.count.y; j++)
            {
                // 隐藏 拼图对象
                GetPiece(i, j).SetActive(false);
            }

        // 拼图已经清理
        pieceCreated = false;
    }


    #region 重载基类函数
    /// <summary>
    /// 激活拼图对象
    /// </summary>
    /// <param name="go">激活的拼图对象</param>
    protected override void ActiveObject(GameObject go)
    {
        // 通知 激活的拼图 
        go.GetComponent<Piece>().OnSelect();
    }

    /// <summary>
    /// 取消激活对象
    /// </summary>
    /// <param name="go">取消激活拼图对象</param>
    protected override void DeactiveObject(GameObject go)
    {
        // 拼图移动结束
        puzzle.MoveEnd(go);
    }

    /// <summary>
    /// 移动拼图
    /// </summary>
    /// <param name="go">拼图对象</param>
    /// <param name="delta">移动的距离</param>
    protected override void MoveObject(GameObject go, Vector3 delta)
    {
        // 通知 拼图 移动
        puzzle.Move(go, delta);
    }

    /// <summary>
    /// 碰撞测试的 优先值
    /// </summary>
    /// <param name="go">拼图对象</param>
    /// <returns>拼图对象的 优先值</returns>
    protected override int RaycastHitOrder(GameObject go)
    {
        // 返回 拼图对象的 优先值
        return go.GetComponent<Piece>().order;
    }



    #endregion

    /// <summary>
    /// 生成拼图
    /// </summary>
    protected void MakePuzzle()
    {

        // 设置 拼图 的最大 优先值
        Piece.maxDepth = puzzle.totalCount + 1;

        // 生成 （x,y) 个 拼图块
        for (int i = 0; i < puzzle.count.x; i++)
        {
            for (int j = 0; j < puzzle.count.y; j++)
            {
                // 创建 第 （ x,y ) 个拼图
                CreatePiece(i, j);
            }
        }

        // 检查 拼图的 显示
        CheckPiece();


        // 重置拼图
        puzzle.Reset();

        // 拼图已经创建
        pieceCreated = true;
    }






    /// <summary>
    /// 重设拼图的大小
    /// </summary>
    protected void ReSize()
    {
        // 如果 拼图对象为空，返回
        if (pieceImage == null) return;

        // 适配 图像 的 宽高比
        transform.localScale = new Vector3(
            pieceImage.texture.width / (pieceImage.texture.height / displayY) / displayX,
            1, 1);

        // 更新 图像大小
        pieceSize = new Vector2(pieceImage.texture.width / puzzle.count.x, pieceImage.texture.height / puzzle.count.y);

        // 更新 拼图缩放比率
        displayRatio.x = displayX / pieceImage.texture.width;
        displayRatio.y = displayY / pieceImage.texture.height;

        // 更新 拼图显示大小
        displaySize.x = displayRatio.x * pieceSize.x * transform.localScale.x;
        displaySize.y = displayRatio.y * pieceSize.y;

        // 更新 拼图标准缩放值
        pieceScale.x = 1 / (float)puzzle.count.x;
        pieceScale.y = 1 / (float)puzzle.count.y;
    }

    /// <summary>
    /// 创建 第 (x,y) 个拼图
    /// </summary>
    /// <param name="x">第 x 列</param>
    /// <param name="y">第 y 行</param>
    void CreatePiece(int x, int y)
    {
        // 获取 当前 位置的 拼图对象
        GameObject child = GetPiece(x, y);

        // 激活对象
        child.SetActive(true);

        // 初始化 拼图
        Piece piece = child.GetComponent<Piece>();
        if (piece == null) piece = child.AddComponent<Piece>();

        piece.puzzle = puzzle;

        piece.Init(x, y);


        // 设置材质
        Vector2 offset = pieceScale / 2f;

        // 获取图像渲染对象
        SpriteRenderer rend = child.GetComponent<SpriteRenderer>();

        // 设置图像 uv
        rend.material.mainTextureScale = pieceScale * 2;
        rend.material.mainTextureOffset =
            new Vector2(x * pieceScale.x - offset.x, y * pieceScale.y - offset.y);

        // 设置拼图图像
        rend.sprite = pieceImage;

        // 设置拼图形状
        rend.material.SetTexture("_MarkTex", markImage);

        // 随机位置
        child.transform.position = new Vector3(
                                        Random.Range(-0.15f, 0.15f) * collider.size.x,
                                        Random.Range(-0.15f, 0.15f) * collider.size.y,
                                        0);
    }

    /// <summary>
    /// 检查 拼图的 显示
    /// </summary>
    void CheckPiece()
    {
        // 上边界
        // 如果 拼图个数 y 是奇数，反转  
        if (puzzle.count.y % 2 == 1)
        {
            Debug.Log("fix puzzle at top edge.");

            // 左上角
            Transform leftTop = GetPiece(0, puzzle.count.y - 1).transform;

            // 反转 Y 轴显示
            leftTop.localScale = new Vector3(leftTop.localScale.x, -leftTop.localScale.y, 1);

            // 设置成 左下角的形状
            leftTop.GetComponent<Renderer>().material.SetTextureOffset("_MarkTex", new Vector2(0, 0));


            // 遍历 上边界的其它拼图，不包括 右上角
            for (int i = 1; i < puzzle.count.x - 1; i++)
            {
                // 获取拼图对象
                GameObject piece = GetPiece(i, puzzle.count.y - 1);

                // 反转 形状
                piece.GetComponent<Renderer>().material.SetTextureOffset("_MarkTex", new Vector2(0.5f, i % 2 == 1 ? 0.5f : 0.75f));
            }
        }

        // 右边界
        // 如果 拼图个数 x 是奇数，反转  
        if (puzzle.count.x % 2 == 1)
        {
            Debug.Log("fix puzzle at right edge.");

            // 右下角
            Transform rightBottom = GetPiece(puzzle.count.x - 1, 0).transform;

            // 反转 Y 轴显示
            rightBottom.localScale = new Vector3(rightBottom.localScale.x, -rightBottom.localScale.y, 1);

            // 设置成 右上角的形状
            rightBottom.GetComponent<Renderer>().material.SetTextureOffset("_MarkTex", new Vector2(0, 0.75f));

            // 遍历 右边界的其它拼图，不包括 右上角
            for (int i = 1; i < puzzle.count.y - 1; i++)
            {
                // 获取拼图对象
                GameObject piece = GetPiece(puzzle.count.x - 1, i);

                // 反转 形状
                piece.GetComponent<Renderer>().material.SetTextureOffset("_MarkTex", new Vector2(0.25f, i % 2 == 1 ? 0.5f : 0.75f));
            }

        }

        // 右上角
        // 只有 x, 和 y 其中一个是奇数时，才反转
        if ((puzzle.count.x + puzzle.count.y) % 2 == 1)
        {
            Debug.Log("fix right top corner");

            // 获取拼图对象
            Transform piece = GetPiece(puzzle.count.x - 1, puzzle.count.y - 1).transform;

            // 反转 Y 轴显示
            piece.localScale = new Vector3(piece.localScale.x, -piece.localScale.y, 1);

            // 设置成 右下角的形状
            piece.GetComponent<Renderer>().material.SetTextureOffset("_MarkTex", new Vector2(0, 0.5f));

        }
    }

}
