using TMPro;
using UnityEngine;
using System;

public class Text_GetKill : MonoBehaviour
{
    TextMeshProUGUI _text;
    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        int kill = PlayerPersistence.Instance?.Current.enemyDefeatCount ?? 0;
        _text.text = kill.ToString();
    }
}
