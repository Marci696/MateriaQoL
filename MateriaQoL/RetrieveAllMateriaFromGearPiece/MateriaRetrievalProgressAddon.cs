using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace MateriaQol.RetrieveAllMateriaFromGearPiece;

public class MateriaRetrievalProgressAddon(
    Queue<QueuedGearPiece> queuedGearItems,
    List<GearPieceNodeData> finishedGearItems,
    Action onFinalize
)
    : NativeAddon
{
    private ListNode<GearPieceNodeData, GearPieceListItemNode>? listNode;

    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        base.OnSetup(addon);

        Size = new Vector2(300f, 400f);

        var padding = new Vector2(8f, 0f);

        listNode = new ListNode<GearPieceNodeData, GearPieceListItemNode>()
        {
            Position = (WindowNode?.ContentStartPosition ?? Vector2.Zero) + padding,
            Size = (WindowNode?.ContentSize ?? Vector2.Zero) - (padding * 2.0f),
            OptionsList = [],
            ItemSpacing = 5f,
        };

        listNode.AttachNode(this);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon)
    {
        base.OnUpdate(addon);

        var itemsForNodeList = finishedGearItems.Concat(
                queuedGearItems.Select(item => item.ToGearListItemNodeData())
            )
            .ToList();

        listNode?.OptionsList = finishedGearItems.Concat(
                queuedGearItems.Select(item => item.ToGearListItemNodeData())
            )
            .ToList();
        listNode?.Update();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon)
    {
        base.OnFinalize(addon);

        onFinalize?.Invoke();
    }
}
