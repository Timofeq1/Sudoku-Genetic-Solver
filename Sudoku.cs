using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
/// Sudoku Solver (Evolutionary Algrotirm)
/// By 2nd yer bachelor student of Innopolis University
/// Timofey Ivlev
namespace Sudoku
{/// <summary>
/// Main class of the program, contains Main method
/// </summary>
    public class Sudoku
    {
        #region Static Constants
        public static SudokuGrid init = new SudokuGrid();  // Initial grid given by input
        public static int FitnessCriticalNumber = 240; // Maximum number of generations without fitness improvement
        public static int InitialPopulationSize = 100; // Number of grids in inital population
        public static int SelectionSize = 9; // Size of selection group (grids with best fitness)
        public static int MaxPopulationSize = SelectionSize * SelectionSize; // Square of selection group
        public static double MutationBase = 0.0; // This number is used to regualte mutations number
        private static Random random = new Random(); // Static random variable for better efficeiency
        public static HashSet<int>[] PrecomputedGivenRows = new HashSet<int>[9]; // Set of given numbers in each row
        public static HashSet<int>[] PrecomputedGivenCols = new HashSet<int>[9]; // Set of given numbers in each column
        public static List<List<int>> precomputedAvailableNumbers = new List<List<int>>(); // Set of avaliable numbers for each 3x3 subgrid
        #endregion
        /// <summary>
        /// In main we firstly fill the initial grid, then precompute given numbers for row and column,
        /// and start Evolutionary algoritm, if it did not converge in first attempt we restart,
        /// with higher mutation level (yet not bigger than 30% for population)
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            ReadInputs(init);
            PrecomputeGivenNumbers(init);

