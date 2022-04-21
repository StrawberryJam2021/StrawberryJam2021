using Celeste.Mod.Helpers;
using System;
using System.Collections.Generic;
using System.Net;

namespace Celeste.Mod.StrawberryJam2021 {
    /// <summary>
    /// "Self-updater" is probably an overstatement, since this effectively just adds an extra "update source" to the Everest mod updater.
    /// The rest of updating is handled by Everest itself.
    /// </summary>
    public static class SelfUpdater {
        public static void Load() {
            On.Celeste.Mod.Helpers.ModUpdaterHelper.DownloadModUpdateList += onDownloadModUpdateList;
        }

        public static void Unload() {
            On.Celeste.Mod.Helpers.ModUpdaterHelper.DownloadModUpdateList -= onDownloadModUpdateList;
        }

        private static Dictionary<string, ModUpdateInfo> onDownloadModUpdateList(On.Celeste.Mod.Helpers.ModUpdaterHelper.orig_DownloadModUpdateList orig) {
            Dictionary<string, ModUpdateInfo> updateCatalog = orig();

            if (updateCatalog != null) {
                try {
                    // download a Strawberry Jam-specific everest_update.yaml, and add it to the mod updater database.
                    using (WebClient wc = new WebClient()) {
                        string yamlData = wc.DownloadString("https://storage.googleapis.com/max480-random-stuff.appspot.com/strawberry_jam_extra_everest_update.yaml");
                        Dictionary<string, ModUpdateInfo> extraUpdateCatalog = YamlHelper.Deserializer.Deserialize<Dictionary<string, ModUpdateInfo>>(yamlData);
                        foreach (string name in extraUpdateCatalog.Keys) {
                            extraUpdateCatalog[name].Name = name;
                            updateCatalog[name] = extraUpdateCatalog[name];
                        }
                        Logger.Log("StrawberryJam2021/SelfUpdater", $"Downloaded {extraUpdateCatalog.Count} extra item(s), total: {updateCatalog.Count}");
                    }
                } catch (Exception) {
                    // Don't worry about it. The file will be taken down later on anyway.
                }
            }

            return updateCatalog;
        }
    }
}
