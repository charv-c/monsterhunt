using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public HealthController TargetHealth;
    public Image GreenBar; // 实血条
    public Image RedBar;   // 红血背景条

    void Update()
    {
        if (TargetHealth == null) return;

        float max = TargetHealth.MaxHealth;

        // 绿条显示实际血量
        GreenBar.fillAmount = TargetHealth.CurrentHealth / max;

        // 红条显示“实血+红血”的总和，这样它会比绿条长出一截
        RedBar.fillAmount = (TargetHealth.CurrentHealth + TargetHealth.RedHealth) / max;
    }
}