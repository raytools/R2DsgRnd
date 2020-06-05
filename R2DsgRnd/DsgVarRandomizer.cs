using Ray2Mod;
using Ray2Mod.Components;
using Ray2Mod.Components.Text;
using Ray2Mod.Components.Types;
using Ray2Mod.Game;
using Ray2Mod.Game.Structs;
using Ray2Mod.Utils;
using System;
using System.Collections.Generic;

namespace R2DsgRnd
{
    public unsafe class DsgVarRandomizer : IMod
    {
        private RemoteInterface Interface;
        private Random random;

        public static int Frame;
        
        //private RandomizeMode mode = Randomi

        private float randomizeInterval = 1; // Randomize every 1 second
        private float randomizeChance = 0.05f; // 5 % chance to randomize a variable

        private float minFloatAdd = -10.0f;
        private float maxFloatAdd = 10.0f;
        private int minIntAdd = -10;
        private int maxIntAdd = 10;
        private float randomVectorMinMagnitude = 0.0f;
        private float randomVectorMaxMagnitude = 10.0f;

        public void Run(RemoteInterface remoteInterface)
        {
            Interface = remoteInterface;
            GlobalActions.Engine += CountFrames;
            random = new Random();

            World world = new World(remoteInterface);
            List<TextOverlay> vars = new List<TextOverlay>();

            GlobalInput.Actions['g'] = () =>
            {
                foreach (TextOverlay overlay in vars) overlay.Hide();
                vars = new List<TextOverlay>();

                vars.Add(new TextOverlay("Rayman Dsgvars=".Red(), 6, 5, 0).Show());

                world.ReadObjectNames();
                Dictionary<string, Pointer<SuperObject>> superObjects = world.GetActiveSuperObjects();

                Interface.Log("SUPEROBJECT NAMES:", LogType.Debug);
                foreach (KeyValuePair<string, Pointer<SuperObject>> o in superObjects)
                {
                    Interface.Log($"{o.Key} {o.Value}", LogType.Debug);
                }

                SuperObject* rayman = superObjects["Rayman"];
                Perso* perso = (Perso*)rayman->engineObjectPtr;

                DsgVar* dsgVars = *perso->brain->mind->dsgMem->dsgVar;

                Interface.Log("DSGVARS:", LogType.Debug);
                for (int i = 0; i < dsgVars->dsgVarInfosLength; i++)
                {
                    DsgVarInfo info = dsgVars->dsgVarInfos[i];
                    DsgVarType type = info.type;

                    Pointer<byte> buffer = perso->brain->mind->dsgMem->memoryBufferCurrent;
                    int offset = info.offsetInBuffer;

                    string name = $"{Enum.GetName(typeof(DsgVarType), type)}!{i}";
                    Func<object> value = buffer.GetDisplayReference(type, offset);

                    if (value != null)
                    {
                        vars.Add(new TextOverlay(_ => $"{name.Yellow()}\\{value()}", 5, ((vars.Count + 1) * 5 * 2.6f + 5) < 1000 ? 5 : 505, (vars.Count * 5 * 2.6f + 5) % 980).Show());
                    }
                }
            };

            GlobalInput.Actions['r'] = () =>
            {
                RandomizeAllObjects(world);
            };

            RandomizeMode mode = new RandomizeModeInterval(randomizeInterval);

            GlobalActions.Engine += () =>
            {
                if (mode.ShouldRandomize())
                {
                    RandomizeAllObjects(world);
                }
            };

        }

        private void RandomizeAllObjects(World world)
        {
            world.ReadObjectNames();
            Dictionary<string, Pointer<SuperObject>> superObjects = world.GetActiveSuperObjects();

            foreach (SuperObject* superObject in superObjects.Values)
            {
                Perso * perso = (Perso*)superObject->engineObjectPtr;
                int aiModelID = perso->stdGamePtr->modelID;
                string aiModelName = world.ObjectNames[ObjectSet.Model][aiModelID];

                if (aiModelName == "DS1_GEN_PTC_GenCKS" || aiModelName == "DS1_GEN_PTC_GenBigFile")
                {
                    perso->brain = null;
                    continue;
                }

                RandomizeObject(superObject);
            }

            SuperObject* global = superObjects["global"];
            DsgMem* dsgMem = ((Perso*) global->engineObjectPtr)->brain->mind->dsgMem;
            DsgVar* dsgVars = *dsgMem->dsgVar;
            DsgVarInfo info = dsgVars->dsgVarInfos[63];

            bool* bool63 = (bool*)((int) dsgMem->memoryBufferCurrent + info.offsetInBuffer);
            *bool63 = false;
        }

