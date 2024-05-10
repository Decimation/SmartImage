# Main menu

<img align="right" src="https://github.com/Decimation/SmartImage/raw/master/Examples/Main%20menu.png" width=601 height=316>

<p style="text-align: left;">


- **Run**: Runs a search
- **Engines**: Configures engines
- **Priority engines**: Configures priority engines
- **Filter**: Toggles filtering
- **Notification**: Toggles toast notification
- **Notification image**: Toggles toast notification image
- **Context menu**: Toggles context menu integration
- **Config**: Displays current configuration
- **Info**: Displays program info
- **Update**: Checks and installs new versions
- **Help**: Opens help


<!-- todo
-->


</p>

<br /><br />

# Results

## Result

Once the search is complete, the UI shows extensive information about the search results.


<img align="right" src="https://github.com/Decimation/SmartImage/raw/master/Examples/Example%20search%20results.png" width=591.5 height=391.5>

<p style="text-align: left;">

- **Result**: Most accurate and specific result URL
- **Raw**: Undifferentiated result URL
- **Direct**: Direct image URL
- **Similarity**: Image similarity (delta)
- **Description**: Image description, caption, etc.
- **Site**: Result site<sup>1</sup>
- **Artist**: Image artist<sup>1</sup>
- **Characters**: Character(s) in the image<sup>1</sup>
- **Source**: Image source<sup>1</sup>
- **Resolution**: Image resolution
- **Detail score**: Number of detail fields
- **Other image results**: Other results

</p>


<sup>1</sup> This metadata is usually only for anime or related image results (i.e. *SauceNao*, *IQDB*, *TraceMoe*, etc.)

# Interaction

## General

- Press the option character on the keyboard to open it (i.e., press **0** to open **[0]** or **A** to open **[A]**).
- Press `Escape` to return to the previous interface.
- Press `F1` to show filtered results.
- Press `F5` to refresh the console buffer.

## Results

- Press the result character to open the **Result** URL in your browser.
  - Hold down `Ctrl` to search for a direct image link.
  - Hold down `Alt` to show more info and results. Certain engines will return multiple results; those can be viewed using this option.
  - Hold down `Shift` to open the raw URL.
  - Hold down `Ctrl` and `Alt` to download the image.