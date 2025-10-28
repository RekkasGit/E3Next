
# Order of Operations
When it comes to what to cast first it is based on few things. First is which sections to prioritize first, this is defined in *Advanced Settings.ini*. Second, is some section are hardcoded with priority checks, like heals and cures. Third, is the order you place your items under that section such as Nukes/Dots/Debuff. If by chance for example sakes you were to place an instant cast zero cooldown nuke above a spell with 12s cooldown without any defined conditions(Ifs). The 12s cooldown spell will never cast. Now if you place the 12s cooldown spell above the instant, then the 12s will cast and then the instant cast will be spam until the cooldown is back up.

*Visual Graphic Coming in the future.*

# Misc
|Settings(Key)  | Default |Values  |Description | Has Slash Cmd |
|:--------|:----------------:|:---------------------:|:-----------|:-:|
|Autofood= | Off | On/Off | Turns on eating the food and drink defined below |
|Food= <br> Drink= | | *String* | Define what you would like to eat and drink. This can be used to keep stat food from getting consumed. <br> `Food=Misty Thicket Picnic`<br> `Drink=Fuzzlecutter Formula 5000` <br><br> *Note: Multiple Food and Drink can be defined*
|End MedBreak in Combat(On/Off)=| Off | On/Off | When enabled you will cancel medding to assist in combat even if you are not at defined mana percentage.
|AutoMedBreak (On/Off)=| Off | On/Off | Will sit and med when below the defind Mana percentage in your general_settings.ini |
| Auto-Loot (On/Off)= | Off | On/Off | When enabled your character will autoloot once combat is over. Looting in combact can be enabled in general_settings.ini | Y|
| Anchor (Char to Anchor to)= | | *String* | When one of your characters is defined, it will stick to that character. They will go out fight and loot(if on) and then return to the define anchored character.| Y|
| Remove Torpor After Combat | On | On/Off | if you would like Torpor automatically removed once combat is over. |
| Auto-Forage (On/Off)= | Off | On/Off | When enabled you will start foraging. You can leverage [Cursor Delete] section in this Ini to manage the junk loot |
| Dismount On Interrupt (On/Off)= | On | On/Off | If you are on a mount and your priority requires you to inturrupt a spell for a higher priority spell(ex: Nuking -> Heal). |
| Delay in MS After CastWindow Drops For Spell Completion= | 0 | *integer* | Will interject a delay before casting or moving again after you are done casting.<br> Sometimes lag can create an issue where you/bot see itself as done casting and then moves and then your spell gets canceled. This creates milliseconds of day to make sure cast finished. | |
|If FD stay down (true/false)= | False | True/False |If you are Feign Death and an assist command is giving it will stand you up. |

# Assist Settings
|Settings(Key)  | Default |Values  |Description | Has Slash Cmd |
|:--------|:----------------:|:---------------------:|:-----------|:-:|
|Assist Type (Melee/Ranged/Off)= | Melee | Melee<br>AutoAttack<br>Ranged<br>Autofire<br>Off | Defines how your character will behave in combat. <br> <br> `Melee` - Will melee, stick, and use all configuration <br> `Autoattack` - Will melee and use configuration. No automated movement/stick <br> `Ranged` Will ranged attack, stick, and use configuration. <br> `Autofire` Will ranged attack and use configuration. No automated movement/stick.<br> `Off` - Will use no movement, stick, or facing and just use configuration. | Y |
|Melee Stick Point=| behind | behind<br>behindonce<br>front<br>pin<br>!front | The position you want you character stand during combat.<br><br> `behind` - Will stay behind the target and readjust position<br>`behindonce` - Will move behind target and not readjust unless a new assist command is giving. <br> `Front` - Will place you in front of mob facing it. Normally used by tanks. <br>  `Pin` - Will put on you on the side of the of target. Not in front and not behind. <br> `!front` Will place you anywhere that isn't the front of the mob. 
| Delayed Strafe Enabled (On/Off)= | On | On/Off | The amount of time your character will wait before readjusting to a moved target. | |
| Melee Distance= | MaxMelee | MaxMelee<br>*Integer #*| The distance you wish to stand from the target when melee'ing.<br> `MaxMelee` - Will calculate based off the target the furthest point to melee, between 25 - 33.
| Ranged Distance= | 100 | *Integer #*<br>Clamped | When Ranged is specified as Assist Type will keep you the defined distance away from target.<br>`Clamped` - Doesn't care about a defined distance as long as you are between 30 and 200. |
| Auto-Assist Engage Percent= | 98 | *integer %* | *Note: This is dependent on you enabling in you general_settings.ini(Off by default)*<br> What this setting is stating that when the character of this configed Ini's target hits the specfied percentage it will automatically tell all other bots to assist. It is ***NOT*** stating that this character starts assisting at defined percentage. <br><br> My personal(Metaljacx) recommendation for those new; not to use autoassist. Leverage `/assistme` `/cleartargets`
| Pet back off on Enrage (On/Off)= <br> Back off on Enrage (On/Off)= | Off | On/Off | When Enrage is detected, E3 will stop attacks. | |

