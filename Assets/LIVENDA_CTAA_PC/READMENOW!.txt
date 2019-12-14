PLEASE NOTE DETAILED DOCUMENTATION WILL BE AVAILABLE VERY SOON

PLEASE DELETE THE OLD VERSION BEFORE INSTALLING THIS VERSION.

CTAA Cinematic Temporal Anti-Aliasing is the next generation Anti-Alisasing Solution from Livenda Labs Pty Ltd for PC and VR Devices (This version cannot be used with VR).

CTAA V2.5 included in this package has significant quality and performance improvements and is compatible with Unity 5.4 and above. CTAA works with all objects including static, dynamic, Characters (Skinned) and foliage.

CTAA is easy to incorporate and is a SINGLE SCRIPT NOW!, it is a 'Temporal' post effect, it should be added to the main camera via the image effects 'Add Component ' section of the main camera under LIVENDA first. CTAA works in all render paths such as Forward and Deferred and all light spaces, HOWEVER, the included Demo (in the scenes folder) is designed for Linear Space, so please make sure to change your color space to Linear before running the demo. 
 

THE EFFECTS CHAIN and THE ORDER OF OTHER EFFECTS
Think of CTAA as a filter in relation to all other effects you would like to add to your scene, for example if you like to add bloom more then likely the best place to add is 'After' CTAA as it will be temporalily stable and will completely eliminate rouge shimmering (looks bad). Effects like SSAO can be added before or after CTAA, the best is to try both as every other third party implents different methods.

Third party effects.. IMPORTANT

CTAA is the Ultimate Anti-Aliasing solution so there is no need to add any other Anti-Aliasing solution either before or after in the effects chain, doing so could degrade the quality and performance and could cause unwanted shimmer. HOWEVER, in forward mode, MSAA can be enabled to provide even further anti-aliasing and very thin lines as CTAA works great together with MSAA in forward.


Multiple Cameras: If you have multiple cameras and swithing between them, you need to add CTAA effect to each one, also Please make sure your camera swithing script disables all other cameras so there is only one active camera.

DYNAMIC OBJECTS: With this version you do not need to do anything, all dynamic objects will work with CTAA

SETTINGS:

ALL the settings can be changed in realtime to suit your scene requirements.

The ' Tempopral Stability' Slider Defaults to 6 which is great (Sharp and responsive) for most scenes, this can be reduced to obtain faster temporal-convergance (might cause shimmer but sharper) or increased for a more Cinematic effect (Hint: You can dynamicaly change this via scripting depending on your scene)

Adaptive Enhance: This modulates the strenght of antialiasing (and a few other parameters) based on relative velocity, higher values will yield a sharper image. no impact on performance for any value

Temporal Jitter Scale: Default is 1, this value modulates the distance/size of off-axis jitter which is applied to each camera per frame. The larger the value the larger the sampling distance. Default value of 1 if great for General Use in all PC/Mac/Console projects, if you like a little sharper image this can be decreased for your particular scene requirements. No impact on performance for any value.

- We have also included a DemoScene to get you started in the Scenes folder called Cardemo_CTAA


Thanks again for your purchase and please note CTAA is in constant development to increase visual quality and performance.

