# Luxodd Unity Plugin

Unity plugin for WebGL arcade games on the Luxodd platform. It integrates browser/iframe-embedded WebGL builds with Luxodd backend services through WebSocket commands and browser bridge code.

## Compatibility

- Unity 2022 LTS minimum.
- Compatible with Unity 6+.
- Basic WebGL integration was also tested with Unity 2021.3.45f2; not all SDK flows were validated.
- WebGL arcade game target.
- Some example scenes contain optional render-pipeline components; see Known Limitations.

## Dependencies

Required Unity packages:

- UGUI (`com.unity.ugui`)
- TextMeshPro (`com.unity.textmeshpro`)
- Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`)

Optional Unity package:

- Input System (`com.unity.inputsystem`)

The plugin includes editor checks for missing TextMeshPro and Newtonsoft.Json packages. Input System is optional and is not required for Core runtime support.

After installing the required packages, import the plugin `.unitypackage` into the project. If TMP Essential Resources are not already installed, select **Window → TextMeshPro → Import TMP Essential Resources**. Select `LuxoddTemplate` in the WebGL Player Settings before building.

## Architecture

- `Assets/Luxodd.Game/Runtime/Core/**` — always-available runtime API.
- `Assets/Luxodd.Game/Runtime/InputSystem/**` — optional Input System backend.
- `Assets/Luxodd.Game/Editor/Core/**` — editor tooling without Input System dependency.
- `Assets/Luxodd.Game/Editor/InputSystem/**` — optional Input System editor tooling.

Core does not directly depend on `UnityEngine.InputSystem`. Optional assemblies use asmdef package-presence gating through `versionDefines` and `LUXODD_INPUT_SYSTEM`.

## Input Compatibility

`ArcadeControls` is the recommended arcade input API:

```csharp
Vector2 stick = ArcadeControls.GetJoystick();
bool isPressed = ArcadeControls.GetButton(ArcadeButtonColor.Red);
bool pressedThisFrame = ArcadeControls.GetButtonDown(ArcadeButtonColor.Black);
bool releasedThisFrame = ArcadeControls.GetButtonUp(ArcadeButtonColor.Green);
```

- With Legacy Input Manager available, Core uses guarded Legacy Input fallback paths.
- With Active Input Handling set to **Input System Package** only, Legacy fallback paths return neutral values (`Vector2.zero`, `0f`, or `false`) instead of throwing.
- Active Input Handling = **Both** is not required merely to avoid Legacy Input exceptions.
- When `com.unity.inputsystem` is installed, the optional backend registers automatically and provides Input System device support. Configure `ArcadeInputMappingConfig` through the supplied setup/config components for hardware-specific mappings.

Standard development gamepads support both the left analog stick and D-pad through `ArcadeControls`. D-pad input has priority while held; releasing it immediately returns joystick input to the left stick, which retains its analog magnitude for development and testing.

Current Luxodd arcade joystick hardware reports digital directions (typically `0` or `1`) and does not provide analog pressure, while the public API remains `Vector2`-compatible for future analog hardware. Use the Control Test example as the recommended local input-validation entry point. Gameplay should continue to use `ArcadeControls` rather than direct Gamepad or Input System APIs.

## EventSystem Input Module

`EventSystemInputModuleSwitcher` corrects the EventSystem input module in Editor and at runtime:

- Uses `InputSystemUIInputModule` when the Input System UI module is available.
- Uses `StandaloneInputModule` otherwise.
- Editor correction is delayed outside `OnValidate`; it does not destroy components directly during `OnValidate`.

## prize_won Forwarding and Logging

For WebGL `level_end` responses:

- Every non-empty `prize_tickets` response is forwarded to the frontend as `prize_won`.
- Repeated identical non-empty payloads are forwarded and are not suppressed.
- Null or empty prize ticket responses are ignored.
- Full prize ticket JSON is not logged.
- Selected backend command handlers log safe parsing/forwarding metadata instead of full payload contents.

This does not imply that all transport, token, URL, or session logging has been hardened in this release.

## Runtime Environment and Arcade Layout

Use `Luxodd.Game.Scripts.Runtime.Viewport.Context.LuxoddRuntimeContext` for runtime metadata
provided by the accepted host session:

| API | Meaning |
| --- | --- |
| `Platform` | `LuxoddRuntimePlatform.Unknown`, `Web`, or `Arcade`. |
| `ArcadeType` | `LuxoddArcadeType.Unknown`, `Pawn`, or `Rook`; always Unknown outside Arcade. |
| `HasHostRuntimeContext` | The accepted host session contained a runtime object, including unsupported values. |
| `IsWeb`, `IsArcade` | Derived from Platform. |
| `IsPawn`, `IsRook` | True only for the matching Arcade layout. |
| `IsMobileRuntime` | Existing mobile/browser input detection, independent of host platform and cabinet layout. |
| `RuntimeContextChanged` | `Action` event raised when Platform, ArcadeType, or HasHostRuntimeContext changes. |

Legacy sessions without runtime metadata still connect normally and report Unknown / Unknown
with `HasHostRuntimeContext == false`. An unsupported platform reports Unknown; an unsupported
Arcade layout reports Arcade / Unknown. A supplied runtime object still sets
`HasHostRuntimeContext == true`. Editor and non-WebGL runtimes default to Unknown unless
authoritative host metadata is explicitly supplied.

Do not detect Rook from screen resolution or portrait orientation. Use LuxoddRuntimeContext.IsRook.

```csharp
using Luxodd.Game.Scripts.Runtime.Viewport.Context;

private void OnEnable()
{
    LuxoddRuntimeContext.RuntimeContextChanged += ApplyRuntimeLayout;
    ApplyRuntimeLayout();
}

private void OnDisable()
{
    LuxoddRuntimeContext.RuntimeContextChanged -= ApplyRuntimeLayout;
}

private void ApplyRuntimeLayout()
{
    if (LuxoddRuntimeContext.IsRook)
        ApplyRookLayout();
    else if (LuxoddRuntimeContext.IsWeb && LuxoddRuntimeContext.IsMobileRuntime)
        ApplyMobileLayout();
    else
        ApplyStandardLayout();
}
```

The layout methods above are game-owned UI methods. Explicit Pawn/Rook switching is also
available through `ArcadeType`. Query actual viewport geometry through the existing viewport
APIs; host metadata does not describe screen size, orientation, or safe areas. Continue using
viewport events for resizing and orientation changes.

Runtime context follows accepted session ownership. During Fast Restart, the last accepted
context remains available while the replacement is pending. Early replacement metadata is
applied only when that session is accepted after Restart confirmation. Token/URL changes with
the same effective context do not raise the event. Duplicate credentials do not update metadata;
runtime metadata is not part of session identity. Subscriber exceptions are isolated so one
callback cannot interrupt session acceptance or prevent other subscribers from being notified.

## Examples and Local Resources

The plugin includes input, arcade button, joystick, mobile, and gameplay examples. Example font and TMP resources are contained under `Assets/Luxodd.Game`; package export does not require root `Assets/Fonts` or root `Assets/TextMesh Pro` folders.

## WebGL Template

`Assets/WebGLTemplates/LuxoddTemplate` is required release content for browser, iframe, and platform bridge integration.

`Assets/WebGLTemplates` is a reserved Unity folder name. Do not rename it, move it under `Assets/Luxodd.Game`, or nest/restructure the template.

Select `LuxoddTemplate` in Unity WebGL Player Settings before building.

The WebGL bridge buffers messages while the browser WebSocket is CONNECTING and flushes them after the socket opens. This prevents early-session WebSocket send race errors.

## Exporting the Plugin

Export exactly these roots:

- `Assets/Luxodd.Game/**`
- `Assets/WebGLTemplates/LuxoddTemplate/**`

Exclude project-specific content, including:

- `Assets/Fonts/**`
- `Assets/TextMesh Pro/**`
- `Assets/Scenes/**`
- `Assets/Settings/**`
- `Packages/**`
- `ProjectSettings/**`
- `UserSettings/**`
- `.agent/**`, `.codex/**`, and `docs/**`
- IDE folders and root zip/export artifacts

Use **Luxodd → Release → Export Unity Package...** in the development project. The exporter recursively selects these roots and saves outside the project. It blocks export while a scene has unsaved changes and may normalize the release network settings asset before export.

## Upgrading to a Newer Version

When upgrading to a newer plugin version, use a clean reinstall instead of importing over an older version:

1. Back up the Unity project.
2. Remove the old `Assets/Luxodd.Game` folder.
3. Back up customized `Assets/WebGLTemplates/LuxoddTemplate` if applicable.
4. Import the new package.
5. Replace or merge `Assets/WebGLTemplates/LuxoddTemplate` carefully.
6. Let Unity recompile and check scenes/prefabs for missing script warnings.

This avoids stale files from earlier layouts causing duplicate classes, duplicate assemblies, or mixed old/new plugin structure. Do not bulk-delete `Assets/Luxodd.Game/Scripts/**` after import; valid plugin files may remain there.

## Known Limitations

- Cross-scene SDK persistence is not guaranteed.
- Some example scenes may require optional Unity packages or cleanup of serialized components.
- Host/browser Metadata integration has not been fully validated.
- Secure score signing/encryption is not included in the current public plugin release.
- Final clean-project validation in Unity 2022 LTS and Unity 6+, plus final WebGL browser validation, is required before release publication.
