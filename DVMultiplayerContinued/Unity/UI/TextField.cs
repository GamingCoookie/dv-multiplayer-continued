using DVMultiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class TextField : MonoBehaviour
{
    public string title;
    public bool isDigitOnly = false;
    private void Awake()
    {
        TextMeshProUGUI text = transform.Find("label").GetComponent<TextMeshProUGUI>();
        InputScreen input = CustomUI.InputScreenUI.GetComponent<InputScreen>();
        Button btnConfirm = CustomUI.InputScreenUI.transform.Find("Button Confirm").GetComponent<Button>();
        Button btnClose = CustomUI.InputScreenUI.transform.Find("Button Close").GetComponent<Button>();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            MenuScreen prevScreen = CustomUI.currentScreen;
            input.SetTitle(title);
            input.Input = text.text;
            input.isDigitOnly = isDigitOnly;
            CustomUI.Open(CustomUI.InputScreenUI);
            input.OnOpen();
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(() =>
            {
                text.text = input.Input;
                CustomUI.Open(prevScreen);
                input.OnClose();
            });

            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() =>
            {
                CustomUI.Open(prevScreen);
                input.OnClose();
            });
        });
    }
}
