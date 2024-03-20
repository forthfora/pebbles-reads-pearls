using System;
using System.Linq;

namespace PebblesReadsPearls;

public static partial class Hooks
{
    public static void ApplyInit() => On.RainWorld.OnModsInit += RainWorld_OnModsInit;

    public static bool IsInit { get; private set; } = false;

    private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        try
        {
            if (IsInit) return;
            IsInit = true;

            // Fetch metadata about the mod
            var mod = ModManager.ActiveMods.FirstOrDefault(mod => mod.id == Plugin.MOD_ID);

            Plugin.MOD_NAME = mod.name;
            Plugin.VERSION = mod.version;
            Plugin.AUTHORS = mod.authors;

            PRPRivulet = new(nameof(PRPRivulet), true);
            PRPRivuletEnding = new(nameof(PRPRivuletEnding), true);

            ApplyHooks();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("OnModsInit:\n" + e);
        }
        finally
        {
            orig(self);
        }
    }

    public static void ApplyHooks()
    {
        ApplyFunctionHooks();
    }
}