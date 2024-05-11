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
        Vector3 ProjectOnPlane(Vector3 a, Vector3 b)
        {
            Vector3 projection = (Vector3.Dot(a, b) / b.Length()) * (b / b.Length());
            return a - projection;
        }

        double AngleBetween(Vector3 a, Vector3 b)
        {
            return Math.Atan2(Vector3.Cross(a, b).Length(), Vector3.Dot(a, b));
        }

        /// <summary>
        /// Stablization handling
        /// </summary>
        /// <param name="steerValue">The current steer value, from A/D or Q/E input, for turning the mech's loewr half</param>
        void HandleStabilization(float steerValue)
        {
            //bool overrideEnabled = !GyroscopesDisableOverride || turnValue != 0;
            IMyShipController reference = cockpits.Count > 0 ? cockpits.First() : null;
            if (reference == null)
            {
                Log($"no cockpit");
                return;
            }
            Vector3D gravity = reference.GetTotalGravity();
            Vector3D up = reference.WorldMatrix.Up;
            Vector3D forward = reference.WorldMatrix.Forward.Normalized();
            Vector3D back = reference.WorldMatrix.Backward;
            Vector3D right = reference.WorldMatrix.Right;

            /*Vector3D gravityAlignedRight = Vector3D.Cross(gravity.Normalized(), -forward).Normalized();
            Vector3D gravityAlignedForward = Vector3D.Cross(gravity.Normalized(), gravityAlignedRight).Normalized();
            Vector3D gravityAlignedDown = Vector3D.Cross(gravityAlignedRight, forward).Normalized();

            double pitchDot = -Vector3D.Dot(gravityAlignedDown, gravityAlignedForward);
            double rollDot = -Vector3D.Dot(up, gravityAlignedRight);

            double pitch = Vector3D.Angle(forward, gravityAlignedForward) * Math.Sign(pitchDot);
            double roll = Vector3D.Angle(right, gravityAlignedRight) * Math.Sign(rollDot);*/

            Vector3D plane = forward - (Vector3D.Dot(forward, gravity) / gravity.Length()) * (gravity / gravity.Length());
            double pitch = AngleBetween(forward, ProjectOnPlane(forward, gravity.Normalized())); //Math.Atan2(Vector3D.Cross(forward, plane).Normalize(), Vector3.Dot(forward, plane));
            plane = right - (Vector3D.Dot(right, gravity) / gravity.Length()) * (gravity / gravity.Length());
            double roll = Math.Atan2(Vector3D.Cross(right, plane).Normalize(), Vector3.Dot(right, plane)) * Math.Sign(Vector3.Dot(right, gravity));

            pitch = pitch * Math.Sign(forward.Dot(gravity.Normalized()));

            Log($"pitch?: {pitch} >< {forward.Dot(gravity.Normalized())}");
            Log($"roll?: {roll}");


            /*Vector3D crossed = gravity.Normalized().Cross(forward);
            Vector3D rollCrossed = gravity.Normalized().Cross(up);
            double rollDirection = (rollCrossed.Y) * 6;
            Log($"crossed:");
            Log($"{rollCrossed.X}");
            Log($"{rollCrossed.Y}");
            Log($"{rollCrossed.Z}");*/

            double pitchDirection = pitch * 2;
            double rollDirection = roll * 2;

            Log($"roll dir: {rollDirection} fpr {rollStators.Count} rotors");
            Log($"pitc dir: {pitchDirection} fpr {elevationStators.Count} rotors");

            float azimuthValue = -steerValue * ((float)SteeringSensitivity / 60f) * 60f;
            bool isTurning = azimuthValue.Absolute() > 0;
            foreach (var stator in azimuthStators)
            {
                if (!stator.Stator.IsSharingInertiaTensor())
                    Warn($"Share Inertia Tensor", $"Share intertia tensor is disabled for azimuth/yaw stabilization rotor {stator.Stator.CustomName}, enable it for better results");
                foreach (var gyro in stator.SubGyros)
                    gyro.Enabled = isTurning;

                stator.SetRPM(azimuthValue * (float)stator.Configuration.InversedMultiplier);
            }

            float elevationValue = (float)pitchDirection * 60;
            foreach (var stator in elevationStators)
            {
                if (!stator.Stator.IsSharingInertiaTensor())
                    Warn($"Share Inertia Tensor", $"Share intertia tensor is disabled for elevation/pitch stabilization rotor {stator.Stator.CustomName}, enable it for better results");
                foreach (var gyro in stator.SubGyros)
                    gyro.Enabled = /*isTurning ?*/ elevationValue.Absolute() > 0.5f;// : true;

                stator.SetRPM(elevationValue * (float)stator.Configuration.InversedMultiplier);
                // TODO
            }

            float rollValue = (float)rollDirection * 60f;
            foreach (var stator in rollStators)
            {
                if (!stator.Stator.IsSharingInertiaTensor())
                    Warn($"Share Inertia Tensor", $"Share intertia tensor is disabled for roll stabilization rotor {stator.Stator.CustomName}, enable it for better results");
                foreach (var gyro in stator.SubGyros)
                    gyro.Enabled = /*isTurning ?*/ rollValue.Absolute() > 0.1f;// : true;

                stator.SetRPM(rollValue * (float)stator.Configuration.InversedMultiplier);
            }

            foreach (var gyro in stabilizationGyros)
            {
                if (gyro.GyroType == BlockType.GyroscopeStabilization || gyro.GyroType == BlockType.GyroscopeRoll)
                    gyro.Gyro.Roll = rollValue * (float)gyro.Configuration.InversedMultiplier;
                if (gyro.GyroType == BlockType.GyroscopeStabilization || gyro.GyroType == BlockType.GyroscopeAzimuth)
                    gyro.Gyro.Yaw = -azimuthValue * (float)gyro.Configuration.InversedMultiplier;
                if (gyro.GyroType == BlockType.GyroscopeStabilization || gyro.GyroType == BlockType.GyroscopeElevation)
                    gyro.Gyro.Pitch = elevationValue * (float)gyro.Configuration.InversedMultiplier;

                if (gyro.GyroType == BlockType.GyroscopeAzimuth && !isTurning)
                    gyro.Gyro.Enabled = false;
                else
                    gyro.Gyro.Enabled = /*isTurning ?*/
                        gyro.Gyro.Roll.Absolute() > 0.1f ||
                        gyro.Gyro.Yaw.Absolute() > 0.1f ||
                        gyro.Gyro.Pitch.Absolute() > 0.1f;// : true;
            }
        }
    }
}
