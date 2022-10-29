using DV;
using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;
using Main = DVMultiplayer.Main;

public class NetworkTrainSync : MonoBehaviour
{
    public TrainCar loco;
    public SteamLocoSimulation steamSim;

    public void Awake()
    {
        Main.Log($"NetworkTrainSync.Awake()");
        loco = GetComponent<TrainCar>();
        if (loco.GetComponent<SteamLocoSimulation>() != null)
        {
            steamSim = loco.GetComponent<SteamLocoSimulation>();
            StartCoroutine(CheckLocoValues());
            StartCoroutine(MuteWhistle(loco));
        }
        loco.LoadInterior();
        loco.keepInteriorLoaded = true;
        StartCoroutine(PeriodicServerStateUpdate());
#if DEBUG
        AddInfotagAboveTrain();
#endif
    }

    public void Update()
    {
#if DEBUG
        WorldTrain serverState = SingletonBehaviour<NetworkTrainManager>.Instance.serverCarStates.FirstOrDefault(s => s.Guid == loco.CarGUID);
        //Main.Log($"{serverState.AuthorityPlayerId}");
        NetworkPlayerSync playerSync = new NetworkPlayerSync();
        if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Id == serverState.AuthorityPlayerId)
        {
            playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();
        }
        else
        {
            playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerSyncById(serverState.AuthorityPlayerId);
        }
        if (playerSync != null)
        {
            //Main.Log($"{playerSync.Username}");
            loco.GetComponentInChildren<Text>().text = $"Authority: {playerSync.Username}";
        }
#endif
    }

    public IEnumerator CheckLocoValues()
    {
        while (true)
        {
            if (steamSim != null)
            {
                var fireOn = steamSim.fireOn.value;
                var fireboxCoal = steamSim.coalbox.value;
                var tenderCoal = steamSim.tenderCoal.value;
                yield return new WaitUntil(() => steamSim.fireOn.value != fireOn || steamSim.coalbox.value > fireboxCoal || steamSim.tenderCoal.value < tenderCoal);
            }
            else
                yield break;

            if (!SingletonBehaviour<NetworkTrainManager>.Instance || !loco || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
                continue;

            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoValue(loco);
        }
    }

    public IEnumerator PeriodicServerStateUpdate()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(5);
            if (SingletonBehaviour<NetworkTrainManager>.Exists && loco && loco.GetComponent<NetworkTrainPosSync>().hasLocalPlayerAuthority)
                SingletonBehaviour<NetworkTrainManager>.Instance.UpdateLocoValue(loco);
        }
    }

    private void AddInfotagAboveTrain()
    {
        GameObject infotagCanvas = new GameObject("Infotag canvas");
        infotagCanvas.transform.parent = loco.transform;
        infotagCanvas.transform.localPosition = new Vector3(0, 5, 0);
        infotagCanvas.AddComponent<Canvas>();
        infotagCanvas.AddComponent<RotateTowardsPlayer>();

        RectTransform rectTransform = infotagCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920, 1080);
        rectTransform.localScale = new Vector3(0.004f, .001f, 0);

        GameObject infotagBackground = new GameObject("Infotag BG");
        infotagBackground.transform.parent = infotagCanvas.transform;
        infotagBackground.transform.localPosition = new Vector3(0, 0, 0);

        RawImage bg = infotagBackground.AddComponent<RawImage>();
        bg.color = new Color(69 / 255, 69 / 255, 69 / 255, .45f);

        rectTransform = infotagBackground.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 1f, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);

        GameObject infotag = new GameObject("Infotag");
        infotag.transform.parent = infotagCanvas.transform;
        infotag.transform.localPosition = new Vector3(775, 0, 0);

        Text tag = infotag.AddComponent<Text>();
        tag.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        tag.fontSize = 300;
        tag.alignment = TextAnchor.MiddleCenter;
        tag.resizeTextForBestFit = true;
        tag.text = "Authority: Nobody";

        rectTransform = infotag.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(2f, 5f, 0);
        rectTransform.anchorMin = new Vector2(0, .5f);
        rectTransform.anchorMax = new Vector2(0, .5f);
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);
        rectTransform.sizeDelta = new Vector2(1575, 350);
    }

    private IEnumerator MuteWhistle(TrainCar car)
    {
        yield return new WaitUntil(() => car.IsInteriorLoaded);
        yield return new WaitForSeconds(.1f);
        switch (car.carType)
        {
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                WhistleRopeInit whistle = car.interior.GetComponentInChildren<WhistleRopeInit>();
                whistle.muted = true;
                break;
        }
    }
}