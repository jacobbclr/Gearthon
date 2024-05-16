﻿using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using SmashHammer.GearBlocks.Construction;
using static SmashHammer.GearBlocks.Construction.PartPointGrid;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using System;

namespace Gearthon;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("GearLib")]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    private static string MODS_FOLDER = Path.Combine(Paths.PluginPath, "Gearthon", "mods");
    public static Dictionary<ulong, string> scripts = new Dictionary<ulong, string>();
    public static Dictionary<ulong, JArray> tweakables = new Dictionary<ulong, JArray>();
    
    public override void Load()
    {
        Log = base.Log;

        ClassInjector.RegisterTypeInIl2Cpp<ScriptBehaviour>();

        // If we don't have a mod folder, let's make it for the user
        if (!Directory.Exists(MODS_FOLDER))
        {
            Directory.CreateDirectory(MODS_FOLDER);
        }

        // Load custom parts
        foreach (string mod_folder_name in Directory.GetDirectories(MODS_FOLDER))
        {
            string mod_folder = Path.Combine(MODS_FOLDER, mod_folder_name);
            string content = File.ReadAllText(Path.Combine(mod_folder, "mod.json"));
            JObject mod = JObject.Parse(content);
            JArray parts = mod["parts"].Cast<JArray>();

            for (int i = 0; i < parts.Count; i++)
            {
                JToken part_data = parts[i].Cast<JToken>();
                ulong uid = (ulong)part_data["uid"];
                string script = (string)part_data["script"];
                // JArray behaviours = part_data["behaviours"].Cast<JArray>();

                // Add our modded part into the game
                GearLib.API.Part part = new GearLib.API.Part(
                    part_uid: uid, 
                    display_name: (string)part_data["display_name"],
                    category: (string)part_data["category"],
                    mass: (float)part_data["mass"],
                    asset_name: uid.ToString(),
                    asset_path: $"{mod_folder}/models",
                    is_paintable: (string)part_data["is_paintable"] == "True",
                    is_swappable_material: (string)part_data["is_swappable_material"] == "True",
                    mesh_collider: (string)part_data["mesh_collider"] == "True"
                );

                if (script != "")
                {
                    scripts[uid] = script;
                    tweakables[uid] = new JArray();
                    tweakables[uid].Merge(part_data["int_tweakables"].Cast<JArray>());
                    tweakables[uid].Merge(part_data["string_tweakables"].Cast<JArray>());
                    tweakables[uid].Merge(part_data["float_tweakables"].Cast<JArray>());
                    tweakables[uid].Merge(part_data["boolean_tweakables"].Cast<JArray>());
                    tweakables[uid].Merge(part_data["joystick_tweakables"].Cast<JArray>());
                    tweakables[uid].Merge(part_data["inputaction_tweakables"].Cast<JArray>());
                    ScriptBehaviour script2 = part.game_object.AddComponent<ScriptBehaviour>();
                    script2.name = uid.ToString();
                }

                // Temporarily disabled due to issues with replacing existing classes
                // For example, WheelBehaviour needs WheelPhysics. WheelPhysics can't be added w/ PartPhysics
                // for (int i2 = 0; i2 < behaviours.Count; i2++)
                // {
                //     string behaviour = (string)behaviours[i2].Cast<JToken>();
                //     part.game_object.AddComponent(Il2CppSystem.Type.GetType(behaviour));
                // }

                // Look over any attachments and add them on the part
                JArray attachments = part_data["attachments"].Cast<JArray>();
                for (int i2 = 0; i2 < attachments.Count; i2++)
                {
                    JToken attachment_data = attachments[i2].Cast<JToken>();
                    part.AddAttachmentPoint(
                        attachment_name: (string)attachment_data["name"],
                        attachment_flags: JTokenToTypeFlags(attachment_data["attachment_flags"].Cast<JArray>()),
                        alignment_flags: JTokenToAlignmentFlags(attachment_data["alignment_flags"].Cast<JArray>()),
                        position: JTokenToVector3(attachment_data["position"]),
                        orientation: JTokenToVector3(attachment_data["orientation"]),
                        size: JTokenToVector3Int(attachment_data["size"]),
                        pivot: (bool)attachment_data["pivot"]
                    );
                }
            }
        }
        
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    AttachmentTypeFlags JTokenToTypeFlags(JArray token)
    {
        AttachmentTypeFlags type = AttachmentTypeFlags.Null;
        if (token.ToString().Contains("linear_bearing"))
            type |= AttachmentTypeFlags.LinearBearing;
        if (token.ToString().Contains("rotary_bearing"))
            type |= AttachmentTypeFlags.RotaryBearing;
        if (token.ToString().Contains("constant_velocity_joint"))
            type |= AttachmentTypeFlags.ConstantVelocityJoint;
        if (token.ToString().Contains("fixed"))
            type |= AttachmentTypeFlags.Fixed;
        if (token.ToString().Contains("knuckle_joint"))
            type |= AttachmentTypeFlags.KnuckleJoint;
        if (token.ToString().Contains("linear_rotary_bearing"))
            type |= AttachmentTypeFlags.LinearRotaryBearing;
        if (token.ToString().Contains("spherical_bearing"))
            type |= AttachmentTypeFlags.SphericalBearing;

        type -= AttachmentTypeFlags.Null;
        return type;
    }

    AlignmentFlags JTokenToAlignmentFlags(JArray token)
    {
        AlignmentFlags type = AlignmentFlags.UNUSED;
        if (token.ToString().Contains("clamp_180"))
            type |= AlignmentFlags.Clamp180;
        if (token.ToString().Contains("clamp_90"))
            type |= AlignmentFlags.Clamp90;
        if (token.ToString().Contains("is_bidirectional"))
            type |= AlignmentFlags.IsBidirectional;
        if (token.ToString().Contains("is_female"))
            type |= AlignmentFlags.IsFemale;
        if (token.ToString().Contains("is_interior"))
            type |= AlignmentFlags.IsInterior;
        if (token.ToString().Contains("is_part_pairing_limited"))
            type |= AlignmentFlags.IsPartPairingLimited;

        type -= AlignmentFlags.UNUSED;
        return type;
    }

    Vector3 JTokenToVector3(JToken token)
    {
        Vector3 vector = new Vector3
        {
            x = (float)token["x"],
            y = (float)token["y"],
            z = (float)token["z"]
        };
        return vector;
    }

    Vector3Int JTokenToVector3Int(JToken token)
    {
        Vector3Int vector = new Vector3Int
        {
            x = (int)token["x"],
            y = (int)token["y"],
            z = (int)token["z"]
        };
        return vector;
    }
}