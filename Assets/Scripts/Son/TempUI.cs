using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TempUI : MonoBehaviour
{
    public TextMeshProUGUI weaponDurableText;
    public Image weaponIconImage;

    public void UpdateWeapon(WeaponInstance weapon)
    {
        if (weapon == null)
        {
            weaponDurableText.text = "No Weapon";
            weaponIconImage.sprite = null;
            weaponIconImage.enabled = false;
            return;
        }

        weaponDurableText.text =
            $"{weapon.currentDurability}/{weapon.template.maxDurability}";

        weaponIconImage.sprite = weapon.template.icon;
        weaponIconImage.enabled = true;
    }
}
