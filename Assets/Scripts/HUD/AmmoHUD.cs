using UnityEngine;
using TMPro;

public class AmmoHUD : MonoBehaviour
{
    public GunController gun;      
    public TextMeshProUGUI ammoText;
    public GameObject reloadingHint; 
    void LateUpdate()
    {
        if (!gun || !ammoText) return;

        ammoText.text = $"{gun.ammoInClip} / {gun.reserveAmmo}";

        if (reloadingHint)
            reloadingHint.SetActive(IsReloading(gun));
    }

    bool IsReloading(GunController g)
    {
        var prop = g.GetType().GetProperty("IsReloading");
        return prop != null && (bool)prop.GetValue(g);
    }
}
