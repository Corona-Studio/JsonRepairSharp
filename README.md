# JsonRepairSharp [![CodeQL Advanced](https://github.com/Corona-Studio/JsonRepairSharp/actions/workflows/codeql.yml/badge.svg)](https://github.com/Corona-Studio/JsonRepairSharp/actions/workflows/codeql.yml)

Repair invalid JSON documents.

This is the C# version of the original project: [https://github.com/josdejong/jsonrepair](https://github.com/josdejong/jsonrepair)

Read the background article ["How to fix JSON and validate it with ease"](https://jsoneditoronline.org/indepth/parse/fix-json/)

## How to use?

```csharp
using JsonRepairSharp.Class;

// ...

var fixedJson = JsonRepairCore.JsonRepair(rawJson);

// ...

```

You can also catch the `JsonRepairError` exception to handle the case where the library failed to fix the JSON.

## Items to fix

- Add missing quotes around keys
- Add missing escape characters
- Add missing commas
- Add missing closing brackets
- Repair truncated JSON
- Replace single quotes with double quotes
- Replace special quote characters like “`...`” with regular double quotes
- Replace special white space characters with regular spaces
- Replace Python constants None, True, and False with null, true, and false
- Strip trailing commas
- Strip comments like `/* ... */` and `// ...`
- Strip ellipsis in arrays and objects like `[1, 2, 3, ...]`
- Strip JSONP notation like callback(`{ ... }`)
- Strip escape characters from an escaped string like `{\"stringified\": \"content\"}`
- Strip MongoDB data types like `NumberLong(2)` and `ISODate("2012-12-19T06:01:17.171Z")`
- Concatenate strings like `"long text" + "more text on next line"`
- Turn newline delimited JSON into a valid JSON array, for example:
  ```json
  { "id": 1, "name": "John" }
  { "id": 2, "name": "Sarah" }
  ```
- The JsonRepairSharp library has streaming support and can handle infinitely large documents.
