using System.Collections.Generic;
using UnityEngine;
public enum ATKActType
{
    BasicCombo,     // �ʏ�R���{
    ComboToFinisher,// �h���\�ȃR���{
    ComboEnd,       // �R���{�̍ŏI�i�K
    SubAttack,      // �T�u�U��
    Finisher        // �t�B�j�b�V���U��
}
[System.Serializable]
public class ComboAction
{
    public string name; // �f�o�b�O��UI�\���p�̖��O
    public AnimationClip animation;
    [Tooltip("�ϋv�l�����")]
    public int durabilityCost = 1; 
    [Tooltip("�U���A�N�V�����̎��")]
    public ATKActType actionType;
    [Tooltip("�U�����̃T�E���h�G�t�F�N�g")]
    public AudioClip swingSFX;

    [Header("���͎�t�E�B���h�E�i0~1�j")]
    [Range(0f, 1f)] public float inputWindowStart = 0.3f;
    [Range(0f, 1f)] public float inputWindowEnd = 0.8f;

    [Header("���i�֑J�ڂ���I���^�C�~���O�i0~1�j")]
    [Range(0f, 1f)] public float endNormalizedTime = 0.9f;

    [Header("���i�ւ̃u�����h���ԁi�b�j")]
    [Min(0f)] public float blendToNext = 0.12f;

    [Header("�U������")]
    public Vector3 hitBoxCenter = new Vector3(0.5f, 1.0f, 1.0f); // �q�b�g�{�b�N�X�̒��S�i���[�J�����W�j
    public Vector3 hitBoxSize = new Vector3(1.0f, 1.0f, 1.0f);   // �q�b�g�{�b�N�X�̃T�C�Y
    [Header("�q�b�g����^�C�~���O�i�b�j0�����Ȃ�蓮")]
    public float hitCheckTime = 0.2f;

    [Header("�G�t�F�N�g")]
    public GameObject attackVFXPrefab; // �q�b�g���̃G�t�F�N�g�v���n�u
    public float attackVFXTime = 0.2f; // �G�t�F�N�g�����^�C�~���O
}
[CreateAssetMenu(fileName = "WeaponItem", menuName = "Scriptable Objects/WeaponItem")]
public class WeaponItem : ScriptableObject
{
    [Header("��{���")]
    public string weaponName;              // ����̖��O
    public GameObject modelPrefab;         // ���f���̃v���n�u
    public Sprite icon;                    // UI�p�A�C�R��
    public GameObject crackDropPrefab;     // �ϋv�l��0�ɂȂ����Ƃ��Ƀh���b�v����N���b�N�̃v���n�u
    public int numberOfCracks = 3;         // �N���b�N�̐�

    [Header("�啐��R���{�U��")]
    [Tooltip("�啐��Ŏg�p����A���U��")]
    public List<ComboAction> mainWeaponCombo;

    [Header("�T�u�U��")]
    [Tooltip("�T�u����̒ʏ�U��")]
    public List<ComboAction> subWeaponAttack;

    [Header("�t�B�j�b�V���U��")]
    [Tooltip("�R���{�ŏI�i�K�O�ɔ����������U��")]
    public List<ComboAction> finisherAttack; 

    [Header("�X�e�[�^�X")]
    [Tooltip("�ő�ϋv�l")]
    public int maxDurability = 100;
    [Tooltip("��b�U����)")]
    public float attackPower = 3f;
    [Tooltip("�U���͈�")]
    public float attackRange = 2f;
    [Tooltip("�U�����x")]
    public float attackSpeed = 1.0f;

    [Header("���ʉ��E�G�t�F�N�g")]
    public AudioClip hitSFX; // �q�b�g���̌��ʉ�
    public GameObject hitVFXPrefab; // �q�b�g���̃G�t�F�N�g�v���n�u

    
}
