# Settings

**SmartImage** is highly customizable. The following table lists config options.

| Setting | Description |
|--|--|
| Search engines | Engines to use when searching. |
| Priority engines | Engines whose results are opened in the browser.|
| Filtering | Hides low-quality and unsuccessful results. |
| Notification | Displays a toast notification with result information once the search is completed.|
| Notification image | Includes a preview image in the toast notification. |
| Context menu | Integrates **SmartImage** into the context menu (right-click menu). |

These settings can be configured in different ways:

- The main menu
- The command line


# Engines

## Options

Search engine names and configuration:

| Real Name       | Option Name     |
| --------------- | --------------- |
| (All)           | `All`           |
| (None)          | `None`          |
| (Auto)          | `Auto`          |
| (Artwork)       | `Artwork`       |
| SauceNao        | `SauceNao`      |
| ImgOps          | `ImgOps`        |
| Google Images   | `GoogleImages`  |
| TinEye          | `TinEye`        |
| IQDB            | `Iqdb`          |
| trace.moe       | `TraceMoe`      |
| Karma Decay     | `KarmaDecay`    |
| Yandex          | `Yandex`        |
| Bing            | `Bing`          |
| Tidder          | `Tidder`        |
| Ascii2D         | `Ascii2D`       |

Special options:
 - `All`: Use all available engines
 - `None`: Use no engines<sup>1</sup>
 - `Auto`: Use the best engine result<sup>1</sup>
 - `Artwork`: The engines *SauceNao*, *IQDB*, *Ascii2D*

<sup>1</sup> This option can only be used with priority engine options. They cannot be used for the main search engine options.

## Search Engines

These options are the engines used to perform searches.

## Priority Search Engines

These options are the engines whose results will be opened in your browser. If `Auto` is used, the best result is opened. If `None` is used,
no results will be opened in the browser.


# Behavior

Configuration is loaded in this order:
1. Configuration file (`SmartImage.cfg`)
2. Command line parameters (if specified)

Therefore, command line parameters have precedence over the config file. For example, if the config file designates  `SauceNao` as a priority engine but the command line argument `-pe Iqdb` is specified, `Iqdb` will take precedence.

This can be useful when you want to run a search with certain settings but want to keep your personalized configuration intact.