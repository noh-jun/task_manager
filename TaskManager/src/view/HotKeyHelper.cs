using System;
using System.Collections.Generic;
using TaskManager.Model;

namespace TaskManager.View;

public static class HotKeyHelper
{
    public static IReadOnlyList<HotKeyAction> GetEditableActions()
    {
        return new[]
        {
            HotKeyAction.TaskAdd,
            HotKeyAction.TaskEdit,
            HotKeyAction.TaskDelete,
            HotKeyAction.TaskCopy,
            HotKeyAction.ArgAdd,
            HotKeyAction.ArgDelete,
            HotKeyAction.PackageAdd,
            HotKeyAction.PackageEdit,
            HotKeyAction.PackageDelete,
            HotKeyAction.PackageFavorite,
            HotKeyAction.LogReload,
            HotKeyAction.LogPageUp,
            HotKeyAction.LogPageDown,
            HotKeyAction.LogHome,
            HotKeyAction.LogEnd,
        };
    }

    public static IReadOnlyList<HotKeyAction> GetTaskEditableActions()
    {
        return new[]
        {
            HotKeyAction.TaskAdd,
            HotKeyAction.TaskEdit,
            HotKeyAction.TaskDelete,
            HotKeyAction.TaskCopy,
            HotKeyAction.ArgAdd,
            HotKeyAction.ArgDelete,
        };
    }

    public static IReadOnlyList<HotKeyAction> GetPackageEditableActions()
    {
        return new[]
        {
            HotKeyAction.PackageAdd,
            HotKeyAction.PackageEdit,
            HotKeyAction.PackageDelete,
        };
    }

    public static string GetActionDisplayName(HotKeyAction hotKeyAction)
    {
        return hotKeyAction switch
        {
            HotKeyAction.MoveUp => "Move Up",
            HotKeyAction.MoveDown => "Move Down",
            HotKeyAction.Select => "Select",
            HotKeyAction.Back => "Back",
            HotKeyAction.Exit => "Exit",
            HotKeyAction.TaskAdd => "Task Add",
            HotKeyAction.TaskEdit => "Task Edit",
            HotKeyAction.TaskDelete => "Task Delete",
            HotKeyAction.TaskCopy => "Task Copy",
            HotKeyAction.ArgAdd    => "Arg Add",
            HotKeyAction.ArgDelete => "Arg Delete",
            HotKeyAction.PackageAdd => "Package Add",
            HotKeyAction.PackageEdit => "Package Edit",
            HotKeyAction.PackageDelete   => "Package Delete",
            HotKeyAction.PackageFavorite => "Package Favorite",
            HotKeyAction.LogReload   => "Log Reload",
            HotKeyAction.LogPageUp   => "Log Page Up",
            HotKeyAction.LogPageDown => "Log Page Down",
            HotKeyAction.LogHome     => "Log Home",
            HotKeyAction.LogEnd      => "Log End",
            _ => hotKeyAction.ToString(),
        };
    }

    public static bool IsSelectKey(ConsoleKeyInfo keyInfo)
    {
        return keyInfo is { Key: ConsoleKey.Enter, Modifiers: 0 };
    }

    public static bool IsBackKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key != ConsoleKey.Enter)
        {
            return false;
        }

        bool isCtrlEnter = (keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control;
        bool isAltEnter = (keyInfo.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt;

        return isCtrlEnter || isAltEnter;
    }

    public static bool IsExitKey(ConsoleKeyInfo keyInfo)
    {
        return keyInfo.Key == ConsoleKey.D
               && (keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control;
    }

    public static bool IsActionKey(ConsoleKeyInfo keyInfo, HotKeyConfig hotKeyConfig, HotKeyAction hotKeyAction)
    {
        ArgumentNullException.ThrowIfNull(hotKeyConfig);

        HotKeyGesture gesture = hotKeyConfig.GetGesture(hotKeyAction);
        return HotKeyGesture.FromConsoleKeyInfo(keyInfo).Equals(gesture);
    }

    public static string ToDisplayText(HotKeyAction hotKeyAction, HotKeyConfig hotKeyConfig)
    {
        ArgumentNullException.ThrowIfNull(hotKeyConfig);

        return hotKeyConfig.GetGesture(hotKeyAction).ToString();
    }
}