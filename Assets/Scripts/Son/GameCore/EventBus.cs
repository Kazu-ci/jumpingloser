using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;

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
        // ����: (�������탊�X�g, fromIndex, toIndex)
        public static Action<List<WeaponInstance>, int, int> OnRightWeaponSwitch;
        public static Action<List<WeaponInstance>, int, int> OnLeftWeaponSwitch;

        // ����j��i�C���x���g������폜���ꂽ���O�� index �� WeaponItem�j
        public static Action<int, WeaponItem> OnWeaponDestroyed;

        // �ϋv�x�ύX�i�� / ���� index / ���ݑϋv / �ő�ϋv�j
        public static Action<HandType, int, int, int> OnDurabilityChanged;

        public static Action<int, int> OnPlayerHpChange;
        public static Action OnShowGameOverUI;
        public static Action OnShowStageClearUI;
    }
    public static class PlayerEvents
    {
        public static Func<GameObject> GetPlayerObject;
        public static Action<PlayerAudioPart,AudioClip> PlayClipByPart;

        public static Action<HandType> OnWeaponBroke;
        public static Action<GameObject> OnAimTargetChanged;
    }
}
