# FirefoxSession2Bookmark
Convert uncompressed Firefox `.jsonlz4` sessionstore backups to bookmark file.

## Usage
Firefox stores session information of previous runs in `.jsonlz4` files, typically located in `%AppData%\Roaming\Mozilla\Firefox\Profiles\<profile>\sessionstore-backups\` on Windows.

First uncompress the `.jsonlz4` file to `.json` using tools like [mozlz4](https://github.com/jusw85/mozlz4). Then run
```
Firefox2Bookmark.exe <uncompressed .json> <bookmark name>
```
to generate bookmark file (which should be a `.html` file). Once imported into Firefox, converted bookmarks will appear in a folder named "Restored from <uncompressed .json>" under Bookmarks Menu folder.

## Features
1. Grouped tabs are  organized into subfolders.
2. Closed tabs recorded in the `.jsonlz4` are *not* included in the conversion.

## Notes
This project also serves as a sample for reading large json files using `UTF8JsonReader`.
