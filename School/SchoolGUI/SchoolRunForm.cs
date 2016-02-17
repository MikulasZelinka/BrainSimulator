﻿using GoodAI.BrainSimulator.Forms;
using GoodAI.Core.Execution;
using GoodAI.Core.Observers;
using GoodAI.Core.Utils;
using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace GoodAI.School.GUI
{
    public partial class SchoolRunForm : DockContent
    {
        public List<LearningTaskNode> Data;
        public PlanDesign Design;

        private readonly MainForm m_mainForm;
        private string m_runName;
        private SchoolWorld m_school;
        private bool m_showObserver;
        private ObserverForm m_observer;

        private int m_currentRow = -1;
        private int m_stepOffset = 0;
        private DateTime? m_ltStart = null;

        public string RunName
        {
            get { return m_runName; }
            set
            {
                m_runName = value;

                string result = "School run";
                if (!String.IsNullOrEmpty(m_runName))
                    result += " - " + RunName;
                this.Text = result;
            }
        }

        public SchoolRunForm(MainForm mainForm)
        {
            m_mainForm = mainForm;
            InitializeComponent();

            observerCheckBox.Checked = Properties.School.Default.ShowVisual;
            m_showObserver = observerCheckBox.Checked;

            // here so it does not interfere with designer generated code
            btnRun.Click += new System.EventHandler(m_mainForm.runToolButton_Click);
            btnStop.Click += new System.EventHandler(m_mainForm.stopToolButton_Click);
            btnPause.Click += new System.EventHandler(m_mainForm.pauseToolButton_Click);
            btnStepOver.Click += new System.EventHandler(m_mainForm.stepOverToolButton_Click);
            btnDebug.Click += new System.EventHandler(m_mainForm.debugToolButton_Click);

            m_mainForm.SimulationHandler.StateChanged += UpdateButtons;
            m_mainForm.SimulationHandler.ProgressChanged += SimulationHandler_ProgressChanged;
            UpdateButtons(null, null);
        }

        private void SimulationHandler_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (m_school == null)
                return;
            ILearningTask actualTask = m_school.CurrentLearningTask;
            if (actualTask == null)
                return;
            if (m_ltStart == null)  // for the first LT
                m_ltStart = DateTime.UtcNow;

            int simStep = (int)m_mainForm.SimulationHandler.SimulationStep;
            if (m_currentRow < 0 || actualTask.GetType() != Data.ElementAt(m_currentRow).TaskType) // next LT
            {
                m_currentRow++;
                HighlightCurrentTask();
                m_stepOffset = simStep;
                DateTime end = DateTime.UtcNow;
                m_ltStart = end;
            }
            if (actualTask.GetType() != Data.ElementAt(m_currentRow).TaskType) //should not happen at all - just a safeguard
            {
                MyLog.ERROR.WriteLine("One of the Learning Tasks was skipped. Stopping simulation.");
                return;
            }

            LearningTaskNode node = Data.ElementAt(m_currentRow);
            node.Steps = simStep - m_stepOffset;
            TimeSpan? diff = DateTime.UtcNow - m_ltStart;
            if (diff != null)
                node.Time = (float)Math.Round(diff.Value.TotalSeconds, 2);

            UpdateData();
        }

        private void UpdateButtons(object sender, MySimulationHandler.StateEventArgs e)
        {
            btnRun.Enabled = m_mainForm.runToolButton.Enabled;
            btnPause.Enabled = m_mainForm.pauseToolButton.Enabled;
            btnStop.Enabled = m_mainForm.stopToolButton.Enabled;
        }

        public void Ready()
        {
            UpdateData();
            PrepareSimulation();
            SetObserver();
            if (Properties.School.Default.AutorunEnabled && Data != null)
                btnRun.PerformClick();
        }

        public void UpdateData()
        {
            dataGridView1.DataSource = Data;
            dataGridView1.Invalidate();

        }

        private void SetObserver()
        {
            if (m_showObserver)
            {
                if (m_observer == null)
                {
                    try
                    {
                        MyMemoryBlockObserver observer = new MyMemoryBlockObserver();
                        observer.Target = m_school.Visual;

                        if (observer == null)
                            throw new InvalidOperationException("No observer was initialized");

                        m_observer = new ObserverForm(m_mainForm, observer, m_school);

                        m_observer.TopLevel = false;
                        observerDockPanel.Controls.Add(m_observer);
                        
                        m_observer.CloseButtonVisible = false;
                        m_observer.MaximizeBox = false;
                        m_observer.Size = observerDockPanel.Size + new System.Drawing.Size(16, 38);
                        m_observer.Location = new System.Drawing.Point(-8, -30);

                        m_observer.Show();
                    }
                    catch (Exception e)
                    {
                        MyLog.ERROR.WriteLine("Error creating observer: " + e.Message);
                    }
                }
                else
                {
                    m_observer.Show();
                    observerDockPanel.Show();
                }
            }
            else
            {
                if (m_observer != null)
                {
                    observerDockPanel.Hide();
                }
            }
        }

        private void SelectSchoolWorld()
        {
            m_mainForm.SelectWorldInWorldList(typeof(SchoolWorld));
            m_school = (SchoolWorld)m_mainForm.Project.World;
        }

        private void CreateCurriculum()
        {
            m_school.Curriculum = Design.AsSchoolCurriculum(m_school);
            foreach (ILearningTask task in m_school.Curriculum)
                task.SchoolWorld = m_school;
        }

        private void HighlightCurrentTask()
        {
            if (m_currentRow < 0)
                return;

            DataGridViewCellStyle defaultStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle highlightStyle = new DataGridViewCellStyle();
            highlightStyle.BackColor = Color.PaleGreen;

            foreach (DataGridViewRow row in dataGridView1.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    if (row.Index == m_currentRow)
                        cell.Style = highlightStyle;
                    else
                        cell.Style = defaultStyle;
        }

        private void PrepareSimulation()
        {
            // data
            SelectSchoolWorld();
            CreateCurriculum();

            // gui
            m_stepOffset = 0;
            HighlightCurrentTask();
            Data.ForEach(x => x.Steps = 0);
            Data.ForEach(x => x.Time = 0f);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            string columnName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (columnName.Equals(TaskType.Name) || columnName.Equals(WorldType.Name))
            {
                // I am not sure about how bad this approach is, but it get things done
                if (e.Value != null)
                {
                    Type typeValue = e.Value as Type;

                    DisplayNameAttribute displayNameAtt = typeValue.GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAtt != null)
                        e.Value = displayNameAtt.DisplayName;
                    else
                        e.Value = typeValue.Name;
                }
            }
        }

        private void SchoolRunForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    {
                        btnRun.PerformClick();
                        break;
                    }
                case Keys.F7:
                    {
                        btnPause.PerformClick();
                        break;
                    }
                case Keys.F8:
                    {
                        btnStop.PerformClick();
                        break;
                    }
                case Keys.F10:
                    {
                        btnStepOver.PerformClick();
                        break;
                    }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.School.Default.ShowVisual = (sender as CheckBox).Checked;
            Properties.School.Default.Save();

            CheckBox c = (CheckBox)sender;
            m_showObserver = c.Checked;
            SetObserver();
        }

        private void simulationStart(object sender, EventArgs e)
        {
            if (m_mainForm.SimulationHandler.State == MySimulationHandler.SimulationState.STOPPED)
                PrepareSimulation();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {

        }
    }
}