        private void RandomizeObject(SuperObject* superObject)
        {
            Perso* perso = (Perso*)superObject->engineObjectPtr;

            Brain* brain = perso->brain;
            if (brain == null) return;

            DsgMem* dsgMem = brain->mind->dsgMem;
            if (dsgMem == null) return;

            DsgVar* dsgVars = *dsgMem->dsgVar;

            for (int i = 0; i < dsgVars->dsgVarInfosLength; i++) 
            {
                if (random.NextDouble() > randomizeChance) continue;

                DsgVarInfo info = dsgVars->dsgVarInfos[i];
                DsgVarType type = info.type;

                byte* buffer = dsgMem->memoryBufferCurrent;
                int ptr = (int)buffer + info.offsetInBuffer;

                switch (type)
                {
                    case DsgVarType.Boolean:
                        *(bool*) ptr = random.Next(0, 2) == 0;
                        break;
                    case DsgVarType.Byte:
                        *(sbyte*) ptr = (sbyte) random.Next(-127, 128);
                        break;
                    case DsgVarType.UByte:
                        *(byte*) ptr = (byte) random.Next(0, 256);
                        break;
                    case DsgVarType.Short:
                        *(short*) ptr = (short) random.Next();
                        break;
                    case DsgVarType.UShort:
                        *(ushort*) ptr = (ushort) random.Next();
                        break;
                    case DsgVarType.Int:
                        *(int*) ptr = random.Next();
                        break;
                    case DsgVarType.UInt:
                        *(uint*) ptr = (uint)random.Next();
                        break;
                    case DsgVarType.Float:
                        *(float*) ptr += random.RandomFloat(-10f, 10f);
                        break;
                    case DsgVarType.Vector:
                        Vector3* vector = (Vector3*) ptr;
                        vector->X += random.RandomFloat(-10f, 10f);
                        vector->Y += random.RandomFloat(-10f, 10f);
                        vector->Z += random.RandomFloat(-10f, 10f);
                        break;
                    case DsgVarType.IntegerArray:
                        int* array = brain->mind->GetDsgVar<int>(i, buffer, out byte size);
                        for (int j = 0; j < size; j++)
                        {
                            array[j] = random.Next();
                        }
                        break;
                }
            }
        }

        private void CountFrames()
        {
            Frame++;
        }
    }

    internal static unsafe class ModExtensions
    {
        internal static Func<object> GetDisplayReference(this Pointer<byte> buffer, DsgVarType type, int offset)
        {
            int value = buffer + offset;

            switch (type)
            {
                case DsgVarType.Boolean:
                    return () => *(bool*) value;
                case DsgVarType.Byte:
                    return () => *(sbyte*) value;
                case DsgVarType.UByte:
                    return () => *(byte*) value;
                case DsgVarType.Short:
                    return () => *(short*) value;
                case DsgVarType.UShort:
                    return () => *(ushort*) value;
                case DsgVarType.Int:
                    return () => *(int*) value;
                case DsgVarType.UInt:
                    return () => *(uint*) value;
                case DsgVarType.Float:
                    return () => *(float*) value;
                case DsgVarType.WayPoint:
                    break;
                case DsgVarType.Perso:
                    break;
                case DsgVarType.List:
                    break;
                case DsgVarType.Vector:
                    return () => *(Vector3*) value;
                case DsgVarType.Comport:
                    break;
                case DsgVarType.Action:
                    break;
                case DsgVarType.Text:
                    break;
                case DsgVarType.GameMaterial:
                    break;
                case DsgVarType.Caps:
                    break;
                case DsgVarType.Graph:
                    break;
                case DsgVarType.PersoArray:
                    break;
                case DsgVarType.VectorArray:
                    break;
                case DsgVarType.FloatArray:
                    break;
                case DsgVarType.IntegerArray:
                    break;
                case DsgVarType.WayPointArray:
                    break;
                case DsgVarType.TextArray:
                    break;
                case DsgVarType.SuperObject:
                    break;
            }

            return null;
        }
    }
}
