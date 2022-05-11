# LevelUpPlanCustomizer mod for Pathfinder: Wrath of the Righteous 

Allows you to customize pregens and companions automatic level up builds and enables auto level regardless of difficulty setting.

## IMPORTANT: By itself this mod does NOT change any in game builds. Until you add them yourself, the only effect will be avilability of automatic level up regardless on setting.

Clarfification on last point: this is done by bypassing the check of game setting entirely. So even if 'Off' is selected, auto level can happen until you take control of leveling process.


## How it works

1. Write (or get someone else to write) level up plan for some character (either companion or your pregen) in specified format.
2. Put it into `FeatureLists/` (for companions and other units) or `Pregens/` (for MC) folder inside mod install folder.  
3. It will be applied to characters that you haven't met yet. Meaning that pregen for MC can only be defined before starting the game.
4. Just play.
5. If at any point you want to remove the mod, you're free to do so. All existing characters will keep what they already picked and if your normal difficulty setting allows auto level, they will progress on plan that were defined for them.

## What it can do
- Replace existing in game pregens.
- Replace companion level plans entirely. Meaning feat selections can be defined from lvl 1. But not stats (or race, or gender, or alignment). Once again reminder - this will only apply if you haven't met that companion yet.   
- Edit feature selections of other in game units if for some reason you want to.

## What it CAN't do
- Change already met companions
- Change companions stats, race, gender, alignment.
- Change your level up plan if you're already following one
- Pre-set pregen visual characteristics to your liking. You'll get default race/class look, that you can change during character creation.

## How to try it 

Take [PregenMeleeSorcerer.json](LevelUpPlanCustomizer/Sample/v1/PregenMeleeSorcerer.json) from Samples/v1 folder in mod folder and put it in Pregens/ folder (you can create it or mod will create this folder if it doesn't exist).  
This replaces pregen of default Rogue pregen. Name and portrait on selection screen will be default, but you will notice the class will be Monk. Because that's what taken at lvl1.  
Start a new game, use Toybox to level up to 20, and then it's all just pressing next.


## How to write feature list or pregen.  
Take a look at [How to.md](/How%20to.md), I try to describe how it works, but it still requires a bit of technical knowledge and you will probably need additional tools.   
Look at provided samples. Seelah alternative build is an easy example (just lvl20 paladin). Pregen is a Monk(SF)1/Sorc4/DD4/Ek10/Sorc1 gish, and it shows how multiclassed builds work. Note - neither of two builds are recommended, they're just samples.