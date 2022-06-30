[![master Actions Status](https://github.com/wwdenis/DatasetRefactor/workflows/Test/badge.svg)](https://github.com/wwdenis/DatasetRefactor/actions)

# DatasetRefactor
Convert ADO .NET Typed Datasets to plain code

## Introduction
`DatasetRefactor` scans a .NET Assembly for TypedDatasets and rewrite them to plain code.

It uses the power of [HashScript](https://github.com/wwdenis/HashScript) for templating.

## Command Syntax

```
DatasetRefactor assemblyFile=[assembly] outputRoot=[directory] templateRoot=[directory] saveData=[0/1] filterFile=[file] rootNamespace=[namespace]
```

## Command Arguments

| Argument | Description |
| -- | --------- |
| **assemblyFile** | The source .NET Assembly |
| **outputRoot** | The destination directory for the generated code |
| **templateRoot** | The directory containing custom `HashScript` templates |
| **saveData** | When set to `1` saves the DataSet structure in `JSON` format |
| **filterFile** | A comma-separated file containing a list of TableAdapters/Methods to be generated |
| **rootNamespace** | Overrides the source .NET Assembly Root Namespace on the refactored code |

## Example

Scans the assembly `AdventureWorkds.dll` and saves the generated code to `C:\Target` using the default `HashScript` templates

```
DatasetRefactor assemblyFile=C:\Source\AdventureWorkds.dll outputRoot=C:\Target
```

Scans the assembly `AdventureWorkds.dll` and saves the generated code `C:\Target` using custom `HashScript` templates, and custom filter and namespace

```
DatasetRefactor assemblyFile=C:\Source\AdventureWorkds.dll outputRoot=C:\Target templateRoot=C:\Templates saveData=1 filterFile=MyFilter.txt rootNamespace=MyCustomNamespace
```