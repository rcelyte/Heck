﻿using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static NoodleExtensions.Plugin;

namespace NoodleExtensions.HarmonyPatches
{
    [NoodlePatch(typeof(ObstacleController))]
    [NoodlePatch("Init")]
    internal class ObstacleControllerInit
    {
        private static void Postfix(ObstacleController __instance, ObstacleData obstacleData, Quaternion ____worldRotation, ref float ____passedAvoidedMarkTime, ref float ____finishMovementTime)
        {
            if (obstacleData is CustomObstacleData customData)
            {
                dynamic dynData = customData.customData;
                IEnumerable<float> _localrot = ((List<object>)Trees.at(dynData, LOCALROTATION))?.Select(Convert.ToSingle);

                // oh my god im actually adding rotation
                Quaternion? localRotation = null;
                if (_localrot != null)
                {
                    Vector3 vector = new Vector3(_localrot.ElementAt(0), _localrot.ElementAt(1), _localrot.ElementAt(2));
                    localRotation = Quaternion.Euler(vector);
                    __instance.transform.Rotate(vector);
                }

                float? despawnTime = (float?)Trees.at(dynData, DESPAWNTIME);
                float? despawnDuration = (float?)Trees.at(dynData, DESPAWNDURATION);
                if (despawnTime.HasValue) ____passedAvoidedMarkTime = despawnTime.Value;
                if (despawnDuration.HasValue) ____finishMovementTime = ____passedAvoidedMarkTime + despawnDuration.Value;

                RotationData.savedRotation = ____worldRotation;

                List<object> varRotation = Trees.at(dynData, VARIABLEROTATION);
                if (varRotation != null)
                {
                    List<RotationData> rotationData = new List<RotationData>();
                    float dataTime = 0;
                    foreach (object n in varRotation)
                    {
                        IDictionary<string, object> dictData = n as IDictionary<string, object>;

                        IEnumerable<float> startrot = ((List<object>)Trees.at(dictData, "_startRotation"))?.Select(Convert.ToSingle);
                        IEnumerable<float> endrot = ((List<object>)Trees.at(dictData, "_endRotation"))?.Select(Convert.ToSingle);

                        float duration = (float)Trees.at(dictData, "_duration");
                        string easing = (string)Trees.at(dictData, "_easing");
                        rotationData.Add(new RotationData(dataTime, duration, startrot, endrot, easing));
                        dataTime += duration;
                    }
                    dynData.varRotation = rotationData;
                }

                RotationData.savedRotation = ____worldRotation * localRotation.GetValueOrDefault(Quaternion.identity);

                List<object> varLocalRotation = Trees.at(dynData, VARIABLELOCALROTATION);
                if (varLocalRotation != null)
                {
                    List<RotationData> rotationData = new List<RotationData>();
                    float dataTime = 0;
                    foreach (object n in varLocalRotation)
                    {
                        IDictionary<string, object> dictData = n as IDictionary<string, object>;

                        IEnumerable<float> startrot = ((List<object>)Trees.at(dictData, "_startRotation"))?.Select(Convert.ToSingle);
                        IEnumerable<float> endrot = ((List<object>)Trees.at(dictData, "_endRotation"))?.Select(Convert.ToSingle);

                        float duration = (float)Trees.at(dictData, "_duration");
                        string easing = (string)Trees.at(dictData, "_easing");
                        rotationData.Add(new RotationData(dataTime, duration, startrot, endrot, easing));
                        dataTime += duration;
                    }
                    dynData.varLocalRotation = rotationData;
                }
            }
        }

