using System;
using System.Collections.Generic;
using System.Linq;

namespace WFC_Procedural_Generator_Framework
{
    public class WfcSolver
    {
        int width;
        int height;
        int depth;

        int patternSize;

        private Random random = new Random();
        private List<Position> positionsByEntrophy;
        public Cell[,,] cellMap;
        private PatternInfo[] patternInfo;
        private int numberOfPatterns;
        private int collapsedCount = 0;

        private Queue<RemovalUpdate> removalQueue;


        private int[,] InitialEnablerCount()
        {
            int numberOfDirections = Enum.GetValues(typeof(Direction)).Length;
            int[,] result = new int[numberOfPatterns, numberOfDirections];

            for (int patternIndex = 0; patternIndex < numberOfPatterns; patternIndex++)
            {
                for (int direction = 0; direction < numberOfDirections; direction++)
                {
                    HashSet<int> compatibles = patternInfo[patternIndex].GetCompatiblesInDirection((Direction)direction);
                    foreach (int compatible in compatibles)
                    {
                        result[compatible, direction] += 1;
                    }
                }
            }
            return result;
        }

        private void InitializeOutputGrid()
        {
            int[,] enablerCountTemplate = InitialEnablerCount();

            cellMap = new Cell[width, height, depth];
            positionsByEntrophy = new List<Position>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        cellMap[i, k, j] = new Cell(Enumerable.Range(0, patternInfo.Length).ToArray(), patternInfo, enablerCountTemplate);
                        positionsByEntrophy.Add(new Position(i, k, j));
                    }
                }
            }
        }

        public WfcSolver(InputReader inputReader, int width = -1, int height = -1, int depth = -1)
        {
            this.patternSize = inputReader.patternSize;

            this.width = width ;
            this.height = height;// - (patternHeight - 1);
            this.depth = depth;

            this.patternInfo = inputReader.GetPatternInfo();
            this.numberOfPatterns = patternInfo.Length;

            removalQueue = new Queue<RemovalUpdate>();

            if (width != -1 && height != -1 && depth != -1)
            {
                InitializeOutputGrid();
            }
        }

        public void SetOutputSize(int width, int height, int depth)
        {

            this.width = width;
            this.height = height;// - (patternHeight - 1);
            this.depth = depth;

            InitializeOutputGrid();
        }

        private bool PositionIsValid(Position pos)
        {
            return (pos.x < width && pos.x >= 0) && (pos.y < height && pos.y >= 0) && (pos.z < depth && pos.z >= 0);
        }

        private Position FindLowestEntropyCell()
        {
            float minEntropy = float.MaxValue;
            Position pos = new Position();
            //for now, lineal search, must optimize later
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        if (!cellMap[i, k, j].collapsed &&
                            minEntropy > cellMap[i, k, j].entrophy)
                        {
                            minEntropy = cellMap[i, k, j].entrophy;
                            pos = new Position(i, k, j);
                        }
                    }
                }
            }
            return pos;
        }

        private void PrintCellEntrophy()
        {
            string msg = "";
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        msg += cellMap[i, k, j].ToString() + " ";
                    }
                }
                msg += "\n";
            }
            UnityEngine.Debug.Log(msg);
        }


        private (Position, int) Observe()
        {
            // find cell with minimal entropy;
            Position candidatePosition = FindLowestEntropyCell();
            int collapsedPattern = CollapseBasedOnPatternFrecuency(candidatePosition);
            return (candidatePosition, collapsedPattern);
        }

        //we can optimize this:
        private int CollapseBasedOnPatternFrecuency(Position pos)
        {
            int[] candidatePatternIndices = cellMap[pos.x, pos.y, pos.z].possiblePatterns.ToArray();
            int numberOfCandidates = candidatePatternIndices.Length;

            positionsByEntrophy.Remove(positionsByEntrophy.First());

            if (numberOfCandidates == 0)
            {
                collapsedCount++;
                cellMap[pos.x, pos.y, pos.z].CollapseOn(0);
                return 0;
            }

            if (numberOfCandidates == 1)
            {
                collapsedCount++;
                cellMap[pos.x, pos.y, pos.z].CollapseOn(candidatePatternIndices[0]);
                return candidatePatternIndices[0];
            }

            float[] candidateFrecuencies = new float[numberOfCandidates];
            float sumOfFrecuencies = 0;
            for (int i = 0; i < numberOfCandidates; i++)
            {
                candidateFrecuencies[i] = patternInfo[candidatePatternIndices[i]].relativeFrecuency;
                sumOfFrecuencies += candidateFrecuencies[i];
            }

            int collapsedIndex = -1;

            double randomValue = random.NextDouble();
            randomValue = randomValue * (sumOfFrecuencies);
            for (int i = 0; i < numberOfCandidates; i++)
            {
                if (collapsedIndex < 0)
                {
                    if (randomValue > candidateFrecuencies[i])
                    {
                        randomValue -= candidateFrecuencies[i];
                        //Add removal updates to the queue
                        // removalQueue.Enqueue((pos, candidatePatternIndices[i]));
                        removalQueue.Enqueue(new RemovalUpdate(pos, candidatePatternIndices[i]));
                    }
                    else { collapsedIndex = i; }
                }
                else
                {
                    // removalQueue.Enqueue((pos, candidatePatternIndices[i]));
                    removalQueue.Enqueue(new RemovalUpdate(pos, candidatePatternIndices[i]));
                }
            }

            collapsedCount++;

            cellMap[pos.x, pos.y, pos.z].CollapseOn(candidatePatternIndices[collapsedIndex]);
            return candidatePatternIndices[collapsedIndex];
        }


        private void RemoveUncompatiblePatternsInNeighbour(RemovalUpdate removalUpdate, Position neighbourCoord, int direction)
        {
            Cell neighbourCell = cellMap[neighbourCoord.x, neighbourCoord.y, neighbourCoord.z];
            int[,] neighbourEnablers = cellMap[neighbourCoord.x, neighbourCoord.y, neighbourCoord.z].tileEnablerCountsByDirection;
            HashSet<int> compatiblePatterns = patternInfo[removalUpdate.patternIndex].GetCompatiblesInDirection((Direction)direction);

            foreach (int compatiblePattern in compatiblePatterns)
            {
                int oppositeDirection = (direction + 2) % 4;

                //We must remove in the opossite direction from the pov of the neighbour cell.
                neighbourEnablers[compatiblePattern, oppositeDirection]--;
                
                if (neighbourEnablers[compatiblePattern, oppositeDirection] == 0)
                {
                    //if it has a 0 in another direction we have already removed them from this cell.
                    if (!cellMap[neighbourCoord.x, neighbourCoord.y, neighbourCoord.z].ContainsAnyZeroEnablerCount(compatiblePattern))
                    {
                        cellMap[neighbourCoord.x, neighbourCoord.y, neighbourCoord.z].RemovePattern(compatiblePattern, patternInfo);
                    }
                    //CHECK FOR NO MORE POSSIBLE TILES NOW

                    removalQueue.Enqueue(new RemovalUpdate(neighbourCoord, compatiblePattern));
                }
            }
        }

        private void wfcPropagation()
        {
            int numberOfDirections = Enum.GetValues(typeof(Direction)).Length;

            string msg = "Propagation Function call:\n";
            while (removalQueue.Count > 0)
            {
                RemovalUpdate removalUpdate = removalQueue.Dequeue();

                msg += $"\tRemoved pattern {removalUpdate.patternIndex} from cell {removalUpdate.position.x}, {removalUpdate.position.y}, {removalUpdate.position.z}\n";

                for (int direction = 0; direction < numberOfDirections; direction++)
                {
                    Position neighbourCoord = removalUpdate.position + Position.directions[direction];
                    
                    if (!PositionIsValid(neighbourCoord) || cellMap[neighbourCoord.x, neighbourCoord.y, neighbourCoord.z].collapsed) continue;

                    RemoveUncompatiblePatternsInNeighbour(removalUpdate, neighbourCoord, direction);
                    
                }
            }
            UnityEngine.Debug.Log(msg);
        }


        public int[,,] Generate()
        {
            int cellsToBeCollapsed = width * height * depth;
            collapsedCount = 0;

            PrintCellEntrophy();
            while (collapsedCount < cellsToBeCollapsed)
            {
                (Position candidatePosition, int collapsedPattern) = Observe();
                UnityEngine.Debug.Log($"Collapsed cell {candidatePosition} with pattern {collapsedPattern}");
                wfcPropagation();
                PrintCellEntrophy();
            }
            return GetOutputTileIndexGrid();
        }

        public int[,,] Iterate()
        {
            Observe();
            wfcPropagation();
            return GetOutputTileIndexGrid();
        }

        public int[,,] GetPatternGridOutOfOutputGrid()
        {
            int[,,] patternGrid = new int[width, height, depth];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        patternGrid[i, k, j] = cellMap[i, k, j].GetCollapsedIndex();
                    }
                }
            }
            return patternGrid;
        }

        public int[,,] GetOutputTileIndexGrid()
        {
            int[,,] output = new int[width, width, width];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int patternIndex = cellMap[x, 0, z].GetCollapsedIndex();
                    int[,,] pattern = patternInfo[patternIndex].pattern;
                    int tile = pattern[0, 0, 0];
                    output[x, 0, z] = tile;
                }
            }
            return output;
        }
    }
}
