using System;
using System.Collections.Generic;
using System.Reflection;
using DvMod.RemoteDispatch;
using DVMultiplayer.Utils;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Main = DVMultiplayer.Main;

namespace DVMultiplayerContinued.Patches.RemoteDispatch
{
    public static class RemoteDispatchInitializer
    {
        public static void Initialize(Harmony harmony)
        {
            Main.Log("Patching for Remote Dispatch compatibility...");
            try
            {
                Main.Log("Patching PlayerData#GetPlayerData");
                MethodInfo PlayerDataGetPlayerData = AccessTools.Method(typeof(PlayerData), nameof(PlayerData.GetPlayerData));
                MethodInfo PlayerDataGetPlayerDataPatchPrefix = AccessTools.Method(typeof(PlayerData_GetPlayerData_Patch), nameof(PlayerData_GetPlayerData_Patch.Prefix));
                harmony.Patch(PlayerDataGetPlayerData, new HarmonyMethod(PlayerDataGetPlayerDataPatchPrefix));
                Main.Log("Patching PlayerData#CheckTransform");
                MethodInfo PlayerDataCheckTransform = AccessTools.Method(typeof(PlayerData), nameof(PlayerData.CheckTransform));
                MethodInfo PlayerDataCheckTransformPatchPrefix = AccessTools.Method(typeof(PlayerData_CheckTransform_Patch), nameof(PlayerData_CheckTransform_Patch.Prefix));
                harmony.Patch(PlayerDataCheckTransform, new HarmonyMethod(PlayerDataCheckTransformPatchPrefix));
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to patch: {ex.Message}");
            }
        }
    }

    public static class PlayerData_GetPlayerData_Patch
    {
        public static bool Prefix(ref JObject __result)
        {
            NetworkPlayerManager npm = SingletonBehaviour<NetworkPlayerManager>.Instance;
            if (npm == null) return true;
            List<JObject> players = new List<JObject>(npm.localPlayers.Count + 1);

            // Local player
            // This is first due to the 'Zoom to player' button in Remote Dispatch going to the first player in the array.
            NetworkPlayerSync localPlayer = npm.GetLocalPlayerSync();
            players.Add(CreatePlayerObject($"{localPlayer.Id}", ColorUtility.ToHtmlStringRGBA(Main.Settings.Color), localPlayer.transform));

            // Other players
            foreach (KeyValuePair<ushort, GameObject> e in npm.localPlayers)
            {
                string hexRGBA = ColorUtility.ToHtmlStringRGBA(ColorTT.Unpack(npm.GetPlayerSyncById(e.Key).Color));
                players.Add(CreatePlayerObject($"{e.Key}", hexRGBA, e.Value.transform));
            }

            __result = new JObject(new JProperty("players", new JArray(players)));
            return false;
        }

        private static JObject CreatePlayerObject(string id, string rgba, Transform t)
        {
            World.Position pos = new World.Position(t.position - WorldMover.currentMove);
            return new JObject(
                new JProperty("id", id),
                new JProperty("color", $"#{rgba}"),
                new JProperty("position", World.LatLon.From(pos).ToJson()),
                new JProperty("rotation", Math.Round(t.eulerAngles.y, 2))
            );
        }
    }

    public static class PlayerData_CheckTransform_Patch
    {
        public static bool Prefix()
        {
            NetworkPlayerManager npm = SingletonBehaviour<NetworkPlayerManager>.Instance;
            if (npm == null) return true;
            Sessions.AddTag("player");
            return false;
        }
    }
}
