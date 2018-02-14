#does not work anymore, feel free to fork it or whatever since i give up on this

![screenshot1](https://i.imgur.com/O3ykoPj.png)
![screenshot2](https://i.imgur.com/rg9KWGO.png)
![screenshot3](https://i.imgur.com/WlULdui.png)
![screenshot4](https://i.imgur.com/TxXtOuI.png)

This is my first time posting something I made to the interwebs, hope I did it good. 

Doesn't work on DirectX 9 mode because there aren't any memory signatures available and I have no idea how to get those.

[Download](https://github.com/Poliwrath/ffxiv-discord-rpc/releases/latest)

Demo (old):

[![video](https://img.youtube.com/vi/GBYuvp6H5ak/0.jpg)](https://www.youtube.com/watch?v=GBYuvp6H5ak)

How to use:

Open FFXIV

Open FFXIV-discord-rpc

Ta-da! Your online status on discord should show your FFXIV character name, online status, location (hover over status), job, job level (hover over job icon) and duty elapsed time (may be delayed and the time starts when the application detects that you're in a duty/pvp)

How to build:

1. ```git clone --recursive https://github.com/Poliwrath/ffxiv-discord-rpc.git```

2. Open the solution in Visual Studio 2017

3. Build (the "asdf" configuration uses hardcoded/random values for testing rather than reading memory, so only use this if you're on a computer that can't play the game or if it's down for maintenance!)

4. Download the latest discord-rpc from [here](https://ci.appveyor.com/project/crmarsh/discord-rpc/build/artifacts) (builds\install\win64-dynamic.zip)

5. Copy the ```discord-rpc.dll``` from the ```bin/discord-rpc``` folder and put it in the build output

TODO:

better / higher quality icons

more testing

read / write configuration files
