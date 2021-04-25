﻿using System;
using Character;
using UniRx;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Mp;

public class AttackEnemyAgent : BaseEnemyAgent
{
    public override void Initialize()
    {
        base.Initialize();
        attackObservable//通常弾
            .Where(attack => attack == 1)
            .ThrottleFirst(TimeSpan.FromSeconds(AttackInterval.bulletInterval))
            .Subscribe(_ =>
            {
                _attackManager.BulletAttack();
                _enemyAnimation.SetAnimation("Attack");
                AttackState = State.Bullet_Attack;
            });

        attackObservable//炎
            .Where(attack => attack == 2 && MpValue >= Attack.firetMp)
            .ThrottleFirst(TimeSpan.FromSeconds(AttackInterval.firetInterval))
            .Subscribe(_ =>
            {
                _attackManager.FireAttack();
                _enemyAnimation.SetAnimation("Attack");
                MpConsumption(Attack.firetMp);
                AttackState = State.Fire_Attack;
            });

        attackObservable//爆弾
            .Where(attack => attack == 3 && MpValue >= Attack.bombMp)
            .ThrottleFirst(TimeSpan.FromSeconds(AttackInterval.bombInterval))
            .Subscribe(_ =>
            {
                _attackManager.BombAttack();
                _enemyAnimation.SetAnimation("Attack");
                MpConsumption(Attack.bombMp);
                AttackState = State.Bomb_Attack;
            });
    }

    //観察対象 個数:9(自分の位置(3), 自分のMP(1),　自分の攻撃状態(1), playerの位置(3), playerの防御状態(1))
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(this.transform.position);
        sensor.AddObservation(MpValue);
        sensor.AddObservation((float)AttackState);
        if (player != null)
        {
            sensor.AddObservation(player.transform.position);
            if (_playerAgent != null)
            {
                sensor.AddObservation((float)_playerAgent.GetGuardState);
            }
            else if (_playerController != null)
            {
                sensor.AddObservation((float)_playerController.GetGuardState);
            }
        }
        else //playerが消滅(撃破)した際のバグ対策
        {
            sensor.AddObservation(this.transform.position);
            sensor.AddObservation((float)State.Normal);
        }

    }

    public override void OnActionReceived(float[] vectorAction)
    {
        base.OnActionReceived(vectorAction); //攻撃　0:なし　1:通常弾　2:炎 3:ボム

        if (_playerAgent != null)
        {
            if (_playerAgent.GetHpValue <= 0) //撃破
            {
                AddReward(1.0f);
                EndEpisode();
            }
        }
    }

    //ダメージ処理
    public override void Attacked(float damage)
    {
        base.Attacked(damage);
        if (HpValue <= 0)//死亡(学習中）
        {
            EndEpisode();
        }
    }
}
