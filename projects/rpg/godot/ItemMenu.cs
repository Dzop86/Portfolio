using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// An item's right click (sprint 117, D77): a small menu at the mouse with what <see cref="ItemInfo.Actions"/> offers,
/// and the card shown when the mouse rests on any item, with a word about that menu.
/// </summary>
public static class ItemMenu
{
    /// <summary>The card of an item under the mouse, everywhere: the same lines, then "Right click: more options".</summary>
    public static string Tooltip(Item item, Texts texts) =>
        (texts ?? throw new ArgumentNullException(nameof(texts))).ItemCard(item) + "\n" + texts["item.hint"];

    /// <summary>The menu of these actions, open at <paramref name="at"/>; <paramref name="chosen"/> gets the one clicked.</summary>
    public static PopupMenu Open(Node parent, IReadOnlyList<ItemAction> actions, Texts texts, Vector2 at, Action<ItemAction> chosen)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(actions);
        var menu = new PopupMenu();
        foreach (ItemAction action in actions)
            menu.AddItem(texts["item." + action], (int)action);
        menu.IdPressed += id => chosen((ItemAction)(int)id);
        menu.PopupHide += () => menu.QueueFree();
        parent.AddChild(menu);
        menu.Popup(new Rect2I((Vector2I)at, Vector2I.Zero));
        return menu;
    }
}
