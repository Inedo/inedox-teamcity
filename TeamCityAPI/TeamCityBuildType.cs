using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public class TeamCityBuildType
    {

        public string id { get; private set; }
        public string name { get; private set; }
        public string projectName { get; private set; }
        public string projectId { get; private set; }
        public string href { get; private set; }

        public string[] ProjectNameParts
        {
            get
            {
                return this.projectName.Split(new[] { " :: " }, StringSplitOptions.None);
            }
        }

        private XmlDocument _doc;

        public TeamCityBuildType() { }

        public TeamCityBuildType(string xml, string root = "/") {
            this.Update(xml, root);
        }

        private string getValue(string path, string defaultValue = "")
        {
            return (_doc.SelectSingleNode(path) != null) ? _doc.SelectSingleNode(path).Value : defaultValue;
        }

        private string getInnerText(string path, string defaultValue = "")
        {
            return (_doc.SelectSingleNode(path) != null) ? _doc.SelectSingleNode(path).InnerText : defaultValue;
        }

        public void Update(string xml, string root = "/")
        {
            _doc = new XmlDocument();
            _doc.LoadXml(xml);

            this.id = getValue($"{root}buildType/@id");
            this.name = getValue($"{root}buildType/@name");
            this.projectName = getValue($"{root}buildType/@projectName");
            this.projectId = getValue($"{root}buildType/@projectId");
            this.href = getValue($"{root}buildType/@href");

            _doc = null;
        }
    }
}
