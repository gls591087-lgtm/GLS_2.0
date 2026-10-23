using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO;

namespace KartRider.Game.Data;

public sealed class CachedLevels : Dictionary<sbyte, Statistics>
{
    public CachedLevels()
    {
        using (Log.Load("Levels"))
        {
            string baseDirectory = AppContext.BaseDirectory;
            string dataPath = Path.Combine(baseDirectory, "data.bml");

            Log.Inform(
                "BML data path: {0}",
                dataPath);

            if (!File.Exists(dataPath))
            {
                throw new FileNotFoundException(
                    "Unable to locate data.bml.",
                    dataPath);
            }

            BmlObject root = BmlObject.Create(dataPath);

            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Failed to load BML data: {dataPath}");
            }

            Log.Inform(
                "BML root: Name={0}, Value={1}",
                root.Name,
                root.Value);

            BmlObject levelsObj = root.GetObject("Levels");

            if (levelsObj == null)
            {
                throw new InvalidOperationException(
                    "BML object 'Levels' was not found.");
            }

            Log.Inform(
                "Levels objects: {0}",
                levelsObj.SubObjects.Count);

            sbyte level = 0;

            foreach (Tuple<string, BmlObject> entry in levelsObj.SubObjects)
            {
                BmlObject levelObj = entry.Item2;

                Statistics statistics = new Statistics
                {
                    Mass = levelObj.GetFloat("Mass"),
                    AirFriction = levelObj.GetFloat("AirFriction"),
                    DragFactor = levelObj.GetFloat("DragFactor"),

                    ForwardAccelForce =
                        levelObj.GetFloat("ForwardAccelForce"),

                    BackwardAccelForce =
                        levelObj.GetFloat("BackwardAccelForce"),

                    GripBrakeForce =
                        levelObj.GetFloat("GripBrakeForce"),

                    SlipBrakeForce =
                        levelObj.GetFloat("SlipBrakeForce"),

                    MaxSteerAngle =
                        levelObj.GetFloat("MaxSteerAngle"),

                    SteerConstraint =
                        levelObj.GetFloat("SteerConstraint"),

                    FrontGripFactor =
                        levelObj.GetFloat("FrontGripFactor"),

                    RearGripFactor =
                        levelObj.GetFloat("RearGripFactor"),

                    DriftTriggerFactor =
                        levelObj.GetFloat("DriftTriggerFactor"),

                    DriftTriggerTime =
                        levelObj.GetFloat("DriftTriggerTime"),

                    DriftSlipFactor =
                        levelObj.GetFloat("DriftSlipFactor"),

                    DriftEscapeForce =
                        levelObj.GetFloat("DriftEscapeForce"),

                    CornerDrawFactor =
                        levelObj.GetFloat("CornerDrawFactor"),

                    DriftLeanFactor =
                        levelObj.GetFloat("DriftLeanFactor"),

                    SteerLeanFactor =
                        levelObj.GetFloat("SteerLeanFactor"),

                    DriftMaxGauge =
                        levelObj.GetFloat("DriftMaxGauge"),

                    NormalBoosterTime =
                        levelObj.GetFloat("NormalBoosterTime"),

                    TeamBoosterTime =
                        levelObj.GetFloat("TeamBoosterTime")
                };

                Add(level, statistics);

                Log.Inform(
                    "Loaded Level: Index={0}, Name={1}",
                    level,
                    entry.Item1);

                level++;
            }

            if (Count == 0)
            {
                Log.Inform(
                    "No Levels found in data.bml.");
            }

            Log.Inform(
                "CachedLevels: Loaded {0} level(s).",
                Count);
        }
    }
}