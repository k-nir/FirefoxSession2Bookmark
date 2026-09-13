# FirefoxSession2Bookmark
Convert uncompressed Firefox .jsonlz4 sessionstore backups to bookmark file.

## Usage
Firefox store session information of previous run in .jsonlz4 files, typically located in %AppData%\Roaming\Mozilla\Firefox\Profiles\<profile>\sessionstore-backups\ on Windows.

First uncompress the .jsonlz4 file using tools like [mozlz4](https://github.com/jusw85/mozlz4). Then run
```
Firefox2Bookmark.exe <uncompressed .jsonlz4 filename> <bookmark filename>
```
to generate bookmark file. Once imported into Firefox, converted bookmarks will appear in a folder named "Restored from <uncompressed .jsonlz4 filename>" under Bookmarks Menu folder.

## Features
1. Grouped tabs are  organized into subfolders.
2. Closed tabs  recorded in the .jsonlz4 are not included in the conversion.