        private static readonly MethodInfo customWidth = SymbolExtensions.GetMethodInfo(() => GetCustomWidth(null, 0));
        private static readonly MethodInfo worldRotation = SymbolExtensions.GetMethodInfo(() => GetWorldRotation(null, 0));
        private static readonly MethodInfo inverseQuaternion = SymbolExtensions.GetMethodInfo(() => Quaternion.Inverse(Quaternion.identity));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool foundRotation = false;
            bool foundWidth = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (!foundRotation &&
                    instructionList[i].opcode == OpCodes.Stfld &&
                    ((FieldInfo)instructionList[i].operand).Name == "_worldRotation")
                {
                    foundRotation = true;

                    instructionList[i - 1] = new CodeInstruction(OpCodes.Call, worldRotation);
                    instructionList[i - 4] = new CodeInstruction(OpCodes.Ldarg_1);
                    instructionList.RemoveAt(i - 2);

                    instructionList.RemoveRange(i + 1, 2);
                    instructionList[i + 1] = new CodeInstruction(OpCodes.Ldarg_0);
                    instructionList[i + 2] = new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ObstacleController), "_worldRotation"));
                    instructionList[i + 3] = new CodeInstruction(OpCodes.Call, inverseQuaternion);
                }
                if (!foundWidth &&
                    instructionList[i].opcode == OpCodes.Callvirt &&
                    ((MethodInfo)instructionList[i].operand).Name == "get_width")
                {
                    foundWidth = true;
                    instructionList.Insert(i + 2, new CodeInstruction(OpCodes.Call, customWidth));
                    instructionList.Insert(i - 1, new CodeInstruction(OpCodes.Ldarg_1));
                }
            }
            if (!foundRotation) Logger.Log("Failed to find _worldRotation stfld, ping Aeroluna!", IPA.Logging.Logger.Level.Error);
            if (!foundWidth) Logger.Log("Failed to find get_width call, ping Aeroluna!", IPA.Logging.Logger.Level.Error);
            return instructionList.AsEnumerable();
        }

        private static Quaternion GetWorldRotation(ObstacleData obstacleData, float @default)
        {
            Quaternion _worldRotation = Quaternion.Euler(0, @default, 0);
            if (obstacleData is CustomObstacleData customData)
            {
                dynamic dynData = customData.customData;
                dynamic _rotation = Trees.at(dynData, ROTATION);

                if (_rotation != null)
                {
                    if (_rotation is List<object> list)
                    {
                        IEnumerable<float> _rot = (list)?.Select(Convert.ToSingle);
                        _worldRotation = Quaternion.Euler(_rot.ElementAt(0), _rot.ElementAt(1), _rot.ElementAt(2));
                    }
                    else _worldRotation = Quaternion.Euler(0, (float)_rotation, 0);
                }
            }
            return _worldRotation;
        }

        private static float GetCustomWidth(ObstacleData obstacleData, float @default)
        {
            if (obstacleData is CustomObstacleData customData)
            {
                dynamic dynData = customData.customData;
                IEnumerable<float?> _scale = ((List<object>)Trees.at(dynData, SCALE))?.Select(n => n.ToNullableFloat());
                float? _width = _scale?.ElementAtOrDefault(0);
                if (_width.HasValue) return _width.Value;
            }
            return @default;
        }
    }

    [NoodlePatch(typeof(ObstacleController))]
    [NoodlePatch("GetPosForTime")]
    internal class ObstacleControllerGetPosForTime
    {
        private static void Postfix(float time, ObstacleController __instance, ObstacleData ____obstacleData, ref Quaternion ____worldRotation,
            ref Quaternion ____inverseWorldRotation)
        {
            if (____obstacleData is CustomObstacleData customData) {
                dynamic dynData = customData.customData;

                List<RotationData> rotationData = Trees.at(dynData, "varRotation");
                if (rotationData != null)
                {
                    RotationData truncatedRotation = rotationData
                        .Where(n => n.time < time)
                        .Where(n => n.time + n.duration > time)
                        .LastOrDefault();
                    if (truncatedRotation != null)
                    {
                        Quaternion rotation = Quaternion.Lerp(truncatedRotation.startRotation, truncatedRotation.endRotation, 
                            Easings.Interpolate((time - truncatedRotation.time) / truncatedRotation.duration, truncatedRotation.easing));
                        ____worldRotation = rotation;
                        ____inverseWorldRotation = Quaternion.Inverse(rotation);
                    }
                }

                Quaternion? localRotation = null;
                List<RotationData> localRotationData = Trees.at(dynData, "varLocalRotation");
                if (localRotationData != null)
                {
                    RotationData truncatedRotation = localRotationData
                        .Where(n => n.time < time)
                        .Where(n => n.time + n.duration > time)
                        .LastOrDefault();
                    if (truncatedRotation != null)
                        localRotation = Quaternion.Lerp(truncatedRotation.startRotation, truncatedRotation.endRotation, 
                            Easings.Interpolate((time - truncatedRotation.time) / truncatedRotation.duration, truncatedRotation.easing));
                }

                __instance.transform.localRotation = ____worldRotation * localRotation.GetValueOrDefault(Quaternion.identity);
            }
        }
    }
}