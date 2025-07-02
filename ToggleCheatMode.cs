using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToggleCheatMode
{
    [BepInPlugin("jas.ToggleCheatMode", "Toggle Cheat Mode", "0.1")]
    public class ToggleCheatMode : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static ConfigEntry<KeyCode> toggleModeHotKey;
        internal static ConfigEntry<KeyCode> toggleMenuHotKey;
        internal static bool CreativeEnabled = false;
        internal static bool CreativeChecked = false;

        internal static FieldInfo commandsOn = AccessTools.Field(typeof(ChatBox), "commandsOn");
        internal static FieldInfo creativeMenuOpen = AccessTools.Field(typeof(CreativeManager), "creativeMenuOpen");
        internal static FieldInfo sortedIds = AccessTools.Field(typeof(CreativeManager), "sortedIds");
        internal static FieldInfo spawnableAnimals = AccessTools.Field(typeof(CreativeManager), "spawnableAnimals");
        internal static FieldInfo allButtons = AccessTools.Field(typeof(CreativeManager), "allButtons");
        internal static FieldInfo pageButtons = AccessTools.Field(typeof(CreativeManager), "pageButtons");
        internal static MethodInfo RunCreativeMode = AccessTools.Method(typeof(CreativeManager), "RunCreativeMode");
        internal static Harmony harmony = new Harmony("jas.Dinkum.ToggleCheatMode");

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("Plugin jas.Dinkum.ToggleCheatMode is loaded!");
            toggleModeHotKey = Config.Bind<KeyCode>("config", "toggleHotKey", KeyCode.LeftBracket);
            // NetworkMapSharer.Instance.onChangeMaps.AddListener(OnChangeMapsListener);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnChangeMapsListener()
        {
            Logger.LogMessage("Map Changed");
            // don't allow this mod to work in multiplayer
            if (!NetworkPlayersManager.manage.IsPlayingSinglePlayer)
            {
                Logger.LogMessage("MULTIPLAYER DETECTED, ABORTING");
                CreativeEnabled = false;
                CreativeChecked = false;
                return;
            }
            if (NetworkMapSharer.Instance.creativeAllowed)
            {
                Logger.LogMessage("Creative Mode Detected");
                CreativeEnabled = true;
            }
            else
            {
                Logger.LogMessage("Creative Mode Not Detected");
                CreativeEnabled = false;
            }
            CreativeChecked = true;
        }

        private void Update()
        {
            // Don't let this run prematurely or outside of singleplayer
            if (!CreativeChecked)
            {
                // Logger.LogMessage("Creative Mode not checked or not available.");
                return;
            }
            if (Input.GetKeyDown(toggleModeHotKey.Value))
            {
                if (!CreativeEnabled)
                {
                    Logger.LogDebug("Creative Enabled");
                    commandsOn.SetValue(ChatBox.chat, true);
                    Inventory.Instance.isCreative = true;
                    NetworkMapSharer.Instance.creativeAllowed = true;
                    PlayerPrefs.SetInt("DevCommandOn", 1);
                    PlayerPrefs.SetInt("Cheats", 1);
                    CreativeEnabled = true;
                    CreativeManager.instance.StartCreativeMode();
                }
                else
                {
                    Logger.LogDebug("Creative Disabled");
                    commandsOn.SetValue(ChatBox.chat, false);
                    Inventory.Instance.isCreative = false;
                    Inventory.Instance.hasBeenCreative = false;
                    NetworkMapSharer.Instance.creativeAllowed = false;
                    PlayerPrefs.DeleteKey("DevCommandOn");
                    PlayerPrefs.DeleteKey("Cheats");
                    CreativeEnabled = false;
                    creativeMenuOpen.SetValue(CreativeManager.instance, false);
                    // clear these lists as they're initialized over themselves, duplicating entries, if creative mode is enabled multiple times in one session

                    ((List<int>)sortedIds.GetValue(CreativeManager.instance)).Clear();
                    ((List<int>)spawnableAnimals.GetValue(CreativeManager.instance)).Clear();
                    ((List<CheatMenuButton>)allButtons.GetValue(CreativeManager.instance)).Clear();
                    ((List<CreativePageButton>)pageButtons.GetValue(CreativeManager.instance)).Clear();

                    GameObject creativeWindow = GameObject.Find("CreativeWindow");
                    Transform itemWindow = creativeWindow?.transform.Find("ItemWindow");
                    Transform buttons = itemWindow?.Find("Buttons");
                    if (buttons == null)
                    {
                        Logger.LogWarning("Failed to retrieve creative buttons!");
                    }
                    else
                    {
                        foreach (Transform child in buttons)
                        {
                            GameObject.Destroy(child.gameObject);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(LoadingScreen), "disappear")]
    public class LoadingScreenPatch
    {

        public static void Postfix(LoadingScreen __instance)
        {
            ToggleCheatMode.Logger.LogMessage("Postfix Triggered");
            // FieldInfo loadingWindowForMultiplayer = AccessTools.Field(typeof(LoadGameWindow), "loadingWindowForMultiplayer");
            if (!NetworkPlayersManager.manage.IsPlayingSinglePlayer)
            {
                ToggleCheatMode.Logger.LogWarning("This mod will not work in multiplayer out of respect for the Dinkum team's policy on cheating.");
                ToggleCheatMode.CreativeChecked = false;
                return;
            }
            else // if (NetworkPlayersManager.manage.IsPlayingSinglePlayer)
            {
                if (NetworkMapSharer.Instance.creativeAllowed)
                {
                    ToggleCheatMode.Logger.LogDebug("Creative Mode Detected");
                    ToggleCheatMode.CreativeEnabled = true;
                }
                else
                {
                    ToggleCheatMode.Logger.LogDebug("Creative Mode Not Detected");
                    ToggleCheatMode.CreativeEnabled = false;
                }
                ToggleCheatMode.CreativeChecked = true;
            }
        }
    }

}
