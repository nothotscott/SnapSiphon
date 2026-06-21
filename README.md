# SnapSiphon

A desktop app that processes your Snapchat "My Data" export — fixing timestamps, embedding GPS locations into your photos and videos, and organizing everything into a clean folder structure you can drop onto a NAS or any storage solution.

Snapchat is requiring a subscription to keep your Memories. SnapSiphon lets you take them with you.

![Avalonia](https://img.shields.io/badge/Avalonia-12.0-blue) ![.NET](https://img.shields.io/badge/.NET-10-purple) ![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

## What it does

- Extracts and organizes Memories, Chat Media, and Shared Stories into a single output folder
- Embeds GPS coordinates from Snapchat's metadata into JPEG EXIF and MP4 QuickTime tags — your photos and videos show up on the map in any photo app
- Fixes file timestamps so your media sorts chronologically
- Deduplicates files by content hash, keeping only the oldest copy
- Renames files to a consistent `Snapchat-YYYY-MM-DD_UUID` format
- Includes a simulation mode to preview what will happen before writing anything

## Exporting your data from Snapchat

Follow Snapchat's guide: [How do I download my data from Snapchat?](https://help.snapchat.com/hc/en-us/articles/7012305371156-How-do-I-download-my-data-from-Snapchat)

When requesting your export, select:

| Option | Required? | Notes |
|---|---|---|
| Export your Memories | **Required** | Your saved photos and videos |
| Export JSON Files | **Required** | Contains timestamps and GPS coordinates used for tagging |
| Export Shared Stories | Optional | Stories you contributed to |
| Export Chat Media | Optional | Photos/videos from chats |

Once your export is ready, **download all ZIP files within 3 days** — the download links expire after that. You do not need to extract the ZIPs yourself; SnapSiphon can do it for you.

## Getting started

1. Download your Snapchat data (see above)
2. Place all the downloaded ZIP files in a single folder (or extract them — either works)
3. Open SnapSiphon and point it at that folder
4. Click **Start Processing**

Your organized media will appear in an `output/` subfolder (configurable), with subdirectories for `memories/`, `chat_media/`, and `shared_story/`.

## Building from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet build SnapSiphon.slnx
dotnet run --project SnapSiphon.Desktop
```

## Disclaimer

SnapSiphon is not affiliated with, endorsed by, or associated with Snap Inc. or any of its subsidiaries. "Snapchat" and the Snapchat ghost logo are trademarks of Snap Inc. This project processes data that users have exported from their own accounts using Snapchat's official data export tool.

## License

[MIT](LICENSE.md)