            int restratNumber = 0;
            int result;
            while (true)
            {
                result = EvolutionaryAlgorithm();
                switch (result)
                {
                    case 0:
                        // solution is found and printed
                        return;
                    case -1:
                        // restart algorithm
                        restratNumber++;
                        if (MutationBase < 0.1)
                        {
                            MutationBase += 0.05;
                        }
                        //Console.WriteLine("restart # " + restratNumber);
                        break;
                }
            }
        }
        /// <summary>
        /// This method is simply needed to have an initial list of all given numbers in each row and column,
        /// used to calculate penalty in fitness function
        /// </summary>
        /// <param name="grid"></param>
        public static void PrecomputeGivenNumbers(SudokuGrid grid)
        {
            for (int i = 0; i < 9; i++)
            {

                for (int j = 0; j < 9; j++)
                {

                    if (grid.isGiven[i, j])
                    {
                        if (PrecomputedGivenRows[i] == null)
                        {
                            PrecomputedGivenRows[i] = new HashSet<int>();
                        }
                        PrecomputedGivenRows[i].Add(grid.values[i, j]);
                    }
                    if (grid.isGiven[j, i])
                    {
                        if (PrecomputedGivenCols[i] == null)
                        {
                            PrecomputedGivenCols[i] = new HashSet<int>();
                        }
                        PrecomputedGivenCols[i].Add(grid.values[j, i]);
                    }
                }
            }
        }
        /// <summary>
        /// Includes Mutaion, crossover, generations selection.
        /// Steps of the algoritm are given by regions inside.
        /// </summary>
        /// <returns></returns>
        public static int EvolutionaryAlgorithm()
        {
            #region 1. Create initual population
            // Use a priority queue for the population
            var population = new PriorityQueue<SudokuGrid, int>();

            // Generate initial population
            var initialPopulation = new List<SudokuGrid>();
            CreatePopulation(InitialPopulationSize, init, initialPopulation);

            Parallel.ForEach(initialPopulation, grid =>
            {
                grid.rank = FitnessFunction(grid);
                lock (population) // PriorityQueue is not thread-safe
                {
                    population.Enqueue(grid, grid.rank);
                }
            });


            int currentGeneration = 0;
            List<int> minFitnessHistory = new List<int>(); // Track stagnation
            List<int> varienceHistory = new List<int>();
            List<bool> guarenteedMutationHistory = new List<bool>();
            bool GuaranteedMutation = false;
            bool TakeDiversity = false;
            int takeNumber = 2;
            int id1 = 20;
            int id2 = 21;
#endregion
            while (true)
            {
                // Extract the best 10 individuals from the population

                if (guarenteedMutationHistory.Count > SelectionSize)
                {
                    if (guarenteedMutationHistory.All(x => x))
                    {
                        //Console.WriteLine("Diversity turned on");
                        TakeDiversity = true;
                        takeNumber = 2;
                        id1 = random.Next(SelectionSize, MaxPopulationSize);
                        id2 = random.Next(SelectionSize, MaxPopulationSize);
                        while (id1 == id2)
                        {
                            id1 = random.Next(SelectionSize, MaxPopulationSize);
                        }
                    }
                }
                SudokuGrid[] temp = new SudokuGrid[SelectionSize];
                for (int i = 0; i < SelectionSize; i++)
                {
                    temp[i] = population.Dequeue();
                }

                if (TakeDiversity)
                {
                    for (int i = SelectionSize; i <= Math.Max(id1, id2); i++)
                    {
                        if (i == id1 || i == id2)
                        {
                            temp[SelectionSize - takeNumber] = population.Dequeue();
                            takeNumber++;
                        }
                        else
                        {
                            population.Dequeue();
                        }
                    }
                    takeNumber = 2;
                }
                population.Clear();
                for (int i = 0; i < SelectionSize; i++)
                {
                    population.Enqueue(temp[i], temp[i].rank);
                }

                // Check if the best individual is a solution
                if (population.Peek().rank == 0)
                {
                    PrintOutputs(population.Peek());
                    Console.WriteLine();
                    // av min fitness
                    int avg = temp.Sum(x => x.rank) / temp.Count();
                    Console.WriteLine(avg);
                    int max = temp.Max(x => x.rank);
                    Console.WriteLine(max);
                    return 0;
                }

                // track varience to make less restarts
                int average = temp.Sum(x => x.rank) / SelectionSize;
                int[] diff = new int[SelectionSize];
                for (int i = 0; i < SelectionSize; i++)
                {
                    diff[i] = Math.Abs(temp[i].rank - average);
                    //Console.Write(temp[i].rank + " ");
                }
                int varience = diff.Sum() / SelectionSize;
                varienceHistory.Add(varience);
                //Console.WriteLine(varience);
                if (varienceHistory.Count > SelectionSize)
                {
                    varienceHistory.RemoveAt(0);
                    if (varienceHistory.Distinct().Count() == 1)
                    {
                        // we have a stagnation for 10 generation,
                        // of varience within best grids,
                        // hence it makes sense to increase mutations level,
                        // to get better diversity
                        GuaranteedMutation = true;

                        //Console.WriteLine("Now mutation is 100%");
                    }
                    else if (GuaranteedMutation)
                    {
                        //Console.WriteLine("Mutation is usual");
                        GuaranteedMutation = false;
                    }
                    if (guarenteedMutationHistory.Count > SelectionSize)
                    {
                        guarenteedMutationHistory.RemoveAt(0);
                    }
                    guarenteedMutationHistory.Add(GuaranteedMutation);

                }

                // Track fitness history for stagnation detection
                minFitnessHistory.Add(population.Peek().rank);
                if (minFitnessHistory.Count > FitnessCriticalNumber)
                {
                    minFitnessHistory.RemoveAt(0);
                    if (minFitnessHistory.Distinct().Count() == 1)
                    {
                        //Console.WriteLine("curGen: " + currentGeneration);
                        //Console.WriteLine("var: " + varience);
                        return -1; // Stagnation detected
                    }
                }

                // Perform crossover and create children
                Parallel.ForEach(temp, parent1 =>
                {
                    foreach (var parent2 in temp)
                    {
                        if (parent2.values == parent1.values)
                        {
                            continue;
                        }
                        SudokuGrid child = Crossover(parent1, parent2);
                        if (random.NextDouble() <= (0.2 + MutationBase) || GuaranteedMutation)
                        {
                            Mutate(child, GuaranteedMutation);
                        }
                        child.rank = FitnessFunction(child);
                        lock (population) // Add mutated children back to the population
                        {
                            population.Enqueue(child, child.rank);
                        }
                    }
                });
                /*Console.WriteLine("population");
                var un = population.UnorderedItems;
                List<int> ints = new List<int>();
                int smp = 0;
                foreach (var u in un)
                {
                    ints.Add(u.Element.rank);
                    smp += u.Element.rank;
                }
                smp /= ints.Count;
                ints.Sort();
                int[] diff2 = new int[ints.Count];
                for (int i = 0; i < ints.Count; i++)
                {
                    diff2[i] = Math.Abs(ints[i] - smp);
                    Console.Write(ints[i] + " ");
                }
                int varin2 = diff2.Sum() / ints.Count;
                Console.WriteLine("\nvar2 " + varin2 + " sm2 " + smp +"\n");*/

                currentGeneration++;

            }
        }


