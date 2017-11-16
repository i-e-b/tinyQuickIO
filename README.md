tinyQuickIO
===========

A conversion of https://github.com/SchwabenCode/QuickIO to reduce the code size. This is designed to be a more minimal project to be embedded into installers and similar tools.

A lot of the helpful interfaces, overloaded methods and meaningful exceptions are removed for the sake of code size.

This project will not follow changes to QuickIO.


Getting started
---------------

Most of what you need are static methods on the NativeIO object.

```csharp
using Native;
. . .

NativeIO.CreateDirectory(new PathInfo(@"your\path\here"), recursive: true);
NativeIO.DeleteDirectory(new DirectoryDetail(@"your"), recursive: true);
var names = NativeIO.EnumerateFiles(PullRequestRoot, ResultType.DirectoriesOnly).Select(f=>f.Name).ToList();

if (NativeIO.SymbolicLink.IsSymLink(srcFile)) { . . . }

```

Look at the test suites for some examples.
Most calls are based on the `NativeIO` static object in the `Native` namespace.

