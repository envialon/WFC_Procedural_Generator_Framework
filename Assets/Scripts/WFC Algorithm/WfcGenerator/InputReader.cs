using System;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace WFC_Procedural_Generator_Framework
{
    public class InputReader
    {
        public int patternSize = 2; // 2x2x2
        public int patternHeight = 2;
        public Tilemap inputTileMap;

        private int height;
        private int mapSize;
        private int[,,] offsettedIndexGrid;
        //must change
        public int[,,] patternGrid;
        private PatternInfo[] patterns;
        private int totalPatterns = 0;

        /// <summary>
        /// Deberemos transformar la informaci�n de tile y rotaci�n a enteros puramente, acabaremos con 
        /// 4N �ndices �nicos donde N es el tama�o de tileset.
        /// </summary>
        private void PopulateIndexGrid()
        {
            Tile[,,] tilemap = inputTileMap.map;
            offsettedIndexGrid = new int[inputTileMap.width, inputTileMap.height, inputTileMap.depth];

            for (int k = 0; k < height; k++)
            {
                for (int i = 0; i < mapSize; i++)
                {
                    for (int j = 0; j < mapSize; j++)
                    {
                        offsettedIndexGrid[i, k, j] = tilemap[i, k, j].id * 4 + tilemap[i, k, j].rotation;
                    }
                }
            }
        }

        private int mod(int x, int y)
        {
            return x - y * (int)Math.Floor((double)x / y);
        }

        private int[,,] Extract2DPatternAt(int x, int y)
        {
            int[,,] output = new int[patternSize, 1, patternSize];
            for (int i = 0; i < patternSize; i++)
            {
                for (int j = 0; j < patternSize; j++)
                {
                    output[i, 0, j] = offsettedIndexGrid[mod((i + x), mapSize), 0, mod(j + y, mapSize)];
                }
            }
            return output;
        }


        private string hashPattern(int[,,] pattern)
        {
            string digits = "";
            foreach (int i in pattern)
            {
                digits += i + ".";
            }
            return digits;
        }

        private void ExtractUniquePatterns()
        {
            //usamos el diccionario para aprovechar el hasheo

            Dictionary<string, PatternInfo> patternFrecuency = new Dictionary<string, PatternInfo>();
            HashSet<PatternInfo> uniquePatterns = new HashSet<PatternInfo>();
            totalPatterns = 0;

            for (int i = -patternSize; i <= mapSize + patternSize; i++)
            {
                for (int j = -patternSize; j <= mapSize; j++)
                {
                    int[,,] pattern = Extract2DPatternAt(i, j);
                    string patternHash = hashPattern(pattern);
                    // PatternInfo candidate = new PatternInfo(pattern, uniquePatterns.Count);
                    if (!patternFrecuency.ContainsKey(hashPattern(pattern)))
                    {
                        //uniquePatterns.Add(candidate);
                        patternFrecuency.Add(patternHash, new PatternInfo(pattern, patternFrecuency.Count));
                    }
                    totalPatterns++;
                    patternFrecuency[patternHash]++;
                    //  patternGrid[i % mapSize, 0, j % mapSize] = patternFrecuency[patternHash].id;
                    //PatternInfo actualValue = new PatternInfo();
                    //uniquePatterns.TryGetValue(candidate, out actualValue);
                    //actualValue.frecuency++;
                    //patternGrid[i, 0, j] = actualValue.id;
                }
            }

            patterns = patternFrecuency.Values.ToArray();
            int numberOfValues = patterns.Length;
            //for (int i = 0; i < numberOfValues; i++)
            //{
            //    patterns[i].relativeFrecuency = patterns[i].frecuency / totalPatterns;
            //    patterns[i].relativeFrecuencyLog2 = MathF.Log(patterns[i].relativeFrecuencyLog2, 2);
            //}
        }

        private void UpdateFrecuencies()
        {

            for (int i = 0; i < patterns.Length; i++)
            {
                patterns[i].UpdateFrecuencies(totalPatterns);
            }
        }
           
        private bool NorthNeighbour(PatternInfo current, PatternInfo candidate)
        {
            int[,,] currentGrid = current.pattern;
            int[,,] candidateGrid = candidate.pattern;

            for (int i = 1; i < patternSize; i++)
            {
                for (int j = 0; j < patternSize; j++)
                {
                    int a = currentGrid[i, 0, j];
                    int b = candidateGrid[i-1, 0, j];
                    if (a != b)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private bool EastNeighbour(PatternInfo current, PatternInfo candidate)
        {
            int[,,] currentGrid = current.pattern;
            int[,,] candidateGrid = candidate.pattern;

            for (int i = 0; i < patternSize; i++)
            {
                for (int j = 1; j < patternSize; j++)
                {
                    int a = currentGrid[i, 0, j];
                    int b = candidateGrid[i, 0, j-1];
                    if (a != b)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void OldCheckForNeighborhood(int currentIndex, PatternInfo current, int candidateIndex, PatternInfo candidate)
        {
            //bool northNeighbour = true;
            //bool southNeighbour = true;
            //bool eastNeighbour = true;
            //bool westNeighbour = true;

            //int lastIndex = patternSize - 1;
            //int[,,] currentGrid = current.pattern;
            //int[,,] candidateGrid = candidate.pattern;

            //for (int i = 0; i < patternSize; i++)
            //{
            //    for (int j = 0; j < patternSize; j++)
            //    {
            //        int mirrorJ = lastIndex - j;
            //        northNeighbour &= currentGrid[i, 0, j] == candidateGrid[i, 0, mirrorJ];
            //        southNeighbour &= currentGrid[i, 0, mirrorJ] == candidateGrid[i, 0, j];
            //        eastNeighbour &= currentGrid[j, 0, i] == candidateGrid[mirrorJ, 0, i];
            //        westNeighbour &= currentGrid[mirrorJ, 0, i] == candidateGrid[j, 0, i];
            //    }
            //}       
            if (NorthNeighbour(current, candidate))
            {
                candidate.neigbourIndices[Direction.south].Add(currentIndex);
                current.neigbourIndices[Direction.north].Add(candidateIndex);
            }
            if (EastNeighbour(current, candidate))
            {
                candidate.neigbourIndices[Direction.west].Add(currentIndex);
                current.neigbourIndices[Direction.east].Add(candidateIndex);
            }
        }

        private void FindOverlappingNeighbours()
        {
            int numberOfPatterns = patterns.Length;
            for (int i = 0; i < numberOfPatterns; i++)
            {
                PatternInfo current = patterns[i];
                for (int j = 0; j < numberOfPatterns; j++)
                {
                    PatternInfo candidate = patterns[j];
                    OldCheckForNeighborhood(i, current, j, candidate);
                }
            }
        }

        private void PopulatePatternNeighbours()
        {
            FindOverlappingNeighbours();
        }

        private void PlacePattern(ref int[,,] indexGrid, int patternId, int x, int y, int z)
        {
            for (int i = 0; i < patternSize; i++)
            {
                for (int j = 0; j < patternSize; j++)
                {
                    indexGrid[i + x, y, j + z] = patterns[patternId].pattern[i, 0, j];
                }
            }
        }

        public int[,,] GetIndexGridFromPatternIndexGrid(int[,,] patternIndexGrid)
        {
            int maxX = patternIndexGrid.GetLength(0);
            int maxY = patternIndexGrid.GetLength(1);
            int maxZ = patternIndexGrid.GetLength(2);

            int[,,] indexGrid = new int[maxX * patternSize, maxY * patternHeight, maxZ * patternSize];

            for (int i = 0; i < maxX; i++)
            {
                for (int j = 0; j < maxZ; j++)
                {
                    for (int k = 0; k < maxY; k++)
                    {
                        PlacePattern(ref indexGrid, patterns[patternIndexGrid[i, k, j]].id, i, k, j);
                    }
                }
            }


            return indexGrid;
        }

        public PatternInfo[] GetPatternInfo()
        {
            return patterns;
        }

        public void Train(int patternSize = 2, Tilemap inputTileMap = null)
        {
            if (inputTileMap is not null)
            {
                Initialize(inputTileMap, patternSize);
            }
            if (this.inputTileMap is null)
            {
                throw new Exception("The InputReader doesn't have any data to read.");
            }

            patterns = new PatternInfo[0];

            PopulateIndexGrid();
            ExtractUniquePatterns();
            UpdateFrecuencies();
            PopulatePatternNeighbours();
        }


        private void Initialize(Tilemap inputTileMap, int patternSize = 2)
        {
            this.patternSize = patternSize;
            this.inputTileMap = inputTileMap;
            this.mapSize = inputTileMap.width;
            this.height = inputTileMap.height;
            this.patternGrid = new int[mapSize, 1, mapSize];
        }

        public int[,,] GetOutputTileIndexGrid()
        {
            int[,,] output = new int[mapSize, height, mapSize];

            for (int x = 0; x < mapSize - patternSize; x++)
            {
                for (int z = 0; z < mapSize - patternSize; z++)
                {
                    int patternIndex = patternGrid[x, 0, z];
                    int[,,] pattern = patterns[patternIndex].pattern;
                    for (int i = 0; i < patternSize; i++)
                    {
                        for (int j = 0; j < patternSize; j++)
                        {
                            output[x + i, 0, z + j] = pattern[i, 0, j];
                        }
                    }
                }
            }
            return output;
        }

        public string GetMatrixVisualization(int[,,] mat)
        {
            string patternVisualization = "";
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(2); j++)
                {
                    patternVisualization += "\t" + mat[i, 0, j] + "\t";
                }
                patternVisualization += "\n";
            }
            return patternVisualization;
        }

        private string GetNeighboursVisualization(Dictionary<Direction, HashSet<int>> neighbours)
        {
            string str = "";

            foreach (KeyValuePair<Direction, HashSet<int>> entry in neighbours)
            {
                str += "\t" + entry.Key + ": ";
                foreach (int index in entry.Value)
                {
                    str += index + ", ";
                }
                str += "\n";
            }

            return str;
        }

        public string GetPatternSummary()
        {
            const string spacer = "\n/////////////////////\n";
            string messsage = "";
            messsage += "InputMap:\n" + GetMatrixVisualization(offsettedIndexGrid) + spacer + spacer;

            messsage += "Pattern Info:\n" + spacer;
            foreach (PatternInfo pattern in patterns)
            {
                string patternMessage = "Pattern " + pattern.id + ":\n";
                patternMessage += "Frecuency: " + pattern.frecuency + "\n";
                patternMessage += "RelativeFrecuency: " + pattern.relativeFrecuency + "\n";
                patternMessage += "Tile pattern:\n " + GetMatrixVisualization(pattern.pattern) + "\n";
                patternMessage += "Neigbours:\n" + GetNeighboursVisualization(pattern.neigbourIndices) + "\n";


                messsage += patternMessage + spacer;
            }

            return messsage;
        }

        public InputReader(Tilemap inputTileMap, int patternSize = 2)
        {
            Initialize(inputTileMap, patternSize);
        }
    }
}