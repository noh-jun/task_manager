using System;
using System.Collections.Generic;
using TaskManager.Core;
using TaskManager.View;

namespace TaskManager.Model;

public sealed class HotKeyConfig
{
    public sealed class KeyBindingCheckResult
    {
        public bool IsEditableAction { get; set; } = false;
        public bool HasFixedKeyConflict { get; set; } = false;
        public List<HotKeyAction> DuplicateEditableActions { get; } = new List<HotKeyAction>();
    }

    private static readonly HotKeyGesture[] FixedGestures = new[]
    {
        new HotKeyGesture(ConsoleKey.UpArrow, 0),
        new HotKeyGesture(ConsoleKey.DownArrow, 0),
        new HotKeyGesture(ConsoleKey.Enter, 0),
        new HotKeyGesture(ConsoleKey.Enter, ConsoleModifiers.Control),
        new HotKeyGesture(ConsoleKey.Enter, ConsoleModifiers.Alt),
        new HotKeyGesture(ConsoleKey.D, ConsoleModifiers.Control),
    };

    private readonly Dictionary<HotKeyAction, HotKeyGesture> _keyMap;

    public HotKeyConfig()
    {
        _keyMap = new Dictionary<HotKeyAction, HotKeyGesture>
        {
            { HotKeyAction.MoveUp,     new HotKeyGesture(ConsoleKey.UpArrow, 0) },
            { HotKeyAction.MoveDown,   new HotKeyGesture(ConsoleKey.DownArrow, 0) },
            { HotKeyAction.Select,     new HotKeyGesture(ConsoleKey.Enter, 0) },
            { HotKeyAction.Back,       new HotKeyGesture(ConsoleKey.Enter, ConsoleModifiers.Alt) },
            { HotKeyAction.Exit,       new HotKeyGesture(ConsoleKey.D, ConsoleModifiers.Control) },

            { HotKeyAction.TaskAdd,    new HotKeyGesture(ConsoleKey.A, 0) },
            { HotKeyAction.TaskEdit,   new HotKeyGesture(ConsoleKey.E, 0) },
            { HotKeyAction.TaskDelete, new HotKeyGesture(ConsoleKey.Delete, 0) },
            { HotKeyAction.TaskCopy,   new HotKeyGesture(ConsoleKey.C, 0) },
            { HotKeyAction.ArgAdd,    new HotKeyGesture(ConsoleKey.A, 0) },
            { HotKeyAction.ArgDelete, new HotKeyGesture(ConsoleKey.Delete, 0) },
            { HotKeyAction.PackageAdd,      new HotKeyGesture(ConsoleKey.A, 0) },
            { HotKeyAction.PackageEdit,     new HotKeyGesture(ConsoleKey.E, 0) },
            { HotKeyAction.PackageDelete,   new HotKeyGesture(ConsoleKey.Delete, 0) },
            { HotKeyAction.PackageFavorite, new HotKeyGesture(ConsoleKey.F, 0) },
            { HotKeyAction.LogReload,     new HotKeyGesture(ConsoleKey.R, 0) },
            { HotKeyAction.LogPageUp,     new HotKeyGesture(ConsoleKey.PageUp, 0) },
            { HotKeyAction.LogPageDown,   new HotKeyGesture(ConsoleKey.PageDown, 0) },
            { HotKeyAction.LogHome,       new HotKeyGesture(ConsoleKey.Home, 0) },
            { HotKeyAction.LogEnd,        new HotKeyGesture(ConsoleKey.End, 0) },
        };
    }

    public HotKeyGesture GetGesture(HotKeyAction hotKeyAction)
    {
        if (_keyMap.TryGetValue(hotKeyAction, out HotKeyGesture gesture))
        {
            return gesture;
        }

        throw new ArgumentOutOfRangeException(nameof(hotKeyAction), hotKeyAction, "Hot key action is not configured.");
    }

    public bool IsEditable(HotKeyAction hotKeyAction)
    {
        IReadOnlyList<HotKeyAction> editableActions = HotKeyHelper.GetEditableActions();
        for (int actionIndex = 0; actionIndex < editableActions.Count; actionIndex++)
        {
            if (editableActions[actionIndex] == hotKeyAction)
            {
                return true;
            }
        }

        return false;
    }

    public KeyBindingCheckResult CheckKeyBinding(HotKeyAction targetAction, HotKeyGesture gesture)
    {
        var keyBindingCheckResult = new KeyBindingCheckResult
        {
            IsEditableAction = IsEditable(targetAction),
        };

        if (!keyBindingCheckResult.IsEditableAction)
        {
            return keyBindingCheckResult;
        }

        if (HasFixedKeyConflict(gesture))
        {
            keyBindingCheckResult.HasFixedKeyConflict = true;
            return keyBindingCheckResult;
        }

        IReadOnlyList<HotKeyAction> editableActions = HotKeyHelper.GetEditableActions();
        for (int actionIndex = 0; actionIndex < editableActions.Count; actionIndex++)
        {
            HotKeyAction compareAction = editableActions[actionIndex];
            if (compareAction == targetAction)
            {
                continue;
            }

            if (_keyMap.TryGetValue(compareAction, out HotKeyGesture existingGesture)
                && existingGesture.Equals(gesture))
            {
                keyBindingCheckResult.DuplicateEditableActions.Add(compareAction);
            }
        }

        return keyBindingCheckResult;
    }

    public bool ApplyKeyBinding(HotKeyAction targetAction, HotKeyGesture gesture)
    {
        if (!IsEditable(targetAction))
        {
            return false;
        }

        if (HasFixedKeyConflict(gesture))
        {
            return false;
        }

        HotKeyGesture previous = _keyMap[targetAction];
        _keyMap[targetAction] = gesture;
        AppLog.Write(LogLevel.Lv2, $"[HotKeyConfig] 키 바인딩 변경: {targetAction} {previous} → {gesture}");
        return true;
    }

    public void SetKeyWithoutValidation(HotKeyAction hotKeyAction, HotKeyGesture gesture)
    {
        if (!_keyMap.ContainsKey(hotKeyAction))
        {
            throw new ArgumentOutOfRangeException(nameof(hotKeyAction), hotKeyAction, "Hot key action is not configured.");
        }

        _keyMap[hotKeyAction] = gesture;
    }

    public IReadOnlyDictionary<HotKeyAction, HotKeyGesture> Export()
    {
        return new Dictionary<HotKeyAction, HotKeyGesture>(_keyMap);
    }

    private static bool HasFixedKeyConflict(HotKeyGesture gesture)
    {
        foreach (HotKeyGesture fixedGesture in FixedGestures)
        {
            if (fixedGesture.Equals(gesture))
            {
                return true;
            }
        }

        return false;
    }
}