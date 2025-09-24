using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;
using static LungeManager;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;

public class EventBus
{
    public static class SystemEvents
    {
        public static Action<GameState> OnGameStateChange;
        public static Action OnGamePause;
        public static Action OnGameResume;
        public static Action OnGameExit;
        public static Action OnSceneLoadComplete;
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

        public static Action<int, int> OnPlayerHpChange;
        public static Action OnShowGameOverUI;
        public static Action OnShowStageClearUI;

        public static Action<bool> OnDashUIChange;
        public static Action<Transform> OnAimPointChanged;

        // 攻撃ボタン長押しUI
        public static Action<bool> OnAttackHoldUI;         // true=表示 / false=非表示
        public static Action<float> OnAttackHoldProgress;  // 進捗表示
        public static Action OnAttackHoldCommitted;
        public static Action OnAttackHoldDenied;
    }
    public static class PlayerEvents
    {
        public static Action<GameObject> OnPlayerSpawned;
        public static Action OnPlayerDead;
        public static Func<GameObject> GetPlayerObject;
        public static System.Func<PlayerAudioPart,AudioClip,float,float,float,bool> PlayClipByPart;

        public static Action<GameObject> OnAimTargetChanged;

        public static System.Func<LungeAim, Vector3, Vector3, float, float, AnimationCurve,bool> LungeByDistance;
        public static System.Func<LungeAim, Vector3, Vector3, float, float, AnimationCurve,bool> LungeByTime;

        public static Action<int /*current*/, int /*max*/> ApplyHP;
        public static Action<List<WeaponInstance> /*instances*/, int /*mainIndex*/> ApplyLoadoutInstances;

        public static Action<Transform, float, float> ChangeCameraTarget;

        public static Action<float, float> OnGamepadShake;
        public static Action<float,float,float> OnGamepadShakeCurve;

        public static Action<WeaponType> OnWeaponSkillUsed;
    }
    public static class EnemyEvents
    {
        public static Action<GameObject> OnEnemyDeath;
    }
}
