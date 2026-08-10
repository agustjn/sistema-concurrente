# Cache lectores/escritores: preferencia a lectores vs politica justa

Parametros fijos: 20,000 ordenes, 5,000 iteraciones, 3 productores + 3 consumidores (= 6 escritores de cache), buffer 10.
Cada celda promedia 3 corridas (se descarta una corrida previa de warm-up). Timeout por corrida: 90 s.
Hardware: 8 procesadores logicos. Fecha: 2026-08-10.

Esc = espera de un ESCRITOR para completar una escritura en la cache (el indicador de inanicion).
Lect = espera de un LECTOR para completar una lectura. Lect/s = lecturas servidas por segundo.

## L0 - Sin lectores (referencia)

| Variante | Tiempo (s) | Throughput (ord/s) | Esc prom (ms) | Esc p95 (ms) | Esc max (ms) | Lect prom (ms) | Lect/s |
|---|---|---|---|---|---|---|---|
| SemaforosLectores | 0.638 | 31,366 | 0.000 | 0.000 | 1.0 | 0.000 | 0 |
| SemaforosJusta | 0.617 | 32,422 | 0.001 | 0.001 | 0.7 | 0.000 | 0 |
| MonitoresLectores | 0.639 | 31,302 | 0.000 | 0.000 | 0.7 | 0.000 | 0 |
| MonitoresJusta | 0.611 | 32,760 | 0.000 | 0.000 | 0.8 | 0.000 | 0 |

## L1 - Ratio 1:1 (6 lectores, 1 ms)

| Variante | Tiempo (s) | Throughput (ord/s) | Esc prom (ms) | Esc p95 (ms) | Esc max (ms) | Lect prom (ms) | Lect/s |
|---|---|---|---|---|---|---|---|
| SemaforosLectores | 0.652 | 30,707 | 0.001 | 0.000 | 1.1 | 0.086 | 377 |
| SemaforosJusta | 0.655 | 30,543 | 0.001 | 0.001 | 1.0 | 0.129 | 397 |
| MonitoresLectores | 0.620 | 32,273 | 0.001 | 0.000 | 1.0 | 0.088 | 384 |
| MonitoresJusta | 0.629 | 31,831 | 0.001 | 0.000 | 0.9 | 0.163 | 375 |

## L2 - Ratio 4:1 (24 lectores, 1 ms)

| Variante | Tiempo (s) | Throughput (ord/s) | Esc prom (ms) | Esc p95 (ms) | Esc max (ms) | Lect prom (ms) | Lect/s |
|---|---|---|---|---|---|---|---|
| SemaforosLectores | 0.684 | 29,271 | 0.001 | 0.001 | 1.6 | 0.073 | 1,527 |
| SemaforosJusta | 0.759 | 26,407 | 0.003 | 0.001 | 10.3 | 0.329 | 1,527 |
| MonitoresLectores | 0.719 | 27,813 | 0.002 | 0.001 | 1.3 | 0.069 | 1,480 |
| MonitoresJusta | 0.734 | 27,239 | 0.002 | 0.003 | 1.0 | 0.679 | 1,493 |

## L3 - Ratio 8:1 (48 lectores, 1 ms)

| Variante | Tiempo (s) | Throughput (ord/s) | Esc prom (ms) | Esc p95 (ms) | Esc max (ms) | Lect prom (ms) | Lect/s |
|---|---|---|---|---|---|---|---|
| SemaforosLectores | 0.742 | 26,977 | 0.003 | 0.000 | 1.8 | 0.091 | 2,982 |
| SemaforosJusta | 0.739 | 27,084 | 0.003 | 0.001 | 1.6 | 0.425 | 3,055 |
| MonitoresLectores | 0.724 | 27,640 | 0.002 | 0.001 | 2.0 | 0.060 | 3,035 |
| MonitoresJusta | 0.780 | 25,659 | 0.006 | 0.010 | 1.3 | 2.124 | 3,010 |

## L4 - Presion maxima (48 lectores, 0 ms)

| Variante | Tiempo (s) | Throughput (ord/s) | Esc prom (ms) | Esc p95 (ms) | Esc max (ms) | Lect prom (ms) | Lect/s |
|---|---|---|---|---|---|---|---|
| SemaforosLectores | INANICION: no completo en 90 s | — | — | — | — | — | — |
| SemaforosJusta | 3.804 | 5,259 | 0.207 | 0.353 | 43.8 | 0.278 | 158,801 |
| MonitoresLectores | INANICION: no completo en 90 s | — | — | — | — | — | — |
| MonitoresJusta | 1.399 | 14,300 | 0.056 | 0.328 | 4.6 | 4.979 | 9,718 |

