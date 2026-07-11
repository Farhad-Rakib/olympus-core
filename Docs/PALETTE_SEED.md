# Palette Seeder

Default palettes are added by `DefaultSiteSettingsSeeder` at application bootstrap.

Current default keys added by the seeder:

- `Palette.Default` — a light palette
- `Palette.Dark` — a dark palette

Example entry in `DefaultSiteSettingsSeeder.cs`:

```csharp
new SiteSetting {
  Key = "Palette.Default",
  Value = "{\"primary\":\"#0d6efd\",\"secondary\":\"#6c757d\",\"background\":\"#ffffff\",\"text\":\"#212529\"}",
  Description = "Default color palette"
}
```

How to add a new palette:

1. Add a new `SiteSetting` object to `DefaultSiteSettingsSeeder` with a unique key (e.g., `Palette.Brand`).
2. Use a JSON object mapping token names to color hex values for `Value`.
3. The seeder runs idempotently; it will not overwrite an existing key.

Alternatively, create/update palettes at runtime via the API (see Docs/PALETTE_API.md).
