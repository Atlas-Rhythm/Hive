# Upload Configuration

## Sample configuration

```json
"Uploads": {
    "MaxFileSize": 32768
}
```

## `MaxFileSize` - `long`

The maximum file size in bytes. It must also be > 0, otherwise, it will be set to: `32 * 1024 * 1024` on default.
This configuration option must also be present.
