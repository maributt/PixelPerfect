<h1 align="center">Pixel Perfect Plus</h1>
<p align="center">A <a href="https://github.com/goatcorp/Dalamud">Dalamud</a> plugin for showing hitboxes<br>Based on <a href="https://github.com/Haplo064">Haplo's</a> <a href="https://github.com/Haplo064/PixelPerfect">PixelPerfect</a> - Type<code>/ppp</code> in-game to configure!</p><br>

<img src="https://user-images.githubusercontent.com/76499752/116016839-255d8080-a63e-11eb-8aaa-ea65011a4b6a.png" width="40%" align="right">

This plugin aims to give the player a framework with which 
to better understand / feel the distance they are away from one or multiple
given enemies with configurable colors communicating non-verbally said distance
and abiding a set of player defined coloring rules for whatever distance is needed.

The player can choose to display hitboxes for only themselves, the enemy they are targeting
or even for all enemies nearby that are only closer to the player than a given distance.


It of course still provides the base functionality of the standard Pixel Perfect
plugin but probably shouldn't be used for only this though it can be used as such
in case the player only wants to give their target a hitbox, the conditional coloring
feature is entirely optional.


This plugin also hides by default the entirety of the player ring settings as 
I found the feature to be a little too visually intrusive / not telling enough 
of the distance of the player's surroundings in most scenarios.


## Plans

- i REALLY need to clean up the code because it's a huge mess
- probably add more configurable things for all sorts of things (aka: i heard you like checkboxes so i put checkboxes in your checkboxes pretty much)
- segment the lines drawn in between hitboxes so they can be drawn even if the target is off-screen, or just complain that worldtoscreen SUCKS(?)
- i swear im going to update this 
- add a way to filter party members to display (either when Dalamud updates with better party support or just add a name/world filter even though that would be troublesome to set up)
- add swappable configs so you could have a config or more per job/per fight and a chat command to swap between those fast (i.e. `/ppp e10s`)
- maybe add a way to bind a config to a job so you don't even have to macro it(?)
