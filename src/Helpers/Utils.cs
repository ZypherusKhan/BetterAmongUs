using AmongUs.Data;
using BetterAmongUs.Modules;
using BetterAmongUs.Patches.Gameplay.UI.Chat;
using InnerNet;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterAmongUs.Helpers;

internal static class Utils
{
    internal static Dictionary<string, Sprite> CachedSprites = [];

    // String extensions and formatting
    internal static string Size(this string str, float size) => $"<size={size}%>{str}</size>";

    internal static string RemoveSizeHtmlText(string text)
    {
        text = Regex.Replace(text, "<size=[^>]*>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</size>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ").Trim();

        return text;
    }

    internal static string FormatInfo(StringBuilder source)
    {
        if (source.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var part in source.ToString().Split("+++"))
        {
            if (!string.IsNullOrEmpty(RemoveHtmlText(part)))
            {
                sb.Append(part).Append(" - ");
            }
        }
        return sb.ToString().TrimEnd(" - ".ToCharArray());
    }

    internal static string RemoveHtmlText(string text)
    {
        text = Regex.Replace(text, "<[^>]*>", "");
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ");
        text = text.Trim();

        return text;
    }

    internal static bool IsHtmlText(string text)
    {
        return Regex.IsMatch(text, "<[^>]*>") ||
               Regex.IsMatch(text, "{[^}]*}") ||
               text.Contains("\n") ||
               text.Contains("\r");
    }

    // Network utilities
    internal static bool IsInternetAvailable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
            return false;

