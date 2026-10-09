/* using HarmonyLib;

namespace StoreRotationConfig.Compatibility
{
    internal static class InteractiveStoreCompatibility
    {
        /// <summary>
        ///     Whether <c>InteractiveStore</c> is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(WeatherProbe.Misc.Metadata.GUID);

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;
    }
} */