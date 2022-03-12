using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class InputScreen : MonoBehaviour
{
    private TextMeshProUGUI label;

    private string input;
    public bool isUppercase;
    public bool isDigitOnly = false;
    private Button casingButton;
    private Button confirmButton;
    private bool listenToKeyboard = false;

    public string Input
    {
        get
        {
            return input;
        }
        internal set
        {
            input = value;
            UpdateLabel();
        }
    }

    private void Awake()
    {
        label = transform.Find("Label Input").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI casingBtnText = transform.Find("Button Casing").Find("label").GetComponent<TextMeshProUGUI>();
        casingButton = transform.Find("Button Casing").GetComponent<Button>();
        confirmButton = transform.Find("Button Confirm").GetComponent<Button>();
        casingButton.onClick.AddListener(() =>
        {
            isUppercase = !isUppercase;
            casingBtnText.text = isUppercase ? "Lowercase" : "Uppercase";
        });
        ResetInput();
    }

    internal void Paste()
    {
        TextEditor editor = new TextEditor();
        if (editor.Paste())
        {
            editor.text = editor.text.Trim();
            if (isDigitOnly)
                Input += Regex.Replace(editor.text, "[^0-9]*$", "");
            else
                Input += editor.text.Replace(" ", "");
        }
    }

    internal void OnOpen()
    {
        listenToKeyboard = true;
        casingButton.interactable = !isDigitOnly;
    }

    internal void OnClose()
    {
        listenToKeyboard = false;
    }

    private void Update()
    {
        if (listenToKeyboard)
        {
            if ((UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl)) && UnityEngine.Input.GetKeyUp(KeyCode.V))
                Paste();
            else
            {
                foreach (char c in UnityEngine.Input.inputString)
                {
                    if (c == '\b') // has backspace/delete been pressed?
                    {
                        Backspace();
                    }
                    else if ((c == '\n') || (c == '\r')) // enter/return
                    {
                        confirmButton.onClick?.Invoke();
                    }
                    else if (char.IsDigit(c) && isDigitOnly)
                        input += c;
                    else if ((char.IsLetterOrDigit(c) || c == '.' || c == '-' || c != ' ') && !isDigitOnly)
                    {
                        Input += c;
                    }
                }
            }
        }
    }

    public void ResetInput()
    {
        Input = "";
    }

    public void SendKeyPress(char key)
    {
        Input += (isUppercase ? $"{key}".ToUpper() : $"{key}");
    }

    private void UpdateLabel()
    {
        label.text = Input;
    }

    public void Backspace()
    {
        if (Input.Length <= 0)
            return;

        Input = Input.Substring(0, Input.Length - 1);
    }

    public void SetTitle(string inputWindowTitle)
    {
        transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"Entering {inputWindowTitle}";
    }
}