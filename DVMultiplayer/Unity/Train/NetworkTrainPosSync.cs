using DV.Logic.Job;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTrainPosSync : MonoBehaviour
{
    private TrainCar trainCar;
    internal WorldTrain serverState;
    private bool isStationary;

    private Vector3 newPos = Vector3.zero;
    private Quaternion newRot = Quaternion.identity;
    //internal bool isLocationApplied;
    public bool isDerailed;
    internal Vector3 velocity = Vector3.zero;
    public event Action<TrainCar> OnTrainCarInitialized;
    public bool hasLocalPlayerAuthority = false;
    internal bool resetAuthority = false;
    internal NetworkTurntableSync turntable = null;
    public bool IsCarDamageEnabled { get; internal set; }
    NetworkPlayerSync localPlayer;
    ShunterLocoSimulation shunterLocoSimulation = null;
    DieselLocoSimulation dieselLocoSimulation = null;
    SteamLocoSimulation steamLocoSimulation = null;
    ParticleSystem.MainModule shunterExhaust;
    ParticleSystem.MainModule dieselExhaust;
    ParticleSystem.MainModule steamParticles;
    private bool isBeingDestroyed;
    internal Trainset tempFrontTrainsetWithAuthority;
    internal Trainset tempRearTrainsetWithAuthority;

    //private TrainAudio trainAudio;
    //private BogieAudioController[] bogieAudios;

#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkTrainPosSync.Awake()");
        trainCar = GetComponent<TrainCar>();

        //bogieAudios = new BogieAudioController[trainCar.Bogies.Length];

        Main.Log($"[{trainCar.ID}] NetworkTrainPosSync Awake called");

        Main.Log($"Listening to derailment/rerail events");
        trainCar.OnDerailed += TrainDerail;
        trainCar.OnRerailed += TrainRerail;
        localPlayer = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();

        isStationary = trainCar.isStationary;

        Main.Log($"Listening to movement changed event");
        trainCar.MovementStateChanged += TrainCar_MovementStateChanged;
        trainCar.CarDamage.CarEffectiveHealthStateUpdate += OnBodyDamageTaken;
        switch (trainCar.carType) {
            case TrainCarType.LocoShunter:
                shunterLocoSimulation = GetComponent<ShunterLocoSimulation>();
                shunterExhaust = trainCar.transform.Find("[particles]").Find("ExhaustEngineSmoke").GetComponent<ParticleSystem>().main;
                break;
            case TrainCarType.LocoDiesel:
                dieselLocoSimulation = GetComponent<DieselLocoSimulation>();
                dieselExhaust = trainCar.transform.Find("[particles]").Find("ExhaustEngineSmoke").GetComponent<ParticleSystem>().main;
                break;
        }

        if (!trainCar.IsLoco)
        {
            trainCar.CargoLoaded += OnCargoLoaded;
            trainCar.CargoUnloaded += OnCargoUnloaded;
            trainCar.CargoDamage.CargoDamaged += OnCargoDamageTaken;
        }

        //for(int i = 0; i < trainCar.Bogies.Length; i++)
        //{
        //    bogieAudios[i] = trainCar.Bogies[i].GetComponent<BogieAudioController>();
        //}

        if (NetworkManager.IsHost())
        {
            trainCar.TrainsetChanged += TrainCar_TrainsetChanged;
            SetAuthority(true);
        }
        else
        {
            SetAuthority(false);
        }
    }

    private void TrainCar_TrainsetChanged(Trainset set)
    {
        if (isBeingDestroyed || set == null || set.firstCar == null || trainCar.logicCar == null || !trainCar)
            return;
        //Issue with trainset being detatched in the middle positioning not updating correctly.
        if (set.locoIndices.Count == 0 && set.firstCar == trainCar)
            StartCoroutine(ResetAuthorityToHostWhenStationary(set));
        else
            CheckAuthorityChange();

        if (!Trainset.allSets.Contains(tempFrontTrainsetWithAuthority))
            tempFrontTrainsetWithAuthority = null;

        if (!Trainset.allSets.Contains(tempRearTrainsetWithAuthority))
            tempRearTrainsetWithAuthority = null;
    }

    private IEnumerator ResetAuthorityToHostWhenStationary(Trainset set)
    {
        yield return new WaitUntil(() => velocity.magnitude * 3.6f < 1);
        SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(set, localPlayer.Id);
    }

    private void OnCargoUnloaded()
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.CargoStateChanged(trainCar, CargoType.None, false);
    }

    private void OnCargoLoaded(CargoType type)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.CargoStateChanged(trainCar, type, true);
    }

    internal void CheckAuthorityChange()
    {
        if (!trainCar || trainCar.logicCar == null || !trainCar.IsLoco)
            return;

        try
        {
            // If not on turntable or no one is in control shed of turntable
            if (turntable == null || turntable != null && !turntable.IsAnyoneInControlArea)
            {
                bool authNeedsChange = false;
                GameObject newOwner = null;
                GameObject currentOwner;
                if (!hasLocalPlayerAuthority)
                    currentOwner = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerById(serverState.AuthorityPlayerId);
                else
                    currentOwner = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();

                // Should force authority change
                if (!resetAuthority)
                {
                    // Check if current owner is disconnected
                    if (currentOwner)
                    {
                        // Get new owner that is valid
                        newOwner = GetNewOwnerIfConditionsAreMet(currentOwner);
                        authNeedsChange = newOwner is object;
                    }
                    else
                    {
                        // Give host authority if current client is disconnected
                        newOwner = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();
                        authNeedsChange = true;
                    }
                }
                else
                {
                    // Force authority change
                    authNeedsChange = true;
                    // Use host as fallback if no one is in control
                    newOwner = GetPlayerAuthorityReplacement(true);
                }

                // If authority needs to be changed
                if (authNeedsChange)
                {
                    NetworkPlayerSync newOwnerPlayerData = newOwner.GetComponent<NetworkPlayerSync>();
                    bool shouldSendAuthChange = resetAuthority ? true : newOwnerPlayerData.Id != serverState.AuthorityPlayerId;

                    // Check if the current authority is already given to the new owner
                    if (newOwnerPlayerData.Id == serverState.AuthorityPlayerId && !shouldSendAuthChange)
                    {
                        // Check if all cars in the trainset have the correct authority
                        shouldSendAuthChange = CheckTrainsetMismatchesAuthority(newOwnerPlayerData.Id);
                    }

                    // Check if authority still needs to be sent
                    if (shouldSendAuthChange)
                    {
                        // Send authority change
                        SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, newOwnerPlayerData.Id);
                        resetAuthority = false;
                    }
                }
                else
                {
                    // Set the new owner to current owner
                    if (newOwner == null)
                        newOwner = currentOwner;
                    NetworkPlayerSync newOwnerPlayerData = newOwner.GetComponent<NetworkPlayerSync>();

                    // Send authority change if trainset mismatched
                    if (CheckTrainsetMismatchesAuthority(newOwnerPlayerData.Id))
                        SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, newOwnerPlayerData.Id);
                }
            }
            else
            {
                // Send authority change if on turntable based of turntable controller
                if (serverState.AuthorityPlayerId != turntable.playerAuthId)
                    SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, turntable.playerAuthId);
            }
        }
        catch (Exception ex)
        {
            Main.Log($"Exception thrown in authority check. {ex.Message}");
        }
    }

    private bool CheckTrainsetMismatchesAuthority(ushort id)
    {
        foreach (TrainCar car in trainCar.trainset.cars)
        {
            // If train logic car not set
            if (car.logicCar == null)
                continue;

            // Get the server state
            WorldTrain state = SingletonBehaviour<NetworkTrainManager>.Instance.GetServerStateById(car.CarGUID);
            if (state.AuthorityPlayerId != id)
            {
                return true;
            }
        }
        return false;
    }

    private GameObject GetNewOwnerIfConditionsAreMet(GameObject currentOwner)
    {
        if (tempFrontTrainsetWithAuthority != null || tempRearTrainsetWithAuthority != null)
            return null;

        if(velocity.magnitude * 3.6f >= .2f)
        {
            StartCoroutine(WaitTilTrainIsStationaryThenRecheck());
            return null;
        }

        // Is like Playermanager.Car but then for all clients
        TrainCar currentOwnerCurrentCar = currentOwner.GetComponent<NetworkPlayerSync>().Train;

        // Set fallback option
        bool useFallback = currentOwnerCurrentCar && !currentOwnerCurrentCar.trainset.cars.Contains(trainCar);

        // Check if player is on the ground
        if (!currentOwnerCurrentCar)
            return GetPlayerAuthorityReplacement(useFallback);

        // Check if player is in a different trainset that is not within couple range
        if (!currentOwnerCurrentCar.trainset.cars.Contains(trainCar))
            return GetPlayerAuthorityReplacement(useFallback);

        return null;
    }

    private IEnumerator WaitTilTrainIsStationaryThenRecheck()
    {
        yield return new WaitUntil(() => velocity.magnitude * 3.6f < 1);
        CheckAuthorityChange();
    }

    private void GainAndReleaseAuthorityOfTrainsInRangeOfCurrent()
    {
        if (!trainCar.rearCoupler || !trainCar.frontCoupler || velocity.magnitude * 3.6f <= .5f)
            return;

        GameObject collidedCouplerRear = trainCar.rearCoupler.GetFirstCouplerInRange(3)?.gameObject;
        GameObject collidedCouplerFront = trainCar.frontCoupler.GetFirstCouplerInRange(3)?.gameObject;

        if (collidedCouplerRear && collidedCouplerRear.GetComponent<Coupler>() && collidedCouplerRear.GetComponent<Coupler>().train != trainCar)
        {
            var coupler = collidedCouplerRear.GetComponent<Coupler>();
            var otherTrain = coupler.train;
            var otherTrainServerState = SingletonBehaviour<NetworkTrainManager>.Instance.GetServerStateById(otherTrain.CarGUID);
            if (otherTrainServerState != null && serverState.AuthorityPlayerId != otherTrainServerState.AuthorityPlayerId)
            {
                if (tempRearTrainsetWithAuthority != otherTrain.trainset)
                {
                    ushort gainer = otherTrainServerState.AuthorityPlayerId;
                    if (tempRearTrainsetWithAuthority == null)
                        gainer = serverState.AuthorityPlayerId;

                    var trainSync = otherTrain.GetComponent<NetworkTrainPosSync>();
                    if (coupler.isFrontCoupler)
                        trainSync.tempFrontTrainsetWithAuthority = trainCar.trainset;
                    else
                        trainSync.tempRearTrainsetWithAuthority = trainCar.trainset;
                    SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(otherTrain.trainset, gainer);
                    tempRearTrainsetWithAuthority = otherTrain.trainset;
                }
            }
        }

        if (!collidedCouplerRear && tempRearTrainsetWithAuthority != null)
        {
            var frontTrain = tempRearTrainsetWithAuthority.firstCar.GetComponent<NetworkTrainPosSync>();
            var rearTrain = tempRearTrainsetWithAuthority.lastCar.GetComponent<NetworkTrainPosSync>();

            tempRearTrainsetWithAuthority = null;
            if (frontTrain.tempFrontTrainsetWithAuthority == trainCar.trainset)
                frontTrain.tempFrontTrainsetWithAuthority = null;

            if (frontTrain.tempRearTrainsetWithAuthority == trainCar.trainset)
                frontTrain.tempRearTrainsetWithAuthority = null;

            if (rearTrain.tempFrontTrainsetWithAuthority == trainCar.trainset)
                rearTrain.tempFrontTrainsetWithAuthority = null;

            if (rearTrain.tempRearTrainsetWithAuthority == trainCar.trainset)
                rearTrain.tempRearTrainsetWithAuthority = null;

            frontTrain.CheckAuthorityChange();
            rearTrain.CheckAuthorityChange();
            CheckAuthorityChange();
        }

        if (collidedCouplerFront && collidedCouplerFront.GetComponent<Coupler>() && collidedCouplerFront.GetComponent<Coupler>().train != trainCar)
        {
            var coupler = collidedCouplerFront.GetComponent<Coupler>();
            var otherTrain = coupler.train;
            var otherTrainServerState = SingletonBehaviour<NetworkTrainManager>.Instance.GetServerStateById(otherTrain.CarGUID);
            if (otherTrainServerState != null && serverState.AuthorityPlayerId != otherTrainServerState.AuthorityPlayerId)
            {
                if (tempFrontTrainsetWithAuthority != otherTrain.trainset)
                {
                    var trainSync = otherTrain.GetComponent<NetworkTrainPosSync>();
                    if (coupler.isFrontCoupler)
                        trainSync.tempFrontTrainsetWithAuthority = trainCar.trainset;
                    else
                        trainSync.tempRearTrainsetWithAuthority = trainCar.trainset;
                    SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(otherTrain.trainset, serverState.AuthorityPlayerId);
                    tempFrontTrainsetWithAuthority = otherTrain.trainset;
                }
            }
        }

        if (!collidedCouplerFront && tempFrontTrainsetWithAuthority != null)
        {
            var frontTrain = tempFrontTrainsetWithAuthority.firstCar.GetComponent<NetworkTrainPosSync>();
            var rearTrain = tempFrontTrainsetWithAuthority.lastCar.GetComponent<NetworkTrainPosSync>();

            tempFrontTrainsetWithAuthority = null;
            if (frontTrain.tempFrontTrainsetWithAuthority == trainCar.trainset)
                frontTrain.tempFrontTrainsetWithAuthority = null;

            if (frontTrain.tempRearTrainsetWithAuthority == trainCar.trainset)
                frontTrain.tempRearTrainsetWithAuthority = null;

            if (rearTrain.tempFrontTrainsetWithAuthority == trainCar.trainset)
                rearTrain.tempFrontTrainsetWithAuthority = null;

            if (rearTrain.tempRearTrainsetWithAuthority == trainCar.trainset)
                rearTrain.tempRearTrainsetWithAuthority = null;

            frontTrain.CheckAuthorityChange();
            rearTrain.CheckAuthorityChange();
            CheckAuthorityChange();
        }
    }

    private GameObject GetPlayerAuthorityReplacement(bool useFallback = false)
    {
        // Get players on current car
        GameObject[] playersInCar = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(trainCar);
        if (playersInCar.Length > 0)
        {
            // Check if player is valid
            foreach(GameObject playerObject in playersInCar)
            {
                if(playerObject.GetComponent<NetworkPlayerSync>())
                {
                    return playerObject;
                }
            }
        }
        else
        {
            foreach (int locoId in trainCar.trainset.locoIndices)
            {
                // Get locomotive in trainset
                TrainCar loco = trainCar.trainset.cars[locoId];
                if (!loco || loco == trainCar) continue;

                // Get players on locomotive in trainset
                playersInCar = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(loco);
                if (playersInCar.Length > 0)
                {
                    // Check if player is valid
                    foreach (GameObject playerObject in playersInCar)
                    {
                        if (playerObject.GetComponent<NetworkPlayerSync>())
                        {
                            return playerObject;
                        }
                    }
                }
            }

            // If no player is found yet and fallback is true return the host
            if (useFallback) return SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();
        }
        // Return null if no players are found and fallback was false
        return null;
    }

    private void OnDestroy()
    {
        isBeingDestroyed = true;
        StopAllCoroutines();

        trainCar.MovementStateChanged -= TrainCar_MovementStateChanged;
        trainCar.CarDamage.CarEffectiveHealthStateUpdate -= OnBodyDamageTaken;

        if (!trainCar.IsLoco)
        {
            trainCar.CargoDamage.CargoDamaged -= OnCargoDamageTaken;
            trainCar.CargoLoaded -= OnCargoLoaded;
            trainCar.CargoUnloaded -= OnCargoUnloaded;
        }

        Main.Log($"NetworkTrainPosSync.OnDestroy()");
    }

    private void FixedUpdate()
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Exists || SingletonBehaviour<NetworkTrainManager>.Instance.IsDisconnecting)
            return;

        try
        {
            if (!hasLocalPlayerAuthority && Vector3.Distance(transform.position - WorldMover.currentMove, newPos) > 1e-4f && newPos != Vector3.zero)
                UpdateNonAuthorityPositioning();
        }
        catch (Exception ex)
        {
            Main.Log($"NetworkTrainPosSync threw an exception while updating position: {ex.Message} inner exception: {ex.InnerException}");
        }

        if (hasLocalPlayerAuthority)
        {
            velocity = trainCar.rb.velocity;
            isStationary = trainCar.isStationary;

            if(velocity.magnitude * 3.6f <= .2f && trainCar.stress.enabled)
                trainCar.stress.EnableStress(false);
        }

        if (hasLocalPlayerAuthority && ((velocity.magnitude * 3.6f > .2f && Vector3.Distance(transform.position - WorldMover.currentMove, newPos) > Mathf.Lerp(1e-4f, 1e-2f, velocity.magnitude * 3.6f / 50)) || Quaternion.Angle(transform.rotation, newRot) > 1e-2f))
        {
            if (!trainCar.stress.enabled)
                trainCar.stress.EnableStress(true);
            SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar);
            newPos = transform.position - WorldMover.currentMove;
            newRot = transform.rotation;

            if (!turntable && !IsCarDamageEnabled)
            {
                trainCar.CarDamage.IgnoreDamage(false);
            }
        }
    }

    private void UpdateNonAuthorityPositioning()
    {
        float increment = (velocity.magnitude * 3f);
        if (increment <= 5f && turntable)
            increment = 5;

        if (increment <= 5f && Vector3.Distance(transform.position - WorldMover.currentMove, newPos) > 1 && isDerailed)
            increment = 5;

        if (increment <= 5f && Vector3.Distance(transform.position - WorldMover.currentMove, newPos) > 10)
            increment = 5;

        if (increment == 0)
            increment = 1;

        if (Vector3.Distance(transform.position, newPos + WorldMover.currentMove) > 5 || isDerailed)
        {
            if (!isDerailed)
            {
                foreach (Bogie b in trainCar.Bogies)
                {
                    if (b.rb)
                        b.rb.isKinematic = true;
                }
            }
            trainCar.rb.MovePosition(newPos + WorldMover.currentMove);
            trainCar.rb.MoveRotation(newRot);
            if (!isDerailed)
            {
                foreach (Bogie b in trainCar.Bogies)
                {
                    if (b.rb)
                    {
                        b.ResetBogiesToStartPosition();
                        b.rb.isKinematic = false;
                    }
                }
            }
        }
        else
        {
            float step = increment * Time.deltaTime; // calculate distance to move
            trainCar.rb.MovePosition(Vector3.MoveTowards(transform.position, newPos + WorldMover.currentMove, step));
            
            //Main.Log($"Rotating train");
            if (!turntable)
            {
                trainCar.rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, newRot, step));
            }
            else
            {
                trainCar.rb.MoveRotation(newRot);
            }
        }
    }

    private void Update()
    {
        if (!SingletonBehaviour<NetworkPlayerManager>.Exists || !SingletonBehaviour<NetworkTrainManager>.Exists || SingletonBehaviour<NetworkTrainManager>.Instance.IsDisconnecting)
            return;

        if(serverState == null)
        {
            serverState = SingletonBehaviour<NetworkTrainManager>.Instance.GetServerStateById(trainCar.CarGUID);
            return;
        }

        if (NetworkManager.IsHost())
        {
            if (trainCar.trainset.firstCar == trainCar || trainCar.trainset.lastCar == trainCar)
            {
                // This is to simulate impact
                GainAndReleaseAuthorityOfTrainsInRangeOfCurrent();
            }
            //CheckAuthorityChange();
        }

        //if(trainAudio == null)
        //{
        //    trainAudio = trainCar.GetComponentInChildren<TrainAudio>();
        //    return;
        //}

        bool willLocalPlayerGetAuthority = localPlayer.Id == serverState.AuthorityPlayerId;
        

        //if (!(hasLocalPlayerAuthority || (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)))
        //{
        //    trainAudio.frictionAudio?.Stop();
        //    foreach (BogieAudioController bogieAudio in bogieAudios)
        //    {
        //        bogieAudio.SetLOD(AudioLOD.NONE);
        //    }
        //}

        try
        {
            if (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)
            {
                Main.Log($"Car {trainCar.CarGUID}: Changing authority [GAINED]");
                if (!trainCar.IsInteriorLoaded)
                {
                    trainCar.LoadInterior();
                }
                trainCar.keepInteriorLoaded = true;
                SetAuthority(true);
            }
            else if (!willLocalPlayerGetAuthority && hasLocalPlayerAuthority)
            {
                Main.Log($"Car {trainCar.CarGUID}: Changing authority [RELEASED]");
                SetAuthority(false);
                newPos = transform.position - WorldMover.currentMove;
                newRot = transform.rotation;
                trainCar.keepInteriorLoaded = false;
            }
        }
        catch (Exception ex)
        {
            Main.Log($"NetworkTrainPosSync threw an exception while changing authority: {ex.Message} inner exception: {ex.InnerException}");
        }


        //if (!hasLocalPlayerAuthority)
        //{
        //    trainCar.rb.MovePosition(newPos);
        //    trainCar.rb.MoveRotation(newRot);
        //}
    }

    private void SetAuthority(bool gain)
    {
        Main.Log($"Setting authority");
        hasLocalPlayerAuthority = gain;
        Main.Log($"Set kinematic state {!gain}");
        trainCar.rb.isKinematic = !gain;
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                shunterExhaust.emitterVelocityMode = gain ? ParticleSystemEmitterVelocityMode.Rigidbody : ParticleSystemEmitterVelocityMode.Transform;
                break;
            case TrainCarType.LocoDiesel:
                dieselExhaust.emitterVelocityMode = gain ? ParticleSystemEmitterVelocityMode.Rigidbody : ParticleSystemEmitterVelocityMode.Transform;
                break;
        }

        //if (gain)
        //{
        //    foreach(Bogie b in trainCar.Bogies)
        //    {
        //        b.
        //        b.ForceSleep(true);
        //        b.ResetBogiesToStartPosition();
        //    }
        //}

        Main.Log($"Toggle damage for 2 seconds");
        StartCoroutine(ToggleDamageAfterSeconds(2));
        Main.Log($"Resync train");
        SingletonBehaviour<NetworkTrainManager>.Instance.ResyncCar(trainCar);
    }
