# Input

**SmartImage** can be used in a variety of ways.
- Input text field in the main menu
  - Copy/paste text
  - Use the *Browse* button to open the file picker dialog
- Context menu
  - Use the *Config* button to open the [configuration dialog](https://github.com/Decimation/SmartImage/wiki/Interface#configuration) to toggle context menu integration
- Clipboard
  - copying an _image_ or _URI_ outside of the program will automatically populate the input field
- Command line
  - Use the `--i` parameter to specify input

# Queries

The value given as [input](#Input) is referred to hereafter as _search query_ or _query_.

* Search queries may be either a _file_ or _URI_.
* All queries must be a recognized image type.
* If query is a _URI_, it must be a direct link (i.e., the payload returned is a binary image). For example, `https://i.imgur.com/zoBIh8t.jpg` returns
an image payload with `Content-Type` as `image/jpeg`.

