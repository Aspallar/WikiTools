using System.Collections.Generic;
using System.Xml;

namespace UploadFiles
{
    internal class UploadResponse
    {
        public string Xml { get; private set; }
        public string Result { get; private set; }
        public bool AlreadyExists { get; private set; }
        public bool BadFilename { get; private set; }
        public string ArchiveDuplicate { get; set; }
        public List<string> Duplicates { get; private set; }
        public List<ApiError> Errors { get; private set; }
        public bool UnwantedFileType { get; private set; }
        public bool LargeFile { get; set; }
        public bool EmptyFile { get; private set; }

        public bool IsDuplicate => Duplicates.Count > 0;
        public bool IsDuplicateOfArchive => !string.IsNullOrEmpty(ArchiveDuplicate);
        public bool IsError => Errors.Count > 0;

        public UploadResponse(string xml)
        {
            Xml = xml;
            Duplicates = new List<string>();
            Errors = new List<ApiError>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode upload = doc.SelectSingleNode("/api/upload");
            if (upload != null)
            {
                Result = upload.Attributes["result"].Value;

                if (Result == ResponseCodes.Warning)
                {
                    XmlNode warnings = upload.SelectSingleNode("warnings");

                    if (warnings.Attributes["exists"] != null)
                        AlreadyExists = true;

                    if (warnings.Attributes["badfilename"] != null)
                        BadFilename = true;

                    if (warnings.Attributes["duplicate-archive"] != null)
                        ArchiveDuplicate = warnings.Attributes["duplicate-archive"].Value;

                    if (warnings.Attributes["filetype-unwanted-type"] != null)
                        UnwantedFileType = true;

                    if (warnings.Attributes["large-file"] != null)
                        LargeFile = true;

                    if (warnings.Attributes["emptyfile"] != null)
                        EmptyFile = true;

                    XmlNodeList duplicates = warnings.SelectNodes("duplicate/duplicate");
                    foreach (XmlNode node in duplicates)
                        Duplicates.Add(node.InnerText);
                }
            }
            else
            {
                Result = ResponseCodes.NoResult;
            }

            XmlNodeList errors = doc.SelectNodes("/api/error");
            foreach (XmlNode node in errors)
            {
                Errors.Add(new ApiError
                {
                    Code = node.Attributes["code"]?.Value,
                    Info = node.Attributes["info"]?.Value,
                });
            }
        }
    }
}
