# LevelUpPlanCustomizer mod for Pathfinder: Wrath of the Righteous 

Allows you to customize pregens and companions automatic level up builds and enables auto level regardless of difficulty setting.

Now with export for MC and story companions! (See How to try it section)

## IMPORTANT: By itself this mod does NOT change any in game builds. Until you add them yourself, the only effect will be avilability of automatic level up regardless on setting.

Clarification on last point: this is done by bypassing the check of game setting entirely. So even if 'Off' is selected, auto level can happen until you take control of levelling process.


## How it works

1. Get pregen of MC or feature list of companion. Two options for that:
	- Write (or get someone else to write) level up plan for some character (either companion or your pregen) in specified format.	Put it into `FeatureLists/` (for companions and other units) or `Pregens/` (for MC) folder inside mod install folder.
	- Load some save, open UMM mod menu and press export. This will output files directly into needed folder.
2. It will be applied to characters that you haven't met yet. Meaning that pregen for MC can only be defined before starting the game.
3. Just play.
4. If at any point you want to remove the mod, you're free to do so. All existing characters will keep what they already picked and if your normal difficulty setting allows auto level, they will progress on plan that were defined for them.

## What it can do
- Replace existing in game pregens.
- Replace companion level plans entirely. Meaning feat selections can be defined from lvl 1. But not stats (or race, or gender, or alignment). Once again reminder - this will only apply if you haven't met that companion yet.   
- Edit feature selections of other in game units if for some reason you want to.

## What it CAN't do
- Change already met companions
- Change companions stats, race, gender, alignment.
- Change your level up plan if you're already following one
- Pre-set pregen visual characteristics to your liking. You'll get default race/class look, that you can change during character creation.
- Make builds that have normal progression depend on mythic progression (second bloodline feats or mythic feat extra mythic feat being a prerequisite for) (Will be solved in the future)

## What can export do
- It knows which classes you took and in which order
- It tries to calculate level when you took feature selections.
- It tries to guess what stat you raised on level ups. Mostly it assumes you put point into your highest stat
- It takes all skills you put levels into and just throws them all. If your skill strategy is more complicated then max same skills, you will have to adjust output yourself
- Game does not know which spell you learned when, so it throws all your known spells on each spellcaster level. Adjust spells learned yourself.

## How to try it 

1. Load one of your saves.
2. Export your MC overwriting one of the standard pregens.
3. (Optional) Close the game and go open pregen in Pregens folder (file should be named after your character) and adjust skills taken and spells learned.
4. Start new game with it.
5. Toybox yourself to 20
6. Enjoy clicking next while seeing levels go by. Hopefully :)


## How to write feature list or pregen.  
Take a look at [How to.md](/How%20to.md), I try to describe how it works, but it still requires a bit of technical knowledge and you will probably need additional tools.   
Look at provided samples. Seelah alternative build is an easy example (just lvl20 paladin). Pregen is a Monk(SF)1/Sorc4/DD4/Ek10/Sorc1 gish, and it shows how multiclassed builds work. Note - neither of two builds are recommended, they're just samples.


## Troubleshooting
If level up plan stops for no reason that can be because of several reasons

Possibility: Your character on that level depended on something gained from outside source. Game tries to evaluate plan entirely right when it first loads the character and if it forecasts that you for example won't be able to take this feat, it does not record this action for future use.  
Solution: Don't do such builds for the moment, until I find a safe way to fix this. Alternatively enable ToyBox option that allows taking any feat without prerequisites and choose pregen with it enabled. Disable when you get control of your character.

Possibility: something-something that I haven't encountered. 
Solution: try temporarily disabling experimental patches and reloading the game. If that doesn't help you will have to continue manually. If you want to help me troubleshoot please prepare pregen that you loaded and save file. (Or better yet if you have archive manager, open save with it since it's just a zip file and extract party.json out of it - that's the only file of interest).


If something during manual leveling process seems to go wrong, try disabling "experimental" patches and restarting the game.  
