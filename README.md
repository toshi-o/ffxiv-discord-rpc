Discord Rich Presence for FINAL FANTASY XIV

This is my first time posting something I made on the interwebs, hope I did it good.

[Download](https://github.com/Poliwrath/ffxiv-discord-rpc/releases/latest)

Demo:

[![video](https://img.youtube.com/vi/GBYuvp6H5ak/0.jpg)](https://www.youtube.com/watch?v=GBYuvp6H5ak)

How to use:

Open FFXIV

Open FFXIV-discord-rpc

Ta-da! Your online status on discord should show your FFXIV character name, online status, location (hover over status), job, job level (hover over job icon)

How to build:

1. ```git clone --recursive https://github.com/Poliwrath/ffxiv-discord-rpc.git```

2. Open the solution in VS 2017

3. Build

4. Download the latest discord-rpc from [here](https://github.com/discordapp/discord-rpc/releases/latest)

5. Copy the ```discord-rpc.dll``` from the ```win64-dynamic/bin/discord-rpc``` folder and put it in the build output

TODO:

better / higher quality icons

more testing