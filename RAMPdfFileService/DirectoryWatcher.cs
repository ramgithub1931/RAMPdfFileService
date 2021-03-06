﻿/*
    Copyright (C) 2018 RAM Mutual Insurance Company

    This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
    FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License along with this program. If not, see http://www.gnu.org/licenses/.  
*/
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAMPdfFileService
{
    class DirectoryWatcher : IDisposable
    {
        #region Data Fields

        private readonly string _errorsPath;
        private readonly string _backupsPath;
        private readonly string _workPath;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private bool _doSaveBackups;
        private bool _doSaveErrors;

        #endregion

        #region Constructors

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public DirectoryWatcher(DirectoryWatcherConfiguration configuration)
        {
            if (!String.IsNullOrWhiteSpace(configuration.WatcherDirectory) && Directory.Exists(configuration.WatcherDirectory))
            {
                this._fileSystemWatcher = new FileSystemWatcher(configuration.WatcherDirectory)
                {
                    NotifyFilter = NotifyFilters.DirectoryName,
                    IncludeSubdirectories = false,
                    InternalBufferSize = configuration.InternalBufferSize
                };
                this.DoSaveBackups = configuration.DoSaveBackups;
                this.DoSaveErrors = configuration.DoSaveErrors;
                this._fileSystemWatcher.Created += DirectoryWatcher_OnCreated;

                #region Create subdirecories used for processing files

                this._errorsPath = System.IO.Path.Combine(configuration.WatcherDirectory, "errors");

                if (!Directory.Exists(this._errorsPath))
                {
                    Directory.CreateDirectory(this._errorsPath, Directory.GetAccessControl(configuration.WatcherDirectory));
                    EventLogManager.WriteInformation(String.Format("Errors directory created at {0}", this._errorsPath));
                }

                this._backupsPath = System.IO.Path.Combine(configuration.WatcherDirectory, "backups");

                if (!Directory.Exists(this._backupsPath))
                {
                    Directory.CreateDirectory(this._backupsPath, Directory.GetAccessControl(configuration.WatcherDirectory));
                    EventLogManager.WriteInformation(String.Format("Backups directory created at {0}", this._backupsPath));
                }

                this._workPath = System.IO.Path.Combine(configuration.WatcherDirectory, "work");

                if (!Directory.Exists(this._workPath))
                {
                    Directory.CreateDirectory(this._workPath, Directory.GetAccessControl(configuration.WatcherDirectory));
                    EventLogManager.WriteInformation(String.Format("Work directory created at {0}", this._workPath));
                }

                #endregion
            }
            else
            {
                EventLogManager.WriteError(new Exception(String.Format("Path {0} does not exist.", configuration.WatcherDirectory)));
            }
        }

        #endregion

        #region Properties

        public bool DoSaveBackups
        {
            get
            {
                return _doSaveBackups;
            }
            set
            {
                _doSaveBackups = value;
            }
        }

        public bool DoSaveErrors
        {
            get
            {
                return _doSaveErrors;
            }
            set
            {
                _doSaveErrors = value;
            }
        }

        public string ErrorsPath
        {
            get
            {
                return this._errorsPath;
            }
        }

        public string BackupsPath
        {
            get
            {
                return this._backupsPath;
            }
        }

        public string WorkPath
        {
            get
            {
                return this._workPath;
            }
        }

        #endregion

        #region Methods

        private void DirectoryWatcher_OnCreated(object sender, FileSystemEventArgs e)
        {
            ProcessDirectory(e.FullPath);
        }
        
        private void ProcessDirectory(string directoryPath)
        {
            // DON'T TOUCH THE BACKUPS, ERRORS AND WORK DIRECTORIES.  Just in case they were made or renamed after the fact for some reason
            if (directoryPath != this._errorsPath && directoryPath != this._backupsPath && directoryPath != this._workPath)
            {
                string pdfJsonPath = System.IO.Path.Combine(directoryPath, "pdf.json");

                if (File.Exists(pdfJsonPath))
                {
                    string workPath = System.IO.Path.Combine(this._workPath, System.IO.Path.GetFileName(directoryPath));

                    try
                    {
                        CopyToDirectory(directoryPath, workPath);

                        PdfMerge pdfMerge = null;

                        string jsonPath = System.IO.Path.Combine(workPath, "pdf.json");
                        using (StreamReader r = Helpers.GetStreamReader(jsonPath))
                        {
                            string json = r.ReadToEnd();
                            pdfMerge = JsonConvert.DeserializeObject<PdfMerge>(json);
                        }

                        FillFormFields(workPath, pdfMerge);
                        MergePdfs(workPath, pdfMerge);
                        //NumberPages(workPath, pdfMerge);
                        FinishPdf(workPath, pdfMerge);

                        // Move original to backups directory
                        if (DoSaveBackups)
                        {
                            string backupsPath = System.IO.Path.Combine(this._backupsPath, String.Format("{0}_{1}", System.IO.Path.GetFileName(directoryPath), DateTime.Now.ToString("yyyyMMddHHmmss")));
                            Directory.Move(directoryPath, backupsPath);
                        }
                        else
                        {
                            Directory.Delete(directoryPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        EventLogManager.WriteError(ex);

                        if (DoSaveErrors)
                        {
                            // Move original to errors directory
                            string errorsPath = System.IO.Path.Combine(this._errorsPath, String.Format("{0}_{1}", System.IO.Path.GetFileName(directoryPath), DateTime.Now.ToString("yyyyMMddHHmmss")));
                            Directory.Move(directoryPath, errorsPath);
                        }
                        else
                        {
                            Directory.Delete(directoryPath, true);
                        }
                    }

                    // Delete work directory
                    Directory.Delete(workPath, true);
                }
                else
                {
                    EventLogManager.WriteInformation(String.Format("No pdf.json file.  {0} skipped.", directoryPath));
                }
            }
        }

        private void FillFormFields(string directoryPath, PdfMerge pdfMerge)
        {
            if (pdfMerge != null && pdfMerge.Pdfs != null)
            {
                string formPath;
                string newFilePath;
                PdfDocument document = null;
                PdfAcroForm form;
                PdfFormField pdfFormField;

                foreach (var pdf in pdfMerge.Pdfs)
                {
                    try
                    {
                        formPath = System.IO.Path.Combine(directoryPath, pdf.Filename);
                        newFilePath = System.IO.Path.Combine(
                            directoryPath,
                            String.Format("{0}.{1}", String.Format("{0}{1}", System.IO.Path.GetFileNameWithoutExtension(pdf.Filename), "_Revised"), System.IO.Path.GetExtension(pdf.Filename)));
                        document = new PdfDocument(Helpers.GetPdfReader(formPath), new PdfWriter(newFilePath));

                        form = PdfAcroForm.GetAcroForm(document, true);
                        if (pdf.Fields != null && pdf.Fields.Count > 0)
                        {
                            foreach (var field in pdf.Fields)
                            {
                                if (field.Value != null)
                                {
                                    pdfFormField = form.GetField(field.Name);

                                    if (pdfFormField != null)
                                    {
                                        form.GetField(field.Name).SetValue(field.Value);
                                        
                                        //form.GetField(field.Name).SetFontSize(0);
                                    }
                                    else
                                    {
                                        EventLogManager.WriteWarning(String.Format("Field '{0}' does not exist in '{1}'", field.Name, pdf.Filename));
                                        //document.Close();
                                        //throw new Exception(String.Format("Field '{0}' does not exist in '{1}'", field.Name, pdf.Filename));
                                    }
                                }
                            }
                        }

                        //The below will make sure the fields are not editable in
                        //the output PDF.
                        form.FlattenFields();  // Maybe make this an option in the json so we can partialy fill out forms at some point?
                        //document.Close();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (document != null && !document.IsClosed())
                        {
                            document.Close();
                        }
                    }

                    // Now rename the new one back to the old name
                    File.Delete(formPath);
                    File.Move(newFilePath, formPath);
                }
            }
        }

        private void MergePdfs(string directoryPath, PdfMerge pdfMerge)
        {
            if (pdfMerge != null && pdfMerge.Pdfs != null && pdfMerge.Pdfs.Count > 0)
            {
                string mergeFilePath = System.IO.Path.Combine(directoryPath, pdfMerge.Filename);
                PdfDocument pdfDocument = null;
                PdfDocument mergedPdf = null;
                PdfMerger merger = null;
                string filePath = null;

                try
                {
                    mergedPdf = new PdfDocument(new PdfWriter(mergeFilePath));
                    merger = new PdfMerger(mergedPdf);
                    foreach (var pdf in pdfMerge.Pdfs)
                    {
                        filePath = System.IO.Path.Combine(directoryPath, pdf.Filename);
                        pdfDocument = new PdfDocument(Helpers.GetPdfReader(filePath));
                        merger.Merge(pdfDocument, 1, pdfDocument.GetNumberOfPages());
                        pdfDocument.Close();
                    }

                    mergedPdf.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (pdfDocument != null)
                    {
                        pdfDocument.Close();
                    }
                    if (mergedPdf != null)
                    {
                        mergedPdf.Close();
                    }
                    if (merger != null)
                    {
                        merger.Close();
                    }
                }
            }
        }

        private void NumberPages(string directoryPath, PdfMerge pdfMerge)
        {
            string newFilePath = System.IO.Path.Combine(directoryPath, pdfMerge.Filename);
            string numberedFileName = String.Format("{0}_numbered{1}", System.IO.Path.GetFileNameWithoutExtension(pdfMerge.Filename), System.IO.Path.GetExtension(pdfMerge.Filename));
            string numberedFilePath = System.IO.Path.Combine(directoryPath, numberedFileName);
            PdfDocument pdfDocument = new PdfDocument(Helpers.GetPdfReader(newFilePath), new PdfWriter(numberedFilePath));
            Document document = new Document(pdfDocument);

            Paragraph paragraph = null;
            int n = pdfDocument.GetNumberOfPages();
            for (int i = 1; i <= n; i++)
            {
                //document.ShowTextAligned(new Paragraph(String.Format("page {0} of {1}", i, n)),
                //        50, 50, i, TextAlignment.RIGHT, VerticalAlignment.TOP, 0);
                paragraph = new Paragraph(String.Format("page {0} of {1}", i, n));
                paragraph.SetWidthPercent(100);
                paragraph.SetBorder(new SolidBorder(1));
                paragraph.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                document.ShowTextAligned(paragraph,
                        50, 50, i, TextAlignment.CENTER, VerticalAlignment.BOTTOM, 0);
            }
            document.Close();

            // Now rename the new one back to the old name
            File.Delete(newFilePath);
            File.Move(numberedFilePath, newFilePath);
            //EventLogManager.WriteInformation(String.Format("{0}|{1}|{2}", newFilePath, numberedFileName, numberedFilePath));
        }

        private void FinishPdf(string directoryPath, PdfMerge pdfMerge)
        {
            string newFilePath = System.IO.Path.Combine(directoryPath, pdfMerge.Filename);

            if (!String.IsNullOrWhiteSpace(pdfMerge.PrinterPath))
            {
                File.Copy(newFilePath, pdfMerge.PrinterPath, true);
            }

            if (!String.IsNullOrWhiteSpace(pdfMerge.DestinationPath))
            {                
                string destinationPath = System.IO.Path.Combine(pdfMerge.DestinationPath, pdfMerge.Filename);
                File.Copy(newFilePath, destinationPath, true);
                //File.Move(newFilePath, destinationPath);
            }
        }

        private void CopyToDirectory(string sourcePath, string destinationPath, bool allowOverwrite = false)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            else if (!allowOverwrite)
            {
                throw new Exception(String.Format("Directory {0} already exists.", destinationPath));
            }

            FileStream s = null;
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                try
                {
                    s = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (Exception)
                {
                    EventLogManager.WriteError("Cannot open file " + filePath);
                }

                if (s != null)
                {
                    s.Close();
                }

                File.Copy(filePath, System.IO.Path.Combine(destinationPath, System.IO.Path.GetFileName(filePath)));
            }
        }

        public void Start()
        {
            if (this._fileSystemWatcher != null)
            {
                #region Process anything sitting the watcher directory initially

                IEnumerable<string> initialDirectories = Directory.GetDirectories(this._fileSystemWatcher.Path).Where(d => d != this._workPath && d != this._backupsPath && d != this._errorsPath);
                if (initialDirectories != null && initialDirectories.Count() > 0)
                {
                    EventLogManager.WriteInformation(String.Format("Processing {0} initial directories.", initialDirectories.Count()));

                    foreach (var initialDirectory in initialDirectories)
                    {
                        ProcessDirectory(initialDirectory);
                        Thread.Sleep(100);
                    }
                }

                #endregion

                this._fileSystemWatcher.EnableRaisingEvents = true;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("RAMPdfFileService Started");
                sb.AppendLine(String.Format("Watching at: {0}", this._fileSystemWatcher.Path));
                sb.AppendLine(String.Format("Internal buffer size: {0}", this._fileSystemWatcher.InternalBufferSize));
                sb.AppendLine(String.Format("Saving backups: {0}", this.DoSaveBackups));
                sb.AppendLine(String.Format("Saving errors: {0}", this.DoSaveErrors));
                EventLogManager.WriteInformation(sb.ToString());
            }
            else
            {
                EventLogManager.WriteError("Unable to start RAMPdfFileService.  No FileSystemWatcher present.");
            }
        }

        public void Stop()
        {
            if (this._fileSystemWatcher != null)
            {
                this._fileSystemWatcher.EnableRaisingEvents = false;
            }

            this.Dispose();

            EventLogManager.WriteInformation("Stopped.");
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            this._fileSystemWatcher?.Dispose();
        }

        #endregion
    }
}
