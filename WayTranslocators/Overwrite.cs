using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using WayTranslocators;

[HarmonyPatchCategory("waytranslocator")]
class Overwrite
{
    private static Initialization instance;
    private Harmony overwriter;

    private static readonly Random random = new();

    public void OverwriteNativeFunctions(Initialization _instance)
    {
        instance = _instance;
        if (!Harmony.HasAnyPatches("waytranslocator"))
        {
            overwriter = new Harmony("waytranslocator");
            overwriter.PatchCategory("waytranslocator");
            Debug.Log("Translocators has been overwrited");
        }
        else
        {
            if (instance.api.Side == EnumAppSide.Client) Debug.Log("Waytranslocator has already been patched, probably by the singleplayer server");
            else Debug.Log("ERROR: Block Waytranslocator has already been patched, did some mod already has waytranslocator in harmony?");
        }
    }

    // Overwrite interaction
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityStaticTranslocator), "didTeleport")]
    public static void didTeleport(BlockEntityStaticTranslocator __instance, Entity entity)
    {
        // We need to understand how the translocator works
        Debug.Log("Testing...");
    }


    // Overwrite block info
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityStaticTranslocator), "GetBlockInfo")]
    public static bool GetBlockInfo(BlockEntityStaticTranslocator __instance, IPlayer forPlayer, StringBuilder dsc)
    {

        if (!__instance.FullyRepaired)
        {
            dsc.AppendLine(Lang.Get("Seems to be missing a couple of gears. I think I've seen such gears before.", Array.Empty<object>()));
        }
        else if (Initialization.knownTranslocators.TryGetValue(forPlayer.PlayerUID, out List<KeyValuePair<string, string>> translocators))
        {
            foreach (KeyValuePair<string, string> keyValue in translocators)
            {
                if (keyValue.Value == __instance.Pos.ToString())
                {
                    dsc.AppendLine(keyValue.Key);
                    break;
                }
            }
        } else {
            dsc.AppendLine(Lang.Get("waytranslocators:unkown-traslocator"));
        }
        return false;
    }
}