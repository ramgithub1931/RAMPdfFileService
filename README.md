# RAMPdfFileService
RAMPdfFileService is a windows service that monitors a folder for new folders of pdfs.  It will fill out any fields within the pdfs and merge them into one pdf when finished.  It can then print to a physical printer and/or move the finished pdf to a different location.
## Practical Uses
* Fill out form fillable pdfs from data pulled from a database or some other source.
* Merge multiple pdfs into one.

The way we use this service is a combination of the two listed above.  We used to use software to place data on a pdf and then overlay a pdf on top of that.  We had to print the data perfectly to get things lined up on top of the overlay and if that overlaid pdf ever changed formats we needed to go through and relign everything.  Now we can simple modify the form fillable version as needed and the data will populate exactly where it needs to be.
## Getting Started
### Prerequisites
* Visual Studio 2017
* [Installer Tool](https://docs.microsoft.com/en-us/dotnet/framework/tools/installutil-exe-installer-tool)
### Installing
You should only need to download the code, unzip, open, and build the solution.
## Deployment
* Build the solution in Release mode.  This will create a series of files in the /bin/Release folder.
* Create a config.json file in that same folder.  This is what RAMPdfFileService uses to configure the service on installation.
#### config.json
```
{
  "WatcherDirectory": "",
  "InternalBufferSize": 8192, 
  "DoSaveBackups": false,
  "DoSaveErrors": false
}
```
Field | Type | Note
------|------|------
WatcherDirectory|string|The directory to watch for incoming folders.
InternalBufferSize|number|The size of the internal buffer used to process requests.  If processing many folders at once this may need to be insureased.  Use multiples of 4096 for best results.
DoSaveBackups|boolean|If true backups of all folders processed will be saved to the backups directory.
DoSaveErrors|boolean|If true all folders that errored will be saved to the backups directory.

* Copy all of these files to the server you wish to install RAMPdfFileService.
* Create a directory on that the same server where you want RAMPdfFileService to monitor.
* Open a command prompt and navigate to the directory where you placed RAMPdfFileService.
* Type 'installutil RAMPdfFileService.exe' to install the service.
* Open task manager, find the RAMPdfFileService service, right click and Start.
## How To Use
You will need to create folders of pdfs and a pdf.json file and drop them into the directory you told RAMPdfFileService to watch (WatcherDirectory).  RAMPdfFileService will do nothing with folders not containing a pdf.json file and will report an error to the RAMPdfFileServiceLog in the Event Viewer.  For our purposes we automated this from the database to create these folders and move them to our watcher directory.

RAMPdfFileService will read the pdf.json file to get the names of the pdfs to process.  It will then loop through them one by one.  If there are Fields to process it will populate those with the values.  As it goes it will continue to merge the pdfs together to produce one final pdf, and name it based on the Filename provided.  Once finished, it will send the pdf to a printer if PrinterPath is provided and/or copy the pdf to a directory if DestinationPath was provided.
## pdf.json
```
{
  "PrinterPath": "",
  "DestinationPath": "C:\\Users\\akrause\\Desktop",
  "Filename": "TheNewOne",
  "Pdfs": [
    {
      "Filename": "Dec.pdf",
      "Fields": null
    },
    {
      "Filename": "SampleForm.pdf",
      "Fields": [
        {
          "Name": "Name",
          "Value": "FRED FLINTSTONE"
        },
{
          "Name": "Check Box1",
          "Value": "On"
        }
      ]
    }
  ]
}
```
Field | Type | Note
------|------|------
PrinterPath|string|The path to the physical printer for printing the finished pdf.
DestinationPath|number|The path to the directory to copy the finished pdf to.
Filename|string|The name of the finished pdf.
Pdfs|array|The pdfs to process.
