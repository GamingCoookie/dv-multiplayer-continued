using DVMultiplayer.Utils;
using UnityEngine;
using UnityEngine.UI;

internal class ButtonFeatures : MonoBehaviour
{
    private Button btn;
    private Image btnImage;
    private Sprite enabledSprite;
    private Sprite disabledSprite;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btnImage = btn.GetComponent<Image>();
        enabledSprite = btnImage.sprite;
        Texture2D disabledTexture = UUI.LoadTextureFromFile("UI_Button_disabled.png");
        disabledSprite = Sprite.Create(disabledTexture, new Rect(0, 0, disabledTexture.width, disabledTexture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        if (btn.interactable && btnImage.sprite == disabledSprite)
            btnImage.sprite = enabledSprite;
        else if (!btn.interactable && btnImage.sprite == enabledSprite)
            btnImage.sprite = disabledSprite;
    }
}
