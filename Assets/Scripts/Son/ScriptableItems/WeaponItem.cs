using System.Collections.Generic;
using UnityEngine;
public enum ATKActType
{
    BasicCombo,     // 通常コンボ
    ComboToFinisher,// 派生可能なコンボ
    ComboEnd,       // コンボの最終段階
    SubAttack,      // サブ攻撃
    Finisher        // フィニッシュ攻撃
}
[System.Serializable]
public class ComboAction
{
    public string name; // デバッグやUI表示用の名前
    public AnimationClip animation;
    [Tooltip("耐久値消費量")]
    public int durabilityCost = 1; 
    [Tooltip("攻撃アクションの種類")]
    public ATKActType actionType;
    [Tooltip("攻撃時のサウンドエフェクト")]
    public AudioClip swingSFX;
    // 攻撃判定や入力受付タイミングは AnimationEvent で制御する
}
[CreateAssetMenu(fileName = "WeaponItem", menuName = "Scriptable Objects/WeaponItem")]
public class WeaponItem : ScriptableObject
{
    [Header("基本情報")]
    public string weaponName;              // 武器の名前
    public GameObject modelPrefab;         // モデルのプレハブ
    public Sprite icon;                    // UI用アイコン
    public GameObject crackDropPrefab;     // 耐久値が0になったときにドロップするクラックのプレハブ
    public int numberOfCracks = 3;         // クラックの数

    [Header("主武器コンボ攻撃")]
    [Tooltip("主武器で使用する連続攻撃")]
    public List<ComboAction> mainWeaponCombo;

    [Header("サブ攻撃")]
    [Tooltip("サブ武器の通常攻撃")]
    public List<ComboAction> subWeaponAttack;

    [Header("フィニッシュ攻撃")]
    [Tooltip("コンボ最終段階前に発動する特殊攻撃")]
    public List<ComboAction> finisherAttack; 

    [Header("ステータス")]
    [Tooltip("最大耐久値")]
    public int maxDurability = 100;
    [Tooltip("基礎攻撃力)")]
    public float attackPower = 3f;
    [Tooltip("攻撃範囲")]
    public float attackRange = 2f;
    [Tooltip("攻撃速度")]
    public float attackSpeed = 1.0f;

    [Header("効果音・エフェクト")]
    public AudioClip hitSFX; // ヒット時の効果音
    public GameObject hitVFXPrefab; // ヒット時のエフェクトプレハブ
}
