using System;
using System.Collections.Generic;
using System.Text;
using TaskManager.Core.Navigation;
using TaskManager.Infra.HotKeys;
using TaskManager.Model;
using TaskManager.View;

namespace TaskManager.View.Options;

public sealed class HotKeyCaptureView : IView
{
    private enum CaptureState
    {
        Capture,
        ConfirmApply,
        ConfirmDuplicate,
    }

    private readonly IViewNavigator _viewNavigator;
    private readonly HotKeyConfig _hotKeyConfig;
    private readonly HotKeyConfigStore _hotKeyConfigStore;
    private readonly HotKeyCaptureSession _captureSession;

    private CaptureState _state;
    private string _statusMessage = string.Empty;
    private HotKeyGesture _pendingGesture;
    private HotKeyGesture? _capturedGesture;

    public HotKeyCaptureView(
        IViewNavigator viewNavigator,
        HotKeyConfig hotKeyConfig,
        HotKeyConfigStore hotKeyConfigStore,
        HotKeyCaptureSession captureSession)
    {
        _viewNavigator = viewNavigator ?? throw new ArgumentNullException(nameof(viewNavigator));
        _hotKeyConfig = hotKeyConfig ?? throw new ArgumentNullException(nameof(hotKeyConfig));
        _hotKeyConfigStore = hotKeyConfigStore ?? throw new ArgumentNullException(nameof(hotKeyConfigStore));
        _captureSession = captureSession ?? throw new ArgumentNullException(nameof(captureSession));
    }

    public ViewId ViewId => ViewId.HotKeyCapture;

    public event Action? InvalidateRequested;

    public void OnEnter()
    {
        _state = CaptureState.Capture;
        _statusMessage = $"Press a new key for '{HotKeyHelper.GetActionDisplayName(_captureSession.TargetAction)}'.";
        _pendingGesture  = default;
        _capturedGesture = null;
        InvalidateRequested?.Invoke();
    }

    public void OnLeave()
    {
    }

    public void Render()
    {
        string actionName = HotKeyHelper.GetActionDisplayName(_captureSession.TargetAction);
        string currentKey = _hotKeyConfig.GetGesture(_captureSession.TargetAction).ToString();

        // Title
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Main Menu  ›  Options  ›  HotKey Edit  ›  Capture");
        Console.ResetColor();
        ConsoleColor titleColor = _state == CaptureState.ConfirmDuplicate
            ? ConsoleColor.Yellow
            : ConsoleColor.Cyan;
        Console.ForegroundColor = titleColor;
        Console.WriteLine("  ┌──────────────────────────┐");
        Console.WriteLine("  │      Key Capture         │");
        Console.WriteLine("  └──────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        // Hotkey hints
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(_state == CaptureState.Capture
            ? "  Press a new key    Alt+Enter Cancel"
            : "  Y Confirm    N Cancel    Alt+Enter Back");
        Console.ResetColor();
        Console.WriteLine();

        // Action info
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Action   ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(actionName);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Current  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(currentKey);
        Console.ResetColor();

        // 캡처된 키 상세 표시
        HotKeyGesture? displayGesture = _state == CaptureState.Capture
            ? _capturedGesture
            : _pendingGesture;

        if (displayGesture.HasValue)
        {
            HotKeyGesture g = displayGesture.Value;
            ConsoleColor  infoColor = _state == CaptureState.ConfirmDuplicate
                ? ConsoleColor.Yellow
                : ConsoleColor.Cyan;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Key       ");
            Console.ForegroundColor = infoColor;
            Console.WriteLine(g.Key == 0 ? "None" : g.Key.ToString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Char      ");
            Console.ForegroundColor = infoColor;
            Console.WriteLine($"'{g.FormatKeyChar()}'");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Modifiers ");
            Console.ForegroundColor = infoColor;
            Console.WriteLine(g.FormatModifiersString());
            Console.ResetColor();
        }

        // Status at bottom
        int currentRow = Console.CursorTop;
        int windowHeight = Math.Max(Console.WindowHeight, currentRow + 6);
        int paddingLines = windowHeight - currentRow - 5;
        for (int i = 0; i < paddingLines; i++)
            Console.WriteLine();

        ConsoleColor statusColor = _state == CaptureState.ConfirmDuplicate
            ? ConsoleColor.Yellow
            : ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ForegroundColor = statusColor;
        Console.WriteLine($"  {_statusMessage}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ──────────────────────────────────────");
        Console.ResetColor();
    }

    public bool HandleKey(ConsoleKeyInfo keyInfo)
    {
        return _state switch
        {
            CaptureState.Capture          => HandleCaptureKey(keyInfo),
            CaptureState.ConfirmApply     => HandleConfirmKey(keyInfo),
            CaptureState.ConfirmDuplicate => HandleConfirmKey(keyInfo),
            _                             => false,
        };
    }

    private bool HandleCaptureKey(ConsoleKeyInfo keyInfo)
    {
        HotKeyGesture gesture = HotKeyGesture.FromConsoleKeyInfo(keyInfo);
        _capturedGesture = gesture;
        HotKeyAction targetAction = _captureSession.TargetAction;

        HotKeyConfig.KeyBindingCheckResult result =
            _hotKeyConfig.CheckKeyBinding(targetAction, gesture);

        if (!result.IsEditableAction)
        {
            _statusMessage = "Selected action is not editable.";
            InvalidateRequested?.Invoke();
            return true;
        }

        if (result.HasFixedKeyConflict)
        {
            _statusMessage = $"'{gesture}' is a fixed key. It cannot be assigned.";
            InvalidateRequested?.Invoke();
            return true;
        }

        if (result.DuplicateEditableActions.Count > 0)
        {
            _pendingGesture = gesture;
            _state          = CaptureState.ConfirmDuplicate;
            _statusMessage  = $"'{gesture}' is already used by {BuildDuplicateText(result.DuplicateEditableActions)}. Apply anyway? (Y/N)";
            InvalidateRequested?.Invoke();
            return true;
        }

        _pendingGesture = gesture;
        _state          = CaptureState.ConfirmApply;
        _statusMessage  = $"Apply '{gesture}' to {HotKeyHelper.GetActionDisplayName(targetAction)}? (Y/N)";
        InvalidateRequested?.Invoke();
        return true;
    }

    private bool HandleConfirmKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Modifiers != 0)
            return true;

        if (keyInfo.Key == ConsoleKey.Y)
        {
            ApplyAndSave(_captureSession.TargetAction, _pendingGesture);
            return true;
        }

        if (keyInfo.Key == ConsoleKey.N)
        {
            _state           = CaptureState.Capture;
            _capturedGesture = null;
            _pendingGesture  = default;
            _statusMessage   = $"Press a new key for '{HotKeyHelper.GetActionDisplayName(_captureSession.TargetAction)}'.";
            InvalidateRequested?.Invoke();
            return true;
        }

        return true;
    }

    private void ApplyAndSave(HotKeyAction action, HotKeyGesture gesture)
    {
        bool isApplied = _hotKeyConfig.ApplyKeyBinding(action, gesture);

        if (!isApplied)
        {
            _captureSession.SetLastResultMessage("Failed to apply key binding.");
        }
        else
        {
            _hotKeyConfigStore.Save(_hotKeyConfig);
            _captureSession.SetLastResultMessage(
                $"{HotKeyHelper.GetActionDisplayName(action)} changed to {gesture}.");
        }

        _viewNavigator.Pop();
    }

    private static string BuildDuplicateText(IReadOnlyList<HotKeyAction> actions)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < actions.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(HotKeyHelper.GetActionDisplayName(actions[i]));
        }
        return sb.ToString();
    }
}
