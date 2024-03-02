using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace StoreRotationConfig
{
    public class Config
    {
        public static ConfigEntry<int> minItems, maxItems;
        public static ConfigEntry<bool> stockAll;

        public Config(ConfigFile cfg)
        {
            minItems = cfg.Bind("General", "minItems", 8, "Minimum number of items in store rotation.");
            maxItems = cfg.Bind("General", "maxItems", 12, "Maximum number of items in store rotation.");
            stockAll = cfg.Bind("General", "stockAll", false, "[EXPERIMENTAL] Make every item available in store rotation.");
        }
    }

    [BepInPlugin(GUID, PluginInfo.PLUGIN_NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string GUID = "pacoito.StoreRotationConfig", VERSION = "1.0.1";
        public static Config Settings { get; internal set; }

        private void Awake()
        {
            try
            {
                Settings = new(Config);

                Harmony harmony = new(PluginInfo.PLUGIN_NAME);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} is loaded!");
            }
            catch (Exception e)
            {
                Logger.LogError("[!] Error while initializing: '" + e.Message + "'\nSource: '" + e.Source + "'");
            }
        }
    }

    [HarmonyPatch(typeof(Terminal), methodName: nameof(Terminal.RotateShipDecorSelection))]
    public static class RotateShipDecorSelectionPatch
    {
        [HarmonyPriority(Priority.VeryHigh)]
        private static bool Prefix(Terminal __instance)
        {
            Random random = new(StartOfRound.Instance.randomMapSeed + 65);
            __instance.ShipDecorSelection.Clear();

            List<TerminalNode> list = []; // TODO: Cache list if stockAll is true?
            foreach (UnlockableItem item in StartOfRound.Instance.unlockablesList.unlockables)
            {
                if (item.shopSelectionNode != null && !item.alwaysInStock)
                {
                    list.Add(item.shopSelectionNode);
                }
            }

            bool stockAll = Config.stockAll.Value;
            int maxItems = Config.maxItems.Value, minItems = (Config.minItems.Value < maxItems) ? Config.minItems.Value : maxItems;

            int num = !stockAll ? ((minItems != maxItems) ? random.Next(minItems, maxItems + 1) : maxItems) : list.Count;

            for (int i = 0; i < num; i++)
            {
                if (list.Count < 1)
                {
                    break;
                }

                int index = !stockAll ? random.Next(0, list.Count) : 0;
                __instance.ShipDecorSelection.Add(list[index]);
                list.RemoveAt(index);
            }

            return false;
        }
    }
}