using System.Collections.Generic;
using System.Xml;

namespace UploadFiles
{
    internal class UploadResponse
    {
        public string Xml { get; private set; }
        public string Result { get; private set; }
        public bool AlreadyExists { get; private set; }
        public List<string> Duplicates { get; private set; }

        public UploadResponse(string xml)
        {
            Xml = xml;
            Duplicates = new List<string>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode upload = doc.SelectSingleNode("/api/upload");
            Result = upload.Attributes["result"].Value;

            if (Result == ResponseCodes.Warning)
            {

                XmlNode warnings = upload.SelectSingleNode("warnings");
                if (warnings.Attributes["exists"] != null)
                    AlreadyExists = true;
                XmlNodeList duplicates = warnings.SelectNodes("duplicate/duplicate");
                foreach (XmlNode node in duplicates)
                    Duplicates.Add(node.InnerText);
            }
        }
    }
}