# Melee Abilities
> **Available Conditionals**
> | Ifs | CastIf | CheckFor | MinEnd | PctAggro | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent
> [[More Info on Conditionals|3b)-Tag-Data]]

|Settings(Key) |Description  | Has Slash Cmd |
|:--------|:-----------|:--:|
| Ability= | The name of a melee ability, skill, or discipline |

# Buffs
> **Available Conditionals**
> | Ifs | CheckFor | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | MinDurationBeforeRecast | MinEnd | NoInterrupt
> [[More Info on Conditionals|3b)-Tag-Data]]

|Settings(Key) |Description  | Has Slash Cmd |
|:--------|:-----------|:--:|
| Instant Buff= | Any buff that is self targetable that has a cast time less then .1 second. This will make sure the buff stays up inside and outside of combat.<br><br> Great for fights where buffs get dispell to keep cheap buffs in first few slots. |
| Self Buff= | For any spell where you can target yourself and has a castime greater then Instant Buff allows. Will only cast outside of combat |
| Bot Buff = | This is for targeting your other characters for buffs. Will only cast outside of combat. <br><br> **Example:**<br> `bot buff=buffname/target` <br> `Bot Buff=Spirit of Might/Metaljacx` <br>`Bot Buff=Spirit of Might/Silverjacx` |
| Combat Buff= | If you just want the buff during combat <br><br>**Example:** <br> `Combat Buff=Artifact of the Leopard` - If casting self <br> `Combat Buff=Artifact of the Leopard/Metaljacx` - If casting on another character. |
| Group Buff= | **Despite it's name this has nothing to do with automating buffs for your 6 Man Group** <br>What this key value actually does it buff anyone who asks you for buffs with the pharse "Buff Me" or "Buff my Pet". This is a triggered non-automated event. Who can ask for buff an be more defined in you general_settings.ini <br><br>**Example:** <br> `Group Buff=Blessing of Aegolism` <br><br> *Note: You may need to do `/tgb on` for group buffs to apply to people outside your group* | Y |
| Pet Buff= | This is for other characters on your bot network pets.***Must be a part of your bot network.*** If your class has a pet that will be defined in the [pet section]. <br><br>**Example:** <br> `Pet Buff=buffname/PetownerName` <br> `Pet Buff=Artifact of the Leopard/Copperjacx`
| Group Buff Request=<br><br>Raid Buff Request= | This is to request a buff from someone else who is running E3 and not part of bot network. <br><br>**Example:**<br>`Group Buff Request=buffname/target` <br> `Group Buff Request=Torpor Rk. V/Tophet/Ifs\|TorporIf` <br><br> *Tip: In order to not not spam and annoy your friends. Try using an If Statement.* <br> `TorporIf=!${Bool[${Me.Song[Torpor Rk. V].ID}]} \|\| ${Me.Song[Torpor Rk. V].Duration} <=9000` |
|Stack Buff Request= | This is where you can request the same buff from multiple characters who can provide the same buff. This is request, First IN First Out(FIFO). <br><br>**Switches**<br>`/StackRequetTargets\|` - All the characters who can cast buff. <br>`/StackRecastDelay\|` - How long to wait before asking for buff again. <br> `/StackCheckInterval\|` - How often to check if you still have buff <br> `/StackRequestItem\|` - If the buff requires an Item to be clicked   |
| Cast Aura(On/Off)= | This will automatically cast your class's highest level aura if your class has one |

