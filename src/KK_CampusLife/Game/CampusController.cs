using System;
using ActionGame;
using ExtensibleSaveFormat;
using KKAPI.MainGame;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>KKAPI hooks into the main game: save/load and the day cycle.</summary>
    internal sealed class CampusController : GameCustomFunctionController
    {
        private const string DataKey = "state";

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            try
            {
                var data = GetExtendedData();
                object json = null;
                if (data != null && data.data != null) data.data.TryGetValue(DataKey, out json);
                Campus.Load(json as string);
                CampusLifePlugin.Log.LogInfo("Campus Life state loaded (day " + Campus.S.Day + ", week " + Campus.S.Week + ").");
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
                Campus.Reset();
            }
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            try
            {
                if (!Campus.Loaded) return;
                var data = new PluginData { version = CampusState.CurrentVersion };
                data.data[DataKey] = Campus.S.ToJson();
                SetExtendedData(data);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
            }
        }

        protected override void OnNewGame()
        {
            try { Campus.Reset(); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            Safe(() => Campus.OnDayChange(day));
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            Safe(() => Campus.OnPeriodChange(period));
        }

        protected override void OnEnterNightMenu()
        {
            Safe(Campus.OnNightMenu);
        }

        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            Safe(GiftMenu.Close);
            Safe(PeepReturn.OnStartH);
            Weather.InH = true;
        }

        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            Safe(() =>
            {
                if (hFlag == null) return;
                Campus.OnHEnd(hFlag.lstHeroine, hFlag.isFreeH);
            });
            Safe(PeepReturn.OnEndH);
            Weather.InH = false;
        }

        private static void Safe(Action a)
        {
            try
            {
                // A save made before the mod was installed loads without our data: start fresh.
                if (!Campus.Loaded && G.HasGame) Campus.Load(null);
                a();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }
    }
}