        UnityWebRequest? www = null;
        try
        {
            www = UnityWebRequest.Get("http://clients3.google.com/generate_204");
            www.SendWebRequest();
            while (!www.isDone) { }
            return www.result == UnityWebRequest.Result.Success && www.responseCode == 204;
        }
        catch
        {
            return false;
        }
        finally
        {
            www?.Dispose();
        }
    }

    // Player lookup methods
    internal static ClientData? ClientFromClientId(int clientId) =>
        AmongUsClient.Instance.allClients.FirstOrDefaultIl2Cpp(cd => cd.Id == clientId);

    internal static NetworkedPlayerInfo? PlayerDataFromPlayerId(int playerId) =>
        GameData.Instance.AllPlayers.FirstOrDefaultIl2Cpp(data => data.PlayerId == playerId);

    internal static NetworkedPlayerInfo? PlayerDataFromClientId(int clientId) =>
        GameData.Instance.AllPlayers.FirstOrDefaultIl2Cpp(data => data.ClientId == clientId);

    internal static NetworkedPlayerInfo? PlayerDataFromFriendCode(string friendCode) =>
        GameData.Instance.AllPlayers.FirstOrDefaultIl2Cpp(data => data.FriendCode == friendCode);

    internal static PlayerControl? PlayerFromPlayerId(int playerId) =>
        BAUPlugin.AllPlayerControls.FirstOrDefault(player => player.PlayerId == playerId);

    internal static PlayerControl? PlayerFromClientId(int clientId) =>
        BAUPlugin.AllPlayerControls.FirstOrDefault(player => player.GetClientId() == clientId);

    internal static PlayerControl? PlayerFromNetId(uint netId) =>
        BAUPlugin.AllPlayerControls.FirstOrDefault(player => player.NetId == netId);

    // Chat functionality
    internal static void AddChatPrivate(string text, string overrideName = "", bool setRight = false)
    {
        if (!GameState.IsInGame) return;

        var chat = HudManager.Instance?.Chat;
        if (chat == null) return;

        var data = PlayerControl.LocalPlayer?.Data;
        if (data == null) return;

        var pooledBubble = chat.GetPooledBubble();
        var messageName = $"<color=#ffffff><b>(<color=#00ff44>{Translator.GetString("SystemMessage")}</color>)</b>" + ChatPatch.COMMAND_POSTFIX_ID;

        if (!string.IsNullOrEmpty(overrideName))
            messageName = overrideName + ChatPatch.COMMAND_POSTFIX_ID;

        try
        {
            pooledBubble.transform.SetParent(chat.scroller.Inner);
            pooledBubble.transform.localScale = Vector3.one;
            pooledBubble.SetCosmetics(data);
            pooledBubble.gameObject.transform.Find("PoolablePlayer").gameObject.SetActive(false);
            pooledBubble.ColorBlindName.gameObject.SetActive(false);

            if (!setRight)
            {
                pooledBubble.SetLeft();
                pooledBubble.gameObject.transform.Find("NameText (TMP)").transform.localPosition += new Vector3(-0.7f, 0f);
                pooledBubble.gameObject.transform.Find("ChatText (TMP)").transform.localPosition += new Vector3(-0.7f, 0f);
            }
            else
            {
                pooledBubble.SetRight();
            }

            chat.SetChatBubbleName(pooledBubble, data, false, false, PlayerNameColor.Get(data), null);
            pooledBubble.SetText(text);
            pooledBubble.AlignChildren();
            chat.AlignAllBubbles();
            pooledBubble.NameText.text = messageName;

            if (!chat.IsOpenOrOpening && chat.notificationRoutine == null)
            {
                chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
            }

            SoundManager.Instance.PlaySound(chat.messageSound, false, 1f, null).pitch = 0.5f + data.PlayerId / 15f;
        }
        catch
        {
            // Intentionally empty - chat failures shouldn't crash the game
        }
    }

    // System type checks
    internal static bool SystemTypeIsSabotage(SystemTypes type) => type is
        SystemTypes.Reactor or SystemTypes.Laboratory or SystemTypes.Comms or
        SystemTypes.LifeSupp or SystemTypes.MushroomMixupSabotage or
        SystemTypes.HeliSabotage or SystemTypes.Electrical;

    internal static bool SystemTypeIsSabotage(int typeNum) =>
        SystemTypeIsSabotage((SystemTypes)typeNum);

    // Hashing utilities
    internal static string GetHashPuid(PlayerControl player)
    {
        return player?.Data?.Puid == null ? "" : GetHashStr(player.Data.Puid);
    }

    internal static string GetHashStr(this string str)
    {
        if (string.IsNullOrEmpty(str)) return "";

        using var sha256 = SHA256.Create();
        var sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
        var sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();
        return sha256Hash[..5] + sha256Hash[^4..];
    }

    internal static ushort GetHashUInt16(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        return (ushort)(BitConverter.ToUInt16(SHA256.HashData(Encoding.UTF8.GetBytes(input)), 0) % 65536);
    }

    // Color utilities
    internal static string GetTeamHexColor(RoleTeamTypes team)
    {
        return team == RoleTeamTypes.Impostor ? "#f00202" : "#8cffff";
    }

    internal static string Color32ToHex(Color32 color) => $"#{color.r:X2}{color.g:X2}{color.b:X2}{255:X2}";

    internal static Color HexToColor32(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex[1..];
        }

        byte r = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, 255);
    }

    internal static Color LerpColor(Color[] colors, (float min, float max) lerpRange, float t, bool reverse = false)
    {
        float normalizedT = Mathf.InverseLerp(lerpRange.min, lerpRange.max, t);

        if (colors.Length == 1)
            return colors[0];

        if (reverse)
        {
            Array.Reverse(colors);
        }

        if (normalizedT <= 0f)
            return colors[0];
        if (normalizedT >= 1f)
            return colors[^1];

        float segmentSize = 1f / (colors.Length - 1);
        int segmentIndex = (int)(normalizedT / segmentSize);
        float segmentT = (normalizedT - segmentIndex * segmentSize) / segmentSize;

        return Color.Lerp(colors[segmentIndex], colors[segmentIndex + 1], segmentT);
    }

    // Network and UI operations
    internal static void DisconnectAccountFromOnline(bool apiError = false)
    {
        if (GameState.IsInGame)
        {
            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
        }

        DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.Offline;
        DataManager.Player.Save();

        if (apiError)
        {
            ShowPopUp(Translator.GetString("DataBaseConnect.InitFailure"), true);
        }
    }

    internal static void SettingsChangeNotifier(int id, string text, bool playSound = true)
    {
        var notifier = HudManager.Instance.Notifier;

        if (notifier.lastMessageKey == id && notifier.activeMessages.Count > 0)
        {
            notifier.activeMessages[^1].UpdateMessage(text);
        }
        else
        {
            notifier.lastMessageKey = id;
            var newMessage = UnityEngine.Object.Instantiate(
                notifier.notificationMessageOrigin,
                Vector3.zero,
                Quaternion.identity,
                notifier.transform
            );

            newMessage.transform.localPosition = new Vector3(0f, 0f, -2f);
            newMessage.SetUp(text, notifier.settingsChangeSprite, notifier.settingsChangeColor, (Action)(() =>
            {
                notifier.OnMessageDestroy(newMessage);
            }));

            notifier.ShiftMessages();
            notifier.AddMessageToQueue(newMessage);
        }

        if (playSound)
        {
            SoundManager.Instance.PlaySoundImmediate(notifier.settingsChangeSound, false, 1f, 1f, null);
        }
    }

    internal static void DisconnectSelf(string reason, bool showReason = true)
    {
        AmongUsClient.Instance.ExitGame(0);

        LateTask.Schedule(() =>
        {
            SceneChanger.ChangeScene("MainMenu");

            if (showReason)
            {
                LateTask.Schedule(() =>
                {
                    var lines = "<color=#ebbd34>----------------------------------------------------------------------------------------------</color>";
                    ShowPopUp($"{lines}\n\n\n<size=150%>{reason}</size>\n\n\n{lines}");
                }, 0.1f, "DisconnectSelf 2");
            }
        }, 0.2f, "DisconnectSelf 1");
    }

    internal static void ShowPopUp(string text, bool enableWordWrapping = false)
    {
        DisconnectPopup.Instance.gameObject.SetActive(true);
        DisconnectPopup.Instance._textArea.enableWordWrapping = enableWordWrapping;
        DisconnectPopup.Instance._textArea.text = text;
    }

    // Resource loading
    internal static Sprite? LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            var cacheKey = path + pixelsPerUnit;
            if (CachedSprites.TryGetValue(cacheKey, out var sprite))
                return sprite;

            var texture = LoadTextureFromResources(path);
            if (texture == null)
                return null;

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return CachedSprites[cacheKey] = sprite;
        }
        catch (Exception ex)
        {
            Logger_.Error(ex);
            return null;
        }
    }

    internal static Texture2D? LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            if (stream == null)
                return null;

            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                if (!texture.LoadImage(ms.ToArray(), false))
                    return null;
            }

            return texture;
        }
        catch (Exception ex)
        {
            Logger_.Error(ex);
            return null;
        }
    }

    // Platform utilities
    internal static string GetPlatformName(PlayerControl player, bool useTag = false)
    {
        if (player?.GetClient()?.PlatformData == null) return string.Empty;
        return GetPlatformName(player.GetClient().PlatformData.Platform, useTag);
    }

    internal static string GetPlatformName(Platforms platform, bool useTag = false)
    {
        var (platformName, tag) = platform switch
        {
            Platforms.StandaloneSteamPC => ("Steam", "PC"),
            Platforms.StandaloneEpicPC => ("Epic Games", "PC"),
            Platforms.StandaloneWin10 => ("Microsoft Store", "PC"),
            Platforms.StandaloneMac => ("Mac OS", "PC"),
            Platforms.StandaloneItch => ("Itch.io", "PC"),
            Platforms.Xbox => ("Xbox", "Console"),
            Platforms.Playstation => ("Playstation", "Console"),
            Platforms.Switch => ("Switch", "Console"),
            Platforms.Android => ("Android", "Mobile"),
            Platforms.IPhone => ("IPhone", "Mobile"),
            Platforms.Unknown => ("None", ""),
            _ => (string.Empty, string.Empty)
        };

        if (string.IsNullOrEmpty(platformName))
            return string.Empty;

        return useTag && !string.IsNullOrEmpty(tag) ? $"{tag}: {platformName}" : platformName;
    }
}