# Nukes/Stuns/PBAE
> **Available Conditionals**
> | Ifs | Delay | GiftOfMana | NoAggro | PctAggro | CastIf | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | NoInterrupt
> [[More Info on Conditionals|3b)-Tag-Data]]

|Settings(Key) |Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Main= | Name of the spell you wish to cast. <br><br>**Example:**<br> `main=Spear of Ro` |
| PBAE= | Point Blank Area of Effect(PBAE) is turned off by default to turn on or off use `/pbaeon` and `/pbaeoff`. Otherwise it is the same as Nuke just input your poe spell <br><br>**Example:**<br> `PBAE=Spear of Ro` <br><br> *Reminder: Priority matters(FIFO) for Advanced Settings AE Order and what a spell CD is.*| Y |

# Dots/Debuffs
> **Available Conditionals**
> | Ifs | MaxTries | CheckFor | CastIf | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | NoInterrupt
> [[More Info on Conditionals|3b)-Tag-Data]]

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Main=<br>Debuff on Assist= | Don't ask why they are built different. This applies to `main=` under [DoTs on Assist] and `Debuff on Assist=`. These will automatically fire off once they receive one of the assist commands. |
| Main=<br>Debuff on Command=| This applies to `main=` under [DoTs on Command] and `Debuff on Command=`. These are turned off by default, and allow for more control if you want. To toggle on and off use `/dot` to toggle for dots and `/debuff` for debuffs. | Y |

# Off Assist Spells
> **Available Conditionals**
> | Ifs | MaxTries | CheckFor | CastIf | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | NoInterrupt | MinDurationBeforeRecast
> [[More Info on Conditionals|3b)-Tag-Data]]

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Debuff on Assist=| These spells will be cast on every mob in the XTargets list when the bot receives an assist command. | Y |

# Dispel
| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Main= | Spell or Item you wish to use to dispell your current target. The looks for any benificals spells on the target. <br><br>**Example:**<br> `Main=Abashi's Rod of Disempowerment` |
| Ignore | Buffs on the target you wish to ignore from trying to debuff. Each bufff you wish to ignore is a new `ignore=` <br><br>**Example:**<br> `Ignore=Yaulp III` <br> `Ignore=Spirit of Wolf` |

# LifeSupport
> **Available Conditionals**
> | Ifs | HealPct | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | NoInterrupt

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Life Support= | This is top priority whe it comes to what to process first and it based on your characters health percentage. Use this for self heal, mitigation, imunnitity, or evasions. <br><br>**Example:**<br> `Life Support=Hymn of the Last Stand/HealPct\|30`<br> `Life Support=Shield of Notes/HealPct\|40` <br> `Life Support=Cazel's Distillate of Celestial Healing/HealPct\|80` |

# Rez
|Settings(Key)  | Default |Values  |Description | Has Slash Cmd |
|:--------|:----------------:|:---------------------:|:-----------|:-:|
| AutoRez= | Off | On/Off | If turn on will Rez in and out of combat | |
| Auto Rez Spells= | | | This is the spell to be used if `AutoRez=On`<br><br>**Example:**<br> `Auto Rez Spells=Blessing of Resurrection` | |
| Rez Spells= | | | These are the spell that will be used for the slash commands. You can use multiple rez spells for ones with longer CD. <br><br>**Example:**<br> `Rez Spells=Blessing of Resurrection` <br> `Rez Spells=Resurrection` | Y |

