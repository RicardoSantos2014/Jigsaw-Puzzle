﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 这个脚本用于和UI元素交互
/// </summary>
public class PuzzleGame : Puzzle
{


    [Header("旋转")]

    /// <summary>
    /// 拼图最少的旋转个数
    /// </summary>
    public int minCount = 1;

    /// <summary>
    /// 拼图最少的旋转个数
    /// </summary>
    public int maxCount = 5;

    /// <summary>
    /// 是否旋转
    /// </summary>
    public static bool isRotate;


    /// <summary>
    /// 用户新的 “是否旋转” 选择
    /// </summary>
    bool newIsRotate;

    /// <summary>
    /// 平铺拼图的 起始 位置
    /// </summary>
    Vector3 tileOrigin;


    /// <summary>
    /// 是否显示所有拼图
    /// </summary>
    bool isShowAll;



    /// <summary>
    /// 保存随机数的缓存
    /// </summary>
    List<int> randomBuffer = new List<int>();

    /// <summary>
    /// 新的拼图块数
    /// </summary>
    Vector2 newPieceCount;


    /// <summary>
    /// 游戏是否结束
    /// </summary>
    bool gameFinish;

    /// <summary>
    /// 移动的次数
    /// </summary>
    public int moveCount = 0;

    /// <summary>
    /// 记录开始的时间
    /// </summary>
    public float gameTime = 0;

    /// <summary>
    /// 记录信息
    /// </summary>
    public Record record;


    GameObject originImage;

    public bool needRestart = false;

    public List<EventDelegate> onGameEnd = new List<EventDelegate>();


    #region virtual function


    /// <summary>
    /// 初始化
    /// </summary>
    protected override void Start()
    {
        base.Start();

        // 停止 时间更新
        Time.timeScale = 0f;

        originImage = transform.GetChild(0).gameObject;

        //获取平铺的起始位置
        tileOrigin = transform.GetChild(1).position;

    }

