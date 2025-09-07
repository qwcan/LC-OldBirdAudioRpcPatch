using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace qwcan.patch;

/// <summary>
/// Patch to modify the behavior of the ship lights.
/// </summary>
[HarmonyPatch(typeof(RadMechAI))]
public class RadMechPatch
{
    /// <summary>
    /// Method called when mech is alerted to a threat.
    ///
    /// Check the link below for more information about Harmony patches.
    /// Class patches: https://github.com/BepInEx/HarmonyX/wiki/Class-patches
    /// Patch parameters: https://github.com/BepInEx/HarmonyX/wiki/Patch-parameters
    /// </summary>
    /// <param name="__instance">Instance that called the method.</param>
    /// <param name="__args">Arguments passed to the method.</param>
    /// <returns>True if the original method should be called, false otherwise.</returns>
    [HarmonyPatch(nameof(RadMechAI.SetMechAlertedToThreat))]
    [HarmonyPrefix]
    private static void OnSetMechAlertedToThreat(ref RadMechAI __instance, object[] __args)
    {
        Plugin.Log.LogInfo("Sending ClientRPC");
        if (!__instance.isAlerted)
        {
            __instance.SetMechAlertedClientRpc();
        }
    }
    
    // Insert code to play the sound in SetMechAlertedClientRpc()
    [HarmonyPatch(nameof(RadMechAI.SetMechAlertedClientRpc))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        Plugin.Log.LogInfo("Patching SetMechAlertedClientRpc");
        var code = new List<CodeInstruction>(instructions);
        var alertTimerField = AccessTools.Field(typeof(RadMechAI), "alertTimer");
        
        

        for (int i = 0; i < code.Count; i++)
        {
            var instr = code[i];
            yield return instr;

            if (instr.StoresField(alertTimerField))
            {
                // Inject LocalLRADAudio.Play() right after alertTimer is set
                // *Should* only be called in the client
                
                //Add a log statement
                /*
	            IL_0000: call class [BepInEx]BepInEx.Logging.ManualLogSource qwcan.Plugin::get_Log()
	            IL_0005: ldstr "Calling LocalLRADAudio.Play()"
	            IL_000a: callvirt instance void [BepInEx]BepInEx.Logging.ManualLogSource::LogInfo(object)
                 */
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Plugin), "get_Log"));
                yield return new CodeInstruction(OpCodes.Ldstr, "Calling LocalLRADAudio.Play()");
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ManualLogSource), "LogInfo", new[] {typeof(object)}));
                
                // Code to insert:
                /*
                IL_001b: ldarg.0
                IL_001c: ldfld class [UnityEngine.AudioModule]UnityEngine.AudioSource RadMechAI::LocalLRADAudio
                IL_0021: callvirt instance void [UnityEngine.AudioModule]UnityEngine.AudioSource::Play()
                */
                Plugin.Log.LogInfo("Found alertTimer ldfld, inserting call to LocalLRADAudio.Play()");
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RadMechAI), "LocalLRADAudio") );
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AudioSource), "Play"));
            }
        }
    }

}
