/*
 * 【后续功能扩展说明】
 * 目的：暴露变量给状态机供判断是否允许冲刺等行为。
 * * 实现步骤：
 * 1. 参数创建：在 Animator 面板中创建 Float 参数 "StaminaNormalized" 和 Bool 参数 "HasStamina"。
 * 2. 逻辑同步：
 * - 使用 Animator.SetFloat("StaminaNormalized", CurrentStamina / MaxStamina) 驱动混合树（如：体力低时切换为疲惫走动画）。
 * - 使用 Animator.SetBool("HasStamina", CurrentStamina > 0) 作为 Transition（转换）条件。
 * 3. 状态限制：
 * - 从 "Sprinting" 转换到 "Walking" 的连线上增加条件：[HasStamina -> false]。
 * - 这样当耐力耗尽时，状态机会强制切断冲刺动画，确保表现层与逻辑层同步。
 */
using UnityEngine;
using UnityEngine.UI; // 必须引用 UI 命名空间

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    public StaminaController TargetStaminaController; // 拖入你的 Player
    public Image FillImage;                           // 拖入上面的 StaminaBar
    void Update()
    {
        if (TargetStaminaController == null || FillImage == null) return;

        // 计算百分比 (0 到 1)
        float staminaPercent = TargetStaminaController.CurrentStamina / TargetStaminaController.MaxStamina;

        // 更新进度条长度
        FillImage.fillAmount = staminaPercent;
    }
}