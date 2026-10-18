
## References

[MOOER](https://www.mooeraudio.com/)  
[F15i Li Intelligent Amp](https://www.mooeraudio.com/products/230.html)   

[Downloads](https://www.mooeraudio.com/companyfile/Downloads-1)  
[Future Series Downloads](https://www.mooeraudio.com/Downloads_xq/8.html)   
MOOER_iAMP_V1.6.1_2026.06.26   
Mooer Studio For Future Series V1.1.9 Setup_win   

## Mooer Studio Backups   

The files are stored in this directory:    

```bash
C:\Users\Paul\Documents\Mooer Studio For Future Series\BackupFiles\F15i\
```

## iAMP Desktop App being developed   

The app being developed is available here:   

```bash
D:\A_A_Codex\rea_apk\artifacts\Iamp.Desktop\Iamp.Desktop.exe
```

## Presets

Has 80 Presets: 20 Groups x 4 presets

0D British Lead Solo - Volume: 6
  - Very close to Schenker sound for Lead + Riffs
  - Maybe a bit bassy?

6B Rock N Roll
  - Straight riff sound, no reverb
  
3D Cali MK3 Lead
  - Good lead with Reverb
  
5C Ballad Sonic
  - Nice clean Chorus


I believe it has a pitch shifter, how do I use it, do I add it as the very first effect?

I want to have these sounds:

1 - Basic straight riff sound (I think preset 6b is a bit to ditored or bassy)
2 - Lead sound with delay/Reverb

## TODO






Do you understand what I want?

**DONE**   

See screenshot, I want you to add a button that will show me in JSON format all the settings for the effects for the current preset.

It should work this way

- When I press up the button it will pop up a dialog
- In the dialog it wil have a a listview that shows the JSON and I can scroll through it
- The dialog should also have a copy icon that allow me to save the JSON to my clipboard
- The json format should follow a consistent structre that it will use for all presets


I am trying to restore from a backup I made in MOOER Studio but it fails with this error why, see screenshot

The backup file is here:

C:\Users\Paul\Documents\Mooer Studio For Future Series\BackupFiles\F15i\2026-10-13 18_34_47.backup

 I added another amp and it never showed up in the Desktop app. I closed and restarted the app and then it showed up, then I tried to delete it and the 'Remove effect' button became disabled but the newly added amp is still showing. When I closed and restated the app the new amp was gone then. These are bugs need fixing

Make the effect adjuster line half the width and also look and work the same way they do on the mobile iAMP app. Note that the set part uses the color of the effect icon

Cache the effect names so you don't have to read them every time I connect to the amplifier. Add a button that lets me refresh the effect names only if I want to.   

Add a reset button to allow me to reset the effect dropdown back to the default 'All effects' value.   

Add another filter so that I can search for Presets that are using a specific effect. I want that filter to be a dropdown that has a list of all the effects available in the iAMP app that I can choose from. 

Get Codex to add a version history in the app like I do for my Flutter apps.  



