using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Globalization;

namespace client
{
    class Program
    {
        const string BASE_DOMAIN = "http://192.168.123.113/";
        const int THREAD_COUNT = 4;
        static bool running = true;
        static float[,] neighbourMatrix;
        static int nodeCount;

        static void Main(string[] args)
        {
            Console.Title = "(c) Jannik B."; // hust

            while (!IsServerUp())
            {
                Console.Write(".");
            }
            PullMatrix();

            for (int i = 0; i < THREAD_COUNT; i++)
            {
                SetupThread(i);
            }

            Console.ReadLine();
            running = false;
            Console.WriteLine("shutting down!");
        }

        static void SetupThread(int id)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(Work));
            thread.Start(id);
        }

        static void Work(object id)
        {
            foreach (Job job in PullJob())
            {
                Console.WriteLine($"[{id}] pulled job '{job.ID}' at '{job.Minimum}'");
                if (job.Path.Length == nodeCount)
                {
                    SendSolution(new Solution() { Minimum = job.Minimum, Path = job.PathString });
                    Console.WriteLine("FERTIG");
                    Console.WriteLine(job.Minimum);
                    running = false;
                    Console.ReadLine();
                }
                foreach (Solution solution in GetSolution(job))
                {
                    SendSolution(solution);
                }
            }
        }

        static void PullMatrix()
        {
            string[] raw = Read("init/").Split('\n');
            nodeCount = int.Parse(raw[0]);
            neighbourMatrix = new float[nodeCount, nodeCount];
            for (int x = 0; x < nodeCount; x++)
            {
                for (int y = 0; y < nodeCount; y++)
                {
                    neighbourMatrix[x, y] = float.PositiveInfinity;
                }
            }

            for (int i = 1; i < raw.Length; i++)
            {
                string[] line = raw[i].Split(' ');
                int id1 = int.Parse(line[0])-1;
                int id2 = int.Parse(line[1])-1;
                float length = float.Parse(line[2]);
                neighbourMatrix[id1, id2] = length;
                neighbourMatrix[id2, id1] = length;
            }
        }

        static IEnumerable<Job> PullJob()
        {
            while (running)
            {
                string[] jobRequest = Read("job/").Split(';');
                if (jobRequest.Length == 1)
                {
                    Thread.Sleep(100);
                    continue;
                }
                int id = int.Parse(jobRequest[0]);
                float minimum = float.Parse(jobRequest[2], CultureInfo.InvariantCulture);
                string[] path_raw = jobRequest[1].Split(',');
                int[] path = new int[path_raw.Length];
                for (int i = 0; i < path.Length; i++) path[i] = int.Parse(path_raw[i]);
                yield return new Job() { ID = id, Path = path, Minimum = minimum, PathString = jobRequest[1] };
            }
        }

        static IEnumerable<Solution> GetSolution(Job job)
        {
            List<int> remainingIndicies = new List<int>(nodeCount - job.Path.Length);
            for (int i = 0; i < nodeCount; i++)
            {
                if (!job.Path.Contains(i))
                {
                    remainingIndicies.Add(i);
                }
            }

            for (int i = 0; i < remainingIndicies.Count; i++)
            {
                float[,] modifiedNeighbourMatrix = GetModifiedNeighbourMatrix(job.Path, remainingIndicies[i]);
                float sum = GetMinSum(modifiedNeighbourMatrix);
                if (!float.IsInfinity(sum))
                    yield return new Solution() { Minimum = sum, Path = job.PathString + "," + remainingIndicies[i] };
            }
        }

        static float[,] GetModifiedNeighbourMatrix(int[] path, int last)
        {
            float[,] modifiedMatrix = (float[,])neighbourMatrix.Clone();

            void modify(int start, int finish)
            {
                for (int k = 0; k < nodeCount; k++)
                {
                    if (k != finish)
                    {
                        modifiedMatrix[k, start] = float.PositiveInfinity;
                    }
                    if (k != start)
                    {
                        modifiedMatrix[finish, k] = float.PositiveInfinity;
                    }
                }
            }

            for (int i = 0; i < path.Length - 1; i++)
            {
                modify(path[i], path[i + 1]);
            }
            modify(path[path.Length - 1], last);

            return modifiedMatrix;
        }

        static float GetMinSum(float[,] matrix)
        {
            float[] rows = new float[nodeCount], lines = new float[nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                rows[i] = float.PositiveInfinity;
                lines[i] = float.PositiveInfinity;
            }

            for (int x = 0; x < nodeCount; x++)
            {
                for (int y = 0; y < nodeCount; y++)
                {
                    float value = matrix[x, y];
                    rows[x] = Math.Min(value, rows[x]);
                    lines[y] = Math.Min(value, lines[y]);
                }
            }

            float sumLines = 0, sumRows = 0;
            for (int i = 0; i < nodeCount; i++)
            {
                sumLines += lines[i];
                sumRows += rows[i];
            }

            return Math.Max(sumLines, sumRows);
        }

        static string SendSolution(Solution solution)
        {
            Console.WriteLine($"submitted {solution.Minimum}");
            return Read($"submit/?min={solution.Minimum}&path={solution.Path}");
        }

        static string Read(string attachment)
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(BASE_DOMAIN + attachment);
                WebResponse webResponse = webRequest.GetResponse();
                using (Stream responseStream = webResponse.GetResponseStream())
                using (StreamReader responseReader = new StreamReader(responseStream))
                {
                    return responseReader.ReadToEnd();
                }
            }
            catch
            {
                return "";
            }
        }

        static bool IsServerUp()
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(BASE_DOMAIN);
                webRequest.Timeout = 200;
                WebResponse webResponse = webRequest.GetResponse();
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        struct Job
        {
            public int ID;
            public int[] Path;
            public float Minimum;
            public string PathString;
        }

        struct Solution
        {
            public float Minimum;
            public string Path;
        }
    }
}
