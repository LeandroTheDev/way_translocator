using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using Vintagestory.API.Client;
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

    // Overwrite teleportation target
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityStaticTranslocator), "GetTarget")]
    public static Vec3d GetTarget(Vec3d __result, BlockEntityStaticTranslocator __instance, Entity forEntity)
    {
        string translocatorPosition = __instance.Pos.ToString();

        // Check if is the player
        if (forEntity is EntityPlayer)
        {
            EntityPlayer player = forEntity as EntityPlayer;
            // Check if the translocator is know by the player
            if (knownTranslocators.TryGetValue(player.PlayerUID, out List<KeyValuePair<string, string>> translocatorFromPlayer))
            {
                // Know check...
                bool exist = false;
                foreach (KeyValuePair<string, string> keyValue in translocatorFromPlayer)
                {
                    if (keyValue.Value == translocatorPosition)
                    {
                        exist = true;
                        break;
                    }
                }
                // If is not know them add it to the know translocators
                if (!exist)
                {
                    if (true) // Extended logs
                        Debug.Log($"{player.Player.PlayerName} has discovered the waystone in: {translocatorPosition}");
                    knownTranslocators[player.PlayerUID].Add(new(GenerateRandomWayTranslocatorName(), translocatorPosition));
                }
            }

            // If player is sneaking and on the client side...
            if (player.Controls.Sneak && Initialization.ClientAPI != null)
            {
                ElementBounds[] buttons = new ElementBounds[1];

                buttons[0] = ElementBounds.Fixed(0, 0, 300, 40);
                for (int i = 1; i < buttons.Length; i++)
                {
                    buttons[i] = buttons[i - 1].BelowCopy(0, 1);
                }

                ElementBounds listBounds = ElementBounds
                    .Fixed(0, 0, 302, 400)
                    .WithFixedPadding(1);

                listBounds.BothSizing = ElementSizing.Fixed;

                ElementBounds messageBounds = listBounds
                    .BelowCopy(0, 10)
                    .WithFixedHeight(40);

                ElementBounds clipBounds = listBounds.ForkBoundingParent();
                ElementBounds insetBounds = listBounds
                    .FlatCopy()
                    .FixedGrow(6)
                    .WithFixedOffset(-3, -3);

                ElementBounds scrollbarBounds = insetBounds
                    .CopyOffsetedSibling(insetBounds.fixedWidth + 3.0)
                    .WithFixedWidth(GuiElementScrollbar.DefaultScrollbarWidth)
                    .WithFixedPadding(GuiElementScrollbar.DeafultScrollbarPadding);


                ElementBounds bgBounds = ElementBounds.Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                    .WithFixedOffset(0, GuiStyle.TitleBarHeight);

                bgBounds.BothSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren(insetBounds, scrollbarBounds);


                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog;

                bool emptyList = false;

                var SingleComposer = Initialization.ClientAPI.Gui
                    .CreateCompo($"waytranslocator-teleport-dialog", dialogBounds)
                    .AddDialogTitleBar("Teleports", () => { Debug.Log("BUTTON CLICKED"); })
                    .AddDialogBG(bgBounds, false)
                    .BeginChildElements(bgBounds)
                        .AddIf(emptyList)
                            .AddStaticText("No Teleports", CairoFont.WhiteSmallText(), buttons[0])
                        .EndIf()
                        .AddIf(!emptyList)
                            .BeginClip(clipBounds)
                                .AddInset(insetBounds, 3)
                                .AddContainer(listBounds, "stacklist")
                            .EndClip()
                            .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                            .AddHoverText("", CairoFont.WhiteDetailText(), 300, listBounds.FlatCopy(), "hoverText")
                        .EndIf()
                    .EndChildElements();

                SingleComposer.GetHoverText("hoverText").SetAutoWidth(true);

                void SetupTargetButtons(ElementBounds[] buttons)
                {
                    var stacklist = SingleComposer.GetContainer("stacklist");
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        var tp = _allPoints.ElementAt(i);
                        var data = tp.GetClientData(capi);

                        string name = tp.Name ?? "null";

                        var nameFont = CairoFont.WhiteSmallText();
                        bool activated = tp.ActivatedByPlayers.Contains(capi.World.Player!.PlayerUID);
                        bool enabled = tp.Enabled;

                        if (!enabled)
                        {
                            if (!activated)
                            {
                                nameFont.Color = ColorUtil.Hex2Doubles("#c91a1a");
                            }
                            else
                            {
                                nameFont.Color = ColorUtil.Hex2Doubles("#c95a5a");
                            }
                        }
                        else if (!activated)
                        {
                            nameFont.Color = ColorUtil.Hex2Doubles("#555555");
                        }

                        if (data.Pinned)
                        {
                            nameFont.FontWeight = FontWeight.Bold;
                        }

                        stacklist.Add(new GuiElementTeleportButton(capi,
                            name, tp.Pos, nameFont,
                            data.Pinned ?
                                CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold) :
                                CairoFont.WhiteSmallText(),
                            () => OnTeleportButtonClick(tp.Pos),
                            buttons[i],
                            EnumButtonStyle.Normal)
                        {
                            Enabled = IsPointEnabled(tp)
                        });
                    }
                }
                SetupTargetButtons(buttons);

                SingleComposer.Compose();

                SingleComposer.GetScrollbar("scrollbar").SetHeights(
                    (float)insetBounds.fixedHeight,
                    (float)Math.Max(insetBounds.fixedHeight, listBounds.fixedHeight));
            }
        }

        // Try to find the selected translocator to teleport to
        if (translocatorsSelections.TryGetValue(translocatorPosition, out string toPos))
        {
            forEntity.TeleportTo(10000, 150, 10000);
            Debug.Log($"SUCCESS FINDED THE TRANSLOCATOR POSITION: {toPos}");
        }

        return null;
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

    static string GenerateRandomWayTranslocatorName()
    {
        return "Unkown";
    }
}