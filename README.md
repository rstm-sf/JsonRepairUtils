[![Nuget](https://img.shields.io/nuget/v/https%3A%2F%2Fwww.nuget.org%2Fpackages%2FJsonRepairUtils)](https://www.nuget.org/packages/JsonRepairUtils)
[![.NET](https://github.com/rstm-sf/JsonRepairUtils/actions/workflows/dotnet_test.yml/badge.svg)](https://github.com/rstm-sf/JsonRepairUtils/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

# JsonRepairUtils

JsonRepairUtils is a near-literal translation of the TypeScript JsonRepair library, see https://github.com/josdejong/jsonrepair

The jsonrepair library is basically an extended JSON parser. It parses the provided JSON document character by character. When it encounters a non-valid JSON structures it wil look to see it it can reconstruct the intended JSON. For example, after encountering an opening bracket `{`, it expects a key wrapped in double quotes. When it encounters a key without quotes, or wrapped in other quote characters, it will change these to double quotes instead.

The library has many uses, such as:

- Convert from an a Word document
- Convert from objects with a JSON-like structure, such as Javascript
- Convert from a string containing a JSON document
- Convert from MongoDB output
- Convert from Newline Delimited JSON logs
- Convert from JSON dialects
- Convert from Truncated or corrupted JSON.

*The library can fix the  following issues:*

- Add missing quotes around keys
- Add missing escape characters
- Add missing commas
- Add missing closing brackets
- Replace single quotes with double quotes
- Replace special quote characters like `“...”`  with regular double quotes
- Replace special white space characters with regular spaces
- Replace Python constants `None`, `True`, and `False` with `null`, `true`, and `false`
- Strip trailing commas
- Strip comments like `/* ... */` and `// ...`
- Strip JSONP notation like `callback({ ... })`
- Strip escape characters from an escaped string like `{\"stringified\": \"content\"}`
- Strip MongoDB data types like `NumberLong(2)` and `ISODate("2012-12-19T06:01:17.171Z")`
- Concatenate strings like `"long text" + "more text on next line"`
- Turn newline delimited JSON into a valid JSON array, for example:
    ```
    { "id": 1, "name": "John" }
    { "id": 2, "name": "Sarah" }
    ```


## Use

Use the original typescript version in a full-fledged application: https://jsoneditoronline.org
Read the background article ["How to fix JSON and validate it with ease"](https://jsoneditoronline.org/indepth/parse/fix-json/)

## Code example

```cs
var jsonRepair = JsonRepair();

// Enable throwing exceptions when JSON code can not be repaired or even understood (enabled by default)
jsonRepair.ThrowExceptions = true;

try
{
     // The following is invalid JSON: is consists of JSON contents copied from 
     // a JavaScript code base, where the keys are missing double quotes, 
     // and strings are using single quotes:
     string json = "{name: 'John'}";
     string repaired = jsonRepair.Repair(json);
     Console.WriteLine(repaired);
     // Output: {"name": "John"}
}
catch (JsonRepairError err)
{
     Console.WriteLine(err.Message);
     Console.WriteLine("Position: " + err.Data["Position"]);
}
```

## Alternatives:

Similar libraries:

- https://github.com/thijse/JsonRepairSharp (initial)
- https://github.com/josdejong/jsonrepair
- https://github.com/RyanMarcus/dirty-json

## Acknowledgements

Thanks go out to Jos de Jong, who not only did all the heavy lifting in the original jsonrepair for typescript library, but also patiently helped getting this port to pass all unit tests. 

## License

Released under the [MITS license](LICENSE.md).
