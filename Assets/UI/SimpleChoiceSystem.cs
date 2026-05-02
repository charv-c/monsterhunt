using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SimpleChoiceSystem : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI questionDisplay;
    public Transform buttonParent;
    public GameObject buttonPrefab;

    // 定义一个简单的结构体来存储选项数据
    [System.Serializable]
    public struct ChoiceNode
    {
        [TextArea] public string question;
        public string[] answers;
    }

    public void ShowNode(ChoiceNode node)
    {
        // 1. 设置问题文本
        questionDisplay.text = node.question;

        // 2. 清理旧按钮（如果有的话）
        foreach (Transform child in buttonParent) Destroy(child.gameObject);

        // 3. 生成新按钮
        for (int i = 0; i < node.answers.Length; i++)
        {
            int index = i;
            GameObject btn = Instantiate(buttonPrefab, buttonParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = node.answers[i];

            // 绑定点击事件，这里直接输出点击的索引
            btn.GetComponent<Button>().onClick.AddListener(() => OnChoiceMade(index));
        }
    }

    void OnChoiceMade(int index)
    {
        Debug.Log($"玩家选择了: {index}");
        // 在这里编写你的逻辑：比如跳转到下一个剧情节点或关闭UI
        gameObject.SetActive(false);
    }
}