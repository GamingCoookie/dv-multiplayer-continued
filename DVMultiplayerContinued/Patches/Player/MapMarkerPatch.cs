using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using DVMultiplayer;
using DV.CabControls.Spec;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Button = DV.CabControls.Spec.Button;
using Object = UnityEngine.Object;

namespace DVMultiplayerContinued.Patches.Player
{
    internal static class MapMarkerPatch
    {
        [HarmonyPatch(typeof(MapMarkersController), nameof(MapMarkersController.OnEnable))]
        internal static class OnEnablePatch
        {
            internal static void Postfix(MapMarkersController __instance)
            {
                MapMarkersController mapMarkersController = __instance;
                if (!SingletonBehaviour<NetworkPlayerManager>.Exists)
                    return;
                foreach (var player in SingletonBehaviour<NetworkPlayerManager>.Instance.localPlayers.Values)
                {
                    mapMarkersController.dynamicMarkers.TryGetValue(player, out var mapMarker);
                    if (mapMarker != null)
                    {
                        Main.Log($"Map marker for {player.name} already exists :3");
                        return;
                    }
                    Main.Log($"Adding marker for {player.name}");
                    mapMarkersController.markerObjectsChanged = true;
                    mapMarker = new MapMarkersController.MapMarker(mapMarkersController, mapMarkersController.map, player.transform, player.transform, player.name, MakePlayerMarkerPrefab(player.name));
                    mapMarkersController.dynamicMarkers[player] = mapMarker;
                    mapMarker.UpdatePosition(player.transform);
                    if (mapMarker.Button != null)
                        mapMarker.Button.InteractionAllowed = false;
                }
            }
        }

        [HarmonyPatch(typeof(MapMarkersController), nameof(MapMarkersController.UpdateMarkerForEntity), new Type[] { typeof(GameObject) })]
        internal static class UpdateMarkerForEntityPatch
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var insertIndex = -1;
                Label returnRotationLabel = il.DefineLabel();
                LocalBuilder textType = il.DeclareLocal(typeof(Type));
                var codes = new List<CodeInstruction>(instructions);
                foreach (var code in codes)
                {
                    if (code.opcode == OpCodes.Callvirt && (MethodInfo)code.operand == AccessTools.Method(typeof(MapMarkersController.MapMarker), "UpdatePosition", new Type[] { typeof(Transform) }))
                    {
                        insertIndex = codes.IndexOf(code) + 1;
                        codes[insertIndex].labels.Add(returnRotationLabel);
                    }
                }

                if (insertIndex == -1)
                    return codes.AsEnumerable();

                var instructionsToInsert = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldtoken, typeof(Text)),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle), new Type[] { typeof(RuntimeTypeHandle) })),
                    new CodeInstruction(OpCodes.Stloc, textType),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc, textType),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "GetComponentInChildren", new Type[] { typeof(Type) })),
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "Equals", new Type[] { typeof(Object) })),
                    new CodeInstruction(OpCodes.Brtrue_S, returnRotationLabel),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(insertIndex, instructionsToInsert);

                return codes.AsEnumerable();
            }
        }

        internal static GameObject MakePlayerMarkerPrefab(string username)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.transform.localScale = new Vector3(0.004f, 0.001f, 0.004f);
            prefab.transform.position = new Vector3(0, 0.0235f, 0);
            prefab.transform.rotation = new Quaternion(0.3f, 0.6f, 0.8f, 1);

            Vector3 normalized = Vector3.ProjectOnPlane(prefab.transform.forward, Vector3.up).normalized;
            prefab.transform.localRotation = Quaternion.LookRotation(normalized);

            prefab.GetComponent<Renderer>().material.color = Color.magenta;
            Button button = prefab.AddComponent<Button>();
            button.createRigidbody = false;
            button.useJoints = false;

            GameObject namePlaceCanvas = new GameObject("NamePlace Canvas");
            namePlaceCanvas.transform.parent = prefab.transform;
            namePlaceCanvas.transform.localPosition = new Vector3(2.5f, 0.5f, 0.5f);
            namePlaceCanvas.AddComponent<Canvas>();
            namePlaceCanvas.AddComponent<RotateTowardsPlayer>();

            RectTransform rectTransform = namePlaceCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1920, 1080);
            rectTransform.localScale = new Vector3(.0016f, .0009f, 0);

            GameObject namePlace = new GameObject("NamePlace");
            namePlace.transform.parent = namePlaceCanvas.transform;

            Text name = namePlace.AddComponent<Text>();
            name.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            name.fontSize = 50;
            name.alignment = TextAnchor.MiddleCenter;
            name.resizeTextForBestFit = true;
            name.text = username.Replace("(Clone)", "");

            rectTransform = name.GetComponent<RectTransform>();
            rectTransform.localScale = new Vector3(35f, 35f, 0f);
            rectTransform.anchorMin = new Vector2(.5f, .5f);
            rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
            rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);
            rectTransform.sizeDelta = new Vector2(1000, 500);
            return prefab;
        }
    }
}
