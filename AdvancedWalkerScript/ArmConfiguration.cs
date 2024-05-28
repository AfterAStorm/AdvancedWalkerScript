﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// Holds configuration information
        /// Each configuration has a numerical id starting at one (default)
        /// </summary>
        public struct ArmConfiguration
        {
            /*
             * 
             * Formatted as such:
             * 
             * [Arm]
             * ...
             * [Joint]
             * ...
             * 
             * */

            #region # - Properties

            public static readonly ArmConfiguration DEFAULT = Create();

            public int Id;

            private static MyIni ini;

            private int defaultValue;
            public bool Default => defaultValue <= 0;

            #endregion

            #region # - Methods

            public string ToCustomDataString()
            {
                ini.Clear();
                ini.Set("Arm", "na", false);
                ini.SetComment("Arm", "na", "tbd");

                ini.SetSectionComment("Arm", $"These are all the settings associated with this arm group (group {Id}),\nchanging these will change all the other joints in the same group");
                return ini.ToString();
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public static ArmConfiguration Parse(MyIni ini)
            {
                ArmConfiguration config = new ArmConfiguration
                {


                    defaultValue = 1
                };
                return config;
            }

            public static ArmConfiguration Parse(string iniData)
            {
                ini = ini ?? new MyIni();
                ini.Clear();
                bool parsed = ini.TryParse(iniData);
                //if (!parsed)
                //    return null;
                return Parse(ini);
            }

            public static ArmConfiguration Create()
            {
                return Parse("");
            }

            #endregion
        }
    }
}