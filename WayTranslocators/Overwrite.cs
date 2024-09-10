using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using WayTranslocators;

[HarmonyPatchCategory("waytranslocator")]
class Overwrite
{
    private static Initialization instance;
    private Harmony overwriter;

    /// Stores all know translocations by players
    ///{
    ///     "playeruid": {
    ///         "translocator_name": "X,Y,Z"
    ///     }
    ///}
    public readonly static Dictionary<string, List<KeyValuePair<string, string>>> knownTranslocators = new();

    /// Stores all translocators setups for to other translocators
    ///{
    ///     "trans-pos-from-xyz": "trans-pos-to-xyz"
    ///}
    public readonly static Dictionary<string, string> translocatorsSelections = new();


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

    // Overwrite collide target
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityStaticTranslocator), "OnEntityCollide")]
    public static void OnEntityCollide(BlockEntityStaticTranslocator __instance, Entity entity)
    {
        // 100, 100, 100
        Debug.Log(__instance.Pos.ToString());
        // Show the dialog here
    }

    // Overwrite teleportation target
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityStaticTranslocator), "GetTarget")]
    public static Vec3d GetTarget(Vec3d __result, BlockEntityStaticTranslocator __instance, Entity forEntity)
    {
        // Try to find the selected translocator to teleport to
        if (translocatorsSelections.TryGetValue(__instance.Pos.ToString(), out string toPos))
        {
            Debug.Log($"SUCCESS FINDED THE TRANSLOCATOR POSITION: {toPos}");
            return null;
        }
        // If cannot find the selected translocator we disable the teleport
        else
        {
            Debug.Log("NO TRANSLOCATORS HAS BEEN SET");
            return null;
        }
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
        else if (knownTranslocators.TryGetValue(forPlayer.PlayerUID, out List<KeyValuePair<string, string>> translocators))
        {
            foreach (KeyValuePair<string, string> keyValue in translocators)
            {
                if (keyValue.Value == __instance.Pos.ToString())
                {
                    dsc.AppendLine(keyValue.Key);
                    break;
                }
            }
        }
        else
        {
            dsc.AppendLine(Lang.Get("waytranslocators:unkown-traslocator"));
        }
        return false;
    }
}