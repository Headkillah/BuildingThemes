﻿using ColossalFramework;
using ColossalFramework.Math;
using System.Reflection;
using UnityEngine;

namespace BuildingThemes.Detour
{
    public class BuildingManagerDetour
    {
        private static bool deployed = false;

        private static RedirectCallsState _BuildingManager_GetRandomBuildingInfo_state;
        private static MethodInfo _BuildingManager_GetRandomBuildingInfo_original;
        private static MethodInfo _BuildingManager_GetRandomBuildingInfo_detour;

        private static MethodInfo _RefreshAreaBuidlings;
        private static MethodInfo _GetAreaIndex;

        private static FieldInfo _m_areaBuildings;

        // we'll use this variable to pass the building position to GetRandomBuildingInfo method
        public static Vector3 position;

        // this variables are used to indicate that the building is upgrading
        public static bool upgrade = false;
        public static ushort infoIndex;

        public static void Deploy()
        {
            if (!deployed)
            {
                _BuildingManager_GetRandomBuildingInfo_original = typeof(BuildingManager).GetMethod("GetRandomBuildingInfo", BindingFlags.Instance | BindingFlags.Public);
                _BuildingManager_GetRandomBuildingInfo_detour = typeof(BuildingManagerDetour).GetMethod("GetRandomBuildingInfo", BindingFlags.Instance | BindingFlags.Public);
                _BuildingManager_GetRandomBuildingInfo_state = RedirectionHelper.RedirectCalls(_BuildingManager_GetRandomBuildingInfo_original, _BuildingManager_GetRandomBuildingInfo_detour);

                _RefreshAreaBuidlings = typeof(BuildingManager).GetMethod("RefreshAreaBuildings", BindingFlags.NonPublic | BindingFlags.Instance);
                _GetAreaIndex = typeof(BuildingManager).GetMethod("GetAreaIndex", BindingFlags.NonPublic | BindingFlags.Static);
                _m_areaBuildings = typeof(BuildingManager).GetField("m_areaBuildings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                deployed = true;

                Debugger.Log("Building Themes: BuildingManager Methods detoured!");
            }
        }

        public static void Revert()
        {
            if (deployed)
            {
                RedirectionHelper.RevertRedirect(_BuildingManager_GetRandomBuildingInfo_original, _BuildingManager_GetRandomBuildingInfo_state);
                _BuildingManager_GetRandomBuildingInfo_original = null;
                _BuildingManager_GetRandomBuildingInfo_detour = null;

                _RefreshAreaBuidlings = null;
                _GetAreaIndex = null;
                _m_areaBuildings = null;

                deployed = false;

                Debugger.Log("Building Themes: BuildingManager Methods restored!");
            }
        }


        // Detour

        public BuildingInfo GetRandomBuildingInfo(ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {
            var buildingManager = Singleton<BuildingManager>.instance;

            var areaIndex = GetAreaIndex(service, subService, level, width, length, zoningMode);
            var districtId = (int)Singleton<DistrictManager>.instance.GetDistrict(position);

            // list of possible prefabs
            var fastList = Singleton<BuildingThemesManager>.instance.GetAreaBuildings(districtId, areaIndex);

            if (fastList == null || fastList.m_size == 0)
            {
                return (BuildingInfo)null;
            }

            // select a random prefab from the list
            int index = r.Int32((uint)fastList.m_size);
            var buildingInfo = PrefabCollection<BuildingInfo>.GetPrefab((uint)fastList.m_buffer[index]);

            return buildingInfo;
        }

        // This is just a copy of the method in BuildingManager for easy access
        private static int GetAreaIndex(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {
            int areaIndex;
            if (subService != ItemClass.SubService.None)
            {
                areaIndex = 8 + subService - ItemClass.SubService.ResidentialLow;
            }
            else
            {
                areaIndex = service - ItemClass.Service.Residential;
            }
            areaIndex = (int)(areaIndex * 5 + level);
            if (zoningMode == BuildingInfo.ZoningMode.CornerRight)
            {
                areaIndex = areaIndex * 4 + length - 1;
                areaIndex = areaIndex * 4 + width - 1;
                areaIndex = areaIndex * 2 + 1;
            }
            else
            {
                areaIndex = areaIndex * 4 + width - 1;
                areaIndex = areaIndex * 4 + length - 1;
                areaIndex = (int)(areaIndex * 2 + zoningMode);
            }
            return areaIndex;
        }
    }
}