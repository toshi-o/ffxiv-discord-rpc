Discord Rich Presence for FINAL FANTASY XIV

![screenshot1](https://i.imgur.com/O3ykoPj.png)
![screenshot2](https://i.imgur.com/rg9KWGO.png)
![screenshot3](https://i.imgur.com/WlULdui.png)
![screenshot4](https://i.imgur.com/7Z3J8as.png)

This is my first time posting something I made to the interwebs, hope I did it good. 

Doesn't work on DirectX 9 mode because there aren't any memory signatures available and I have no idea how to get those.

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

4. Download the latest discord-rpc from [here](https://ci.appveyor.com/project/crmarsh/discord-rpc/build/artifacts) (builds\install
\win64-dynamic.zip)

5. Copy the ```discord-rpc.dll``` from the ```bin/discord-rpc``` folder and put it in the build output

TODO:

better / higher quality icons

more testing

read/write configuration files