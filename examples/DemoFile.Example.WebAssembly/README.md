

## DemoFile on WebAssembly

This project contains example how to run `DemoFile` library on WebAssembly (eg. in a Web browser).


## Instructions

Install `wasm-tools`: `dotnet workload install wasm-tools`

Build: `dotnet publish -c Release`

This creates a folder `bin/Release/net8.0/browser-wasm/AppBundle` which can be hosted on a Web server.

To host a Web server from dotnet:

Install `dotnet-serve`: `dotnet tool update dotnet-serve --global`

Run: `dotnet serve -d:bin/Release/net8.0/browser-wasm/AppBundle`

It will output URL that you can open in a Web browser.

See official MS [documentation](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop?view=aspnetcore-8.0).


## Notes

Project uses `<RunAOTCompilation>` flag because AOT build seems to be around 6 times faster than JIT.

Also, it's 3-4 times slower than Windows build.
