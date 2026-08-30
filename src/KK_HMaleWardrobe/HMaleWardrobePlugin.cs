using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Game;
using KKAPI;
using KKAPI.MainGame;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Joojkk.HMaleWardrobe
{
    /// <summary>
    /// Gives the male the same dress menu the girl has in H: the game's own dress icon is copied into the H menu bar
    /// (right after hers) together with its sub-menus (clothes, accessories, change outfit, outfit type), and every
    /// copied button does for him exactly what the original does for her. Changes only last for the H scene.
    /// Author: Joojkk.
    /// </summary>
    [BepInPlugin(GUID, "H Male Wardrobe", Version)]
    [BepInDependency(KoikatuAPI.GUID, "1.30.1")]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    public sealed class HMaleWardrobePlugin : BaseUnityPlugin
    {
        public const string GUID = "joojkk.hmalewardrobe";
        public const string Version = "2.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyboardShortcut> Key;

        private void Awake()
        {
            Log = Logger;
            Key = Config.Bind("General", "Open his dress menu", new KeyboardShortcut(KeyCode.F10),
                "Opens or closes the male dress menu in H (the same as clicking his dress icon in the H menu bar).");
            GameAPI.StartH += (sender, args) => StartCoroutine(Attach());
        }

        private static IEnumerator Attach()
        {
            // HSprite.Init runs a few frames after the H scene starts.
            for (var i = 0; i < 600; i++)
            {
                var hs = FindObjectOfType<HSprite>();
                if (hs != null && hs.categoryDress != null && hs.objBaseMainMenu != null && MaleDressMenu.MaleOf(hs) != null)
                {
                    if (hs.GetComponent<MaleDressMenu>() == null)
                    {
                        try { hs.gameObject.AddComponent<MaleDressMenu>(); }
                        catch (Exception e) { Log.LogError("Could not add the male dress menu: " + e); }
                    }
                    yield break;
                }
                yield return null;
            }
        }
    }

    /// <summary>The copied menu, living on the H scene's HSprite object (destroyed with it).</summary>
    internal sealed class MaleDressMenu : MonoBehaviour
    {
        private const float IconStep = 72f;

        private HSprite _hs;
        private GameObject _icon;
        private GameObject _dress;                    // copy of SubMenu/DressCategory
        private readonly GameObject[] _subs = new GameObject[3]; // copies of ClothGroup, AccessoryGroup, CoordinatGroup
        private HSceneSpriteCategory _dressCat, _cloth, _accessory, _accessoryAll;
        private bool _built;

        private static readonly FieldInfo MaleField = AccessTools.Field(typeof(HSprite), "male");

        public static ChaControl MaleOf(HSprite hs)
        {
            return MaleField != null ? MaleField.GetValue(hs) as ChaControl : null;
        }

        private ChaControl Male { get { return MaleOf(_hs); } }

        private void Start()
        {
            _hs = GetComponent<HSprite>();
            try { Build(); _built = true; }
            catch (Exception e) { HMaleWardrobePlugin.Log.LogError("Could not copy the dress menu for the male: " + e); }
        }

        // ---------------- copy ----------------

        private void Build()
        {
            var bar = _hs.objBaseMainMenu.transform as RectTransform;
            var femaleIcon = FindButton(bar, "OnMainMenu", 0);
            var femaleDress = _hs.categoryDress.gameObject;
            if (femaleIcon == null) throw new Exception("dress icon not found in the H menu bar");

            // 1. Icon: right after hers; everything to its right moves one slot and the bar grows by one slot.
            var iconRt = femaleIcon.transform as RectTransform;
            var x0 = iconRt.anchoredPosition.x;
            foreach (RectTransform child in bar)
                if (child != iconRt && child.anchoredPosition.x > x0 + 1f)
                    child.anchoredPosition += new Vector2(IconStep, 0f);
            GrowBar(bar, IconStep);
            _icon = Instantiate(femaleIcon.gameObject, bar, false);
            _icon.name = "DressMale";
            _icon.transform.SetSiblingIndex(femaleIcon.transform.GetSiblingIndex() + 1);
            ((RectTransform)_icon.transform).anchoredPosition = iconRt.anchoredPosition + new Vector2(IconStep, 0f);
            Rewire(_icon);

            // 2. Sub-menu column under his icon.
            _dress = Instantiate(femaleDress, femaleDress.transform.parent, false);
            _dress.name = "DressCategoryMale";
            _dress.SetActive(false);
            ((RectTransform)_dress.transform).anchoredPosition = ((RectTransform)femaleDress.transform).anchoredPosition + new Vector2(IconStep, 0f);
            _dressCat = _dress.GetComponent<HSceneSpriteCategory>();
            for (var k = 0; k < 3; k++)
            {
                var src = _hs.objMenuSubSubs[k];
                if (src == null) continue;
                var path = RelativePath(femaleDress.transform, src.transform);
                var t = path != null ? _dress.transform.Find(path) : null;
                if (t != null) { _subs[k] = t.gameObject; _subs[k].SetActive(false); }
            }
            _cloth = Twin(femaleDress, _hs.categoryCloth);
            _accessory = Twin(femaleDress, _hs.categoryAccessory);
            _accessoryAll = Twin(femaleDress, _hs.categoryAccessoryAll);
            Rewire(_dress);
            HMaleWardrobePlugin.Log.LogInfo("Male dress menu added to the H menu bar.");
        }

        /// <summary>Widens the menu bar background: the strip right of the icons is stretched (9-slice), the ends keep their shape.</summary>
        private static void GrowBar(RectTransform bar, float by)
        {
            var img = bar.GetComponent<Image>();
            if (img != null && img.sprite != null && img.type == Image.Type.Simple)
            {
                var sp = img.sprite;
                var w = sp.rect.width;
                // Left border covers the icon area, right border the closing tab; the plain strip between them stretches.
                var left = Mathf.Round(w * 0.6f);
                var right = Mathf.Round(w * 0.12f);
                var sliced = Sprite.Create(sp.texture, sp.rect, new Vector2(0f, 1f), sp.pixelsPerUnit, 0, SpriteMeshType.FullRect,
                    new Vector4(left, 0f, right, 0f));
                sliced.name = sp.name + "_male";
                img.sprite = sliced;
                img.type = Image.Type.Sliced;
                img.fillCenter = true;
            }
            bar.sizeDelta += new Vector2(by, 0f);
        }

        private HSceneSpriteCategory Twin(GameObject femaleRoot, HSceneSpriteCategory original)
        {
            if (original == null) return null;
            var path = RelativePath(femaleRoot.transform, original.transform);
            var t = path != null ? _dress.transform.Find(path) : null;
            return t != null ? t.GetComponent<HSceneSpriteCategory>() : null;
        }

        private static string RelativePath(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (; t != null && t != root; t = t.parent) parts.Insert(0, t.name);
            return t == root ? string.Join("/", parts.ToArray()) : null;
        }

        private static Button FindButton(Transform root, string method, int arg)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                for (var i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    if (b.onClick.GetPersistentMethodName(i) == method && Arg(b.onClick, i).Int == arg) return b;
            return null;
        }

        // ---------------- rewiring ----------------

        private struct CallArg { public int Int; public bool Bool; }

        private static CallArg Arg(UnityEventBase ev, int index)
        {
            var r = new CallArg();
            try
            {
                var group = Traverse.Create(ev).Field("m_PersistentCalls").GetValue();
                var calls = Traverse.Create(group).Field("m_Calls").GetValue() as IList;
                var args = Traverse.Create(calls[index]).Field("m_Arguments");
                r.Int = args.Field("m_IntArgument").GetValue<int>();
                r.Bool = args.Field("m_BoolArgument").GetValue<bool>();
            }
            catch (Exception e) { HMaleWardrobePlugin.Log.LogDebug("Reading a button argument failed: " + e.Message); }
            return r;
        }

        /// <summary>Every copied button gets the male version of the girl's action; the copied calls to HSprite are switched off.</summary>
        private void Rewire(GameObject root)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                var ev = b.onClick;
                Action action = null;
                for (var i = 0; i < ev.GetPersistentEventCount(); i++)
                {
                    if (!(ev.GetPersistentTarget(i) is HSprite)) continue;
                    var a = Arg(ev, i);
                    var m = ev.GetPersistentMethodName(i);
                    var mapped = Map(m, a);
                    ev.SetPersistentListenerState(i, UnityEventCallState.Off);
                    if (mapped != null) action = mapped;
                    else HMaleWardrobePlugin.Log.LogDebug("Copied button '" + b.name + "' (" + m + ") has no male version; disabled.");
                }
                if (action != null)
                {
                    var act = action;
                    b.onClick.AddListener(() =>
                    {
                        try { if (_hs != null && _hs.IsSpriteAciotn()) act(); }
                        catch (Exception e) { HMaleWardrobePlugin.Log.LogWarning(e.Message); }
                    });
                }
            }
        }

        private Action Map(string method, CallArg a)
        {
            switch (method)
            {
                case "OnMainMenu": return ToggleDress;
                case "OnSubMenu": return () => ToggleSub(a.Int);
                case "OnClickCloth": return () => Do(m => m.SetClothesStateNext(a.Int));
                case "OnClickAllCloth": return () => Do(m => m.SetClothesStateAll((byte)a.Int));
                case "OnClickAccessory": return () => Do(m => m.chaFile.status.showAccessory[a.Int] = !m.chaFile.status.showAccessory[a.Int]);
                case "OnClickAllAccessory": return () => Do(m => m.SetAccessoryStateAll(a.Bool));
                case "OnClickAllAccessoryGroup1": return () => Do(m => m.SetAccessoryStateCategory(0, a.Bool));
                case "OnClickAllAccessoryGroup2": return () => Do(m => m.SetAccessoryStateCategory(1, a.Bool));
                case "OnClickCoordinateChange": return () => Do(m => m.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType)a.Int, true));
                case "OnClothChange": return OpenOutfitWindow;
                default: return null;
            }
        }

        private void Do(Action<ChaControl> change)
        {
            var m = Male;
            if (m == null) return;
            change(m);
            Utils.Sound.Play(SystemSE.sel);
        }

        // ---------------- menu behaviour (same as HSprite.OnMainMenu / OnSubMenu / OnClothChange) ----------------

        private bool DressOpen { get { return _dress != null && _dress.activeSelf; } }

        public void ToggleDress()
        {
            if (_dress == null) return;
            if (DressOpen) { CloseAll(); return; }
            // Opening his menu closes the game's open menus, like picking another icon of the bar does.
            _hs.CloseAllMenu();
            _dress.SetActive(true);
            Refresh();
            Utils.Sound.Play(SystemSE.sel);
        }

        private void ToggleSub(int kind)
        {
            var sub = kind >= 0 && kind < _subs.Length ? _subs[kind] : null;
            if (sub == null) return;
            var anim = sub.GetComponent<Animator>();
            if (sub.activeSelf)
            {
                if (anim != null)
                {
                    anim.SetBool("on", false);
                    var beh = anim.GetBehaviour<MenuStateMachineBehaviour>();
                    if (beh != null) beh.obj = sub; else sub.SetActive(false);
                }
                else sub.SetActive(false);
            }
            else
            {
                sub.SetActive(true);
                for (var i = 0; i < _subs.Length; i++)
                    if (i != kind && _subs[i] != null) _subs[i].SetActive(false);
                if (anim != null) anim.SetBool("on", true);
                Refresh();
            }
            Utils.Sound.Play(SystemSE.sel);
        }

        private void OpenOutfitWindow()
        {
            var m = Male;
            var win = _hs.clothCusutomCtrl;
            if (m == null || win == null) return;
            win.chaCtrl = m;
            win.gameObject.SetActive(true);
            Utils.Sound.Play(SystemSE.window_o);
            CloseAll();
        }

        private void CloseAll()
        {
            foreach (var s in _subs) if (s != null) s.SetActive(false);
            if (_dress != null) _dress.SetActive(false);
        }

        /// <summary>Shows only the pieces he is wearing and names his accessories (HSprite.ClothProc / AccessoryProc).</summary>
        private void Refresh()
        {
            var m = Male;
            if (m == null) return;
            if (_cloth != null)
            {
                for (var i = 0; i < 7; i++) _cloth.SetActive(m.IsClothesStateKind(i), i);
                _cloth.SetActive(m.IsShoesStateKind(), 7);
            }
            if (_accessory != null)
            {
                for (var i = 0; i < 20; i++)
                {
                    var has = m.IsAccessory(i);
                    _accessory.SetActive(has, i);
                    if (!has) continue;
                    var info = m.objAccessory[i] != null ? m.objAccessory[i].GetComponent<ListInfoComponent>() : null;
                    var obj = _accessory.GetObject(i);
                    var text = obj != null ? obj.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                    if (info != null && text != null) text.text = info.data.Name;
                }
            }
            if (_accessoryAll != null)
                _accessoryAll.SetActive(m.GetAccessoryCategoryCount(0) == 0 || m.GetAccessoryCategoryCount(1) == 0, 2);
        }

        private void Update()
        {
            if (!_built || _hs == null) return;
            try
            {
                var m = Male;
                if (m != null && _dressCat != null)
                {
                    // Same rule the game uses for her: hide "Clothes" / "Accessories" when there is nothing to take off.
                    var clothes = 0;
                    for (var j = 0; j < 7; j++) if (m.IsClothesStateKind(j)) clothes++;
                    if (m.IsShoesStateKind()) clothes++;
                    var acc = 0;
                    for (var k = 0; k < 20; k++) if (m.IsAccessory(k)) acc++;
                    _dressCat.SetActive(clothes != 0, 0);
                    _dressCat.SetActive(acc != 0, 1);
                }
                if (DressOpen)
                {
                    // Another icon of the bar was picked, or the bar was folded: close his menu like the game closes hers.
                    var other = _hs.menuSub != null && Enumerable.Range(0, _hs.menuSub.GetCount()).Any(i => _hs.menuSub.GetActive(i));
                    if (other || !_hs.objBaseMainMenu.activeInHierarchy) CloseAll();
                    else if (_subs[0] != null && _subs[0].activeSelf) Refresh();
                }
                if (HMaleWardrobePlugin.Key.Value.IsDown() && _hs.IsSpriteAciotn())
                {
                    if (!_hs.objBaseMainMenu.activeInHierarchy) _hs.OnMainMenuOpenAndClose(false);
                    ToggleDress();
                }
            }
            catch (Exception e) { HMaleWardrobePlugin.Log.LogDebug(e.Message); }
        }
    }
}
