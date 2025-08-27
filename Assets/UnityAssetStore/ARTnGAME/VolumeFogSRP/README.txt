--------- ETHEREAL 2021 - Main core module ---------

This is a quick overview of the Ethereal system.

The asset purpose is to create volumetric lighting effects in URP pipeline. Also has a few
extra functionalities, like creating impostor light volumes.

The Ethereal system is also a submodule of Sky Master ULTIMATE URP version, the only difference at 
the core is a few extra lines of code to connect the refelctions to the Sky Master ULTIMATE water
system. Also Ethereal store version has an extra forest demo and assets and a URP foliage shader.

INSTALL INSTRUCTIONS
First install URP latest version and insert the custom pipeline supplied in "Assets\ARTnGAME\EXTRAS\URP Custom Renderer\SettingsETHEREAL" 
in the Edit -> Project Settings -> Graphics -> Scriptable renderer pipeline settings slot.

Also if have a pipeline assigned in Quality settings, also make sure to change it to the same as the Graphics settings.

Add the script "ConnectSunToVolumeFogURP" to the camera and set the Directional Light of the scene in the "Sun" slot of the script.

Tweak the effect using the script on the camera.

If after install get an error mentioning "not accesible due to protection level", restart Unity to resolve the issue.

Video tutorials playlists:

Ethereal:
https://www.youtube.com/watch?v=vJIka5aX94o&list=PLJQvoQM6t9GdvknDg470wVngbVAo_WEGo&index=1

Sky Master ULTIMATE URP:
https://www.youtube.com/watch?v=w6useVWMeMM&list=PLJQvoQM6t9Gead80gg5MSyN-gsEzmO0LW&index=1


--------- ETHEREAL 2024 - Extra new module ---------

The new module and its manual are in the Ethereal2024 folder, inside ARTnGAME -> VolumeFogSRP root folder.

This is a new module that allows much faster local lights volumes rendering, especially using Forward+ mode.
The original ETHEREAL system still has many more options for the volumetric global fog, the sun light volumes, 
can use impostor light volumes, has a haze effect and option to cutoff some light volumes by color, so can 
be used for those effects and in combination with the Ethereal2024 system.

Note that the Unity 6 URP and RenderGraph versions, while fully stable and tested, are still in Beta since
Unity team is still developing that new mode, so for any issues please contact ARTnGAME support to handle
any potential changes from Unity side in newer Unity versions.

For any issues and questions can visit the Discord channels at https://discord.gg/X6fX6J5

REQUIREMENTS:
URP pipeline and Unity 2022.3 LTS and above

EXTRA DEMO SCENES

Extra demo scenes and assets link (Forest demo - Attrium Demo - Tutorials)
https://drive.google.com/file/d/1Fr1U9Zi9Th4uygFe7piIZ2hffymFQ_lF/view?usp=sharing