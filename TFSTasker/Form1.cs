using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;
using System.Xml;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFSTasker
{
    public partial class Form1 : Form
    {
        const string tfsUrlKey = "tfsProjectCollectionUrl";
        const string projectNameKey = "projectName";

        List<string> taskTitles;

        public Form1()
        {
            InitializeComponent();

            taskTitles = new List<string>();
            GetTasks();

        }

        private void GetTasks()
        {
            XmlDocument tasksXml = new XmlDocument();
            tasksXml.Load("tasks.xml");
            var tasksNode = tasksXml.SelectSingleNode("Tasks");
            foreach (XmlNode task in tasksNode.ChildNodes)
            {
                string taskTitle = task.Attributes["Title"].Value;
                taskTitles.Add(taskTitle);
            }
        }

        WorkItemStore workItemStore;
        Project project;
        Project GetProject()
        {
            if (this.project == null)
            {
                string tfsUrl = ConfigurationManager.AppSettings[tfsUrlKey];
                string projectName = ConfigurationManager.AppSettings[projectNameKey];

                var tfs = new TfsTeamProjectCollection(new Uri(tfsUrl));
                tfs.EnsureAuthenticated();

                workItemStore = tfs.GetService<WorkItemStore>();

                this.project = workItemStore.Projects[projectName];
            }

            return project;
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            string usn = tbUSNo.Text;

            if (!string.IsNullOrEmpty(usn))
            {
                AddLog("---------------------");
                int workItemId = 0;

                if (!Int32.TryParse(usn, out workItemId))
                {
                    return;
                }

                var project = GetProject();


                // Get the user story
                var backlogItem = GetBackLogItem(workItemId);
                string iterationPath = backlogItem.IterationPath;
                DialogResult doIt = MessageBox.Show("Link to " + backlogItem.Title, "Link to", MessageBoxButtons.YesNo);

                if (doIt == System.Windows.Forms.DialogResult.Yes)
                {
                    foreach (string taskTitle in this.taskTitles)
                    {
                        var task = CreateTask(
                            project,
                            string.Format(taskTitle, backlogItem.Title),
                            iterationPath);
                        var taskLink = CreateTaskLink(task);
                        UpdateBackLogItemLink(backlogItem, taskLink);
                    }
                }
            }
        }

        private WorkItem GetBackLogItem(int workItemId)
        {
            var backlogItem = this.workItemStore.GetWorkItem(workItemId);
            AddLog("Getting backlog item -" + backlogItem.Id + " - " + backlogItem.Title);
            return backlogItem;
        }

        private void UpdateBackLogItemLink(WorkItem backlogItem, WorkItemLink taskLink1)
        {
            backlogItem.Links.Add(taskLink1);
            AddLog("updating backlogworkitem link - " + taskLink1.TargetId.ToString() + "- " + taskLink1.SourceId.ToString());
            backlogItem.Save();
        }

        private WorkItemLink CreateTaskLink(WorkItem task1)
        {
            var linkType = workItemStore.WorkItemLinkTypes["System.LinkTypes.Hierarchy"];
            var taskLink = new WorkItemLink(linkType.ForwardEnd, task1.Id);

            AddLog("created link- " + task1.Id.ToString());
            return taskLink;
        }

        private WorkItem CreateTask(Project project, string taskTitle, string iterationPath)
        {
            // Create the tasks
            var taskType = project.WorkItemTypes["Task"];
            var task = new WorkItem(taskType);
            task.IterationPath = iterationPath;
            //task.State = "New";

            task.Title = taskTitle;
            task.Save();

            AddLog("created task - " + task.Id.ToString());
            return task;
        }

        private void AddLog(string text)
        {
            listBox1.Items.Add(text);
            Console.WriteLine(text);
        }
    }
}
