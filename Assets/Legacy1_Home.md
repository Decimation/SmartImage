```
  ____                       _   ___
 / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
 \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
 |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
                                                     |___/
```

Welcome to the **SmartImage** wiki!

# Installation

## Requirements

The only requirements are .NET 6 and Windows. You can check if .NET 6 is installed by running 
one of the following commands:

`dotnet --list-runtimes`: <br />
_Output:_ `Microsoft.NETCore.App 6.0.0 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]`

`dotnet --list-sdks`: <br />
_Output:_ `6.0.100 [C:\Program Files\dotnet\sdk]`

If the version major number is 6 (i.e., first number in the version), then .NET 6 is installed.

***

**SmartImage** must be added to the system PATH (*`%PATH%`*) environment variable, otherwise context menu integration will not work. **SmartImage** will automatically do this for you. Otherwise, you can read about how to manually do this [here](https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10).

If the IME (system language) is a non-Romance language (e.g., Japanese or Chinese), some features may not work correctly (i.e., keyboard input, context menu integration). To resolve this, set the IME to English.

# Engines

Supported search engines and notes:

- <img src="https://saucenao.com/favicon.ico" width="16" height="16"/> [SauceNao](https://saucenao.com/)
  - Multi-service image search
  - **Use case:** Finding sauce, usually artwork
- <img src="https://iqdb.org/favicon.ico" width="16" height="16"/> [IQDB](https://iqdb.org/)
  - Multi-service image search
  - Similar to *SauceNao*
  - **Use case:** Finding sauce, usually artwork
- <img src="https://trace.moe/favicon128.png" width="16" height="16"/> [trace.moe](https://trace.moe/)
  - Multi-database image search
  - **Use case:** Identifying anime from a screenshot
- <img src="http://karmadecay.com/favicon.ico" width="16" height="16"/> [Karma Decay](http://karmadecay.com/)
  - Reddit image search
  - **Disclaimer:** Very slow
- <img src="http://imgops.com/favicon.ico" width="16" height="16"/> [ImgOps](http://imgops.com/)
  - Multi-service image search
  - **Use case:** Performing multiple image operations
  - **Restrictions:** Max upload size is 5MB
- <img src="https://images.google.com/favicon.ico" width="16" height="16"/> [Google Images](https://images.google.com/)
  - General-purpose image search
- <img src="https://tineye.com/favicon.ico" width="16" height="16"/> [TinEye](https://tineye.com/)
  - General-purpose image search
  - Generally better than *Google Images*
- <img src="https://yandex.com/favicon.ico" width="16" height="16"/> [Yandex](https://yandex.com/images/)
  - General-purpose image search
  - **Disclaimer:** Russian
- <img src="https://www.bing.com/favicon.ico" width="16" height="16"/> [Bing](https://www.bing.com/images/)
  - General-purpose image search
- <img src="http://tidder.xyz/favicon.ico" width="16" height="16"/> [Tidder](http://tidder.xyz/)
  - Reddit image search
  - Generally better than *Karma Decay*
- <img src="https://ascii2d.net/favicon.ico" width="16" height="16"/> [Ascii2D](https://ascii2d.net/)
  - Multi-service image search
  - Similar to *SauceNao* and *IQDB*
  - **Use case:** Finding sauce, usually artwork

***


# Usage

<b>SmartImage</b> can be used in multiple ways:

- Open the program normally (double click) and you can use the program in a user-friendly way. You can then drag and drop your image into the command prompt and run a search.

<p align="center">
<img src="https://github.com/Decimation/SmartImage/raw/master/Examples/Demo%203.gif" width="636.35" height="370.7">
</p>

- Right-click on an image (once the context menu integration is set up) and select the <b>SmartImage</b> option to immediately perform a search.

<p align="center">
<img src="https://github.com/Decimation/SmartImage/raw/master/Examples/Demo%201.gif" width="640" height="360">
</p>

- Drag and drop an image over the executable to immediately perform a search (functionally the same as right-clicking on an image and using the <b>SmartImage</b> option).

<p align="center">
<img src="https://github.com/Decimation/SmartImage/raw/master/Examples/Demo%202.gif" width="636.35" height="370.7">
</p>

- Use the command line.
