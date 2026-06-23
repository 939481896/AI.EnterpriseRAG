# Module Copy Playbook

This folder provides copy-first templates for enterprise feature expansion.

## Goal

Enable "copy an existing module, then do local edits" with low risk and consistent output.

## Recommended Flow

1. Copy [src/pages/Templates/CrudModulePageTemplate.tsx](src/pages/Templates/CrudModulePageTemplate.tsx) to your new page.
2. Copy [src/hooks/Templates/useCrudModuleTemplate.ts](src/hooks/Templates/useCrudModuleTemplate.ts) to your domain hook file.
3. Add domain query keys in [src/config/queryKeys.ts](src/config/queryKeys.ts).
4. Replace hard-coded labels with entries in [src/config/uiText.ts](src/config/uiText.ts).
5. Keep mutation `meta.silentError: true` if local notification is used.
6. Register route in [src/App.tsx](src/App.tsx) with lazy import.

## Completion Checklist

- Query keys centralized
- Localized text keys added
- Notification strategy consistent
- Lint and build pass
