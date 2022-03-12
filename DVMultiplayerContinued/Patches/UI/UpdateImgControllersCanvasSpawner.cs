using HarmonyLib;
using System.Linq;
using UnityEngine.UI;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(CanvasSpawner), "Update")]
    internal class UpdateImgControllersCanvasSpawner
    {
        private static void Prefix(ref Image[] ___imageComponents)
        {
            if (Main.isInitialized && CustomUI.readyForCSUpdate && !CustomUI.CSUpdateFinished)
            {
                Main.Log("Updating image components");
                ___imageComponents = (from img in SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.GetComponentsInChildren<Image>(true)
                                      where img.transform.name != "VRTK_UICANVAS_DRAGGABLE_PANEL"
                                      select img).ToArray<Image>();

                CustomUI.CSUpdateFinished = true;
            }
        }
    }
}
