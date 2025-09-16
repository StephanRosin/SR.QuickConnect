using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace QuickConnect
{
    [HarmonyPatch]
    internal static class PatchConnectFailed
    {
        // Postfix, der immer gleich bleibt:
        public static void Postfix()
        {
            try
            {
                Mod.Log.LogInfo("[QuickConnect] Join failed (harmony hook)");
                QuickConnectUI.instance?.JoinServerFailed();
            }
            catch (Exception e)
            {
                Mod.Log.LogWarning($"[QuickConnect] Postfix error: {e}");
            }
        }

        // Dynamische Zielsuche
        static MethodBase TargetMethod()
        {
            try
            {
                var asm = typeof(ZNet).Assembly; // Spiel-Assembly
                var candidates = new[] { "ZSteamMatchmaking", "ZNet", "ZPlayFabMatchmaking" };

                // 1) Bevorzugt Typen, die wir kennen
                foreach (var typeName in candidates)
                {
                    var t = asm.GetTypes().FirstOrDefault(x => x.Name == typeName);
                    if (t == null) continue;

                    var m = AccessTools.GetDeclaredMethods(t)
                        .FirstOrDefault(mi =>
                            mi.ReturnType == typeof(void) &&
                            mi.Name.IndexOf("Join", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            mi.Name.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (m != null)
                    {
                        Mod.Log.LogInfo($"[QuickConnect] Hooking {t.FullName}.{m.Name}");
                        return m;
                    }
                }

                // 2) Letzter Fallback: global suchen (kostenlos bei Mod-Start ok)
                var global = asm.GetTypes()
                    .SelectMany(t => AccessTools.GetDeclaredMethods(t)
                        .Select(m => (t, m)))
                    .FirstOrDefault(tm =>
                        tm.m.ReturnType == typeof(void) &&
                        tm.m.Name.IndexOf("Join", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        tm.m.Name.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0);

                if (global.m != null)
                {
                    Mod.Log.LogInfo($"[QuickConnect] Hooking {global.t.FullName}.{global.m.Name}");
                    return global.m;
                }

                Mod.Log.LogWarning("[QuickConnect] No join-failed method found to patch. Falling back to timeout-only.");
                return null; // → Harmony patch wird einfach übersprungen
            }
            catch (Exception e)
            {
                Mod.Log.LogWarning($"[QuickConnect] TargetMethod() failure: {e}");
                return null;
            }
        }
    }
}
