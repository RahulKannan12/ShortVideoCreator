# ShortVideoCreator

There are two possible ways to run short video creator, one via console application other via User Interface created with Razor.

Before that, we have some pre-requisites 

 - dotnet SDK or runtime (version 8)
 - install FFMpeg
 - Other nuget packages (should be installed/restored on building the application)
 - Key to connect the Azure cognitive services

## Console Application

ShortVideoCreator.csproj is the console application project, before running that create a folder named *storage* inside the application folder (../bin/debug/net8.0)

Inside the storage folder 2 sub-folders named *video-library* and *pdf-files* should be created.

> If you run the application first time, it will create the above
> folders automatically for you.

Input content (pdf file) should be placed in pdf-files folder with the name "content-input.pdf" [ don't want to get custom name from the input and make it pass to the input, but yes, it can be done ]
Place some random content videos in the *video-library* folder, all should be in ***.mp4 format**

On running the application, it will create a Job folder (random 6 digit folder), where all the processed files will be available. After successful completion, the output video file would be available in the same folder.

## User Interface

ShortVideoCreator.Razor.FrontEnd.csproj is the FrontEnd project, it is required to be run, to access the interface.
2 values should be replaced in appsettings.json file

 - VideoLibraryFilePath - full path to the video library 
 example - /Users/rahul.kannan/RahulWorkspace/personal-projects/ShortVideoCreator/ShortVideoCreator/bin/Debug/net8.0/storage/video-library
  - ContentProcessingFilePath - full parent path [ storage folder ] 
 example - /Users/rahul.kannan/RahulWorkspace/personal-projects/ShortVideoCreator/ShortVideoCreator/bin/Debug/net8.0/storage

It has 2 menus

### Upload Videos
Option to upload videos to the library, uploaded videos will be stored in the local storage folder "video-library", post successful upload, a success message will be shown and routed to Home page.

[Screenshot]

### Generate Content 
It will ask for the input file [pdf] , and behind the screen, it will create a Job Id (6 digit random number) and stores the input file.

[Screenshot]

Now, the screen will be routed to Generate screen, where the button with Text "Generate Content Using Random Video available from the library" will be displayed [Need to extend the functionality with getting a input from the User]. On clicking this button, it start processing the content. post successful processing the output file will be downloaded automatically.

[Screenshot]
