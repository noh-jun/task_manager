# TaskManager TUI sample structure

## Top-level directories

- `core`: common flow, navigator, factory, app-level coordination
- `infra`: file persistence and external resource implementations
- `model`: domain/config data
- `view`: tui screens and shared view base classes

## Namespace rule

- Directory name is lower case on disk.
- Namespace follows the directory path, with the first letter capitalized.
- Example:
  - `core/navigation/ViewNavigator.cs`
  - namespace: `TaskManager.Core.Navigation`

## Notes

- `ConsoleView` is the entry screen.
- `ViewNavigator` uses a stack.
- `ResetPath()` supports jump navigation by reusing the common prefix of the current path and the target path.
- `TaskEditorView` keeps the current behavior style and hotkey-based input.
