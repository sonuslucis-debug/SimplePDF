# SimplePDF
A simple PDF reader with no bells and whistles.

## Features

- Open and view PDF documents
- Page navigation, zoom and continuous mode
- Print the whole document with the **Print** button or **Ctrl+P**
- Custom application icon and an **About** dialog

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
- Printing support, application icon integration and About dialog by
  **DeepSeek V4.1 Flash Free (OpenCode)**
