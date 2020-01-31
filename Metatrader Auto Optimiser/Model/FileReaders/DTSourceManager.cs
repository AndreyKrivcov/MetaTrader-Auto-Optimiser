using Metatrader_Auto_Optimiser.View_Model;
using ReportManager;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Metatrader_Auto_Optimiser.Model.FileReaders
{
    class DTSourceManager
    {
        public static List<KeyValuePair<DateBorders, OptimisationType>> GetBorders(string pathToFile)
        {
            XmlDocument document = new XmlDocument();
            document.Load(pathToFile);

            List<KeyValuePair<DateBorders, OptimisationType>> borders = new List<KeyValuePair<DateBorders, OptimisationType>>();

            foreach (XmlNode item in document["Borders"].ChildNodes)
            {
                DateBorders borderItem = new DateBorders(
                    DateTime.ParseExact(item["From"].InnerText, "dd.MM.yyyy HH:mm:ss.fff", null),
                    DateTime.ParseExact(item["Till"].InnerText, "dd.MM.yyyy HH:mm:ss.fff", null));
                OptimisationType type = (OptimisationType)Enum.Parse(typeof(OptimisationType), item["Type"].InnerText);

                borders.Add(new KeyValuePair<DateBorders, OptimisationType>(borderItem, type));
            }

            return borders;
        }
        public static void SaveBorders(IEnumerable<DateBordersItem> borders, string pathToFile)
        {
            using (var xmlWriter = new XmlTextWriter(pathToFile, null))
            {
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.IndentChar = '\t';
                xmlWriter.Indentation = 1;

                xmlWriter.WriteStartDocument();

                xmlWriter.WriteStartElement("Borders");

                foreach (var item in borders)
                {
                    xmlWriter.WriteStartElement("Item");

                    xmlWriter.WriteStartElement("From");
                    xmlWriter.WriteString(item.From.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("Till");
                    xmlWriter.WriteString(item.Till.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("Type");
                    xmlWriter.WriteString(item.BorderType.ToString());
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndDocument();
            }
        }
    }
}
