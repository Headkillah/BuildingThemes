﻿using ColossalFramework;

namespace BuildingThemes
{
    public class DefaultFilteringStrategy : IFilteringStrategy
    {
        public bool DoesBuildingBelongToDistrict(string name, uint districtIdx)
        {
            return Singleton<BuildingThemesManager>.instance.DoesBuildingBelongToDistrict(name, districtIdx);
        }
    }
}