﻿using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Tanukinomori
{
	[BepInPlugin(ModGuid, ModName, ModVersion)]
	public class CruiseAssist : BaseUnityPlugin
	{
		public const string ModGuid = "tanu.CruiseAssist";
		public const string ModName = "CruiseAssist";
		public const string ModVersion = "0.0.12";

		public static bool Enable = true;
		public static StarData ReticuleTargetStar = null;
		public static PlanetData ReticuleTargetPlanet = null;
		public static StarData SelectTargetStar = null;
		public static PlanetData SelectTargetPlanet = null;
		public static int SelectTargetAstroId = 0;
		public static StarData TargetStar = null;
		public static PlanetData TargetPlanet = null;
		public static CruiseAssistState State = CruiseAssistState.INACTIVE;
		public static bool TechTreeShow = false;

		public static List<int> History = new List<int>();

		public static Func<StarData, string> GetStarName = star => star.displayName;
		public static Func<PlanetData, string> GetPlanetName = planet => planet.displayName;

		public void Awake()
		{
			LogManager.Logger = base.Logger;
			new CruiseAssistConfigManager(base.Config);
			ConfigManager.CheckConfig(ConfigManager.Step.AWAKE);
			var harmony = new Harmony($"{ModGuid}.Patch");
			harmony.PatchAll(typeof(Patch_GameMain));
			harmony.PatchAll(typeof(Patch_UISailPanel));
			harmony.PatchAll(typeof(Patch_UITechTree));
			harmony.PatchAll(typeof(Patch_PlayerMoveSail));
		}

		public void OnGUI()
		{
			if (CruiseAssistMainUI.Show && !TechTreeShow)
			{
				CruiseAssistMainUI.OnGUI();
				if (CruiseAssistDebugUI.Show)
				{
					CruiseAssistDebugUI.OnGUI();
				}
				if (CruiseAssistStarListUI.Show)
				{
					CruiseAssistStarListUI.OnGUI();
				}
			}
		}
	}
}
