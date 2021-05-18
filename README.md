### Description

![View](view.jpg)

Console application to benchmark request dynamic of different databases to compare it with each other.
It supports three scenaries: Select, Insert One and Insert Many.

### Usage
First of all, describe `contract.csv` with model that will be constructed dynamicly and fullfiled with random data. This file has typical csv format
where first column is expected property (column) name that will available as sql parameter in each sql script.
Second column is data type that may be restricted with max data length, for example - string.
Common description of property is
```
[property_name]; [clr_type_name](max_length);
```
_Max length may be omitted so file will stores only two names separated with semicolumn - name of property and name of clr type._
Supported CLR types:
1.  Guid
2.  Byte
3.  Short
4.  Int
5.  Long
6.  Float
7.  Decimal
8.  DateTime
9.  String
10. Bool

Next step is preparing of `.sql` files that will used during benchmarking. They separates by directories with the same names like 
database used to execute it. It's available `ClickHouse` and `PostgreSql` providers now. This directories may be changed in `appsettings.json` file.

Syntax inside `sql` files relates to providers used to execute it. For `ClickHouse` it is [ClickHouseClient](https://github.com/Octonica/ClickHouseClient), 
for `PostgreSql` is [npgsql](https://github.com/npgsql/npgsql).

Finally needs to start application and go to drink coffe. During this application will executing three steps:
1. **prologue** - preparing database;
2. **middle (body)** - main test plan [unlimited! Information below];
3. **epilogue** - shutdown/cleanup plan.

Moreover each of this steps may be omitted. It's configured through cmd args:
`benchmark.exe -all` 
or `benchmark.exe -wp -wm -we` 
or even `benchmark.exe --withPrologue --withMiddle --withEpilogue`- executes all steps. 
_By default no steps included_.  
Middle step is unlimited. It works during process didn't terminated through `CTL+C` after that gauge data will flushed on disk, aggregated and displayed in separated for each database window.

