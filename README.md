# Scroll of Taiwu Mods

Mods for [The Scroll of Taiwu][scroll-of-taiwu-steam].

## Installation

Extract the contents of mod archives into `<Game Folder>/Mod/`.

## Build

1. Define `GamePath` property in `Local.props` next to `Directory.Build.props`.
2. Define `MOD_PATH` in an `.env` file in the repo's root.
   ```
   MOD_PATH="/path/to/The Scroll of Taiwu/Mod/"
   ```
3. Run [just] `dev` from within a mod subdirectory to install the mod.

[just]: https://github.com/casey/just/
[scroll-of-taiwu-steam]: https://store.steampowered.com/app/838350/The_Scroll_of_Taiwu__Beyond_The_Dome/
[raindb-tutorial]: https://andrewfm.github.io/RainDB/tutorials.html
