# SimplePDF
A simple PDF reader with no bells and whistles.

## Features

- Open and view PDF documents
- Drag and drop a PDF onto the window, or pass a PDF path on the command line
- Smooth mouse-wheel scrolling through the whole document
- Page-by-page or continuous mode, with zoom
- Selectable text, so passages can be copied out of a PDF
- Print the whole document with the **Print** button or **Ctrl+P**
- Sonus Lucis styling drawn from the website palette and logo
- Custom application icon and an **About** dialog
- Single, self-contained EXE with PDF.js embedded (no CDN or support files)
- Works offline

## Requirements

- Windows 10 or Windows 11
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
  (already installed on most Windows systems)

## Build

This project is intended to be built by GitHub Actions. Pushing to `main`
runs the workflow in `.github/workflows/build.yml` and publishes the
self-contained Windows build as the `publish` artifact.

To build locally, run:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

## Credits

- PDF rendering by [Mozilla PDF.js](https://mozilla.github.io/pdf.js/)
- Built with .NET 8, Windows Forms and Microsoft Edge WebView2
- Design and branding by [Sonus Lucis](https://sonuslucis.com)
- Printing support, offline PDF.js bundling, application icon integration and
  the About dialog by **DeepSeek V4.1 Flash Free (OpenCode)**
