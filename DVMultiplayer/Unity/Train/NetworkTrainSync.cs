using DV.CabControls;
using DV.CabControls.Spec;
using DVMultiplayer;
using DVMultiplayer.DTO.Train.Locomotives;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using UnityEngine;

internal class NetworkTrainSync : MonoBehaviour
{
    public TrainCar loco;
    public bool listenToLocalPlayerInputs = false;
    private LocoControllerBase baseController;
    private bool isAlreadyListening = false;

    public void ListenToTrainInputEvents()
    {
        if (!loco.IsLoco && isAlreadyListening)
            return;

        StartCoroutine(CoroListenToEvents());

        isAlreadyListening = true;
    }

    private IEnumerator CoroListenToEvents()
    {
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to base loco controller");
        baseController = loco.GetComponent<LocoControllerBase>();
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen throttle change on base loco controller");
        baseController.ThrottleUpdated += OnTrainThrottleChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen brake change on base loco controller");
        baseController.BrakeUpdated += OnTrainBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated += OnTrainIndependentBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen reverser change on base loco controller");
        baseController.ReverserUpdated += OnTrainReverserStateChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen sander change on base loco controller");
        baseController.SandersUpdated += OnTrainSanderChanged;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to specific train events");

        switch (loco.carType)
        {
            case TrainCarType.LocoShunter:
                ShunterDashboardControls shunterDashboard = loco.interior.GetComponentInChildren<ShunterDashboardControls>();
                CabInputShunter cabInputShunter = loco.interior.GetComponentInChildren<CabInputShunter>();
                FuseBoxPowerController fuseBox = shunterDashboard.fuseBoxPowerController;
                for (int i = 0; i < fuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = fuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged += OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged += OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                fuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged += OnTrainMainFuseChanged;
                shunterDashboard.hornObj.GetComponent<ControlImplBase>().ValueChanged += ShunterHornUsed;
                SingletonBehaviour<CoroutineManager>.Instance.Run(ShunterRotaryAmplitudeCheckerStartListen(fuseBox));
                break;

            case TrainCarType.LocoDiesel:
                DieselDashboardControls dieselDashboard = loco.interior.GetComponentInChildren<DieselDashboardControls>();
                FuseBoxPowerControllerDiesel dieselFuseBox = dieselDashboard.fuseBoxPowerControllerDiesel;
                for (int i = 0; i < dieselFuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = dieselFuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged += OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged += OnTrainSideFuse_2Changed;
                            break;

                        case 2:
                            sideFuse.ValueChanged += OnTrainSideFuse_3Changed;
                            break;
                    }
                }
                dieselFuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged += OnTrainMainFuseChanged;
                dieselDashboard.hornObj.GetComponent<ControlImplBase>().ValueChanged += DieselHornUsed;
                SingletonBehaviour<CoroutineManager>.Instance.Run(DieselRotaryAmplitudeCheckerStartListen(dieselFuseBox));
                yield return new WaitUntil(() => cabInputShunter.ctrl);
                fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
                break;

            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                Main.Log($"Get Steamer scripts");
                LocoControllerSteam steamerController = baseController as LocoControllerSteam;
                CabInputSteam cabInputSteamer = loco.interior.GetComponentInChildren<CabInputSteam>();
                CabInputSteamExtra cabInputSteamerExtra = loco.interior.GetComponentInChildren<CabInputSteamExtra>();
                Main.Log($"Wait till all scripts are loaded");
                yield return new WaitUntil(() => cabInputSteamerExtra.ctrl);
                Main.Log($"Listening to draft puller");
                cabInputSteamerExtra.draftPullerCtrl.ValueChanged += OnSteamerDraftPullerChanged;
                Main.Log($"Listening to water injector");
                cabInputSteamerExtra.injectorCtrl.ValueChanged += OnSteamerWaterInjectorChanged;
                Main.Log($"Listening to fire door");
                cabInputSteamerExtra.fireDoorLeverCtrl.ValueChanged += OnSteamerFireboxDoorChanged;
                Main.Log($"Listening to blower valve");
                cabInputSteamerExtra.blowerValveObj.GetComponent<ControlImplBase>().ValueChanged += OnSteamerBlowerChanged;
                Main.Log($"Listening to steam releaser valve");
                cabInputSteamerExtra.steamReleaserValveObj.GetComponent<ControlImplBase>().ValueChanged += OnSteamerSteamReleaserChanged;
                Main.Log($"Listening to water dump valve");
                cabInputSteamerExtra.waterDumpValveObj.GetComponent<ControlImplBase>().ValueChanged += OnSteamerWaterDumpChanged;
                break;
        }
    }

    private void OnSteamerWaterDumpChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.WaterDump, e.newValue);
    }

    private void OnSteamerSteamReleaserChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SteamRelease, e.newValue);
    }

    internal void OnSteamerCoalShoveled(float value)
    {
        if ((SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork) || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Coal, value);
    }

    internal void OnSteamerWhistleChanged(float val)
    {
        if ((SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork) || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Horn, val);
    }

    internal void OnSteamerFireOnChanged(float percentage)
    {
        if ((SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork) || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Fire, percentage);
    }

    private void OnSteamerBlowerChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Blower, e.newValue);
    }

    private void OnSteamerFireboxDoorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FireboxDoor, e.newValue);
    }

    private void OnSteamerWaterInjectorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.WaterInjector, e.newValue);
    }

    private void OnSteamerDraftPullerChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.DraftPuller, e.newValue);
    }

    public void StopListeningToTrainInputEvents()
    {
        if (!loco || !loco.IsLoco)
            return;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening throttle change on base loco controller");
        baseController.ThrottleUpdated -= OnTrainThrottleChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening brake change on base loco controller");
        baseController.BrakeUpdated -= OnTrainBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated -= OnTrainIndependentBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening reverser change on base loco controller");
        baseController.ReverserUpdated -= OnTrainReverserStateChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening sander change on base loco controller");
        baseController.SandersUpdated -= OnTrainSanderChanged;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening to train specific events");
        switch (loco.carType)
        {
            case TrainCarType.LocoShunter:
                FuseBoxPowerController fuseBox = loco.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController;
                for (int i = 0; i < fuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = fuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged -= OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged -= OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                fuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged -= OnTrainMainFuseChanged;
                fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged -= OnTrainFusePowerStarterStateChanged;
                break;

            case TrainCarType.LocoDiesel:
                FuseBoxPowerControllerDiesel dieselFuseBox = loco.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel;
                for (int i = 0; i < dieselFuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = dieselFuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged -= OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged -= OnTrainSideFuse_2Changed;
                            break;

                        case 2:
                            sideFuse.ValueChanged -= OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                dieselFuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged -= OnTrainMainFuseChanged;
                dieselFuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged -= OnTrainFusePowerStarterStateChanged;
                break;
        }
    }

    public void Awake()
    {
        Main.Log($"NetworkTrainSync.Awake()");
        loco = GetComponent<TrainCar>();
    }

    private IEnumerator ShunterRotaryAmplitudeCheckerStartListen(FuseBoxPowerController fuseBox)
    {
        yield return new WaitUntil(() => fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>() != null);
        fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
    }
    private IEnumerator DieselRotaryAmplitudeCheckerStartListen(FuseBoxPowerControllerDiesel fuseBox)
    {
        yield return new WaitUntil(() => fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>() != null);
        fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
    }

    private void ShunterHornUsed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        float val = e.newValue;
        if (val < .7f && val > .3f)
            val = 0;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Horn, val);
    }

    private void DieselHornUsed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        float val = e.newValue;
        if (val < .7f && val > .3f)
            val = 0;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Horn, val);
    }

    private void OnTrainFusePowerStarterStateChanged(int state)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        float val = .5f;
        if (state == -1)
            val = 0;
        else if (state == 1)
            val = 1;
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FusePowerStarter, val);
    }

    private void OnTrainSideFuse_3Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SideFuse_3, e.newValue);
    }

    private void OnTrainSideFuse_2Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SideFuse_2, e.newValue);
    }

    private void OnTrainSideFuse_1Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SideFuse_1, e.newValue);
    }

    private void OnTrainMainFuseChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.MainFuse, e.newValue);
    }

    private void OnTrainSanderChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Sander, value);
    }

    private void OnTrainReverserStateChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Reverser, value);
    }

    private void OnTrainIndependentBrakeChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.IndependentBrake, value);
    }

    private void OnTrainBrakeChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Brake, value);
    }

    private void OnTrainThrottleChanged(float newThrottle)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Throttle, newThrottle);
    }
}