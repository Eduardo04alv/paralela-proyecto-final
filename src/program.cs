using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        int nucleosLogicos = Environment.ProcessorCount;
        Console.WriteLine($"Núcleos lógicos disponibles: {nucleosLogicos}");

        Console.Write("¿Cuántos núcleos quieres usar? ");
        if (!int.TryParse(Console.ReadLine(), out int z) || z <= 0)
        {
            Console.WriteLine("Valor inválido.");
            return;
        }

        Grafo grafo = new Grafo();

        Console.Write("¿Cuántos nodos quieres? ");
        if (!int.TryParse(Console.ReadLine(), out int x) || x <= 0)
        {
            Console.WriteLine("Valor inválido.");
            return;
        }

        Console.Write("¿Cuál será el destino? ");
        if (!int.TryParse(Console.ReadLine(), out int y))
        {
            Console.WriteLine("Valor inválido.");
            return;
        }

        grafo.GenerarGrafo(x, 2);

        Stopwatch sw = new Stopwatch();

        
        sw.Start();
        var ruta = grafo.BFS(1, y);
        sw.Stop();
        Console.WriteLine($"\nBFS Secuencial: {sw.ElapsedMilliseconds} ms");

        
        sw.Restart();
        var rutaParalela = grafo.BFSParalelo(1, y, z);
        sw.Stop();
        Console.WriteLine($"BFS Paralelo: {sw.ElapsedMilliseconds} ms");

        
        Console.WriteLine("\nResultado BFS Secuencial:");
        MostrarRuta(ruta);

        Console.WriteLine("\nResultado BFS Paralelo:");
        MostrarRuta(rutaParalela);
    }

    static void MostrarRuta(List<int> ruta)
    {
        if (ruta == null)
        {
            Console.WriteLine("No hay ruta");
            return;
        }

        foreach (var n in ruta)
            Console.Write(n + " ");
        Console.WriteLine();
    }
}

public class Grafo
{
    private Dictionary<int, List<int>> grafo = new();
    private static readonly Random rand = new();

    public void AddEdge(int u, int v)
    {
        if (!grafo.ContainsKey(u)) grafo[u] = new List<int>();
        if (!grafo.ContainsKey(v)) grafo[v] = new List<int>();

        grafo[u].Add(v);
        grafo[v].Add(u);
    }

    public List<int> GetNeighbors(int node)
    {
        return grafo.ContainsKey(node) ? grafo[node] : new List<int>();
    }

    public void GenerarGrafo(int numNodos, int conexionesPorNodo)
    {
        for (int i = 1; i <= numNodos; i++)
        {
            for (int j = 0; j < conexionesPorNodo; j++)
            {
                int vecino = rand.Next(1, numNodos + 1);
                if (vecino != i)
                    AddEdge(i, vecino);
            }
        }
    }

    public List<int> BFS(int inicio, int destino)
    {
        var visitados = new HashSet<int>();
        var cola = new Queue<List<int>>();

        cola.Enqueue(new List<int> { inicio });

        while (cola.Count > 0)
        {
            var camino = cola.Dequeue();
            int nodoActual = camino[^1];

            if (nodoActual == destino)
                return camino;

            if (visitados.Add(nodoActual))
            {
                foreach (var vecino in GetNeighbors(nodoActual))
                {
                    var nuevoCamino = new List<int>(camino) { vecino };
                    cola.Enqueue(nuevoCamino);
                }
            }
        }

        return null;
    }

    public List<int> BFSParalelo(int inicio, int destino, int maxHilos)
    {
        var visitados = new ConcurrentDictionary<int, bool>();
        var cola = new ConcurrentQueue<List<int>>();

        cola.Enqueue(new List<int> { inicio });

        List<int> resultado = null;

        Parallel.For(0, maxHilos, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxHilos
        },
        (i, state) =>
        {
            while (!cola.IsEmpty && resultado == null)
            {
                if (cola.TryDequeue(out var camino))
                {
                    int nodoActual = camino[^1];

                    if (nodoActual == destino)
                    {
                        resultado = camino;
                        state.Stop();
                        return;
                    }

                    if (visitados.TryAdd(nodoActual, true))
                    {
                        foreach (var vecino in GetNeighbors(nodoActual))
                        {
                            var nuevoCamino = new List<int>(camino) { vecino };
                            cola.Enqueue(nuevoCamino);
                        }
                    }
                }
            }
        });

        return resultado;
    }
}

