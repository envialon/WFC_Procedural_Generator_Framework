using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Schema;
using Unity.VisualScripting;

namespace WFC_Procedural_Generator_Framework
{
    public struct PatternInfo
    {
        public int frecuency;
        public float relativeFrecuency;
        public float relativeFrecuencyLog2;
        public int[,,] pattern;
        public static PatternInfo operator ++(PatternInfo patternInfo)
        {
            patternInfo.frecuency++;
            return patternInfo;
        }


    }

    public class InputReader
    {
        public int patternSize = 2; // 2x2x2
        public TileSet tileSet;
        public InputTileMapData inputTileMap;

        private int height;
        private int mapSize;
        private int[,,] indexMap;
        private PatternInfo[] patterns;


        /// <summary>
        /// Deberemos transformar la informaci�n de tile y rotaci�n a enteros puramente, acabaremos con 
        /// 4N �ndices �nicos donde N es el tama�o de tileset.
        /// </summary>
        private void PopulateIndexMap()
        {
            Tile[,,] tilemap = inputTileMap.map;
            indexMap = new int[inputTileMap.mapSize, inputTileMap.height, inputTileMap.mapSize];

            for (int k = 0; k < height; k++)
            {
                for (int i = 0; i < mapSize; i++)
                {
                    for (int j = 0; j < mapSize; j++)
                    {
                        indexMap[i, k, j] = tilemap[i, k, j].id * 4 + tilemap[i, k, j].rotation;
                    }
                }
            }
        }


        private int[,,] Extract2DPatternAt(int x, int y)
        {
            int[,,] output = new int[patternSize, 1, patternSize];
            for (int i = 0; i < patternSize; i++)
            {
                for (int j = 0; j < patternSize; j++)
                {
                    output[i, 0, j] = indexMap[i, 0, j];
                }
            }
            return output;
        }

        private void PopulatePatternFrequency2D()
        {
            Dictionary<int[,,], PatternInfo> patternFrecuency = new Dictionary<int[,,], PatternInfo>();
            int totalPatterns = 0;
            for (int i = 0; i < mapSize - patternSize; i++)
            {
                for (int j = 0; j < mapSize - patternSize; j++)
                {
                    int[,,] pattern = Extract2DPatternAt(i, j);
                    if (!patternFrecuency.ContainsKey(pattern))
                    {
                        patternFrecuency.Add(pattern, new PatternInfo());
                    }
                    totalPatterns++;
                    patternFrecuency[pattern]++;
                }
            }

            PatternInfo[] output = patternFrecuency.Values.ToArray();
            int numberOfValues = output.Length;
            for (int i = 0; i < numberOfValues; i++)
            {
                output[i].relativeFrecuency = output[i].frecuency / totalPatterns;
                output[i].relativeFrecuencyLog2 = MathF.Log(output[i].relativeFrecuencyLog2, 2);
            }
        }

        public InputReader(InputTileMapData inputTileMap)
        {
            this.inputTileMap = inputTileMap;
            this.mapSize = inputTileMap.mapSize;
            this.height = inputTileMap.height;
            PopulateIndexMap();
            PopulatePatternFrequency2D();
        }


    }
}