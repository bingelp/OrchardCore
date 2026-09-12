# Toast Accessibility — Current-State Catalog

Issue: #19780 (toast accessibility)
Scope: How toast notifications work in Orchard Core today, and whether their markup meets the ARIA/WCAG guidance in [resources.md](resources.md). This is a description of current state only. It does not propose a solution.

## TL;DR

- There are **three separate toast implementations**:
  1. The **core notifier** (`INotifier` → Bootstrap toast). It is used for nearly all flash messages (≈180 `SuccessAsync` call sites).
  2. A **one-off Bootstrap toast** on the Data Localization admin page.
  3. **Custom Vue toasts** in the Media module (media gallery and media picker).
- **Core notifier:** by default only **Success** auto-dismisses, after a hard-coded **5000 ms**. Information, Warning and Error stay until the user clicks the close button. The timeout can only be changed per call in C#. There is no site setting, options class or appsettings key. No call site in the repo overrides the default.
- **ARIA:** every core toast uses `role="alert"` + `aria-live="assertive"`, **even for success and info messages**. The live region is not in the DOM before its content. The Media Vue toasts have **no live-region semantics at all**, and some of their icon-only buttons have no accessible name.

---

## 1. What types of toasts are there?

### 1.1 Core notifier (primary system)

**Server API:** `src/OrchardCore/OrchardCore.DisplayManagement/Notify/`

| File | Purpose |
|---|---|
| `INotifier.cs` | `AddAsync(NotifyType, LocalizedHtmlString[, NotifyContext])`, `List()` |
| `NotifyEntry.cs` | `enum NotifyType { Success, Information, Warning, Error }`; entry holds `Type`, `Message`, `DismissalMilliseconds` |
| `NotifyContext.cs` | `int? DismissalMilliseconds`. The doc comment says: *"Keep null to not auto dismiss."* |
| `NotifierExtensions.cs` | `SuccessAsync` / `InformationAsync` / `WarningAsync` / `ErrorAsync`, each with an optional `int dismissalMilliseconds` overload |
| `NotifyFilter.cs` | Global MVC filter. Carries entries across redirects in a data-protected `orch_notify` cookie. When a view/page renders, it adds a `NotifyMessages` shape to the layout's `Messages` zone. It does this **only if there are entries** (lines 143-146). |

The Workflows `NotifyTask` activity (`OrchardCore.Workflows/Activities/NotifyTask.cs`) also raises these toasts, with any `NotifyType`.

**Severity → visual mapping** (`Message.cshtml`):

| `NotifyType` | CSS classes | Close button |
|---|---|---|
| Success | `message-success text-bg-success` | `btn-close btn-close-white` |
| Information | `message-information text-bg-info` | `btn-close` |
| Warning | `message-warning text-bg-warning` | `btn-close` |
| Error | `message-error text-bg-danger` | `btn-close btn-close-white` |

**Rendering by theme:**

