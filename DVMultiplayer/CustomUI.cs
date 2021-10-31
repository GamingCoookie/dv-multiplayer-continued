using DVMultiplayer.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DVMultiplayer
{
    internal class CustomUI
    {
        internal static MenuScreen NetworkUI;
        internal static MenuScreen ConnectMenuUI;
        internal static MenuScreen InputScreenUI;
        internal static MenuScreen HostMenuUI;
        internal static MenuScreen SaveFavoriteMenuUI;
        internal static MenuScreen FavoriteConnectMenuUI;
        internal static MenuScreen UsernameRequestMenuUI;
        internal static MenuScreen HostConnectedMenuUI;
        internal static MenuScreen ClientConnectedMenuUI;
        internal static MenuScreen ModMismatchScreen;
        internal static MenuScreen PopupUI;
        internal static MenuScreen currentScreen;
        internal static MenuScreen prevScreen;
        internal static bool readyForCSUpdate = false;
        internal static bool CSUpdateFinished = false;
        internal static float menuOffset = -500;
        internal static bool isAllowedToBeClosedByPlayer = true;

        internal static void Initialize()
        {
            try
            {
                ReplaceMainMenuButton();
                GenerateNetworkUI();
                GenerateConnectUI();
                GenerateInputScreenUI();
                GenerateFavoriteUI();
                GenerateFavoriteListUI();
                GenerateRequestUsernameUI();
                GenerateHostNetworkUI();
                GenerateClientNetworkUI();
                GenerateModMismatchScreenUI();
                GeneratePopUp();

                ModMismatchScreen.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    Close();
                });

                ModMismatchScreen.transform.Find("Button Ok").GetComponent<Button>().onClick.AddListener(() =>
                {
                    Close();
                });
            }
            catch (Exception ex)
            {
                Main.Log($"{ex}");
            }
            readyForCSUpdate = true;
        }

        private static void ReplaceMainMenuButton()
        {
            Transform mainMenu = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu");
            GameObject go = CreateButton(new ButtonBuilder("Multiplayer", "UI_Multiplayer.png", mainMenu, mainMenu.Find("Button Altfuture").gameObject, "Multiplayer"));
            Object.DestroyImmediate(go.GetComponent<MenuSocial>());
            MenuButtonBase bbase = go.AddComponent<MenuButtonBase>();
            MenuButtonBase refBase = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Back to Game").GetComponent<MenuButtonBase>();
            bbase.hoverAudio = refBase.hoverAudio;
            bbase.clickAudio = refBase.clickAudio;
        }

        internal static void Open(MenuScreen screen, bool allowedToBeClosedByPlayer = true, bool allowOutsideClickClose = true)
        {
            isAllowedToBeClosedByPlayer = allowedToBeClosedByPlayer;
            if (currentScreen && currentScreen != InputScreenUI)
                prevScreen = currentScreen;
            currentScreen = screen;
            SingletonBehaviour<CanvasSpawner>.Instance.AllowOutsideClickClose = allowOutsideClickClose;
            SingletonBehaviour<CanvasSpawner>.Instance.Open(screen);
        }

        internal static void Open()
        {
            currentScreen = null;
            SingletonBehaviour<CanvasSpawner>.Instance.Open();
        }

        internal static void Close()
        {
            isAllowedToBeClosedByPlayer = true;
            currentScreen = null;
            SingletonBehaviour<CanvasSpawner>.Instance.Close();
            SingletonBehaviour<CanvasSpawner>.Instance.AllowOutsideClickClose = true;
        }

        internal static void OpenPopup(string title, string message)
        {
            SingletonBehaviour<CanvasSpawner>.Instance.AllowOutsideClickClose = false;
            PopupUI.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = title;
            PopupUI.transform.Find("Label Message").GetComponent<TextMeshProUGUI>().text = message;
            Open(PopupUI, false);
        }

        private static void GenerateModMismatchScreenUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer ModMismatched", "Mod mismatch", 800, 500, true, false));

            CreateSection(new Rect(0f, -245, 750, 255), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            //Max 11 mods or if mod amount > 10 show "and {amount} more"
            CreateLabel("Mismatched", "Your mods and the mods of the host mismatched.\n", menuBuilder.transform, new Rect(32f, -125, 740, 250), FontStyles.Normal, TextAlignmentOptions.TopLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 1f), Color.white, 18);
            CreateSection(new Rect(0f, 115, 750, 91f), RectTransformAnchoring.BottomCenter, menuBuilder.transform, new Vector2(.5f, 1));
            ButtonBuilder buttonBuilder = new ButtonBuilder("Ok", "OK", menuBuilder.transform, new Rect(0f, 107, 608, 76), RectTransformAnchoring.BottomCenter, new Vector2(.5f, 1f), TextAlignmentOptions.Center);
            CreateButton(buttonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            ModMismatchScreen = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Main Menu", "DV Multiplayer", 508, 560, false));

            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect Manually", menuBuilder.transform, new Rect(0f, -177.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an existing server");
            ButtonBuilder connectFavButtonBuilder = new ButtonBuilder("Connect to Favorite", "Connect to Favorite", menuBuilder.transform, new Rect(0f, -257.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an favorited server");
            //ButtonBuilder hostButtonBuilder = new ButtonBuilder("Host", "Host server", menuBuilder.transform, new Rect(0f, -377.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Host a new server");

            CreateSection(new Rect(0f, -218, 458, 168), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateButton(connectButtonBuilder);
            CreateButton(connectFavButtonBuilder);
            CreateSection(new Rect(0f, -377, 458, 91f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            //CreateButton(hostButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            NetworkUI = menu.GetComponent<MenuScreen>();
        }

        private static void GeneratePopUp()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Popup", "Popup", 800, 300));

            CreateSection(new Rect(30f, -270, 740, 150), RectTransformAnchoring.TopLeft, menuBuilder.transform, new Vector2(0, 0));
            CreateLabel("Message", "TEST", menuBuilder.transform, new Rect(32, -270, 738, 145), FontStyles.UpperCase, TextAlignmentOptions.Center, RectTransformAnchoring.TopLeft, new Vector2(0, 0), Color.white);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            PopupUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateHostNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Hosting", "Hosting", 668, 418, false, false));

            ButtonBuilder stopServerButtonBuilder = new ButtonBuilder("Stop Server", "Stop Server", menuBuilder.transform, new Rect(0f, -318.5f, 608, 91f), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center);

            CreateSection(new Rect(0f, -177, 618, 91), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Username", "Connected as: ", menuBuilder.transform, new Rect(0f, -177.5f, 608, 76), FontStyles.Normal, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f));
            CreateSection(new Rect(0f, -318, 618, 91f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateButton(stopServerButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            HostConnectedMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateClientNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Client", "Connected To Server", 668, 418, false, false));

            ButtonBuilder stopServerButtonBuilder = new ButtonBuilder("Disconnect", "Disconnect", menuBuilder.transform, new Rect(0f, -318.5f, 608, 91f), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center);

            CreateSection(new Rect(0f, -177, 618, 91), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Username", "Connected as: ", menuBuilder.transform, new Rect(0f, -177.5f, 608, 76), FontStyles.Normal, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f));
            CreateSection(new Rect(0f, -318, 618, 91f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateButton(stopServerButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            ClientConnectedMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateConnectUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Connect", "Connect to server", 975, 540f, false, false));

            TextFieldBuilder inputIpField = new TextFieldBuilder("IP", menuBuilder.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            TextFieldBuilder inputPortField = new TextFieldBuilder("Port", menuBuilder.transform, new Rect(-32f, -315, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f), true);
            TextFieldBuilder inputUsernameField = new TextFieldBuilder("Username", menuBuilder.transform, new Rect(-32f, -415, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect", menuBuilder.transform, new Rect(0f, -515, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);
            ButtonBuilder favoriteButtonBuilder = new ButtonBuilder("Save as Favorite", "UI_Unfavorited.png", menuBuilder.transform, new Rect(280f, -515, 76, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f));

            CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("IP", "IP:", menuBuilder.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            CreateTextField(inputIpField);

            CreateSection(new Rect(0f, -277, 925, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Port", "Port:", menuBuilder.transform, new Rect(32, -315, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            CreateTextField(inputPortField);

            CreateSection(new Rect(0f, -377, 925, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Username", "Username:", menuBuilder.transform, new Rect(32, -415, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            CreateTextField(inputUsernameField);

            CreateButton(connectButtonBuilder);
            CreateButton(favoriteButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            ConnectMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateFavoriteListUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Favorite Connector", "Connect to Favorite", 700, 640, false, false));

            for (int i = 0; i < 4; i++)
            {
                CreateSection(new Rect(0f, -177 - (100 * i), 650, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
                CreateButton(new ButtonBuilder($"Fav{i + 1}", "", menuBuilder.transform, new Rect(32f, -177.5f - (100 * i), 559, 76), RectTransformAnchoring.TopLeft, new Vector2(0f, .5f), TextAlignmentOptions.MidlineLeft, fontStyle: FontStyles.Normal));
                CreateButton(new ButtonBuilder($"Del Fav{i + 1}", "UI_Bin.png", menuBuilder.transform, new Rect(-32f, -177.5f - (100 * i), 76, 76), RectTransformAnchoring.TopRight, new Vector2(1f, .5f)));
            }
            CreateSection(new Rect(0f, -177 - (100 * 4), 650, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateButton(new ButtonBuilder($"NextPage", ">", menuBuilder.transform, new Rect(-30, -177.5f - (100 * 4), 76, 76), RectTransformAnchoring.TopRight, new Vector2(1f, .5f), TextAlignmentOptions.Center));
            CreateButton(new ButtonBuilder($"PrevPage", "<", menuBuilder.transform, new Rect(30, -177.5f - (100 * 4), 76, 76), RectTransformAnchoring.TopLeft, new Vector2(0f, .5f), TextAlignmentOptions.Center));

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            FavoriteConnectMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateFavoriteUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Favorite", "Favorite Server", 975, 345f, false, false));

            TextFieldBuilder inputNameField = new TextFieldBuilder("Name", menuBuilder.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder acceptButtonBuilder = new ButtonBuilder("Accept", "Save as Favorite", menuBuilder.transform, new Rect(0, -320, 975 / 3, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Name", "Name:", menuBuilder.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            CreateLabel("Error", "", menuBuilder.transform, new Rect(0, -240f, 920, 16), FontStyles.Normal, TextAlignmentOptions.MidlineRight, RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), Color.red, 16);
            CreateTextField(inputNameField);

            CreateButton(acceptButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            SaveFavoriteMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateRequestUsernameUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Username Request", "Enter username please", 975, 345f, false, false));

            TextFieldBuilder inputNameField = new TextFieldBuilder("Username", menuBuilder.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder acceptButtonBuilder = new ButtonBuilder("Accept", "Connect", menuBuilder.transform, new Rect(0, -320, 975 / 3, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Username", "Username:", menuBuilder.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            CreateTextField(inputNameField);

            CreateButton(acceptButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            UsernameRequestMenuUI = menu.GetComponent<MenuScreen>();
        }


        private static void GenerateInputScreenUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer InputMenu", "Input", 975, 672.6f, false, false));
            menuBuilder.AddComponent<InputScreen>();

            CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            CreateLabel("Input", "", menuBuilder.transform, new Rect(32, -215, 861, 76), FontStyles.Normal, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));

            char[] row = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-' };
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", menuBuilder.transform, new Rect(925 - (80 * 11) + (80 * i), -300, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            row = new char[] { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p' };
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", menuBuilder.transform, new Rect(925 - (80 * 10.5f) + (80 * i), -380, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            row = new char[] { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l' };
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", menuBuilder.transform, new Rect(925 - (80 * 10) + (80 * i), -460, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            row = new char[] { 'z', 'x', 'c', 'v', 'b', 'n', 'm', '.' };
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", menuBuilder.transform, new Rect(925 - (80 * 9.5f) + (80 * i), -540, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            ButtonBuilder builderBtn = new ButtonBuilder($"Backspace", $"Backspace", menuBuilder.transform, new Rect(925 - (80 * 9f) - 60, -620, 180, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject backspaceBtn = CreateButton(builderBtn);
            InputButton inputbtn = backspaceBtn.AddComponent<InputButton>();
            inputbtn.isBackspace = true;

            builderBtn = new ButtonBuilder($"Paste", $"Paste", menuBuilder.transform, new Rect(925 - (80 * 9f) + 125, -620, 150, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject pasteBtn = CreateButton(builderBtn);
            InputButton inputPastebtn = pasteBtn.AddComponent<InputButton>();
            inputPastebtn.isPaste = true;

            builderBtn = new ButtonBuilder($"Casing", $"Uppercase", menuBuilder.transform, new Rect(925 - (80 * 9f) + 280, -620, 180, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            CreateButton(builderBtn);

            builderBtn = new ButtonBuilder($"Confirm", $"Confirm", menuBuilder.transform, new Rect(925 - (80 * 9f) + 465, -620, 150, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            CreateButton(builderBtn);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            InputScreenUI = menu.GetComponent<MenuScreen>();
        }

        private static GameObject CreateMenu(MenuBuilder menuBuilder)
        {
            menuOffset -= menuBuilder.widthAndHeight.x + 100;

            GameObject refMenu = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").gameObject;
            GameObject newMenu = Object.Instantiate(refMenu, SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform);
            newMenu.name = menuBuilder.name;
            DestroyChildren(newMenu);

            RectTransform transform = newMenu.GetComponent<RectTransform>();
            transform.sizeDelta = menuBuilder.widthAndHeight;
            transform.localPosition = new Vector3(menuOffset, 0);

            GameObject section = CreateTextBackground(new Rect(0, -24.39999f, 458, 80), RectTransformAnchoring.TopCenter, newMenu.transform, new Vector2(.5f, 1));
            transform = section.GetComponent<RectTransform>();
            transform.sizeDelta = new Vector2(menuBuilder.widthAndHeight.x - 50, transform.sizeDelta.y);

            if (menuBuilder.withButton)
            {
                ButtonBuilder closeButtonBuilder = new ButtonBuilder("Close", (menuBuilder.isClose ? "UI_Close.png" : "UI_ArrowLeft.png"), newMenu.transform, new Rect(65, -64.5f, 76, 76), RectTransformAnchoring.TopLeft, new Vector2(.5f, .5f), "Close menu");
                CreateButton(closeButtonBuilder);
            }

            GameObject refTitle = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("title").gameObject;
            GameObject title = Object.Instantiate(refTitle, newMenu.transform);
            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
            title.name = "Title";
            titleText.text = menuBuilder.title;

            if (menuBuilder.withTooltip)
            {
                GameObject tooltipSection = CreateTextBackground(new Rect(0, 70, 458, 76), RectTransformAnchoring.BottomCenter, newMenu.transform, new Vector2(.5f, .5f));
                transform = tooltipSection.GetComponent<RectTransform>();
                transform.sizeDelta = new Vector2(menuBuilder.widthAndHeight.x - 50, transform.sizeDelta.y);

                GameObject refTooltip = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Tooltip").gameObject;
                GameObject tooltip = Object.Instantiate(refTooltip, newMenu.transform);
                transform = tooltip.GetComponent<RectTransform>();
                transform.ChangeRect(new Rect(0, 96.5f, menuBuilder.widthAndHeight.x - 83, transform.sizeDelta.y));
            }

            return newMenu;
        }

        private static GameObject CreateButton(ButtonBuilder buttonBuilder)
        {
            GameObject newButton = buttonBuilder.btn;
            if (!newButton && buttonBuilder.type == ButtonType.Text)
            {
                GameObject refButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Fast Travel Menu").Find("Button teleport with loco").gameObject;
                newButton = Object.Instantiate(refButton, buttonBuilder.parent);
            }
            else if (!newButton && buttonBuilder.type == ButtonType.Icon)
            {
                GameObject refSpriteButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Back to Game").gameObject;
                newButton = Object.Instantiate(refSpriteButton, buttonBuilder.parent);
            }

            UpdateButtonTransform(newButton, buttonBuilder);

            newButton.name = $"Button {buttonBuilder.name}";
            if (buttonBuilder.type == ButtonType.Icon)
            {
                newButton.transform.Find("image").GetComponent<Image>().SetSprite(buttonBuilder.icon);
            }
            else
            {
                TextMeshProUGUI text = newButton.transform.Find("label").GetComponent<TextMeshProUGUI>();
                text.text = buttonBuilder.label;
                text.alignment = buttonBuilder.textAlignment;
                text.fontStyle = buttonBuilder.fontStyle;
            }

            UIElementTooltip tooltip = newButton.GetComponent<UIElementTooltip>();
            if (buttonBuilder.btn)
            {
                tooltip.TooltipInteractableText = buttonBuilder.tooltipEnabledText;
                tooltip.TooltipNonInteractableText = buttonBuilder.tooltipDisabledText;
            }
            else
            {
                tooltip.tooltipEnabledText = buttonBuilder.tooltipEnabledText;
                tooltip.tooltipDisabledText = buttonBuilder.tooltipDisabledText;
            }

            newButton.GetComponent<Button>().onClick.RemoveAllListeners();

            newButton.AddComponent<ButtonFeatures>();

            return newButton;
        }

        private static GameObject CreateLabel(string name, string labelText, Transform parent, Rect rect, FontStyles fontStyle, TextAlignmentOptions textAlignment, RectTransformAnchoring anchor, Vector2 pivot, Color? color = null, float? fontSize = null)
        {
            GameObject refLabel = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("title").gameObject;
            GameObject label = Object.Instantiate(refLabel, parent);
            label.name = $"Label {name}";

            TextMeshProUGUI titleText = label.GetComponent<TextMeshProUGUI>();
            titleText.text = labelText;
            titleText.alignment = textAlignment;
            titleText.fontStyle = fontStyle;
            if (color.HasValue)
                titleText.color = color.Value;

            if (fontSize.HasValue)
                titleText.fontSize = fontSize.Value;

            RectTransform transform = label.GetComponent<RectTransform>();
            transform.pivot = pivot;
            transform.ChangeAnchors(anchor);
            transform.ChangeRect(rect);

            return label;
        }

        private static GameObject CreateTextField(TextFieldBuilder textFieldBuilder)
        {
            GameObject refButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Fast Travel Menu").Find("Button teleport with loco").gameObject;
            GameObject newTextField = Object.Instantiate(refButton, textFieldBuilder.parent);
            UpdateTextFieldTransform(newTextField, textFieldBuilder);

            newTextField.name = $"TextField {textFieldBuilder.name}";
            TextMeshProUGUI text = newTextField.transform.Find("label").GetComponent<TextMeshProUGUI>();
            text.alignment = textFieldBuilder.textAlignment;
            text.fontStyle = FontStyles.Normal;
            text.text = "";

            TextField field = newTextField.AddComponent<TextField>();
            field.title = textFieldBuilder.name;
            field.isDigitOnly = textFieldBuilder.isDigitOnly;

            return newTextField;
        }

        private static GameObject CreateSection(Rect rect, RectTransformAnchoring anchor, Transform parent, Vector2? pivot = null)
        {
            GameObject refSection = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Section").gameObject;
            GameObject newSection = Object.Instantiate(refSection, parent);
            RectTransform transform = newSection.GetComponent<RectTransform>();

            transform.ChangeAnchors(anchor);
            if (pivot.HasValue)
                transform.pivot = pivot.Value;
            transform.ChangeRect(rect);

            return newSection;
        }

        private static GameObject CreateTextBackground(Rect rect, RectTransformAnchoring anchor, Transform parent, Vector2 pivot)
        {
            GameObject refSection = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Title bg").gameObject;
            GameObject newTextBackground = Object.Instantiate(refSection, parent);
            RectTransform transform = newTextBackground.GetComponent<RectTransform>();

            transform.ChangeAnchors(anchor);
            transform.ChangeRect(rect);
            transform.pivot = pivot;

            return newTextBackground;
        }

        private static void UpdateButtonTransform(GameObject btn, ButtonBuilder buttonBuilder)
        {
            RectTransform transform = btn.GetComponent<RectTransform>();

            if (buttonBuilder.anchor != RectTransformAnchoring.NotSet)
            {
                transform.ChangeAnchors(buttonBuilder.anchor);
            }

            if (buttonBuilder.pivot.HasValue)
            {
                transform.pivot = buttonBuilder.pivot.Value;
            }

            if (buttonBuilder.rect.HasValue)
            {
                transform.ChangeRect(buttonBuilder.rect.Value);
                transform.Find("image hover").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, buttonBuilder.rect.Value.width, buttonBuilder.rect.Value.height));
                transform.Find("image click").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, buttonBuilder.rect.Value.width, buttonBuilder.rect.Value.height));
                if (buttonBuilder.type == ButtonType.Icon)
                    transform.Find("image").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, buttonBuilder.rect.Value.width, buttonBuilder.rect.Value.height));
            }
        }

        private static void UpdateTextFieldTransform(GameObject btn, TextFieldBuilder textFieldBuilder)
        {
            RectTransform transform = btn.GetComponent<RectTransform>();

            if (textFieldBuilder.anchor != RectTransformAnchoring.NotSet)
            {
                transform.ChangeAnchors(textFieldBuilder.anchor);
            }

            if (textFieldBuilder.pivot.HasValue)
            {
                transform.pivot = textFieldBuilder.pivot.Value;
            }

            if (textFieldBuilder.rect.HasValue)
            {
                transform.ChangeRect(textFieldBuilder.rect.Value);
                transform.Find("image hover").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, textFieldBuilder.rect.Value.width, textFieldBuilder.rect.Value.height));
                transform.Find("image click").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, textFieldBuilder.rect.Value.width, textFieldBuilder.rect.Value.height));
            }
        }

        private struct MenuBuilder
        {
            public readonly string name;
            public readonly string title;
            public readonly Vector2 widthAndHeight;
            public readonly bool withTooltip;
            public readonly bool withButton;
            public readonly bool isClose;

            public MenuBuilder(string name, string title, float width, float height, bool isClose)
            {
                this.name = name;
                this.title = title;
                this.widthAndHeight = new Vector2(width, height);
                withTooltip = true;
                withButton = true;
                this.isClose = isClose;
            }

            public MenuBuilder(string name, string title, float width, float height, bool isClose, bool tooltip)
            {
                this.name = name;
                this.title = title;
                this.widthAndHeight = new Vector2(width, height);
                withTooltip = tooltip;
                withButton = true;
                this.isClose = isClose;
            }

            public MenuBuilder(string name, string title, float width, float height)
            {
                this.name = name;
                this.title = title;
                this.widthAndHeight = new Vector2(width, height);
                withTooltip = false;
                this.isClose = false;
                withButton = false;
            }
        }

        private struct ButtonBuilder
        {
            public readonly string name;
            public readonly ButtonType type;
            public readonly string icon;
            public readonly string label;
            public readonly Transform parent;
            public readonly GameObject btn;
            public readonly Rect? rect;
            public readonly TextAlignmentOptions textAlignment;
            public readonly RectTransformAnchoring anchor;
            public readonly Vector2? pivot;
            public readonly string tooltipEnabledText;
            public readonly string tooltipDisabledText;
            public readonly FontStyles fontStyle;

            public ButtonBuilder(string name, string icon, Transform parent, Rect pos, RectTransformAnchoring anchor, Vector2 pivot, string tooltipEnabledText = "", string tooltipDisabledText = "")
            {
                this.name = name;
                this.type = ButtonType.Icon;
                this.icon = icon;
                this.label = "";
                this.parent = parent;
                this.btn = null;
                this.rect = pos;
                this.textAlignment = TextAlignmentOptions.Left;
                this.anchor = anchor;
                this.pivot = pivot;
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
                fontStyle = FontStyles.UpperCase;
            }

            public ButtonBuilder(string name, string label, Transform parent, Rect pos, RectTransformAnchoring anchor, Vector2 pivot, TextAlignmentOptions textAlignment, string tooltipEnabledText = "", string tooltipDisabledText = "", FontStyles fontStyle = FontStyles.UpperCase)
            {
                this.name = name;
                this.type = ButtonType.Text;
                this.icon = "";
                this.label = label;
                this.parent = parent;
                this.btn = null;
                this.rect = pos;
                this.textAlignment = textAlignment;
                this.anchor = anchor;
                this.pivot = pivot;
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
                this.fontStyle = fontStyle;
            }

            public ButtonBuilder(string name, string icon, Transform parent, GameObject btn = null, string tooltipEnabledText = "", string tooltipDisabledText = "")
            {
                this.name = name;
                this.type = ButtonType.Icon;
                this.icon = icon;
                this.label = "";
                this.parent = parent;
                this.btn = btn;
                this.rect = null;
                this.textAlignment = TextAlignmentOptions.Left;
                this.anchor = RectTransformAnchoring.NotSet;
                this.pivot = null;
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
                fontStyle = FontStyles.UpperCase;
            }

            public ButtonBuilder(string name, string label, Transform parent, TextAlignmentOptions textAlignment, GameObject btn = null, string tooltipEnabledText = "", string tooltipDisabledText = "", FontStyles fontStyle = FontStyles.UpperCase)
            {
                this.name = name;
                this.type = ButtonType.Text;
                this.icon = "";
                this.label = label;
                this.parent = parent;
                this.btn = btn;
                this.rect = null;
                this.textAlignment = textAlignment;
                this.anchor = RectTransformAnchoring.NotSet;
                this.pivot = null;
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
                this.fontStyle = fontStyle;
            }
        }

        private struct TextFieldBuilder
        {
            public readonly string name;
            public readonly Transform parent;
            public readonly Rect? rect;
            public readonly TextAlignmentOptions textAlignment;
            public readonly RectTransformAnchoring anchor;
            public readonly Vector2? pivot;
            public readonly bool isDigitOnly;

            public TextFieldBuilder(string name, Transform parent, Rect pos, TextAlignmentOptions textAlignment, RectTransformAnchoring anchor, Vector2 pivot, bool isDigitOnly = false)
            {
                this.name = name;
                this.parent = parent;
                this.rect = pos;
                this.textAlignment = textAlignment;
                this.anchor = anchor;
                this.pivot = pivot;
                this.isDigitOnly = isDigitOnly;
            }
        }

        private enum ButtonType
        {
            Icon,
            Text
        }

        private static void DestroyChildren(GameObject gameObject, string prefixToDelete = "")
        {
            if (prefixToDelete != "")
            {
                for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
                {
                    if (gameObject.transform.GetChild(i).gameObject.name.StartsWith(prefixToDelete))
                        Object.DestroyImmediate(gameObject.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(gameObject.transform.GetChild(i).gameObject);
                }
            }
        }
    }
}