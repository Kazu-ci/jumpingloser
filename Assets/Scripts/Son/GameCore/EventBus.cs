using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;
using static LungeManager;

public class EventBus
{
    public static class SystemEvents
    {
        public static Action<GameState> OnGameStateChange;
        public static Action OnGamePause;
        public static Action OnGameResume;
        public static Action OnGameExit;
        public static Action OnSceneLoadComplete;

        public static Action<float, float> OnChangeTimeScaleForSeconds;
    }
    public static class UIEvents
    {
        // 引数: (所持武器リスト, fromIndex, toIndex)
        public static Action<List<WeaponInstance>, int, int> OnRightWeaponSwitch;
        public static Action<List<WeaponInstance>, int, int> OnLeftWeaponSwitch;

        // 武器破壊（インベントリから削除された直前の index と WeaponItem）
        public static Action<int, WeaponItem> OnWeaponDestroyed;

        // 耐久度変更（手 / 現在 index / 現在耐久 / 最大耐久）
        public static Action<HandType, int, int, int> OnDurabilityChanged;
        public static Action OnWeaponUseFailed;

        // HP変更（現在HP / 最大HP）
        public static Action<int, int> OnPlayerHpChange;
        public static Action OnShowGameOverUI;
        public static Action OnShowStageClearUI;

        // ダッシュUI
        public static Action<bool> OnDashUIChange;

        // エイムポイント変更
        public static Action<Transform> OnAimPointChanged;

        // 攻撃ボタン長押しUI
        public static Action<bool> OnAttackHoldUI;         // true=表示 / false=非表示
        public static Action<float> OnAttackHoldProgress;  // 進捗表示
        public static Action OnAttackHoldCommitted;
        public static Action OnAttackHoldDenied;

        //チュートリアル
        public static Action OnShowWeaponSkillTutorial;
        public static Action OnShowDashTutorial;
        public static Action OnShowSwitchWeaponTutorial;
    }
    public static class PlayerEvents
    {
        // 保存データ適用
        public static Action<int /*current*/, int /*max*/> ApplyHP;
        public static Action<List<WeaponInstance> /*instances*/, int /*mainIndex*/> ApplyLoadoutInstances;

        // プレーヤーオブジェクト
        public static Action<GameObject> OnPlayerSpawned;
        public static Action OnPlayerDead;
        public static Func<GameObject> GetPlayerObject;

        // 音声
        public static System.Func<PlayerAudioPart,AudioClip,float,float,float,bool> PlayClipByPart;

        // エイムターゲット変更
        public static Action<GameObject> OnAimTargetChanged;
        public static Action<Transform, float, float> ChangeCameraTarget;

        // 突進、ダッシュ
        public static System.Func<LungeAim, Vector3, Vector3, float, float, AnimationCurve,bool> LungeByDistance;
        public static System.Func<LungeAim, Vector3, Vector3, float, float, AnimationCurve,bool> LungeByTime;

        // ゲームパッド振動
        public static Action<float, float> OnGamepadShake;
        public static Action<float,float,float> OnGamepadShakeCurve;

        // 武器スキル使用
        public static Action<WeaponType> OnWeaponSkillUsed;
    }
    public static class EnemyEvents
    {
        public static Action<GameObject> OnEnemyDeath;
    }
}
