using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class VehicleTurretDefaults
    {
        private static readonly ConditionalWeakTable<ThingComp, object> NewComponents =
            new ConditionalWeakTable<ThingComp, object>();
        private static readonly PropertyInfo Turrets;
        private static readonly PropertyInfo AutoTarget;
        private static readonly FieldInfo TurretDef;
        private static readonly FieldInfo TurretList;
        private static readonly FieldInfo AutoTargetActive;
        private static readonly MethodInfo CreateTurrets;
        private static readonly ConditionalWeakTable<ThingComp, object> WithoutTurrets =
            new ConditionalWeakTable<ThingComp, object>();

        static VehicleTurretDefaults()
        {
            Type comp = AccessTools.TypeByName("Vehicles.CompVehicleTurrets");
            Type turret = AccessTools.TypeByName("Vehicles.VehicleTurret");
            if (comp == null || turret == null) return;

            var generated = AccessTools.DeclaredMethod(comp, "PostGeneration");
            var spawned = AccessTools.DeclaredMethod(comp, "PostSpawnSetup", new[] { typeof(bool) });
            Turrets = AccessTools.Property(comp, "Turrets");
            AutoTarget = AccessTools.Property(turret, "AutoTarget");
            TurretDef = AccessTools.Field(turret, "def");
            if (generated == null || spawned == null || Turrets == null || AutoTarget == null ||
                !AutoTarget.CanWrite || TurretDef == null) return;

            var harmony = new Harmony("colonyproject.integrated.vehicleturretdefaults");
            harmony.Patch(generated, postfix: new HarmonyMethod(typeof(VehicleTurretDefaults), "TrackNewTurrets"));
            harmony.Patch(spawned, postfix: new HarmonyMethod(typeof(VehicleTurretDefaults), "ActivateNewTurrets"));

            var expose = AccessTools.DeclaredMethod(comp, "PostExposeData");
            CreateTurrets = AccessTools.DeclaredMethod(comp, "CreateTurretInstances");
            TurretList = AccessTools.Field(comp, "turrets");
            AutoTargetActive = AccessTools.Field(turret, "autoTargetingActive");
            if (expose == null || CreateTurrets == null || TurretList == null || AutoTargetActive == null) return;
            harmony.Patch(expose, prefix: new HarmonyMethod(typeof(VehicleTurretDefaults), "BeforeLoad"),
                postfix: new HarmonyMethod(typeof(VehicleTurretDefaults), "AfterLoad"));
        }

        /// A vehicle saved before its model got a turret has no turret data at all. Vehicle Framework then reports a missing
        /// reference list and leaves the vehicle without the turret for good.
        private static void BeforeLoad(ThingComp __instance)
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars || !IsOurs(__instance)) return;
            // The assembly is built without System.Xml: the node is reached by reflection.
            object parent = AccessTools.Field(typeof(ScribeLoader), "curXmlParent").GetValue(Scribe.loader);
            if (parent == null) return;
            if (Child(parent, "turrets") == null) WithoutTurrets.GetValue(__instance, key => new object());
            if (Child(parent, "turretQuotas") != null) return;
            object document = parent.GetType().GetProperty("OwnerDocument").GetValue(parent, null);
            object quotas = NewElement(document, "turretQuotas");
            Append(quotas, NewElement(document, "keys"));
            Append(quotas, NewElement(document, "values"));
            Append(parent, quotas);
        }

        private static object Child(object node, string name)
        {
            return node.GetType().GetProperty("Item", new[] { typeof(string) }).GetValue(node, new object[] { name });
        }

        private static object NewElement(object document, string name)
        {
            return document.GetType().GetMethod("CreateElement", new[] { typeof(string) }).Invoke(document, new object[] { name });
        }

        private static void Append(object node, object child)
        {
            foreach (MethodInfo method in node.GetType().GetMethods())
                if (method.Name == "AppendChild" && method.GetParameters().Length == 1) { method.Invoke(node, new[] { child }); return; }
        }

        private static void AfterLoad(ThingComp __instance)
        {
            if (Scribe.mode != LoadSaveMode.PostLoadInit || !WithoutTurrets.Remove(__instance)) return;
            var turrets = TurretList.GetValue(__instance) as IList;
            if (turrets == null || turrets.Count > 0) return;
            AddMissingRoles(__instance.parent);
            CreateTurrets.Invoke(__instance, null);
            foreach (object turret in turrets) if (StartsOn(turret)) AutoTargetActive.SetValue(turret, true);
        }

        /// The gunner's seat came with the turret. Without it Vehicle Framework takes the turret for one that needs no operator.
        private static void AddMissingRoles(ThingWithComps vehicle)
        {
            var handlers = AccessTools.Field(vehicle.GetType(), "handlers").GetValue(vehicle) as IList;
            object properties = AccessTools.Field(vehicle.def.GetType(), "properties").GetValue(vehicle.def);
            var roles = AccessTools.Field(properties.GetType(), "roles").GetValue(properties) as IList;
            Type handlerType = AccessTools.TypeByName("Vehicles.VehicleRoleHandler");
            if (handlers == null || roles == null || handlerType == null) return;
            foreach (object role in roles)
            {
                string key = KeyOf(role);
                bool present = false;
                foreach (object handler in handlers)
                    present |= KeyOf(AccessTools.Field(handlerType, "role").GetValue(handler)) == key ||
                        AccessTools.Field(handlerType, "roleKey") != null && (string)AccessTools.Field(handlerType, "roleKey").GetValue(handler) == key;
                if (!present) handlers.Add(Activator.CreateInstance(handlerType, new object[] { vehicle, role }));
            }
        }

        private static string KeyOf(object role)
        {
            return role == null ? null : (string)AccessTools.Field(role.GetType(), "key").GetValue(role);
        }

        private static bool IsOurs(ThingComp comp)
        {
            return comp.parent != null && comp.parent.def.defName.StartsWith("TMM_Vehicle_", StringComparison.Ordinal);
        }

        private static bool StartsOn(object turret)
        {
            var def = turret == null ? null : TurretDef.GetValue(turret) as Def;
            return def != null && (def.defName == "TMM_Turret_HunterLMG" || def.defName == "TMM_Turret_GunnerHMG" ||
                def.defName == "TMM_Turret_VertibirdDoorGun");
        }

        private static void TrackNewTurrets(ThingComp __instance)
        {
            if (IsOurs(__instance)) NewComponents.GetValue(__instance, key => new object());
        }

        private static void ActivateNewTurrets(ThingComp __instance, bool __0)
        {
            if (!NewComponents.Remove(__instance) || __0) return;
            var turrets = Turrets.GetValue(__instance, null) as IEnumerable;
            if (turrets == null) return;

            foreach (object turret in turrets) if (StartsOn(turret)) AutoTarget.SetValue(turret, true, null);
        }
    }
}
