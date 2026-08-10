using System.Diagnostics;
using System.Globalization;
using System.Text;
using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Cache.CacheMonitores;
using SistemaConcurrente.Core.Cache.CacheSemaforos;
using SistemaConcurrente.Core.Coordinadores;

namespace SistemaConcurrente.Mediciones
{
    // Harness del experimento "proporcion de lecturas/escrituras sobre la cache" (secc. 7).
    //
    // Compara las dos politicas de la cache (preferencia a lectores vs justa) en sus dos
    // mecanismos (semaforos y monitores), barriendo la cantidad de hilos lectores.
    //
    // Funciona en dos modos:
    //   - SIN argumentos (modo padre): orquesta todas las corridas. Cada corrida individual
    //     se lanza como un PROCESO hijo con timeout. El motivo: bajo preferencia a lectores
    //     y presion de lectura sostenida puede haber inanicion REAL de escritores (la corrida
    //     no termina nunca). Un proceso se puede matar; un Thread de una corrida colgada, no.
    //     Un timeout vencido no es un error del harness: ES el resultado (inanicion).
    //   - CON argumentos (modo hijo): ejecuta UNA corrida y escupe una linea RESULTADO;...
    //
    // Salidas (en Mediciones/Resultados/):
    //   - lectores-vs-escritores-crudo.csv : una fila por corrida (datos crudos)
    //   - lectores-vs-escritores-resumen.md: tablas promediadas listas para el informe
    //   - log.txt                          : progreso y detalle de cada corrida
    public static class Program
    {
        // ---------------- Parametros base (fijos en todo el experimento) ----------------
        // Escritores: 3 productores + 3 consumidores = 6 hilos que escriben la cache.
        // Volumen/computo elegidos para que cada corrida dure ~1-2 s: suficiente para que
        // la presion de lectura sea sostenida, sin estirar el total del experimento.
        const int Ordenes = 20_000;
        const int Iteraciones = 5_000;
        const int Productores = 3;
        const int Consumidores = 3;
        const int CapacidadBuffer = 10;

        // Corridas por combinacion: 1 warm-up (JIT, se descarta) + Medidas que se promedian.
        const int Medidas = 3;

        // Si una corrida no termina en este tiempo, se la mata y se registra INANICION.
        // Referencia: sin lectores, la misma carga termina en ~1-2 s (ver escenario L0).
        const int TimeoutSegundos = 90;

        // ---------------- Escenarios: el barrido de proporcion lectores/escritores ----------------
        // (nombre, cantLectores, intervaloMs entre lecturas de cada lector)
        // Los lectores se dimensionan como multiplo k de los 6 escritores: k = 0, 1, 4, 8.
        // El ultimo escenario repite k=8 pero con intervalo 0 (lectores sin pausa): presion
        // maxima sostenida, el caso disenado para exponer la inanicion.
        static readonly (string Nombre, int Lectores, int IntervaloMs)[] Escenarios =
        {
            ("L0 - Sin lectores (referencia)",          0,  0),
            ("L1 - Ratio 1:1 (6 lectores, 1 ms)",       6,  1),
            ("L2 - Ratio 4:1 (24 lectores, 1 ms)",     24,  1),
            ("L3 - Ratio 8:1 (48 lectores, 1 ms)",     48,  1),
            ("L4 - Presion maxima (48 lectores, 0 ms)",48,  0),
        };

        static readonly string[] Variantes =
        {
            "SemaforosLectores", "SemaforosJusta", "MonitoresLectores", "MonitoresJusta"
        };

        public static int Main(string[] args)
        {
            return args.Length == 0 ? EjecutarComoPadre() : EjecutarComoHijo(args);
        }

