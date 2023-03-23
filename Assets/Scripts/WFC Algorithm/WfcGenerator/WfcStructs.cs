using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WFC_Procedural_Generator_Framework
{
    public enum Direction
    {
        north,
        east,
        south,
        west,
    }

    public struct Position
    {
        public int x; public int y; public int z;
        public Position(int x = 0, int y = 0, int z = 0)
        {
            this.x = x; this.y = y; this.z = z;
        }
    }

    public struct RemovalUpdate
    {
        int patternIndex;
        Position position;

        public RemovalUpdate(Position position, int patternIndex)
        {
            this.patternIndex = patternIndex;
            this.position = position;
        }
    }

    public struct Cell
    {
        public HashSet<int> possiblePatterns;
        //first index is the pattern, second is the direction
        public int[,] tileEnablerCountsByDirection;
        public float entrophy;
        public bool collapsed;

        float sumOfPatternWeights;
        float sumOfPatternLogWeights;


        public Cell(int[] possiblePatterns, PatternInfo[] patternInfo, int[,] tileEnablerTemplate)
        {
            this.possiblePatterns = new HashSet<int>(possiblePatterns);
            sumOfPatternWeights = 0;
            sumOfPatternLogWeights = 0;
            entrophy = 0;
            collapsed = false;
            for (int i = 0; i < possiblePatterns.Length; i++)
            {
                sumOfPatternWeights += patternInfo[possiblePatterns[i]].relativeFrecuency;
                //im doing it different from the rust resource, might be an error:
                sumOfPatternLogWeights += patternInfo[possiblePatterns[i]].relativeFrecuencyLog2;
            }
            tileEnablerCountsByDirection = new int[tileEnablerTemplate.GetLength(0), tileEnablerTemplate.GetLength(1)];
            Buffer.BlockCopy(tileEnablerTemplate, 0, tileEnablerCountsByDirection, 0, tileEnablerCountsByDirection.Length * sizeof(int));
            CalculateEntrophy();
        }

        public void CollapseOn(int patternToCollapse)
        {
            possiblePatterns.RemoveWhere(x => x != patternToCollapse);
            entrophy = 0;
            collapsed = true;
        }

        private void CalculateEntrophy()
        {
            entrophy = (float)(Math.Log(sumOfPatternWeights, 2) - (sumOfPatternLogWeights / sumOfPatternWeights));
        }

        public void RemovePattern(int patternIndex, PatternInfo[] patternInfo)
        {
            possiblePatterns.Remove(patternIndex);
            sumOfPatternLogWeights -= patternInfo[patternIndex].relativeFrecuencyLog2;
            sumOfPatternWeights -= patternInfo[patternIndex].relativeFrecuency;
            CalculateEntrophy();
        }
    }

    /// <summary>
    /// Contains all of the pattern information extracted from the input.
    /// </summary>
    public struct PatternInfo
    {
        public int id;
        public int frecuency;
        public float relativeFrecuency;
        public float relativeFrecuencyLog2;
        public int[,,] pattern;

        public Dictionary<Direction, HashSet<int>> neigbourIndices;

        public PatternInfo(int[,,] pattern, int patternId)
        {
            this.pattern = pattern;
            this.id = patternId;
            frecuency = 0;
            relativeFrecuency = 0;
            relativeFrecuencyLog2 = 0;
            neigbourIndices = new Dictionary<Direction, HashSet<int>>
            {
                { Direction.north, new HashSet<int>() },
                { Direction.south, new HashSet<int>() },
                { Direction.west, new HashSet<int>() },
                { Direction.east, new HashSet<int>() }
            };
        }

        public static PatternInfo operator ++(PatternInfo patternInfo)
        {
            patternInfo.frecuency++;
            return patternInfo;
        }

        public HashSet<int> GetCompatiblesInDirection(Direction direction)
        {
            return neigbourIndices[direction];
        }
        
    }
}