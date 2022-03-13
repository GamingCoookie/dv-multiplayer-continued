using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
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
                shunterDashboard.hornObj.GetComponent<ControlImplBase>().ValueChanged += HornUsed;
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
                dieselDashboard.hornObj.GetComponent<ControlImplBase>().ValueChanged += HornUsed;
                SingletonBehaviour<CoroutineManager>.Instance.Run(DieselRotaryAmplitudeCheckerStartListen(dieselFuseBox));
                break;
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                RotaryBase[] valves;
                valves = loco.interior.GetComponentsInChildren<RotaryBase>();
                foreach (RotaryBase valve in valves)
                {
                    Main.Log($"Listen valve change: {valve.name}");
                    switch (valve.name)
                    {
                        case "C valve 1":
                            valve.ValueChanged += OnWaterDumpChanged;
                            break;
                        case "C valve 2":
                            valve.ValueChanged += OnSteamReleaseChanged;
                            break;
                        case "C valve 3":
                            valve.ValueChanged += OnBlowerChanged;
                            break;
                        case "C valve 4":
                            valve.ValueChanged += OnBlankValveChanged;
                            break;
                        case "C valve 5":
                            valve.ValueChanged += OnFireOutChanged;
                            break;
                        case "C injector":
                            valve.ValueChanged += OnInjectorChanged;
                            break;
                    }
                }
                LeverBase[] levers;
                levers = loco.interior.GetComponentsInChildren<LeverBase>();
                foreach (LeverBase lever in levers)
                {
                    Main.Log($"Listen lever change: {lever.name}");
                    switch (lever.name)
                    {
                        case "C firebox handle invisible":
                            lever.ValueChanged += OnFireDoorChanged;
                            break;
                        case "C sand valve":
                            lever.ValueChanged += OnSteamSanderChanged;
                            break;
                        case "C light lever":
                            lever.ValueChanged += OnLightLeverChanged;
                            break;
                    }
                }

                ButtonBase[] buttons = loco.interior.GetComponentsInChildren<ButtonBase>();
                foreach (ButtonBase button in buttons) 
                {
                    Main.Log($"Listen button change: {button.name}");
                    switch(button.name)
                    {
                        case "C inidactor light switch":
                            button.ValueChanged += OnLightSwitchChanged;
                            break;
                    }
                }
                PullerBase draft = loco.interior.GetComponentInChildren<PullerBase>();
                Main.Log($"Listen puller change: C draft");
                draft.ValueChanged += OnDraftChanged;

                break;
        }
        isAlreadyListening = true;
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
                            sideFuse.ValueChanged -= OnTrainSideFuse_3Changed;
                            break;
                    }
                }
                dieselFuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged -= OnTrainMainFuseChanged;
                dieselFuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged -= OnTrainFusePowerStarterStateChanged;
                break;
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                RotaryBase[] valves;
                valves = loco.interior.GetComponentsInChildren<RotaryBase>();
                foreach (RotaryBase valve in valves)
                {
                    Main.Log($"Stop listening valve change: {valve.name}");
                    switch (valve.name)
                    {
                        case "C valve 1":
                            valve.ValueChanged -= OnWaterDumpChanged;
                            break;
                        case "C valve 2":
                            valve.ValueChanged -= OnSteamReleaseChanged;
                            break;
                        case "C valve 3":
                            valve.ValueChanged -= OnBlowerChanged;
                            break;
                        case "C valve 4":
                            valve.ValueChanged -= OnBlankValveChanged;
                            break;
                        case "C valve 5":
                            valve.ValueChanged -= OnFireOutChanged;
                            break;
                        case "C injector":
                            valve.ValueChanged -= OnInjectorChanged;
                            break;
                    }
                }
                LeverBase[] levers;
                levers = loco.interior.GetComponentsInChildren<LeverBase>();
                foreach (LeverBase lever in levers)
                {
                    Main.Log($"Stop listening lever change: {lever.name}");
                    switch (lever.name)
                    {
                        case "C firebox handle invisible":
                            lever.ValueChanged -= OnFireDoorChanged;
                            break;
                        case "C sand valve":
                            lever.ValueChanged -= OnSteamSanderChanged;
                            break;
                        case "C light lever":
                            lever.ValueChanged -= OnLightLeverChanged;
                            break;
                    }
                }

                ButtonBase lightSwitch = loco.interior.GetComponentInChildren<ButtonBase>();
                lightSwitch.ValueChanged -= OnLightSwitchChanged;
                PullerBase draft = loco.interior.GetComponentInChildren<PullerBase>();
                draft.ValueChanged -= OnDraftChanged;
                break;
        }
    }

    public void Awake()
    {
        Main.Log($"NetworkTrainSync.Awake()");
        loco = GetComponent<TrainCar>();
    }

    public void Update()
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoValue(loco);
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

    private void HornUsed(ValueChangedEventArgs e)
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

    private void OnFireDoorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FireDoor, e.newValue);
    }

    private void OnWaterDumpChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.WaterDump, e.newValue);
    }

    private void OnSteamReleaseChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SteamRelease, e.newValue);
    }

    private void OnBlowerChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Blower, e.newValue);
    }

    private void OnBlankValveChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.BlankValve, e.newValue);
    }

    private void OnFireOutChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FireOut, e.newValue);
    }

    private void OnInjectorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Injector, e.newValue);
    }

    private void OnSteamSanderChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SteamSander, e.newValue);
    }

    private void OnLightLeverChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.LightLever, e.newValue);
    }

    private void OnLightSwitchChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.LightSwitch, e.newValue);
    }

    private void OnDraftChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Draft, e.newValue);
    }
}