        // ============================ MODO PADRE ============================
        static int EjecutarComoPadre()
        {
            string raizRepo = BuscarRaizRepo();
            string dirResultados = Path.Combine(raizRepo, "Mediciones", "Resultados");
            Directory.CreateDirectory(dirResultados);

            var log = new StreamWriter(Path.Combine(dirResultados, "log.txt"), append: false) { AutoFlush = true };
            void Log(string msg)
            {
                Console.WriteLine(msg);
                log.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
            }

            Log($"Experimento lectores vs escritores | {Ordenes} ordenes, {Iteraciones} iteraciones, " +
                $"{Productores}P+{Consumidores}C, buffer {CapacidadBuffer} | warm-up + {Medidas} medidas | timeout {TimeoutSegundos}s");
            Log($"Hardware: {Environment.ProcessorCount} procesadores logicos | .NET {Environment.Version} | {DateTime.Now:yyyy-MM-dd}");
            Log("");

            var csv = new StringBuilder();
            csv.AppendLine("escenario;variante;rep;esWarmup;inanicion;ordenes;tiempoSeg;throughput;latenciaPromMs;" +
                           "escrituras;escPromMs;escP95Ms;escMaxMs;lecturas;lecPromMs;lecP95Ms;lecMaxMs;lecturasPorSeg");

            // resultados agregados por (escenario, variante)
            var agregados = new Dictionary<(string, string), List<ResultadoCorrida>>();
            var inaniciones = new HashSet<(string, string)>();

            foreach (var esc in Escenarios)
            {
                foreach (var variante in Variantes)
                {
                    var clave = (esc.Nombre, variante);
                    agregados[clave] = new List<ResultadoCorrida>();

                    for (int rep = 0; rep <= Medidas; rep++)  // rep 0 = warm-up
                    {
                        bool esWarmup = rep == 0;
                        Log($"{esc.Nombre} | {variante} | corrida {rep}{(esWarmup ? " (warm-up)" : "")}...");

                        var (resultado, huboInanicion) = LanzarCorridaHija(variante, esc.Lectores, esc.IntervaloMs, Log);

                        if (huboInanicion)
                        {
                            // La corrida no termino: eso ES el dato. Se registra y no se insiste
                            // con mas repeticiones (tardarian lo mismo en no terminar).
                            inaniciones.Add(clave);
                            csv.AppendLine(string.Join(';', esc.Nombre, variante, rep, esWarmup ? 1 : 0, 1,
                                Ordenes, "", "", "", "", "", "", "", "", "", "", "", ""));
                            Log($"   >>> INANICION: no completo en {TimeoutSegundos}s. Se saltean las repeticiones restantes.");
                            break;
                        }

                        var r = resultado!;
                        csv.AppendLine(FormattableString.Invariant(
                            $"{esc.Nombre};{variante};{rep};{(esWarmup ? 1 : 0)};0;{r.Ordenes};{r.TiempoSeg:F3};{r.Throughput:F0};{r.LatenciaPromMs:F3};{r.Escrituras};{r.EscPromMs:F4};{r.EscP95Ms:F4};{r.EscMaxMs:F2};{r.Lecturas};{r.LecPromMs:F4};{r.LecP95Ms:F4};{r.LecMaxMs:F2};{r.LecturasPorSeg:F0}"));

                        if (!esWarmup) agregados[clave].Add(r);
                        Log(FormattableString.Invariant(
                            $"   ok: {r.TiempoSeg:F3}s | esc prom {r.EscPromMs:F3} ms / p95 {r.EscP95Ms:F3} / max {r.EscMaxMs:F1} | lecturas/s {r.LecturasPorSeg:F0}"));
                    }
                }
                Log("");
            }

            File.WriteAllText(Path.Combine(dirResultados, "lectores-vs-escritores-crudo.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(dirResultados, "lectores-vs-escritores-resumen.md"),
                ArmarResumen(agregados, inaniciones));

            Log("Listo. Resultados en Mediciones/Resultados/");
            log.Dispose();
            return 0;
        }

        static (ResultadoCorrida?, bool inanicion) LanzarCorridaHija(string variante, int lectores, int intervaloMs, Action<string> log)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                Arguments = string.Join(' ', variante, Ordenes, Iteraciones, Productores, Consumidores,
                                             lectores, intervaloMs, CapacidadBuffer),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var proceso = Process.Start(psi)!;
            string salida = "";
            var lectura = Task.Run(() => salida = proceso.StandardOutput.ReadToEnd());

            if (!proceso.WaitForExit(TimeoutSegundos * 1000))
            {
                proceso.Kill(entireProcessTree: true);
                return (null, true);
            }
            lectura.Wait();

            var linea = salida.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("RESULTADO;"));
            if (linea == null)
            {
                log("   ERROR: la corrida hija no devolvio resultado. stderr: " + proceso.StandardError.ReadToEnd());
                throw new InvalidOperationException("Corrida hija sin resultado");
            }

            var c = linea.Split(';');
            double D(int i) => double.Parse(c[i], CultureInfo.InvariantCulture);
            int I(int i) => int.Parse(c[i], CultureInfo.InvariantCulture);

            return (new ResultadoCorrida(I(1), D(2), D(3), D(4), I(5), D(6), D(7), D(8), I(9), D(10), D(11), D(12)), false);
        }

