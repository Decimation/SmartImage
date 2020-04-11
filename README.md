# SmartImage

*Find the source image in one click!*

SmartImage is a reverse image search tool for Windows with context menu integration. SmartImage will open the best match found returned from various image search engines (see the supported sites) right in your web browser. This behavior can be configured to the user's preferences.

Supported sites:

- SauceNao (`SauceNao`)
- ImgOps (`ImgOps`)
- Google Images (`GoogleImages`)
- TinEye (`TinEye`)
- IQDB (`Iqdb`)
- trace.moe (`TraceMoe`)
- KarmaDecay (`KarmaDecay`)

# Download

**[See the latest releases](https://github.com/Decimation/SmartImage/releases)**

# Example

![Demo](https://github.com/Decimation/SmartImage/raw/master/Demo.gif)

![Context menu image](https://github.com/Decimation/SmartImage/blob/master/Context%20menu%20integration.png)

# Configuration

SmartImage stores its configuration in registry. The rationale behind this is that SmartImage is designed to be used primarily through the context menu, so configuration persisting between uses is more logical.

# Commands

`--set-saucenao-auth <api key>`

Configures the SauceNao API key. Register an application [here](https://saucenao.com/user.php), then get your key [here](https://saucenao.com/user.php?page=search-api). If this is configured, SmartImage will be able to return more specific results. SmartImage will be able to function as seen in the demo (opening the direct source image in your browser).

`--set-imgur-auth <consumer id> <consumer secret>`

Configures Imgur API keys. Register an application [here](https://api.imgur.com/oauth2/addclient), then get your ID [here](https://imgur.com/account/settings/apps). If this is configured, SmartImage will use Imgur to upload temporary images instead of ImgOps.

`--search-engines <engines>`

Sets the search engines to use when searching, delimited by commas. *Default: All*

`--priority-engines <engines>`

Sets the priority search engines. Priority search engines are engines whose results will be automatically opened in your browser when searching is complete. For example, if you designate `SauceNao` as a priority engine, then results returned by
`SauceNao` will be automatically opened in your browser. *Default: `SauceNao`*

`--ctx-menu`

Installs context menu integration.

`--add-to-path`

Adds executable path to path environment variable.

`--reset [all]`

Resets configuration to defaults. Specify <all> to fully reset.

`--info`

Information about the program and its configuration.

# Notes

- Ensure that the executable is placed in the system PATH (*`%PATH%`*) environment variable, otherwise the context menu integration will not work. You can read about how to do this [here](https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10).
- SmartImage uploads temporary images using ImgOps (the uploaded images are automatically deleted after 2 hours). Imgur can also be used, but you must register an Imgur application client.

# to-do

- Add an icon
- Automate SauceNao API registration and make its configuration more user-friendly

# Inspiration

- [SauceNao-Windows](https://github.com/RoxasShadow/SauceNao-Windows)
- [SharpNao](https://github.com/Lazrius/SharpNao)