        public static void Mutate(SudokuGrid grid, bool moreMutations)
        {
            int mutationsNumber;
            if (moreMutations)
            {
                mutationsNumber = random.Next(20, 30); // from 0 to 9
            }
            else
            {
                mutationsNumber = random.Next(1, 3); // from 0 to 9
            }

            for (int m = 0; m < mutationsNumber; m++)
            {
                // Select a random 3x3 subgrid
                int gridRow = random.Next(0, 3) * 3; // 0, 3, or 6
                int gridCol = random.Next(0, 3) * 3; // 0, 3, or 6

                // Find all mutable (non-given) elements in this subgrid
                var mutableCells = new List<(int row, int col)>();
                for (int i = gridRow; i < gridRow + 3; i++)
                {
                    for (int j = gridCol; j < gridCol + 3; j++)
                    {
                        if (!grid.isGiven[i, j])
                        {
                            mutableCells.Add((i, j));
                        }
                    }
                }

                // Swap two random mutable elements if there are at least two
                if (mutableCells.Count >= 2)
                {
                    var idx1 = mutableCells[random.Next(mutableCells.Count)];
                    var idx2 = mutableCells[random.Next(mutableCells.Count)];

                    (grid.values[idx1.row, idx1.col], grid.values[idx2.row, idx2.col]) =
                        (grid.values[idx2.row, idx2.col], grid.values[idx1.row, idx1.col]);
                }
            }
        }


        public static SudokuGrid Crossover(SudokuGrid parent1, SudokuGrid parent2)
        {
            SudokuGrid child = new SudokuGrid(init);
            // Iterate over each 3x3 subgrid
            for (int gridRow = 0; gridRow < 9; gridRow += 3)
            {
                for (int gridCol = 0; gridCol < 9; gridCol += 3)
                {
                    bool takeFromParent1 = random.NextDouble() < 0.5; // Randomly choose a parent
                    SudokuGrid chosenParent = takeFromParent1 ? parent1 : parent2;

                    // Copy the chosen parent's 3x3 subgrid to the child
                    for (int i = gridRow; i < gridRow + 3; i++)
                    {
                        for (int j = gridCol; j < gridCol + 3; j++)
                        {
                            if (!child.isGiven[i, j])
                            {
                                child.values[i, j] = chosenParent.values[i, j];
                            }
                        }
                    }
                }
            }

            return child;
        }


        public static int FitnessFunction(SudokuGrid grid)
        {
            //PrintOutputs(grid);

            int rowViolations = 0, columnViolations = 0;
            int penalty = 1000; // Huge penalty for violating given values
            double result = 0;

            /*Console.WriteLine();
            PrintOutputs(grid);
            Console.WriteLine();*/

            for (int i = 0; i < 9; i++)
            {
                HashSet<int> seenNumbersRows = new HashSet<int>();
                HashSet<int> seenNumbersCols = new HashSet<int>();

                for (int j = 0; j < 9; j++)
                {

                    if (!grid.isGiven[i, j] && PrecomputedGivenRows[i] != null && PrecomputedGivenRows[i].Contains(grid.values[i, j]))
                    {
                        result += penalty;
                    }
                    if (!grid.isGiven[j, i] && PrecomputedGivenCols[i] != null && PrecomputedGivenCols[i].Contains(grid.values[j, i]))
                    {
                        result += penalty;
                    }
                    seenNumbersRows.Add(grid.values[i, j]);
                    seenNumbersCols.Add(grid.values[j, i]);
                }
                rowViolations += 9 - seenNumbersRows.Count;
                columnViolations += 9 - seenNumbersCols.Count;

            }

            // Penalize duplicates and prioritize valid solutions
            result = Math.Pow(rowViolations, 2) + Math.Pow(columnViolations, 2);

            //Console.WriteLine($"{rowViolations} row violations, {columnViolations} column violations, {result} result");
            return (int)result;
        }