| Theme / layout | Views used | Result |
|---|---|---|
| TheAdmin (`Layout`, `Layout-Login`, `Layout-TwoFactor`) | `TheAdmin/Views/NotifyMessages.cshtml`, `Message.cshtml` | Bootstrap 5.3.8 toast, fixed top-right |
| TheTheme | `TheTheme/Views/NotifyMessages.cshtml`, `Message.cshtml` (Message is identical to TheAdmin's; NotifyMessages differs only in its CSS `top` offset) | Same as TheAdmin |
| TheAgencyTheme, TheBlogTheme (Liquid) | No override. Falls back to the C# shapes in `OrchardCore.DisplayManagement/Shapes/CoreShapes.cs:100-132` | Plain `<div class="message message-{type}" role="alert">`. **No close button, no auto-dismiss, no Bootstrap JS.** Stays inline until navigation. |
| SafeMode | Layout has no `Messages` zone | Notifications are not shown |

Core toast markup (`src/OrchardCore.Themes/TheAdmin/Views/Message.cshtml:15-27`):

```html
<div class="toast border-0 shadow message-@type text-bg-@bsClassName"
     role="alert"
     aria-live="assertive"
     aria-atomic="true"
     data-bs-autohide="@autoHide"
     data-bs-delay="@(autoHide ? milliseconds : 5000)">
    <div class="d-flex">
        <div class="toast-body">@Model.Message</div>
        <button type="button" class="... me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>
</div>
```

Container and activation script (`TheAdmin/Views/NotifyMessages.cshtml`):

```html
<div class="toast-container position-fixed p-3 oc-notify-toast-container">
  ...one Message shape per entry...
</div>
<script at="Foot">
  document.addEventListener('DOMContentLoaded', () => {
    if (!window.bootstrap?.Toast) { return; }
    for (const toastElement of document.querySelectorAll('.oc-notify-toast-container .toast')) {
      const toast = bootstrap.Toast.getOrCreateInstance(toastElement);
      toast.show();
      toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove(), { once: true });
    }
  });
</script>
```

### 1.2 Data Localization ad-hoc toast

`src/OrchardCore.Modules/OrchardCore.DataLocalization/Views/Admin/Index.cshtml:379-406`. The page builds a client-side `showNotification(message, type)` with its own HTML template. It is called with `'success'` (translations saved) and `'danger'` (save/load failures). If no `.toast-container` exists, it creates one and appends it to `<body>`.

### 1.3 Media module Vue toasts (not Bootstrap)

| Component | Used for | Location (`src/OrchardCore.Modules/OrchardCore.Media/Assets/`) |
|---|---|---|
| `NotificationToast.vue` | Success, Info, Warn and Error messages from media-gallery operations (move, copy, delete, unauthorized, errors). Fed by the `notify()` event bus in `.scripts/bloom/services/notifications/notifier.ts`. | `media-gallery/src/components/` |
| `UploadToast.vue` | Upload progress panel (bottom-right) in the media gallery | `media-gallery/src/components/` |
| `UploadList.vue` + `FieldUploadService.ts` | Upload progress list on attached media fields | `media-picker/src/` |

`notifiersClose()` in `notifier.ts` has no call sites. It is dead code.

### 1.4 Not toasts (out of scope)

Static inline `.alert` banners (for example, empty-state messages), the Layers "Widget moved, Undo" alert (`OrchardCore.Layers/Views/Admin/Index.cshtml:20`) and `ValidationSummary` output. None of these auto-dismiss. The `OrchardCore.Notifications` module is a separate persistent, bell-icon notification system and is not a toast.

---

## 2. How does auto-dismiss work?

### Core notifier

1. **Server:** `NotifierExtensions.SuccessAsync(message)` passes `new NotifyContext { DismissalMilliseconds = 5000 }` (`NotifierExtensions.cs:71`). The other three plain overloads pass no context, so the value is `null`.
2. **Render:** `Message.cshtml` sets `autoHide = milliseconds is > 0`. It emits `data-bs-autohide="true|false"` and `data-bs-delay`. The delay falls back to `5000` when auto-hide is off, but Bootstrap ignores it in that case.
3. **Client:** on `DOMContentLoaded`, `NotifyMessages.cshtml` calls `bootstrap.Toast.getOrCreateInstance(el).show()`. Bootstrap reads the data attributes and schedules `hide()` after `delay`.
4. **Pause behavior:** Bootstrap's Toast clears the timer on `mouseover`/`focusin` and restarts it on `mouseout`/`focusout`. This is built into `bootstrap.bundle.js` (`_onInteraction`). The timer restarts from the full delay; it does not resume where it stopped.
5. **Cleanup:** on `hidden.bs.toast`, the element is **removed from the DOM**. There is no history, so a dismissed message cannot be recovered.

### Data Localization toast

`new bootstrap.Toast(el, { delay: 3000 })` with Bootstrap's default `autohide: true`. **Both success and error toasts auto-dismiss after 3000 ms.** Hover/focus pause applies because this is a Bootstrap toast.

### Media Vue toasts

- `NotificationToast.vue:74-75`: `setTimeout(() => dismiss(id), severity === Error ? 8000 : 5000)`. **All severities auto-dismiss**: errors after 8 s, everything else after 5 s.
- `UploadToast.vue:135` and `FieldUploadService.ts:254`: a successful upload row is removed after 3000 ms.
- These use plain `setTimeout`, so **hover and focus do not pause them**.

---

## 3. Are there toasts that don't auto-dismiss? How are they closed?

| Toast | Auto-dismisses? | How it is closed |
|---|---|---|
| Core: Information / Warning / Error (default) | No (`data-bs-autohide="false"`) | Close button (`data-bs-dismiss="toast"`), or navigating away. The cookie is deleted once the toasts render, so they do not reappear. **No Escape-key handling:** Bootstrap Toast registers no keydown listener. |
| Core: any type rendered by the Liquid themes (CoreShapes fallback) | No | No close control. It disappears only on navigation. |
| Media: failed upload rows (`UploadToast`, `UploadList`) | No | Per-row × button, "Clear errors", or dismiss-all × (shown once no uploads are pending) |
| Media `NotificationToast` | Always auto-dismisses | Can also be closed early with its × button |
| Data Localization toast | Always auto-dismisses | Can also be closed early with its close button |

Keyboard reachability: the core close button is a real `<button>`, but the toast is `position-fixed` and injected near the top of the content area. Nothing moves focus to it, so a keyboard user must tab through the page to reach it. This is appropriate for status messages, which should not take focus, but it makes closing slow for keyboard users.

---

## 4. How is the auto-dismiss timeout configured?

| Implementation | Where the value lives | Configurable? |
|---|---|---|
| Core notifier, Success | Hard-coded `5000` in `NotifierExtensions.cs:71` | Only per call, in C#: `SuccessAsync(message, ms)`. **No call site in the repo uses the overload** (searched all `*.cs`). |
| Core notifier, other types | `null` by default | Per call: `InformationAsync/WarningAsync/ErrorAsync(message, ms)` would opt that toast *into* auto-dismiss. Unused today. |
| Core fallback in `Message.cshtml` | `data-bs-delay` falls back to `5000` | Only matters if auto-hide is on, which requires `ms > 0`, so the fallback is effectively dead |
| Data Localization | `{ delay: 3000 }` inline JS | No |
| Media `NotificationToast` | `8000` / `5000` inline TS | No |
| Media upload rows | `3000` inline TS | No |

No site setting, `IOptions<>` class, appsettings key, recipe step, data attribute override or CSS variable controls toast duration. There is also **no user-level preference** to extend or disable the timer.

---

## 5. Do the current toasts have appropriate ARIA markup?

### Reference guidance (from resources.md)

- **WCAG 2.2 SC 4.1.3 Status Messages (AA):** status messages must be programmatically determinable through role or properties, so assistive technology can announce them without moving focus. Technique ARIA22 uses `role="status"` for success/result messages. ARIA19 uses `role="alert"` for errors and urgent information. Failure F103 covers messages with no such semantics.
- **aria-live (MDN):**
  - `polite` waits for a pause and suits most updates. `assertive` interrupts and "should be used sparingly".
  - `role="status"` implies `aria-live="polite"` and `aria-atomic="true"`. `role="alert"` implies `aria-live="assertive"` and `aria-atomic="true"`.
  - The live region should **exist in the DOM before** content is injected. Adding a region and its content at the same time is often not announced.
- **Related:**
  - SC 2.2.1 Timing Adjustable (A): time limits must be adjustable, extendable or removable.
  - SC 2.2.4 Interruptions (AAA): interruptions can be postponed or suppressed.
  - A success toast that removes itself after 5 s, with no way to extend it, is the concern raised in #19780.

### 5.1 Core notifier (TheAdmin / TheTheme)

| Check | Current | Assessment |
|---|---|---|
| Has live-region semantics | `role="alert"` + `aria-live="assertive"` + `aria-atomic="true"` on every toast | ✅ Meets the minimum of 4.1.3 (messages are programmatically determinable) |
| Role matches severity | **`alert`/`assertive` for all four types** | ⚠️ Success and Information should be `role="status"` / `polite` (ARIA22). Assertive success messages interrupt the user and overuse assertive. Assertive is reasonable for Error, and arguably for Warning. |
| Redundant attributes | Explicit `aria-live="assertive"` and `aria-atomic="true"` duplicate the implicit values of `role="alert"` | ℹ️ Harmless. Bootstrap's docs recommend it for older assistive technology. It would need to change together with the role. |
| Live region exists before content | No. The container is rendered only when entries exist (`NotifyFilter.cs:143`), and each toast is its own region, present in the initial HTML at `display:none` until `show()` | ⚠️ Announcement depends on the screen reader and browser. Content already in the page at load is not a "change". Some screen readers announce `role="alert"` on load, others don't. **Needs testing with NVDA, JAWS and VoiceOver.** |
| Container semantics | `.toast-container` has no role or label | ℹ️ Not required if the toasts carry roles. It is the natural place for a persistent region if item 4 above is addressed. |
| Close button name | `aria-label="Close"`, **hard-coded English** | ⚠️ Not localized (`T["Close"]` is not used), so non-English admins hear English |
| Focus | Not moved to the toast; the close button is tabbable | ✅ Correct for status messages |
| Timing | Success removes itself after 5 s; the timer pauses only on hover or focus *inside* the toast | ⚠️ A screen reader or low-vision user may miss the message, and there is no way to extend or disable the timer (SC 2.2.1 concern). Removed toasts leave no record. |
| Colour-only severity | Severity is shown only by background colour; no icon or visually hidden prefix such as "Error:" | ℹ️ Mostly covered by message wording. Worth noting under SC 1.4.1. |

### 5.2 Core fallback for Liquid themes (CoreShapes)

`<div class="message message-{type}" role="alert">`. It has a live role, uses `alert` for every severity (same concern as 5.1), and is present at page load (same announcement caveat). It does not auto-dismiss, so it raises no timing issue.

### 5.3 Data Localization toast

- Same markup as core: `role="alert" aria-live="assertive" aria-atomic="true"`, and `alert` is used for the success message too. The close button's `aria-label="Close"` is not localized.
- ⚠️ The first time it runs it **creates the container and the toast together**, so the "region must exist first" guidance is not followed. The toast element is itself a new region added along with its content.
- ⚠️ **Error messages auto-dismiss after 3 s.**
- Side note (not ARIA): `message`, which can be `error.message`, is interpolated into HTML via `insertAdjacentHTML` without encoding.

### 5.4 Media module Vue toasts

| Check | Current | Assessment |
|---|---|---|
| Live-region semantics | **None.** No `role` or `aria-live` on `.notification-toast-container`, the items, or the upload panels | ❌ **Fails SC 4.1.3 (F103).** Screen reader users are not told about success, warning or error results in the media gallery, or about upload completion or failure. |
| Close / action button names | `NotificationToast.vue:21` close button is icon-only with **no `aria-label` or `title`**. The copy button has only `title`. `UploadToast.vue` dismiss-all and expand/collapse buttons are icon-only with no name. `UploadList.vue` hides its icons with `aria-hidden="true"`, but its expand/collapse, dismiss-all and per-row dismiss buttons have no text, `title` or `aria-label`, so they have **no accessible name at all**. Only "clear errors" has a `title`. | ❌ Buttons have no accessible name (SC 4.1.2) |
| Timing | Errors vanish after 8 s and others after 5 s; no hover or focus pause | ⚠️ Stricter than core: errors auto-dismiss, and the timer cannot be paused |

---

## Summary of gaps (input for the next spec)

1. Success and Information toasts use `role="alert"`/`assertive` instead of `role="status"`/`polite` (core, CoreShapes fallback, Data Localization).
2. Live regions are not in the DOM before their content (core container is conditional; Data Localization creates container and toast together).
3. Auto-dismiss durations are hard-coded, have no global or user setting, and cannot be extended. Success is 5 s (core), all messages are 3 s (Data Localization), and 5 s/8 s (Media) includes errors.
4. Media Vue toasts have no live-region semantics and have unnamed icon buttons.
5. Close button labels are not localized.
6. Dismissed toasts are removed and cannot be reviewed.
7. Liquid themes' fallback toasts cannot be dismissed.
8. Three separate implementations. Any fix to core alone leaves Data Localization and Media unchanged.
