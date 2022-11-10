using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace NoTrapFF
{
    [StaticConstructorOnStartup]
    public static class NoTrapFFMainLoader
    {
        public static bool staticEnablePrisonerFF = false;
        public static bool staticEnableSlaveFF = false;

        public static bool staticTrapsForAllAnimals = false;
        public static bool staticTrapEverythingElse = false;
        static NoTrapFFMainLoader()
        {
            //Log.Message("Animals Can Do Drugs startup success");

            var harmony = new Harmony("NoTrapFriendlyFire");
            harmony.PatchAll();
            Log.Message("[NoTrapFriendlyFire]patched harmony assembly. Startup success.");

        }
    }

    public class NoTrapFFSettings : ModSettings
    {
        public bool enablePrisonerFF = false;
        public bool enableSlaveFF = false;
        public bool trapsForAllAnimals = false;
        public bool trapEverythingElse = false;

        public override void ExposeData()
        {
        
            Scribe_Values.Look(ref enablePrisonerFF, "enablePrisonerFF", false, true);
            NoTrapFFMainLoader.staticEnablePrisonerFF = enablePrisonerFF;

            Scribe_Values.Look(ref enableSlaveFF, "enableSlaveFF", false, true);
            NoTrapFFMainLoader.staticEnableSlaveFF = enableSlaveFF;

            Scribe_Values.Look(ref enableSlaveFF, "enableSlaveFF", false, true);
            NoTrapFFMainLoader.staticEnableSlaveFF = enableSlaveFF;

            Scribe_Values.Look(ref enableSlaveFF, "enableSlaveFF", false, true);
            NoTrapFFMainLoader.staticEnableSlaveFF = enableSlaveFF;
            base.ExposeData();
        }

    }


    public class NoTrapFF : Mod
    {

        public static Vector2 scrollPosition;

        /// <summary>
        /// A reference to our settings.
        /// </summary>
        public NoTrapFFSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public NoTrapFF(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<NoTrapFFSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {

            Rect viewRect = new Rect(0f, 0f, inRect.width - 26f, inRect.height);
            Rect outRect = new Rect(0f, 30f, inRect.width, inRect.height - 30f);
            Listing_Standard listingStandard = new Listing_Standard();
            //Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            listingStandard.Begin(viewRect);
            listingStandard.Gap(24);        //add a gap so that it will look right
            listingStandard.CheckboxLabeled("enable trap friendly fire for prisoners of same faction", ref settings.enablePrisonerFF, "Enables or disables trap friendly fire when dealing with a prisoner of your own faction");
            listingStandard.CheckboxLabeled("enable trap friendly fire for slaves of same faction", ref settings.enableSlaveFF, "Enables or disables trap friendly fire when dealing with a slave of your own faction");
            listingStandard.GapLine(24);
            listingStandard.Label("the following settings only effect traps placed by the player");
            listingStandard.CheckboxLabeled("trap all factionless pawns and animals", ref settings.trapsForAllAnimals, "Checking this makes it so that any time a pawn (including humanlikes and normal animals) that doesn't belong to a faction touches a trap, the trap will activate regardless of if that pawn is in aggro or not");
            listingStandard.CheckboxLabeled("trap all non-player faction pawns of any kind", ref settings.trapEverythingElse, "Checking this makes it so that any time ANYTHING that doesn't belong to the player faction touches one of the player's traps, it will activate");
            listingStandard.Label("No restart is required for changes to take effect.");
            //Widgets.EndScrollView();
            listingStandard.End();

            NoTrapFFMainLoader.staticEnablePrisonerFF = settings.enablePrisonerFF;
            NoTrapFFMainLoader.staticEnableSlaveFF = settings.enableSlaveFF;
            NoTrapFFMainLoader.staticTrapsForAllAnimals = settings.trapsForAllAnimals;
            NoTrapFFMainLoader.staticTrapEverythingElse = settings.trapEverythingElse;

            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            //return "MyExampleModName".Translate();
            return "No Trap Friendly Fire";
        }

        //i'm making a function to return the mouseover description string since pasting this in the function in the CheckboxLavel in DoSettingsWindowContents would get too cluttered

    }

    //pretty similar to the way ED Enhanced does it
    [HarmonyPatch(typeof(Building_Trap), "CheckSpring")]
    static class harmonyPatchThatDisablesTrapFriendlyFire
    {
        static bool Prefix(Pawn p, ref Building_Trap __instance)
        {
            //it doesn't matter what you're doing, you ALWAYS have to check for nulls
            if (p == null)
            {
                return true;
            }

            //if pawn is a pawn type that doesnt have a faction or faction is otherwise invalid or absent, gtfo
            if (p.Faction == null && !NoTrapFFMainLoader.staticTrapsForAllAnimals)
            {
                return true;
            }
            else if (p.Faction == null && NoTrapFFMainLoader.staticTrapsForAllAnimals && __instance.Faction == Faction.OfPlayer)
            {
                //if this happens, there is probably an animal on the trap
                Log.Message("a pawn with no faction is on a trap. NoTrapFF configured setting of 'TrapsForAllAnimals = true' says to activate the trap right now.");
                __instance.Spring(p); //activate the trap now
                return false;
            }

            //the player faction is included as being friendly to itself. We can abuse this mechanic to easily figure out if the trap should be triggered or not based on this alone instead of a bigger, more grandiose buggy workaround
            //if (!FactionUtility.HostileTo(p.Faction, Faction.OfPlayer))
            if (__instance.Faction != null && __instance.Faction == p.Faction)
            {
                //a friendly unit is on trap. Return false to abort the usual CheckSpring() behavior, forcing the game to not trigger the trap regardless of what the random friendly fire chance says to do
                //if same faction AND is prisoner AND prisoner friendly fire enabled
                if (p.IsPrisoner && NoTrapFFMainLoader.staticEnablePrisonerFF)
                {
                    Log.Message("pawn is prisoner and prisoner friendly fire is enabled");
                    __instance.Spring(p);
                    return false;
                }

                if (p.IsSlave && NoTrapFFMainLoader.staticEnableSlaveFF)
                {
                    Log.Message("pawn is slave and slave friendly fire is enabled");
                    __instance.Spring(p);
                    return false;
                }

                return false;
            }
            else if (__instance.Faction != null && NoTrapFFMainLoader.staticTrapEverythingElse && __instance.Faction != p.Faction && __instance.Faction == Faction.OfPlayer)
            {
                //if trap everything is enabled and trap owner faction is different from pawn on trap, spring the trap
                Log.Message("trap everything enabled. Springing trap");
                __instance.Spring(p);
                return false;
            }

            //if none of the aboive criteria happened, continue to run the CheckSpring() method normally
            return true;

        }

        //static bool doTrappingIfSettingsSaySo()
        //{
        //    if (NoTrapFFMainLoader.)
        //}
    }

}
