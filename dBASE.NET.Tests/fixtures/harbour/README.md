# Фикстуры Harbour

В `data/` находятся семь DBF, связанные FPT и JSON-эталоны. Таблицы созданы Harbour 3.2.0dev (r2503200530) через `DBFCDX`. Обычные тесты берут готовые файлы и запускаются через MSTest вместе с остальными тестами решения.

Схемы `basic`, `special`, `numeric` и `wide` адаптированы по сценариям `HarbourDBF.NET/tests/generate-fixtures.prg` и `complex-fixture.prg`. Схема платежей и граничные значения timestamp взяты из `read-dbf/MakeData/import-csv.prg` и `read-dbf/tests/timestamp-fixture.prg`. Значения платежей формируются по фиксированным формулам в Harbour. Файлы `empty` и `single` используют ту же схему.

`capture.prg` открывает каждый DBF отдельно для чтения и выгружает записи в физическом порядке, включая записи с меткой удаления. В JSON остаются исходные пробелы в полях `C` и `M`. Тест убирает правое заполнение `C` и сопоставляет пустые `C` и `M` с `null` в .NET. Пустые даты и timestamp выгружаются как `null`. Числа выгружаются строками. `wide.json` фиксирует точное значение Harbour, а тест отдельно проверяет результат в рамках текущего контракта `double`.

## Пересоздание на Windows

Из корня репозитория запустите следующие команды с путями к своей установке Harbour и BCC.

```powershell
$generator = './dBASE.NET.Tests/fixtures/harbour/New-HarbourFixtures.ps1'
$toolchain = @{
    Hbmk2 = 'C:/MiniGUI_2607/harbour/bin/hbmk2.exe'
    Compiler = 'bcc'
    CompilerBin = 'C:/borland/bcc58/bin'
    CompilerLib = 'C:/borland/bcc58/lib/PSDK'
}
& $generator -OutputDirectory ./dBASE.NET.Tests/bin/harbour-fixtures @toolchain
```

Скрипт принимает новый выходной каталог. После проверки перенесите DBF, FPT и соответствующие JSON вместе в `data/`. При каждом запуске могут меняться дата в заголовке DBF и время модификации `=`, поэтому обновляйте эталон из той же генерации.

## Замеры

```powershell
$bench = './dBASE.NET.Tests/benchmarks/Harbour/HarbourBenchmarks.csproj'
dotnet run -c Release --project $bench -- ./dBASE.NET.Tests/fixtures/harbour/data/test_main.dbf

foreach ($rows in 100000, 1000000) {
    $output = "./dBASE.NET.Tests/bin/harbour-$rows"
    & $generator -OutputDirectory $output -BenchmarkRows $rows @toolchain
    dotnet run -c Release --project $bench -- "$output/test_main.dbf"
}
```

Замер прогревает каждую операцию и выводит медиану трёх прогонов. Для открытия один прогон состоит из 100 открытий с закрытием, в таблице указано время на каждое. Для чтения один прогон обходит все физические записи. Аллокации считаются для потока, выполняющего замер.

Первый локальный замер на Windows, .NET 8.0.0 x64, Intel Core i7-12700K. В таблице указаны миллисекунды и выделенные байты на операцию или полный проход.

| Операция | 1 000 записей, мс / байт | 100 000 записей, мс / байт | 1 000 000 записей, мс / байт |
| --- | ---: | ---: | ---: |
| Открытие и закрытие | 0,046 / 15 440 | 0,062 / 15 504 | 0,059 / 15 504 |
| `GetRecord` | 3,589 / 2 522 616 | 163,466 / 252 467 744 | 1 264,334 / 2 533 369 656 |
| `GetValue` для `ID` и `IS_PRINTED` | 0,409 / 223 928 | 21,582 / 22 399 936 | 218,039 / 231 199 936 |
| `GetValue` для `NOTE` | 0,261 / 165 600 | 18,290 / 16 571 360 | 189,612 / 167 200 248 |
