# azure-blob-searcher
High performance search of file and folder names within a blob 

Parallel task spawned per child folder found when fanning out from specified root folder looking for file or folder names.

Progress updates written asyncronously back to the UI.

Either use this as a UI tool or use just the search code if needing blob searching in you app code.

TODO: make the hard coded initial state folders dynamic.
