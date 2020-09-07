# SmartImage

<p align="center">
    <img src="SmartImage/Icon.png" width="180" height="180">
</p>

<p align="center">
    <a href="https://GitHub.com/Decimation/SmartImage/releases/" alt="Releases">
        <img src="https://img.shields.io/github/release/Decimation/SmartImage.svg" /></a>
  <a href="https://GitHub.com/Decimation/SmartImage/releases/" alt="Total Downloads">
        <img src="https://img.shields.io/github/downloads/Decimation/SmartImage/total.svg" /></a>
</p>

```
  ____                       _   ___
 / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
 \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
 |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
                                                     |___/
```

*Find the source image in one click!*

SmartImage is a reverse image search tool for Windows with context menu integration. SmartImage will open the best match found returned from various image search engines (see the supported sites) right in your web browser. This behavior can be configured to the user's preferences.


### [Download](https://github.com/Decimation/SmartImage/releases)


# Supported sites

Supported sites:

- [SauceNao](https://saucenao.com/)
- ImgOps
- Google Images
- TinEye
- IQDB
- trace.moe
- Karma Decay
- Yandex
- Bing

# Download

**[See the latest releases](https://github.com/Decimation/SmartImage/releases)**

# Example

![Demo](https://github.com/Decimation/SmartImage/raw/master/Demo.gif)

![Context menu image](https://github.com/Decimation/SmartImage/blob/master/Context%20menu%20integration.png)

# Usage

SmartImage can be used in multiple ways:

- Open the program normally (double click) and you can use the program in a user-friendly way. You can then drag and drop your image into the command prompt and run a search.
- Right-click on an image (once the context menu integration is set up) and select the SmartImage option to immediately perform a search.
- Drag and drop an image over the executable to immediately perform a search (functionally the same as right-clicking on an image and using the SmartImage option).
- Use the command line which allows for more specific and advanced searching by using the listed arguments and options.

# Reference

See the [Wiki](https://github.com/Decimation/SmartImage/wiki) for documentation on command line usage and options.

# Notes

- SmartImage may trigger an antivirus warning when first running. This is because SmartImage dynamically creates and runs batch files to add its context menu entry. Unfortunately this seems to be the only way to add context menu entries (for now).

- SmartImage must be added to the system PATH (*`%PATH%`*) environment variable, otherwise context menu integration will not work. SmartImage will automatically do this for you. Otherwise, you can read about how to manually do this [here](https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10).

- SmartImage uploads temporary images using ImgOps (the uploaded images are automatically deleted after 2 hours). Imgur can also be used, but you must register an Imgur application client.

- Some functions use hacky solutions (like dynamically creating a registry key file to install context menu integration). This is temporary until I can find better approaches, but it should work in the meantime.

# to-do

- Further rewrite & refactor codebase

- Find better, less hacky approaches to various functions

- Update examples

# Inspiration

- [SauceNao-Windows](https://github.com/RoxasShadow/SauceNao-Windows)
- [SharpNao](https://github.com/Lazrius/SharpNao)
