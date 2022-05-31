[![master Actions Status](https://github.com/wwdenis/DatasetRefactor/workflows/Test/badge.svg)](https://github.com/wwdenis/DatasetRefactor/actions)

# DatasetRefactor
Convert ADO .NET Typed Datasets to plain code

## Introduction
`DatasetRefactor` scans a .NET Assembly for TypedDatasets and rewrite them to plain code.

It uses the power of [HashScript](https://github.com/wwdenis/HashScript) for templating.

## Command Syntax

```
DatasetRefactor source=[assembly] target=[directory] templates=[directory] save=[0/1] filter=[filterFile]
```

## Command Arguments

| Argument | Description |
| -- | --------- |
| **source** | The source .NET Assembly |
| **target** | The destination directory for the generated code |
| **templates** | The directory containing custom `HashScript` templates |
| **save** | When set to `1` saves the DataSet structure in `JSON` format |
| **filter** | When set to a existing DataSet, generates only the code related to it |

## Example

Scans the assembly `AdventureWorkds.dll` and saves the generated code to `C:\Target` using the default `HashScript` templates

```
DatasetRefactor source=C:\Source\AdventureWorkds.dll target=C:\Target
```

Scans the assembly `AdventureWorkds.dll` and saves the generated code `C:\Target` using custom `HashScript` templates
Filters only the Dataset `HumanResourcesDataset`

```
DatasetRefactor source=C:\Source\AdventureWorkds.dll target=C:\Target templates=C:\Templates save=1 filter=HumanResourcesDataset
```