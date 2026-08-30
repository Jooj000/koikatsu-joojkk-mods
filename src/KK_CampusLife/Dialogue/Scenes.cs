using System;
using System.Collections;
using System.Collections.Generic;
using ADV;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>Who says a line, for the optional TTS voice.</summary>
    internal sealed class VoiceTag
    {
        public string GirlKey = "";   // empty for player / narrator
        public bool IsPlayer;
        public bool IsNarrator;
    }

    /// <summary>
    /// Builds an ADV command list (the game's own text box) with narration, lines, choices and branches.
    /// </summary>
    internal sealed class SceneBuilder
    {
        public const string ChoiceVar = "cl_choice";
        public const string HerSpeaker = "[H名]";
        public const string PlayerSpeaker = "[P名]";

        public readonly List<Program.Transfer> List;
        public readonly Dictionary<string, VoiceTag> Voices = new Dictionary<string, VoiceTag>();
        public readonly SaveData.Heroine Her;
        private readonly bool _inTalk;
        private readonly HashSet<string> _registeredNames = new HashSet<string>();

        /// <param name="her">Main heroine (talk scenes) or null (map narration).</param>
        /// <param name="inTalk">True when played inside a TalkScene: [H名] then shows her name.</param>
        public SceneBuilder(SaveData.Heroine her, bool inTalk)
        {
            Her = her;
            _inTalk = inTalk;
            List = EventUtils.CreateNewEvent(!inTalk, true);
        }

        public SceneBuilder Narr(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            List.Add(Program.Transfer.Text(EventUtils.Narrator, text));
            Voices[text] = new VoiceTag { IsNarrator = true };
            return this;
        }

        public SceneBuilder Me(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            List.Add(Program.Transfer.Text(PlayerSpeaker, text));
            Voices[text] = new VoiceTag { IsPlayer = true };
            return this;
        }

        /// <summary>Line spoken by the main heroine.</summary>
        public SceneBuilder Her_(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            if (_inTalk) List.Add(Program.Transfer.Text(HerSpeaker, text));
            else return Say(Her, text);
            Voices[text] = new VoiceTag { GirlKey = GirlInfo.Key(Her) };
            return this;
        }

        /// <summary>Line spoken by any girl, shown with her English first name.</summary>
        public SceneBuilder Say(SaveData.Heroine girl, string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            if (girl == null) return Narr(text);
            if (_inTalk && girl == Her) return Her_(text);
            var name = GirlInfo.FirstName(girl);
            if (_registeredNames.Add(name))
                List.Add(Program.Transfer.Create(true, Command.FontColor, "Color2", name));
            List.Add(Program.Transfer.Text(name, text));
            Voices[text] = new VoiceTag { GirlKey = GirlInfo.Key(girl) };
            return this;
        }

        /// <summary>Shows a choice. Each option jumps to its tag; use Branch(tag) to start each branch.</summary>
        public SceneBuilder Choice(params string[] labelsAndTags)
        {
            var args = new List<string> { "true" };
            for (var i = 0; i + 1 < labelsAndTags.Length; i += 2)
                args.Add(labelsAndTags[i].Replace(",", " ") + "," + labelsAndTags[i + 1]);
            List.Add(Program.Transfer.Create(true, Command.Choice, args.ToArray()));
            return this;
        }

        /// <summary>Starts a branch: tag + records the choice in a scenario variable.</summary>
        public SceneBuilder Branch(string tag)
        {
            List.Add(Program.Transfer.Create(true, Command.Tag, tag));
            List.Add(Program.Transfer.VAR("string", ChoiceVar, tag));
            return this;
        }

        /// <summary>Fades the screen to black (true) or back (false), like the game's black-screen actions.</summary>
        public SceneBuilder FadeBlack(bool toBlack)
        {
            if (!toBlack)
            {
                List.Add(Program.Transfer.Create(false, Command.TextClear));
                List.Add(Program.Transfer.Create(false, Command.WindowActive, "FALSE", "0"));
            }
            List.Add(Program.Transfer.Create(false, Command.Fade, toBlack ? "in" : "out", "0.5", "black", "back", "TRUE"));
            return this;
        }

        public SceneBuilder End()
        {
            List.Add(Program.Transfer.Close());
            return this;
        }
    }

    /// <summary>Plays SceneBuilder lists in a talk scene or on the map, then reports the choice.</summary>
    internal static class Scenes
    {
        public static bool Running { get; private set; }
        public static Dictionary<string, VoiceTag> CurrentVoices = new Dictionary<string, VoiceTag>();
        private static float _startedAt;

        /// <summary>A multi-step scene (two girls) holds the map between its parts: other map scenes wait.</summary>
        public static object Owner;

        public static bool CanPlayOnMap { get { return !Running && Owner == null && G.IsFreeRoaming; } }

        private static string ReadChoice()
        {
            try
            {
                var act = G.Act;
                if (act == null || act.AdvScene == null || act.AdvScene.Scenario == null) return "";
                var vars = act.AdvScene.Scenario.Vars;
                ValData v;
                if (vars != null && vars.TryGetValue(SceneBuilder.ChoiceVar, out v) && v != null && v.o != null)
                {
                    var s = v.o.ToString();
                    vars.Remove(SceneBuilder.ChoiceVar);
                    return s;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
            return "";
        }

        private static IEnumerator Wrap(IEnumerator co, SceneBuilder b, Action<string> onDone)
        {
            Running = true;
            _startedAt = Time.realtimeSinceStartup;
            CurrentVoices = b.Voices;
            yield return co;
            var choice = ReadChoice();
            Running = false;
            CurrentVoices = new Dictionary<string, VoiceTag>();
            Tts.StopSpeaking();
            if (onDone != null)
            {
                try { onDone(choice); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            }
        }

        /// <summary>Play inside the current conversation (the girl stays on screen).</summary>
        public static bool PlayInTalk(SceneBuilder b, Action<string> onDone, bool useTalkTime = true)
        {
            var ts = G.TalkScene;
            if (ts == null || Running) return false;
            b.End();
            var co = EventUtils.StartTextSceneEvent(ts, b.List, false, useTalkTime);
            CampusLifePlugin.Instance.StartCoroutine(Wrap(co, b, onDone).PreventFromCrashing());
            return true;
        }

        /// <summary>Play on the map as dialogue-only narration.</summary>
        public static bool PlayOnMap(SceneBuilder b, Action<string> onDone, object owner = null)
        {
            if (Running || !G.IsFreeRoaming || Owner != null && Owner != owner) return false;
            var pl = EventUtils.GetPlayerData();
            if (pl == null || pl.transform == null) return false;
            b.End();
            var co = EventUtils.StartAdvEvent(b.List, true, pl.transform.position, pl.transform.rotation);
            CampusLifePlugin.Instance.StartCoroutine(Wrap(co, b, onDone).PreventFromCrashing());
            return true;
        }

        /// <summary>Safety net: if an event got stuck for minutes, release the flag.</summary>
        public static void Watchdog()
        {
            if (Running && Time.realtimeSinceStartup - _startedAt > 600f)
            {
                Running = false;
                CampusLifePlugin.Log.LogWarning("Scene watchdog released a stuck scene flag.");
            }
        }
    }
}
