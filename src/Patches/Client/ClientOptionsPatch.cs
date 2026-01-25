using BepInEx;
using BetterAmongUs.Data;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Chat;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class OptionsMenuBehaviourPatch
{
    private static ClientOptionItem? AntiCheat;
    private static ClientOptionItem? SendBetterRpc;
    private static ClientOptionItem? BetterNotifications;
    private static ClientOptionItem? ForceOwnLanguage;
    private static ClientOptionItem? ChatDarkMode;
    private static ClientOptionItem? ChatInGameplay;
    private static ClientOptionItem? LobbyPlayerInfo;
    private static ClientOptionItem? DisableLobbyTheme;
    private static ClientOptionItem? UnlockFPS;
    private static ClientOptionItem? ShowFPS;
    private static ClientOptionItem? OpenSaveData;
    private static ClientOptionItem? SwitchToVanilla;

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    [HarmonyPrefix]
    private static void Start_Postfix(OptionsMenuBehaviour __instance)
    {
        /*
        static bool toggleCheckInGamePlay(string buttonName)
        {
            bool flag = GameState.IsInGame && !GameState.IsLobby || GameState.IsFreePlay;
            if (flag)
                BetterNotificationManager.Notify($"Unable to toggle '{buttonName}' while in gameplay!", 2.5f);

            return flag;
        }
        */

        static bool toggleCheckInGame(string buttonName)
        {
            bool flag = GameState.IsInGame;
            if (flag)
                BetterNotificationManager.Notify($"Unable to toggle '{buttonName}' while in game!", 2.5f);

            return flag;
        }

        if (__instance.DisableMouseMovement == null) return;

        if (AntiCheat == null || AntiCheat.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.AntiCheat");
            AntiCheat = ClientOptionItem.Create(title, BAUPlugin.AntiCheat, __instance);
        }

        if (SendBetterRpc == null || SendBetterRpc.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.SendBetterRpc");
            SendBetterRpc = ClientOptionItem.Create(title, BAUPlugin.SendBetterRpc, __instance, SendBetterRpcToggle);

            static void SendBetterRpcToggle()
            {
                if (GameState.IsInGame)
                {
                    foreach (var player in BAUPlugin.AllPlayerControls)
                    {
                        if (player.IsLocalPlayer()) continue;
                        player.BetterData().HandshakeHandler.ResendSecretToPlayer();
                    }
                }
            }
        }

        if (BetterNotifications == null || BetterNotifications.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.BetterNotifications");
            BetterNotifications = ClientOptionItem.Create(title, BAUPlugin.BetterNotifications, __instance, BetterNotificationsToggle);

            static void BetterNotificationsToggle()
            {
                BetterNotificationManager.NotifyQueue.Clear();
                BetterNotificationManager.showTime = 0f;
                BetterNotificationManager.Notifying = false;
            }
        }

        if (ForceOwnLanguage == null || ForceOwnLanguage.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ForceOwnLanguage");
            ForceOwnLanguage = ClientOptionItem.Create(title, BAUPlugin.ForceOwnLanguage, __instance);
        }

        if (ChatDarkMode == null || ChatDarkMode.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ChatDarkMode");
            ChatDarkMode = ClientOptionItem.Create(title, BAUPlugin.ChatDarkMode, __instance, ChatDarkModeToggle);

            static void ChatDarkModeToggle()
            {
                ChatPatch.SetChatTheme();
            }
        }

        if (ChatInGameplay == null || ChatInGameplay.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ChatInGame");
            ChatInGameplay = ClientOptionItem.Create(title, BAUPlugin.ChatInGameplay, __instance);
        }

        if (LobbyPlayerInfo == null || LobbyPlayerInfo.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.LobbyInfo");
            LobbyPlayerInfo = ClientOptionItem.Create(title, BAUPlugin.LobbyPlayerInfo, __instance);
        }

        if (DisableLobbyTheme == null || DisableLobbyTheme.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.LobbyTheme");
            DisableLobbyTheme = ClientOptionItem.Create(title, BAUPlugin.DisableLobbyTheme, __instance, DisableLobbyThemeButtonToggle);
            static void DisableLobbyThemeButtonToggle()
            {
                if (GameState.IsLobby && !BAUPlugin.DisableLobbyTheme.Value)
                {
                    SoundManager.instance.CrossFadeSound("MapTheme", LobbyBehaviour.Instance.MapTheme, 0.5f, 1.5f);
                }
            }
        }

        if (UnlockFPS == null || UnlockFPS.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.UnlockFPS");
            UnlockFPS = ClientOptionItem.Create(title, BAUPlugin.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = BAUPlugin.UnlockFPS.Value ? 165 : 60;
            }
        }

        if (ShowFPS == null || ShowFPS.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ShowFPS");
            ShowFPS = ClientOptionItem.Create(title, BAUPlugin.ShowFPS, __instance);
        }

        if (OpenSaveData == null || OpenSaveData.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.SaveData");
            OpenSaveData = ClientOptionItem.Create(title, null, __instance, OpenSaveDataButtonToggle, IsToggle: false);
            static void OpenSaveDataButtonToggle()
            {
                if (File.Exists(BetterDataManager.dataPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = BetterDataManager.dataPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
        }

        if (SwitchToVanilla == null || SwitchToVanilla.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ToVanilla");
            SwitchToVanilla = ClientOptionItem.Create(title, null, __instance, SwitchToVanillaButtonToggle, IsToggle: false, toggleCheck: () => !toggleCheckInGame(title));
            static void SwitchToVanillaButtonToggle()
            {
                ConsoleManager.DetachConsole();
                BetterNotificationManager.BAUNotificationManagerObj.DestroyObj();
                Harmony.UnpatchAll();
                ModManager.Instance.ModStamp.gameObject.SetActive(false);
                SceneChanger.ChangeScene("MainMenu");
            }
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    [HarmonyPrefix]
    private static void Close_Postfix()
    {
        ClientOptionItem.CustomBackground?.gameObject.SetActive(false);
    }
}