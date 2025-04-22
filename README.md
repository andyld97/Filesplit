# FileSplit

> ⚠️ **Warning:** This currently only works for Windows 7 and higher. But I plan to make a `.NET 4.0-Version` for Windows XP too!

## Info

You can split a large file into smaller parts — for example, to transfer big files via CDs, chat apps, or other services with file size limits.

For instance, a 5 GB file can be split into 10 parts of 500 MB each. If the file is 5.2 GB, and you split it into 500 MB chunks, you'll get 11 parts: ten full-sized parts and one smaller one at the end.

Alongside the split files, an additional info file is created. This file contains metadata and a hash of the original file, which is used to verify the integrity of the reassembled file.

Make sure to transfer the info file together with the parts — it’s required for recombining them correctly.

## Screenshots
<p align="center">
  <img src="https://github.com/user-attachments/assets/e8ff3721-7d3c-44d9-a91a-2483ad8dd85c" width="48%" />
  <img src="https://github.com/user-attachments/assets/45a220d8-ab14-4c71-9035-1f9e7a98944e" width="48%" />
</p>
