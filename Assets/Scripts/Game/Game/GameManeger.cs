﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;
using System;
using Game;

public class GameManeger : SingletonMonoBehaviour<GameManeger>
{

    public ReactiveProperty<GameState> currentGameStates = new ReactiveProperty<GameState>();
    [SerializeField] private GameObject loadSceneImage = default;
    private int enemyNumber; //戦う敵の番号
    private MoveScene _moveScene;

    private bool isRushRetry;//rushgameのリトライかどうか
    private bool isSceneMoveComplete;//シーンの移動完了したかどうか

    void Start()
    {
        enemyNumber = 0;
        DontDestroyOnLoad(this.gameObject);
        _moveScene = new MoveScene();
        SceneManager.sceneLoaded += SceneLoaded;

        //タイトル
        currentGameStates
            .Where(state => state == GameState.Title)
            .Skip(1)
            .Subscribe(_ => LoadScene("Title", "None"));

        //ゲーム選択画面
        currentGameStates
            .Where(state => state == GameState.Start)
            .Subscribe(_ =>
            {
                LoadScene("Start", "Select");
                isRushRetry = false;
            });

        //ゲーム画面１vs１
        currentGameStates
            .Where(state => state == GameState.VsGame)
            .Subscribe(_ => LoadScene("Play", "None"));

        //ゲーム画面BossRush
        currentGameStates
            .Where(state => state == GameState.RushGame)
            .Subscribe(_ =>
            {
                LoadScene("Play", "None");
                isRushRetry = true;
            }); 

        //play
        currentGameStates
            .Where(state => state == GameState.Play)
            .Subscribe(_ =>
            {
                Time.timeScale = 1.0f; //止まってたら動かす
                SoundManager.Instance.PlayBgm("Fight");
            });

        //pause
        currentGameStates
            .Where(state => state == GameState.Pause)
            .Subscribe(_ => Time.timeScale = 0f);

        //ゲーム終了(result画面）
        currentGameStates
            .Where(state => state == GameState.Result)
            .Subscribe(_ => SoundManager.Instance.StopBgm());

        //ゲーム終了(アプリ落とす）
        currentGameStates
            .Where(state => state == GameState.GameEnd)
            .Subscribe(_ => Application.Quit());

        //リトライ
        currentGameStates
            .Where(state => state == GameState.Retry)
            .Subscribe(_ =>
            {
                if (isRushRetry)
                {
                    enemyNumber = 0; //rushゲームだったら最初から
                    currentGameStates.Value = GameState.RushGame;
                }
                else{
                    currentGameStates.Value = GameState.VsGame;
                }
            });
    }

    //シーン移動
    private void LoadScene(string sceneName, string bgmName)
    {
        isSceneMoveComplete = false;
        var copyLoadSceneImage = Instantiate(loadSceneImage, loadSceneImage.transform.position, Quaternion.identity);
        Time.timeScale = 1.0f; //止まってたら動かす
        //シーン移動演出
        Observable
            .FromCoroutine(() => _moveScene.LoadSceneImage(copyLoadSceneImage, true))
            .Subscribe(_ =>
            {
                SceneManager.LoadScene(sceneName);
                SoundManager.Instance.PlayBgm(bgmName);
            });
    }

    //シーン移動完了後
    private void OpenScene()
    {
        var copyLoadSceneImage = Instantiate(loadSceneImage, loadSceneImage.transform.position, Quaternion.identity);
        StartCoroutine(_moveScene.LoadSceneImage(copyLoadSceneImage, false));
        //シーン移動演出完了したらフラグ立てる
        Observable
            .FromCoroutine(() => _moveScene.LoadSceneImage(copyLoadSceneImage, false))
            .Subscribe(_ => isSceneMoveComplete = true);
    }

    // イベントハンドラー
    void SceneLoaded(Scene nextScene, LoadSceneMode mode)
    {
        OpenScene();
    }

    public int EnemyNumber
    {
        get { return this.enemyNumber; }  //取得用
        set { this.enemyNumber = value; } //値入力用
    }

    public bool SceneMoveComplete { get { return this.isSceneMoveComplete; } } //取得用
}
