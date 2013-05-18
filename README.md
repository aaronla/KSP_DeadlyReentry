KSP_DeadlyReentry
=================

Reentry heat for KSP 0.19+

Heat Effects
------------

1.3 - Heat effects are computed using the reentry shader, applied to main vessel. Heat is cumulative. Some 
issues with overheating at low velocities when mach effect shader kicks in, making spaceplane 

1.3.1 (unofficial) - Heat effects are computed for all active vessels, and use a new heat curve. It aims to replicate 
the safety zone of normal Earth reentry, for Kerbin. 0-6 degree reentry to atmosphere is generally safe; above 
that, heat shielding is a must. Parts that are shielded from reentry plasma (e.g. by heat shield or other parts) 
receive a lower heat load.

A primary goal of 1.3.1 is that it be possible to safely exit *and* reenter with spaceplanes -- if you're careful. 
S-turns are very useful here.


Parts
-----

The reentry shields parts are from another parts pack. They have generally higher heat tolerance, so are perfect 
for those times where you just want to 'gun it' and get to the surface fast. You can reenter coming back from the 
Mun with a heat shield and a decent reentry angle, but if you're going fast enough, steep enough, even that won't
save you unless you stack them.