        public static void CreatePopulation(int size, SudokuGrid init, List<SudokuGrid> population)
        {
            // Precompute available numbers for each subgrid
            if (precomputedAvailableNumbers.Capacity == 0)
            {
                for (int gridRow = 0; gridRow < 9; gridRow += 3)
                {
                    for (int gridCol = 0; gridCol < 9; gridCol += 3)
                    {
                        var usedNumbers = new HashSet<int>();
                        for (int i = gridRow; i < gridRow + 3; i++)
                        {
                            for (int j = gridCol; j < gridCol + 3; j++)
                            {
                                if (init.isGiven[i, j])
                                {
                                    usedNumbers.Add(init.values[i, j]);
                                }
                            }
                        }
                        precomputedAvailableNumbers.Add(Enumerable.Range(1, 9).Except(usedNumbers).ToList());
                    }
                }
            }


            // Create population
            for (int s = 0; s < size; s++)
            {
                SudokuGrid copy = new SudokuGrid(init);
                int subgridIndex = 0;

                for (int gridRow = 0; gridRow < 9; gridRow += 3)
                {
                    for (int gridCol = 0; gridCol < 9; gridCol += 3)
                    {
                        // Get a reshuffled version of the precomputed available numbers
                        var availableNumbers = new List<int>(precomputedAvailableNumbers[subgridIndex]);
                        ShuffleList(availableNumbers);

                        int numberIndex = 0;
                        for (int i = gridRow; i < gridRow + 3; i++)
                        {
                            for (int j = gridCol; j < gridCol + 3; j++)
                            {
                                if (!copy.isGiven[i, j] && numberIndex < availableNumbers.Count)
                                {
                                    copy.values[i, j] = availableNumbers[numberIndex++];
                                }
                            }
                        }

                        subgridIndex++;
                    }
                }

                population.Add(copy);
            }
        }


        // Helper method to shuffle a list
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static void ReadInputs(SudokuGrid grid)
        {
            for (int i = 0; i < 9; i++)
            {
                var inputLine = Console.ReadLine().Split(" ");
                for (int j = 0; j < 9; j++)
                {
                    try
                    {
                        if (inputLine[j] == "-")
                        {
                            grid.values[i, j] = 0;
                            grid.isGiven[i, j] = false;
                        }
                        else
                        {
                            try
                            {
                                grid.values[i, j] = int.Parse(inputLine[j]);
                                grid.isGiven[i, j] = true;
                            }
                            catch (Exception)
                            {
                                grid.values[i, j] = 1;
                                grid.isGiven[i, j] = true;
                            }

                        }
                    } catch (Exception)
                    {
                        grid.values[i, j] = 0;
                        grid.isGiven[i, j] = false;
                    }
                    
                }
            }
        }

        public static void PrintOutputs(SudokuGrid grid)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (j == 8 && i != 8)
                    {
                        Console.Write(grid.values[i, j] + "\n");
                    } else if (j == 8 && i == 8)
                    {
                        Console.Write(grid.values[i, j]);
                    } else
                    {
                        Console.Write(grid.values[i, j] + " ");
                    }
                    
                }
            }
        }
    }

    public class SudokuGrid : IComparable<SudokuGrid>
    {
        public int rank;
        public int[,] values;
        public bool[,] isGiven;

        public SudokuGrid()
        {
            values = new int[9, 9];
            isGiven = new bool[9, 9];
        }

        public SudokuGrid(SudokuGrid grid)
        {
            values = (int[,])grid.values.Clone();
            isGiven = (bool[,])grid.isGiven.Clone();
        }

        public int CompareTo(SudokuGrid? other)
        {
            return rank.CompareTo(other?.rank ?? 0);
        }
    }
}
