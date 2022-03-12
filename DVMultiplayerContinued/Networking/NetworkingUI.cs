using DVMultiplayer.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DVMultiplayer.Networking
{
    public class NetworkingUI
    {
        private MenuScreen UI;
        private MenuScreen ConnectUI;
        private MenuScreen HostUI;
        private MenuScreen SaveFavoriteUI;
        private MenuScreen FavoritesListUI;
        private MenuScreen RequestUsernameUI;
        private MenuScreen HostConnectedMenuUI;
        private MenuScreen ClientConnectedMenuUI;

        internal void Setup()
        {
            SingletonBehaviour<CoroutineManager>.Instance.Run(SetupCoroutine());
        }

        private IEnumerator SetupCoroutine()
        {
            yield return new WaitUntil(() => CustomUI.CSUpdateFinished);

            UI = CustomUI.NetworkUI;
            ConnectUI = CustomUI.ConnectMenuUI;
            HostUI = CustomUI.HostMenuUI;
            SaveFavoriteUI = CustomUI.SaveFavoriteMenuUI;
            FavoritesListUI = CustomUI.FavoriteConnectMenuUI;
            RequestUsernameUI = CustomUI.UsernameRequestMenuUI;
            HostConnectedMenuUI = CustomUI.HostConnectedMenuUI;
            ClientConnectedMenuUI = CustomUI.ClientConnectedMenuUI;
            int pagination = 0;
            Favorite selectedFav = null;

            UI.transform.Find("Button Connect to Favorite").GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedFav = null;
                pagination = 0;
                LoadFavorites(pagination);
                CustomUI.Open(FavoritesListUI);
            });

            FavoritesListUI.transform.Find("Button Fav1").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav1").GetComponentInChildren<TextMeshProUGUI>().text;
                selectedFav = FavoritesManager.Find(name);
                CustomUI.Open(RequestUsernameUI);
            });

            FavoritesListUI.transform.Find("Button Fav2").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav2").GetComponentInChildren<TextMeshProUGUI>().text;
                selectedFav = FavoritesManager.Find(name);
                CustomUI.Open(RequestUsernameUI);
            });

            FavoritesListUI.transform.Find("Button Fav3").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav3").GetComponentInChildren<TextMeshProUGUI>().text;
                selectedFav = FavoritesManager.Find(name);
                CustomUI.Open(RequestUsernameUI);
            });

            FavoritesListUI.transform.Find("Button Fav4").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav4").GetComponentInChildren<TextMeshProUGUI>().text;
                selectedFav = FavoritesManager.Find(name);
                CustomUI.Open(RequestUsernameUI);
            });

            FavoritesListUI.transform.Find("Button Del Fav1").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav1").GetComponentInChildren<TextMeshProUGUI>().text;
                FavoritesManager.Delete(name);
                pagination = 0;
                LoadFavorites(pagination);
            });

            FavoritesListUI.transform.Find("Button Del Fav2").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav2").GetComponentInChildren<TextMeshProUGUI>().text;
                FavoritesManager.Delete(name);
                pagination = 0;
                LoadFavorites(pagination);
            });

            FavoritesListUI.transform.Find("Button Del Fav3").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav3").GetComponentInChildren<TextMeshProUGUI>().text;
                FavoritesManager.Delete(name);
                pagination = 0;
                LoadFavorites(pagination);
            });

            FavoritesListUI.transform.Find("Button Del Fav4").GetComponent<Button>().onClick.AddListener(() =>
            {
                string name = FavoritesListUI.transform.Find($"Button Fav4").GetComponentInChildren<TextMeshProUGUI>().text;
                FavoritesManager.Delete(name);
                pagination = 0;
                LoadFavorites(pagination);
            });

            RequestUsernameUI.transform.Find("Button Accept").GetComponent<Button>().onClick.AddListener(() =>
            {
                string username = RequestUsernameUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;
                if (!string.IsNullOrWhiteSpace(username))
                {
                    NetworkManager.Connect(selectedFav.Hostname, selectedFav.Port, username);
                    HideUI();
                }
            });

            RequestUsernameUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedFav = null;
                LoadFavorites(pagination);
                CustomUI.Open(FavoritesListUI);
            });

            UI.transform.Find("Button Connect").GetComponent<Button>().onClick.AddListener(() =>
            {
                ConnectUI.transform.Find("TextField IP").GetComponentInChildren<TextMeshProUGUI>().text = "";
                ConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text = "4296";
                ConnectUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text = "";
                ConnectUI.transform.Find("Button Save as Favorite").Find("image").GetComponent<Image>().SetSprite("UI_Unfavorited.png");
                CustomUI.Open(ConnectUI);
            });

            ConnectUI.transform.Find("Button Connect").GetComponent<Button>().onClick.AddListener(() =>
            {
                string host = ConnectUI.transform.Find("TextField IP").GetComponentInChildren<TextMeshProUGUI>().text;
                string portString = ConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                string username = ConnectUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;
                if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(portString) && int.TryParse(portString, out int port) && !string.IsNullOrWhiteSpace(username))
                {
                    NetworkManager.Connect(host, port, username);
                    HideUI();
                }
            });

            ConnectUI.transform.Find("Button Save as Favorite").GetComponent<Button>().onClick.AddListener(() =>
            {
                string host = ConnectUI.transform.Find("TextField IP").GetComponentInChildren<TextMeshProUGUI>().text;
                string portString = ConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(portString) && int.TryParse(portString, out int port))
                {
                    CustomUI.Open(SaveFavoriteUI);
                }
            });

            SaveFavoriteUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open(ConnectUI);
            });

            SaveFavoriteUI.transform.Find("Button Accept").GetComponent<Button>().onClick.AddListener(() =>
            {
                string favName = SaveFavoriteUI.transform.Find("TextField Name").GetComponentInChildren<TextMeshProUGUI>().text;
                if (string.IsNullOrWhiteSpace(favName))
                    return;
                string host = ConnectUI.transform.Find("TextField IP").GetComponentInChildren<TextMeshProUGUI>().text;
                string portString = ConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                int.TryParse(portString, out int port);
                try
                {
                    FavoritesManager.SaveAsFavorite(favName, host, port);
                    ConnectUI.transform.Find("Button Save as Favorite").Find("image").GetComponent<Image>().SetSprite("UI_Favorited.png");
                    CustomUI.Open(ConnectUI);
                }
                catch (Exception ex)
                {
                    Main.Log(ex.Message);
                    SaveFavoriteUI.transform.Find("Label Error").GetComponentInChildren<TextMeshProUGUI>().text = ex.Message;
                }
            });

            UI.transform.Find("Button Host").GetComponent<Button>().onClick.AddListener(() =>
            {
                HostUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text = "4296";
                HostUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text = "";
                CustomUI.Open(HostUI);
            });

            HostUI.transform.Find("Button Host").GetComponent<Button>().onClick.AddListener(() =>
            {
                string portString = HostUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                string username = HostUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;

                bool portValid = ushort.TryParse(portString, out ushort port) && port < 65535 && port > 0;
                if (!portValid)
                    port = 4296;

                if (!string.IsNullOrWhiteSpace(username))
                {
                    NetworkManager.StartServer(username, port);
                    HideUI();
                }
            });

            UI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open();
            });

            ClientConnectedMenuUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open();
            });

            HostConnectedMenuUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open();
            });

            HostConnectedMenuUI.transform.Find("Button Stop Server").GetComponent<Button>().onClick.AddListener(() =>
            {
                NetworkManager.StopServer();
                HideUI();
            });

            ClientConnectedMenuUI.transform.Find("Button Disconnect").GetComponent<Button>().onClick.AddListener(() =>
            {
                NetworkManager.Disconnect();
                HideUI();
            });

            FavoritesListUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open(UI);
            });

            ConnectUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open(UI);
            });

            HostUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
            {
                CustomUI.Open(UI);
            });

            Object.DestroyImmediate(SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Multiplayer").GetComponent<MenuSocial>());
            SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Multiplayer").GetComponent<Button>().onClick.AddListener(() =>
            {
                UI.transform.Find("Button Connect").GetComponent<Button>().interactable = !(TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress);
                UI.transform.Find("Button Connect").GetComponent<UIElementTooltip>().TooltipNonInteractableText = TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress ? "Finish the tutorial first" : "";
                UI.transform.Find("Button Host").GetComponent<Button>().interactable = !(TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress);
                UI.transform.Find("Button Host").GetComponent<UIElementTooltip>().TooltipNonInteractableText = TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress ? "Finish the tutorial first" : "";

                if (NetworkManager.IsClient() && NetworkManager.IsHost())
                {
                    HostConnectedMenuUI.transform.Find("Label Username").GetComponent<TextMeshProUGUI>().text = $"Connected as: {NetworkManager.username}";
                    CustomUI.Open(HostConnectedMenuUI);
                }
                else if (NetworkManager.IsClient() && !NetworkManager.IsHost()) 
                {
                    ClientConnectedMenuUI.transform.Find("Label Username").GetComponent<TextMeshProUGUI>().text = $"Connected as: {NetworkManager.username}";
                    CustomUI.Open(ClientConnectedMenuUI);
                }
                else
                {
                    CustomUI.Open(UI);
                }
            });
        }

        private void LoadFavorites(int pagination)
        {
            List<Favorite> favorites = FavoritesManager.GetFavorites();
            for (int i = 0; i < 4; i++)
            {
                FavoritesListUI.transform.Find($"Button Fav{i + 1}").gameObject.SetActive(true);
                Favorite favorite = null;
                if (i + (pagination * 4) < favorites.Count)
                {
                    favorite = favorites[i + (pagination * 4)];
                }
                if (favorite != null)
                    FavoritesListUI.transform.Find($"Button Fav{i + 1}").GetComponentInChildren<TextMeshProUGUI>().text = favorite.Name;
                else
                {
                    FavoritesListUI.transform.Find($"Button Fav{i + 1}").gameObject.SetActive(false);
                    FavoritesListUI.transform.Find($"Button Del Fav{i + 1}").gameObject.SetActive(false);
                }
            }

            FavoritesListUI.transform.Find($"Button NextPage").gameObject.SetActive(favorites.Count > 5 + (pagination * 4));
            FavoritesListUI.transform.Find($"Button PrevPage").gameObject.SetActive(pagination > 0);
        }

        internal void HideUI()
        {
            CustomUI.Close();
        }
    }
}
