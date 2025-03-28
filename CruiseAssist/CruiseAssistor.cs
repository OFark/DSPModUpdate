using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// https://docs.unity3d.com/ja/2018.4/Manual/ExecutionOrder.html

namespace tanu.CruiseAssist
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class CruiseAssistor : BaseUnityPlugin
    {
        public const string ModGuid = "OFark.CruiseAssistor";
        public const string ModName = "CruiseAssist";
        public const string ModVersion = "0.1.40";

        public static bool Enabled = true;
        public static bool MarkVisitedFlag = true;
        public static bool SelectFocusFlag = false;
        public static bool HideDuplicateHistoryFlag = true;
        public static bool AutoDisableLockCursorFlag = false;
        public static StarData ReticuleTargetStar = null;
        public static PlanetData ReticuleTargetPlanet = null;
        public static StarData SelectTargetStar = null;
        public static PlanetData SelectTargetPlanet = null;
        public static int SelectTargetAstroId = 0;
        public static int SelectTargetEnemyId = 0;


        public static StarData TargetStar = null;
        public static PlanetData TargetPlanet = null;
        public static EnemyData? TargetEnemy = null;
        public static CruiseAssistState State = CruiseAssistState.INACTIVE;

        public static List<int> History = new List<int>();
        public static List<int> Bookmark = new List<int>();

        public static Func<StarData, string> GetStarName = star => star.displayName;
        public static Func<PlanetData, string> GetPlanetName = planet => planet.displayName;

        private Harmony harmony;

        public void Awake()
        {
            LogManager.Logger = base.Logger;
            new CruiseAssistorConfigManager(base.Config);
            ConfigManager.CheckConfig(ConfigManager.Step.AWAKE);
            harmony = new Harmony($"{ModGuid}.Patch");
            harmony.PatchAll(typeof(Patch_GameMain));
            harmony.PatchAll(typeof(Patch_UISailPanel));
            harmony.PatchAll(typeof(Patch_UIStarmap));
            harmony.PatchAll(typeof(Patch_PlayerMoveSail));
        }

        public void OnDestroy()
        {
            harmony.UnpatchAll();
        }

        public void OnGUI()
        {
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null)
            {
                return;
            }
            var uiGame = UIRoot.instance.uiGame;
            if (!uiGame.guideComplete || uiGame.techTree.active || uiGame.escMenu.active || uiGame.globemap.active || uiGame.hideAllUI0 || uiGame.hideAllUI1)
            {
                return;
            }
            if (GameMain.mainPlayer.sailing || uiGame.starmap.active)
            {
                Check();

                CruiseAssistorMainUI.wIdx = uiGame.starmap.active ? 1 : 0;

                var scale = CruiseAssistorMainUI.Scale / 100.0f;

                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), Vector2.zero);

                CruiseAssistorMainUI.OnGUI();
                if (CruiseAssistorStarListUI.Show[CruiseAssistorMainUI.wIdx])
                {
                    CruiseAssistorStarListUI.OnGUI();
                }
                if (CruiseAssistorConfigUI.Show[CruiseAssistorMainUI.wIdx])
                {
                    CruiseAssistorConfigUI.OnGUI();
                }
                if (CruiseAssistorDebugUI.Show)
                {
                    CruiseAssistorDebugUI.OnGUI();
                }

                bool resetInputFlag = false;

                resetInputFlag = ResetInput(CruiseAssistorMainUI.Rect[CruiseAssistorMainUI.wIdx], scale);

                if (!resetInputFlag && CruiseAssistorStarListUI.Show[CruiseAssistorMainUI.wIdx])
                {
                    resetInputFlag = ResetInput(CruiseAssistorStarListUI.Rect[CruiseAssistorMainUI.wIdx], scale);
                }

                if (!resetInputFlag && CruiseAssistorConfigUI.Show[CruiseAssistorMainUI.wIdx])
                {
                    resetInputFlag = ResetInput(CruiseAssistorConfigUI.Rect[CruiseAssistorMainUI.wIdx], scale);
                }

                if (!resetInputFlag && CruiseAssistorDebugUI.Show)
                {
                    resetInputFlag = ResetInput(CruiseAssistorDebugUI.Rect, scale);
                }
            }
        }


        private void Check()
        {
            try
            {
                var astroId = GameMain.mainPlayer.navigation.indicatorAstroId;
                var enemyId = GameMain.mainPlayer.navigation.indicatorEnemyId;

                if (CruiseAssistor.SelectTargetAstroId != astroId)
                {
                    CruiseAssistor.SelectTargetAstroId = astroId;
                    if (astroId % 100 != 0)
                    {
                        CruiseAssistor.SelectTargetPlanet = GameMain.galaxy.PlanetById(astroId);
                        CruiseAssistor.SelectTargetStar = CruiseAssistor.SelectTargetPlanet.star;
                    }
                    else
                    {
                        CruiseAssistor.SelectTargetPlanet = null;
                        CruiseAssistor.SelectTargetStar = GameMain.galaxy.StarById(astroId / 100);
                    }
                }

                if (CruiseAssistor.SelectTargetEnemyId != enemyId)
                {
                    CruiseAssistor.SelectTargetEnemyId = enemyId;
                }

                if (GameMain.localPlanet != null)
                {
                    if (CruiseAssistor.History.Count == 0 || CruiseAssistor.History.Last() != GameMain.localPlanet.id)
                    {
                        if (CruiseAssistor.History.Count >= 128)
                        {
                            CruiseAssistor.History.RemoveAt(0);
                        }

                        CruiseAssistor.History.Add(GameMain.localPlanet.id);
                        ConfigManager.CheckConfig(ConfigManager.Step.STATE);
                    }
                }
            }
            catch (Exception error)
            {
                MyLogger.LogToGame(error.Message);
            }
        }

        private bool ResetInput(Rect rect, float scale)
        {
            var left = rect.xMin * scale;
            var right = rect.xMax * scale;
            var top = rect.yMin * scale;
            var bottom = rect.yMax * scale;
            var inputX = Input.mousePosition.x;
            var inputY = Screen.height - Input.mousePosition.y;
            if (left <= inputX && inputX <= right && top <= inputY && inputY <= bottom)
            {
                int[] zot = { 0, 1, 2 };
                if (zot.Any(Input.GetMouseButton) || Input.mouseScrollDelta.y != 0)
                {
                    Input.ResetInputAxes();
                    return true;
                }
            }
            return false;
        }
    }
}
