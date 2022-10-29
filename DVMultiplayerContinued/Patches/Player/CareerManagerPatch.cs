using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkRift;
using DarkRift.Client.Unity;
using DV;
using DV.CashRegister;
using DV.ServicePenalty;
using DV.ServicePenalty.UI;
using DVMultiplayer;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using DVMP.DTO.Player;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayerContinued.Patches.Player
{
    internal static class CareerManagerPatch
    {
        [HarmonyPatch(typeof(CareerManagerLicensePayingScreen), nameof(CareerManagerLicensePayingScreen.HandleInputAction))]
        internal static class LicensePayingScreenHandleInputActionPatch
        {
            internal static bool Prefix(InputAction input, CareerManagerLicensePayingScreen __instance)
            {
                if (!NetworkManager.IsClient())
                    return true;

                NetworkPlayerManager npm = SingletonBehaviour<NetworkPlayerManager>.Instance;
                if (NetworkManager.IsHost() || input != InputAction.Confirm ||
                    __instance.cashReg.DepositedCash != double.Parse(__instance.licensePriceText.text.TrimStart(new char[] { '$' })) ||
                    npm.IsChangeByNetwork)
                {
                    npm.IsChangeByNetwork = false;
                    return true;
                }
                UUI.UnlockMouse(true);
                TutorialController.movementAllowed = false;
                AppUtil.Instance.PauseGame();
                CustomUI.OpenPopup("Waiting", "Host is deciding...");
                SingletonBehaviour<CoroutineManager>.Instance.Run(AskToBuyLicenseCoro(__instance));
                return false;
            }

            internal static IEnumerator AskToBuyLicenseCoro(CareerManagerLicensePayingScreen importantData)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(new License
                    {
                        LicenseName = importantData.licenseNameText.text,
                        Price = double.Parse(importantData.licensePriceText.text.TrimStart(new char[] { '$' })),
                        PlayerID = NetworkManager.client.ID
                    });
                    Main.Log($"[CLIENT] > PLAYER_BUY_LICENSE");
                    using (Message message = Message.Create((ushort)NetworkTags.PLAYER_BUY_LICENSE, writer))
                        SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
                }
                NetworkPlayerManager npm = SingletonBehaviour<NetworkPlayerManager>.Instance;
                yield return new WaitUntil(() => npm.CanBuyLicense != 0);
                if (npm.CanBuyLicense == 1)
                {
                    npm.IsChangeByNetwork = true;
                    importantData.HandleInputAction(InputAction.Confirm);
                    LicenseManager.SaveData();
                    SingletonBehaviour<SaveGameManager>.Instance.DoSaveIO(SaveGameManager.data);
                }
                else
                {
                    importantData.HandleInputAction(InputAction.Cancel);
                }
                npm.CanBuyLicense = 0;
                UUI.UnlockMouse(false);
                TutorialController.movementAllowed = true;
                AppUtil.Instance.UnpauseGame();
                CustomUI.Close();
            }
        }
        [HarmonyPatch(typeof(CareerManagerFeePayingScreen), nameof(CareerManagerFeePayingScreen.HandleInputAction))]
        internal static class FeePayingScreenHandleInputActionPatch
        {
            internal static bool Prefix(CareerManagerFeePayingScreen __instance, InputAction input)
            {
                if (NetworkManager.IsClient() && input == InputAction.Up)
                {
                    DebtType debtType = __instance.DebtToPay.GetDebtType();
                    if (debtType == DebtType.ExistingLoco)
                    {
                        if (!(__instance.DebtToPay is ExistingLocoDebt item))
                        {
                            Debug.LogError(string.Format("Unexpected state: {0}: {1} couldn't be casted properly, returning to main screen!", "debtType", debtType));
                            __instance.SwitchToFeesScreen();
                            return false;
                        }
                        if (!SingletonBehaviour<LocoDebtController>.Instance.trackedLocosDebts.Contains(item))
                        {
                            Debug.LogWarning("Fee was staged in the meantime (loco destroyed), returning to main screen!");
                            __instance.SwitchToFeesScreen();
                            return false;
                        }
                    }
                    if (debtType == DebtType.ExistingJob)
                    {
                        if (!(__instance.DebtToPay is ExistingJobDebt item2))
                        {
                            Debug.LogError(string.Format("Unexpected state: {0}: {1} couldn't be casted properly, returning to main screen!", "debtType", debtType));
                            __instance.SwitchToFeesScreen();
                            return false;
                        }
                        if (!SingletonBehaviour<JobDebtController>.Instance.existingTrackedJobs.Contains(item2))
                        {
                            Debug.LogWarning("Fee was staged in the meantime (job abandoned/completed/expired), returning to main screen!");
                            __instance.SwitchToFeesScreen();
                            return false;
                        }
                    }
                    if (debtType == DebtType.StagedOther || debtType == DebtType.ExistingOther || debtType == DebtType.ExistingLoco || debtType == DebtType.ExistingJob)
                    {
                        __instance.UpdateDebtCost();
                        if (__instance.cashReg.GetTotalCost() < 0.0099999997764825821)
                        {
                            Debug.LogWarning("In the meantime price of debt became 0, returning to fees screen.");
                            __instance.SwitchToFeesScreen();
                            return false;
                        }
                    }

                    __instance.DebtToPay.Pay();
                    __instance.SwitchToFeesScreen();
                    return false;
                }
                return true;
            }
        }
    }
}
