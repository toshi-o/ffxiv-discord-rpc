@echo off
echo resizing icons
magick\magick mogrify -resize 512x512 *.png
echo optimizing
for %%f in (*.png) do (
    optipng "%%~f"
)
pause