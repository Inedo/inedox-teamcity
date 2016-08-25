using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public class TeamCityBuild
    {

        public enum BuildStatuses { Unknown, Success, Failure, Error }

        public string id { get; private set; }
        public string state { get; private set; }
        public string buildTypeId { get; private set; }
        public string branchName { get; private set; }
        public string href { get; private set; }
        public string statusText { get; private set; }
        public string number { get; private set; }
        public BuildStatuses status { get; private set; }
        public int percentageComplete { get; private set; }
        public string finishDate { get; private set; }
        public string startDate { get; private set; }
        public string queuedDate { get; private set; }
        public string artifacts_href { get; private set; }
        public bool running { get; private set; }
        public string currentStageText { get; private set; }
        public string name { get; private set; }
        public string projectName { get; private set; }

        public string[] ProjectNameParts
        {
            get
            {
                return this.projectName.Split(new[] { " :: " }, StringSplitOptions.None);
            }
        }

        public string waitReason { get; private set; }

        private XmlDocument _doc;

        public TeamCityBuild() { }
        public TeamCityBuild(string xml) { this.Update(xml);  }

        private string getValue(string path, string defaultValue = "")
        {
            return (_doc.SelectSingleNode(path) != null) ? _doc.SelectSingleNode(path).Value : defaultValue;
        }

        private string getInnerText(string path, string defaultValue = "")
        {
            return (_doc.SelectSingleNode(path) != null) ? _doc.SelectSingleNode(path).InnerText : defaultValue;
        }

        public void Update(string xml)
        {
            _doc = new XmlDocument();
            _doc.LoadXml(xml);

            this.id = getValue("/build/@id");
            this.buildTypeId = getValue("/build/@buildTypeId"); // build configuration id
            this.number = getValue("/build/@number"); //  version 
            this.status = (BuildStatuses)Enum.Parse(typeof(BuildStatuses), getValue("/build/@status", "Unknown"), true);
            this.state = getValue("/build/@state");
            this.running = bool.Parse(getValue("/build/@running", "false"));
            this.percentageComplete = this.running ? 100 : int.Parse(getValue("/build/@percentageComplete", "0"));

            this.branchName = getValue("/build/@branchName");
            this.href = getValue("/build/@href");
            this.artifacts_href = getValue("/build/artifacts/@href");

            this.name = getValue("/build/buildType/@name");
            this.projectName = getValue("/build/buildType/@projectName");

            this.statusText = getInnerText("/build/statusText");

            this.waitReason = getInnerText("/build/waitReason");

            if (this.running)
            {
                // record running info
                this.percentageComplete = int.Parse(getValue("/build/running-info/@percentageComplete", "0"));
                this.currentStageText = getValue("/build/running-info/@currentStageText");
            }

            this.queuedDate = getInnerText("/build/queuedDate");

            this.startDate = getInnerText("/build/startDate");

            this.finishDate = getInnerText("/build/finishDate");

            _doc = null;
        }
    }
}
