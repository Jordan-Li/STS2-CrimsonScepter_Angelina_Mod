using System;
using System.Linq;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Random;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
internal static class AngelinaRestSiteCreatePatch
{
    private static readonly ModelId AngelinaCharacterId = ModelDb.Character<Angelina>().Id;

    private static readonly System.Reflection.FieldInfo PlayerBackingField =
        AccessTools.Field(typeof(NRestSiteCharacter), "<Player>k__BackingField")!;

    private static readonly System.Reflection.FieldInfo CharacterIndexField =
        AccessTools.Field(typeof(NRestSiteCharacter), "_characterIndex")!;

    private static bool Prefix(Player player, int characterIndex, ref NRestSiteCharacter __result)
    {
        if (player.Character?.Id != AngelinaCharacterId)
        {
            return true;
        }

        Node2D sceneRoot =
            PreloadManager.Cache.GetScene(player.Character.RestSiteAnimPath).Instantiate<Node2D>(PackedScene.GenEditState.Disabled);

        var restSiteCharacter = new NRestSiteCharacter
        {
            Name = sceneRoot.Name,
            Position = sceneRoot.Position,
            Rotation = sceneRoot.Rotation,
            Scale = sceneRoot.Scale
        };

        // 原版这里会直接要求 NRestSiteCharacter，先把纯场景的子节点搬进去再继续后续初始化。
        while (sceneRoot.GetChildCount() > 0)
        {
            Node child = sceneRoot.GetChild(0);
            sceneRoot.RemoveChild(child);
            restSiteCharacter.AddChild(child);
            child.Owner = restSiteCharacter;
        }

        sceneRoot.Free();

        PlayerBackingField.SetValue(restSiteCharacter, player);
        CharacterIndexField.SetValue(restSiteCharacter, characterIndex);
        __result = restSiteCharacter;
        return false;
    }
}

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
internal static class AngelinaRestSiteSelectionReticlePatch
{
    private static readonly ModelId AngelinaCharacterId = ModelDb.Character<Angelina>().Id;

    private static readonly Action<NRestSiteCharacter> OnFocusDelegate =
        AccessTools.MethodDelegate<Action<NRestSiteCharacter>>(AccessTools.Method(typeof(NRestSiteCharacter), "OnFocus")!);

    private static readonly Action<NRestSiteCharacter> OnUnfocusDelegate =
        AccessTools.MethodDelegate<Action<NRestSiteCharacter>>(AccessTools.Method(typeof(NRestSiteCharacter), "OnUnfocus")!);

    private static readonly System.Reflection.FieldInfo ControlRootField =
        AccessTools.Field(typeof(NRestSiteCharacter), "_controlRoot")!;

    private static readonly System.Reflection.FieldInfo SelectionReticleField =
        AccessTools.Field(typeof(NRestSiteCharacter), "_selectionReticle")!;

    private static readonly System.Reflection.FieldInfo LeftThoughtAnchorField =
        AccessTools.Field(typeof(NRestSiteCharacter), "_leftThoughtAnchor")!;

    private static readonly System.Reflection.FieldInfo RightThoughtAnchorField =
        AccessTools.Field(typeof(NRestSiteCharacter), "_rightThoughtAnchor")!;

    private static readonly System.Reflection.FieldInfo HitboxBackingField =
        AccessTools.Field(typeof(NRestSiteCharacter), "<Hitbox>k__BackingField")!;

    private static bool Prefix(NRestSiteCharacter __instance)
    {
        if (__instance.Player?.Character?.Id != AngelinaCharacterId)
        {
            return true;
        }

        Control controlRoot = __instance.GetNode<Control>("ControlRoot");
        Control hitbox = controlRoot.GetNode<Control>("Hitbox");
        Control leftThoughtAnchor = controlRoot.GetNode<Control>("ThoughtBubbleLeft");
        Control rightThoughtAnchor = controlRoot.GetNode<Control>("ThoughtBubbleRight");
        NSelectionReticle selectionReticle = EnsureSelectionReticle(__instance, controlRoot);

        ControlRootField.SetValue(__instance, controlRoot);
        SelectionReticleField.SetValue(__instance, selectionReticle);
        LeftThoughtAnchorField.SetValue(__instance, leftThoughtAnchor);
        RightThoughtAnchorField.SetValue(__instance, rightThoughtAnchor);
        HitboxBackingField.SetValue(__instance, hitbox);

        PlayRestSiteAnimation(__instance);

        hitbox.Connect(Control.SignalName.FocusEntered, Callable.From(() => OnFocusDelegate(__instance)));
        hitbox.Connect(Control.SignalName.FocusExited, Callable.From(() => OnUnfocusDelegate(__instance)));
        hitbox.Connect(Control.SignalName.MouseEntered, Callable.From(() => OnFocusDelegate(__instance)));
        hitbox.Connect(Control.SignalName.MouseExited, Callable.From(() => OnUnfocusDelegate(__instance)));
        return false;
    }

    private static NSelectionReticle EnsureSelectionReticle(NRestSiteCharacter owner, Control controlRoot)
    {
        if (controlRoot.GetNodeOrNull<NSelectionReticle>("SelectionReticle") is { } existingTyped)
        {
            return existingTyped;
        }

        Control? oldControl = controlRoot.GetNodeOrNull<Control>("SelectionReticle");
        var reticle = new NSelectionReticle
        {
            Name = "SelectionReticle",
            LayoutMode = oldControl?.LayoutMode ?? 3,
            AnchorLeft = oldControl?.AnchorLeft ?? 0f,
            AnchorTop = oldControl?.AnchorTop ?? 0f,
            AnchorRight = oldControl?.AnchorRight ?? 0f,
            AnchorBottom = oldControl?.AnchorBottom ?? 0f,
            OffsetLeft = oldControl?.OffsetLeft ?? -294.629f,
            OffsetTop = oldControl?.OffsetTop ?? -409.523f,
            OffsetRight = oldControl?.OffsetRight ?? 100.371f,
            OffsetBottom = oldControl?.OffsetBottom ?? 280.477f,
            Scale = oldControl?.Scale ?? Vector2.One,
            PivotOffset = oldControl?.PivotOffset ?? Vector2.Zero,
            MouseFilter = oldControl?.MouseFilter ?? Control.MouseFilterEnum.Ignore
        };

        controlRoot.AddChild(reticle);
        reticle.Owner = owner;

        if (oldControl != null)
        {
            while (oldControl.GetChildCount() > 0)
            {
                Node child = oldControl.GetChild(0);
                oldControl.RemoveChild(child);
                reticle.AddChild(child);
                child.Owner = owner;
            }

            oldControl.GetParent()?.RemoveChild(oldControl);
            oldControl.QueueFree();
        }

        return reticle;
    }

    private static void PlayRestSiteAnimation(NRestSiteCharacter instance)
    {
        string animationName = instance.Player.RunState.CurrentActIndex switch
        {
            0 => "overgrowth_loop",
            1 => "hive_loop",
            2 => "glory_loop",
            _ => throw new InvalidOperationException("Unexpected act")
        };

        foreach (Node2D childSpineNode in instance.GetChildren().OfType<Node2D>().Where(static node => node.GetClass() == "SpineSprite"))
        {
            MegaTrackEntry? track = new MegaSprite(childSpineNode).GetAnimationState().SetAnimation(animationName);
            track?.SetTrackTime(track.GetAnimationEnd() * Rng.Chaotic.NextFloat());
        }
    }
}
