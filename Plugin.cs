using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Creative
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;
        public static ConfigEntry<KeyboardShortcut> finishKey;

        private void Awake()
        {
            finishKey = Config.Bind("General", "Finish Blueprint Key", new KeyboardShortcut(KeyCode.I), "Key to finish blueprints");

            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            Logger.LogInfo($"Patching...");
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Patched");
        }

        static bool isKeyPressed()
        {
            return finishKey.Value.IsDown();
        }

        [HarmonyPatch(typeof(CraftableObject), "Update")]
        [HarmonyPrefix]
        static void Update_Prefix(CraftableObject __instance)
        {
            if (finishKey.Value.IsDown())
            {
                __instance.CraftIngredient("", 1, true, null);
            }
        }

        [HarmonyPatch(typeof(CraftableObject), "CraftIngredient")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CraftIngredient_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
            .MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(CraftableObject), "m_bComplete"))
            );

            matcher.Advance(1).SetInstruction(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Plugin), "isKeyPressed"))
            );

            return matcher.InstructionEnumeration();
        }
    }
}