        // ============================ MODO HIJO ============================
        // args: variante ordenes iteraciones productores consumidores lectores intervaloMs buffer
        static int EjecutarComoHijo(string[] args)
        {
            string variante = args[0];
            int ordenes = int.Parse(args[1]);
            int iteraciones = int.Parse(args[2]);
            int productores = int.Parse(args[3]);
            int consumidores = int.Parse(args[4]);
            int lectores = int.Parse(args[5]);
            int intervaloMs = int.Parse(args[6]);
            int buffer = int.Parse(args[7]);

            ICache cacheReal = variante switch
            {
                "SemaforosLectores" => new CacheSemaforosLectores(),
                "SemaforosJusta" => new CacheSemaforosJusta(),
                "MonitoresLectores" => new CacheMonitoresLectores(),
                "MonitoresJusta" => new CacheMonitoresJusta(),
                _ => throw new ArgumentException("Variante desconocida: " + variante)
            };
            // El buffer acompania al mecanismo de la cache (como en los 4 endpoints de la API).
            IBuffer buf = variante.StartsWith("Semaforos")
                ? new BufferSemaforos(buffer)
                : new BufferMonitores(buffer);

            var cache = new CacheConMediciones(cacheReal);

            var service = new ConfigurationRunService(productores, consumidores, iteraciones, ordenes,
                                                      lectores, cache, buf, intervaloMs);
            ResultadoEjecucion resultado = service.Ejecutar().GetAwaiter().GetResult();

            var esc = cache.DuracionEscriturasMs.OrderBy(x => x).ToList();
            var lec = cache.DuracionLecturasMs.OrderBy(x => x).ToList();
            double latenciaProm = resultado.Ordenes.Count > 0 ? resultado.Ordenes.Average(o => o.LatenciaMs ?? 0) : 0;
            double t = resultado.TiempoTotalSegundos;

            Console.WriteLine(FormattableString.Invariant(
                $"RESULTADO;{resultado.Ordenes.Count};{t:F4};{(t > 0 ? resultado.Ordenes.Count / t : 0):F1};{latenciaProm:F4};{esc.Count};{Promedio(esc):F5};{Percentil(esc, 0.95):F5};{Maximo(esc):F3};{lec.Count};{Promedio(lec):F5};{Percentil(lec, 0.95):F5};{Maximo(lec):F3}"));
            return 0;
        }

        static double Promedio(List<double> xs) => xs.Count == 0 ? 0 : xs.Average();
        static double Maximo(List<double> xs) => xs.Count == 0 ? 0 : xs[^1];

        // Mismo criterio que CalculadoraMetricas.Percentil (lista ya ordenada, p en [0,1]).
        static double Percentil(List<double> ordenados, double p)
        {
            if (ordenados.Count == 0) return 0;
            int idx = (int)Math.Ceiling(p * ordenados.Count) - 1;
            return ordenados[Math.Clamp(idx, 0, ordenados.Count - 1)];
        }

        // ============================ RESUMEN ============================
        record ResultadoCorrida(int Ordenes, double TiempoSeg, double Throughput, double LatenciaPromMs,
                                int Escrituras, double EscPromMs, double EscP95Ms, double EscMaxMs,
                                int Lecturas, double LecPromMs, double LecP95Ms, double LecMaxMs)
        {
            public double LecturasPorSeg => TiempoSeg > 0 ? Lecturas / TiempoSeg : 0;
        }

        static string ArmarResumen(Dictionary<(string, string), List<ResultadoCorrida>> agregados,
                                   HashSet<(string, string)> inaniciones)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("# Cache lectores/escritores: preferencia a lectores vs politica justa");
            sb.AppendLine();
            sb.AppendLine(FormattableString.Invariant(
                $"Parametros fijos: {Ordenes:N0} ordenes, {Iteraciones:N0} iteraciones, {Productores} productores + {Consumidores} consumidores (= 6 escritores de cache), buffer {CapacidadBuffer}."));
            sb.AppendLine(FormattableString.Invariant(
                $"Cada celda promedia {Medidas} corridas (se descarta una corrida previa de warm-up). Timeout por corrida: {TimeoutSegundos} s."));
            sb.AppendLine($"Hardware: {Environment.ProcessorCount} procesadores logicos. Fecha: {DateTime.Now:yyyy-MM-dd}.");
            sb.AppendLine();
            sb.AppendLine("Esc = espera de un ESCRITOR para completar una escritura en la cache (el indicador de inanicion).");
            sb.AppendLine("Lect = espera de un LECTOR para completar una lectura. Lect/s = lecturas servidas por segundo.");
            sb.AppendLine();

            foreach (var esc in Escenarios)
            {
                sb.AppendLine($"## {esc.Nombre}");
                sb.AppendLine();
                sb.AppendLine("| Variante | Tiempo (s) | Throughput (ord/s) | Esc prom (ms) | Esc p95 (ms) | Esc max (ms) | Lect prom (ms) | Lect/s |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|");

                foreach (var variante in Variantes)
                {
                    var clave = (esc.Nombre, variante);
                    if (inaniciones.Contains(clave))
                    {
                        sb.AppendLine($"| {variante} | INANICION: no completo en {TimeoutSegundos} s | — | — | — | — | — | — |");
                        continue;
                    }
                    var rs = agregados[clave];
                    if (rs.Count == 0) continue;

                    sb.AppendLine(string.Format(inv,
                        "| {0} | {1:F3} | {2:N0} | {3:F3} | {4:F3} | {5:F1} | {6:F3} | {7:N0} |",
                        variante,
                        rs.Average(r => r.TiempoSeg),
                        rs.Average(r => r.Throughput),
                        rs.Average(r => r.EscPromMs),
                        rs.Average(r => r.EscP95Ms),
                        rs.Max(r => r.EscMaxMs),
                        rs.Average(r => r.LecPromMs),
                        rs.Average(r => r.LecturasPorSeg)));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // El harness corre desde bin/... : sube hasta encontrar la carpeta Mediciones.
        static string BuscarRaizRepo()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Mediciones")))
                dir = dir.Parent;
            return dir?.FullName ?? Directory.GetCurrentDirectory();
        }
    }
}
