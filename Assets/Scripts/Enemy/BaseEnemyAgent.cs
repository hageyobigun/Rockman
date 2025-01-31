﻿using UnityEngine;
using Unity.MLAgents;
using UniRx;
using System;
using Character;
using Game;

public abstract class BaseEnemyAgent : Agent, IAttackable
{
    [SerializeField] private int maxHpValue = 100;
    [SerializeField] private int maxMpValue = 100;
    private int hpValue;
    private int mpValue;
    private State enemyAttackState;
    private State enemyGuardState;
    //class
    private EnemyMove _enemyMove;
    protected EnemyAnimation _enemyAnimation;
    protected AttackManager _attackManager;
    [SerializeField] private LifePresenter _lifePresenter = null;
    [SerializeField] private StageManager _stageManager = null;
    [SerializeField] private ResultPresenter _resultPresenter = default;

    //行動subject
    private Subject<int> moveSubject = new Subject<int>();
    private Subject<int> attackSubject = new Subject<int>();
    public IObservable<int> attackObservable { get { return attackSubject; }}

    //player関連
    public GameObject player;
    protected PlayerAgent _playerAgent;
    protected PlayerController _playerController;

    public override void Initialize()
    {
        _enemyMove = new EnemyMove(gameObject, _stageManager.GetStageList(Category.Enemy));
        _enemyAnimation = new EnemyAnimation(this.GetComponent<Animator>());
        _attackManager = GetComponent<AttackManager>();
        _playerAgent = player.GetComponent<PlayerAgent>();
        _playerController = player.GetComponent<PlayerController>();


        moveSubject
            .Where(move => _enemyMove.IsMove(move))
            .Subscribe(_ => _enemyMove.Move());

        attackSubject
            .Where(attack => attack == 0)
            .Subscribe(_ => enemyAttackState = State.Normal);//攻撃しない
    }

    //エピソード開始時
    public override void OnEpisodeBegin()
    {
        _lifePresenter.Initialize(maxHpValue, maxMpValue);
        hpValue = maxHpValue;
        mpValue = maxMpValue;
        enemyAttackState = State.Normal;
        enemyGuardState = State.Normal;
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (GameManeger.Instance.currentGameStates.Value == GameState.Play)//play開始
        { 
            int move = (int)vectorAction[0]; //0:静止 1:上　2:左　3:右　4:下
            int attack = (int)vectorAction[1]; //攻撃　継承した先で登録
            moveSubject.OnNext(move);
            attackSubject.OnNext(attack);
        }
    }

    public virtual void Attacked(float damage)
    {
        hpValue -= (int)damage;
        _lifePresenter.OnChangeHpLife(hpValue);
        _enemyAnimation.SetAnimation("Damage");
        if (hpValue <= 0 && _playerController != null)//死亡(Game中)
        {
            _resultPresenter.Win();
            //sliderが戻ってしまうバグ防止
            maxHpValue = hpValue;
            maxMpValue = mpValue;
            Destroy(gameObject);
        }
    }

    //攻撃や防御などMP使用行動
    public void MpAction(int useMpvalue, string animationName)
    {
        mpValue -= useMpvalue;
        _lifePresenter.OnChangeMpLife(mpValue);
        _enemyAnimation.SetAnimation(animationName);
    }

    //プロパティー
    public int HpValue
    {
        get { return this.hpValue; }  //取得用
        set { this.hpValue = value; } //値入力用
    }

    //プロパティー
    public int MpValue
    {
        get { return this.mpValue; }  //取得用
        set { this.mpValue = value; } //値入力用
    }

    //プロパティー 攻撃状態
    public State AttackState
    {
        get { return this.enemyAttackState; }  //取得用
        set { this.enemyAttackState = value; } //値入力用
    }

    //プロパティー 防御状態
    public State GuardState
    {
        get { return this.enemyGuardState; }  //取得用
        set { this.enemyGuardState = value; } //値入力用
    }
}
