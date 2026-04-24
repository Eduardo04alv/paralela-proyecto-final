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
        if (!int.TryParse(Console.ReadLine(), out int z) || z <= 0 )
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
        /*
        foreach (var nodo in grafo.GetAll()) 
        { 
            Console.Write(nodo.Key + ": "); 
            foreach (var vecino in nodo.Value) 
            { 
                Console.Write(vecino + " "); 
            } 
            Console.WriteLine(); 
        }
        */
        Stopwatch sw = new Stopwatch();

        // BFS normal
        sw.Restart();
        var rutaBFS = grafo.BFS(1, y);
        sw.Stop();
        long BFSSecuencial = sw.ElapsedMilliseconds;
        Console.WriteLine($"BFS Secuencial: {BFSSecuencial} ms");

        // DFS normal
        /*
        sw.Restart();
        var rutaDFS = grafo.DFS(1, y);
        sw.Stop();
        long DFSSecuencial = sw.ElapsedMilliseconds;
        Console.WriteLine($"DFS Secuencial: {DFSSecuencial} ms");
        */

        // BFS paralelo
        sw.Restart();
        var rutaParalela = grafo.BFSParalelo(1, y, z);
        sw.Stop();
        long BFSparalelo = sw.ElapsedMilliseconds;
        Console.WriteLine($"BFS Paralelo: {BFSparalelo} ms");

        // DFS paralelo
        /*
        sw.Restart();
        var rutaDFSParalelo = grafo.DFSParalelo(1, y, z);
        sw.Stop();
        long DFSparalelo = sw.ElapsedMilliseconds;
        Console.WriteLine($"DFS Paralelo: {DFSparalelo} ms");
        */

        // Mostrar resultados
        Console.WriteLine("Resultado BFS Secuencial:");
        MostrarRuta(rutaBFS);

       // Console.WriteLine("Resultado DFS Secuencial:");
       // MostrarRuta(rutaDFS);

        Console.WriteLine("Resultado BFS Paralelo:");
        MostrarRuta(rutaParalela);

        // Console.WriteLine("Resultado DFS Paralelo:");
        //MostrarRuta(rutaDFSParalelo);

        // metricas 
        double speedup = (double)BFSSecuencial / BFSparalelo;
        double eficiencia = speedup / z;

        Console.WriteLine($"\n--- MÉTRICAS BFS ---");
        Console.WriteLine($"Speedup: {speedup:F2}");
        Console.WriteLine($"Eficiencia: {eficiencia:F2}");

        /*
        double speedup2 = (double)DFSSecuencial / DFSparalelo;
        double eficiencia2 = speedup2 / z;

        Console.WriteLine($"\n--- MÉTRICAS DFS ---");
        Console.WriteLine($"Speedup: {speedup2:F2}");
        Console.WriteLine($"Eficiencia: {eficiencia2:F2}");

        */
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

    public Dictionary<int, List<int>> GetAll() 
    { 
        return grafo;
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
        var visitados = new ConcurrentDictionary<int, int>(); // guarda padre
        var actualNivel = new List<int> { inicio };

        visitados[inicio] = -1;

        while (actualNivel.Count > 0)
        {
            var siguienteNivel = new ConcurrentBag<int>();

            Parallel.ForEach(actualNivel, new ParallelOptions
            {
                MaxDegreeOfParallelism = maxHilos
            },
            (nodoActual, state) =>
            {
                if (nodoActual == destino)
                {
                    state.Stop();
                    return;
                }

                foreach (var vecino in GetNeighbors(nodoActual))
                {
                    if (visitados.TryAdd(vecino, nodoActual))
                    {
                        siguienteNivel.Add(vecino);
                    }
                }
            });

            if (visitados.ContainsKey(destino))
                break;

            actualNivel = new List<int>(siguienteNivel);
        }

        if (!visitados.ContainsKey(destino))
            return null;

        // reconstruir camino
        var camino = new List<int>();
        int actual = destino;

        while (actual != -1)
        {
            camino.Add(actual);
            actual = visitados[actual];
        }

        camino.Reverse();
        return camino;
    }

    public List<int> DFS(int inicio, int destino)
    {
        var visitados = new HashSet<int>();
        var pila = new Stack<List<int>>();

        pila.Push(new List<int> { inicio });

        while (pila.Count > 0)
        {
            var camino = pila.Pop();
            int nodoActual = camino[^1];

            if (nodoActual == destino)
                return camino;

            if (visitados.Add(nodoActual))
            {
                foreach (var vecino in GetNeighbors(nodoActual))
                {
                    var nuevoCamino = new List<int>(camino) { vecino };
                    pila.Push(nuevoCamino);
                }
            }
        }

        return null;
    }

    public List<int> DFSParalelo(int inicio, int destino, int maxHilos)
    {
        var visitados = new ConcurrentDictionary<int, bool>();
        var resultado = new ConcurrentBag<List<int>>();

        var vecinosIniciales = GetNeighbors(inicio);

        Parallel.ForEach(vecinosIniciales, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxHilos
        },
        (vecino, state) =>
        {
            var pila = new Stack<List<int>>();
            pila.Push(new List<int> { inicio, vecino });

            while (pila.Count > 0 && resultado.IsEmpty)
            {
                var camino = pila.Pop();
                int nodoActual = camino[^1];

                if (nodoActual == destino)
                {
                    resultado.Add(camino);
                    state.Stop(); 
                    return;
                }

                if (visitados.TryAdd(nodoActual, true))
                {
                    foreach (var sig in GetNeighbors(nodoActual))
                    {
                        var nuevoCamino = new List<int>(camino) { sig };
                        pila.Push(nuevoCamino);
                    }
                }
            }
        });

        return resultado.IsEmpty ? null : new List<List<int>>(resultado)[0];
    }

}

