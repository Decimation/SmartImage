# Configuration

## Search Engines

`-se <engines>` _(parameter)_

Specifies engines with a comma-separated list of search engine names

## Priority Engines

`-pe <engines>` _(parameter)_

Specifies priority engines with a comma-separated list of search engine names

## Filtering

`-f` _(switch)_

Enables filtering of results (omit to disable)

# Input

The last parameter should be the image query.

# Examples

`smartimage -se All -pe SauceNao 'https://litter.catbox.moe/x8jfkj.jpg'`

Runs a search of `https://litter.catbox.moe/x8jfkj.jpg` with `All` search engines, and `SauceNao` as a priority engine.

<br></br>

`smartimage 'C:\Users\<User>\Downloads\image.jpg'`

Runs a search of `C:\Users\<User>\Downloads\image.jpg` with configuration from the config file.
