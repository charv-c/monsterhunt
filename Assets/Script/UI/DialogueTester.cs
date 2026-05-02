using UnityEngine;
using System.Collections.Generic;

public class DialogueTester : MonoBehaviour
{
    void Update()
    {
        // 玩家按下键盘上的数字 1 键，触发牢笼破碎剧情
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TriggerEscapeScene();
        }
    }

    private void TriggerEscapeScene()
    {
        // 1. 旁白描述牢笼破碎的动作
        DialogueManager.Instance.ShowDialogue("旁白", "【你使用了攻击，关押你的牢笼应声破碎...】");

        // 2. 准备玩家的选项
        List<DialogueOption> escapeOptions = new List<DialogueOption>
        {
            new DialogueOption { OptionId = 1, OptionText = "发生了什么？" },
            new DialogueOption { OptionId = 2, OptionText = "你竟敢暗算我！" }
        };

        // 3. 弹出选项面板让玩家选择
        DialogueManager.Instance.ShowOptions(escapeOptions);
    }
}