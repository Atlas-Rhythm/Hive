
### This file is for editing documentation in Visual Studio only.

When adding a file to the docs foler of a project in Visual Studio, make sure it
actually ends up in `/docs/Project/`, and not `/src/Project/docs/`.

If it does not do that by default, use File -> Save File As to save the file to
the appropriate directory, and remove the one in the source directory.

To add documentation to a project which does not already have it, simply create
in `/docs/` a directory with the same name as the project. Place all documentation
files there; they will be visible in their respective projects in Visual Studio.