#pragma warning restore IDE0051 // Remove unused private members

    private IEnumerator ToggleDamageAfterSeconds(float seconds)
    {
        IgnoreDamage(true);
        trainCar.stress.EnableStress(false);
        trainCar.TrainCarCollisions.enabled = false;
        if (!hasLocalPlayerAuthority)
        {
            yield break;
        }
        trainCar.CarDamage.IgnoreDamage(true);
        yield return new WaitForSeconds(seconds);
        if (!turntable)
        {
            trainCar.stress.EnableStress(true);
            trainCar.TrainCarCollisions.enabled = true;
            IgnoreDamage(false);
        }
    }

    private void IgnoreDamage(bool set)
    {
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                trainCar.GetComponent<DamageControllerShunter>().IgnoreDamage(set);
                break;
            case TrainCarType.LocoDiesel:
                trainCar.GetComponent<DamageControllerDiesel>().IgnoreDamage(set);
                break;
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                trainCar.GetComponent<DamageController>().IgnoreDamage(set);
                break;
        }
    }

    private IEnumerator ToggleKinematic(float seconds)
    {
        trainCar.rb.isKinematic = true;
        trainCar.rb.Sleep();
        trainCar.stress.EnableStress(false);
        //foreach (Bogie bogie in trainCar.Bogies)
        //{
        //    bogie.RerailInitialize();
        //    bogie.ResetBogiesToStartPosition();
        //}
        yield return new WaitForSeconds(seconds);
        if (hasLocalPlayerAuthority)
        {
            trainCar.rb.isKinematic = false;
            trainCar.stress.EnableStress(true);
        }
    }

    private void TrainCar_MovementStateChanged(bool isMoving)
    {
        if (velocity.magnitude * 3.6f >= 1f && !isMoving)
            return;

        if (!isMoving && hasLocalPlayerAuthority)
        {
            trainCar.stress.EnableStress(false);
            if (SingletonBehaviour<NetworkTrainManager>.Exists && trainCar.IsLoco)
            {
                Main.Log($"Movement state changed is moving: {isMoving}");
                SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar, true);
                newPos = trainCar.transform.position - WorldMover.currentMove;
                newRot = transform.rotation;
            }
        }

        if (!isMoving)
        {
            foreach (Bogie b in trainCar.Bogies)
            {
                if (b.rb)
                {
                    b.rb.isKinematic = true;
                    b.ResetBogiesToStartPosition();
                    b.rb.isKinematic = false;
                }
            }
            trainCar.ForceOptimizationState(true);
        }
    }

    private void TrainCar_LogicCarInitialized()
    {
        OnTrainCarInitialized?.Invoke(trainCar);
    }

    private void TrainRerail()
    {
        if (!hasLocalPlayerAuthority)
        {
            StartCoroutine(ToggleKinematic(2));
            newPos = transform.position - WorldMover.currentMove;
            newRot = transform.rotation;
        }
        else
        {
            newPos = transform.position - WorldMover.currentMove;
            newRot = transform.rotation;
        }

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendRerailCarUpdate(trainCar);
    }

    private void TrainDerail(TrainCar derailedCar)
    {
        if (!hasLocalPlayerAuthority || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendDerailCarUpdate(trainCar);
    }

    private void OnCargoDamageTaken(float _)
    {
        if (serverState is null)
            return;

        if (!SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork && (!hasLocalPlayerAuthority || !IsCarDamageEnabled && hasLocalPlayerAuthority) && Math.Round(trainCar.CarDamage.currentHealth, 2) != Math.Round(serverState.CarHealth, 2))
            trainCar.CargoDamage.LoadCargoDamageState(serverState.CargoHealth);

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority || !IsCarDamageEnabled)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarDamaged(trainCar.CarGUID, DamageType.Cargo, trainCar.CargoDamage.currentHealth, "");
    }

    private void OnBodyDamageTaken(float _)
    {
        if (serverState is null)
            return;

        if (!SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork && (!hasLocalPlayerAuthority || !IsCarDamageEnabled && hasLocalPlayerAuthority) && Math.Round(trainCar.CarDamage.currentHealth, 2) != Math.Round(serverState.CarHealth, 2))
        {
            if (trainCar.IsLoco)
                LoadLocoDamage(serverState.CarHealthData);
            else
                trainCar.CarDamage.LoadCarDamageState(serverState.CarHealth);
        }

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority || !IsCarDamageEnabled)
            return;


        string data = "";
        if (trainCar.IsLoco)
        {
            switch (trainCar.carType)
            {
                case TrainCarType.LocoShunter:
                    data = trainCar.GetComponent<DamageControllerShunter>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case TrainCarType.LocoDiesel:
                    data = trainCar.GetComponent<DamageControllerDiesel>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case TrainCarType.LocoSteamHeavy:
                case TrainCarType.LocoSteamHeavyBlue:
                    data = trainCar.GetComponent<DamageController>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                    break;
            }
        }

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarDamaged(trainCar.CarGUID, DamageType.Car, trainCar.CarDamage.currentHealth, data);
    }

    internal void LoadLocoDamage(string carHealthData)
    {
        switch(trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                trainCar.GetComponent<DamageControllerShunter>().LoadDamagesState(JObject.Parse(carHealthData));
                break;
            case TrainCarType.LocoDiesel:
                trainCar.GetComponent<DamageControllerDiesel>().LoadDamagesState(JObject.Parse(carHealthData));
                break;

            default:
                trainCar.GetComponent<DamageController>().LoadDamagesState(JObject.Parse(carHealthData));
                break;
        }
    }

    internal void UpdateLocation(TrainLocation location)
    {
        StartCoroutine(CoroUpdateLocation(location));
    }

    internal IEnumerator CoroUpdateLocation(TrainLocation location)
    {
        if (hasLocalPlayerAuthority)
            yield break;

        velocity = location.Velocity;
        isStationary = velocity.magnitude > 0;
        newPos = location.Position;
        newRot = location.Rotation;
        if (trainCar.IsLoco)
        {
            switch (trainCar.carType)
            {
                case TrainCarType.LocoShunter:
                    shunterLocoSimulation.engineTemp.SetValue(location.Temperature);
                    shunterLocoSimulation.engineRPM.SetValue(location.RPM);
                    break;
                case TrainCarType.LocoDiesel:
                    dieselLocoSimulation.engineTemp.SetValue(location.Temperature);
                    dieselLocoSimulation.engineRPM.SetValue(location.RPM);
                    break;
            }
        }
        
    }

    //private void SyncVelocityAndSpeedUpIfDesyncedOnFrontCar(TrainLocation location)
    //{
    //    if (trainCar.frontCoupler.IsCoupled())
    //    {
    //        return;
    //    }

    //    SyncVelocityAndSpeedUpIfDesynced(location);
    //}

    //private void SyncVelocityAndSpeedUpIfDesynced(TrainLocation location)
    //{
    //    float distance = Distance(trainCar.transform, location.Position);
    //    float curSpeed = trainCar.GetForwardSpeed() * 3.6f;
    //    Vector3 newVelocity;
    //    if (distance > 10f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + 1.5f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance > 3f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + .86f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance <= 3f && distance > .1f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + (curSpeed < 25 ? .25f : .19f));
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance < .1f && distance > -.1f)
    //    {
    //        newVelocity = location.Velocity;
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = false;
    //    }
    //    else if (distance <= -.1f && distance > -3f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - (curSpeed < 25 ? .25f : .19f));
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance <= -3f && distance >= -10f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - .86f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance > -10f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - 1.5f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }

    //    if (isOutOfSync)
    //        Main.mod.Logger.Log($"{trainCar.ID} Is out of sync difference is {distance}m");
    //}

    //private float Distance(Transform a, Vector3 b)
    //{
    //    Vector3 forward = a.TransformDirection(a.forward);
    //    Vector3 toOther = b - a.position;
    //    if (Vector3.Dot(forward, toOther) < 0)
    //        return -Vector3.Distance(a.position, b);
    //    else
    //        return Vector3.Distance(a.position, b);
    //}

    private TrainCar GetMostFrontCar(TrainCar car)
    {
        if (car.frontCoupler.coupledTo != null)
        {
            return GetMostFrontCar(car.frontCoupler.coupledTo.train);
        }
        else
        {
            return car;
        }
    }
}
