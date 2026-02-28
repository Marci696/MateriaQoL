using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using MateriaQoL;

namespace MateriaQol.RetrieveAllMateriaFromGearPiece;

public unsafe class RetrieveAllMateriaFromGearPiece
{
    private MateriaRetrievalProgressAddon? materiaRetrievalProgressAddon;

    private readonly List<GearPieceNodeData> finishedGearPieces = [];
    private readonly Queue<QueuedGearPiece> queuedGearPieces = [];

    public RetrieveAllMateriaFromGearPiece()
    {
        materiaRetrievalProgressAddon = new MateriaRetrievalProgressAddon(
            queuedGearPieces,
            finishedGearPieces,
            ClearLists
        )
        {
            // todo how can this be moved directly to addon, no need to do it here really.
            Size = new Vector2(300, 350),
            InternalName = "MateriaRetrievalProgress",
            Title = "Materia Retrieval Progress",
        };

        Plugin.ContextMenu.OnMenuOpened += OnMenuOpened;
    }

    public void Dispose()
    {
        ClearLists();

        Plugin.ContextMenu.OnMenuOpened -= OnMenuOpened;
        Plugin.Framework.Update -= OnFrameworkUpdate;

        materiaRetrievalProgressAddon?.Dispose();
        materiaRetrievalProgressAddon = null;
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (GetInventoryItem(args) is not { } inventoryItem)
        {
            return;
        }

        if (!DoesInventoryItemSupportMateria(inventoryItem))
        {
            return;
        }

        args.AddMenuItem(
            new MenuItem
            {
                IsSubmenu = false,
                IsEnabled = inventoryItem.Value->GetMateriaCount() > 0,
                Name = "Retrieve All Materia",

                // Blue circle to imitate the look of melded materia.
                PrefixColor = 37,
                Prefix = SeIconChar.Circle,

                OnClicked = clickedArgs =>
                {
                    clickedArgs.OpenSubmenu(
                        [
                            new MenuItem
                            {
                                Name = "Confirm",
                                OnClicked = _ => AddGearPieceToQueue(inventoryItem),
                            },
                            new MenuItem
                            {
                                Name = "Cancel",
                            },
                        ]
                    );
                },
            }
        );
    }

    private static Pointer<InventoryItem>? GetInventoryItem(IMenuOpenedArgs args)
    {
        if (args.AddonName == "MateriaAttach")
        {
            var agent = AgentMateriaAttach.Instance();

            if (agent->SelectedItemIndex < 0)
            {
                return null;
            }

            if (agent->Data->ItemsSorted.Length <= agent->SelectedItemIndex)
            {
                return null;
            }

            var itemByIndex = agent->Data->ItemsSorted[agent->SelectedItemIndex];

            return itemByIndex.Value->Item;
        }

        if (args.MenuType != ContextMenuType.Inventory)
        {
            return null;
        }

        if (args.Target is not MenuTargetInventory invTarget)
        {
            return null;
        }

        if (invTarget.TargetItem is not { } targetItem)
        {
            return null;
        }

        return (InventoryItem*)targetItem.Address;
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
    {
        var currentItemForRetrieval = queuedGearPieces.FirstOrDefault();

        if (currentItemForRetrieval is null)
        {
            Plugin.Log.Debug("Queue is empty and framework update will be unsubscribed to.");
            Plugin.Framework.Update -= OnFrameworkUpdate;

            return;
        }

        if (IsCurrentlyRetrievingMateria())
        {
            return;
        }

        switch (currentItemForRetrieval.GetRetrievalAttemptStatus())
        {
            case RetrievalAttemptStatus.NoAttemptMade:
                currentItemForRetrieval.AttemptRetrieval();

                return;
            case RetrievalAttemptStatus.RetrievedSome:
                Plugin.Log.Debug($"Retrieved some materia from itemId: {currentItemForRetrieval.ItemId}");
                // There is more materia left to retrieve.
                currentItemForRetrieval.AttemptRetrieval();

                return;
            case RetrievalAttemptStatus.RetrievedAll:
                Plugin.Log.Debug($"Retrieved all materia from itemId: {currentItemForRetrieval.ItemId}");

                DequeueGearPiece();

                return;
            case RetrievalAttemptStatus.AttemptRunning:
                // Check again in the next update tick.
                return;
            case RetrievalAttemptStatus.RetryNeeded:
                Plugin.Log.Debug(
                    $"Retrying retrieval of materia from itemId: {currentItemForRetrieval.ItemId}"
                );
                currentItemForRetrieval.AttemptRetrieval();

                return;
            case RetrievalAttemptStatus.TimedOut:
                // Character must have been busy and unable to retrieve materia in the current state.
                Plugin.Log.Debug("Timed out while retrieving materia from one gear piece");
                Plugin.ChatGui.PrintError("Materia cannot be retrieved in current state.");

                DequeueGearPiece();

                return;
        }
    }

    private void AddGearPieceToQueue(InventoryItem* item)
    {
        // No need to queue duplicates.
        if (queuedGearPieces.Any((alreadyQueuedItem) => alreadyQueuedItem.IsForInventoryItem(item)))
        {
            return;
        }

        queuedGearPieces.Enqueue(new QueuedGearPiece(item));

        materiaRetrievalProgressAddon?.Open();
        Plugin.Framework.Update += OnFrameworkUpdate;

        Plugin.Log.Debug($"Queued material retrieval for itemId: {item->ItemId}");
    }

    private void DequeueGearPiece()
    {
        if (!queuedGearPieces.TryDequeue(out var dequeuedGearPiece))
        {
            return;
        }

        finishedGearPieces.Add(dequeuedGearPiece.ToGearListItemNodeData());
    }

    private static bool DoesInventoryItemSupportMateria(InventoryItem* item)
    {
        var itemSheet = Plugin.DataManager.Excel.GetSheet<Item>();

        return itemSheet.GetRowOrDefault(item->ItemId)?.MateriaSlotCount > 0;
    }

    private void ClearLists()
    {
        queuedGearPieces.Clear();
        finishedGearPieces.Clear();
    }

    private static bool IsCurrentlyRetrievingMateria()
    {
        return Plugin.Condition[ConditionFlag.Occupied39];
    }
}