# Burn
> **Available Conditionals**
> | Ifs | MaxTries | CheckFor | CastIf | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | NoInterrupt
> [[More Info on Conditionals|3b)-Tag-Data]]

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
|Quick Burn= | Will accept any spell or item and the concept is for "short" CD spells. Your preferace on what "short" is. | Y |
|Long Burn= | Will accept any spell or item and the concept is for "Long" CD spells. Your preferace on what "long" is. | Y |
|Full Burn= | The spells or items you want to use in a full send moment | Y |
| | **Example:**<br>`Quick Burn=Thunderkick Discipline/AfterEvent\|Union`<br>`Quick Burn=Zan Fi's Whistle`<br>`Long Burn=Innerflame Discipline`<br> `Full Burn=Speed Focus Discipline/AfterEvent\|Union`<br> `Full Burn=Zan Fi's Whistle`<br> `Full Burn=Thunderkick Discipline`<br> `Full Burn=Innerflame Discipline`| |

[Back to Top](#Misc)

# Pets
> **Available Conditionals**
> | Ifs | CheckFor | CastIf | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | HealPct | NoInterrupt
> [[More Info on Conditionals|3b)-Tag-Data]]

|Settings(Key)  | Default |Values  |Description | Has Slash Cmd |
|:--------|:----------------:|:---------------------:|:-----------|:-:|
| Pet Spell= | | String | The pet spell you wish to use for summoning | |
| Pet Heal= | | String | Spells you wish to use to heal you pet. <br><br>**Example:**<br> `Pet Heal=Healing of Mikkily/Gem\|1/HealPct\|55` |
| Pet Buff= | | String | Buff you wish for your pet to have. You can configure multiple `Pet Buff=` <br><br>**Example:**<br> `Pet Buff=Spirit of Irionu`<br> `Pet Buff=Growl of the Beast`||
| Pet Mend (Pct)= | | Pct | What percentage you want you character to use Mend AA. <br><br> **Example:**<br> `Pet Mend(Pct)=40` <br> This will trigger at 40% of pet's health | |
| Pet Taunt (On/Off)= | On | On/Off | Set's whether your pet taunts or not. | |
| Pet Auto-Shrink (On/Off)= | Off | On/Off | Will auto shrink your pet when summon or illusioned. | |
| Pet Summon Combat (On/Off)= | Off | On/Off | If your pet dies during combat will prioritize summonging your pet based on what is defined in `Pet Spell=` | |
| Pet Buff Combat (On/Off)= | On | On/Off | If a buff drops off during combat your bot will rebuff during combat. Good if you casting puma/leopard line spell in combat. | |

[Back to Top](#Misc)

# Cure
> **Available Conditionals**
> | CheckFor | MinSick | Zone | Gem
> [[More Info on Conditionals|3b)-Tag-Data]]

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| AutoRadiant (On/Off)= | Default is `On`. Will leverage your Radiant Cure AA if defined | |
| Cure= | Specify a cure to a particular debuff to a particular person *(Higher Prio(2))* <br><br>**Example:**<br> `Cure=Remove Greater Curse/Steeljacx/CheckFor\|Feeblemind/Gem\|12` <br> `Cure=Crusader's Touch/Metaljacx/CheckFor\|Ikaav's Venom` | |
| CureAll= | Specify a cure to a particular debuff to anyone in group *(Lower Prio(3))* <br><br>**Example:**<br> `CureAll=Remove Greater Curse/CheckFor\|Relinquish Spirit/Gem\|12` <br> `CureAll=Remove Greater Curse/CheckFor\|Torment of Body/Gem\|12` | |
| RadiantCure= | Specify a type of debuff to use radiant cure if at least this many people have it. *(Highest Prio(1))* <br><br>**Example:**<br> `RadiantCure=Fulmination/MinSick\|1/Zone\|txevu` <br> `RadiantCure=Fabled Destruction/MinSick\|1/Zone\|Unrest` | |
| CurseCounters= <br> PoisonCounters= <br> DiseaseCounters= <br> CorruptedCounters= | Cath All *(Lowest Prio (4))* Use spell(s) to try and cure if you see this type of debuff counter on a toon in group. <br><br>**Example:**<br> `CurseCounters=Remove Greater Curse` <br> `PoisonCounters=Blood of Nadox` <br> `DiseaseCounters=Blood of Nadox` | | This is only for catch all. If you see this debuff type with counter don't try and cure. <br><br>**Example:**<br> `PoisonCountersIgnore=Aura of Destruction` | |

