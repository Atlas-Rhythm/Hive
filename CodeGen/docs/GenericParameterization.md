# Generic Parameterization

We use a generator that automatically generates versions of a generic type with any number of fewer
arguments (greater than one and less than the number of existing parameters). It will automatically
fix usages (to the best of its ability), and is capable of generating many at a time.

Currently, it is used only for `Utilities.UnformattedString`.