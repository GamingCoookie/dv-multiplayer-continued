using DV;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DVMultiplayer.Utils
{
    public static class UUI
    {
        public static GUIStyle GenerateStyle(Color? textColor = null, int fontSize = 12, TextAnchor allignment = TextAnchor.MiddleLeft)
        {
            GUIStyle style = new GUIStyle();

            style.normal.textColor = textColor == null ? Color.white : (Color)textColor;
            style.fontSize = fontSize;
            style.alignment = allignment;

            return style;
        }

        public static void UnlockMouse(bool value)
        {
            if (value)
                SingletonBehaviour<AppUtil>.Instance.RequireCursor();
            else
                SingletonBehaviour<AppUtil>.Instance.ReleaseCursor();
        }

        public static Texture2D LoadTextureFromFile(string filename)
        {
            if (!File.Exists($"./Mods/DVMultiplayerContinued/Resources/Textures/{filename}"))
                return null;

            Texture2D texture = new Texture2D(512, 512);
            if (texture.LoadImage(File.ReadAllBytes($"./Mods/DVMultiplayerContinued/Resources/Textures/{filename}")))
                return texture;
            else
                return null;
        }

        public static void SetSprite(this Image img, string filename)
        {
            Texture2D texture = UUI.LoadTextureFromFile(filename);
            if (!texture)
                return;

            Sprite icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            img.sprite = icon;
        }

        public static void ChangeAnchors(this RectTransform rt, RectTransformAnchoring anchor)
        {
            switch (anchor)
            {
                case RectTransformAnchoring.TopLeft:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    break;

                case RectTransformAnchoring.TopCenter:
                    rt.anchorMin = new Vector2(.5f, 1);
                    rt.anchorMax = new Vector2(.5f, 1);
                    break;

                case RectTransformAnchoring.TopRight:
                    rt.anchorMin = new Vector2(1f, 1);
                    rt.anchorMax = new Vector2(1f, 1);
                    break;

                case RectTransformAnchoring.TopStretch:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    break;

                case (RectTransformAnchoring.MiddleLeft):
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    break;

                case (RectTransformAnchoring.MiddleCenter):
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    break;

                case (RectTransformAnchoring.MiddleRight):
                    rt.anchorMin = new Vector2(1, 0.5f);
                    rt.anchorMax = new Vector2(1, 0.5f);
                    break;

                case RectTransformAnchoring.BottomCenter:
                    rt.anchorMin = new Vector2(.5f, 0);
                    rt.anchorMax = new Vector2(.5f, 0);
                    break;
            }
        }

        public static void ChangeRect(this RectTransform rt, Rect rect)
        {
            if (!(rt.anchorMin.x == 0 && rt.anchorMax.x == 1) && !(rt.anchorMin.y == 0 && rt.anchorMax.y == 1))
            {
                rt.anchoredPosition = new Vector2(rect.x, rect.y);
                rt.sizeDelta = new Vector2(rect.width, rect.height);
            }
        }
    }

    public enum RectTransformAnchoring
    {
        NotSet,
        TopLeft,
        TopCenter,
        TopRight,
        TopStretch,
        BottomCenter,
        MiddleLeft,
        MiddleRight,
        MiddleCenter,
    }
}