[Back to Top](#Misc)

# Heal
> **Available Tags**
> | HealPct | Ifs | CheckFor | CastIf | BeforeSpell | AfterSpell | BeforeEvent | AfterEvent | Reagent | NoInterrupt
> [[More Info on Conditionals|3b)-Tag-Data]]

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Who to Heal= | **Default:** Tanks/ImportantBots/XTargets/Pets/Party<br> Defines which key you would like to be heal. This allows for quick on and off without having to comment lines out. | |
| Who to HoT= | Same as `Who to Heal=` just for `Heal Over Time Spell=` define key <br><br>**Example:**<br> `Who to HoT=Tanks` | |
| Tank= | Define who your tank/tanks will be. The bots define here will recieve the highest priority for heals. <br>***Must be a part of your bot network.*** <br><br>**Example:**<br> `Tank=Metaljacx` | |
| Tank Heal= | Heal spell/item/aa you wish to use on your tanks. Recommend you order in the order from lowest `/healPct\|10` to the highest `/HealPct\|90`. One exception you might see is for the spells like Reptile. <br><br>**Example:**<br> `Tank Heal=Artifact of the Reptile/HealPct\|100/CheckFor\|Skin of the Reptile`<br> `Tank Heal=Aged Dragon Spine Staff/HealPct\|50/NoInterrupt` <br> `Tank Heal=Mask of the Ancients/HealPct\|60/NoInterrupt` <br> `Tank Heal=Chlorotrope/Gem\|1/HealPct\|85/NoInterrupt` |
| Important Bot= | Define which bots you would like to pay close attention too just behind the tank priority. In essance "second priority". Alot use for other healers, offtanks, or high threat classes.<br>***Must be a part of your bot network.*** <br><br>**Example:**<br> `Important Bot=Orihime` <br> `Important Bot=Rukia` <br> `Important Bot=Mayuri` <br>|
| Important Heal= | Heal spell/item/aa you wish to use on your important bots.<br><br>**Example:**<br> `Important Heal=Chlorotrope/Gem\|1/HealPct\|65` |
| Group Heal= | This is not based on individual group members, but on the average missing health of the group. There is a minimal number required to be injured which can be control with next explain setting. <br><br>**Example:**<br> `Group Heal=Wave of Marr/Gem\|10/HealPct\|20/NoInterrupt` <br> `Group Heal=Wave of Trushar/HealPct\|40/NoInterrupt` <br> `Group Heal=Healing Wave of Prexus/HealPct\|65` <br> `Group Heal=Wave of Life/HealPct\|70` | |
| Number Of Injured Members For Group Heal= | **Default:** 3 <br> This define how many people need to be injured before triggering average Group heal. | |
| Party Heal= | This heals your individual party members and heals based on your configured. Wether they are part of your bot network or not `/HealPct` tag. <br>***Heals outside your bot network.*** <br><br>**Example:**<br> `Party Heal=Touch of Piety/HealPct\|60` | |
| Heal Over Time Spell= |  The HoT spell/item/aa you wish to use for the groups defined in `Who to HoT=` <br>***Must be a part of your bot network.*** <br><br>**Example:**<br> `Heal Over Time Spell=Breath of Trushar/Gem\|9/HealPct\|95` | |
| All Heal= | Heals all bots part of you network whether bots are in your group or not. <br><br>**Example:**<br> `All Heal=Yoppa's Mending/Gem\|1/HealPct\|65` | |
| XTarget Heal= | How to heal individuals not part of your bot network and in your group. You will need to assign each player you want to heal to your xTarget Window. <br>***Heals outside your bot network.***<br><br>**Example:**<br> `XTarget=Chlorotrope/Gem\|1/HealPct\|65` | |
| Pet Owner= | The bot in your network which pet you would like to heal. <br><br>**Example:**<br> `Pet Owner=Mayuri` | |
| Pet Heal= | Heal spell/item/aa you wish to use on pets.<br><br> Example:  <br> `XTarget=Chlorotrope/Gem\|1/HealPct\|55` | |
| Emergency Heal= | Heal spell/item/aa that will be used immediatly (cancels other casts) when health drops below the threshold for the specified target.<br><br>  Example:  <br>`Emergy Heal=Burst of Life/Uguk/HealPct\|40` | |
| Emergency Group Heal= | Heal spell/item/aa that will be used immediatly (cancels other casts) when health of any character drops below the threshold.<br><br>  Example:  <br>`Emergy Group Heal=Divine Arbitration/HealPct\|40` | |
[Back to Top](#Misc)

# Bando Buff
You will need to make 3 Bandoliers in game this leverage this ability. 

| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| Enabled= | **Default:** On <br> Turn on and off | |
| BuffName= | The Buff Name on yourself you wish to monitor | |
| DebuffName= | The Debuff Name on target you wish to monitor | |
| PrimaryWithBuff= <br> SecondaryWithBuff= <br> PrimaryWithoutBuff= <br> SecondaryWithoutBuff= | The items you wish to have in your your equipment slots, with and without buff. These weapons need to match with/without your bandolier setup  | |
| BandoNameWithBuff= | The name of the bandolier when you have buff | |
| BandoNameWithoutBuff= | The name of the bandolier when you don't have buff | |
| BandoNameWithoutDeBuff= | When the mob doesn't have debuff. Should move back to `BandoNameWithBuff=` when debuff detected and you have your buff on | |

[Back to Top](#Misc)

# Events
| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| _EventName_= | Allows you to define arbitrary commands for your bot to perform. These commands must be triggered by a /BeforeEvent or /AfterEvent conditional, or by a line in the \[EventLoop\] section. You make up your own _EventName_ on the left side of the equal sign, and then on the right side you write the command that will be performed when the event is triggered. The command will be executed whenever the event is triggered by a /BeforeEvent or /AfterEvent conditional, or by a line in the \[EventLoop\] section.<br><br>Example:<br>`[Life Support]`<br>`Life Support=Divine Barrier/HealPct\|20/Gem\|7/AfterEvent\|TellGroupIUsedDivineBarrier`<br><br>`[Events]`<br>`TellGroupIUsedDivineBarrier=/g I just used Divine Barrier!` | |

[Back to Top](#Misc)

# EventLoop
| Settings(Key) | Description | Has Slash Cmd |
|:--------|:-----------|:--:|
| _EventName_= | Allows you to define conditions that will trigger lines from the \[Events\] section to execute. Lines in the \[EventLoop\] section are evaluated about once every second, and whenever one of the lines evaluates to True, its associated event is executed. On the left side of the equal sign you put the name of an event that's defined in your \[Events\] section, and on the right side you put the condition that you want to trigger the event and execute its command.<br><br>Example:<br>`[Events]`<br>`DropInvisCombat=/makemevisible`<br><br>`[EventLoop]`<br>`DropInvisCombat=(${Me.CombatState.Equal[Combat]} && ${Me.Invis})` | |

[Back to Top](#Misc)