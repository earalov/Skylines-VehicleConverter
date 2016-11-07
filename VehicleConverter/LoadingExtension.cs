﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using PrefabHook;
using UnityEngine;
using VehicleConverter.Config;

namespace VehicleConverter
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            Cache.Reset();
            if (!IsHooked())
            {
                return;
            }
            var isModActive = Util.IsModActive("Metro Overhaul");
            VehicleInfoHook.OnPreInitialization += info =>
            {
                try
                {
                    if (info.m_class.m_subService == ItemClass.SubService.PublicTransportTrain)
                    {
                        if (isModActive)
                        {
                            TrainToMetro.Convert(info);
                        }
                        TrainToTram.Convert(info);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            };
            VehicleInfoHook.Deploy();

            if (isModActive)
            {
                BuildingInfoHook.OnPreInitialization += info =>
                {
                    try
                    {
                        TrainStationToMetroStation.Convert(info);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                    }
                };
                BuildingInfoHook.Deploy();
            }
        }

        private static void ReleaseTrains()
        {
            var toRelease = new List<ushort>();
            for (int i = 0; i < TransportManager.instance.m_lines.m_buffer.Length; i++)
            {
                var line = TransportManager.instance.m_lines.m_buffer[i];
                if (line.m_flags == TransportLine.Flags.None || line.Info == null)
                {
                    continue;
                }
                if (line.Info.m_vehicleType != VehicleInfo.VehicleType.Train)
                {
                    continue;
                }
                int num1 = 0;
                if (line.m_vehicles != 0)
                {
                    var convertedIds = Trains.GetConvertedIds();
                    VehicleManager instance = VehicleManager.instance;
                    ushort num2 = line.m_vehicles;
                    int num3 = 0;
                    while (num2 != 0)
                    {
                        var vehicle = instance.m_vehicles.m_buffer[(int)num2];
                        long id;
                        if (Util.TryGetWorkshoId(vehicle.Info, out id) && convertedIds.Contains(id))
                        {
                            line.RemoveVehicle(num2, ref instance.m_vehicles.m_buffer[(int)num2]);
                            toRelease.Add(num2);
                        }
                        ushort num4 = vehicle.m_nextLineVehicle;
                        ++num1;
                        num2 = num4;
                        if (++num3 > VehicleManager.MAX_VEHICLE_COUNT)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core,
                                "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            foreach (var id in toRelease)
            {
                VehicleManager.instance.ReleaseVehicle(id);
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (!IsHooked())
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(
                    "Missing dependency",
                    "'VehicleConverter' mod requires the 'Prefab Hook' mod to work properly. Please subscribe to the mod and restart the game!",
                    false);
                return;
            }
            ReleaseTrains();
        }

        public override void OnReleased()
        {
            base.OnReleased();
            if (!IsHooked())
            {
                return;
            }
            VehicleInfoHook.Revert();
            BuildingInfoHook.Revert();
        }

        private static bool IsHooked()
        {
            return Util.IsModActive("Prefab Hook");
        }
    }
}