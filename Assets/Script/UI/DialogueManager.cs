using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 推荐使用 TextMeshPro

// 选项的数据实体
[System.Serializable]
public class DialogueOption
{
    public int OptionId;      // 选项ID，用于逻辑判断
    public string OptionText; // 显示在按钮上的文字
}

public class DialogueManager : MonoBehaviour
{
    // 单例模式，方便其他脚本调用
    public static DialogueManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI SpeakerNameText;    // 说话人名字UI
    public TextMeshProUGUI DialogueContentText; // 对话内容UI
    public GameObject OptionPanel;       // 右侧选项面板容器
    public GameObject OptionPrefab;      // 选项按钮的预制体
    public Transform OptionContainer;    // 放置按钮的父物体（带垂直布局组）
    [Header("Data Storage")]
    // 新增：专门用于存储玩家最后一次选择的文本
    public string LastSelectedOptionText = "";
    private void Awake()
    {
        Instance = this;
        // 初始时隐藏面板
        if (OptionPanel != null) OptionPanel.SetActive(false);
    }

    /// <summary>
    /// 外部调用接口：弹出选项
    /// </summary>
    /// <param name="options">传入的选项列表</param>
     public void ShowDialogue(string speakerName, string content)
        {
            // 更新名字和内容
            if (SpeakerNameText != null) SpeakerNameText.text = speakerName;
            if (DialogueContentText != null) DialogueContentText.text = content;

            // 当NPC说话时，通常要隐藏选项面板，直到真正需要选择时才弹出来
            if (OptionPanel != null) OptionPanel.SetActive(false);
        }
    public void ShowOptions(List<DialogueOption> options)
    {
       
        // 1. 清理旧选项
        ClearOptions();

        // 2. 显示面板
        OptionPanel.SetActive(true);

        // 3. 根据数据生成按钮
        foreach (var data in options)
        {
            GameObject go = Instantiate(OptionPrefab, OptionContainer);
            // 获取按钮脚本并初始化（稍后创建该脚本）
            OptionButton btnScript = go.GetComponent<OptionButton>();
            if (btnScript != null)
            {
                btnScript.Init(data);
            }
        }
    }

    /// <summary>
    /// 接口：处理玩家选择
    /// </summary>
    public void OnOptionSelected(int id, string selectedText)
    {
        Debug.Log($"玩家选择了选项 ID: {id}");
        LastSelectedOptionText = selectedText;
        Debug.Log($"玩家选择了选项 ID: {id}，记录的文本是: {LastSelectedOptionText}");

        // 【修改点 3】：实现你的任务“弹出功能，用if else实现”
        if (id == 1)
        {
            // 在这里写选项1的逻辑，比如贺采的后续反应
            Debug.Log("执行选项1的功能：处理贺采的逻辑");
        }
        else if (id == 2)
        {
            // 在这里写选项2的逻辑
            Debug.Log("执行选项2的功能：处理其他逻辑");
        }
        else
        {
            // 兜底逻辑
            Debug.Log("执行默认功能");
        }

        // 选择后关闭面板
        OptionPanel.SetActive(false);
    }
    private void ClearOptions()
    {
        foreach (Transform child in OptionContainer)
        {
            Destroy(child.gameObject);
        }
    }
}