using TMPro;
using UnityEngine;
using System;

public class Text_GetClearTime : MonoBehaviour
{
    private TextMeshProUGUI _text;
    public bool showHundredth = false;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        double sec = PlayerPersistence.Instance?.Current.elapsedGameTimeSec ?? 0.0;

        _text.text = FormatMinSec(sec, showHundredth);
    }


    private static string FormatMinSec(double seconds, bool hundredth)
    {
        int totalMinutes = (int)Math.Floor(seconds / 60.0);
        int sec = (int)Math.Floor(seconds % 60.0);
        return $"{totalMinutes}:{sec:D2}";
    }
}
