using System;
using System.Collections;
using ActionGame.H;
using Manager;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// After watching (peeping at) an H scene, the game takes the player out of that map through its nearest gate
    /// (ActionScene.ChangeHCommon), so he is not caught in a restroom or changing room. On a map that is no such
    /// place (the 3F corridor) that just dropped him in whatever room the nearest door led to (the traditional
    /// room). There he is put back where he stood.
    /// </summary>
    internal static class PeepReturn
    {
        private static int _map = -1;
        private static Vector3 _pos;
        private static Quaternion _rot;

        public static void OnStartH()
        {
            _map = -1;
            var act = G.Act;
            if (act == null || act.Map == null || act.Player == null || act.Map.Info == null || act.Map.Info.isWarning) return;
            var open = Singleton<Scene>.Instance.commonSpace.GetComponent<OpenHData>();
            var data = open != null ? open.data as OpenHData.Data : null;
            if (data == null || data.isDarkness || data.peepCategory == null || data.peepCategory.Count == 0) return;
            _map = act.Map.no;
            _pos = act.Player.position;
            _rot = act.Player.rotation;
            CampusLifePlugin.Log.LogDebug("Peeping on map " + _map + " (not a restroom or changing room): the player comes back here afterwards");
        }

        public static void OnEndH()
        {
            if (_map < 0) return;
            var map = _map;
            _map = -1;
            CampusLifePlugin.Instance.StartCoroutine(Return(map, _pos, _rot));
        }

        private static IEnumerator Return(int map, Vector3 pos, Quaternion rot)
        {
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 60f && !(G.IsFreeRoaming && G.Act != null && G.Act.Map != null && !G.Act.Map.isMapLoading)) yield return null;
            try
            {
                var act = G.Act;
                if (act == null || act.Player == null || !G.IsFreeRoaming || act.Player.mapNo == map) yield break;
                CampusLifePlugin.Log.LogInfo("After peeping: the game moved the player to map " + act.Player.mapNo + "; back to map " + map);
                // The position is set after the map loads (set in the callback, the door spawn overwrote it).
                act.Map.PlayerMapWarp(map, () => act.Player.StartCoroutine(StatusUi.PlaceAfterLoad(map, () => pos, () => rot)));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Peep return: " + e.Message); }
        }
    }
}
