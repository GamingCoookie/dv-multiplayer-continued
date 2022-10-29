using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DV;
using DVMultiplayer;
using DVMultiplayer.Utils;
using UnityEngine;

namespace DVMultiplayerContinued.Unity.Player
{
    internal static class GameChat
    {
        private static bool openChatLine = false;
        private static bool openChat = true;
        private static string text = "";
        private static int remainingLength;
        public static List<string> messages = new List<string>();
        private static GUIStyle labelStyle = null;
        private static GUIStyle inputStyle = null;
        private static bool initalized = false;
        private static int line_length;
        private static int line_count;

        internal static void Setup()
        {
            Main.Log($"Initializing Chat");
            Main.OnGameUpdate += Update;
            Main.OnGameFixedGUI += OnGUI;
        }

        internal static void Initialize()
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            inputStyle = new GUIStyle(GUI.skin.textField);

            labelStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
            inputStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
            CharacterInfo characterInfo = new CharacterInfo();
            labelStyle.font.RequestCharactersInTexture("a");
            labelStyle.font.GetCharacterInfo('a', out characterInfo);
            line_length = Convert.ToInt32(Math.Floor(Screen.width * 0.29f / characterInfo.advance));
            line_count = Convert.ToInt32(Math.Floor((Screen.height * 0.3f - 20) / 20));
            PutSystemMessage($"Mod successfully loaded. Have fun. :)");
            PutSystemMessage($"You can minimize this window by hitting T and then clicking on the little button" +
                $" at the top right");
            initalized = true;
        }

        internal static void Update()
        {
            if (!openChatLine && SingletonBehaviour<AppUtil>.Instance.isCursorNeeded)
                return;
            if (Input.GetKeyUp(KeyCode.T))
            {
                openChatLine = !openChatLine;
                if (openChatLine == true)
                {
                    UUI.UnlockMouse(true);
                }
                else
                {
                    UUI.UnlockMouse(false);
                }
            }
            if (Input.GetKeyUp(KeyCode.Return))
            {
                if (!openChatLine)
                    return;
                openChatLine = false;
                UUI.UnlockMouse(false);
                text = text.Trim();
                if (SingletonBehaviour<NetworkPlayerManager>.Exists && text.Length > 0)
                {
                    SingletonBehaviour<NetworkPlayerManager>.Instance.SendChatMessage(text);
                }
                else if (text.Length > 0)
                {
                    PutSystemMessage("You aren't connected to any server!");
                }
                text = "";
            }
        }

        internal static void OnGUI()
        {
            if (!initalized)
                Initialize();
            if (GUI.Button(new Rect(Screen.width * 0.99f, Screen.height * 0.05f, 20, 20), ""))
                openChat = !openChat;

            if (openChat)
            {
                GUI.Box(new Rect(Screen.width * 0.7f, Screen.height * 0.05f, Screen.width * 0.3f, Screen.height * 0.3f), "Multiplayer Continued Chat");
                float ypos = Screen.height * 0.05f + 20;

                foreach (string message in messages)
                {
                    GUI.Label(new Rect(Screen.width * 0.705f, ypos, Screen.width * 0.29f, 20), message, labelStyle);
                    ypos += 20;
                }

                if (openChatLine == true)
                {
                    text = GUI.TextField(new Rect(Screen.width * 0.7f, Screen.height * 0.35f - 20, Screen.width * 0.27f, 20), text, 160, inputStyle);
                    remainingLength = 160 - text.Length;
                    GUI.Label(new Rect(Screen.width * 0.98f, Screen.height * 0.35f - 20, Screen.width * 0.03f, 20), remainingLength.ToString(), labelStyle);
                }
            }
        }

        internal static void AppendNewMessage(string message)
        {
            if (message.Length > line_length)
            {
                while (message.Length > 0)
                {
                    int stop_index = message.Length >= line_length ? line_length : message.Length;
                    messages.Add(message.Substring(0, stop_index));
                    message = message.Remove(0, stop_index);
                    if (messages.Count > line_count)
                        messages.RemoveAt(0);
                }
            }
            else
            {
                messages.Add(message);
                if (messages.Count > line_count)
                    messages.RemoveAt(0);
            }
        }

        internal static void PutSystemMessage(string message)
        {
            AppendNewMessage(message.Insert(0, "System> "));
        }
    }
}
