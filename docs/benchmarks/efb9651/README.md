# Базовый замер чтения DBF Harbour на 10 000 записей

Замеры выполнены 19 ноября 2026 года. Исходники библиотеки и генератора соответствуют коммиту `efb9651b2c35f8e6b03d779e2e6191d33cd2ba40`. Стенд использует BenchmarkDotNet 0.15.8 и `MemoryDiagnoser`. [Проект стенда](../../../dBASE.NET.Tests/benchmarks/Harbour/HarbourBenchmarks.csproj), [сценарии чтения](../../../dBASE.NET.Tests/benchmarks/Harbour/HarbourReadBenchmarks.cs), [генератор Harbour](../../../dBASE.NET.Tests/fixtures/harbour/generate.prg)

## Условия замера

Windows 10 22H2 (10.0.19045.6456), Intel Core i7-12700K (12 физических ядер, 20 логических), .NET SDK 8.0.100. Для каждого сценария настроены один запуск процесса, три итерации прогрева и восемь измерительных итераций с целевой длительностью 200 мс. Прогоны выполнены последовательно в Release, с Concurrent Workstation GC и прогретым файловым кэшем Windows. Обе платформы заняли около 35 секунд вместе со сборкой запускаемых BenchmarkDotNet проектов.

| Цель стенда | Сборка dBASE.NET | Фактический рантайм | Отчёт BenchmarkDotNet |
| --- | --- | --- | --- |
| `net472` | `net462` | .NET Framework 4.8.1 (4.8.9310.0), x64 | [JSON](net472-10000.json) |
| `net8.0` | `netstandard2.0` | .NET 8.0.0 (8.0.23.53103), x64 | [JSON](net8.0-10000.json) |

Таблица `test_main` сгенерирована Harbour 3.2.0dev (r2503200530) через DBFCDX с кодовой страницей UA866. Стенд читает в CP866. DBF имеет версию `0xF5`, длина записи 169 байт. Для обеих платформ использована одна и та же пара файлов. При повторной генерации дата заголовка DBF может изменить SHA-256.

| Файл | Размер, байт | SHA-256 |
| --- | ---: | --- |
| `test_main.dbf` | 1 690 451 | `3173b2d9ecc60dfd66af17de37f265e6467136207ca8fa2dbe0db9f1ff1770b5` |
| `test_main.fpt` | 183 872 | `1fa8e8dcff0963072702b125cf12b20cb9ae5af14b3934e9dedeb8a3e1e35326` |

## Результаты

Для чтения одна операция обходит все 10 000 физических записей. Аллокации указаны в байтах на запись, для открытия — в байтах на открытие и закрытие. `Error` — половина 99,9%-го доверительного интервала BenchmarkDotNet. `N` — число измерительных итераций, которые вошли в статистику.

| Цель | Сценарий | Среднее ± Error, мс | Строк/с | Аллокации, байт | N |
| --- | --- | ---: | ---: | ---: | ---: |
| `net472` | Открытие и закрытие | 0,05660 ± 0,00130 | — | 16 761 | 6 |
| `net472` | Полный проход `GetRecord` | 31,329 ± 0,396 | 319 196 | 2 694,28 | 6 |
| `net472` | `GetValue` для `ID` и `IS_PRINTED` | 19,586 ± 0,214 | 510 567 | 271,98 | 8 |
| `net472` | `GetValue` для `NOTE` | 12,630 ± 0,624 | 791 764 | 167,66 | 7 |
| `net8.0` | Открытие и закрытие | 0,03176 ± 0,00162 | — | 15 456 | 7 |
| `net8.0` | Полный проход `GetRecord` | 12,691 ± 1,448 | 787 974 | 2 524,56 | 8 |
| `net8.0` | `GetValue` для `ID` и `IS_PRINTED` | 1,847 ± 0,122 | 5 413 009 | 223,99 | 8 |
| `net8.0` | `GetValue` для `NOTE` | 1,482 ± 0,231 | 6 746 926 | 165,71 | 8 |

На .NET 8 BenchmarkDotNet отметил, что минимальная итерация полного чтения составила 93,9 мс при рекомендуемых 100 мс. Для полного чтения и чтения memo на этом рантайме `Error` составляет около 11% и 16% соответственно. Небольшой выигрыш в этих сценариях стоит подтвердить повторным сравнением.

## Воспроизведение

Из корня репозитория выполните команды с путями к своей установке Harbour и BCC. Нужны .NET 8 SDK и ссылочные сборки .NET Framework 4.7.2. Для сравнения с этой серией используйте тот же фактический рантайм.

```powershell
$generator = './dBASE.NET.Tests/fixtures/harbour/New-HarbourFixtures.ps1'
$bench = './dBASE.NET.Tests/benchmarks/Harbour/HarbourBenchmarks.csproj'
$output = './dBASE.NET.Tests/bin/harbour-bdn-efb9651'
$toolchain = @{
    Hbmk2 = 'C:/MiniGUI_2607/harbour/bin/hbmk2.exe'
    Compiler = 'bcc'
    CompilerBin = 'C:/borland/bcc58/bin'
    CompilerLib = 'C:/borland/bcc58/lib/PSDK'
}

git diff --exit-code efb9651 -- dBASE.NET dBASE.NET.Tests/fixtures/harbour/generate.prg dBASE.NET.Tests/fixtures/harbour/New-HarbourFixtures.ps1
if ($LASTEXITCODE -ne 0) { throw 'Library or fixture generator differs from the baseline' }

$datasetDir = Join-Path $output '10000'
if (!(Test-Path -LiteralPath $datasetDir)) {
    & $generator -OutputDirectory $datasetDir -BenchmarkRows 10000 @toolchain
}
$dataset = (Resolve-Path -LiteralPath (Join-Path $datasetDir 'test_main.dbf')).Path

dotnet build $bench -c Release
if ($LASTEXITCODE -ne 0) { throw 'Benchmark build failed' }

foreach ($target in 'net472', 'net8.0') {
    $artifacts = Join-Path (Resolve-Path -LiteralPath $output).Path "artifacts-10000/$target"
    dotnet run -c Release -f $target --no-build --project $bench -- $dataset --filter '*' --runtimes $target --launchCount 1 --warmupCount 3 --iterationCount 8 --iterationTime 200 --artifacts $artifacts
    if ($LASTEXITCODE -ne 0) { throw "Benchmark failed for $target" }
}
```

BenchmarkDotNet пишет `HarbourReadBenchmarks-report-brief.json` в подкаталог `results` каждого прогона. Для замера после оптимизации используйте ту же пару DBF/FPT, те же настройки запуска и тот же фактический рантайм.
