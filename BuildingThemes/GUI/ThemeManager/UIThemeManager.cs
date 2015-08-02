﻿using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BuildingThemes.GUI
{
    public class UIThemeManager : UIPanel
    {
        private UITitleBar m_title;
        private UIBuildingFilter m_filter;
        private UIFastList m_themeSelection;
        private UIButton m_themeAdd;
        private UIButton m_themeRemove;
        private UIFastList m_buildingSelection;
        private UIButton m_includeAll;
        private UIButton m_includeNone;
        private UIBuildingPreview m_buildingPreview;
        private UIBuildingOptions m_buildingOptions;
        private UIButton m_cloneBuilding;

        private Dictionary<Configuration.Theme, List<BuildingItem>> m_themes = new Dictionary<Configuration.Theme,List<BuildingItem>>();
        private bool m_isDistrictThemesDirty = false;

        #region Constant values
        private const float LEFT_WIDTH = 250;
        private const float MIDDLE_WIDTH = 450;
        private const float RIGHT_WIDTH = 250;
        private const float HEIGHT = 550;
        private const float SPACING = 5;
        private const float TITLE_HEIGHT = 40;
        #endregion

        private static GameObject _gameObject;
        private static UIThemeManager _instance;

        public static UIThemeManager instance
        {
            get { return _instance; }
        }

        public Configuration.Theme selectedTheme
        {
            get { return m_themeSelection.selectedItem as Configuration.Theme; }
        }

        public UIBuildingPreview buildingPreview
        {
            get { return m_buildingPreview; }
        }

        public static void Initialize()
        {
            try
            {
                // Destroy the UI if already exists
                _gameObject = GameObject.Find("BuildingThemes");
                Destroy();

                // Creating our own gameObect, helps finding the UI in ModTools
                _gameObject = new GameObject("BuildingThemes");
                _gameObject.transform.parent = UIView.GetAView().transform;
                _instance = _gameObject.AddComponent<GUI.UIThemeManager>();
            }
            catch (Exception e)
            {
                // Catching any exception to not block the loading process of other mods
                Debugger.Log("Building Themes: An error has happened during the UI creation.");
                Debugger.LogException(e);
            }
        }

        public static void Destroy()
        {
            try
            {
                if (_gameObject != null)
                    GameObject.Destroy(_gameObject);
            }
            catch (Exception e)
            {
                // Catching any exception to not block the unloading process of other mods
                Debugger.Log("Building Themes: An error has happened during the UI destruction.");
                Debugger.LogException(e);
            }
        }

        public void Toggle()
        {
            if (isVisible)
            {
                Hide();
            }
            else
            {
                Show(true);

                if (m_themeSelection.selectedIndex == -1) m_themeSelection.selectedIndex = 0;
            }
        }

        public void CreateTheme(string name)
        {
            if (BuildingThemesManager.instance.GetThemeByName(name) == null)
            {
                Configuration.Theme newTheme = new Configuration.Theme(name);

                BuildingThemesManager.instance.Configuration.themes.Add(newTheme);
                m_isDistrictThemesDirty = true;

                InitBuildingLists();

                m_themeSelection.selectedIndex = -1;
                m_themeSelection.rowsData.m_buffer = m_themes.Keys.ToArray();
                m_themeSelection.rowsData.m_size = m_themeSelection.rowsData.m_buffer.Length;

                for (int i = 0; i < m_themeSelection.rowsData.m_buffer.Length; i++)
                {
                    if (m_themeSelection.rowsData.m_buffer[i] == newTheme)
                    {
                        m_themeSelection.DisplayAt(i);
                        m_themeSelection.selectedIndex = i;
                    }
                }

                ThemePolicyTab.RefreshThemesContainer();
            }
        }

        public void DeleteTheme(Configuration.Theme theme)
        {
            if (!theme.isBuiltIn)
            {
                BuildingThemesManager.instance.Configuration.themes.Remove(theme);
                m_isDistrictThemesDirty = true;

                InitBuildingLists();

                m_themeSelection.selectedIndex = -1;
                m_themeSelection.rowsData.m_buffer = m_themes.Keys.ToArray();
                m_themeSelection.rowsData.m_size = m_themeSelection.rowsData.m_buffer.Length;
                m_themeSelection.DisplayAt(0);
                m_themeSelection.selectedIndex = 0;

                ThemePolicyTab.RefreshThemesContainer();
            }
        }

        public void ChangeBuildingStatus(BuildingItem item, bool include)
        {
            if (include == item.included) return;

            if (item.building != null && item.building.isBuiltIn)
            {
                item.building.include = include;
            }
            else if (include)
            {
                Configuration.Building building = new Configuration.Building(item.name);

                if (!selectedTheme.containsBuilding(building.name))
                {
                    selectedTheme.buildings.Add(building);
                    item.building = building;
                }
            }
            else
            {
                Configuration.Building building = selectedTheme.getBuilding(item.name);
                if (building != null)
                    selectedTheme.buildings.Remove(building);

                item.building = null;
            }

            m_isDistrictThemesDirty = true;
            m_themeSelection.Refresh();
        }

        public void UpdateBuildingInfo(BuildingItem item)
        {
            m_buildingPreview.Show(item);
            m_buildingOptions.Show(item);
        }

        public BuildingItem GetBuildingItem(string name)
        {
            for(int i = 0; i< m_buildingSelection.rowsData.m_size; i++)
            {
                BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                if (item.name == name) return item;
            }

            return null;
        }

        public bool IsThemeValid(Configuration.Theme theme)
        {
            List<BuildingItem> list = m_themes[theme];

            foreach (BuildingItem item in list)
            {
                if (item.level == "L1" && item.included) return true;
            }
            return false;
        }

        public override void Update()
        {
            base.Update();

            if (m_isDistrictThemesDirty)
            {
                ThemePolicyTab.RefreshThemesContainer();
                BuildingThemesManager.instance.RefreshDistrictThemeInfos();
                BuildingThemesManager.instance.SaveConfig();
                m_isDistrictThemesDirty = false;
            }
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            width = SPACING + LEFT_WIDTH + SPACING + MIDDLE_WIDTH + SPACING + RIGHT_WIDTH + SPACING;
            height = TITLE_HEIGHT + HEIGHT + SPACING;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            InitBuildingLists();
            SetupControls();
        }

        private void SetupControls()
        {
            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Theme Manager";
            m_title.iconSprite = "ToolbarIconZoomOutCity";

            // Filter
            m_filter = AddUIComponent<UIBuildingFilter>();
            m_filter.width = width - SPACING * 2;
            m_filter.height = 70;
            m_filter.relativePosition = new Vector3(SPACING, TITLE_HEIGHT);

            m_filter.eventFilteringChanged += (c, i) =>
            {
                if (m_themeSelection != null && m_themeSelection.selectedIndex != -1)
                {
                    Configuration.Theme theme = m_themeSelection.selectedItem as Configuration.Theme;
                    m_buildingSelection.selectedIndex = -1;
                    m_buildingSelection.rowsData = Filter(m_themes[theme]);
                }
            };

            // Panels
            UIPanel left = AddUIComponent<UIPanel>();
            left.width = LEFT_WIDTH;
            left.height = HEIGHT - m_filter.height;
            left.relativePosition = new Vector3(SPACING, TITLE_HEIGHT + m_filter.height + SPACING);

            UIPanel middle = AddUIComponent<UIPanel>();
            middle.width = MIDDLE_WIDTH;
            middle.height = HEIGHT - m_filter.height;
            middle.relativePosition = new Vector3(LEFT_WIDTH + SPACING * 2, TITLE_HEIGHT + m_filter.height + SPACING);

            UIPanel right = AddUIComponent<UIPanel>();
            right.width = RIGHT_WIDTH;
            right.height = HEIGHT - m_filter.height;
            right.relativePosition = new Vector3(LEFT_WIDTH + MIDDLE_WIDTH + SPACING * 3, TITLE_HEIGHT + m_filter.height + SPACING);

            // Theme selection
            m_themeSelection = UIFastList.Create<UIThemeItem>(left);

            m_themeSelection.backgroundSprite = "UnlockingPanel";
            m_themeSelection.width = left.width;
            m_themeSelection.height = left.height - 40;
            m_themeSelection.canSelect = true;
            m_themeSelection.rowHeight = 40;
            m_themeSelection.autoHideScrollbar = true;
            m_themeSelection.relativePosition = Vector3.zero;

            m_themeSelection.rowsData.m_buffer = m_themes.Keys.ToArray();
            m_themeSelection.rowsData.m_size = m_themeSelection.rowsData.m_buffer.Length;
            m_themeSelection.DisplayAt(0);

            m_themeSelection.eventSelectedIndexChanged += (c, i) =>
            {
                if (i == -1) return;

                int listCount = m_buildingSelection.rowsData.m_size;
                float pos = m_buildingSelection.listPosition;

                Configuration.Theme theme = m_themeSelection.selectedItem as Configuration.Theme;
                m_buildingSelection.selectedIndex = -1;
                m_buildingSelection.rowsData = Filter(m_themes[theme]);

                if (m_filter.buildingStatus == Status.All && m_buildingSelection.rowsData.m_size == listCount)
                {
                    m_buildingSelection.DisplayAt(pos);
                }

                m_themeRemove.isEnabled = !((Configuration.Theme)m_themeSelection.selectedItem).isBuiltIn;
            };

            // Add theme
            m_themeAdd = UIUtils.CreateButton(left);
            m_themeAdd.width = (LEFT_WIDTH - SPACING) / 2;
            m_themeAdd.text = "New Theme";
            m_themeAdd.relativePosition = new Vector3(0, m_themeSelection.height + SPACING);

            m_themeAdd.eventClick += (c, p) =>
            {
                UIView.PushModal(UINewThemeModal.instance);
                UINewThemeModal.instance.Show(true);
            };

            // Remove theme
            m_themeRemove = UIUtils.CreateButton(left);
            m_themeRemove.width = (LEFT_WIDTH - SPACING) / 2;
            m_themeRemove.text = "Delete Theme";
            m_themeRemove.isEnabled = false;
            m_themeRemove.relativePosition = new Vector3(LEFT_WIDTH - m_themeRemove.width, m_themeSelection.height + SPACING);

            m_themeRemove.eventClick += (c, p) =>
            {
                ConfirmPanel.ShowModal("Delete Theme", "Are you sure you want to delete '" + selectedTheme.name + "' theme ?",
                    (d, i) => { if (i == 1) DeleteTheme(selectedTheme); });
            };

            // Building selection
            m_buildingSelection = UIFastList.Create<UIBuildingItem>(middle);

            m_buildingSelection.backgroundSprite = "UnlockingPanel";
            m_buildingSelection.width = middle.width;
            m_buildingSelection.height = middle.height - 40;
            m_buildingSelection.canSelect = true;
            m_buildingSelection.rowHeight = 40;
            m_buildingSelection.autoHideScrollbar = true;
            m_buildingSelection.relativePosition = Vector3.zero;

            m_buildingSelection.rowsData = new FastList<object>();

            BuildingItem selectedItem = null;
            m_buildingSelection.eventSelectedIndexChanged += (c, i) =>
            {
                selectedItem = m_buildingSelection.selectedItem as BuildingItem;
                m_cloneBuilding.isEnabled = selectedItem != null && selectedItem.prefab != null;
            };

            m_buildingSelection.eventMouseLeave += (c, p) =>
            {
                if (selectedItem != null)
                    UpdateBuildingInfo(selectedItem);
                else
                    UpdateBuildingInfo(null);
            };

            // Include buttons
            m_includeNone = UIUtils.CreateButton(middle);
            m_includeNone.width = 55;
            m_includeNone.text = "None";
            m_includeNone.relativePosition = new Vector3(MIDDLE_WIDTH - m_includeNone.width, m_buildingSelection.height + SPACING);

            m_includeAll = UIUtils.CreateButton(middle);
            m_includeAll.width = 55;
            m_includeAll.text = "All";
            m_includeAll.relativePosition = new Vector3(m_includeNone.relativePosition.x - m_includeAll.width - SPACING, m_buildingSelection.height + SPACING);

            UILabel include = middle.AddUIComponent<UILabel>();
            include.width = 100;
            include.padding = new RectOffset(0, 0, 8, 0);
            include.textScale = 0.8f;
            include.text = "Include:";
            include.relativePosition = new Vector3(m_includeAll.relativePosition.x - include.width - SPACING, m_buildingSelection.height + SPACING);

            m_includeAll.eventClick += (c, p) =>
            {
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                    if (item != null) ChangeBuildingStatus(item, true);
                }

                m_buildingSelection.Refresh();
            };

            m_includeNone.eventClick += (c, p) =>
            {
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                    if (item != null) ChangeBuildingStatus(item, false);
                }

                m_buildingSelection.Refresh();
            };

            // Preview
            m_buildingPreview = right.AddUIComponent<UIBuildingPreview>();
            m_buildingPreview.width = right.width;
            m_buildingPreview.height = (right.height - SPACING) / 2;
            m_buildingPreview.relativePosition = Vector3.zero;

            // Building Options
            m_buildingOptions = right.AddUIComponent<UIBuildingOptions>();
            m_buildingOptions.width = RIGHT_WIDTH;
            m_buildingOptions.height = (right.height - SPACING) / 2 - 40;
            m_buildingOptions.relativePosition = new Vector3(0, m_buildingPreview.height + SPACING);

            // Clone building
            m_cloneBuilding = UIUtils.CreateButton(right);
            m_cloneBuilding.width = RIGHT_WIDTH;
            m_cloneBuilding.height = 30;
            m_cloneBuilding.text = "Clone building";
            m_cloneBuilding.isEnabled = false;
            m_cloneBuilding.relativePosition = new Vector3(0, m_buildingOptions.relativePosition.y + m_buildingOptions.height + SPACING);

        }

        private void InitBuildingLists()
        {
            Configuration.Theme[] themes = BuildingThemesManager.instance.GetAllThemes().ToArray();
            Array.Sort(themes, ThemeCompare);

            m_themes.Clear();
            for (int i = 0; i < themes.Length; i++)
            {
                m_themes.Add(themes[i], GetBuildingItemList(themes[i]));
            }
        }

        private List<BuildingItem> GetBuildingItemList(Configuration.Theme theme)
        {
            // List of all growables prefabs
            Dictionary<string, BuildingItem> buildingDictionary = new Dictionary<string, BuildingItem>();
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.PrefabCount(); i++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                if (prefab != null && prefab.m_placementStyle == ItemClass.Placement.Automatic)
                {
                    BuildingItem item = new BuildingItem();
                    item.prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                    buildingDictionary.Add(item.name, item);
                }
            }

            // Combine growables with buildings in configuration
            List<BuildingItem> list = buildingDictionary.Values.ToList<BuildingItem>();

            Configuration.Building[] buildings = theme.buildings.ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildingDictionary.ContainsKey(buildings[i].name))
                {
                    // Associate building with prefab
                    BuildingItem item = buildingDictionary[buildings[i].name];
                    item.building = buildings[i];
                    // TODO: better fix
                    if (!item.building.include)
                    {
                        theme.isBuiltIn = true;
                        item.building.isBuiltIn = true;
                    }
                }
                else
                {
                    // Prefab not found, adding building without prefab
                    BuildingItem item = new BuildingItem();
                    item.building = buildings[i];
                    list.Add(item);
                }
            }

            // Sorting
            list.Sort(BuildingCompare);
            return list;
        }

        #region Filtering/Sorting
        private FastList<object> Filter(List<BuildingItem> list)
        {
            List<BuildingItem> filtered = new List<BuildingItem>();
            for (int i = 0; i < list.Count; i++)
            {
                BuildingItem item = (BuildingItem)list[i];
                bool prefabExists = item.prefab != null;

                // Origin
                if (m_filter.buildingOrigin == Origin.Default && item.isCustomAsset) continue;
                if (m_filter.buildingOrigin == Origin.Custom && !item.isCustomAsset) continue;

                // Status
                if (m_filter.buildingStatus == Status.Included && !item.included) continue;
                if (m_filter.buildingStatus == Status.Excluded && item.included) continue;

                // Level
                if (m_filter.buildingLevel != ItemClass.Level.None && !(prefabExists && item.prefab.m_class.m_level == m_filter.buildingLevel)) continue;

                // size
                Vector2 buildingSize = m_filter.buildingSize;
                if (m_filter.buildingSize != Vector2.zero && !(prefabExists && item.prefab.m_cellWidth == buildingSize.x && item.prefab.m_cellLength == buildingSize.y)) continue;

                // zone
                if (!m_filter.IsAllZoneSelected())
                {
                    Category category = item.category;
                    if (category == Category.None || !m_filter.IsZoneSelected(category)) continue;
                }
                // Name
                if (!m_filter.buildingName.IsNullOrWhiteSpace() && !item.name.ToLower().Contains(m_filter.buildingName.ToLower())) continue;

                filtered.Add(item);
            }

            list = filtered;

            FastList<object> fastList = new FastList<object>();
            fastList.m_buffer = list.ToArray();
            fastList.m_size = list.Count;

            return fastList;
        }

        private static int ThemeCompare(Configuration.Theme a, Configuration.Theme b)
        {
            // Sort by name
            return a.name.CompareTo(b.name);
        }

        private static int BuildingCompare(BuildingItem a, BuildingItem b)
        {
            // Sort by category > displayName > level > size > name
            int compare = (int)a.category - (int)b.category;
            if (compare == 0) compare = a.displayName.CompareTo(b.displayName);
            if (compare == 0) compare = a.level.CompareTo(b.level);
            if (compare == 0) compare = a.size.CompareTo(b.size);
            if (compare == 0) compare = a.name.CompareTo(b.name);

            return compare;
        }
        #endregion
    }
}

