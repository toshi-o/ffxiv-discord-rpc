@echo off
echo resizing class icons
magick\magick mogrify -resize 512x512 class\*.png
echo resizing status icons
magick\magick mogrify -resize 512x512 status\*.png
echo optimizing class icons
for %%f in (class\*.png) do (
    optipng "%%~f"
)
echo optimizing status icons
for %%f in (status\*.png) do (
    optipng "%%~f"
)
pause