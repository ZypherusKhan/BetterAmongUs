using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules.AntiCheat;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Client.Managers;

[HarmonyPatch]
internal static class ModManagerPatch
{
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    [HarmonyPostfix]
    private static void LateUpdate_Postfix(ModManager __instance)
    {
        if (SplashIntroPatch.IsReallyDoneLoading)
        {
            __instance.ShowModStamp();
        }

        if (__instance.ModStamp.gameObject.active == true)
            __instance.ModStamp.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("BetterAmongUs.Resources.Images.BetterAmongUs-Mod.png", 250f);

        BetterAntiCheat.Update();
        LateTask.UpdateAll(Time.deltaTime);
        BetterNotificationManager.Update();
    }
}
