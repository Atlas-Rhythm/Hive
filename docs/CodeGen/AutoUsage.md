# Automatic Usage

Code generators (and `Hive.Utilities` references) are automatically added to all projects in the solution.
In general, tweaks should be made primarily to `Directory.Build.*` or `Hive.CodeGen/build/Hive.CodeGen.*`,
unless there is a specific reason for it to be applied to a single project.