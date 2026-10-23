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
        public const string PickPrefix = "cl_pick_";
        public const string HerSpeaker = "[H名]";
        public const string PlayerSpeaker = "[P名]";

        public readonly List<Program.Transfer> List;
        public readonly Dictionary<string, VoiceTag> Voices = new Dictionary<string, VoiceTag>();
        /// <summary>Text line -> background picture shown when that line appears (see <see cref="Bg"/>).</summary>
        public readonly Dictionary<string, string> Backgrounds = new Dictionary<string, string>();
        private string _pendingBg;
        /// <summary>Text line -> faces the girls make when that line appears (see <see cref="Face"/>).</summary>
        public readonly Dictionary<string, List<FaceCue>> Faces = new Dictionary<string, List<FaceCue>>();
        private List<FaceCue> _pendingFaces;
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

        /// <summary>
        /// The next line is shown over this painted background (an id from BepInEx\plugins\Joojkk\backgrounds, or "map"
        /// to see the map again). Missing pictures are skipped, so the scene still plays over the map.
        /// </summary>
        public SceneBuilder Bg(string id)
        {
            _pendingBg = string.IsNullOrEmpty(id) ? "map" : id;
            return this;
        }

        /// <summary>The girl makes this face from the next line on (staged scenes, where she stands on screen).</summary>
        public SceneBuilder Face(SaveData.Heroine girl, Feel feel)
        {
            if (girl == null) return this;
            if (_pendingFaces == null) _pendingFaces = new List<FaceCue>();
            _pendingFaces.Add(new FaceCue { GirlKey = GirlInfo.Key(girl), Feel = feel });
            return this;
        }

        private void Line(string text)
        {
            if (_pendingFaces != null)
            {
                List<FaceCue> l;
                if (!Faces.TryGetValue(text, out l)) Faces[text] = l = new List<FaceCue>();
                l.AddRange(_pendingFaces);
                _pendingFaces = null;
            }
            if (_pendingBg == null) return;
            if (!Backgrounds.ContainsKey(text)) Backgrounds[text] = _pendingBg;
            _pendingBg = null;
        }

        public SceneBuilder Narr(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            text = Gender.Fix(text, null, Her);
            Line(text);
            List.Add(Program.Transfer.Text(EventUtils.Narrator, text));
            Voices[text] = new VoiceTag { IsNarrator = true };
            return this;
        }

        public SceneBuilder Me(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            text = Gender.Fix(text, null, Her);
            Line(text);
            List.Add(Program.Transfer.Text(PlayerSpeaker, text));
            Voices[text] = new VoiceTag { IsPlayer = true };
            return this;
        }

        /// <summary>Line spoken by the main heroine.</summary>
        public SceneBuilder Her_(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            // In her own voice (fillers, exclaims, a stutter, her ~): every line of a talk, old or procedural.
            if (_inTalk && Her != null) text = Voice.Say(Her, Variants.Swap(Her, text));
            text = Gender.Fix(text, Her, Her);
            if (_inTalk) { Line(text); List.Add(Program.Transfer.Text(HerSpeaker, text)); }
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
            text = Gender.Fix(Variants.Swap(girl, text), girl, Her);
            var name = GirlInfo.FirstName(girl);
            if (_registeredNames.Add(name))
                List.Add(Program.Transfer.Create(true, Command.FontColor, "Color2", name));
            Line(text);
            List.Add(Program.Transfer.Text(name, text));
            Voices[text] = new VoiceTag { GirlKey = GirlInfo.Key(girl) };
            return this;
        }

        /// <summary>Shows a choice. Each option jumps to its tag; use Branch(tag) to start each branch.</summary>
        public SceneBuilder Choice(params string[] labelsAndTags)
        {
            var args = new List<string> { "true" };
            for (var i = 0; i + 1 < labelsAndTags.Length; i += 2)
                args.Add(Gender.Fix(labelsAndTags[i], null, Her).Replace(", ", " ").Replace(",", " ") + "," + labelsAndTags[i + 1]);
            // Not "multi", as the game's own choices (Communication.Info): a multi choice let the ADV run on into the first
            // branch's tag and variables before the click, so every first option counted as taken.
            List.Add(Program.Transfer.Create(false, Command.Choice, args.ToArray()));
            return this;
        }

        /// <summary>Starts a branch: tag + records the choice in a scenario variable.</summary>
        public SceneBuilder Branch(string tag)
        {
            List.Add(Program.Transfer.Create(true, Command.Tag, tag));
            List.Add(Program.Transfer.VAR("string", ChoiceVar, tag));
            List.Add(Program.Transfer.VAR("string", PickPrefix + tag, "1")); // every choice taken, for long scenes (Scenes.Picks)
            return this;
        }

        /// <summary>Continues at <see cref="Label"/>(tag): branches of a long scene meet again instead of ending.</summary>
        public SceneBuilder Goto(string tag)
        {
            // Not "multi": the Jump command already reads on from the tag by itself (TextScenario.Jump -> RequestNextLine).
            // As multi, the outer read went on too, past a choice right after the tag and into its first branch, so that
            // branch counted as taken as well.
            List.Add(Program.Transfer.Create(false, Command.Jump, tag));
            return this;
        }

        public SceneBuilder Label(string tag)
        {
            List.Add(Program.Transfer.Create(true, Command.Tag, tag));
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
        public static Dictionary<string, string> CurrentBackgrounds = new Dictionary<string, string>();
        public static Dictionary<string, List<FaceCue>> CurrentFaces = new Dictionary<string, List<FaceCue>>();

        /// <summary>The scene now playing is staged in the talk screen (girls on screen in front of the backdrop).</summary>
        public static bool Staged { get; private set; }

        /// <summary>Every branch tag taken in the scene that just ended (read it in the onDone callback).</summary>
        public static HashSet<string> Picks = new HashSet<string>();
        private static float _startedAt;

        /// <summary>A multi-step scene (two girls) holds the map between its parts: other map scenes wait.</summary>
        public static object Owner;

        public static bool CanPlayOnMap { get { return !Running && Owner == null && G.IsFreeRoaming; } }

        private static string ReadChoice()
        {
            Picks = new HashSet<string>();
            try
            {
                var act = G.Act;
                if (act == null || act.AdvScene == null || act.AdvScene.Scenario == null) return "";
                var vars = act.AdvScene.Scenario.Vars;
                if (vars != null)
                    foreach (var k in new List<string>(vars.Keys))
                        if (k.StartsWith(SceneBuilder.PickPrefix, StringComparison.Ordinal)) { Picks.Add(k.Substring(SceneBuilder.PickPrefix.Length)); vars.Remove(k); }
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

        private static IEnumerator Wrap(IEnumerator co, SceneBuilder b, Action<string> onDone, bool staged)
        {
            Running = true;
            Staged = staged;
            _startedAt = Time.realtimeSinceStartup;
            CurrentVoices = b.Voices;
            CurrentBackgrounds = b.Backgrounds;
            CurrentFaces = b.Faces;
            try { EventBackdrop.Preload(b.Backgrounds.Values); } catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
            yield return co;
            var choice = ReadChoice();
            Running = false;
            CurrentVoices = new Dictionary<string, VoiceTag>();
            CurrentBackgrounds = new Dictionary<string, string>();
            if (CurrentFaces.Count > 0) Expressions.ResetAll();
            CurrentFaces = new Dictionary<string, List<FaceCue>>();
            EventBackdrop.Hide();
            Staged = false;
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
            CampusLifePlugin.Instance.StartCoroutine(Wrap(co, b, onDone, true).PreventFromCrashing());
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
            CampusLifePlugin.Instance.StartCoroutine(Wrap(co, b, onDone, false).PreventFromCrashing());
            return true;
        }

        /// <summary>
        /// A scene to yield from inside one of the game's own event coroutines (the Sunday date, before the day moves on),
        /// opened the way the game opens its own event ADVs there (Cycle.EventADVStart: Program.Open, then Program.Wait).
        /// The list must not wait for the scene fade (SceneFadeRegulate False, like the game's own lists): inside the
        /// wake-up coroutine the fade only ends after that coroutine returns, so a regulated list waited for good and the
        /// game sat on the loading screen (user, 2026-10-11). Null if another scene is playing.
        /// </summary>
        public static IEnumerator PlayInline(SceneBuilder b, Action<string> onDone)
        {
            if (Running || G.Act == null) return null;
            b.End();
            if (b.List.Count > 0) b.List[0] = Program.Transfer.Create(true, Command.SceneFadeRegulate, bool.FalseString);
            return Wrap(OpenLikeGame(b), b, onDone, false).PreventFromCrashing();
        }

        private static IEnumerator OpenLikeGame(SceneBuilder b)
        {
            var act = G.Act;
            var opened = false;
            // The game's SimpleFade: Fade.In COVERS the screen (alpha 0 -> 1), Fade.Out uncovers it. The date ends with the
            // screen covered (white); our story opened under it and nothing ever uncovered it: the text ran behind a white
            // screen and the first choice waited for a click nobody could see (user, 2026-10-12, with the game's Skip on;
            // then uncover it; at the end cover again, so the game carries on from the state it left.
            var sc = Singleton<Scene>.IsInstance() ? Singleton<Scene>.Instance : null;
            if (sc != null && sc.sceneFade != null && sc.sceneFade._Fade != SimpleFade.Fade.In)
                yield return sc.Fade(SimpleFade.Fade.In, null);
            var heroines = new List<SaveData.Heroine>();
            if (b.Her != null) heroines.Add(b.Her);
            yield return Program.Open(new ADV.Data
            {
                fadeInTime = 0f,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                camera = null,
                heroineList = heroines,
                scene = act,
                transferList = b.List
            }, new Program.OpenDataProc { onLoad = () => opened = true });
            var t = Time.realtimeSinceStartup;
            while (!opened && Time.realtimeSinceStartup - t < 15f) yield return null;
            if (!opened) { CampusLifePlugin.Log.LogWarning("Inline scene: the ADV did not open; skipped."); yield break; }
            if (sc != null && sc.sceneFade != null)
            {
                CampusLifePlugin.Log.LogInfo("Inline scene: open; uncovering the screen (was " + sc.sceneFade._Fade + ", alpha " + sc.sceneFade._Color.a.ToString("0.00") + ").");
                sc.sceneFade.FadeSet(SimpleFade.Fade.Out, -1f, null);
            }
            var guard = new FadeGuardState();
            if (CampusLifePlugin.Instance != null) CampusLifePlugin.Instance.StartCoroutine(FadeGuard(guard));
            yield return Program.Wait(string.Empty);
            guard.Done = true;
            if (sc != null && sc.sceneFade != null)
            {
                yield return sc.Fade(SimpleFade.Fade.In, null);
                try { sc.SetFadeColorDefault(); } catch (Exception e) { CampusLifePlugin.Log.LogDebug("Inline scene fade color: " + e.Message); }
            }
        }

        private sealed class FadeGuardState { public bool Done; }

        /// <summary>
        /// While an inline story plays: if the screen stays covered by the scene fade (Fade.In finished) for 2 s, something left
        /// it covered over our text (the game's Skip, a fade command cut short); uncover it so the player can read and choose.
        /// </summary>
        private static IEnumerator FadeGuard(FadeGuardState st)
        {
            var since = -1f;
            while (!st.Done && Running)
            {
                try
                {
                    var sc = Singleton<Scene>.IsInstance() ? Singleton<Scene>.Instance : null;
                    var covered = sc != null && sc.sceneFade != null && sc.sceneFade._Fade == SimpleFade.Fade.In && sc.sceneFade.IsEnd && sc.sceneFade._Color.a > 0.5f;
                    if (!covered) since = -1f;
                    else if (since < 0f) since = Time.realtimeSinceStartup;
                    else if (Time.realtimeSinceStartup - since > 2f)
                    {
                        CampusLifePlugin.Log.LogWarning("Inline scene: the screen stayed faded over the story for 2 s (alpha " + sc.sceneFade._Color.a.ToString("0.00") + "); fading it in.");
                        sc.sceneFade.FadeSet(SimpleFade.Fade.Out, -1f, null);
                        since = -1f;
                    }
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Inline fade guard: " + e.Message); st.Done = true; }
                yield return null;
            }
        }

        /// <summary>Safety net: if an event got stuck for minutes, release the flag.</summary>
        public static void Watchdog()
        {
            if (Running && Time.realtimeSinceStartup - _startedAt > 600f)
            {
                Running = false;
                EventBackdrop.Hide();
                Staged = false;
                CampusLifePlugin.Log.LogWarning("Scene watchdog released a stuck scene flag.");
            }
        }
    }
}
