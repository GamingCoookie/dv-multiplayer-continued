using System;
using System.Collections.Generic;
using System.Linq;
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
            if (!SingletonBehaviour<NetworkPlayerManager>.Exists) return true;
            NetworkPlayerManager npm = SingletonBehaviour<NetworkPlayerManager>.Instance;
            Dictionary<string, JObject> players = new Dictionary<string, JObject>(npm.localPlayers.Count + 1);

            // Local player
            NetworkPlayerSync localPlayer = npm.GetLocalPlayerSync();
            players.Add($"{localPlayer.Id}", CreatePlayerObject(ColorUtility.ToHtmlStringRGBA(Main.Settings.Color), localPlayer.transform));

            // Other players
            foreach (KeyValuePair<ushort, GameObject> e in npm.localPlayers)
            {
                string hexRGBA = ColorUtility.ToHtmlStringRGBA(ColorTT.Unpack(npm.GetPlayerSyncById(e.Key).Color));
                players.Add($"{e.Key}", CreatePlayerObject(hexRGBA, e.Value.transform));
            }

            __result = new JObject(
                players.Select(data => (object)new JProperty(data.Key, data.Value)).ToArray()
            );

            return false;
        }

        private static JObject CreatePlayerObject(string rgba, Transform t)
        {
            World.Position pos = new World.Position(t.position - WorldMover.currentMove);
            return new JObject(
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
            if (!SingletonBehaviour<NetworkPlayerManager>.Exists) return true;
            Sessions.AddTag("player");
            return false;
        }
    }
}
