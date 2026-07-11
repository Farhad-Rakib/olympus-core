# Palette API

This document describes the palette endpoints available on the Site Settings controller.

## Endpoints

- GET /api/v1/sitesettings/palette/{key}
  - Permission: `site-settings.read`
  - Returns: the palette value parsed as an object (dictionary) when possible, otherwise the raw string.
  - Response example:

```json
{
  "success": true,
  "message": "Palette retrieved",
  "data": {
    "primary": "#0d6efd",
    "secondary": "#6c757d",
    "background": "#ffffff",
    "text": "#212529"
  }
}
```

- POST /api/v1/sitesettings/palette
  - Permission: `site-settings.create`
  - Body: `ColorPaletteDto` (example below)
  - Validates that the palette JSON is well-formed (a dictionary of string→string). If invalid, returns 400.

Example request body (`ColorPaletteDto`):

```json
{
  "id": 0,
  "key": "Palette.Default",
  "colors": {
    "primary": "#0d6efd",
    "secondary": "#6c757d",
    "background": "#ffffff",
    "text": "#212529"
  }
}
```

- DELETE /api/v1/sitesettings/palette/{id}
  - Permission: `site-settings.delete`
  - Deletes a palette site setting by id.

## Notes
- Palettes are stored in the `SiteSetting.Value` column as JSON strings. The service validates palette JSON on save for keys containing `palette` (case-insensitive).
- The keys used by the default seeder are `Palette.Default` and `Palette.Dark` (see Docs/PALETTE_SEED.md).

```
// To read palettes in the UI, call GET /api/v1/sitesettings/palette/Palette.Default
```
