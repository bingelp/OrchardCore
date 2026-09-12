# Orchard Core

A modular, multi-tenant application framework and CMS built on ASP.NET Core. This glossary pins down terms where everyday words are overloaded in this codebase.

## Language

### Status messages

**Toast**:
A short-lived status message shown to the user after an action (for example "Content item saved"), raised through the notifier and rendered by the active theme.
_Avoid_: Notification, flash message, alert

**Notification**:
A persistent, per-user message stored by the `OrchardCore.Notifications` feature and shown in the bell-icon inbox. Not a toast.
_Avoid_: Toast

**Severity**:
The kind of a toast: Success, Information, Warning or Error. It determines the toast's colour, its announcement urgency and whether it auto-dismisses.
_Avoid_: Type, level, notify type

**Success toast**:
A toast whose severity is Success. It is the only severity that auto-dismisses by default.

**Auto-dismiss**:
A toast removing itself after a delay without the user closing it.
_Avoid_: Auto-hide, timeout, expire

**Success dismissal delay**:
The site-wide number of seconds a success toast stays visible before it auto-dismisses. A value of 0 means success toasts never auto-dismiss.
_Avoid_: Toast timeout, toast duration