    /// <summary>
    /// 固定时间间隔执行
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // 如果 已经开始移动，并且游戏没有结束，就更新并显示时间
        if (moveCount > 0 && !gameFinish)
            gameTime += Time.fixedDeltaTime;
    }

    protected override void ActiveObject(GameObject go)
    {
        base.ActiveObject(go);

        moveCount++;
    }

    protected override void DeactiveObject(GameObject go)
    {
        base.DeactiveObject(go);

        if (gameFinish) return;

        // 是否和所有块相连
        Piece piece = go.GetComponent<Piece>();
        if (piece.connectedPieces.Count == (int)(pieceCount.x * pieceCount.y) - 1)
        {
            Time.timeScale = 0;

            needRestart = true;

            record = new Record((int)(pieceCount.x * pieceCount.y), moveCount, gameTime, isRotate);

            gameTime = 0;
            for (int i = 0; i < onGameEnd.Count; i++)
                onGameEnd[i].Execute();
        }
    }

    #endregion

    #region public function

    #region game
    public void StartGame()
    {
        if (!needRestart && pieceCreated)
        {
            Continue();
            return;
        }

        ClearPiece();

        if (pieceCount != newPieceCount)
        {
            pieceCount = newPieceCount;
            ReSize();
        }

        MakePuzzle();
        ShowAllOrNot(isShowAll);
        RotatePiece();

        Piece.theFirstRun = false;

        moveCount = 0;
        gameFinish = false;

        Time.timeScale = 1f;

        needRestart = false;
    }

    public void Pause()
    {
        Time.timeScale = 0f;
    }

    public void Continue()
    {
        Time.timeScale = 1f;
    }
    #endregion

    #region piece

    public void TilePiece()
    {
        if (!pieceCreated) return;

        BuildRandomBuffer(firstPieceIndex, firstPieceIndex + pieceTotalCount);

        int maxVCount = (int)(pieceCount.y * 1.2f);

        int count = 0;
        for (int i = 0; i < pieceTotalCount; i++)
        {
            GameObject child = transform.GetChild(randomBuffer[i]).gameObject;
            if (!child.activeSelf) continue;
            if (child.GetComponent<Piece>().connectedPieces.Count == 0)
            {

                // 移动到新位置
                int x = count / maxVCount;
                int y = count % maxVCount;

                child.transform.position = tileOrigin +
                    new Vector3(x * displaySize.x * 1.2f,
                                y * displaySize.y * 1.2f, 0);

                count++;
            }
        }
    }



    public void SetPieceCount(Vector2 count)
    {
        newPieceCount = count;

        // 游戏是否未开始
        if (!pieceCreated)
        {
            pieceCount = newPieceCount;
            ReSize();
            return;
        }

        needRestart = newPieceCount != pieceCount;
    }

    public void SetPieceShape(string name)
    {
        string markName = "Image/puzzle mark/" + name;
        // 是否包含风格
        if (markImage != null && markImage.name.IndexOf('-') != -1)
            markName += "-" + markImage.name.Split('-')[1];

        markImage = Resources.Load<Texture>(markName);
        if (pieceCreated) UpdatePieceMark();
    }


    public void ToggleRotate(bool rotate)
    {
        newIsRotate = rotate;
        if (!pieceCreated)
        {
            isRotate = newIsRotate;
            return;
        }

        needRestart = newIsRotate != isRotate;

    }

    public void SetPieceImage(string name)
    {
        pieceImage = Resources.Load<Sprite>("Image/" + name);
        transform.GetChild(0).gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", pieceImage.texture);

        ReSize();

        if (pieceCreated) UpdatePieceImage();

    }

    public void SetPieceStyle(string name)
    {
        if (markImage == null) return;

        // 禁用风格
        if (name == "none")
            name = markImage.name.Split('-')[0];
        else
            name = markImage.name.Split('-')[0] + "-" + name;

        if (name != markImage.name)
        {
            markImage = Resources.Load<Texture>("Image/puzzle mark/" + name);

            if (pieceCreated) UpdatePieceMark();
        }
    }
    #endregion

    public void ShowAllOrNot(bool show)
    {
        isShowAll = show;
        if (!pieceCreated) return;

        for (int i = 0; i < pieceCount.x; i++)
            for (int j = 0; j < pieceCount.y; j++)
            {
                GameObject child = GetPiece(i, j);
                if (!child.GetComponent<Piece>().pid.isAtEdge)
                {
                    child.SetActive(show);
                }
            }
    }

    public void ToggleImage(bool show)
    {
        originImage.SetActive(show);
    }
    #endregion

    #region function



    void BuildRandomBuffer(int min, int max)
    {
        randomBuffer.Clear();

        // 初始化列表
        for (int i = min; i < max; i++)
            randomBuffer.Add(i);

        // 随机化列表
        for (int i = 0; i < randomBuffer.Count; i++)
        {
            int index = Random.Range(0, randomBuffer.Count);
            int temp = randomBuffer[i];
            randomBuffer[i] = randomBuffer[index];
            randomBuffer[index] = temp;
        }
    }


    void UpdatePieceMark()
    {
        for (int i = 0; i < pieceCount.x; i++)
            for (int j = 0; j < pieceCount.y; j++)
            {
                Renderer rend = GetPiece(i, j).GetComponent<Renderer>();
                rend.material.SetTexture("_MarkTex", markImage);
            }
    }

    void UpdatePieceImage()
    {
        for (int i = 0; i < pieceCount.x; i++)
            for (int j = 0; j < pieceCount.y; j++)
            {
                GameObject child = GetPiece(i, j);

                child.GetComponent<SpriteRenderer>().sprite = pieceImage;
                child.GetComponent<Piece>().ReSize();
            }
    }





    void RotatePiece()
    {
        if (isRotate != newIsRotate)
            isRotate = newIsRotate;
        if (!isRotate) return;

        // 随机选择个数
        int count = Random.Range(minCount, maxCount);
        for (int i = 0; i < count; i++)
        {
            // 随机角度
            float angle = Random.Range(1, 4) * 90;

            // 随机元素
            GameObject child = GetPiece(Random.Range(0, pieceCount.x), Random.Range(0, pieceCount.y));
            child.transform.localEulerAngles = new Vector3(0, 0, angle);
            print(child.GetComponent<Piece>() + " " + angle);
        }
    }

    #endregion
}

public class Record
{
    public int count;
    public int step;
    public float time;
    bool rotate;


    public override string ToString()
    {
        return "拼图块数：" + count + (rotate ? ",旋转" : "") + " 移动次数：" + step + " 使用时间：" + time.ToString("F2");
    }

    public Record(int _count, int _step, float _time, bool _rotate = false)
    {
        count = _count;
        step = _step;
        time = _time;
        rotate = _rotate;
    }
}