using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SkaldicSaga
{
    /// <summary>
    /// SkaldicSagaPlugin: A Valheim mod that procedurally generates humorous Norse-inspired poems
    /// in response to player actions such as deaths, cooking failures, and ship destruction.
    /// Uses BepInEx framework for mod initialization and Harmony for runtime method patching.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SkaldicSagaPlugin : BaseUnityPlugin
    {
        // --- PLUGIN METADATA ---
        public const string PluginGUID = "com.user.skaldicsaga";
        public const string PluginName = "SkaldicSagaLog";
        public const string PluginVersion = "1.0.0";

        // --- COOLDOWN SYSTEM ---
        /// <summary>
        /// Configuration entry that controls the minimum time interval (in minutes) between saga broadcasts.
        /// This prevents chat spam by enforcing a cooldown period after each poem is displayed.
        /// </summary>
        private static ConfigEntry<int> configCooldownMinutes;

        /// <summary>
        /// Tracks the timestamp of the last saga broadcast. Used to enforce the cooldown interval
        /// and prevent multiple poems from appearing in rapid succession.
        /// </summary>
        private static DateTime lastBroadcastTime = DateTime.MinValue;

        /// <summary>
        /// Random number generator used for selecting saga variants and individual poem lines.
        /// Uses Unity's Random class for consistent behavior in the game engine.
        /// </summary>
        private static readonly System.Random random = new System.Random();

        // --- POEM TEMPLATES ---
        /// <summary>
        /// Collection of death saga variants. A random variant is selected each time a player dies.
        /// Each template contains multiple possible lines per position, allowing for varied poem generation.
        /// </summary>
        private static readonly List<DeathSagaTemplate> deathSagas = new List<DeathSagaTemplate>
        {
            new DeathSagaTemplate(
                "The Fallen Warrior",
                new[] { "The mighty halls of Valhalla braced for a golden champion," },
                new[] { "As {PLAYER} journeyed deep into the shifting winds of the {BIOME}," },
                new[] { "But they miscalculated their stamina meter and perished miserably." },
                new[] { "The Valkyries checked their ledger and chose to look away." }
            ),
            new DeathSagaTemplate(
                "The Untimely Demise",
                new[] { "A warrior stood tall, their legend yet unwritten," },
                new[] { "When {PLAYER} ventured into the depths of {BIOME}," },
                new[] { "A creature emerged that they had not prepared to face." },
                new[] { "Now their name is sung only in songs of caution and regret." }
            ),
            new DeathSagaTemplate(
                "The Unexpected End",
                new[] { "Fortune favors the bold, or so the saying goes," },
                new[] { "But {PLAYER} tested fate's patience in the {BIOME}," },
                new[] { "And fate, it seems, had had quite enough of their hubris." },
                new[] { "Perhaps the next life will teach humility." }
            )
        };

        /// <summary>
        /// Collection of cooking failure saga variants. A random variant is selected each time food is overcooked.
        /// Each template contains multiple possible lines per position, allowing for varied poem generation.
        /// </summary>
        private static readonly List<CookingSagaTemplate> cookingSagas = new List<CookingSagaTemplate>
        {
            new CookingSagaTemplate(
                "The Culinary Catastrophe",
                new[] { "The grand hearths of the clan danced with pure, roaring flame," },
                new[] { "As {PLAYER} sought to nourish legends with a feast of unparalleled scale," },
                new[] { "They stared directly into the embers, forgot to press 'E', and created charcoal." },
                new[] { "The Allfather wept, his grand halls smelling faintly of burnt bacon." }
            ),
            new CookingSagaTemplate(
                "The Burnt Offering",
                new[] { "What should have been sustenance for mighty warriors," },
                new[] { "Became ash and smoke under {PLAYER}'s watchful gaze," },
                new[] { "They forgot a single keystroke, a moment's inattention." },
                new[] { "The gods have rejected this sacrifice most thoroughly." }
            ),
            new CookingSagaTemplate(
                "The Kitchen's Tragedy",
                new[] { "The cooking station glowed with promise and potential," },
                new[] { "{PLAYER} approached with confidence, though misplaced," },
                new[] { "Flames consumed the ingredients meant to fuel legendary deeds." },
                new[] { "Perhaps next time they will remember to tend the fire." }
            )
        };

        /// <summary>
        /// Collection of ship destruction saga variants. A random variant is selected each time a ship is destroyed.
        /// Each template contains multiple possible lines per position, allowing for varied poem generation.
        /// </summary>
        private static readonly List<ShipSagaTemplate> shipSagas = new List<ShipSagaTemplate>
        {
            new ShipSagaTemplate(
                "The Maritime Misfortune",
                new[] { "Behold a grand vessel engineered to master the raging Ocean boundaries," },
                new[] { "Carrying the glorious dreams and iron spirits of a proud, seafaring clan," },
                new[] { "It collided at maximum speed with a completely stationary sandbar." },
                new[] { "The ship's timbers split, drifting away alongside the captain's dignity." }
            ),
            new ShipSagaTemplate(
                "The Sailor's Shame",
                new[] { "A ship set forth upon the untamed seas of destiny," },
                new[] { "Its hull strong, its sails full, its course set with determination," },
                new[] { "Until it met the immovable obstacle that the captain somehow did not see." },
                new[] { "Perhaps future voyages will include better navigational awareness." }
            ),
            new ShipSagaTemplate(
                "The Sunken Dreams",
                new[] { "Crafted with care and launched with ceremony," },
                new[] { "A vessel sailed forth to explore the vast unknown waters," },
                new[] { "But the captain's attention wandered for just a moment too long." },
                new[] { "Now its wreckage settles upon the ocean floor, a monument to carelessness." }
            )
        };

        // --- INITIALIZATION ---
        /// <summary>
        /// Called when the plugin is first loaded. Initializes configuration settings and applies
        /// Harmony patches to hook into game events.
        /// </summary>
        private void Awake()
        {
            // Bind the cooldown configuration to the config file with a default of 15 minutes
            configCooldownMinutes = Config.Bind("General", "CooldownMinutes", 15, "Minimum minutes between saga broadcasts to prevent chat spam.");

            // Initialize Harmony patcher and apply all marked Harmony patches
            Harmony harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} version {PluginVersion} loaded successfully!");
        }

        // --- COOLDOWN MANAGEMENT ---
        /// <summary>
        /// Determines whether the cooldown period is still active, preventing saga broadcasts.
        /// </summary>
        /// <returns>True if cooldown is active (time elapsed is less than configured cooldown), false otherwise.</returns>
        public static bool IsCooldownActive()
        {
            return (DateTime.Now - lastBroadcastTime).TotalMinutes < configCooldownMinutes.Value;
        }

        // --- SAGA BROADCASTING ---
        /// <summary>
        /// Broadcasts a formatted saga (poem) to all players via the chat system. Applies color formatting
        /// for visual distinction: cyan for title, gold for epic lines, gray for punchline.
        /// Only broadcasts if cooldown is inactive and the calling instance is a server.
        /// </summary>
        /// <param name="title">The title of the saga displayed after "[The Skald: ".</param>
        /// <param name="epicLine1">First epic line of the poem (displayed in gold).</param>
        /// <param name="epicLine2">Second epic line of the poem (displayed in gold).</param>
        /// <param name="punchline">The comedic punchline or twist ending (displayed in gray).</param>
        /// <param name="epicLine3">Final epic line providing dramatic conclusion (displayed in gold).</param>
        public static void BroadcastSaga(string title, string epicLine1, string epicLine2, string punchline, string epicLine3)
        {
            // Exit early if cooldown is active or if this is not the server instance
            if (IsCooldownActive() || !ZNet.instance || !ZNet.instance.IsServer()) return;

            // Update the broadcast timestamp to start cooldown period
            lastBroadcastTime = DateTime.Now;

            // Format the poem with color codes for visual presentation
            string formattedPoem =
                $"<color=#00FFFF>[The Skald: {title}]</color>\n" +
                $"<color=#FFD700>{epicLine1}</color>\n" +
                $"<color=#FFD700>{epicLine2}</color>\n" +
                $"<color=#808080>{punchline}</color>\n" +
                $"<color=#FFD700>{epicLine3}</color>";

            // Send the formatted poem to all players via the routed RPC system
            // RPC parameters: position (0,0,0), message type (2 = normal chat), sender name, message content
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new object[]
            {
                new Vector3(0, 0, 0),
                2,
                "[The Skald]",
                formattedPoem
            });
        }

        // --- SAGA SELECTION ---
        /// <summary>
        /// Selects a random death saga template and populates it with player name and biome information.
        /// </summary>
        private static void BroadcastRandomDeathSaga(string playerName, string biome)
        {
            // Select a random saga variant from the death saga collection
            DeathSagaTemplate saga = deathSagas[random.Next(deathSagas.Count)];
            
            // Get random lines from each position and substitute player/biome placeholders
            string line1 = saga.GetRandomEpicLine1().Replace("{PLAYER}", playerName).Replace("{BIOME}", biome);
            string line2 = saga.GetRandomEpicLine2().Replace("{PLAYER}", playerName).Replace("{BIOME}", biome);
            string punchline = saga.GetRandomPunchline();
            string line3 = saga.GetRandomEpicLine3();

            BroadcastSaga(saga.title, line1, line2, punchline, line3);
        }

        /// <summary>
        /// Selects a random cooking saga template and populates it with player name information.
        /// </summary>
        private static void BroadcastRandomCookingSaga(string playerName)
        {
            // Select a random saga variant from the cooking saga collection
            CookingSagaTemplate saga = cookingSagas[random.Next(cookingSagas.Count)];

            // Get random lines from each position and substitute player placeholder
            string line1 = saga.GetRandomEpicLine1().Replace("{PLAYER}", playerName);
            string line2 = saga.GetRandomEpicLine2().Replace("{PLAYER}", playerName);
            string punchline = saga.GetRandomPunchline();
            string line3 = saga.GetRandomEpicLine3();

            BroadcastSaga(saga.title, line1, line2, punchline, line3);
        }

        /// <summary>
        /// Selects a random ship saga template and broadcasts it.
        /// </summary>
        private static void BroadcastRandomShipSaga()
        {
            // Select a random saga variant from the ship saga collection
            ShipSagaTemplate saga = shipSagas[random.Next(shipSagas.Count)];

            // Get random lines from each position
            string line1 = saga.GetRandomEpicLine1();
            string line2 = saga.GetRandomEpicLine2();
            string punchline = saga.GetRandomPunchline();
            string line3 = saga.GetRandomEpicLine3();

            BroadcastSaga(saga.title, line1, line2, punchline, line3);
        }

        // --- HARMONY PATCHES ---

        /// <summary>
        /// Harmony patch for Character.OnDeath event. Triggered when a player dies.
        /// Generates and broadcasts a humorous saga about the player's demise, including
        /// the player's name and their current biome at the time of death.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        static class Patch_Character_OnDeath
        {
            static void Prefix(Character __instance)
            {
                // Only execute on server and if not currently on cooldown
                if (!ZNet.instance || !ZNet.instance.IsServer()) return;
                if (!__instance.IsPlayer() || IsCooldownActive()) return;

                // Retrieve the player's hover name for personalization
                string playerName = __instance.GetHoverName();
                string biome = "Unknown Biome";

                // Safe cast to Player type to access biome tracking mechanics
                if (__instance is Player player)
                {
                    biome = player.GetCurrentBiome().ToString();
                }

                // Broadcast a random death saga with player name and biome context
                BroadcastRandomDeathSaga(playerName, biome);
            }
        }

        /// <summary>
        /// Harmony patch for CookingStation.RPC_OvercookItem event. Triggered when food is overcooked.
        /// Generates and broadcasts a humorous saga about culinary failure, including
        /// the player's name who caused the cooking catastrophe.
        /// </summary>
        [HarmonyPatch(typeof(CookingStation), "RPC_OvercookItem")]
        static class Patch_CookingStation_OvercookItem
        {
            static void Prefix(CookingStation __instance, long sender)
            {
                // Only execute on server and if not currently on cooldown
                if (!ZNet.instance || !ZNet.instance.IsServer() || IsCooldownActive()) return;

                // Retrieve the player name from network peer information
                ZNetPeer peer = ZNet.instance.GetPeer(sender);
                string playerName = peer != null ? peer.m_playerName : "A clumsy hero";

                // Broadcast a random cooking saga with player name context
                BroadcastRandomCookingSaga(playerName);
            }
        }

        /// <summary>
        /// Harmony patch for WearNTear.OnDestroyed event. Triggered when a ship is destroyed.
        /// Generates and broadcasts a humorous saga about maritime failure, but only if the
        /// destroyed object is actually a ship (has Ship component).
        /// </summary>
        [HarmonyPatch(typeof(WearNTear), "OnDestroyed")]
        static class Patch_Ship_OnDestroyed
        {
            static void Prefix(WearNTear __instance)
            {
                // Only execute on server and if not currently on cooldown
                if (!ZNet.instance || !ZNet.instance.IsServer() || IsCooldownActive()) return;

                // Retrieve the Ship component to verify this is actually a ship
                Ship shipComponent = __instance.GetComponent<Ship>();
                if (shipComponent != null)
                {
                    // Broadcast a random ship saga
                    BroadcastRandomShipSaga();
                }
            }
        }
    }

    // --- SAGA TEMPLATE CLASSES ---
    /// <summary>
    /// Base template class for formatting saga poems with placeholder substitution.
    /// Provides common functionality for retrieving random lines from saga variants.
    /// </summary>
    public abstract class SagaTemplate
    {
        /// <summary>Title of this saga variant.</summary>
        public string title;

        /// <summary>Collection of possible first epic lines.</summary>
        protected string[] epicLines1;

        /// <summary>Collection of possible second epic lines.</summary>
        protected string[] epicLines2;

        /// <summary>Collection of possible punchlines.</summary>
        protected string[] punchlines;

        /// <summary>Collection of possible final epic lines.</summary>
        protected string[] epicLines3;

        /// <summary>
        /// Initializes a new saga template with the specified title and line collections.
        /// </summary>
        protected SagaTemplate(string title, string[] epics1, string[] epics2, string[] punch, string[] epics3)
        {
            this.title = title;
            this.epicLines1 = epics1;
            this.epicLines2 = epics2;
            this.punchlines = punch;
            this.epicLines3 = epics3;
        }

        /// <summary>
        /// Returns a random line from the first epic line collection using System.Random.
        /// </summary>
        public string GetRandomEpicLine1()
        {
            return epicLines1[UnityEngine.Random.Range(0, epicLines1.Length)];
        }

        /// <summary>
        /// Returns a random line from the second epic line collection using System.Random.
        /// </summary>
        public string GetRandomEpicLine2()
        {
            return epicLines2[UnityEngine.Random.Range(0, epicLines2.Length)];
        }

        /// <summary>
        /// Returns a random line from the punchline collection using System.Random.
        /// </summary>
        public string GetRandomPunchline()
        {
            return punchlines[UnityEngine.Random.Range(0, punchlines.Length)];
        }

        /// <summary>
        /// Returns a random line from the final epic line collection using System.Random.
        /// </summary>
        public string GetRandomEpicLine3()
        {
            return epicLines3[UnityEngine.Random.Range(0, epicLines3.Length)];
        }
    }

    /// <summary>
    /// Death saga template containing variables for player name and biome.
    /// Inherits random line selection functionality from SagaTemplate base class.
    /// </summary>
    public class DeathSagaTemplate : SagaTemplate
    {
        /// <summary>
        /// Initializes a new death saga template with title and line collections.
        /// </summary>
        public DeathSagaTemplate(string title, string[] epics1, string[] epics2, string[] punch, string[] epics3)
            : base(title, epics1, epics2, punch, epics3) { }
    }

    /// <summary>
    /// Cooking saga template containing variables for player name.
    /// Inherits random line selection functionality from SagaTemplate base class.
    /// </summary>
    public class CookingSagaTemplate : SagaTemplate
    {
        /// <summary>
        /// Initializes a new cooking saga template with title and line collections.
        /// </summary>
        public CookingSagaTemplate(string title, string[] epics1, string[] epics2, string[] punch, string[] epics3)
            : base(title, epics1, epics2, punch, epics3) { }
    }

    /// <summary>
    /// Ship saga template for ship destruction events (no variable substitution needed).
    /// Inherits random line selection functionality from SagaTemplate base class.
    /// </summary>
    public class ShipSagaTemplate : SagaTemplate
    {
        /// <summary>
        /// Initializes a new ship saga template with title and line collections.
        /// </summary>
        public ShipSagaTemplate(string title, string[] epics1, string[] epics2, string[] punch, string[] epics3)
            : base(title, epics1, epics2, punch, epics3) { }
    }
}