using BepInEx.Configuration;
using System.Collections.Generic;

namespace tanu.CruiseAssist
{
	public class CruiseAssistorConfigManager : ConfigManager
	{
		public CruiseAssistorConfigManager(ConfigFile Config) : base(Config)
		{
		}

		protected override void CheckConfigImplements(Step step)
		{
			bool saveFlag = false;

			if (step == Step.AWAKE)
			{
				var modVersion = Bind<string>("Base", "ModVersion", CruiseAssistor.ModVersion, "Don't change.");
				modVersion.Value = CruiseAssistor.ModVersion;

				Migration("State", "MainWindow0Left", 100, "State", "InfoWindowLeft");
				Migration("State", "MainWindow0Top", 100, "State", "InfoWindowTop");
				Migration("State", "MainWindow0Left", 100, "State", "MainWindowLeft");
				Migration("State", "MainWindow0Top", 100, "State", "MainWindowTop");
				Migration("State", "StarListWindow0Left", 100, "State", "StarListWindowLeft");
				Migration("State", "StarListWindow0Top", 100, "State", "StarListWindowTop");

				saveFlag = true;
			}
			if (step == Step.AWAKE || step == Step.GAME_MAIN_BEGIN)
			{
				CruiseAssistorDebugUI.Show = Bind("Debug", "DebugWindowShow", false).Value;

				CruiseAssistor.Enabled = Bind("Setting", "Enable", true).Value;

				CruiseAssistor.MarkVisitedFlag = Bind("Setting", "MarkVisited", true).Value;
				CruiseAssistor.SelectFocusFlag = Bind("Setting", "SelectFocus", true).Value;
				CruiseAssistor.HideDuplicateHistoryFlag = Bind("Setting", "HideDuplicateHistory", true).Value;
				CruiseAssistor.AutoDisableLockCursorFlag = Bind("Setting", "AutoDisableLockCursor", false).Value;

				CruiseAssistorMainUI.Scale = (float)Bind("Setting", "UIScale", 150).Value;

				var viewModeStr = Bind("Setting", "MainWindowViewMode", CruiseAssistMainUIViewMode.FULL.ToString()).Value;
				EnumUtils.TryParse<CruiseAssistMainUIViewMode>(viewModeStr, out CruiseAssistorMainUI.ViewMode);

				for (int i = 0; i < 2; ++i)
				{
					CruiseAssistorMainUI.Rect[i].x = (float)Bind("State", $"MainWindow{i}Left", 100).Value;
					CruiseAssistorMainUI.Rect[i].y = (float)Bind("State", $"MainWindow{i}Top", 100).Value;
					CruiseAssistorStarListUI.Rect[i].x = (float)Bind("State", $"StarListWindow{i}Left", 100).Value;
					CruiseAssistorStarListUI.Rect[i].y = (float)Bind("State", $"StarListWindow{i}Top", 100).Value;
					CruiseAssistorConfigUI.Rect[i].x = (float)Bind("State", $"ConfigWindow{i}Left", 100).Value;
					CruiseAssistorConfigUI.Rect[i].y = (float)Bind("State", $"ConfigWindow{i}Top", 100).Value;
				}

				CruiseAssistorStarListUI.ListSelected = Bind("State", "StarListWindowListSelected", 0).Value;

				CruiseAssistorDebugUI.Rect.x = (float)Bind("State", "DebugWindowLeft", 100).Value;
				CruiseAssistorDebugUI.Rect.y = (float)Bind("State", "DebugWindowTop", 100).Value;

				if (!DSPGame.IsMenuDemo && GameMain.galaxy != null)
				{
					CruiseAssistor.History = ListUtils.ParseToIntList(Bind("Save", $"History_{GameMain.galaxy.seed}", "").Value);
					CruiseAssistor.Bookmark = ListUtils.ParseToIntList(Bind("Save", $"Bookmark_{GameMain.galaxy.seed}", "").Value);
				}
				else
				{
					CruiseAssistor.History = new List<int>();
					CruiseAssistor.Bookmark = new List<int>();
				}
			}
			else if (step == Step.STATE)
			{
				LogManager.LogInfo("check state.");

				saveFlag |= UpdateEntry("Setting", "Enable", CruiseAssistor.Enabled);

				saveFlag |= UpdateEntry("Setting", "MarkVisited", CruiseAssistor.MarkVisitedFlag);
				saveFlag |= UpdateEntry("Setting", "SelectFocus", CruiseAssistor.SelectFocusFlag);
				saveFlag |= UpdateEntry("Setting", "HideDuplicateHistory", CruiseAssistor.HideDuplicateHistoryFlag);
				saveFlag |= UpdateEntry("Setting", "AutoDisableLockCursor", CruiseAssistor.AutoDisableLockCursorFlag);

				saveFlag |= UpdateEntry("Setting", "UIScale", (int)CruiseAssistorMainUI.Scale);

				saveFlag |= UpdateEntry("Setting", "MainWindowViewMode", CruiseAssistorMainUI.ViewMode.ToString());

				for (int i = 0; i < 2; ++i)
				{
					saveFlag |= UpdateEntry("State", $"MainWindow{i}Left", (int)CruiseAssistorMainUI.Rect[i].x);
					saveFlag |= UpdateEntry("State", $"MainWindow{i}Top", (int)CruiseAssistorMainUI.Rect[i].y);
					saveFlag |= UpdateEntry("State", $"StarListWindow{i}Left", (int)CruiseAssistorStarListUI.Rect[i].x);
					saveFlag |= UpdateEntry("State", $"StarListWindow{i}Top", (int)CruiseAssistorStarListUI.Rect[i].y);
					saveFlag |= UpdateEntry("State", $"ConfigWindow{i}Left", (int)CruiseAssistorConfigUI.Rect[i].x);
					saveFlag |= UpdateEntry("State", $"ConfigWindow{i}Top", (int)CruiseAssistorConfigUI.Rect[i].y);
				}

				saveFlag |= UpdateEntry("State", "StarListWindowListSelected", CruiseAssistorStarListUI.ListSelected);

				saveFlag |= UpdateEntry("State", "DebugWindowLeft", (int)CruiseAssistorDebugUI.Rect.x);
				saveFlag |= UpdateEntry("State", "DebugWindowTop", (int)CruiseAssistorDebugUI.Rect.y);

				if (!DSPGame.IsMenuDemo && GameMain.galaxy != null)
				{
					if (!ContainsKey("Save", $"History_{GameMain.galaxy.seed}") || !ContainsKey("Save", $"Bookmark_{GameMain.galaxy.seed}"))
					{
						Bind("Save", $"History_{GameMain.galaxy.seed}", "");
						Bind("Save", $"Bookmark_{GameMain.galaxy.seed}", "");
						saveFlag = true;
					}
					saveFlag |= UpdateEntry("Save", $"History_{GameMain.galaxy.seed}", ListUtils.ToString(CruiseAssistor.History));
					saveFlag |= UpdateEntry("Save", $"Bookmark_{GameMain.galaxy.seed}", ListUtils.ToString(CruiseAssistor.Bookmark));
				}

				CruiseAssistorMainUI.NextCheckGameTick = long.MaxValue;
			}
			if (saveFlag)
			{
				Save(false);
			}
		}
	}
}
