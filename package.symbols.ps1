&.\scripts\ensureNugetInstalled.ps1

nuget pack SQLitePCL.pretty\SQLitePCL.pretty.symbols.nuspec -symbols
nuget pack SQLitePCL.pretty.Async\SQLitePCL.pretty.Async.symbols.nuspec -symbols
nuget pack SQLitePCL.pretty.Orm\SQLitePCL.pretty.Orm.symbols.nuspec -symbols
