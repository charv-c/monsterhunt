using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionButton : MonoBehaviour
{
    public TextMeshProUGUI ButtonText;
    private int _optionId;

    public void Init(DialogueOption data)
    {
        _optionId = data.OptionId;
        ButtonText.text = data.OptionText;

        // 绑定点击事件：把 ID 和 文本 一起传给 Manager
        GetComponent<Button>().onClick.AddListener(() => {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnOptionSelected(_optionId, data.OptionText);
            }
        });
    }
}