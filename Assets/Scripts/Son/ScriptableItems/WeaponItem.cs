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
public enum WeaponType
{
    Fist,
    Sword,
    Spear,
    Boomerang
}
[System.Serializable]
public class ComboAction
{
    public string name; // デバッグやUI表示用の名前
    public AnimationClip animation;

    [Header("攻撃性能")]
    public int actPowerModifier = 0;
    [Tooltip("耐久値消費量")]
    public int durabilityCost = 1; 
    [Tooltip("攻撃アクションの種類")]
    public ATKActType actionType;
    [Tooltip("攻撃時のサウンドエフェクト")]
    public AudioClip swingSFX;
    [Tooltip("攻撃のキャラーヴォイス")]
    public AudioClip voiceSFX;


    [Header("入力受付ウィンドウ（0~1）")]
    [Range(0f, 1f)] public float inputWindowStart = 0.3f;
    [Range(0f, 1f)] public float inputWindowEnd = 0.8f;

    [Header("次段へ遷移する終了タイミング（0~1）")]
    [Range(0f, 1f)] public float endNormalizedTime = 0.9f;

    [Header("次段へのブレンド時間（秒）")]
    [Min(0f)] public float blendToNext = 0.12f;

    [Header("攻撃判定")]
    public Vector3 hitBoxCenter = new Vector3(0.5f, 1.0f, 1.0f); // ヒットボックスの中心（ローカル座標）
    public Vector3 hitBoxSize = new Vector3(1.0f, 1.0f, 1.0f);   // ヒットボックスのサイズ
    [Header("ヒット判定タイミング（秒）0未満なら手動")]
    public float hitCheckTime = 0.2f;
    public List<float> hitTimeList = new List<float>();
    [Header("攻撃プレハブ")]
    public GameObject attackPrefab; // 攻撃判定用のプレハブ（nullならヒットボックスのみ）

    [Header("突進動作")]
    [Tooltip("突進する直線距離（0はなし）")]
    public float lungeDistance = 0f;
    [Tooltip("突進速度（m/s）")]
    public float lungeSpeed = 10f;
    [Tooltip("突進開始時間")]
    public float lungeTime = 0.1f;

    [Header("アニメーション速度補正")]
    public float animationSpeed = 1.0f; // アニメーション再生速度補正

    [Header("エフェクト")]
    public GameObject attackVFXPrefab; // ヒット時のエフェクトプレハブ
    public float attackVFXTime = 0.2f; // エフェクト発生タイミング
}
[CreateAssetMenu(fileName = "WeaponItem", menuName = "Scriptable Objects/WeaponItem")]
public class WeaponItem : ScriptableObject
{
    [Header("基本情報")]
    public string weaponName;              // 武器の名前
    public GameObject modelPrefab;         // モデルのプレハブ
    public Sprite icon;                    // UI用アイコン
    public WeaponType weaponType;          // 武器の種類

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
    [Tooltip("耐久回復値")]
    public int addDurabilityOnPickup = 20; // 拾得時に回復する耐久値

    [Header("効果音・エフェクト")]
    public AudioClip hitSFX; // ヒット時の効果音
    public GameObject hitVFXPrefab; // ヒット時のエフェクトプレハブ

    
}
